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
using System.Threading.Tasks;

namespace MySql.Data.Types
{
  internal struct MySqlGuid : IMySqlValue
  {
    public MySqlGuid(byte[] buff)
    {
      OldGuids = false;
      Value = new Guid(buff);
      IsNull = false;
      Bytes = buff;
    }

    public byte[] Bytes { get; }

    public bool OldGuids { get; set; }

    #region IMySqlValue Members

    public bool IsNull { get; private set; }

    MySqlDbType IMySqlValue.MySqlDbType => MySqlDbType.Guid;

    object IMySqlValue.Value => Value;

    public Guid Value { get; private set; }

    Type IMySqlValue.SystemType => typeof(Guid);

    string IMySqlValue.MySqlTypeName => OldGuids ? "BINARY(16)" : "CHAR(36)";

    /// <summary>
    /// Writes the GUID value to the MySQL packet.
    /// If <see cref="OldGuids"/> is <c>true</c>, writes in the old binary format (16 bytes). Otherwise, writes as a string in "D" format (CHAR(36)), with length prefix in binary mode or escaped quoted string in text mode.
    /// </summary>
    /// <param name="packet">The MySQL packet to write the value to.</param>
    /// <param name="binary">Indicates whether to write in binary mode (<c>true</c>) or text mode (<c>false</c>).</param>
    /// <param name="val">The GUID value to write, which can be a Guid, string, or byte array.</param>
    /// <param name="length">The length of the value.</param>
    void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
    {
      Guid guid = GetGuid(val);

      if (OldGuids)
        WriteOldGuid(packet, guid, binary);
      else
      {
        if (binary)
          packet.WriteLenString(guid.ToString("D"));
        else
          packet.WriteStringNoNull("'" + MySqlHelper.EscapeString(guid.ToString("D")) + "'");
      }
    }

