// Copyright (c) 2004, 2022, Oracle and/or its affiliates.
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
        Read(localByte, 0, 1);
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

    public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count, false).GetAwaiter().GetResult();

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) => ReadAsync(buffer, offset, count, true);

    private async Task<int> ReadAsync(byte[] buffer, int offset, int count, bool execAsync)
    {
      if (buffer == null)
        throw new ArgumentNullException(nameof(buffer), Resources.BufferCannotBeNull);
      if (offset < 0 || offset >= buffer.Length)
        throw new ArgumentOutOfRangeException(nameof(offset), Resources.OffsetMustBeValid);
      if ((offset + count) > buffer.Length)
        throw new ArgumentException(Resources.BufferNotLargeEnough, nameof(buffer));

      if (inPos == maxInPos)
        await PrepareNextPacketAsync(execAsync).ConfigureAwait(false);

      int countToRead = Math.Min(count, maxInPos - inPos);
      int countRead;

      if (compInStream != null)
        countRead = execAsync
          ? await compInStream.ReadAsync(buffer, offset, count).ConfigureAwait(false)
          : compInStream.Read(buffer, offset, countToRead);
      else
        countRead = execAsync
          ? await baseStream.ReadAsync(buffer, offset, count).ConfigureAwait(false)
          : baseStream.Read(buffer, offset, countToRead);

      inPos += countRead;

      // release the weak reference
      if (inPos == maxInPos)
      {
        compInStream = null;

        if (!Platform.IsMono())
        {
          inBufferRef = new WeakReference(inBuffer, false);
          inBuffer = null;
        }
      }

      return countRead;
    }

    private async Task PrepareNextPacketAsync(bool execAsync)
    {
      await MySqlStream.ReadFullyAsync(baseStream, lengthBytes, 0, 7, execAsync).ConfigureAwait(false);
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
        await ReadNextPacketAsync(compressedLength, execAsync).ConfigureAwait(false);
        MemoryStream ms = new MemoryStream(inBuffer, 2, compressedLength - 2);
        compInStream = new DeflateStream(ms, CompressionMode.Decompress);
      }

      inPos = 0;
      maxInPos = unCompressedLength;
    }

    private async Task ReadNextPacketAsync(int len, bool execAsync)
    {
      inBuffer = inBufferRef.Target as byte[];

      if (inBuffer == null || inBuffer.Length < len)
        inBuffer = new byte[len];
      await MySqlStream.ReadFullyAsync(baseStream, inBuffer, 0, len, execAsync).ConfigureAwait(false);
    }

    private async Task<MemoryStream> CompressCacheAsync(bool execAsync)
    {
      // small arrays almost never yeild a benefit from compressing
      if (cache.Length < 50)
        return null;

      byte[] cacheBytes = cache.GetBuffer();

      MemoryStream compressedBuffer = new MemoryStream();

      compressedBuffer.WriteByte(0x78);
      compressedBuffer.WriteByte(0x9c);
      var outCompStream = new DeflateStream(compressedBuffer, CompressionMode.Compress, true);

      if (execAsync)
        await outCompStream.WriteAsync(cacheBytes, 0, (int)cacheBytes.Length).ConfigureAwait(false);
      else
        outCompStream.Write(cacheBytes, 0, (int)cache.Length);

      outCompStream.Dispose();
      int adler = IPAddress.HostToNetworkOrder(Adler32(cacheBytes, 0, (int)cache.Length));

      if (execAsync)
        await compressedBuffer.WriteAsync(BitConverter.GetBytes(adler), 0, sizeof(uint)).ConfigureAwait(false);
      else
        compressedBuffer.Write(BitConverter.GetBytes(adler), 0, sizeof(uint));

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

    private async Task CompressAndSendCacheAsync(bool execAsync)
    {
      long compressedLength, uncompressedLength;

      // we need to save the sequence byte that is written
      byte[] cacheBuffer = cache.GetBuffer();

      byte seq = cacheBuffer[3];
      cacheBuffer[3] = 0;

      // first we compress our current cache
      MemoryStream compressedBuffer = await CompressCacheAsync(execAsync).ConfigureAwait(false);

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

      if (execAsync)
      {
        await baseStream.WriteAsync(buffer, 0, bytesToWrite).ConfigureAwait(false);
        await baseStream.FlushAsync().ConfigureAwait(false);
      }
      else
      {
        baseStream.Write(buffer, 0, bytesToWrite);
        baseStream.Flush();
      }

      cache.SetLength(0);

      compressedBuffer?.Dispose();
    }

    public override void Flush() => FlushAsync(false).GetAwaiter().GetResult();

    public override Task FlushAsync(CancellationToken cancellationToken) => FlushAsync(true);

    private async Task FlushAsync(bool execAsync)
    {
      if (!InputDone()) return;

      if (execAsync)
        await CompressAndSendCacheAsync(true).ConfigureAwait(false);
      else
        CompressAndSendCacheAsync(false).GetAwaiter().GetResult();
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

    public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer, offset, count, false).GetAwaiter().GetResult();

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) => WriteAsync(buffer, offset, count, true);

    private async Task WriteAsync(byte[] buffer, int offset, int count, bool execAsync)
    {
      if (execAsync)
        await cache.WriteAsync(buffer, offset, count).ConfigureAwait(false);
      else
        cache.Write(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      return baseStream.Seek(offset, origin);
    }
  }
}
