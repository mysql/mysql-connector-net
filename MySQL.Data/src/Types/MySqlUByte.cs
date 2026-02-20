// Copyright © 2004, 2026, Oracle and/or its affiliates.
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
using System.Globalization;
using System.Threading.Tasks;

namespace MySql.Data.Types
{
  internal struct MySqlUByte : IMySqlValue
  {
    public MySqlUByte(bool isNull)
    {
      IsNull = isNull;
      Value = 0;
    }

    public MySqlUByte(byte val)
    {
      IsNull = false;
      Value = val;
    }

    #region IMySqlValue Members

    public bool IsNull { get; }

    MySqlDbType IMySqlValue.MySqlDbType => MySqlDbType.UByte;

    object IMySqlValue.Value => Value;

    public byte Value { get; }

    Type IMySqlValue.SystemType => typeof(byte);

    string IMySqlValue.MySqlTypeName => "TINYINT";

    /// <summary>
    /// Writes the unsigned byte value to the MySQL packet, either in binary or text format.
    /// </summary>
    /// <param name="packet">The MySQL packet to write the value to.</param>
    /// <param name="binary">Indicates whether to write the value in binary (<c>true</c>, 1 byte) or text (<c>false</c>, string) format.</param>
    /// <param name="val">The byte value to write.</param>
    /// <param name="length">The length of the value.</param>
    void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
    {
      byte v = val as byte? ?? Convert.ToByte(val);
      if (binary)
        packet.WriteByte(v);
      else
        packet.WriteStringNoNull(v.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Asynchronously writes the unsigned byte value to the MySQL packet, either in binary or text format.
    /// </summary>
    /// <param name="packet">The MySQL packet to write the value to.</param>
    /// <param name="binary">Indicates whether to write the value in binary (<c>true</c>, 1 byte) or text (<c>false</c>, string) format.</param>
    /// <param name="val">The byte value to write.</param>
    /// <param name="length">The length of the value.</param>
    async Task IMySqlValue.WriteValueAsync(MySqlPacket packet, bool binary, object val, int length)
    {
      byte v = val as byte? ?? Convert.ToByte(val);
      if (binary)
        packet.WriteByte(v);
      else
        await packet.WriteStringNoNullAsync(v.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously reads the unsigned byte value from the MySQL packet.
    /// </summary>
    /// <param name="packet">The MySQL packet to read from.</param>
    /// <param name="length">The length of the value to read. A value of -1 indicates binary format (1 byte).</param>
    /// <param name="nullVal">Indicates if the value is null.</param>
    /// <returns>A new <see cref="MySqlUByte"/> instance representing the read value, or a null instance if <paramref name="nullVal"/> is <c>true</c>.</returns>
    async Task<IMySqlValue> IMySqlValue.ReadValueAsync(MySqlPacket packet, long length, bool nullVal)
    {
      if (nullVal)
        return new MySqlUByte(true);

      if (length == -1)
        return new MySqlUByte((byte)packet.ReadByte());
      else
        return new MySqlUByte(Byte.Parse(await packet.ReadStringAsync(length).ConfigureAwait(false), CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Reads the unsigned byte value from the MySQL packet.
    /// </summary>
    /// <param name="packet">The MySQL packet to read from.</param>
    /// <param name="length">The length of the value to read. A value of -1 indicates binary format (1 byte).</param>
    /// <param name="nullVal">Indicates if the value is null.</param>
    /// <returns>A new <see cref="MySqlUByte"/> instance representing the read value, or a null instance if <paramref name="nullVal"/> is <c>true</c>.</returns>
    IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
    {
      if (nullVal)
        return new MySqlUByte(true);
      if (length == -1)
        return new MySqlUByte((byte)packet.ReadByte());
      else
        return new MySqlUByte(Byte.Parse(packet.ReadString(length), CultureInfo.InvariantCulture));
    }

    void IMySqlValue.SkipValue(MySqlPacket packet)
    {
      packet.ReadByte();
    }

    #endregion

    internal static void SetDSInfo(MySqlSchemaCollection sc)
    {
      // we use name indexing because this method will only be called
      // when GetSchema is called for the DataSourceInformation 
      // collection and then it wil be cached.
      MySqlSchemaRow row = sc.AddRow();
      row["TypeName"] = "TINYINT";
      row["ProviderDbType"] = MySqlDbType.UByte;
      row["ColumnSize"] = 0;
      row["CreateFormat"] = "TINYINT UNSIGNED";
      row["CreateParameters"] = null;
      row["DataType"] = "System.Byte";
      row["IsAutoincrementable"] = true;
      row["IsBestMatch"] = true;
      row["IsCaseSensitive"] = false;
      row["IsFixedLength"] = true;
      row["IsFixedPrecisionScale"] = true;
      row["IsLong"] = false;
      row["IsNullable"] = true;
      row["IsSearchable"] = true;
      row["IsSearchableWithLike"] = false;
      row["IsUnsigned"] = true;
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
