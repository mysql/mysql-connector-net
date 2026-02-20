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

  internal struct MySqlBinary : IMySqlValue
  {
    private readonly MySqlDbType _type;

    private readonly byte[] _mValue;

    public MySqlBinary(MySqlDbType type, bool isNull)
    {
      _type = type;
      IsNull = isNull;
      _mValue = null;
    }

    public MySqlBinary(MySqlDbType type, byte[] val)
    {
      _type = type;
      IsNull = false;
      _mValue = val;
    }

    #region IMySqlValue Members

    public bool IsNull { get; }

    MySqlDbType IMySqlValue.MySqlDbType => _type;

    object IMySqlValue.Value => _mValue;

    public byte[] Value => _mValue;

    Type IMySqlValue.SystemType => typeof(byte[]);

    string IMySqlValue.MySqlTypeName
    {
      get
      {
        switch (_type)
        {
          case MySqlDbType.TinyBlob: return "TINY_BLOB";
          case MySqlDbType.MediumBlob: return "MEDIUM_BLOB";
          case MySqlDbType.LongBlob: return "LONG_BLOB";
          case MySqlDbType.Vector: return "VECTOR";
          default:
            return "BLOB";
        }
      }
    }

    /// <summary>
    /// Writes the binary value to the MySQL packet. Supports both binary protocol format and escaped text format for non-binary mode.
    /// Converts input value (byte[] or string) to bytes using the packet's encoding if necessary.
    /// </summary>
    /// <param name="packet">The MySQL packet stream to write the value into.</param>
    /// <param name="binary">If true, writes in binary format (length-prefixed); if false, writes as escaped string prefixed with '_binary '.</param>
    /// <param name="val">The value to write; must be byte[], char[], or string. Throws MySqlException if unsupported type.</param>
    /// <param name="length">The maximum length to write; 0 or greater than value length means write all.</param>
    void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val, int length)
    {
      GetBytesToWrite(packet, val, length, out byte[] buffToWrite, out int writeLength);
      if (binary)
      {
        packet.WriteLength(writeLength);
        packet.Write(buffToWrite, 0, writeLength);
      }
      else
      {
        packet.WriteStringNoNull("_binary ");
        packet.WriteByte((byte)'\'');
        EscapeByteArray(buffToWrite, writeLength, packet);
        packet.WriteByte((byte)'\'');
      }
    }

    /// <summary>
    /// Asynchronously writes the binary value to the MySQL packet. Supports both binary protocol format and escaped text format for non-binary mode.
    /// Converts input value (byte[] or string) to bytes using the packet's encoding if necessary.
    /// </summary>
    /// <param name="packet">The MySQL packet stream to write the value into.</param>
    /// <param name="binary">If true, writes in binary format (length-prefixed); if false, writes as escaped string prefixed with '_binary '.</param>
    /// <param name="val">The value to write; must be byte[], char[], or string. Throws MySqlException if unsupported type.</param>
    /// <param name="length">The maximum length to write; 0 or greater than value length means write all.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    async Task IMySqlValue.WriteValueAsync(MySqlPacket packet, bool binary, object val, int length)
    {
      GetBytesToWrite(packet, val, length, out byte[] buffToWrite, out int writeLength);
      if (binary)
      {
        await packet.WriteLengthAsync(writeLength).ConfigureAwait(false);
        await packet.WriteAsync(buffToWrite, 0, writeLength).ConfigureAwait(false);
      }
      else
      {
        await packet.WriteStringNoNullAsync("_binary ").ConfigureAwait(false);
        packet.WriteByte((byte)'\'');
        EscapeByteArray(buffToWrite, writeLength, packet);
        packet.WriteByte((byte)'\'');
      }
    }

    /// <summary>
    /// Reads the binary value from the MySQL packet.
    /// If the value is null, returns a null MySqlBinary instance.
    /// Otherwise, reads the length (if not provided) and the binary data bytes from the packet.
    /// </summary>
    /// <param name="packet">The MySQL packet to read the value from.</param>
    /// <param name="length">The length of the binary data; -1 indicates the length should be read from the packet.</param>
    /// <param name="nullVal">Indicates if the value is null.</param>
    /// <returns>A MySqlBinary instance representing the read value.</returns>
    IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length, bool nullVal)
    {
      if (nullVal)
      {
        return new MySqlBinary(_type, true); 
      }

      long effectiveLength = length == -1 ? (long)packet.ReadFieldLength(): length;
      byte[] newBuff = new byte[effectiveLength];
      packet.Read(newBuff, 0, (int)effectiveLength);
      return new MySqlBinary(_type, newBuff);
    }

    /// <summary>
    /// Asynchronously reads the binary value from the MySQL packet.
    /// If the value is null, returns a null MySqlBinary instance.
    /// Otherwise, reads the length (if not provided) and the binary data bytes from the packet.
    /// </summary>
    /// <param name="packet">The MySQL packet to read the value from.</param>
    /// <param name="length">The length of the binary data; -1 indicates the length should be read from the packet.</param>
    /// <param name="nullVal">Indicates if the value is null.</param>
    /// <returns>A task that represents the asynchronous read operation, returning a MySqlBinary instance.</returns>
    async Task<IMySqlValue> IMySqlValue.ReadValueAsync(MySqlPacket packet, long length, bool nullVal)
    {
      if (nullVal)
      {
        return new MySqlBinary(_type, true);
      }

      long effectiveLength = length == -1 ? (long)packet.ReadFieldLength(): length;
      byte[] newBuff = new byte[effectiveLength];
      await packet.ReadAsync(newBuff, 0, (int)effectiveLength).ConfigureAwait(false);
      return new MySqlBinary(_type, newBuff);
    }

    void IMySqlValue.SkipValue(MySqlPacket packet)
    {
      int len = (int)packet.ReadFieldLength();
      packet.Position += len;
    }

    #endregion

    public static void SetDSInfo(MySqlSchemaCollection sc)
    {
      string[] types = new string[] { "BLOB", "TINYBLOB", "MEDIUMBLOB", "LONGBLOB", "BINARY", "VARBINARY", "VECTOR" };
      MySqlDbType[] dbtype = new MySqlDbType[] { MySqlDbType.Blob,
                MySqlDbType.TinyBlob, MySqlDbType.MediumBlob, MySqlDbType.LongBlob, MySqlDbType.Binary, MySqlDbType.VarBinary,
                MySqlDbType.Vector};
      long[] sizes = new long[] { 65535L, 255L, 16777215L, 4294967295L, 255L, 65535L, 16777215L };
      string[] format = new string[] { null, null, null, null, "binary({0})", "varbinary({0})", null };
      string[] parms = new string[] { null, null, null, null, "length", "length", null };

      // we use name indexing because this method will only be called
      // when GetSchema is called for the DataSourceInformation 
      // collection and then it wil be cached.
      for (int x = 0; x < types.Length; x++)
      {
        MySqlSchemaRow row = sc.AddRow();
        row["TypeName"] = types[x];
        row["ProviderDbType"] = dbtype[x];
        row["ColumnSize"] = sizes[x];
        row["CreateFormat"] = format[x];
        row["CreateParameters"] = parms[x];
        row["DataType"] = "System.Byte[]";
        row["IsAutoincrementable"] = false;
        row["IsBestMatch"] = true;
        row["IsCaseSensitive"] = false;
        row["IsFixedLength"] = x < 4 ? false : true;
        row["IsFixedPrecisionScale"] = false;
        row["IsLong"] = sizes[x] > 255;
        row["IsNullable"] = true;
        row["IsSearchable"] = false;
        row["IsSearchableWithLike"] = false;
        row["IsUnsigned"] = DBNull.Value;
        row["MaximumScale"] = DBNull.Value;
        row["MinimumScale"] = DBNull.Value;
        row["IsConcurrencyType"] = DBNull.Value;
        row["IsLiteralSupported"] = false;
        row["LiteralPrefix"] = "0x";
        row["LiteralSuffix"] = DBNull.Value;
        row["NativeDataType"] = DBNull.Value;
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
    /// Converts the input value to a byte array and determines the effective length to write.
    /// Supports byte[], char[], and string inputs. For strings, applies truncation based on length if specified.
    /// Uses the packet's encoding for conversions. Throws MySqlException if the input type is unsupported.
    /// </summary>
    /// <param name="packet">The MySQL packet providing the encoding for conversions.</param>
    /// <param name="val">The input value to convert; must be byte[], char[], or string.</param>
    /// <param name="length">The maximum length; 0 means use full length.</param>
    /// <param name="buffToWrite">Output: The converted byte array.</param>
    /// <param name="writeLength">Output: The effective length to write (min of specified length and buffer length).</param>
    private static void GetBytesToWrite(MySqlPacket packet, object val, int length, out byte[] buffToWrite, out int writeLength)
    {
      buffToWrite = (val as byte[]);
      if (buffToWrite == null)
      {
        char[] valAsChar = (val as Char[]);
        if (valAsChar != null)
          buffToWrite = packet.Encoding.GetBytes(valAsChar);
        else
        {
          string s = val.ToString();
          if (length == 0)
            length = s.Length;
          else
            s = s.Substring(0, length);
          buffToWrite = packet.Encoding.GetBytes(s);
        }
      }

      // we assume zero or maxsize length means write all of the value
      if (length == 0 || buffToWrite.Length < length)
        writeLength = buffToWrite.Length;
      else
        writeLength = length;

      if (buffToWrite == null)
        throw new MySqlException("Only byte arrays and strings can be serialized by MySqlBinary");
    }
  }
}