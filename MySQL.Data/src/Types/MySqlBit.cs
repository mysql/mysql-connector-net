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

    /// <summary>
    /// Writes the BIT value to the MySQL packet.
    /// Converts the input value to ulong. In binary mode, writes as 8-byte integer. In text mode, writes as string representation.
    /// </summary>
    /// <param name="packet">The MySQL packet stream to write into.</param>
    /// <param name="binary">True for binary protocol (8-byte fixed length), false for text protocol (string).</param>
    /// <param name="value">The value to write, convertible to ulong.</param>
    /// <param name="length">Not used for BIT type.</param>
    void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object value, int length)
    {
      ulong v = value as ulong? ?? Convert.ToUInt64(value);
      if (binary)
        packet.WriteInteger((long)v, 8);
      else
        packet.WriteStringNoNull(v.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Asynchronously writes the BIT value to the MySQL packet.
    /// Converts the input value to ulong. In binary mode, writes as 8-byte integer. In text mode, writes as string representation.
    /// </summary>
    /// <param name="packet">The MySQL packet stream to write into.</param>
    /// <param name="binary">True for binary protocol (8-byte fixed length), false for text protocol (string).</param>
    /// <param name="value">The value to write, convertible to ulong.</param>
    /// <param name="length">Not used for BIT type.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    async Task IMySqlValue.WriteValueAsync(MySqlPacket packet, bool binary, object value, int length)
    {
      ulong v = value as ulong? ?? Convert.ToUInt64(value);
      if (binary)
        await packet.WriteIntegerAsync((long)v, 8).ConfigureAwait(false);
      else
        await packet.WriteStringNoNullAsync(v.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the BIT value from the MySQL packet.
    /// Sets IsNull if indicated. If ReadAsString is true, parses the string representation as ulong; otherwise, reads as bit value.
    /// </summary>
    /// <param name="packet">The MySQL packet to read from.</param>
    /// <param name="length">The length of the field; -1 means read length from packet.</param>
    /// <param name="isNull">Indicates if the value is null.</param>
    /// <returns>The MySqlBit instance with the read value.</returns>
    IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool isNull)
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

    /// <summary>
    /// Asynchronously reads the BIT value from the MySQL packet.
    /// Sets IsNull if indicated. If ReadAsString is true, parses the string representation as ulong; otherwise, reads as bit value.
    /// </summary>
    /// <param name="packet">The MySQL packet to read from.</param>
    /// <param name="length">The length of the field; -1 means read length from packet.</param>
    /// <param name="isNull">Indicates if the value is null.</param>
    /// <returns>A task that returns the MySqlBit instance with the read value.</returns>
    async Task<IMySqlValue> IMySqlValue.ReadValueAsync(MySqlPacket packet, long length, bool isNull)
    {
      this.IsNull = isNull;
      if (isNull)
        return this;

      if (length == -1)
        length = packet.ReadFieldLength();

      if (ReadAsString)
        _value = UInt64.Parse(await packet.ReadStringAsync(length).ConfigureAwait(false), CultureInfo.InvariantCulture);
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
