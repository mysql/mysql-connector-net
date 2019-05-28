// Copyright (c) 2004, 2019, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Globalization;
using MySql.Data.MySqlClient;

namespace MySql.Data.Types
{
  /// <summary>
  /// Summary description for MySqlUInt64.
  /// </summary>
  internal struct MySqlBit : IMySqlValue
  {
    private ulong _value;

    public MySqlBit(bool isnull)
    {
      _value = 0;
      IsNull = isnull;
      ReadAsString = false;
    }

    public bool ReadAsString { get; set; }

    public bool IsNull { get; private set; }

    MySqlDbType IMySqlValue.MySqlDbType => MySqlDbType.Bit;

    object IMySqlValue.Value => _value;

    Type IMySqlValue.SystemType => typeof(ulong);

    string IMySqlValue.MySqlTypeName => "BIT";

    public void WriteValue(MySqlPacket packet, bool binary, object value, int length)
    {
      ulong v = value as ulong? ?? Convert.ToUInt64(value);
      if (binary)
        packet.WriteInteger((long)v, 8);
      else
        packet.WriteStringNoNull(v.ToString(CultureInfo.InvariantCulture));
    }

    public IMySqlValue ReadValue(MySqlPacket packet, long length, bool isNull)
    {
      this.IsNull = isNull;
      if (isNull)
        return this;

      if (length == -1)
        length = packet.ReadFieldLength();

      if (ReadAsString)
        _value = UInt64.Parse(packet.ReadString(length), CultureInfo.InvariantCulture);
      else
        _value = (UInt64)packet.ReadBitValue((int)length);
      return this;
    }

    public void SkipValue(MySqlPacket packet)
    {
      int len = (int)packet.ReadFieldLength();
      packet.Position += len;
    }

    internal static void SetDSInfo(MySqlSchemaCollection sc)
    {
      // we use name indexing because this method will only be called
      // when GetSchema is called for the DataSourceInformation 
      // collection and then it wil be cached.
      MySqlSchemaRow row = sc.AddRow();
      row["TypeName"] = "BIT";
      row["ProviderDbType"] = MySqlDbType.Bit;
      row["ColumnSize"] = 64;
      row["CreateFormat"] = "BIT";
      row["CreateParameters"] = DBNull.Value;
      row["DataType"] = typeof(ulong).ToString();
      row["IsAutoincrementable"] = false;
      row["IsBestMatch"] = true;
      row["IsCaseSensitive"] = false;
      row["IsFixedLength"] = false;
      row["IsFixedPrecisionScale"] = true;
      row["IsLong"] = false;
      row["IsNullable"] = true;
      row["IsSearchable"] = true;
      row["IsSearchableWithLike"] = false;
      row["IsUnsigned"] = false;
      row["MaximumScale"] = 0;
      row["MinimumScale"] = 0;
      row["IsConcurrencyType"] = DBNull.Value;
      row["IsLiteralSupported"] = false;
      row["LiteralPrefix"] = DBNull.Value;
      row["LiteralSuffix"] = DBNull.Value;
      row["NativeDataType"] = DBNull.Value;
    }
  }
}
