//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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

using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Tests
{
  internal class ParityTests : TestBase
  {
    /// <summary>
    /// Checks parity among sync and async execution of ExecuteNonQuery.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task ExecuteNonQueryParity()
    {
      ExecuteSQL("DROP TABLE IF EXISTS wl17260_parity;");
      ExecuteSQL("CREATE TABLE wl17260_parity(id int PRIMARY KEY, name varchar(50));");

      var connectionString = Connection.ConnectionString;
      int syncRows;
      int asyncRows;

      // Sync path
      using (var syncConnection = new MySqlConnection(connectionString))
      {
        syncConnection.Open();
        using var cmd = new MySqlCommand("INSERT INTO wl17260_parity(id, name) VALUES(1, 'sync')", syncConnection);
        syncRows = cmd.ExecuteNonQuery();
      }

      // Async path
      using (var asyncConnection = new MySqlConnection(connectionString))
      {
        await asyncConnection.OpenAsync();
        using var cmd = new MySqlCommand("INSERT INTO wl17260_parity(id, name) VALUES(2, 'async')", asyncConnection);
        asyncRows = await cmd.ExecuteNonQueryAsync();
      }

      // Assert parity on affected rows
      Assert.That(syncRows, Is.EqualTo(1));
      Assert.That(asyncRows, Is.EqualTo(1));

      // Assert parity on resulting state
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        using var countCmd = new MySqlCommand("SELECT COUNT(*) FROM wl17260_parity", connection);
        var count = Convert.ToInt32(countCmd.ExecuteScalar());
        Assert.That(count, Is.EqualTo(2));
      }
    }

    /// <summary>
    /// Checks parity among sync and async execution of ExecuteNonQuery with out parameters.
    /// </summary>
    [Test]
    public async Task StoredProcedureOutputParameterParity()
    {
      // Arrange
      ExecuteSQL("DROP PROCEDURE IF EXISTS wl17260_out_parity;");
      ExecuteSQL(@"
    CREATE PROCEDURE wl17260_out_parity(
      in p_in int,
      inout p_inout int,
      out p_out int
    )
    BEGIN
      set p_inout = p_inout + p_in;
      set p_out = p_in * 10;
    END;");

      var connectionString = Connection.ConnectionString;
      int syncInOut;
      int syncOut;
      int asyncInOut;
      int asyncOut;

      // Sync path
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        using (var command = new MySqlCommand("wl17260_out_parity", connection))
        {
          command.CommandType = CommandType.StoredProcedure;

          var pIn = command.Parameters.Add("@p_in", MySqlDbType.Int32);
          pIn.Direction = ParameterDirection.Input;
          pIn.Value = 7;

          var pInOut = command.Parameters.Add("@p_inout", MySqlDbType.Int32);
          pInOut.Direction = ParameterDirection.InputOutput;
          pInOut.Value = 3;

          var pOut = command.Parameters.Add("@p_out", MySqlDbType.Int32);
          pOut.Direction = ParameterDirection.Output;

          command.ExecuteNonQuery();

          syncInOut = Convert.ToInt32(pInOut.Value);
          syncOut = Convert.ToInt32(pOut.Value);
        }
      }

      // Async path
      using (var connection = new MySqlConnection(connectionString))
      {
        await connection.OpenAsync();
        using (var command = new MySqlCommand("wl17260_out_parity", connection))
        {
          command.CommandType = CommandType.StoredProcedure;

          var pIn = command.Parameters.Add("@p_in", MySqlDbType.Int32);
          pIn.Direction = ParameterDirection.Input;
          pIn.Value = 7;

          var pInOut = command.Parameters.Add("@p_inout", MySqlDbType.Int32);
          pInOut.Direction = ParameterDirection.InputOutput;
          pInOut.Value = 3;

          var pOut = command.Parameters.Add("@p_out", MySqlDbType.Int32);
          pOut.Direction = ParameterDirection.Output;

          await command.ExecuteNonQueryAsync();

          asyncInOut = Convert.ToInt32(pInOut.Value);
          asyncOut = Convert.ToInt32(pOut.Value);
        }
      }

      // Assert parity
      Assert.That(syncInOut, Is.EqualTo(asyncInOut));
      Assert.That(syncOut, Is.EqualTo(asyncOut));

      Assert.That(syncInOut, Is.EqualTo(10));
      Assert.That(syncOut, Is.EqualTo(70));
    }

    /// <summary>
    /// Checks parity among sync and async readers.
    /// </summary>
    [Test]
    public async Task ExecuteReaderMultiResultParity()
    {
      var connectionString = Connection.ConnectionString;
      const string sql = @"SELECT 1 AS a, 'first' AS tag; SELECT 2 AS b, 'second' AS tag;";

      ReaderParityResult syncResult;
      ReaderParityResult asyncResult;

      // Sync path
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        using (var cmd = new MySqlCommand(sql, connection))
        using (DbDataReader reader = cmd.ExecuteReader())
        {
          syncResult = ReadAllResults(reader);
        }
      }

      // Async path
      using (var connection = new MySqlConnection(connectionString))
      {
        await connection.OpenAsync();
        using (var cmd = new MySqlCommand(sql, connection))
        using (DbDataReader reader = await cmd.ExecuteReaderAsync())
        {
          asyncResult = await ReadAllResultsAsync(reader);
        }
      }

      // Assert same number of result sets
      Assert.That(syncResult.ResultSets, Is.EqualTo(2));
      Assert.That(asyncResult.ResultSets, Is.EqualTo(2));

      // Assert same row materialization sequence
      CollectionAssert.AreEqual(syncResult.Rows, asyncResult.Rows);

      // Explicit expectation
      CollectionAssert.AreEqual(new[] { "rs1:1|first", "rs2:2|second" }, syncResult.Rows);
    }

    private sealed class ReaderParityResult
    {
      public List<string> Rows { get; set; }
      public int ResultSets { get; set; }
    }

    /// <summary>
    /// Helper method to reads results from a data reader.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <returns>The read results.</returns>
    private static ReaderParityResult ReadAllResults(DbDataReader reader)
    {
      var rows = new List<string>();
      int rowCount = 0;

      do
      {
        rowCount++;
        while (reader.Read())
        {
          var c0 = reader.GetValue(0)?.ToString();
          var c1 = reader.GetValue(1)?.ToString();
          rows.Add("rs" + rowCount + ":" + c0 + "|" + c1);
        }
      } while (reader.NextResult());

      return new ReaderParityResult { Rows = rows, ResultSets = rowCount };
    }

    /// <summary>
    /// Helper method to read results from a data reader asynchronously.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <returns>The read results.</returns>
    private static async Task<ReaderParityResult> ReadAllResultsAsync(DbDataReader reader)
    {
      var rows = new List<string>();
      int rowCount = 0;

      do
      {
        rowCount++;
        while (await reader.ReadAsync())
        {
          var c0 = reader.GetValue(0)?.ToString();
          var c1 = reader.GetValue(1)?.ToString();
          rows.Add("rs" + rowCount + ":" + c0 + "|" + c1);
        }
      } while (await reader.NextResultAsync());

      return new ReaderParityResult { Rows = rows, ResultSets = rowCount };
    }

    /// <summary>
    /// Checks parity among sync and async prepared statements.
    /// </summary>
    [Test]
    public async Task PreparedStatementReusedParity()
    {
      ExecuteSQL("DROP TABLE IF EXISTS wl17260_ps_reuse;");
      ExecuteSQL(@"
      CREATE TABLE wl17260_ps_reuse(
        id int PRIMARY KEY,
        name varchar(50) NOT NULL,
        score int NOT NULL
      );");

      var connectionString = Connection.ConnectionString;
      int syncAffectedTotal = 0;
      int asyncAffectedTotal = 0;

      // Sync path
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        using (var command = new MySqlCommand("INSERT INTO wl17260_ps_reuse(id, name, score) VALUES(@id, @name, @score)", connection))
        {
          command.Parameters.Add("@id", MySqlDbType.Int32);
          command.Parameters.Add("@name", MySqlDbType.VarChar);
          command.Parameters.Add("@score", MySqlDbType.Int32);

          command.Prepare();

          // First execution
          command.Parameters["@id"].Value = 1;
          command.Parameters["@name"].Value = "sync-1";
          command.Parameters["@score"].Value = 10;
          syncAffectedTotal += command.ExecuteNonQuery();

          // Second execution (same prepared statement, different params)
          command.Parameters["@id"].Value = 2;
          command.Parameters["@name"].Value = "sync-2";
          command.Parameters["@score"].Value = 20;
          syncAffectedTotal += command.ExecuteNonQuery();
        }
      }

      // Async path
      using (var connection = new MySqlConnection(connectionString))
      {
        await connection.OpenAsync();
        using (var command = new MySqlCommand("INSERT INTO wl17260_ps_reuse(id, name, score) VALUES(@id, @name, @score)", connection))
        {
          command.Parameters.Add("@id", MySqlDbType.Int32);
          command.Parameters.Add("@name", MySqlDbType.VarChar);
          command.Parameters.Add("@score", MySqlDbType.Int32);

          await command.PrepareAsync();

          // First execution
          command.Parameters["@id"].Value = 3;
          command.Parameters["@name"].Value = "async-1";
          command.Parameters["@score"].Value = 30;
          asyncAffectedTotal += await command.ExecuteNonQueryAsync();

          // Second execution (same prepared statement, different params)
          command.Parameters["@id"].Value = 4;
          command.Parameters["@name"].Value = "async-2";
          command.Parameters["@score"].Value = 40;
          asyncAffectedTotal += await command.ExecuteNonQueryAsync();
        }
      }

      // Assert parity at API level
      Assert.That(syncAffectedTotal, Is.EqualTo(2));
      Assert.That(asyncAffectedTotal, Is.EqualTo(2));

      // Assert resulting DB state
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        using (var countCommand = new MySqlCommand("SELECT COUNT(*) FROM wl17260_ps_reuse", connection))
        {
          int count = Convert.ToInt32(countCommand.ExecuteScalar());
          Assert.That(count, Is.EqualTo(4));
        }

        using (var sumCommand = new MySqlCommand("SELECT SUM(score) FROM wl17260_ps_reuse", connection))
        {
          int sum = Convert.ToInt32(sumCommand.ExecuteScalar());
          Assert.That(sum, Is.EqualTo(10 + 20 + 30 + 40));
        }

        using (var minMaxCommand = new MySqlCommand("SELECT MIN(id), max(id) FROM wl17260_ps_reuse", connection))
        using (var reader = minMaxCommand.ExecuteReader())
        {
          Assert.That(reader.Read(), Is.True);
          Assert.That(Convert.ToInt32(reader.GetValue(0)), Is.EqualTo(1));
          Assert.That(Convert.ToInt32(reader.GetValue(1)), Is.EqualTo(4));
        }
      }
    }

    /// <summary>
    /// Checks parity among sync and async execution of BeginTransaction with default behavior.
    /// </summary>
    [Test]
    public async Task BeginTransactionDefaultIsolationParity()
    {
      var connectionString = Connection.ConnectionString;
      string syncIsolation;
      string asyncIsolation;

      // Sync path
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        using (var tx = connection.BeginTransaction())
        {
          syncIsolation = GetIsolationLevel(connection);
          tx.Rollback();
        }
      }

      // Async path
      using (var connection = new MySqlConnection(connectionString))
      {
        await connection.OpenAsync();
        using (var tx = await connection.BeginTransactionAsync())
        {
          asyncIsolation = await GetIsolationLevelAsync(connection);
          await tx.RollbackAsync();
        }
      }

      // Assert parity
      Assert.That(syncIsolation, Is.EqualTo(asyncIsolation), "Sync/async default BeginTransaction isolation mismatch.");

      // Assert expected default for this codebase behavior is REPEATABLE READ since session was not affected.
      var normalized = NormalizeIsolation(syncIsolation);
      Assert.That(normalized, Is.EqualTo("REPEATABLE READ"), "Unexpected default isolation level for BeginTransaction().");
    }

    /// <summary>
    /// Helper method to get the current session isolation level.
    /// </summary>
    /// <param name="connection">The connection object.</param>
    /// <returns>A string representing the isolation level.</returns>
    private static string GetIsolationLevel(MySqlConnection connection)
    {
      using (var cmd = new MySqlCommand("SELECT @@transaction_isolation", connection))
      {
        var value = cmd.ExecuteScalar();
        return value == null ? string.Empty : value.ToString();
      }
    }

    /// <summary>
    /// Async helper method to get the current session isolation level.
    /// </summary>
    /// <param name="connection">The connection object.</param>
    /// <returns>A task containing the string that represent the isolation level.</returns>
    private static async Task<string> GetIsolationLevelAsync(MySqlConnection connection)
    {
      using (var cmd = new MySqlCommand("SELECT @@transaction_isolation", connection))
      {
        var value = await cmd.ExecuteScalarAsync();
        return value == null ? string.Empty : value.ToString();
      }
    }

    /// <summary>
    /// Helper method used to normalize an isolation level.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>The normalized value.</returns>
    private static string NormalizeIsolation(string value)
    {
      if (value == null) 
        return string.Empty;
      
      return value.Replace("-", " ").Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Checks parity among sync and async handling of data readers.
    /// </summary>
    [Test]
    public async Task ReaderLifecycleParityPartialReadThenClose_AllowsNextCommand()
    {
      var connectionString = Connection.ConnectionString;
      const string multiRowSql = @"
    SELECT 1 AS v UNION ALL SELECT 2 UNION ALL SELECT 3;
  ";

      int syncNextCommandResult;
      int asyncNextCommandResult;

      // Sync path: partial read + close + next command
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();

        using (var command = new MySqlCommand(multiRowSql, connection))
        using (DbDataReader reader = command.ExecuteReader())
        {
          // Partial read only first row
          Assert.That(reader.Read(), Is.True);
          Assert.That(Convert.ToInt32(reader.GetValue(0)), Is.EqualTo(1));

          // Close before consuming remaining rows
          reader.Close();
        }

        // Must still be usable
        using (var verify = new MySqlCommand("SELECT 42", connection))
        {
          syncNextCommandResult = Convert.ToInt32(verify.ExecuteScalar());
        }
      }

      // Async path: partial read + close + next command
      using (var conn = new MySqlConnection(connectionString))
      {
        await conn.OpenAsync();

        using (var cmd = new MySqlCommand(multiRowSql, conn))
        using (DbDataReader reader = await cmd.ExecuteReaderAsync())
        {
          // Partial read only first row
          Assert.That(await reader.ReadAsync(), Is.True);
          Assert.That(Convert.ToInt32(reader.GetValue(0)), Is.EqualTo(1));

          // Close before consuming remaining rows
          reader.Close();
        }

        // Must still be usable
        using (var verify = new MySqlCommand("SELECT 42", conn))
        {
          asyncNextCommandResult = Convert.ToInt32(await verify.ExecuteScalarAsync());
        }
      }

      // Assert parity
      Assert.That(syncNextCommandResult, Is.EqualTo(42));
      Assert.That(asyncNextCommandResult, Is.EqualTo(42));
    }

    /// <summary>
    /// Checks parity among sync and async handling of null parameters in prepared statements.
    /// </summary>
    [Test]
    public async Task PreparedStatementNullMapParity()
    {
      ExecuteSQL("DROP TABLE IF EXISTS wl17260_nullmap_parity;");
      ExecuteSQL(@"
    CREATE TABLE wl17260_nullmap_parity(
      id int PRIMARY KEY,
      name varchar(50) NULL,
      score int NULL
    );");

      var connectionString = Connection.ConnectionString;
      int syncAffected = 0;
      int asyncAffected = 0;

      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        using (var command = new MySqlCommand("INSERT INTO wl17260_nullmap_parity(id, name, score) VALUES(@id, @name, @score)", connection))
        {
          command.Parameters.Add("@id", MySqlDbType.Int32);
          command.Parameters.Add("@name", MySqlDbType.VarChar);
          command.Parameters.Add("@score", MySqlDbType.Int32);
          command.Prepare();

          command.Parameters["@id"].Value = 1;
          command.Parameters["@name"].Value = DBNull.Value;
          command.Parameters["@score"].Value = 10;
          syncAffected += command.ExecuteNonQuery();

          command.Parameters["@id"].Value = 2;
          command.Parameters["@name"].Value = "sync";
          command.Parameters["@score"].Value = DBNull.Value;
          syncAffected += command.ExecuteNonQuery();
        }
      }

      using (var connection = new MySqlConnection(connectionString))
      {
        await connection.OpenAsync();
        using (var command = new MySqlCommand("INSERT INTO wl17260_nullmap_parity(id, name, score) VALUES(@id, @name, @score)", connection))
        {
          command.Parameters.Add("@id", MySqlDbType.Int32);
          command.Parameters.Add("@name", MySqlDbType.VarChar);
          command.Parameters.Add("@score", MySqlDbType.Int32);
          await command.PrepareAsync();

          command.Parameters["@id"].Value = 3;
          command.Parameters["@name"].Value = DBNull.Value;
          command.Parameters["@score"].Value = 30;
          asyncAffected += await command.ExecuteNonQueryAsync();

          command.Parameters["@id"].Value = 4;
          command.Parameters["@name"].Value = "async";
          command.Parameters["@score"].Value = DBNull.Value;
          asyncAffected += await command.ExecuteNonQueryAsync();
        }
      }

      Assert.That(syncAffected, Is.EqualTo(2));
      Assert.That(asyncAffected, Is.EqualTo(2));

      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();

        using (var countCommand = new MySqlCommand("SELECT COUNT(*) FROM wl17260_nullmap_parity", connection))
          Assert.That(Convert.ToInt32(countCommand.ExecuteScalar()), Is.EqualTo(4));

        using (var nullNameCommand = new MySqlCommand("SELECT COUNT(*) FROM wl17260_nullmap_parity WHERE name is NULL", connection))
          Assert.That(Convert.ToInt32(nullNameCommand.ExecuteScalar()), Is.EqualTo(2));

        using (var nullScoreCommand = new MySqlCommand("SELECT COUNT(*) FROM wl17260_nullmap_parity WHERE score is NULL", connection))
          Assert.That(Convert.ToInt32(nullScoreCommand.ExecuteScalar()), Is.EqualTo(2));

        using (var sumScoreCommand = new MySqlCommand("SELECT SUM(COALESCE(score,0)) FROM wl17260_nullmap_parity", connection))
          Assert.That(Convert.ToInt32(sumScoreCommand.ExecuteScalar()), Is.EqualTo(40));
      }
    }

    /// <summary>
    /// Checks parity among sync and async handling of the CommandBehavior enumeration.
    /// </summary>
    [Test]
    public async Task CommandBehaviorSchemaOnlyAndSingleRowParity()
    {
      ExecuteSQL("DROP TABLE IF EXISTS wl17260_cb_parity;");
      ExecuteSQL(@"
    create table wl17260_cb_parity(
      id int PRIMARY KEY,
      name varchar(50) NOT NULL,
      score int NULL
    );");
      ExecuteSQL("INSERT INTO wl17260_cb_parity VALUES (1,'a',10),(2,'b',20),(3,'c',30);");

      var connectionString = Connection.ConnectionString;
      int syncSchemaColumnCount;
      int asyncSchemaColumnCount;
      int syncSingleRowCount;
      int asyncSingleRowCount;

      // Sync path
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();

        // SchemaOnly
        using (var command = new MySqlCommand("select id, name, score from wl17260_cb_parity order by id", connection))
        using (DbDataReader reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
        {
          syncSchemaColumnCount = reader.FieldCount;
        }

        // Ensure connection still usable after behavior path
        using (var sanity = new MySqlCommand("select 1", connection))
          Assert.That(Convert.ToInt32(sanity.ExecuteScalar()), Is.EqualTo(1));

        // SingleRow
        using (var command = new MySqlCommand("select id, name, score from wl17260_cb_parity order by id", connection))
        using (DbDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
        {
          syncSingleRowCount = 0;
          while (reader.Read())
            syncSingleRowCount++;
        }
      }

      // Async path
      using (var connection = new MySqlConnection(connectionString))
      {
        await connection.OpenAsync();

        // SchemaOnly
        using (var command = new MySqlCommand("select id, name, score from wl17260_cb_parity order by id", connection))
        using (DbDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly))
        {
          asyncSchemaColumnCount = reader.FieldCount;
        }

        // Ensure connection still usable after behavior path
        using (var sanity = new MySqlCommand("select 1", connection))
          Assert.That(Convert.ToInt32(await sanity.ExecuteScalarAsync()), Is.EqualTo(1));

        // SingleRow
        using (var command = new MySqlCommand("select id, name, score from wl17260_cb_parity order by id", connection))
        using (DbDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
        {
          asyncSingleRowCount = 0;
          while (await reader.ReadAsync())
            asyncSingleRowCount++;
        }
      }

      // Assert parity
      Assert.That(syncSchemaColumnCount, Is.EqualTo(asyncSchemaColumnCount));
      Assert.That(syncSchemaColumnCount, Is.EqualTo(3));

      Assert.That(syncSingleRowCount, Is.EqualTo(asyncSingleRowCount));
      Assert.That(syncSingleRowCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Checks parity among sync and async basic pooling.
    /// </summary>
    [Test]
    public async Task PoolConcurrentAcquireReleaseParity()
    {
      var builder = new MySqlConnectionStringBuilder(Connection.ConnectionString)
      {
        Pooling = true,
        MinimumPoolSize = 0,
        MaximumPoolSize = 2,
        ConnectionTimeout = 30
      };
      var connectionString = builder.ConnectionString;
      const int workers = 12;

      // Sync workers (run in parallel tasks)
      var syncTasks = new List<Task>();
      for (int i = 0; i < workers; i++)
      {
        syncTasks.Add(Task.Run(() =>
        {
          using (var connection = new MySqlConnection(connectionString))
          {
            connection.Open();
            using (var command = new MySqlCommand("SELECT 1", connection))
            {
              int v = Convert.ToInt32(command.ExecuteScalar());
              if (v != 1) throw new Exception("Unexpected scalar value in sync path.");
            }
          }
        }));
      }
      await Task.WhenAll(syncTasks);

      // Async workers
      var asyncTasks = new List<Task>();
      for (int i = 0; i < workers; i++)
      {
        asyncTasks.Add(Task.Run(async () =>
        {
          using (var connection = new MySqlConnection(connectionString))
          {
            await connection.OpenAsync();
            using (var command = new MySqlCommand("SELECT 1", connection))
            {
              int v = Convert.ToInt32(await command.ExecuteScalarAsync());
              if (v != 1) throw new Exception("Unexpected scalar value in async path.");
            }
          }
        }));
      }
      await Task.WhenAll(asyncTasks);

      // Final sanity, ensure pool remains usable
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        using (var cmd = new MySqlCommand("SELECT 1", connection))
          Assert.That(Convert.ToInt32(cmd.ExecuteScalar()), Is.EqualTo(1));
      }
    }

    /// <summary>
    /// Checks parity among sync and async pool clearing.
    /// </summary>
    [Test]
    public async Task ClearPoolWhileActiveConnectionParity()
    {
      var builder = new MySqlConnectionStringBuilder(Connection.ConnectionString)
      {
        Pooling = true,
        MinimumPoolSize = 0,
        MaximumPoolSize = 5,
        ConnectionTimeout = 30
      };
      var connectionString = builder.ConnectionString;

      // Sync path
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        using (var command = new MySqlCommand("SELECT 1", connection))
          Assert.That(Convert.ToInt32(command.ExecuteScalar()), Is.EqualTo(1));

        // Clear while one connection is active
        MySqlConnection.ClearPool(connection);

        // Held connection still usable until closed
        using (var command2 = new MySqlCommand("SELECT 1", connection))
          Assert.That(Convert.ToInt32(command2.ExecuteScalar()), Is.EqualTo(1));
      }

      // New connections should still work
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        using (var command = new MySqlCommand("SELECT 1", connection))
          Assert.That(Convert.ToInt32(command.ExecuteScalar()), Is.EqualTo(1));
      }

      // Async path
      using (var connection = new MySqlConnection(connectionString))
      {
        await connection.OpenAsync();
        using (var command = new MySqlCommand("SELECT 1", connection))
          Assert.That(Convert.ToInt32(await command.ExecuteScalarAsync()), Is.EqualTo(1));

        await connection.ClearPoolAsync(connection);

        using (var command2 = new MySqlCommand("SELECT 1", connection))
          Assert.That(Convert.ToInt32(await command2.ExecuteScalarAsync()), Is.EqualTo(1));
      }

      using (var connection = new MySqlConnection(connectionString))
      {
        await connection.OpenAsync();
        using (var command = new MySqlCommand("SELECT 1", connection))
          Assert.That(Convert.ToInt32(await command.ExecuteScalarAsync()), Is.EqualTo(1));
      }
    }

    /// <summary>
    /// Checks parity among sync and async global pool clearing.
    /// </summary>
    [Test]
    public async Task ClearAllPoolsParity()
    {
      var builder = new MySqlConnectionStringBuilder(Connection.ConnectionString)
      {
        Pooling = true,
        MinimumPoolSize = 0,
        MaximumPoolSize = 5,
        ConnectionTimeout = 30
      };
      var connectionString = builder.ConnectionString;

      // Warm up pool with sync open/close
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        using (var command = new MySqlCommand("SELECT 1", connection))
          Assert.That(Convert.ToInt32(command.ExecuteScalar()), Is.EqualTo(1));
      }

      // Clear all pools synchronously
      MySqlConnection.ClearAllPools();

      // Verify new connection still works after sync clear
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        using (var command = new MySqlCommand("SELECT 1", connection))
          Assert.That(Convert.ToInt32(command.ExecuteScalar()), Is.EqualTo(1));
      }

      // Warm up again with async open/close
      using (var connection = new MySqlConnection(connectionString))
      {
        await connection.OpenAsync();
        using (var command = new MySqlCommand("SELECT 1", connection))
          Assert.That(Convert.ToInt32(await command.ExecuteScalarAsync()), Is.EqualTo(1));
      }

      // Clear all pools asynchronously
      using (var helperConnection = new MySqlConnection(connectionString))
      {
        await helperConnection.ClearAllPoolsAsync();
      }

      // Verify new connection still works after async clear
      using (var connection = new MySqlConnection(connectionString))
      {
        await connection.OpenAsync();
        using (var command = new MySqlCommand("SELECT 1", connection))
          Assert.That(Convert.ToInt32(await command.ExecuteScalarAsync()), Is.EqualTo(1));
      }
    }
  }
}
