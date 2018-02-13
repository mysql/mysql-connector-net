// Copyright © 2014, Oracle and/or its affiliates. All rights reserved.
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
using System.Linq;
using MySql.Data.MySqlClient;
using System.Data;

namespace MySql.Web.Security
{
  /// <summary>
  /// Perform basic operations against a Database
  /// </summary>
  internal class MySqlDatabaseWrapper : IDisposable
  {
    private MySqlConnection _conn;
    #region Public
    /// <summary>
    /// Initialize a new instance of the class
    /// </summary>
    /// <param name="connectionString">Connection String</param>
    public MySqlDatabaseWrapper(string connectionString)
    {
      _conn = new MySqlConnection(connectionString);
    }
    ~MySqlDatabaseWrapper()
    {
      this.Dispose(true);
    }
    /// <summary>
    /// Close the current instance
    /// </summary>
    public void Close()
    {
      this.Dispose();
    }
    /// <summary>
    /// Dispose the current instance
    /// </summary>
    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Execute given query on the database
    /// </summary>
    /// <param name="cmdText">Query to exeute</param>
    /// <param name="parametersValues">Parameters used in the query</param>
    /// <returns>Query resultset</returns>
    public IEnumerable<DataRow> ExecuteQuery(string cmdText, params object[] parametersValues)
    {
      CheckIsConnectionOpen();
      MySqlCommand cmd = _conn.CreateCommand();
      cmd.CommandText = cmdText;
      AddParameters(cmd, parametersValues);
      DataTable result = new DataTable();
      MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
      adapter.Fill(result);
      foreach (DataRow row in result.Rows)
      {
        yield return row;
      }
    }

    /// <summary>
    /// Execute given query on the database
    /// </summary>
    /// <param name="cmdText">Query to exeute</param>
    /// <param name="parametersValues">Parameters used in the query</param>
    /// <returns>First record in the Query resultset</returns>
    public DataRow ExecuteQuerySingleRecord(string cmdText, params object[] parametersValues)
    {
      return ExecuteQuery(cmdText, parametersValues).FirstOrDefault<DataRow>();
    }

    /// <summary>
    /// Execute given query on the database
    /// </summary>
    /// <param name="cmdText">Query to exeute</param>
    /// <param name="parametersValues">Parameters used in the query</param>
    /// <returns>Rows affected by the query</returns>
    public int ExecuteNonQuery(string cmdText, params object[] parametersValues)
    {
      CheckIsConnectionOpen();
      MySqlCommand cmd = _conn.CreateCommand();
      cmd.CommandText = cmdText;
      AddParameters(cmd, parametersValues);
      return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Execute given query on the database
    /// </summary>
    /// <param name="cmdText">Query to exeute</param>
    /// <param name="parametersValues">Parameters used in the query</param>
    /// <returns>Value of the first column in the first row in the query resulset</returns>
    public object ExecuteScalar(string cmdText, params object[] parametersValues)
    {
      CheckIsConnectionOpen();
      MySqlCommand cmd = _conn.CreateCommand();
      cmd.CommandText = cmdText;
      AddParameters(cmd, parametersValues);
      return cmd.ExecuteScalar();
    }

    /// <summary>
    /// Execute all given queries on the database inside of a transaction
    /// </summary>
    /// <param name="commands">Queries to exeute</param>
    /// <returns>If queries were successfully executed</returns>
    public bool ExecuteInTransaction(IEnumerable<Tuple<string, object[]>> commands)
    {
      CheckIsConnectionOpen();
      MySqlTransaction tran = _conn.BeginTransaction();
      try
      {
        foreach (var command in commands)
        {
          MySqlCommand cmd = _conn.CreateCommand();
          cmd.CommandText = command.Item1;
          AddParameters(cmd, command.Item2);
          cmd.ExecuteNonQuery();
        }
        tran.Commit();
        return true;
      }
      catch (Exception)
      {
        tran.Rollback();
        return false;
      }
    }
    #endregion

    #region Protected
    protected virtual void Dispose(bool disposing)
    {
      if (disposing && (this._conn != null))
      {
        if (_conn.State != ConnectionState.Closed)
          _conn.Close();

        _conn = null;
      }
    }
    #endregion

    #region Private
    /// <summary>
    /// Verifies if the current connection is open, if not is opened
    /// </summary>
    private void CheckIsConnectionOpen()
    {
      if (this._conn.State != ConnectionState.Open)
      {
        this._conn.Open();
      }
    }

    /// <summary>
    /// Add parameters to a command, nomenclature name used for the parameters are 'param[n]'
    /// </summary>
    /// <param name="cmd">Command that will stores the parameters</param>
    /// <param name="values">Parameters values</param>
    private void AddParameters(MySqlCommand cmd, object[] values)
    {
      int ctr = 1;
      foreach (object value in values)
      {
        cmd.Parameters.Add(new MySqlParameter()
        {
          ParameterName = string.Format("param{0}", ctr),
          Value = value ?? DBNull.Value
        });
        ctr++;
      }
    }
    #endregion
  }
}
