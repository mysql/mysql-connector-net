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
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  internal class SchemaProvider
  {
    protected MySqlConnection connection;
    public static string MetaCollection = "MetaDataCollections";

    public SchemaProvider(MySqlConnection connectionToUse)
    {
      connection = connectionToUse;
    }

    public MySqlSchemaCollection GetSchema(string collection, String[] restrictions) => GetSchema(collection, restrictions, default);

    /// <summary>
    /// Retrieves schema information for the specified collection, validating the connection state and normalizing the collection name.
    /// This method serves as the entry point for synchronous schema queries, delegating to the virtual implementation for actual data retrieval.
    /// </summary>
    /// <param name="collection">The name of the schema collection to retrieve (e.g., "Tables", "Columns"). Case-insensitive; normalized to uppercase.</param>
    /// <param name="restrictions">An array of string restrictions to filter the results (specific meaning depends on the collection).</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>A MySqlSchemaCollection containing the schema information for the specified collection.</returns>
    /// <exception cref="MySqlException">Thrown if the connection is not open.</exception>
    /// <exception cref="ArgumentException">Thrown if the collection name is invalid (returns null from the virtual method).</exception>
    public MySqlSchemaCollection GetSchema(string collection, String[] restrictions, CancellationToken cancellationToken = default)
    {
      if (connection.State != ConnectionState.Open)
        throw new MySqlException("GetSchema can only be called on an open connection.");

      collection = StringUtility.ToUpperInvariant(collection);
      MySqlSchemaCollection c = GetSchemaInternal(collection, restrictions, cancellationToken);

      if (c == null)
        throw new ArgumentException("Invalid collection name");
      return c;
    }

    /// <summary>
    /// Asynchronously retrieves schema information for the specified collection, validating the connection state and normalizing the collection name.
    /// This method serves as the entry point for asynchronous schema queries, delegating to the virtual implementation for actual data retrieval.
    /// </summary>
    /// <param name="collection">The name of the schema collection to retrieve (e.g., "Tables", "Columns"). Case-insensitive; normalized to uppercase.</param>
    /// <param name="restrictions">An array of string restrictions to filter the results (specific meaning depends on the collection).</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation, containing a MySqlSchemaCollection with the schema information.</returns>
    /// <exception cref="MySqlException">Thrown if the connection is not open.</exception>
    /// <exception cref="ArgumentException">Thrown if the collection name is invalid (returns null from the virtual method).</exception>
    public async Task<MySqlSchemaCollection> GetSchemaAsync(string collection, String[] restrictions, CancellationToken cancellationToken = default)
    {
      if (connection.State != ConnectionState.Open)
        throw new MySqlException("GetSchema can only be called on an open connection.");

      collection = StringUtility.ToUpperInvariant(collection);
      MySqlSchemaCollection c = await GetSchemaInternalAsync(collection, restrictions, cancellationToken).ConfigureAwait(false);

      if (c == null)
        throw new ArgumentException("Invalid collection name");
      return c;
    }

    /// <summary>
    /// Retrieves a collection of available databases (schemas) on the server.
    /// Uses SHOW DATABASES with optional LIKE filter based on restrictions[0] if lower_case_table_names is 0 (case-sensitive).
    /// </summary>
    /// <param name="restrictions">Array where restrictions[0] filters database names (LIKE or regex).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A MySqlSchemaCollection containing database schema information.</returns>
    public virtual MySqlSchemaCollection GetDatabases(string[] restrictions, CancellationToken cancellationToken = default)
    {
      Regex regex = null;
      int caseSetting = Int32.Parse(connection.driver.Property("lower_case_table_names"), CultureInfo.InvariantCulture);

      string sql = "SHOW DATABASES";

      // if lower_case_table_names is zero, then case lookup should be sensitive
      // so we can use LIKE to do the matching.
      if (caseSetting == 0)
      {
        if (restrictions != null && restrictions.Length >= 1)
          sql = sql + " LIKE '" + restrictions[0] + "'";
      }

      MySqlSchemaCollection c = QueryCollection("Databases", sql, cancellationToken);

      if (caseSetting != 0 && restrictions != null && restrictions.Length >= 1 && restrictions[0] != null)
        regex = new Regex(restrictions[0], RegexOptions.IgnoreCase);

      MySqlSchemaCollection c2 = new MySqlSchemaCollection("Databases");
      c2.AddColumn("CATALOG_NAME", typeof(string));
      c2.AddColumn("SCHEMA_NAME", typeof(string));

      foreach (MySqlSchemaRow row in c.Rows)
      {
        if (regex != null && !regex.Match(row[0].ToString()).Success) continue;
        MySqlSchemaRow newRow = c2.AddRow();
        newRow[1] = row[0];
      }
      return c2;
    }

    /// <summary>
    /// Asynchronously retrieves a collection of available databases (schemas) on the server.
    /// Uses SHOW DATABASES with optional LIKE filter based on restrictions[0] if lower_case_table_names is 0 (case-sensitive).
    /// </summary>
    /// <param name="restrictions">Array where restrictions[0] filters database names (LIKE or regex).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning a MySqlSchemaCollection with database schema information.</returns>
    public virtual async Task<MySqlSchemaCollection> GetDatabasesAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      Regex regex = null;
      int caseSetting = Int32.Parse(connection.driver.Property("lower_case_table_names"), CultureInfo.InvariantCulture);

      string sql = "SHOW DATABASES";

      // if lower_case_table_names is zero, then case lookup should be sensitive
      // so we can use LIKE to do the matching.
      if (caseSetting == 0)
      {
        if (restrictions != null && restrictions.Length >= 1)
          sql = sql + " LIKE '" + restrictions[0] + "'";
      }

      MySqlSchemaCollection c = await QueryCollectionAsync("Databases", sql, cancellationToken).ConfigureAwait(false);

      if (caseSetting != 0 && restrictions != null && restrictions.Length >= 1 && restrictions[0] != null)
        regex = new Regex(restrictions[0], RegexOptions.IgnoreCase);

      MySqlSchemaCollection c2 = new MySqlSchemaCollection("Databases");
      c2.AddColumn("CATALOG_NAME", typeof(string));
      c2.AddColumn("SCHEMA_NAME", typeof(string));

      foreach (MySqlSchemaRow row in c.Rows)
      {
        if (regex != null && !regex.Match(row[0].ToString()).Success) continue;
        MySqlSchemaRow newRow = c2.AddRow();
        newRow[1] = row[0];
      }
      return c2;
    }

    /// <summary>
    /// Adds the standard columns to a Tables schema collection.
    /// </summary>
    /// <param name="c">The MySqlSchemaCollection to add columns to.</param>
    private static void AddTablesColumns(MySqlSchemaCollection c)
    {
      c.AddColumn("TABLE_CATALOG", typeof(string));
      c.AddColumn("TABLE_SCHEMA", typeof(string));
      c.AddColumn("TABLE_NAME", typeof(string));
      c.AddColumn("TABLE_TYPE", typeof(string));
      c.AddColumn("ENGINE", typeof(string));
      c.AddColumn("VERSION", typeof(ulong));
      c.AddColumn("ROW_FORMAT", typeof(string));
      c.AddColumn("TABLE_ROWS", typeof(ulong));
      c.AddColumn("AVG_ROW_LENGTH", typeof(ulong));
      c.AddColumn("DATA_LENGTH", typeof(ulong));
      c.AddColumn("MAX_DATA_LENGTH", typeof(ulong));
      c.AddColumn("INDEX_LENGTH", typeof(ulong));
      c.AddColumn("DATA_FREE", typeof(ulong));
      c.AddColumn("AUTO_INCREMENT", typeof(ulong));
      c.AddColumn("CREATE_TIME", typeof(DateTime));
      c.AddColumn("UPDATE_TIME", typeof(DateTime));
      c.AddColumn("CHECK_TIME", typeof(DateTime));
      c.AddColumn("TABLE_COLLATION", typeof(string));
      c.AddColumn("CHECKSUM", typeof(ulong));
      c.AddColumn("CREATE_OPTIONS", typeof(string));
      c.AddColumn("TABLE_COMMENT", typeof(string));
    }

    /// <summary>
    /// Prepares the database restriction array for table retrieval.
    /// </summary>
    /// <param name="restrictions">The original restrictions array.</param>
    /// <returns>The prepared dbRestriction array with schema in index 0.</returns>
    private static string[] PrepareDbRestriction(string[] restrictions)
    {
      string[] dbRestriction = new string[4];
      if (restrictions != null && restrictions.Length >= 2)
        dbRestriction[0] = restrictions[1];

      return dbRestriction;
    }

    /// <summary>
    /// Populates the tables collection by iterating over databases and finding tables in each.
    /// </summary>
    /// <param name="c">The tables schema collection to populate.</param>
    /// <param name="restrictions">The original restrictions array.</param>
    /// <param name="dbRestriction">The mutable db restriction array.</param>
    /// <param name="databases">The collection of databases.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private void PopulateTables(MySqlSchemaCollection c, string[] restrictions, string[] dbRestriction, MySqlSchemaCollection databases, CancellationToken cancellationToken)
    {
      if (restrictions != null)
        Array.Copy(restrictions, dbRestriction, Math.Min(dbRestriction.Length, restrictions.Length));

      foreach (MySqlSchemaRow row in databases.Rows)
      {
        dbRestriction[1] = row["SCHEMA_NAME"].ToString();
        FindTables(c, dbRestriction, cancellationToken);
      }
    }

    /// <summary>
    /// Asynchronously populates the tables collection by iterating over databases and finding tables in each.
    /// </summary>
    /// <param name="c">The tables schema collection to populate.</param>
    /// <param name="restrictions">The original restrictions array.</param>
    /// <param name="dbRestriction">The mutable db restriction array.</param>
    /// <param name="databases">The collection of databases.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task for the asynchronous population.</returns>
    private async Task PopulateTablesAsync(MySqlSchemaCollection c, string[] restrictions, string[] dbRestriction, MySqlSchemaCollection databases, CancellationToken cancellationToken)
    {
      if (restrictions != null)
        Array.Copy(restrictions, dbRestriction, Math.Min(dbRestriction.Length, restrictions.Length));

      foreach (MySqlSchemaRow row in databases.Rows)
      {
        dbRestriction[1] = row["SCHEMA_NAME"].ToString();
        await FindTablesAsync(c, dbRestriction, cancellationToken).ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Retrieves a collection of tables across specified databases.
    /// </summary>
    /// <param name="restrictions">Array: [0] unused, [1] schema filter, [2] table name filter, [3] table type filter (ignored here).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A MySqlSchemaCollection with comprehensive table information.</returns>
    public virtual MySqlSchemaCollection GetTables(string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection c = new MySqlSchemaCollection("Tables");
      AddTablesColumns(c);
      string[] dbRestriction = PrepareDbRestriction(restrictions);
      MySqlSchemaCollection databases = GetDatabases(dbRestriction, cancellationToken);
      PopulateTables(c, restrictions, dbRestriction, databases, cancellationToken);

      return c;
    }

    /// <summary>
    /// Asynchronously retrieves a collection of tables across specified databases.
    /// </summary>
    /// <param name="restrictions">Array: [0] unused, [1] schema filter, [2] table name filter, [3] table type filter (ignored here).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning a MySqlSchemaCollection with comprehensive table information.</returns>
    public virtual async Task<MySqlSchemaCollection> GetTablesAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection c = new MySqlSchemaCollection("Tables");
      AddTablesColumns(c);
      string[] dbRestriction = PrepareDbRestriction(restrictions);
      MySqlSchemaCollection databases = await GetDatabasesAsync(dbRestriction, cancellationToken).ConfigureAwait(false);
      await PopulateTablesAsync(c, restrictions, dbRestriction, databases, cancellationToken).ConfigureAwait(false);

      return c;
    }

    protected void QuoteDefaultValues(MySqlSchemaCollection schemaCollection)
    {
      if (schemaCollection == null) return;
      if (!schemaCollection.ContainsColumn("COLUMN_DEFAULT")) return;

      foreach (MySqlSchemaRow row in schemaCollection.Rows)
      {
        object defaultValue = row["COLUMN_DEFAULT"];
        if (MetaData.IsTextType(row["DATA_TYPE"].ToString()))
          row["COLUMN_DEFAULT"] = String.Format("{0}", defaultValue);
      }
    }

    /// <summary>
    /// Adds the standard columns to a Columns schema collection.
    /// </summary>
    /// <param name="c">The MySqlSchemaCollection to add columns to.</param>
    private static void AddColumnsToCollection(MySqlSchemaCollection c)
    {
      c.AddColumn("TABLE_CATALOG", typeof(string));
      c.AddColumn("TABLE_SCHEMA", typeof(string));
      c.AddColumn("TABLE_NAME", typeof(string));
      c.AddColumn("COLUMN_NAME", typeof(string));
      c.AddColumn("ORDINAL_POSITION", typeof(ulong));
      c.AddColumn("COLUMN_DEFAULT", typeof(string));
      c.AddColumn("IS_NULLABLE", typeof(string));
      c.AddColumn("DATA_TYPE", typeof(string));
      c.AddColumn("CHARACTER_MAXIMUM_LENGTH", typeof(ulong));
      c.AddColumn("CHARACTER_OCTET_LENGTH", typeof(ulong));
      c.AddColumn("NUMERIC_PRECISION", typeof(ulong));
      c.AddColumn("NUMERIC_SCALE", typeof(ulong));
      c.AddColumn("CHARACTER_SET_NAME", typeof(string));
      c.AddColumn("COLLATION_NAME", typeof(string));
      c.AddColumn("COLUMN_TYPE", typeof(string));
      c.AddColumn("COLUMN_KEY", typeof(string));
      c.AddColumn("EXTRA", typeof(string));
      c.AddColumn("PRIVILEGES", typeof(string));
      c.AddColumn("COLUMN_COMMENT", typeof(string));
      c.AddColumn("GENERATION_EXPRESSION", typeof(string));
    }

    /// <summary>
    /// Extracts the column name restriction and modifies restrictions[3] to null.
    /// </summary>
    /// <param name="restrictions">The original restrictions array.</param>
    /// <returns>The extracted columnName or null.</returns>
    private static string ExtractColumnRestriction(string[] restrictions)
    {
      string columnName = null;
      if (restrictions != null && restrictions.Length == 4)
      {
        columnName = restrictions[3];
        restrictions[3] = null;
      }
      return columnName;
    }

    /// <summary>
    /// Retrieves a collection of columns for tables in the specified schema(s).
    /// </summary>
    /// <param name="restrictions">Array: [0] unused, [1] schema, [2] table, [3] column name filter.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A MySqlSchemaCollection with detailed column information.</returns>
    public virtual MySqlSchemaCollection GetColumns(string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection c = new MySqlSchemaCollection("Columns");
      AddColumnsToCollection(c);
      string columnName = ExtractColumnRestriction(restrictions);
      MySqlSchemaCollection tables = GetTables(restrictions, cancellationToken);
      foreach (MySqlSchemaRow row in tables.Rows)
        LoadTableColumns(c, row["TABLE_SCHEMA"].ToString(), row["TABLE_NAME"].ToString(), columnName, cancellationToken);
      QuoteDefaultValues(c);
      return c;
    }

    /// <summary>
    /// Asynchronously retrieves a collection of columns for tables in the specified schema(s).
    /// </summary>
    /// <param name="restrictions">Array: [0] unused, [1] schema, [2] table, [3] column name filter.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning a MySqlSchemaCollection with detailed column information.</returns>
    public virtual async Task<MySqlSchemaCollection> GetColumnsAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection c = new MySqlSchemaCollection("Columns");
      AddColumnsToCollection(c);
      string columnName = ExtractColumnRestriction(restrictions);
      MySqlSchemaCollection tables = await GetTablesAsync(restrictions, cancellationToken).ConfigureAwait(false);
      foreach (MySqlSchemaRow row in tables.Rows)
        await LoadTableColumnsAsync(c, row["TABLE_SCHEMA"].ToString(), row["TABLE_NAME"].ToString(), columnName, cancellationToken).ConfigureAwait(false);

      QuoteDefaultValues(c);
      return c;
    }

    /// <summary>
    /// Loads columns for a specific table using SHOW FULL COLUMNS, populating the schema collection row-by-row.
    /// Handles optional column name restriction, assigns ordinal positions, extracts values from reader (Field, Type, Null, Key, Default, Extra, Priv, Comment).
    /// Sets generation expression for virtual columns, calls ParseColumnRow for type parsing.
    /// </summary>
    /// <param name="schemaCollection">The collection to add rows to.</param>
    /// <param name="schema">The database schema name.</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="columnRestriction">Optional filter for specific column name.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private void LoadTableColumns(MySqlSchemaCollection schemaCollection, string schema, string tableName, string columnRestriction, CancellationToken cancellationToken = default)
    {
      string sql = String.Format("SHOW FULL COLUMNS FROM `{0}`.`{1}`", schema, tableName);
      using MySqlCommand cmd = new MySqlCommand(sql, connection);

      int pos = 1;
      using (MySqlDataReader reader = cmd.ExecuteReader(default, cancellationToken))
      {
        while (reader.Read(cancellationToken))
        {
          ProcessColumnRow(schemaCollection, reader, schema, tableName, columnRestriction, ref pos);
        }
      }
    }

    /// <summary>
    /// Asynchronously loads columns for a specific table using SHOW FULL COLUMNS, populating the schema collection row-by-row.
    /// Handles optional column name restriction, assigns ordinal positions, extracts values from reader (Field, Type, Null, Key, Default, Extra, Priv, Comment).
    /// Sets generation expression for virtual columns, calls ParseColumnRow for type parsing.
    /// </summary>
    /// <param name="schemaCollection">The collection to add rows to.</param>
    /// <param name="schema">The database schema name.</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="columnRestriction">Optional filter for specific column name.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LoadTableColumnsAsync(MySqlSchemaCollection schemaCollection, string schema, string tableName, string columnRestriction, CancellationToken cancellationToken = default)
    {
      string sql = String.Format("SHOW FULL COLUMNS FROM `{0}`.`{1}`", schema, tableName);
      using MySqlCommand cmd = new MySqlCommand(sql, connection);

      int pos = 1;
      using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, cancellationToken).ConfigureAwait(false))
      {
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
          ProcessColumnRow(schemaCollection, reader, schema, tableName, columnRestriction, ref pos);
        }
      }
    }

    private static void ParseColumnRow(MySqlSchemaRow row)
    {
      // first parse the character set name
      string charset = row["CHARACTER_SET_NAME"].ToString();
      int index = charset.IndexOf('_');
      if (index != -1)
        row["CHARACTER_SET_NAME"] = charset.Substring(0, index);

      // now parse the data type
      string dataType = row["DATA_TYPE"].ToString();
      index = dataType.IndexOf('(');
      if (index == -1)
        return;
      row["DATA_TYPE"] = dataType.Substring(0, index);
      int stop = dataType.IndexOf(')', index);
      string dataLen = dataType.Substring(index + 1, stop - (index + 1));
      string lowerType = row["DATA_TYPE"].ToString().ToLower();
      if (lowerType == "char" || lowerType == "varchar")
        row["CHARACTER_MAXIMUM_LENGTH"] = dataLen;
      else if (lowerType == "real" || lowerType == "decimal")
      {
        string[] lenparts = dataLen.Split(new char[] { ',' });
        row["NUMERIC_PRECISION"] = lenparts[0];
        if (lenparts.Length == 2)
          row["NUMERIC_SCALE"] = lenparts[1];
      }
    }

    /// <summary>
    /// Processes a single column row from the SHOW FULL COLUMNS query result.
    /// Applies the column name restriction filter, creates and populates a new MySqlSchemaRow in the provided collection if the column matches,
    /// sets all standard column metadata fields from the reader, handles virtual column generation expressions, and invokes ParseColumnRow for type parsing.
    /// This method enables code reuse between the synchronous and asynchronous LoadTableColumns implementations without mixing synchronous and asynchronous operations.
    /// </summary>
    /// <param name="schemaCollection">The MySqlSchemaCollection to which the processed row is added if it passes the restriction filter.</param>
    /// <param name="reader">The MySqlDataReader positioned at the current SHOW FULL COLUMNS row containing column details.</param>
    /// <param name="schema">The database (schema) name associated with the table.</param>
    /// <param name="tableName">The name of the table being queried for columns.</param>
    /// <param name="columnRestriction">Optional column name filter; if provided and the current column name does not match, the row is skipped.</param>
    /// <param name="pos">Reference to the ordinal position counter; incremented only when a row is successfully added.</param>
    private void ProcessColumnRow(MySqlSchemaCollection schemaCollection, MySqlDataReader reader, string schema, string tableName, string columnRestriction, ref int pos)
    {
      string colName = reader.GetString(0);
      if (columnRestriction != null && colName != columnRestriction)
        return;

      MySqlSchemaRow row = schemaCollection.AddRow();
      row["TABLE_CATALOG"] = DBNull.Value;
      row["TABLE_SCHEMA"] = schema;
      row["TABLE_NAME"] = tableName;
      row["COLUMN_NAME"] = colName;
      row["ORDINAL_POSITION"] = pos++;
      row["COLUMN_DEFAULT"] = reader.GetValue(5);
      row["IS_NULLABLE"] = reader.GetString(3);
      row["DATA_TYPE"] = reader.GetString(1);
      row["CHARACTER_MAXIMUM_LENGTH"] = DBNull.Value;
      row["CHARACTER_OCTET_LENGTH"] = DBNull.Value;
      row["NUMERIC_PRECISION"] = DBNull.Value;
      row["NUMERIC_SCALE"] = DBNull.Value;
      row["CHARACTER_SET_NAME"] = reader.GetValue(2);
      row["COLLATION_NAME"] = row["CHARACTER_SET_NAME"];
      row["COLUMN_TYPE"] = reader.GetString(1);
      row["COLUMN_KEY"] = reader.GetString(4);
      row["EXTRA"] = reader.GetString(6);
      row["PRIVILEGES"] = reader.GetString(7);
      row["COLUMN_COMMENT"] = reader.GetString(8);
      row["GENERATION_EXPRESSION"] = reader.GetString(6).Contains("VIRTUAL") ? reader.GetString(9) : string.Empty;
      ParseColumnRow(row);
    }

    /// <summary>
    /// Creates and configures a MySqlSchemaCollection for storing index information.
    /// Includes standard columns such as INDEX_CATALOG, INDEX_SCHEMA, INDEX_NAME, TABLE_NAME,
    /// UNIQUE, PRIMARY, TYPE, and COMMENT.
    /// </summary>
    /// <returns>A new MySqlSchemaCollection named "Indexes" with predefined columns.</returns>
    private static MySqlSchemaCollection CreateIndexesCollection()
    {
      MySqlSchemaCollection dt = new MySqlSchemaCollection("Indexes");
      dt.AddColumn("INDEX_CATALOG", typeof(string));
      dt.AddColumn("INDEX_SCHEMA", typeof(string));
      dt.AddColumn("INDEX_NAME", typeof(string));
      dt.AddColumn("TABLE_NAME", typeof(string));
      dt.AddColumn("UNIQUE", typeof(bool));
      dt.AddColumn("PRIMARY", typeof(bool));
      dt.AddColumn("TYPE", typeof(string));
      dt.AddColumn("COMMENT", typeof(string));

      return dt;
    }

    /// <summary>
    /// Adds qualifying index rows from SHOW INDEX results to the target schema collection.
    /// Filters for primary index entries (SEQ_IN_INDEX == 1) and optional index name restriction from restrictions[3].
    /// Determines uniqueness based on the connection's MySQL version and sets the primary flag if KEY_NAME is "PRIMARY".
    /// This method enables code reuse in the GetIndexes and GetIndexesAsync implementations without mixing synchronous and asynchronous operations.
    /// </summary>
    /// <param name="dt">The target MySqlSchemaCollection ("Indexes") to add rows to.</param>
    /// <param name="indexes">The MySqlSchemaCollection containing the results from QueryCollection or QueryCollectionAsync for SHOW INDEX.</param>
    /// <param name="table">The current MySqlSchemaRow from the tables collection being processed.</param>
    /// <param name="restrictions">The original restrictions array; uses index 3 for optional index name filter.</param>
    private void AddIndexRows(MySqlSchemaCollection dt, MySqlSchemaCollection indexes, MySqlSchemaRow table, string[] restrictions)
    {
      foreach (MySqlSchemaRow index in indexes.Rows)
      {
        if (!index["SEQ_IN_INDEX"].Equals(Convert.ChangeType(1, index["SEQ_IN_INDEX"].GetType())))
          continue;
        if (restrictions != null && restrictions.Length == 4 &&
          restrictions[3] != null &&
          !index["KEY_NAME"].Equals(restrictions[3])) continue;
        MySqlSchemaRow row = dt.AddRow();
        row["INDEX_CATALOG"] = null;
        row["INDEX_SCHEMA"] = table["TABLE_SCHEMA"];
        row["INDEX_NAME"] = index["KEY_NAME"];
        row["TABLE_NAME"] = index["TABLE"];
        row["UNIQUE"] = connection.driver.Version.isAtLeast(8, 0, 1) ?
          Convert.ToInt64(index["NON_UNIQUE"]) == 0 :
          (long)index["NON_UNIQUE"] == 0;
        row["PRIMARY"] = index["KEY_NAME"].Equals("PRIMARY");
        row["TYPE"] = index["INDEX_TYPE"];
        row["COMMENT"] = index["COMMENT"];
      }
    }

    /// <summary>
    /// Retrieves a collection of indexes for base tables in the specified schema(s).
    /// </summary>
    /// <param name="restrictions">Array: [0] unused, [1] schema, [2] table, [3] index name filter.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A MySqlSchemaCollection with index metadata (name, unique, primary, type, comment).</returns>
    public virtual MySqlSchemaCollection GetIndexes(string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection dt = CreateIndexesCollection();
      
      // Get the list of tables first
      int max = restrictions?.Length ?? 4;
      string[] tableRestrictions = new string[Math.Max(max, 4)];
      restrictions?.CopyTo(tableRestrictions, 0);
      tableRestrictions[3] = "BASE TABLE";
      MySqlSchemaCollection tables = GetTables(tableRestrictions, cancellationToken);

      foreach (MySqlSchemaRow table in tables.Rows)
      {
        string sql = String.Format("SHOW INDEX FROM `{0}`.`{1}`",
          MySqlHelper.DoubleQuoteString((string)table["TABLE_SCHEMA"]),
          MySqlHelper.DoubleQuoteString((string)table["TABLE_NAME"]));
        MySqlSchemaCollection indexes = QueryCollection("indexes", sql, cancellationToken);
        AddIndexRows(dt, indexes, table, restrictions);
      }

      return dt;
    }

    /// <summary>
    /// Asynchronously retrieves a collection of indexes for base tables in the specified schema(s).
    /// </summary>
    /// <param name="restrictions">Array: [0] unused, [1] schema, [2] table, [3] index name filter.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning a MySqlSchemaCollection with index metadata (name, unique, primary, type, comment).</returns>
    public virtual async Task<MySqlSchemaCollection> GetIndexesAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection dt = CreateIndexesCollection();
      
      // Get the list of tables first
      int max = restrictions?.Length ?? 4;
      string[] tableRestrictions = new string[Math.Max(max, 4)];
      restrictions?.CopyTo(tableRestrictions, 0);
      tableRestrictions[3] = "BASE TABLE";
      MySqlSchemaCollection tables = await GetTablesAsync(tableRestrictions, cancellationToken).ConfigureAwait(false);

      foreach (MySqlSchemaRow table in tables.Rows)
      {
        string sql = String.Format("SHOW INDEX FROM `{0}`.`{1}`",
          MySqlHelper.DoubleQuoteString((string)table["TABLE_SCHEMA"]),
          MySqlHelper.DoubleQuoteString((string)table["TABLE_NAME"]));
        MySqlSchemaCollection indexes = await QueryCollectionAsync("indexes", sql, cancellationToken).ConfigureAwait(false);
        AddIndexRows(dt, indexes, table, restrictions);
      }

      return dt;
    }

    /// <summary>
    /// Creates and configures a MySqlSchemaCollection for storing index column information.
    /// Includes standard columns such as INDEX_CATALOG, INDEX_SCHEMA, INDEX_NAME, TABLE_NAME,
    /// COLUMN_NAME, ORDINAL_POSITION, and SORT_ORDER.
    /// </summary>
    /// <returns>A new MySqlSchemaCollection named "IndexColumns" with predefined columns.</returns>
    private static MySqlSchemaCollection CreateIndexColumnsCollection()
    {
      MySqlSchemaCollection dt = new MySqlSchemaCollection("IndexColumns");
      dt.AddColumn("INDEX_CATALOG", typeof(string));
      dt.AddColumn("INDEX_SCHEMA", typeof(string));
      dt.AddColumn("INDEX_NAME", typeof(string));
      dt.AddColumn("TABLE_NAME", typeof(string));
      dt.AddColumn("COLUMN_NAME", typeof(string));
      dt.AddColumn("ORDINAL_POSITION", typeof(int));
      dt.AddColumn("SORT_ORDER", typeof(string));

      return dt;
    }

    /// <summary>
    /// Prepares the restrictions array for retrieving only base tables when collecting index columns.
    /// Copies the original restrictions and sets the table type restriction to "BASE TABLE".
    /// Handles variable restriction lengths.
    /// </summary>
    /// <param name="restrictions">The original restrictions array provided to GetIndexColumns.</param>
    /// <returns>A modified string array suitable for passing to GetTables to filter base tables.</returns>
    private static string[] GetTableRestrictionsForIndexColumns(string[] restrictions)
    {
      int max = restrictions == null ? 4 : restrictions.Length;
      string[] tableRestrictions = new string[Math.Max(max, 4)];
      if (restrictions != null)
        restrictions.CopyTo(tableRestrictions, 0);
      tableRestrictions[3] = "BASE TABLE";
      return tableRestrictions;
    }

    /// <summary>
    /// Processes a single row from the SHOW INDEX query result for index columns.
    /// Applies filters for index name (restrictions[3]) and column name (restrictions[4]),
    /// extracts key_name and col_name, and adds a new MySqlSchemaRow to the collection if it passes the filters.
    /// This method enables code reuse between the synchronous and asynchronous GetIndexColumns implementations without mixing operations.
    /// </summary>
    /// <param name="dt">The target MySqlSchemaCollection ("IndexColumns") to add the row to if it passes filters.</param>
    /// <param name="reader">The MySqlDataReader positioned at the current SHOW INDEX row.</param>
    /// <param name="table">The current MySqlSchemaRow from the tables collection being processed.</param>
    /// <param name="restrictions">The original restrictions array; uses indices 3 and 4 for index and column name filters.</param>
    private void ProcessIndexColumnRow(MySqlSchemaCollection dt, MySqlDataReader reader, MySqlSchemaRow table, string[] restrictions)
    {
      string key_name = GetString(reader, reader.GetOrdinal("KEY_NAME"));
      string col_name = GetString(reader, reader.GetOrdinal("COLUMN_NAME"));

      if (restrictions != null)
      {
        if (restrictions.Length >= 4 && restrictions[3] != null && key_name != restrictions[3]) return;
        if (restrictions.Length >= 5 && restrictions[4] != null && col_name != restrictions[4]) return;
      }

      MySqlSchemaRow row = dt.AddRow();
      row["INDEX_CATALOG"] = null;
      row["INDEX_SCHEMA"] = table["TABLE_SCHEMA"];
      row["INDEX_NAME"] = key_name;
      row["TABLE_NAME"] = table["TABLE_NAME"];
      row["COLUMN_NAME"] = col_name;
      row["ORDINAL_POSITION"] = reader.GetValue(reader.GetOrdinal("SEQ_IN_INDEX"));
      row["SORT_ORDER"] = reader.GetValue(reader.GetOrdinal("COLLATION"));
    }

    /// <summary>
    /// Retrieves a collection of index columns for base tables in the specified schema(s).
    /// </summary>
    /// <param name="restrictions">Array of filters: [1] schema name, [2] table name, [3] index name, [4] column name. Indices 0 and beyond unused or as specified.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A MySqlSchemaCollection containing index column information.</returns>
    public virtual MySqlSchemaCollection GetIndexColumns(string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection dt = CreateIndexColumnsCollection();
      string[] tableRestrictions = GetTableRestrictionsForIndexColumns(restrictions);
      MySqlSchemaCollection tables = GetTables(tableRestrictions, cancellationToken);
      foreach (MySqlSchemaRow table in tables.Rows)
      {
        string sql = String.Format("SHOW INDEX FROM `{0}`.`{1}`", table["TABLE_SCHEMA"], table["TABLE_NAME"]);
        using MySqlCommand cmd = new MySqlCommand(sql, connection);
        using (MySqlDataReader reader = cmd.ExecuteReader(default, cancellationToken))
        {
          while (reader.Read(cancellationToken))
          {
            ProcessIndexColumnRow(dt, reader, table, restrictions);
          }
        }
      }

      return dt;
    }

    /// <summary>
    /// Asynchronously retrieves a collection of index columns for base tables in the specified schema(s).
    /// </summary>
    /// <param name="restrictions">Array of filters: [1] schema name, [2] table name, [3] index name, [4] column name. Indices 0 and beyond unused or as specified.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning a MySqlSchemaCollection with index column information.</returns>
    public virtual async Task<MySqlSchemaCollection> GetIndexColumnsAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection dt = CreateIndexColumnsCollection();
      string[] tableRestrictions = GetTableRestrictionsForIndexColumns(restrictions);
      MySqlSchemaCollection tables = await GetTablesAsync(tableRestrictions, cancellationToken).ConfigureAwait(false);
      foreach (MySqlSchemaRow table in tables.Rows)
      {
        string sql = String.Format("SHOW INDEX FROM `{0}`.`{1}`", table["TABLE_SCHEMA"], table["TABLE_NAME"]);
        using MySqlCommand cmd = new MySqlCommand(sql, connection);
        using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, cancellationToken).ConfigureAwait(false))
        {
          while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
          {
            ProcessIndexColumnRow(dt, reader, table, restrictions);
          }
        }
      }

      return dt;
    }

    /// <summary>
    /// Creates a new MySqlSchemaCollection configured for foreign key constraints (non-column version).
    /// Adds the standard columns for the "Foreign Keys" collection excluding column-specific fields.
    /// </summary>
    /// <returns>The configured MySqlSchemaCollection.</returns>
    private static MySqlSchemaCollection CreateForeignKeysCollection()
    {
      MySqlSchemaCollection dt = new MySqlSchemaCollection("Foreign Keys");
      dt.AddColumn("CONSTRAINT_CATALOG", typeof(string));
      dt.AddColumn("CONSTRAINT_SCHEMA", typeof(string));
      dt.AddColumn("CONSTRAINT_NAME", typeof(string));
      dt.AddColumn("TABLE_CATALOG", typeof(string));
      dt.AddColumn("TABLE_SCHEMA", typeof(string));
      dt.AddColumn("TABLE_NAME", typeof(string));
      dt.AddColumn("MATCH_OPTION", typeof(string));
      dt.AddColumn("UPDATE_RULE", typeof(string));
      dt.AddColumn("DELETE_RULE", typeof(string));
      dt.AddColumn("REFERENCED_TABLE_CATALOG", typeof(string));
      dt.AddColumn("REFERENCED_TABLE_SCHEMA", typeof(string));
      dt.AddColumn("REFERENCED_TABLE_NAME", typeof(string));
      return dt;
    }

    /// <summary>
    /// Creates and configures a MySqlSchemaCollection for storing foreign key column mappings.
    /// Includes standard columns such as CONSTRAINT_CATALOG, CONSTRAINT_SCHEMA, CONSTRAINT_NAME, TABLE_CATALOG,
    /// TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, ORDINAL_POSITION, REFERENCED_TABLE_CATALOG, REFERENCED_TABLE_SCHEMA,
    /// REFERENCED_TABLE_NAME, and REFERENCED_COLUMN_NAME.
    /// </summary>
    /// <returns>A new MySqlSchemaCollection named "Foreign Keys" with predefined columns for foreign key columns.</returns>
    private static MySqlSchemaCollection CreateForeignKeyColumnsCollection()
    {
      MySqlSchemaCollection dt = new MySqlSchemaCollection("Foreign Keys");
      dt.AddColumn("CONSTRAINT_CATALOG", typeof(string));
      dt.AddColumn("CONSTRAINT_SCHEMA", typeof(string));
      dt.AddColumn("CONSTRAINT_NAME", typeof(string));
      dt.AddColumn("TABLE_CATALOG", typeof(string));
      dt.AddColumn("TABLE_SCHEMA", typeof(string));
      dt.AddColumn("TABLE_NAME", typeof(string));
      dt.AddColumn("COLUMN_NAME", typeof(string));
      dt.AddColumn("ORDINAL_POSITION", typeof(int));
      dt.AddColumn("REFERENCED_TABLE_CATALOG", typeof(string));
      dt.AddColumn("REFERENCED_TABLE_SCHEMA", typeof(string));
      dt.AddColumn("REFERENCED_TABLE_NAME", typeof(string));
      dt.AddColumn("REFERENCED_COLUMN_NAME", typeof(string));
      return dt;
    }

    /// <summary>
    /// Extracts the constraint name restriction from the restrictions array and nullifies the entry for use in subsequent calls like GetTables.
    /// This allows filtering foreign keys by constraint name without interfering with table retrieval restrictions.
    /// </summary>
    /// <param name="restrictions">The restrictions array to process; modified in place by setting index 3 to null if a constraint name is present.</param>
    /// <returns>The extracted constraint name from restrictions[3], or null if not present or array is too short.</returns>
    private static string HandleKeyNameRestriction(string[] restrictions)
    {
      if (restrictions != null && restrictions.Length >= 4)
      {
        string keyName = restrictions[3];
        restrictions[3] = null;
        return keyName;
      }
      return null;
    }

    /// <summary>
    /// Retrieves a collection of foreign key constraints for tables in the specified schema(s).
    /// </summary>
    /// <param name="restrictions">Array of filters: [1] schema name, [2] table name, [3] constraint name. Index 0 unused.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A MySqlSchemaCollection containing foreign key constraint information.</returns>
    public virtual MySqlSchemaCollection GetForeignKeys(string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection dt = CreateForeignKeysCollection();

      // first we use our restrictions to get a list of tables that should be
      // consulted.  We save the keyname restriction since GetTables doesn't 
      // understand that.
      string keyName = null;
      if (restrictions != null && restrictions.Length >= 4)
      {
        keyName = restrictions[3];
        restrictions[3] = null;
      }

      MySqlSchemaCollection tables = GetTables(restrictions, cancellationToken);

      // now for each table retrieved, we call our helper function to
      // parse it's foreign keys
      foreach (MySqlSchemaRow table in tables.Rows)
        GetForeignKeysOnTable(dt, table, keyName, false, cancellationToken);

      return dt;
    }

    /// <summary>
    /// Asynchronously retrieves a collection of foreign key constraints for tables in the specified schema(s).
    /// </summary>
    /// <param name="restrictions">Array of filters: [1] schema name, [2] table name, [3] constraint name. Index 0 unused.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning a MySqlSchemaCollection with foreign key constraint information.</returns>
    public virtual async Task<MySqlSchemaCollection> GetForeignKeysAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection dt = CreateForeignKeysCollection();

      // first we use our restrictions to get a list of tables that should be
      // consulted.  We save the keyname restriction since GetTables doesn't 
      // understand that.
      string keyName = null;
      if (restrictions != null && restrictions.Length >= 4)
      {
        keyName = restrictions[3];
        restrictions[3] = null;
      }

      MySqlSchemaCollection tables = await GetTablesAsync(restrictions, cancellationToken).ConfigureAwait(false);

      // now for each table retrieved, we call our helper function to
      // parse it's foreign keys
      foreach (MySqlSchemaRow table in tables.Rows)
        await GetForeignKeysOnTableAsync(dt, table, keyName, false, cancellationToken).ConfigureAwait(false);

      return dt;
    }

    /// <summary>
    /// Retrieves a collection of foreign key column mappings for tables in the specified schema(s).
    /// </summary>
    /// <param name="restrictions">Array of filters: [1] schema name, [2] table name, [3] constraint name. Index 0 unused.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A MySqlSchemaCollection containing foreign key column mapping information.</returns>
    public virtual MySqlSchemaCollection GetForeignKeyColumns(string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection dt = CreateForeignKeyColumnsCollection();

      // first we use our restrictions to get a list of tables that should be
      // consulted.  We save the keyname restriction since GetTables doesn't 
      // understand that.
      string keyName = HandleKeyNameRestriction(restrictions);

      MySqlSchemaCollection tables = GetTables(restrictions, cancellationToken);

      // now for each table retrieved, we call our helper function to
      // parse it's foreign keys
      foreach (MySqlSchemaRow table in tables.Rows)
        GetForeignKeysOnTable(dt, table, keyName, true, cancellationToken);

      return dt;
    }

    /// <summary>
    /// Asynchronously retrieves a collection of foreign key column mappings for tables in the specified schema(s).
    /// </summary>
    /// <param name="restrictions">Array of filters: [1] schema name, [2] table name, [3] constraint name. Index 0 unused.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning a MySqlSchemaCollection with foreign key column mapping information.</returns>
    public virtual async Task<MySqlSchemaCollection> GetForeignKeyColumnsAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection dt = CreateForeignKeyColumnsCollection();

      // first we use our restrictions to get a list of tables that should be
      // consulted.  We save the keyname restriction since GetTables doesn't 
      // understand that.
      string keyName = HandleKeyNameRestriction(restrictions);

      MySqlSchemaCollection tables = await GetTablesAsync(restrictions, cancellationToken).ConfigureAwait(false);

      // now for each table retrieved, we call our helper function to
      // parse it's foreign keys
      foreach (MySqlSchemaRow table in tables.Rows)
        await GetForeignKeysOnTableAsync(dt, table, keyName, true, cancellationToken).ConfigureAwait(false);

      return dt;
    }

    /// <summary>
    /// Retrieves the current SQL mode from the MySQL server.
    /// Executes "SELECT @@SQL_MODE" to obtain the session's SQL mode as a string.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The current SQL mode as a string (e.g., "STRICT_TRANS_TABLES,ERROR_FOR_DIVISION_BY_ZERO").</returns>
    private string GetSqlMode(CancellationToken cancellationToken = default)
    {
      using MySqlCommand cmd = new MySqlCommand("SELECT @@SQL_MODE", connection);
      var result = cmd.ExecuteScalar(cancellationToken);
      return result.ToString();
    }

    /// <summary>
    /// Asynchronously retrieves the current SQL mode from the MySQL server.
    /// Executes "SELECT @@SQL_MODE" to obtain the session's SQL mode as a string.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning the current SQL mode as a string.</returns>
    private async Task<string> GetSqlModeAsync(CancellationToken cancellationToken = default)
    {
      using MySqlCommand cmd = new MySqlCommand("SELECT @@SQL_MODE", connection);
      var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
      return result.ToString();
    }

    #region Foreign Key routines

    /// <summary>
    /// GetForeignKeysOnTable retrieves the foreign keys on the given table.
    /// Since MySQL supports foreign keys on versions prior to 5.0, we can't  use
    /// information schema.  MySQL also does not include any type of SHOW command
    /// for foreign keys so we have to resort to use SHOW CREATE TABLE and parsing
    /// the output.
    /// </summary>
    /// <param name="fkTable">The table to store the key info in.</param>
    /// <param name="tableToParse">The table to get the foeign key info for.</param>
    /// <param name="filterName">Only get foreign keys that match this name.</param>
    /// <param name="includeColumns">Should column information be included in the table.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private void GetForeignKeysOnTable(MySqlSchemaCollection fkTable, MySqlSchemaRow tableToParse,
                       string filterName, bool includeColumns, CancellationToken cancellationToken = default)
    {
      string sqlMode = GetSqlMode(cancellationToken);

      if (filterName != null)
        filterName = StringUtility.ToLowerInvariant(filterName);

      string sql = string.Format("SHOW CREATE TABLE `{0}`.`{1}`", tableToParse["TABLE_SCHEMA"], tableToParse["TABLE_NAME"]);
      string lowerBody = null, body = null;
      using MySqlCommand cmd = new MySqlCommand(sql, connection);
      using (MySqlDataReader reader = cmd.ExecuteReader(default, cancellationToken))
      {
        reader.Read(cancellationToken);
        body = reader.GetString(1);
        lowerBody = StringUtility.ToLowerInvariant(body);
      }

      MySqlTokenizer tokenizer = new MySqlTokenizer(lowerBody);
      tokenizer.AnsiQuotes = sqlMode.IndexOf("ANSI_QUOTES") != -1;
      tokenizer.BackslashEscapes = sqlMode.IndexOf("NO_BACKSLASH_ESCAPES") != -1;

      while (true)
      {
        string token = tokenizer.NextToken();
        // look for a starting contraint
        while (token != null && (token != "constraint" || tokenizer.Quoted))
          token = tokenizer.NextToken();
        if (token == null) break;

        ParseConstraint(fkTable, tableToParse, tokenizer, includeColumns);
      }
    }

    /// <summary>
    /// GetForeignKeysOnTable retrieves the foreign keys on the given table.
    /// Since MySQL supports foreign keys on versions prior to 5.0, we can't  use
    /// information schema.  MySQL also does not include any type of SHOW command
    /// for foreign keys so we have to resort to use SHOW CREATE TABLE and parsing
    /// the output.
    /// </summary>
    /// <param name="fkTable">The table to store the key info in.</param>
    /// <param name="tableToParse">The table to get the foeign key info for.</param>
    /// <param name="filterName">Only get foreign keys that match this name.</param>
    /// <param name="includeColumns">Should column information be included in the table.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task GetForeignKeysOnTableAsync(MySqlSchemaCollection fkTable, MySqlSchemaRow tableToParse,
                       string filterName, bool includeColumns, CancellationToken cancellationToken = default)
    {
      string sqlMode = await GetSqlModeAsync(cancellationToken).ConfigureAwait(false);

      if (filterName != null)
        filterName = StringUtility.ToLowerInvariant(filterName);

      string sql = string.Format("SHOW CREATE TABLE `{0}`.`{1}`", tableToParse["TABLE_SCHEMA"], tableToParse["TABLE_NAME"]);
      string lowerBody = null, body = null;
      using MySqlCommand cmd = new MySqlCommand(sql, connection);
      using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, cancellationToken).ConfigureAwait(false))
      {
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        body = reader.GetString(1);
        lowerBody = StringUtility.ToLowerInvariant(body);
      }

      MySqlTokenizer tokenizer = new MySqlTokenizer(lowerBody);
      tokenizer.AnsiQuotes = sqlMode.IndexOf("ANSI_QUOTES") != -1;
      tokenizer.BackslashEscapes = sqlMode.IndexOf("NO_BACKSLASH_ESCAPES") != -1;

      while (true)
      {
        string token = tokenizer.NextToken();
        // look for a starting contraint
        while (token != null && (token != "constraint" || tokenizer.Quoted))
          token = tokenizer.NextToken();
        if (token == null) break;

        ParseConstraint(fkTable, tableToParse, tokenizer, includeColumns);
      }
    }

    private static void ParseConstraint(MySqlSchemaCollection fkTable, MySqlSchemaRow table,
      MySqlTokenizer tokenizer, bool includeColumns)
    {
      string name = tokenizer.NextToken();
      MySqlSchemaRow row = fkTable.AddRow();

      // make sure this constraint is a FK
      string token = tokenizer.NextToken();
      if (token != "foreign" || tokenizer.Quoted)
        return;
      tokenizer.NextToken(); // read off the 'KEY' symbol
      tokenizer.NextToken(); // read off the '(' symbol

      row["CONSTRAINT_CATALOG"] = table["TABLE_CATALOG"];
      row["CONSTRAINT_SCHEMA"] = table["TABLE_SCHEMA"];
      row["TABLE_CATALOG"] = table["TABLE_CATALOG"];
      row["TABLE_SCHEMA"] = table["TABLE_SCHEMA"];
      row["TABLE_NAME"] = table["TABLE_NAME"];
      row["REFERENCED_TABLE_CATALOG"] = null;
      row["CONSTRAINT_NAME"] = name.Trim(new char[] { '\'', '`' });

      List<string> srcColumns = includeColumns ? ParseColumns(tokenizer) : null;

      // now look for the references section
      while (token != "references" || tokenizer.Quoted)
        token = tokenizer.NextToken();
      string target1 = tokenizer.NextToken();
      string target2 = tokenizer.NextToken();
      if (target2.StartsWith(".", StringComparison.Ordinal))
      {
        row["REFERENCED_TABLE_SCHEMA"] = target1;
        row["REFERENCED_TABLE_NAME"] = target2.Substring(1).Trim(new char[] { '\'', '`' });
        tokenizer.NextToken();  // read off the '('
      }
      else
      {
        row["REFERENCED_TABLE_SCHEMA"] = table["TABLE_SCHEMA"];
        row["REFERENCED_TABLE_NAME"] = target1.Substring(1).Trim(new char[] { '\'', '`' }); ;
      }

      // if we are supposed to include columns, read the target columns
      List<string> targetColumns = includeColumns ? ParseColumns(tokenizer) : null;

      if (includeColumns)
        ProcessColumns(fkTable, row, srcColumns, targetColumns);
      else
        fkTable.Rows.Add(row);
    }

    private static List<string> ParseColumns(MySqlTokenizer tokenizer)
    {
      List<string> sc = new List<string>();
      string token = tokenizer.NextToken();
      while (token != ")")
      {
        if (token != ",")
          sc.Add(token);
        token = tokenizer.NextToken();
      }
      return sc;
    }

    private static void ProcessColumns(MySqlSchemaCollection fkTable, MySqlSchemaRow row, List<string> srcColumns, List<string> targetColumns)
    {
      for (int i = 0; i < srcColumns.Count; i++)
      {
        MySqlSchemaRow newRow = fkTable.AddRow();
        row.CopyRow(newRow);
        newRow["COLUMN_NAME"] = srcColumns[i];
        newRow["ORDINAL_POSITION"] = i;
        newRow["REFERENCED_COLUMN_NAME"] = targetColumns[i];
        fkTable.Rows.Add(newRow);
      }
    }

    #endregion

    /// <summary>
    /// Retrieves a collection of MySQL user accounts.
    /// </summary>
    /// <param name="restrictions">Array where restrictions[0] filters user names (LIKE pattern).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A MySqlSchemaCollection with HOST and USERNAME columns containing user information.</returns>
    public MySqlSchemaCollection GetUsers(string[] restrictions, CancellationToken cancellationToken = default)
    {
      StringBuilder sb = new StringBuilder("SELECT Host, User FROM mysql.user");
      if (restrictions != null && restrictions.Length > 0)
        sb.AppendFormat(CultureInfo.InvariantCulture, " WHERE User LIKE '{0}'", restrictions[0]);

      MySqlSchemaCollection c = QueryCollection("Users", sb.ToString(), cancellationToken);
      c.Columns[0].Name = "HOST";
      c.Columns[1].Name = "USERNAME";

      return c;
    }

    /// <summary>
    /// Asynchronously retrieves a collection of MySQL user accounts.
    /// </summary>
    /// <param name="restrictions">Array where restrictions[0] filters user names (LIKE pattern).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning a MySqlSchemaCollection with HOST and USERNAME columns.</returns>
    public async Task<MySqlSchemaCollection> GetUsersAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      StringBuilder sb = new StringBuilder("SELECT Host, User FROM mysql.user");
      if (restrictions != null && restrictions.Length > 0)
        sb.AppendFormat(CultureInfo.InvariantCulture, " WHERE User LIKE '{0}'", restrictions[0]);

      MySqlSchemaCollection c = await QueryCollectionAsync("Users", sb.ToString(), cancellationToken).ConfigureAwait(false);
      c.Columns[0].Name = "HOST";
      c.Columns[1].Name = "USERNAME";

      return c;
    }

    protected virtual MySqlSchemaCollection GetCollections()
    {
      object[][] collections = {
          new object[] {"MetaDataCollections", 0, 0},
          new object[] {"DataSourceInformation", 0, 0},
          new object[] {"DataTypes", 0, 0},
          new object[] {"Restrictions", 0, 0},
          new object[] {"ReservedWords", 0, 0},
          new object[] {"Databases", 1, 1},
          new object[] {"Tables", 4, 2},
          new object[] {"Columns", 4, 4},
          new object[] {"Users", 1, 1},
          new object[] {"Foreign Keys", 4, 3},
          new object[] {"IndexColumns", 5, 4},
          new object[] {"Indexes", 4, 3},
          new object[] {"Foreign Key Columns", 4, 3},
          new object[] {"UDF", 1, 1}
        };

      MySqlSchemaCollection dt = new MySqlSchemaCollection("MetaDataCollections");
      dt.AddColumn("CollectionName", typeof(string));
      dt.AddColumn("NumberOfRestrictions", typeof(int));
      dt.AddColumn("NumberOfIdentifierParts", typeof(int));

      FillTable(dt, collections);

      return dt;
    }

    private MySqlSchemaCollection GetDataSourceInformation()
    {
      MySqlSchemaCollection dt = new MySqlSchemaCollection("DataSourceInformation");
      dt.AddColumn("CompositeIdentifierSeparatorPattern", typeof(string));
      dt.AddColumn("DataSourceProductName", typeof(string));
      dt.AddColumn("DataSourceProductVersion", typeof(string));
      dt.AddColumn("DataSourceProductVersionNormalized", typeof(string));
      dt.AddColumn("GroupByBehavior", typeof(GroupByBehavior));
      dt.AddColumn("IdentifierPattern", typeof(string));
      dt.AddColumn("IdentifierCase", typeof(IdentifierCase));
      dt.AddColumn("OrderByColumnsInSelect", typeof(bool));
      dt.AddColumn("ParameterMarkerFormat", typeof(string));
      dt.AddColumn("ParameterMarkerPattern", typeof(string));
      dt.AddColumn("ParameterNameMaxLength", typeof(int));
      dt.AddColumn("ParameterNamePattern", typeof(string));
      dt.AddColumn("QuotedIdentifierPattern", typeof(string));
      dt.AddColumn("QuotedIdentifierCase", typeof(IdentifierCase));
      dt.AddColumn("StatementSeparatorPattern", typeof(string));
      dt.AddColumn("StringLiteralPattern", typeof(string));
      dt.AddColumn("SupportedJoinOperators", typeof(SupportedJoinOperators));

      DBVersion v = connection.driver.Version;
      string ver = $"{v.Major:0}.{v.Minor:0}.{v.Build:0}";

      MySqlSchemaRow row = dt.AddRow();
      row["CompositeIdentifierSeparatorPattern"] = "\\.";
      row["DataSourceProductName"] = "MySQL";
      row["DataSourceProductVersion"] = connection.ServerVersion;
      row["DataSourceProductVersionNormalized"] = ver;
      row["GroupByBehavior"] = GroupByBehavior.Unrelated;
      row["IdentifierPattern"] =
        @"(^\`\p{Lo}\p{Lu}\p{Ll}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Nd}@$#_]*$)|(^\`[^\`\0]|\`\`+\`$)|(^\"" + [^\""\0]|\""\""+\""$)";
      row["IdentifierCase"] = IdentifierCase.Insensitive;
      row["OrderByColumnsInSelect"] = false;
      row["ParameterMarkerFormat"] = "{0}";
      row["ParameterMarkerPattern"] = "(@[A-Za-z0-9_$#]*)";
      row["ParameterNameMaxLength"] = 128;
      row["ParameterNamePattern"] =
        @"^[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";
      row["QuotedIdentifierPattern"] = @"(([^\`]|\`\`)*)";
      row["QuotedIdentifierCase"] = IdentifierCase.Sensitive;
      row["StatementSeparatorPattern"] = ";";
      row["StringLiteralPattern"] = "'(([^']|'')*)'";
      row["SupportedJoinOperators"] = 15;
      dt.Rows.Add(row);

      return dt;
    }

    private static MySqlSchemaCollection GetDataTypes()
    {
      MySqlSchemaCollection dt = new MySqlSchemaCollection("DataTypes");
      dt.AddColumn("TypeName", typeof(string));
      dt.AddColumn("ProviderDbType", typeof(int));
      dt.AddColumn("ColumnSize", typeof(long));
      dt.AddColumn("CreateFormat", typeof(string));
      dt.AddColumn("CreateParameters", typeof(string));
      dt.AddColumn("DataType", typeof(string));
      dt.AddColumn("IsAutoincrementable", typeof(bool));
      dt.AddColumn("IsBestMatch", typeof(bool));
      dt.AddColumn("IsCaseSensitive", typeof(bool));
      dt.AddColumn("IsFixedLength", typeof(bool));
      dt.AddColumn("IsFixedPrecisionScale", typeof(bool));
      dt.AddColumn("IsLong", typeof(bool));
      dt.AddColumn("IsNullable", typeof(bool));
      dt.AddColumn("IsSearchable", typeof(bool));
      dt.AddColumn("IsSearchableWithLike", typeof(bool));
      dt.AddColumn("IsUnsigned", typeof(bool));
      dt.AddColumn("MaximumScale", typeof(short));
      dt.AddColumn("MinimumScale", typeof(short));
      dt.AddColumn("IsConcurrencyType", typeof(bool));
      dt.AddColumn("IsLiteralSupported", typeof(bool));
      dt.AddColumn("LiteralPrefix", typeof(string));
      dt.AddColumn("LiteralSuffix", typeof(string));
      dt.AddColumn("NativeDataType", typeof(string));

      // have each one of the types contribute to the datatypes collection
      MySqlBit.SetDSInfo(dt);
      MySqlBinary.SetDSInfo(dt);
      MySqlDateTime.SetDSInfo(dt);
      MySqlTimeSpan.SetDSInfo(dt);
      MySqlString.SetDSInfo(dt);
      MySqlDouble.SetDSInfo(dt);
      MySqlSingle.SetDSInfo(dt);
      MySqlByte.SetDSInfo(dt);
      MySqlInt16.SetDSInfo(dt);
      MySqlInt32.SetDSInfo(dt);
      MySqlInt64.SetDSInfo(dt);
      MySqlDecimal.SetDSInfo(dt);
      MySqlUByte.SetDSInfo(dt);
      MySqlUInt16.SetDSInfo(dt);
      MySqlUInt32.SetDSInfo(dt);
      MySqlUInt64.SetDSInfo(dt);

      return dt;
    }

    protected virtual MySqlSchemaCollection GetRestrictions()
    {
      object[][] restrictions = {
          new object[] {"Users", "Name", "", 0},
          new object[] {"Databases", "Name", "", 0},
          new object[] {"Tables", "Database", "", 0},
          new object[] {"Tables", "Schema", "", 1},
          new object[] {"Tables", "Table", "", 2},
          new object[] {"Tables", "TableType", "", 3},
          new object[] {"Columns", "Database", "", 0},
          new object[] {"Columns", "Schema", "", 1},
          new object[] {"Columns", "Table", "", 2},
          new object[] {"Columns", "Column", "", 3},
          new object[] {"Indexes", "Database", "", 0},
          new object[] {"Indexes", "Schema", "", 1},
          new object[] {"Indexes", "Table", "", 2},
          new object[] {"Indexes", "Name", "", 3},
          new object[] {"IndexColumns", "Database", "", 0},
          new object[] {"IndexColumns", "Schema", "", 1},
          new object[] {"IndexColumns", "Table", "", 2},
          new object[] {"IndexColumns", "ConstraintName", "", 3},
          new object[] {"IndexColumns", "Column", "", 4},
          new object[] {"Foreign Keys", "Database", "", 0},
          new object[] {"Foreign Keys", "Schema", "", 1},
          new object[] {"Foreign Keys", "Table", "", 2},
          new object[] {"Foreign Keys", "Constraint Name", "", 3},
          new object[] {"Foreign Key Columns", "Catalog", "", 0},
          new object[] {"Foreign Key Columns", "Schema", "", 1},
          new object[] {"Foreign Key Columns", "Table", "", 2},
          new object[] {"Foreign Key Columns", "Constraint Name", "", 3},
          new object[] {"UDF", "Name", "", 0}
        };

      MySqlSchemaCollection dt = new MySqlSchemaCollection("Restrictions");
      dt.AddColumn("CollectionName", typeof(string));
      dt.AddColumn("RestrictionName", typeof(string));
      dt.AddColumn("RestrictionDefault", typeof(string));
      dt.AddColumn("RestrictionNumber", typeof(int));

      FillTable(dt, restrictions);

      return dt;
    }

    internal static MySqlSchemaCollection GetReservedWords()
    {
      MySqlSchemaCollection dt = new MySqlSchemaCollection("ReservedWords");
      string resourceName = "MySql.Data.Properties.ReservedWords.txt";
      dt.AddColumn(DbMetaDataColumnNames.ReservedWord, typeof(string));
      using (Stream str = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
      {
        if (str == null)
          throw new Exception($"Resource {resourceName} not found in {Assembly.GetExecutingAssembly()}.");

        using (StreamReader sr = new StreamReader(str))
        {
          string line = sr.ReadLine();
          while (line != null)
          {
            string[] keywords = line.Split(new char[] { ' ' });
            foreach (string s in keywords)
            {
              if (String.IsNullOrEmpty(s)) continue;
              MySqlSchemaRow row = dt.AddRow();
              row[0] = s;
            }

            line = sr.ReadLine();
          }
        }
      }

      return dt;
    }

    protected static void FillTable(MySqlSchemaCollection dt, object[][] data)
    {
      foreach (object[] dataItem in data)
      {
        MySqlSchemaRow row = dt.AddRow();
        for (int i = 0; i < dataItem.Length; i++)
          row[i] = dataItem[i];
      }
    }

    /// <summary>
    /// Populates a single row in the schema collection from a SHOW TABLE STATUS reader row.
    /// Sets all standard table metadata fields, handling DBNull values via GetString where appropriate.
    /// </summary>
    /// <param name="row">The MySqlSchemaRow to populate.</param>
    /// <param name="reader">The MySqlDataReader positioned at the current table status row.</param>
    /// <param name="schemaName">The database/schema name for the table.</param>
    /// <param name="tableType">The type of the table (e.g., "BASE TABLE" or "SYSTEM VIEW").</param>
    private static void LoadTableStatusRow(MySqlSchemaRow row, MySqlDataReader reader, string schemaName, string tableType)
    {
      row["TABLE_CATALOG"] = null;
      row["TABLE_SCHEMA"] = schemaName;
      row["TABLE_NAME"] = reader.GetString(0);
      row["TABLE_TYPE"] = tableType;
      row["ENGINE"] = GetString(reader, 1);
      row["VERSION"] = reader.GetValue(2);
      row["ROW_FORMAT"] = GetString(reader, 3);
      row["TABLE_ROWS"] = reader.GetValue(4);
      row["AVG_ROW_LENGTH"] = reader.GetValue(5);
      row["DATA_LENGTH"] = reader.GetValue(6);
      row["MAX_DATA_LENGTH"] = reader.GetValue(7);
      row["INDEX_LENGTH"] = reader.GetValue(8);
      row["DATA_FREE"] = reader.GetValue(9);
      row["AUTO_INCREMENT"] = reader.GetValue(10);
      row["CREATE_TIME"] = reader.GetValue(11);
      row["UPDATE_TIME"] = reader.GetValue(12);
      row["CHECK_TIME"] = reader.GetValue(13);
      row["TABLE_COLLATION"] = GetString(reader, 14);
      row["CHECKSUM"] = reader.GetValue(15);
      row["CREATE_OPTIONS"] = GetString(reader, 16);
      row["TABLE_COMMENT"] = GetString(reader, 17);
    }

    /// <summary>
    /// Populates the provided schema collection with table status information for a specific database.
    /// </summary>
    /// <param name="schema">The MySqlSchemaCollection to populate with table rows.</param>
    /// <param name="restrictions">Array where restrictions[1] is the database/schema name, [2] optional table name filter (LIKE).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private void FindTables(MySqlSchemaCollection schema, string[] restrictions, CancellationToken cancellationToken = default)
    {
      StringBuilder sql = new StringBuilder();
      StringBuilder where = new StringBuilder();
      sql.AppendFormat(CultureInfo.InvariantCulture, "SHOW TABLE STATUS FROM `{0}`", restrictions[1]);

      if (restrictions != null && restrictions.Length >= 3 && restrictions[2] != null)
        where.AppendFormat(CultureInfo.InvariantCulture, " LIKE '{0}'", restrictions[2]);

      sql.Append(where.ToString());
      string table_type = restrictions[1].ToLower() == "information_schema" ? "SYSTEM VIEW" : "BASE TABLE";
      using MySqlCommand cmd = new MySqlCommand(sql.ToString(), connection);

      using (MySqlDataReader reader = cmd.ExecuteReader(default, cancellationToken))
      {
        while (reader.Read(cancellationToken))
        {
          MySqlSchemaRow row = schema.AddRow();
          LoadTableStatusRow(row, reader, restrictions[1], table_type);
        }
      }
    }

    /// <summary>
    /// Asynchronously populates the provided schema collection with table status information for a specific database.
    /// </summary>
    /// <param name="schema">The MySqlSchemaCollection to populate with table rows.</param>
    /// <param name="restrictions">Array where restrictions[1] is the database/schema name, [2] optional table name filter (LIKE).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task FindTablesAsync(MySqlSchemaCollection schema, string[] restrictions, CancellationToken cancellationToken = default)
    {
      StringBuilder sql = new StringBuilder();
      StringBuilder where = new StringBuilder();
      sql.AppendFormat(CultureInfo.InvariantCulture, "SHOW TABLE STATUS FROM `{0}`", restrictions[1]);

      if (restrictions != null && restrictions.Length >= 3 && restrictions[2] != null)
        where.AppendFormat(CultureInfo.InvariantCulture, " LIKE '{0}'", restrictions[2]);

      sql.Append(where.ToString());
      string table_type = restrictions[1].ToLower() == "information_schema" ? "SYSTEM VIEW" : "BASE TABLE";
      using MySqlCommand cmd = new MySqlCommand(sql.ToString(), connection);

      using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, cancellationToken).ConfigureAwait(false))
      {
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
          MySqlSchemaRow row = schema.AddRow();
          LoadTableStatusRow(row, reader, restrictions[1], table_type);
        }
      }
    }

    private static string GetString(MySqlDataReader reader, int index)
    {
      if (reader.IsDBNull(index))
        return null;
      return reader.GetString(index);
    }

    /// <summary>
    /// Retrieves a collection of user-defined functions (UDFs) from the MySQL server.
    /// </summary>
    /// <param name="restrictions">Array where restrictions[0] filters UDF names (LIKE pattern).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A MySqlSchemaCollection containing UDF information.</returns>
    /// <exception cref="MySqlException">Thrown on access denial to mysql.func (e.g., insufficient privileges).</exception>
    public MySqlSchemaCollection GetUDF(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string sql = "SELECT name,ret,dl FROM mysql.func";
      if (restrictions?.Length >= 1 && !String.IsNullOrEmpty(restrictions[0]))
        sql += $" WHERE name LIKE '{restrictions[0]}'";

      MySqlSchemaCollection dt = new MySqlSchemaCollection("User-defined Functions");
      dt.AddColumn("NAME", typeof(string));
      dt.AddColumn("RETURN_TYPE", typeof(int));
      dt.AddColumn("LIBRARY_NAME", typeof(string));

      using MySqlCommand cmd = new MySqlCommand(sql, connection);

      try
      {
        using (MySqlDataReader reader = cmd.ExecuteReader(default, cancellationToken))
        {
          while (reader.Read(cancellationToken))
          {
            MySqlSchemaRow row = dt.AddRow();
            row[0] = reader.GetString(0);
            row[1] = reader.GetInt32(1);
            row[2] = reader.GetString(2);
          }
        }
      }
      catch (MySqlException ex)
      {
        if (ex.Number != (int)MySqlErrorCode.TableAccessDenied)
          throw;
        throw new MySqlException(Resources.UnableToEnumerateUDF, ex);
      }

      return dt;
    }

    /// <summary>
    /// Asynchronously retrieves a collection of user-defined functions (UDFs) from the MySQL server.
    /// </summary>
    /// <param name="restrictions">Array where restrictions[0] filters UDF names (LIKE pattern).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning a MySqlSchemaCollection with UDF information.</returns>
    /// <exception cref="MySqlException">Thrown on access denial to mysql.func (e.g., insufficient privileges).</exception>
    public async Task<MySqlSchemaCollection> GetUDFAsync(string[] restrictions, CancellationToken cancellationToken = default)
    {
      string sql = "SELECT name,ret,dl FROM mysql.func";
      if (restrictions?.Length >= 1 && !String.IsNullOrEmpty(restrictions[0]))
        sql += $" WHERE name LIKE '{restrictions[0]}'";

      MySqlSchemaCollection dt = new MySqlSchemaCollection("User-defined Functions");
      dt.AddColumn("NAME", typeof(string));
      dt.AddColumn("RETURN_TYPE", typeof(int));
      dt.AddColumn("LIBRARY_NAME", typeof(string));

      using MySqlCommand cmd = new MySqlCommand(sql, connection);

      try
      {
        using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, cancellationToken).ConfigureAwait(false))
        {
          while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
          {
            MySqlSchemaRow row = dt.AddRow();
            row[0] = reader.GetString(0);
            row[1] = reader.GetInt32(1);
            row[2] = reader.GetString(2);
          }
        }
      }
      catch (MySqlException ex)
      {
        if (ex.Number != (int)MySqlErrorCode.TableAccessDenied)
          throw;
        throw new MySqlException(Resources.UnableToEnumerateUDF, ex);
      }

      return dt;
    }

    /// <summary>
    /// Core implementation for retrieving schema information by collection name.
    /// </summary>
    /// <param name="collection">Uppercase name of the schema collection (e.g., "TABLES", "COLUMNS", "USERS").</param>
    /// <param name="restrictions">Array of string filters specific to the collection (e.g., schema in [1] for tables).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A MySqlSchemaCollection for the specified collection, or null if invalid.</returns>
    protected virtual MySqlSchemaCollection GetSchemaInternal(string collection, string[] restrictions, CancellationToken cancellationToken = default)
    {
      switch (collection)
      {
        // common collections
        case "METADATACOLLECTIONS":
          return GetCollections();
        case "DATASOURCEINFORMATION":
          return GetDataSourceInformation();
        case "DATATYPES":
          return GetDataTypes();
        case "RESTRICTIONS":
          return GetRestrictions();
        case "RESERVEDWORDS":
          return GetReservedWords();

        // collections specific to our provider
        case "USERS":
          return GetUsers(restrictions, cancellationToken);
        case "DATABASES":
          return GetDatabases(restrictions, cancellationToken);
        case "UDF":
          return GetUDF(restrictions, cancellationToken);
      }

      // if we have a current database and our users have
      // not specified a database, then default to the currently
      // selected one.
      if (restrictions == null)
        restrictions = new string[2];
      if (connection != null &&
        connection.Database != null &&
        connection.Database.Length > 0 &&
        restrictions.Length > 1 &&
        restrictions[1] == null)
        restrictions[1] = connection.Database;

      switch (collection)
      {
        case "TABLES":
          return GetTables(restrictions, cancellationToken);
        case "COLUMNS":
          return GetColumns(restrictions, cancellationToken);
        case "INDEXES":
          return GetIndexes(restrictions, cancellationToken);
        case "INDEXCOLUMNS":
          return GetIndexColumns(restrictions, cancellationToken);
        case "FOREIGN KEYS":
          return GetForeignKeys(restrictions, cancellationToken);
        case "FOREIGN KEY COLUMNS":
          return GetForeignKeyColumns(restrictions, cancellationToken);
      }
      return null;
    }

    /// <summary>
    /// Asynchronous core implementation for retrieving schema information by collection name.
    /// </summary>
    /// <param name="collection">Uppercase name of the schema collection (e.g., "TABLES", "COLUMNS", "USERS").</param>
    /// <param name="restrictions">Array of string filters specific to the collection (e.g., schema in [1] for tables).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning a MySqlSchemaCollection or null if invalid.</returns>
    protected virtual async Task<MySqlSchemaCollection> GetSchemaInternalAsync(string collection, string[] restrictions, CancellationToken cancellationToken = default)
    {
      switch (collection)
      {
        // common collections
        case "METADATACOLLECTIONS":
          return GetCollections();
        case "DATASOURCEINFORMATION":
          return GetDataSourceInformation();
        case "DATATYPES":
          return GetDataTypes();
        case "RESTRICTIONS":
          return GetRestrictions();
        case "RESERVEDWORDS":
          return GetReservedWords();

        // collections specific to our provider
        case "USERS":
          return await GetUsersAsync(restrictions, cancellationToken).ConfigureAwait(false);
        case "DATABASES":
          return await GetDatabasesAsync(restrictions, cancellationToken).ConfigureAwait(false);
        case "UDF":
          return await GetUDFAsync(restrictions, cancellationToken).ConfigureAwait(false);
      }

      // if we have a current database and our users have
      // not specified a database, then default to the currently
      // selected one.
      if (restrictions == null)
        restrictions = new string[2];
      if (connection != null &&
        connection.Database != null &&
        connection.Database.Length > 0 &&
        restrictions.Length > 1 &&
        restrictions[1] == null)
        restrictions[1] = connection.Database;

      switch (collection)
      {
        case "TABLES":
          return await GetTablesAsync(restrictions, cancellationToken).ConfigureAwait(false);
        case "COLUMNS":
          return await GetColumnsAsync(restrictions, cancellationToken).ConfigureAwait(false);
        case "INDEXES":
          return await GetIndexesAsync(restrictions, cancellationToken).ConfigureAwait(false);
        case "INDEXCOLUMNS":
          return await GetIndexColumnsAsync(restrictions, cancellationToken).ConfigureAwait(false);
        case "FOREIGN KEYS":
          return await GetForeignKeysAsync(restrictions, cancellationToken).ConfigureAwait(false);
        case "FOREIGN KEY COLUMNS":
          return await GetForeignKeyColumnsAsync(restrictions, cancellationToken).ConfigureAwait(false);
      }
      return null;
    }

    internal string[] CleanRestrictions(string[] restrictionValues)
    {
      string[] restrictions = null;
      if (restrictionValues != null)
      {
        restrictions = (string[])restrictionValues.Clone();

        for (int x = 0; x < restrictions.Length; x++)
        {
          string s = restrictions[x];
          if (s == null) continue;
          restrictions[x] = s.Trim('`');
        }
      }
      return restrictions;
    }

    /// <summary>
    /// Executes a SQL query on the connection and populates a MySqlSchemaCollection with the results.
    /// </summary>
    /// <param name="name">The name of the schema collection (used for the collection's title).</param>
    /// <param name="sql">The SQL query to execute (e.g., "SHOW DATABASES").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A MySqlSchemaCollection populated with the query results.</returns>
    protected MySqlSchemaCollection QueryCollection(string name, string sql, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection c = new MySqlSchemaCollection(name);
      using MySqlCommand cmd = new MySqlCommand(sql, connection);
      using MySqlDataReader reader = cmd.ExecuteReader(default, cancellationToken);

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
    /// Asynchronously executes a SQL query on the connection and populates a MySqlSchemaCollection with the results.
    /// </summary>
    /// <param name="name">The name of the schema collection (used for the collection's title).</param>
    /// <param name="sql">The SQL query to execute (e.g., "SHOW DATABASES").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning a MySqlSchemaCollection populated with the query results.</returns>
    protected async Task<MySqlSchemaCollection> QueryCollectionAsync(string name, string sql, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection c = new MySqlSchemaCollection(name);
      using MySqlCommand cmd = new MySqlCommand(sql, connection);
      using MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, cancellationToken).ConfigureAwait(false);

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
  }
}
