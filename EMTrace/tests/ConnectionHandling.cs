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
    public class ConnectionHandling : BaseTest
    {
        [Fact]
        public void OpenConnection()
        {
            // simulate some events
            OpenConnection(1, "server=localhost;uid=root;database=mydb", 1);
            Assert.Equal(1, wrapper.ConnectionStrings.Count);
        }

        [Fact]
        public void OpenMultipleConnections()
        {
            // simulate a series of open connection events
            for (int i=1; i < 5; i++)
                source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.ConnectionOpened, 
                    "no message needed", i, "server=localhost;uid=root;database=mydb", 1);
            Assert.Equal(4, wrapper.ConnectionStrings.Count);

            for (int i = 1; i < 5; i++)
                source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.ConnectionClosed, 
                    "no message needed", i);
            Assert.Equal(0, wrapper.ConnectionStrings.Count);
        }

        [Fact]
        public void OpenConnectionsToDifferentServers()
        {
            // simulate a series of open connection events
            for (int i = 1; i < 5; i++)
                source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.ConnectionOpened, 
                    "no message needed", i, 
                    String.Format("server=localhost{0};uid=root;database=mydb", i), 1);
            Assert.Equal(4, wrapper.ConnectionStrings.Count);

            for (int i = 1; i < 5; i++)
                source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.ConnectionClosed, 
                    "no message needed", i);
            Assert.Equal(0, wrapper.ConnectionStrings.Count);
        }

        [Fact]
        public void CloseConnection()
        {
            // simulate an open connection event
            source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.ConnectionOpened, 
                "no message needed", 1, "server=localhost;uid=root;database=mydb", 1);
            source.TraceEvent(TraceEventType.Information, (int)MySqlTraceEventType.ConnectionClosed, 
                "no message needed", 1);
            Assert.Equal(0, wrapper.ConnectionStrings.Count);
        }

        //[Fact]
        //public void NewQueryIsPickedUp()
        //{
        //    EMTraceListener tl = new EMTraceListener("test", 1);
        //    MySqlTraceQueryInfo qi = (MySqlTraceQueryInfo)CreateNewQuery();
        //    tl.Write(qi);

        //    System.Threading.Thread.Sleep(1000);

        //    // first make sure new queries queue is empty
        //    ICollection o = GetNewQueriesQueue(tl);
        //    Assert.Equal(0, o.Count);

        //    tl.Write(qi);
        //    tl.Write(qi);
        //    tl.Write(qi);
        //    tl.Write(qi);
        //    tl.Write(qi);
        //    tl.Write(qi);
        //    System.Threading.Thread.Sleep(250);
        //    Assert.Equal(0, o.Count);
        //}

        //[Fact]
        //public void ServersCreatedProperly1()
        //{
        //    EMTraceListener tl = new EMTraceListener("test", 1);

        //    for (int i = 0; i < 4; i++)
        //    {
        //        MySqlTraceQueryInfo q = (MySqlTraceQueryInfo)CreateNewQuery();
        //        q.ConnectionString = String.Format("server=host{0};database=test;uid=root", i);
        //        tl.Write(q);
        //    }

        //    System.Threading.Thread.Sleep(1000);

        //    // first make sure new queries queue is empty
        //    ICollection o = GetServerList(tl);
        //    Assert.Equal(4, o.Count);
        //}

        //[Fact]
        //public void ServersCreatedProperly2()
        //{
        //    EMTraceListener tl = new EMTraceListener("test", 1);

        //    for (int i = 0; i < 4; i++)
        //    {
        //        MySqlTraceQueryInfo q = (MySqlTraceQueryInfo)CreateNewQuery();
        //        q.ConnectionString = String.Format("server=host;database=test{0};uid=root", i);
        //        tl.Write(q);
        //    }

        //    System.Threading.Thread.Sleep(1000);

        //    // first make sure new queries queue is empty
        //    ICollection o = GetServerList(tl);
        //    Assert.Equal(4, o.Count);
        //}
    }
}
