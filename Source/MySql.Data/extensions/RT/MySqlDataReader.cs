using System;

namespace MySql.Data.MySqlClient
{
  public sealed partial class MySqlDataReader : RTDataReader
  {
  }

  public abstract class RTDataReader
  {
    private RTDataReader() { }

    public abstract int FieldCount { get; }
    public abstract bool HasRows { get; }
    public abstract bool IsClosed { get; }
    public abstract int RecordsAffected { get; }
    public abstract object this[int i] { get; }
    public abstract object this[String name] { get; }

    public abstract void Close();
    public abstract bool GetBoolean(int i);
    public abstract byte GetByte(int i);
    public abstract char GetChar(int i);
    public abstract decimal GetDecimal(int i);
    public abstract double GetDouble(int i);
    public abstract float GetFloat(int i);
    public abstract Guid GetGuid(int i);
    public abstract Int16 GetInt16(int i);
    public abstract Int32 GetInt32(int i);
    public abstract Int64 GetInt64(int i);
    public abstract string GetString(int i);
    public abstract object GetValue(int i);
    public abstract int GetValues(object[] values);

    public abstract long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length);
    public abstract long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length);
    public abstract string GetDataTypeName(int ordinal);
    public abstract DateTime GetDateTime(int ordinal);
    public abstract Type GetFieldType(int ordinal);
    public abstract string GetName(int ordinal);
    public abstract int GetOrdinal(string name);
    public abstract bool IsDBNull(int i);
    public abstract bool NextResult();
    public abstract bool Read();
  }
}
