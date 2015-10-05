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

using MySql.XDevAPI.Common;
using MySql.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PortableConnectorNetTests.RelationalTests
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
        insertStatement.Values(i, "", i);
      }
      Assert.True(insertStatement.Execute().Succeeded);
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
      }
      var result = statement.Execute();
      Assert.True(result.Succeeded);
      while (result.Next()) ;
      return result.Rows;
    }

    private void ValidateUpdate(TableUpdateStatement statement)
    {
      var result = statement.Execute();
      Assert.True(result.Succeeded);

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
      var result = table.Update().Execute();
      Assert.False(result.Succeeded);
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
      ValidateUpdate(table.Update().Set("age", 55).Where("id = :id or id = :id2").Bind("id", 4).Bind("id2", 7));
    }
  }
}
