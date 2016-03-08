// Copyright © 2015, 2016 Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.XDevAPI;
using Xunit;
using System.Linq;
using MySqlX.XDevAPI.Relational;
using MySqlX.XDevAPI.Common;
using System;

namespace MySqlX.Data.Tests.RelationalTests
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
  }

}
