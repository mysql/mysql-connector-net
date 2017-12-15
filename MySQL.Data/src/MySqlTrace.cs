// Copyright © 2009, 2016, Oracle and/or its affiliates. All rights reserved.
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
using System.Diagnostics;
using System.Linq;
using System.Reflection;


namespace MySql.Data.MySqlClient
{
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

    public static TraceListenerCollection Listeners { get; } = source.Listeners;

    public static SourceSwitch Switch
    {
      get { return source.Switch; }
      set { source.Switch = value; }
    }

    public static bool QueryAnalysisEnabled { get; set; }

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

  public enum UsageAdvisorWarningFlags
  {
    NoIndex = 1,
    BadIndex,
    SkippedRows,
    SkippedColumns,
    FieldConversion
  }

  public enum MySqlTraceEventType : int
  {
    ConnectionOpened = 1,
    ConnectionClosed,
    QueryOpened,
    ResultOpened,
    ResultClosed,
    QueryClosed,
    StatementPrepared,
    StatementExecuted,
    StatementClosed,
    NonQuery,
    UsageAdvisorWarning,
    Warning,
    Error,
    QueryNormalized
  }
}
