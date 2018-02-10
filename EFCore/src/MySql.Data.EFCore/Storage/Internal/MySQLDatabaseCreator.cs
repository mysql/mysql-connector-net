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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.Data.MySqlClient;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using MySql.Data.EntityFrameworkCore.Migrations.Operations;

namespace MySql.Data.EntityFrameworkCore
{
  /// <summary>
  /// Relational Database creator implementation in MySQL
  /// </summary>
  internal partial class MySQLDatabaseCreator : RelationalDatabaseCreator
  {
    private readonly MySQLServerConnection _connection;
    private readonly IMigrationsSqlGenerator _sqlGenerator;
    private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;
    private IMigrationCommandExecutor _migrationCommandExecutor;



    public override void Create()
    {
      using (var workingConnection = _connection.CreateSystemConnection())
      {
        _migrationCommandExecutor.ExecuteNonQuery(GetCreateOps(), workingConnection);        
        MySqlConnection.ClearPool((MySqlConnection)_connection.DbConnection);
      }      

    }

    public override async Task CreateAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
      using (var workingConnection = _connection.CreateSystemConnection())
      {
        await _migrationCommandExecutor.ExecuteNonQueryAsync(GetCreateOps(), workingConnection, cancellationToken);
        MySqlConnection.ClearPool((MySqlConnection)_connection.DbConnection);
      }
    }

    //public override void CreateTables()
    //{
    //  IReadOnlyList<MigrationOperation> ops = modelDiffer.GetDifferences(null, Model);
    //  IReadOnlyList<SqlBatch> sqlBatches = sqlGenerator.Generate(ops, Model);
    //  SqlStatementExecutor.ExecuteNonQuery(connection, connection.DbTransaction, sqlBatches);
    //}

    //public override async Task CreateTablesAsync(CancellationToken cancellationToken = default(CancellationToken))
    //{
    //  IReadOnlyList<MigrationOperation> ops = modelDiffer.GetDifferences(null, Model);
    //  IReadOnlyList<SqlBatch> sqlBatches = sqlGenerator.Generate(ops, Model);
    //  await SqlStatementExecutor.ExecuteNonQueryAsync(connection, connection.DbTransaction, sqlBatches, cancellationToken);
    //}

    public override void Delete()
    {
      MySqlConnection.ClearAllPools();
      using (var workingConnecton = _connection.CreateSystemConnection())
      {
        _migrationCommandExecutor.ExecuteNonQuery(GetDropOps(), workingConnecton);
      }
    }

    public override async Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
      MySqlConnection.ClearAllPools();
      using (var workingConnecton = _connection.CreateSystemConnection())
      {
        await _migrationCommandExecutor.ExecuteNonQueryAsync(GetDropOps(), workingConnecton, cancellationToken);
      }
    }

    public override bool Exists()
    {
      Task<bool> task = ExistsAsync();
      task.Wait((int)((MySqlConnection)_connection.DbConnection).Settings.DefaultCommandTimeout * 1000);
      if (task.IsFaulted)
        throw task.Exception.GetBaseException();
      return task.Result;
    }

    public override async Task<bool> ExistsAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(_connection.ConnectionString);
      string database = settings.Database;
      if (string.IsNullOrWhiteSpace(database))
        throw new ArgumentNullException("Database");
      settings.Database = string.Empty;
      using(MySqlConnection conn = new MySqlConnection(settings.ToString()))
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

    protected override bool HasTables()
    {
      string sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '" + _connection.DbConnection.Database + "'";
      long count = (long)_rawSqlCommandBuilder.Build(sql).ExecuteScalar(_connection);
      return count != 0;
    }

    protected override async Task<bool> HasTablesAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
      string sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = `" + _connection.DbConnection.Database + "`";
      long count = (long)await _rawSqlCommandBuilder.Build(sql).ExecuteScalarAsync(_connection, cancellationToken: cancellationToken);
      return count != 0;
    }

    private IReadOnlyList<MigrationCommand> GetCreateOps()
    {
      var ops = new MigrationOperation[]
          {
                new MySQLCreateDatabaseOperation { Name = _connection.DbConnection.Database }
          };
      return _sqlGenerator.Generate(ops);
    }

    private IReadOnlyList<MigrationCommand> GetDropOps()
    {
      var ops = new MigrationOperation[]
          {
                new MySQLDropDatabaseOperation { Name = _connection.DbConnection.Database }
          };
      return _sqlGenerator.Generate(ops);
    }
  }
}