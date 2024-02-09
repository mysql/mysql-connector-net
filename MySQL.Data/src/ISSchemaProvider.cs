// Copyright © 2004, 2024, Oracle and/or its affiliates.
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

    public override async Task<MySqlSchemaCollection> GetDatabasesAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[1];
      keys[0] = "SCHEMA_NAME";
      MySqlSchemaCollection dt = await QueryAsync("SCHEMATA", "", keys, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      dt.Columns[1].Name = "database_name";
      dt.Name = "Databases";
      return dt;
    }

    public override async Task<MySqlSchemaCollection> GetTablesAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[4];
      keys[0] = "TABLE_CATALOG";
      keys[1] = "TABLE_SCHEMA";
      keys[2] = "TABLE_NAME";
      keys[3] = "TABLE_TYPE";
      MySqlSchemaCollection dt = await QueryAsync("TABLES", "TABLE_TYPE != 'VIEW'", keys, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      dt.Name = "Tables";
      return dt;
    }

    public override async Task<MySqlSchemaCollection> GetColumnsAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[4];
      keys[0] = "TABLE_CATALOG";
      keys[1] = "TABLE_SCHEMA";
      keys[2] = "TABLE_NAME";
      keys[3] = "COLUMN_NAME";
      MySqlSchemaCollection dt = await QueryAsync("COLUMNS", null, keys, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      dt.RemoveColumn("CHARACTER_OCTET_LENGTH");
      dt.Name = "Columns";
      QuoteDefaultValues(dt);
      return dt;
    }

    private async Task<MySqlSchemaCollection> GetViewsAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[3];
      keys[0] = "TABLE_CATALOG";
      keys[1] = "TABLE_SCHEMA";
      keys[2] = "TABLE_NAME";
      MySqlSchemaCollection dt = await QueryAsync("VIEWS", null, keys, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      dt.Name = "Views";
      return dt;
    }

    private async Task<MySqlSchemaCollection> GetViewColumnsAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      StringBuilder where = new StringBuilder();
      StringBuilder sql = new StringBuilder(
          "SELECT C.* FROM information_schema.columns C");
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
      MySqlSchemaCollection dt = await GetTableAsync(sql.ToString(), execAsync, cancellationToken).ConfigureAwait(false);
      dt.Name = "ViewColumns";
      dt.Columns[0].Name = "VIEW_CATALOG";
      dt.Columns[1].Name = "VIEW_SCHEMA";
      dt.Columns[2].Name = "VIEW_NAME";
      QuoteDefaultValues(dt);
      return dt;
    }

    private async Task<MySqlSchemaCollection> GetTriggersAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[4];
      keys[0] = "TRIGGER_CATALOG";
      keys[1] = "TRIGGER_SCHEMA";
      keys[2] = "EVENT_OBJECT_TABLE";
      keys[3] = "TRIGGER_NAME";
      MySqlSchemaCollection dt = await QueryAsync("TRIGGERS", null, keys, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      dt.Name = "Triggers";
      return dt;
    }

    /// <summary>
    /// Return schema information about procedures and functions
    /// Restrictions supported are:
    /// schema, name, type
    /// </summary>
    /// <param name="restrictions"></param>
    /// <param name="execAsync">Boolean that indicates if the function will be executed asynchronously.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<MySqlSchemaCollection> GetProceduresAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[4];
      keys[0] = "ROUTINE_CATALOG";
      keys[1] = "ROUTINE_SCHEMA";
      keys[2] = "ROUTINE_NAME";
      keys[3] = "ROUTINE_TYPE";

      MySqlSchemaCollection dt = await QueryAsync("ROUTINES", null, keys, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      dt.Name = "Procedures";
      return dt;
    }

    private async Task<MySqlSchemaCollection> GetParametersForRoutineFromexecAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[5];
      keys[0] = "SPECIFIC_CATALOG";
      keys[1] = "SPECIFIC_SCHEMA";
      keys[2] = "SPECIFIC_NAME";
      keys[3] = "ROUTINE_TYPE";
      keys[4] = "PARAMETER_NAME";

      StringBuilder sql = new StringBuilder(@"SELECT * FROM INFORMATION_SCHEMA.PARAMETERS");
      // now get our where clause and append it if there is one
      string where = GetWhereClause(null, keys, restrictions);
      if (!String.IsNullOrEmpty(where))
        sql.AppendFormat(CultureInfo.InvariantCulture, " WHERE {0}", where);

      MySqlSchemaCollection coll = await QueryCollectionAsync("parameters", sql.ToString(), execAsync, cancellationToken).ConfigureAwait(false);

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
    /// Return schema information about parameters for procedures and functions
    /// Restrictions supported are:
    /// schema, name, type, parameter name
    /// </summary>
    public virtual async Task<MySqlSchemaCollection> GetProcedureParametersAsync(string[] restrictions,
        MySqlSchemaCollection routines, bool execAsync, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection parms = null;

      if (routines == null || routines.Rows.Count == 0)
      {
        parms = await GetParametersForRoutineFromexecAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      }
      else foreach (MySqlSchemaRow routine in routines.Rows)
        {
          if (restrictions != null && restrictions.Length >= 3)
            restrictions[2] = routine["ROUTINE_NAME"].ToString();

          parms = await GetParametersForRoutineFromexecAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
        }
      parms.Name = "Procedure Parameters";
      return parms;
    }

    protected override async Task<MySqlSchemaCollection> GetSchemaInternalAsync(string collection, string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection dt = await base.GetSchemaInternalAsync(collection, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      if (dt != null)
        return dt;

      switch (collection)
      {
        case "VIEWS":
          return await GetViewsAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
        case "PROCEDURES":
          return await GetProceduresAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
        case "PROCEDURE PARAMETERS":
          return await GetProcedureParametersAsync(restrictions, null, execAsync, cancellationToken).ConfigureAwait(false);
        case "TRIGGERS":
          return await GetTriggersAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
        case "VIEWCOLUMNS":
          return await GetViewColumnsAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
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

    private async Task<MySqlSchemaCollection> QueryAsync(string tableName, string initialWhere, string[] keys, string[] values, bool execAsync, CancellationToken cancellationToken = default)
    {
      StringBuilder query = new StringBuilder("SELECT * FROM INFORMATION_SCHEMA.");
      query.Append(tableName);

      string where = GetWhereClause(initialWhere, keys, values);

      if (where.Length > 0)
        query.AppendFormat(CultureInfo.InvariantCulture, " WHERE {0}", where);

      if (tableName.Equals("COLUMNS", StringComparison.OrdinalIgnoreCase))
        query.Append(" ORDER BY ORDINAL_POSITION");

      return await GetTableAsync(query.ToString(), execAsync, cancellationToken).ConfigureAwait(false);
    }

    private async Task<MySqlSchemaCollection> GetTableAsync(string sql, bool execAsync, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection c = new MySqlSchemaCollection();
      using MySqlCommand cmd = new MySqlCommand(sql, connection);
      MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, execAsync, cancellationToken).ConfigureAwait(false);

      // add columns
      for (int i = 0; i < reader.FieldCount; i++)
        c.AddColumn(reader.GetName(i), reader.GetFieldType(i));

      using (reader)
      {
        while (await reader.ReadAsync(execAsync, cancellationToken).ConfigureAwait(false))
        {
          MySqlSchemaRow row = c.AddRow();
          for (int i = 0; i < reader.FieldCount; i++)
            row[i] = reader.GetValue(i);
        }
      }

      return c;
    }

    public override async Task<MySqlSchemaCollection> GetForeignKeysAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      if (!connection.driver.Version.isAtLeast(5, 1, 16))
        return await base.GetForeignKeysAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);

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
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND rc.constraint_schema LIKE '{0}'", restrictions[1]);
      if (restrictions.Length >= 3 && !String.IsNullOrEmpty(restrictions[2]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND rc.table_name LIKE '{0}'", restrictions[2]);
      if (restrictions.Length >= 4 && !String.IsNullOrEmpty(restrictions[3]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND rc.constraint_name LIKE '{0}'", restrictions[2]);

      sql += where.ToString();

      return await GetTableAsync(sql, execAsync, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<MySqlSchemaCollection> GetForeignKeyColumnsAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      if (!connection.driver.Version.isAtLeast(5, 0, 6))
        return await base.GetForeignKeyColumnsAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);

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

      return await GetTableAsync(sql, execAsync, cancellationToken).ConfigureAwait(false);
    }
  }
}
