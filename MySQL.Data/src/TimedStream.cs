// Copyright © 2009, 2026, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using MySql.Data.Common;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Stream that supports timeout of IO operations.
  /// This class is used is used to support timeouts for SQL command, where a 
  /// typical operation involves several network reads/writes. 
  /// Timeout here is defined as the accumulated duration of all IO operations.
  /// </summary>
  internal class TimedStream : Stream
  {
    readonly Stream _baseStream;

    int _timeout;
    int _lastReadTimeout;
    int _lastWriteTimeout;
    readonly LowResolutionStopwatch _stopwatch;

    internal bool IsClosed { get; private set; }

    enum IOKind
    {
      Read,
      Write
    };

    /// <summary>
    /// Construct a TimedStream
    /// </summary>
    /// <param name="baseStream"> Undelying stream</param>
    public TimedStream(Stream baseStream)
    {
      this._baseStream = baseStream;
      _timeout = baseStream.CanTimeout ? baseStream.ReadTimeout : System.Threading.Timeout.Infinite;
      IsClosed = false;
      _stopwatch = new LowResolutionStopwatch();
    }

    /// <summary>
    /// Figure out whether it is necessary to reset timeout on stream.
    /// We track the current value of timeout and try to avoid
    /// changing it too often, because setting Read/WriteTimeout property
    /// on network stream maybe a slow operation that involves a system call 
    /// (setsockopt). Therefore, we allow a small difference, and do not 
    /// reset timeout if current value is slightly greater than the requested
    /// one (within 0.1 second).
    /// </summary>
    private bool ShouldResetStreamTimeout(int currentValue, int newValue)
    {
      if (!_baseStream.CanTimeout) return false;
      if (newValue == Timeout.Infinite
          && currentValue != newValue)
        return true;
      if (newValue > currentValue)
        return true;
      return currentValue >= newValue + 100;
    }

    /// <returns>
    /// The remaining time budget in milliseconds at the moment this I/O operation started, for
    /// use by the async deadline (<see cref="RunWithDeadlineAsync{T}"/>) - or exactly
    /// <see cref="Timeout.Infinite"/> if no timeout is configured at all. Clamped to a minimum
    /// of 0 whenever a real (non-infinite) timeout is configured, specifically so a budget that
    /// has merely expired (accumulated elapsed time caught up with or overshot <see cref="_timeout"/>
    /// by exactly 1ms) can never numerically collide with <see cref="Timeout.Infinite"/> (-1) -
    /// which would otherwise be misread as "no timeout at all" instead of "expired" by
    /// RunWithDeadlineAsync's own Timeout.Infinite check.
    /// </returns>
    private int StartTimer(IOKind op)
    {

      int streamTimeout;

      if (_timeout == Timeout.Infinite)
        streamTimeout = Timeout.Infinite;
      else
        streamTimeout = _timeout - (int)_stopwatch.ElapsedMilliseconds;

      if (op == IOKind.Read)
      {
        if (ShouldResetStreamTimeout(_lastReadTimeout, streamTimeout))
        {
          _baseStream.ReadTimeout = streamTimeout;
          _lastReadTimeout = streamTimeout;
        }
      }
      else
      {
        if (ShouldResetStreamTimeout(_lastWriteTimeout, streamTimeout))
        {
          _baseStream.WriteTimeout = streamTimeout;
          _lastWriteTimeout = streamTimeout;
        }
      }

      if (_timeout == Timeout.Infinite)
        return streamTimeout;

      _stopwatch.Start();

      // Clamp only the returned value, after the ReadTimeout/WriteTimeout property assignments
      // above (which intentionally still use the raw, unclamped value - unrelated to this fix).
      return Math.Max(0, streamTimeout);
    }

    /// <summary>
    /// Runs <paramref name="operation"/> against a deadline of <paramref name="timeoutMilliseconds"/>,
    /// in addition to honoring <paramref name="callerToken"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Stream.ReadTimeout"/>/<see cref="Stream.WriteTimeout"/> only apply to the
    /// synchronous <see cref="Stream.Read(byte[], int, int)"/>/<see cref="Stream.Write(byte[], int, int)"/>
    /// overloads - the *Async methods ignore them entirely and will await forever if the remote
    /// end stops responding mid-operation. This is what enforces the same accumulated timeout
    /// (<see cref="_timeout"/>, populated from <c>CommandTimeout</c>/"Default Command Timeout")
    /// for async I/O.
    ///
    /// <paramref name="callerToken"/> is honored if a caller ever passes a live one, but as of
    /// this writing no call site above TimedStream (MySqlStream.ReadFullyAsync and its callers)
    /// does - they all pass CancellationToken.None. This is forward-looking, not currently
    /// exercised in practice; don't assume external cancellation is wired end-to-end through the
    /// rest of the driver just because it's honored at this layer.
    /// </remarks>
#if NETFRAMEWORK
    private async Task<T> RunWithDeadlineAsync<T>(Func<CancellationToken, Task<T>> operation, int timeoutMilliseconds, CancellationToken callerToken)
    {
      // NetworkStream/SslStream on .NET Framework inherit Stream's default ReadAsync/WriteAsync,
      // which wraps the legacy BeginRead/EndRead APM pattern: it only checks a CancellationToken
      // before starting an operation, never once one is already in flight. So unlike every other
      // target this project builds for, cancelling deadlineCts (the #else branch's approach)
      // would have NO effect on an already-pending read/write here - the await would just never
      // return, reproducing the original hang bug. Race against a separate timer instead, and
      // forcibly close the underlying stream if that timer wins - that's the one thing
      // guaranteed to unblock an in-flight BeginRead/BeginWrite on this target.
      if (timeoutMilliseconds == Timeout.Infinite)
        return await operation(callerToken).ConfigureAwait(false);

      callerToken.ThrowIfCancellationRequested();
      Task<T> operationTask = operation(callerToken);

      using (var timerCts = new CancellationTokenSource())
      {
        Task delayTask = Task.Delay(Math.Max(timeoutMilliseconds, 0), timerCts.Token);
        Task winner = await Task.WhenAny(operationTask, delayTask).ConfigureAwait(false);

        if (winner == operationTask)
        {
          timerCts.Cancel(); // stop the now-unneeded delay promptly rather than leak a timer
          return await operationTask.ConfigureAwait(false); // observe the real result/exception
        }

        // Deadline elapsed; operationTask may genuinely still be running against the network -
        // force-close so it's guaranteed to fault rather than run (and hold the connection)
        // forever. This means the connection cannot be gracefully reused after an async timeout
        // on .NET Framework specifically (contrast the #else branch, where cancellation aborts
        // just the one pending operation and leaves the stream otherwise reusable) - it will
        // always be discarded via the existing MySqlConnection.HandleTimeoutOrThreadAbort fatal
        // path once it tries to use this now-closed stream - but that is a strict improvement
        // over hanging (and leaking the pooled connection) indefinitely, which is the bug this
        // fix exists for.
        try { _baseStream.Close(); } catch { /* best effort: already failing this call */ }
        IsClosed = true;

        // The abandoned task will eventually fault on its own time (from the forced close,
        // above) - observe that fault here so it doesn't surface later as an unobserved task
        // exception. Deliberately fire-and-forget: nothing here should (or can usefully) await it.
        _ = operationTask.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

        throw new TimeoutException("Timeout in IO operation");
      }
    }
#else
    private async Task<T> RunWithDeadlineAsync<T>(Func<CancellationToken, Task<T>> operation, int timeoutMilliseconds, CancellationToken callerToken)
    {
      if (timeoutMilliseconds == Timeout.Infinite)
        return await operation(callerToken).ConfigureAwait(false);

      // Skip CreateLinkedTokenSource's extra parent-token callback registration when
      // callerToken can never actually be cancelled (true for every current call site into
      // TimedStream - see remarks above); functionally identical either way, just cheaper for
      // what is, today, always the case.
      using (var deadlineCts = callerToken.CanBeCanceled
        ? CancellationTokenSource.CreateLinkedTokenSource(callerToken)
        : new CancellationTokenSource())
      {
        // A negative budget means earlier operations in this command already consumed the
        // whole timeout; cancel essentially immediately rather than let this op run unbounded.
        deadlineCts.CancelAfter(Math.Max(timeoutMilliseconds, 0));
        try
        {
          return await operation(deadlineCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
        {
          // Cancellation came from our own deadline, not the caller's token - surface it the
          // same way the synchronous safety-net check in StopTimer() does, so the existing
          // upstream handling (NativeDriver.GetResult[Async], MySqlDataReader.Read[Async],
          // MySqlConnection.HandleTimeoutOrThreadAbort[Async]) - all of which key off catching
          // a plain TimeoutException - applies identically to the async path.
          throw new TimeoutException("Timeout in IO operation");
        }
      }
    }
#endif

    private async Task RunWithDeadlineAsync(Func<CancellationToken, Task> operation, int timeoutMilliseconds, CancellationToken callerToken)
    {
      await RunWithDeadlineAsync(async ct =>
      {
        await operation(ct).ConfigureAwait(false);
        return true;
      }, timeoutMilliseconds, callerToken).ConfigureAwait(false);
    }

    private void StopTimer()
    {
      if (_timeout == Timeout.Infinite)
        return;

      _stopwatch.Stop();

      // Normally, a timeout exception would be thrown  by stream itself, 
      // since we set the read/write timeout  for the stream.  However 
      // there is a gap between  end of IO operation and stopping the 
      // stop watch,  and it makes it possible for timeout to exceed 
      // even after IO completed successfully.
      if (_stopwatch.ElapsedMilliseconds > _timeout)
      {
        ResetTimeout(Timeout.Infinite);
        throw new TimeoutException("Timeout in IO operation");
      }
    }

    public override bool CanRead => _baseStream.CanRead;

    public override bool CanSeek => _baseStream.CanSeek;

    public override bool CanWrite => _baseStream.CanWrite;

    public override void Flush() => FlushInternal();

    public override Task FlushAsync(CancellationToken cancellationToken = default) => FlushInternalAsync(cancellationToken);

    /// <summary>
    /// Performs the synchronous flush operation on the underlying stream with timeout support.
    /// </summary>
    private void FlushInternal()
    {
      try
      {
        StartTimer(IOKind.Write);
        _baseStream.Flush();
        StopTimer();
      }
      catch (Exception e)
      {
        HandleException(e);
        throw;
      }
    }

    /// <summary>
    /// Performs the asynchronous flush operation on the underlying stream with timeout support.
    /// </summary>
    private async Task FlushInternalAsync(CancellationToken cancellationToken)
    {
      try
      {
        int streamTimeout = StartTimer(IOKind.Write);
        await RunWithDeadlineAsync(ct => _baseStream.FlushAsync(ct), streamTimeout, cancellationToken).ConfigureAwait(false);
        StopTimer();
      }
      catch (Exception e)
      {
        HandleException(e);
        throw;
      }
    }

    public override long Length => _baseStream.Length;

    public override long Position
    {
      get
      {
        return _baseStream.Position;
      }
      set
      {
        _baseStream.Position = value;
      }
    }

    public override int Read(byte[] buffer, int offset, int count) => ReadInternal(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) => ReadInternalAsync(buffer, offset, count, cancellationToken);

    /// <summary>
    /// Reads from the underlying stream with timeout support.
    /// </summary>
    /// <param name="buffer">The buffer to read data into.</param>
    /// <param name="offset">The offset in the buffer to start reading into.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <returns>The total number of bytes read into the buffer.</returns>
    private int ReadInternal(byte[] buffer, int offset, int count)
    {
      try
      {
        StartTimer(IOKind.Read);
        int retval = _baseStream.Read(buffer, offset, count);
        StopTimer();
        return retval;
      }
      catch (Exception e)
      {
        HandleException(e);
        throw;
      }
    }

    /// <summary>
    /// Asynchronously reads from the underlying stream with timeout support. Unlike
    /// <see cref="Stream.ReadTimeout"/>, which the framework only honors on the synchronous
    /// <see cref="ReadInternal"/> path, the accumulated timeout here is enforced explicitly via
    /// <see cref="RunWithDeadlineAsync{T}"/> so that async callers (e.g. <c>ExecuteReaderAsync</c>)
    /// get the same <c>CommandTimeout</c> behavior as sync callers instead of awaiting indefinitely.
    /// </summary>
    /// <param name="buffer">The buffer to read data into.</param>
    /// <param name="offset">The offset in the buffer to start reading into.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">A token to observe in addition to the accumulated timeout.</param>
    /// <returns>A task that represents the asynchronous read operation, containing the total number of bytes read.</returns>
    private async Task<int> ReadInternalAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
      try
      {
        int streamTimeout = StartTimer(IOKind.Read);
        int retval = await RunWithDeadlineAsync(ct => _baseStream.ReadAsync(buffer, offset, count, ct), streamTimeout, cancellationToken).ConfigureAwait(false);
        StopTimer();
        return retval;
      }
      catch (Exception e)
      {
        HandleException(e);
        throw;
      }
    }

    public override int ReadByte()
    {
      try
      {
        StartTimer(IOKind.Read);
        int retval = _baseStream.ReadByte();
        StopTimer();
        return retval;
      }
      catch (Exception e)
      {
        HandleException(e);
        throw;
      }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      return _baseStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
      _baseStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count) => WriteInternal(buffer, offset, count);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) => WriteInternalAsync(buffer, offset, count, cancellationToken);

    /// <summary>
    /// Writes to the underlying stream with timeout support.
    /// </summary>
    /// <param name="buffer">The buffer containing data to write.</param>
    /// <param name="offset">The offset in the buffer to start writing from.</param>
    /// <param name="count">The maximum number of bytes to write.</param>
    private void WriteInternal(byte[] buffer, int offset, int count)
    {
      try
      {
        StartTimer(IOKind.Write);
        _baseStream.Write(buffer, offset, count);
        StopTimer();
      }
      catch (Exception e)
      {
        HandleException(e);
        throw;
      }
    }

    /// <summary>
    /// Asynchronously writes to the underlying stream with timeout support. See the remarks on
    /// <see cref="ReadInternalAsync"/> - the same accumulated-timeout gap applied here (this
    /// matters for e.g. bulk INSERT/LOAD DATA payloads written via the async API).
    /// </summary>
    /// <param name="buffer">The buffer containing data to write.</param>
    /// <param name="offset">The offset in the buffer to start writing from.</param>
    /// <param name="count">The maximum number of bytes to write.</param>
    /// <param name="cancellationToken">A token to observe in addition to the accumulated timeout.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    private async Task WriteInternalAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
      try
      {
        int streamTimeout = StartTimer(IOKind.Write);
        await RunWithDeadlineAsync(ct => _baseStream.WriteAsync(buffer, offset, count, ct), streamTimeout, cancellationToken).ConfigureAwait(false);
        StopTimer();
      }
      catch (Exception e)
      {
        HandleException(e);
        throw;
      }
    }

    public override bool CanTimeout => _baseStream.CanTimeout;

    public override int ReadTimeout
    {
      get { return _baseStream.ReadTimeout; }
      set { _baseStream.ReadTimeout = value; }
    }

    public override int WriteTimeout
    {
      get { return _baseStream.WriteTimeout; }
      set { _baseStream.WriteTimeout = value; }
    }

    public override void Close()
    {
      if (IsClosed)
        return;
      IsClosed = true;
      _baseStream.Close();
      _baseStream.Dispose();
    }

    public void ResetTimeout(int newTimeout)
    {
      if (newTimeout == Timeout.Infinite || newTimeout == 0)
        _timeout = Timeout.Infinite;
      else
        _timeout = newTimeout;
      _stopwatch.Reset();
    }

    /// <summary>
    /// Common handler for IO exceptions.
    /// Resets timeout to infinity if timeout exception is 
    /// detected and stops the times.
    /// </summary>
    /// <param name="e">original exception</param>
    void HandleException(Exception e)
    {
      _stopwatch.Stop();
      ResetTimeout(-1);
    }
  }
}
