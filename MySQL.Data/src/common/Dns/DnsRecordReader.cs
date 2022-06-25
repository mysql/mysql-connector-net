// Copyright (c) 2022, Oracle and/or its affiliates.
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

using System.Collections.Generic;
using System.Text;

namespace MySql.Data.Common.DnsClient
{
  internal abstract class DnsRecord
  {
    /// <summary>
    /// The Resource Record this record data belongs to.
    /// </summary>
    internal DnsResourceRecord ResourceRecord;
  }

  /// <summary>
  /// A DNS record reader.
  /// </summary>
  internal class DnsRecordReader
  {
    private readonly byte[] _data;

    /// <summary>
    /// Gets or sets the position of the cursor in the record.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsRecordReader" /> class.
    /// </summary>
    /// <param name="data">Byte array of the record.</param>
    /// <param name="position">Position of the cursor in the record.</param>
    internal DnsRecordReader(byte[] data, int position)
    {
      _data = data;
      Position = position;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DnsRecordReader" /> class.
    /// </summary>
    /// <param name="data">Byte array of the record.</param>
    internal DnsRecordReader(byte[] data)
        : this(data, 0)
    {
    }

    /// <summary>
    /// Read a byte from the record.
    /// </summary>
    internal byte ReadByte()
    {
      return (byte)(Position >= _data.Length ? 0 : _data[Position++]);
    }

    /// <summary>
    /// Read a char from the record.
    /// </summary>
    internal char ReadChar()
    {
      return (char)ReadByte();
    }

    /// <summary>
    /// Read an unsigned int 16 from the record.
    /// </summary>
    internal ushort ReadUInt16()
    {
      return (ushort)((ReadByte() << 8) | ReadByte());
    }

    /// <summary>
    /// Read an unsigned int 16 from the offset of the record.
    /// </summary>
    /// <param name="offset">Offset to start reading from.</param>
    internal ushort ReadUInt16(int offset)
    {
      Position += offset;
      return ReadUInt16();
    }

    /// <summary>
    /// Read an unsigned int 32 from the record.
    /// </summary>
    internal uint ReadUInt32()
    {
      return (uint)((ReadUInt16() << 16) | ReadUInt16());
    }

    /// <summary>
    /// Read the domain name from the record.
    /// </summary>
    /// <returns>Domain name of the record.</returns>
    internal string ReadDomainName()
    {
      var name = new StringBuilder();
      int length;

      while ((length = ReadByte()) != 0)
      {
        if ((length & 0xc0) == 0xc0)
        {
          var newRecordReader = new DnsRecordReader(_data, ((length & 0x3f) << 8) | ReadByte());

          name.Append(newRecordReader.ReadDomainName());
          return name.ToString();
        }

        while (length > 0)
        {
          name.Append(ReadChar());
          length--;
        }

        name.Append('.');
      }

      return name.Length == 0 ? "." : name.ToString().Trim('.');
    }

    /// <summary>
    /// Read a string from the record.
    /// </summary>
    internal string ReadString()
    {
      short length = ReadByte();

      return Encoding.UTF8.GetString(ReadBytes(length));
    }

    /// <summary>
    /// Read a series of bytes from the record.
    /// </summary>
    /// <param name="length">Length to read from the record.</param>
    internal byte[] ReadBytes(int length)
    {
      var list = new List<byte>();
      for (var i = 0; i < length; i++)
      {
        list.Add(ReadByte());
      }

      return list.ToArray();
    }

    /// <summary>
    /// Read record from the data.
    /// </summary>
    /// <param name="type">Type of the record to read.</param>
    /// <returns>Record read from the data.</returns>
    internal DnsRecord ReadRecord(RecordType type)
    {
      if (type is RecordType.SRV)
        return new DnsSrvRecord(this);
      else
        return new DnsRecordUnknown(this);
    }
  }
}
