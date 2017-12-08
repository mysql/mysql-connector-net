﻿// Copyright © 2016, 2017 Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using MySql.EntityFrameworkCore.Migrations.Tests.Utilities;
using MySql.Data.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Linq;
using Xunit;

namespace MySql.EntityFrameworkCore.Migrations.Tests
{
    public abstract class MySQLMigrationsGeneratorTestBase
    {
      protected abstract IMigrationsSqlGenerator SqlGenerator { get; }
      protected virtual string Sql { get; set; }
      protected static string EOL => Environment.NewLine;
    
      [Fact]
      public virtual void CreateTableOperation()
      {
        Generate(
            new CreateTableOperation
            {
              Name = "People",
              Schema = null,
              Columns =
                {
                          new AddColumnOperation
                          {
                              Name = "Id",
                              ClrType = typeof(int),
                              IsNullable = false,
                              [MySQLAnnotationNames.AutoIncrement] = true
                          },
                          new AddColumnOperation
                          {
                              Name = "EmployerId",
                              ClrType = typeof(int),
                              IsNullable = true
                          },
                           new AddColumnOperation
                          {
                              Name = "SSN",
                              ClrType = typeof(string),
                              ColumnType = "char(11)",
                              IsNullable = true
                          }
                },
              PrimaryKey = new AddPrimaryKeyOperation
              {
                Table = "People",
                Columns = new[] { "Id" }           
              },
              UniqueConstraints =
                {
                          new AddUniqueConstraintOperation
                          {
                              Columns = new[] { "SSN" }
                          }
                },
              ForeignKeys =
                {
                          new AddForeignKeyOperation
                          {
                              Columns = new[] { "EmployerId" },
                              PrincipalTable = "Companies",
                              PrincipalColumns = new[] { "Id" }
                          }
                }
            });
      }


    [Fact]
    public void AddColumnOperation()
    {
      Generate(new AddColumnOperation
        {
          Table = "People",
          Schema = null,
          Name = "Name",
          ClrType = typeof(string),
          ColumnType = "varchar(50)",
          IsNullable = false
        }
      );
    }


    [Fact]
    public virtual void AddColumnOperationWithComputedValueSql()
    {
      Generate(new AddColumnOperation
      {
        Table = "People",
        Schema = null,
        Name = "DisplayName",
        ClrType = typeof(string),
        ColumnType = "varchar(50)",
        IsNullable = false,
        ComputedColumnSql = "CONCAT_WS(' ', LastName , FirstName)"
      }
     );
    }

    [Fact]
    public virtual void AddColumnOperationWithDefaultValueSql()
    {
      Generate(new AddColumnOperation
      {
        Table = "People",
        Schema = null,
        Name = "Timestamp",
        ClrType = typeof(DateTime),
        ColumnType = "datetime",
        IsNullable = false,
        DefaultValueSql = "CURRENT_TIMESTAMP"
      }
     );
    }


    [Fact]
    public virtual void AddColumnOperation_with_maxLength()
    {
      Generate(
               modelBuilder => modelBuilder.Entity("Person").Property<string>("Name").HasMaxLength(30),
               new AddColumnOperation
               {                
                 Table = "Person",
                 Name = "Name",
                 ClrType = typeof(string),
                 ColumnType = "varchar(30)",
                 IsNullable = true,
                 MaxLength = 30
               });

    }


    [Fact]
    public virtual void AlterColumnOperation()
    {
      Generate(new AlterColumnOperation
      {
        Table = "Person",
        Schema = "",
        Name = "Age",
        ClrType = typeof(int),
        ColumnType = "int",
        IsNullable = false,
        DefaultValue = 7
      });
    }


    [Fact]
    public virtual void AlterColumnOperationWithoutType()
    {
      Generate(
              new AlterColumnOperation
              {
                Table = "Person",
                Name = "Age",
                ClrType = typeof(int)
              });
    }


    [Fact]
    public virtual void RenameTableOperationInSchema()
    { 
       Generate(
              new RenameTableOperation
              {
                Name = "t1",
                Schema = "",
                NewName = "t2",
                NewSchema = ""
              });
    }

    [Fact]
    public virtual void CreateUniqueIndexOperation()
    { 
        Generate(
            new CreateIndexOperation
            {
              Name = "IXPersonName",
              Table = "Person",
              Schema = "",
              Columns = new[] { "FirstName", "LastName" },
              IsUnique = true
            });
    }


    [Fact]
    public virtual void CreateNonUniqueIndexOperation()
    { 
        Generate(
            new CreateIndexOperation
            {
              Name = "IXPersonName",
              Table = "Person",
              Columns = new[] { "Name" },
              IsUnique = false
            });
    }

    [Fact]
    public virtual void RenameIndexOperation()
    {
      Generate(
           new RenameIndexOperation
           {
             Name = "IXPersonName",
             Table = "Person",
             NewName = "IXNombre"             
           });
    }

    [Fact]
    public virtual void DropIndexOperation()
    {
      Generate(
            new DropIndexOperation
            {
              Name = "IXPersonName",
              Table = "Person"              
            });
    }

    protected virtual void Generate(MigrationOperation operation)
    {      
       Generate(_ => { }, new[] { operation });
    }

    protected virtual void Generate(Action<ModelBuilder> buildAction, params MigrationOperation[] operation)
    {
      var modelBuilder = ContextUtils.Instance.CreateModelBuilder();
      buildAction(modelBuilder);

      var batch = SqlGenerator.Generate(operation, modelBuilder.Model);

      Sql = string.Join(EOL, batch.Select(b => b.CommandText));
    }
  }
}
