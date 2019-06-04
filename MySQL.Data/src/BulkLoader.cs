// Copyright © 2006, 2019, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Allows importing large amounts of data into a database with bulk loading.
  /// </summary>
  public class MySqlBulkLoader
  {
    // constant values
    private const string defaultFieldTerminator = "\t";
    private const string defaultLineTerminator = "\n";
    private const char defaultEscapeCharacter = '\\';

    // fields

    public MySqlBulkLoader(MySqlConnection connection)
    {
      Connection = connection;
      Local = false;
      FieldTerminator = defaultFieldTerminator;
      LineTerminator = defaultLineTerminator;
      FieldQuotationCharacter = Char.MinValue;
      ConflictOption = MySqlBulkLoaderConflictOption.None;
      Columns = new List<string>();
      Expressions = new List<string>();
    }

#region Properties

    /// <summary>
    /// Gets or sets the connection.
    /// </summary>
    /// <value>The connection.</value>
    public MySqlConnection Connection { get; set; }

    /// <summary>
    /// Gets or sets the field terminator.
    /// </summary>
    /// <value>The field terminator.</value>
    public string FieldTerminator { get; set; }

    /// <summary>
    /// Gets or sets the line terminator.
    /// </summary>
    /// <value>The line terminator.</value>
    public string LineTerminator { get; set; }

    /// <summary>
    /// Gets or sets the name of the table.
    /// </summary>
    /// <value>The name of the table.</value>
    public string TableName { get; set; }

    /// <summary>
    /// Gets or sets the character set.
    /// </summary>
    /// <value>The character set.</value>
    public string CharacterSet { get; set; }

    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    /// <value>The name of the file.</value>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the timeout.
    /// </summary>
    /// <value>The timeout.</value>
    public int Timeout { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the file name that is to be loaded
    /// is local to the client or not. The default value is false.
    /// </summary>
    /// <value><c>true</c> if local; otherwise, <c>false</c>.</value>
    public bool Local { get; set; }

    /// <summary>
    /// Gets or sets the number of lines to skip.
    /// </summary>
    /// <value>The number of lines to skip.</value>
    public int NumberOfLinesToSkip { get; set; }

    /// <summary>
    /// Gets or sets the line prefix.
    /// </summary>
    /// <value>The line prefix.</value>
    public string LinePrefix { get; set; }

    /// <summary>
    /// Gets or sets the field quotation character.
    /// </summary>
    /// <value>The field quotation character.</value>
    public char FieldQuotationCharacter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether [field quotation optional].
    /// </summary>
    /// <value>
    /// 	<c>true</c> if [field quotation optional]; otherwise, <c>false</c>.
    /// </value>
    public bool FieldQuotationOptional { get; set; }

    /// <summary>
    /// Gets or sets the escape character.
    /// </summary>
    /// <value>The escape character.</value>
    public char EscapeCharacter { get; set; }

    /// <summary>
    /// Gets or sets the conflict option.
    /// </summary>
    /// <value>The conflict option.</value>
    public MySqlBulkLoaderConflictOption ConflictOption { get; set; }

    /// <summary>
    /// Gets or sets the priority.
    /// </summary>
    /// <value>The priority.</value>
    public MySqlBulkLoaderPriority Priority { get; set; }

    /// <summary>
    /// Gets the columns.
    /// </summary>
    /// <value>The columns.</value>
    public List<string> Columns { get; }

    /// <summary>
    /// Gets the expressions.
    /// </summary>
    /// <value>The expressions.</value>
    public List<string> Expressions { get; }

#endregion

    /// <summary>
    /// Executes the load operation.
    /// </summary>
    /// <returns>The number of rows inserted.</returns>
    public int Load()
    {
      bool openedConnection = false;

      if (Connection == null)
        throw new InvalidOperationException(Resources.ConnectionNotSet);

      // next we open up the connetion if it is not already open
      if (Connection.State != ConnectionState.Open)
      {
        openedConnection = true;
        Connection.Open();
      }

      try
      {
        string sql = BuildSqlCommand();
        MySqlCommand cmd = new MySqlCommand(sql, Connection) {CommandTimeout = Timeout};
        return cmd.ExecuteNonQuery();
      }
      finally
      {
        if (openedConnection)
          Connection.Close();
      }
    }

#region Async
    /// <summary>
    /// Asynchronous version of the load operation.
    /// </summary>
    /// <returns>The number of rows inserted.</returns>
    public Task<int> LoadAsync()
    {
      return LoadAsync(CancellationToken.None);
    }

    /// <summary>
    /// Executes the load operation asynchronously while the cancellation isn't requested.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows inserted.</returns>
    public Task<int> LoadAsync(CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          int loadResult = Load();
          result.SetResult(loadResult);
        }
        catch (Exception ex)
        {
          result.SetException(ex);
        }
      }
      else
      {
        result.SetCanceled();
      }
      return result.Task;
    }
