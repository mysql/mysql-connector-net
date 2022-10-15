// Copyright (c) 2022, Oracle and/or its affiliates.
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
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.EntityFrameworkCore.Update
{
  /// <summary>
  ///   AffectedCountModificationCommandBatch implementation for MySQL
  /// </summary>
  internal class MySQLModificationCommandBatch : AffectedCountModificationCommandBatch
  {
    private const int DefaultNetworkPacketSizeBytes = 4096;
    private const int MaxScriptLength = 65536 * DefaultNetworkPacketSizeBytes / 2;
    private const int MaxParameterCount = 2100;
    private readonly List<IReadOnlyModificationCommand> _pendingBulkInsertCommands = new();

    /// <summary>
    ///   This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///   the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///   any release. You should only use it directly in your code with extreme caution and knowing that
    ///   doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public MySQLModificationCommandBatch(ModificationCommandBatchFactoryDependencies dependencies, int maxBatchSize)
      : base(dependencies) => MaxBatchSize = maxBatchSize;

    protected new virtual IMySQLUpdateSqlGenerator UpdateSqlGenerator => (IMySQLUpdateSqlGenerator)base.UpdateSqlGenerator;

    /// <summary>
    ///   The maximum number of <see cref="ModificationCommand"/> instances that can be added to a single batch.
    /// </summary>
    protected override int MaxBatchSize { get; }

    protected override void RollbackLastCommand(IReadOnlyModificationCommand modificationCommand)
    {
      if (_pendingBulkInsertCommands.Count > 0)
      {
        _pendingBulkInsertCommands.RemoveAt(_pendingBulkInsertCommands.Count - 1);
        return;
      }

      base.RollbackLastCommand(modificationCommand);
    }

    protected override bool IsValid()
      => SqlBuilder.Length < MaxScriptLength
        && ParameterValues.Count + 1 < MaxParameterCount;

    private void ApplyPendingBulkInsertCommands()
    {
      if (_pendingBulkInsertCommands.Count == 0)
        return;

      var commandPosition = ResultSetMappings.Count;
      var resultSetMapping = UpdateSqlGenerator.AppendBulkInsertOperation(
        SqlBuilder, _pendingBulkInsertCommands, commandPosition, out var requiresTransaction);

      foreach (var pendingCommand in _pendingBulkInsertCommands)
      {
        AddParameters(pendingCommand);

        ResultSetMappings.Add(resultSetMapping);
      }

      if (resultSetMapping != ResultSetMapping.NoResults)
        ResultSetMappings[^1] = ResultSetMapping.LastInResultSet;
    }

    protected override void AddCommand(IReadOnlyModificationCommand modificationCommand)
    {
      if (modificationCommand.EntityState == EntityState.Added)
      {
        if (_pendingBulkInsertCommands.Count > 0
          && !CanBeInsertedInSameStatement(_pendingBulkInsertCommands[0], modificationCommand))
        {
          ApplyPendingBulkInsertCommands();
          _pendingBulkInsertCommands.Clear();
        }

        _pendingBulkInsertCommands.Add(modificationCommand);
      }
      else
      {
        if (_pendingBulkInsertCommands.Count > 0)
        {
          ApplyPendingBulkInsertCommands();
          _pendingBulkInsertCommands.Clear();
        }

        base.AddCommand(modificationCommand);
      }
    }

    private static bool CanBeInsertedInSameStatement(
      IReadOnlyModificationCommand firstCommand,
      IReadOnlyModificationCommand secondCommand)
      => firstCommand.TableName == secondCommand.TableName
        && firstCommand.Schema == secondCommand.Schema
        && firstCommand.ColumnModifications.Where(o => o.IsWrite).Select(o => o.ColumnName).SequenceEqual(
          secondCommand.ColumnModifications.Where(o => o.IsWrite).Select(o => o.ColumnName))
        && firstCommand.ColumnModifications.Where(o => o.IsRead).Select(o => o.ColumnName).SequenceEqual(
          secondCommand.ColumnModifications.Where(o => o.IsRead).Select(o => o.ColumnName));

    /// <summary>
    ///   This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///   the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///   any release. You should only use it directly in your code with extreme caution and knowing that
    ///   doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void Complete(bool moreBatchesExpected)
    {
      ApplyPendingBulkInsertCommands();
      base.Complete(moreBatchesExpected);
    }

    /// <summary>
    ///   This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///   the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///   any release. You should only use it directly in your code with extreme caution and knowing that
    ///   doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void Execute(IRelationalConnection connection)
    {
      try
      {
        base.Execute(connection);
      }
      catch (DbUpdateException e) when (e.InnerException is MySqlException)
      {
        throw new DbUpdateException(MySQLStrings.SaveChangesFailed, e.InnerException, e.Entries);
      }
    }

    /// <summary>
    ///   This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///   the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///   any release. You should only use it directly in your code with extreme caution and knowing that
    ///   doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override async Task ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken = default)
    {
      try
      {
        await base.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
      }
      catch (DbUpdateException e) when (e.InnerException is MySqlException)
      {
        throw new DbUpdateException(MySQLStrings.SaveChangesFailed, e.InnerException, e.Entries);
      }
    }
  }
}
