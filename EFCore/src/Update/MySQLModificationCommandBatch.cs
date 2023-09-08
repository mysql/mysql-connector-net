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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
#if NET6_0
    private const int MaxRowCount = 1000;
    private int _parameterCount = 1; // Implicit parameter for the command text
    private readonly int _maxBatchSize;
    private readonly List<ModificationCommand> _bulkInsertCommands = new List<ModificationCommand>();
    private int _commandsLeftToLengthCheck = 50;
#elif NET7_0 || NET8_0
    private readonly List<IReadOnlyModificationCommand> _pendingBulkInsertCommands = new();
#endif


    

    protected new virtual IMySQLUpdateSqlGenerator UpdateSqlGenerator => (IMySQLUpdateSqlGenerator)base.UpdateSqlGenerator;

#if NET6_0
    public MySQLModificationCommandBatch(
    [NotNull] ModificationCommandBatchFactoryDependencies dependencies,
        int? maxBatchSize)
        : base(dependencies)
    {
      if (maxBatchSize.HasValue
        && maxBatchSize.Value <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(maxBatchSize), RelationalStrings.InvalidMaxBatchSize(maxBatchSize));
      }

      _maxBatchSize = Math.Min(maxBatchSize ?? int.MaxValue, MaxRowCount);
    }

    protected override bool CanAddCommand(IReadOnlyModificationCommand modificationCommand)
    {
      if (ModificationCommands.Count >= _maxBatchSize)
      {
        return false;
      }

      var additionalParameterCount = CountParameters(modificationCommand);

      if (_parameterCount + additionalParameterCount >= MaxParameterCount)
      {
        return false;
      }

      _parameterCount += additionalParameterCount;
      return true;
    }

    private static int CountParameters(IReadOnlyModificationCommand modificationCommand)
    {
      var parameterCount = 0;

      foreach (var columnModification in modificationCommand.ColumnModifications)
      {
        if (columnModification.UseCurrentValueParameter)
        {
          parameterCount++;
        }

        if (columnModification.UseOriginalValueParameter)
        {
          parameterCount++;
        }
      }

      return parameterCount;
    }

    /// <inheritdoc/>
    protected override bool IsCommandTextValid()
    {
      if (--_commandsLeftToLengthCheck < 0)
      {
        var commandTextLength = GetCommandText().Length;
        if (commandTextLength >= MaxScriptLength)
        {
          return false;
        }

        var averageCommandLength = commandTextLength / ModificationCommands.Count;
        var expectedAdditionalCommandCapacity = (MaxScriptLength - commandTextLength) / averageCommandLength;
        _commandsLeftToLengthCheck = Math.Max(1, expectedAdditionalCommandCapacity / 4);
      }

      return true;
    }

    /// <inheritdoc/>
    protected override int GetParameterCount()
      => _parameterCount;

    /// <inheritdoc/>
    protected override void ResetCommandText()
    {
      base.ResetCommandText();
      _bulkInsertCommands.Clear();
    }

    /// <inheritdoc/>
    protected override string GetCommandText()
      => base.GetCommandText() + GetBulkInsertCommandText(ModificationCommands.Count);

    private string GetBulkInsertCommandText(int lastIndex)
    {
      if (_bulkInsertCommands.Count == 0)
      {
        return string.Empty;
      }

      var stringBuilder = new StringBuilder();
      var resultSetMapping = UpdateSqlGenerator.AppendBulkInsertOperation(
        stringBuilder, _bulkInsertCommands, lastIndex - _bulkInsertCommands.Count);
      for (var i = lastIndex - _bulkInsertCommands.Count; i < lastIndex; i++)
      {
        CommandResultSet[i] = resultSetMapping;
      }

      if (resultSetMapping != ResultSetMapping.NoResultSet)
      {
        CommandResultSet[lastIndex - 1] = ResultSetMapping.LastInResultSet;
      }

      return stringBuilder.ToString();
    }

    /// <inheritdoc/>
    protected override void UpdateCachedCommandText(int commandPosition)
    {
      var newModificationCommand = ModificationCommands[commandPosition];
      if (newModificationCommand.EntityState == EntityState.Added)
      {
        if (_bulkInsertCommands.Count > 0
          && !CanBeInsertedInSameStatement(_bulkInsertCommands[0], (ModificationCommand)newModificationCommand))
        {
          CachedCommandText.Append(GetBulkInsertCommandText(commandPosition));
          _bulkInsertCommands.Clear();
        }

        _bulkInsertCommands.Add((ModificationCommand)newModificationCommand);

        LastCachedCommandIndex = commandPosition;
      }
      else
      {
        CachedCommandText.Append(GetBulkInsertCommandText(commandPosition));
        _bulkInsertCommands.Clear();

        base.UpdateCachedCommandText(commandPosition);
      }
    }

#elif NET7_0 || NET8_0
    /// <summary>
    ///   This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///   the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///   any release. You should only use it directly in your code with extreme caution and knowing that
    ///   doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public MySQLModificationCommandBatch(ModificationCommandBatchFactoryDependencies dependencies, int maxBatchSize)
      : base(dependencies) => MaxBatchSize = maxBatchSize;

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
#endif


    private static bool CanBeInsertedInSameStatement(
      IReadOnlyModificationCommand firstCommand,
      IReadOnlyModificationCommand secondCommand)
      => firstCommand.TableName == secondCommand.TableName
        && firstCommand.Schema == secondCommand.Schema
        && firstCommand.ColumnModifications.Where(o => o.IsWrite).Select(o => o.ColumnName).SequenceEqual(
          secondCommand.ColumnModifications.Where(o => o.IsWrite).Select(o => o.ColumnName))
        && firstCommand.ColumnModifications.Where(o => o.IsRead).Select(o => o.ColumnName).SequenceEqual(
          secondCommand.ColumnModifications.Where(o => o.IsRead).Select(o => o.ColumnName));

    

    
  }
}
