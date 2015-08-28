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
using System.Collections.Generic;
using Xunit;
using System.Linq;
using MySql.XDevAPI.Statements;

namespace PortableConnectorNetTests
{
  public class CrudOperations : BaseTest
  {
    [Collection("Database")]
    public class TableTests : IClassFixture<TableFixture>
    {
      TableFixture fixture;

      object[][] allRows = {
        new object[] { 2, "jonh doe", (byte)38 },
        new object[] { 4, "milton green", (byte)45 }
      };

      public TableTests(TableFixture fixture)
      {
        this.fixture = fixture;
      }

      private void MultiTableSelectTest(SelectStatement statement, object[][] expectedValues)
      {
        var result = statement.Execute();
        int rowCount = 0;
        while (result.Next())
        {
          rowCount++;
        };

        Assert.Equal(expectedValues.Length, rowCount);
        Assert.Equal(expectedValues.Length, result.Rows.Count);
        for(int i = 0; i < expectedValues.Length; i++)
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
        Session s = fixture.DatabaseFixture.GetSession();
        Schema db = s.GetSchema(fixture.Schema);
        var employees = db.GetTable(fixture.Table);

        MultiTableSelectTest(employees.Select(), allRows);
        MultiTableSelectTest(employees.Select("name", "age"),
          allRows.Select(c => new[] { c[1], c[2] }).ToArray());
        MultiTableSelectTest(employees.Select("name", "age").Where("age == 38"),
          allRows.Select(c => new[] { c[1], c[2] }).Where(c => (byte)c[1] == (byte)38).ToArray());
        MultiTableSelectTest(employees.Select().Where("age == 45"),
          allRows.Where(c => (byte)c[2] == (byte)45).ToArray());
        MultiTableSelectTest(employees.Select().OrderBy("age"),
          allRows.OrderBy(c => c[2]).ToArray());
        MultiTableSelectTest(employees.Select().OrderBy("age desc"),
          allRows.OrderByDescending(c => c[2]).ToArray());
        MultiTableSelectTest(employees.Select().OrderBy("age desc, name"),
          allRows.OrderByDescending(c => c[2]).ThenBy(c => c[1]).ToArray());
        MultiTableSelectTest(employees.Select().Limit(1),
          allRows.Take(1).ToArray());
        MultiTableSelectTest(employees.Select().Limit(10, 1),
          allRows.Skip(1).Take(10).ToArray());
        MultiTableSelectTest(employees.Select().Limit(1 , 1),
          allRows.Skip(1).Take(1).ToArray());
        MultiTableSelectTest(employees.Select().Where("name like :name").Bind("%jon%"),
          allRows.Where(c => c[1].ToString().Contains("jon")).ToArray());
        MultiTableSelectTest(employees.Select().Where("name like :name").Bind("%on%"),
          allRows.Where(c => c[1].ToString().Contains("on")).ToArray());
        //MultiTableSelectTest(employees.Select().GroupBy("age"),
          //allRows.GroupBy(c => new[] { c[2] }).First().ToArray());
      }

      [Fact]
      public void TableInsert()
      {

      }
    }

    [Fact]
    public void ExecuteSql()
    {
      var result = MySqlX.GetNodeSession("").ExecuteSql("SELECT name, age " +
        "FROM employee " +
        "WHERE name like ? " +
        "ORDER BY name", false, "m%");
    }

    [Fact]
    public void ParameterBinding()
    {
      Session s = MySqlX.GetSession("server=localhost;port=33060;uid=userx;password=userx1;");
      Schema schema = s.GetDefaultSchema();
      Collection myColl = new Collection<JsonDoc>(schema, "default");

      // Collection.add() function with hardcoded values
//      myColl.Add(new { name = "Sakila", age = 15 }).Run();

      // Using the .bind() function to bind parameters
  //    myColl.Add(new { name = ":1", age = ":2" }).Bind("jack", 58).Run();

      // Using named parameters
    //  myColl.Add(new { name = ":name", age = ":age" })
      //  .Bind(new { name = "clare", age = 37 }).Run();

      // Binding works for all CRUD statements
      var myRes = myColl.Find("name LIKE ?")
        .Bind("J%").Execute();
    }

    [Fact]
    public void PreparingCrudStatements()
    {
      Session s = MySqlX.GetSession("server=localhost;port=33060;uid=userx;password=userx1;");
      Schema schema = s.GetDefaultSchema();
      Collection myColl = new Collection<JsonDoc>(schema, "default");

      // Only prepare a Colleciton.add() operation, but don't run it yet
      var myAdd = myColl.Add(new { name = ":1", age = ":2" });

      // Binding parameters to the prepared function and .run()
//      myAdd.Bind("mike", 39).Run();
  //    myAdd.Bind("johannes", 28).Run();

      // Binding works for all CRUD statements
      var myFind = myColl.Find("name LIKE :name AND age > :age");

//      var myDoc = myFind.Bind(new { name = "S%", age = 18 }).Execute().One();
  //    var MyOtherDoc = myFind.Bind(new { name = "M%", age = 24 }).Execute().One();

    }

    [Fact]
    public void UsingIteratorsToBindParameterValues()
    {
      Session s = MySqlX.GetSession("server=localhost;port=33060;uid=userx;password=userx1;");
      Schema schema = s.GetDefaultSchema();
      Collection myColl = new Collection<JsonDoc>(schema, "default");

      // Only prepare a Collection.Add() operation, but don"t run it yet
      var myPrep = myColl.Add(new { name = ":1", age = ":2" });

      // Binding an Iterator Object to the prepared function
      var myIterator = MySqlX.CsvFileRowIterator().Open("foo.csv");
      //myPrep.Bind(myIterator).Run();

      // Instead of using individual parameters, and iterator can also return full docs
      var myIterator2 = MySqlX.JsonFileDocIterator().Open("bar.json");
      //myColl.Add().Bind(myIterator2).Run();

    }


  }
}
