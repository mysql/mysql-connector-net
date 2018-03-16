// Copyright © 2016, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using System.Diagnostics;

namespace MySql.Data.MySqlClient.Tests
{
  public class TestFixture : IDisposable
  {
    internal protected string table;
    internal protected string csAdditions = String.Empty;
    internal protected bool accessToMySqlDb;
    private bool disposed = false;

    private TestBase TestClass { get; set; }
    protected string Namespace { get; set; }
    public MySqlConnectionStringBuilder Settings { get; set;  }
    public MySqlConnectionStringBuilder RootSettings { get; set; }
    protected string BaseDBName { get; set; }
    protected string BaseUserName { get; set;  }
    public Version Version { get; set; }
    public int MaxPacketSize { get; set; }
    public string UnixSocket { get; private set; } = Environment.GetEnvironmentVariable("MYSQL_SOCKET") ?? "/tmp/mysql.sock";

    public void Setup(TestBase testClass, bool reinitDatabase = false)
    {
      if (!String.IsNullOrEmpty(Namespace) && !reinitDatabase) return;

      Debug.Assert(testClass != null);

      TestClass = testClass;
      Namespace = testClass.GetType().Name.ToLower();
      string ns = Namespace.Length > 10 ? Namespace.Substring(0, 10) : Namespace;
      BaseDBName = "db-" + ns + "-";
      BaseUserName = "u-" + ns + "-"; 

      var settings = new MySqlConnectionStringBuilder();
      settings.Server = "localhost";
      var port = Environment.GetEnvironmentVariable("MYSQL_PORT");
      settings.Port = port == null ? 3306 : UInt32.Parse(port);
      settings.UserID = "root";
      settings.Password = null;
#if !(NETCOREAPP1_1 || NETCOREAPP2_0)
      var memName = Environment.GetEnvironmentVariable("MYSQL_MEM");
      settings.SharedMemoryName = memName == null ? "MySQLSocket" : memName;
      var pipeName = Environment.GetEnvironmentVariable("MYSQL_PIPE");
      settings.PipeName = pipeName == null ? "MySQLSocket" : pipeName;
#endif
      settings.PersistSecurityInfo = true;
      settings.AllowUserVariables = true;
      settings.Pooling = false;
      settings.IgnorePrepare = false;
      TestClass.AdjustConnectionSettings(settings);
      MaxPacketSize = 1000 * 1024;

      RootSettings = new MySqlConnectionStringBuilder(settings.GetConnectionString(true));
      Settings = new MySqlConnectionStringBuilder(settings.GetConnectionString(true));
      Version = GetVersion();
      InitializeDatabase();
    }

    private void InitializeDatabase()
    {
      CleanupDatabase();
      Settings.Database = CreateDatabase("0");
      Settings.UserID = CreateUser("0", "pwd");
      Settings.Password = "pwd";
    }

    protected void CleanupDatabase()
    {
      using (var root = GetConnection(true))
      {
        executeSQL("SET GLOBAL max_allowed_packet=" + MaxPacketSize, root);
        executeSQL("SET GLOBAL SQL_MODE = STRICT_ALL_TABLES", root);

        var data = Utils.FillTable("SHOW DATABASES", root);
        foreach (DataRow row in data.Rows)
        {

          string name = row[0].ToString();
          if (!name.StartsWith(BaseDBName)) continue;
          executeSQL(String.Format("DROP DATABASE IF EXISTS `{0}`", name), root);
        }
        data = Utils.FillTable(String.Format("SELECT user,host FROM mysql.user WHERE user LIKE '{0}%'", BaseUserName), root);
        foreach (DataRow row in data.Rows)
        {
          if (Version >= new Version("5.7"))
            executeSQL(String.Format("DROP USER IF EXISTS '{0}'@'{1}'", row[0].ToString(), row[1].ToString()), root);
          else
            executeSQL(String.Format("DROP USER '{0}'@'{1}'", row[0].ToString(), row[1].ToString()), root);
        }
        executeSQL("FLUSH PRIVILEGES", root);
      }
    }

