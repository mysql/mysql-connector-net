// Copyright (c) 2004, 2023, Oracle and/or its affiliates.
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  ///  Provides a means of reading a forward-only stream of rows from a MySQL database. This class cannot be inherited.
  /// </summary>
  /// <remarks>
  ///  <para>
  ///    To create a <see cref="MySqlDataReader"/>, you must call the <see cref="MySqlCommand.ExecuteReader()"/>
  ///    method of the <see cref="MySqlCommand"/> object, rather than directly using a constructor.
  ///  </para>
  ///  <para>
  ///    While the <see cref="MySqlDataReader"/> is in use, the associated <see cref="MySqlConnection"/>
  ///    is busy serving the <see cref="MySqlDataReader"/>, and no other operations can be performed
  ///    on the <see cref="MySqlConnection"/> other than closing it. This is the case until the
  ///    <see cref="Close"/> method of the <see cref="MySqlDataReader"/> is called.
  ///  </para>
  ///  <para>
  ///    <see cref="IsClosed"/> and <see cref="RecordsAffected"/>
  ///    are the only properties that you can call after the <see cref="MySqlDataReader"/> is
  ///    closed. Though the <see cref="RecordsAffected"/> property may be accessed at any time
  ///    while the <see cref="MySqlDataReader"/> exists, always call <see cref="Close"/> before returning
  ///    the value of <see cref="RecordsAffected"/> to ensure an accurate return value.
  ///  </para>
  ///  <para>
  ///    For optimal performance, <see cref="MySqlDataReader"/> avoids creating
  ///    unnecessary objects or making unnecessary copies of data. As a result, multiple calls
  ///    to methods such as <see cref="MySqlDataReader.GetValue"/> return a reference to the
  ///    same object. Use caution if you are modifying the underlying value of the objects
  ///    returned by methods such as <see cref="GetValue"/>.
  ///  </para>
  /// </remarks>
  public sealed class MySqlDataReader : DbDataReader, IDataReader, IDataRecord, IDisposable
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

      if (cmd.CommandType == CommandType.StoredProcedure && cmd.UpdatedRowSource == UpdateRowSource.FirstReturnedRecord)
        _disableZeroAffectedRows = true;
    }

    #region Properties

    internal PreparableStatement Statement { get; }

    internal MySqlCommand Command { get; private set; }

    internal ResultSet ResultSet { get; private set; }

    internal CommandBehavior CommandBehavior { get; private set; }

    /// <summary>
    /// Gets the number of columns in the current row.
    /// </summary>
    /// <returns>The number of columns in the current row.</returns>
    public override int FieldCount => ResultSet?.Size ?? 0;

    /// <summary>
    /// Gets a value indicating whether the <see cref="MySqlDataReader"/> contains one or more rows.
    /// </summary>
    /// <returns>true if the <see cref="MySqlDataReader"/> contains one or more rows; otherwise false.</returns>
    public override bool HasRows => ResultSet?.HasRows ?? false;

    /// <summary>
    /// Gets a value indicating whether the data reader is closed.
    /// </summary>
    /// <returns>true if the <see cref="MySqlDataReader"/> is closed; otherwise false.</returns>
    public override bool IsClosed => !_isOpen;

    /// <summary>
    /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
    /// </summary>
    /// <returns>The number of rows changed, inserted, or deleted. 
    /// -1 for SELECT statements; 0 if no rows were affected or the statement failed.</returns>
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
    /// In C#, this property is the indexer for the <see cref="MySqlDataReader"/> class.
    /// </summary>
    /// <returns>The value of the specified column.</returns>
    public override object this[int i] => GetValue(i);

    /// <summary>
    /// Gets the value of a column in its native format.
    ///	[C#] In C#, this property is the indexer for the <see cref="MySqlDataReader"/> class.
    /// </summary>
    /// <returns>The value of the specified column.</returns>
    public override object this[String name] => this[GetOrdinal(name)];

    /// <summary>
    /// Gets a value indicating the depth of nesting for the current row. This method is not 
    /// supported currently and always returns 0.
    /// </summary>
    /// <returns>The depth of nesting for the current row.</returns>
    public override int Depth => 0;

    #endregion

    /// <summary>
    /// Closes the <see cref="MySqlDataReader"/> object.
    /// </summary>
    public override void Close() => CloseAsync(false).GetAwaiter().GetResult();

