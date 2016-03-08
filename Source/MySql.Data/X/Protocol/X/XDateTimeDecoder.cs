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

using Google.ProtocolBuffers;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.X.XDevAPI.Common;
using MySqlX.Data;
using MySqlX.XDevAPI;
using System;

namespace MySqlX.Protocol.X
{
  internal class XDateTimeDecoder : ValueDecoder
  {
    public override void SetMetadata()
    {
      Column.Type = GetDbType();
      Column.ClrType = typeof(DateTime);
      ClrValueDecoder = ValueDecoder;
    }

    private ColumnType GetDbType()
    {
      if ((Flags & 1) != 0)
        return ColumnType.Timestamp;
      if (Column.Length == 10)
        return ColumnType.Date;
      return ColumnType.DateTime;
    }

    public object ValueDecoder(byte[] bytes)
    {
      CodedInputStream input = CodedInputStream.CreateInstance(bytes);
      UInt64 year = 0, month = 0, day = 0;
      Int64 hour = 0, min = 0, sec = 0, usec = 0;

      input.ReadUInt64(ref year);
      input.ReadUInt64(ref month);
      input.ReadUInt64(ref day);
      if (!input.IsAtEnd)
        input.ReadInt64(ref hour);
      if (!input.IsAtEnd)
        input.ReadInt64(ref min);
      if (!input.IsAtEnd)
        input.ReadInt64(ref sec);
      if (!input.IsAtEnd)
        input.ReadInt64(ref usec);
      return new DateTime((int)year, (int)month, (int)day, (int)hour, (int)min, (int)sec).AddTicks(usec * 10);
    }
  }
}
