// Copyright (c) 2009-2010 Sun Microsystems, Inc.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Timers;
using System.Data.Common;
using System.Threading;
using System.Net;

namespace MySql.EMTrace
{
    public class EMTraceListener : TraceListener
    {
        private ServerAggregationFactory serverFactory;
        private Dictionary<long, string> connectionStrings = new Dictionary<long, string>();
        private Dictionary<string, string> preparedStatements = new Dictionary<string, string>();
        private Dictionary<long, MySqlTraceQuery> inProcessQueries = new Dictionary<long, MySqlTraceQuery>();
        private Dictionary<long, MySqlTraceQuery> parentQueries = new Dictionary<long, MySqlTraceQuery>();

        private BackgroundWorker processingThread = new BackgroundWorker();
        private Queue<MySqlTraceQuery> newQueries = new Queue<MySqlTraceQuery> ();
        private Dictionary<string, QueryAggregation> queries = new Dictionary<string, QueryAggregation>();
        private System.Timers.Timer pushTimer;
        private AutoResetEvent newQueryEvent = new AutoResetEvent(false);
        private static string callingHost;

        public EMTraceListener()
        {
            if (callingHost == null)
                callingHost = Dns.GetHostName();
        }

        public EMTraceListener(string host, string userId, string password, int postInterval) : this()
        {
            Attributes["Host"] = host;
            Attributes["UserId"] = userId;
            Attributes["Password"] = password;
            Attributes["PostInterval"] = postInterval.ToString();
        }

        private void Init()
        {
            Trace.WriteLine("EMTraceListener.Init called");

            if (!ClassFactory.LoadAndCheckFactory()) return;

            string emHost = GetAttribute("Host", true);
            string userId = GetAttribute("UserId", true);
            string pwd = GetAttribute("Password", true);

            int postInterval = 60;
            if (Attributes.ContainsKey("PostInterval"))
                postInterval = Int32.Parse(Attributes["PostInterval"]);

            Trace.WriteLine(String.Format("EMTrace config data:  host={0}, userid={1}, password={2}, post interval={3}",
                emHost, userId, pwd, postInterval));

            serverFactory = new ServerAggregationFactory(emHost, postInterval, userId, pwd);

            pushTimer = new System.Timers.Timer(postInterval * 1000);
            pushTimer.AutoReset = true;
            pushTimer.Enabled = true;

            processingThread.DoWork += new DoWorkEventHandler(processingThread_DoWork);
            processingThread.RunWorkerAsync(null);
            pushTimer.Elapsed += new ElapsedEventHandler(pushTimer_Elapsed);
            pushTimer.Start();

            Trace.WriteLine("EMTraceListener.Init finished");
        }

        protected override string[] GetSupportedAttributes()
        {
            string[] attr = new string[4] { "Host", "PostInterval", "UserId", "Password" };
            return attr;
        }

        void pushTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Trace.WriteLine("posting timer fired");
            (sender as System.Timers.Timer).Stop();
            try
            {
                foreach (ServerAggregation sa in serverFactory.Servers.Values)
                    sa.DecrementPostCounter((int)(pushTimer.Interval / 1000));
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Caught exception posting trace data: " + ex.Message);
            }
            finally
            {
                (sender as System.Timers.Timer).Start();
            }
        }

