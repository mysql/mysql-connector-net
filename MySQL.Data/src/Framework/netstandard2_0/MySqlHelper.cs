// Copyright © 2004, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  public partial class MySqlHelper
  {
    #region DataRow
    /// <summary>
    /// Asynchronous version of ExecuteDataRow.
    /// </summary>
    /// <param name="connectionString">The settings to be used for the connection.</param>
    /// <param name="commandText">The command to execute.</param>
    /// <param name="parms">The parameters to use for the command.</param>
    /// <returns>The DataRow containing the first row of the resultset.</returns>
    public static Task<DataRow> ExecuteDataRowAsync(string connectionString, string commandText, params MySqlParameter[] parms)
    {
      return ExecuteDataRowAsync(connectionString, commandText, CancellationToken.None, parms);
    }

    /// <summary>
    /// Asynchronous version of ExecuteDataRow.
    /// </summary>
    /// <param name="connectionString">The settings to be used for the connection.</param>
    /// <param name="commandText">The command to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="parms">The parameters to use for the command.</param>
    /// <returns>The DataRow containing the first row of the resultset.</returns>
    public static Task<DataRow> ExecuteDataRowAsync(string connectionString, string commandText, CancellationToken cancellationToken, params MySqlParameter[] parms)
    {
      var result = new TaskCompletionSource<DataRow>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var row = ExecuteDataRow(connectionString, commandText, parms);
          result.SetResult(row);
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

    #region DataSet

    /// <summary>
    /// Executes a single SQL command and returns the first row of the resultset.  A new MySqlConnection object
    /// is created, opened, and closed during this method.
    /// </summary>
    /// <param name="connectionString">Settings to be used for the connection</param>
    /// <param name="commandText">Command to execute</param>
    /// <param name="parms">Parameters to use for the command</param>
    /// <returns>DataRow containing the first row of the resultset</returns>
    public static DataRow ExecuteDataRow(string connectionString, string commandText, params MySqlParameter[] parms)
    {
      DataSet ds = ExecuteDataset(connectionString, commandText, parms);
      if (ds == null) return null;
      if (ds.Tables.Count == 0) return null;
      if (ds.Tables[0].Rows.Count == 0) return null;
      return ds.Tables[0].Rows[0];
    }

    /// <summary>
    /// Executes a single SQL command and returns the resultset in a <see cref="DataSet"/>.  
    /// A new MySqlConnection object is created, opened, and closed during this method.
    /// </summary>
    /// <param name="connectionString">Settings to be used for the connection</param>
    /// <param name="commandText">Command to execute</param>
    /// <returns><see cref="DataSet"/> containing the resultset</returns>
    public static DataSet ExecuteDataset(string connectionString, string commandText)
    {
      //pass through the call providing null for the set of SqlParameters
      return ExecuteDataset(connectionString, commandText, (MySqlParameter[])null);
    }

    /// <summary>
    /// Executes a single SQL command and returns the resultset in a <see cref="DataSet"/>.  
    /// A new MySqlConnection object is created, opened, and closed during this method.
    /// </summary>
    /// <param name="connectionString">Settings to be used for the connection</param>
    /// <param name="commandText">Command to execute</param>
    /// <param name="commandParameters">Parameters to use for the command</param>
    /// <returns><see cref="DataSet"/> containing the resultset</returns>
    public static DataSet ExecuteDataset(string connectionString, string commandText, params MySqlParameter[] commandParameters)
    {
      //create & open a SqlConnection, and dispose of it after we are done.
      using (MySqlConnection cn = new MySqlConnection(connectionString))
      {
        cn.Open();

        //call the overload that takes a connection in place of the connection string
        return ExecuteDataset(cn, commandText, commandParameters);
      }
    }

    /// <summary>
    /// Executes a single SQL command and returns the resultset in a <see cref="DataSet"/>.  
    /// The state of the <see cref="MySqlConnection"/> object remains unchanged after execution
    /// of this method.
    /// </summary>
    /// <param name="connection"><see cref="MySqlConnection"/> object to use</param>
    /// <param name="commandText">Command to execute</param>
    /// <returns><see cref="DataSet"/> containing the resultset</returns>
    public static DataSet ExecuteDataset(MySqlConnection connection, string commandText)
    {
      //pass through the call providing null for the set of SqlParameters
      return ExecuteDataset(connection, commandText, (MySqlParameter[])null);
    }

    /// <summary>
    /// Executes a single SQL command and returns the resultset in a <see cref="DataSet"/>.  
    /// The state of the <see cref="MySqlConnection"/> object remains unchanged after execution
    /// of this method.
    /// </summary>
    /// <param name="connection"><see cref="MySqlConnection"/> object to use</param>
    /// <param name="commandText">Command to execute</param>
    /// <param name="commandParameters">Parameters to use for the command</param>
    /// <returns><see cref="DataSet"/> containing the resultset</returns>
    public static DataSet ExecuteDataset(MySqlConnection connection, string commandText, params MySqlParameter[] commandParameters)
    {
      //create a command and prepare it for execution
      MySqlCommand cmd = new MySqlCommand();
      cmd.Connection = connection;
      cmd.CommandText = commandText;
      cmd.CommandType = CommandType.Text;

      if (commandParameters != null)
        foreach (MySqlParameter p in commandParameters)
          cmd.Parameters.Add(p);

      //create the DataAdapter & DataSet
      MySqlDataAdapter da = new MySqlDataAdapter(cmd);
      DataSet ds = new DataSet();

      //fill the DataSet using default values for DataTable names, etc.
      da.Fill(ds);

      // detach the MySqlParameters from the command object, so they can be used again.			
      cmd.Parameters.Clear();

      //return the dataset
      return ds;
    }

    /// <summary>
    /// Updates the given table with data from the given <see cref="DataSet"/>
    /// </summary>
    /// <param name="connectionString">Settings to use for the update</param>
    /// <param name="commandText">Command text to use for the update</param>
    /// <param name="ds"><see cref="DataSet"/> containing the new data to use in the update</param>
    /// <param name="tablename">Tablename in the dataset to update</param>
    public static void UpdateDataSet(string connectionString, string commandText, DataSet ds, string tablename)
    {
      MySqlConnection cn = new MySqlConnection(connectionString);
      cn.Open();
      MySqlDataAdapter da = new MySqlDataAdapter(commandText, cn);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      cb.ToString();
      da.Update(ds, tablename);
      cn.Close();
    }

    /// <summary>
    /// Async version of ExecuteDataset
    /// </summary>
    /// <param name="connectionString">Settings to be used for the connection</param>
    /// <param name="commandText">Command to execute</param>
    /// <returns><see cref="DataSet"/> containing the resultset</returns>
    public static Task<DataSet> ExecuteDatasetAsync(string connectionString, string commandText)
    {
      return ExecuteDatasetAsync(connectionString, commandText, CancellationToken.None, (MySqlParameter[])null);
    }

    public static Task<DataSet> ExecuteDatasetAsync(string connectionString, string commandText, CancellationToken cancellationToken)
    {
      return ExecuteDatasetAsync(connectionString, commandText, cancellationToken, (MySqlParameter[])null);
    }

    /// <summary>
    /// Async version of ExecuteDataset
    /// </summary>
    /// <param name="connectionString">Settings to be used for the connection</param>
    /// <param name="commandText">Command to execute</param>
    /// <param name="commandParameters">Parameters to use for the command</param>
    /// <returns><see cref="DataSet"/> containing the resultset</returns>
    public static Task<DataSet> ExecuteDatasetAsync(string connectionString, string commandText, params MySqlParameter[] commandParameters)
    {
      return ExecuteDatasetAsync(connectionString, commandText, CancellationToken.None, commandParameters);
    }

    public static Task<DataSet> ExecuteDatasetAsync(string connectionString, string commandText, CancellationToken cancellationToken, params MySqlParameter[] commandParameters)
    {
      var result = new TaskCompletionSource<DataSet>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var dataset = ExecuteDataset(connectionString, commandText, commandParameters);
          result.SetResult(dataset);
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

    /// <summary>
    /// Async version of ExecuteDataset
    /// </summary>
    /// <param name="connection"><see cref="MySqlConnection"/> object to use</param>
    /// <param name="commandText">Command to execute</param>
    /// <returns><see cref="DataSet"/> containing the resultset</returns>
    public static Task<DataSet> ExecuteDatasetAsync(MySqlConnection connection, string commandText)
    {
      return ExecuteDatasetAsync(connection, commandText, CancellationToken.None, (MySqlParameter[])null);
    }

    public static Task<DataSet> ExecuteDatasetAsync(MySqlConnection connection, string commandText, CancellationToken cancellationToken)
    {
      return ExecuteDatasetAsync(connection, commandText, cancellationToken, (MySqlParameter[])null);
    }

    /// <summary>
    /// Async version of ExecuteDataset
    /// </summary>
    /// <param name="connection"><see cref="MySqlConnection"/> object to use</param>
    /// <param name="commandText">Command to execute</param>
    /// <param name="commandParameters">Parameters to use for the command</param>
    /// <returns><see cref="DataSet"/> containing the resultset</returns>
    public static Task<DataSet> ExecuteDatasetAsync(MySqlConnection connection, string commandText, params MySqlParameter[] commandParameters)
    {
      return ExecuteDatasetAsync(connection, commandText, CancellationToken.None, commandParameters);
    }

    public static Task<DataSet> ExecuteDatasetAsync(MySqlConnection connection, string commandText, CancellationToken cancellationToken, params MySqlParameter[] commandParameters)
    {
      var result = new TaskCompletionSource<DataSet>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var dataset = ExecuteDataset(connection, commandText, commandParameters);
          result.SetResult(dataset);
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

    /// <summary>
    /// Async version of UpdateDataset
    /// </summary>
    /// <param name="connectionString">Settings to use for the update</param>
    /// <param name="commandText">Command text to use for the update</param>
    /// <param name="ds"><see cref="DataSet"/> containing the new data to use in the update</param>
    /// <param name="tablename">Tablename in the dataset to update</param>
    public static Task UpdateDataSetAsync(string connectionString, string commandText, DataSet ds, string tablename)
    {
      return UpdateDataSetAsync(connectionString, commandText, ds, tablename, CancellationToken.None);
    }

    public static Task UpdateDataSetAsync(string connectionString, string commandText, DataSet ds, string tablename, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<bool>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          UpdateDataSet(connectionString, commandText, ds, tablename);
          result.SetResult(true);
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

  }
}

