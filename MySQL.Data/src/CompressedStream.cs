// Copyright © 2004, 2026, Oracle and/or its affiliates.
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
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Summary description for CompressedStream.
  /// </summary>
  internal class CompressedStream : Stream
  {
    // writing fields
    private Stream baseStream;
    private MemoryStream cache;

    // reading fields
    private byte[] localByte;
    private byte[] inBuffer;
    private byte[] lengthBytes;
    private WeakReference inBufferRef;
    private int inPos;
    private int maxInPos;
    private DeflateStream compInStream;

    public CompressedStream(Stream baseStream)
    {
      this.baseStream = baseStream;
      localByte = new byte[1];
      lengthBytes = new byte[7];
      cache = new MemoryStream();
      inBufferRef = new WeakReference(inBuffer, false);
    }

    #region Properties


    public override bool CanRead => baseStream.CanRead;

    public override bool CanWrite => baseStream.CanWrite;

    public override bool CanSeek => baseStream.CanSeek;

    public override long Length => baseStream.Length;

    public override long Position
    {
      get { return baseStream.Position; }
      set { baseStream.Position = value; }
    }

    #endregion

    public override void Close()
    {
      base.Close();
      baseStream.Close();
      cache.Dispose();
    }

    public override void SetLength(long value)
    {
      throw new NotSupportedException(Resources.CSNoSetLength);
    }

    public override int ReadByte()
    {
      try
      {
#if NET9_0_OR_GREATER
        ReadExactly(localByte, 0, 1);
#else
        Read(localByte, 0, 1);
#endif

        return localByte[0];
      }
      catch (EndOfStreamException)
      {
        return -1;
      }
    }

    public override bool CanTimeout => baseStream.CanTimeout;

    public override int ReadTimeout
    {
      get
      {
        return baseStream.ReadTimeout;
      }
      set
      {
        baseStream.ReadTimeout = value;
      }
    }

    public override int WriteTimeout
    {
      get
      {
        return baseStream.WriteTimeout;
      }
      set
      {
        baseStream.WriteTimeout = value;
      }
    }

    public override int Read(byte[] buffer, int offset, int count) => ReadInternal(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) => ReadInternalAsync(buffer, offset, count);

    /// <summary>
    /// Reads a sequence of bytes from the compressed stream and advances the position within the stream by the number of bytes read.
    /// </summary>
    /// <param name="buffer">The buffer to write the data into.</param>
    /// <param name="offset">The byte offset in buffer at which to begin writing data from the stream.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
    private int ReadInternal(byte[] buffer, int offset, int count)
    {
      ValidateReadArgs(buffer, offset, count);
      if (inPos == maxInPos)
        PrepareNextPacket();

      int countToRead = Math.Min(count, maxInPos - inPos);
      int countRead = ReadFromCurrent(buffer, offset, countToRead);
      inPos += countRead;
      CleanupIfNeeded();

      return countRead;
    }

    /// <summary>
    /// Asynchronously reads a sequence of bytes from the compressed stream and advances the position within the stream by the number of bytes read.
    /// </summary>
    /// <param name="buffer">The buffer to write the data into.</param>
    /// <param name="offset">The byte offset in buffer at which to begin writing data from the stream.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <returns>A task that represents the asynchronous read operation. The value of the task result contains the total number of bytes read into the buffer.</returns>
    private async Task<int> ReadInternalAsync(byte[] buffer, int offset, int count)
    {
      ValidateReadArgs(buffer, offset, count);
      if (inPos == maxInPos)
        await PrepareNextPacketAsync().ConfigureAwait(false);

      int countToRead = Math.Min(count, maxInPos - inPos);
      int countRead = await ReadFromCurrentAsync(buffer, offset, countToRead).ConfigureAwait(false);
      inPos += countRead;
      CleanupIfNeeded();

      return countRead;
    }

    /// <summary>
    /// Validates the arguments for read operations to ensure they are valid.
    /// </summary>
    /// <param name="buffer">The buffer to read into. Must not be null.</param>
    /// <param name="offset">The byte offset in the buffer at which to begin writing data. Must be within the buffer bounds.</param>
    /// <param name="count">The maximum number of bytes to read. The sum of offset and count must not exceed the buffer length.</param>
    private void ValidateReadArgs(byte[] buffer, int offset, int count)
    {
      if (buffer == null)
        throw new ArgumentNullException(nameof(buffer), Resources.BufferCannotBeNull);

      if (offset < 0 || offset >= buffer.Length)
        throw new ArgumentOutOfRangeException(nameof(offset), Resources.OffsetMustBeValid);

      if ((offset + count) > buffer.Length)
        throw new ArgumentException(Resources.BufferNotLargeEnough, nameof(buffer));
    }

    /// <summary>
    /// Performs the actual read from the current input source, which is either the decompression stream or the base stream.
    /// </summary>
    /// <param name="buffer">The buffer to write the data into.</param>
    /// <param name="offset">The byte offset in buffer at which to begin writing data.</param>
    /// <param name="countToRead">The maximum number of bytes to read from the current source.</param>
    /// <returns>The total number of bytes read into the buffer.</returns>
    private int ReadFromCurrent(byte[] buffer, int offset, int countToRead)
    {
      if (compInStream != null)
        return compInStream.Read(buffer, offset, countToRead);
      else
        return baseStream.Read(buffer, offset, countToRead);
    }

    /// <summary>
    /// Asynchronously performs the actual read from the current input source, which is either the decompression stream or the base stream.
    /// </summary>
    /// <param name="buffer">The buffer to write the data into.</param>
    /// <param name="offset">The byte offset in buffer at which to begin writing data.</param>
    /// <param name="countToRead">The maximum number of bytes to read from the current source.</param>
    /// <returns>A task that represents the asynchronous read operation. The task result contains the total number of bytes read into the buffer.</returns>
    private async Task<int> ReadFromCurrentAsync(byte[] buffer, int offset, int countToRead)
    {
      if (compInStream != null)
        return await compInStream.ReadAsync(buffer, offset, countToRead).ConfigureAwait(false);
      else
        return await baseStream.ReadAsync(buffer, offset, countToRead).ConfigureAwait(false);
    }

    /// <summary>
    /// Cleans up resources, such as the decompression stream and input buffer, if the current packet has been fully read.
    /// </summary>
    private void CleanupIfNeeded()
    {
      if (inPos == maxInPos)
      {
        compInStream = null;

        if (!Platform.IsMono())
        {
          inBufferRef = new WeakReference(inBuffer, false);
          inBuffer = null;
        }
      }
    }

    /// <summary>
    /// Prepares the next compressed packet for reading by reading the header and setting up decompression if necessary.
    /// </summary>
    private void PrepareNextPacket()
    {
      MySqlStream.ReadFully(baseStream, lengthBytes, 0, 7);
      int compressedLength = lengthBytes[0] + (lengthBytes[1] << 8) + (lengthBytes[2] << 16);
      // lengthBytes[3] is seq
      int unCompressedLength = lengthBytes[4] + (lengthBytes[5] << 8) +
                   (lengthBytes[6] << 16);

      if (unCompressedLength == 0)
      {
        unCompressedLength = compressedLength;
        compInStream = null;
      }
      else
      {
        ReadNextPacket(compressedLength);
        MemoryStream ms = new MemoryStream(inBuffer, 2, compressedLength - 2);
        compInStream = new DeflateStream(ms, CompressionMode.Decompress);
      }

      inPos = 0;
      maxInPos = unCompressedLength;
    }

    /// <summary>
    /// Asynchronously prepares the next compressed packet for reading by reading the header and setting up decompression if necessary.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task PrepareNextPacketAsync()
    {
      await MySqlStream.ReadFullyAsync(baseStream, lengthBytes, 0, 7).ConfigureAwait(false);
      int compressedLength = lengthBytes[0] + (lengthBytes[1] << 8) + (lengthBytes[2] << 16);
      // lengthBytes[3] is seq
      int unCompressedLength = lengthBytes[4] + (lengthBytes[5] << 8) +
                   (lengthBytes[6] << 16);

      if (unCompressedLength == 0)
      {
        unCompressedLength = compressedLength;
        compInStream = null;
      }
      else
      {
        await ReadNextPacketAsync(compressedLength).ConfigureAwait(false);
        MemoryStream ms = new MemoryStream(inBuffer, 2, compressedLength - 2);
        compInStream = new DeflateStream(ms, CompressionMode.Decompress);
      }

      inPos = 0;
      maxInPos = unCompressedLength;
    }

    /// <summary>
    /// Reads the next compressed packet of the specified length into the buffer.
    /// </summary>
    /// <param name="len">The length of the packet to read.</param>
    private void ReadNextPacket(int len)
    {
      inBuffer = inBufferRef.Target as byte[];
      if (inBuffer == null || inBuffer.Length < len)
        inBuffer = new byte[len];

      MySqlStream.ReadFully(baseStream, inBuffer, 0, len);
    }

    /// <summary>
    /// Asynchronously reads the next compressed packet of the specified length into the buffer.
    /// </summary>
    /// <param name="len">The length of the packet to read.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ReadNextPacketAsync(int len)
    {
      inBuffer = inBufferRef.Target as byte[];
      if (inBuffer == null || inBuffer.Length < len)
        inBuffer = new byte[len];

      await MySqlStream.ReadFullyAsync(baseStream, inBuffer, 0, len).ConfigureAwait(false);
    }

    /// <summary>
    /// Compresses the cached data using Deflate if the compression would reduce the size.
    /// </summary>
    /// <returns>A MemoryStream containing the compressed data, or null if compression does not reduce size.</returns>
    private MemoryStream CompressCache()
    {
      // small arrays almost never yeild a benefit from compressing
      if (cache.Length < 50)
        return null;

      byte[] cacheBytes = cache.GetBuffer();

      MemoryStream compressedBuffer = new MemoryStream();

      compressedBuffer.WriteByte(0x78);
      compressedBuffer.WriteByte(0x9c);
      var outCompStream = new DeflateStream(compressedBuffer, CompressionMode.Compress, true);

      outCompStream.Write(cacheBytes, 0, (int)cache.Length);
      outCompStream.Dispose();
      int adler = IPAddress.HostToNetworkOrder(Adler32(cacheBytes, 0, (int)cache.Length));
      compressedBuffer.Write(BitConverter.GetBytes(adler), 0, sizeof(uint));

      // if the compression hasn't helped, then just return null
      if (compressedBuffer.Length >= cache.Length)
        return null;
      return compressedBuffer;
    }

    /// <summary>
    /// Asynchronously compresses the cached data using Deflate if the compression would reduce the size.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a MemoryStream with the compressed data, or null if compression does not reduce size.</returns>
    private async Task<MemoryStream> CompressCacheAsync()
    {
      // small arrays almost never yeild a benefit from compressing
      if (cache.Length < 50)
        return null;

      byte[] cacheBytes = cache.GetBuffer();

      MemoryStream compressedBuffer = new MemoryStream();

      compressedBuffer.WriteByte(0x78);
      compressedBuffer.WriteByte(0x9c);
      var outCompStream = new DeflateStream(compressedBuffer, CompressionMode.Compress, true);

      await outCompStream.WriteAsync(cacheBytes, 0, (int)cache.Length).ConfigureAwait(false);
      outCompStream.Dispose();
      int adler = IPAddress.HostToNetworkOrder(Adler32(cacheBytes, 0, (int)cache.Length));
      await compressedBuffer.WriteAsync(BitConverter.GetBytes(adler), 0, sizeof(uint)).ConfigureAwait(false);
      
      // if the compression hasn't helped, then just return null
      if (compressedBuffer.Length >= cache.Length)
        return null;
      return compressedBuffer;
    }

    int Adler32(byte[] bytes, int index, int length)
    {
      const uint a32mod = 65521;
      uint s1 = 1, s2 = 0;
      for (int i = index; i < length; i++)
      {
        byte b = bytes[i];
        s1 = (s1 + b) % a32mod;
        s2 = (s2 + s1) % a32mod;
      }
      return unchecked((int)((s2 << 16) + s1));
    }

    /// <summary>
    /// Compresses the cached data and sends it to the base stream with the appropriate header.
    /// </summary>
    private void CompressAndSendCache()
    {
      long compressedLength, uncompressedLength;

      // we need to save the sequence byte that is written
      byte[] cacheBuffer = cache.GetBuffer();

      byte seq = cacheBuffer[3];
      cacheBuffer[3] = 0;

      // first we compress our current cache
      MemoryStream compressedBuffer = CompressCache();

      // now we set our compressed and uncompressed lengths
      // based on if our compression is going to help or not
      MemoryStream memStream;

      if (compressedBuffer == null)
      {
        compressedLength = cache.Length;
        uncompressedLength = 0;
        memStream = cache;
      }
      else
      {
        compressedLength = compressedBuffer.Length;
        uncompressedLength = cache.Length;
        memStream = compressedBuffer;
      }

      // Make space for length prefix (7 bytes) at the start of output
      long dataLength = memStream.Length;
      int bytesToWrite = (int)dataLength + 7;
      memStream.SetLength(bytesToWrite);
      byte[] buffer = memStream.GetBuffer();
      Array.Copy(buffer, 0, buffer, 7, (int)dataLength);

      // Write length prefix
      buffer[0] = (byte)(compressedLength & 0xff);
      buffer[1] = (byte)((compressedLength >> 8) & 0xff);
      buffer[2] = (byte)((compressedLength >> 16) & 0xff);
      buffer[3] = seq;
      buffer[4] = (byte)(uncompressedLength & 0xff);
      buffer[5] = (byte)((uncompressedLength >> 8) & 0xff);
      buffer[6] = (byte)((uncompressedLength >> 16) & 0xff);

      baseStream.Write(buffer, 0, bytesToWrite);
      baseStream.Flush();
      cache.SetLength(0);
      compressedBuffer?.Dispose();
    }

    /// <summary>
    /// Asynchronously compresses the cached data and sends it to the base stream with the appropriate header.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task CompressAndSendCacheAsync()
    {
      long compressedLength, uncompressedLength;

      // we need to save the sequence byte that is written
      byte[] cacheBuffer = cache.GetBuffer();

      byte seq = cacheBuffer[3];
      cacheBuffer[3] = 0;

      // first we compress our current cache
      MemoryStream compressedBuffer = await CompressCacheAsync().ConfigureAwait(false);

      // now we set our compressed and uncompressed lengths
      // based on if our compression is going to help or not
      MemoryStream memStream;

      if (compressedBuffer == null)
      {
        compressedLength = cache.Length;
        uncompressedLength = 0;
        memStream = cache;
      }
      else
      {
        compressedLength = compressedBuffer.Length;
        uncompressedLength = cache.Length;
        memStream = compressedBuffer;
      }

      // Make space for length prefix (7 bytes) at the start of output
      long dataLength = memStream.Length;
      int bytesToWrite = (int)dataLength + 7;
      memStream.SetLength(bytesToWrite);
      byte[] buffer = memStream.GetBuffer();
      Array.Copy(buffer, 0, buffer, 7, (int)dataLength);

      // Write length prefix
      buffer[0] = (byte)(compressedLength & 0xff);
      buffer[1] = (byte)((compressedLength >> 8) & 0xff);
      buffer[2] = (byte)((compressedLength >> 16) & 0xff);
      buffer[3] = seq;
      buffer[4] = (byte)(uncompressedLength & 0xff);
      buffer[5] = (byte)((uncompressedLength >> 8) & 0xff);
      buffer[6] = (byte)((uncompressedLength >> 16) & 0xff);

      await baseStream.WriteAsync(buffer, 0, bytesToWrite).ConfigureAwait(false);
      await baseStream.FlushAsync().ConfigureAwait(false);
      cache.SetLength(0);
      compressedBuffer?.Dispose();
    }

    public override void Flush() => FlushInternal();

    public override Task FlushAsync(CancellationToken cancellationToken) => FlushInternalAsync();

    /// <summary>
    /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
    /// </summary>
    private void FlushInternal()
    {
      if (!InputDone()) return;

      CompressAndSendCache();
    }

    /// <summary>
    /// Asynchronously clears all buffers for this stream, causes any buffered data to be written to the underlying device, and monitors cancellation requests.
    /// </summary>
    /// <returns>A task that represents the asynchronous flush operation.</returns>
    private async Task FlushInternalAsync()
    {
      if (!InputDone()) return;

      await CompressAndSendCacheAsync().ConfigureAwait(false);
    }

    private bool InputDone()
    {
      // if we have not done so yet, see if we can calculate how many bytes we are expecting
      if (baseStream is TimedStream && ((TimedStream)baseStream).IsClosed) return false;
      if (cache.Length < 4) return false;
      byte[] buf = cache.GetBuffer();
      int expectedLen = buf[0] + (buf[1] << 8) + (buf[2] << 16);
      if (cache.Length < (expectedLen + 4)) return false;
      return true;
    }

    public override void WriteByte(byte value)
    {
      cache.WriteByte(value);
    }

    public override void Write(byte[] buffer, int offset, int count) => WriteInternal(buffer, offset, count);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) => WriteInternalAsync(buffer, offset, count);

    /// <summary>
    /// Writes a sequence of bytes to the compressed stream and advances the current position within the stream by the number of bytes written.
    /// </summary>
    /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    private void WriteInternal(byte[] buffer, int offset, int count)
    {
      cache.Write(buffer, offset, count);
    }

    /// <summary>
    /// Asynchronously writes a sequence of bytes to the compressed stream and advances the current position within the stream by the number of bytes written.
    /// </summary>
    /// <param name="buffer">The buffer to write data from.</param>
    /// <param name="offset">The zero-based byte offset in buffer from which to begin copying bytes to the stream.</param>
    /// <param name="count">The maximum number of bytes to write.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    private async Task WriteInternalAsync(byte[] buffer, int offset, int count)
    {
      await cache.WriteAsync(buffer, offset, count).ConfigureAwait(false);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      return baseStream.Seek(offset, origin);
    }
  }
}