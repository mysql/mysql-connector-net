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
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using MySql.EntityFrameworkCore.Internal;
using MySql.EntityFrameworkCore.Properties;
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MySql.EntityFrameworkCore.Scaffolding.Internal
{
  internal class MySQLDatabaseModelFactory : DatabaseModelFactory
  {
    private readonly IDiagnosticsLogger<DbLoggerCategory.Scaffolding> _logger;
    private readonly IMySQLOptions _options;

    public MySQLDatabaseModelFactory([NotNull] IDiagnosticsLogger<DbLoggerCategory.Scaffolding> logger, IMySQLOptions options)
    {
      Check.NotNull(logger, nameof(logger));

      _logger = logger;
      _options = options;
    }

    /// <inheritdoc/>
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

      SetupMySQLOptions(connection);

      var connectionStartedOpen = connection.State == ConnectionState.Open;
      if (!connectionStartedOpen)
        connection.Open();

      try
      {
        var databaseModel = new DatabaseModel();
        databaseModel.DatabaseName = connection.Database;
        databaseModel.DefaultSchema = GetDefaultSchema(connection);

        var schemaList = options.Schemas.ToList();
        var schemaFilter = GenerateSchemaFilter(schemaList, databaseModel.DefaultSchema);
        var tableList = options.Tables.ToList();
        var tableFilter = GenerateTableFilter(tableList.Select(Parse).ToList(), schemaFilter);

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

    private void SetupMySQLOptions(DbConnection connection)
    {
      if (_options.ConnectionSettings.Equals(new MySQLOptions().ConnectionSettings))
        _options.Initialize(new DbContextOptionsBuilder()
          .UseMySQL(connection).Options);
    }

    private string? GetDefaultSchema(DbConnection connection)
    {
      using (var command = connection.CreateCommand())
      {
        command.CommandText = "SELECT SCHEMA()";
        if (command.ExecuteScalar() is string schema)
          return schema;
        return null;
      }
    }

    private static (string? Schema, string Table) Parse(string table)
    {
      var match = _partExtractor.Match(table.Trim());

      if (!match.Success)
      {
        throw new InvalidOperationException(string.Format(MySQLStrings.InvalidTableToIncludeInScaffolding, table));
      }

      var part1 = match.Groups["part1"].Value.Replace("]]", string.Empty);
      var part2 = match.Groups["part2"].Value.Replace("]]", string.Empty);

      return string.IsNullOrEmpty(part2) ? (null, part1) : (part1, part2);
    }

    private const string NamePartRegex
    = @"(?:(?:\[(?<part{0}>(?:(?:\]\])|[^\]])+)\])|(?<part{0}>[^\.\[\]]+))";

    private static readonly Regex _partExtractor
      = new Regex(
        string.Format(
          CultureInfo.InvariantCulture,
          @"^{0}(?:\.{1})?$",
          string.Format(CultureInfo.InvariantCulture, NamePartRegex, 1),
          string.Format(CultureInfo.InvariantCulture, NamePartRegex, 2)),
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(1000));

    private static Func<string, string>? GenerateSchemaFilter(IReadOnlyList<string> schemas, string? defaultSchema)
    {
      return schemas.Count > 0 || defaultSchema != null
        ? (s =>
        {
          var schemaFilterBuilder = new StringBuilder();
          schemaFilterBuilder.Append(s);
          schemaFilterBuilder.Append(" IN (");
          if (schemas.Count > 0)
            schemaFilterBuilder.Append(string.Join(", ", schemas.Select(EscapeLiteral)));
          else
            schemaFilterBuilder.Append(EscapeLiteral(defaultSchema!));
          schemaFilterBuilder.Append(")");
          return schemaFilterBuilder.ToString();
        })
        : null;
    }

    private static Func<string, string, string>? GenerateTableFilter(IReadOnlyList<(string? Schema, string Table)> tables, Func<string, string>? schemaFilter)
    {
      return schemaFilter != null || tables.Count > 0
        ? ((s, t) =>
        {
          var tableFilterBuilder = new StringBuilder();

          var openBracket = false;
          if (schemaFilter != null)
          {
            tableFilterBuilder
            .Append("(")
            .Append(schemaFilter(s));
            openBracket = true;
          }

          if (tables.Count > 0)
          {
            if (openBracket)
            {
              tableFilterBuilder
              .AppendLine()
              .Append("AND ");
            }
            else
            {
              tableFilterBuilder.Append("(");
              openBracket = true;
            }

            var tablesWithoutSchema = tables.Where(e => string.IsNullOrEmpty(e.Schema)).ToList();
            if (tablesWithoutSchema.Count > 0)
            {
              tableFilterBuilder.Append(t);
              tableFilterBuilder.Append(" IN (");
              tableFilterBuilder.Append(string.Join(", ", tablesWithoutSchema.Select(e => EscapeLiteral(e.Table))));
              tableFilterBuilder.Append(")");
            }

            var tablesWithSchema = tables.Where(e => !string.IsNullOrEmpty(e.Schema)).ToList();
            if (tablesWithSchema.Count > 0)
            {
              if (tablesWithoutSchema.Count > 0)
                tableFilterBuilder.Append(" OR ");

              tableFilterBuilder.Append("CONCAT_WS(N'.',");
              tableFilterBuilder.Append(string.Join(",", s, t));
              tableFilterBuilder.Append(") IN (");
              tableFilterBuilder.Append(string.Join(", ", tablesWithSchema.Select(e => EscapeLiteral($"{e.Schema}.{e.Table}"))));
              tableFilterBuilder.Append(")");
            }
          }

          if (openBracket)
            tableFilterBuilder.Append(")");

          return tableFilterBuilder.ToString();
        }) : null;
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

    private const string GetTablesQuery = @"SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE,
  IF(TABLE_COMMENT = 'VIEW' AND TABLE_TYPE = 'VIEW', '', TABLE_COMMENT) AS TABLE_COMMENT 
  FROM INFORMATION_SCHEMA.TABLES
  WHERE TABLE_TYPE IN ('BASE TABLE', 'VIEW')
  AND {0};";

    private const string GetPrimaryQuery = @"SELECT
   `TABLE_SCHEMA`,
   `TABLE_NAME`,
   `INDEX_NAME`,
   GROUP_CONCAT(`COLUMN_NAME` ORDER BY `SEQ_IN_INDEX` SEPARATOR ',') AS COLUMNS
   FROM `INFORMATION_SCHEMA`.`STATISTICS`
   WHERE {0}
   AND `INDEX_NAME` = 'PRIMARY'
   GROUP BY `TABLE_SCHEMA`, `TABLE_NAME`, `INDEX_NAME`, `NON_UNIQUE`;";

    private const string GetColumnsQuery = @"SELECT
    `TABLE_SCHEMA`,
    `TABLE_NAME`,
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
    WHERE {0}
    ORDER BY
    `ORDINAL_POSITION`;";

    private const string GetIndexesQuery = @"SELECT 
    `TABLE_SCHEMA`,
    `TABLE_NAME`,
    `INDEX_NAME`,
    IF(`NON_UNIQUE`, 'TRUE', 'FALSE') AS NON_UNIQUE,
    GROUP_CONCAT(`COLUMN_NAME` ORDER BY `SEQ_IN_INDEX` SEPARATOR ',') AS COLUMNS
    FROM `INFORMATION_SCHEMA`.`STATISTICS`
    WHERE {0}
    AND `INDEX_NAME` <> 'PRIMARY'
    GROUP BY `TABLE_SCHEMA`, `TABLE_NAME`, `INDEX_NAME`, `NON_UNIQUE`;";

    private const string GetConstraintsQuery = @"SELECT
    `TABLE_SCHEMA`,
    `TABLE_NAME`,
    `CONSTRAINT_NAME`,
    `REFERENCED_TABLE_NAME`,
    GROUP_CONCAT(CONCAT_WS('|', `COLUMN_NAME`, `REFERENCED_COLUMN_NAME`) ORDER BY `ORDINAL_POSITION` SEPARATOR ',') AS PAIRED_COLUMNS,
    (SELECT `DELETE_RULE` FROM `INFORMATION_SCHEMA`.`REFERENTIAL_CONSTRAINTS` 
    WHERE `REFERENTIAL_CONSTRAINTS`.`CONSTRAINT_NAME` = `KEY_COLUMN_USAGE`.`CONSTRAINT_NAME` 
    AND `REFERENTIAL_CONSTRAINTS`.`CONSTRAINT_SCHEMA` = `KEY_COLUMN_USAGE`.`CONSTRAINT_SCHEMA`) AS `DELETE_RULE`
    FROM `INFORMATION_SCHEMA`.`KEY_COLUMN_USAGE`
    WHERE {0}
    AND `CONSTRAINT_NAME` <> 'PRIMARY'
    AND `REFERENCED_TABLE_NAME` IS NOT NULL
    GROUP BY `TABLE_SCHEMA`, `TABLE_NAME`, 
    `CONSTRAINT_SCHEMA`, `CONSTRAINT_NAME`,
    `TABLE_NAME`, `REFERENCED_TABLE_NAME`;";

    private IEnumerable<DatabaseTable> GetTables(DbConnection connection, Func<string, string, string>? tableFilter)
    {
      using (var command = connection.CreateCommand())
      {
        var tables = new List<DatabaseTable>();
        string filter = tableFilter!("TABLE_SCHEMA", "TABLE_NAME");
        command.CommandText = string.Format(GetTablesQuery, filter);
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            var schema = reader.GetValueOrDefault<string>("TABLE_SCHEMA");
            var name = reader.GetValueOrDefault<string>("TABLE_NAME");
            var type = reader.GetValueOrDefault<string>("TABLE_TYPE");
            var comment = reader.GetValueOrDefault<string>("TABLE_COMMENT");

            var table = string.Equals(type, "base table", StringComparison.OrdinalIgnoreCase)
              ? new DatabaseTable()
              : new DatabaseView();

            table.Schema = schema;
            table.Name = name!;
            table.Comment = string.IsNullOrEmpty(comment) ? null : comment;

            tables.Add(table);
          }
        }

        // This is done separately due to MARS property may be turned off
        GetColumns(connection, tables, filter);
        GetPrimaryKeys(connection, tables, filter);
        GetIndexes(connection, tables, filter);
        GetConstraints(connection, tables, filter);

        return tables;
      }
    }

    private void GetColumns(DbConnection connection, IReadOnlyList<DatabaseTable> tables, string tableFilter)
    {
      using (var command = connection.CreateCommand())
      {
        command.CommandText = string.Format(GetColumnsQuery, tableFilter);
        using (var reader = command.ExecuteReader())
        {
          var tableColumnGroups = reader.Cast<DbDataRecord>()
          .GroupBy(
            ddr => (tableSchema: ddr.GetValueOrDefault<string>("TABLE_SCHEMA"),
            tableName: ddr.GetValueOrDefault<string>("TABLE_NAME")));

          foreach (var tableColumnGroup in tableColumnGroups)
          {
            var tableSchema = tableColumnGroup.Key.tableSchema;
            var tableName = tableColumnGroup.Key.tableName;

            var table = tables.Single(t => t.Schema == tableSchema && t.Name == tableName);

            foreach (var dataRecord in tableColumnGroup)
            {
              var name = dataRecord.GetValueOrDefault<string>("COLUMN_NAME");
              var defaultValue = dataRecord.GetValueOrDefault<string>("COLUMN_DEFAULT");
              var nullable = dataRecord.GetValueOrDefault<string>("IS_NULLABLE")!.Contains("YES");
              var dataType = dataRecord.GetValueOrDefault<string>("DATA_TYPE");
              var charset = dataRecord.GetValueOrDefault<string>("CHARACTER_SET_NAME");
              var collation = dataRecord.GetValueOrDefault<string>("COLLATION_NAME");
              var columType = dataRecord.GetValueOrDefault<string>("COLUMN_TYPE");
              var extra = dataRecord.GetValueOrDefault<string>("EXTRA");
              var comment = dataRecord.GetValueOrDefault<string>("COLUMN_COMMENT");

              ValueGenerated valueGenerated;

              if (extra!.IndexOf("auto_increment", StringComparison.Ordinal) >= 0)
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

              defaultValue = FilterClrDefaults(dataType!, nullable, defaultValue);

              var column = new DatabaseColumn
              {
                Table = table,
                Name = name!,
                StoreType = columType,
                IsNullable = nullable,
                DefaultValueSql = CreateDefaultValueString(defaultValue, dataType!),
                ValueGenerated = valueGenerated,
                Comment = string.IsNullOrEmpty(comment) ? null : comment
              };

              table.Columns.Add(column);
            }
          }
        }
      }
    }

    private void GetPrimaryKeys(DbConnection connection, IReadOnlyList<DatabaseTable> tables, string tableFilter)
    {
      using (var command = connection.CreateCommand())
      {
        command.CommandText = string.Format(GetPrimaryQuery, tableFilter);
        using (var reader = command.ExecuteReader())
        {
          var tablePrimaryKeyGroups = reader.Cast<DbDataRecord>()
            .GroupBy(
              ddr => (tableSchema: ddr.GetValueOrDefault<string>("TABLE_SCHEMA"),
                tableName: ddr.GetValueOrDefault<string>("TABLE_NAME")));

          foreach (var tablePrimaryKeyGroup in tablePrimaryKeyGroups)
          {
            var tableSchema = tablePrimaryKeyGroup.Key.tableSchema;
            var tableName = tablePrimaryKeyGroup.Key.tableName;

            var table = tables.Single(t => t.Schema == tableSchema && t.Name == tableName);

            foreach (var dataRecord in tablePrimaryKeyGroup)
            {
              try
              {
                var index = new DatabasePrimaryKey
                {
                  Table = table,
                  Name = dataRecord.GetValueOrDefault<string>("INDEX_NAME")
                };

                foreach (var column in dataRecord.GetValueOrDefault<string>("COLUMNS")!.Split(','))
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

    private void GetIndexes(DbConnection connection, IReadOnlyList<DatabaseTable> tables, string tableFilter)
    {
      using (var command = connection.CreateCommand())
      {
        command.CommandText = string.Format(GetIndexesQuery, tableFilter);
        using (var reader = command.ExecuteReader())
        {
          var tableIndexGroups = reader.Cast<DbDataRecord>()
          .GroupBy(
            ddr => (tableSchema: ddr.GetValueOrDefault<string>("TABLE_SCHEMA"),
            tableName: ddr.GetValueOrDefault<string>("TABLE_NAME")));

          foreach (var tableIndexGroup in tableIndexGroups)
          {
            var tableSchema = tableIndexGroup.Key.tableSchema;
            var tableName = tableIndexGroup.Key.tableName;

            var table = tables.Single(t => t.Schema == tableSchema && t.Name == tableName);

            foreach (var dataRecord in tableIndexGroup)
            {
              try
              {
                var index = new DatabaseIndex
                {
                  Table = table,
                  Name = dataRecord.GetValueOrDefault<string>("INDEX_NAME"),
                  IsUnique = !bool.Parse(dataRecord.GetValueOrDefault<string>("NON_UNIQUE")!)
                };

                foreach (var column in dataRecord.GetValueOrDefault<string>("COLUMNS")!.Split(','))
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

    private void GetConstraints(DbConnection connection, IReadOnlyList<DatabaseTable> tables, string tableFilter)
    {
      using (var command = connection.CreateCommand())
      {
        command.CommandText = string.Format(GetConstraintsQuery, tableFilter);

        using (var reader = command.ExecuteReader())
        {
          var tableConstraintGroups = reader.Cast<DbDataRecord>()
          .GroupBy(
            ddr => (tableSchema: ddr.GetValueOrDefault<string>("TABLE_SCHEMA"),
            tableName: ddr.GetValueOrDefault<string>("TABLE_NAME")));

          foreach (var tableConstraintGroup in tableConstraintGroups)
          {
            var tableSchema = tableConstraintGroup.Key.tableSchema;
            var tableName = tableConstraintGroup.Key.tableName;

            var table = tables.Single(t => t.Schema == tableSchema && t.Name == tableName);

            foreach (var dataRecord in tableConstraintGroup)
            {
              var referencedTableName = dataRecord.GetValueOrDefault<string>("REFERENCED_TABLE_NAME");
              var referencedTable = tables.FirstOrDefault(t => t.Name == referencedTableName);
              if (referencedTable != null)
              {
                var fkInfo = new DatabaseForeignKey
                {
                  Name = dataRecord.GetValueOrDefault<string>("CONSTRAINT_NAME"),
                  OnDelete = ConvertToReferentialAction(dataRecord.GetValueOrDefault<string>("DELETE_RULE")!),
                  Table = table,
                  PrincipalTable = referencedTable
                };
                foreach (var pair in dataRecord.GetValueOrDefault<string>("PAIRED_COLUMNS")!.Split(','))
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

    private static string? FilterClrDefaults(string dataTypeName, bool nullable, string? defaultValue)
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

    private string? CreateDefaultValueString(string? defaultValue, string dataType)
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

    private static string EscapeLiteral(string s)
    {
      return $"N'{s}'";
    }
  }
}
