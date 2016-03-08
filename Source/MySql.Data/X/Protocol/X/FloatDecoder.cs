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
using MySqlX.Data;
using MySqlX.XDevAPI;
using System;
using MySql.Data.MySqlClient;
using MySqlX.Properties;
using MySql.Data.MySqlClient.X.XDevAPI.Common;

namespace MySqlX.Protocol.X
{
  internal class FloatDecoder : ValueDecoder
  {
    bool _float;

    public FloatDecoder(bool isFloat)
    {
      _float = isFloat;
    }

    public override void SetMetadata()
    {
      Column.Type = _float ? ColumnType.Float : ColumnType.Double;
      Column.ClrType = _float ? typeof(float) : typeof(double);
      ClrValueDecoder = FloatValueDecoder;
      if (!_float)
        ClrValueDecoder = DoubleValueDecoder;
    }

    private object FloatValueDecoder(byte[] bytes)
    {
      float value = 0;
      if (!CodedInputStream.CreateInstance(bytes).ReadFloat(ref value))
        throw new MySqlException(ResourcesX.UnableToDecodeDataValue);
      return value;
    }

    private object DoubleValueDecoder(byte[] bytes)
    {
      double value = 0;
      if (!CodedInputStream.CreateInstance(bytes).ReadDouble(ref value))
        throw new MySqlException(ResourcesX.UnableToDecodeDataValue);
      return value;
    }
  }
}
