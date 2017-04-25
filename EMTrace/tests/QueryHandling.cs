// Copyright (c) 2009-2010 Sun Microsystems, Inc., 2014 Oracle and/or its affiliates. All rights reserved.
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
    public class QueryHandling : BaseTest
    {
        [Fact]
        public void StartQuery()
        {
            OpenConnection(1, "server=localhost;uid=root;database=mydb", 1);
            OpenQuery(1, 1, "SELECT 1");

            Assert.Equal(1, wrapper.ConnectionStrings.Count);
            Assert.Equal(1, wrapper.InProcessQueries.Count);
        }

        [Fact]
        public void StartAndFinishQuery()
        {
            OpenConnection(1, "server=localhost;uid=root;database=mydb", 1);
            OpenQuery(1, 1, "SELECT 1");
            CloseQuery(1);

            Assert.Equal(1, wrapper.ConnectionStrings.Count);
            Assert.Equal(0, wrapper.InProcessQueries.Count);
            wrapper.ServerFactory.WaitForNumberOfServers(1, 2);
            Assert.Equal(1, wrapper.ServerFactory.Servers.Count);
            ServerAggregationWrapper server = wrapper.ServerFactory.GetServer(0);
            server.WaitForNumberOfQueries(1, 2);
            Assert.Equal(1, server.Queries.Count);
        }

        [Fact]
        public void SetDatabase()
        {
            OpenConnection(1, "server=localhost;uid=root;database=mydb", 1);
            OpenQuery(1, 1, "SELECT 1");
            CloseQuery(1);

            Assert.Equal(1, wrapper.ConnectionStrings.Count);
            Assert.Equal(0, wrapper.InProcessQueries.Count);
            wrapper.ServerFactory.WaitForNumberOfServers(1, 2);
            Assert.Equal(1, wrapper.ServerFactory.Servers.Count);
            ServerAggregationWrapper server = wrapper.ServerFactory.GetServer(0);
            server.WaitForNumberOfQueries(1, 2);
            Assert.Equal(1, server.Queries.Count);
            QueryAggregationWrapper q = server.GetQuery(0);
            q.WaitForSummary(2);
            Assert.Equal("mydb", q.Summary.database);
            
            NonQuery(1, "newdb");

            OpenQuery(1, 1, "SELECT 2");
            CloseQuery(1);

            Assert.Equal(1, wrapper.ConnectionStrings.Count);
            Assert.Equal(0, wrapper.InProcessQueries.Count);
            wrapper.ServerFactory.WaitForNumberOfServers(2, 2);
            Assert.Equal(2, wrapper.ServerFactory.Servers.Count);


            server = wrapper.ServerFactory.GetServer(1);
            server.WaitForNumberOfQueries(1, 2);
            Assert.Equal(1, server.Queries.Count);
            q = server.GetQuery(0);
            q.WaitForSummary(2);
            Assert.Equal("newdb", q.Summary.database);
        }
    }
}
