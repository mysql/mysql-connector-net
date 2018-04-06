// Copyright © 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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
        new object[] { 1, "jonh doe", 38 },
        new object[] { 2, "milton green", 45 },
        new object[] { 3, "larry smith", 24}
      };

    public TableSelectTests()
    {
      ExecuteSQL("CREATE TABLE test.test (id INT, name VARCHAR(45), age INT)");
      TableInsertStatement stmt = testSchema.GetTable("test").Insert();
      stmt.Values(allRows[0]);
      stmt.Values(allRows[1]);
      stmt.Values(allRows[2]);
      Result result = stmt.Execute();
    }

    [Fact]
    public void FetchOne()
    {
      Table t = testSchema.GetTable("test");
      Assert.Equal(38, t.Select("age").Execute().FetchOne()["age"]);
    }

    private void MultiTableSelectTest(TableSelectStatement statement, object[][] expectedValues)
    {
      RowResult result = statement.Execute();
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
      MultiTableSelectTest(table.Select().Limit(10, 1),
        allRows.Skip(1).Take(10).ToArray());
      MultiTableSelectTest(table.Select().Limit(1, 1),
        allRows.Skip(1).Take(1).ToArray());
      MultiTableSelectTest(table.Select().Where("name like :name").Bind("nAme", "%jon%"),
        allRows.Where(c => c[1].ToString().Contains("jon")).ToArray());
      MultiTableSelectTest(table.Select().Where("name like :name").Bind("naMe", "%on%"),
        allRows.Where(c => c[1].ToString().Contains("on")).ToArray());
      //MultiTableSelectTest(employees.Select().GroupBy("age"),
      //allRows.GroupBy(c => new[] { c[2] }).First().ToArray());
    }

    [Fact]
    public void AllColumns()
    {
      var table = testSchema.GetTable("test");
      var select = table.Select("*, 42 as a_number, '43' as a_string").Execute();
      var rows = select.FetchAll();
      Assert.Equal(5, select.Columns.Count);
      Assert.Equal(allRows.Length, rows.Count);
      Assert.Equal(allRows[0][0], rows[0]["id"]);
      Assert.Equal(allRows[0][1], rows[0]["name"]);
      Assert.Equal(allRows[0][2], rows[0]["age"]);
      Assert.Equal((sbyte)42, rows[0]["a_number"]);
      Assert.Equal("43", rows[0]["a_string"]);
    }

    [Fact]
    public void CountAllColumns()
    {
      var table = testSchema.GetTable("test");
      var select = table.Select("count(*) + 10").Execute();
      var rows = select.FetchAll();
      Assert.Equal(1, select.Columns.Count);
      Assert.Equal(1, rows.Count);
      Assert.Equal<long>(allRows.Length + 10, (long)rows[0][0]);
    }

    [Fact]
    public void MultipleBind()
    {
      object[] validationRow = allRows[1];
      var table = testSchema.GetTable("test");
      var select = table.Select().Where("Name = :nAme && Age = :aGe").Bind("agE", validationRow[2]).Bind("naMe", validationRow[1]).Execute();
      var rows = select.FetchAll();
      Assert.Equal(1, rows.Count);
      Assert.Equal(validationRow[1], rows[0]["namE"]);
      Assert.Equal(validationRow[2], rows[0]["AGe"]);
    }

    [Fact]
    public void DatetimeAndMicroseconds()
    {
      ExecuteSQL("CREATE TABLE test.testDate (id INT, name VARCHAR(45), birthday DATETIME(6))");
      ExecuteSQL("INSERT INTO test.testDate VALUES(1, 'JOHN', '1985-10-21 16:34:22.123456')");
      ExecuteSQL("INSERT INTO test.testDate VALUES(1, 'BILL', '1985-10-21 10:00:45.987')");
      var rows = GetSession().GetSchema("test").GetTable("testDate").Select().Execute().FetchAll();
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
      var rows = GetSession().GetSchema("test").GetTable("testDate").Select().Execute().FetchAll();
      Assert.Equal(2, rows.Count);
      Assert.Equal(new DateTime(1985, 10, 21, 16, 34, 22).AddTicks(1230000), (DateTime)rows[0]["birthday"]);
      Assert.Equal(new DateTime(1985, 10, 21, 10, 0, 45).AddTicks(980000), (DateTime)rows[1]["birthday"]);
    }

    [Fact]
    public void RowLockingNotSupportedInOlderVersions()
    {
      if (session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      Table table = session.Schema.GetTable("test");

      Exception ex = Assert.Throws<MySqlException>(() => table.Select().LockShared().Execute());
      Assert.Equal("This functionality is only supported from server version 8.0.3 onwards.", ex.Message);

      ex = Assert.Throws<MySqlException>(() => table.Select().LockExclusive().Execute());
      Assert.Equal("This functionality is only supported from server version 8.0.3 onwards.", ex.Message);
    }

    [Fact]
    public void SimpleSharedLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
        Table table = session.Schema.GetTable("test");
        Table table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        RowResult rowResult = table.Select().Where("id = 1").LockShared().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since row isn't locked.
        rowResult = table2.Select().Where("id = 2").LockShared().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);
        // Should return immediately due to LockShared() allows reading by other sessions.
        rowResult = table2.Select().Where("id = 1").LockShared().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Fact]
    public void SimpleExclusiveLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
        Table table = session.Schema.GetTable("test");
        session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        Table table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        RowResult rowResult = table.Select().Where("id = 1").LockExclusive().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since row isn't locked.
        rowResult = table2.Select().Where("id = 2").LockExclusive().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);
        // Session2 blocks due to to LockExclusive() not allowing to read locked rows.
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Exception ex = Assert.Throws<MySqlException>(() => table2.Select().Where("id = 1").LockExclusive().Execute());
        Assert.Equal("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Fact]
    public void SharedLockForbidsToModifyDocuments()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
        Table table = session.Schema.GetTable("test");
        Table table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        RowResult rowResult = table.Select().Where("id = 1").LockShared().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);

        session2.SQL("START TRANSACTION").Execute();
        // Reading the same row is allowed with LockShared().
        rowResult = table2.Select().Where("id = 1").Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);

        // Modify() is allowed for non-locked rows.
        Result result = table2.Update().Where("id = 2").Set("age", 2).Execute();
        Assert.Equal<ulong>(1, result.AffectedItemsCount);
        // Session1 blocks, Modify() is not allowed for locked rows.
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Exception ex = Assert.Throws<MySqlException>(() => table2.Update().Where("id = 1").Set("age", 2).Execute());
        Assert.Equal("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        session.SQL("ROLLBACK").Execute();
        // Modify() is allowed since row isn't locked anymore.
        table2.Update().Where("id = 1").Set("age", 2).Execute();
        session2.SQL("COMMIT").Execute();
      }
    }

    [Fact]
    public void ExclusiveLockForbidsToModifyDocuments()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
        Table table = session.Schema.GetTable("test");
        Table table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        RowResult rowResult = table.Select().Where("id = 1").LockExclusive().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);

        session2.SQL("START TRANSACTION").Execute();

        // Modify() is allowed for non-locked rows.
        Result result = table2.Update().Where("id = 2").Set("age", 2).Execute();
        Assert.Equal<ulong>(1, result.AffectedItemsCount);
        // Session1 blocks, Modify() is not allowed for locked rows.
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Exception ex = Assert.Throws<MySqlException>(() => table2.Update().Where("id = 1").Set("age", 2).Execute());
        Assert.Equal("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        session.SQL("ROLLBACK").Execute();
        // Modify() is allowed since row isn't locked anymore.
        table2.Update().Where("id = 1").Set("age", 2).Execute();
        session2.SQL("COMMIT").Execute();
      }
    }

    [Fact]
    public void SharedLockAfterExclusiveLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
        Table table = session.Schema.GetTable("test");
        session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        Table table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        RowResult rowResult = table.Select().Where("id = 1").LockExclusive().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since row isn't locked.
        rowResult = table2.Select().Where("id = 2").LockShared().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);
        // Session2 blocks due to LockExclusive() not allowing to read locked rows.
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Exception ex = Assert.Throws<MySqlException>(() => table2.Select().Where("id = 1").LockShared().Execute());
        Assert.Equal("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        // Session unlocks rows.
        session.SQL("ROLLBACK").Execute();
        // Row can now be recovered.
        rowResult = table2.Select().Where("id = 1").LockShared().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Fact]
    public void ExclusiveLockAfterSharedLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
        Table table = session.Schema.GetTable("test");
        session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        Table table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        RowResult rowResult = table.Select().Where("id in (1, 3)").LockShared().Execute();
        Assert.Equal(2, rowResult.FetchAll().Count);

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since row isn't locked.
        rowResult = table2.Select().Where("id = 2").LockExclusive().Execute();
        // Should return immediately due to LockShared() allows reading by other sessions.
        rowResult = table2.Select().Where("id = 2").LockShared().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);
        // Session2 blocks due to to LockExclusive() not allowing to read locked rows.
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Exception ex = Assert.Throws<MySqlException>(() => table2.Select().Where("id = 1").LockExclusive().Execute());
        Assert.Equal("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        // Session unlocks rows.
        session.SQL("ROLLBACK").Execute();
        rowResult = table2.Select().Where("id = 1").LockExclusive().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Fact]
    public void ExclusiveLockAfterExclusiveLock()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8,0,3)) return;

      session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
        Table table = session.Schema.GetTable("test");
        session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        Table table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        RowResult rowResult = table.Select().Where("id = 1").LockExclusive().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since row isn't locked.
        rowResult = table2.Select().Where("id = 2").LockExclusive().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);
        // Session2 blocks due to to LockExclusive() not allowing to read locked rows.
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Exception ex = Assert.Throws<MySqlException>(() => table2.Select().Where("id = 1").LockExclusive().Execute());
        Assert.Equal("Lock wait timeout exceeded; try restarting transaction", ex.Message);

        // Session unlocks rows.
        session.SQL("ROLLBACK").Execute();
        rowResult = table2.Select().Where("id = 1").LockExclusive().Execute();
        Assert.Equal(1, rowResult.FetchAll().Count);
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Fact]
    public void SelectWithInOperator()
    {
      Table table = testSchema.GetTable("test");
      Assert.Equal(3, table.Select().Execute().FetchAll().Count);

      Assert.Equal(2, table.Select().Where("name IN (\"jonh doe\", \"milton green\")").Execute().FetchAll().Count);
      Assert.Equal(1, table.Select().Where("name NOT IN (\"jonh doe\", \"milton green\")").Execute().FetchAll().Count);
      Assert.Equal(0, table.Select().Where("name IN (\"\", \"\")").Execute().FetchAll().Count);
      Assert.Equal(0, table.Select().Where("\"\" IN (1,2,3)").Execute().FetchAll().Count);
      Assert.Equal(0, table.Select().Where("name IN ('', '')").Execute().FetchAll().Count);
      Assert.Equal(0, table.Select().Where("'' IN (1,2,3)").Execute().FetchAll().Count);
      Assert.Equal(3, table.Select().Where("'' IN ('')").Execute().FetchAll().Count);

      Assert.Throws<MySqlException>(() => table.Select().Where("name NOT IN [\"jonh doe\", \"milton green\"]").Execute().FetchAll().Count);
      Assert.Throws<MySqlException>(() => table.Select().Where("a IN [3]").Execute().FetchAll().Count);
      Assert.Throws<MySqlException>(() => table.Select().Where("3 IN a").Execute().FetchAll().Count);
    }
  }
}
