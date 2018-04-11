// Copyright © 2009, 2018, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;


namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Traces information about the client execution.
  /// </summary>
  public class MySqlTrace
  {
    private static TraceSource source = new TraceSource("mysql");

    static MySqlTrace()
    {
      foreach (TraceListener listener in source.Listeners.Cast<TraceListener>().Where(listener => listener.GetType().ToString().Contains("MySql.EMTrace.EMTraceListener")))
      {
        QueryAnalysisEnabled = true;
        break;
      }
    }

    /// <summary>
    /// Gets the list of trace listeners.
    /// </summary>
    public static TraceListenerCollection Listeners { get; } = source.Listeners;

    /// <summary>
    /// Gets or sets the switch to control tracing and debugging.
    /// </summary>
    public static SourceSwitch Switch
    {
      get { return source.Switch; }
      set { source.Switch = value; }
    }

    /// <summary>
    /// Gets or sets a flag indicating if query analysis is enabled.
    /// </summary>
    public static bool QueryAnalysisEnabled { get; set; }

    /// <summary>
    /// Enables query analysis.
    /// </summary>
    /// <param name="host">The host on which to enable query analysis.</param>
    /// <param name="postInterval">The interval of time for logging trace info.</param>
    public static void EnableQueryAnalyzer(string host, int postInterval)
    {
      if (QueryAnalysisEnabled) return;
      // create a EMTraceListener and add it to our source
      TraceListener l = (TraceListener)Activator.CreateInstance(Type.GetType("MySql.EMTrace.EMTraceListener"), host, postInterval);

      if (l == null)
        throw new MySqlException(Resources.UnableToEnableQueryAnalysis);

      source.Listeners.Add(l);
      Switch.Level = SourceLevels.All;
    }

    /// <summary>
    /// Disables query analysis.
    /// </summary>
    public static void DisableQueryAnalyzer()
    {
      QueryAnalysisEnabled = false;
      foreach (TraceListener l in from TraceListener l in Source.Listeners where l.GetType().ToString().Contains("EMTraceListener") select l)
      {
        source.Listeners.Remove(l);
        break;
      }
    }

    internal static TraceSource Source
    {
      get 
        { 
            return source; 
        }
    }

    internal static void LogInformation(int id, string msg)
    {
      Source.TraceEvent(TraceEventType.Information, id, msg, MySqlTraceEventType.NonQuery, -1);
      Trace.TraceInformation(msg);
    }

    internal static void LogWarning(int id, string msg)
    {
      Source.TraceEvent(TraceEventType.Warning, id, msg, MySqlTraceEventType.NonQuery, -1);
      Trace.TraceWarning(msg);
    }

    internal static void LogError(int id, string msg)
    {
      Source.TraceEvent(TraceEventType.Error, id, msg, MySqlTraceEventType.NonQuery, -1);
      Trace.TraceError(msg);
    }

    internal static void TraceEvent(TraceEventType eventType,
        MySqlTraceEventType mysqlEventType, string msgFormat, params object[] args)
    {
      Source.TraceEvent(eventType, (int)mysqlEventType, msgFormat, args);
    }
  }

  /// <summary>
  /// Specifies the types of warning flags.
  /// </summary>
  public enum UsageAdvisorWarningFlags
  {
    /// <summary>
    /// No index exists.
    /// </summary>
    NoIndex = 1,
    /// <summary>
    /// Bad index exists.
    /// </summary>
    BadIndex,
    /// <summary>
    /// Rows have been excluded from the result.
    /// </summary>
    SkippedRows,
    /// <summary>
    /// Columns have been excluded from the result.
    /// </summary>
    SkippedColumns,
    /// <summary>
    /// Type conversions took place.
    /// </summary>
    FieldConversion
  }

  /// <summary>
  /// Specifies the event that triggered the trace.
  /// </summary>
  public enum MySqlTraceEventType : int
  {
    /// <summary>
    /// A connection has been opened.
    /// </summary>
    ConnectionOpened = 1,
    /// <summary>
    /// A connection has been closed.
    /// </summary>
    ConnectionClosed,
    /// <summary>
    /// A query has been executed.
    /// </summary>
    QueryOpened,
    /// <summary>
    /// Data has been retrieved from the resultset.
    /// </summary>
    ResultOpened,
    /// <summary>
    /// Data retrieval has ended.
    /// </summary>
    ResultClosed,
    /// <summary>
    /// Query execution has ended.
    /// </summary>
    QueryClosed,
    /// <summary>
    /// The statement to be executed has been created.
    /// </summary>
    StatementPrepared,
    /// <summary>
    /// The statement has been executed.
    /// </summary>
    StatementExecuted,
    /// <summary>
    /// The statement is no longer required.
    /// </summary>
    StatementClosed,
    /// <summary>
    /// The query provided is of a nonquery type.
    /// </summary>
    NonQuery,
    /// <summary>
    /// Usage advisor warnings have been requested.
    /// </summary>
    UsageAdvisorWarning,
    /// <summary>
    /// Noncritical problem.
    /// </summary>
    Warning,
    /// <summary>
    /// An error has been raised during data retrieval.
    /// </summary>
    Error,
    /// <summary>
    /// The query has been normalized.
    /// </summary>
    QueryNormalized
  }
}
