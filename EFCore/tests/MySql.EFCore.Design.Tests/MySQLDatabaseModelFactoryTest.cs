// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.Extensions.Logging;
using MySql.Data.EntityFrameworkCore.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace MySql.EntityFrameworkCore.Design.Tests
{
    public class MySQLDatabaseModelFactoryTest : IClassFixture<MySQLDatabaseModelFixture>
    {
        readonly MySQLDatabaseModelFixture _fixture;
        public MySQLDatabaseModelFactoryTest(MySQLDatabaseModelFixture fixture)
        {
            _fixture = fixture;            
        }



        [Fact]
        public void CanReadTables()
        {
            _fixture.dbName = "blogman";

            var sql = @"
DROP DATABASE IF EXISTS blogman;
CREATE DATABASE blogman; 
USE blogman;
CREATE TABLE blogs (id int);
CREATE TABLE posts (id int);";
            var dbModel = _fixture.CreateModel(sql, new TableSelectionSet(new List<string> { "blogs", "posts" }));

            Assert.Collection(dbModel.Tables.OrderBy(t => t.Name),
                d =>
                {                    
                    Assert.Equal("blogs", d.Name);
                },
                e =>
                {                 
                    Assert.Equal("posts", e.Name);
                });
        }

        [Fact]
        public void CanReadColumns()
        {
            _fixture.dbName = "blogman";

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
);
";
            var dbModel = _fixture.CreateModel(sql, new TableSelectionSet(new List<string> { "blogs"}));

            var columns = dbModel.Tables.Single().Columns;

            Assert.All(columns, c =>
            {
                Assert.Equal("blogman", c.Table.GetSchema());
                Assert.Equal("blogs", c.Table.Name);
            });

            Assert.Collection(columns,
                           id =>
                           {
                               Assert.Equal("id", id.Name);
                               Assert.Equal("int(11)", id.GetDataType());
                               Assert.Equal(2, id.GetPrimaryKeyOrdinal(2));
                               Assert.False(id.IsNullable);
                               Assert.Equal(0, id.GetOrdinal(0));
                             if (FactOnVersionsAttribute.Version >= new Version("5.7.0"))
                               Assert.Null(id.GetDefaultValue());
                             else
                               Assert.Equal("0", id.GetDefaultValue());
                           },
                        description =>
                        {
                            Assert.Equal("description", description.Name);
                            Assert.Equal("varchar(100)", description.GetDataType());
                            Assert.Equal(1, description.GetPrimaryKeyOrdinal(1));
                            Assert.False(description.IsNullable);
                            Assert.Equal(1, description.GetOrdinal(1));
                            Assert.Null(description.GetDefaultValue());
                            Assert.Equal(100, description.GetMaxLength(100));
                        },
                        rate =>
                        {
                            Assert.Equal("rate", rate.Name);
                            Assert.Equal("decimal(5,2)", rate.GetDataType());
                            Assert.Null(rate.GetPrimaryKeyOrdinal(null));
                            Assert.True(rate.IsNullable);
                            Assert.Equal(2, rate.GetOrdinal(2));
                            Assert.Equal("0.00", rate.GetDefaultValue());
                            Assert.Equal(5, rate.GetPrecision(5));
                            Assert.Equal(2, rate.GetScale(2));
                            Assert.Null(rate.GetMaxLength(null));
                        },
                    created =>
                    {
                        Assert.Equal("created", created.Name);
                        Assert.Equal("CURRENT_TIMESTAMP", created.GetDefaultValue());
                    });
        }


        [Fact]
        public void CanReadFKs()
        {
            _fixture.dbName = "sakiladb";

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
            var dbModel = _fixture.CreateModel(sql, new TableSelectionSet(new List<string> { "city", "country" }));

            var fk = Assert.Single(dbModel.Tables.Single(t => t.ForeignKeys.Count > 0).ForeignKeys);

            Assert.Equal("sakiladb", fk.Table.GetSchema());
            Assert.Equal("city", fk.Table.Name);
            Assert.Equal("sakiladb", fk.PrincipalTable.GetSchema());
            Assert.Equal("country", fk.PrincipalTable.Name);
            Assert.Equal("country_id", fk.GetColumn().Name);
            Assert.Equal("country_id", fk.GetPrincipalColumn().Name);
            Assert.Equal(ReferentialAction.Restrict, fk.OnDelete);
        }     

        [Fact]
        public void CanReadIndexes()
        {
            _fixture.dbName = "sakilaIndex";

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

            var dbModel = _fixture.CreateModel(sql, new TableSelectionSet(new List<string> { "actor" }));

            var indexes = dbModel.Tables.Single().Indexes;

            Assert.All(indexes, c =>
            {
                Assert.Equal("sakilaIndex", c.Table.GetSchema(), ignoreCase: true);
                Assert.Equal("actor", c.Table.Name);
            });

            Assert.Collection(indexes,
                onecolumn =>
                {
                  Assert.Equal("last_name", onecolumn.GetColumn().Name);
                  Assert.True(onecolumn.IsUnique);
                },
                unique =>
                {
                  Assert.Equal("actor_id", unique.GetColumn().Name);
                  Assert.True(unique.IsUnique);
                },
                composite =>
                {
                  Assert.Equal("idx_actor_first_last_name", composite.Name);
                  Assert.False(composite.IsUnique);
                  Assert.Equal(new List<string> { "first_name", "last_name" }, composite.GetColumns().Select(c => c.GetColumn().Name).ToList());
                });
        }


        [Fact]
        public void CanCreateModelForWorldDB()
        {
            Assembly executingAssembly = typeof(MySQLDatabaseModelFixture).GetTypeInfo().Assembly;
            Stream stream = executingAssembly.GetManifestResourceStream("MySql.EntityFrameworkCore.Design.Tests.Properties.world.sql");
            StreamReader sr = new StreamReader(stream);
            string sql = sr.ReadToEnd();
            sr.Dispose();
            _fixture.dbName = "world";


            var dbModel = _fixture.CreateModel(sql, new TableSelectionSet(new List<string> {"city", "country", "countrylanguage"}), null, true);
            Assert.Collection(dbModel.Tables.OrderBy(t => t.Name),
                          d =>
                          {
                              Assert.Equal("city", d.Name);
                          },
                          e =>
                          {
                              Assert.Equal("country", e.Name);
                          },
                          e =>
                          {
                            Assert.Equal("countrylanguage", e.Name);
                          }
            );
        }


        [FactOnVersions("5.6.0", null)]
        public void CanCreateModelForSakila()
        {
            Assembly executingAssembly = typeof(MySQLDatabaseModelFixture).GetTypeInfo().Assembly;
            Stream stream = executingAssembly.GetManifestResourceStream("MySql.EntityFrameworkCore.Design.Tests.Properties.sakiladb-schema.sql");
            StreamReader sr = new StreamReader(stream);
            string sql = sr.ReadToEnd();
            sr.Dispose();

            _fixture.dbName = "sakiladb";

            var dbModel = _fixture.CreateModel(sql, new TableSelectionSet(new List<string> { "actor", "address", "category",
            "city", "country", "customer", 
            "film", "film_actor", "film_category", "film_text",
            "inventory", "language", "payment", "rental", "staff", "store"}), null, true);
            Assert.Collection(dbModel.Tables.OrderBy(t => t.Name),
                          d =>
                          {
                              Assert.Equal("actor", d.Name);
                          },
                          e =>
                          {
                              Assert.Equal("address", e.Name);
                          },
                          f =>
                          {
                              Assert.Equal("category", f.Name);
                          },
                         g =>
                         {
                             Assert.Equal("city", g.Name);
                         },
                         f =>
                         {
                             Assert.Equal("country", f.Name);
                         },
                         f =>
                         {
                             Assert.Equal("customer", f.Name);
                         },
                         f =>
                         {
                             Assert.Equal("film", f.Name);
                         },
                         f =>
                         {
                             Assert.Equal("film_actor", f.Name);
                         },
                         f =>
                         {
                             Assert.Equal("film_category", f.Name);
                         },
                        f =>
                        {
                            Assert.Equal("film_text", f.Name);
                        },
                        f =>
                        {
                            Assert.Equal("inventory", f.Name);
                        },
                        f =>
                        {
                            Assert.Equal("language", f.Name);
                        },
                        f =>
                        {
                            Assert.Equal("payment", f.Name);
                        },
                        f =>
                        {
                            Assert.Equal("rental", f.Name);
                        },
                        f =>
                        {
                            Assert.Equal("staff", f.Name);
                        },
                        f =>
                        {
                            Assert.Equal("store", f.Name);
                        });
        }



        /// <summary>
        /// No support for views added in EF Core 
        /// issue for follow up https://github.com/aspnet/EntityFramework/issues/827
        /// views should be ignored
        /// </summary>

        [Fact]
        public void CanCreateModelOfDBWithViews()
        {
            _fixture.dbName = "testview";
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
            _fixture.dbName = "testview";

            var dbModel = _fixture.CreateModel(sql, new TableSelectionSet(new List<string> { "t1", "t2" }));
            Assert.Collection(dbModel.Tables.OrderBy(t => t.Name),
                                  d =>
                                  {
                                      Assert.Equal("t1", d.Name);
                                  },
                                  e =>
                                  {
                                      Assert.Equal("t2", e.Name);
                                  });
                                  
        }
       
        [Fact]
        public void CanFiltersViews()
        {
            _fixture.dbName = "testview";
            var sql = @"DROP DATABASE IF EXISTS testview;
            create database testview character set utf8mb4;
            use testview;
            create table t1(a serial, b int);            
            create view x1 as select t1.a as a, t1.b as b1 from t1;
            ";
            _fixture.dbName = "testview";

            var selectionSet = new TableSelectionSet(new List<string> { "t1" , "x1"});
            var logger = new MySQLDatabaseModelFixture.MyTestLogger();

            var dbModel = _fixture.CreateModel(sql, selectionSet, logger);
            Assert.Single(dbModel.Tables);
            Assert.DoesNotContain(logger.Items, i => i.logLevel == LogLevel.Warning);
        }
    }
}
