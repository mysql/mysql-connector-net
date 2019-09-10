// Copyright (c) 2015, 2019, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.XDevAPI;
using Xunit;
using System.Linq;
using MySqlX.XDevAPI.Relational;
using MySqlX.XDevAPI.Common;
using System;
using MySql.Data.MySqlClient;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class TableSelectTests : BaseTest
  {
    object[][] allRows = {
        new object[] { 1, "jonh doe", 38, "{\"company\": \"xyz\", \"hobbies\": \"reading\", \"vehicle\": \"bike\"}" },
        new object[] { 2, "milton green", 45, "{\"company\": \"abc\", \"hobbies\": [\"boxing\", \"running\"], \"vehicle\": \"car\"}" },
        new object[] { 3, "larry smith", 24, "{\"company\": \"zxc\", \"hobbies\": \"painting\", \"vehicle\": \"boat\"}" }
      };

    public TableSelectTests()
    {
      ExecuteSQL("CREATE TABLE test.test (id INT, name VARCHAR(45), age INT, additionalinfo JSON)");
      TableInsertStatement stmt = testSchema.GetTable("test").Insert();
      stmt.Values(allRows[0]);
      stmt.Values(allRows[1]);
      stmt.Values(allRows[2]);
      Result result = ExecuteInsertStatement(stmt);
    }

    [Fact]
    public void FetchOne()
    {
      Table t = testSchema.GetTable("test");
      Assert.Equal(38, ExecuteSelectStatement(t.Select("age")).FetchOne()["age"]);
    }

    private void MultiTableSelectTest(TableSelectStatement statement, object[][] expectedValues)
    {
      RowResult result = ExecuteSelectStatement(statement);
      int rowCount = result.FetchAll().Count;

      Assert.Equal(expectedValues.Length, rowCount);
      Assert.Equal(expectedValues.Length, result.Rows.Count);
      for (int i = 0; i < expectedValues.Length; i++)
      {
        for (int j = 0; j < expectedValues[i].Length; j++)
        {
          Assert.Equal(expectedValues[i][j], result.Rows.ToArray()[i][j]);
        }
      }
    }

    [Fact]
    public void TableSelect()
    {
      var table = testSchema.GetTable("test");

      MultiTableSelectTest(table.Select(), allRows);
      MultiTableSelectTest(table.Select("name", "age"),
        allRows.Select(c => new[] { c[1], c[2] }).ToArray());
      MultiTableSelectTest(table.Select("name", "age").Where("age == 38"),
        allRows.Select(c => new[] { c[1], c[2] }).Where(c => (int)c[1] == 38).ToArray());
      MultiTableSelectTest(table.Select().Where("age == 45"),
        allRows.Where(c => (int)c[2] == 45).ToArray());
      MultiTableSelectTest(table.Select().OrderBy("age"),
        allRows.OrderBy(c => c[2]).ToArray());
      MultiTableSelectTest(table.Select().OrderBy("age desc"),
        allRows.OrderByDescending(c => c[2]).ToArray());
      MultiTableSelectTest(table.Select().OrderBy("age desc, name"),
        allRows.OrderByDescending(c => c[2]).ThenBy(c => c[1]).ToArray());
      MultiTableSelectTest(table.Select().Limit(1),
        allRows.Take(1).ToArray());
      MultiTableSelectTest(table.Select().Limit(10).Offset(1),
        allRows.Skip(1).Take(10).ToArray());
      MultiTableSelectTest(table.Select().Limit(1).Offset(1),
        allRows.Skip(1).Take(1).ToArray());
      MultiTableSelectTest(table.Select().Where("name like :name").Bind("nAme", "%jon%"),
        allRows.Where(c => c[1].ToString().Contains("jon")).ToArray());
      MultiTableSelectTest(table.Select().Where("name like :name").Bind("naMe", "%on%"),
        allRows.Where(c => c[1].ToString().Contains("on")).ToArray());
    }

    [Fact]
    public void AllColumns()
    {
      var table = testSchema.GetTable("test");
      var select = ExecuteSelectStatement(table.Select("*, 42 as a_number, '43' as a_string"));
      var rows = select.FetchAll();
      Assert.Equal(6, select.Columns.Count);
      Assert.Equal(allRows.Length, rows.Count);
      Assert.Equal(allRows[0][0], rows[0]["id"]);
      Assert.Equal(allRows[0][1], rows[0]["name"]);
      Assert.Equal(allRows[0][2], rows[0]["age"]);
      Assert.Equal(allRows[0][3], rows[0]["additionalinfo"]);
      Assert.Equal((sbyte)42, rows[0]["a_number"]);
      Assert.Equal("43", rows[0]["a_string"]);
    }

    [Fact]
    public void CountAllColumns()
    {
      var table = testSchema.GetTable("test");
      var select = ExecuteSelectStatement(table.Select("count(*) + 10"));
      var rows = select.FetchAll();
      Assert.Single(select.Columns);
      Assert.Single(rows);
      Assert.Equal<long>(allRows.Length + 10, (long)rows[0][0]);
    }

    [Fact]
    public void MultipleBind()
    {
      object[] validationRow = allRows[1];
      var table = testSchema.GetTable("test");
      var select = ExecuteSelectStatement(table.Select().Where("Name = :nAme && Age = :aGe").Bind("agE", validationRow[2]).Bind("naMe", validationRow[1]));
      var rows = select.FetchAll();
      Assert.Single(rows);
      Assert.Equal(validationRow[1], rows[0]["namE"]);
      Assert.Equal(validationRow[2], rows[0]["AGe"]);
    }

    [Fact]
    public void DatetimeAndMicroseconds()
    {
      ExecuteSQL("CREATE TABLE test.testDate (id INT, name VARCHAR(45), birthday DATETIME(6))");
      ExecuteSQL("INSERT INTO test.testDate VALUES(1, 'JOHN', '1985-10-21 16:34:22.123456')");
      ExecuteSQL("INSERT INTO test.testDate VALUES(1, 'BILL', '1985-10-21 10:00:45.987')");
      var rows = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("testDate").Select()).FetchAll();
      Assert.Equal(2, rows.Count);
      Assert.Equal(new DateTime(1985, 10, 21, 16, 34, 22).AddTicks(1234560), (DateTime)rows[0]["birthday"]);
      Assert.Equal(new DateTime(1985, 10, 21, 10, 0, 45).AddTicks(9870000), (DateTime)rows[1]["birthday"]);
    }

    [Fact]
    public void DatetimeAndMilliseconds()
    {
      ExecuteSQL("CREATE TABLE test.testDate (id INT, name VARCHAR(45), birthday DATETIME(3))");
      ExecuteSQL("INSERT INTO test.testDate VALUES(1, 'JOHN', '1985-10-21 16:34:22.123456')");
      ExecuteSQL("INSERT INTO test.testDate VALUES(1, 'BILL', '1985-10-21 10:00:45.098')");
      var rows = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("testDate").Select()).FetchAll();
      Assert.Equal(2, rows.Count);
      Assert.Equal(new DateTime(1985, 10, 21, 16, 34, 22).AddTicks(1230000), (DateTime)rows[0]["birthday"]);
      Assert.Equal(new DateTime(1985, 10, 21, 10, 0, 45).AddTicks(980000), (DateTime)rows[1]["birthday"]);
    }

    [Fact]
    public void RowLockingNotSupportedInOlderVersions()
    {
      if (session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      Table table = session.Schema.GetTable("test");

      Exception ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select().LockShared()));
      Assert.Equal("This functionality is only supported from server version 8.0.3 onwards.", ex.Message);

      ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select().LockExclusive()));
      Assert.Equal("This functionality is only supported from server version 8.0.3 onwards.", ex.Message);
    }

    [Fact]
    public void SimpleSharedLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Table table = session.Schema.GetTable("test");
        Table table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        RowResult rowResult = ExecuteSelectStatement(table.Select().Where("id = 1").LockShared());
        Assert.Single(rowResult.FetchAll());

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since row isn't locked.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 2").LockShared());
        Assert.Single(rowResult.FetchAll());
        // Should return immediately due to LockShared() allows reading by other sessions.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 1").LockShared());
        Assert.Single(rowResult.FetchAll());

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Fact]
    public void SimpleExclusiveLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Table table = session.Schema.GetTable("test");
        ExecuteSQLStatement(session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)"));
        Table table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        RowResult rowResult = ExecuteSelectStatement(table.Select().Where("id = 1").LockExclusive());
        Assert.Single(rowResult.FetchAll());

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since row isn't locked.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 2").LockExclusive());
        Assert.Single(rowResult.FetchAll());
        // Session2 blocks due to to LockExclusive() not allowing to read locked rows.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive()));
        Assert.Equal("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Fact]
    public void SharedLockForbidsToModifyDocuments()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Table table = session.Schema.GetTable("test");
        Table table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        RowResult rowResult = ExecuteSelectStatement(table.Select().Where("id = 1").LockShared());
        Assert.Single(rowResult.FetchAll());

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Reading the same row is allowed with LockShared().
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 1"));
        Assert.Single(rowResult.FetchAll());

        // Modify() is allowed for non-locked rows.
        Result result = ExecuteUpdateStatement(table2.Update().Where("id = 2").Set("age", 2));
        Assert.Equal<ulong>(1, result.AffectedItemsCount);
        // Session1 blocks, Modify() is not allowed for locked rows.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteUpdateStatement(table2.Update().Where("id = 1").Set("age", 2)));
        Assert.Equal("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        // Modify() is allowed since row isn't locked anymore.
        ExecuteUpdateStatement(table2.Update().Where("id = 1").Set("age", 2));
        ExecuteSQLStatement(session2.SQL("COMMIT"));
      }
    }

    [Fact]
    public void ExclusiveLockForbidsToModifyDocuments()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Table table = session.Schema.GetTable("test");
        Table table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        RowResult rowResult = ExecuteSelectStatement(table.Select().Where("id = 1").LockExclusive());
        Assert.Single(rowResult.FetchAll());

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));

        // Modify() is allowed for non-locked rows.
        Result result = ExecuteUpdateStatement(table2.Update().Where("id = 2").Set("age", 2));
        Assert.Equal<ulong>(1, result.AffectedItemsCount);
        // Session1 blocks, Modify() is not allowed for locked rows.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteUpdateStatement(table2.Update().Where("id = 1").Set("age", 2)));
        Assert.Equal("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        // Modify() is allowed since row isn't locked anymore.
        ExecuteUpdateStatement(table2.Update().Where("id = 1").Set("age", 2));
        ExecuteSQLStatement(session2.SQL("COMMIT"));
      }
    }

    [Fact]
    public void SharedLockAfterExclusiveLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Table table = session.Schema.GetTable("test");
        ExecuteSQLStatement(session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)"));
        Table table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        RowResult rowResult = ExecuteSelectStatement(table.Select().Where("id = 1").LockExclusive());
        Assert.Single(rowResult.FetchAll());

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since row isn't locked.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 2").LockShared());
        Assert.Single(rowResult.FetchAll());
        // Session2 blocks due to LockExclusive() not allowing to read locked rows.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockShared()));
        Assert.Equal("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        // Session unlocks rows.
        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        // Row can now be recovered.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 1").LockShared());
        Assert.Single(rowResult.FetchAll());
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Fact]
    public void ExclusiveLockAfterSharedLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Table table = session.Schema.GetTable("test");
        ExecuteSQLStatement(session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)"));
        Table table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        RowResult rowResult = ExecuteSelectStatement(table.Select().Where("id in (1, 3)").LockShared());
        Assert.Equal(2, rowResult.FetchAll().Count);

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since row isn't locked.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 2").LockExclusive());
        // Should return immediately due to LockShared() allows reading by other sessions.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 2").LockShared());
        Assert.Single(rowResult.FetchAll());
        // Session2 blocks due to to LockExclusive() not allowing to read locked rows.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive()));
        Assert.Equal("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        // Session unlocks rows.
        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive());
        Assert.Single(rowResult.FetchAll());
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Fact]
    public void ExclusiveLockAfterExclusiveLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Table table = session.Schema.GetTable("test");
        ExecuteSQLStatement(session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)"));
        Table table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        RowResult rowResult = ExecuteSelectStatement(table.Select().Where("id = 1").LockExclusive());
        Assert.Single(rowResult.FetchAll());

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since row isn't locked.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 2").LockExclusive());
        Assert.Single(rowResult.FetchAll());
        // Session2 blocks due to to LockExclusive() not allowing to read locked rows.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive()));
        Assert.Equal("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        // Session unlocks rows.
        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive());
        Assert.Single(rowResult.FetchAll());
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Fact]
    public void SelectWithInOperator()
    {
      Table table = testSchema.GetTable("test");
      Assert.Equal(3, ExecuteSelectStatement(table.Select()).FetchAll().Count);

      Assert.Equal(2, ExecuteSelectStatement(table.Select().Where("name IN (\"jonh doe\", \"milton green\")")).FetchAll().Count);
      Assert.Single(ExecuteSelectStatement(table.Select().Where("name NOT IN (\"jonh doe\", \"milton green\")")).FetchAll());
      Assert.Empty(ExecuteSelectStatement(table.Select().Where("name IN (\"\", \"\")")).FetchAll());
      Assert.Empty(ExecuteSelectStatement(table.Select().Where("\"\" IN (1,2,3)")).FetchAll());
      Assert.Empty(ExecuteSelectStatement(table.Select().Where("name IN ('', '')")).FetchAll());
      Assert.Empty(ExecuteSelectStatement(table.Select().Where("'' IN (1,2,3)")).FetchAll());
      Assert.Equal(3, ExecuteSelectStatement(table.Select().Where("'' IN ('')")).FetchAll().Count);

      Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select().Where("name NOT IN [\"jonh doe\", \"milton green\"]")).FetchAll().Count);
      Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select().Where("a IN [3]")).FetchAll().Count);
      Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select().Where("3 IN a")).FetchAll().Count);
    }

    [Fact]
    public void Grouping()
    {
      ExecuteSQLStatement(GetSession().SQL("SET GLOBAL sql_mode=(SELECT REPLACE(@@sql_mode, 'ONLY_FULL_GROUP_BY', '')); "));
      Table table = testSchema.GetTable("test");

      // Insert additonal users.
      object[][] additionalUsers = {
        new object[] { 4, "mary weinstein", 24, null },
        new object[] { 5, "jerry pratt", 45, null },
        new object[] { 6, "hugh jackman", 20, null },
        new object[] { 7, "elizabeth olsen", 31, null }
      };
      var statement = table.Insert();
      foreach (object[] user in additionalUsers)
      {
        statement = statement.Values(user);
      }

      Assert.Equal<ulong>(4, ExecuteInsertStatement(statement).AffectedItemsCount);

      // GroupBy operation.
      // GroupBy returns 5 rows since age 45 and 24 is repeated.
      var result = ExecuteSelectStatement(table.Select().GroupBy("age"));
      Assert.Equal(5, result.FetchAll().Count);

      // GroupBy with null.
      result = ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy(null));
      Assert.Equal(7, result.FetchAll().Count);
      result = ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy(null, null));
      Assert.Equal(7, result.FetchAll().Count);
      result = ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy(null, "age"));
      Assert.Equal(5, result.FetchAll().Count);

      // Having operation.
      // Having reduces the original 5 rows to 3 since 2 rows have a cnt=2, due to the repeated names.
      result = ExecuteSelectStatement(table.Select("id", "count(name) as cnt", "age").GroupBy("age").Having("cnt = 1"));
      Assert.Equal(3, result.FetchAll().Count);

      // Having with null.
      result = ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having(null));
      Assert.Equal(5, result.FetchAll().Count);

      // GroupBy with invalid field name.
      var ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy("none")));
      Assert.Equal("Unknown column 'none' in 'group statement'", ex.Message);

      // GroupBy with empty strings.
      var ex2 = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy("")));
      Assert.Equal("No more tokens when expecting one at token pos 0", ex2.Message);
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy(" ")));
      Assert.Equal("No more tokens when expecting one at token pos 0", ex2.Message);
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy(string.Empty)));
      Assert.Equal("No more tokens when expecting one at token pos 0", ex2.Message);

      // Having with invalid field name.
      ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having("none = 1")));
      Assert.Equal("Unknown column 'none' in 'having clause'", ex.Message);

      // Having with empty strings.
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having("")));
      Assert.Equal("Unable to parse query ''", ex2.Message);
      Assert.Equal("No more tokens when expecting one at token pos 0", ex2.InnerException.Message);
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having(" ")));
      Assert.Equal("Unable to parse query ' '", ex2.Message);
      Assert.Equal("No more tokens when expecting one at token pos 0", ex2.InnerException.Message);
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having(string.Empty)));
      Assert.Equal("Unable to parse query ''", ex2.Message);
      Assert.Equal("No more tokens when expecting one at token pos 0", ex2.InnerException.Message);
    }

    [Theory]
    [InlineData(":hobbies IN additionalinfo->$.hobbies", "hobbies", "painting", 3)]
    [InlineData(":hobbies IN additionalinfo->$.hobbies", "hobbies", "[\"boxing\", \"running\"]", 0)]
    [InlineData("[\"boxing\", \"running\"] IN additionalinfo->$.hobbies", null, null, 2)]
    [InlineData(":hobbies IN additionalinfo$.hobbies", "hobbies", "painting", 3)]
    public void InOperatorBindingJson(string condition, string bind, string value, int id)
    {
      Table table = testSchema.GetTable("test");
      Assert.Equal(3, ExecuteSelectStatement(table.Select()).FetchAll().Count);

      var stmt = table.Select().Where(condition);
      if (bind != null) stmt.Bind(bind, value);
      var result = ExecuteSelectStatement(stmt).FetchAll();
      Assert.Equal(id == 0 ? 0 : 1, result.Count);
      if (id > 0)
      {
        Assert.Equal(id, result[0]["id"]);
      }
    }
  }
}
