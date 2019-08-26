// Copyright (c) 2004, 2019, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.Types;
using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading;

namespace MySql.Data.MySqlClient
{
  /// <include file='docs/MySqlDataReader.xml' path='docs/ClassSummary/*'/>
  public sealed partial class MySqlDataReader : DbDataReader, IDataReader, IDataRecord, IDisposable
  {
    // The DataReader should always be open when returned to the user.
    private bool _isOpen = true;

    internal long affectedRows;
    internal Driver driver;

    // Used in special circumstances with stored procs to avoid exceptions from DbDataAdapter
    // If set, AffectedRows returns -1 instead of 0.
    private readonly bool _disableZeroAffectedRows;

    /* 
     * Keep track of the connection in order to implement the
     * CommandBehavior.CloseConnection flag. A null reference means
     * normal behavior (do not automatically close).
     */
    private MySqlConnection _connection;

    /*
     * Because the user should not be able to directly create a 
     * DataReader object, the constructors are
     * marked as internal.
     */
    internal MySqlDataReader(MySqlCommand cmd, PreparableStatement statement, CommandBehavior behavior)
    {
      this.Command = cmd;
      _connection = Command.Connection;
      CommandBehavior = behavior;
      driver = _connection.driver;
      affectedRows = -1;
      this.Statement = statement;

      if (cmd.CommandType == CommandType.StoredProcedure
        && cmd.UpdatedRowSource == UpdateRowSource.FirstReturnedRecord
      )
      {
        _disableZeroAffectedRows = true;
      }
    }

    #region Properties

    internal PreparableStatement Statement { get; }

    internal MySqlCommand Command { get; private set; }

    internal ResultSet ResultSet { get; private set; }

    internal CommandBehavior CommandBehavior { get; private set; }

    /// <summary>
    /// Gets the number of columns in the current row.
    /// </summary>
    public override int FieldCount => ResultSet?.Size ?? 0;

    /// <summary>
    /// Gets a value indicating whether the MySqlDataReader contains one or more rows.
    /// </summary>
    public override bool HasRows => ResultSet?.HasRows ?? false;

    /// <summary>
    /// Gets a value indicating whether the data reader is closed.
    /// </summary>
    public override bool IsClosed => !_isOpen;

    /// <summary>
    /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
    /// </summary>
    public override int RecordsAffected
    {
      // RecordsAffected returns the number of rows affected in batch
      // statments from insert/delete/update statments.  This property
      // is not completely accurate until .Close() has been called.
      get
      {
        if (!_disableZeroAffectedRows) return (int)affectedRows;
        // In special case of updating stored procedure called from 
        // within data adapter, we return -1 to avoid exceptions 
        // (s. Bug#54895)
        if (affectedRows == 0)
          return -1;

        return (int)affectedRows;
      }
    }

    /// <summary>
    /// Overloaded. Gets the value of a column in its native format.
    /// In C#, this property is the indexer for the MySqlDataReader class.
    /// </summary>
    public override object this[int i] => GetValue(i);

    /// <summary>
    /// Gets the value of a column in its native format.
    ///	[C#] In C#, this property is the indexer for the MySqlDataReader class.
    /// </summary>
    public override object this[String name] => this[GetOrdinal(name)];

    /// <summary>
    /// Gets a value indicating the depth of nesting for the current row.  This method is not 
    /// supported currently and always returns 0.
    /// </summary>
    public override int Depth => 0;

    #endregion