        private void processingThread_DoWork(object sender, DoWorkEventArgs e)
        {
            Trace.WriteLine("EMTraceListener worker thread starting up");

            List<MySqlTraceQuery> queriesToProcess = new List<MySqlTraceQuery>();

            BackgroundWorker worker = sender as BackgroundWorker;
            while (newQueryEvent.WaitOne())
            {
                Trace.WriteLine("New query event released. At least one query waiting to be aggregated");
                if (worker.CancellationPending) break;
                queriesToProcess.Clear();
                lock (newQueries)
                {
                    while (newQueries.Count > 0)
                        queriesToProcess.Add(newQueries.Dequeue());
                }
                // now we have freed up the lock for more queries to be added but
                // we are free to spend some time now processing
                // The AddNewQuery method might take some time.  This is why we
                // make a separate list first
                foreach (MySqlTraceQuery qi in queriesToProcess)
                {
                    ServerAggregation sa = serverFactory.GetServer(qi.ConnectionString);
                    if (sa.Enabled)
                    {
                        Trace.WriteLine("Adding new query to server");
                        sa.AddNewQuery(qi);
                    }
                }
            }

            Trace.WriteLine("EMTraceListener worker thread exiting");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                processingThread.CancelAsync();
                newQueryEvent.Set();
                // wait for thread to stop
            }
            base.Dispose(disposing);
        } 

        public override void TraceEvent(TraceEventCache eventCache, string source,
            TraceEventType eventType, int id, string format, params object[] args)
        {
            Debug.Assert(args != null && args.Length >= 1);

            if (serverFactory == null)
                Init();

            // if the source is not mysql then we pass it through
            if (String.Compare(source, "mysql", true) != 0)
                base.TraceEvent(eventCache, source, eventType, id, format, args);

            MySqlTraceEventType traceEventType = (MySqlTraceEventType)id;
            long connectionId = Convert.ToInt64(args[0]);

            // if our connection strings hash doens't have this base key
            // then 
            if (traceEventType != MySqlTraceEventType.ConnectionOpened &&
                !connectionStrings.ContainsKey(connectionId))
                return;

            switch (traceEventType)
            {
                case MySqlTraceEventType.QueryOpened:
                    QueryOpened(connectionId, eventCache, false, args); 
                    break;
                case MySqlTraceEventType.QueryNormalized:
                    QueryNormalized(connectionId, args);
                    break;
                case MySqlTraceEventType.QueryClosed:
                    QueryClosed(connectionId, eventCache, args); 
                    break;
                case MySqlTraceEventType.StatementPrepared:
                    StatementPrepared(connectionId, args);
                    break;
                case MySqlTraceEventType.StatementExecuted:
                    StatementPrepared(connectionId, eventCache, args);
                    break;
                case MySqlTraceEventType.StatementClosed:
                    StatementPrepared(connectionId, args);
                    break;
                case MySqlTraceEventType.ResultOpened:
                    ResultOpened(connectionId, eventCache.Timestamp, args);
                    break;
                case MySqlTraceEventType.ResultClosed:
                    ResultClosed(connectionId, args);
                    break;
                case MySqlTraceEventType.ConnectionOpened:
                    ConnectionOpened(connectionId, args); 
                    break;
                case MySqlTraceEventType.ConnectionClosed:
                    ConnectionClosed(connectionId, args); 
                    break;
                case MySqlTraceEventType.UsageAdvisorWarning:
                    UsageAdvisorWarning(connectionId, args);
                    break;
                case MySqlTraceEventType.Warning:
                    Warning(connectionId, args);
                    break;
                case MySqlTraceEventType.Error:
                    Error(connectionId, args);
                    break;
                case MySqlTraceEventType.NonQuery:
                    NonQuery(connectionId, args);
                    break;
            }
        }

        private void NonQuery(long connectionId, params object[] args)
        {
            Debug.Assert(args.Length >= 2);

            string connStr = GetConnectionString(connectionId);
            DbConnectionStringBuilder builder = ClassFactory.CreateConnectionStringBuilder();
            builder.ConnectionString = connStr;
            builder["database"] = args[1];
            SetConnectionString(connectionId, builder.ConnectionString);
        }

        private void Warning(long connectionId, params object[] args)
        {
            lock (inProcessQueries)
            {
                MySqlTraceQuery q = null;
                if (inProcessQueries.ContainsKey(connectionId))
                    q = inProcessQueries[connectionId];
                else if (parentQueries.ContainsKey(connectionId))
                    q = parentQueries[connectionId];
                if (q != null)
                    q.Warnings++;
            }
        }

        private void Error(long connectionId, params object[] args)
        {
            lock (inProcessQueries)
            {
                MySqlTraceQuery q = null;
                if (inProcessQueries.ContainsKey(connectionId))
                    q = inProcessQueries[connectionId];
                else if (parentQueries.ContainsKey(connectionId))
                    q = parentQueries[connectionId];
                if (q != null)
                    q.Errors++;
            }
        }

        private void UsageAdvisorWarning(long connectionId, params object[] args)
        {
            Debug.Assert(args.Length >= 2);
            UsageAdvisorWarningFlags flag = (UsageAdvisorWarningFlags)args[1];

            lock (inProcessQueries)
            {
                MySqlTraceQuery q = inProcessQueries[connectionId];
                Debug.Assert(q != null);
                Debug.Assert(q.ActiveResult != null);
                if (flag == UsageAdvisorWarningFlags.NoIndex)
                    q.ActiveResult.NoIndex = true;
                else if (flag == UsageAdvisorWarningFlags.BadIndex)
                    q.ActiveResult.BadIndex = true;
            }
        }

        private void QueryOpened(long connectionId, TraceEventCache eventCache, bool prepared, 
            params object[] args)
        {
            Debug.Assert(args.Length == 3);

            MySqlTraceQuery q = new MySqlTraceQuery();
            q.ConnectionId = (int)args[1];
            q.ConnectionString = String.Copy(GetConnectionString(connectionId));
            q.CommandText = args[2] as string;
            if (prepared)
                q.FullCommandText = q.CommandText;
            q.CallStack = eventCache.Callstack;
            q.TimeOfQuery = eventCache.DateTime;
            q.StartTimestamp = eventCache.Timestamp;
            q.CallingHost = callingHost;
            lock (inProcessQueries)
            {
                if (inProcessQueries.ContainsKey(connectionId))
                {
                    q.Parent = inProcessQueries[connectionId];
                    parentQueries[connectionId] = inProcessQueries[connectionId];
                    inProcessQueries.Remove(connectionId);
                }
                inProcessQueries[connectionId] = q;
            }
        }

        private void QueryNormalized(long connectionId, params object[] args)
        {
            Debug.Assert(args.Length == 3);

            MySqlTraceQuery q = null;
            lock (inProcessQueries)
            {
                if (inProcessQueries.ContainsKey(connectionId))
                {
                    q = inProcessQueries[connectionId];
                }
                else if (parentQueries.ContainsKey(connectionId))
                {
                    q = parentQueries[connectionId];
                }
                Debug.Assert(q != null);
                q.FullCommandText = q.CommandText;
                q.CommandText = args[2] as string;
            }
        }

        private void QueryClosed(long connectionId, TraceEventCache eventCache, params object[] args)
        {
            Debug.Assert(args.Length >= 1);

            MySqlTraceQuery q = null;
            lock (inProcessQueries)
            {
                if (inProcessQueries.ContainsKey(connectionId))
                {
                    q = inProcessQueries[connectionId];
                    inProcessQueries.Remove(connectionId);
                }
                else if (parentQueries.ContainsKey(connectionId))
                {
                    q = parentQueries[connectionId];
                    parentQueries.Remove(connectionId);
                }
                if (q.StopTimestamp == 0)
                    q.StopTimestamp = eventCache.Timestamp;
                Debug.Assert(q != null);
            }
            lock (newQueries)
            {
                newQueries.Enqueue(q);
            }
            newQueryEvent.Set();
        }

        private void StatementPrepared(long connectionId, params object[] args)
        {
            Debug.Assert(args.Length >= 3);

            string key = String.Format("{0}-{1}", connectionId, args[2]);
            lock (preparedStatements)
            {
                preparedStatements.Add(key, (string)args[1]);
            }
        }

        private void StatementExecuted(long connectionId, TraceEventCache eventCache, params object[] args)
        {
            Debug.Assert(args.Length >= 3);

            string key = String.Format("{0}-{1}", connectionId, args[1]);
            lock (preparedStatements)
            {
                string sql = preparedStatements[key];
                int threadId = (int)args[2];
                args[1] = threadId;
                args[2] = sql;
                QueryOpened(connectionId, eventCache, true, args);
            }
        }

        private void StatementClosed(long connectionId, params object[] args)
        {
            Debug.Assert(args.Length >= 2);

            string key = String.Format("{0}-{1}", connectionId, args[1]);
            lock (preparedStatements)
            {
                preparedStatements.Remove(key);
            }
        }

        private void ResultOpened(long connectionId, long timestamp, params object[] args)
        {
            Debug.Assert(args.Length == 4);

            lock (inProcessQueries)
            {
                MySqlTraceQuery q = inProcessQueries[connectionId];
                Debug.Assert(q != null);
                if (q.StopTimestamp == 0)
                    q.StopTimestamp = timestamp;
                MySqlTraceQueryResult rs = new MySqlTraceQueryResult();
                rs.RowsChanged = (int)args[2];
                rs.InsertedId = Convert.ToInt64(args[3]);
                q.ActiveResult = rs;
            }
        }

        private void ResultClosed(long connectionId, params object[] args)
        {
            Debug.Assert(args.Length == 4);

            lock (inProcessQueries)
            {
                MySqlTraceQuery q = inProcessQueries[connectionId];
                Debug.Assert(q != null);
                Debug.Assert(q.ActiveResult != null);
                q.ActiveResult.RowsRead = (int)args[1];
                q.ActiveResult.RowsSkipped = (int)args[2];
                q.ActiveResult.SizeInBytes = (int)args[3];
                q.Results.Add(q.ActiveResult);
                q.ActiveResult = null;
            }
        }

        private void ConnectionOpened(long connectionId, params object[] args)
        {
            Debug.Assert(args.Length >= 2);
            string connectionString = args[1] as string;
            SetConnectionString(connectionId, connectionString);
        }

        /// <summary>
        /// When we close a connection then we need to remove all connection strings and possible
        /// in process queries that use that same base key
        /// </summary>
        /// <param name="baseKey"></param>
        /// <param name="args"></param>
        private void ConnectionClosed(long connectionId, params object[] args)
        {
            // first remove our connection string
            connectionStrings.Remove(connectionId);

            // then remove any in process queries that may be using this mysql thread
            List<long> keysToDelete = new List<long>();
            foreach (long key in inProcessQueries.Keys)
                if (key == connectionId)
                    keysToDelete.Add(key);
            foreach (long keyToDelete in keysToDelete)
                inProcessQueries.Remove(keyToDelete);
        }

        private string GetConnectionString(long connectionId)
        {
            lock (connectionStrings)
            {
                return connectionStrings[connectionId];
            }
        }

        private void SetConnectionString(long connectionId, string value)
        {
            lock (connectionStrings)
            {
                connectionStrings[connectionId] = value;
            }
        }

        private string GetAttribute(string attribute, bool required)
        {
            if (required && !Attributes.ContainsKey(attribute))
                throw new InvalidOperationException(
                    String.Format("EMTraceListener requires the {0} attribute", attribute));
            return Attributes[attribute];
        }

        #region Abstracts that we had to implement (but do not need)

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
        }

        #endregion
    }
}
