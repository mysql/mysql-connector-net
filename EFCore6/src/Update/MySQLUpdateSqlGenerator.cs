// Copyright (c) 2021, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Update;
using MySql.EntityFrameworkCore.Extensions;
using MySql.EntityFrameworkCore.Metadata;
using MySql.EntityFrameworkCore.Update;
using MySql.EntityFrameworkCore.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MySql.EntityFrameworkCore
{
  /// <summary>
  ///     <para>
  ///         The service lifetime is <see cref="ServiceLifetime.Singleton" />. This means a single instance
  ///         is used by many <see cref="DbContext" /> instances. The implementation must be thread-safe.
  ///         This service cannot depend on services registered as <see cref="ServiceLifetime.Scoped" />.
  ///     </para>
  /// </summary>
  internal class MySQLUpdateSqlGenerator : UpdateSqlGenerator, IMySQLUpdateSqlGenerator
  {
    public MySQLUpdateSqlGenerator([NotNull] UpdateSqlGeneratorDependencies dependencies)
      : base(dependencies)
    {
    }

    public virtual ResultSetMapping AppendBulkInsertOperation(
      StringBuilder commandStringBuilder,
      IReadOnlyList<ModificationCommand> modificationCommands,
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
    IReadOnlyList<ModificationCommand> modificationCommands,
    List<IColumnModification> writeOperations)
    {
      Debug.Assert(writeOperations.Count > 0);

      var name = modificationCommands[0].TableName;
      var schema = modificationCommands[0].Schema;

      AppendInsertCommandHeader(commandStringBuilder, name, schema, writeOperations);
      AppendValuesHeader(commandStringBuilder, writeOperations);
      AppendValues(commandStringBuilder,string.Empty, null, writeOperations);
      for (var i = 1; i < modificationCommands.Count; i++)
      {
        commandStringBuilder.Append(",").AppendLine();
        AppendValues(commandStringBuilder,string.Empty,null, modificationCommands[i].ColumnModifications.Where(o => o.IsWrite).ToList());
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
      commandStringBuilder.AppendFormat("{0}=LAST_INSERT_ID()", SqlGenerationHelper.DelimitIdentifier(columnModification.ColumnName));
    }

    protected override void AppendRowsAffectedWhereCondition(StringBuilder commandStringBuilder, int expectedRowsAffected)
    {
      Check.NotNull(commandStringBuilder, "commandStringBuilder");
      commandStringBuilder
        .Append("ROW_COUNT() = " + expectedRowsAffected.ToString(CultureInfo.InvariantCulture))
        .AppendLine();
    }

    protected override ResultSetMapping AppendSelectAffectedCountCommand(StringBuilder commandStringBuilder, string name, string? schemaName, int commandPosition)
    {
      Check.NotNull(commandStringBuilder, "commandStringBuilder");
      Check.NotEmpty(name, "name");


      commandStringBuilder
        .Append("SELECT ROW_COUNT()")
        .Append(SqlGenerationHelper.StatementTerminator)
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
  }
}
