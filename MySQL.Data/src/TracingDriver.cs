// Copyright © 2009, 2026, Oracle and/or its affiliates.
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

using MySql.Data.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  internal class TracingDriver : Driver
  {
    private static long driverCounter;
    private long driverId;
    private ResultSet activeResult;
    private int rowSizeInBytes;

    public TracingDriver(MySqlConnectionStringBuilder settings)
      : base(settings)
    {
      driverId = Interlocked.Increment(ref driverCounter);
    }

    /// <summary>
    /// Opens the connection and traces the connection open event.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    public override void Open(CancellationToken cancellationToken)
    {
      base.Open(cancellationToken);
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ConnectionOpened,
          Resources.TraceOpenConnection, driverId, Settings.ConnectionString, ThreadID);
    }

    /// <summary>
    /// Asynchronously opens the connection and traces the connection open event.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous open operation.</returns>
    public override async Task OpenAsync(CancellationToken cancellationToken)
    {
      await base.OpenAsync(cancellationToken).ConfigureAwait(false);
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ConnectionOpened,
          Resources.TraceOpenConnection, driverId, Settings.ConnectionString, ThreadID);
    }

    /// <summary>
    /// Closes the connection and traces the connection closure event.
    /// </summary>
    public override void Close()
    {
      base.Close();
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ConnectionClosed,
          Resources.TraceCloseConnection, driverId);
    }

    /// <summary>
    /// Asynchronously closes the connection and traces the connection closure event.
    /// </summary>
    /// <returns>A task that represents the asynchronous close operation.</returns>
    public override async Task CloseAsync()
    {
      await base.CloseAsync();
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ConnectionClosed,
          Resources.TraceCloseConnection, driverId);
    }

    /// <summary>
    /// Sends a query to the server and traces the query open event, normalizing long queries.
    /// </summary>
    /// <param name="p">The MySqlPacket containing the query.</param>
    /// <param name="paramsPosition">The position of parameters in the packet.</param>
    public override void SendQuery(MySqlPacket p, int paramsPosition)
    {
      rowSizeInBytes = 0;
      string cmdText = Encoding.GetString(p.Buffer, paramsPosition, p.Length - paramsPosition);
      string normalizedQuery = null;

      if (cmdText.Length > 300)
      {
        QueryNormalizer normalizer = new QueryNormalizer();
        normalizedQuery = normalizer.Normalize(cmdText);
        cmdText = cmdText.Substring(0, 300);
      }

      base.SendQuery(p, paramsPosition);

      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.QueryOpened,
          Resources.TraceQueryOpened, driverId, ThreadID, cmdText);
      if (normalizedQuery != null)
        MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.QueryNormalized,
            Resources.TraceQueryNormalized, driverId, ThreadID, normalizedQuery);
    }

    /// <summary>
    /// Asynchronously sends a query to the server and traces the query open event, normalizing long queries.
    /// </summary>
    /// <param name="p">The MySqlPacket containing the query.</param>
    /// <param name="paramsPosition">The position of parameters in the packet.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    public override async Task SendQueryAsync(MySqlPacket p, int paramsPosition)
    {
      rowSizeInBytes = 0;
      string cmdText = Encoding.GetString(p.Buffer, paramsPosition, p.Length - paramsPosition);
      string normalizedQuery = null;

      if (cmdText.Length > 300)
      {
        QueryNormalizer normalizer = new QueryNormalizer();
        normalizedQuery = normalizer.Normalize(cmdText);
        cmdText = cmdText.Substring(0, 300);
      }

      await base.SendQueryAsync(p, paramsPosition).ConfigureAwait(false);

      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.QueryOpened,
          Resources.TraceQueryOpened, driverId, ThreadID, cmdText);
      if (normalizedQuery != null)
        MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.QueryNormalized,
            Resources.TraceQueryNormalized, driverId, ThreadID, normalizedQuery);
    }

    /// <summary>
    /// Retrieves the result of a statement execution and traces the result open event or errors.
    /// </summary>
    /// <param name="statementId">The ID of the statement.</param>
    /// <param name="affectedRows">The number of affected rows from the statement.</param>
    /// <param name="insertedId">The auto-generated ID of the inserted row, if any.</param>
    /// <returns>A tuple containing the field count, affected rows, and inserted ID.</returns>
    protected override Tuple<int, int, long> GetResult(int statementId, int affectedRows, long insertedId)
    {
      try
      {
        var result = base.GetResult(statementId, affectedRows, insertedId);
        int fieldCount = result.Item1;
        affectedRows = result.Item2;
        insertedId = result.Item3;

        MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ResultOpened,
            Resources.TraceResult, driverId, fieldCount, affectedRows, insertedId);

        return new Tuple<int, int, long>(fieldCount, affectedRows, insertedId);
      }
      catch (MySqlException ex)
      {
        // we got an error so we report it
        MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.Error,
            Resources.TraceOpenResultError, driverId, ex.Number, ex.Message);
        throw;
      }
    }

    /// <summary>
    /// Asynchronously retrieves the result of a statement execution and traces the result open event or errors.
    /// </summary>
    /// <param name="statementId">The ID of the statement.</param>
    /// <param name="affectedRows">The number of affected rows from the statement.</param>
    /// <param name="insertedId">The auto-generated ID of the inserted row, if any.</param>
    /// <returns>A task that represents the asynchronous operation, containing a tuple with the field count, affected rows, and inserted ID.</returns>
    protected override async Task<Tuple<int, int, long>> GetResultAsync(int statementId, int affectedRows, long insertedId)
    {
      try
      {
        var result = await base.GetResultAsync(statementId, affectedRows, insertedId).ConfigureAwait(false);
        int fieldCount = result.Item1;
        affectedRows = result.Item2;
        insertedId = result.Item3;

        MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ResultOpened,
            Resources.TraceResult, driverId, fieldCount, affectedRows, insertedId);

        return new Tuple<int, int, long>(fieldCount, affectedRows, insertedId);
      }
      catch (MySqlException ex)
      {
        // we got an error so we report it
        MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.Error,
            Resources.TraceOpenResultError, driverId, ex.Number, ex.Message);
        throw;
      }
    }

    /// <summary>
    /// Closes the active result set if present, reporting usage advisor warnings and tracing the result closed event.
    /// </summary>
    /// <param name="statementId">The ID of the statement.</param>
    private void CloseActiveResult(int statementId)
    {
      if (activeResult == null)
      {
        return;
      }

      if (Settings.UseUsageAdvisor)
        ReportUsageAdvisorWarnings(statementId, activeResult);
        
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ResultClosed,
          Resources.TraceResultClosed, driverId, activeResult.TotalRows, activeResult.SkippedRows,
          rowSizeInBytes);
      rowSizeInBytes = 0;
      activeResult = null;
    }

    /// <summary>
    /// Retrieves the next result set, closing any active result and tracing the result closed event if applicable.
    /// </summary>
    /// <param name="statementId">The ID of the statement.</param>
    /// <param name="force">Indicates whether to force the next result.</param>
    /// <returns>The next ResultSet, or null if no more results.</returns>
    public override ResultSet NextResult(int statementId, bool force)
    {
      CloseActiveResult(statementId);
      activeResult = base.NextResult(statementId, force);
      return activeResult;
    }

    /// <summary>
    /// Asynchronously retrieves the next result set, closing any active result and tracing the result closed event if applicable.
    /// </summary>
    /// <param name="statementId">The ID of the statement.</param>
    /// <param name="force">Indicates whether to force the next result.</param>
    /// <returns>A task that represents the asynchronous operation, returning the next ResultSet or null if no more results.</returns>
    public override async Task<ResultSet> NextResultAsync(int statementId, bool force)
    {
      CloseActiveResult(statementId);
      activeResult = await base.NextResultAsync(statementId, force).ConfigureAwait(false);
      return activeResult;
    }

    /// <summary>
    /// Prepares a statement and traces the statement prepared event.
    /// </summary>
    /// <param name="sql">The SQL statement to prepare.</param>
    /// <returns>A tuple containing the statement ID and the MySqlField array for parameters.</returns>
    public override Tuple<int, MySqlField[]> PrepareStatement(string sql)
    {
      var result = base.PrepareStatement(sql);
      int statementId = result.Item1;
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.StatementPrepared,
          Resources.TraceStatementPrepared, driverId, sql, statementId);
      return new Tuple<int, MySqlField[]>(result.Item1, result.Item2);
    }

    /// <summary>
    /// Asynchronously prepares a statement and traces the statement prepared event.
    /// </summary>
    /// <param name="sql">The SQL statement to prepare.</param>
    /// <returns>A task that represents the asynchronous prepare operation, containing a tuple with the statement ID and the MySqlField array for parameters.</returns>
    public override async Task<Tuple<int, MySqlField[]>> PrepareStatementAsync(string sql)
    {
      var result = await base.PrepareStatementAsync(sql).ConfigureAwait(false);
      int statementId = result.Item1;
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.StatementPrepared,
          Resources.TraceStatementPrepared, driverId, sql, statementId);
      return new Tuple<int, MySqlField[]>(result.Item1, result.Item2);
    }

    /// <summary>
    /// Closes a prepared statement and traces the statement closed event.
    /// </summary>
    /// <param name="id">The ID of the statement to close.</param>
    public override void CloseStatement(int id)
    {
      base.CloseStatement(id);
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.StatementClosed,
          Resources.TraceStatementClosed, driverId, id);
    }

    /// <summary>
    /// Asynchronously closes a prepared statement and traces the statement closed event.
    /// </summary>
    /// <param name="id">The ID of the statement to close.</param>
    /// <returns>A task that represents the asynchronous close operation.</returns>
    public override async Task CloseStatementAsync(int id)
    {
      await base.CloseStatementAsync(id).ConfigureAwait(false);
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.StatementClosed,
          Resources.TraceStatementClosed, driverId, id);
    }

    /// <summary>
    /// Sets the current database and traces the set database event.
    /// </summary>
    /// <param name="dbName">The name of the database to set.</param>
    public override void SetDatabase(string dbName)
    {
      base.SetDatabase(dbName);
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.NonQuery,
          Resources.TraceSetDatabase, driverId, dbName);
    }

    /// <summary>
    /// Asynchronously sets the current database and traces the set database event.
    /// </summary>
    /// <param name="dbName">The name of the database to set.</param>
    /// <returns>A task that represents the asynchronous set database operation.</returns>
    public override async Task SetDatabaseAsync(string dbName)
    {
      await base.SetDatabaseAsync(dbName);
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.NonQuery,
          Resources.TraceSetDatabase, driverId, dbName);
    }

    /// <summary>
    /// Executes a prepared statement and traces the statement executed event.
    /// </summary>
    /// <param name="packetToExecute">The packet containing the statement to execute.</param>
    public override void ExecuteStatement(MySqlPacket packetToExecute)
    {
      base.ExecuteStatement(packetToExecute);
      int pos = packetToExecute.Position;
      packetToExecute.Position = 1;
      int statementId = packetToExecute.ReadInteger(4);
      packetToExecute.Position = pos;

      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.StatementExecuted,
          Resources.TraceStatementExecuted, driverId, statementId, ThreadID);
    }

    /// <summary>
    /// Asynchronously executes a prepared statement and traces the statement executed event.
    /// </summary>
    /// <param name="packetToExecute">The packet containing the statement to execute.</param>
    /// <returns>A task that represents the asynchronous execute operation.</returns>
    public override async Task ExecuteStatementAsync(MySqlPacket packetToExecute)
    {
      await base.ExecuteStatementAsync(packetToExecute).ConfigureAwait(false);
      int pos = packetToExecute.Position;
      packetToExecute.Position = 1;
      int statementId = packetToExecute.ReadInteger(4);
      packetToExecute.Position = pos;

      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.StatementExecuted,
          Resources.TraceStatementExecuted, driverId, statementId, ThreadID);
    }

    /// <summary>
    /// Fetches a data row for the given statement and traces any errors during the fetch operation.
    /// </summary>
    /// <param name="statementId">The ID of the statement.</param>
    /// <param name="columns">The number of columns in the row.</param>
    /// <returns>True if a row was fetched; otherwise, false.</returns>
    public override bool FetchDataRow(int statementId, int columns)
    {
      try
      {
        bool b = base.FetchDataRow(statementId, columns);
        if (b)
          rowSizeInBytes += (handler as NativeDriver).Packet.Length;
        return b;
      }
      catch (MySqlException ex)
      {
        MySqlTrace.TraceEvent(TraceEventType.Error, MySqlTraceEventType.Error,
            Resources.TraceFetchError, driverId, ex.Number, ex.Message);
        throw;
      }
    }

    /// <summary>
    /// Asynchronously fetches a data row for the given statement and traces any errors during the fetch operation.
    /// </summary>
    /// <param name="statementId">The ID of the statement.</param>
    /// <param name="columns">The number of columns in the row.</param>
    /// <returns>A task that represents the asynchronous fetch operation, returning true if a row was fetched; otherwise, false.</returns>
    public override async Task<bool> FetchDataRowAsync(int statementId, int columns)
    {
      try
      {
        bool b = await base.FetchDataRowAsync(statementId, columns).ConfigureAwait(false);
        if (b)
          rowSizeInBytes += (handler as NativeDriver).Packet.Length;
        return b;
      }
      catch (MySqlException ex)
      {
        MySqlTrace.TraceEvent(TraceEventType.Error, MySqlTraceEventType.Error,
            Resources.TraceFetchError, driverId, ex.Number, ex.Message);
        throw;
      }
    }

    /// <summary>
    /// Closes the current query and traces the query closed event.
    /// </summary>
    /// <param name="connection">The MySqlConnection associated with the query.</param>
    /// <param name="statementId">The ID of the statement.</param>
    public override void CloseQuery(MySqlConnection connection, int statementId)
    {
      base.CloseQuery(connection, statementId);
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.QueryClosed, Resources.TraceQueryDone, driverId);
    }

    /// <summary>
    /// Asynchronously closes the current query and traces the query closed event.
    /// </summary>
    /// <param name="connection">The MySqlConnection associated with the query.</param>
    /// <param name="statementId">The ID of the statement.</param>
    /// <returns>A task that represents the asynchronous close query operation.</returns>
    public override async Task CloseQueryAsync(MySqlConnection connection, int statementId)
    {
      await base.CloseQueryAsync(connection, statementId).ConfigureAwait(false);
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.QueryClosed, Resources.TraceQueryDone, driverId);
    }

    /// <summary>
    /// Reports server warnings and traces each warning event.
    /// </summary>
    /// <param name="connection">The MySqlConnection to report warnings for.</param>
    /// <returns>A list of MySqlError objects representing the warnings.</returns>
    public override List<MySqlError> ReportWarnings(MySqlConnection connection)
    {
      List<MySqlError> warnings = base.ReportWarnings(connection);

      foreach (MySqlError warning in warnings)
        MySqlTrace.TraceEvent(TraceEventType.Warning, MySqlTraceEventType.Warning, Resources.TraceWarning, driverId, warning.Level, warning.Code, warning.Message);

      return warnings;
    }

    /// <summary>
    /// Asynchronously reports server warnings and traces each warning event.
    /// </summary>
    /// <param name="connection">The MySqlConnection to report warnings for.</param>
    /// <returns>A task that represents the asynchronous report warnings operation, containing a list of MySqlError objects.</returns>
    public override async Task<List<MySqlError>> ReportWarningsAsync(MySqlConnection connection)
    {
      List<MySqlError> warnings = await base.ReportWarningsAsync(connection).ConfigureAwait(false);

      foreach (MySqlError warning in warnings)
        MySqlTrace.TraceEvent(TraceEventType.Warning, MySqlTraceEventType.Warning, Resources.TraceWarning, driverId, warning.Level, warning.Code, warning.Message);

      return warnings;
    }

    private bool AllFieldsAccessed(ResultSet rs)
    {
      if (rs.Fields == null || rs.Fields.Length == 0) return true;

      for (int i = 0; i < rs.Fields.Length; i++)
        if (!rs.FieldRead(i)) return false;
      return true;
    }

    private void ReportUsageAdvisorWarnings(int statementId, ResultSet rs)
    {
      if (!Settings.UseUsageAdvisor) return;

      if (HasStatus(ServerStatusFlags.NoIndex))
        MySqlTrace.TraceEvent(TraceEventType.Warning, MySqlTraceEventType.UsageAdvisorWarning,
            Resources.TraceUAWarningNoIndex, driverId, UsageAdvisorWarningFlags.NoIndex);
      else if (HasStatus(ServerStatusFlags.BadIndex))
        MySqlTrace.TraceEvent(TraceEventType.Warning, MySqlTraceEventType.UsageAdvisorWarning,
            Resources.TraceUAWarningBadIndex, driverId, UsageAdvisorWarningFlags.BadIndex);

      // report abandoned rows
      if (rs.SkippedRows > 0)
        MySqlTrace.TraceEvent(TraceEventType.Warning, MySqlTraceEventType.UsageAdvisorWarning,
            Resources.TraceUAWarningSkippedRows, driverId, UsageAdvisorWarningFlags.SkippedRows, rs.SkippedRows);

      // report not all fields accessed
      if (!AllFieldsAccessed(rs))
      {
        StringBuilder notAccessed = new StringBuilder("");
        string delimiter = "";
        for (int i = 0; i < rs.Size; i++)
          if (!rs.FieldRead(i))
          {
            notAccessed.AppendFormat("{0}{1}", delimiter, rs.Fields[i].ColumnName);
            delimiter = ",";
          }
        MySqlTrace.TraceEvent(TraceEventType.Warning, MySqlTraceEventType.UsageAdvisorWarning,
            Resources.TraceUAWarningSkippedColumns, driverId, UsageAdvisorWarningFlags.SkippedColumns,
                notAccessed.ToString());
      }

      // report type conversions if any
      if (rs.Fields != null)
      {
        foreach (MySqlField f in rs.Fields)
        {
          StringBuilder s = new StringBuilder();
          string delimiter = "";
          foreach (Type t in f.TypeConversions)
          {
            s.AppendFormat("{0}{1}", delimiter, t.Name);
            delimiter = ",";
          }
          if (s.Length > 0)
            MySqlTrace.TraceEvent(TraceEventType.Warning, MySqlTraceEventType.UsageAdvisorWarning,
                Resources.TraceUAWarningFieldConversion, driverId, UsageAdvisorWarningFlags.FieldConversion,
                f.ColumnName, s.ToString());
        }
      }
    }
  }
}
