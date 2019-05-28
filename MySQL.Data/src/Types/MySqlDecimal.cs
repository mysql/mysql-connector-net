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
  /// Represents a decimal data type object in a MySql database.
  /// </summary>
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

    /// <summary>
    /// Gets a boolean value signaling if the type is <c>null</c>.
    /// </summary>
    public bool IsNull { get; }

    MySqlDbType IMySqlValue.MySqlDbType => MySqlDbType.Decimal;

    /// <summary>
    /// Gets or sets the decimal precision of the type.
    /// </summary>
    public byte Precision { get; set; }

    /// <summary>
    /// Gets or sets the scale of the type.
    /// </summary>
    public byte Scale { get; set; }


    object IMySqlValue.Value => Value;

    /// <summary>
    /// Gets the decimal value associated to this type.
    /// </summary>
    public decimal Value => Convert.ToDecimal(_value, CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts this decimal value to a double value.
    /// </summary>
    /// <returns>The value of this type converted to a dobule value.</returns>
    public double ToDouble()
    {
      return Double.Parse(_value, CultureInfo.InvariantCulture);
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
