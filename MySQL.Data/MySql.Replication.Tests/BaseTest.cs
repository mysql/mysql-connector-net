// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Text;
using NUnit.Framework;
using System.Reflection;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace MySql.LoadBalancing.Tests
{
  public class BaseTest
  {
    protected string databaseName;
    protected int masterPort;
    protected int slavePort;
    protected string server;
    private MySqlConnectionStringBuilder connStringRootMaster;
    private MySqlConnectionStringBuilder connStringSlave;

    protected string ConnectionString
    {
      get
      {
        return string.Format("{0}database={1};", ConnectionStringNoDb, databaseName);
      }
    }

    protected string ConnectionStringNoDb
    {
      get
      {
        return string.Format("server={0};", server);
      }
    }

    protected string ConnectionStringRootMaster
    {
      get
      {
        return connStringRootMaster.ConnectionString;
      }
    }

    protected string ConnectionStringSlave
    {
      get
      {
        return connStringSlave.ConnectionString;
      }
    }



    public BaseTest()
    {
      Initialize();
      LoadBaseConfiguration();
    }

    protected void LoadBaseConfiguration()
    {
      string masterPortString = ValueIfEmpty(ConfigurationManager.AppSettings["masterPort"], "3305");
      string slavePortString = ValueIfEmpty(ConfigurationManager.AppSettings["slavePort"], "3307");
      server = ValueIfEmpty(ConfigurationManager.AppSettings["server"], "Group1");

      masterPort = int.Parse(masterPortString);
      slavePort = int.Parse(slavePortString);

      connStringRootMaster = new MySqlConnectionStringBuilder();
      connStringRootMaster.UserID = ValueIfEmpty(ConfigurationManager.AppSettings["rootuser"], "root");
      connStringRootMaster.Password = ValueIfEmpty(ConfigurationManager.AppSettings["rootpassword"], string.Empty);
      connStringRootMaster.Server = ValueIfEmpty(ConfigurationManager.AppSettings["host"], "localhost");
      connStringRootMaster.Port = (uint)masterPort;

      connStringSlave = new MySqlConnectionStringBuilder();
      connStringSlave.UserID = ValueIfEmpty(ConfigurationManager.AppSettings["slaveUser"], "lbuser");
      connStringSlave.Password = ValueIfEmpty(ConfigurationManager.AppSettings["slavePassword"], "lbpass");
      connStringSlave.Server = ValueIfEmpty(ConfigurationManager.AppSettings["slaveHost"], "localhost");
      connStringSlave.Port = (uint)slavePort;
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

    [SetUp]
    public void Setup()
    {
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

    //[TearDown]
    public void Teardown()
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

    protected MySqlDataReader ExecuteQuery(MySqlConnection connection, string query)
    {
      MySqlCommand cmd = new MySqlCommand(query, connection);
      return cmd.ExecuteReader();
    }

    protected int ExecuteNonQuery(MySqlConnection connection, string query)
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
