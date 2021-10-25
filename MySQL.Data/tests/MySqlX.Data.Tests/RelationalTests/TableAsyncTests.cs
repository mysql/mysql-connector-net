// Copyright (c) 2015, 2021, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class TableAsyncTests : BaseTest
  {
    [TearDown]
    public void TearDown() => ExecuteSQL("DROP TABLE IF EXISTS test");

    [SetUp]
    public void SetUp() => ExecuteSQL("CREATE TABLE test.test(id INT, age INT)");

    [Test]
    public void MultipleTableInsertAsync()
    {
      Table table = testSchema.GetTable("test");
      List<Task<Result>> tasksList = new List<Task<Result>>();

      for (int i = 1; i <= 200; i++)
      {
        tasksList.Add(table.Insert().Values(i, i % 250).ExecuteAsync());
      }

      Assert.True(Task.WaitAll(tasksList.ToArray(), TimeSpan.FromMinutes(2)), "WaitAll timeout");
      Assert.AreEqual(200, table.Count());
    }

    [Test]
    public void MultipleTableSelectAsync()
    {
      var table = testSchema.GetTable("test");
      int rows = 100;
      var insert = table.Insert();
      HashSet<int> validator = new HashSet<int>();

      for (int i = 1; i <= rows; i++)
      {
        insert.Values(i, i);
      }
      var result = ExecuteInsertStatement(insert);

      List<Task<RowResult>> tasksList = new List<Task<RowResult>>();

      for (int i = 1; i <= rows; i++)
      {
        tasksList.Add(table.Select().Where("age = :age").Bind("aGe", i).ExecuteAsync());
      }

      Assert.True(Task.WaitAll(tasksList.ToArray(), TimeSpan.FromMinutes(2)), "WaitAll timeout");
      foreach (Task<RowResult> task in tasksList)
      {
        Assert.AreEqual(2, task.Result.Columns.Count);
        Assert.That(task.Result.Rows, Has.One.Items);
        int value = (int)task.Result.Rows[0][1];
        Assert.False(validator.Contains(value), value + " value exists");
        validator.Add(value);
      }
      Assert.AreEqual(rows, validator.Count);
    }
    #region WL14389
    [Test, Description("Table.Select() with shared lock and Table.Update() ")]
    public async Task TableSelectAndUpdateAsync()
    {
      _ = await SubProcess1();
      _ = await SubProcess2();
    }

    private Task<int> SubProcess1()
    {
      if (!session.Version.isAtLeast(8, 0, 3)) return Task.FromResult(0);
      session.SQL("SET autocommit = 0").Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET autocommit = 0").Execute();
        var table = session.Schema.GetTable("test");
        try
        {
          session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        }
        catch (MySqlException ex)
        {
          Assert.True(ex.Message.Contains("Duplicate"));
        }

        var table2 = session2.GetSchema("test").GetTable("test");
        session2.SQL("START TRANSACTION").Execute();
        for (var i = 0; i < 1000; i++)
        {
          var result = table2.Update().Where("id = 1").Set("age", 2).Execute();
          Assert.IsNotNull(result);
        }
      }

      session.SQL("SET autocommit = 1").Execute();
      return Task.FromResult(0);
    }


    private Task<int> SubProcess2()
    {
      Thread.Sleep(1000);
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return Task.FromResult(0);
      session.SQL("SET autocommit = 0").Execute();

      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET autocommit = 0").Execute();
        var table = session.Schema.GetTable("test");
        try
        {
          session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        }
        catch (MySqlException ex)
        {
          Assert.True(ex.Message.Contains("Duplicate"));
        }

        var table2 = session2.GetSchema("test").GetTable("test");
        session.SQL("START TRANSACTION").Execute();
        var rowResult = table.Select().Where("id = 1").LockExclusive().Execute();
      }

      session.SQL("SET autocommit = 0").Execute();
      return Task.FromResult(0);
    }
    #endregion WL14389
  }
}
