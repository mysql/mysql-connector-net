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

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using MySql.Data.MySqlClient;
using MySql.Data.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace MySql.Data.EntityFrameworkCore.Storage.Internal
{
  internal partial class MySQLRelationalCommand : RelationalCommand
  {

    public MySQLRelationalCommand(
        [NotNull] IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger, 
        [NotNull] string commandText, 
        [NotNull] IReadOnlyList<IRelationalParameter> parameters)
      : base(logger, commandText, parameters)
    {
    }

    //TODO: check this
    //private RelationalDataReader ExecuteReader2(
    //  [NotNull] IRelationalConnection connection, 
    //  [CanBeNull] IReadOnlyDictionary<string, object> parameterValues)
    //{
    //  ThrowIf.Argument.IsNull(connection, nameof(connection));

    //  var dbCommand = CreateCommand(connection, parameterValues);

    //  connection.Open();

    //  var commandId = Guid.NewGuid();

    //  var startTime = DateTimeOffset.UtcNow;
    //  var stopwatch = Stopwatch.StartNew();

    //  Logger.CommandExecuting(
    //    dbCommand,
    //    DbCommandMethod.ExecuteReader,
    //    commandId,
    //    connection.ConnectionId,
    //    false,
    //    startTime);

    //  object result;
    //  var readerOpen = false;
    //  try
    //  {
    //    result = new MySQLRelationalDataReader(
    //      connection, 
    //      dbCommand, 
    //      new MySQLDataReader(((MySqlCommand)dbCommand).ExecuteReader() as MySqlDataReader),
    //      commandId,
    //      Logger);
    //    readerOpen = true;

    //    Logger.CommandExecuted(
    //      dbCommand,
    //      DbCommandMethod.ExecuteReader,
    //      commandId,
    //      connection.ConnectionId,
    //      result,
    //      false,
    //      startTime,
    //      stopwatch.Elapsed);
    //  }
    //  catch(Exception ex)
    //  {
    //    Logger.CommandError(
    //      dbCommand,
    //      DbCommandMethod.ExecuteReader,
    //      commandId,
    //      connection.ConnectionId,
    //      ex,
    //      false,
    //      startTime,
    //      stopwatch.Elapsed);

    //    throw;
    //  }
    //  finally
    //  {
    //    if (!readerOpen)
    //    {
    //      dbCommand.Dispose();
    //      connection.Close();
    //    }
    //  }

    //  return (MySQLRelationalDataReader)result;
    //}

    //TODO: check this
    //private async Task<RelationalDataReader> ExecuteReaderAsync2(
    //  [NotNull] IRelationalConnection connection, 
    //  [CanBeNull] IReadOnlyDictionary<string, object> parameterValues, 
    //  CancellationToken cancellationToken = default(CancellationToken))
    //{
    //  ThrowIf.Argument.IsNull(connection, nameof(connection));

    //  var dbCommand = CreateCommand(connection, parameterValues);

    //  await connection.OpenAsync(cancellationToken);

    //  var commandId = Guid.NewGuid();

    //  var startTime = DateTimeOffset.UtcNow;
    //  var stopwatch = Stopwatch.StartNew();

    //  Logger.CommandExecuting(
    //    dbCommand,
    //    DbCommandMethod.ExecuteReader,
    //    commandId,
    //    connection.ConnectionId,
    //    true,
    //    startTime);

    //  object result;
    //  var readerOpen = false;
    //  try
    //  {
    //    result = new RelationalDataReader(
    //      connection,
    //      dbCommand,
    //      new MySQLDataReader(await ((MySqlCommand)dbCommand).ExecuteReaderAsync(cancellationToken) as MySqlDataReader),
    //      commandId,
    //      Logger);
    //    readerOpen = true;

    //    Logger.CommandExecuted(
    //      dbCommand,
    //      DbCommandMethod.ExecuteReader,
    //      commandId,
    //      connection.ConnectionId,
    //      result,
    //      true,
    //      startTime,
    //      stopwatch.Elapsed);
    //  }
    //  catch(Exception ex)
    //  {
    //    Logger.CommandError(
    //      dbCommand,
    //      DbCommandMethod.ExecuteReader,
    //      commandId,
    //      connection.ConnectionId,
    //      ex,
    //      true,
    //      startTime,
    //      stopwatch.Elapsed);

    //    throw;
    //  }
    //  finally
    //  {
    //    if (!readerOpen)
    //    {
    //      dbCommand.Dispose();
    //      connection.Close();
    //    }
    //  }

    //  return (RelationalDataReader)result;
    //}
  }
}
