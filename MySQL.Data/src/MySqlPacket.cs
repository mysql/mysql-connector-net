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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;


namespace MySql.Data.MySqlClient
{
  class MySqlPacket
  {
    private byte[] _tempBuffer = new byte[256];
    private Encoding _encoding;
    private readonly MemoryStream _buffer = new MemoryStream(5);

    private MySqlPacket()
    {
      Clear();
    }

    public MySqlPacket(Encoding enc)
      : this()
    {
      Encoding = enc;
    }

    public MySqlPacket(MemoryStream stream)
      : this()
    {
      _buffer = stream;
    }

    #region Properties

    public Encoding Encoding
    {
      get { return _encoding; }
      set
      {
        Debug.Assert(value != null);
        _encoding = value;
      }
    }

    public bool HasMoreData
    {
      get { return _buffer.Position < _buffer.Length; }
    }

    public int Position
    {
      get { return (int)_buffer.Position; }
      set { _buffer.Position = (long)value; }
    }

    public int Length
    {
      get { return (int)_buffer.Length; }
      set { _buffer.SetLength(value); }
    }

    public bool IsLastPacket
    {
      get
      {
        byte[] bits = _buffer.GetBuffer();

        return bits[0] == 0xfe && Length <= 5;
      }
    }

    public byte[] Buffer
    {
      get
      {
        byte[] bits = _buffer.GetBuffer();

        return bits;
      }
    }

    public DBVersion Version { get; set; }

    #endregion

    public void Clear()
    {
      Position = 4;
    }


    #region Byte methods

    public byte ReadByte()
    {
      return (byte)_buffer.ReadByte();
    }

  /// <summary>
  /// Reads a specified number of bytes from the packet buffer into the provided byte array.
  /// </summary>
  /// <param name="byteBuffer">The byte array to read data into.</param>
  /// <param name="offset">The zero-based offset in the byte array at which to begin reading.</param>
  /// <param name="count">The maximum number of bytes to read.</param>
  /// <returns>The total number of bytes read into the buffer.</returns>
  public int Read(byte[] byteBuffer, int offset, int count)
  {
    return _buffer.Read(byteBuffer, offset, count);
  }

  /// <summary>
  /// Asynchronously reads a specified number of bytes from the packet buffer into the provided byte array.
  /// </summary>
  /// <param name="byteBuffer">The byte array to read data into.</param>
  /// <param name="offset">The zero-based offset in the byte array at which to begin reading.</param>
  /// <param name="count">The maximum number of bytes to read.</param>
  /// <returns>A task representing the asynchronous operation, containing the total number of bytes read.</returns>
  public async Task<int> ReadAsync(byte[] byteBuffer, int offset, int count)
  {
    return await _buffer.ReadAsync(byteBuffer, offset, count).ConfigureAwait(false);
  }

    public void WriteByte(byte b)
    {
      _buffer.WriteByte(b);
    }

  /// <summary>
  /// Writes a complete byte array to the packet buffer.
  /// </summary>
  /// <param name="bytes">The byte array to write.</param>
  public void Write(byte[] bytes)
  {
    _buffer.Write(bytes, 0, bytes.Length);
  }

  /// <summary>
  /// Writes a portion of a byte array to the packet buffer.
  /// </summary>
  /// <param name="bytes">The byte array containing the data to write.</param>
  /// <param name="offset">The zero-based offset in the array at which to begin writing.</param>
  /// <param name="countToWrite">The number of bytes to write.</param>
  public void Write(byte[] bytes, int offset, int countToWrite)
  {
    _buffer.Write(bytes, offset, countToWrite);
  }

  /// <summary>
  /// Asynchronously writes a complete byte array to the packet buffer.
  /// </summary>
  /// <param name="bytesToWrite">The byte array to write.</param>
  public async Task WriteAsync(byte[] bytesToWrite)
  {
    await WriteAsync(bytesToWrite, 0, bytesToWrite.Length).ConfigureAwait(false);
  }

  /// <summary>
  /// Asynchronously writes a portion of a byte array to the packet buffer.
  /// </summary>
  /// <param name="bytesToWrite">The byte array containing the data to write.</param>
  /// <param name="offset">The zero-based offset in the array at which to begin writing.</param>
  /// <param name="countToWrite">The number of bytes to write.</param>
  public async Task WriteAsync(byte[] bytesToWrite, int offset, int countToWrite)
  {
    await _buffer.WriteAsync(bytesToWrite, offset, countToWrite).ConfigureAwait(false);
  }

    public int ReadNBytes()
    {
      byte c = ReadByte();
      if (c < 1 || c > 4)
        throw new MySqlException(Resources.IncorrectTransmission);
      return ReadInteger(c);
    }

    public void SetByte(long position, byte value)
    {
      long currentPosition = _buffer.Position;
      _buffer.Position = position;
      _buffer.WriteByte(value);
      _buffer.Position = currentPosition;
    }

