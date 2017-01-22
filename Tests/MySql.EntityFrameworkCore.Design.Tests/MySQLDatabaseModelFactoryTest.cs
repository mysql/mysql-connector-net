// Copyright © 2017 Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var sql = @" CREATE DATABASE blogman; 
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

            var sql = @" CREATE DATABASE blogman; 
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

            var columns = dbModel.Tables.Single().Columns.OrderBy(c => c.Ordinal);

            Assert.All(columns, c =>
            {
                Assert.Equal("blogman", c.Table.SchemaName);
                Assert.Equal("blogs", c.Table.Name);
            });

            Assert.Collection(columns,
                           id =>
                           {
                               Assert.Equal("id", id.Name);
                               Assert.Equal("int(11)", id.DataType);
                               Assert.Equal(2, id.PrimaryKeyOrdinal);
                               Assert.False(id.IsNullable);
                               Assert.Equal(0, id.Ordinal);
                               Assert.Null(id.DefaultValue);
                           },
                        description =>
                        {
                            Assert.Equal("description", description.Name);
                            Assert.Equal("varchar(100)", description.DataType);
                            Assert.Equal(1, description.PrimaryKeyOrdinal);
                            Assert.False(description.IsNullable);
                            Assert.Equal(1, description.Ordinal);
                            Assert.Null(description.DefaultValue);
                            Assert.Equal(100, description.MaxLength);
                        },
                        rate =>
                        {
                            Assert.Equal("rate", rate.Name);
                            Assert.Equal("decimal(5,2)", rate.DataType);
                            Assert.Null(rate.PrimaryKeyOrdinal);
                            Assert.True(rate.IsNullable);
                            Assert.Equal(2, rate.Ordinal);
                            Assert.Equal("0.00", rate.DefaultValue);
                            Assert.Equal(5, rate.Precision);
                            Assert.Equal(2, rate.Scale);
                            Assert.Null(rate.MaxLength);
                        },
                    created =>
                    {
                        Assert.Equal("created", created.Name);
                        Assert.Equal("CURRENT_TIMESTAMP", created.DefaultValue);
                    });
        }


        [Fact]
        public void CanReadFKs()
        {
            _fixture.dbName = "sakila";

            var sql = @" CREATE DATABASE sakila; 
USE sakila;
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

            Assert.Equal("sakila", fk.Table.SchemaName);
            Assert.Equal("city", fk.Table.Name);
            Assert.Equal("sakila", fk.PrincipalTable.SchemaName);
            Assert.Equal("city", fk.PrincipalTable.Name);
            Assert.Equal("country_id", fk.Columns.Single().Column.Name);
            Assert.Equal("country_id", fk.Columns.Single().PrincipalColumn.Name);
            Assert.Equal(ReferentialAction.Restrict, fk.OnDelete);
        }     

        [Fact]
        public void CanReadIndexes()
        {

            _fixture.dbName = "sakilaIndex";

            var sql = @" CREATE DATABASE sakilaIndex; 
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
                Assert.Equal("sakilaindex", c.Table.SchemaName);
                Assert.Equal("actor", c.Table.Name);
            });

            Assert.Collection(indexes,
                unique =>
                {
                    Assert.True(unique.IsUnique);
                    Assert.Equal("actor_id", unique.IndexColumns.Single().Column.Name);
                },
                onecolumn =>
                {
                    Assert.True(onecolumn.IsUnique);
                    Assert.Equal("last_name", onecolumn.IndexColumns.Single().Column.Name);
                },
                composite =>
                {
                    Assert.Equal("idx_actor_first_last_name", composite.Name);
                    Assert.False(composite.IsUnique);
                    Assert.Equal(new List<string> { "first_name", "last_name" }, composite.IndexColumns.Select(c => c.Column.Name).ToList());
                });
        }
    }
}
