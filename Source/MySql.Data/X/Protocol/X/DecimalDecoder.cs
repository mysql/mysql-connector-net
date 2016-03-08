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
  internal class DecimalDecoder : ValueDecoder
  {
    public override void SetMetadata()
    {
      Column.Type = ColumnType.Decimal;
      Column.ClrType = typeof(decimal);
      ClrValueDecoder = DecimalValueDecoder;
    }

    private object DecimalValueDecoder(byte[] bytes)
    {
      // this decoder is based on the BCD standard
      int scale = bytes[0];
      byte sign = bytes[bytes.Length - 1];
      string stringValue = string.Empty;
      string lastDigit = string.Empty;

      if (sign == 0xc0)
        stringValue = "+";
      else if (sign == 0xd0)
        stringValue = "-";
      else if((sign & 0x0F) == 0x0c)
      {
        stringValue = "+";
        lastDigit = (sign >> 4).ToString();
      }
      else if((sign & 0x0F) == 0x0d)
      {
        stringValue = "-";
        lastDigit = (sign >> 4).ToString();
      }
      else
        throw new FormatException(Properties.ResourcesX.InvalidDecimalFormat);

      for(int i = 1; i < bytes.Length - 1; i++)
      {
        stringValue += bytes[i].ToString("x2");
      }
      stringValue += lastDigit;
      stringValue = stringValue.Insert(stringValue.Length - scale, ".");

      return Decimal.Parse(stringValue);
    }
  }
}