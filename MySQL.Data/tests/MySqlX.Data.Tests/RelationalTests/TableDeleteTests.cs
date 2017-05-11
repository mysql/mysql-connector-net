// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

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
      insertStatement.Execute();
      Assert.Equal(rowsToInsert, CountRows());
    }

    private long CountRows()
    {
      return testSchema.GetTable("test").Count();
    }

    private void ExecuteDelete(TableDeleteStatement statement, int expectedRowsCount)
    {
      Result result = statement.Execute();
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
  }
}
