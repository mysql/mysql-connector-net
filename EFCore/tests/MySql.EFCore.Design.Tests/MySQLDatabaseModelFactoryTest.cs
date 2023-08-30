// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using MySql.EntityFrameworkCore.Diagnostics.Internal;
using MySql.EntityFrameworkCore.Scaffolding.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;


namespace MySql.EntityFrameworkCore.Design.Tests
{
  public class MySQLDatabaseModelFactoryTest : MySQLDatabaseModelFixture
  {
    private MySQLDatabaseModelFixture? _fixture;

    [SetUp]
    public void Init()
    {
      this._fixture = new MySQLDatabaseModelFixture();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
      MySQLTestStore.DeleteDatabase("blogman");
      MySQLTestStore.DeleteDatabase("sakiladb");
      MySQLTestStore.DeleteDatabase("sakilaindex");
      MySQLTestStore.DeleteDatabase("testview");
      MySQLTestStore.DeleteDatabase("world");
    }


    [Test]
    public void CanReadTables()
    {
      var sql = @"
        DROP DATABASE IF EXISTS blogman;
        CREATE DATABASE blogman; 
        USE blogman;
        CREATE TABLE blogs (id int);
        CREATE TABLE posts (id int);";
      var dbModel = _fixture!.CreateModel("blogman", sql, new List<string> { "blogs", "posts" }, new List<string>());

      Assert.That(dbModel.Tables.Select(c => c.Name), Has.Exactly(1).Matches<string>(table => table.Contains("blogs")));
      Assert.That(dbModel.Tables.Select(c => c.Name), Has.Exactly(1).Matches<string>(table => table.Contains("posts")));
    }

    [Test]
    public void CanReadColumns()
    {
      var sql = @"
        DROP DATABASE IF EXISTS blogman;
        CREATE DATABASE blogman; 
        USE blogman;
        CREATE TABLE blogs (
            id int,
            description varchar(100) NOT NULL,
            rate decimal(5,2) DEFAULT 0.0,    
            created timestamp DEFAULT now(),
            PRIMARY KEY (description, id)
        );";
      var dbModel = _fixture!.CreateModel("blogman", sql, new List<string> { "blogs" }, new List<string>());

      var columns = dbModel.Tables.Single().Columns;

      Assert.Multiple(() =>
      {
        Assert.AreEqual("blogman", dbModel.DatabaseName);
        Assert.That(dbModel.Tables.Select(c => c.Name), Has.Exactly(1).Matches<string>(table => table.Contains("blogs")));
      });

      string smallintWidth = TestUtils.Version >= new Version("8.0.0") ? string.Empty : "(11)";

      Assert.That(columns.Where(n => n.Name == "id").Select(a => a.Name), Has.Exactly(1).Matches<string>(name => name.Contains("id")));
      Assert.That(columns.Where(n => n.Name == "id").Select(a => a.GetDataType()), Has.One.Items.EqualTo("int" + smallintWidth));
      Assert.That(columns.Where(n => n.Name == "id").Select(a => a.GetPrimaryKeyOrdinal(2)), Has.One.Items.EqualTo(2));
      Assert.False(columns.Where(n => n.Name == "id").Select(a => a.IsNullable).FirstOrDefault());
      Assert.That(columns.Where(n => n.Name == "id").Select(a => a.GetOrdinal(0)), Has.One.Items.EqualTo(0));
      Assert.IsNull(columns.Where(n => n.Name == "id").Select(a => a.GetDefaultValue()).FirstOrDefault());

      Assert.That(columns.Where(n => n.Name == "description").Select(a => a.Name), Has.Exactly(1).Matches<string>(name => name.Contains("description")));
      Assert.That(columns.Where(n => n.Name == "description").Select(a => a.GetDataType()), Has.One.Items.EqualTo("varchar(100)"));
      Assert.That(columns.Where(n => n.Name == "description").Select(a => a.GetPrimaryKeyOrdinal(1)), Has.One.Items.EqualTo(1));
      Assert.False(columns.Where(n => n.Name == "description").Select(a => a.IsNullable).FirstOrDefault());
      Assert.That(columns.Where(n => n.Name == "description").Select(a => a.GetOrdinal(1)), Has.One.Items.EqualTo(1));
      Assert.IsNull(columns.Where(n => n.Name == "description").Select(a => a.GetDefaultValue()).FirstOrDefault());
      Assert.That(columns.Where(n => n.Name == "description").Select(a => a.GetMaxLength(100)), Has.One.Items.EqualTo(100));

      Assert.That(columns.Where(n => n.Name == "rate").Select(a => a.Name), Has.Exactly(1).Matches<string>(name => name.Contains("rate")));
      Assert.That(columns.Where(n => n.Name == "rate").Select(a => a.GetDataType()), Has.One.Items.EqualTo("decimal(5,2)"));
      Assert.IsNull(columns.Where(n => n.Name == "rate").Select(a => a.GetPrimaryKeyOrdinal(null)).FirstOrDefault());
      Assert.True(columns.Where(n => n.Name == "rate").Select(a => a.IsNullable).FirstOrDefault());
      Assert.That(columns.Where(n => n.Name == "rate").Select(a => a.GetOrdinal(2)), Has.One.Items.EqualTo(2));
      StringAssert.AreEqualIgnoringCase(columns.Where(n => n.Name == "rate").Select(a => a.GetDefaultValue()).FirstOrDefault(), "'0.00'");
      Assert.That(columns.Where(n => n.Name == "rate").Select(a => a.GetPrecision(5)), Has.One.Items.EqualTo(5));
      Assert.That(columns.Where(n => n.Name == "rate").Select(a => a.GetScale(2)), Has.One.Items.EqualTo(2));
      Assert.IsNull(columns.Where(n => n.Name == "rate").Select(a => a.GetMaxLength(null)).FirstOrDefault());

      Assert.That(columns.Where(n => n.Name == "created").Select(a => a.Name), Has.Exactly(1).Matches<string>(name => name.Contains("created")));
      Assert.That(columns.Where(n => n.Name == "created").Select(a => a.GetDefaultValue()), Has.One.Items.EqualTo("CURRENT_TIMESTAMP"));
    }


