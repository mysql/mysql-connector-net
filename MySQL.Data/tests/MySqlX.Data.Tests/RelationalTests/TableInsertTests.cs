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

using System.Linq;
using MySqlX.XDevAPI.Relational;
using Xunit;
using MySql.Data.MySqlClient;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class TableInsertTests : BaseTest
  {
    public TableInsertTests()
    {
    }

    [Fact]
    public void InsertMultipleValues()
    {
      ExecuteSQL("CREATE TABLE test.test(name VARCHAR(40), age INT)");
      Table table = testSchema.GetTable("test");

      var result = ExecuteInsertStatement(table.Insert("name", "age")
        .Values("Henry", "22")
        .Values("Patric", 30)
        );
      Assert.Equal<ulong>(2, result.AffectedItemsCount);

      var selectResult = ExecuteSelectStatement(table.Select());
      while (selectResult.Next()) ;
      Assert.Equal(2, selectResult.Rows.Count);
      Assert.Equal("Henry", selectResult.Rows.ToArray()[0][0]);
      Assert.Equal(22, selectResult.Rows.ToArray()[0][1]);
      Assert.Equal("Patric", selectResult.Rows.ToArray()[1][0]);
      Assert.Equal(30, selectResult.Rows.ToArray()[1][1]);

      Assert.Equal(2, table.Count());
    }

    [Fact]
    public void InsertExpressions()
    {
      ExecuteSQL("CREATE TABLE test.test(name VARCHAR(40), age INT)");
      Table table = testSchema.GetTable("test");

      var result = ExecuteInsertStatement(table.Insert("name", "age")
        .Values("upper('mark')", "50-16")
        );
      Assert.Equal<ulong>(1, result.AffectedItemsCount);

      var selectResult = ExecuteSelectStatement(table.Select());
      while (selectResult.Next()) ;
      Assert.Single(selectResult.Rows);
      Assert.Equal("MARK", selectResult.Rows.ToArray()[0][0]);
      Assert.Equal(34, selectResult.Rows.ToArray()[0][1]);
    }

    [Fact]
    public void ReuseStatement()
    {
      ExecuteSQL("CREATE TABLE test(name VARCHAR(40), age INT)");
      Table table = testSchema.GetTable("test");

      var stmt = table.Insert("name", "age");
      var result = ExecuteInsertStatement(stmt.Values("upper('mark')", "50-16"));
      Assert.Equal<ulong>(1, result.AffectedItemsCount);
      // error 5014 - Wrong number of fields in row being inserted
      Assert.Equal(5014u, Assert.Throws<MySqlException>(() => result = ExecuteInsertStatement(stmt.Values("George", 34, 1))).Code);
      Assert.Equal(5014u, Assert.Throws<MySqlException>(() => ExecuteInsertStatement(stmt.Values("George", 34))).Code);
      Assert.Single(ExecuteSelectStatement(table.Select()).FetchAll());
    }
  }
}
