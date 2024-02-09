// Copyright © 2004, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using System.Threading.Tasks;

namespace MySql.Data.Types
{
  internal struct MySqlString : IMySqlValue
  {
    private readonly MySqlDbType _type;

    public MySqlString(MySqlDbType type, bool isNull)
    {
      _type = type;
      IsNull = isNull;
      Value = String.Empty;
    }

    public MySqlString(MySqlDbType type, string val)
    {
      _type = type;
      IsNull = false;
      Value = val;
    }

    #region IMySqlValue Members

    public bool IsNull { get; }

    MySqlDbType IMySqlValue.MySqlDbType => _type;

    object IMySqlValue.Value => Value;

    public string Value { get; }

    Type IMySqlValue.SystemType => typeof(string);

    string IMySqlValue.MySqlTypeName => _type == MySqlDbType.Set ? "SET" : _type == MySqlDbType.Enum ? "ENUM" : "VARCHAR";

    async Task IMySqlValue.WriteValueAsync(MySqlPacket packet, bool binary, object val, int length, bool execAsync)
    {
      string v = val.ToString();
      if (length > 0)
      {
        length = Math.Min(length, v.Length);
        v = v.Substring(0, length);
      }

      if (binary)
        await packet.WriteLenStringAsync(v, execAsync).ConfigureAwait(false);
      else
        await packet.WriteStringNoNullAsync("'" + MySqlHelper.EscapeString(v) + "'", execAsync).ConfigureAwait(false);
    }

    async Task<IMySqlValue> IMySqlValue.ReadValueAsync(MySqlPacket packet, long length, bool nullVal, bool execAsync)
    {
      if (nullVal)
        return new MySqlString(_type, true);

      string s = String.Empty;
      if (length == -1)
        s = await packet.ReadLenStringAsync(execAsync).ConfigureAwait(false);
      else
        s = await packet.ReadStringAsync(length, execAsync).ConfigureAwait(false);
      MySqlString str = new MySqlString(_type, s);
      return str;
    }

    void IMySqlValue.SkipValue(MySqlPacket packet)
    {
      int len = (int)packet.ReadFieldLength();
      packet.Position += len;
    }

    #endregion

    internal static void SetDSInfo(MySqlSchemaCollection sc)
    {
      string[] types = new string[] { "CHAR", "NCHAR", "VARCHAR", "NVARCHAR", "SET",
                "ENUM", "TINYTEXT", "TEXT", "MEDIUMTEXT", "LONGTEXT" };
      MySqlDbType[] dbtype = new MySqlDbType[] { MySqlDbType.String, MySqlDbType.String,
                MySqlDbType.VarChar, MySqlDbType.VarChar, MySqlDbType.Set, MySqlDbType.Enum,
                MySqlDbType.TinyText, MySqlDbType.Text, MySqlDbType.MediumText,
                MySqlDbType.LongText };

      // we use name indexing because this method will only be called
      // when GetSchema is called for the DataSourceInformation 
      // collection and then it wil be cached.
      for (int x = 0; x < types.Length; x++)
      {
        MySqlSchemaRow row = sc.AddRow();
        row["TypeName"] = types[x];
        row["ProviderDbType"] = dbtype[x];
        row["ColumnSize"] = 0;
        row["CreateFormat"] = x < 4 ? types[x] + "({0})" : types[x];
        row["CreateParameters"] = x < 4 ? "size" : null;
        row["DataType"] = "System.String";
        row["IsAutoincrementable"] = false;
        row["IsBestMatch"] = true;
        row["IsCaseSensitive"] = false;
        row["IsFixedLength"] = false;
        row["IsFixedPrecisionScale"] = true;
        row["IsLong"] = false;
        row["IsNullable"] = true;
        row["IsSearchable"] = true;
        row["IsSearchableWithLike"] = true;
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
