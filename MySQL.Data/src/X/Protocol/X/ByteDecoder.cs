// Copyright © 2015, 2016, Oracle and/or its affiliates. All rights reserved.
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

using System.Text;
using MySqlX.Data;
using System;
using MySqlX.XDevAPI;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.X.XDevAPI.Common;
using MySql.Data.Common;

namespace MySqlX.Protocol.X
{
  internal class ByteDecoder : ValueDecoder
  {
    private Encoding _encoding;
    private bool _isEnum;

    public ByteDecoder(bool isEnum)
    {
      _isEnum = isEnum;
    }

    public override void SetMetadata()
    {
      Column.Type = GetDbType();
      Column.ClrType = GetClrType(Column.Type);
      Column.IsPadded = (Flags & 1) != 0;
      ClrValueDecoder = GetClrValueDecoder();
    }

    private ColumnType GetDbType()
    {
      if (_isEnum)
        return ColumnType.Enum;
      if (ContentType == (uint)ColumnContentType.Geometry)
        return ColumnType.Geometry;
      if (ContentType == (uint)ColumnContentType.Json)
        return ColumnType.Json;
      if ((Column.CollationName??"").Equals("binary", StringComparison.OrdinalIgnoreCase))
        return ColumnType.Bytes;
      return ColumnType.String;
   }

    private Type GetClrType(ColumnType dbType)
    {
      if (dbType == ColumnType.String || dbType == ColumnType.Json
        || dbType == ColumnType.Enum)
        return typeof(string);
      return typeof(byte[]);
    }

    private ClrDecoderDelegate GetClrValueDecoder()
    {
      if (Column.ClrType == typeof(String)) return StringValueDecoder;
      return ByteValueDecoder;
    }

    private object StringValueDecoder(byte[] bytes)
    {
      if (bytes.Length == 0) return null;

      if (_encoding == null)
      {
        string charset = Column.CharacterSetName ?? string.Empty;
        _encoding = CharSetMap.GetEncoding(new DBVersion(), charset);
      }
      return _encoding.GetString(bytes, 0, bytes.Length - 1);
    }

    private object ByteValueDecoder(byte[] bytes)
    {
      byte[] newValue = new byte[bytes.Length - 1];
      Array.Copy(bytes, newValue, newValue.Length);
      return newValue;
    }
  }
}
