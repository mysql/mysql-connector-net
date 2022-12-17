// Copyright (c) 2009, 2022, Oracle and/or its affiliates.
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

    public override async Task OpenAsync(bool execAsync, CancellationToken cancellationToken)
    {
      await base.OpenAsync(execAsync, cancellationToken).ConfigureAwait(false);
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ConnectionOpened,
          Resources.TraceOpenConnection, driverId, Settings.ConnectionString, ThreadID);
    }

    public override async Task CloseAsync(bool execAsync)
    {
      await base.CloseAsync(execAsync);
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ConnectionClosed,
          Resources.TraceCloseConnection, driverId);
    }

    public override async Task SendQueryAsync(MySqlPacket p, int paramsPosition, bool execAsync)
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

      await base.SendQueryAsync(p, paramsPosition, execAsync).ConfigureAwait(false);

      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.QueryOpened,
          Resources.TraceQueryOpened, driverId, ThreadID, cmdText);
      if (normalizedQuery != null)
        MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.QueryNormalized,
            Resources.TraceQueryNormalized, driverId, ThreadID, normalizedQuery);
    }

    protected override async Task<Tuple<int, int, long>> GetResultAsync(int statementId, int affectedRows, long insertedId, bool execAsync)
    {
      try
      {
        var result = await base.GetResultAsync(statementId, affectedRows, insertedId, execAsync).ConfigureAwait(false);
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

    public override async Task<ResultSet> NextResultAsync(int statementId, bool force, bool execAsync)
    {
      // first let's see if we already have a resultset on this statementId
      if (activeResult != null)
      {
        //oldRS = activeResults[statementId];
        if (Settings.UseUsageAdvisor)
          ReportUsageAdvisorWarnings(statementId, activeResult);
        MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.ResultClosed,
            Resources.TraceResultClosed, driverId, activeResult.TotalRows, activeResult.SkippedRows,
            rowSizeInBytes);
        rowSizeInBytes = 0;
        activeResult = null;
      }

      activeResult = await base.NextResultAsync(statementId, force, execAsync).ConfigureAwait(false);
      return activeResult;
    }

    public override async Task<Tuple<int, MySqlField[]>> PrepareStatementAsync(string sql, bool execAsync)
    {
      var result = await base.PrepareStatementAsync(sql, execAsync).ConfigureAwait(false);
      int statementId = result.Item1;
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.StatementPrepared,
          Resources.TraceStatementPrepared, driverId, sql, statementId);
      return new Tuple<int, MySqlField[]>(result.Item1, result.Item2);
    }

    public override async Task CloseStatementAsync(int id, bool execAsync)
    {
      await base.CloseStatementAsync(id, execAsync).ConfigureAwait(false);
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.StatementClosed,
          Resources.TraceStatementClosed, driverId, id);
    }

    public override async Task SetDatabaseAsync(string dbName, bool execAsync)
    {
      await base.SetDatabaseAsync(dbName, execAsync);
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.NonQuery,
          Resources.TraceSetDatabase, driverId, dbName);
    }

    public override async Task ExecuteStatementAsync(MySqlPacket packetToExecute, bool execAsync)
    {
      await base.ExecuteStatementAsync(packetToExecute, execAsync).ConfigureAwait(false);
      int pos = packetToExecute.Position;
      packetToExecute.Position = 1;
      int statementId = packetToExecute.ReadInteger(4);
      packetToExecute.Position = pos;

      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.StatementExecuted,
          Resources.TraceStatementExecuted, driverId, statementId, ThreadID);
    }

    public override async Task<bool> FetchDataRowAsync(int statementId, int columns, bool execAsync)
    {
      try
      {
        bool b = await base.FetchDataRowAsync(statementId, columns, execAsync).ConfigureAwait(false);
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

    public override async Task CloseQueryAsync(MySqlConnection connection, int statementId, bool execAsync)
    {
      await base.CloseQueryAsync(connection, statementId, execAsync).ConfigureAwait(false);
      MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.QueryClosed, Resources.TraceQueryDone, driverId);
    }

    public override async Task<List<MySqlError>> ReportWarningsAsync(MySqlConnection connection, bool execAsync)
    {
      List<MySqlError> warnings = await base.ReportWarningsAsync(connection, execAsync).ConfigureAwait(false);

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