    /// <summary>
    /// Asynchronously writes the GUID value to the MySQL packet.
    /// If <see cref="OldGuids"/> is <c>true</c>, writes in the old binary format (16 bytes). Otherwise, writes as a string in "D" format (CHAR(36)), with length prefix in binary mode or escaped quoted string in text mode.
    /// </summary>
    /// <param name="packet">The MySQL packet to write the value to.</param>
    /// <param name="binary">Indicates whether to write in binary mode (<c>true</c>) or text mode (<c>false</c>).</param>
    /// <param name="val">The GUID value to write, which can be a Guid, string, or byte array.</param>
    /// <param name="length">The length of the value.</param>
    async Task IMySqlValue.WriteValueAsync(MySqlPacket packet, bool binary, object val, int length)
    {
      Guid guid = GetGuid(val);

      if (OldGuids)
        await WriteOldGuidAsync(packet, guid, binary).ConfigureAwait(false);
      else
      {
        if (binary)
          await packet.WriteLenStringAsync(guid.ToString("D")).ConfigureAwait(false);
        else
          await packet.WriteStringNoNullAsync("'" + MySqlHelper.EscapeString(guid.ToString("D")) + "'").ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Writes the GUID in the old binary format (16 bytes) to the MySQL packet.
    /// In binary mode, writes with length prefix; in text mode, prefixes with "_binary " and writes escaped hex bytes as a quoted string.
    /// </summary>
    /// <param name="packet">The MySQL packet to write the value to.</param>
    /// <param name="guid">The GUID value to write.</param>
    /// <param name="binary">Indicates whether to write in binary mode (<c>true</c>) or text mode (<c>false</c>).</param>
    private void WriteOldGuid(MySqlPacket packet, Guid guid, bool binary)
    {
      byte[] bytes = guid.ToByteArray();
      if (binary)
      {
        packet.WriteLength(bytes.Length);
        packet.Write(bytes);
      }
      else
      {
        packet.WriteStringNoNull("_binary ");
        packet.WriteByte((byte)'\'');
        EscapeByteArray(bytes, bytes.Length, packet);
        packet.WriteByte((byte)'\'');
      }
    }

    /// <summary>
    /// Asynchronously writes the GUID in the old binary format (16 bytes) to the MySQL packet.
    /// In binary mode, writes with length prefix; in text mode, prefixes with "_binary " and writes escaped hex bytes as a quoted string.
    /// </summary>
    /// <param name="packet">The MySQL packet to write the value to.</param>
    /// <param name="guid">The GUID value to write.</param>
    /// <param name="binary">Indicates whether to write in binary mode (<c>true</c>) or text mode (<c>false</c>).</param>
    private async Task WriteOldGuidAsync(MySqlPacket packet, Guid guid, bool binary)
    {
      byte[] bytes = guid.ToByteArray();

      if (binary)
      {
        await packet.WriteLengthAsync(bytes.Length).ConfigureAwait(false);
        await packet.WriteAsync(bytes).ConfigureAwait(false);
      }
      else
      {
        await packet.WriteStringNoNullAsync("_binary ").ConfigureAwait(false);
        packet.WriteByte((byte)'\'');
        EscapeByteArray(bytes, bytes.Length, packet);
        packet.WriteByte((byte)'\'');
      }
    }

    private static void EscapeByteArray(byte[] bytes, int length, MySqlPacket packet)
    {
      for (int x = 0; x < length; x++)
      {
        byte b = bytes[x];
        if (b == '\0')
        {
          packet.WriteByte((byte)'\\');
          packet.WriteByte((byte)'0');
        }

        else if (b == '\\' || b == '\'' || b == '\"')
        {
          packet.WriteByte((byte)'\\');
          packet.WriteByte(b);
        }
        else
          packet.WriteByte(b);
      }
    }

    /// <summary>
    /// Parses the input value to a Guid, supporting Guid, string, or byte array inputs.
    /// </summary>
    /// <param name="val">The input value to parse as Guid.</param>
    /// <returns>The parsed Guid value.</returns>
    /// <exception cref="MySqlException">Thrown if the value cannot be parsed as a Guid.</exception>
    private Guid GetGuid(object val)
    {
      Guid guid = Guid.Empty;
      string valAsString = val as string;
      byte[] valAsByte = val as byte[];
      if (val is Guid)
        return (Guid)val;
      else
      {
        try
        {
          if (valAsString != null)
            return new Guid(valAsString);
          else if (valAsByte != null)
            return new Guid(valAsByte);
        }
        catch (Exception ex)
        {
          throw new MySqlException(Resources.DataNotInSupportedFormat, ex);
        }
      }
      return guid;
    }

    private MySqlGuid ReadOldGuid(MySqlPacket packet, long length)
    {
      if (length == -1)
        length = (long)packet.ReadFieldLength();
      byte[] buff = new byte[length];
      packet.Read(buff, 0, (int)length);
      MySqlGuid g = new MySqlGuid(buff);
      g.OldGuids = OldGuids;
      return g;
    }

    private async Task<MySqlGuid> ReadOldGuidAsync(MySqlPacket packet, long length)
    {
      if (length == -1)
        length = (long)packet.ReadFieldLength();

      byte[] buff = new byte[length];
      await packet.ReadAsync(buff, 0, (int)length).ConfigureAwait(false);
      MySqlGuid g = new MySqlGuid(buff);
      g.OldGuids = OldGuids;
      return g;
    }

    /// <summary>
    /// Reads the GUID value from the MySQL packet.
    /// If <see cref="OldGuids"/> is <c>true</c>, reads as binary bytes (16 bytes) and constructs a GUID. Otherwise, reads as a string and parses to GUID.
    /// </summary>
    /// <param name="packet">The MySQL packet to read from.</param>
    /// <param name="length">The length of the value to read. A value of -1 indicates length-prefixed reading.</param>
    /// <param name="nullVal">Indicates if the value is null.</param>
    /// <returns>A new <see cref="MySqlGuid"/> instance representing the read value, or a null instance if <paramref name="nullVal"/> is <c>true</c>.</returns>
    IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
    {
      MySqlGuid g = new MySqlGuid();
      g.IsNull = true;
      g.OldGuids = OldGuids;
      if (!nullVal)
      {
        if (OldGuids)
          return ReadOldGuid(packet, length);
        string s = String.Empty;
        if (length == -1)
          s = packet.ReadLenString();
        else
          s = packet.ReadString(length);
        g.Value = new Guid(s);
        g.IsNull = false;
      }
      return g;
    }

    /// <summary>
    /// Asynchronously reads the GUID value from the MySQL packet.
    /// If <see cref="OldGuids"/> is <c>true</c>, reads as binary bytes (16 bytes) and constructs a GUID. Otherwise, reads as a string and parses to GUID.
    /// </summary>
    /// <param name="packet">The MySQL packet to read from.</param>
    /// <param name="length">The length of the value to read. A value of -1 indicates length-prefixed reading.</param>
    /// <param name="nullVal">Indicates if the value is null.</param>
    /// <returns>A new <see cref="MySqlGuid"/> instance representing the read value, or a null instance if <paramref name="nullVal"/> is <c>true</c>.</returns>
    async Task<IMySqlValue> IMySqlValue.ReadValueAsync(MySqlPacket packet, long length, bool nullVal)
    {
      MySqlGuid g = new MySqlGuid();
      g.IsNull = true;
      g.OldGuids = OldGuids;
      if (!nullVal)
      {
        if (OldGuids)
          return await ReadOldGuidAsync(packet, length).ConfigureAwait(false);
        string s = String.Empty;
        if (length == -1)
          s = await packet.ReadLenStringAsync().ConfigureAwait(false);
        else
          s = await packet.ReadStringAsync(length).ConfigureAwait(false);
        g.Value = new Guid(s);
        g.IsNull = false;
      }
      return g;
    }

    void IMySqlValue.SkipValue(MySqlPacket packet)
    {
      int len = (int)packet.ReadFieldLength();
      packet.Position += len;
    }

    #endregion

    public static void SetDSInfo(MySqlSchemaCollection sc)
    {
      // we use name indexing because this method will only be called
      // when GetSchema is called for the DataSourceInformation 
      // collection and then it wil be cached.
      MySqlSchemaRow row = sc.AddRow();
      row["TypeName"] = "GUID";
      row["ProviderDbType"] = MySqlDbType.Guid;
      row["ColumnSize"] = 0;
      row["CreateFormat"] = "BINARY(16)";
      row["CreateParameters"] = null;
      row["DataType"] = "System.Guid";
      row["IsAutoincrementable"] = false;
      row["IsBestMatch"] = true;
      row["IsCaseSensitive"] = false;
      row["IsFixedLength"] = true;
      row["IsFixedPrecisionScale"] = true;
      row["IsLong"] = false;
      row["IsNullable"] = true;
      row["IsSearchable"] = false;
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
