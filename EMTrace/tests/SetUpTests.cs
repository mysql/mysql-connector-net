// Copyright © 2014, Oracle and/or its affiliates. All rights reserved.
//
// This software product is not publicly available software.  This software
// product is MySQL commercial software and use of this software is governed
// by your applicable license agreement with MySQL.

using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Data.Common;
using System.Text;
using Xunit;

namespace MySql.EMTrace.Tests
{
  public class SetUpTests : IDisposable
  {
    public readonly string rootConnectionString;
    public readonly string testConnectionString;
    public readonly string testDatabase = "employees";

    public SetUpTests()
    {
      Assert.True(ClassFactory.LoadAndCheckFactory());
      string connectionString = ConfigurationManager.ConnectionStrings["testConnection"].ConnectionString;
      MySqlConnectionStringBuilder rootSettings = new MySqlConnectionStringBuilder(connectionString);
      rootSettings.Database = "mysql";
      MySqlConnectionStringBuilder testSettings = new MySqlConnectionStringBuilder(connectionString);
      testSettings.Database = testDatabase;
      rootConnectionString = rootSettings.ToString();
      testConnectionString = testSettings.ToString();

      StringBuilder query = new StringBuilder();
      query.AppendFormat("DROP DATABASE IF EXISTS {0};", testDatabase);
      query.AppendFormat("CREATE DATABASE {0};", testDatabase);
      ExecRootSql(query.ToString());

      query = new StringBuilder();
      query.Append("CREATE TABLE employees(id INT KEY, name VARCHAR(256));");
      ExecTestSql(query.ToString());
    }

    public void ExecRootSql(string sql)
    {
      DbConnection c = ClassFactory.CreateConnection(rootConnectionString);
      using (c)
      {
        c.Open();

        DbCommand cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.Connection = c;
        cmd.ExecuteNonQuery();
      }
    }

    public void ExecTestSql(string sql)
    {
      DbConnection c = ClassFactory.CreateConnection(testConnectionString);
      using (c)
      {
        c.Open();

        DbCommand cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.Connection = c;
        cmd.ExecuteNonQuery();
      }
    }

    public void Dispose()
    {
      ExecTestSql(string.Format("DROP DATABASE IF EXISTS {0};", testDatabase));
    }
  }
}
