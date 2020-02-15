// Copyright (c) 2017, 2020, Oracle and/or its affiliates. All rights reserved.
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
using System.Linq;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System.Data.Common;
using MySql.Data.MySqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using MySql.Data.EntityFrameworkCore.Utils;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MySql.Data.EntityFrameworkCore.Scaffolding.Internal
{
  internal class MySQLDatabaseModelFactory : DatabaseModelFactory
  {
    private readonly IDiagnosticsLogger<DbLoggerCategory.Scaffolding> _logger;

    public MySQLDatabaseModelFactory([NotNull] IDiagnosticsLogger<DbLoggerCategory.Scaffolding> logger)
    {
      Check.NotNull(logger, nameof(logger));

      _logger = logger;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override DatabaseModel Create(string connectionString, DatabaseModelFactoryOptions options)
    {
      Check.NotEmpty(connectionString, nameof(connectionString));
      Check.NotNull(options, nameof(options));

      using (var connection = new MySqlConnection(connectionString))
      {
        return Create(connection, options);
      }
    }

    public override DatabaseModel Create(DbConnection connection, DatabaseModelFactoryOptions options)
    {
      Check.NotNull(connection, nameof(connection));
      Check.NotNull(options, nameof(options));

      var databaseModel = new DatabaseModel();

      var connectionStartedOpen = connection.State == ConnectionState.Open;
      if (!connectionStartedOpen)
      {
        connection.Open();
      }

      try
      {
        databaseModel.DatabaseName = connection.Database;
        databaseModel.DefaultSchema = GetDefaultSchema(connection);

        var schemaList = Enumerable.Empty<string>().ToList();
        var tableList = options.Tables.ToList();
        var tableFilter = GenerateTableFilter(tableList, schemaList);

        var tables = GetTables(connection, tableFilter);
        foreach (var table in tables)
        {
          table.Database = databaseModel;
          databaseModel.Tables.Add(table);
        }

        return databaseModel;
      }
      finally
      {
        if (!connectionStartedOpen)
        {
          connection.Close();
        }
      }
    }

    private string GetDefaultSchema(DbConnection connection)
    {
      return null;
    }

    private static Func<string, string, bool> GenerateTableFilter(IReadOnlyList<string> tables, IReadOnlyList<string> schemas)
    {
      return tables.Count > 0 ? (s, t) => tables.Contains(t) : (Func<string, string, bool>)null;
    }

    private static ReferentialAction? ConvertToReferentialAction(string deleteAction)
    {
      switch (deleteAction.ToUpperInvariant())
      {
        case "RESTRICT":
          return ReferentialAction.Restrict;

        case "CASCADE":
          return ReferentialAction.Cascade;

        case "SET NULL":
          return ReferentialAction.SetNull;

        case "SET DEFAULT":
          return ReferentialAction.SetDefault;

        case "NO ACTION":
          return ReferentialAction.NoAction;

        default:
          return null;
      }
    }

    private const string GetTablesQuery = @"SELECT
    `TABLE_NAME`,
    `TABLE_TYPE`,
    IF(`TABLE_COMMENT` = 'VIEW' AND `TABLE_TYPE` = 'VIEW', '', `TABLE_COMMENT`) AS `TABLE_COMMENT` 
FROM
    `INFORMATION_SCHEMA`.`TABLES`
WHERE
    `TABLE_SCHEMA` = SCHEMA()
AND
    `TABLE_TYPE` IN ('BASE TABLE', 'VIEW');";

    private const string GetPrimaryQuery = @"SELECT `INDEX_NAME`,
     `NON_UNIQUE`,
     GROUP_CONCAT(`COLUMN_NAME` ORDER BY `SEQ_IN_INDEX` SEPARATOR ',') AS COLUMNS
     FROM `INFORMATION_SCHEMA`.`STATISTICS`
     WHERE `TABLE_SCHEMA` = '{0}'
     AND `TABLE_NAME` = '{1}'
     AND `INDEX_NAME` = 'PRIMARY'
     GROUP BY `INDEX_NAME`, `NON_UNIQUE`;";

    private const string GetColumnsQuery = @"SELECT
      `COLUMN_NAME`,
      `ORDINAL_POSITION`,
      `COLUMN_DEFAULT`,
      `IS_NULLABLE`,
      `DATA_TYPE`,
      `CHARACTER_SET_NAME`,
      `COLLATION_NAME`,
      `COLUMN_TYPE`,
      `COLUMN_COMMENT`,
      `EXTRA`
      FROM
      `INFORMATION_SCHEMA`.`COLUMNS`
      WHERE
      `TABLE_SCHEMA` = SCHEMA()
      AND
      `TABLE_NAME` = '{0}'
      ORDER BY
      `ORDINAL_POSITION`;";

    private const string GetIndexesQuery = @"SELECT `INDEX_NAME`,
      `NON_UNIQUE`,
      GROUP_CONCAT(`COLUMN_NAME` ORDER BY `SEQ_IN_INDEX` SEPARATOR ',') AS COLUMNS
      FROM `INFORMATION_SCHEMA`.`STATISTICS`
      WHERE `TABLE_SCHEMA` = '{0}'
      AND `TABLE_NAME` = '{1}'
      AND `INDEX_NAME` <> 'PRIMARY'
      GROUP BY `INDEX_NAME`, `NON_UNIQUE`;";

    private const string GetConstraintsQuery = @"SELECT
      `CONSTRAINT_NAME`,
      `TABLE_NAME`,
      `REFERENCED_TABLE_NAME`,
      GROUP_CONCAT(CONCAT_WS('|', `COLUMN_NAME`, `REFERENCED_COLUMN_NAME`) ORDER BY `ORDINAL_POSITION` SEPARATOR ',') AS PAIRED_COLUMNS,
      (SELECT `DELETE_RULE` FROM `INFORMATION_SCHEMA`.`REFERENTIAL_CONSTRAINTS` WHERE `REFERENTIAL_CONSTRAINTS`.`CONSTRAINT_NAME` = `KEY_COLUMN_USAGE`.`CONSTRAINT_NAME` AND `REFERENTIAL_CONSTRAINTS`.`CONSTRAINT_SCHEMA` = `KEY_COLUMN_USAGE`.`CONSTRAINT_SCHEMA`) AS `DELETE_RULE`
      FROM `INFORMATION_SCHEMA`.`KEY_COLUMN_USAGE`
      WHERE `TABLE_SCHEMA` = '{0}'
      AND `TABLE_NAME` = '{1}'
      AND `CONSTRAINT_NAME` <> 'PRIMARY'
      AND `REFERENCED_TABLE_NAME` IS NOT NULL
      GROUP BY `CONSTRAINT_SCHEMA`,
      `CONSTRAINT_NAME`,
      `TABLE_NAME`,
      `REFERENCED_TABLE_NAME`;";

    private IEnumerable<DatabaseTable> GetTables(DbConnection connection, Func<string, string, bool> filter)
    {
      using (var command = connection.CreateCommand())
      {
        var tables = new List<DatabaseTable>();
        command.CommandText = GetTablesQuery;
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            var name = reader.GetValueOrDefault<string>("TABLE_NAME");
            var type = reader.GetValueOrDefault<string>("TABLE_TYPE");
            var comment = reader.GetValueOrDefault<string>("TABLE_COMMENT");

            var table = string.Equals(type, "base table", StringComparison.OrdinalIgnoreCase)
                ? new DatabaseTable()
                : new DatabaseView();

            table.Schema = null;
            table.Name = name;
            table.Comment = string.IsNullOrEmpty(comment) ? null : comment;

            if (filter?.Invoke(table.Schema, table.Name) ?? true)
            {
              tables.Add(table);
            }
          }
        }

        // This is done separately due to MARS property may be turned off
        GetColumns(connection, tables, filter);
        GetPrimaryKeys(connection, tables);
        GetIndexes(connection, tables, filter);
        GetConstraints(connection, tables);

        return tables;
      }
    }

    private void GetPrimaryKeys(DbConnection connection, IReadOnlyList<DatabaseTable> tables)
    {
      foreach (var table in tables)
      {
        using (var command = connection.CreateCommand())
        {
          command.CommandText = string.Format(GetPrimaryQuery, connection.Database, table.Name);
          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              try
              {
                var index = new DatabasePrimaryKey
                {
                  Table = table,
                  Name = reader.GetString(0)
                };

                foreach (var column in reader.GetString(2).Split(','))
                {
                  index.Columns.Add(table.Columns.Single(y => y.Name == column));
                }

                table.PrimaryKey = index;
              }
              catch (Exception ex)
              {
                _logger.Logger.LogError(ex, "Error assigning PK for {table}.", table.Name);
              }
            }
          }
        }
      }
    }

    private void GetIndexes(DbConnection connection, IReadOnlyList<DatabaseTable> tables, Func<string, string, bool> tableFilter)
    {
      foreach (var table in tables)
      {
        using (var command = connection.CreateCommand())
        {
          command.CommandText = string.Format(GetIndexesQuery, connection.Database, table.Name);
          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              try
              {
                var index = new DatabaseIndex
                {
                  Table = table,
                  Name = reader.GetString(0),
                  IsUnique = !reader.GetBoolean(1)
                };

                foreach (var column in reader.GetString(2).Split(','))
                {
                  index.Columns.Add(table.Columns.Single(y => y.Name == column));
                }

                table.Indexes.Add(index);
              }
              catch (Exception ex)
              {
                _logger.Logger.LogError(ex, "Error assigning index for {table}.", table.Name);
              }
            }
          }
        }
      }
    }

    private void GetColumns(DbConnection connection, IReadOnlyList<DatabaseTable> tables, Func<string, string, bool> tableFilter)
    {
      foreach (var table in tables)
      {
        using (var command = connection.CreateCommand())
        {
          command.CommandText = string.Format(GetColumnsQuery, table.Name);
          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              var name = reader.GetValueOrDefault<string>("COLUMN_NAME");
              var defaultValue = reader.GetValueOrDefault<string>("COLUMN_DEFAULT");
              var nullable = reader.GetValueOrDefault<string>("IS_NULLABLE").Contains("YES") ? true : false;
              var dataType = reader.GetValueOrDefault<string>("DATA_TYPE");
              var charset = reader.GetValueOrDefault<string>("CHARACTER_SET_NAME");
              var collation = reader.GetValueOrDefault<string>("COLLATION_NAME");
              var columType = reader.GetValueOrDefault<string>("COLUMN_TYPE");
              var extra = reader.GetValueOrDefault<string>("EXTRA");
              var comment = reader.GetValueOrDefault<string>("COLUMN_COMMENT");

              ValueGenerated valueGenerated;

              if (extra.IndexOf("auto_increment", StringComparison.Ordinal) >= 0)
              {
                valueGenerated = ValueGenerated.OnAdd;
              }
              else if (extra.IndexOf("on update", StringComparison.Ordinal) >= 0)
              {
                if (defaultValue != null && extra.IndexOf(defaultValue, StringComparison.Ordinal) > 0 ||
                    (string.Equals(dataType, "timestamp", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(dataType, "datetime", StringComparison.OrdinalIgnoreCase)) &&
                    extra.IndexOf("CURRENT_TIMESTAMP", StringComparison.Ordinal) > 0)
                {
                  valueGenerated = ValueGenerated.OnAddOrUpdate;
                }
                else
                {
                  if (defaultValue != null)
                  {
                    valueGenerated = ValueGenerated.OnAddOrUpdate;
                  }
                  else
                  {
                    valueGenerated = ValueGenerated.OnUpdate;
                  }
                }
              }
              else
              {
                valueGenerated = ValueGenerated.Never;
              }

              defaultValue = FilterClrDefaults(dataType, nullable, defaultValue);

              var column = new DatabaseColumn
              {
                Table = table,
                Name = name,
                StoreType = columType,
                IsNullable = nullable,
                DefaultValueSql = CreateDefaultValueString(defaultValue, dataType),
                ValueGenerated = valueGenerated,
                Comment = string.IsNullOrEmpty(comment) ? null : comment
              };

              table.Columns.Add(column);
            }
          }
        }
      }
    }

    private void GetConstraints(DbConnection connection, IReadOnlyList<DatabaseTable> tables)
    {
      foreach (var table in tables)
      {
        using (var command = connection.CreateCommand())
        {
          command.CommandText = string.Format(GetConstraintsQuery, connection.Database, table.Name);
          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              var referencedTableName = reader.GetString(2);
              var referencedTable = tables.FirstOrDefault(t => t.Name == referencedTableName);
              if (referencedTable != null)
              {
                var fkInfo = new DatabaseForeignKey
                {
                  Name = reader.GetString(0),
                  OnDelete = ConvertToReferentialAction(reader.GetString(4)),
                  Table = table,
                  PrincipalTable = referencedTable
                };
                foreach (var pair in reader.GetString(3).Split(','))
                {
                  fkInfo.Columns.Add(table.Columns.Single(y =>
                      string.Equals(y.Name, pair.Split('|')[0], StringComparison.OrdinalIgnoreCase)));
                  fkInfo.PrincipalColumns.Add(fkInfo.PrincipalTable.Columns.Single(y =>
                      string.Equals(y.Name, pair.Split('|')[1], StringComparison.OrdinalIgnoreCase)));
                }

                table.ForeignKeys.Add(fkInfo);
              }
              else
              {
                _logger.Logger.LogWarning($"Referenced table `{referencedTableName}` is not in dictionary.");
              }
            }
          }
        }
      }
    }

    private static string FilterClrDefaults(string dataTypeName, bool nullable, string defaultValue)
    {
      if (defaultValue == null)
      {
        return null;
      }

      if (nullable)
      {
        return defaultValue;
      }

      if (defaultValue == "0")
      {
        if (dataTypeName == "bit"
            || dataTypeName == "tinyint"
            || dataTypeName == "smallint"
            || dataTypeName == "int"
            || dataTypeName == "bigint"
            || dataTypeName == "decimal"
            || dataTypeName == "double"
            || dataTypeName == "float")
        {
          return null;
        }
      }
      else if (Regex.IsMatch(defaultValue, @"^0\.0+$"))
      {
        if (dataTypeName == "decimal"
            || dataTypeName == "double"
            || dataTypeName == "float")
        {
          return null;
        }
      }

      return defaultValue;
    }

    private string CreateDefaultValueString(string defaultValue, string dataType)
    {
      if (defaultValue == null)
      {
        return null;
      }

      if ((string.Equals(dataType, "timestamp", StringComparison.OrdinalIgnoreCase) ||
          string.Equals(dataType, "datetime", StringComparison.OrdinalIgnoreCase)) &&
          string.Equals(defaultValue, "CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase))
      {
        return defaultValue;
      }

      // Handle bit values.
      if (string.Equals(dataType, "bit", StringComparison.OrdinalIgnoreCase)
          && defaultValue.StartsWith("b'"))
      {
        return defaultValue;
      }

      return "'" + defaultValue.Replace(@"\", @"\\").Replace("'", "''") + "'";
    }
  }
}