#if NETSTANDARD2_1 || NET6_0_OR_GREATER
    /// <summary>
    /// Asynchronously closes the <see cref="MySqlDataReader"/> object.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override Task CloseAsync() => CloseAsync(true);
#endif

    internal async Task CloseAsync(bool execAsync)
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
        while (await NextResultAsync(execAsync, CancellationToken.None).ConfigureAwait(false)) { }
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
            if (exception is IOException)
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
      catch (IOException)
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
      // we now give the command a chance to terminate. In the case of
      // stored procedures it needs to update out and inout parameters
      await Command.CloseAsync(this, execAsync).ConfigureAwait(false);
      CommandBehavior = CommandBehavior.Default;

      if (this.Command.Canceled && _connection.driver.Version.isAtLeast(5, 1, 0))
      {
        // Issue dummy command to clear kill flag
        await ClearKillFlagAsync(execAsync).ConfigureAwait(false);
      }

      if (shouldCloseConnection)
        await _connection.CloseAsync().ConfigureAwait(false);

      Command = null;
      _connection.IsInUse = false;
      _connection = null;
      _isOpen = false;
    }

    #region TypeSafe Accessors

    /// <summary>
    /// Gets the value of the specified column as a Boolean.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public bool GetBoolean(string name)
    {
      return GetBoolean(GetOrdinal(name));
    }

    /// <summary>
    /// Gets the value of the specified column as a Boolean.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
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
    /// <param name="name">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public byte GetByte(string name)
    {
      return GetByte(GetOrdinal(name));
    }

    /// <summary>
    /// Gets the value of the specified column as a byte.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override byte GetByte(int i)
    {
      IMySqlValue v = GetFieldValue(i, false);
      if (v is MySqlUByte)
        return ((MySqlUByte)v).Value;
      else
        return checked((byte)((MySqlByte)v).Value);
    }

    /// <summary>
    /// Gets the value of the specified column as a sbyte.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public sbyte GetSByte(string name)
    {
      return GetSByte(GetOrdinal(name));
    }

    /// <summary>
    /// Gets the value of the specified column as a sbyte.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public sbyte GetSByte(int i)
    {
      IMySqlValue v = GetFieldValue(i, false);
      if (v is MySqlByte)
        return (sbyte)ChangeType(v, i, typeof(sbyte));
      else
#if !NET8_0
        return checked(((MySqlByte)v).Value);
#else
        return checked((sbyte)((MySqlUByte)v).Value);
#endif
    }

    /// <summary>
    /// Reads a stream of bytes from the specified column offset into the buffer an array starting at the given buffer offset.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <param name="fieldOffset">The index within the field from which to begin the read operation.</param>
    /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
    /// <param name="bufferoffset">The index for buffer to begin the read operation.</param>
    /// <param name="length">The maximum length to copy into the buffer.</param>
    /// <returns>The actual number of bytes read.</returns>
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
    /// <param name="name">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public char GetChar(string name)
    {
      return GetChar(GetOrdinal(name));
    }

    /// <summary>
    /// Gets the value of the specified column as a single character.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override char GetChar(int i)
    {
      string s = GetString(i);
      return s[0];
    }

    /// <summary>
    /// Reads a stream of characters from the specified column offset into the buffer as an array starting at the given buffer offset.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <param name="fieldoffset">The index within the row from which to begin the read operation.</param>
    /// <param name="buffer">The buffer into which to copy the data.</param>
    /// <param name="bufferoffset">The index with the buffer to which the data will be copied.</param>
    /// <param name="length">The maximum number of characters to read.</param>
    /// <returns>The actual number of characters read.</returns>
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
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>A string representing the name of the data type.</returns>
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

    /// <summary>
    ///  Gets the value of the specified column as a <see cref="MySqlDateTime"/> object.
    /// </summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="DateTime"/> object.</para>
    ///  <para>Call IsDBNull to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public MySqlDateTime GetMySqlDateTime(string column)
    {
      return GetMySqlDateTime(GetOrdinal(column));
    }

    /// <summary>
    ///  Gets the value of the specified column as a <see cref="MySqlDateTime"/> object.
    /// </summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="DateTime"/> object.</para>
    ///  <para>Call IsDBNull to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public MySqlDateTime GetMySqlDateTime(int column)
    {
      return (MySqlDateTime)GetFieldValue(column, true);
    }

    /// <summary>
    ///  Gets the value of the specified column as a <see cref="DateTime"/> object.
    /// </summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="DateTime"/> object.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    ///  <note>
    ///    <para>
    ///      MySql allows date columns to contain the value '0000-00-00' and datetime
    ///      columns to contain the value '0000-00-00 00:00:00'.  The DateTime structure cannot contain
    ///      or represent these values.  To read a datetime value from a column that might
    ///      contain zero values, use <see cref="GetMySqlDateTime(int)"/>.
    ///    </para>
    ///    <para>
    ///      The behavior of reading a zero datetime column using this method is defined by the
    ///      <i>ZeroDateTimeBehavior</i> connection string option.  For more information on this option,
    ///      please refer to <see cref="MySqlConnection.ConnectionString"/>.
    ///    </para>
    ///  </note>
    /// </remarks>
    /// <param name="column">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public DateTime GetDateTime(string column)
    {
      return GetDateTime(GetOrdinal(column));
    }

    /// <summary>
    ///  Gets the value of the specified column as a <see cref="DateTime"/> object.
    /// </summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="DateTime"/> object.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    ///  <note>
    ///    <para>
    ///      MySql allows date columns to contain the value '0000-00-00' and datetime
    ///      columns to contain the value '0000-00-00 00:00:00'.  The DateTime structure cannot contain
    ///      or represent these values.  To read a datetime value from a column that might
    ///      contain zero values, use <see cref="GetMySqlDateTime(int)"/>.
    ///    </para>
    ///    <para>
    ///      The behavior of reading a zero datetime column using this method is defined by the
    ///      <i>ZeroDateTimeBehavior</i> connection string option.  For more information on this option,
    ///      please refer to <see cref="MySqlConnection.ConnectionString"/>.
    ///    </para>
    ///  </note>
    /// </remarks>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
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

    /// <summary>
    ///  Gets the value of the specified column as a <see cref="Decimal"/> object.
    /// </summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="Decimal"/> object.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public Decimal GetDecimal(string column)
    {
      return GetDecimal(GetOrdinal(column));
    }

    /// <summary>
    ///  Gets the value of the specified column as a <see cref="Decimal"/> object.
    /// </summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="Decimal"/> object.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="i">The zero-based column ordinal</param>
    /// <returns>The value of the specified column.</returns>
    public override Decimal GetDecimal(int i)
    {
      IMySqlValue v = GetFieldValue(i, true);
      if (v is MySqlDecimal)
        return ((MySqlDecimal)v).Value;
      return Convert.ToDecimal(v.Value);
    }

    /// <summary>Gets the value of the specified column as a double-precision floating point number.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="double"/> object.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public double GetDouble(string column)
    {
      return GetDouble(GetOrdinal(column));
    }

    /// <summary>Gets the value of the specified column as a double-precision floating point number.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="double"/> object.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override double GetDouble(int i)
    {
      IMySqlValue v = GetFieldValue(i, true);
      if (v is MySqlDouble)
        return ((MySqlDouble)v).Value;
      return Convert.ToDouble(v.Value);
    }

    /// <summary>
    /// Gets the Type that is the data type of the object.
    /// </summary>
    /// <param name="column">The column name.</param>
    /// <returns>The data type of the specified column.</returns>
    public Type GetFieldType(string column)
    {
      return GetFieldType(GetOrdinal(column));
    }

    /// <summary>
    /// Gets the Type that is the data type of the object.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The data type of the specified column.</returns>
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

    /// <summary>
    ///  Gets the value of the specified column as a single-precision floating point number.
    /// </summary>
    /// <remarks>
    ///  <para> No conversions are performed; therefore, the data retrieved must already be a <see cref="float"/> object.</para>
    ///  <para> Call <see cref="IsDBNull"/> to check for null values before calling this method. </para>
    /// </remarks>
    /// <param name="column">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public float GetFloat(string column)
    {
      return GetFloat(GetOrdinal(column));
    }

    /// <summary>
    ///  Gets the value of the specified column as a single-precision floating point number.
    /// </summary>
    /// <remarks>
    ///  <para> No conversions are performed; therefore, the data retrieved must already be a <see cref="float"/> object.</para>
    ///  <para> Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override float GetFloat(int i)
    {
      IMySqlValue v = GetFieldValue(i, true);
      if (v is MySqlSingle)
        return ((MySqlSingle)v).Value;
      return Convert.ToSingle(v.Value);
    }

    /// <summary>
    /// Gets the body definition of a routine.
    /// </summary>
    /// <param name="column">The column name.</param>
    /// <returns>The definition of the routine.</returns>
    public string GetBodyDefinition(string column)
    {
      var value = GetValue(GetOrdinal(column));
      if (value.GetType().FullName.Equals("System.Byte[]"))
      {
        return GetString(column);
      }
      else
      {
        return GetValue(GetOrdinal(column)).ToString();
      }
    }

    /// <summary>
    /// Gets the value of the specified column as a globally-unique identifier(GUID).
    /// </summary>
    /// <param name="column">The name of the column.</param>
    /// <returns>The value of the specified column.</returns>
    public Guid GetGuid(string column)
    {
      return GetGuid(GetOrdinal(column));
    }

    /// <summary>
    /// Gets the value of the specified column as a globally-unique identifier(GUID).
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
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

    /// <summary>Gets the value of the specified column as a 16-bit signed integer.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="Int16"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public Int16 GetInt16(string column)
    {
      return GetInt16(GetOrdinal(column));
    }

    /// <summary>Gets the value of the specified column as a 16-bit signed integer.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="Int16"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override Int16 GetInt16(int i)
    {
      IMySqlValue v = GetFieldValue(i, true);
      if (v is MySqlInt16)
        return ((MySqlInt16)v).Value;

      return (short)ChangeType(v, i, typeof(short));
    }

    /// <summary>Gets the value of the specified column as a 32-bit signed integer.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="Int32"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public Int32 GetInt32(string column)
    {
      return GetInt32(GetOrdinal(column));
    }

    /// <summary>Gets the value of the specified column as a 32-bit signed integer.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="Int32"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override Int32 GetInt32(int i)
    {
      IMySqlValue v = GetFieldValue(i, true);
      if (v is MySqlInt32)
        return ((MySqlInt32)v).Value;

      return (Int32)ChangeType(v, i, typeof(Int32));
    }

    /// <summary>Gets the value of the specified column as a 64-bit signed integer.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="Int64"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public Int64 GetInt64(string column)
    {
      return GetInt64(GetOrdinal(column));
    }

    /// <summary>Gets the value of the specified column as a 64-bit signed integer.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="Int64"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
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
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The name of the specified column.</returns>
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
    /// <param name="name">The name of the column.</param>
    /// <returns>The zero-based column ordinal.</returns>
    public override int GetOrdinal(string name)
    {
      if (!_isOpen || ResultSet == null)
        Throw(new Exception("No current query in data reader"));

      return ResultSet.GetOrdinal(name);
    }

    /// <summary>
    /// Gets a stream to retrieve data from the specified column.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>A stream</returns>
    public override Stream GetStream(int i)
    {
      if (i >= FieldCount)
        Throw(new IndexOutOfRangeException());

      IMySqlValue val = GetFieldValue(i, false);

      if (!(val is MySqlBinary) && !(val is MySqlGuid))
        Throw(new MySqlException("GetStream can only be called on binary or guid columns"));

      byte[] bytes = new byte[0];
      if (val is MySqlBinary)
        bytes = ((MySqlBinary)val).Value;
      else
        bytes = ((MySqlGuid)val).Bytes;

      return new MemoryStream(bytes, false);
    }

    /// <summary>
    ///  Gets the value of the specified column as a <see cref="String"/> object.
    /// </summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="String"/> object.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public string GetString(string column)
    {
      return GetString(GetOrdinal(column));
    }

    /// <summary>
    ///  Gets the value of the specified column as a <see cref="String"/> object.
    /// </summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="String"/> object.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public override String GetString(int i)
    {
      IMySqlValue val = GetFieldValue(i, true);

      if (!_connection.Settings.OldGetStringBehavior)
        return (string)val.Value;
        
        
      if (val is MySqlBinary)
      {
        byte[] v = ((MySqlBinary)val).Value;
        return ResultSet.Fields[i].Encoding.GetString(v, 0, v.Length);
      }

      return val.Value.ToString();
    }

    /// <summary>
    ///  Gets the value of the specified column as a <see cref="TimeSpan"/> object.
    /// </summary>
    /// <remarks>
    ///  <para> No conversions are performed; therefore, the data retrieved must already be a <see cref="TimeSpan"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public TimeSpan GetTimeSpan(string column)
    {
      return GetTimeSpan(GetOrdinal(column));
    }

    /// <summary>
    ///  Gets the value of the specified column as a <see cref="TimeSpan"/> object.
    /// </summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="TimeSpan"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public TimeSpan GetTimeSpan(int column)
    {
      IMySqlValue val = GetFieldValue(column, true);

      MySqlTimeSpan ts = (MySqlTimeSpan)val;
      return ts.Value;
    }

    /// <summary>
    /// Gets the value of the specified column in its native format.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
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
    /// <param name="values">An array of <see cref="Object"/> into which to copy the attribute columns.</param>
    /// <returns>The number of instances of <see cref="Object"/> in the array.</returns>
    public override int GetValues(object[] values)
    {
      int numCols = Math.Min(values.Length, FieldCount);
      for (int i = 0; i < numCols; i++)
        values[i] = GetValue(i);

      return numCols;
    }

    /// <summary>Gets the value of the specified column as a 16-bit unsigned integer.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="UInt16"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public UInt16 GetUInt16(string column)
    {
      return GetUInt16(GetOrdinal(column));
    }

    /// <summary>Gets the value of the specified column as a 16-bit unsigned integer.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="UInt16"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public UInt16 GetUInt16(int column)
    {
      IMySqlValue v = GetFieldValue(column, true);
      if (v is MySqlUInt16)
        return ((MySqlUInt16)v).Value;

      return (UInt16)ChangeType(v, column, typeof(UInt16));
    }

    /// <summary>Gets the value of the specified column as a 32-bit unsigned integer.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="UInt32"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public UInt32 GetUInt32(string column)
    {
      return GetUInt32(GetOrdinal(column));
    }

    /// <summary>Gets the value of the specified column as a 32-bit unsigned integer.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="UInt32"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public UInt32 GetUInt32(int column)
    {
      IMySqlValue v = GetFieldValue(column, true);
      if (v is MySqlUInt32)
        return ((MySqlUInt32)v).Value;
      return (uint)ChangeType(v, column, typeof(UInt32));
    }

    /// <summary>Gets the value of the specified column as a 64-bit unsigned integer.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="UInt64"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The column name.</param>
    /// <returns>The value of the specified column.</returns>
    public UInt64 GetUInt64(string column)
    {
      return GetUInt64(GetOrdinal(column));
    }

    /// <summary>Gets the value of the specified column as a 64-bit unsigned integer.</summary>
    /// <remarks>
    ///  <para>No conversions are performed; therefore, the data retrieved must already be a <see cref="UInt64"/> value.</para>
    ///  <para>Call <see cref="IsDBNull"/> to check for null values before calling this method.</para>
    /// </remarks>
    /// <param name="column">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column.</returns>
    public UInt64 GetUInt64(int column)
    {
      IMySqlValue v = GetFieldValue(column, true);
      if (v is MySqlUInt64)
        return ((MySqlUInt64)v).Value;

      return (UInt64)ChangeType(v, column, typeof(UInt64));
    }

    #endregion

    /// <summary>
    /// Returns a <see cref="DbDataReader"/> object for the requested column ordinal.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>A <see cref="DbDataReader"/> object.</returns>
    IDataReader IDataRecord.GetData(int i)
    {
      return base.GetData(i);
    }

    /// <summary>
    /// Gets a value indicating whether the column contains non-existent or missing values.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>true if the specified column is equivalent to <see cref="DBNull"/>; otherwise false.</returns>
    public override bool IsDBNull(int i)
    {
      return DBNull.Value == GetValue(i);
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
    /// <returns>An <see cref="IEnumerator"/> that can be used to iterate through the rows in the data reader.</returns>
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

    /// <summary>
    /// Gets the value of the specified column as a type.
    /// </summary>
    /// <typeparam name="T">Type.</typeparam>
    /// <param name="ordinal">The index of the column.</param>
    /// <returns>The value of the column.</returns>
    public override T GetFieldValue<T>(int ordinal)
    {
      if (typeof(T).Equals(typeof(Stream)))
        return (T)(object)GetStream(ordinal);
      if (typeof(T).Equals(typeof(sbyte)))
        return (T)(object)GetSByte(ordinal);
      if (typeof(T).Equals(typeof(byte)))
        return (T)(object)GetByte(ordinal);
      if (typeof(T).Equals(typeof(short)))
        return (T)(object)GetInt16(ordinal);
      if (typeof(T).Equals(typeof(ushort)))
        return (T)(object)GetUInt16(ordinal);
      if (typeof(T).Equals(typeof(int)))
        return (T)(object)GetInt32(ordinal);
      if (typeof(T).Equals(typeof(uint)))
        return (T)(object)GetUInt32(ordinal);
      if (typeof(T).Equals(typeof(long)))
        return (T)(object)GetInt64(ordinal);
      if (typeof(T).Equals(typeof(ulong)))
        return (T)(object)GetUInt64(ordinal);
      if (typeof(T).Equals(typeof(char)))
        return (T)(object)GetChar(ordinal);
      if (typeof(T).Equals(typeof(decimal)))
        return (T)(object)GetDecimal(ordinal);
      if (typeof(T).Equals(typeof(float)))
        return (T)(object)GetFloat(ordinal);
      if (typeof(T).Equals(typeof(double)))
        return (T)(object)GetDouble(ordinal);
      if (typeof(T).Equals(typeof(string)))
        return (T)(object)GetString(ordinal);
      if (typeof(T).Equals(typeof(Guid)))
        return (T)(object)GetGuid(ordinal);
      if (typeof(T).Equals(typeof(bool)))
        return (T)(object)GetBoolean(ordinal);
      if (typeof(T).Equals(typeof(DateTime)))
        return (T)(object)GetDateTime(ordinal);
      if (typeof(T).Equals(typeof(TimeSpan)))
        return (T)(object)GetTimeSpan(ordinal);
      if (typeof(T).Equals(typeof(MySqlGeometry)))
        return (T)(object)GetMySqlGeometry(ordinal);
      if (typeof(T).Equals(typeof(DateTimeOffset)))
      {
        var dtValue = new DateTime();
        var result = DateTime.TryParse(this.GetValue(ordinal).ToString(), out dtValue);
        DateTime datetime = result ? dtValue : DateTime.MinValue;
        return (T)Convert.ChangeType(new DateTimeOffset(datetime, TimeSpan.FromHours(0)), typeof(T));
      }

      return base.GetFieldValue<T>(ordinal);
    }

    /// <summary>
    /// Describes the column metadata of the <see cref="MySqlDataReader"/>.
    /// </summary>
    /// <returns>A <see cref="DataTable"/> object.</returns>
    public override DataTable GetSchemaTable()
    {
      // Only Results from SQL SELECT Queries 
      // get a DataTable for schema of the result
      // otherwise, DataTable is null reference
      if (FieldCount == 0) return null;

      DataTable dataTableSchema = new DataTable("SchemaTable");

      dataTableSchema.Columns.Add("ColumnName", typeof(string));
      dataTableSchema.Columns.Add("ColumnOrdinal", typeof(int));
      dataTableSchema.Columns.Add("ColumnSize", typeof(int));
      dataTableSchema.Columns.Add("NumericPrecision", typeof(int));
      dataTableSchema.Columns.Add("NumericScale", typeof(int));
      dataTableSchema.Columns.Add("IsUnique", typeof(bool));
      dataTableSchema.Columns.Add("IsKey", typeof(bool));
      DataColumn dc = dataTableSchema.Columns["IsKey"];
      dc.AllowDBNull = true; // IsKey can have a DBNull
      dataTableSchema.Columns.Add("BaseCatalogName", typeof(string));
      dataTableSchema.Columns.Add("BaseColumnName", typeof(string));
      dataTableSchema.Columns.Add("BaseSchemaName", typeof(string));
      dataTableSchema.Columns.Add("BaseTableName", typeof(string));
      dataTableSchema.Columns.Add("DataType", typeof(Type));
      dataTableSchema.Columns.Add("AllowDBNull", typeof(bool));
      dataTableSchema.Columns.Add("ProviderType", typeof(int));
      dataTableSchema.Columns.Add("IsAliased", typeof(bool));
      dataTableSchema.Columns.Add("IsExpression", typeof(bool));
      dataTableSchema.Columns.Add("IsIdentity", typeof(bool));
      dataTableSchema.Columns.Add("IsAutoIncrement", typeof(bool));
      dataTableSchema.Columns.Add("IsRowVersion", typeof(bool));
      dataTableSchema.Columns.Add("IsHidden", typeof(bool));
      dataTableSchema.Columns.Add("IsLong", typeof(bool));
      dataTableSchema.Columns.Add("IsReadOnly", typeof(bool));

      int ord = 1;
      for (int i = 0; i < FieldCount; i++)
      {
        MySqlField f = ResultSet.Fields[i];
        DataRow r = dataTableSchema.NewRow();
        r["ColumnName"] = f.ColumnName;
        r["ColumnOrdinal"] = ord++;
        r["ColumnSize"] = f.IsTextField ? f.ColumnLength / f.MaxLength : f.ColumnLength;
        int prec = f.Precision;
        int pscale = f.Scale;
        if (prec != -1)
          r["NumericPrecision"] = (short)prec;
        if (pscale != -1)
          r["NumericScale"] = (short)pscale;
        r["DataType"] = GetFieldType(i);
        r["ProviderType"] = (int)f.Type;
        r["IsLong"] = f.IsBlob && (f.ColumnLength > 255 || f.ColumnLength == -1);
        r["AllowDBNull"] = f.AllowsNull;
        r["IsReadOnly"] = false;
        r["IsRowVersion"] = false;
        r["IsUnique"] = false;
        r["IsKey"] = f.IsPrimaryKey;
        r["IsAutoIncrement"] = f.IsAutoIncrement;
        r["BaseSchemaName"] = f.DatabaseName;
        r["BaseCatalogName"] = null;
        r["BaseTableName"] = f.RealTableName;
        r["BaseColumnName"] = f.OriginalColumnName;

        dataTableSchema.Rows.Add(r);
      }

      return dataTableSchema;
    }

    /// <summary>
    /// Advances the data reader to the next result when reading the results of batch SQL statements.
    /// </summary>
    /// <returns><see langword="true"/> if there are more result sets; otherwise <see langword="false"/>.</returns>
    public override bool NextResult() => NextResultAsync(false, CancellationToken.None).GetAwaiter().GetResult();

    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => NextResultAsync(true, cancellationToken);

    internal async Task<bool> NextResultAsync(bool execAsync, CancellationToken cancellationToken)
    {
      if (!_isOpen)
        Throw(new MySqlException(Resources.NextResultIsClosed));

      bool isCaching = Command.CommandType == CommandType.TableDirect && Command.EnableCaching &&
        (CommandBehavior & CommandBehavior.SequentialAccess) == 0;

      // this will clear out any unread data
      if (ResultSet != null)
      {
        await ResultSet.CloseAsync(execAsync).ConfigureAwait(false);
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
            ResultSet = TableCache.RetrieveFromCache(Command.CommandText, Command.CacheAge);

          if (ResultSet == null)
          {
            ResultSet = await driver.NextResultAsync(Statement.StatementId, false, execAsync).ConfigureAwait(false);

            if (ResultSet == null) return false;

            if (ResultSet.IsOutputParameters && Command.CommandType == CommandType.StoredProcedure)
            {
              StoredProcedure sp = Statement as StoredProcedure;
              sp.ProcessOutputParameters(this);
              await ResultSet.CloseAsync(execAsync).ConfigureAwait(false);

              for (int i = 0; i < ResultSet.Fields.Length; i++)
              {
                if (ResultSet.Fields[i].ColumnName.StartsWith("@" + StoredProcedure.ParameterPrefix, StringComparison.OrdinalIgnoreCase))
                {
                  ResultSet = null;
                  break;
                }
              }

              if (!sp.ServerProvidingOutputParameters) return false;
              // if we are using server side output parameters then we will get our ok packet
              // *after* the output parameters resultset
              ResultSet = await driver.NextResultAsync(Statement.StatementId, true, execAsync).ConfigureAwait(false);
            }
            else if (ResultSet.IsOutputParameters && Command.CommandType == CommandType.Text && !Command.IsPrepared && !Command.InternallyCreated)
            {
              Command.ProcessOutputParameters(this);
              await ResultSet.CloseAsync(execAsync).ConfigureAwait(false);

              for (int i = 0; i < ResultSet.Fields.Length; i++)
              {
                if (ResultSet.Fields[i].ColumnName.StartsWith("@" + MySqlCommand.ParameterPrefix, StringComparison.OrdinalIgnoreCase))
                {
                  ResultSet = null;
                  break;
                }
              }

              if (!Statement.ServerProvidingOutputParameters) return false;

              ResultSet = await driver.NextResultAsync(Statement.StatementId, true, execAsync).ConfigureAwait(false);
            }
            ResultSet.Cached = isCaching;
          }

          if (ResultSet.Size == 0)
          {
            if (Command.LastInsertedId == -1) Command.LastInsertedId = ResultSet.InsertedId;
            else {
              if (ResultSet.InsertedId > 0) Command.LastInsertedId = ResultSet.InsertedId;
            }

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
          await _connection.AbortAsync(execAsync, CancellationToken.None).ConfigureAwait(false);
        if (ex.Number == 0)
          throw new MySqlException(Resources.FatalErrorReadingResult, ex);
        if ((CommandBehavior & CommandBehavior.CloseConnection) != 0)
          await CloseAsync(execAsync).ConfigureAwait(false);
        throw;
      }
    }

    /// <summary>
    /// Advances the <see cref="MySqlDataReader"/> to the next record.
    /// </summary>
    /// <returns>true if there are more rows; otherwise false.</returns>
    public override bool Read() => ReadAsync(false, CancellationToken.None).GetAwaiter().GetResult();

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => ReadAsync(true, cancellationToken);

    internal async Task<bool> ReadAsync(bool execAsync, CancellationToken cancellationToken = default)
    {
      if (!_isOpen)
        Throw(new MySqlException("Invalid attempt to Read when reader is closed."));
      if (ResultSet == null)
        return false;

      try
      {
        return await ResultSet.NextRowAsync(CommandBehavior, execAsync).ConfigureAwait(false);
      }
      catch (TimeoutException tex)
      {
        await _connection.HandleTimeoutOrThreadAbortAsync(tex, execAsync, cancellationToken).ConfigureAwait(false);
        throw; // unreached
      }
      catch (ThreadAbortException taex)
      {
        await _connection.HandleTimeoutOrThreadAbortAsync(taex, execAsync, cancellationToken).ConfigureAwait(false);
        throw;
      }
      catch (MySqlException ex)
      {
        if (ex.IsFatal)
          await _connection.AbortAsync(execAsync, cancellationToken).ConfigureAwait(false);

        if (ex.IsQueryAborted)
          throw;

        throw new MySqlException(Resources.FatalErrorDuringRead, ex);
      }
    }

    private async Task ClearKillFlagAsync(bool execAsync)
    {
      // This query will silently crash because of the Kill call that happened before.
      string dummyStatement = "SELECT * FROM bogus_table LIMIT 0"; /* dummy query used to clear kill flag */
      MySqlCommand dummyCommand = new MySqlCommand(dummyStatement, _connection) { InternallyCreated = true };

      try
      {
        await dummyCommand.ExecuteReaderAsync(default, execAsync).ConfigureAwait(false); // ExecuteReader catches the exception and returns null, which is expected.
      }
      catch (MySqlException ex)
      {
        int[] errors = { (int)MySqlErrorCode.NoSuchTable, (int)MySqlErrorCode.TableAccessDenied, (int)MySqlErrorCode.UnknownTable };

        if (Array.IndexOf(errors, (int)ex.Number) < 0)
          throw;
      }
    }

    private void Throw(Exception ex)
    {
      _connection?.Throw(ex);
      throw ex;
    }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="MySqlDataReader"/> class.
    /// </summary>
#if NETFRAMEWORK || NETSTANDARD2_0
    public Task DisposeAsync() => DisposeAsync(true);
#else
    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="MySqlDataReader"/> class.
    /// </summary>
    public override ValueTask DisposeAsync() => DisposeAsync(true);
#endif

    protected override void Dispose(bool disposing)
    {
      try
      {
        if (disposing)
          DisposeAsync(false).GetAwaiter().GetResult();
      }
      finally
      {
        base.Dispose(disposing);
      }
    }

#if NETFRAMEWORK || NETSTANDARD2_0
    internal async Task DisposeAsync(bool execAsync)
#else
    internal async ValueTask DisposeAsync(bool execAsync)
#endif
    {
      await CloseAsync(execAsync).ConfigureAwait(false);
      GC.SuppressFinalize(this);
    }

    #region Destructor
    ~MySqlDataReader()
    {
      Dispose(false);
    }
    #endregion
  }
}
