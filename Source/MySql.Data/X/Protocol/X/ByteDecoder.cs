// Copyright © 2015, 2016 Oracle and/or its affiliates. All rights reserved.
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

using System.Text;
using MySqlX.Data;
using System;
using MySqlX.XDevAPI;
using MySql.Data.MySqlClient;
using MySql.Data.Common;
using MySql.Data.MySqlClient.X.XDevAPI.Common;

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
      if ((Column.CollationName??"").EndsWith("_bin"))
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
        string charset = (Column.CollationName??"").Split('_')[0];
        _encoding = CharSetMap.GetEncoding(DBVersion.Parse("0.0.0"), charset);
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
