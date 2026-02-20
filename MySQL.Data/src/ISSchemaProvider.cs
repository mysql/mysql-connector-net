// Copyright © 2004, 2026, Oracle and/or its affiliates.
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

using MySql.Data.Common;
using MySql.Data.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  internal class ISSchemaProvider : SchemaProvider
  {
    public ISSchemaProvider(MySqlConnection connection)
      : base(connection)
    {
    }

    protected override MySqlSchemaCollection GetCollections()
    {
      MySqlSchemaCollection dt = base.GetCollections();

      object[][] collections = {
                new object[] {"Views", 2, 3},
                new object[] {"ViewColumns", 3, 4},
                new object[] {"Procedure Parameters", 5, 1},
                new object[] {"Procedures", 4, 3},
                new object[] {"Triggers", 2, 4}
            };

      FillTable(dt, collections);
      return dt;
    }

    protected override MySqlSchemaCollection GetRestrictions()
    {
      MySqlSchemaCollection dt = base.GetRestrictions();

      object[][] restrictions = new object[][]
            {
                new object[] {"Procedure Parameters", "Database", "", 0},
                new object[] {"Procedure Parameters", "Schema", "", 1},
                new object[] {"Procedure Parameters", "Name", "", 2},
                new object[] {"Procedure Parameters", "Type", "", 3},
                new object[] {"Procedure Parameters", "Parameter", "", 4},
                new object[] {"Procedures", "Database", "", 0},
                new object[] {"Procedures", "Schema", "", 1},
                new object[] {"Procedures", "Name", "", 2},
                new object[] {"Procedures", "Type", "", 3},
                new object[] {"Views", "Database", "", 0},
                new object[] {"Views", "Schema", "", 1},
                new object[] {"Views", "Table", "", 2},
                new object[] {"ViewColumns", "Database", "", 0},
                new object[] {"ViewColumns", "Schema", "", 1},
                new object[] {"ViewColumns", "Table", "", 2},
                new object[] {"ViewColumns", "Column", "", 3},
                new object[] {"Triggers", "Database", "", 0},
                new object[] {"Triggers", "Schema", "", 1},
                new object[] {"Triggers", "Name", "", 2},
                new object[] {"Triggers", "EventObjectTable", "", 3},
            };
      FillTable(dt, restrictions);
      return dt;
    }

    /// <summary>
    /// Retrieves metadata about the databases (schemas) available on the MySQL server.
    /// Queries the INFORMATION_SCHEMA.SCHEMATA table and applies any provided restrictions.
    /// </summary>
    /// <param name="restrictions">An array of filter values corresponding to the schema's restriction keys (e.g., schema name pattern).</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> containing database information, with columns renamed for consistency.</returns>
    public override MySqlSchemaCollection GetDatabases(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[1];
      keys[0] = "SCHEMA_NAME";
      MySqlSchemaCollection dt = Query("SCHEMATA", "", keys, restrictions, cancellationToken);
      dt.Columns[1].Name = "database_name";
      dt.Name = "Databases";
      return dt;
    }

    /// <summary>
    /// Asynchronously retrieves metadata about the databases (schemas) available on the MySQL server.
    /// Queries the INFORMATION_SCHEMA.SCHEMATA table and applies any provided restrictions.
    /// </summary>
    /// <param name="restrictions">An array of filter values corresponding to the schema's restriction keys (e.g., schema name pattern).</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> containing database information, with columns renamed for consistency.</returns>
    public override async Task<MySqlSchemaCollection> GetDatabasesAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys = ["SCHEMA_NAME"];
      MySqlSchemaCollection dt = await QueryAsync("SCHEMATA", "", keys, restrictions, cancellationToken).ConfigureAwait(false);
      dt.Columns[1].Name = "database_name";
      dt.Name = "Databases";
      return dt;
    }

    /// <summary>
    /// Retrieves metadata about the tables in the MySQL database, excluding views.
    /// Queries the INFORMATION_SCHEMA.TABLES table with a filter to exclude views and applies restrictions.
    /// </summary>
    /// <param name="restrictions">An array of filter values for catalog, schema, table name, and table type.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> containing table information.</returns>
    public override MySqlSchemaCollection GetTables(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys = ["TABLE_CATALOG", "TABLE_SCHEMA", "TABLE_NAME", "TABLE_TYPE"];
      MySqlSchemaCollection dt = Query("TABLES", "TABLE_TYPE != 'VIEW'", keys, restrictions, cancellationToken);
      dt.Name = "Tables";
      return dt;
    }

    /// <summary>
    /// Asynchronously retrieves metadata about the tables in the MySQL database, excluding views.
    /// Queries the INFORMATION_SCHEMA.TABLES table with a filter to exclude views and applies restrictions.
    /// </summary>
    /// <param name="restrictions">An array of filter values for catalog, schema, table name, and table type.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> containing table information.</returns>
    public override async Task<MySqlSchemaCollection> GetTablesAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys = ["TABLE_CATALOG", "TABLE_SCHEMA", "TABLE_NAME", "TABLE_TYPE"];
      MySqlSchemaCollection dt = await QueryAsync("TABLES", "TABLE_TYPE != 'VIEW'", keys, restrictions, cancellationToken).ConfigureAwait(false);
      dt.Name = "Tables";
      return dt;
    }

    /// <summary>
    /// Retrieves metadata about the columns in the specified tables.
    /// Queries the INFORMATION_SCHEMA.COLUMNS table, removes octet length column, quotes default values, and applies restrictions.
    /// </summary>
    /// <param name="restrictions">An array of filter values for catalog, schema, table name, and column name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> containing column information.</returns>
    public override MySqlSchemaCollection GetColumns(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys = ["TABLE_CATALOG", "TABLE_SCHEMA", "TABLE_NAME", "COLUMN_NAME"];
      MySqlSchemaCollection dt = Query("COLUMNS", null, keys, restrictions, cancellationToken);
      dt.RemoveColumn("CHARACTER_OCTET_LENGTH");
      dt.Name = "Columns";
      QuoteDefaultValues(dt);
      return dt;
    }

    /// <summary>
    /// Asynchronously retrieves metadata about the columns in the specified tables.
    /// Queries the INFORMATION_SCHEMA.COLUMNS table, removes octet length column, quotes default values, and applies restrictions.
    /// </summary>
    /// <param name="restrictions">An array of filter values for catalog, schema, table name, and column name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> containing column information.</returns>
    public override async Task<MySqlSchemaCollection> GetColumnsAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys = ["TABLE_CATALOG", "TABLE_SCHEMA", "TABLE_NAME", "COLUMN_NAME"];
      MySqlSchemaCollection dt = await QueryAsync("COLUMNS", null, keys, restrictions, cancellationToken).ConfigureAwait(false);
      dt.RemoveColumn("CHARACTER_OCTET_LENGTH");
      dt.Name = "Columns";
      QuoteDefaultValues(dt);
      return dt;
    }

    /// <summary>
    /// Retrieves metadata about the views in the database.
    /// Queries the INFORMATION_SCHEMA.VIEWS table and applies restrictions. Intended for internal use.
    /// </summary>
    /// <param name="restrictions">An array of filter values for catalog, schema, and view name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> containing view information.</returns>
    private MySqlSchemaCollection GetViews(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys = ["TABLE_CATALOG", "TABLE_SCHEMA", "TABLE_NAME"];
      MySqlSchemaCollection dt = Query("VIEWS", null, keys, restrictions, cancellationToken);
      dt.Name = "Views";
      return dt;
    }

    /// <summary>
    /// Asynchronously retrieves metadata about the views in the database.
    /// Queries the INFORMATION_SCHEMA.VIEWS table and applies restrictions. Intended for internal use.
    /// </summary>
    /// <param name="restrictions">An array of filter values for catalog, schema, and view name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> containing view information.</returns>
    private async Task<MySqlSchemaCollection> GetViewsAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys = ["TABLE_CATALOG", "TABLE_SCHEMA", "TABLE_NAME"];
      MySqlSchemaCollection dt = await QueryAsync("VIEWS", null, keys, restrictions, cancellationToken).ConfigureAwait(false);
      dt.Name = "Views";
      return dt;
    }

    /// <summary>
    /// Builds the SQL query for retrieving view columns, including JOIN and WHERE clause based on restrictions.
    /// </summary>
    /// <param name="restrictions">An array of filter values for database, schema, table (view name), and column name.</param>
    /// <returns>The constructed SQL query string.</returns>
    private string BuildViewColumnsQuery(string[] restrictions)
    {
      StringBuilder where = new StringBuilder();
      StringBuilder sql = new StringBuilder("SELECT C.* FROM information_schema.columns C");
      sql.Append(" JOIN information_schema.views V ");
      sql.Append("ON C.table_schema=V.table_schema AND C.table_name=V.table_name ");
      if (restrictions != null && restrictions.Length >= 2 &&
          restrictions[1] != null)
        where.AppendFormat(CultureInfo.InvariantCulture, "C.table_schema='{0}' ", restrictions[1]);
      
      if (restrictions != null && restrictions.Length >= 3 &&
          restrictions[2] != null)
      {
        if (where.Length > 0)
          where.Append("AND ");
        where.AppendFormat(CultureInfo.InvariantCulture, "C.table_name='{0}' ", restrictions[2]);
      }

      if (restrictions != null && restrictions.Length == 4 &&
          restrictions[3] != null)
      {
        if (where.Length > 0)
          where.Append("AND ");
        where.AppendFormat(CultureInfo.InvariantCulture, "C.column_name='{0}' ", restrictions[3]);
      }

      if (where.Length > 0)
        sql.AppendFormat(CultureInfo.InvariantCulture, " WHERE {0}", where);
      
      return sql.ToString();
    }

    /// <summary>
    /// Processes the view columns collection by setting the name and renaming columns for view context, then quotes default values.
    /// </summary>
    /// <param name="dt">The MySqlSchemaCollection to process.</param>
    private void ProcessViewColumnsCollection(MySqlSchemaCollection dt)
    {
      dt.Name = "ViewColumns";
      dt.Columns[0].Name = "VIEW_CATALOG";
      dt.Columns[1].Name = "VIEW_SCHEMA";
      dt.Columns[2].Name = "VIEW_NAME";
      QuoteDefaultValues(dt);
    }

    /// <summary>
    /// Retrieves metadata about the columns in views.
    /// Constructs a JOIN query between INFORMATION_SCHEMA.COLUMNS and VIEWS, applies restrictions, renames columns for view context, and quotes default values. Intended for internal use.
    /// </summary>
    /// <param name="restrictions">An array of filter values for database, schema, table (view name), and column name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> containing view column information.</returns>
    private MySqlSchemaCollection GetViewColumns(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string sql = BuildViewColumnsQuery(restrictions);
      MySqlSchemaCollection dt = GetTable(sql, cancellationToken);
      ProcessViewColumnsCollection(dt);
      return dt;
    }

    /// <summary>
    /// Asynchronously retrieves metadata about the columns in views.
    /// Constructs a JOIN query between INFORMATION_SCHEMA.COLUMNS and VIEWS, applies restrictions, renames columns for view context, and quotes default values. Intended for internal use.
    /// </summary>
    /// <param name="restrictions">An array of filter values for database, schema, table (view name), and column name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> containing view column information.</returns>
    private async Task<MySqlSchemaCollection> GetViewColumnsAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string sql = BuildViewColumnsQuery(restrictions);
      MySqlSchemaCollection dt = await GetTableAsync(sql, cancellationToken).ConfigureAwait(false);
      ProcessViewColumnsCollection(dt);
      return dt;
    }

    /// <summary>
    /// Retrieves metadata about the triggers in the database.
    /// Queries the INFORMATION_SCHEMA.TRIGGERS table and applies restrictions. Intended for internal use.
    /// </summary>
    /// <param name="restrictions">An array of filter values for catalog, schema, event object table, and trigger name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> containing trigger information.</returns>
    private MySqlSchemaCollection GetTriggers(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys = ["TRIGGER_CATALOG", "TRIGGER_SCHEMA", "EVENT_OBJECT_TABLE", "TRIGGER_NAME"];
      MySqlSchemaCollection dt = Query("TRIGGERS", null, keys, restrictions, cancellationToken);
      dt.Name = "Triggers";
      return dt;
    }

    /// <summary>
    /// Asynchronously retrieves metadata about the triggers in the database.
    /// Queries the INFORMATION_SCHEMA.TRIGGERS table and applies restrictions. Intended for internal use.
    /// </summary>
    /// <param name="restrictions">An array of filter values for catalog, schema, event object table, and trigger name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> containing trigger information.</returns>
    private async Task<MySqlSchemaCollection> GetTriggersAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys = ["TRIGGER_CATALOG", "TRIGGER_SCHEMA", "EVENT_OBJECT_TABLE", "TRIGGER_NAME"];
      MySqlSchemaCollection dt = await QueryAsync("TRIGGERS", null, keys, restrictions, cancellationToken).ConfigureAwait(false);
      dt.Name = "Triggers";
      return dt;
    }

    /// <summary>
    /// Retrieves schema information about stored procedures and functions on the MySQL server.
    /// Queries the INFORMATION_SCHEMA.ROUTINES table and supports restrictions on catalog, schema (database), name, and type (PROCEDURE or FUNCTION).
    /// </summary>
    /// <param name="restrictions">An array of filter values for routine catalog, schema, name, and type.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> containing procedure and function information.</returns>
    public MySqlSchemaCollection GetProcedures(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys = ["ROUTINE_CATALOG", "ROUTINE_SCHEMA", "ROUTINE_NAME", "ROUTINE_TYPE"];
      MySqlSchemaCollection dt = Query("ROUTINES", null, keys, restrictions, cancellationToken);
      dt.Name = "Procedures";
      return dt;
    }

    /// <summary>
    /// Asynchronously retrieves schema information about stored procedures and functions on the MySQL server.
    /// Queries the INFORMATION_SCHEMA.ROUTINES table and supports restrictions on catalog, schema (database), name, and type (PROCEDURE or FUNCTION).
    /// </summary>
    /// <param name="restrictions">An array of filter values for routine catalog, schema, name, and type.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> containing procedure and function information.</returns>
    public async Task<MySqlSchemaCollection> GetProceduresAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys = ["ROUTINE_CATALOG", "ROUTINE_SCHEMA", "ROUTINE_NAME", "ROUTINE_TYPE"];
      MySqlSchemaCollection dt = await QueryAsync("ROUTINES", null, keys, restrictions, cancellationToken).ConfigureAwait(false);
      dt.Name = "Procedures";
      return dt;
    }

    /// <summary>
    /// Retrieves parameter information for a specific routine (procedure or function) from INFORMATION_SCHEMA.PARAMETERS.
    /// Applies restrictions and adjusts the first row for function return values if applicable. Intended for internal use.
    /// </summary>
    /// <param name="restrictions">An array of filter values for specific catalog, schema, name, routine type, and parameter name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> containing parameter information for the routine.</returns>
    private MySqlSchemaCollection GetParametersForRoutineFromExec(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys =
      [
        "SPECIFIC_CATALOG",
        "SPECIFIC_SCHEMA",
        "SPECIFIC_NAME",
        "ROUTINE_TYPE",
        "PARAMETER_NAME",
      ];
      StringBuilder sql = new StringBuilder(@"SELECT * FROM INFORMATION_SCHEMA.PARAMETERS");
      // now get our where clause and append it if there is one
      string where = GetWhereClause(null, keys, restrictions);
      if (!string.IsNullOrEmpty(where))
        sql.AppendFormat(CultureInfo.InvariantCulture, " WHERE {0}", where);

      MySqlSchemaCollection coll = QueryCollection("parameters", sql.ToString(), cancellationToken);

      if ((coll.Rows.Count != 0) && ((string)coll.Rows[0]["routine_type"] == "FUNCTION"))
      {
        // update missing data for the first row (function return value).
        // (using sames valus than GetParametersFromShowCreate).
        coll.Rows[0]["parameter_mode"] = "IN";
        coll.Rows[0]["parameter_name"] = "return_value"; // "FUNCTION";
      }
      return coll;
    }

    /// <summary>
    /// Asynchronously retrieves parameter information for a specific routine (procedure or function) from INFORMATION_SCHEMA.PARAMETERS.
    /// Applies restrictions and adjusts the first row for function return values if applicable. Intended for internal use.
    /// </summary>
    /// <param name="restrictions">An array of filter values for specific catalog, schema, name, routine type, and parameter name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> containing parameter information for the routine.</returns>
    private async Task<MySqlSchemaCollection> GetParametersForRoutineFromExecAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string[] keys =
      [
        "SPECIFIC_CATALOG",
        "SPECIFIC_SCHEMA",
        "SPECIFIC_NAME",
        "ROUTINE_TYPE",
        "PARAMETER_NAME",
      ];
      StringBuilder sql = new StringBuilder(@"SELECT * FROM INFORMATION_SCHEMA.PARAMETERS");
      // now get our where clause and append it if there is one
      string where = GetWhereClause(null, keys, restrictions);
      if (!string.IsNullOrEmpty(where))
        sql.AppendFormat(CultureInfo.InvariantCulture, " WHERE {0}", where);

      MySqlSchemaCollection coll = await QueryCollectionAsync("parameters", sql.ToString(), cancellationToken).ConfigureAwait(false);

      if ((coll.Rows.Count != 0) && ((string)coll.Rows[0]["routine_type"] == "FUNCTION"))
      {
        // update missing data for the first row (function return value).
        // (using sames valus than GetParametersFromShowCreate).
        coll.Rows[0]["parameter_mode"] = "IN";
        coll.Rows[0]["parameter_name"] = "return_value"; // "FUNCTION";
      }
      return coll;
    }

    /// <summary>
    /// Retrieves schema information about parameters for stored procedures and functions.
    /// Supports restrictions on database (schema), name, type, and parameter name. Uses provided routines collection or queries directly.
    /// </summary>
    /// <param name="restrictions">An array of filter values for database, schema, name, type, and parameter.</param>
    /// <param name="routines">An optional <see cref="MySqlSchemaCollection"/> of routines to retrieve parameters for; if null or empty, queries directly.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> containing procedure parameter information.</returns>
    public virtual MySqlSchemaCollection GetProcedureParameters(string[] restrictions,
        MySqlSchemaCollection routines, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection parms = null;

      if (routines == null || routines.Rows.Count == 0)
      {
        parms = GetParametersForRoutineFromExec(restrictions, cancellationToken);
      }
      else foreach (MySqlSchemaRow routine in routines.Rows)
        {
          if (restrictions != null && restrictions.Length >= 3)
            restrictions[2] = routine["ROUTINE_NAME"].ToString();

          parms = GetParametersForRoutineFromExec(restrictions, cancellationToken);
        }
      parms.Name = "Procedure Parameters";
      return parms;
    }

    /// <summary>
    /// Asynchronously retrieves schema information about parameters for stored procedures and functions.
    /// Supports restrictions on database (schema), name, type, and parameter name. Uses provided routines collection or queries directly.
    /// </summary>
    /// <param name="restrictions">An array of filter values for database, schema, name, type, and parameter.</param>
    /// <param name="routines">An optional <see cref="MySqlSchemaCollection"/> of routines to retrieve parameters for; if null or empty, queries directly.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> containing procedure parameter information.</returns>
    public virtual async Task<MySqlSchemaCollection> GetProcedureParametersAsync(string[] restrictions,
        MySqlSchemaCollection routines, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection parms = null;

      if (routines == null || routines.Rows.Count == 0)
      {
        parms = await GetParametersForRoutineFromExecAsync(restrictions, cancellationToken).ConfigureAwait(false);
      }
      else foreach (MySqlSchemaRow routine in routines.Rows)
        {
          if (restrictions != null && restrictions.Length >= 3)
            restrictions[2] = routine["ROUTINE_NAME"].ToString();

          parms = await GetParametersForRoutineFromExecAsync(restrictions, cancellationToken).ConfigureAwait(false);
        }
      parms.Name = "Procedure Parameters";
      return parms;
    }

    /// <summary>
    /// Retrieves schema metadata for the specified collection, delegating to base or specific handlers.
    /// Handles dispatching to specialized methods for views, procedures, parameters, triggers, and view columns. Intended for override in derived classes.
    /// </summary>
    /// <param name="collection">The name of the schema collection to retrieve (e.g., "VIEWS", "PROCEDURES").</param>
    /// <param name="restrictions">An array of filter values specific to the collection.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> containing the requested schema information, or null if the collection is not handled.</returns>
    protected override MySqlSchemaCollection GetSchemaInternal(string collection, string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection dt = base.GetSchemaInternal(collection, restrictions, cancellationToken);
      if (dt != null)
        return dt;

      switch (collection)
      {
        case "VIEWS":
          return GetViews(restrictions, cancellationToken);
        case "PROCEDURES":
          return GetProcedures(restrictions, cancellationToken);
        case "PROCEDURE PARAMETERS":
          return GetProcedureParameters(restrictions, null, cancellationToken);
        case "TRIGGERS":
          return GetTriggers(restrictions, cancellationToken);
        case "VIEWCOLUMNS":
          return GetViewColumns(restrictions, cancellationToken);
      }
      return null;
    }

    /// <summary>
    /// Asynchronously retrieves schema metadata for the specified collection, delegating to base or specific handlers.
    /// Handles dispatching to specialized methods for views, procedures, parameters, triggers, and view columns. Intended for override in derived classes.
    /// </summary>
    /// <param name="collection">The name of the schema collection to retrieve (e.g., "VIEWS", "PROCEDURES").</param>
    /// <param name="restrictions">An array of filter values specific to the collection.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> containing the requested schema information, or null if the collection is not handled.</returns>
    protected override async Task<MySqlSchemaCollection> GetSchemaInternalAsync(string collection, string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection dt = await base.GetSchemaInternalAsync(collection, restrictions, cancellationToken).ConfigureAwait(false);
      if (dt != null)
        return dt;

      switch (collection)
      {
        case "VIEWS":
          return await GetViewsAsync(restrictions, cancellationToken).ConfigureAwait(false);
        case "PROCEDURES":
          return await GetProceduresAsync(restrictions, cancellationToken).ConfigureAwait(false);
        case "PROCEDURE PARAMETERS":
          return await GetProcedureParametersAsync(restrictions, null, cancellationToken).ConfigureAwait(false);
        case "TRIGGERS":
          return await GetTriggersAsync(restrictions, cancellationToken).ConfigureAwait(false);
        case "VIEWCOLUMNS":
          return await GetViewColumnsAsync(restrictions, cancellationToken).ConfigureAwait(false);
      }
      return null;
    }

    private static string GetWhereClause(string initial_where, string[] keys, string[] values)
    {
      StringBuilder where = new StringBuilder(initial_where);
      if (values != null)
      {
        for (int i = 0; i < keys.Length; i++)
        {
          if (i >= values.Length) break;
          if (values[i] == null || values[i] == String.Empty) continue;
          if (where.Length > 0)
            where.Append(" AND ");
          where.AppendFormat(CultureInfo.InvariantCulture,
              "{0} LIKE '{1}'", keys[i], values[i]);
        }
      }
      return where.ToString();
    }

    /// <summary>
    /// Constructs the query SQL using <see cref="BuildQueryString"/> and executes it against an INFORMATION_SCHEMA table.
    /// Applies initial WHERE clause and restrictions based on keys. Adds ordering for COLUMNS table.
    /// Intended for internal use in schema retrieval.
    /// </summary>
    /// <param name="tableName">The name of the INFORMATION_SCHEMA table to query (e.g., "TABLES", "COLUMNS").</param>
    /// <param name="initialWhere">An initial WHERE clause fragment to append.</param>
    /// <param name="keys">An array of column names used for building restriction filters.</param>
    /// <param name="values">An array of filter values corresponding to the keys.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> populated with the query results.</returns>
    private MySqlSchemaCollection Query(string tableName, string initialWhere, string[] keys, string[] values, CancellationToken cancellationToken = default)
    {
      string sql = BuildQueryString(tableName, initialWhere, keys, values);
      return GetTable(sql, cancellationToken);
    }

    /// <summary>
    /// Asynchronously constructs the query SQL using <see cref="BuildQueryString"/> and executes it against an INFORMATION_SCHEMA table.
    /// Applies initial WHERE clause and restrictions based on keys. Adds ordering for COLUMNS table.
    /// Intended for internal use in schema retrieval.
    /// </summary>
    /// <param name="tableName">The name of the INFORMATION_SCHEMA table to query (e.g., "TABLES", "COLUMNS").</param>
    /// <param name="initialWhere">An initial WHERE clause fragment to append.</param>
    /// <param name="keys">An array of column names used for building restriction filters.</param>
    /// <param name="values">An array of filter values corresponding to the keys.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> populated with the query results.</returns>
    private async Task<MySqlSchemaCollection> QueryAsync(string tableName, string initialWhere, string[] keys, string[] values, CancellationToken cancellationToken = default)
    {
      string sql = BuildQueryString(tableName, initialWhere, keys, values);
      return await GetTableAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Constructs the SQL query string for querying an INFORMATION_SCHEMA table, including WHERE clause and ordering for COLUMNS table.
    /// Intended for internal use in schema retrieval.
    /// </summary>
    /// <param name="tableName">The name of the INFORMATION_SCHEMA table to query (e.g., "TABLES", "COLUMNS").</param>
    /// <param name="initialWhere">An initial WHERE clause fragment to append.</param>
    /// <param name="keys">An array of column names used for building restriction filters.</param>
    /// <param name="values">An array of filter values corresponding to the keys.</param>
    /// <returns>The complete SQL query string.</returns>
    private string BuildQueryString(string tableName, string initialWhere, string[] keys, string[] values)
    {
      StringBuilder query = new StringBuilder("SELECT * FROM INFORMATION_SCHEMA.");
      query.Append(tableName);

      string where = GetWhereClause(initialWhere, keys, values);

      if (where.Length > 0)
        query.AppendFormat(CultureInfo.InvariantCulture, " WHERE {0}", where);

      if (tableName.Equals("COLUMNS", StringComparison.OrdinalIgnoreCase))
        query.Append(" ORDER BY ORDINAL_POSITION");

      return query.ToString();
    }

    /// <summary>
    /// Executes a SQL query and populates a MySqlSchemaCollection from the results.
    /// Dynamically adds columns based on the reader and reads all rows. Intended for internal schema queries.
    /// </summary>
    /// <param name="sql">The SQL query to execute against the connection.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> populated with the query results, including columns and rows.</returns>
    private MySqlSchemaCollection GetTable(string sql, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection c = new MySqlSchemaCollection();
      using MySqlCommand cmd = new MySqlCommand(sql, connection);
      MySqlDataReader reader = cmd.ExecuteReader(default, cancellationToken);

      // add columns
      for (int i = 0; i < reader.FieldCount; i++)
        c.AddColumn(reader.GetName(i), reader.GetFieldType(i));

      using (reader)
      {
        while (reader.Read(cancellationToken))
        {
          MySqlSchemaRow row = c.AddRow();
          for (int i = 0; i < reader.FieldCount; i++)
            row[i] = reader.GetValue(i);
        }
      }

      return c;
    }

    /// <summary>
    /// Asynchronously executes a SQL query and populates a MySqlSchemaCollection from the results.
    /// Dynamically adds columns based on the reader and reads all rows. Intended for internal schema queries.
    /// </summary>
    /// <param name="sql">The SQL query to execute against the connection.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> populated with the query results, including columns and rows.</returns>
    private async Task<MySqlSchemaCollection> GetTableAsync(string sql, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection c = new MySqlSchemaCollection();
      using MySqlCommand cmd = new MySqlCommand(sql, connection);
      MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, cancellationToken).ConfigureAwait(false);

      // add columns
      for (int i = 0; i < reader.FieldCount; i++)
        c.AddColumn(reader.GetName(i), reader.GetFieldType(i));

      using (reader)
      {
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
          MySqlSchemaRow row = c.AddRow();
          for (int i = 0; i < reader.FieldCount; i++)
            row[i] = reader.GetValue(i);
        }
      }

      return c;
    }

    /// <summary>
    /// Retrieves metadata about foreign keys in the database using <see cref="BuildForeignKeysSql"/>.
    /// For MySQL 5.1.16+, queries INFORMATION_SCHEMA with JOINs between REFERENTIAL_CONSTRAINTS and KEY_COLUMN_USAGE, applying restrictions. Falls back to base for older versions.
    /// </summary>
    /// <param name="restrictions">An array of filter values for constraint schema, table name, and constraint name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> containing foreign key information.</returns>
    public override MySqlSchemaCollection GetForeignKeys(string[] restrictions, CancellationToken cancellationToken = default)
    {
      if (!connection.driver.Version.isAtLeast(5, 1, 16))
        return base.GetForeignKeys(restrictions, cancellationToken);

      string sql = BuildForeignKeysSql(restrictions);

      return GetTable(sql, cancellationToken);
    }

    /// <summary>
    /// Asynchronously retrieves metadata about foreign keys in the database using <see cref="BuildForeignKeysSql"/>.
    /// For MySQL 5.1.16+, queries INFORMATION_SCHEMA with JOINs between REFERENTIAL_CONSTRAINTS and KEY_COLUMN_USAGE, applying restrictions. Falls back to base for older versions.
    /// </summary>
    /// <param name="restrictions">An array of filter values for constraint schema, table name, and constraint name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> containing foreign key information.</returns>
    public override async Task<MySqlSchemaCollection> GetForeignKeysAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      if (!connection.driver.Version.isAtLeast(5, 1, 16))
        return await base.GetForeignKeysAsync(restrictions, cancellationToken).ConfigureAwait(false);

      string sql = BuildForeignKeysSql(restrictions);

      return await GetTableAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves metadata about the columns involved in foreign keys.
    /// For MySQL 5.0.6+, queries INFORMATION_SCHEMA.KEY_COLUMN_USAGE where referenced_table_name is not null, applying restrictions. Falls back to base for older versions.
    /// </summary>
    /// <param name="restrictions">An array of filter values for constraint schema, table name, and constraint name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>A <see cref="MySqlSchemaCollection"/> containing foreign key column information.</returns>
    public override MySqlSchemaCollection GetForeignKeyColumns(string[] restrictions, CancellationToken cancellationToken = default)
    {
      if (!connection.driver.Version.isAtLeast(5, 0, 6))
        return base.GetForeignKeyColumns(restrictions, cancellationToken);

      string sql = @"SELECT kcu.* FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                WHERE kcu.referenced_table_name IS NOT NULL";

      StringBuilder where = new StringBuilder();
      if (restrictions.Length >= 2 && !String.IsNullOrEmpty(restrictions[1]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND kcu.constraint_schema LIKE '{0}'", restrictions[1]);
      if (restrictions.Length >= 3 && !String.IsNullOrEmpty(restrictions[2]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND kcu.table_name LIKE '{0}'", restrictions[2]);
      if (restrictions.Length >= 4 && !String.IsNullOrEmpty(restrictions[3]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND kcu.constraint_name LIKE '{0}'", restrictions[3]);

      sql += where.ToString();

      return GetTable(sql, cancellationToken);
    }

    /// <summary>
    /// Constructs the SQL query for retrieving foreign keys from INFORMATION_SCHEMA, including JOIN and WHERE clauses based on restrictions.
    /// Intended for internal use in schema retrieval for MySQL 5.1.16+.
    /// </summary>
    /// <param name="restrictions">An array of filter values for constraint schema, table name, and constraint name.</param>
    /// <returns>The complete SQL query string.</returns>
    private string BuildForeignKeysSql(string[] restrictions)
    {
      string sql = @"SELECT rc.constraint_catalog, rc.constraint_schema,
                rc.constraint_name, kcu.table_catalog, kcu.table_schema, rc.table_name,
                rc.match_option, rc.update_rule, rc.delete_rule, 
                NULL as referenced_table_catalog,
                kcu.referenced_table_schema, rc.referenced_table_name 
                FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON 
                kcu.constraint_catalog <=> rc.constraint_catalog AND
                kcu.constraint_schema <=> rc.constraint_schema AND 
                kcu.constraint_name <=> rc.constraint_name 
                WHERE 1=1 AND kcu.ORDINAL_POSITION=1";

      StringBuilder where = new StringBuilder();
      if (restrictions.Length >= 2 && !String.IsNullOrEmpty(restrictions[1]))
        where.AppendFormat(CultureInfo.InvariantCulture, " AND rc.constraint_schema LIKE '{0}'", restrictions[1]);
      
      if (restrictions.Length >= 3 && !String.IsNullOrEmpty(restrictions[2]))
        where.AppendFormat(CultureInfo.InvariantCulture, " AND rc.table_name LIKE '{0}'", restrictions[2]);
      
      if (restrictions.Length >= 4 && !String.IsNullOrEmpty(restrictions[3]))
        where.AppendFormat(CultureInfo.InvariantCulture, " AND rc.constraint_name LIKE '{0}'", restrictions[2]);

      sql += where.ToString();

      return sql;
    }

    /// <summary>
    /// Asynchronously retrieves metadata about the columns involved in foreign keys.
    /// For MySQL 5.0.6+, queries INFORMATION_SCHEMA.KEY_COLUMN_USAGE where referenced_table_name is not null, applying restrictions. Falls back to base for older versions.
    /// </summary>
    /// <param name="restrictions">An array of filter values for constraint schema, table name, and constraint name.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, yielding a <see cref="MySqlSchemaCollection"/> containing foreign key column information.</returns>
    public override async Task<MySqlSchemaCollection> GetForeignKeyColumnsAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      if (!connection.driver.Version.isAtLeast(5, 0, 6))
        return await base.GetForeignKeyColumnsAsync(restrictions, cancellationToken).ConfigureAwait(false);

      string sql = @"SELECT kcu.* FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                WHERE kcu.referenced_table_name IS NOT NULL";

      StringBuilder where = new StringBuilder();
      if (restrictions.Length >= 2 && !String.IsNullOrEmpty(restrictions[1]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND kcu.constraint_schema LIKE '{0}'", restrictions[1]);
      if (restrictions.Length >= 3 && !String.IsNullOrEmpty(restrictions[2]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND kcu.table_name LIKE '{0}'", restrictions[2]);
      if (restrictions.Length >= 4 && !String.IsNullOrEmpty(restrictions[3]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND kcu.constraint_name LIKE '{0}'", restrictions[3]);

      sql += where.ToString();

      return await GetTableAsync(sql, cancellationToken).ConfigureAwait(false);
    }
  }
}
