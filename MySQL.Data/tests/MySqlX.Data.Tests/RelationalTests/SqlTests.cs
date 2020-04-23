// Copyright (c) 2015, 2020, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class SqlTests : BaseTest
  {
    [Test]
    public void ReturnSimpleScalar()
    {
      ExecuteSQL("CREATE TABLE test(id INT)");
      ExecuteSQL("INSERT INTO test VALUES (1)");
      SqlResult r = ExecuteSQLStatement(GetSession(true).SQL("SELECT * FROM test"));
      Assert.True(r.Next());
      Assert.AreEqual(1, r[0]);
      Assert.False(r.NextResult());
    }

    [Test]
    public void ExecuteStoredProcedure()
    {
      ExecuteSQL("CREATE PROCEDURE `my_proc` () BEGIN SELECT 5; END");

      Session session = GetSession(true);
      var result = ExecuteSQLStatement(session.SQL("CALL my_proc()"));
      Assert.True(result.HasData);
      var row = result.FetchOne();
      Assert.NotNull(row);
      Assert.AreEqual((sbyte)5, row[0]);
      Assert.False(result.Next());
      Assert.Null(result.FetchOne());
      Assert.False(result.NextResult());
    }

    [Test]
    public void ExecuteStoredProcedureMultipleResults()
    {
      ExecuteSQL("CREATE PROCEDURE `my_proc` () BEGIN SELECT 5; SELECT 'A'; SELECT 5 * 2; END");

      Session session = GetSession(true);
      var result = ExecuteSQLStatement(session.SQL("CALL my_proc()"));
      Assert.True(result.HasData);
      var row = result.FetchOne();
      Assert.NotNull(row);
      Assert.AreEqual((sbyte)5, row[0]);
      Assert.False(result.Next());
      Assert.Null(result.FetchOne());

      Assert.True(result.NextResult());
      row = result.FetchOne();
      Assert.NotNull(row);
      Assert.AreEqual("A", row[0]);
      Assert.False(result.Next());
      Assert.Null(result.FetchOne());

      Assert.True(result.NextResult());
      row = result.FetchOne();
      Assert.NotNull(row);
      Assert.AreEqual((sbyte)10, row[0]);
      Assert.False(result.Next());
      Assert.Null(result.FetchOne());

      Assert.False(result.NextResult());
    }

    [Test]
    public void Bind()
    {
      ExecuteSQL("CREATE TABLE test(id INT, letter varchar(1))");
      for (int i = 1; i <= 10; i++)
        ExecuteSQLStatement(GetSession(true).SQL("INSERT INTO test VALUES (?, ?), (?, ?)")
          .Bind(i, ((char)('@' + i)).ToString())
          .Bind(++i, ((char)('@' + i)).ToString()));

      SqlResult result = ExecuteSQLStatement(GetSession(true).SQL("select * from test where id=?").Bind(5));
      Assert.True(result.Next());
      Assert.That(result.Rows, Has.One.Items);
      Assert.AreEqual(5, result[0]);
      Assert.AreEqual("E", result[1]);
    }

    [Test]
    public void BindNull()
    {
      ExecuteSQL("CREATE TABLE test(id INT, letter varchar(1))");

      var session = GetSession(true);
      var result = ExecuteSQLStatement(session.SQL("INSERT INTO test VALUES(1, ?), (2, 'B');").Bind(null));
      Assert.AreEqual(2ul, result.AffectedItemsCount);

      var sqlResult = ExecuteSQLStatement(session.SQL("SELECT * FROM test WHERE letter is ?").Bind(null)).FetchAll();
      Assert.That(sqlResult, Has.One.Items);
      Assert.AreEqual(1, sqlResult[0][0]);
      Assert.Null(sqlResult[0][1]);
    }

    [Test]
    public void Alias()
    {
      var session = GetSession(true);
      var stmt = ExecuteSQLStatement(session.SQL("SELECT 1 AS UNO"));
      var result = stmt.FetchAll();
      Assert.AreEqual("UNO", stmt.Columns[0].ColumnLabel);
    }
  }
}
