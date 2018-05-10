// Copyright (c) 2004, 2018, Oracle and/or its affiliates. All rights reserved.
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
  internal struct MySqlByte : IMySqlValue
  {
    public MySqlByte(bool isNull)
    {
      IsNull = isNull;
      Value = 0;
      TreatAsBoolean = false;
    }

    public MySqlByte(sbyte val)
    {
      IsNull = false;
      Value = val;
      TreatAsBoolean = false;
    }

    #region IMySqlValue Members

    public bool IsNull { get; }

    MySqlDbType IMySqlValue.MySqlDbType => MySqlDbType.Byte;

    object IMySqlValue.Value
    {
      get
      {
        if (TreatAsBoolean)
          return Convert.ToBoolean(Value);
        return Value;
      }
    }

    public sbyte Value { get; set; }

    Type IMySqlValue.SystemType => TreatAsBoolean ? typeof(bool) : typeof(sbyte);

    string IMySqlValue.MySqlTypeName => "TINYINT";

    void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
    {
      sbyte v = val as sbyte? ?? Convert.ToSByte(val);
      if (binary)
        packet.WriteByte((byte)v);
      else
        packet.WriteStringNoNull(v.ToString());
    }

    IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
    {
      if (nullVal)
        return new MySqlByte(true) { TreatAsBoolean = TreatAsBoolean };

      MySqlByte b;
      if (length == -1)
        b = new MySqlByte((sbyte)packet.ReadByte());
      else
      {
        string s = packet.ReadString(length);
        b = new MySqlByte(SByte.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));  
      }
      
      b.TreatAsBoolean = TreatAsBoolean;
      return b;
    }

    void IMySqlValue.SkipValue(MySqlPacket packet)
    {
      packet.ReadByte();
    }

    #endregion

    internal bool TreatAsBoolean { get; set; }

    internal static void SetDSInfo(MySqlSchemaCollection sc)
    {
      // we use name indexing because this method will only be called
      // when GetSchema is called for the DataSourceInformation 
      // collection and then it wil be cached.
      MySqlSchemaRow row = sc.AddRow();
      row["TypeName"] = "TINYINT";
      row["ProviderDbType"] = MySqlDbType.Byte;
      row["ColumnSize"] = 0;
      row["CreateFormat"] = "TINYINT";
      row["CreateParameters"] = null;
      row["DataType"] = "System.SByte";
      row["IsAutoincrementable"] = true;
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
