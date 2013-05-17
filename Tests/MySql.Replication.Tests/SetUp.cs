// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace MySql.Replication.Tests
{
  public class SetUp : IDisposable
  {
    protected internal string databaseName;
    protected internal int masterPort;
    protected internal int slavePort;
    protected internal string groupName;
    private MySqlConnectionStringBuilder connStringRootMaster;
    private MySqlConnectionStringBuilder connStringSlave;

    protected internal string ConnectionString
    {
      get
      {
        return string.Format("{0}database={1};", ConnectionStringNoDb, databaseName);
      }
    }

    protected internal string ConnectionStringNoDb
    {
      get
      {
        return string.Format("server={0};", groupName);
      }
    }

    protected internal string ConnectionStringRootMaster
    {
      get
      {
        return connStringRootMaster.ConnectionString;
      }
    }

    protected internal string ConnectionStringSlave
    {
      get
      {
        return connStringSlave.ConnectionString;
      }
    }

    public SetUp()
    {
      Initialize();
      LoadBaseConfiguration();

      using (MySqlConnection connection = new MySqlConnection(ConnectionStringRootMaster))
      {
        connection.Open();
        MySqlScript script = new MySqlScript(connection);

        // Sets users
        script.Query = Properties.Resources._01_Startup_root_script;
        script.Execute();

        // Sets database objects
        script.Query = string.Format(Properties.Resources._02_Startup_script, databaseName);
        script.Execute();
      }
    }

    protected internal void LoadBaseConfiguration()
    {
      ReplicationServerGroupConfigurationElement group1 = (ConfigurationManager.GetSection("MySQL") as MySqlConfiguration).Replication.ServerGroups.ToArray()[0];
      ReplicationServerConfigurationElement masterConfiguration = group1.Servers.ToArray()[0];
      ReplicationServerConfigurationElement slaveConfiguration = group1.Servers.ToArray()[1];

      groupName = group1.Name;

      connStringRootMaster = new MySqlConnectionStringBuilder(masterConfiguration.ConnectionString);
      masterPort = (int)connStringRootMaster.Port;

      connStringSlave = new MySqlConnectionStringBuilder(slaveConfiguration.ConnectionString);
      slavePort = (int)connStringSlave.Port;
      connStringSlave.Database = databaseName;
    }

    protected virtual void Initialize()
    {
      // we don't use FileVersion because it's not available
      // on the compact framework
      string fullname = Assembly.GetExecutingAssembly().FullName;
      string[] parts = fullname.Split(new char[] { '=' });
      string[] versionParts = parts[1].Split(new char[] { '.' });
      databaseName = String.Format("dblb{0}{1}", versionParts[0], versionParts[1]);
    }
    
  
    public void Dispose()
    {
      using (MySqlConnection connection = new MySqlConnection(ConnectionStringRootMaster))
      {
        connection.Open();
        MySqlCommand cmd = new MySqlCommand(string.Empty, connection);

        cmd.CommandText = "DROP USER lbuser@localhost;";
        cmd.CommandText += "DROP DATABASE IF EXISTS " + databaseName;
        cmd.ExecuteNonQuery();
      }
    }

    protected internal MySqlDataReader ExecuteQuery(MySqlConnection connection, string query)
    {
      MySqlCommand cmd = new MySqlCommand(query, connection);
      return cmd.ExecuteReader();
    }

    protected internal int ExecuteNonQuery(MySqlConnection connection, string query)
    {
      MySqlCommand cmd = new MySqlCommand(query, connection);
      return cmd.ExecuteNonQuery();
    }

    private string ValueIfEmpty(string value, string valueIfEmtpy)
    {
      if (string.IsNullOrEmpty(value)) return valueIfEmtpy;
      return value;
    }
  }
}
