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

namespace MySqlX_DevAPI.Sections
{
  public class CrudOperations
  {
    [Fact]
    public void TableSelect()
    {
      Session s = MySqlX.GetSession("server=127.0.0.1;port=33060;uid=userx;password=userx1;");
      Schema db = s.GetSchema("testx");
      var employees = db.GetTable("employees");

      var res = employees.Select("name")//, "age")
        //.Where("name like :name")
        //.OrderBy("name")
        //.Bind(parameters)
        .Execute();

      Assert.Equal(2, res.Rows.Count);
    }

    [Fact]
    public void ExecuteSql()
    {
      var result = MySqlX.GetNodeSession("").ExecuteSql("SELECT name, age " +
        "FROM employee " +
        "WHERE name like ? " +
        "ORDER BY name", "m%");
    }

    [Fact]
    public void ParameterBinding()
    {
      Session s = MySqlX.GetSession("server=localhost;port=33060;uid=userx;password=userx1;");
      Schema schema = s.GetDefaultSchema();
      Collection myColl = new Collection(schema, "default");

      // Collection.add() function with hardcoded values
      myColl.Add(new { name = "Sakila", age = 15 }).Run();

      // Using the .bind() function to bind parameters
      myColl.Add(new { name = ":1", age = ":2" }).Bind("jack", 58).Run();

      // Using named parameters
      myColl.Add(new { name = ":name", age = ":age" })
        .Bind(new { name = "clare", age = 37 }).Run();

      // Binding works for all CRUD statements
      var myRes = myColl.Find("name LIKE ?")
        .Bind("J%").Execute();
    }

    [Fact]
    public void PreparingCrudStatements()
    {
      Session s = MySqlX.GetSession("server=localhost;port=33060;uid=userx;password=userx1;");
      Schema schema = s.GetDefaultSchema();
      Collection myColl = new Collection(schema, "default");

      // Only prepare a Colleciton.add() operation, but don't run it yet
      var myAdd = myColl.Add(new { name = ":1", age = ":2" });

      // Binding parameters to the prepared function and .run()
      myAdd.Bind("mike", 39).Run();
      myAdd.Bind("johannes", 28).Run();

      // Binding works for all CRUD statements
      var myFind = myColl.Find("name LIKE :name AND age > :age");

      var myDoc = myFind.Bind(new { name = "S%", age = 18 }).Execute().One();
      var MyOtherDoc = myFind.Bind(new { name = "M%", age = 24 }).Execute().One();

    }

    [Fact]
    public void UsingIteratorsToBindParameterValues()
    {
      Session s = MySqlX.GetSession("server=localhost;port=33060;uid=userx;password=userx1;");
      Schema schema = s.GetDefaultSchema();
      Collection myColl = new Collection(schema, "default");

      // Only prepare a Collection.Add() operation, but don"t run it yet
      var myPrep = myColl.Add(new { name = ":1", age = ":2" });

      // Binding an Iterator Object to the prepared function
      var myIterator = MySqlX.CsvFileRowIterator().Open("foo.csv");
      myPrep.Bind(myIterator).Run();

      // Instead of using individual parameters, and iterator can also return full docs
      var myIterator2 = MySqlX.JsonFileDocIterator().Open("bar.json");
      myColl.Add().Bind(myIterator2).Run();

    }


  }
}