    [Test]
    public void CanReadFKs()
    {
      var sql = @" 
      DROP DATABASE IF EXISTS sakiladb;
      CREATE DATABASE sakiladb; 
      USE sakiladb;
      CREATE TABLE country (
        country_id SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT,
        country VARCHAR(50) NOT NULL,
        last_update TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
        PRIMARY KEY  (country_id)
      );
      CREATE TABLE city (
        city_id SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT,
        city VARCHAR(50) NOT NULL,
        country_id SMALLINT UNSIGNED NOT NULL,
        last_update TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
        PRIMARY KEY  (city_id),
        KEY idx_fk_country_id (country_id),
        CONSTRAINT `fk_city_country` FOREIGN KEY (country_id) REFERENCES country (country_id) ON DELETE RESTRICT ON UPDATE CASCADE
      );";
      var dbModel = _fixture!.CreateModel("sakiladb", sql, new List<string> { "city", "country" }, new List<string>());

      var fk = (dbModel.Tables.Single(t => t.ForeignKeys.Count > 0).ForeignKeys);
      Assert.IsNotNull(fk);
      Assert.AreEqual("sakiladb", fk[0].Table.Database!.DatabaseName);
      Assert.AreEqual("city", fk[0].Table.Name);
      Assert.AreEqual("sakiladb", fk[0].PrincipalTable.Database!.DatabaseName);
      Assert.AreEqual("country", fk[0].PrincipalTable.Name);
      Assert.AreEqual("country_id", fk[0].GetColumn().Name);
      Assert.AreEqual("country_id", fk[0].GetPrincipalColumn().Name);
      Assert.AreEqual(ReferentialAction.Restrict, fk[0].OnDelete);
    }

    [Test]
    public void CanReadIndexes()
    {
      var sql = @" 
      DROP DATABASE IF EXISTS sakilaIndex;
      CREATE DATABASE sakilaIndex; 
                use sakilaIndex;
                CREATE TABLE actor(
                  actor_id SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT,
                  first_name VARCHAR(45) NOT NULL,
                  last_name VARCHAR(45) NOT NULL UNIQUE,
                  last_update TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                  PRIMARY KEY(actor_id),
                  KEY idx_actor_first_last_name(first_name, last_name))";

      var dbModel = _fixture!.CreateModel("sakilaIndex", sql, new List<string> { "actor" }, new List<string>());

      var indexes = dbModel.Tables.Single().Indexes;

      Assert.Multiple(() =>
      {
        var c = indexes[0];
        Assert.AreEqual("sakilaIndex", c.Table!.Database!.DatabaseName);
        Assert.AreEqual("actor", c.Table.Name);
      });

      Assert.Multiple(() =>
        {
          var composite = indexes[0];
          Assert.AreEqual("idx_actor_first_last_name", composite.Name);
          Assert.False(composite.IsUnique);
          Assert.AreEqual(new List<string> { "first_name", "last_name" }, composite.GetColumns().Select(c => c.GetColumn().Name).ToList());
        });

      Assert.Multiple(() =>
        {
          var onecolumn = indexes[1];
          Assert.AreEqual("last_name", onecolumn.GetColumn().Name);
          Assert.True(onecolumn.IsUnique);
        }
        );
    }


