// Copyright (c) 2004, 2023, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an
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
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  ///  Represents a set of data commands and a database connection that are used to fill a dataset and update a MySQL database. 
  ///  This class cannot be inherited.
  /// </summary>
  /// <remarks>
  ///  <para>
  ///    The <see cref="MySqlDataAdapter"/>, serves as a bridge between a <see cref="DataSet"/>
  ///    and MySQL for retrieving and saving data. The <see cref="MySqlDataAdapter"/> provides this
  ///    bridge by mapping <see cref="DbDataAdapter.Fill(DataSet)"/>, which changes the data in the
  ///    <see cref="DataSet"/> to match the data in the data source, and <see cref="DbDataAdapter.Update(DataSet)"/>,
  ///    which changes the data in the data source to match the data in the <see cref="DataSet"/>,
  ///    using the appropriate SQL statements against the data source.
  ///  </para>
  ///  <para>
  ///    When the <see cref="MySqlDataAdapter"/> fills a <see cref="DataSet"/>, it will create the necessary
  ///    tables and columns for the returned data if they do not already exist. However, primary
  ///    key information will not be included in the implicitly created schema unless the
  ///    <see cref="MissingSchemaAction"/> property is set to <see cref="MissingSchemaAction.AddWithKey"/>.
  ///    You may also have the <see cref="MySqlDataAdapter"/> create the schema of the <see cref="DataSet"/>,
  ///    including primary key information, before filling it with data using <see cref="DbDataAdapter.FillSchema(DataTable, SchemaType)"/>.
  ///  </para>
  ///  <para>
  ///    <see cref="MySqlDataAdapter"/> is used in conjunction with <see cref="MySqlConnection"/>
  ///    and <see cref="MySqlCommand"/> to increase performance when connecting to a MySQL database.
  ///  </para>
  ///  <para>
  ///    The <see cref="MySqlDataAdapter"/> also includes the <see cref="MySqlDataAdapter.SelectCommand"/>,
  ///    <see cref="MySqlDataAdapter.InsertCommand"/>, <see cref="MySqlDataAdapter.DeleteCommand"/>,
  ///    <see cref="MySqlDataAdapter.UpdateCommand"/>, and <see cref="DataAdapter.TableMappings"/>
  ///    properties to facilitate the loading and updating of data.
  ///  </para>
  ///  <para>
  ///    When an instance of <see cref="MySqlDataAdapter"/> is created, the read/write properties
  ///    are set to initial values. For a list of these values, see the <see cref="MySqlDataAdapter"/>
  ///    constructor.
  ///  </para>
  ///  <note>
  ///    Please be aware that the <see cref="DataColumn"/> class allows only
  ///    Int16, Int32, and Int64 to have the AutoIncrement property set.
  ///    If you plan to use autoincremement columns with MySQL, you should consider
  ///    using signed integer columns.
  ///  </note>
  /// </remarks>
  /// <example>
  ///  The following example creates a <see cref="MySqlCommand"/> and a <see cref="MySqlConnection"/>.
  ///  The <see cref="MySqlConnection"/> is opened and set as the <see cref="MySqlCommand.Connection"/> for the
  ///  <see cref="MySqlCommand"/>. The example then calls <see cref="MySqlCommand.ExecuteNonQuery"/>, and closes
  ///  the connection. To accomplish this, the <see cref="MySqlCommand.ExecuteNonQuery"/> is
  ///  passed a connection string and a query string that is a SQL INSERT
  ///  statement.
  ///  <code lang="C#">
  ///    public DataSet SelectRows(DataSet dataset,string connection,string query)
  ///    {
  ///      MySqlConnection conn = new MySqlConnection(connection);
  ///      MySqlDataAdapter adapter = new MySqlDataAdapter();
  ///      adapter.SelectCommand = new MySqlCommand(query, conn);
  ///      adapter.Fill(dataset);
  ///      return dataset;
  ///    }
  ///  </code>
  /// </example>
#if NET452
  [ToolboxBitmap(typeof(MySqlDataAdapter), "MySqlClient.resources.dataadapter.bmp")]
