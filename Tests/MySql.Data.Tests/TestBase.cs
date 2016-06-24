// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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

namespace MySql.Data.MySqlClient.Tests
{
  public class TestBase : IDisposable
  {

    protected static string databaseName;
    protected static MySqlConnectionStringBuilder Settings;
    protected static MySqlConnection root;
    protected MySqlConnection connection;

    static TestBase()
    {
      databaseName = "db0";

      Settings = new MySqlConnectionStringBuilder();
      Settings.Server = "localhost";
      Settings.UserID = "root";
      Settings.Password = "";
      Settings.Database = "mysql";
      Settings.AllowUserVariables = true;
      Settings.Pooling = false;
      Settings.PersistSecurityInfo = true;
      root = new MySqlConnection(Settings.GetConnectionString(true));
      root.Open();

      Settings.UserID = "user0";
      Settings.Password = "pwd";
      Settings.Database = databaseName;
    }

    public TestBase()
    {
      // cleanup
      for (int x = 0; x < 3; x++)
      {
        executeAsRoot("DROP DATABASE IF EXISTS db" + x);
        executeAsRoot(String.Format("DROP USER IF EXISTS 'user{0}'@'localhost'", x));
      }
      executeAsRoot("FLUSH PRIVILEGES");

      executeAsRoot("CREATE DATABASE " + databaseName);
      executeAsRoot("CREATE USER 'user0'@'localhost' IDENTIFIED BY 'pwd'");
      executeAsRoot(String.Format("GRANT ALL ON *.* TO 'user0'@'localhost'", databaseName));
      executeAsRoot("FLUSH PRIVILEGES");


      Settings.Database = databaseName;
      connection = new MySqlConnection(Settings.GetConnectionString(true));
      connection.Open();
    }

    private List<string> GetUserList(bool includeRoot)
    {
      var list = new List<string>();
      var reader = ExecuteReaderAsRoot("select user,host from mysql.user");
      while (reader.Read())
      {
        string user = reader.GetString(0);
        if (user == "root" && !includeRoot) continue;
        list.Add(String.Format("'{0}'@'{1}'", reader.GetString(0), reader.GetString(1)));
      }
      return list;
    }

    protected MySqlConnection GetConnection(bool asRoot = false)
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder(Settings.GetConnectionString(true));
      if (asRoot)
      {
        s.UserID = "root";
        s.Password = null;
      }
      return new MySqlConnection(s.GetConnectionString(true));
    }

    protected MySqlConnection GetRoot()
    {
      return GetConnection(true);
    }

    private void executeInternal(string sql, MySqlConnection conn)
    {
      var cmd = conn.CreateCommand();
      cmd.CommandText = sql;
      cmd.ExecuteNonQuery();
    }

    protected void executeSQL(string sql)
    {
      executeInternal(sql, connection);
    }

    protected void executeAsRoot(string sql)
    {
      executeInternal(sql, root);
    }

    protected MySqlDataReader ExecuteReaderAsRoot(string sql)
    {
      MySqlConnection root = GetRoot();
      root.Open();
      MySqlCommand cmd = new MySqlCommand(sql, root);
      return cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
    }

    protected void KillConnection(MySqlConnection c)
    {
      int threadId = c.ServerThread;
      var root = GetRoot();
      root.Open();
      MySqlCommand cmd = new MySqlCommand("KILL " + threadId, root);
      cmd.ExecuteNonQuery();

      // the kill flag might need a little prodding to do its thing
      try
      {
        cmd.CommandText = "SELECT 1";
        cmd.Connection = c;
        cmd.ExecuteNonQuery();
      }
      catch (Exception) { }

      // now wait till the process dies
      bool processStillAlive = false;
      while (true)
      {
        MySqlCommand cmdProcess = new MySqlCommand("SHOW PROCESSLIST", root);
        MySqlDataReader dr = cmdProcess.ExecuteReader();
        while (dr.Read())
        {
          if (dr.GetInt32(0) == threadId) processStillAlive = true;
        }
        dr.Close();

        if (!processStillAlive) break;
        System.Threading.Thread.Sleep(500);
      }
    }

    public void Dispose()
    {
    }
  }
}
