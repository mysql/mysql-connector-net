// Copyright (c) 2009 Sun Microsystems, Inc., 2014 Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace MySql.EMTrace.Tests
{
    public class QueryPosting : BaseTest
    {
        public QueryPosting()
        {
            postInterval = 1;
        }

        [Fact]
        public void SingleSelect()
        {
            TraceCollector collector = new TraceCollector();
            Trace.Listeners.Add(collector);

            // simulate an open connection event
            source.TraceEvent(TraceEventType.Information, 1, "no message needed",
                MySqlTraceEventType.ConnectionOpened, "server=localhost;uid=root;database=mydb");
            IssueSimpleSelect("SELECT 1", 1, 1, 1, 25, 0);

            Assert.Equal(1, wrapper.ConnectionStrings.Count);
            Assert.Equal(0, wrapper.InProcessQueries.Count);
            wrapper.ServerFactory.WaitForNumberOfServers(1, 2);
            Assert.Equal(1, wrapper.ServerFactory.Servers.Count);

            System.Threading.Thread.Sleep(2000);
        }

        [Fact]
        public void MultipleSelects()
        {
            TraceCollector collector = new TraceCollector();
            Trace.Listeners.Add(collector);

            // simulate an open connection event
            source.TraceEvent(TraceEventType.Information, 1, "no message needed",
                MySqlTraceEventType.ConnectionOpened, "server=localhost;uid=root;database=mydb");
            IssueSimpleSelect("SELECT * FROM mytable", 4, 20, 5, 220, 0);
            IssueSimpleSelect("SELECT * FROM mytable", 4, 25, 8, 245, 0);
            IssueSimpleSelect("SELECT * FROM mytable", 4, 30, 4, 287, 0);
            IssueSimpleSelect("SELECT * FROM mytable", 4, 35, 6, 317, 0);
            IssueSimpleSelect("SELECT * FROM mytable", 4, 40, 11, 356, 0);

            Assert.Equal(1, wrapper.ConnectionStrings.Count);
            Assert.Equal(0, wrapper.InProcessQueries.Count);
            wrapper.ServerFactory.WaitForNumberOfServers(1, 1);
            Assert.Equal(1, wrapper.ServerFactory.Servers.Count);
            System.Threading.Thread.Sleep(250);

        }

        [Fact]
        public void SingleSelectWithMultipleResults()
        {
            TraceCollector collector = new TraceCollector();
            Trace.Listeners.Add(collector);

            // simulate an open connection event
            source.TraceEvent(TraceEventType.Information, 1, "no message needed",
                MySqlTraceEventType.ConnectionOpened, "server=localhost;uid=root;database=mydb");
            IssueSelect("call spTest1()", 
                new int[] { 2, 6, 8}, new int[] { 10, 55, 34}, new int[] { 10, 15, 20 }, 
                new int[] { 200, 250, 312 }, new int[] { 0,0,0 });

            Assert.Equal(1, wrapper.ConnectionStrings.Count);
            Assert.Equal(0, wrapper.InProcessQueries.Count);
            wrapper.ServerFactory.WaitForNumberOfServers(1, 2);
        }
    }
}
