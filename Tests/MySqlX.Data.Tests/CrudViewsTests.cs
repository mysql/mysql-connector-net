// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class CrudViewsTests : BaseTest
  {
    private string tableName = "test";
    private string collectionName = "mycoll";
    object[][] allRows = {
        new object[] { 1, "jonh doe", 38 },
        new object[] { 2, "milton green", 45 }
      };
    public CrudViewsTests()
    {
    }

    public override void Dispose()
    {
      GetSession().DropSchema(schemaName);
      base.Dispose();
    }

    private void CreateTableData()
    {
      ExecuteSQL($"CREATE TABLE `{tableName}` (id INT, name VARCHAR(45), age INT)");
      Table table = GetSession().GetSchema(schemaName).GetTable(tableName);
      var insertStmt = table.Insert().Values(allRows[0]);
      for (int i = 1; i < allRows.Length; i++)
        insertStmt.Values(allRows[0]);
      insertStmt.Execute();
    }

    private void CreateCollectionData()
    {
      var coll = GetSession().GetSchema(schemaName).CreateCollection(collectionName);
      var addStmt = coll.Add(ConvertObjToAnonymous(allRows[0]));
      for (int i = 1; i < allRows.Length; i++)
        addStmt.Add(ConvertObjToAnonymous(allRows[i]));
      addStmt.Execute();
    }

    private object ConvertObjToAnonymous(object[] values)
    {
      return new { id = values[0], name = values[1], age = values[2] };
    }

    [Fact]
    public void CreateViewFromTable()
    {
      CreateTableData();
      Schema db = GetSession().Schema;
      Table table = db.GetTable(tableName);
      var result = db.CreateView("myview").DefinedAs(table.Select()).Execute();

      Table view = db.GetTable("myview");
      Assert.True(view.IsView);
      Assert.Equal(allRows.Length, view.Count());
    }

    [Fact]
    public void CreateViewFromCollection()
    {
      CreateCollectionData();
      Schema db = GetSession().Schema;
      Collection coll = db.GetCollection(collectionName);
      var result = db.CreateView("myview").DefinedAs(coll.Find()).Execute();

      Table view = db.GetTable("myview");
      Assert.True(view.IsView);
      Assert.Equal(allRows.Length, view.Count());
    }

    [Fact]
    public void CreateViewDeferedExecute()
    {
      CreateTableData();
      Schema db = GetSession().Schema;
      Table table = db.GetTable(tableName);
      var query = table.Select();
      var createView = db.CreateView("myview").DefinedAs(query);
      // change the defined query but the view should be unaffected
      query.Where("id = 5");
      query.Limit(1);
      var result = createView.Execute();

      Table view = db.GetTable("myview");
      Assert.True(view.IsView);
      Assert.Equal(allRows.Length, view.Count());
    }
  }
}