#endregion

    private string BuildSqlCommand()
    {
      StringBuilder sql = new StringBuilder("LOAD DATA ");
      if (Priority == MySqlBulkLoaderPriority.Low)
        sql.Append("LOW_PRIORITY ");
      else if (Priority == MySqlBulkLoaderPriority.Concurrent)
        sql.Append("CONCURRENT ");

      if (Local)
        sql.Append("LOCAL ");
      sql.Append("INFILE ");
      if (Path.DirectorySeparatorChar == '\\')
        sql.AppendFormat("'{0}' ", FileName.Replace(@"\", @"\\"));
      else
        sql.AppendFormat("'{0}' ", FileName);

      if (ConflictOption == MySqlBulkLoaderConflictOption.Ignore)
        sql.Append("IGNORE ");
      else if (ConflictOption == MySqlBulkLoaderConflictOption.Replace)
        sql.Append("REPLACE ");

      sql.AppendFormat("INTO TABLE {0} ", TableName);

      if (CharacterSet != null)
        sql.AppendFormat("CHARACTER SET {0} ", CharacterSet);

      StringBuilder optionSql = new StringBuilder(String.Empty);
      if (FieldTerminator != defaultFieldTerminator)
        optionSql.AppendFormat("TERMINATED BY '{0}' ", FieldTerminator);
      if (FieldQuotationCharacter != Char.MinValue)
        optionSql.AppendFormat("{0} ENCLOSED BY '{1}' ",
            FieldQuotationOptional ? "OPTIONALLY" : "", FieldQuotationCharacter);
      if (EscapeCharacter != defaultEscapeCharacter &&
          EscapeCharacter != Char.MinValue)
        optionSql.AppendFormat("ESCAPED BY '{0}' ", EscapeCharacter);
      if (optionSql.Length > 0)
        sql.AppendFormat("FIELDS {0}", optionSql.ToString());

      optionSql = new StringBuilder(String.Empty);
      if (!string.IsNullOrEmpty(LinePrefix))
        optionSql.AppendFormat("STARTING BY '{0}' ", LinePrefix);
      if (LineTerminator != defaultLineTerminator)
        optionSql.AppendFormat("TERMINATED BY '{0}' ", LineTerminator);
      if (optionSql.Length > 0)
        sql.AppendFormat("LINES {0}", optionSql.ToString());

      if (NumberOfLinesToSkip > 0)
        sql.AppendFormat("IGNORE {0} LINES ", NumberOfLinesToSkip);

      if (Columns.Count > 0)
      {
        sql.Append("(");
        sql.Append(Columns[0]);
        for (int i = 1; i < Columns.Count; i++)
          sql.AppendFormat(",{0}", Columns[i]);
        sql.Append(") ");
      }

      if (Expressions.Count > 0)
      {
        sql.Append("SET ");
        sql.Append(Expressions[0]);
        for (int i = 1; i < Expressions.Count; i++)
          sql.AppendFormat(",{0}", Expressions[i]);
      }

      return sql.ToString();
    }
  }

  /// <summary>
  /// Represents the priority set for bulk loading operations.
  /// </summary>
  public enum MySqlBulkLoaderPriority
  {
    /// <summary>
    /// This is the default and indicates normal priority
    /// </summary>
    None,
    /// <summary>
    /// Low priority will cause the load operation to wait until all readers of the table
    /// have finished.  This only affects storage engines that use only table-level locking
    /// such as MyISAM, Memory, and Merge.
    /// </summary>
    Low,
    /// <summary>
    /// Concurrent priority is only relevant for MyISAM tables and signals that if the table
    /// has no free blocks in the middle that other readers can retrieve data from the table
    /// while the load operation is happening.
    /// </summary>
    Concurrent
  }

  /// <summary>
  /// Represents the behavior when conflicts arise during bulk loading operations.
  /// </summary>
  public enum MySqlBulkLoaderConflictOption
  {
    /// <summary>
    /// This is the default and indicates normal operation.  In the event of a LOCAL load, this
    /// is the same as ignore.  When the data file is on the server, then a key conflict will
    /// cause an error to be thrown and the rest of the data file ignored.
    /// </summary>
    None,
    /// <summary>
    /// Replace column values when a key conflict occurs.
    /// </summary>
    Replace,
    /// <summary>
    /// Ignore any rows where the primary key conflicts.
    /// </summary>
    Ignore
  }

}
