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
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using System.Collections.Generic;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class SchemaTests : BaseTest
  {
    [Fact]
    public void GetSchemas()
    {
      XSession session = GetSession();
      List<Schema> schemas = session.GetSchemas();

      Assert.True(schemas.Exists(s => s.Name == base.testSchema.Name));
    }

    [Fact]
    public void GetInvalidSchema()
    {
      XSession s = GetSession();
      Schema schema = s.GetSchema("test-schema");
      Assert.False(schema.ExistsInDatabase());
    }

    [Fact]
    public void GetAllTables()
    {
      Collection coll = CreateCollection("coll");
      ExecuteSQL("CREATE TABLE test.test(id int)");

      List<Table> tables = testSchema.GetTables();
      Assert.True(tables.Count == 1);
    }

    [Fact]
    public void GetAllViews()
    {
      Collection coll = CreateCollection("coll");

      ExecuteSQL("CREATE TABLE test.test(id int)");
      ExecuteSQL("CREATE VIEW test.view1 AS select * from test.test");

      List<View> views = testSchema.GetViews();
      Assert.True(views.Count == 1);
    }

    [Fact]
    public void GetCollectionAsTable()
    {
      Collection testCollection = CreateCollection("test");

      Result r = testCollection.Add(@"{ ""_id"": 1, ""foo"": 1 }").Execute();
      Assert.Equal<ulong>(1, r.RecordsAffected);

      Table test = testSchema.GetCollectionAsTable("test");
      Assert.True(test.ExistsInDatabase());

      RowResult result = test.Select("_id").Execute();
      Assert.True(result.Next());
      Assert.Equal("1", result[0]);
    }
  }
}
