// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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


using Google.ProtocolBuffers;
using MySql.Data.MySqlClient;
using MySqlX.Data;
using MySqlX.XDevAPI;
using System;

namespace MySqlX.Protocol.X
{
  internal class IntegerDecoder : ValueDecoder
  {
    bool _signed;

    public IntegerDecoder(bool signed)
    {
      _signed = signed;
    }

    public override void SetMetadata()
    {
      Column.DbType = _signed ? GetSignedDbType() : GetUnsignedDbType();
      Column.ClrType = _signed ? GetSignedClrType() : GetUnsignedClrType();
      ClrValueDecoder = _signed ? GetSignedValueDecoder() : GetUnsignedValueDecoder();
    }

    private MySqlDbType GetSignedDbType()
    {
      uint len = Column.Length;
      if (len <= 4) return MySqlDbType.Byte;
      else if (len <= 6) return MySqlDbType.Int16;
      else if (len <= 8) return MySqlDbType.Int24;
      else if (len <= 11) return MySqlDbType.Int32;
      else return MySqlDbType.Int64;
    }

    private MySqlDbType GetUnsignedDbType()
    {
      uint len = Column.Length;
      if (len <= 4) return MySqlDbType.UByte;
      else if (len <= 6) return MySqlDbType.UInt16;
      else if (len <= 8) return MySqlDbType.UInt24;
      else if (len <= 11) return MySqlDbType.UInt32;
      else return MySqlDbType.UInt64;
    }

    private Type GetSignedClrType()
    {
      uint len = Column.Length;
      if (len <= 4) return typeof(sbyte);
      else if (len <= 6) return typeof(Int16);
      else if (len <= 8) return typeof(Int32);
      else if (len <= 11) return typeof(Int32);
      else return typeof(Int64);
    }

    private Type GetUnsignedClrType()
    {
      uint len = Column.Length;
      if (len <= 4) return typeof(byte);
      else if (len <= 6) return typeof(UInt16);
      else if (len <= 8) return typeof(UInt32);
      else if (len <= 11) return typeof(UInt32);
      else return typeof(UInt64);
    }

    private ClrDecoderDelegate GetSignedValueDecoder()
    {
      uint len = Column.Length;
      if (len <= 4) return SByteValueDecoder;
      else if (len <= 6) return Int16ValueDecoder;
      else if (len <= 8) return Int32ValueDecoder;
      else if (len <= 11) return Int32ValueDecoder;
      else return Int64ValueDecoder;
    }

    private ClrDecoderDelegate GetUnsignedValueDecoder()
    {
      uint len = Column.Length;
      if (len <= 4) return ByteValueDecoder;
      else if (len <= 6) return UInt16ValueDecoder;
      else if (len <= 8) return UInt32ValueDecoder;
      else if (len <= 11) return UInt32ValueDecoder;
      else return UInt64ValueDecoder;
    }

    private Int64 ReadInt(byte[] bytes)
    {
      Int64 val = 0;
      CodedInputStream.CreateInstance(bytes).ReadSInt64(ref val);
      return val;
    }

    private UInt64 ReadUInt(byte[] bytes)
    {
      UInt64 val = 0;
      CodedInputStream.CreateInstance(bytes).ReadUInt64(ref val);
      return val;
    }

    public object SByteValueDecoder(byte[] bytes)
    {
      return (byte)ReadInt(bytes);
    }

    public object Int16ValueDecoder(byte[] bytes)
    {
      return (Int16)ReadInt(bytes);
    }

    public object Int32ValueDecoder(byte[] bytes)
    {
      return (Int32)ReadInt(bytes);
    }

    public object Int64ValueDecoder(byte[] bytes)
    {
      return ReadInt(bytes);
    }

    public object ByteValueDecoder(byte[] bytes)
    {
      return bytes[0];
    }

    public object UInt16ValueDecoder(byte[] bytes)
    {
      return ReadUInt(bytes);
    }

    public object UInt32ValueDecoder(byte[] bytes)
    {
      return ReadUInt(bytes);
    }

    public object UInt64ValueDecoder(byte[] bytes)
    {
      return ReadUInt(bytes);
    }
  }
}
