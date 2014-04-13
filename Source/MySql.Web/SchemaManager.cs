// Copyright © 2004, 2014, Oracle and/or its affiliates. All rights reserved.
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

//  This code was contributed by Sean Wright (srwright@alcor.concordia.ca) on 2007-01-12
//  The copyright was assigned and transferred under the terms of
//  the MySQL Contributor License Agreement (CLA)

using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Resources;
using System.IO;
using System.Diagnostics;
using MySql.Web.Properties;

namespace MySql.Web.Common
{
  /// <summary>
  /// 
  /// </summary>
  public static class SchemaManager
  {
    private const int schemaVersion = 10;

    /// <summary>
    /// Gets the most recent version of the schema.
    /// </summary>
    /// <value>The most recent version number of the schema.</value>
    public static int Version
    {
      get { return schemaVersion; }
    }

    internal static void CheckSchema(string connectionString, NameValueCollection config)
    {
      int ver = GetSchemaVersion(connectionString);
      if (ver == Version) return;

      try
      {
        if (String.Compare(config["autogenerateschema"], "true", true) == 0)
          UpgradeToCurrent(connectionString, ver);
        else
          throw new ProviderException(Resources.MissingOrWrongSchema);

      }
      catch (Exception ex)
      {
        throw new ProviderException(Resources.MissingOrWrongSchema, ex);
      }
    }

    private static void UpgradeToCurrent(string connectionString, int version)
    {
      ResourceManager r = new ResourceManager("MySql.Web.Properties.Resources",
          typeof(SchemaManager).Assembly);

      if (version == Version) return;

      using (MySqlConnection connection = new MySqlConnection(connectionString))
      {
        connection.Open();

        for (int ver = version + 1; ver <= Version; ver++)
        {
          string schema = r.GetString(String.Format("schema{0}", ver));
          MySqlScript script = new MySqlScript(connection);
          script.Query = schema;

          try
          {
            script.Execute();
          }
          catch (MySqlException ex)
          {
            if (ex.Number == 1050 && ver == 7)
            {
              // Schema7 performs several renames of tables to their lowercase representation. 
              // If the current server OS does not support renaming to lowercase, then let's just continue.             
              script.Query = "UPDATE my_aspnet_schemaversion SET version=7";
              script.Execute();
              continue;
            }
            throw ex;
          }
        }
      }
    }

    private static int GetSchemaVersion(string connectionString)
    {
      // retrieve the current schema version
      using (MySqlConnection conn = new MySqlConnection(connectionString))
      {
        conn.Open();

        MySqlCommand cmd = new MySqlCommand("SELECT * FROM my_aspnet_schemaversion", conn);
        try
        {
          object ver = cmd.ExecuteScalar();
          if (ver != null)
            return (int)ver;
        }
        catch (MySqlException ex)
        {
          if (ex.Number != (int)MySqlErrorCode.NoSuchTable)
            throw;
          string[] restrictions = new string[4];
          restrictions[2] = "mysql_membership";
          DataTable dt = conn.GetSchema("Tables", restrictions);
          if (dt.Rows.Count == 1)
            return Convert.ToInt32(dt.Rows[0]["TABLE_COMMENT"]);
        }
        return 0;
      }
    }

    /// <summary>
    /// Creates the or fetch user id.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="username">The username.</param>
    /// <param name="applicationId">The application id.</param>
    /// <param name="authenticated">if set to <c>true</c> [authenticated].</param>
    /// <returns></returns>
    internal static long CreateOrFetchUserId(MySqlConnection connection, string username,
        long applicationId, bool authenticated)
    {
      Debug.Assert(applicationId > 0);

      // first attempt to fetch an existing user id
      MySqlCommand cmd = new MySqlCommand(@"SELECT id FROM my_aspnet_users
                WHERE applicationId = @appId AND name = @name", connection);
      cmd.Parameters.AddWithValue("@appId", applicationId);
      cmd.Parameters.AddWithValue("@name", username);
      object userId = cmd.ExecuteScalar();
      if (userId != null) return (int)userId;

      cmd.CommandText = @"INSERT INTO my_aspnet_users VALUES 
                (NULL, @appId, @name, @isAnon, Now())";
      cmd.Parameters.AddWithValue("@isAnon", !authenticated);
      int recordsAffected = cmd.ExecuteNonQuery();
      if (recordsAffected != 1)
        throw new ProviderException(Resources.UnableToCreateUser);

      cmd.CommandText = "SELECT LAST_INSERT_ID()";
      return Convert.ToInt64(cmd.ExecuteScalar());
    }
  }
}