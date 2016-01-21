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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations.Operations;
using Microsoft.Data.Entity.Storage;
using MySql.Data.MySqlClient;
using Microsoft.Data.Entity.Migrations;
using MySQL.Data.Entity.Migrations;

namespace MySQL.Data.Entity
{
  public class MySQLDatabaseCreator : RelationalDatabaseCreator
  {
    private readonly MySQLConnection _connection;
    private readonly IMigrationsSqlGenerator _sqlGenerator;
    private readonly ISqlCommandBuilder _commandBuilder;

    public MySQLDatabaseCreator(
        MySQLConnection cxn,
        IMigrationsModelDiffer differ,
        IMigrationsSqlGenerator generator,
        IModel model,
        ISqlCommandBuilder commandBuilder)
        : base(model, cxn, differ, generator)
    {
      ThrowIf.Argument.IsNull(cxn, "connection");      
      ThrowIf.Argument.IsNull(differ, "modelDiffer");
      ThrowIf.Argument.IsNull(generator, "generator");
      ThrowIf.Argument.IsNull(commandBuilder, "commandBuilder");

      cxn.flag = 1;
      _connection = cxn;
      _sqlGenerator = generator;
      _commandBuilder = commandBuilder;
    }

    public override void Create()
    {
      using (var workingConnection = _connection.CreateSystemConnection())
      {
        GetCreateOps().ExecuteNonQuery(workingConnection);
        MySqlConnection.ClearPool((MySqlConnection)_connection.DbConnection);
      }
    }

    public override async Task CreateAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
      using (var workingConnection = _connection.CreateSystemConnection())
      {
        await GetCreateOps().ExecuteNonQueryAsync(workingConnection, cancellationToken);
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
        GetDropOps().ExecuteNonQuery(workingConnecton);
      }
    }

    public override async Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
      MySqlConnection.ClearAllPools();
      using (var workingConnecton = _connection.CreateSystemConnection())
      {
        await GetDropOps().ExecuteNonQueryAsync(workingConnecton, cancellationToken);
      }
    }

    public override bool Exists()
    {
      try
      {
        _connection.Open();
        _connection.Close();
        return true;
      }
      catch (Exception ex)
      {
        MySqlException mex = ex as MySqlException;
        if (mex == null) throw;
        if (mex.Number == 1049) return false;
        if (mex.InnerException == null) throw;
        mex = mex.InnerException as MySqlException;
        if (mex == null) throw;
        if (mex.Number == 1049) return false;
        throw;
      }
    }

    public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
      throw new NotImplementedException();
    }

    protected override bool HasTables()
    {
      string sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '" + _connection.DbConnection.Database + "'";
      long count = (long)_commandBuilder.Build(sql).ExecuteScalar(_connection);
      return count != 0;
    }

    protected override async Task<bool> HasTablesAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
      string sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = `" + _connection.DbConnection.Database + "`";
      long count = (long)await _commandBuilder.Build(sql).ExecuteScalarAsync(_connection, cancellationToken);
      return count != 0;
    }

    private IEnumerable<IRelationalCommand> GetCreateOps()
    {
      var ops = new MigrationOperation[]
          {
                new MySQLCreateDatabaseOperation { Name = _connection.DbConnection.Database }
          };
      return _sqlGenerator.Generate(ops);
    }

    private IEnumerable<IRelationalCommand> GetDropOps()
    {
      var ops = new MigrationOperation[]
          {
                new MySQLDropDatabaseOperation { Name = _connection.DbConnection.Database }
          };
      return _sqlGenerator.Generate(ops);
    }
  }
}