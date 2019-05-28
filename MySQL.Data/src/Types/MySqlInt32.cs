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
  internal struct MySqlInt32 : IMySqlValue
  {
    private readonly int _value;
    private readonly bool _is24Bit;

    private MySqlInt32(MySqlDbType type)
    {
      _is24Bit = type == MySqlDbType.Int24;
      IsNull = true;
      _value = 0;
    }

    public MySqlInt32(MySqlDbType type, bool isNull)
      : this(type)
    {
      IsNull = isNull;
    }

    public MySqlInt32(MySqlDbType type, int val)
      : this(type)
    {
      IsNull = false;
      _value = val;
    }

    #region IMySqlValue Members

    public bool IsNull { get; }

    MySqlDbType IMySqlValue.MySqlDbType => MySqlDbType.Int32;

    object IMySqlValue.Value => _value;

    public int Value => _value;

    Type IMySqlValue.SystemType => typeof(Int32);

    string IMySqlValue.MySqlTypeName => _is24Bit ? "MEDIUMINT" : "INT";

    void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
    {
      int v = val as int? ?? Convert.ToInt32(val);
      if (binary)
        packet.WriteInteger(v, _is24Bit ? 3 : 4);
      else
        packet.WriteStringNoNull(v.ToString(CultureInfo.InvariantCulture));
    }

    IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
    {
      if (nullVal)
        return new MySqlInt32((this as IMySqlValue).MySqlDbType, true);

      if (length == -1)
        return new MySqlInt32((this as IMySqlValue).MySqlDbType,
                     packet.ReadInteger(4));
      else
        return new MySqlInt32((this as IMySqlValue).MySqlDbType,
                     Int32.Parse(packet.ReadString(length),
           CultureInfo.InvariantCulture));
    }

    void IMySqlValue.SkipValue(MySqlPacket packet)
    {
      packet.Position += 4;
    }

    #endregion

    internal static void SetDSInfo(MySqlSchemaCollection sc)
    {
      string[] types = new string[] { "INT", "YEAR", "MEDIUMINT" };
      MySqlDbType[] dbtype = new MySqlDbType[] { MySqlDbType.Int32, 
                MySqlDbType.Year, MySqlDbType.Int24 };

      // we use name indexing because this method will only be called
      // when GetSchema is called for the DataSourceInformation 
      // collection and then it wil be cached.
      for (int x = 0; x < types.Length; x++)
      {
        MySqlSchemaRow row = sc.AddRow();
        row["TypeName"] = types[x];
        row["ProviderDbType"] = dbtype[x];
        row["ColumnSize"] = 0;
        row["CreateFormat"] = types[x];
        row["CreateParameters"] = null;
        row["DataType"] = "System.Int32";
        row["IsAutoincrementable"] = dbtype[x] != MySqlDbType.Year;
        row["IsBestMatch"] = true;
        row["IsCaseSensitive"] = false;
        row["IsFixedLength"] = true;
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
        row["LiteralPrefix"] = null;
        row["LiteralSuffix"] = null;
        row["NativeDataType"] = null;
      }
    }
  }
}
