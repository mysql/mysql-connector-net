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
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Extensions;
using MySql.EntityFrameworkCore.Metadata;
using MySql.EntityFrameworkCore.Update;
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MySql.EntityFrameworkCore
{
  /// <summary>
  ///   <para>
  ///     The service lifetime is <see cref="ServiceLifetime.Singleton" />. This means a single instance
  ///     is used by many <see cref="DbContext" /> instances. The implementation must be thread-safe.
  ///     This service cannot depend on services registered as <see cref="ServiceLifetime.Scoped" />.
  ///   </para>
  /// </summary>
  internal class MySQLUpdateSqlGenerator : UpdateSqlGenerator, IMySQLUpdateSqlGenerator
  {
    public MySQLUpdateSqlGenerator([NotNull] UpdateSqlGeneratorDependencies dependencies)
      : base(dependencies)
    {
    }

#if NET6_0
    public virtual ResultSetMapping AppendBulkInsertOperation(
    StringBuilder commandStringBuilder,
    IReadOnlyList<IReadOnlyModificationCommand> modificationCommands,
    int commandPosition)
    {
      var table = StoreObjectIdentifier.Table(modificationCommands[0].TableName, modificationCommands[0].Schema);
      if (modificationCommands.Count == 1
        && modificationCommands[0].ColumnModifications.All(
                   o => !o.IsKey
                  || !o.IsRead
                  || o.Property?.GetValueGenerationStrategy(table) == MySQLValueGenerationStrategy.IdentityColumn))
      {
        return AppendInsertOperation(commandStringBuilder, modificationCommands[0], commandPosition);
      }

      var readOperations = modificationCommands[0].ColumnModifications.Where(o => o.IsRead).ToList();
      var writeOperations = modificationCommands[0].ColumnModifications.Where(o => o.IsWrite).ToList();
      var keyOperations = modificationCommands[0].ColumnModifications.Where(o => o.IsKey).ToList();

      var defaultValuesOnly = writeOperations.Count == 0;
      var nonIdentityOperations = modificationCommands[0].ColumnModifications
        .Where(o => o.Property?.GetValueGenerationStrategy(table) != MySQLValueGenerationStrategy.IdentityColumn)
        .ToList();

      if (defaultValuesOnly)
      {
        if (nonIdentityOperations.Count == 0 || readOperations.Count == 0)
        {
          foreach (var modification in modificationCommands)
          {
            AppendInsertOperation(commandStringBuilder, modification, commandPosition);
          }

          return readOperations.Count == 0
              ? ResultSetMapping.NoResultSet
              : ResultSetMapping.LastInResultSet;
        }

        if (nonIdentityOperations.Count > 1)
        {
          nonIdentityOperations = new List<IColumnModification> { nonIdentityOperations.First() };
        }
      }

      if (readOperations.Count == 0)
      {
        return AppendBulkInsertWithoutServerValues(commandStringBuilder, modificationCommands, writeOperations);
      }

      foreach (var modification in modificationCommands)
      {
        AppendInsertOperation(commandStringBuilder, modification, commandPosition);
      }

      return ResultSetMapping.LastInResultSet;
    }

    private ResultSetMapping AppendBulkInsertWithoutServerValues(
    StringBuilder commandStringBuilder,
    IReadOnlyList<IReadOnlyModificationCommand> modificationCommands,
    List<IColumnModification> writeOperations)
    {
      Debug.Assert(writeOperations.Count > 0);

      var name = modificationCommands[0].TableName;
      var schema = modificationCommands[0].Schema;

      AppendInsertCommandHeader(commandStringBuilder, name, schema, writeOperations);
      AppendValuesHeader(commandStringBuilder, writeOperations);
      AppendValues(commandStringBuilder, string.Empty, null, writeOperations);
      for (var i = 1; i < modificationCommands.Count; i++)
      {
        commandStringBuilder.Append(",").AppendLine();
        AppendValues(commandStringBuilder, string.Empty, null, modificationCommands[i].ColumnModifications.Where(o => o.IsWrite).ToList());
      }
      commandStringBuilder.Append(SqlGenerationHelper.StatementTerminator).AppendLine();

      return ResultSetMapping.NoResultSet;
    }

    protected override void AppendInsertCommandHeader(
          [NotNull] StringBuilder commandStringBuilder,
          [NotNull] string name,
           string? schema,
          [NotNull] IReadOnlyList<IColumnModification> operations)
    {
      Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
      Check.NotEmpty(name, nameof(name));
      Check.NotNull(operations, nameof(operations));

      base.AppendInsertCommandHeader(commandStringBuilder, name, schema, operations);

      if (operations.Count <= 0)
      {
        commandStringBuilder.Append(" ()");
      }
    }

    protected override void AppendValuesHeader(
      [NotNull] StringBuilder commandStringBuilder,
      [NotNull] IReadOnlyList<IColumnModification> operations)
    {
      Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
      Check.NotNull(operations, nameof(operations));

      commandStringBuilder.AppendLine();
      commandStringBuilder.Append("VALUES ");
    }

    protected override void AppendValues(
          [NotNull] StringBuilder commandStringBuilder,
          [NotNull] string name,
            string? schema,
          [NotNull] IReadOnlyList<IColumnModification> operations)
    {
      base.AppendValues(commandStringBuilder, name, schema, operations);

      if (operations.Count <= 0)
      {
        commandStringBuilder.Append("()");
      }
    }

    protected override void AppendIdentityWhereCondition(StringBuilder commandStringBuilder, IColumnModification columnModification)
    {
      Check.NotNull(columnModification, "columnModification");
      Check.NotNull(commandStringBuilder, "commandStringBuilder");
      commandStringBuilder.AppendFormat("{0} = LAST_INSERT_ID()", SqlGenerationHelper.DelimitIdentifier(columnModification.ColumnName));
    }

    protected override void AppendRowsAffectedWhereCondition(StringBuilder commandStringBuilder, int expectedRowsAffected)
    {
      Check.NotNull(commandStringBuilder, "commandStringBuilder");
      commandStringBuilder
        .Append("ROW_COUNT() = " + expectedRowsAffected.ToString(CultureInfo.InvariantCulture));
    }

    protected override ResultSetMapping AppendSelectAffectedCountCommand(StringBuilder commandStringBuilder, string name, string? schemaName, int commandPosition)
    {
      Check.NotNull(commandStringBuilder, "commandStringBuilder");
      Check.NotEmpty(name, "name");

      commandStringBuilder
        .Append("SELECT ROW_COUNT()")
        .Append(SqlGenerationHelper.StatementTerminator).AppendLine()
        .AppendLine();

      return ResultSetMapping.LastInResultSet;
    }

    protected override void AppendWhereAffectedClause(
      [NotNull] StringBuilder commandStringBuilder,
      [NotNull] IReadOnlyList<IColumnModification> operations)
    {
      Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
      Check.NotNull(operations, nameof(operations));

      var nonDefaultOperations = operations
        .Where(
            o => !o.IsKey ||
               !o.IsRead ||
               o.Property == null ||
               !o.Property.ValueGenerated.HasFlag(ValueGenerated.OnAdd) ||
               MySQLPropertyExtensions.IsCompatibleAutoIncrementColumn(o.Property))
        .ToList()
        .AsReadOnly();

      base.AppendWhereAffectedClause(commandStringBuilder, nonDefaultOperations);
    }

    internal enum ResultsGrouping
    {
      OneResultSet,
      OneCommandPerResultSet
    }
#elif NET7_0 || NET8_0
    public virtual ResultSetMapping AppendBulkInsertOperation(
      StringBuilder commandStringBuilder,
      IReadOnlyList<IReadOnlyModificationCommand> modificationCommands,
      int commandPosition,
      out bool requiresTransaction)
    {
      requiresTransaction = false;
      var firstCommand = modificationCommands[0];
      var table = StoreObjectIdentifier.Table(firstCommand.TableName, firstCommand.Schema);

      if (modificationCommands.Count == 1
      && firstCommand.ColumnModifications.All(
      o => !o.IsKey
      || !o.IsRead
      || o.Property?.GetValueGenerationStrategy(table) == MySQLValueGenerationStrategy.IdentityColumn))
      {
        return AppendInsertOperation(commandStringBuilder, firstCommand, commandPosition, false, out requiresTransaction);
      }

      var readOperations = firstCommand.ColumnModifications.Where(o => o.IsRead).ToList();
      var writeOperations = firstCommand.ColumnModifications.Where(o => o.IsWrite).ToList();
      var keyOperations = firstCommand.ColumnModifications.Where(o => o.IsKey).ToList();

      var defaultValuesOnly = writeOperations.Count == 0;
      var nonIdentityOperations = firstCommand.ColumnModifications
        .Where(o => o.Property?.GetValueGenerationStrategy(table) != MySQLValueGenerationStrategy.IdentityColumn)
        .ToList();

      if (defaultValuesOnly)
      {
        if (nonIdentityOperations.Count == 0 || readOperations.Count == 0)
        {
          requiresTransaction = modificationCommands.Count > 1;
          foreach (var modification in modificationCommands)
          {
            AppendInsertOperation(commandStringBuilder, modification, commandPosition, out var localRequiresTransaction);
            requiresTransaction = requiresTransaction || localRequiresTransaction;
          }

          return readOperations.Count == 0
            ? ResultSetMapping.NoResults
            : ResultSetMapping.LastInResultSet;
        }

        if (nonIdentityOperations.Count > 1)
        {
          nonIdentityOperations = new List<IColumnModification> { nonIdentityOperations.First() };
        }
      }

      if (readOperations.Count == 0)
      {
        return AppendInsertMultipleRows(commandStringBuilder, modificationCommands, writeOperations, out requiresTransaction);
      }

      foreach (var modification in modificationCommands)
      {
        AppendInsertOperation(commandStringBuilder, modification, commandPosition, false, out requiresTransaction);
      }

      return ResultSetMapping.LastInResultSet;
    }

    
    private ResultSetMapping AppendInsertMultipleRows(
    StringBuilder commandStringBuilder,
    IReadOnlyList<IReadOnlyModificationCommand> modificationCommands,
    List<IColumnModification> writeOperations,
    out bool requiresTransaction)
    {
      Debug.Assert(writeOperations.Count > 0);

      var name = modificationCommands[0].TableName;
      var schema = modificationCommands[0].Schema;

      AppendInsertCommandHeader(commandStringBuilder, name, schema, writeOperations);
      AppendValuesHeader(commandStringBuilder, writeOperations);
      AppendValues(commandStringBuilder, name, schema, writeOperations);

      for (var i = 1; i < modificationCommands.Count; i++)
      {
        commandStringBuilder.Append(",");
        AppendValues(commandStringBuilder, name, schema, modificationCommands[i].ColumnModifications.Where(o => o.IsWrite).ToList());
      }

      commandStringBuilder.Append(SqlGenerationHelper.StatementTerminator).AppendLine();
      requiresTransaction = false;

      return ResultSetMapping.NoResults;
    }

    public override ResultSetMapping AppendInsertOperation(
      StringBuilder commandStringBuilder,
      IReadOnlyModificationCommand command,
      int commandPosition,
      out bool requiresTransaction)
      => AppendInsertOperation(commandStringBuilder, command, commandPosition, overridingSystemValue: false, out requiresTransaction);

    public virtual ResultSetMapping AppendInsertOperation(
      StringBuilder commandStringBuilder,
      IReadOnlyModificationCommand command,
      int commandPosition,
      bool overridingSystemValue,
      out bool requiresTransaction)
    {
      requiresTransaction = false;
      var name = command.TableName;
      var schema = command.Schema;
      var operations = command.ColumnModifications;

      var writeOperations = operations.Where(o => o.IsWrite).ToList();
      var readOperations = operations.Where(o => o.IsRead).ToList();

      AppendInsertCommand(commandStringBuilder, name, schema, writeOperations, readOperations, overridingSystemValue);

      if (readOperations.Count > 0)
      {
        var keyOperations = operations.Where(o => o.IsKey).ToList();
        return AppendSelectAffectedCommand(commandStringBuilder, name, schema, readOperations, keyOperations);
      }

      return readOperations.Count > 0 ? ResultSetMapping.LastInResultSet : ResultSetMapping.NoResults;
    }

    protected virtual void AppendInsertCommand(
      StringBuilder commandStringBuilder,
      string name,
      string? schema,
      IReadOnlyList<IColumnModification> writeOperations,
      IReadOnlyList<IColumnModification> readOperations,
      bool overridingSystemValue)
    {
      AppendInsertCommandHeader(commandStringBuilder, name, schema, writeOperations);
      AppendValuesHeader(commandStringBuilder, writeOperations);
      AppendValues(commandStringBuilder, name, schema, writeOperations);
      commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator);
    }

    public override ResultSetMapping AppendUpdateOperation(
      StringBuilder commandStringBuilder,
      IReadOnlyModificationCommand command,
      int commandPosition,
      out bool requiresTransaction)
    {
      Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
      Check.NotNull(command, nameof(command));

      requiresTransaction = false;
      var name = command.TableName;
      var schema = command.Schema;
      var operations = command.ColumnModifications;

      var writeOperations = operations.Where(o => o.IsWrite).ToList();
      var conditionOperations = operations.Where(o => o.IsCondition).ToList();
      var readOperations = operations.Where(o => o.IsRead).ToList();

      AppendUpdateCommand(commandStringBuilder, name, schema, writeOperations, readOperations, conditionOperations);

      if (readOperations.Count > 0)
      {
        var keyOperations = operations.Where(o => o.IsKey).ToList();

        return AppendSelectAffectedCommand(commandStringBuilder, name, schema, readOperations, keyOperations);
      }

      return ResultSetMapping.NoResults;
    }

    public override ResultSetMapping AppendDeleteOperation(
      StringBuilder commandStringBuilder,
      IReadOnlyModificationCommand command,
      int commandPosition,
      out bool requiresTransaction)
    {
      // The default implementation adds RETURNING 1 to do concurrency check (was the row actually deleted), but in PostgreSQL we check
      // the per-statement row-affected value exposed by Npgsql in the batch; so no need for RETURNING 1.
      var name = command.TableName;
      var schema = command.Schema;
      var conditionOperations = command.ColumnModifications.Where(o => o.IsCondition).ToList();

      requiresTransaction = false;

      AppendDeleteCommand(commandStringBuilder, name, schema, Array.Empty<IColumnModification>(), conditionOperations);

      return ResultSetMapping.NoResults;
    }

    protected override void AppendValues(
      [NotNull] StringBuilder commandStringBuilder,
      [NotNull] string name,
      string? schema,
      [NotNull] IReadOnlyList<IColumnModification> operations)
    {
      base.AppendValues(commandStringBuilder, name, schema, operations);

      if (operations.Count <= 0)
      {
        commandStringBuilder.Append("()");
      }
    }

    /// <summary>
    /// Appends a SQL command for selecting affected data.
    /// </summary>
    /// <param name="commandStringBuilder">The builder to which the SQL should be appended.</param>
    /// <param name="name">The name of the table.</param>
    /// <param name="schema">The table schema, or <see langword="null" /> to use the default schema.</param>
    /// <param name="readOperations">The operations representing the data to be read.</param>
    /// <param name="conditionOperations">The operations used to generate the <c>WHERE</c> clause for the select.</param>
    /// <returns>The <see cref="ResultSetMapping" /> for this command.</returns>
    private ResultSetMapping AppendSelectAffectedCommand(
      StringBuilder commandStringBuilder,
      string name,
      string? schema,
      IReadOnlyList<IColumnModification> readOperations,
      IReadOnlyList<IColumnModification> conditionOperations)
    {
      Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
      Check.NotEmpty(name, nameof(name));
      Check.NotNull(readOperations, nameof(readOperations));
      Check.NotNull(conditionOperations, nameof(conditionOperations));

      AppendSelectCommandHeader(commandStringBuilder, readOperations);
      AppendFromClause(commandStringBuilder, name, schema);
      AppendWhereAffectedClause(commandStringBuilder, conditionOperations);
      commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator)
        .AppendLine();

      return ResultSetMapping.LastInResultSet;
    }

    /// <summary>
    ///   Appends a SQL fragment for starting a <c>SELECT</c>.
    /// </summary>
    /// <param name="commandStringBuilder">The builder to which the SQL should be appended.</param>
    /// <param name="operations">The operations representing the data to be read.</param>
    private void AppendSelectCommandHeader(
      StringBuilder commandStringBuilder,
      IReadOnlyList<IColumnModification> operations)
    {
      Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
      Check.NotNull(operations, nameof(operations));

      commandStringBuilder
        .Append("SELECT ")
        .AppendJoin(
          operations,
          SqlGenerationHelper,
          (sb, o, helper) => helper.DelimitIdentifier(sb, o.ColumnName));
    }

    /// <summary>
    ///   Appends a SQL fragment for starting a <c>FROM</c> clause.
    /// </summary>
    /// <param name="commandStringBuilder">The builder to which the SQL should be appended.</param>
    /// <param name="name">The name of the table.</param>
    /// <param name="schema">The table schema, or <see langword="null" /> to use the default schema.</param>
    private void AppendFromClause(
      StringBuilder commandStringBuilder,
      string name,
      string? schema)
    {
      Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
      Check.NotEmpty(name, nameof(name));

      commandStringBuilder
      .AppendLine()
      .Append("FROM ");

      SqlGenerationHelper.DelimitIdentifier(commandStringBuilder, name, schema);
    }

    /// <summary>
    ///   Appends a <c>WHERE</c> clause involving rows affected.
    /// </summary>
    /// <param name="commandStringBuilder">The builder to which the SQL should be appended.</param>
    /// <param name="operations">The operations from which to build the conditions.</param>
    protected virtual void AppendWhereAffectedClause(
      StringBuilder commandStringBuilder,
      IReadOnlyList<IColumnModification> operations)
    {
      Check.NotNull(commandStringBuilder, nameof(commandStringBuilder));
      Check.NotNull(operations, nameof(operations));

      commandStringBuilder
        .AppendLine()
        .Append("WHERE ");

      AppendRowsAffectedWhereCondition(commandStringBuilder, 1);

      if (operations.Count > 0)
      {
        commandStringBuilder
          .Append(" AND ")
          .AppendJoin(
            operations, (sb, v) =>
            {
              if (v.IsKey)
              {
                if (!v.IsRead)
                {
                  AppendWhereCondition(sb, v, v.UseOriginalValueParameter);
                  return true;
                }
              }

              if (IsIdentityOperation(v))
              {
                AppendIdentityWhereCondition(sb, v);
                return true;
              }

              return false;
            }, " AND ");
      }
    }

    private void AppendRowsAffectedWhereCondition(StringBuilder commandStringBuilder, int expectedRowsAffected)
    {
      Check.NotNull(commandStringBuilder, "commandStringBuilder");
      commandStringBuilder
      .Append("ROW_COUNT() = " + expectedRowsAffected.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///   Returns a value indicating whether the given modification represents an auto-incrementing column.
    /// </summary>
    /// <param name="modification">The column modification.</param>
    /// <returns><see langword="true" /> if the given modification represents an auto-incrementing column.</returns>
    protected virtual bool IsIdentityOperation(IColumnModification modification)
      => modification.IsKey && modification.IsRead;

    private void AppendIdentityWhereCondition(StringBuilder commandStringBuilder, IColumnModification columnModification)
    {
      Check.NotNull(columnModification, "columnModification");
      Check.NotNull(commandStringBuilder, "commandStringBuilder");
      commandStringBuilder.AppendFormat("{0} = LAST_INSERT_ID()", SqlGenerationHelper.DelimitIdentifier(columnModification.ColumnName));
    }
#endif


  }
}