    #endregion

    #region Integer methods

    public long ReadFieldLength()
    {
      byte c = ReadByte();

      switch (c)
      {
        case 251: return -1;
        case 252: return ReadInteger(2);
        case 253: return ReadInteger(3);
        case 254: return ReadLong(8);
        default: return c;
      }
    }

    public ulong ReadBitValue(int numbytes)
    {
      ulong value = 0;

      int pos = (int)_buffer.Position;
      byte[] bits = _buffer.GetBuffer();
      int shift = 0;

      for (int i = 0; i < numbytes; i++)
      {
        value <<= shift;
        value |= bits[pos++];
        shift = 8;
      }
      _buffer.Position += numbytes;
      return value;
    }

    public long ReadLong(int numbytes)
    {
      Debug.Assert((_buffer.Position + numbytes) <= _buffer.Length);

      byte[] bits = _buffer.GetBuffer();
      int pos = (int)_buffer.Position;
      _buffer.Position += numbytes;

      switch (numbytes)
      {
        case 2: return PacketBitConverter.ToUInt16(bits, pos);
        case 4: return PacketBitConverter.ToUInt32(bits, pos);
        case 8: return PacketBitConverter.ToInt64(bits, pos);
      }
      throw new NotSupportedException("Only byte lengths of 2, 4, or 8 are supported");
    }

    public ulong ReadULong(int numbytes)
    {
      Debug.Assert((_buffer.Position + numbytes) <= _buffer.Length);

      byte[] bits = _buffer.GetBuffer();

      int pos = (int)_buffer.Position;
      _buffer.Position += numbytes;

      switch (numbytes)
      {
        case 2: return PacketBitConverter.ToUInt16(bits, pos);
        case 4: return PacketBitConverter.ToUInt32(bits, pos);
        case 8: return PacketBitConverter.ToUInt64(bits, pos);
      }
      throw new NotSupportedException("Only byte lengths of 2, 4, or 8 are supported");
    }

    public int Read3ByteInt()
    {
      int value = 0;

      int pos = (int)_buffer.Position;
      byte[] bits = _buffer.GetBuffer();
      int shift = 0;

      for (int i = 0; i < 3; i++)
      {
        value |= (int)(bits[pos++] << shift);
        shift += 8;
      }
      _buffer.Position += 3;
      return value;
    }

    public int ReadInteger(int numbytes)
    {
      if (numbytes == 3)
        return Read3ByteInt();
      Debug.Assert(numbytes <= 4);
      return (int)ReadLong(numbytes);
    }

    /// <summary>
    /// WriteInteger
    /// </summary>
    /// <param name="v"></param>
    /// <param name="numbytes"></param>
    public async Task WriteIntegerAsync(long v, int numbytes)
    {
      long val = v;

      Debug.Assert(numbytes > 0 && numbytes < 9);

      for (int x = 0; x < numbytes; x++)
      {
        _tempBuffer[x] = (byte)(val & 0xff);
        val >>= 8;
      }

      await WriteAsync(_tempBuffer, 0, numbytes).ConfigureAwait(false);
    }

    public void WriteInteger(long v, int numbytes)
    {
      long val = v;

      Debug.Assert(numbytes > 0 && numbytes < 9);

      for (int x = 0; x < numbytes; x++)
      {
        _tempBuffer[x] = (byte)(val & 0xff);
        val >>= 8;
      }

      Write(_tempBuffer, 0, numbytes);
    }

    public int ReadPackedInteger()
    {
      byte c = ReadByte();

      switch (c)
      {
        case 251: return -1;
        case 252: return ReadInteger(2);
        case 253: return ReadInteger(3);
        case 254: return ReadInteger(4);
        default: return c;
      }
    }

    public void WriteLength(long length)
    {
      if (length < 251)
        WriteByte((byte)length);
      else if (length < 65536L)
      {
        WriteByte(252);
        WriteInteger(length, 2);
      }
      else if (length < 16777216L)
      {
        WriteByte(253);
        WriteInteger(length, 3);
      }
      else
      {
        WriteByte(254);
        WriteInteger(length, 8);
      }
    }

    public async Task WriteLengthAsync(long length)
    {
      if (length < 251)
        WriteByte((byte)length);
      else if (length < 65536L)
      {
        WriteByte(252);
        await WriteIntegerAsync(length, 2).ConfigureAwait(false);
      }
      else if (length < 16777216L)
      {
        WriteByte(253);
        await WriteIntegerAsync(length, 3).ConfigureAwait(false);
      }
      else
      {
        WriteByte(254);
        await WriteIntegerAsync(length, 8).ConfigureAwait(false);
      }
    }

    #endregion

    #region String methods

