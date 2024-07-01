// Copyright © 2015, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySqlX.Data.Tests
{
  public class SchemaTests : BaseTest
  {
    [TearDown]
    public void TearDown() => ExecuteSQL("DROP TABLE IF EXISTS test");
    [Test]
    public void GetSchemas()
    {
      Session session = GetSession();
      List<Schema> schemas = session.GetSchemas();

      Assert.That(schemas.Exists(s => s.Name == base.testSchema.Name));

      Schema schema = session.GetSchema(schemaName);
      Assert.That(schema.Name, Is.EqualTo(schemaName));
      schema = session.GetSchema(null);
      Assert.That(schema.Name, Is.Null);
    }

    [Test]
    public void GetInvalidSchema()
    {
      Session s = GetSession();
      Schema schema = s.GetSchema("test-schema");
      Assert.That(SchemaExistsInDatabase(schema), Is.False);
    }

    [Test]
    public void GetAllTables()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test");
      ExecuteSQL("CREATE TABLE test(id int)");

      List<Table> tables = testSchema.GetTables();
      Assert.That(tables.Count == 1);
    }

    [Test]
    public void GetAllViews()
    {
      CreateCollection("coll");

      ExecuteSQL("DROP TABLE IF EXISTS test");
      ExecuteSQL("CREATE TABLE test(id int)");
      ExecuteSQL("CREATE VIEW view1 AS select * from test");
      ExecuteSQL("CREATE VIEW view2 AS select * from test");

      List<Table> tables = testSchema.GetTables();
      Assert.That(tables.Count, Is.EqualTo(3));
      Assert.That(tables.Count(i => !i.IsView), Is.EqualTo(1));
      Assert.That(tables.Count(i => i.IsView), Is.EqualTo(2));

      List<Collection> colls = testSchema.GetCollections();
      Assert.That(colls, Has.One.Items);
    }

    [Test]
    [Ignore("Fix for 8.0.13")]
    public void GetCollectionAsTable()
    {
      Collection testCollection = CreateCollection("test");

      Result r = ExecuteAddStatement(testCollection.Add(@"{ ""_id"": 1, ""foo"": 1 }"));
      Assert.That(r.AffectedItemsCount, Is.EqualTo(1));

      Table test = testSchema.GetCollectionAsTable("test");
      Assert.That(TableExistsInDatabase(test));

      RowResult result = ExecuteSelectStatement(test.Select("_id"));
      Assert.That(result.Next());
      Assert.That(result[0], Is.EqualTo("1"));
    }

    [Test]
    public void DropSchema()
    {
      string schemaName = "testDrop";
      Session session = GetSession();
      session.CreateSchema(schemaName);
      Schema schema = session.GetSchema(schemaName);
      Assert.That(SchemaExistsInDatabase(schema));

      // Drop existing schema.
      session.DropSchema(schemaName);
      Assert.That(SchemaExistsInDatabase(schema), Is.False);

      // Drop non-existing schema.
      session.DropSchema(schemaName);
      Assert.That(SchemaExistsInDatabase(schema), Is.False);

      // Empty, whitespace and null schema name.
      Assert.Throws<ArgumentNullException>(() => session.DropSchema(string.Empty));
      Assert.Throws<ArgumentNullException>(() => session.DropSchema(" "));
      Assert.Throws<ArgumentNullException>(() => session.DropSchema("  "));
      Assert.Throws<ArgumentNullException>(() => session.DropSchema(null));
    }

    #region WL14389

    [Test, Description("Test MySQLX plugin Exception Handling Scenario 1")]
    public void ExceptionHandlingSchema()
    {
      Session sessionPlain = MySQLX.GetSession(ConnectionString);
      sessionPlain.DropSchema("test1");
      var db = sessionPlain.CreateSchema("test1");
      db = sessionPlain.GetSchema("test1");
      if (db.ExistsInDatabase())
      {
        sessionPlain.DropSchema("test1");
        db = sessionPlain.CreateSchema("test1");
        db.DropCollection("test");
        var coll = db.CreateCollection("test");
        var res = coll.Add("{\"_id\":null,\"FLD1\":\"nulldata\"}").Execute();
        var docIds = res.GeneratedIds.Count;

        var docs = coll.Find("$.FLD1 == 'nulldata'").Execute();
        while (docs.Next())
        {
          Assert.Throws<NullReferenceException>(() => docs.Current["_id"].ToString());
        }
      }
      else { db = sessionPlain.CreateSchema("test1"); }
      Assert.Throws<MySqlException>(() => sessionPlain.CreateSchema("test1"));
      sessionPlain.SQL(@"drop database if exists test1;").Execute();

      sessionPlain.SQL(@"drop database if exists `test\84`;").Execute();
      db = sessionPlain.CreateSchema("test\\84");

      sessionPlain.SQL(@"drop database if exists `test\84`;").Execute();
      db = sessionPlain.CreateSchema(@"test\84");

      sessionPlain.SQL(@"drop database if exists `test\84`;").Execute();
    }

    [Test, Description("Test MySQLX plugin Exception Handling Scenario 2")]
    public void ExceptionHandlingCollection()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher.");

      Session sessionPlain = MySQLX.GetSession(ConnectionString);

      if (sessionPlain.GetSchema("test_collection_123456789").ExistsInDatabase())
      {
        sessionPlain.DropSchema("test_collection_123456789");
      }
      var db = sessionPlain.CreateSchema("test_collection_123456789");
      if (db.GetCollection("my_collection_123456789").ExistsInDatabase())
      {
        db.DropCollection("my_collection_123456789");
      }
      db.CreateCollection("my_collection_123456789");
      Assert.Throws<MySqlException>(() => db.CreateCollection("my_collection_123456789"));

      sessionPlain.DropSchema("test_collection_123456789");
    }

    [Test, Description("GetSession Create Schema Valid Name(Session and Session)")]
    public void CreateValidSchema()
    {
      Schema db = session.GetSchema("test_123456789");
      if (db.ExistsInDatabase())
      {
        session.DropSchema("test_123456789");
        db = session.CreateSchema("test_123456789");
      }
      else { db = session.CreateSchema("test_123456789"); }
      Assert.That(db.ExistsInDatabase());
      Assert.Throws<MySqlException>(() => session.CreateSchema("test_123456789"));
      session.DropSchema("test_123456789");
      Assert.Throws<MySqlException>(() => session.CreateSchema("test_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789"));
      Assert.Throws<MySqlException>(() => session.CreateSchema(null));
    }

    [Test, Description("Set Node Schema")]
    public void SessionSetSchema()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher.");
      if (!session.GetSchema("test1").ExistsInDatabase())
        session.CreateSchema("test1");
      Assert.DoesNotThrow(() => session.SetCurrentSchema("test1"));
      Assert.That(session.Schema.Name, Is.EqualTo("test1"));
      // Retry the same schema
      Assert.DoesNotThrow(() => session.SetCurrentSchema("test1"));
      Assert.That(session.Schema.Name, Is.EqualTo("test1"));
      session.Schema.CreateCollection("my_collection_123456789");
      session.Schema.DropCollection("my_collection_123456789");
      //Exceptions
      Assert.Throws<MySqlException>(() => session.SQL("USE nonExistingSchema").Execute());
      Assert.Throws<MySqlException>(() => session.SetCurrentSchema("nonExistingSchema"));
      Assert.Throws<MySqlException>(() => session.SetCurrentSchema(null));
      session.DropSchema("test1");
      //No Active Schema
      using (Session sessionPlain = MySQLX.GetSession(ConnectionString))
      {
        Assert.That(sessionPlain.GetCurrentSchema(), Is.Null);
      }
    }

    [Test, Description("Session Status before Execution - Negative")]
    public void SessionClosedBeforeExecution()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher.");
      Schema schema = null;
      Session sessionPlain = MySQLX.GetSession(ConnectionString);
      schema = sessionPlain.GetSchema(schemaName);
      schema.CreateCollection("test123");
      schema.DropCollection("test123");
      sessionPlain.Close();
      Assert.Throws<MySqlException>(() => schema.CreateCollection("test123"));
      sessionPlain.Dispose();
    }

    [Test, Description("Session Switch-SetCurrentSchema(Same and Different Session)")]
    public void SessionChangeCurrentSchema()
    {
      using (Session session = MySQLX.GetSession(ConnectionString))
      {
        session.SQL("DROP DATABASE IF EXISTS dbname1").Execute();
        session.SQL("CREATE DATABASE dbname1").Execute();
        session.SQL("USE dbname1").Execute();
        Assert.That(session.GetCurrentSchema().Name, Is.EqualTo("dbname1").IgnoreCase);
        session.SQL("CREATE TABLE address1" +
                    "(address_number  INT NOT NULL AUTO_INCREMENT, " +
                    "building_name  VARCHAR(100) NOT NULL, " +
                    "district VARCHAR(100) NOT NULL, PRIMARY KEY (address_number)" + ");").Execute();
        session.SQL("INSERT INTO address1" +
                    "(address_number,building_name,district)" +
                    " VALUES " +
                    "(1,'MySQL1','BGL1');").Execute();

        session.SQL("DROP DATABASE IF EXISTS dbname2").Execute();
        session.SQL("CREATE DATABASE dbname2").Execute();
        session.SQL("USE dbname2").Execute();
        Assert.That(session.GetCurrentSchema().Name, Is.EqualTo("dbname2").IgnoreCase);
        session.SQL("CREATE TABLE address2" +
                    "(address_number  INT NOT NULL AUTO_INCREMENT, " +
                    "building_name  VARCHAR(100) NOT NULL, " +
                    "district VARCHAR(100) NOT NULL, PRIMARY KEY (address_number)" + ");").Execute();
        session.SQL("INSERT INTO address2" +
                    "(address_number,building_name,district)" +
                    " VALUES " +
                    "(2,'MySQL2','BGL2');").Execute();
        session.SetCurrentSchema("dbname1");
        Assert.That(session.Schema.Name, Is.EqualTo("dbname1"));
        session.SQL("SELECT * FROM address1").Execute();
        session.SQL("DROP TABLE address1").Execute();
        session.SetCurrentSchema("dbname2");
        Assert.That(session.Schema.Name, Is.EqualTo("dbName2").IgnoreCase);
        session.SQL("SELECT * FROM address2").Execute();
        session.SQL("DROP TABLE address2").Execute();
        session.SQL("DROP DATABASE dbname1").Execute();
        session.SQL("DROP DATABASE dbname2").Execute();
        session.Close();
        session.Dispose();
      }
    }
    #endregion WL14389
  }
}
