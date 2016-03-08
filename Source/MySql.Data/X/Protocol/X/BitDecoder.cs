// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.MySqlClient.X.XDevAPI.Common;
using System;

namespace MySqlX.Protocol.X
{
  internal class BitDecoder : ValueDecoder
  {
    public override void SetMetadata()
    {
      Column.Type = ColumnType.Bit;
      Column.ClrType = typeof(UInt64);
      ClrValueDecoder = BitValueDecoder;
    }

    private UInt64 ReadUInt(byte[] bytes)
    {
      UInt64 val = 0;
      CodedInputStream.CreateInstance(bytes).ReadUInt64(ref val);
      return val;
    }

    private object BitValueDecoder(byte[] bytes)
    {
      return ReadUInt(bytes);
    }
  }
}