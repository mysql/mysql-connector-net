// Copyright (c) 2016, 2021, Oracle and/or its affiliates.
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
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using MySqlX.XDevAPI;
using System;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class ViewTests : BaseTest
  {
    [TearDown]
    public void TearDown()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test");
      ExecuteSQL("DROP view IF EXISTS view1");
      ExecuteSQL("DROP view IF EXISTS view2");
    }

    [Test]
    public void TryUpdatingView()
    {
      ExecuteSQL("CREATE TABLE test(id int)");
      ExecuteSQL("CREATE VIEW view2 AS select *, 1 from test");

      List<Table> tables = testSchema.GetTables();
      Assert.AreEqual(2, tables.Count);

      Table view = tables.First(i => i.IsView);
      Assert.AreEqual("view2", view.Name);
      MySqlException ex = Assert.Throws<MySqlException>(() => ExecuteInsertStatement(view.Insert().Values(1)));
      Assert.AreEqual("Column '1' is not updatable", ex.Message);
    }

    [Test]
    public void GetView()
    {
      ExecuteSQL("CREATE TABLE test(id int)");
      ExecuteSQL("CREATE VIEW view1 AS select *, 1 from test");

      Table table = testSchema.GetTable("test");
      Table view = testSchema.GetTable("view1");
      Assert.True(view.IsView);
      Assert.False(table.IsView);

      ExecuteSQL("DROP VIEW view1");
    }

    [Test]
    public void NonExistingView()
    {
      bool isView;
      void IsView() { isView = testSchema.GetTable("no_exists").IsView; };
      Assert.Throws<MySqlException>(() => IsView());
    }

    #region WL14389
    [Test, Description("Test MySQLX plugin MySQL Net 839 - Views-Get Table with param")]
    public void CreateJoinAndGetTableValidation()
    {
      CreateCollection("coll");
      ExecuteSQL("CREATE TABLE test1(id1 int,firstname varchar(20))");
      ExecuteSQL("INSERT INTO test1 values ('1','Rob')");
      ExecuteSQL("INSERT INTO test1 values ('2','Steve')");
      ExecuteSQL("CREATE TABLE test2(id2 int,lastname varchar(20))");
      ExecuteSQL("INSERT INTO test2 values ('1','Williams')");
      ExecuteSQL("INSERT INTO test2 values ('2','Waugh')");
      ExecuteSQL("CREATE VIEW view1 AS select * from test.test1");
      ExecuteSQL("SELECT * FROM view1");
      ExecuteSQL("CREATE VIEW view2 AS select * from test.test2");

      var tables = testSchema.GetTables();
      Assert.AreEqual(true, tables.Count == 4, "Match being done");
      Assert.AreEqual(2, tables.Count(i => !i.IsView), "Match being done when isview not true");
      Assert.AreEqual(2, tables.Count(i => i.IsView), "Match being done when isview true");
      List<Collection> colls = testSchema.GetCollections();
      Assert.AreEqual(1, colls.Count, "Match being done");
      var view1 = testSchema.GetTable("view1");
      Assert.True(view1.IsView);

      //Valid Scenario-1
      view1.Select().Execute();
      var result = testSchema.GetTable("test1").Update().Set("firstname", "Peter").Where("id1=1").Execute();
      result = view1.Update().Set("firstname", "Rob").Where("id1=1").Execute();
      result = view1.Insert("id1", "firstname").Values("3", "Mark").Execute();
      result = view1.Delete().Where("id1=3").Execute();
      ExecuteSQL("ALTER VIEW view1 AS select * from test.test2");
      result = testSchema.GetTable("test2").Update().Set("lastname", "Morgan").Where("id2=1").Execute();
      result = view1.Update().Set("lastname", "Williams").Where("id2=1").Execute();
      result = view1.Insert("id2", "lastname").Values("3", "Twain").Execute();
      result = view1.Delete().Where("id2=3").Execute();

      //Join Two Views Scenario-2
      ExecuteSQL(
          "CREATE VIEW myView AS SELECT a.ID1 as a_tID,b.ID2 as b_tID, a.firstname as a_firstname, " +
          "b.lastname as b_lastname FROM test1 a JOIN test2 b ON a.ID1=b.ID2;");
      var joinedview = testSchema.GetTable("myView");
      Assert.True(joinedview.IsView);

      joinedview.Select().Execute();
      result = joinedview.Update().Set("a_firstname", "Peter").Where("a_tID=1").Execute();
      result = joinedview.Update().Set("a_firstname", "Rob").Where("a_tID=1").Execute();
      result = joinedview.Insert("a_tID", "a_firstname").Values("3", "Mark").Execute();
      result = joinedview.Insert("b_tID", "b_lastname").Values("3", "Twain").Execute();

      Assert.Throws<MySqlException>(() => joinedview.Delete().Where("a_tID=3").Where("b_tID=3").Execute());

      //Valid View - Invalid Select/Insert/Update Statements-Scenario-3
      var view2 = testSchema.GetTable("view2");
      Assert.True(view2.IsView);

      Assert.Throws<MySqlException>(() => view2.Select("id2", "age").Execute());

      view2.Select().Execute();
      Assert.Throws<MySqlException>(() => view2.Update().Set("firstname", "Rob").Where("id2=100").Execute());
      view2.Select().Execute();
      Assert.Throws<MySqlException>(() => view2.Insert("id2", "firstname").Values("3", "Mark").Execute());

      view2.Select().Execute();
      var res = view2.Delete().Where("id2=100").Execute();
      Assert.AreEqual(0, res.AffectedItemsCount, "View2-Not Possible to remove an invalid record");

      //View Doesn't Exist-Scenario-4
      var view3 = testSchema.GetTable("view3");
      Assert.Throws<MySqlException>(() => view3.Select().Execute());

      //View Passed as Null-Scenario-5
      view3 = testSchema.GetTable(null);
      Assert.Throws<ArgumentNullException>(() => view3.Select().Execute());

      //View Passed as Null-Scenario-6
      var view4 = testSchema.GetTable("test1");
      Assert.IsFalse(view4.IsView);

      //Change the query processing env-Scenario-7
      ExecuteSQL("SET sql_mode = '';");
      var result_view = view1.Select().Execute();
      session.SQL("Drop table if exists test1").Execute();
      session.SQL("Drop table if exists test2").Execute();
      session.SQL("Drop view if exists view1").Execute();
      session.SQL("Drop view if exists view2").Execute();
      session.SQL("Drop view if exists myView").Execute();
    }

    [Test, Description("Test MySQLX plugin MySQL Net 839 - Views-Get Tables")]
    public void ViewsGetTables()
    {
      CreateCollection("coll");
      ExecuteSQL("CREATE TABLE test1(id1 int,firstname varchar(20))");
      ExecuteSQL("INSERT INTO test1 values ('1','Rob')");
      ExecuteSQL("INSERT INTO test1 values ('2','Steve')");
      ExecuteSQL("CREATE TABLE test2(id2 int,lastname varchar(20))");
      ExecuteSQL("INSERT INTO test2 values ('1','Williams')");
      ExecuteSQL("INSERT INTO test2 values ('2','Waugh')");
      ExecuteSQL("CREATE VIEW view1 AS select * from test.test1");
      ExecuteSQL("SELECT * FROM view1");
      ExecuteSQL("CREATE VIEW view2 AS select * from test.test2");
      ExecuteSQL("SELECT * FROM view2");
      var tables = testSchema.GetTables();
      Assert.True(tables.Count >= 4, "Match being done");
      Assert.AreEqual(2, tables.Count(i => !i.IsView), "Match being done when isview not true");
      Assert.AreEqual(2, tables.Count(i => i.IsView), "Match being done when isview true");
      List<Collection> colls = testSchema.GetCollections();
      Assert.AreEqual(1, colls.Count, "Match being done");

      var view1 = tables[2];
      Assert.True(view1.IsView);
      //Valid Scenario-1
      view1.Select().Execute();

      var result = testSchema.GetTable("test1").Update().Set("firstname", "Peter").Where("id1=1").Execute();
      result = tables[2].Update().Set("firstname", "Rob").Where("id1=1").Execute();
      result = tables[2].Insert("id1", "firstname").Values("3", "Mark").Execute();
      result = tables[2].Delete().Where("id1=3").Execute();
      ExecuteSQL("ALTER VIEW view1 AS select * from test.test2");
      result = testSchema.GetTable("test2").Update().Set("lastname", "Morgan").Where("id2=1").Execute();
      result = tables[2].Update().Set("lastname", "Williams").Where("id2=1").Execute();
      result = tables[2].Insert("id2", "lastname").Values("3", "Twain").Execute();
      result = tables[2].Delete().Where("id2=3").Execute();

      //Join Two Views Scenario-2
      ExecuteSQL(
          "CREATE VIEW myView AS SELECT a.ID1 as a_tID,b.ID2 as b_tID, a.firstname as a_firstname, " +
          "b.lastname as b_lastname FROM test1 a JOIN test2 b ON a.ID1=b.ID2;");
      var joinedview = testSchema.GetTables();
      Assert.True(joinedview[0].IsView);
      joinedview[0].Select().Execute();
      result = joinedview[0].Update().Set("a_firstname", "Peter").Where("a_tID=1").Execute();
      result = joinedview[0].Update().Set("a_firstname", "Rob").Where("a_tID=1").Execute();
      result = joinedview[0].Insert("a_tID", "a_firstname").Values("3", "Mark").Execute();
      result = joinedview[0].Insert("b_tID", "b_lastname").Values("3", "Twain").Execute();

      Assert.Throws<MySqlException>(() => joinedview[0].Delete().Where("a_tID=3").Where("b_tID=3").Execute());

      ExecuteSQL("DROP VIEW " + joinedview[0].Name);
      //Valid View - Invalid Select/Insert/Update Statements-Scenario-3
      var view2 = testSchema.GetTables();
      Assert.True(view2[3].IsView);

      Assert.Throws<MySqlException>(() => view2[3].Select("id2", "age").Execute());

      view2[3].Select().Execute();
      Assert.Throws<MySqlException>(() => view2[3].Update().Set("firstname", "Rob").Where("id2=100").Execute());

      view2[3].Select().Execute();
      Assert.Throws<MySqlException>(() => view2[3].Insert("id2", "firstname").Values("3", "Mark").Execute());

      view2[3].Select().Execute();
      var res = view2[3].Delete().Where("id2=100").Execute();
      //WL11843-Core API v1 alignment Changes
      Assert.AreEqual(0, res.AffectedItemsCount);

      //Change the query processing env-Scenario-5
      ExecuteSQL("SET sql_mode = '';");
      var result_view = view1.Select().Execute();

      ExecuteSQL("DROP TABLE test1");
      ExecuteSQL("DROP TABLE test2");
      ExecuteSQL("DROP VIEW view1");
      ExecuteSQL("DROP VIEW view2");
      //GetTables when there no tables/views -Scenario-4
      var view3 = testSchema.GetTables();
      Assert.AreEqual(0, view3.Count());
    }
    #endregion WL14389

  }
}
