// Copyright © 2004, 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using MySql.Data.Common;
using MySql.Data.Types;
using System.Collections.Specialized;
using System.Collections;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient.Properties;

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

    public virtual DataTable GetSchema(string collection, String[] restrictions)
    {
      if (connection.State != ConnectionState.Open)
        throw new MySqlException("GetSchema can only be called on an open connection.");

      collection = collection.ToUpper(CultureInfo.InvariantCulture);

      DataTable dt = GetSchemaInternal(collection, restrictions);

      if (dt == null)
        throw new ArgumentException("Invalid collection name");
      return dt;
    }

    public virtual DataTable GetDatabases(string[] restrictions)
    {
      Regex regex = null;
      int caseSetting = Int32.Parse(connection.driver.Property("lower_case_table_names"));

      string sql = "SHOW DATABASES";

      // if lower_case_table_names is zero, then case lookup should be sensitive
      // so we can use LIKE to do the matching.
      if (caseSetting == 0)
      {
        if (restrictions != null && restrictions.Length >= 1)
          sql = sql + " LIKE '" + restrictions[0] + "'";
      }
      else if (restrictions != null && restrictions.Length >= 1 && restrictions[0] != null)
        regex = new Regex(restrictions[0], RegexOptions.IgnoreCase);

      MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
      DataTable dt = new DataTable();
      da.Fill(dt);

      DataTable table = new DataTable("Databases");
      table.Columns.Add("CATALOG_NAME", typeof(string));
      table.Columns.Add("SCHEMA_NAME", typeof(string));

      foreach (DataRow row in dt.Rows)
      {
        if (caseSetting != 0 && regex != null &&
          !regex.Match(row[0].ToString()).Success) continue;

        DataRow newRow = table.NewRow();
        newRow[1] = row[0];
        table.Rows.Add(newRow);
      }

      return table;
    }

    public virtual DataTable GetTables(string[] restrictions)
    {
      DataTable dt = new DataTable("Tables");
      dt.Columns.Add("TABLE_CATALOG", typeof(string));
      dt.Columns.Add("TABLE_SCHEMA", typeof(string));
      dt.Columns.Add("TABLE_NAME", typeof(string));
      dt.Columns.Add("TABLE_TYPE", typeof(string));
      dt.Columns.Add("ENGINE", typeof(string));
      dt.Columns.Add("VERSION", typeof(ulong));
      dt.Columns.Add("ROW_FORMAT", typeof(string));
      dt.Columns.Add("TABLE_ROWS", typeof(ulong));
      dt.Columns.Add("AVG_ROW_LENGTH", typeof(ulong));
      dt.Columns.Add("DATA_LENGTH", typeof(ulong));
      dt.Columns.Add("MAX_DATA_LENGTH", typeof(ulong));
      dt.Columns.Add("INDEX_LENGTH", typeof(ulong));
      dt.Columns.Add("DATA_FREE", typeof(ulong));
      dt.Columns.Add("AUTO_INCREMENT", typeof(ulong));
      dt.Columns.Add("CREATE_TIME", typeof(DateTime));
      dt.Columns.Add("UPDATE_TIME", typeof(DateTime));
      dt.Columns.Add("CHECK_TIME", typeof(DateTime));
      dt.Columns.Add("TABLE_COLLATION", typeof(string));
      dt.Columns.Add("CHECKSUM", typeof(ulong));
      dt.Columns.Add("CREATE_OPTIONS", typeof(string));
      dt.Columns.Add("TABLE_COMMENT", typeof(string));

      // we have to new up a new restriction array here since
      // GetDatabases takes the database in the first slot
      string[] dbRestriction = new string[4];
      if (restrictions != null && restrictions.Length >= 2)
        dbRestriction[0] = restrictions[1];
      DataTable databases = GetDatabases(dbRestriction);

      if (restrictions != null)
        Array.Copy(restrictions, dbRestriction,
               Math.Min(dbRestriction.Length, restrictions.Length));

      foreach (DataRow db in databases.Rows)
      {
        dbRestriction[1] = db["SCHEMA_NAME"].ToString();
        FindTables(dt, dbRestriction);
      }
      return dt;
    }

    protected void QuoteDefaultValues(DataTable dt)
    {
      if (dt == null) return;
      if (!dt.Columns.Contains("COLUMN_DEFAULT")) return;

      foreach (DataRow row in dt.Rows)
      {
        object defaultValue = row["COLUMN_DEFAULT"];
        if (MetaData.IsTextType(row["DATA_TYPE"].ToString()))
          row["COLUMN_DEFAULT"] = String.Format("{0}", defaultValue);
      }
    }

    public virtual DataTable GetColumns(string[] restrictions)
    {
      DataTable dt = new DataTable("Columns");
      dt.Columns.Add("TABLE_CATALOG", typeof(string));
      dt.Columns.Add("TABLE_SCHEMA", typeof(string));
      dt.Columns.Add("TABLE_NAME", typeof(string));
      dt.Columns.Add("COLUMN_NAME", typeof(string));
      dt.Columns.Add("ORDINAL_POSITION", typeof(ulong));
      dt.Columns.Add("COLUMN_DEFAULT", typeof(string));
      dt.Columns.Add("IS_NULLABLE", typeof(string));
      dt.Columns.Add("DATA_TYPE", typeof(string));
      dt.Columns.Add("CHARACTER_MAXIMUM_LENGTH", typeof(ulong));
      dt.Columns.Add("CHARACTER_OCTET_LENGTH", typeof(ulong));
      dt.Columns.Add("NUMERIC_PRECISION", typeof(ulong));
      dt.Columns.Add("NUMERIC_SCALE", typeof(ulong));
      dt.Columns.Add("CHARACTER_SET_NAME", typeof(string));
      dt.Columns.Add("COLLATION_NAME", typeof(string));
      dt.Columns.Add("COLUMN_TYPE", typeof(string));
      dt.Columns.Add("COLUMN_KEY", typeof(string));
      dt.Columns.Add("EXTRA", typeof(string));
      dt.Columns.Add("PRIVILEGES", typeof(string));
      dt.Columns.Add("COLUMN_COMMENT", typeof(string));

      // we don't allow restricting on table type here
      string columnName = null;
      if (restrictions != null && restrictions.Length == 4)
      {
        columnName = restrictions[3];
        restrictions[3] = null;
      }
      DataTable tables = GetTables(restrictions);

      foreach (DataRow row in tables.Rows)
        LoadTableColumns(dt, row["TABLE_SCHEMA"].ToString(),
                 row["TABLE_NAME"].ToString(), columnName);

      QuoteDefaultValues(dt);
      return dt;
    }

    private void LoadTableColumns(DataTable dt, string schema,
                    string tableName, string columnRestriction)
    {
      string sql = String.Format("SHOW FULL COLUMNS FROM `{0}`.`{1}`",
                     schema, tableName);
      MySqlCommand cmd = new MySqlCommand(sql, connection);

      int pos = 1;
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.Read())
        {
          string colName = reader.GetString(0);
          if (columnRestriction != null && colName != columnRestriction)
            continue;
          DataRow row = dt.NewRow();
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
          ParseColumnRow(row);
          dt.Rows.Add(row);
        }
      }
    }

    private static void ParseColumnRow(DataRow row)
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

    public virtual DataTable GetIndexes(string[] restrictions)
    {
      DataTable dt = new DataTable("Indexes");
      dt.Columns.Add("INDEX_CATALOG", typeof(string));
      dt.Columns.Add("INDEX_SCHEMA", typeof(string));
      dt.Columns.Add("INDEX_NAME", typeof(string));
      dt.Columns.Add("TABLE_NAME", typeof(string));
      dt.Columns.Add("UNIQUE", typeof(bool));
      dt.Columns.Add("PRIMARY", typeof(bool));
      dt.Columns.Add("TYPE", typeof(string));
      dt.Columns.Add("COMMENT", typeof(string));

      // Get the list of tables first
      int max = restrictions == null ? 4 : restrictions.Length;
      string[] tableRestrictions = new string[Math.Max(max, 4)];
      if (restrictions != null)
        restrictions.CopyTo(tableRestrictions, 0);
      tableRestrictions[3] = "BASE TABLE";
      DataTable tables = GetTables(tableRestrictions);

      foreach (DataRow table in tables.Rows)
      {
        string sql = String.Format("SHOW INDEX FROM `{0}`.`{1}`",
          MySqlHelper.DoubleQuoteString((string)table["TABLE_SCHEMA"]),
          MySqlHelper.DoubleQuoteString((string)table["TABLE_NAME"]));
        MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
        DataTable indexes = new DataTable();
        da.Fill(indexes);
        foreach (DataRow index in indexes.Rows)
        {
          long seq_index = (long)index["SEQ_IN_INDEX"];
          if (seq_index != 1) continue;
          if (restrictions != null && restrictions.Length == 4 &&
            restrictions[3] != null &&
            !index["KEY_NAME"].Equals(restrictions[3])) continue;
          DataRow row = dt.NewRow();
          row["INDEX_CATALOG"] = null;
          row["INDEX_SCHEMA"] = table["TABLE_SCHEMA"];
          row["INDEX_NAME"] = index["KEY_NAME"];
          row["TABLE_NAME"] = index["TABLE"];
          row["UNIQUE"] = (long)index["NON_UNIQUE"] == 0;
          row["PRIMARY"] = index["KEY_NAME"].Equals("PRIMARY");
          row["TYPE"] = index["INDEX_TYPE"];
          row["COMMENT"] = index["COMMENT"];
          dt.Rows.Add(row);
        }
      }

      return dt;
    }

    public virtual DataTable GetIndexColumns(string[] restrictions)
    {
      DataTable dt = new DataTable("IndexColumns");
      dt.Columns.Add("INDEX_CATALOG", typeof(string));
      dt.Columns.Add("INDEX_SCHEMA", typeof(string));
      dt.Columns.Add("INDEX_NAME", typeof(string));
      dt.Columns.Add("TABLE_NAME", typeof(string));
      dt.Columns.Add("COLUMN_NAME", typeof(string));
      dt.Columns.Add("ORDINAL_POSITION", typeof(int));
      dt.Columns.Add("SORT_ORDER", typeof(string));

      int max = restrictions == null ? 4 : restrictions.Length;
      string[] tableRestrictions = new string[Math.Max(max, 4)];
      if (restrictions != null)
        restrictions.CopyTo(tableRestrictions, 0);
      tableRestrictions[3] = "BASE TABLE";
      DataTable tables = GetTables(tableRestrictions);

      foreach (DataRow table in tables.Rows)
      {
        string sql = String.Format("SHOW INDEX FROM `{0}`.`{1}`",
                       table["TABLE_SCHEMA"], table["TABLE_NAME"]);
        MySqlCommand cmd = new MySqlCommand(sql, connection);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            string key_name = GetString(reader, reader.GetOrdinal("KEY_NAME"));
            string col_name = GetString(reader, reader.GetOrdinal("COLUMN_NAME"));

            if (restrictions != null)
            {
              if (restrictions.Length >= 4 && restrictions[3] != null &&
                key_name != restrictions[3]) continue;
              if (restrictions.Length >= 5 && restrictions[4] != null &&
                col_name != restrictions[4]) continue;
            }
            DataRow row = dt.NewRow();
            row["INDEX_CATALOG"] = null;
            row["INDEX_SCHEMA"] = table["TABLE_SCHEMA"];
            row["INDEX_NAME"] = key_name;
            row["TABLE_NAME"] = GetString(reader, reader.GetOrdinal("TABLE"));
            row["COLUMN_NAME"] = col_name;
            row["ORDINAL_POSITION"] = reader.GetValue(reader.GetOrdinal("SEQ_IN_INDEX"));
            row["SORT_ORDER"] = reader.GetString("COLLATION");
            dt.Rows.Add(row);
          }
        }
      }

      return dt;
    }

    public virtual DataTable GetForeignKeys(string[] restrictions)
    {
      DataTable dt = new DataTable("Foreign Keys");
      dt.Columns.Add("CONSTRAINT_CATALOG", typeof(string));
      dt.Columns.Add("CONSTRAINT_SCHEMA", typeof(string));
      dt.Columns.Add("CONSTRAINT_NAME", typeof(string));
      dt.Columns.Add("TABLE_CATALOG", typeof(string));
      dt.Columns.Add("TABLE_SCHEMA", typeof(string));
      dt.Columns.Add("TABLE_NAME", typeof(string));
      dt.Columns.Add("MATCH_OPTION", typeof(string));
      dt.Columns.Add("UPDATE_RULE", typeof(string));
      dt.Columns.Add("DELETE_RULE", typeof(string));
      dt.Columns.Add("REFERENCED_TABLE_CATALOG", typeof(string));
      dt.Columns.Add("REFERENCED_TABLE_SCHEMA", typeof(string));
      dt.Columns.Add("REFERENCED_TABLE_NAME", typeof(string));

      // first we use our restrictions to get a list of tables that should be
      // consulted.  We save the keyname restriction since GetTables doesn't 
      // understand that.
      string keyName = null;
      if (restrictions != null && restrictions.Length >= 4)
      {
        keyName = restrictions[3];
        restrictions[3] = null;
      }

      DataTable tables = GetTables(restrictions);

      // now for each table retrieved, we call our helper function to
      // parse it's foreign keys
      foreach (DataRow table in tables.Rows)
        GetForeignKeysOnTable(dt, table, keyName, false);

      return dt;
    }

    public virtual DataTable GetForeignKeyColumns(string[] restrictions)
    {
      DataTable dt = new DataTable("Foreign Keys");
      dt.Columns.Add("CONSTRAINT_CATALOG", typeof(string));
      dt.Columns.Add("CONSTRAINT_SCHEMA", typeof(string));
      dt.Columns.Add("CONSTRAINT_NAME", typeof(string));
      dt.Columns.Add("TABLE_CATALOG", typeof(string));
      dt.Columns.Add("TABLE_SCHEMA", typeof(string));
      dt.Columns.Add("TABLE_NAME", typeof(string));
      dt.Columns.Add("COLUMN_NAME", typeof(string));
      dt.Columns.Add("ORDINAL_POSITION", typeof(int));
      dt.Columns.Add("REFERENCED_TABLE_CATALOG", typeof(string));
      dt.Columns.Add("REFERENCED_TABLE_SCHEMA", typeof(string));
      dt.Columns.Add("REFERENCED_TABLE_NAME", typeof(string));
      dt.Columns.Add("REFERENCED_COLUMN_NAME", typeof(string));

      // first we use our restrictions to get a list of tables that should be
      // consulted.  We save the keyname restriction since GetTables doesn't 
      // understand that.
      string keyName = null;
      if (restrictions != null && restrictions.Length >= 4)
      {
        keyName = restrictions[3];
        restrictions[3] = null;
      }

      DataTable tables = GetTables(restrictions);

      // now for each table retrieved, we call our helper function to
      // parse it's foreign keys
      foreach (DataRow table in tables.Rows)
        GetForeignKeysOnTable(dt, table, keyName, true);
      return dt;
    }


    private string GetSqlMode()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT @@SQL_MODE", connection);
      return cmd.ExecuteScalar().ToString();
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
    private void GetForeignKeysOnTable(DataTable fkTable, DataRow tableToParse,
                       string filterName, bool includeColumns)
    {
      string sqlMode = GetSqlMode();

      if (filterName != null)
        filterName = filterName.ToLower(CultureInfo.InvariantCulture);

      string sql = string.Format("SHOW CREATE TABLE `{0}`.`{1}`",
                     tableToParse["TABLE_SCHEMA"], tableToParse["TABLE_NAME"]);
      string lowerBody = null, body = null;
      MySqlCommand cmd = new MySqlCommand(sql, connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        body = reader.GetString(1);
        lowerBody = body.ToLower(CultureInfo.InvariantCulture);
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

    private static void ParseConstraint(DataTable fkTable, DataRow table,
      MySqlTokenizer tokenizer, bool includeColumns)
    {
      string name = tokenizer.NextToken();
      DataRow row = fkTable.NewRow();

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

      ArrayList srcColumns = includeColumns ? ParseColumns(tokenizer) : null;

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
      ArrayList targetColumns = includeColumns ? ParseColumns(tokenizer) : null;

      if (includeColumns)
        ProcessColumns(fkTable, row, srcColumns, targetColumns);
      else
        fkTable.Rows.Add(row);
    }

    private static ArrayList ParseColumns(MySqlTokenizer tokenizer)
    {
      ArrayList sc = new ArrayList();
      string token = tokenizer.NextToken();
      while (token != ")")
      {
        if (token != ",")
          sc.Add(token);
        token = tokenizer.NextToken();
      }
      return sc;
    }

    private static void ProcessColumns(DataTable fkTable, DataRow row,
      ArrayList srcColumns, ArrayList targetColumns)
    {
      for (int i = 0; i < srcColumns.Count; i++)
      {
        DataRow newRow = fkTable.NewRow();
        newRow.ItemArray = row.ItemArray;
        newRow["COLUMN_NAME"] = (string)srcColumns[i];
        newRow["ORDINAL_POSITION"] = i;
        newRow["REFERENCED_COLUMN_NAME"] = (string)targetColumns[i];
        fkTable.Rows.Add(newRow);
      }
    }

    #endregion

    public virtual DataTable GetUsers(string[] restrictions)
    {
      StringBuilder sb = new StringBuilder("SELECT Host, User FROM mysql.user");
      if (restrictions != null && restrictions.Length > 0)
        sb.AppendFormat(CultureInfo.InvariantCulture, " WHERE User LIKE '{0}'", restrictions[0]);

      MySqlDataAdapter da = new MySqlDataAdapter(sb.ToString(), connection);
      DataTable dt = new DataTable();
      da.Fill(dt);
      dt.TableName = "Users";
      dt.Columns[0].ColumnName = "HOST";
      dt.Columns[1].ColumnName = "USERNAME";

      return dt;
    }

    public virtual DataTable GetProcedures(string[] restrictions)
    {
      DataTable dt = new DataTable("Procedures");
      dt.Columns.Add(new DataColumn("SPECIFIC_NAME", typeof(string)));
      dt.Columns.Add(new DataColumn("ROUTINE_CATALOG", typeof(string)));
      dt.Columns.Add(new DataColumn("ROUTINE_SCHEMA", typeof(string)));
      dt.Columns.Add(new DataColumn("ROUTINE_NAME", typeof(string)));
      dt.Columns.Add(new DataColumn("ROUTINE_TYPE", typeof(string)));
      dt.Columns.Add(new DataColumn("DTD_IDENTIFIER", typeof(string)));
      dt.Columns.Add(new DataColumn("ROUTINE_BODY", typeof(string)));
      dt.Columns.Add(new DataColumn("ROUTINE_DEFINITION", typeof(string)));
      dt.Columns.Add(new DataColumn("EXTERNAL_NAME", typeof(string)));
      dt.Columns.Add(new DataColumn("EXTERNAL_LANGUAGE", typeof(string)));
      dt.Columns.Add(new DataColumn("PARAMETER_STYLE", typeof(string)));
      dt.Columns.Add(new DataColumn("IS_DETERMINISTIC", typeof(string)));
      dt.Columns.Add(new DataColumn("SQL_DATA_ACCESS", typeof(string)));
      dt.Columns.Add(new DataColumn("SQL_PATH", typeof(string)));
      dt.Columns.Add(new DataColumn("SECURITY_TYPE", typeof(string)));
      dt.Columns.Add(new DataColumn("CREATED", typeof(DateTime)));
      dt.Columns.Add(new DataColumn("LAST_ALTERED", typeof(DateTime)));
      dt.Columns.Add(new DataColumn("SQL_MODE", typeof(string)));
      dt.Columns.Add(new DataColumn("ROUTINE_COMMENT", typeof(string)));
      dt.Columns.Add(new DataColumn("DEFINER", typeof(string)));

      StringBuilder sql = new StringBuilder("SELECT * FROM mysql.proc WHERE 1=1");
      if (restrictions != null)
      {
        if (restrictions.Length >= 2 && restrictions[1] != null)
          sql.AppendFormat(CultureInfo.InvariantCulture,
            " AND db LIKE '{0}'", restrictions[1]);
        if (restrictions.Length >= 3 && restrictions[2] != null)
          sql.AppendFormat(CultureInfo.InvariantCulture,
            " AND name LIKE '{0}'", restrictions[2]);
        if (restrictions.Length >= 4 && restrictions[3] != null)
          sql.AppendFormat(CultureInfo.InvariantCulture,
            " AND type LIKE '{0}'", restrictions[3]);
      }

      MySqlCommand cmd = new MySqlCommand(sql.ToString(), connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.Read())
        {
          DataRow row = dt.NewRow();
          row["SPECIFIC_NAME"] = reader.GetString("specific_name");
          row["ROUTINE_CATALOG"] = DBNull.Value;
          row["ROUTINE_SCHEMA"] = reader.GetString("db");
          row["ROUTINE_NAME"] = reader.GetString("name");
          string routineType = reader.GetString("type");
          row["ROUTINE_TYPE"] = routineType;
          row["DTD_IDENTIFIER"] = routineType.ToLower(CultureInfo.InvariantCulture) == "function" ?
            (object)reader.GetString("returns") : DBNull.Value;
          row["ROUTINE_BODY"] = "SQL";
          row["ROUTINE_DEFINITION"] = reader.GetString("body");
          row["EXTERNAL_NAME"] = DBNull.Value;
          row["EXTERNAL_LANGUAGE"] = DBNull.Value;
          row["PARAMETER_STYLE"] = "SQL";
          row["IS_DETERMINISTIC"] = reader.GetString("is_deterministic");
          row["SQL_DATA_ACCESS"] = reader.GetString("sql_data_access");
          row["SQL_PATH"] = DBNull.Value;
          row["SECURITY_TYPE"] = reader.GetString("security_type");
          row["CREATED"] = reader.GetDateTime("created");
          row["LAST_ALTERED"] = reader.GetDateTime("modified");
          row["SQL_MODE"] = reader.GetString("sql_mode");
          row["ROUTINE_COMMENT"] = reader.GetString("comment");
          row["DEFINER"] = reader.GetString("definer");
          dt.Rows.Add(row);
        }
      }

      return dt;
    }

    protected virtual DataTable GetCollections()
    {
      object[][] collections = new object[][]
        {
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

      DataTable dt = new DataTable("MetaDataCollections");
      dt.Columns.Add(new DataColumn("CollectionName", typeof(string)));
      dt.Columns.Add(new DataColumn("NumberOfRestrictions", typeof(int)));
      dt.Columns.Add(new DataColumn("NumberOfIdentifierParts", typeof(int)));

      FillTable(dt, collections);

      return dt;
    }

    private DataTable GetDataSourceInformation()
    {
#if CF
      throw new NotSupportedException();
#else
      DataTable dt = new DataTable("DataSourceInformation");
      dt.Columns.Add("CompositeIdentifierSeparatorPattern", typeof(string));
      dt.Columns.Add("DataSourceProductName", typeof(string));
      dt.Columns.Add("DataSourceProductVersion", typeof(string));
      dt.Columns.Add("DataSourceProductVersionNormalized", typeof(string));
      dt.Columns.Add("GroupByBehavior", typeof(GroupByBehavior));
      dt.Columns.Add("IdentifierPattern", typeof(string));
      dt.Columns.Add("IdentifierCase", typeof(IdentifierCase));
      dt.Columns.Add("OrderByColumnsInSelect", typeof(bool));
      dt.Columns.Add("ParameterMarkerFormat", typeof(string));
      dt.Columns.Add("ParameterMarkerPattern", typeof(string));
      dt.Columns.Add("ParameterNameMaxLength", typeof(int));
      dt.Columns.Add("ParameterNamePattern", typeof(string));
      dt.Columns.Add("QuotedIdentifierPattern", typeof(string));
      dt.Columns.Add("QuotedIdentifierCase", typeof(IdentifierCase));
      dt.Columns.Add("StatementSeparatorPattern", typeof(string));
      dt.Columns.Add("StringLiteralPattern", typeof(string));
      dt.Columns.Add("SupportedJoinOperators", typeof(SupportedJoinOperators));

      DBVersion v = connection.driver.Version;
      string ver = String.Format("{0:0}.{1:0}.{2:0}",
                     v.Major, v.Minor, v.Build);

      DataRow row = dt.NewRow();
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
#endif
    }

    private static DataTable GetDataTypes()
    {
      DataTable dt = new DataTable("DataTypes");
      dt.Columns.Add(new DataColumn("TypeName", typeof(string)));
      dt.Columns.Add(new DataColumn("ProviderDbType", typeof(int)));
      dt.Columns.Add(new DataColumn("ColumnSize", typeof(long)));
      dt.Columns.Add(new DataColumn("CreateFormat", typeof(string)));
      dt.Columns.Add(new DataColumn("CreateParameters", typeof(string)));
      dt.Columns.Add(new DataColumn("DataType", typeof(string)));
      dt.Columns.Add(new DataColumn("IsAutoincrementable", typeof(bool)));
      dt.Columns.Add(new DataColumn("IsBestMatch", typeof(bool)));
      dt.Columns.Add(new DataColumn("IsCaseSensitive", typeof(bool)));
      dt.Columns.Add(new DataColumn("IsFixedLength", typeof(bool)));
      dt.Columns.Add(new DataColumn("IsFixedPrecisionScale", typeof(bool)));
      dt.Columns.Add(new DataColumn("IsLong", typeof(bool)));
      dt.Columns.Add(new DataColumn("IsNullable", typeof(bool)));
      dt.Columns.Add(new DataColumn("IsSearchable", typeof(bool)));
      dt.Columns.Add(new DataColumn("IsSearchableWithLike", typeof(bool)));
      dt.Columns.Add(new DataColumn("IsUnsigned", typeof(bool)));
      dt.Columns.Add(new DataColumn("MaximumScale", typeof(short)));
      dt.Columns.Add(new DataColumn("MinimumScale", typeof(short)));
      dt.Columns.Add(new DataColumn("IsConcurrencyType", typeof(bool)));
      dt.Columns.Add(new DataColumn("IsLiteralSupported", typeof(bool)));
      dt.Columns.Add(new DataColumn("LiteralPrefix", typeof(string)));
      dt.Columns.Add(new DataColumn("LiteralSuffix", typeof(string)));
      dt.Columns.Add(new DataColumn("NativeDataType", typeof(string)));

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

    protected virtual DataTable GetRestrictions()
    {
      object[][] restrictions = new object[][]
        {
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

      DataTable dt = new DataTable("Restrictions");
      dt.Columns.Add(new DataColumn("CollectionName", typeof(string)));
      dt.Columns.Add(new DataColumn("RestrictionName", typeof(string)));
      dt.Columns.Add(new DataColumn("RestrictionDefault", typeof(string)));
      dt.Columns.Add(new DataColumn("RestrictionNumber", typeof(int)));

      FillTable(dt, restrictions);

      return dt;
    }

    private static DataTable GetReservedWords()
    {
      DataTable dt = new DataTable("ReservedWords");
      dt.Columns.Add(new DataColumn(DbMetaDataColumnNames.ReservedWord, typeof(string)));

      Stream str = Assembly.GetExecutingAssembly().GetManifestResourceStream(
        "MySql.Data.MySqlClient.Properties.ReservedWords.txt");
      StreamReader sr = new StreamReader(str);
      string line = sr.ReadLine();
      while (line != null)
      {
        string[] keywords = line.Split(new char[] { ' ' });
        foreach (string s in keywords)
        {
          if (String.IsNullOrEmpty(s)) continue;
          DataRow row = dt.NewRow();
          row[0] = s;
          dt.Rows.Add(row);
        }
        line = sr.ReadLine();
      }
      sr.Close();
      str.Close();

      return dt;
    }

    protected static void FillTable(DataTable dt, object[][] data)
    {
      foreach (object[] dataItem in data)
      {
        DataRow row = dt.NewRow();
        for (int i = 0; i < dataItem.Length; i++)
          row[i] = dataItem[i];
        dt.Rows.Add(row);
      }
    }

    private void FindTables(DataTable schemaTable, string[] restrictions)
    {
      StringBuilder sql = new StringBuilder();
      StringBuilder where = new StringBuilder();
      sql.AppendFormat(CultureInfo.InvariantCulture,
               "SHOW TABLE STATUS FROM `{0}`", restrictions[1]);
      if (restrictions != null && restrictions.Length >= 3 &&
        restrictions[2] != null)
        where.AppendFormat(CultureInfo.InvariantCulture,
                   " LIKE '{0}'", restrictions[2]);
      sql.Append(where.ToString());

      string table_type = restrictions[1].ToLower() == "information_schema"
                  ?
                "SYSTEM VIEW"
                  : "BASE TABLE";

      MySqlCommand cmd = new MySqlCommand(sql.ToString(), connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.Read())
        {
          DataRow row = schemaTable.NewRow();
          row["TABLE_CATALOG"] = null;
          row["TABLE_SCHEMA"] = restrictions[1];
          row["TABLE_NAME"] = reader.GetString(0);
          row["TABLE_TYPE"] = table_type;
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
          schemaTable.Rows.Add(row);
        }
      }
    }

    private static string GetString(MySqlDataReader reader, int index)
    {
      if (reader.IsDBNull(index))
        return null;
      return reader.GetString(index);
    }

    public virtual DataTable GetUDF(string[] restrictions)
    {
      string sql = "SELECT name,ret,dl FROM mysql.func";
      if (restrictions != null)
      {
        if (restrictions.Length >= 1 && !String.IsNullOrEmpty(restrictions[0]))
          sql += String.Format(" WHERE name LIKE '{0}'", restrictions[0]);
      }

      DataTable dt = new DataTable("User-defined Functions");
      dt.Columns.Add("NAME", typeof(string));
      dt.Columns.Add("RETURN_TYPE", typeof(int));
      dt.Columns.Add("LIBRARY_NAME", typeof(string));

      MySqlCommand cmd = new MySqlCommand(sql, connection);
      try
      {
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            DataRow row = dt.NewRow();
            row[0] = reader.GetString(0);
            row[1] = reader.GetInt32(1);
            row[2] = reader.GetString(2);
            dt.Rows.Add(row);
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

    protected virtual DataTable GetSchemaInternal(string collection, string[] restrictions)
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
          return GetUsers(restrictions);
        case "DATABASES":
          return GetDatabases(restrictions);
        case "UDF":
          return GetUDF(restrictions);
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
          return GetTables(restrictions);
        case "COLUMNS":
          return GetColumns(restrictions);
        case "INDEXES":
          return GetIndexes(restrictions);
        case "INDEXCOLUMNS":
          return GetIndexColumns(restrictions);
        case "FOREIGN KEYS":
          return GetForeignKeys(restrictions);
        case "FOREIGN KEY COLUMNS":
          return GetForeignKeyColumns(restrictions);
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
  }
}
