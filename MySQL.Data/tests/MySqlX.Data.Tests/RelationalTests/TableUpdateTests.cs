// Copyright © 2015, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using MySqlX.XDevAPI;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class TableUpdateTests : BaseTest
  {
    Table table;

    [TearDown]
    public void TearDown() => ExecuteSQL("DROP TABLE IF EXISTS test");

    [SetUp]
    public void SetUp()
    {
      ExecuteSQL("CREATE TABLE test.test(id INT, name VARCHAR(40), age INT)");

      table = testSchema.GetTable("test");
      var insertStatement = table.Insert();
      int rowsToInsert = 10;
      for (int i = 1; i <= rowsToInsert; i++)
      {
        insertStatement.Values(i, i, i);
      }
      ExecuteInsertStatement(insertStatement);
      Assert.That(CountRows(), Is.EqualTo(rowsToInsert));
    }

    private int CountRows()
    {
      return GetRows(null).Count;
    }

    private IReadOnlyCollection<Row> GetRows(FilterParams filter)
    {
      var statement = table.Select();
      if (filter != null)
      {
        statement.FilterData.Condition = filter.Condition;
        statement.FilterData.Limit = filter.Limit;
        statement.FilterData.OrderBy = filter.OrderBy;
        statement.FilterData.Parameters = filter.Parameters;
      }
      var result = ExecuteSelectStatement(statement);
      while (result.Next()) ;
      return result.Rows;
    }

    private void ValidateUpdate(TableUpdateStatement statement)
    {
      Dictionary<String, object> parameters = new Dictionary<string, object>(statement.FilterData.Parameters);
      var result = ExecuteUpdateStatement(statement);
      statement.FilterData.Parameters = parameters;
      var rows = GetRows(statement.FilterData);
      foreach (var row in rows)
      {
        foreach (var set in statement.updates)
        {
          Assert.That(row.GetString(set.Path), Is.EqualTo(set.Value.ToString()));
        }
      }
    }

    [Test]
    public void EmptyUpdateTest()
    {
      Assert.Throws<MySqlException>(() => ExecuteUpdateStatement(table.Update()));
    }

    [Test]
    public void UpdateConditionTest()
    {
      ValidateUpdate(table.Update().Where("id = 5").Set("name", "other"));
    }

    [Test]
    public void UpdateMultiSet()
    {
      ValidateUpdate(table.Update().Set("name", "other")
        .Set("age", 21)
        .Set("id", 30)
        .Where("id = 3"));
    }

    [Test]
    public void UpdateMultiRows()
    {
      ValidateUpdate(table.Update().Set("age", 85).Where("id % 2 = 0"));
    }

    [Test]
    public void UpdateAllRows()
    {
      ValidateUpdate(table.Update().Set("age", 32).Set("name", "jonh"));
    }

    [Test]
    public void UpdateOrderbyAndLimit()
    {
      ValidateUpdate(table.Update().Set("age", 15).OrderBy("id DES").Limit(5));
    }

    [Test]
    public void UpdateBind()
    {
      var stmt = table.Update().Set("age", 55).Where("id = :id or id = :id or id = :id2");
      ValidateUpdate(stmt.Bind("id", 4).Bind("id2", 7));
      ValidateUpdate(stmt.Bind("id", 5).Bind("id2", 8));
    }

    [Test]
    public void UpdateWithInOperator()
    {
      Table table = testSchema.GetTable("test");
      Assert.That(CountRows(), Is.EqualTo(10));

      Assert.That(ExecuteUpdateStatement(table.Update().Where("id IN (1,2)").Set("id", 0)).AffectedItemsCount, Is.EqualTo(2));
      Assert.That(ExecuteSelectStatement(table.Select().Where("id = 0")).FetchAll().Count, Is.EqualTo(2));

      Assert.Throws<MySqlException>(() => ExecuteDeleteStatement(table.Delete().Where("a IN [3]")));
      Assert.Throws<MySqlException>(() => ExecuteDeleteStatement(table.Delete().Where("3 IN a")));
      Assert.Throws<MySqlException>(() => ExecuteUpdateStatement(table.Update().Where("age IN [3]").Set("id", 0)));

      Assert.That(ExecuteUpdateStatement(table.Update().Where("age IN (3)").Set("id", 0)).AffectedItemsCount, Is.EqualTo(1));
      Assert.That(ExecuteSelectStatement(table.Select().Where("id = 0")).FetchAll().Count, Is.EqualTo(3));
    }

    #region WL14389

    [Test, Description("Collection.Find(condition).GroupBy(SearchExprStr)")]
    public void TableSelectGroupBy()
    {
      ExecuteSQLStatement(session.SQL("set sql_mode = 'STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';"));
      session.SQL("delete from test").Execute();
      Table table = testSchema.GetTable("test");
      var result1 = table.Insert("id", "name", "age")
          .Values(1, "jonh doe", 38)
          .Values(2, "milton greenh", 45)
          .Values(3, "larry smith", 24)
          .Values(4, "mary weinstein", 24)
          .Values(5, "jerry pratt", 45)
          .Values(6, "hugh jackman", 20)
          .Values(7, "elizabeth olsen", 31)
        .Execute();
      RowResult result = table.Select().OrderBy("age desc").Execute();
      Assert.Throws<MySqlException>(() => table.Delete().Limit(1).Offset(3).Execute());

      var t2 = table.Delete().Limit(1).Offset(0).Execute();
      result1 = table.Insert("id", "name", "age")
          .Values(1, "jonh doe", 38)
        .Execute();

      Assert.Throws<MySqlException>(() => table.Update().Set("name", "updated").Limit(1).Offset(3).Execute());

      t2 = table.Update().Set("name", "updated").Limit(1).Offset(0).Execute();
      var t = table.Select().Limit(1).Offset(3).Execute();
      t = table.Select().Limit(10).Offset(3).Execute();
      Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteSelectStatement(table.Select().Limit(-1).Offset(3)));

      t = table.Select().Limit(100000000).Offset(3).Execute();
      t = table.Select().Limit(2).Offset(-1).Execute();
      t = table.Select().Limit(2).Offset(1000000).Execute();

      result = table.Select().GroupBy("age").Execute();
      Assert.That(result.FetchAll().Count, Is.EqualTo(5));

      // GroupBy with null.
      result = table.Select("id as ID", "name as Name", "age as Age").GroupBy(null).Execute();
      Assert.That(result.FetchAll().Count, Is.EqualTo(7));

      result = table.Select("id as ID", "name as Name", "age as Age").GroupBy(null, null).Execute();
      Assert.That(result.FetchAll().Count, Is.EqualTo(7));

      result = table.Select("id as ID", "name as Name", "age as Age").GroupBy(null, "age").Execute();
      Assert.That(result.FetchAll().Count, Is.EqualTo(5));

      // Having operation.
      // Having reduces the original 5 rows to 3 since 2 rows have a cnt=2, due to the repeated names.
      result = table.Select("id", "count(name) as cnt", "age").GroupBy("age").Having("cnt = 1").Execute();
      Assert.That(result.FetchAll().Count, Is.EqualTo(3));

      // Having with null.
      result = table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having(null).Execute();
      Assert.That(result.FetchAll().Count, Is.EqualTo(5));

      // GroupBy with invalid field name.
      Exception ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy("Required")));
      Assert.That(ex.Message, Is.EqualTo("Unknown column 'Required' in 'group statement'"));

      ex = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy("")));
      Assert.That(ex.Message, Is.EqualTo("No more tokens when expecting one at token pos 0"));

      ex = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy(" ")));
      Assert.That(ex.Message, Is.EqualTo("No more tokens when expecting one at token pos 0"));

      ex = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy(string.Empty)));
      Assert.That(ex.Message, Is.EqualTo("No more tokens when expecting one at token pos 0"));

      ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having("Required = 1")));
      Assert.That(ex.Message, Is.EqualTo("Unknown column 'Required' in 'having clause'"));

      ex = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having("")));
      Assert.That(ex.Message, Is.EqualTo("Unable to parse query ''"));
      Assert.That(ex.InnerException.Message, Is.EqualTo("No more tokens when expecting one at token pos 0"));

      Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having(" ")));
      Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having(string.Empty)));
    }

    [Test, Description("Reading exclusively locked document in a table using lock_shared with DEFAULT waiting option.")]
    public void ExclusiveLockBeforeSharedLockDefaultWaiting()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      session.SQL("DROP TABLE IF EXISTS test.test").Execute();
      session.SQL("CREATE TABLE test.test (id INT, a INT)").Execute();
      var table1 = testSchema.GetTable("test");
      table1.Insert("id", "a").Values(1, 1).Values(2, 2).Values(3, 3).Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        var table = session.Schema.GetTable("test");
        session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        var table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        var rowResult = table.Select().Where("id = 1").LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 2").LockShared(LockContention.Default).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockShared(LockContention.Default)));

        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the deleted record count");

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Reading locked document(lock_shared) in a table using lock_shared with DEFAULT waiting option.")]
    public void SharedLockDefaultWaiting()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      session.SQL("DROP TABLE IF EXISTS test.test").Execute();
      session.SQL("CREATE TABLE test.test (id INT, a INT)").Execute();
      var table1 = testSchema.GetTable("test");
      table1.Insert("id", "a").Values(1, 1).Values(2, 2).Values(3, 3).Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        var table = session.Schema.GetTable("test");
        var table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        var rowResult = table.Select().Where("id = 1").LockShared().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 2").LockShared(LockContention.Default).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        rowResult = table2.Select().Where("id = 1").LockShared(LockContention.Default).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");
        // Session2 blocks due to to LockShared() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test,Description("Reading exclusively locked document in a table using lock_exclusive with DEFAULT waiting option ")]
    public void OnlyExclusiveLocksWithDefaultWaiting()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) Assert.Ignore("This test is for MySql 8.0.3 or higher");

      session.SQL("DROP TABLE IF EXISTS test.test").Execute();
      session.SQL("CREATE TABLE test.test (id INT, a INT)").Execute();
      var table1 = testSchema.GetTable("test");
      table1.Insert("id", "a").Values(1, 1).Values(2, 2).Values(3, 3).Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        var table = session.Schema.GetTable("test");
        session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        var table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        var rowResult = table.Select().Where("id = 1").LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 2").LockExclusive(LockContention.Default).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive(LockContention.Default)));

        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    #endregion WL14389

  }
}
