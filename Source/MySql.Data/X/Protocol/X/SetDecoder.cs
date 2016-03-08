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
using Google.ProtocolBuffers;
using MySql.Data.MySqlClient;
using MySql.Data.Common;
using MySqlX.Properties;
using MySql.Data.MySqlClient.X.XDevAPI.Common;

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
      _encoding = CharSetMap.GetEncoding(DBVersion.Parse("0.0.0"), charset);
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
      while (index < len-1)
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
