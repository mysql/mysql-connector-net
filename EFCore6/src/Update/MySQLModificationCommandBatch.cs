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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Update;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySql.EntityFrameworkCore.Update
{
  /// <summary>
  /// AffectedCountModificationCommandBatch implementation for MySQL
  /// </summary>
  internal class MySQLModificationCommandBatch : AffectedCountModificationCommandBatch
  {
    private const int DefaultNetworkPacketSizeBytes = 4096;
    private const int MaxScriptLength = 65536 * DefaultNetworkPacketSizeBytes / 2;
    private const int MaxParameterCount = 2100;
    private const int MaxRowCount = 1000;
    private int _parameterCount = 1; // Implicit parameter for the command text
    private readonly int _maxBatchSize;
    private readonly List<ModificationCommand> _bulkInsertCommands = new List<ModificationCommand>();
    private int _commandsLeftToLengthCheck = 50;

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

    protected new virtual IMySQLUpdateSqlGenerator UpdateSqlGenerator => (IMySQLUpdateSqlGenerator)base.UpdateSqlGenerator;

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

    private static bool CanBeInsertedInSameStatement(ModificationCommand firstCommand, ModificationCommand secondCommand)
    => string.Equals(firstCommand.TableName, secondCommand.TableName, StringComparison.Ordinal)
       && string.Equals(firstCommand.Schema, secondCommand.Schema, StringComparison.Ordinal)
       && firstCommand.ColumnModifications.Where(o => o.IsWrite).Select(o => o.ColumnName).SequenceEqual(
           secondCommand.ColumnModifications.Where(o => o.IsWrite).Select(o => o.ColumnName))
       && firstCommand.ColumnModifications.Where(o => o.IsRead).Select(o => o.ColumnName).SequenceEqual(
           secondCommand.ColumnModifications.Where(o => o.IsRead).Select(o => o.ColumnName));
  }
}
