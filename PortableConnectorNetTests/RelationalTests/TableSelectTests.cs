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

using MySql.XDevAPI;
using Xunit;
using System.Linq;
using MySql.XDevAPI.Relational;
using MySql.XDevAPI.Common;

namespace PortableConnectorNetTests.RelationalTests
{
  public class TableSelectTests : BaseTest
  {
    object[][] allRows = {
        new object[] { 1, "jonh doe", 38 },
        new object[] { 2, "milton green", 45 }
      };

    public TableSelectTests()
    {
      ExecuteSQL("CREATE TABLE test.test (id INT, name VARCHAR(45), age INT)");
      TableInsertStatement stmt = testSchema.GetTable("test").Insert();
      stmt.Values(allRows[0]);
      stmt.Values(allRows[1]);
      UpdateResult result = stmt.Execute();
      Assert.True(result.Succeeded);
    }

    private void MultiTableSelectTest(TableSelectStatement statement, object[][] expectedValues)
    {
      var result = statement.Execute();
      int rowCount = 0;
      while (result.Next())
      {
        rowCount++;
      };

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
      MultiTableSelectTest(table.Select().Where("name like :name").Bind("%jon%"),
        allRows.Where(c => c[1].ToString().Contains("jon")).ToArray());
      MultiTableSelectTest(table.Select().Where("name like :name").Bind("%on%"),
        allRows.Where(c => c[1].ToString().Contains("on")).ToArray());
      //MultiTableSelectTest(employees.Select().GroupBy("age"),
      //allRows.GroupBy(c => new[] { c[2] }).First().ToArray());
    }

  }

}
