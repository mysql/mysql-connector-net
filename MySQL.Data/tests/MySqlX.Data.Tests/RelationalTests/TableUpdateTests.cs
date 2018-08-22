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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class TableUpdateTests : BaseTest
  {
    Table table;

    public TableUpdateTests()
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
      Assert.Equal(rowsToInsert, CountRows());
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
          Assert.Equal(set.Value.ToString(), row.GetString(set.Path));
        }
      }
    }

    [Fact]
    public void EmptyUpdateTest()
    {
      Assert.Throws<MySqlException>(() => ExecuteUpdateStatement(table.Update()));
    }

    [Fact]
    public void UpdateConditionTest()
    {
      ValidateUpdate(table.Update().Where("id = 5").Set("name", "other"));
    }

    [Fact]
    public void UpdateMultiSet()
    {
      ValidateUpdate(table.Update().Set("name", "other")
        .Set("age", 21)
        .Set("id", 30)
        .Where("id = 3"));
    }

    [Fact]
    public void UpdateMultiRows()
    {
      ValidateUpdate(table.Update().Set("age", 85).Where("id % 2 = 0"));
    }

    [Fact]
    public void UpdateAllRows()
    {
      ValidateUpdate(table.Update().Set("age", 32).Set("name", "jonh"));
    }

    [Fact]
    public void UpdateOrderbyAndLimit()
    {
      ValidateUpdate(table.Update().Set("age", 15).OrderBy("id DES").Limit(5));
    }

    [Fact]
    public void UpdateBind()
    {
      var stmt = table.Update().Set("age", 55).Where("id = :id or id = :id or id = :id2");
      ValidateUpdate(stmt.Bind("id", 4).Bind("id2", 7));
      ValidateUpdate(stmt.Bind("id", 5).Bind("id2", 8));
    }

    [Fact]
    public void UpdateWithInOperator()
    {
      Table table = testSchema.GetTable("test");
      Assert.Equal(10, CountRows());

      Assert.Equal<ulong>(2, ExecuteUpdateStatement(table.Update().Where("id IN (1,2)").Set("id", 0)).AffectedItemsCount);
      Assert.Equal(2, ExecuteSelectStatement(table.Select().Where("id = 0")).FetchAll().Count);

      Assert.Throws<MySqlException>(() => ExecuteDeleteStatement(table.Delete().Where("a IN [3]")).AffectedItemsCount);
      Assert.Throws<MySqlException>(() => ExecuteDeleteStatement(table.Delete().Where("3 IN a")).AffectedItemsCount);
      Assert.Throws<MySqlException>(() => ExecuteUpdateStatement(table.Update().Where("age IN [3]").Set("id", 0)).AffectedItemsCount);

      Assert.Equal<ulong>(1, ExecuteUpdateStatement(table.Update().Where("age IN (3)").Set("id", 0)).AffectedItemsCount);
      Assert.Equal(3, ExecuteSelectStatement(table.Select().Where("id = 0")).FetchAll().Count);
    }
  }
}
