// Copyright � 2004, 2016 Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.MySqlClient;

namespace MySql.Data.Types
{
  internal struct MySqlInt16 : IMySqlValue
  {
    public MySqlInt16(bool isNull)
    {
      IsNull = isNull;
      Value = 0;
    }

    public MySqlInt16(short val)
    {
      IsNull = false;
      Value = val;
    }

    #region IMySqlValue Members

    public bool IsNull { get; }

    MySqlDbType IMySqlValue.MySqlDbType => MySqlDbType.Int16;

    object IMySqlValue.Value => Value;

    public short Value { get; }

    Type IMySqlValue.SystemType => typeof(short);

    string IMySqlValue.MySqlTypeName => "SMALLINT";

    void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
    {
      int v = val as int? ?? Convert.ToInt32(val);
      if (binary)
        packet.WriteInteger((long)v, 2);
      else
        packet.WriteStringNoNull(v.ToString());
    }

    IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
    {
      if (nullVal)
        return new MySqlInt16(true);

      if (length == -1)
        return new MySqlInt16((short)packet.ReadInteger(2));
      else
        return new MySqlInt16(Int16.Parse(packet.ReadString(length)));
    }

    void IMySqlValue.SkipValue(MySqlPacket packet)
    {
      packet.Position += 2;
    }

    #endregion

    internal static void SetDSInfo(MySqlSchemaCollection sc)
    {
      // we use name indexing because this method will only be called
      // when GetSchema is called for the DataSourceInformation 
      // collection and then it wil be cached.
      MySqlSchemaRow row = sc.AddRow();
      row["TypeName"] = "SMALLINT";
      row["ProviderDbType"] = MySqlDbType.Int16;
      row["ColumnSize"] = 0;
      row["CreateFormat"] = "SMALLINT";
      row["CreateParameters"] = null;
      row["DataType"] = "System.Int16";
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
