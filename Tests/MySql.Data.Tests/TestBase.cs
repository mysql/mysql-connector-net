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
using System.ComponentModel;
using System.Data;
using System.Reflection;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class TestBase : IClassFixture<TestSetup>, IDisposable
  {
    protected MySqlConnection connection;
    protected MySqlConnection customConnection;
    protected TestSetup Setup;
    protected string TestNameSpace;


    public TestBase(TestSetup setup, string ns)
    {
      Setup = setup;
      TestNameSpace = ns;
      Init();
      connection = Setup.GetConnection();
      connection.Open();
    }

    protected virtual string Namespace
    {
      get { return null; }
    }

    protected virtual void Init()
    {
      Setup.Init(Namespace ?? TestNameSpace);
    }

    protected MySqlConnectionStringBuilder Settings
    {
      get { return Setup.Settings; }
    }

    protected string CreateUser(string postfix, string pwd)
    {
      return Setup.CreateUser(postfix, pwd);
    }

    protected string CreateDatabase(string postfix)
    {
      return Setup.CreateDatabase(postfix);
    }

    protected MySqlConnectionStringBuilder GetConnectionSettings()
    {
      return new MySqlConnectionStringBuilder(Settings.GetConnectionString(true));
    }

    protected MySqlConnection GetConnection(bool asRoot=false)
    {
      return Setup.GetConnection(asRoot);
    }

    protected virtual string OnGetConnectionStringInfo()
    {
      return Settings.GetConnectionString(true);
    }


    protected MySqlDataReader ExecuteAsReader(string sql, MySqlConnection conn)
    {
      return Setup.ExecuteReader(sql, conn);
    }

    protected void executeSQL(string sql)
    {
      Setup.executeInternal(sql, connection);
    }

    protected void executeAsRoot(string sql)
    {
      Setup.executeInternal(sql, Setup.root);
    }

    protected void KillConnection(MySqlConnection c)
    {
      int threadId = c.ServerThread;
      var root = Setup.GetConnection(true);
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

    public virtual void Dispose()
    {
      if (connection != null)
        connection.Close();
      if (customConnection != null)
        customConnection.Close();
    }
  }
}
