// Copyright © 2009, 2016, Oracle and/or its affiliates. All rights reserved.
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
