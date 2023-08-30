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
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using MySql.EntityFrameworkCore.Metadata.Internal;
using MySql.EntityFrameworkCore.Migrations.Tests.Utilities;
using NUnit.Framework;
using System;
using System.Linq;

namespace MySql.EntityFrameworkCore.Migrations.Tests
{
  public abstract class MySQLMigrationsGeneratorTestBase
  {
    protected abstract IMigrationsSqlGenerator SqlGenerator { get; }
    protected virtual string? Sql { get; set; }
    protected static string EOL => Environment.NewLine;

    [Test]
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
                              Table ="People",
                              ClrType = typeof(int),
                              IsNullable = false,
                              [MySQLAnnotationNames.LegacyValueGeneratedOnAdd] = true
                          },
                          new AddColumnOperation
                          {
                              Name = "EmployerId",
                              Table ="People",
                              ClrType = typeof(int),
                              IsNullable = true
                          },
                           new AddColumnOperation
                          {
                              Name = "SSN",
                              Table ="People",
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


    [Test]
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


    [Test]
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

    [Test]
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


    [Test]
    public virtual void AddColumnOperation_with_maxLength()
    {
      Generate(
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


    [Test]
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


    [Test]
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


    [Test]
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

    [Test]
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


    [Test]
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

    [Test]
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

    [Test]
    public virtual void DropIndexOperation()
    {
      Generate(
            new DropIndexOperation
            {
              Name = "IXPersonName",
              Table = "Person"
            });
    }

    [Test]
    public virtual void DropPrimaryKeyOperation()
    {
      Generate(
        new DropPrimaryKeyOperation
        {
          Name = "IXPersonName",
          Table = "Person"
        });
    }

    [Test]
    public virtual void AddPrimaryKeyOperation()
    {
      Generate(
        new AddPrimaryKeyOperation
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

      var batch = SqlGenerator.Generate(operation, (IModel)modelBuilder.Model);

      Sql = string.Join(EOL, batch.Select(b => b.CommandText));
    }
  }
}
