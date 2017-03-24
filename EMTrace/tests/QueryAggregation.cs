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
    public class QueryAggregation : BaseTest
    {
        [Fact]
        public void SingleSelectSummaryAndWorstCase()
        {
            // simulate some events
            OpenConnection(1, "server=localhost;uid=root;database=mydb", 1);
            IssueSimpleSelect("SELECT 1", 1, 1, 1, 76, 0);
            CloseConnection(1);

            Assert.Equal(0, wrapper.InProcessQueries.Count);
            wrapper.ServerFactory.WaitForNumberOfServers(1, 1);

            ServerAggregationWrapper server = wrapper.ServerFactory.GetServer(0);
            server.WaitForNumberOfQueries(1, 1);

            QueryAggregationWrapper qw = server.GetQuery(0);
            qw.WaitForSummary(2);
            SummaryReportWrapper summary = qw.Summary;
            string s = summary.ToString();
            Assert.True(s.Contains("\"text\": \"SELECT ?\""));
            Assert.True(s.Contains("\"database\": \"mydb\""));
            Assert.True(s.Contains("\"min_rows\": \"1\""));
            Assert.True(s.Contains("\"max_rows\": \"1\""));
            Assert.Equal(76, summary.bytes);
            Assert.Equal(76, summary.max_bytes);
            Assert.Equal(76, summary.min_bytes);

            WorstCaseReportWrapper worst = qw.WorstCase;
            s = worst.ToString();
            Assert.True(s.Contains("\"host_to\": \"localhost\""));
            Assert.True(s.Contains("\"user\": \"root\""));
            Assert.True(s.Contains("\"rows\": \"1\""));
            Assert.True(s.Contains("\"text\": \"SELECT ?\""));
            Assert.True(s.Contains("\"connection_id\": \"1\""));
        }

        [Fact]
        public void MultipleSelects()
        {
            // simulate some events
            OpenConnection(1, "server=localhost;uid=root;database=mydb", 1);
            IssueSimpleSelect("SELECT * FROM mytable", 4, 20, 5, 220, 0);
            IssueSimpleSelect("SELECT * FROM mytable", 4, 25, 8, 245, 0);
            IssueSimpleSelect("SELECT * FROM mytable", 4, 30, 4, 287, 0);
            IssueSimpleSelect("SELECT * FROM mytable", 4, 35, 6, 317, 0);
            IssueSimpleSelect("SELECT * FROM mytable", 4, 40, 11, 356, 0);
            CloseConnection(1);

            Assert.Equal(0, wrapper.InProcessQueries.Count);
            wrapper.ServerFactory.WaitForNumberOfServers(1, 5);

            ServerAggregationWrapper server = wrapper.ServerFactory.GetServer(0);
            server.WaitForNumberOfQueries(1, 5);

            QueryAggregationWrapper qw = server.GetQuery(0);
            qw.WaitForNumberOfAggregates(5, 10);
            qw.WaitForSummary(2);
            SummaryReportWrapper summary = qw.Summary;
            string s = summary.ToString();
            Assert.Equal(1425, summary.bytes);
            Assert.Equal(220, summary.min_bytes);
            Assert.Equal(356, summary.max_bytes);

            Assert.Equal(150, summary.rows);
            Assert.Equal(20, summary.min_rows);
            Assert.Equal(40, summary.max_rows);

            Assert.True(s.Contains("\"text\": \"SELECT * FROM mytable\""));
            Assert.True(s.Contains("\"database\": \"mydb\""));
            Assert.True(s.Contains("\"min_rows\": \"20\""));
            Assert.True(s.Contains("\"max_rows\": \"40\""));
        }

        [Fact]
        public void SingleSelectWithMultipleResults()
        {
            // simulate some events
            OpenConnection(1, "server=localhost;uid=root;database=mydb", 1);
            IssueSelect("call spTest1()", 
                new int[] { 2, 6, 8}, new int[] { 10, 55, 34}, new int[] { 10, 15, 20 }, 
                new int[] { 200, 250, 312 }, new int[] { 0, 0, 0 });
            CloseConnection(1);

            Assert.Equal(0, wrapper.InProcessQueries.Count);
            wrapper.ServerFactory.WaitForNumberOfServers(1, 1);

            ServerAggregationWrapper server = wrapper.ServerFactory.GetServer(0);
            server.WaitForNumberOfQueries(1, 1);

            QueryAggregationWrapper qw = server.GetQuery(0);
            qw.WaitForSummary(2);
            SummaryReportWrapper summary = qw.Summary;
            string s = summary.ToString();

            Assert.True(summary.min_exec_time == summary.max_exec_time);
            Assert.True(summary.max_rows == summary.min_rows);
            Assert.Equal("CALL", summary.query_type);
            Assert.Equal(1, summary.count);
            Assert.True(s.Contains("\"text\": \"CALL spTest1()\""));
            Assert.True(s.Contains("\"database\": \"mydb\""));
            Assert.True(s.Contains("\"min_rows\": \"99\""));
            Assert.True(s.Contains("\"max_rows\": \"99\""));
        }

        [Fact]
        public void NoIndex()
        {
            // simulate some events
            OpenConnection(1, "server=localhost;uid=root;database=mydb", 1);
            OpenQuery(1, 1, "SELECT 1");
            OpenResult(1, 1, -1, -1);
            UAWarning(1, UsageAdvisorWarningFlags.NoIndex);
            CloseResult(1, 1, 1, 76);
            OpenResult(1, 1, -1, -1);
            UAWarning(1, UsageAdvisorWarningFlags.NoIndex);
            CloseResult(1, 1, 1, 76);
            CloseQuery(1);
            CloseConnection(1);

            Assert.Equal(0, wrapper.InProcessQueries.Count);
            wrapper.ServerFactory.WaitForNumberOfServers(1, 5);

            ServerAggregationWrapper server = wrapper.ServerFactory.GetServer(0);
            server.WaitForNumberOfQueries(1, 5);

            QueryAggregationWrapper qw = server.GetQuery(0);

            qw.WaitForSummary(2);
            SummaryReportWrapper summary = qw.Summary;
            Assert.Equal(2, summary.no_index_used);
            
            qw.WaitForWorstCase(2);
            WorstCaseReportWrapper worst = qw.WorstCase;
            Assert.Equal(2, worst.no_index_used);
        }

        [Fact]
        public void BadIndex()
        {
            // simulate some events
            OpenConnection(1, "server=localhost;uid=root;database=mydb", 1);
            OpenQuery(1, 1, "SELECT 1");
            OpenResult(1, 1, -1, -1);
            UAWarning(1, UsageAdvisorWarningFlags.BadIndex);
            CloseResult(1, 1, 1, 76);
            CloseQuery(1);
            CloseConnection(1);

            Assert.Equal(0, wrapper.InProcessQueries.Count);
            wrapper.ServerFactory.WaitForNumberOfServers(1, 5);

            ServerAggregationWrapper server = wrapper.ServerFactory.GetServer(0);
            server.WaitForNumberOfQueries(1, 5);
            QueryAggregationWrapper qw = server.GetQuery(0);
            
            qw.WaitForSummary(2);
            SummaryReportWrapper summary = qw.Summary;
            Assert.Equal(1, summary.no_good_index_used);

            qw.WaitForWorstCase(2);
            WorstCaseReportWrapper worst = qw.WorstCase;
            Assert.Equal(1, worst.no_good_index_used);
        }

        [Fact]
        public void WarningsAndErrors()
        {
            // simulate some events
            OpenConnection(1, "server=localhost;uid=root;database=mydb", 1);

            OpenQuery(1, 1, "SELECT 1");
            OpenResult(1, 1, -1, -1);
            CloseResult(1, 1, 1, 76);
            OpenQuery(1, 1, "SHOW WARNINGS");
            OpenResult(1, 3, -1, -1);
            CloseResult(1, 1, 0, 55);
            CloseQuery(1);
            Warning(1);
            Warning(1);
            Warning(1);
            Error(1);
            CloseQuery(1);
            CloseConnection(1);

            Assert.Equal(0, wrapper.InProcessQueries.Count);
            wrapper.ServerFactory.WaitForNumberOfServers(1, 5);

            ServerAggregationWrapper server = wrapper.ServerFactory.GetServer(0);
            server.WaitForNumberOfQueries(2, 5);

            QueryAggregationWrapper qw = server.GetQuery(1);
            qw.WaitForSummary(2);
            SummaryReportWrapper summary = qw.Summary;
            Assert.Equal(3, summary.warnings);
            Assert.Equal(1, summary.errors);
            
            qw.WaitForWorstCase(2);
            WorstCaseReportWrapper worst = qw.WorstCase;
            Assert.Equal(3, worst.warnings);
            Assert.Equal(1, worst.errors);
        }

        [Fact]
        public void QueryNormalizedByProvider()
        {
            // simulate some events
            OpenConnection(1, "server=localhost;uid=root;database=mydb", 1);
            OpenQuery(1, 1, "SELECT 1");
            QueryNormalized(1, 1, "this is my normalized sql");
            OpenResult(1, 1, -1, -1);
            CloseResult(1, 1, 1, 76);
            CloseQuery(1);
            CloseConnection(1);

            Assert.Equal(0, wrapper.InProcessQueries.Count);
            wrapper.ServerFactory.WaitForNumberOfServers(1, 5);

            ServerAggregationWrapper server = wrapper.ServerFactory.GetServer(0);
            server.WaitForNumberOfQueries(1, 5);

            QueryAggregationWrapper qw = server.GetQuery(0);

            qw.WaitForSummary(2);
            SummaryReportWrapper summary = qw.Summary;
            Assert.Equal("this is my normalized sql", summary.text);
        }
    }
}
