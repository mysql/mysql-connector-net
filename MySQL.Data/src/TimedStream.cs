// Copyright (c) 2009, 2019, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
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
      if (newValue == System.Threading.Timeout.Infinite
          && currentValue != newValue)
        return true;
      if (newValue > currentValue)
        return true;
      return currentValue >= newValue + 100;
    }
    private void StartTimer(IOKind op)
    {

      int streamTimeout;

      if (_timeout == System.Threading.Timeout.Infinite)
        streamTimeout = System.Threading.Timeout.Infinite;
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

      if (_timeout == System.Threading.Timeout.Infinite)
        return;

      _stopwatch.Start();
    }
    private void StopTimer()
    {
      if (_timeout == System.Threading.Timeout.Infinite)
        return;

      _stopwatch.Stop();

      // Normally, a timeout exception would be thrown  by stream itself, 
      // since we set the read/write timeout  for the stream.  However 
      // there is a gap between  end of IO operation and stopping the 
      // stop watch,  and it makes it possible for timeout to exceed 
      // even after IO completed successfully.
      if (_stopwatch.ElapsedMilliseconds > _timeout)
      {
        ResetTimeout(System.Threading.Timeout.Infinite);
        throw new TimeoutException("Timeout in IO operation");
      }
    }
    public override bool CanRead => _baseStream.CanRead;

    public override bool CanSeek => _baseStream.CanSeek;

    public override bool CanWrite => _baseStream.CanWrite;

    public override void Flush()
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

    public override int Read(byte[] buffer, int offset, int count)
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

    public override void Write(byte[] buffer, int offset, int count)
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
      if (newTimeout == System.Threading.Timeout.Infinite || newTimeout == 0)
        _timeout = System.Threading.Timeout.Infinite;
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