#endif
  [DesignerCategory("Code")]
  [Designer("MySql.Data.MySqlClient.Design.MySqlDataAdapterDesigner,MySqlClient.Design")]
  public sealed class MySqlDataAdapter : DbDataAdapter, IDbDataAdapter, IDataAdapter
  {
    private bool loadingDefaults;
    private int updateBatchSize;
    List<IDbCommand> commandBatch;

    /// <summary>
    /// Occurs during Update before a command is executed against the data source. The attempt to update is made, so the event fires.
    /// </summary>
    public event MySqlRowUpdatingEventHandler RowUpdating;

    /// <summary>
    /// Occurs during Update after a command is executed against the data source. The attempt to update is made, so the event fires.
    /// </summary>
    public event MySqlRowUpdatedEventHandler RowUpdated;

    /// <summary>
    ///  Initializes a new instance of the <see cref="MySqlDataAdapter"/> class.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///    When an instance of <see cref="MySqlDataAdapter"/> is created,
    ///    the following read/write properties are set to the following initial
    ///    values.
    ///  </para>
    ///  <list type="table">
    ///    <listheader>
    ///      <term>Properties</term>
    ///      <term>Initial Value</term>
    ///    </listheader>
    ///    <item>
    ///      <term>
    ///        <see cref="MissingMappingAction"/>
    ///      </term>
    ///      <term>
    ///        <see cref="MissingMappingAction.Passthrough"/>
    ///      </term>
    ///    </item>
    ///    <item>
    ///      <term>
    ///        <see cref="MissingSchemaAction"/>
    ///      </term>
    ///      <term>
    ///        <see cref="MissingSchemaAction.Add"/>
    ///      </term>
    ///    </item>
    ///  </list>
    ///  <para>
    ///    You can change the value of any of these properties through a separate call to the property.
    ///  </para>
    /// </remarks>
    public MySqlDataAdapter()
    {
      loadingDefaults = true;
      updateBatchSize = 1;
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="MySqlDataAdapter"/> class with
    ///  the specified <see cref="MySqlCommand"/> as the <see cref="SelectCommand"/>
    ///  property.
    /// </summary>
    /// <param name="selectCommand">
    ///  <see cref="MySqlCommand"/> that is a SQL SELECT statement or stored procedure and is set
    ///  as the <see cref="SelectCommand"/> property of the <see cref="MySqlDataAdapter"/>.
    /// </param>
    public MySqlDataAdapter(MySqlCommand selectCommand)
      : this()
    {
      SelectCommand = selectCommand;
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="MySqlDataAdapter"/> class with
    ///  a <see cref="SelectCommand"/> and a <see cref="MySqlConnection"/> object.
    /// </summary>
    /// <param name="selectCommandText">
    ///  A <b>String</b> that is a SQL SELECT statement or stored procedure to be used by
    ///  the <see cref="SelectCommand"/> property of the <see cref="MySqlDataAdapter"/>.
    /// </param>
    /// <param name="connection">
    ///  A <see cref="MySqlConnection"/> that represents the connection.
    /// </param>
    /// <remarks>
    ///  <para>
    ///    This implementation of the <see cref="MySqlDataAdapter"/> opens and closes a <see cref="MySqlConnection"/>
    ///    if it is not already open. This can be useful in a an application that must call the
    ///    <see cref="DbDataAdapter.Fill(DataSet)"/> method for two or more <B>MySqlDataAdapter</B> objects.
    ///    If the <B>MySqlConnection</B> is already open, you must explicitly call
    ///    <see cref="MySqlConnection.Close"/> or <see cref="MySqlConnection.Dispose()"/> to close it.
    ///  </para>
    /// </remarks>
    public MySqlDataAdapter(string selectCommandText, MySqlConnection connection)
      : this()
    {
      SelectCommand = new MySqlCommand(selectCommandText, connection);
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="MySqlDataAdapter"/> class with
    ///  a <see cref="SelectCommand"/> and a connection string.
    /// </summary>
    /// <param name="selectCommandText">
    ///  A <see cref="string"/> that is a SQL SELECT statement or stored procedure to
    ///  be used by the <see cref="SelectCommand"/> property of the <see cref="MySqlDataAdapter"/>.
    /// </param>
    /// <param name="selectConnString">The connection string</param>
    public MySqlDataAdapter(string selectCommandText, string selectConnString)
      : this()
    {
      SelectCommand = new MySqlCommand(selectCommandText,
        new MySqlConnection(selectConnString));
    }

    #region Properties

    /// <summary>
    ///  Gets or sets a SQL statement or stored procedure used to delete records from the data set.
    /// </summary>
    /// <value>
    ///  A <see cref="MySqlCommand"/> used during <see cref="DbDataAdapter.Update(DataSet)"/> to delete records in the
    ///  database that correspond to deleted rows in the <see cref="DataSet"/>.
    /// </value>
    /// <remarks>
    ///  <para>
    ///    During <see cref="DbDataAdapter.Update(DataSet)"/>, if this property is not set and primary key information
    ///    is present in the <see cref="DataSet"/>, the <see cref="DeleteCommand"/> can be generated
    ///    automatically if you set the <see cref="SelectCommand"/> property and use the
    ///    <see cref="MySqlCommandBuilder"/>. Then, any additional commands that you do not set are
    ///    generated by the <see cref="MySqlCommandBuilder"/>. This generation logic requires key column
    ///    information to be present in the <see cref="DataSet"/>.
    ///  </para>
    ///  <para>
    ///    When <see cref="DeleteCommand"/> is assigned to a previously created <see cref="MySqlCommand"/>,
    ///    the <see cref="MySqlCommand"/> is not cloned. The <see cref="DeleteCommand"/> maintains a reference
    ///    to the previously created <see cref="MySqlCommand"/> object.
    ///  </para>
    /// </remarks>
    [Description("Used during Update for deleted rows in Dataset.")]
    public new MySqlCommand DeleteCommand
    {
      get { return (MySqlCommand)base.DeleteCommand; }
      set { base.DeleteCommand = value; }
    }

    /// <summary>
    ///  Gets or sets a SQL statement or stored procedure used to insert records into the data set.
    /// </summary>
    /// <value>
    ///  A <see cref="MySqlCommand"/> used during <see cref="DbDataAdapter.Update(System.Data.DataSet)"/> to insert records into the
    ///  database that correspond to new rows in the <see cref="DataSet"/>.
    /// </value>
    /// <remarks>
    ///  <para>
    ///    During <see cref="DbDataAdapter.Update(DataSet)"/>, if this property is not set and primary key information
    ///    is present in the <see cref="DataSet"/>, the <B>InsertCommand</B> can be generated
    ///    automatically if you set the <see cref="SelectCommand"/> property and use the
    ///    <see cref="MySqlCommandBuilder"/>. Then, any additional commands that you do not set are
    ///    generated by the <B>MySqlCommandBuilder</B>. This generation logic requires key column
    ///    information to be present in the <B>DataSet</B>.
    ///  </para>
    ///  <para>
    ///    When <B>InsertCommand</B> is assigned to a previously created <see cref="MySqlCommand"/>,
    ///    the <see cref="MySqlCommand"/> is not cloned. The <B>InsertCommand</B> maintains a reference
    ///    to the previously created <see cref="MySqlCommand"/> object.
    ///  </para>
    ///  <note>
    ///    If execution of this command returns rows, these rows may be added to the <B>DataSet</B>
    ///    depending on how you set the <see cref="MySqlCommand.UpdatedRowSource"/> property of the <see cref="MySqlCommand"/> object.
    ///  </note>
    ///</remarks>
    [Description("Used during Update for new rows in Dataset.")]
    public new MySqlCommand InsertCommand
    {
      get { return (MySqlCommand)base.InsertCommand; }
      set { base.InsertCommand = value; }
    }

    /// <summary>
    ///  Gets or sets a SQL statement or stored procedure used to select records in the data source.
    /// </summary>
    /// <value>
    ///  A <see cref="MySqlCommand"/> used during <see cref="DbDataAdapter.Fill(DataSet)"/> to select records from the
    ///  database for placement in the <see cref="DataSet"/>.
    /// </value>
    /// <remarks>
    ///  <para>
    ///    When <see cref="SelectCommand"/> is assigned to a previously created <see cref="MySqlCommand"/>,
    ///    the <see cref="MySqlCommand"/> is not cloned. The <see cref="SelectCommand"/> maintains a reference to the
    ///    previously created <see cref="MySqlCommand"/> object.
    ///  </para>
    ///  <para>
    ///    If the <see cref="SelectCommand"/> does not return any rows, no tables are added to the
    ///    <see cref="DataSet"/>, and no exception is raised.
    ///  </para>
    /// </remarks>
    [Description("Used during Fill/FillSchema")]
    [Category("Fill")]
    public new MySqlCommand SelectCommand
    {
      get { return (MySqlCommand)base.SelectCommand; }
      set { base.SelectCommand = value; }
    }

    /// <summary>
    ///  Gets or sets a SQL statement or stored procedure used to updated records in the data source.
    /// </summary>
    /// <value>
    ///  A <see cref="MySqlCommand"/> used during <see cref="DbDataAdapter.Update(DataSet)"/> to update records in the
    ///  database with data from the <see cref="DataSet"/>.
    /// </value>
    /// <remarks>
    ///  <para>
    ///    During <see cref="DbDataAdapter.Update(DataSet)"/>, if this property is not set and primary key information
    ///    is present in the <see cref="DataSet"/>, the <see cref="UpdateCommand"/> can be generated
    ///    automatically if you set the <see cref="SelectCommand"/> property and use the
    ///    <see cref="MySqlCommandBuilder"/>. Then, any additional commands that you do not set are
    ///    generated by the <see cref="MySqlCommandBuilder"/>. This generation logic requires key column
    ///    information to be present in the <B>DataSet</B>.
    ///  </para>
    ///  <para>
    ///    When <see cref="UpdateCommand"/> is assigned to a previously created <see cref="MySqlCommand"/>,
    ///    the <see cref="MySqlCommand"/> is not cloned. The <see cref="UpdateCommand"/> maintains a reference
    ///    to the previously created <see cref="MySqlCommand"/> object.
    ///  </para>
    ///  <note>
    ///    If execution of this command returns rows, these rows may be merged with the DataSet
    ///    depending on how you set the <see cref="MySqlCommand.UpdatedRowSource"/> property of the <see cref="MySqlCommand"/> object.
    ///  </note>
    /// </remarks>
    [Description("Used during Update for modified rows in Dataset.")]
    public new MySqlCommand UpdateCommand
    {
      get { return (MySqlCommand)base.UpdateCommand; }
      set { base.UpdateCommand = value; }
    }

    internal bool LoadDefaults
    {
      get { return loadingDefaults; }
      set { loadingDefaults = value; }
    }

    #endregion

    /// <summary>
    /// Open connection if it was closed.
    /// Necessary to workaround "connection must be open and valid" error
    /// with batched updates.
    /// </summary>
    /// <param name="state">Row state</param>
    /// <param name="openedConnections"> list of opened connections 
    /// If connection is opened by this function, the list is updated
    /// </param>
    /// <returns>true if connection was opened</returns>
    private void OpenConnectionIfClosed(DataRowState state,
      List<MySqlConnection> openedConnections)
    {
      MySqlCommand cmd = null;
      switch (state)
      {
        case DataRowState.Added:
          cmd = InsertCommand;
          break;
        case DataRowState.Deleted:
          cmd = DeleteCommand;
          break;
        case DataRowState.Modified:
          cmd = UpdateCommand;
          break;
        default:
          return;
      }

      if (cmd != null && cmd.Connection != null &&
        cmd.Connection.connectionState == ConnectionState.Closed)
      {
        cmd.Connection.Open();
        openedConnections.Add(cmd.Connection);
      }
    }

    protected override int Update(DataRow[] dataRows, DataTableMapping tableMapping)
    {

      List<MySqlConnection> connectionsOpened = new List<MySqlConnection>();

      try
      {
        // Open connections for insert/update/update commands, if 
        // connections are closed.
        foreach (DataRow row in dataRows)
        {
          OpenConnectionIfClosed(row.RowState, connectionsOpened);
        }

        int ret = base.Update(dataRows, tableMapping);

        return ret;
      }
      finally
      {
        foreach (MySqlConnection c in connectionsOpened)
          c.Close();
      }
    }

    #region Batching Support
    /// <summary>
    /// Gets or sets a value that enables or disables batch processing support, 
    /// and specifies the number of commands that can be executed in a batch.
    /// </summary>
    /// <remarks>
    /// Returns the number of rows to process for each batch.
    /// <list type="table">
    ///    <listheader>
    ///      <term>Value is</term>
    ///      <term>Effect</term>
    ///    </listheader>
    ///    <item>
    ///      <term>
    ///        0
    ///      </term>
    ///      <term>
    ///        There is no limit on the batch size.
    ///      </term>
    ///    </item>
    ///    <item>
    ///      <term>
    ///        1
    ///      </term>
    ///      <term>
    ///        Disables batch updating.
    ///      </term>
    ///    </item>
    ///    <item>
    ///      <term>
    ///        > 1
    ///      </term>
    ///      <term>
    ///        Changes are sent using batches of <see cref="UpdateBatchSize"/> operations at a time.
    ///      </term>
    ///    </item>
    /// </list>
    /// <para>
    ///  When setting this to a value other than 1, all the commands associated with the <see cref="MySqlDataAdapter"/>
    /// must have their <see cref="UpdateRowSource"/> property set to None or OutputParameters. An exception will be thrown otherwise.
    /// </para>
    /// </remarks>
    public override int UpdateBatchSize
    {
      get { return updateBatchSize; }
      set { updateBatchSize = value; }
    }

    /// <summary>
    /// Initializes batching for the <see cref="MySqlDataAdapter"/>.
    /// </summary>
    protected override void InitializeBatching()
    {
      commandBatch = new List<IDbCommand>();
    }

    /// <summary>
    /// Adds a <see cref="IDbCommand"/> to the current batch.
    /// </summary>
    /// <param name="command">The <see cref="IDbCommand"/> to add to the batch.</param>
    /// <returns>The number of commands in the batch before adding the <see cref="IDbCommand"/>.</returns>
    protected override int AddToBatch(IDbCommand command)
    {
      // the first time each command is asked to be batched, we ask
      // that command to prepare its batchable command text. We only want
      // to do this one time for each command
      MySqlCommand commandToBatch = (MySqlCommand)command;
      if (commandToBatch.BatchableCommandText == null)
        commandToBatch.GetCommandTextForBatching();

      IDbCommand cloneCommand = (IDbCommand)((ICloneable)command).Clone();
      commandBatch.Add(cloneCommand);

      return commandBatch.Count - 1;
    }

    /// <summary>
    /// Executes the current batch.
    /// </summary>
    /// <returns>The return value from the last command in the batch.</returns>
    protected override int ExecuteBatch()
    {
      int recordsAffected = 0;
      int index = 0;
      while (index < commandBatch.Count)
      {
        MySqlCommand cmd = (MySqlCommand)commandBatch[index++];
        for (int index2 = index; index2 < commandBatch.Count; index2++, index++)
        {
          MySqlCommand cmd2 = (MySqlCommand)commandBatch[index2];
          if (cmd2.BatchableCommandText == null ||
            cmd2.CommandText != cmd.CommandText) break;
          cmd.AddToBatch(cmd2);
        }
        recordsAffected += cmd.ExecuteNonQuery();
      }
      return recordsAffected;
    }

    /// <summary>
    /// Removes all <see cref="IDbCommand"/> objects from the batch.
    /// </summary>
    protected override void ClearBatch()
    {
      if (commandBatch.Count > 0)
      {
        MySqlCommand cmd = (MySqlCommand)commandBatch[0];
        if (cmd.Batch != null)
          cmd.Batch.Clear();
      }
      commandBatch.Clear();
    }

    /// <summary>
    /// Ends batching for the <see cref="MySqlDataAdapter"/>.
    /// </summary>
    protected override void TerminateBatching()
    {
      ClearBatch();
      commandBatch = null;
    }

    /// <summary>
    /// Returns a System.Data.IDataParameter from one of the commands in the current batch.
    /// </summary>
    /// <param name="commandIdentifier">The index of the command to retrieve the parameter from.</param>
    /// <param name="parameterIndex">The index of the parameter within the command.</param>
    /// <returns>The <see cref="IDataParameter"/> specified.</returns>
    protected override IDataParameter GetBatchedParameter(int commandIdentifier, int parameterIndex)
    {
      return (IDataParameter)commandBatch[commandIdentifier].Parameters[parameterIndex];
    }

    #endregion

    /// <summary>
    /// Overridden. See <see cref="DbDataAdapter.CreateRowUpdatedEvent"/>.
    /// </summary>
    /// <param name="dataRow"></param>
    /// <param name="command"></param>
    /// <param name="statementType"></param>
    /// <param name="tableMapping"></param>
    /// <returns></returns>
    override protected RowUpdatedEventArgs CreateRowUpdatedEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
    {
      return new MySqlRowUpdatedEventArgs(dataRow, command, statementType, tableMapping);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RowUpdatingEventArgs"/> class.
    /// </summary>
    /// <param name="dataRow">The <see cref="DataRow"/> that updates the data source.</param>
    /// <param name="command">The <see cref="IDbCommand"/> to execute during the <see cref="IDataAdapter.Update(DataSet)"/>.</param>
    /// <param name="statementType">Whether the command is an UPDATE, INSERT, DELETE, or SELECT statement.</param>
    /// <param name="tableMapping">A <see cref="DataTableMapping"/> object.</param>
    /// <returns></returns>
    override protected RowUpdatingEventArgs CreateRowUpdatingEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
    {
      return new MySqlRowUpdatingEventArgs(dataRow, command, statementType, tableMapping);
    }

    /// <summary>
    /// Overridden. Raises the RowUpdating event.
    /// </summary>
    /// <param name="value">A MySqlRowUpdatingEventArgs that contains the event data.</param>
    override protected void OnRowUpdating(RowUpdatingEventArgs value)
    {
      if (RowUpdating != null)
        RowUpdating(this, (value as MySqlRowUpdatingEventArgs));
    }

    /// <summary>
    /// Overridden. Raises the RowUpdated event.
    /// </summary>
    /// <param name="value">A MySqlRowUpdatedEventArgs that contains the event data. </param>
    override protected void OnRowUpdated(RowUpdatedEventArgs value)
    {
      if (RowUpdated != null)
        RowUpdated(this, (value as MySqlRowUpdatedEventArgs));
    }

    #region Async
    #region Fill
    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataSet)"/> method.
    /// </summary>
    /// <param name="dataSet">The <see cref="DataSet"/> to fill records with.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataSet"/>.</returns>
    public Task<int> FillAsync(DataSet dataSet)
    {
      return FillAsync(dataSet, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataSet)"/> method.
    /// </summary>
    /// <param name="dataSet">The <see cref="DataSet"/> to fill records with.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataSet"/>.</returns>
    public Task<int> FillAsync(DataSet dataSet, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var fillResult = base.Fill(dataSet);
          result.SetResult(fillResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataTable)"/> method.
    /// </summary>
    /// <param name="dataTable">The name of the <see cref="DataTable"/> to use for table mapping.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataTable"/>.</returns>
    public Task<int> FillAsync(DataTable dataTable)
    {
      return FillAsync(dataTable, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataTable)"/> method.
    /// </summary>
    /// <param name="dataTable">The name of the <see cref="DataTable"/> to use for table mapping.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataTable"/>.</returns>
    public Task<int> FillAsync(DataTable dataTable, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var fillResult = base.Fill(dataTable);
          result.SetResult(fillResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataSet, string)"/> method.
    /// </summary>
    /// <param name="dataSet">The <see cref="DataSet"/> to fill with records.</param>
    /// <param name="srcTable">The name of the source table to use for table mapping.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataSet"/>.</returns>
    public Task<int> FillAsync(DataSet dataSet, string srcTable)
    {
      return FillAsync(dataSet, srcTable, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataSet, string)"/> method.
    /// </summary>
    /// <param name="dataSet">The <see cref="DataSet"/> to fill with records.</param>
    /// <param name="srcTable">The name of the source table to use for table mapping.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataSet"/>.</returns>
    public Task<int> FillAsync(DataSet dataSet, string srcTable, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var fillResult = base.Fill(dataSet, srcTable);
          result.SetResult(fillResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataTable)"/> method.
    /// </summary>
    /// <param name="dataTable">The <see cref="DataTable"/> to fill with records.</param>
    /// <param name="dataReader">An instance of <see cref="IDataReader"/>.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataTable"/>.</returns>
    public Task<int> FillAsync(DataTable dataTable, IDataReader dataReader)
    {
      return FillAsync(dataTable, dataReader, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataTable)"/> method.
    /// </summary>
    /// <param name="dataTable">The <see cref="DataTable"/> to fill with records.</param>
    /// <param name="dataReader">An instance of <see cref="IDataReader"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataTable"/>.</returns>
    public Task<int> FillAsync(DataTable dataTable, IDataReader dataReader, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var fillResult = base.Fill(dataTable, dataReader);
          result.SetResult(fillResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataTable, IDbCommand, CommandBehavior)"/> method.
    /// </summary>
    /// <param name="dataTable">The <see cref="DataTable"/> to fill with records.</param>
    /// <param name="command">The SQL SELECT statement used to retrieve rows from the data source.</param>
    /// <param name="behavior">One of the <see cref="CommandBehavior"/> values.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataTable"/>.</returns>
    public Task<int> FillAsync(DataTable dataTable, IDbCommand command, CommandBehavior behavior)
    {
      return FillAsync(dataTable, command, behavior, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataTable, IDbCommand, CommandBehavior)"/> method.
    /// </summary>
    /// <param name="dataTable">The <see cref="DataTable"/> to fill with records.</param>
    /// <param name="command">The SQL SELECT statement used to retrieve rows from the data source.</param>
    /// <param name="behavior">One of the <see cref="CommandBehavior"/> values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataTable"/>.</returns>
    public Task<int> FillAsync(DataTable dataTable, IDbCommand command, CommandBehavior behavior, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var fillResult = base.Fill(dataTable, command, behavior);
          result.SetResult(fillResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(int, int, DataTable[])"/> method.
    /// </summary>
    /// <param name="startRecord">The start record.</param>
    /// <param name="maxRecords">The max number of affected records.</param>
    /// <param name="dataTables">The <see cref="DataTable"/>s to fill with records.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataTable"/>.</returns>
    public Task<int> FillAsync(int startRecord, int maxRecords, params DataTable[] dataTables)
    {
      return FillAsync(startRecord, maxRecords, CancellationToken.None, dataTables);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(int, int, DataTable[])"/> method.
    /// </summary>
    /// <param name="startRecord">The start record.</param>
    /// <param name="maxRecords">The max number of affected records.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="dataTables">The <see cref="DataTable"/>s to fill with records.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataTable"/>.</returns>
    public Task<int> FillAsync(int startRecord, int maxRecords, CancellationToken cancellationToken, params DataTable[] dataTables)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var fillResult = base.Fill(startRecord, maxRecords, dataTables);
          result.SetResult(fillResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataSet, int, int, string)"/> method.
    /// </summary>
    /// <param name="dataSet">The <see cref="DataSet"/> to fill with records.</param>
    /// <param name="startRecord">The start record.</param>
    /// <param name="maxRecords">The max number of affected records.</param>
    /// <param name="srcTable">The name of the source table to use for table mapping.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataSet"/>.</returns>
    public Task<int> FillAsync(DataSet dataSet, int startRecord, int maxRecords, string srcTable)
    {
      return FillAsync(dataSet, startRecord, maxRecords, srcTable, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataSet, int, int, string)"/> method.
    /// </summary>
    /// <param name="dataSet">The <see cref="DataSet"/> to fill with records.</param>
    /// <param name="startRecord">The start record.</param>
    /// <param name="maxRecords">The max number of affected records.</param>
    /// <param name="srcTable">The name of the source table to use for table mapping.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataSet"/>.</returns>
    public Task<int> FillAsync(DataSet dataSet, int startRecord, int maxRecords, string srcTable, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var fillResult = base.Fill(dataSet, startRecord, maxRecords, srcTable);
          result.SetResult(fillResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataSet, int, int, string)"/> method.
    /// </summary>
    /// <param name="dataSet">The <see cref="DataSet"/> to fill with records.</param>
    /// <param name="srcTable">The name of the source table to use for table mapping.</param>
    /// <param name="dataReader">An instance of <see cref="IDataReader"/>.</param>
    /// <param name="startRecord">The start record.</param>
    /// <param name="maxRecords">The max number of affected records.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataSet"/>.</returns>
    public Task<int> FillAsync(DataSet dataSet, string srcTable, IDataReader dataReader, int startRecord, int maxRecords)
    {
      return FillAsync(dataSet, srcTable, dataReader, startRecord, maxRecords, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataSet, int, int, string)"/> method.
    /// </summary>
    /// <param name="dataSet">The <see cref="DataSet"/> to fill with records.</param>
    /// <param name="srcTable">The name of the source table to use for table mapping.</param>
    /// <param name="dataReader">An instance of <see cref="IDataReader"/>.</param>
    /// <param name="startRecord">The start record.</param>
    /// <param name="maxRecords">The max number of affected records.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataSet"/>.</returns>
    public Task<int> FillAsync(DataSet dataSet, string srcTable, IDataReader dataReader, int startRecord, int maxRecords, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var fillResult = base.Fill(dataSet, srcTable, dataReader, startRecord, maxRecords);
          result.SetResult(fillResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataTable[], int, int, IDbCommand, CommandBehavior)"/> method.
    /// </summary>
    /// <param name="dataTables">The <see cref="DataTable"/>s to fill with records.</param>
    /// <param name="startRecord">The start record.</param>
    /// <param name="maxRecords">The max number of affected records.</param>
    /// <param name="command">The SQL SELECT statement used to retrieve rows from the data source.</param>
    /// <param name="behavior">One of the <see cref="CommandBehavior"/> values.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataTable"/>s.</returns>
    public Task<int> FillAsync(DataTable[] dataTables, int startRecord, int maxRecords, IDbCommand command, CommandBehavior behavior)
    {
      return FillAsync(dataTables, startRecord, maxRecords, command, behavior, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataTable[], int, int, IDbCommand, CommandBehavior)"/> method.
    /// </summary>
    /// <param name="dataTables">The <see cref="DataTable"/>s to fill with records.</param>
    /// <param name="startRecord">The start record.</param>
    /// <param name="maxRecords">The max number of affected records.</param>
    /// <param name="command">The SQL SELECT statement used to retrieve rows from the data source.</param>
    /// <param name="behavior">One of the <see cref="CommandBehavior"/> values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataTable"/>s.</returns>
    public Task<int> FillAsync(DataTable[] dataTables, int startRecord, int maxRecords, IDbCommand command, CommandBehavior behavior, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var fillResult = base.Fill(dataTables, startRecord, maxRecords, command, behavior);
          result.SetResult(fillResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataSet, int, int, string, IDbCommand, CommandBehavior)"/> method.
    /// </summary>
    /// <param name="dataSet">The <see cref="DataSet"/> to fill with records.</param>
    /// <param name="startRecord">The start record.</param>
    /// <param name="maxRecords">The max number of affected records.</param>
    /// <param name="srcTable">The name of the source table to use for table mapping.</param>
    /// <param name="command">The SQL SELECT statement used to retrieve rows from the data source.</param>
    /// <param name="behavior">One of the <see cref="CommandBehavior"/> values.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataTable"/>.</returns>
    public Task<int> FillAsync(DataSet dataSet, int startRecord, int maxRecords, string srcTable, IDbCommand command, CommandBehavior behavior)
    {
      return FillAsync(dataSet, startRecord, maxRecords, srcTable, command, behavior, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.Fill(DataSet, int, int, string, IDbCommand, CommandBehavior)"/> method.
    /// </summary>
    /// <param name="dataSet">The <see cref="DataSet"/> to fill with records.</param>
    /// <param name="startRecord">The start record.</param>
    /// <param name="maxRecords">The max number of affected records.</param>
    /// <param name="srcTable">The name of the source table to use for table mapping.</param>
    /// <param name="command">The SQL SELECT statement used to retrieve rows from the data source.</param>
    /// <param name="behavior">One of the <see cref="CommandBehavior"/> values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows successfully added to or refreshed in the <see cref="DataTable"/>.</returns>
    public Task<int> FillAsync(DataSet dataSet, int startRecord, int maxRecords, string srcTable, IDbCommand command, CommandBehavior behavior, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var fillResult = base.Fill(dataSet, startRecord, maxRecords, srcTable, command, behavior);
          result.SetResult(fillResult);
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

    #region FillSchema
    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataSet, SchemaType)"/> method.
    /// </summary>
    /// <param name="dataSet">DataSet to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <returns><see cref="DataTable"/>[]</returns>
    public Task<DataTable[]> FillSchemaAsync(DataSet dataSet, SchemaType schemaType)
    {
      return FillSchemaAsync(dataSet, schemaType, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataSet, SchemaType)"/> method.
    /// </summary>
    /// <param name="dataSet">DataSet to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to use.</param>
    /// <returns><see cref="DataTable"/>[]</returns>
    public Task<DataTable[]> FillSchemaAsync(DataSet dataSet, SchemaType schemaType, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<DataTable[]>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var schemaResult = base.FillSchema(dataSet, schemaType);
          result.SetResult(schemaResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataSet, SchemaType, string)"/> method.
    /// </summary>
    /// <param name="dataSet">DataSet to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <param name="srcTable">Source table to use.</param>
    /// <returns><see cref="DataTable"/>[]</returns>
    public Task<DataTable[]> FillSchemaAsync(DataSet dataSet, SchemaType schemaType, string srcTable)
    {
      return FillSchemaAsync(dataSet, schemaType, srcTable, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataSet, SchemaType, string)"/> method.
    /// </summary>
    /// <param name="dataSet">DataSet to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <param name="srcTable">Source table to use.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to use.</param>
    /// <returns><see cref="DataTable"/>[]</returns>
    public Task<DataTable[]> FillSchemaAsync(DataSet dataSet, SchemaType schemaType, string srcTable, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<DataTable[]>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var schemaResult = base.FillSchema(dataSet, schemaType, srcTable);
          result.SetResult(schemaResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataSet, SchemaType, string)"/> method.
    /// </summary>
    /// <param name="dataSet">DataSet to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <param name="srcTable">Source table to use.</param>
    /// <param name="dataReader">DataReader to use.</param>
    /// <returns><see cref="DataTable"/>[]</returns>
    public Task<DataTable[]> FillSchemaAsync(DataSet dataSet, SchemaType schemaType, string srcTable, IDataReader dataReader)
    {
      return FillSchemaAsync(dataSet, schemaType, srcTable, dataReader, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataSet, SchemaType, string)"/> method.
    /// </summary>
    /// <param name="dataSet">DataSet to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <param name="srcTable">Source table to use.</param>
    /// <param name="dataReader"><see cref="IDataReader"/> to use.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to use.</param>
    /// <returns><see cref="DataTable"/>[]</returns>
    public Task<DataTable[]> FillSchemaAsync(DataSet dataSet, SchemaType schemaType, string srcTable, IDataReader dataReader, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<DataTable[]>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var schemaResult = base.FillSchema(dataSet, schemaType, srcTable, dataReader);
          result.SetResult(schemaResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataSet, SchemaType, IDbCommand, string, CommandBehavior)"/> method.
    /// </summary>
    /// <param name="dataSet">DataSet to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <param name="command">DBCommand to use.</param>
    /// <param name="srcTable">Source table to use.</param>
    /// <param name="behavior">Command Behavior</param>
    /// <returns><see cref="DataTable"/>[]</returns>
    public Task<DataTable[]> FillSchemaAsync(DataSet dataSet, SchemaType schemaType, IDbCommand command, string srcTable, CommandBehavior behavior)
    {
      return FillSchemaAsync(dataSet, schemaType, command, srcTable, behavior, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataSet, SchemaType, IDbCommand, string, CommandBehavior)"/> method.
    /// </summary>
    /// <param name="dataSet">DataSet to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <param name="command">DBCommand to use.</param>
    /// <param name="srcTable">Source table to use.</param>
    /// <param name="behavior">Command Behavior</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to use.</param>
    /// <returns><see cref="DataTable"/>[]</returns>
    public Task<DataTable[]> FillSchemaAsync(DataSet dataSet, SchemaType schemaType, IDbCommand command, string srcTable, CommandBehavior behavior, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<DataTable[]>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var schemaResult = base.FillSchema(dataSet, schemaType, command, srcTable, behavior);
          result.SetResult(schemaResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataTable, SchemaType)"/> method.
    /// </summary>
    /// <param name="dataTable">DataTable to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <returns>DataTable</returns>
    public Task<DataTable> FillSchemaAsync(DataTable dataTable, SchemaType schemaType)
    {
      return FillSchemaAsync(dataTable, schemaType, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataTable, SchemaType)"/> method.
    /// </summary>
    /// <param name="dataTable">DataTable to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to use.</param>
    /// <returns><see cref="DataTable"/></returns>
    public Task<DataTable> FillSchemaAsync(DataTable dataTable, SchemaType schemaType, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<DataTable>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var schemaResult = base.FillSchema(dataTable, schemaType);
          result.SetResult(schemaResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataTable, SchemaType)"/> method.
    /// </summary>
    /// <param name="dataTable">DataTable to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <param name="dataReader">DataReader to use.</param>
    /// <returns><see cref="DataTable"/></returns>
    public Task<DataTable> FillSchemaAsync(DataTable dataTable, SchemaType schemaType, IDataReader dataReader)
    {
      return FillSchemaAsync(dataTable, schemaType, dataReader, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataTable, SchemaType)"/> method.
    /// </summary>
    /// <param name="dataTable">DataTable to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <param name="dataReader">DataReader to use.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to use.</param>
    /// <returns><see cref="DataTable"/></returns>
    public Task<DataTable> FillSchemaAsync(DataTable dataTable, SchemaType schemaType, IDataReader dataReader, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<DataTable>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var schemaResult = base.FillSchema(dataTable, schemaType, dataReader);
          result.SetResult(schemaResult);
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
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataTable, SchemaType, IDbCommand, CommandBehavior)"/> method.
    /// </summary>
    /// <param name="dataTable">DataTable to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <param name="command">DBCommand to use.</param>
    /// <param name="behavior">Command Behavior</param>
    /// <returns><see cref="DataTable"/></returns>
    public Task<DataTable> FillSchemaAsync(DataTable dataTable, SchemaType schemaType, IDbCommand command, CommandBehavior behavior)
    {
      return FillSchemaAsync(dataTable, schemaType, command, behavior, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DbDataAdapter.FillSchema(DataTable, SchemaType, IDbCommand, CommandBehavior)"/> method.
    /// </summary>
    /// <param name="dataTable">DataTable to use.</param>
    /// <param name="schemaType">Schema type to use.</param>
    /// <param name="command">DBCommand to use.</param>
    /// <param name="behavior">Command behavior.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to use.</param>
    /// <returns><see cref="DataTable"/></returns>
    public Task<DataTable> FillSchemaAsync(DataTable dataTable, SchemaType schemaType, IDbCommand command, CommandBehavior behavior, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<DataTable>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var schemaResult = base.FillSchema(dataTable, schemaType, command, behavior);
          result.SetResult(schemaResult);
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

    #region Update
    /// <summary>
    /// Asynchronous version of the <see cref="DataAdapter.Update"/> method.
    /// </summary>
    /// <param name="dataRows">DataRow[] to use.</param>
    /// <returns>The number of rows successfully updated from the <see cref="DataSet"/>.</returns>
    public Task<int> UpdateAsync(DataRow[] dataRows)
    {
      return UpdateAsync(dataRows, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DataAdapter.Update"/> method.
    /// </summary>
    /// <param name="dataRows">DataRow[] to use.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to use.</param>
    /// <returns>The number of rows successfully updated from the <see cref="DataSet"/>.</returns>

    public Task<int> UpdateAsync(DataRow[] dataRows, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var update = base.Update(dataRows);
          result.SetResult(update);
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
    /// Asynchronous version of the <see cref="DataAdapter.Update"/> method.
    /// </summary>
    /// <param name="dataSet">DataSet to use.</param>
    /// <returns>The number of rows successfully updated from the <see cref="DataSet"/>.</returns>

    public Task<int> UpdateAsync(DataSet dataSet)
    {
      return UpdateAsync(dataSet, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DataAdapter.Update"/> method.
    /// </summary>
    /// <param name="dataSet">DataSet to use.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to use.</param>
    /// <returns>The number of rows successfully updated from the <see cref="DataSet"/>.</returns>

    public Task<int> UpdateAsync(DataSet dataSet, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var update = base.Update(dataSet);
          result.SetResult(update);
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
    /// Asynchronous version of the <see cref="DataAdapter.Update"/> method.
    /// </summary>
    /// <param name="dataTable">DataTable to use.</param>
    /// <returns>The number of rows successfully updated from the <see cref="DataSet"/>.</returns>

    public Task<int> UpdateAsync(DataTable dataTable)
    {
      return UpdateAsync(dataTable, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DataAdapter.Update"/> method.
    /// </summary>
    /// <param name="dataTable">DataTable to use.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to use.</param>
    /// <returns>The number of rows successfully updated from the <see cref="DataSet"/>.</returns>

    public Task<int> UpdateAsync(DataTable dataTable, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var update = base.Update(dataTable);
          result.SetResult(update);
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
    /// Asynchronous version of the <see cref="DataAdapter.Update"/> method.
    /// </summary>
    /// <param name="dataRows">DataRow[] to use.</param>
    /// <param name="tableMapping">Data Table Mapping</param>
    /// <returns>The number of rows successfully updated from the <see cref="DataSet"/>.</returns>

    public Task<int> UpdateAsync(DataRow[] dataRows, DataTableMapping tableMapping)
    {
      return UpdateAsync(dataRows, tableMapping, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DataAdapter.Update"/> method.
    /// </summary>
    /// <param name="dataRows">DataRow[] to use.</param>
    /// <param name="tableMapping">Data Table Mapping</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to use.</param>
    /// <returns>The number of rows successfully updated from the <see cref="DataSet"/>.</returns>

    public Task<int> UpdateAsync(DataRow[] dataRows, DataTableMapping tableMapping, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var update = base.Update(dataRows, tableMapping);
          result.SetResult(update);
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
    /// Asynchronous version of the <see cref="DataAdapter.Update"/> method.
    /// </summary>
    /// <param name="dataSet">DataSet to use.</param>
    /// <param name="srcTable">Source table to use.</param>
    /// <returns>The number of rows successfully updated from the <see cref="DataSet"/>.</returns>

    public Task<int> UpdateAsync(DataSet dataSet, string srcTable)
    {
      return UpdateAsync(dataSet, srcTable, CancellationToken.None);
    }

    /// <summary>
    /// Asynchronous version of the <see cref="DataAdapter.Update"/> method.
    /// </summary>
    /// <param name="dataSet">DataSet to use.</param>
    /// <param name="srcTable">Source table to use.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to use.</param>
    /// <returns>The number of rows successfully updated from the <see cref="DataSet"/>.</returns>

    public Task<int> UpdateAsync(DataSet dataSet, string srcTable, CancellationToken cancellationToken)
    {
      var result = new TaskCompletionSource<int>();
      if (cancellationToken == CancellationToken.None || !cancellationToken.IsCancellationRequested)
      {
        try
        {
          var update = base.Update(dataSet, srcTable);
          result.SetResult(update);
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
    #endregion

  }

  /// <summary>
  /// Represents the method that will handle the <see cref="MySqlDataAdapter.RowUpdating"/> event of a <see cref="MySqlDataAdapter"/>.
  /// </summary>
  public delegate void MySqlRowUpdatingEventHandler(object sender, MySqlRowUpdatingEventArgs e);

  /// <summary>
  /// Represents the method that will handle the <see cref="MySqlDataAdapter.RowUpdated"/> event of a <see cref="MySqlDataAdapter"/>.
  /// </summary>
  public delegate void MySqlRowUpdatedEventHandler(object sender, MySqlRowUpdatedEventArgs e);

  /// <summary>
  /// Provides data for the RowUpdating event. This class cannot be inherited.
  /// </summary>
  public sealed class MySqlRowUpdatingEventArgs : RowUpdatingEventArgs
  {
    /// <summary>
    /// Initializes a new instance of the MySqlRowUpdatingEventArgs class.
    /// </summary>
    /// <param name="row">The <see cref="DataRow"/> to 
    /// <see cref="DbDataAdapter.Update(DataSet)"/>.</param>
    /// <param name="command">The <see cref="IDbCommand"/> to execute during <see cref="DbDataAdapter.Update(DataSet)"/>.</param>
    /// <param name="statementType">One of the <see cref="StatementType"/> values that specifies the type of query executed.</param>
    /// <param name="tableMapping">The <see cref="DataTableMapping"/> sent through an <see cref="DbDataAdapter.Update(DataSet)"/>.</param>
    public MySqlRowUpdatingEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
      : base(row, command, statementType, tableMapping)
    {
    }

    /// <summary>
    /// Gets or sets the MySqlCommand to execute when performing the Update.
    /// </summary>
    new public MySqlCommand Command
    {
      get { return (MySqlCommand)base.Command; }
      set { base.Command = value; }
    }
  }

  /// <summary>
  /// Provides data for the RowUpdated event. This class cannot be inherited.
  /// </summary>
  public sealed class MySqlRowUpdatedEventArgs : RowUpdatedEventArgs
  {
    /// <summary>
    /// Initializes a new instance of the MySqlRowUpdatedEventArgs class.
    /// </summary>
    /// <param name="row">The <see cref="DataRow"/> sent through an <see cref="DbDataAdapter.Update(DataSet)"/>.</param>
    /// <param name="command">The <see cref="IDbCommand"/> executed when <see cref="DbDataAdapter.Update(DataSet)"/> is called.</param>
    /// <param name="statementType">One of the <see cref="StatementType"/> values that specifies the type of query executed.</param>
    /// <param name="tableMapping">The <see cref="DataTableMapping"/> sent through an <see cref="DbDataAdapter.Update(DataSet)"/>.</param>
    public MySqlRowUpdatedEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping)
      : base(row, command, statementType, tableMapping)
    {
    }

    /// <summary>
    /// Gets or sets the MySqlCommand executed when Update is called.
    /// </summary>
    new public MySqlCommand Command
    {
      get { return (MySqlCommand)base.Command; }
    }
  }
}