    private Version GetVersion()
    {
      using (var root = GetConnection(true))
      {
        string versionString = root.ServerVersion;
        int i = 0;
        while (i < versionString.Length &&
            (Char.IsDigit(versionString[i]) || versionString[i] == '.'))
          i++;

        return new Version(versionString.Substring(0, i));
      }
    }

    public MySqlConnection GetConnection(bool asRoot = false)
    {
      var s = asRoot ? RootSettings : Settings;
      var conn = new MySqlConnection(s.GetConnectionString(true));
      conn.Open();
      return conn;
    }

    public string CreateDatabase(string postfix)
    {
      using (var connection = GetConnection(true))
      {
        string dbName = String.Format("{0}{1}", BaseDBName, postfix);
        executeSQL(String.Format("CREATE DATABASE `{0}`", dbName), connection);
        return dbName;
      }
    }

    public string DropDatabase(string postfix)
    {
      using (var connection = GetConnection(true))
      {
        string dbName = String.Format("{0}{1}", BaseDBName, postfix);
        executeSQL(String.Format("DROP DATABASE `{0}`", dbName), connection);
        return dbName;
      }
    }

    public string CreateUser(string postfix, string password)
    {
      using (var connection = GetConnection(true))
      {
        string userName = String.Format("{0}{1}", BaseUserName, postfix);
        if (Version >= new Version("5.7"))
        executeSQL(String.Format("CREATE USER '{0}'@'localhost' IDENTIFIED WITH mysql_native_password BY '{1}'", userName, password), connection);
        else
          executeSQL(String.Format("CREATE USER '{0}'@'localhost' IDENTIFIED BY '{1}'", userName, password), connection);
        executeSQL(String.Format("GRANT ALL ON *.* TO '{0}'@'localhost'", userName), connection);
        executeSQL("FLUSH PRIVILEGES", connection);
        return userName;
      }
    }

    public string CreateUser(string userName, string password, string plugin)
    {
      using (var connection = GetConnection(true))
      {
        if (Version >= new Version("5.7"))
        {
          executeSQL(String.Format("DROP USER IF EXISTS '{0}'@'localhost';", userName), connection);
          executeSQL(
          String.Format(
            "CREATE USER '{0}'@'localhost' IDENTIFIED {1} BY '{2}'",
            userName,
            (plugin == null ? string.Empty : String.Format("WITH '{0}' ", plugin)), password),
          connection);
        }
        else
        {
          var cmd = connection.CreateCommand();
          cmd.CommandText = String.Format("SELECT count(*) FROM mysql.user WHERE user LIKE '{0}%'", userName);
          if ((long)cmd.ExecuteScalar() > 0)
            executeSQL(String.Format("DROP USER '{0}'@'localhost';", userName), connection);
          executeSQL(String.Format("CREATE USER '{0}'@'localhost' IDENTIFIED WITH '{1}'", userName, plugin), connection);
          if (plugin=="sha256_password") executeSQL("SET old_passwords = 2", connection);
          executeSQL(String.Format("SET PASSWORD FOR '{0}'@'localhost' = PASSWORD('{1}')", userName, password), connection);
        }

        executeSQL(String.Format("GRANT ALL ON *.* TO '{0}'@'localhost'", userName), connection);
        executeSQL("FLUSH PRIVILEGES", connection);
        return userName;
      }
    }

    private void executeSQL(string sql, MySqlConnection connection)
    {
      var cmd = connection.CreateCommand();
      cmd.CommandText = sql;
      cmd.ExecuteNonQuery();
    }

    internal protected string GetPoolingConnectionString()
    {
      RootSettings.Database = BaseDBName + "0";
      MySqlConnectionStringBuilder csBuilder = new MySqlConnectionStringBuilder(RootSettings.GetConnectionString(false));
      csBuilder.Pooling = true;
      return csBuilder.ToString();
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposed)
        return;

      if (disposing)
      {
        MySqlConnection.ClearAllPools();
        CleanupDatabase();        
      }

      disposed = true;

    }

    public virtual void Dispose()
    {
      Dispose(true);
    }
  }
}
