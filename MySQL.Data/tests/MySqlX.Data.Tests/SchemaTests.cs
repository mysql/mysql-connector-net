// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using System.Collections.Generic;
using Xunit;
using System.Linq;
using System;

namespace MySqlX.Data.Tests
{
  public class SchemaTests : BaseTest
  {
    [Fact]
    public void GetSchemas()
    {
      Session session = GetSession();
      List<Schema> schemas = session.GetSchemas();

      Assert.True(schemas.Exists(s => s.Name == base.testSchema.Name));
    }

    [Fact]
    public void GetInvalidSchema()
    {
      Session s = GetSession();
      Schema schema = s.GetSchema("test-schema");
      Assert.False(schema.ExistsInDatabase());
    }

    [Fact]
    public void GetAllTables()
    {
      Collection coll = CreateCollection("coll");
      ExecuteSQL("CREATE TABLE test(id int)");

      List<Table> tables = testSchema.GetTables();
      Assert.True(tables.Count == 1);
    }

    [Fact]
    public void GetAllViews()
    {
      Collection coll = CreateCollection("coll");

      ExecuteSQL("CREATE TABLE test(id int)");
      ExecuteSQL("CREATE VIEW view1 AS select * from test");
      ExecuteSQL("CREATE VIEW view2 AS select * from test");

      List<Table> tables = testSchema.GetTables();
      Assert.Equal(3, tables.Count);
      Assert.Equal(1, tables.Count(i => !i.IsView));
      Assert.Equal(2, tables.Count(i => i.IsView));

      List<Collection> colls = testSchema.GetCollections();
      Assert.Equal(1, colls.Count);
    }

    [Fact (Skip = "Fix for 8.0.5")]
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

    [Fact]
    public void DropSchema()
    {
      string schemaName = "testDrop";
      Session session = GetSession();
      session.CreateSchema(schemaName);
      Schema schema = session.GetSchema(schemaName);
      Assert.True(schema.ExistsInDatabase());

      // Drop existing schema.
      session.DropSchema(schemaName);
      Assert.False(schema.ExistsInDatabase());

      // Drop non-existing schema.
      session.DropSchema(schemaName);
      Assert.False(schema.ExistsInDatabase());

      // Empty, whitespace and null schema name.
      Assert.Throws<ArgumentNullException>(() => session.DropSchema(string.Empty));
      Assert.Throws<ArgumentNullException>(() => session.DropSchema(" "));
      Assert.Throws<ArgumentNullException>(() => session.DropSchema("  "));
      Assert.Throws<ArgumentNullException>(() => session.DropSchema(null));
    }
  }
}
