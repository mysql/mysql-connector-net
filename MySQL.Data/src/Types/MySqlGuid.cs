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

    async Task IMySqlValue.WriteValueAsync(MySqlPacket packet, bool binary, object val, int length, bool execAsync)
    {
      Guid guid = Guid.Empty;
      string valAsString = val as string;
      byte[] valAsByte = val as byte[];

      if (val is Guid)
        guid = (Guid)val;
      else
      {
        try
        {
          if (valAsString != null)
            guid = new Guid(valAsString);
          else if (valAsByte != null)
            guid = new Guid(valAsByte);
        }
        catch (Exception ex)
        {
          throw new MySqlException(Resources.DataNotInSupportedFormat, ex);
        }
      }

      if (OldGuids)
        await WriteOldGuidAsync(packet, guid, binary, execAsync).ConfigureAwait(false);
      else
      {
        guid.ToString("D");

        if (binary)
          await packet.WriteLenStringAsync(guid.ToString("D"), execAsync).ConfigureAwait(false);
        else
          await packet.WriteStringNoNullAsync("'" + MySqlHelper.EscapeString(guid.ToString("D")) + "'", execAsync).ConfigureAwait(false);
      }
    }

    private async Task WriteOldGuidAsync(MySqlPacket packet, Guid guid, bool binary, bool execAsync)
    {
      byte[] bytes = guid.ToByteArray();

      if (binary)
      {
        await packet.WriteLengthAsync(bytes.Length, execAsync).ConfigureAwait(false);
        await packet.WriteAsync(bytes, execAsync).ConfigureAwait(false);
      }
      else
      {
        await packet.WriteStringNoNullAsync("_binary ", execAsync).ConfigureAwait(false);
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

    private async Task<MySqlGuid> ReadOldGuidAsync(MySqlPacket packet, long length, bool execAsync)
    {
      if (length == -1)
        length = (long)packet.ReadFieldLength();

      byte[] buff = new byte[length];
      await packet.ReadAsync(buff, 0, (int)length, execAsync).ConfigureAwait(false);
      MySqlGuid g = new MySqlGuid(buff);
      g.OldGuids = OldGuids;
      return g;
    }

    async Task<IMySqlValue> IMySqlValue.ReadValueAsync(MySqlPacket packet, long length, bool nullVal, bool execAsync)
    {
      MySqlGuid g = new MySqlGuid();
      g.IsNull = true;
      g.OldGuids = OldGuids;
      if (!nullVal)
      {
        if (OldGuids)
          return await ReadOldGuidAsync(packet, length, execAsync).ConfigureAwait(false);
        string s = String.Empty;
        if (length == -1)
          s = await packet.ReadLenStringAsync(execAsync).ConfigureAwait(false);
        else
          s = await packet.ReadStringAsync(length, execAsync).ConfigureAwait(false);
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
