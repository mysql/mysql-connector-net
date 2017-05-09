// Copyright (c) 2009-2010 Sun Microsystems, Inc., 2014 Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Data.Common;
using Xunit;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace MySql.EMTrace.Tests
{
    public class BaseTest : IDisposable , IClassFixture<DefaultFixture>
    {
        protected TraceSource source;
        protected EMTraceListener tl;
        protected ListenerWrapper wrapper;
        protected const string emHost = "test";
        protected int postInterval = 60;

        public SetUpTests SetupData { get; protected set; }

        public BaseTest() : this(emHost)
        {
        }

        protected BaseTest(string host)
        {
          tl = new EMTraceListener(host, "testuser", "testpass", postInterval);
          wrapper = new ListenerWrapper(tl);
          source = new TraceSource("mysql");
          source.Switch.Level = SourceLevels.All;
          source.Listeners.Add(tl);
        }


        protected ICollection GetNewQueriesQueue(EMTraceListener tl)
        {
            FieldInfo fi = tl.GetType().GetField("newQueries", BindingFlags.Instance | BindingFlags.NonPublic);
            return fi.GetValue(tl) as ICollection;
        }

        protected void OpenConnection(int driverId, string connectString, int threadId)
        {
            source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.ConnectionOpened, 
                "", driverId, connectString, threadId);
        }

        protected void CloseConnection(long driverId)
        {
            source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.ConnectionClosed,
                "", driverId);
        }

        protected void NonQuery(long driverId, string dbName)
        {
            source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.NonQuery,
                "", driverId, dbName);
        }

        protected void OpenQuery(long driverId, int threadId, string sql)
        {
            source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.QueryOpened,
                "", driverId, threadId, sql);
        }

        protected void QueryNormalized(long driverId, int threadId, string sql)
        {
            source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.QueryNormalized,
                "", driverId, threadId, sql);
        }

        protected void OpenResult(long driverId, int fieldCount, int affectedRows, int insertedId)
        {
            source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.ResultOpened,
                "", driverId, fieldCount, affectedRows, insertedId);
        }

        protected void CloseResult(long driverId, int totalRows, int skippedRows, int sizeInBytes)
        {
            source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.ResultClosed,
                "", driverId, totalRows, skippedRows, sizeInBytes);
        }

        protected void CloseQuery(long driverId)
        {
            source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.QueryClosed, "", driverId);
        }

        protected void Warning(long driverId)
        {
            source.TraceEvent(TraceEventType.Warning, (int)MySqlTraceEventType.Warning, "", driverId, 0, 0, "");
        }

        protected void UAWarning(long driverId, UsageAdvisorWarningFlags flags)
        {
            source.TraceEvent(TraceEventType.Warning, (int)MySqlTraceEventType.UsageAdvisorWarning, "", 
                driverId, flags);
        }

        protected void Error(long driverId)
        {
            source.TraceEvent(TraceEventType.Error, (int)MySqlTraceEventType.Error, "",
                driverId, 0, 0, "");
        }

        protected void IssueSimpleSelect(string sql, int numCols, int numRows, int numSkipped, int sizeInBytes, int warnings)
        {
            IssueSelect(sql, new int[] { numCols }, new int[] { numRows }, new int[] { numSkipped },
                new int[] { sizeInBytes }, new int[] { warnings });
        }

        protected void IssueSelect(string sql, int[] numCols, int[] numRows, int[] numSkipped, int[] sizeInBytes, int[] warnings)
        {
            OpenQuery(1, 1, sql);
            for (int i = 0; i < numCols.Length; i++)
            {
                OpenResult(1, numCols[i], -1, -1);
                for (int x = 0; x < warnings[i]; x++)
                    Warning(1);
                CloseResult(1, numRows[i], numSkipped[i], sizeInBytes[i]);
            }
            CloseQuery(1);
        }

        public virtual void Dispose()
        {
        }

        public virtual void SetFixture(SetUpTests data)
        {
          SetupData = data;
        }
    }
}
