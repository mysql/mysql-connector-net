// Copyright (c) 2015, 2020 Oracle and/or its affiliates.
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.Data.MySqlClient;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
using MySql.EntityFrameworkCore.Properties;

namespace MySql.EntityFrameworkCore
{
  /// <summary>
  /// Relational Database creator implementation in MySQL
  /// </summary>
  internal class MySQLDatabaseCreator : RelationalDatabaseCreator
  {
    private readonly MySQLServerConnection _connection;
    private readonly IMigrationsSqlGenerator _sqlGenerator;
    private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;
    private IMigrationCommandExecutor _migrationCommandExecutor;

    public MySQLDatabaseCreator(
  [NotNull] RelationalDatabaseCreatorDependencies dependencies,
  [NotNull] IRawSqlCommandBuilder rawSqlCommandBuilder)
  : base(dependencies)
    {
      _connection = (MySQLServerConnection)dependencies.Connection;
      _sqlGenerator = dependencies.MigrationsSqlGenerator;
      _rawSqlCommandBuilder = rawSqlCommandBuilder;
      _migrationCommandExecutor = dependencies.MigrationCommandExecutor;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual TimeSpan RetryTimeout { get; set; } = TimeSpan.FromMinutes(1);

    public override void Create()
    {
      using (var workingConnection = _connection.CreateSourceConnection())
      {
        _migrationCommandExecutor.ExecuteNonQuery(CreateCreateOperations(), workingConnection);
        ClearPool();
      }

      Exists(retryOnNotExists: true);
    }

    public override async Task CreateAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
      using (var workingConnection = _connection.CreateSourceConnection())
      {
        await _migrationCommandExecutor.ExecuteNonQueryAsync(CreateCreateOperations(), workingConnection, cancellationToken);
        ClearPool();
      }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void Delete()
    {
      ClearAllPools();
      using (var sourceConnection = _connection.CreateSourceConnection())
      {
        _migrationCommandExecutor.ExecuteNonQuery(CreateDropCommands(), sourceConnection);
      }
    }

    public override async Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
      ClearAllPools();
      using (var sourceConnection = _connection.CreateSourceConnection())
      {
        await _migrationCommandExecutor.ExecuteNonQueryAsync(CreateDropCommands(), sourceConnection, cancellationToken);
      }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override bool Exists()
      => Exists(retryOnNotExists: false);

    private bool Exists(bool retryOnNotExists)
      => Dependencies.ExecutionStrategyFactory.Create().Execute(
        DateTime.UtcNow + RetryTimeout, giveUp =>
        {
          while (true)
          {
            try
            {
              MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(_connection.ConnectionString);
              string database = settings.Database;
              if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentNullException("Database");
              settings.Database = string.Empty;
              using (var conn = new MySqlConnection(settings.ToString()))
              {
                conn.Open();
                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = $"SHOW DATABASES LIKE '{database}'";
                var result = cmd.ExecuteScalar();
                if (result == null)
                  return false;
                else
                  return ((string)result).Equals(database, StringComparison.OrdinalIgnoreCase);
              }
            }
            catch (MySqlException e)
            {
              if (!retryOnNotExists && IsDoesNotExist(e))
                return false;

              if (DateTime.UtcNow > giveUp
                  || !RetryOnExistsFailure(e))
                throw;

              Thread.Sleep(RetryDelay);
            }
          }
        });

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
        => ExistsAsync(retryOnNotExists: false, cancellationToken: cancellationToken);

    private Task<bool> ExistsAsync(bool retryOnNotExists, CancellationToken cancellationToken)
      => Dependencies.ExecutionStrategyFactory.Create().ExecuteAsync(
        DateTime.UtcNow + RetryTimeout, async (giveUp, ct) =>
        {
          while (true)
          {
            var opened = false;

            try
            {
              MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(_connection.ConnectionString);
              string database = settings.Database;
              if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentNullException("Database");
              settings.Database = string.Empty;
              using (MySqlConnection conn = new MySqlConnection(settings.ToString()))
              {
                await conn.OpenAsync(cancellationToken);
                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = $"SHOW DATABASES LIKE '{database}'";
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                if (result == null)
                  return false;
                else
                  return ((string)result).Equals(database, StringComparison.OrdinalIgnoreCase);
              }
            }
            catch (MySqlException e)
            {
              if (!retryOnNotExists
              && IsDoesNotExist(e))
              {
                return false;
              }

              if (DateTime.UtcNow > giveUp
              || !RetryOnExistsFailure(e))
              {
                throw;
              }

              await Task.Delay(RetryDelay, ct);
            }
          }
        }, cancellationToken);

    private static bool IsDoesNotExist(MySqlException exception) => exception.Number == 1049;

    private bool RetryOnExistsFailure(MySqlException e)
    {
      if (e.Number == 1049)
      {
        ClearPool();
        return true;
      }
      return false;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override bool HasTables()
      => Dependencies.ExecutionStrategyFactory.Create().Execute(
        _connection,
        connection => Convert.ToInt64(CreateHasTablesCommand()
        .ExecuteScalar(
          new RelationalCommandParameterObject(
            connection,
            null,
            null,
            Dependencies.CurrentContext.Context,
            Dependencies.CommandLogger)))
        != 0);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Task<bool> HasTablesAsync(CancellationToken cancellationToken = default(CancellationToken))
      => Dependencies.ExecutionStrategyFactory.Create().ExecuteAsync(
        _connection,
        async (connection, ct) => Convert.ToInt64( await CreateHasTablesCommand()
        .ExecuteScalarAsync(
          new RelationalCommandParameterObject(
            connection,
            null,
            null,
            Dependencies.CurrentContext.Context,
            Dependencies.CommandLogger),
          cancellationToken: ct))
        != 0, cancellationToken);

    private IRelationalCommand CreateHasTablesCommand()
    => _rawSqlCommandBuilder
        .Build(@"SELECT CASE WHEN COUNT(*) = 0 THEN FALSE ELSE TRUE END
              FROM information_schema.tables
              WHERE table_type = 'BASE TABLE' AND table_schema = '" + _connection.DbConnection.Database + "'");

    private IReadOnlyList<MigrationCommand> CreateCreateOperations()
    {
      var operations = new MigrationOperation[]
          {
                new MySQLCreateDatabaseOperation { Name = _connection.DbConnection.Database }
          };
      return Dependencies.MigrationsSqlGenerator.Generate(operations);
    }

    private IReadOnlyList<MigrationCommand> CreateDropCommands()
    {
      var databaseName = _connection.DbConnection.Database;
      if (string.IsNullOrEmpty(databaseName))
      {
        throw new InvalidOperationException(MySQLStrings.NoInitialCatalog);
      }

      var operations = new MigrationOperation[] { new MySQLDropDatabaseOperation { Name = databaseName } };

      return Dependencies.MigrationsSqlGenerator.Generate(operations);
    }

    // Clear connection pools in case there are active connections that are pooled
    private static void ClearAllPools() => MySqlConnection.ClearAllPools();

    // Clear connection pool for the database connection since after the 'create database' call, a previously
    // invalid connection may now be valid.
    private void ClearPool() => MySqlConnection.ClearPool((MySqlConnection)_connection.DbConnection);
  }
}