// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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


using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MySql.Data.EntityFrameworkCore.Design.Internal
{
  internal class MySQLDatabaseModelFactory : IInternalDatabaseModelFactory
  {
    public virtual ILogger Logger { get; }

    MySqlConnection _connection;
    TableSelectionSet _tableSelectionSet;
    DatabaseModel _databaseModel;
    Dictionary<string, TableModel> _tables;
    Dictionary<string, ColumnModel> _tableColumns;
    static string TableKey(TableModel table) => TableKey(table.Name, table.SchemaName);
    static string TableKey(string name, string schema) => $"\"{schema}\".\"{name}\"";
    static string ColumnKey(TableModel table, string columnName) => TableKey(table) + ".[" + columnName + "]";
    string _schemaList = "''";

    public MySQLDatabaseModelFactory(ILogger<MySQLDatabaseModelFactory> logger)
    {
      ThrowIf.Argument.IsNull(logger, "logger");

      Logger = logger;
    }

    private void ResetState()
    {
      _connection = null;
      _tableSelectionSet = null;
      _databaseModel = new DatabaseModel();
      _tables = new Dictionary<string, TableModel>(StringComparer.OrdinalIgnoreCase);
      _tableColumns = new Dictionary<string, ColumnModel>(StringComparer.OrdinalIgnoreCase);
    }


    public DatabaseModel Create(string connectionString, TableSelectionSet tableSelectionSet)
    {
      if (String.IsNullOrEmpty(connectionString))
        new ArgumentException("Argument is empty", "connectionString");

      if (tableSelectionSet == null)
        new ArgumentNullException("tableSelectionSet");

      using (var connection = new MySqlConnection(connectionString))
      {
        return Create(connection, tableSelectionSet);
      }
    }

    public DatabaseModel Create(DbConnection connection, TableSelectionSet tableSelectionSet)
    {
      ResetState();

      _connection = (MySqlConnection)connection;

      var connectionStartedOpen = _connection.State == ConnectionState.Open;
      if (!connectionStartedOpen)
      {
        _connection.Open();
      }

      try
      {
        _tableSelectionSet = tableSelectionSet;
        if (tableSelectionSet.Schemas.Count == 0)
          _schemaList = $"'{_connection.Database}'";
        else
          _schemaList = tableSelectionSet.Schemas.Select(c => $"'{c.Text}'").Join(", ");
        _databaseModel.DatabaseName = _connection.Database;
        GetTables();
        GetColumns();
        GetIndexes();
        GetForeignKeys();
        return _databaseModel;
      }
      finally
      {
        if (!connectionStartedOpen)
        {
          _connection.Close();
        }
      }
    }

    private void GetForeignKeys()
    {
      var command = _connection.CreateCommand();
      var dbName = _connection.Database;
      command.CommandText = $@"SELECT 
  kc.constraint_name, 
  kc.table_schema, 
  kc.table_name, 
  kc.column_name, 
  kc.referenced_table_schema, 
  kc.referenced_table_name, 
  kc.referenced_column_name, 
  kc.ordinal_position, 
  rc.update_rule, 
  rc.delete_rule 
FROM information_schema.key_column_usage as kc 
INNER JOIN information_schema.referential_constraints as rc 
ON kc.constraint_catalog = rc.constraint_catalog 
  AND kc.constraint_schema = rc.constraint_schema 
  AND kc.constraint_name = rc.constraint_name 
WHERE kc.referenced_table_name IS NOT NULL 
  AND kc.table_schema IN ({_schemaList})
  AND kc.table_name <> '{HistoryRepository.DefaultTableName}'";

      using (var reader = command.ExecuteReader())
      {
        while (reader.Read())
        {
          ForeignKeyModel fkModel = null;
          var tableSchema = reader.GetValueOrDefault<string>("table_schema");
          var constraintName = reader.GetValueOrDefault<string>("constraint_name");
          var tableName = reader.GetValueOrDefault<string>("table_name");
          var columnName = reader.GetValueOrDefault<string>("column_name");
          var referencedTableSchema = reader.GetValueOrDefault<string>("referenced_table_schema");
          var referencedTableName = reader.GetValueOrDefault<string>("referenced_table_name");
          var referencedColumnName = reader.GetValueOrDefault<string>("referenced_column_name");
          var updateRule = reader.GetValueOrDefault<string>("update_rule");
          var deleteRule = reader.GetValueOrDefault<string>("delete_rule");
          var ordinal = reader.GetInt32("ordinal_position");

          if (string.IsNullOrEmpty(constraintName))
          {
            Logger.LogWarning("Foreign key must be named warning", tableSchema, tableName);
            continue;
          }

          if (!_tableSelectionSet.Allows(tableSchema, tableName))
          {
            Logger.LogDebug("Foreign key column skipped", new string[] { referencedColumnName, constraintName, tableSchema, tableName });
            continue;
          }
          var table = _tables[TableKey(tableName, tableSchema)];

          TableModel principalTable = null;
          if (!string.IsNullOrEmpty(tableSchema)
              && !string.IsNullOrEmpty(referencedTableName))
          {
            _tables.TryGetValue(TableKey(referencedTableName, tableSchema), out principalTable);
          }

          if (principalTable == null)
          {
            Logger.LogDebug("Foreign key references missing table", new string[] { constraintName, tableName, tableSchema });
          }


          fkModel = new ForeignKeyModel
          {
            Name = constraintName,
            Table = table,
            PrincipalTable = principalTable,
            OnDelete = ConvertToReferentialAction(deleteRule)
          };

          var fkColumn = new ForeignKeyColumnModel
          {
            Ordinal = (int)ordinal
          };

          ColumnModel fromColumn = FindColumnForForeignKey(columnName, fkModel.Table, constraintName);
          if (fromColumn != null)
          {
            fkColumn.Column = fromColumn;
          }

          if (fkModel.PrincipalTable != null)
          {
            ColumnModel toColumn = FindColumnForForeignKey(referencedColumnName, fkModel.PrincipalTable, constraintName);
            if (toColumn != null)
            {
              fkColumn.PrincipalColumn = toColumn;
            }
          }
          fkModel.Columns.Add(fkColumn);
          table.ForeignKeys.Add(fkModel);
        }
      }
    }

    private ColumnModel FindColumnForForeignKey(string columnName, TableModel table, string constraintName)
    {
      ColumnModel column;
      if (string.IsNullOrEmpty(columnName))
      {
        Logger.LogWarning("Column name empty on foreign key", new string[] { table.SchemaName, table.Name, constraintName });
        return null;
      }

      if (!_tableColumns.TryGetValue(
          ColumnKey(table, columnName), out column))
      {
        Logger.LogWarning("Foreign key columns were not mapped", new string[] { constraintName, columnName, table.SchemaName, table.Name });
        return null;
      }

      return column;
    }

    private static ReferentialAction? ConvertToReferentialAction(string deleteAction)
    {
      switch (deleteAction.ToUpperInvariant())
      {
        case "RESTRICT":
          return ReferentialAction.Restrict;

        case "CASCADE":
          return ReferentialAction.Cascade;

        case "SET_NULL":
          return ReferentialAction.SetNull;

        case "SET_DEFAULT":
          return ReferentialAction.SetDefault;

        case "NO_ACTION":
          return ReferentialAction.NoAction;

        default:
          return null;
      }
    }

    void GetIndexes()
    {
      var command = _connection.CreateCommand();
      var dbName = _connection.Database;
      command.CommandText = $@"SELECT DISTINCT 
  s.table_schema, 
  s.table_name, 
  s.non_unique, 
  s.index_name, 
  s.column_name, 
  s.seq_in_index, 
  t.constraint_type 
FROM information_schema.statistics s 
LEFT OUTER JOIN information_schema.table_constraints t 
ON t.table_schema=s.table_schema 
  AND t.table_name=s.table_name 
  AND s.index_name=t.constraint_name 
WHERE s.table_schema IN ({_schemaList}) 
  AND s.table_name <> '{HistoryRepository.DefaultTableName}'
ORDER BY s.table_schema, s.table_name, s.non_unique, s.index_name, s.seq_in_index";

      using (var reader = command.ExecuteReader())
      {
        IndexModel index = null;
        while (reader.Read())
        {
          var tableSchema = reader.GetValueOrDefault<string>("table_schema");
          var tableName = reader.GetValueOrDefault<string>("table_name");
          var indexName = reader.GetValueOrDefault<string>("index_name");
          var isUnique = reader.GetInt32("non_unique") == 0 ? true : false;
          var columnName = reader.GetValueOrDefault<string>("column_name");
          var indexOrdinal = reader.GetInt32("seq_in_index");

          if (!_tableSelectionSet.Allows(tableSchema, tableName))
          {
            Logger.LogDebug("Index column skipped", new string[] { columnName, indexName, tableSchema, tableName });
            continue;
          }

          if (string.IsNullOrEmpty(indexName))
          {
            Logger.LogWarning("Index unnamed", new string[] { tableSchema, tableName });
            continue;
          }

          if (index == null || index.Name != indexName || index.Table.Name != tableName || index.Table.SchemaName != tableSchema)
          {
            TableModel table = null;
            if (!_tables.TryGetValue(TableKey(tableName, tableSchema), out table))
            {
              Logger.LogWarning("Index table missing", new string[] { indexName, tableSchema, tableName });
              continue;
            }

            index = new IndexModel
            {
              Table = table,
              Name = indexName,
              IsUnique = isUnique
            };
            table.Indexes.Add(index);
          }


          ColumnModel column;
          if (string.IsNullOrEmpty(columnName))
          {
            Logger.LogWarning("Index column must be named", new string[] { tableSchema, tableName, indexName });
          }
          else if (!_tableColumns.TryGetValue(ColumnKey(index.Table, columnName), out column))
          {
            Logger.LogWarning("Index columns not mapped", new string[] { indexName, columnName, tableSchema, tableName });
          }
          else
          {
            var indexColumn = new IndexColumnModel
            {
              Index = index,
              Column = column,
              Ordinal = (int)indexOrdinal
            };
            index.IndexColumns.Add(indexColumn);
          }
        }
      }
    }

    void GetTables()
    {
      var command = _connection.CreateCommand();
      var dbName = _connection.Database;
      command.CommandText = $@"SELECT * 
FROM information_schema.tables 
WHERE table_schema IN ({_schemaList}) 
  AND table_type LIKE '%BASE%' 
  AND table_name <> '{HistoryRepository.DefaultTableName}'
ORDER BY table_schema, table_name";

      using (var reader = command.ExecuteReader())
      {
        while (reader.Read())
        {
          var table = new TableModel
          {
            SchemaName = reader.GetValueOrDefault<string>("table_schema"),
            Name = reader.GetValueOrDefault<string>("table_name")
          };

          if (_tableSelectionSet.Allows(table.SchemaName, table.Name))
          {
            _databaseModel.Tables.Add(table);
            _tables[TableKey(table)] = table;
            Logger.LogDebug(RelationalDesignEventId.FoundTable, () => $"Found table: {table.SchemaName}.{table.Name}");
          }
          else
          {
            Logger.LogDebug(RelationalDesignEventId.TableSkipped, () => $"Skipped table: {table.SchemaName}.{table.Name}");
          }

        }
      }
    }

    void GetColumns()
    {
      var command = _connection.CreateCommand();
      var dbName = _connection.Database;
      string generation_expression = string.Empty;
      if (_connection.driver.Version.isAtLeast(5, 7, 0))
        generation_expression = "generation_expression";
      else
        generation_expression = "NULL";
      command.CommandText = $@"SELECT 
  c.table_schema, 
  c.table_name, 
  c.column_name, 
  is_nullable, 
  column_type, 
  column_key, 
  c.ordinal_position, 
  column_default, 
  {generation_expression} as generation_expression, 
  numeric_precision, 
  numeric_scale, 
  character_maximum_length, 
  constraint_name, 
  k.ordinal_position as primarykeyordinal 
FROM (information_schema.tables t 
  INNER JOIN information_schema.columns c 
  ON t.table_schema = c.table_schema 
    AND t.table_name = c.table_name) 
LEFT JOIN information_schema.key_column_usage k 
ON (k.TABLE_SCHEMA = c.TABLE_SCHEMA 
  AND k.TABLE_NAME = c.TABLE_NAME 
  AND k.COLUMN_NAME = c.COLUMN_NAME 
  AND k.CONSTRAINT_NAME = 'PRIMARY') 
WHERE t.table_type = 'BASE TABLE' 
  AND t.table_schema IN ({_schemaList}) 
ORDER BY c.table_name, 
  c.ordinal_position;";

      using (var reader = command.ExecuteReader())
      {
        while (reader.Read())
        {
          var tableSchema = reader.GetValueOrDefault<string>("table_schema");
          var tableName = reader.GetValueOrDefault<string>("table_name");
          var columnName = reader.GetValueOrDefault<string>("column_name");

          if (!_tableSelectionSet.Allows(tableSchema, tableName))
          {
            Logger.LogDebug(RelationalDesignEventId.ColumnSkipped, () => $"Skipped column: {tableSchema}.{tableName}.{columnName}");
            continue;
          }

          var dataTypeName = reader.GetValueOrDefault<string>("column_type");
          var ordinalPosition = reader.GetInt32("ordinal_position");
          var isNullable = reader.GetValueOrDefault<string>("is_nullable").Contains("YES") ? true : false;
          var columnKey = reader.GetValueOrDefault<string>("column_key").Contains("YES") ? true : false; ;
          var defaultValue = reader.GetValueOrDefault<string>("column_default");
          var computedValue = reader.GetValueOrDefault<string>("generation_expression");
          var numeric_Scale = reader.GetValueOrDefault<UInt64?>("numeric_scale");
          var maxLength = reader.IsDBNull(reader.GetOrdinal("character_maximum_length")) ? null : (UInt64?)reader.GetUInt64("character_maximum_length");
          var precision = reader.IsDBNull(reader.GetOrdinal("numeric_precision")) ? null : (UInt64?)reader.GetUInt64("numeric_precision");
          var primaryKeyOrdinal = reader.IsDBNull(reader.GetOrdinal("primarykeyordinal")) ? null : (Int32?)reader.GetInt32("primarykeyordinal");

          var table = _tables[TableKey(tableName, tableSchema)];

          var column = new ColumnModel
          {
            Table = table,
            Name = columnName,
            DataType = dataTypeName,
            Ordinal = (int)ordinalPosition - 1,
            IsNullable = isNullable,
            MaxLength = (int?)maxLength,
            Precision = (int?)precision,
            Scale = (int?)numeric_Scale,
            DefaultValue = string.IsNullOrWhiteSpace(defaultValue) ? null : defaultValue,
            PrimaryKeyOrdinal = (int?)primaryKeyOrdinal,
            ComputedValue = string.IsNullOrWhiteSpace(computedValue) ? null : computedValue
          };

          table.Columns.Add(column);
          Logger.LogDebug(RelationalDesignEventId.FoundColumn, () => $"Found column: {tableSchema}.{tableName}.{columnName}");
          if (!_tableColumns.ContainsKey(ColumnKey(table, column.Name)))
            _tableColumns.Add(ColumnKey(table, column.Name), column);
        }
      }
    }
  }
}
