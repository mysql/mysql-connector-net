// Copyright (c) 2004, 2022, Oracle and/or its affiliates.
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

using MySql.Data.MySqlClient;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace MySql.Data.Types
{

  internal struct MySqlDouble : IMySqlValue
  {
    public MySqlDouble(bool isNull)
    {
      IsNull = isNull;
      Value = 0.0;
    }

    public MySqlDouble(double val)
    {
      IsNull = false;
      Value = val;
    }

    #region IMySqlValue Members

    public bool IsNull { get; }

    MySqlDbType IMySqlValue.MySqlDbType => MySqlDbType.Double;

    object IMySqlValue.Value => Value;

    public double Value { get; }

    Type IMySqlValue.SystemType => typeof(double);

    string IMySqlValue.MySqlTypeName => "DOUBLE";

    async Task IMySqlValue.WriteValueAsync(MySqlPacket packet, bool binary, object val, int length, bool execAsync)
    {
      double v = val as double? ?? Convert.ToDouble(val);
      if (binary)
        await packet.WriteAsync(PacketBitConverter.GetBytes(v), execAsync).ConfigureAwait(false);
      else
        await packet.WriteStringNoNullAsync(v.ToString("R", CultureInfo.InvariantCulture), execAsync).ConfigureAwait(false);
    }

    async Task<IMySqlValue> IMySqlValue.ReadValueAsync(MySqlPacket packet, long length, bool nullVal, bool execAsync)
    {
      if (nullVal)
        return new MySqlDouble(true);

      if (length == -1)
      {
        byte[] b = new byte[8];
        await packet.ReadAsync(b, 0, 8, execAsync).ConfigureAwait(false);
        return new MySqlDouble(PacketBitConverter.ToDouble(b, 0));
      }

      string s = await packet.ReadStringAsync(length, execAsync).ConfigureAwait(false);
      double d;

      try
      {
        d = Double.Parse(s, CultureInfo.InvariantCulture);
      }
      catch (OverflowException)
      {
        // MySQL server < 5.5 can return values not compatible with
        // Double.Parse(), i.e out of range for double.

        if (s.StartsWith("-", StringComparison.Ordinal))
          d = double.MinValue;
        else
          d = double.MaxValue;
      }

      return new MySqlDouble(d);
    }

    void IMySqlValue.SkipValue(MySqlPacket packet)
    {
      packet.Position += 8;
    }

    #endregion

    internal static void SetDSInfo(MySqlSchemaCollection sc)
    {
      // we use name indexing because this method will only be called
      // when GetSchema is called for the DataSourceInformation 
      // collection and then it wil be cached.
      MySqlSchemaRow row = sc.AddRow();
      row["TypeName"] = "DOUBLE";
      row["ProviderDbType"] = MySqlDbType.Double;
      row["ColumnSize"] = 0;
      row["CreateFormat"] = "DOUBLE";
      row["CreateParameters"] = null;
      row["DataType"] = "System.Double";
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