    /// <summary>
    /// Closes the MySqlDataReader object.
    /// </summary>
    public override void Close()
    {
      if (!_isOpen) return;

      bool shouldCloseConnection = (CommandBehavior & CommandBehavior.CloseConnection) != 0;
      CommandBehavior originalBehavior = CommandBehavior;

      // clear all remaining resultsets
      try
      {
        // Temporarily change to Default behavior to allow NextResult to finish properly.
        if (!originalBehavior.Equals(CommandBehavior.SchemaOnly))
          CommandBehavior = CommandBehavior.Default;
        while (NextResult()) { }
      }
      catch (MySqlException ex)
      {
        // Ignore aborted queries
        if (!ex.IsQueryAborted)
        {
          // ignore IO exceptions.
          // We are closing or disposing reader, and  do not
          // want exception to be propagated to used. If socket is
          // is closed on the server side, next query will run into
          // IO exception. If reader is closed by GC, we also would 
          // like to avoid any exception here. 
          bool isIOException = false;
          for (Exception exception = ex; exception != null;
            exception = exception.InnerException)
          {
            if (exception is System.IO.IOException)
            {
              isIOException = true;
              break;
            }
          }
          if (!isIOException)
          {
            // Ordinary exception (neither IO nor query aborted)
            throw;
          }
        }
      }
      catch (System.IO.IOException)
      {
        // eat, on the same reason we eat IO exceptions wrapped into 
        // MySqlExceptions reasons, described above.
      }
      finally
      {
        // always ensure internal reader is null (Bug #55558)
        _connection.Reader = null;
        CommandBehavior = originalBehavior;
      }
      // we now give the command a chance to terminate.  In the case of
      // stored procedures it needs to update out and inout parameters
      Command.Close(this);
      CommandBehavior = CommandBehavior.Default;

      if (this.Command.Canceled && _connection.driver.Version.isAtLeast(5, 1, 0))
      {
        // Issue dummy command to clear kill flag
        ClearKillFlag();
      }

      if (shouldCloseConnection)
        _connection.Close();

      Command = null;
      _connection.IsInUse = false;
      _connection = null;
      _isOpen = false;
    }

    #region TypeSafe Accessors

    /// <summary>
    /// Gets the value of the specified column as a Boolean.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool GetBoolean(string name)
    {
      return GetBoolean(GetOrdinal(name));
    }

    /// <summary>
    /// Gets the value of the specified column as a Boolean.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public override bool GetBoolean(int i)
    {
      var asValue = GetValue(i);
      int numericValue;
      if (int.TryParse(asValue as string, out numericValue))
        return Convert.ToBoolean(numericValue);
      return Convert.ToBoolean(asValue);
    }

    /// <summary>
    /// Gets the value of the specified column as a byte.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public byte GetByte(string name)
    {
      return GetByte(GetOrdinal(name));
    }

    /// <summary>
    /// Gets the value of the specified column as a byte.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public override byte GetByte(int i)
    {
      IMySqlValue v = GetFieldValue(i, false);
      if (v is MySqlUByte)
        return ((MySqlUByte)v).Value;
      else
        return (byte)((MySqlByte)v).Value;
    }

    /// <summary>
    /// Gets the value of the specified column as a sbyte.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public sbyte GetSByte(string name)
    {
      return GetSByte(GetOrdinal(name));
    }

    /// <summary>
    /// Gets the value of the specified column as a sbyte.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public sbyte GetSByte(int i)
    {
      IMySqlValue v = GetFieldValue(i, false);
      if (v is MySqlByte)
        return ((MySqlByte)v).Value;
      else
        return (sbyte)((MySqlByte)v).Value;
    }

    /// <summary>
    /// Reads a stream of bytes from the specified column offset into the buffer an array starting at the given buffer offset.
    /// </summary>
    /// <param name="i">The zero-based column ordinal. </param>
    /// <param name="fieldOffset">The index within the field from which to begin the read operation. </param>
    /// <param name="buffer">The buffer into which to read the stream of bytes. </param>
    /// <param name="bufferoffset">The index for buffer to begin the read operation. </param>
    /// <param name="length">The maximum length to copy into the buffer. </param>
    /// <returns>The actual number of bytes read.</returns>
    /// <include file='docs/MySqlDataReader.xml' path='MyDocs/MyMembers[@name="GetBytes"]/*'/>
    public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
    {
      if (i >= FieldCount)
        Throw(new IndexOutOfRangeException());

      IMySqlValue val = GetFieldValue(i, false);

      if (!(val is MySqlBinary) && !(val is MySqlGuid))
        Throw(new MySqlException("GetBytes can only be called on binary or guid columns"));

      byte[] bytes = null;
      if (val is MySqlBinary)
        bytes = ((MySqlBinary)val).Value;
      else
        bytes = ((MySqlGuid)val).Bytes;

      if (buffer == null)
        return bytes.Length;

      if (bufferoffset >= buffer.Length || bufferoffset < 0)
        Throw(new IndexOutOfRangeException("Buffer index must be a valid index in buffer"));
      if (buffer.Length < (bufferoffset + length))
        Throw(new ArgumentException("Buffer is not large enough to hold the requested data"));
      if (fieldOffset < 0 ||
        ((ulong)fieldOffset >= (ulong)bytes.Length && (ulong)bytes.Length > 0))
        Throw(new IndexOutOfRangeException("Data index must be a valid index in the field"));

      // adjust the length so we don't run off the end
      if ((ulong)bytes.Length < (ulong)(fieldOffset + length))
      {
        length = (int)((ulong)bytes.Length - (ulong)fieldOffset);
      }

      Buffer.BlockCopy(bytes, (int)fieldOffset, buffer, (int)bufferoffset, (int)length);

      return length;
    }

