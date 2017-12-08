// Copyright � 2004, 2016, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MySql.Data.Common;
using System.Net;

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

#if NETSTANDARD1_3
    public void Close()
    {
      base.Dispose();
#else
    public override void Close()
    {
      base.Close();
#endif
#if NETSTANDARD1_3
      baseStream.Dispose();
#else
      baseStream.Close();
#endif
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

    public override int Read(byte[] buffer, int offset, int count)
    {
      if (buffer == null)
        throw new ArgumentNullException(nameof(buffer), Resources.BufferCannotBeNull);
      if (offset < 0 || offset >= buffer.Length)
        throw new ArgumentOutOfRangeException(nameof(offset), Resources.OffsetMustBeValid);
      if ((offset + count) > buffer.Length)
        throw new ArgumentException(Resources.BufferNotLargeEnough, nameof(buffer));

      if (inPos == maxInPos)
        PrepareNextPacket();

      int countToRead = Math.Min(count, maxInPos - inPos);
      int countRead;

      if (compInStream != null)
        countRead  = compInStream.Read(buffer, offset, countToRead);
      else
        countRead = baseStream.Read(buffer, offset, countToRead);

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
        MemoryStream ms = new MemoryStream(inBuffer, 2, compressedLength-2);
        compInStream = new DeflateStream(ms, CompressionMode.Decompress);
      }

      inPos = 0;
      maxInPos = unCompressedLength;
    }

    private void ReadNextPacket(int len)
    {
      inBuffer = inBufferRef.Target as byte[];

      if (inBuffer == null || inBuffer.Length < len)
        inBuffer = new byte[len];
      MySqlStream.ReadFully(baseStream, inBuffer, 0, len);
    }

    private MemoryStream CompressCache()
    {
      // small arrays almost never yeild a benefit from compressing
      if (cache.Length < 50)
        return null;

#if NETSTANDARD1_3
      byte[] cacheBytes;
      ArraySegment<byte> cacheBuffer;
      var cacheResult = cache.TryGetBuffer(out cacheBuffer);

      if (cacheResult)
        cacheBytes = cacheBuffer.ToArray();
      else       // if the conversion fail, then just return null
        return null;
#else
      byte[] cacheBytes = cache.GetBuffer();
#endif

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

    private void CompressAndSendCache()
    {
      long compressedLength, uncompressedLength;

      // we need to save the sequence byte that is written
#if NETSTANDARD1_3
      byte[] cacheBuffer;
      ArraySegment<byte> cacheContentArraySegment;
      var cacheResult = cache.TryGetBuffer(out cacheContentArraySegment);

      if (cacheResult)
        cacheBuffer = cacheContentArraySegment.ToArray();
      else
        throw new InvalidDataException();
#else
      byte[] cacheBuffer = cache.GetBuffer();
#endif

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

#if NETSTANDARD1_3
      byte[] buffer;
      ArraySegment<byte> contentArraySegment;
      var result = memStream.TryGetBuffer(out contentArraySegment);

      if (result)
        buffer = contentArraySegment.ToArray();
      else
        throw new InvalidDataException();

#else
      byte[] buffer = memStream.GetBuffer();
#endif
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

    public override void Flush()
    {
      if (!InputDone()) return;

      CompressAndSendCache();
    }

    private bool InputDone()
    {
      // if we have not done so yet, see if we can calculate how many bytes we are expecting
      if (baseStream is TimedStream && ((TimedStream)baseStream).IsClosed) return false;
      if (cache.Length < 4) return false;
#if NETSTANDARD1_3
      byte[] buf;
      ArraySegment<byte> contentArraySegment;
      var result = cache.TryGetBuffer(out contentArraySegment);

      if (result)
        buf = contentArraySegment.ToArray();
      else
        throw new InvalidDataException();
#else
      byte[] buf = cache.GetBuffer();
#endif
      int expectedLen = buf[0] + (buf[1] << 8) + (buf[2] << 16);
      if (cache.Length < (expectedLen + 4)) return false;
      return true;
    }

    public override void WriteByte(byte value)
    {
      cache.WriteByte(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      cache.Write(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      return baseStream.Seek(offset, origin);
    }
  }
}
