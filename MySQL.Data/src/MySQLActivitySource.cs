using System;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using MySql.Data.MySqlClient;

namespace MySql.Data
{
#if NET5_0_OR_GREATER
  static class MySQLActivitySource
  {
    static readonly ActivitySource Source;

    static MySQLActivitySource()
    {
      var assembly = typeof(MySQLActivitySource).Assembly;
      var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.0.0";
      Source = new("connector-net", version);
    }

    private static bool Active => Source.HasListeners();

    internal static Activity OpenConnection(MySqlConnectionStringBuilder settings)
    {
      return InternalOpenConnection(settings, "Connection");
    }

    internal static Activity OpenPooledConnection(MySqlConnectionStringBuilder settings)
    {
      return InternalOpenConnection(settings, "Connection (pooled)");
    }

    private static Activity InternalOpenConnection(MySqlConnectionStringBuilder settings, string name)
    {
      if (!Active) return null;

      var activity = Source.StartActivity(name, ActivityKind.Client);
      activity?.SetTag("net.transport", settings.ConnectionProtocol);
      activity?.SetTag("db.connection_string", settings.GetConnectionString(false));
      if (settings.ConnectionProtocol == MySqlConnectionProtocol.Tcp)
        activity?.SetTag("net.peer.port", settings.Port);

      return activity;
    }

    internal static void CloseConnection(Activity activity) 
    {
        activity?.SetTag("otel.status_code", "OK");
        activity?.Dispose();
    }

    internal static void SetException(Activity activity, Exception ex)
    {
      var tags = new ActivityTagsCollection
      {
        { "exception.type", ex.GetType().FullName },
        { "exception.message", ex.Message },
        { "exception.stacktrace", ex.ToString() },
      };

      var activityEvent = new ActivityEvent("exception", tags: tags);
      activity?.AddEvent(activityEvent);
      activity?.SetTag("otel.status_code", "ERROR");
      activity?.SetTag("otel.status_description", ex is MySqlException ? (ex as MySqlException).SqlState : ex.Message);
      activity?.Dispose();
    }


    internal static Activity CommandStart(MySqlCommand command)
    {
      if (!Active) return null;

      var settings = command.Connection.Settings;
      var activity = Activity.Current != null ? Source.StartActivity("SQL Statement", ActivityKind.Client, Activity.Current.Context) : Source.StartActivity("SQL Statement", ActivityKind.Client);

      // passing through this attribute will propagate the context into the server
      string query_attr = $"00-{Activity.Current.Context.TraceId}-{Activity.Current.Context.SpanId}-00";
      command.Attributes.SetAttribute("traceparent", query_attr);
      
      activity?.SetTag("db.system", "mysql");
      activity?.SetTag("db.name", command.Connection.Database);
      activity?.SetTag("db.user", command.Connection.Settings.UserID);
      activity?.SetTag("db.statement", command.OriginalCommandText);
      activity?.SetTag("thread.id", Thread.CurrentThread.ManagedThreadId);
      activity?.SetTag("thread.name", Thread.CurrentThread.Name);
      if (command.CommandType == CommandType.TableDirect)
        activity?.SetTag("db.sql.table", command.CommandText);
      return activity;
    }

    internal static void ReceivedFirstResponse(Activity activity)
    {
      var activityEvent = new ActivityEvent("first-packet-received");
      activity.AddEvent(activityEvent);
    }

    internal static void CommandStop(Activity activity)
    {
      activity?.SetTag("otel.status_code", "OK");
      activity?.Dispose();
    }
  }
#endif
}

