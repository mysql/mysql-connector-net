// Copyright © 2015, 2024, Oracle and/or its affiliates.
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

using MySql.Data;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.X.XDevAPI.Common;
using System;
using System.Text;

namespace MySqlX.Protocol.X
{
  internal class SetDecoder : ValueDecoder
  {
    private Encoding _encoding;

    public override void SetMetadata()
    {
      Column.Type = ColumnType.Set;
      Column.ClrType = typeof(string);
      ClrValueDecoder = DecodeValue;

      string charset = Column.CollationName.Split('_')[0];
      _encoding = CharSetMap.GetEncoding(charset);
    }

    private object DecodeValue(byte[] bytes)
    {
      if (bytes == null || bytes.Length == 0) return null;
      if (bytes.Length == 1 && bytes[0] == 0) return String.Empty;
      if (bytes.Length == 1 && bytes[0] == 1) return String.Empty;

      StringBuilder sb = new StringBuilder();
      string delim = "";
      int len = bytes.Length;
      int index = 0;
      while (index < len - 1)
      {
        sb.Append(delim);
        int strLen = bytes[index++];
        if ((index + strLen) > bytes.Length)
          throw new MySqlException(ResourcesX.UnexpectedEndOfPacketFound);
        sb.Append(_encoding.GetString(bytes, index, strLen));
        index += strLen;
        delim = ",";
      }
      return sb.ToString();
    }
  }
}