    [Test]
    public void CanCreateModelForWorldDB()
    {
      Assembly executingAssembly = typeof(MySQLDatabaseModelFixture).GetTypeInfo().Assembly;
      Stream stream = executingAssembly.GetManifestResourceStream("MySql.EntityFrameworkCore.Design.Tests.Properties.world.sql")!;
      StreamReader sr = new StreamReader(stream);
      string sql = sr.ReadToEnd();
      sr.Dispose();

      var dbModel = _fixture!.CreateModel("world", sql, new List<string> { "city", "country", "countrylanguage" }, new List<string>(), true);
      var tables = dbModel.Tables.OrderBy(t => t.Name);

      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("city"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("country"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("countrylanguage"));
    }
    private void Test(string createSql, IEnumerable<string> tables, IEnumerable<string> schemas, Action<DatabaseModel> asserter, string cleanupSql)
    {
      _fixture!.TestStore.ExecuteNonQuery(createSql);

      try
      {
        var databaseModelFactory = new MySQLDatabaseModelFactory(
            new DiagnosticsLogger<DbLoggerCategory.Scaffolding>(
                _fixture.ListLoggerFactory,
                new LoggingOptions(),
                new DiagnosticListener("Fake"),
                new MySQLLoggingDefinitions(),
                new NullDbContextLogger()),
            _fixture.options);

        var databaseModel = databaseModelFactory.Create(_fixture.TestStore.ConnectionString,
            new DatabaseModelFactoryOptions(tables, schemas));
        Assert.NotNull(databaseModel);
        asserter(databaseModel);
      }
      finally
      {
        if (!string.IsNullOrEmpty(cleanupSql))
        {
          _fixture.TestStore.ExecuteNonQuery(cleanupSql);
        }
      }
    }

    [Test]
    public void CanCreateModelForSakila()
    {
      if (!TestUtils.IsAtLeast(5, 6, 0))
      {
        Assert.Ignore();
      }

      Assembly executingAssembly = typeof(MySQLDatabaseModelFixture).GetTypeInfo().Assembly;
      Stream stream = executingAssembly.GetManifestResourceStream("MySql.EntityFrameworkCore.Design.Tests.Properties.sakiladb-schema.sql")!;
      StreamReader sr = new StreamReader(stream);
      string sql = sr.ReadToEnd();
      sr.Dispose();

      var dbModel = _fixture!.CreateModel("sakiladb", sql, new List<string> { "actor", "address", "category",
                "city", "country", "customer",
                "film", "film_actor", "film_category", "film_text",
                "inventory", "language", "payment", "rental", "staff", "store"}, new List<string>(), true);
      var tables = dbModel.Tables.OrderBy(t => t.Name);

      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("actor"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("address"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("category"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("city"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("country"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("customer"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("film"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("film_actor"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("film_category"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("film_text"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("inventory"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("language"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("payment"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("rental"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("staff"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("store"));
    }



    /// <summary>
    /// No support for views added in EF Core 
    /// issue for follow up https://github.com/aspnet/EntityFramework/issues/827
    /// views should be ignored
    /// </summary>

    [Test]
    public void CanCreateModelOfDBWithViews()
    {
      var sql = @"DROP DATABASE IF EXISTS testview;
                create database testview character set utf8mb4;
                use testview;
                create table t1(a serial, b int);
                create table t2 like t1;
                create view x1 as select t1.a as a, t1.b as b1, t2.b as b2 from t1
                join t2 using (a) ;
                create view y1 as select t1.a as a, t1.b as b1, t2.b as b2 from t1
                join t2 using (a) ;
                create view z1 as select t1.a as a, t1.b as b1, t2.b as b2 from t1
                join t2 using (a) ;
                create view b1 as select x1.a as a, x1.b1 as b1, y1.b1 as b2 from x1
                join y1 using (a) ;";

      var dbModel = _fixture!.CreateModel("testview", sql, new List<string> { "t1", "t2" }, new List<string>());
      var tables = dbModel.Tables.OrderBy(t => t.Name);

      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("t1"));
      Assert.That(tables.Select(a => a.Name), Has.One.Items.EqualTo("t2"));
    }

    [Test]
    public void CanFiltersViews()
    {
      var sql = @"DROP DATABASE IF EXISTS testview;
                create database testview character set utf8mb4;
                use testview;
                create table t1(a serial, b int);            
                create view x1 as select t1.a as a, t1.b as b1 from t1;
                ";

      var selectionSet = new List<string> { "t1", "x1" };

      var dbModel = _fixture!.CreateModel("testview", sql, selectionSet, new List<string>());
      Assert.True(dbModel.Tables.Count == 2);
    }
  }
}