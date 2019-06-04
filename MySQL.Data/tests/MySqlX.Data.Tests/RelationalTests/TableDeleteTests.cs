// Copyright (c) 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using Xunit;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class TableDeleteTests : BaseTest
  {
    public TableDeleteTests()
    {
      ExecuteSQL("CREATE TABLE test.test(id INT, age INT)");

      var insertStatement = testSchema.GetTable("test").Insert();
      int rowsToInsert = 10;
      for (int i = 1; i <= rowsToInsert; i++)
      {
        insertStatement.Values(i, i);
      }
      ExecuteInsertStatement(insertStatement);
      Assert.Equal(rowsToInsert, CountRows());
    }

    private long CountRows()
    {
      return testSchema.GetTable("test").Count();
    }

    private void ExecuteDelete(TableDeleteStatement statement, int expectedRowsCount)
    {
      Result result = ExecuteDeleteStatement(statement);
      Assert.Equal(expectedRowsCount, CountRows());
    }

    [Fact]
    public void DeleteAllTest()
    {
      ExecuteDelete(testSchema.GetTable("test").Delete(), 0);
    }

    [Fact]
    public void DeleteConditionTest()
    {
      ExecuteDelete(testSchema.GetTable("test").Delete().Where("age % 2 = 0"), 5);
    }

    [Fact]
    public void DeleteOrderbyAndLimit()
    {
      ExecuteDelete(testSchema.GetTable("test").Delete().OrderBy("age Desc").Limit(3), 7);
    }

    [Fact]
    public void DeleteConditionOrderbyAndLimit()
    {
      ExecuteDelete(testSchema.GetTable("test").Delete().Where("id > 5").OrderBy("id Desc").Limit(2), 8);
    }

    [Fact]
    public void DeleteBindTest()
    {
      var deleteStmt = testSchema.GetTable("test").Delete().Where("age = :aGe");
      ExecuteDelete(deleteStmt.Bind("Age", 4), 9);
      ExecuteDelete(deleteStmt.Bind("age", 6), 8);
    }

    [Fact]
    public void DeleteWithInOperator()
    {
      Table table = testSchema.GetTable("test");
      Assert.Equal(10, CountRows());

      Assert.Equal<ulong>(2, ExecuteDeleteStatement(table.Delete().Where("id IN (1,2)")).AffectedItemsCount);
      Assert.Equal(8, CountRows());

      Assert.Throws<MySqlException>(() => ExecuteDeleteStatement(table.Delete().Where("a IN [3]")).AffectedItemsCount);
      Assert.Throws<MySqlException>(() => ExecuteDeleteStatement(table.Delete().Where("3 IN a")).AffectedItemsCount);
      Assert.Throws<MySqlException>(() => ExecuteDeleteStatement(table.Delete().Where("age IN [3]")).AffectedItemsCount);

      Assert.Equal<ulong>(1, ExecuteDeleteStatement(table.Delete().Where("age IN (3)")).AffectedItemsCount);
      Assert.Equal(7, CountRows());
    }
  }
}
