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

using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Linq;
using System.Text;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class TableSelectTests : BaseTest
  {
    object[][] allRows = {
        new object[] { 1, "jonh doe", 38, "{\"company\": \"xyz\", \"hobbies\": \"reading\", \"vehicle\": \"bike\"}" },
        new object[] { 2, "milton green", 45, "{\"company\": \"abc\", \"hobbies\": [\"boxing\", \"running\"], \"vehicle\": \"car\"}" },
        new object[] { 3, "larry smith", 24, "{\"company\": \"zxc\", \"hobbies\": \"painting\", \"vehicle\": \"boat\"}" }
      };

    [SetUp]
    public void SetUp()
    {
      ExecuteSQL("CREATE TABLE test.test (id INT, name VARCHAR(45), age INT, additionalinfo JSON)");
      TableInsertStatement stmt = testSchema.GetTable("test").Insert();
      stmt.Values(allRows[0]);
      stmt.Values(allRows[1]);
      stmt.Values(allRows[2]);
      Result result = ExecuteInsertStatement(stmt);
    }

    [TearDown]
    public void TearDown()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test");
      ExecuteSQL("DROP TABLE IF EXISTS testDate");
    }

    [Test]
    public void FetchOne()
    {
      Table t = testSchema.GetTable("test");
      Assert.That(ExecuteSelectStatement(t.Select("age")).FetchOne()["age"], Is.EqualTo(38));
    }

    private void MultiTableSelectTest(TableSelectStatement statement, object[][] expectedValues)
    {
      RowResult result = ExecuteSelectStatement(statement);
      int rowCount = result.FetchAll().Count;

      Assert.That(rowCount, Is.EqualTo(expectedValues.Length));
      Assert.That(result.Rows.Count, Is.EqualTo(expectedValues.Length));
      for (int i = 0; i < expectedValues.Length; i++)
      {
        for (int j = 0; j < expectedValues[i].Length; j++)
        {
          Assert.That(result.Rows.ToArray()[i][j], Is.EqualTo(expectedValues[i][j]));
        }
      }
    }

    [Test]
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

    [Test]
    public void AllColumns()
    {
      var table = testSchema.GetTable("test");
      var select = ExecuteSelectStatement(table.Select("*, 42 as a_number, '43' as a_string"));
      var rows = select.FetchAll();
      Assert.That(select.Columns.Count, Is.EqualTo(6));
      Assert.That(rows.Count, Is.EqualTo(allRows.Length));
      Assert.That(rows[0]["id"], Is.EqualTo(allRows[0][0]));
      Assert.That(rows[0]["name"], Is.EqualTo(allRows[0][1]));
      Assert.That(rows[0]["age"], Is.EqualTo(allRows[0][2]));
      Assert.That(rows[0]["additionalinfo"], Is.EqualTo(allRows[0][3]));
      Assert.That(rows[0]["a_number"], Is.EqualTo((sbyte)42));
      Assert.That(rows[0]["a_string"], Is.EqualTo("43"));
    }

    [Test]
    public void CountAllColumns()
    {
      var table = testSchema.GetTable("test");
      var select = ExecuteSelectStatement(table.Select("count(*) + 10"));
      var rows = select.FetchAll();
      Assert.That(select.Columns, Has.One.Items);
      Assert.That(rows, Has.One.Items);
      Assert.That((long)rows[0][0], Is.EqualTo(allRows.Length + 10));
    }

    [Test]
    public void MultipleBind()
    {
      object[] validationRow = allRows[1];
      var table = testSchema.GetTable("test");
      var select = ExecuteSelectStatement(table.Select().Where("Name = :nAme && Age = :aGe").Bind("agE", validationRow[2]).Bind("naMe", validationRow[1]));
      var rows = select.FetchAll();
      Assert.That(rows, Has.One.Items);
      Assert.That(rows[0]["namE"], Is.EqualTo(validationRow[1]));
      Assert.That(rows[0]["AGe"], Is.EqualTo(validationRow[2]));
    }

    [Test]
    public void DatetimeAndMicroseconds()
    {
      ExecuteSQL("CREATE TABLE test.testDate (id INT, name VARCHAR(45), birthday DATETIME(6))");
      ExecuteSQL("INSERT INTO test.testDate VALUES(1, 'JOHN', '1985-10-21 16:34:22.123456')");
      ExecuteSQL("INSERT INTO test.testDate VALUES(1, 'BILL', '1985-10-21 10:00:45.987')");
      var rows = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("testDate").Select()).FetchAll();
      Assert.That(rows.Count, Is.EqualTo(2));
      Assert.That((DateTime)rows[0]["birthday"], Is.EqualTo(new DateTime(1985, 10, 21, 16, 34, 22).AddTicks(1234560)));
      Assert.That((DateTime)rows[1]["birthday"], Is.EqualTo(new DateTime(1985, 10, 21, 10, 0, 45).AddTicks(9870000)));
    }

    [Test]
    public void DatetimeAndMilliseconds()
    {
      ExecuteSQL("CREATE TABLE test.testDate2 (id INT, name VARCHAR(45), birthday DATETIME(3))");
      ExecuteSQL("INSERT INTO test.testDate2 VALUES(1, 'JOHN', '1985-10-21 16:34:22.123456')");
      ExecuteSQL("INSERT INTO test.testDate2 VALUES(1, 'BILL', '1985-10-21 10:00:45.098')");
      var rows = ExecuteSelectStatement(GetSession().GetSchema("test").GetTable("testDate2").Select()).FetchAll();
      Assert.That(rows.Count, Is.EqualTo(2));
      Assert.That((DateTime)rows[0]["birthday"], Is.EqualTo(new DateTime(1985, 10, 21, 16, 34, 22).AddTicks(1230000)));
      Assert.That((DateTime)rows[1]["birthday"], Is.EqualTo(new DateTime(1985, 10, 21, 10, 0, 45).AddTicks(980000)));
    }

    [Test]
    public void RowLockingNotSupportedInOlderVersions()
    {
      if (session.InternalSession.GetServerVersion().isAtLeast(8, 0, 3)) return;

      Table table = session.Schema.GetTable("test");

      Exception ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select().LockShared()));
      Assert.That(ex.Message, Is.EqualTo("This functionality is only supported from server version 8.0.3 onwards."));

      ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select().LockExclusive()));
      Assert.That(ex.Message, Is.EqualTo("This functionality is only supported from server version 8.0.3 onwards."));
    }

    [Test]
    public void SimpleSharedLock()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Table table = session.Schema.GetTable("test");
        Table table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        RowResult rowResult = ExecuteSelectStatement(table.Select().Where("id = 1").LockShared());
        Assert.That(rowResult.FetchAll(), Has.One.Items);

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since row isn't locked.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 2").LockShared());
        Assert.That(rowResult.FetchAll(), Has.One.Items);
        // Should return immediately due to LockShared() allows reading by other sessions.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 1").LockShared());
        Assert.That(rowResult.FetchAll(), Has.One.Items);

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Test]
    public void SimpleExclusiveLock()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Table table = session.Schema.GetTable("test");
        ExecuteSQLStatement(session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)"));
        Table table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        RowResult rowResult = ExecuteSelectStatement(table.Select().Where("id = 1").LockExclusive());
        Assert.That(rowResult.FetchAll(), Has.One.Items);

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since row isn't locked.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 2").LockExclusive());
        Assert.That(rowResult.FetchAll(), Has.One.Items);
        // Session2 blocks due to to LockExclusive() not allowing to read locked rows.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive()));
        Assert.That(ex.Message, Is.EqualTo("Lock wait timeout exceeded; try restarting transaction"));

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Test]
    public void SharedLockForbidsToModifyDocuments()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Table table = session.Schema.GetTable("test");
        Table table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        RowResult rowResult = ExecuteSelectStatement(table.Select().Where("id = 1").LockShared());
        Assert.That(rowResult.FetchAll(), Has.One.Items);

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Reading the same row is allowed with LockShared().
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 1"));
        Assert.That(rowResult.FetchAll(), Has.One.Items);

        // Modify() is allowed for non-locked rows.
        Result result = ExecuteUpdateStatement(table2.Update().Where("id = 2").Set("age", 2));
        Assert.That(result.AffectedItemsCount, Is.EqualTo(1));
        // Session1 blocks, Modify() is not allowed for locked rows.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteUpdateStatement(table2.Update().Where("id = 1").Set("age", 2)));
        Assert.That(ex.Message, Is.EqualTo("Lock wait timeout exceeded; try restarting transaction"));

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        // Modify() is allowed since row isn't locked anymore.
        ExecuteUpdateStatement(table2.Update().Where("id = 1").Set("age", 2));
        ExecuteSQLStatement(session2.SQL("COMMIT"));
      }
    }

    [Test]
    public void ExclusiveLockForbidsToModifyDocuments()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Table table = session.Schema.GetTable("test");
        Table table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        RowResult rowResult = ExecuteSelectStatement(table.Select().Where("id = 1").LockExclusive());
        Assert.That(rowResult.FetchAll(), Has.One.Items);

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));

        // Modify() is allowed for non-locked rows.
        Result result = ExecuteUpdateStatement(table2.Update().Where("id = 2").Set("age", 2));
        Assert.That(result.AffectedItemsCount, Is.EqualTo(1));
        // Session1 blocks, Modify() is not allowed for locked rows.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteUpdateStatement(table2.Update().Where("id = 1").Set("age", 2)));
        Assert.That(ex.Message, Is.EqualTo("Lock wait timeout exceeded; try restarting transaction"));

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        // Modify() is allowed since row isn't locked anymore.
        ExecuteUpdateStatement(table2.Update().Where("id = 1").Set("age", 2));
        ExecuteSQLStatement(session2.SQL("COMMIT"));
      }
    }

    [Test]
    public void SharedLockAfterExclusiveLock()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Table table = session.Schema.GetTable("test");
        ExecuteSQLStatement(session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)"));
        Table table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        RowResult rowResult = ExecuteSelectStatement(table.Select().Where("id = 1").LockExclusive());
        Assert.That(rowResult.FetchAll(), Has.One.Items);

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since row isn't locked.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 2").LockShared());
        Assert.That(rowResult.FetchAll(), Has.One.Items);
        // Session2 blocks due to LockExclusive() not allowing to read locked rows.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockShared()));
        Assert.That(ex.Message, Is.EqualTo("Lock wait timeout exceeded; try restarting transaction"));

        // Session unlocks rows.
        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        // Row can now be recovered.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 1").LockShared());
        Assert.That(rowResult.FetchAll(), Has.One.Items);
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Test]
    public void ExclusiveLockAfterSharedLock()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        Table table = session.Schema.GetTable("test");
        ExecuteSQLStatement(session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)"));
        Table table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        RowResult rowResult = ExecuteSelectStatement(table.Select().Where("id in (1, 3)").LockShared());
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(2));

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));
        // Should return immediately since row isn't locked.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 2").LockExclusive());
        // Should return immediately due to LockShared() allows reading by other sessions.
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 2").LockShared());
        Assert.That(rowResult.FetchAll(), Has.One.Items);
        // Session2 blocks due to to LockExclusive() not allowing to read locked rows.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive()));
        Assert.That(ex.Message, Is.EqualTo("Lock wait timeout exceeded; try restarting transaction"));

        // Session unlocks rows.
        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        rowResult = ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive());
        Assert.That(rowResult.FetchAll(), Has.One.Items);
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Test]
    public void SelectWithInOperator()
    {
      Table table = testSchema.GetTable("test");
      Assert.That(ExecuteSelectStatement(table.Select()).FetchAll().Count, Is.EqualTo(3));

      Assert.That(ExecuteSelectStatement(table.Select().Where("name IN (\"jonh doe\", \"milton green\")")).FetchAll().Count, Is.EqualTo(2));
      Assert.That(ExecuteSelectStatement(table.Select().Where("name NOT IN (\"jonh doe\", \"milton green\")")).FetchAll(), Has.One.Items);
      Assert.That(ExecuteSelectStatement(table.Select().Where("name IN (\"\", \"\")")).FetchAll(), Is.Empty);
      Assert.That(ExecuteSelectStatement(table.Select().Where("\"\" IN (1,2,3)")).FetchAll(), Is.Empty);
      Assert.That(ExecuteSelectStatement(table.Select().Where("name IN ('', '')")).FetchAll(), Is.Empty);
      Assert.That(ExecuteSelectStatement(table.Select().Where("'' IN (1,2,3)")).FetchAll(), Is.Empty);
      Assert.That(ExecuteSelectStatement(table.Select().Where("'' IN ('')")).FetchAll().Count, Is.EqualTo(3));

      Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select().Where("name NOT IN [\"jonh doe\", \"milton green\"]")).FetchAll());
      Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select().Where("a IN [3]")).FetchAll());
      Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select().Where("3 IN a")).FetchAll());
    }

    [Test]
    public void Grouping()
    {
      ExecuteSQLStatement(GetSession().SQL("set sql_mode = 'STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';"));
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

      Assert.That(ExecuteInsertStatement(statement).AffectedItemsCount, Is.EqualTo(4));

      // GroupBy returns 5 rows since age 45 and 24 is repeated.
      var result = ExecuteSelectStatement(table.Select().GroupBy("age"));
      Assert.That(result.FetchAll().Count, Is.EqualTo(5));

      // GroupBy with null.
      result = ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy(null));
      Assert.That(result.FetchAll().Count, Is.EqualTo(7));
      result = ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy(null, null));
      Assert.That(result.FetchAll().Count, Is.EqualTo(7));
      result = ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy(null, "age"));
      Assert.That(result.FetchAll().Count, Is.EqualTo(5));

      // Having operation.
      // Having reduces the original 5 rows to 3 since 2 rows have a cnt=2, due to the repeated names.
      result = ExecuteSelectStatement(table.Select("id", "count(name) as cnt", "age").GroupBy("age").Having("cnt = 1"));
      Assert.That(result.FetchAll().Count, Is.EqualTo(3));

      // Having with null.
      result = ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having(null));
      Assert.That(result.FetchAll().Count, Is.EqualTo(5));

      // GroupBy with invalid field name.
      var ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy("none")));
      Assert.That(ex.Message, Is.EqualTo("Unknown column 'none' in 'group statement'"));

      // GroupBy with empty strings.
      var ex2 = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy("")));
      Assert.That(ex2.Message, Is.EqualTo("No more tokens when expecting one at token pos 0"));
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy(" ")));
      Assert.That(ex2.Message, Is.EqualTo("No more tokens when expecting one at token pos 0"));
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "name as Name", "age as Age").GroupBy(string.Empty)));
      Assert.That(ex2.Message, Is.EqualTo("No more tokens when expecting one at token pos 0"));

      // Having with invalid field name.
      ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having("none = 1")));
      Assert.That(ex.Message, Is.EqualTo("Unknown column 'none' in 'having clause'"));

      // Having with empty strings.
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having("")));
      Assert.That(ex2.Message, Is.EqualTo("Unable to parse query ''"));
      Assert.That(ex2.InnerException.Message, Is.EqualTo("No more tokens when expecting one at token pos 0"));
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having(" ")));
      Assert.That(ex2.Message, Is.EqualTo("Unable to parse query ' '"));
      Assert.That(ex2.InnerException.Message, Is.EqualTo("No more tokens when expecting one at token pos 0"));
      ex2 = Assert.Throws<ArgumentException>(() => ExecuteSelectStatement(table.Select("id as ID", "count(name) as cnt", "age as Age").GroupBy("age").Having(string.Empty)));
      Assert.That(ex2.Message, Is.EqualTo("Unable to parse query ''"));
      Assert.That(ex2.InnerException.Message, Is.EqualTo("No more tokens when expecting one at token pos 0"));
    }

    /// <summary>
    /// Bug-29838254
    /// RESULTSET ERROR WHEN SELECT IS ISSUED WITH IN OPERATOR WITH BLANKS WITH 8.0.17 SERVER-C/NET8.0.17TRUNK
    /// </summary>
    [Test]
    public void SelectWithInBlanksAndBrackets()
    {
      Session session = GetSession();
      Schema schema = session.GetSchema("test");
      object[][] allRows =
      {
        new object[] {1, "john doe", 38},
        new object[] {2, "milton green", 45},
        new object[] {3, "milton blue", 46},
        new object[] {4, "milton red", 47},
        new object[] {5, "milton yellow", 48},
        new object[] {6, "milton check", 49},
        new object[] {7, "milton pink", 14},
        new object[] {8, "milton beize", 25},
        new object[] {9, "milton silver", 35},
        new object[] {10, "milton city", 65}
      };
      session.SQL("USE test").Execute();
      session.SQL("DROP table if exists test").Execute();
      var r = session.SQL("CREATE TABLE test.test(id INT,name VARCHAR(45), age INT)").Execute();
      var rows = r.HasData ? r.FetchAll() : null;
      testSchema = session.GetSchema("test");
      var insertStatement = testSchema.GetTable("test").Insert();
      var rowsToInsert = 10;
      for (var i = 0; i < rowsToInsert; i++)
        insertStatement.Values(allRows[i]);
      insertStatement.Execute();
      var table = testSchema.GetTable("test");
      int count = 0;

      //Exception expected when square brackets are used instead of parenthesis
      Assert.Throws<MySqlException>(() => table.Select().Where("name IN ['', ' ']").Execute().FetchAll());

      // Test using parenthesis should return result
      count = table.Select().Where("name IN (\"john doe\", \"milton green\")").Execute().FetchAll().Count;
      Assert.That(count > 0);

      // Using parenthesis should return empty resultset for empty parameters
      count = table.Select().Where("name IN ('', ' ')").Execute().FetchAll().Count;
      Assert.That(count == 0);
    }

    [TestCase(":hobbies IN additionalinfo->$.hobbies", "hobbies", "painting", 3)]
    [TestCase(":hobbies IN additionalinfo->$.hobbies", "hobbies", "[\"boxing\", \"running\"]", 0)]
    [TestCase("[\"boxing\", \"running\"] IN additionalinfo->$.hobbies", null, null, 2)]
    [TestCase(":hobbies IN additionalinfo$.hobbies", "hobbies", "painting", 3)]
    public void InOperatorBindingJson(string condition, string bind, string value, int id)
    {
      Table table = testSchema.GetTable("test");
      Assert.That(ExecuteSelectStatement(table.Select()).FetchAll().Count, Is.EqualTo(3));

      var stmt = table.Select().Where(condition);
      if (bind != null) stmt.Bind(bind, value);
      var result = ExecuteSelectStatement(stmt).FetchAll();
      Assert.That(result.Count, Is.EqualTo(id == 0 ? 0 : 1));
      if (id > 0)
      {
        Assert.That(result[0]["id"], Is.EqualTo(id));
      }
    }

    #region WL14389

    [Test, Description("Reading locked document(lock_shared) in a table using lock_exclusive with DEFAULT waiting option.")]
    public void ExclusiveLockAfterSharedLockDefaultWaiting()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

      session.SQL("DROP TABLE IF EXISTS test.test").Execute();
      session.SQL("CREATE TABLE test.test (id INT, a INT)").Execute();
      var table1 = testSchema.GetTable("test");
      table1.Insert("id", "a").Values(1, 1).Values(2, 2).Values(3, 3).Execute();
      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        var table = session.Schema.GetTable("test");
        session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        var table2 = session2.GetSchema("test").GetTable("test");

        ExecuteSQLStatement(session.SQL("START TRANSACTION"));
        var rowResult = ExecuteSelectStatement(table.Select().Where("id = 1").LockShared());
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        ExecuteSQLStatement(session2.SQL("START TRANSACTION"));

        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 2").LockExclusive(LockContention.Default).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks as LockExclusive trying to access locked document
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Exception ex = Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive(LockContention.Default)));
        Assert.That(ex.Message, Is.EqualTo("Lock wait timeout exceeded; try restarting transaction"));

        // Session2 blocks due to to LockShared() not allowing to modify locked documents.
        Result result1;
        ex = Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());
        Assert.That(ex.Message, Is.EqualTo("Lock wait timeout exceeded; try restarting transaction"));

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        Assert.That(result.AffectedItemsCount, Is.EqualTo(1));

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Test, Description("Reading exclusively locked document in a table using lock_shared with NOWAIT waiting option. ")]
    public void SharedLockAfterExclusiveLockWithNoWait()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

      session.SQL("DROP TABLE IF EXISTS test.test").Execute();
      session.SQL("CREATE TABLE test.test (id INT, a INT)").Execute();
      var table1 = testSchema.GetTable("test");
      table1.Insert("id", "a").Values(1, 1).Values(2, 2).Values(3, 3).Execute();
      ExecuteSQLStatement(session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED"));
        var table = session.Schema.GetTable("test");
        session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        var table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        var rowResult = table.Select().Where("id = 1").LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 2").LockShared(LockContention.NoWait).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockShared(LockContention.NoWait)));

        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        Assert.That(result.AffectedItemsCount, Is.EqualTo(1));

        ExecuteSQLStatement(session.SQL("ROLLBACK"));
        ExecuteSQLStatement(session2.SQL("ROLLBACK"));
      }
    }

    [Test, Description("Testing Non ASCII characters for utf8mb4 characterset")]
    public void NonAsciiCharsForUtf8mb4()
    {
      //Bug28261283
      string record = "{\"name\": \"New\",\"age\": 4 , ";
      string name1 = "\u201C\u2199\u2197\u2196\u2198\u201D";
      string name2 = "{\"age\": 1, \"misc\": 1.2, \"name\": \"\u201C\u2199\u2197\u2196\u2198\u201D\"}";

      session.SQL($"use {schemaName}").Execute();
      session.SQL("drop table if exists newTable").Execute();
      session.SQL("create table newTable(c1 varchar(200), c2 JSON) CHARACTER SET utf8mb4").Execute();
      var db = session.GetSchema(schemaName);
      Table tabNew = db.GetTable("newTable");

      tabNew.Insert("c1", "c2").Values("\u201C\u2199\u2197\u2196\u2198\u201D", "{ \"name\": \"\u201C\u2199\u2197\u2196\u2198\u201D\", \"age\": 1 , \"misc\": 1.2}").Execute();
      tabNew.Insert().Values("abcNewÂ£Â¢â‚¬Â©Â§Â°âˆš", "{ \"name\": \"abcNewÂ£Â¢â‚¬Â©Â§Â°âˆš\", \"age\": 2 , \"misc\": 1.2}").Execute();
      RowResult result = tabNew.Select("c1", "c2").Where("c1 ='\u201C\u2199\u2197\u2196\u2198\u201D'").Execute();
      var r1 = result.FetchOne();
      Assert.That(r1[0].ToString(), Is.EqualTo(name1));//"“↙↗↖↘”"
      Assert.That(r1[1].ToString(), Is.Not.EqualTo(name2));//"{\"age\": 1, \"misc\": 1.2, \"name\": \"??????????????????\"}"
      var t = session.SQL("SELECT c1, CONVERT(c2 USING utf8mb4) FROM newTable WHERE c1 = '\u201C\u2199\u2197\u2196\u2198\u201D'").Execute();
      r1 = t.FetchOne();
      Assert.That(r1[0].ToString(), Is.EqualTo(name1));//"“↙↗↖↘”"
      Assert.That(r1[1].ToString(), Is.EqualTo(name2));//"{\"age\": 1, \"misc\": 1.2, \"name\": \"??????????????????\"}"

      Collection coll = CreateCollection("test");

      int klen = 1024 * 64;
      string[] key = new string[klen];
      int dlen = 1024 * 100;
      string[] data = new string[dlen];
      Array.Resize<string>(ref key, 'S');
      Array.Resize<string>(ref data, '$');
      string unicodeString = "Maths use \u03a0 (Pi) for calculations";
      // You can convert a string into a byte array
      byte[] asciiBytes = Encoding.ASCII.GetBytes(unicodeString);
      // You can convert a byte array into a char array
      char[] asciiChars = Encoding.ASCII.GetChars(asciiBytes);
      string asciiString = new string(asciiChars);

      record = record + "\"";
      record = record + "\":\"";
      record = record + asciiString;
      record = record + "\"}";

      Result r = coll.Add("{ \"name\": \"\u201C\u2199\u2197\u2196\u2198\u201D\", \"age\": 4 , \"misc\": 1.2}").
          Add("{ \"name\": \"xyzÂ£Â¢â‚¬Â©Â§Â°âˆš\", \"age\": 6 , \"misc\": 10}").Execute();
      var docs = coll.Find("name = '\u201C\u2199\u2197\u2196\u2198\u201D' and age>= 4 and age <= 6").Execute();
      DbDoc doc = docs.FetchOne();
      Assert.That(doc["name"], Is.EqualTo(name1));
      session.SQL("drop table if exists test").Execute();
      session.SQL("drop table if exists newTable").Execute();
    }

    [Test, Description("Test MySQLX plugin Datatype Tests")]
    public void DatatypesOnCreateTable()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

      session.SQL($"use {schemaName}");
      session.SQL("create table test1(c1 int,c2 double GENERATED ALWAYS AS (c1*101/102) Stored COMMENT 'First Gen Col',c3 Json GENERATED ALWAYS AS (concat('{\"F1\":',c1,'}')) VIRTUAL COMMENT 'Second Gen /**/Col', c4 bigint GENERATED ALWAYS as (c1*10000) VIRTUAL UNIQUE KEY Comment '3rd Col' NOT NULL)").Execute();
      session.SQL("insert into test1(c1) values(1000)").Execute();
      RowResult r = session.GetSchema(schemaName).GetTable("test1").Select("c1").Execute();
      r.FetchAll();
      Assert.That(r.Rows[0][0].ToString(), Is.EqualTo("1000"), "Matching the values");
      session.SQL("drop table if exists test1").Execute();
    }

    [Test, Description("Test MySQLX plugin Table Expression using Where")]
    public void TableExpressionWhereBindGroupBy()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

      string match = null;
      session.SQL("create table test1(name VARCHAR(40), age INT)").Execute();
      Table table = session.GetSchema(schemaName).GetTable("test1");

      for (int i = 10; i < 30; i++)
      {
        int j = 1;
        string name = RandomString(i);
        var result = table.Insert("name", "age")
          .Values(name, i)
          .Execute();
        if (i == 28)
          match = name;
        Assert.That(result.AffectedItemsCount, Is.EqualTo((ulong)j), "Matching the values");
        j = j + 1;
      }

      var whereResult = table.Select("name", "age").Where("age == 28").Execute();
      int rowCount = whereResult.FetchAll().Count;
      Assert.That(rowCount, Is.EqualTo(1), "Matching the row count");
      Assert.That(whereResult.Rows.ToArray()[0][0].ToString(), Is.EqualTo(match), "Matching the name");

      whereResult = table.Select("name", "age").Where("age == 100").Execute();
      rowCount = whereResult.FetchAll().Count;
      Assert.That(rowCount, Is.EqualTo(0), "Matching that there is no such value ");

      whereResult = table.Select().Where("name like :name").Bind("nAme", "%ABCDEFGHIJ%").Execute();
      rowCount = whereResult.FetchAll().Count;
      for (int i = 0; i < whereResult.Rows.Count; i++)
      {
        var res = whereResult.Rows.ToArray()[i][0];
        Assert.That(res.ToString(), Does.Contain("ABCDEFGHIJ"));
      }

      whereResult = table.Select().Where("name like :name").Bind("nAme", "%ABCD%").Execute();
      rowCount = whereResult.FetchAll().Count;
      for (int j = 0; j < whereResult.Rows.Count; j++)
      {
        var res = whereResult.Rows.ToArray()[j][0];
        Assert.That(res.ToString(), Does.Contain("ABCD"));
      }
    }

    [Test, Description("Test MySQLX plugin Table")]
    public void TableSelectFetchValuesInOrder()
    {
      ExecuteSQL("drop table if exists test1");
      ExecuteSQL("create table test1(name VARCHAR(40), age INT)");
      Table table = session.GetSchema(schemaName).GetTable("test1");
      var result = table.Insert("name", "age")
        .Values("MARK", 34)
        .Execute();

      Assert.That(result.AffectedItemsCount, Is.EqualTo((ulong)1), "Matching the values");
      result = table.Insert("name", "age")
      .Values("richie", 16)
      .Execute();
      Assert.That(result.AffectedItemsCount, Is.EqualTo((ulong)1), "Matching the values");
      var selectResult = table.Select().Execute();

      while (selectResult.Next()) ;
      Assert.That(selectResult.Rows.Count, Is.EqualTo(2), "Matching the row count");
      Assert.That(selectResult.Rows.ToArray()[0][0].ToString(), Is.EqualTo("MARK"), "Matching the value MARK");
      Assert.That((int)selectResult.Rows.ToArray()[0][1], Is.EqualTo(34), "Matching the age 34");
      Assert.That(selectResult.Rows.ToArray()[1][0].ToString(), Is.EqualTo("richie"), "Matching the value richie");
      Assert.That((int)selectResult.Rows.ToArray()[1][1], Is.EqualTo(16), "Matching the age 16");

      var result1 = testSchema.GetTable("test").Select().OrderBy("age desc").Execute();
      int rowCount = result1.FetchAll().Count;
      for (int i = 0; i < rowCount; i++)
      {
        Assert.That(result1.Rows[0][2].ToString(), Is.EqualTo("45"), "Matching the values");
        Assert.That(result1.Rows[1][2].ToString(), Is.EqualTo("38"), "Matching the values");
        Assert.That(result1.Rows[2][2].ToString(), Is.EqualTo("24"), "Matching the values");
      }

      result1 = testSchema.GetTable("test").Select().OrderBy("age asc").Execute();
      rowCount = result1.FetchAll().Count;
      for (int i = 0; i < rowCount; i++)
      {
        Assert.That(result1.Rows[0][2].ToString(), Is.EqualTo("24"), "Matching the values");
        Assert.That(result1.Rows[1][2].ToString(), Is.EqualTo("38"), "Matching the values");
        Assert.That(result1.Rows[2][2].ToString(), Is.EqualTo("45"), "Matching the values");
      }
    }

    [Test, Description("Reading exclusively locked document in a table using lock_exclusive with NOWAIT waiting option. ")]
    public void DoubleExclusiveLockWithNoWait()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

      session.SQL("DROP TABLE IF EXISTS test.test").Execute();
      session.SQL("CREATE TABLE test.test (id INT, a INT)").Execute();
      var table1 = testSchema.GetTable("test");
      table1.Insert("id", "a").Values(1, 1).Values(2, 2).Values(3, 3).Execute();
      ExecuteSQLStatement(session.SQL("SET autocommit = 0"));
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        ExecuteSQLStatement(session2.SQL("SET autocommit = 0"));
        var table = testSchema.GetTable("test");
        session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        var table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        var rowResult = table.Select().Where("id = 1").LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 2").LockExclusive(LockContention.NoWait).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockShared(LockContention.NoWait)));

        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        Assert.That(result.AffectedItemsCount, Is.EqualTo(1));
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }

      ExecuteSQLStatement(session.SQL("SET autocommit = 1"));
    }

    [Test, Description("Reading a locked document(lock_shared) in a table using lock_shared with NOWAIT waiting option. ")]
    public void DoubleSharedLockWithNoWait()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

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
        rowResult = table2.Select().Where("id = 2").LockShared(LockContention.NoWait).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 doesnt block as LockShare trying to read locked(Lock_shared) document
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        rowResult = table2.Select().Where("id = 1").LockShared(LockContention.NoWait).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks due to to LockShared() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        Assert.That(result.AffectedItemsCount, Is.EqualTo(1));
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Reading a locked document(lock_shared) in a table using lock_shared with NOWAIT waiting option. ")]
    public void ExclusiveLockAfterSharedLockWithNoWait()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

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
        var rowResult = table.Select().Where("id = 1").LockShared().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 2").LockExclusive(LockContention.NoWait).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks as LockExclusive() trying to access locked(LockShared) documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive(LockContention.NoWait)));

        // Session2 blocks due to to LockShared() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        Assert.That(result.AffectedItemsCount, Is.EqualTo(1));
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Reading exclusively locked document in a table using lock_shared with SKIPLOCK waiting option. ")]
    public void SharedLockAfterExclusiveWithSkiplock()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

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
        rowResult = table2.Select().Where("id = 2").LockShared(LockContention.NoWait).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        rowResult = table2.Select().Where("id = 1").LockShared(LockContention.SkipLocked).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(0), "Matching the document ID");

        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the record count");

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Reading exclusively locked document in a table using lock_exclusive with SKIPLOCK waiting option.")]
    public void DoubleExclusiveLockWithSkiplock()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

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
        rowResult = table2.Select().Where("id = 2").LockExclusive(LockContention.SkipLocked).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 doesn't block as SKIPLOCK used
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        rowResult = table2.Select().Where("id = 1").LockExclusive(LockContention.SkipLocked).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(0), "Matching the document ID");
        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the record count");
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Reading a locked document(lock_shared) in a table using lock_shared with SKIPLOCK waiting option. ")]
    public void DoubleSharedLockWithSkiplock()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

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
        rowResult = table2.Select().Where("id = 2").LockShared(LockContention.SkipLocked).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 doesn't block as SKIPLOCK being used
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        rowResult = table2.Select().Where("id = 1").LockShared(LockContention.SkipLocked).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks due to to LockShared() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the record count");
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Reading a locked document(lock_shared) in a table using lock_exclusive with SKIPLOCK waiting option.")]
    public void ExclusiveLockAfterSharedLockWithSkiplock()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

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
        var rowResult = table.Select().Where("id = 1").LockShared().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 2").LockExclusive(LockContention.SkipLocked).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 doesn't block as SKIPLOCK being used
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        rowResult = table2.Select().Where("id = 1").LockExclusive(LockContention.SkipLocked).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(0), "Matching the document ID");

        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the record count");
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Reading an exclusively locked document in a table using lock_shared without any waiting option. ")]
    public void ExclusiveLockBeforeSharedLockWithoutAwaiting()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

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
        rowResult = table2.Select().Where("id = 2").LockShared().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockShared()));

        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the record count");
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Reading an exclusively locked document in a table using lock_exclusive without any waiting option.")]
    public void DoubleExclusiveLockWithoutWaiting()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

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
        rowResult = table2.Select().Where("id = 2").LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive()));

        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("age", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("age", 2).Execute();
        Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the record count");
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Reading a locked document(lock_shared) in a table using lock_shared without any waiting option.")]
    public void DoubleSharedLockWithoutWaiting()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

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
        rowResult = table2.Select().Where("id = 2").LockShared().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        rowResult = table2.Select().Where("id = 1").LockShared().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the record count");
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Reading a locked document(lock_shared) in a table using lock_exclusive without any waiting option. ")]
    public void SharedLockBeforeExclusiveLockWithoutWaiting()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

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
        var rowResult = table.Select().Where("id = 1").LockShared().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 2").LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks as LockExclusive() is trying to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive()));

        // Session2 blocks due to to LockShared() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("a", 2).Execute());

        // Session2 returns immediately as session is committed.
        session.Commit();
        var result = table2.Update().Where("id = 1").Set("a", 2).Execute();
        Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Matching the record count");
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Reading a locked document(shared/exclusive) in a table using SKIPLOCK and NOWAIT waiting options when CRUD is happening parallely")]
    public void SingleLockExclusiveWithNoWaitAndSkip()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

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
        var result1 = table.Update().Where("id = 1").Set("a", 2).Execute();

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        var rowResult1 = table2.Select().Where("id = 2").LockExclusive(LockContention.NoWait).Execute();
        Assert.That(rowResult1.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive(LockContention.NoWait)));
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockShared(LockContention.NoWait)));

        var rowResult = table2.Select().Where("id = 1").LockShared(LockContention.SkipLocked).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(0), "Matching the document ID");

        rowResult = table2.Select().Where("id = 1").LockExclusive(LockContention.SkipLocked).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(0), "Matching the document ID");

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Multiple lock calls on a document in table using NOWAIT and SKIPLOCK waiting options")]
    public void ChainedExclusiveLocks()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

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
        var rowResult = table.Select().Where("id = 1").LockShared().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        session2.SQL("START TRANSACTION").Execute();
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Assert.Throws<MySqlException>(() => rowResult = table2.Select().Where("id = 1").LockExclusive(LockContention.SkipLocked).LockExclusive(LockContention.NoWait).Execute());

        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1")
                                                    .LockExclusive(LockContention.SkipLocked)
                                                    .LockExclusive(LockContention.NoWait)));

        rowResult = table2.Select().Where("id = 1").LockExclusive(LockContention.SkipLocked).LockShared(LockContention.SkipLocked).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");

        rowResult = table2.Select().Where("id = 1").LockShared(LockContention.SkipLocked).LockExclusive(LockContention.SkipLocked).Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(0), "Matching the document ID");
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
    }

    [Test, Description("Test MySQLX plugin - MYSQLCNET 755 Table GetIncrementValue")]
    public void TableGetAutoIncrementValue()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 11), "This test is for MySql 8.0.11 or higher");

      session.SQL("CREATE TABLE address1" +
                  "(address_number1  MEDIUMINT NOT NULL AUTO_INCREMENT, " +
                  "address_number2   CHAR(100) NOT NULL, " +
                  "PRIMARY KEY (address_number1)" + ");").Execute();
      Table table = testSchema.GetTable("address1");
      var result = table.Insert("address_number1", "address_number2")
         .Values(100, "Test the document id in address table")
         .Execute();

      var docId = result.AutoIncrementValue;
      Assert.That(docId.ToString(), Is.EqualTo("100"), "Matching the value if already it is inserted");

      result = table.Insert("address_number2")
         .Values("Test the document id by 2nd insert without id in address table")
         .Execute();
      docId = result.AutoIncrementValue;
      Assert.That(docId.ToString(), Is.EqualTo("101"), "Matching the value if already it is inserted");

      session.SQL("CREATE TABLE address2" +
                  "(address_number3  INT NOT NULL AUTO_INCREMENT, " +
                  "address_number4   CHAR(100) NOT NULL, " +
                  "PRIMARY KEY (address_number3)" + ");").Execute();
      table = testSchema.GetTable("address2");
      result = table.Insert("address_number4")
         .Values("Test the document id in address table without unique id")
         .Execute();

      docId = result.AutoIncrementValue;
      Assert.That(docId.ToString(), Is.EqualTo("1"), "Matching the auto increment value");

      session.SQL("CREATE TABLE address3" +
      "(address_number5  MEDIUMINT NOT NULL AUTO_INCREMENT, " +
      "address_number6   CHAR(100) NOT NULL, " +
      "PRIMARY KEY (address_number5)" + ");").Execute();
      table = testSchema.GetTable("address3");
      result = table.Insert("address_number5", "address_number6")
         .Values(100, "Test multiple document ids in address table - 1st document")
         .Values(200, "Test multiple document ids in address table - 2nd document")
         .Values(300, "Test multiple document ids in address table - 3rd document")
         .Execute();

      docId = result.AutoIncrementValue;
      Assert.That(docId.ToString(), Is.EqualTo("300"), "Matching the value if more than one documents inserted");

      result = table.Insert("address_number6")
        .Values("Test multiple document ids in address table - 4th document without ID")
        .Values("Test multiple document ids in address table - 5th document without ID")
        .Values("Test multiple document ids in address table - 6th document without ID")
        .Execute();

      docId = result.AutoIncrementValue;
      Assert.That(docId.ToString(), Is.EqualTo("301"), "Matching the value if more than one documents inserted");

      session.SQL("CREATE TABLE address4" +
                  "(address_number7  INT NOT NULL AUTO_INCREMENT, " +
                  "address_number8   CHAR(100) NOT NULL, " +
                  "PRIMARY KEY (address_number7)" + ");").Execute();
      table = testSchema.GetTable("address4");
      result = table.Insert("address_number8")
         .Values("Test the document ids in address table without unique id - 1st document")
         .Values("Test the document ids in address table without unique id - 2nd document")
         .Values("Test the document ids in address table without unique id - 3rd document")
         .Execute();

      docId = result.AutoIncrementValue;
      Assert.That(docId.ToString(), Is.EqualTo("1"), "Matching the auto increment value");

      result = table.Insert("address_number8")
       .Values("Test the document ids in address table without unique id - 4th document")
       .Values("Test the document ids in address table without unique id - 5th document")
       .Values("Test the document ids in address table without unique id - 6th document")
       .Execute();

      docId = result.AutoIncrementValue;
      Assert.That(docId.ToString(), Is.EqualTo("4"), "Matching the auto increment value");

      session.SQL("CREATE TABLE address5" +
               "(address_number9  INT, " +
               "address_number10   CHAR(100));").Execute();

      session.SQL("ALTER TABLE address5 ADD c INT UNSIGNED NOT NULL AUTO_INCREMENT, ADD PRIMARY KEY(c)").Execute();

      table = testSchema.GetTable("address5");
      result = table.Insert("address_number9", "address_number10")
         .Values(100, "Test the document ids in address table without unique id - 1st document")
         .Values(200, "Test the document ids in address table without unique id - 2nd document")
         .Values(300, "Test the document ids in address table without unique id - 3rd document")
         .Execute();

      docId = result.AutoIncrementValue;
      Assert.That(docId.ToString(), Is.EqualTo("1"), "Matching the auto increment value");
      session.SQL("drop table if exists address1").Execute();
      session.SQL("drop table if exists address2").Execute();
      session.SQL("drop table if exists address3").Execute();
      session.SQL("drop table if exists address4").Execute();
      session.SQL("drop table if exists address5").Execute();
    }

    [Test, Description("Test MySQLX plugin - MYSQLCNET_684 Fetchone returns null when no rows")]
    public void FetchoneReturnsNullNoRows()
    {
      ExecuteSQL("CREATE TABLE test1(id INT)");
      ExecuteSQL("INSERT INTO test1 VALUES (1),(2),(3),(4)");
      ExecuteSQL("CREATE TABLE test2(id INT, val INT)");
      ExecuteSQL("INSERT INTO test2 VALUES (1,0)");
      var rowResult = testSchema.GetTable("test1").Select("id").Execute();
      foreach (var row in rowResult)
      {
        var result = testSchema.GetTable("test2").Update().Where("id=1").Set("val", row["id"]).Execute();
        //WL11843-Core API v1 alignment Changes
        Assert.That(result.AffectedItemsCount, Is.EqualTo(1), "Matching");
      }

      Row valRow = testSchema.GetTable("test2").Select("val").Execute().FetchOne();
      Assert.That(valRow[0].ToString(), Is.EqualTo("4"), "Matching");
      ExecuteSQL("DELETE FROM test2 WHERE id=1");
      valRow = testSchema.GetTable("test2").Select("val").Execute().FetchOne();
      Assert.That(valRow, Is.Null);
      ExecuteSQL("DROP TABLE if exists test1");
      ExecuteSQL("DROP TABLE if exists test2");
    }

    [Test, Description("Test MySQLX plugin - MYSQLCNET_684 Fetchone returns null when no table")]
    public void FetchoneReturnsNullNoTable()
    {
      ExecuteSQL("CREATE TABLE test111(id INT)");
      ExecuteSQL("INSERT INTO test111 VALUES (1),(2),(3),(4)");
      ExecuteSQL("CREATE TABLE test222(id INT, val INT)");
      ExecuteSQL("INSERT INTO test222 VALUES (1,0)");
      var rowResult = testSchema.GetTable("test111").Select("id").Execute();
      foreach (var row in rowResult)
      {
        var result = testSchema.GetTable("test222").Update().Where("id=1").Set("val", row["id"]).Execute();
        //WL11843-Core API v1 alignment Changes
        Assert.That(result.AffectedItemsCount, Is.EqualTo(1), "Matching");
      }

      Row valRow = testSchema.GetTable("test222").Select("val").Execute().FetchOne();
      Assert.That(valRow[0].ToString(), Is.EqualTo("4"), "Matching");
      ExecuteSQL("DROP TABLE test222");
      Assert.Throws<MySqlException>(() => valRow = testSchema.GetTable("test222").Select("val").Execute().FetchOne());
      ExecuteSQL("DROP TABLE IF EXISTS test111");
    }

    [Test, Description("MySQLX CNET-Test Table.Select() with exclusive lock and Table.Update() normal from two sessions. ")]
    public void DoubleChainedLocksWithTwoSessions_S1()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      session.SQL("SET autocommit = 0").Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET autocommit = 0").Execute();
        var table = session.Schema.GetTable("test");
        session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        var table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        var rowResult = table.Select().Where("id = 1").LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 2").LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive()));

        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("age", 2).Execute());

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }

      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET autocommit = 0").Execute();
        var table = session.Schema.GetTable("test");
        var table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        var rowResult = table.Select().Where("id = 1").LockShared().LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 2").LockShared().LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockShared().LockExclusive()));

        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("age", 2).Execute());
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
      session.SQL("SET autocommit = 1").Execute();
    }

    [Test, Description("MySQLX CNET-Test Table.Select() with shared lock and Table.Update() normal from two sessions. ")]
    public void DoubleChainedLocksWithTwoSessions_S2()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      session.SQL("SET autocommit = 0").Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET autocommit = 0").Execute();
        var table = session.Schema.GetTable("test");
        session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        var table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        var rowResult = table.Select().Where("id = 1").LockShared().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 1").Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        ExecuteSQLStatement(session2.SQL("SET SESSION innodb_lock_wait_timeout=1"));
        var result = table2.Update().Where("id = 2").Set("age", 30).Execute();
        Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Match being done");
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive()));

        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("age", 2).Execute());

        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }

      session.SQL("SET autocommit = 0").Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET autocommit = 0").Execute();
        var table = session.Schema.GetTable("test");
        var table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        var rowResult = table.Select().Where("id = 1").LockExclusive().LockShared().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 1").Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        var result = table2.Update().Where("id = 2").Set("age", 30).Execute();
        Assert.That((int)result.AffectedItemsCount, Is.EqualTo(1), "Match being done");
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockShared().LockExclusive()));

        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Result result1;
        Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("age", 2).Execute());
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
      session.SQL("SET autocommit = 1").Execute();
    }

    [Test, Description("MySQLX CNET-Test Table.Select() with exclusive lock from two sessions. ")]
    public void SingleExclusiveLock()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      session.SQL("SET autocommit = 0").Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET autocommit = 0").Execute();
        var table = session.Schema.GetTable("test");
        session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        var table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        var rowResult = table.Select().Where("id = 1").LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");

        session2.SQL("START TRANSACTION").Execute();
        // Should return immediately since document isn't locked.
        rowResult = table2.Select().Where("id = 2").Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");

        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive()));
        session.SQL("ROLLBACK").Execute();
        session2.SQL("ROLLBACK").Execute();
      }
      session.SQL("SET autocommit = 1").Execute();
    }

    [Test, Description("MySQLX CNET-Test Table.Select() with exclusive lock and Table.Select() with exclusive lock from two sessions-Select multiple records")]
    public void SingleTransactionWithLocks()
    {
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      session.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
      using (var session2 = MySQLX.GetSession(ConnectionString))
      {
        session2.SQL("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED").Execute();
        session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
        var table = session.Schema.GetTable("test");
        var table2 = session2.GetSchema("test").GetTable("test");

        session.SQL("START TRANSACTION").Execute();
        var rowResult = table.Select().Where("id in (1,3)").LockShared().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(2), "Matching the record ID");

        rowResult = table2.Select().Where("id = 2").LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");
        rowResult = table2.Select().Where("id = 2").LockShared().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");
        // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
        session.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        table2.Select().Where("id = 2").LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");
        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive()));

        session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
        // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
        Assert.Throws<MySqlException>(() => ExecuteUpdateStatement(table2.Update().Where("id = 1").Set("age", 2)));

        session.SQL("ROLLBACK").Execute();
        rowResult = table2.Select().Where("id = 1").LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");
        session2.SQL("ROLLBACK").Execute();
        rowResult = table.Select().Where("id = 2").LockExclusive().Execute();
        Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the document ID");
      }
    }

    [Test, Description("MySQLX CNET-Test Table.Select() with exclusive lock and Table.Update() normal from two sessions-50 Iterations")]
    public void IteratedExclusiveLocks()
    {
      Assume.That(Platform.IsWindows(), "This test is only for Windows OS.");
      Assume.That(session.Version.isAtLeast(8, 0, 3), "This test is for MySql 8.0.3 or higher");

      session.SQL("SET autocommit = 0").Execute();
      session.SQL("CREATE UNIQUE INDEX myIndex ON test.test (id)").Execute();
      const int iterations = 30;
      for (var i = 0; i < iterations; i++)
      {
        using (var session2 = MySQLX.GetSession(ConnectionString))
        {
          session2.SQL("SET autocommit = 0").Execute();
          var table = session.Schema.GetTable("test");

          var table2 = session2.GetSchema("test").GetTable("test");

          session.SQL("START TRANSACTION").Execute();
          var rowResult = table.Select().Where("id = 1").LockExclusive().Execute();
          Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");

          session2.SQL("START TRANSACTION").Execute();
          // Should return immediately since document isn't locked.
          rowResult = table2.Select().Where("id = 2").LockExclusive().Execute();
          Assert.That(rowResult.FetchAll().Count, Is.EqualTo(1), "Matching the record ID");

          // Session2 blocks due to to LockExclusive() not allowing to read locked documents.
          session2.SQL("SET SESSION innodb_lock_wait_timeout=1").Execute();
          Assert.Throws<MySqlException>(() => ExecuteSelectStatement(table2.Select().Where("id = 1").LockExclusive()));

          // Session2 blocks due to to LockExclusive() not allowing to modify locked documents.
          Result result1;
          Assert.Throws<MySqlException>(() => result1 = table2.Update().Where("id = 1").Set("age", 2).Execute());
          session.SQL("ROLLBACK").Execute();
          session2.SQL("ROLLBACK").Execute();
        }
      }
      session.SQL("SET autocommit = 1").Execute();
    }

    #endregion WL14389

    #region Methods
    public static string RandomString(int length)
    {
      const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
      const string chars1 = "ABCDEFGHIJ";
      var random = new Random();
      if (length == 20)
      {
        return chars1;
      }
      else
      {
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
      }

    }
    #endregion Methods
  }
}
