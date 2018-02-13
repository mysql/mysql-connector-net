// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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


using MySql.Data;
using MySql.Data.MySqlClient.X.XDevAPI.Common;
using MySqlX.Data;
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
        throw new FormatException(ResourcesX.InvalidDecimalFormat);

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