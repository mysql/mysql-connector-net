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

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Linq;
using MySql.Data.EntityFrameworkCore.Migrations.Operations;
using MySql.Data.EntityFrameworkCore.Metadata.Internal;
using MySql.Data.EntityFrameworkCore.Storage.Internal;

namespace MySql.Data.EntityFrameworkCore.Migrations
{
  /// <summary>
  /// MigrationSqlGenerator implementation for MySQL
  /// </summary>
  internal partial class MySQLMigrationsSqlGenerator : MigrationsSqlGenerator
  {
    private readonly ISqlGenerationHelper _sqlGenerationHelper;
    private IRelationalTypeMapper _typeMapper;

    protected override void Generate(
      [NotNull] MigrationOperation operation,
      [CanBeNull] IModel model,
      [NotNull] MigrationCommandListBuilder builder)
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
      [NotNull] MigrationCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "operation");
      ThrowIf.Argument.IsNull(builder, "builder");

      builder
        .Append("CREATE DATABASE IF NOT EXISTS ")
        .Append(_sqlGenerationHelper.DelimitIdentifier(operation.Name));

      EndStatement(builder, suppressTransaction: true);
    }

    protected virtual void Generate(
        [NotNull] MySQLCreateDatabaseOperation operation,
        [CanBeNull] IModel model,
        MigrationCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "operation");
      ThrowIf.Argument.IsNull(builder, "builder");

      builder
          .Append("CREATE DATABASE ")
          .Append(_sqlGenerationHelper.DelimitIdentifier(operation.Name));

      EndStatement(builder, suppressTransaction: true);
    }

    protected virtual void Generate(
        [NotNull] MySQLDropDatabaseOperation operation,
        [CanBeNull] IModel model,
        [NotNull] MigrationCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "operation");
      ThrowIf.Argument.IsNull(builder, "builder");

      builder
          .Append("DROP DATABASE IF EXISTS ")
          .Append(_sqlGenerationHelper.DelimitIdentifier(operation.Name));

      EndStatement(builder, suppressTransaction: true);
    }


    protected override void ColumnDefinition(
       string schema,
            string table,
            string name,
            Type clrType,
            string type,
            bool? unicode,
            int? maxLength,
            bool rowVersion,
            bool nullable,
            object defaultValue,
            string defaultValueSql,
            string computedColumnSql,
            IAnnotatable annotatable,
            IModel model,
            MigrationCommandListBuilder builder)
    {
      ThrowIf.Argument.IsEmpty(name, "name");
      ThrowIf.Argument.IsNull(clrType, "clrType");
      ThrowIf.Argument.IsNull(annotatable, "annotatable");
      ThrowIf.Argument.IsNull(builder, "builder");


      var property = FindProperty(model, schema, table, name);

      if (type == null)
      {
        //Any property that maps to the column will work
        type = property != null
           ? _typeMapper.GetMapping(property).StoreType
           : _typeMapper.GetMapping(clrType).StoreType;
      }

      var charset = property?.FindAnnotation(MySQLAnnotationNames.Charset);
      if (charset != null)
      {
        type += $" CHARACTER SET {charset.Value}";
      }

      var collation = property?.FindAnnotation(MySQLAnnotationNames.Collation);
      if (collation != null)
      {
        type += $" COLLATE {collation.Value}";
      }

      if (computedColumnSql != null)
      {
        builder
             .Append(_sqlGenerationHelper.DelimitIdentifier(name))
             .Append(string.Format(" {0} AS ", type))
             .Append(" (" + computedColumnSql + ")");

        return;
      }

      var autoInc = annotatable[MySQLAnnotationNames.AutoIncrement];

      base.ColumnDefinition(
                schema, table, name, clrType, type, unicode, maxLength, rowVersion, nullable,
                defaultValue, defaultValueSql, computedColumnSql, annotatable, model, builder);

      if (autoInc != null && (bool)autoInc)
      {
        builder.Append(" AUTO_INCREMENT");
      }
    }


    protected override void DefaultValue(
           [CanBeNull] object defaultValue,
           [CanBeNull] string defaultValueSql,
           [NotNull] MigrationCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(builder, nameof(builder));

      if (defaultValueSql != null)
      {
        builder
            .Append(" DEFAULT ")
            .Append(defaultValueSql);
      }
      else if (defaultValue != null)
      {
        var typeMapping = (MySQLTypeMapping)_typeMapper.GetMappingForValue(defaultValue);
        builder
            .Append(" DEFAULT ")
            .Append(typeMapping.GenerateSqlLiteral(defaultValue));
      }
    }



    protected override void PrimaryKeyConstraint(
         [NotNull] AddPrimaryKeyOperation operation,
         [CanBeNull] IModel model,
         [NotNull] MigrationCommandListBuilder builder)
    {

      ThrowIf.Argument.IsNull(operation, "AddPrimaryKeyOperation");
      ThrowIf.Argument.IsNull(builder, "RelationalCommandListBuider");


      //MySQL always assign PRIMARY to the PK name no way to override that.
      // check http://dev.mysql.com/doc/refman/5.1/en/create-table.html

      builder
          .Append("PRIMARY KEY ")
          .Append("(")
          .Append(string.Join(", ", operation.Columns.Select(_sqlGenerationHelper.DelimitIdentifier)))
          .Append(")");

      IndexTraits(operation, model, builder);
    }

    protected override void Generate(AlterColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "AlterColumnOperation");
      ThrowIf.Argument.IsNull(model, "model");
      ThrowIf.Argument.IsNull(builder, "builder");

      var operationColumn = new AddColumnOperation();
      operationColumn.Schema = operation.Schema;
      operationColumn.Table = operation.Table;
      operationColumn.Name = operation.Name;
      operationColumn.ClrType = operation.ClrType;
      operationColumn.ColumnType = operation.ColumnType;
      operationColumn.ComputedColumnSql = operation.ComputedColumnSql;
      operationColumn.DefaultValue = operation.DefaultValue;
      operationColumn.DefaultValueSql = operation.DefaultValueSql;

      builder
       .Append("ALTER TABLE " + operation.Table)
       .Append(" MODIFY ");
      ColumnDefinition(operationColumn, model, builder);
      builder
        .AppendLine(_sqlGenerationHelper.StatementTerminator);
      EndStatement(builder);
    }

    protected override void Generate(RenameTableOperation operation, IModel model, MigrationCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "RenameTableOperation");
      ThrowIf.Argument.IsNull(model, "model");
      ThrowIf.Argument.IsNull(builder, "builder");

      builder
      .Append("ALTER TABLE " + operation.Name)
      .Append(" RENAME " + operation.NewName)
      .AppendLine(_sqlGenerationHelper.StatementTerminator);

      EndStatement(builder);
    }

    protected override void Generate(CreateIndexOperation operation, IModel model, MigrationCommandListBuilder builder)
    {

      ThrowIf.Argument.IsNull(operation, "CreateIndexOperation");
      ThrowIf.Argument.IsNull(model, "model");
      ThrowIf.Argument.IsNull(builder, "builder");

      builder
      .Append("CREATE " + (operation.IsUnique ? "UNIQUE " : "") + "INDEX ");

      builder.Append(_sqlGenerationHelper.DelimitIdentifier(operation.Name) + " ON " + operation.Table + " (" + string.Join(", ", operation.Columns.Select(_sqlGenerationHelper.DelimitIdentifier)) + ")")
             .AppendLine(_sqlGenerationHelper.StatementTerminator);

      EndStatement(builder);
    }

    protected override void Generate(RenameIndexOperation operation, IModel model, MigrationCommandListBuilder builder)
    {

      throw new NotSupportedException();

      //ThrowIf.Argument.IsNull(operation, "RenameIndexOperation");
      //ThrowIf.Argument.IsNull(model, "model");
      //ThrowIf.Argument.IsNull(builder, "builder");      

      ////table content remains the same
      //builder
      //.Append("DROP INDEX ")
      //.Append(_sqlGenerationHelper.DelimitIdentifier(operation.Name) + ", ")
      //.Append("CREATE INDEX " )
      //.Append(_sqlGenerationHelper.DelimitIdentifier(operation.Name) + " ON " + operation.Table );
    }

    protected override void Generate(DropIndexOperation operation, IModel model, MigrationCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "DropIndexOperation");
      ThrowIf.Argument.IsNull(model, "model");
      ThrowIf.Argument.IsNull(builder, "builder");

      builder
      .Append("DROP INDEX ")
      .Append(operation.Name)
      .Append(" ON " + operation.Table)
      .AppendLine(_sqlGenerationHelper.StatementTerminator);
      EndStatement(builder);
    }

    protected override void Generate(
      [NotNull] CreateTableOperation operation,
      [CanBeNull] IModel model,
      [NotNull] MigrationCommandListBuilder builder,
      bool terminate)
    {
      base.Generate(operation, model, builder, false);

      var entity = FindEntityTypes(model, operation.Schema, operation.Name).FirstOrDefault();

      var charset = entity?.FindAnnotation(MySQLAnnotationNames.Charset);
      if (charset != null)
      {
        builder.Append($" CHARACTER SET {charset.Value}");
      }

      var collation = entity?.FindAnnotation(MySQLAnnotationNames.Collation);
      if (collation != null)
      {
        builder.Append($" COLLATE {collation.Value}");
      }

      if (terminate)
      {
        builder.AppendLine(_sqlGenerationHelper.StatementTerminator);
        EndStatement(builder);
      }
    }
  }
}
