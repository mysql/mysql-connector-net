// Copyright © 2014, Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xunit;

namespace MySql.EMTrace.Tests
{
  public class ReportsTests : BaseTest
  {
    public ReportsTests()
      : base(emHost + "WithExplain")
    {
    }

    [Fact]
    public void InsertExplain()
    {
      OpenConnection(1, SetupData.testConnectionString, 1);
      OpenQuery(1, 1, "INSERT INTO employees(name) VALUES('john')");
      CloseQuery(1);
      CloseConnection(1);

      wrapper.ServerFactory.WaitForNumberOfServers(1, 5);
      ServerAggregationWrapper server = wrapper.ServerFactory.GetServer(0);
      server.WaitForNumberOfQueries(1, 5);
      QueryAggregationWrapper qw = server.GetQuery(0);
      qw.WaitForExplainReport(5);
      Assert.NotNull(qw.ExplainReport);
      Assert.Equal("INSERT", qw.ExplainReport.select_type);
    }

    [Fact]
    public void ReplaceExplain()
    {
      OpenConnection(1, SetupData.testConnectionString, 1);
      OpenQuery(1, 1, "REPLACE INTO employees(id, name) VALUES(1, 'peter')");
      CloseQuery(1);
      CloseConnection(1);

      wrapper.ServerFactory.WaitForNumberOfServers(1, 5);
      ServerAggregationWrapper server = wrapper.ServerFactory.GetServer(0);
      server.WaitForNumberOfQueries(1, 5);
      QueryAggregationWrapper qw = server.GetQuery(0);
      qw.WaitForExplainReport(5);
      Assert.NotNull(qw.ExplainReport);
      Assert.Equal("REPLACE", qw.ExplainReport.select_type);
    }

    [Fact]
    public void UpdateExplain()
    {
      OpenConnection(1, SetupData.testConnectionString, 1);
      OpenQuery(1, 1, "UPDATE employees SET name = 'peter'");
      CloseQuery(1);
      CloseConnection(1);

      wrapper.ServerFactory.WaitForNumberOfServers(1, 5);
      ServerAggregationWrapper server = wrapper.ServerFactory.GetServer(0);
      server.WaitForNumberOfQueries(1, 5);
      QueryAggregationWrapper qw = server.GetQuery(0);
      qw.WaitForExplainReport(5);
      Assert.NotNull(qw.ExplainReport);
      Assert.Equal("UPDATE", qw.ExplainReport.select_type);
    }

    [Fact]
    public void DeleteExplain()
    {
      OpenConnection(1, SetupData.testConnectionString, 1);
      OpenQuery(1, 1, "DELETE FROM employees");
      CloseQuery(1);
      CloseConnection(1);

      wrapper.ServerFactory.WaitForNumberOfServers(1, 5);
      ServerAggregationWrapper server = wrapper.ServerFactory.GetServer(0);
      server.WaitForNumberOfQueries(1, 5);
      QueryAggregationWrapper qw = server.GetQuery(0);
      qw.WaitForExplainReport(5);
      Assert.NotNull(qw.ExplainReport);
      Assert.Equal("DELETE", qw.ExplainReport.select_type);
    }
  }
}