    private object ChangeType(IMySqlValue value, int fieldIndex, Type newType)
    {
      ResultSet.Fields[fieldIndex].AddTypeConversion(newType);
      return Convert.ChangeType(value.Value, newType, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the value of the specified column as a single character.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public char GetChar(string name)
    {
      return GetChar(GetOrdinal(name));
    }

    /// <summary>
    /// Gets the value of the specified column as a single character.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public override char GetChar(int i)
    {
      string s = GetString(i);
      return s[0];
    }

    /// <summary>
    /// Reads a stream of characters from the specified column offset into the buffer as an array starting at the given buffer offset.
    /// </summary>
    /// <param name="i"></param>
    /// <param name="fieldoffset"></param>
    /// <param name="buffer"></param>
    /// <param name="bufferoffset"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
    {
      if (i >= FieldCount)
        Throw(new IndexOutOfRangeException());

      string valAsString = GetString(i);

      if (buffer == null) return valAsString.Length;

      if (bufferoffset >= buffer.Length || bufferoffset < 0)
        Throw(new IndexOutOfRangeException("Buffer index must be a valid index in buffer"));
      if (buffer.Length < (bufferoffset + length))
        Throw(new ArgumentException("Buffer is not large enough to hold the requested data"));
      if (fieldoffset < 0 || fieldoffset >= valAsString.Length)
        Throw(new IndexOutOfRangeException("Field offset must be a valid index in the field"));

      if (valAsString.Length < length)
        length = valAsString.Length;
      valAsString.CopyTo((int)fieldoffset, buffer, bufferoffset, length);
      return length;
    }

    /// <summary>
    /// Gets the name of the source data type.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public override String GetDataTypeName(int i)
    {
      if (!_isOpen)
        Throw(new Exception("No current query in data reader"));
      if (i >= FieldCount)
        Throw(new IndexOutOfRangeException());

      // return the name of the type used on the backend
      IMySqlValue v = ResultSet.Values[i];
      return v.MySqlTypeName;
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetMySqlDateTime/*'/>
    public MySqlDateTime GetMySqlDateTime(string column)
    {
      return GetMySqlDateTime(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetMySqlDateTime/*'/>
    public MySqlDateTime GetMySqlDateTime(int column)
    {
      return (MySqlDateTime)GetFieldValue(column, true);
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetDateTimeS/*'/>
    public DateTime GetDateTime(string column)
    {
      return GetDateTime(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetDateTime/*'/>
    public override DateTime GetDateTime(int i)
    {
      IMySqlValue val = GetFieldValue(i, true);
      MySqlDateTime dt;

      if (val is MySqlDateTime)
        dt = (MySqlDateTime)val;
      else
      {
        // we need to do this because functions like date_add return string
        string s = GetString(i);
        dt = MySqlDateTime.Parse(s);
      }

      dt.TimezoneOffset = driver.timeZoneOffset;
      if (_connection.Settings.ConvertZeroDateTime && !dt.IsValidDateTime)
        return DateTime.MinValue;
      else
        return dt.GetDateTime();
    }

    /// <summary>
    /// Gets the value of the specified column as a <see cref="MySqlDecimal"/>.
    /// </summary>
    /// <param name="column">The name of the colum.</param>
    /// <returns>The value of the specified column as a <see cref="MySqlDecimal"/>.</returns>
    public MySqlDecimal GetMySqlDecimal(string column)
    {
      return GetMySqlDecimal(GetOrdinal(column));
    }

    /// <summary>
    /// Gets the value of the specified column as a <see cref="MySqlDecimal"/>.
    /// </summary>
    /// <param name="i">The index of the colum.</param>
    /// <returns>The value of the specified column as a <see cref="MySqlDecimal"/>.</returns>
    public MySqlDecimal GetMySqlDecimal(int i)
    {
      return (MySqlDecimal)GetFieldValue(i, false);
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetDecimalS/*'/>
    public Decimal GetDecimal(string column)
    {
      return GetDecimal(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetDecimal/*'/>
    public override Decimal GetDecimal(int i)
    {
      IMySqlValue v = GetFieldValue(i, true);
      if (v is MySqlDecimal)
        return ((MySqlDecimal)v).Value;
      return Convert.ToDecimal(v.Value);
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetDoubleS/*'/>
    public double GetDouble(string column)
    {
      return GetDouble(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetDouble/*'/>
    public override double GetDouble(int i)
    {
      IMySqlValue v = GetFieldValue(i, true);
      if (v is MySqlDouble)
        return ((MySqlDouble)v).Value;
      return Convert.ToDouble(v.Value);
    }

    public Type GetFieldType(string column)
    {
      return GetFieldType(GetOrdinal(column));
    }

    /// <summary>
    /// Gets the Type that is the data type of the object.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public override Type GetFieldType(int i)
    {
      if (!_isOpen)
        Throw(new Exception("No current query in data reader"));
      if (i >= FieldCount)
        Throw(new IndexOutOfRangeException());

      // we have to use the values array directly because we can't go through
      // GetValue
      IMySqlValue v = ResultSet.Values[i];
      if (v is MySqlDateTime)
      {
        if (!_connection.Settings.AllowZeroDateTime)
          return typeof(DateTime);
        return typeof(MySqlDateTime);
      }
      return v.SystemType;
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetFloatS/*'/>
    public float GetFloat(string column)
    {
      return GetFloat(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetFloat/*'/>
    public override float GetFloat(int i)
    {
      IMySqlValue v = GetFieldValue(i, true);
      if (v is MySqlSingle)
        return ((MySqlSingle)v).Value;
      return Convert.ToSingle(v.Value);
    }

    /// <summary>
    /// Gets the value of the specified column as a globally-unique identifier(GUID).
    /// </summary>
    /// <param name="column">The name of the column.</param>
    /// <returns></returns>
    public Guid GetGuid(string column)
    {
      return GetGuid(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetGuid/*'/>
    public override Guid GetGuid(int i)
    {
      object v = GetValue(i);
      if (v is Guid)
        return (Guid)v;
      if (v is string)
        return new Guid(v as string);
      if (v is byte[])
      {
        byte[] bytes = (byte[])v;
        if (bytes.Length == 16)
          return new Guid(bytes);
      }
      Throw(new MySqlException(Resources.ValueNotSupportedForGuid));
      return Guid.Empty; // just to silence compiler
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetInt16S/*'/>
    public Int16 GetInt16(string column)
    {
      return GetInt16(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetInt16/*'/>
    public override Int16 GetInt16(int i)
    {
      IMySqlValue v = GetFieldValue(i, true);
      if (v is MySqlInt16)
        return ((MySqlInt16)v).Value;

      return (short)ChangeType(v, i, typeof(short));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetInt32S/*'/>
    public Int32 GetInt32(string column)
    {
      return GetInt32(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetInt32/*'/>
    public override Int32 GetInt32(int i)
    {
      IMySqlValue v = GetFieldValue(i, true);
      if (v is MySqlInt32)
        return ((MySqlInt32)v).Value;

      return (Int32)ChangeType(v, i, typeof(Int32));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetInt64S/*'/>
    public Int64 GetInt64(string column)
    {
      return GetInt64(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetInt64/*'/>
    public override Int64 GetInt64(int i)
    {
      IMySqlValue v = GetFieldValue(i, true);
      if (v is MySqlInt64)
        return ((MySqlInt64)v).Value;

      return (Int64)ChangeType(v, i, typeof(Int64));
    }

    /// <summary>
    /// Gets the name of the specified column.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public override String GetName(int i)
    {
      if (!_isOpen)
        Throw(new Exception("No current query in data reader"));
      if (i >= FieldCount)
        Throw(new IndexOutOfRangeException());

      return ResultSet.Fields[i].ColumnName;
    }

    /// <summary>
    /// Gets the column ordinal, given the name of the column.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public override int GetOrdinal(string name)
    {
      if (!_isOpen || ResultSet == null)
        Throw(new Exception("No current query in data reader"));

      return ResultSet.GetOrdinal(name);
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetStringS/*'/>
    public string GetString(string column)
    {
      return GetString(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetString/*'/>
    public override String GetString(int i)
    {
      IMySqlValue val = GetFieldValue(i, true);

      if (val is MySqlBinary)
      {
        byte[] v = ((MySqlBinary)val).Value;
        return ResultSet.Fields[i].Encoding.GetString(v, 0, v.Length);
      }

      return val.Value.ToString();
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetTimeSpan/*'/>
    public TimeSpan GetTimeSpan(string column)
    {
      return GetTimeSpan(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetTimeSpan/*'/>
    public TimeSpan GetTimeSpan(int column)
    {
      IMySqlValue val = GetFieldValue(column, true);

      MySqlTimeSpan ts = (MySqlTimeSpan)val;
      return ts.Value;
    }

    /// <summary>
    /// Gets the value of the specified column in its native format.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public override object GetValue(int i)
    {
      if (!_isOpen)
        Throw(new Exception("No current query in data reader"));
      if (i >= FieldCount)
        Throw(new IndexOutOfRangeException());

      IMySqlValue val = GetFieldValue(i, false);
      if (val.IsNull)
      {
        if (!(val.MySqlDbType == MySqlDbType.Time && val.Value.ToString() == "00:00:00"))
          return DBNull.Value;
      }

      // if the column is a date/time, then we return a MySqlDateTime
      // so .ToString() will print '0000-00-00' correctly
      if (val is MySqlDateTime)
      {
        MySqlDateTime dt = (MySqlDateTime)val;
        if (!dt.IsValidDateTime && _connection.Settings.ConvertZeroDateTime)
          return DateTime.MinValue;
        else if (_connection.Settings.AllowZeroDateTime)
          return val;
        else
          return dt.GetDateTime();
      }

      return val.Value;
    }

    /// <summary>
    /// Gets all attribute columns in the collection for the current row.
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public override int GetValues(object[] values)
    {
      int numCols = Math.Min(values.Length, FieldCount);
      for (int i = 0; i < numCols; i++)
        values[i] = GetValue(i);

      return numCols;
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetUInt16/*'/>
    public UInt16 GetUInt16(string column)
    {
      return GetUInt16(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetUInt16/*'/>
    public UInt16 GetUInt16(int column)
    {
      IMySqlValue v = GetFieldValue(column, true);
      if (v is MySqlUInt16)
        return ((MySqlUInt16)v).Value;

      return (UInt16)ChangeType(v, column, typeof(UInt16));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetUInt32/*'/>
    public UInt32 GetUInt32(string column)
    {
      return GetUInt32(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetUInt32/*'/>
    public UInt32 GetUInt32(int column)
    {
      IMySqlValue v = GetFieldValue(column, true);
      if (v is MySqlUInt32)
        return ((MySqlUInt32)v).Value;
      return (uint)ChangeType(v, column, typeof(UInt32));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetUInt64/*'/>
    public UInt64 GetUInt64(string column)
    {
      return GetUInt64(GetOrdinal(column));
    }

    /// <include file='docs/MySqlDataReader.xml' path='docs/GetUInt64/*'/>
    public UInt64 GetUInt64(int column)
    {
      IMySqlValue v = GetFieldValue(column, true);
      if (v is MySqlUInt64)
        return ((MySqlUInt64)v).Value;

      return (UInt64)ChangeType(v, column, typeof(UInt64));
    }


    #endregion

    IDataReader IDataRecord.GetData(int i)
    {
      return base.GetData(i);
    }

    /// <summary>
    /// Gets a value indicating whether the column contains non-existent or missing values.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public override bool IsDBNull(int i)
    {
      return DBNull.Value == GetValue(i);
    }

    /// <summary>
    /// Advances the data reader to the next result, when reading the results of batch SQL statements.
    /// </summary>
    /// <returns></returns>
    public override bool NextResult()
    {
      if (!_isOpen)
        Throw(new MySqlException(Resources.NextResultIsClosed));

      bool isCaching = Command.CommandType == CommandType.TableDirect && Command.EnableCaching &&
        (CommandBehavior & CommandBehavior.SequentialAccess) == 0;

      // this will clear out any unread data
      if (ResultSet != null)
      {
        ResultSet.Close();
        if (isCaching)
          TableCache.AddToCache(Command.CommandText, ResultSet);
      }

      // single result means we only return a single resultset.  If we have already
      // returned one, then we return false
      // TableDirect is basically a select * from a single table so it will generate
      // a single result also
      if (ResultSet != null &&
        ((CommandBehavior & CommandBehavior.SingleResult) != 0 || isCaching))
        return false;

      // next load up the next resultset if any
      try
      {
        do
        {
          ResultSet = null;
          // if we are table caching, then try to retrieve the resultSet from the cache
          if (isCaching)
            ResultSet = TableCache.RetrieveFromCache(Command.CommandText,
              Command.CacheAge);

          if (ResultSet == null)
          {
            ResultSet = driver.NextResult(Statement.StatementId, false);
            if (ResultSet == null) return false;
            if (ResultSet.IsOutputParameters && Command.CommandType == CommandType.StoredProcedure)
            {
              StoredProcedure sp = Statement as StoredProcedure;
              sp.ProcessOutputParameters(this);
              ResultSet.Close();
              if (!sp.ServerProvidingOutputParameters) return false;
              // if we are using server side output parameters then we will get our ok packet
              // *after* the output parameters resultset
              ResultSet = driver.NextResult(Statement.StatementId, true);
            }
            ResultSet.Cached = isCaching;
          }

          if (ResultSet.Size == 0)
          {
            Command.LastInsertedId = ResultSet.InsertedId;
            if (affectedRows == -1)
              affectedRows = ResultSet.AffectedRows;
            else
              affectedRows += ResultSet.AffectedRows;
          }
        } while (ResultSet.Size == 0);

        return true;
      }
      catch (MySqlException ex)
      {
        if (ex.IsFatal)
          _connection.Abort();
        if (ex.Number == 0)
          throw new MySqlException(Resources.FatalErrorReadingResult, ex);
        if ((CommandBehavior & CommandBehavior.CloseConnection) != 0)
          Close();
        throw;
      }
    }

    /// <summary>
    /// Advances the MySqlDataReader to the next record.
    /// </summary>
    /// <returns></returns>
    public override bool Read()
    {
      if (!_isOpen)
        Throw(new MySqlException("Invalid attempt to Read when reader is closed."));
      if (ResultSet == null)
        return false;

      try
      {
        return ResultSet.NextRow(CommandBehavior);
      }
      catch (TimeoutException tex)
      {
        _connection.HandleTimeoutOrThreadAbort(tex);
        throw; // unreached
      }
      catch (ThreadAbortException taex)
      {
        _connection.HandleTimeoutOrThreadAbort(taex);
        throw;
      }
      catch (MySqlException ex)
      {
        if (ex.IsFatal)
          _connection.Abort();

        if (ex.IsQueryAborted)
        {
          throw;
        }

        throw new MySqlException(Resources.FatalErrorDuringRead, ex);
      }
    }

    /// <summary>
    /// Gets the value of the specified column as a <see cref="MySqlGeometry"/>.
    /// </summary>
    /// <param name="i">The index of the colum.</param>
    /// <returns>The value of the specified column as a <see cref="MySqlGeometry"/>.</returns>
    public MySqlGeometry GetMySqlGeometry(int i)
    {
      try
      {
        IMySqlValue v = GetFieldValue(i, false);
        if (v is MySqlGeometry || v is MySqlBinary)
          return new MySqlGeometry(MySqlDbType.Geometry, (Byte[])v.Value);
      }
      catch
      {
        Throw(new Exception("Can't get MySqlGeometry from value"));
      }
      return new MySqlGeometry(true);
    }

    /// <summary>
    /// Gets the value of the specified column as a <see cref="MySqlGeometry"/>.
    /// </summary>
    /// <param name="column">The name of the colum.</param>
    /// <returns>The value of the specified column as a <see cref="MySqlGeometry"/>.</returns>
    public MySqlGeometry GetMySqlGeometry(string column)
    {
      return GetMySqlGeometry(GetOrdinal(column));
    }

    /// <summary>
    /// Returns an <see cref="IEnumerator"/> that iterates through the <see cref="MySqlDataReader"/>. 
    /// </summary>    
    public override IEnumerator GetEnumerator()
    {
      return new DbEnumerator(this, (CommandBehavior & CommandBehavior.CloseConnection) != 0);
    }

    private IMySqlValue GetFieldValue(int index, bool checkNull)
    {
      if (index < 0 || index >= FieldCount)
        Throw(new ArgumentException(Resources.InvalidColumnOrdinal));

      IMySqlValue v = ResultSet[index];

      if (!(v.MySqlDbType is MySqlDbType.Time && v.Value.ToString() == "00:00:00"))
      {
        if (checkNull && v.IsNull)
          throw new System.Data.SqlTypes.SqlNullValueException();
      }

      return v;
    }


    private void ClearKillFlag()
    {
      // This query will silently crash because of the Kill call that happened before.
      string dummyStatement = "SELECT * FROM bogus_table LIMIT 0"; /* dummy query used to clear kill flag */
      MySqlCommand dummyCommand = new MySqlCommand(dummyStatement, _connection) { InternallyCreated = true };

      try
      {
        dummyCommand.ExecuteReader(); // ExecuteReader catches the exception and returns null, which is expected.
      }
      catch (MySqlException ex)
      {
        if (ex.Number != (int)MySqlErrorCode.NoSuchTable) throw;
      }
    }

    private void ProcessOutputParameters()
    {
      // if we are not 5.5 or later or we are not prepared then we are simulating output parameters
      // with user variables and they are also string so we have to work some magic with out
      // column types before we read the data
      if (!driver.SupportsOutputParameters || !Command.IsPrepared)
        AdjustOutputTypes();

      // now read the output parameters data row
      if ((CommandBehavior & CommandBehavior.SchemaOnly) != 0) return;
      ResultSet.NextRow(CommandBehavior);

      string prefix = "@" + StoredProcedure.ParameterPrefix;

      for (int i = 0; i < FieldCount; i++)
      {
        string fieldName = GetName(i);
        if (fieldName.StartsWith(prefix))
          fieldName = fieldName.Remove(0, prefix.Length);
        MySqlParameter parameter = Command.Parameters.GetParameterFlexible(fieldName, true);
        parameter.Value = GetValue(i);
      }
    }

    private void AdjustOutputTypes()
    {
      // since MySQL likes to return user variables as strings
      // we reset the types of the readers internal value objects
      // this will allow those value objects to parse the string based
      // return values
      for (int i = 0; i < FieldCount; i++)
      {
        string fieldName = GetName(i);
        fieldName = fieldName.Remove(0, StoredProcedure.ParameterPrefix.Length + 1);
        MySqlParameter parameter = Command.Parameters.GetParameterFlexible(fieldName, true);

        IMySqlValue v = MySqlField.GetIMySqlValue(parameter.MySqlDbType);
        if (v is MySqlBit)
        {
          MySqlBit bit = (MySqlBit)v;
          bit.ReadAsString = true;
          ResultSet.SetValueObject(i, bit);
        }
        else
          ResultSet.SetValueObject(i, v);
      }
    }

    public override T GetFieldValue<T>(int ordinal)
    {
      if (typeof(T).Equals(typeof(DateTimeOffset)))
      {
        var dtValue = new DateTime();
        var result = DateTime.TryParse(this.GetValue(ordinal).ToString(), out dtValue);
        DateTime datetime = result ? dtValue : DateTime.MinValue;
        return (T)Convert.ChangeType(new DateTimeOffset(datetime), typeof(T));
      }
      else
        return base.GetFieldValue<T>(ordinal);
    }

    private void Throw(Exception ex)
    {
      _connection?.Throw(ex);
      throw ex;
    }

    public new void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    internal new void Dispose(bool disposing)
    {
      if (disposing)
      {
        Close();
      }
    }

    #region Destructor
    ~MySqlDataReader()
    {
      Dispose(false);
    }
    #endregion
  }
}
