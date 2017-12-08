// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.Resources;
using MySql.Data.MySqlClient;
using MySql.Web.Security;
using System.Configuration;
using MySql.Web.Common;
using System.Data;
using System.IO;

namespace MySql.Web.Tests
{
  public class WebTestBase
  {
    protected MySqlConnection Connection;
    protected string ConnectionString;
    protected uint Port;

    public WebTestBase()
    {
      var strPort = Environment.GetEnvironmentVariable("MYSQL_PORT");
      Port = strPort == null ? 3306 : UInt32.Parse(strPort);
      Init();
      ConnectionString = $"server=localhost;uid=root;database=mysqlweb;pooling=false;port={Port}";
      Connection = new MySqlConnection(ConnectionString);
      Connection.Open();
      InitSchema();
      AddConnectionStringToConfigFile();
    }


    protected virtual void Init()
    {
      ConnectionString = $"server=localhost;uid=root;database=mysql;pooling=false;port={Port}";
      Connection = new MySqlConnection(ConnectionString);
      Connection.Open();
      execSQL($"DROP DATABASE IF EXISTS `mysqlweb`");
      execSQL($"CREATE DATABASE `mysqlweb`");
      Connection.Close();
    }

    protected virtual void InitSchema()
    {
      for (int ver = 1; ver <= SchemaManager.Version; ver++)
        LoadSchema(ver);
    }

    private void AddConnectionStringToConfigFile()
    {
      Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      config.ConnectionStrings.ConnectionStrings.Remove("LocalMySqlServer");
      ConnectionStringSettings css = new ConnectionStringSettings();
      css.ConnectionString = ConnectionString;
      css.Name = "LocalMySqlServer";
      config.ConnectionStrings.ConnectionStrings.Add(css);
      config.Save();
      ConfigurationManager.RefreshSection("connectionStrings");
    }

    public bool TableExists(string tableName)
    {
      MySqlCommand cmd = new MySqlCommand($"SELECT * FROM {tableName} LIMIT 0", Connection);
      try
      {
        cmd.ExecuteScalar();
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public DataTable FillTable(string sql)
    {
      DataTable dt = new DataTable();
      MySqlDataAdapter da = new MySqlDataAdapter(sql, Connection);
      da.Fill(dt);
      return dt;
    }

    public void execSQL(string sql)
    {
      MySqlCommand cmd = new MySqlCommand(sql, Connection);
      cmd.ExecuteNonQuery();
    }

    private string LoadResource(string name)
    {
      var assembly = typeof(MySQLMembershipProvider).Assembly;
      using (Stream stream = assembly.GetManifestResourceStream(name))
      using (StreamReader reader = new StreamReader(stream))
        return reader.ReadToEnd();
    }

    internal protected void LoadSchema(int version)
    {
      if (version < 1) return;

      MySQLMembershipProvider provider = new MySQLMembershipProvider();
      string schema = LoadResource($"MySql.Web.Properties.schema{version}.sql");
      MySqlScript script = new MySqlScript(Connection);
      script.Query = schema.ToString();

      try
      {
        script.Execute();
      }
      catch (MySqlException ex)
      {
        if (ex.Number == 1050 && version == 7)
        {
          // Schema7 performs several renames of tables to their lowercase representation. 
          // If the current server OS does not support renaming to lowercase, then let's just continue.          
          script.Query = "UPDATE my_aspnet_schemaversion SET version=7";
          script.Execute();
        }
      }
    }
  }
}

