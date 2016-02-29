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

using System;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations.Operations;
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.Storage;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Infrastructure;
using MySQL.Data.Entity.Metadata;
using System.Linq;
using System.Collections.Generic;

namespace MySQL.Data.Entity.Migrations
{
  public class MySQLMigrationsSqlGenerator : MigrationsSqlGenerator
  {    
    public MySQLMigrationsSqlGenerator(
        [NotNull] IRelationalCommandBuilderFactory commandBuilderFactory,
        [NotNull] ISqlGenerator sqlGenerator,
        [NotNull] IRelationalTypeMapper typeMapper,
        [NotNull] IRelationalAnnotationProvider annotations)
            : base(commandBuilderFactory, sqlGenerator, typeMapper, annotations)
    {
    }

    protected override void Generate(
      [NotNull] MigrationOperation operation, 
      [CanBeNull] IModel model, 
      [NotNull] RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "operation");
      ThrowIf.Argument.IsNull(builder, "builder");

      if (operation is MySQLCreateDatabaseOperation)
        Generate(operation as MySQLCreateDatabaseOperation, model, builder);
      else if (operation is MySQLDropDatabaseOperation)
        Generate(operation as MySQLDropDatabaseOperation, model, builder);
      else
        base.Generate(operation, model, builder);
    }

    protected override void Generate(
      [NotNull] EnsureSchemaOperation operation, 
      [CanBeNull] IModel model, 
      [NotNull] RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "operation");
      ThrowIf.Argument.IsNull(builder, "builder");

      throw new NotImplementedException();
      //base.Generate(operation, model, builder);
    }

    protected virtual void Generate(
        [NotNull] MySQLCreateDatabaseOperation operation,
        [CanBeNull] IModel model,
        [NotNull] RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "operation");
      ThrowIf.Argument.IsNull(builder, "builder");

      builder
          .Append("CREATE DATABASE ")
          .Append(SqlGenerator.DelimitIdentifier(operation.Name));
    }

    protected virtual void Generate(
        [NotNull] MySQLDropDatabaseOperation operation,
        [CanBeNull] IModel model,
        [NotNull] RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "operation");
      ThrowIf.Argument.IsNull(builder, "builder");

      builder
          .Append("DROP DATABASE IF EXISTS ")
          .Append(SqlGenerator.DelimitIdentifier(operation.Name));
    }
    

    protected override void ColumnDefinition(
      [CanBeNull] string schema, 
      [CanBeNull] string table, 
      [NotNull] string name, 
      [NotNull] Type clrType, 
      [CanBeNull] string type, 
      bool nullable, 
      [CanBeNull] object defaultValue, 
      [CanBeNull] string defaultValueSql, 
      [CanBeNull] string computedColumnSql, 
      [NotNull] IAnnotatable annotatable, 
      [CanBeNull] IModel model, 
      [NotNull] RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsEmpty(name, "name");
      ThrowIf.Argument.IsNull(clrType, "clrType");
      ThrowIf.Argument.IsNull(annotatable, "annotatable");
      ThrowIf.Argument.IsNull(builder, "builder");


      if (type == null)
      {
        var property = FindProperty(model, schema, table, name);
        type = property != null
            ? TypeMapper.GetMapping(property).DefaultTypeName
            : TypeMapper.GetMapping(clrType).DefaultTypeName;
      }      

      if (computedColumnSql != null)
      {
         builder
              .Append(SqlGenerator.DelimitIdentifier(name))
              .Append(string.Format(" {0} AS ", type))
              .Append(" ( " + computedColumnSql + " )");

          return;
              
      }
      
            
      var autoInc = annotatable[MySQLAnnotationNames.Prefix + MySQLAnnotationNames.AutoIncrement];


      base.ColumnDefinition(
      schema,
      table,
      name,
      clrType,
      type,
      nullable,
      defaultValue,
      defaultValueSql,
      computedColumnSql,
      annotatable,
      model,
      builder);

      if (autoInc != null && (bool)autoInc)
      {
        builder.Append(" AUTO_INCREMENT");
      }      
    }


    protected override void DefaultValue(
           [CanBeNull] object defaultValue,
           [CanBeNull] string defaultValueSql,
           [NotNull] RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(builder, nameof(builder));

      if (defaultValueSql != null)
      {
        builder
            .Append(" DEFAULT (")
            .Append(defaultValueSql)
            .Append(")");
      }
      else if (defaultValue != null)
      {
        builder
            .Append(" DEFAULT ")
            .Append(defaultValue);
      }
    }



    protected override void PrimaryKeyConstraint(
         [NotNull] AddPrimaryKeyOperation operation,
         [CanBeNull] IModel model,
         [NotNull] RelationalCommandListBuilder builder)
    {

      ThrowIf.Argument.IsNull(operation, "AddPrimaryKeyOperation");
      ThrowIf.Argument.IsNull(builder, "RelationalCommandListBuider");


      //MySQL always assign PRIMARY to the PK name no way to override that.
      // check http://dev.mysql.com/doc/refman/5.1/en/create-table.html

   
      builder
          .Append("PRIMARY KEY ")
          .Append("(")
          .Append(string.Join(", ", operation.Columns.Select(SqlGenerator.DelimitIdentifier)))
          .Append(")");

      IndexTraits(operation, model, builder);
    }


  }
}
