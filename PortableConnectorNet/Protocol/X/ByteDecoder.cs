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

using System.Text;
using MySql.Data;
using System;
using MySql.XDevAPI;

namespace MySql.Protocol.X
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
      Column.DbType = _isEnum ? MySQLDbType.Enum : GetDbType();
      Column.ClrType = GetClrType(Column.DbType);
      ClrValueDecoder = GetClrValueDecoder();
    }

    private MySQLDbType GetDbType()
    {
      bool isBinary = Column.Collation == "binary";

      if (Column.Collation == "utf8") return MySQLDbType.VarChar;
      if (Column.Collation == "utf8_bin") return MySQLDbType.VarBinary;
      if (Column.Length < 256) return isBinary ? MySQLDbType.TinyBlob : MySQLDbType.TinyText;
      if (Column.Length < 65536) return isBinary ? MySQLDbType.Blob : MySQLDbType.Text;
      if (Column.Length < 16777216) return isBinary ? MySQLDbType.MediumBlob : MySQLDbType.MediumText;
      return isBinary ? MySQLDbType.LongBlob : MySQLDbType.LongText;
   }

    private Type GetClrType(MySQLDbType dbType)
    {
      if (dbType == MySQLDbType.VarChar || 
          dbType == MySQLDbType.Char ||
          dbType == MySQLDbType.TinyText ||
          dbType == MySQLDbType.Text ||
          dbType == MySQLDbType.MediumText || 
          dbType == MySQLDbType.LongText ||
          dbType == MySQLDbType.Enum) return typeof(string);
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
        _encoding = CharSetMap.GetEncoding(Column.Collation);
      return _encoding.GetString(bytes, 0, bytes.Length - 1);
    }

    private object ByteValueDecoder(byte[] bytes)
    {
      return bytes;
    }
  }
}
