// Copyright © 2004, 2016 Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Globalization;
using MySql.Data.MySqlClient;

namespace MySql.Data.Types
{
  public struct MySqlDecimal : IMySqlValue
  {
    private readonly string _value;

    internal MySqlDecimal(bool isNull)
    {
      IsNull = isNull;
      _value = null;
      Precision = Scale = 0;
    }

    internal MySqlDecimal(string val)
    {
      this.IsNull = false;
      Precision = Scale = 0;
      _value = val;
    }

    #region IMySqlValue Members

    public bool IsNull { get; }

    MySqlDbType IMySqlValue.MySqlDbType => MySqlDbType.Decimal;

    public byte Precision { get; set; }

    public byte Scale { get; set; }


    object IMySqlValue.Value => Value;

    public decimal Value => Convert.ToDecimal(_value, CultureInfo.InvariantCulture);

    public double ToDouble()
    {
      return Double.Parse(_value);
    }

    public override string ToString()
    {
      return _value;
    }

    Type IMySqlValue.SystemType => typeof(decimal);

    string IMySqlValue.MySqlTypeName => "DECIMAL";

    void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
    {
      decimal v = val as decimal? ?? Convert.ToDecimal(val);
      string valStr = v.ToString(CultureInfo.InvariantCulture);
      if (binary)
        packet.WriteLenString(valStr);
      else
        packet.WriteStringNoNull(valStr);
    }

    IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
    {
      if (nullVal)
        return new MySqlDecimal(true);

      string s = String.Empty;
      s = length == -1 ? packet.ReadLenString() : packet.ReadString(length);
      return new MySqlDecimal(s);
    }

    void IMySqlValue.SkipValue(MySqlPacket packet)
    {
      int len = (int)packet.ReadFieldLength();
      packet.Position += len;
    }

    #endregion

    internal static void SetDSInfo(MySqlSchemaCollection sc)
    {
      // we use name indexing because this method will only be called
      // when GetSchema is called for the DataSourceInformation 
      // collection and then it wil be cached.
      MySqlSchemaRow row = sc.AddRow();
      row["TypeName"] = "DECIMAL";
      row["ProviderDbType"] = MySqlDbType.NewDecimal;
      row["ColumnSize"] = 0;
      row["CreateFormat"] = "DECIMAL({0},{1})";
      row["CreateParameters"] = "precision,scale";
      row["DataType"] = "System.Decimal";
      row["IsAutoincrementable"] = false;
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