  /// <summary>
  /// Writes a length-prefixed string to the packet buffer using packed integer length encoding.
  /// </summary>
  /// <param name="s">The string to write.</param>
  public void WriteLenString(string s)
  {
    byte[] bytes = _encoding.GetBytes(s);

    WriteLength(bytes.Length);
    Write(bytes, 0, bytes.Length);
  }

  /// <summary>
  /// Asynchronously writes a length-prefixed string to the packet buffer using packed integer length encoding.
  /// </summary>
  /// <param name="s">The string to write.</param>
  public async Task WriteLenStringAsync(string s)
  {
    byte[] bytes = _encoding.GetBytes(s);

    await WriteLengthAsync(bytes.Length).ConfigureAwait(false);
    await WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
  }

  /// <summary>
  /// Writes a string to the packet buffer without a null terminator, using the current encoding.
  /// </summary>
  /// <param name="v">The string to write.</param>
  public void WriteStringNoNull(string v)
  {
    byte[] bytes = _encoding.GetBytes(v);

    Write(bytes, 0, bytes.Length);
  }

  /// <summary>
  /// Asynchronously writes a string to the packet buffer without a null terminator, using the current encoding.
  /// </summary>
  /// <param name="v">The string to write.</param>
  public async Task WriteStringNoNullAsync(string v)
  {
    byte[] bytes = _encoding.GetBytes(v);

    await WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
  }

    public void WriteString(string v)
    {
      WriteStringNoNull(v);

      WriteByte(0);
    }

    public async Task WriteStringAsync(string v)
    {
      await WriteStringNoNullAsync(v).ConfigureAwait(false);

      WriteByte(0);
    }

  /// <summary>
  /// Reads a length-prefixed string from the packet buffer, where the length is encoded as a packed integer.
  /// </summary>
  /// <returns>The string read from the buffer.</returns>
  public string ReadLenString()
  {
    long len = ReadPackedInteger();
    return ReadString(len);
  }

  /// <summary>
  /// Asynchronously reads a length-prefixed string from the packet buffer, where the length is encoded as a packed integer.
  /// </summary>
  /// <returns>A task containing the string read from the buffer.</returns>
  public async Task<string> ReadLenStringAsync()
  {
    long len = ReadPackedInteger();
    return await ReadStringAsync(len).ConfigureAwait(false);
  }

  /// <summary>
  /// Reads a fixed-length ASCII string from the packet buffer.
  /// </summary>
  /// <param name="length">The number of bytes to read for the ASCII string.</param>
  /// <returns>The ASCII string read from the buffer.</returns>
  public string ReadAsciiString(long length)
  {
    if (length == 0)
      return String.Empty;

    Read(_tempBuffer, 0, (int)length);

    return Encoding.GetEncoding("us-ascii").GetString(_tempBuffer, 0, (int)length);
  }

  /// <summary>
  /// Asynchronously reads a fixed-length ASCII string from the packet buffer.
  /// </summary>
  /// <param name="length">The number of bytes to read for the ASCII string.</param>
  /// <returns>A task containing the ASCII string read from the buffer.</returns>
  public async Task<string> ReadAsciiStringAsync(long length)
  {
    if (length == 0)
      return String.Empty;

    await ReadAsync(_tempBuffer, 0, (int)length).ConfigureAwait(false);

    return Encoding.GetEncoding("us-ascii").GetString(_tempBuffer, 0, (int)length);
  }

    public string ReadString(long length)
    {
      if (length == 0)
        return String.Empty;

      if (_tempBuffer == null || length > _tempBuffer.Length)
        _tempBuffer = new byte[length];

      Read(_tempBuffer, 0, (int)length);

      return _encoding.GetString(_tempBuffer, 0, (int)length);
    }

    public async Task<string> ReadStringAsync(long length)
    {
      if (length == 0)
        return String.Empty;

      if (_tempBuffer == null || length > _tempBuffer.Length)
        _tempBuffer = new byte[length];

      await ReadAsync(_tempBuffer, 0, (int)length).ConfigureAwait(false);

      return _encoding.GetString(_tempBuffer, 0, (int)length);
    }

    public string ReadString()
    {
      return ReadString(_encoding);
    }

    public string ReadString(Encoding theEncoding)
    {
      byte[] bytes = ReadStringAsBytes();
      string s = theEncoding.GetString(bytes, 0, bytes.Length);
      return s;
    }

    public byte[] ReadStringAsBytes()
    {
      byte[] readBytes;
      byte[] bits = _buffer.GetBuffer();
      int end = (int)_buffer.Position;
      byte[] tempBuffer = bits;

      while (end < (int)_buffer.Length &&
          tempBuffer[end] != 0 && (int)tempBuffer[end] != -1)
        end++;

      readBytes = new byte[end - _buffer.Position];
      Array.Copy(tempBuffer, (int)_buffer.Position, readBytes, 0, (int)(end - _buffer.Position));
      _buffer.Position = end + 1;

      return readBytes;
    }

    #endregion
  }
}
