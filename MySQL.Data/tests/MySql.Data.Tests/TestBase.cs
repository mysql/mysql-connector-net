// Copyright (c) 2016, 2020 Oracle and/or its affiliates.
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
using NUnit.Framework;

namespace MySql.Data.MySqlClient.Tests
{
  public class TestBase
  {
    protected string Namespace { get; set; }
    public MySqlConnectionStringBuilder Settings { get; set; }
    public MySqlConnectionStringBuilder RootSettings { get; set; }
    protected string BaseDBName { get; set; }
    protected string BaseUserName { get; set; }
    public Version Version { get; set; }
    public int MaxPacketSize { get; set; }
    public string UnixSocket { get; private set; } = Environment.GetEnvironmentVariable("MYSQL_SOCKET") ?? "/tmp/mysql.sock";

    protected MySqlConnection Connection { get; set; }
    protected MySqlConnection Root { get; set; }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      Namespace = this.GetType().Name.ToLower();
      string ns = Namespace.Length > 10 ? Namespace.Substring(0, 10) : Namespace;
      BaseDBName = "db-" + ns + "-";
      BaseUserName = "u-" + ns + "-";

      var settings = new MySqlConnectionStringBuilder();
      settings.Server = "localhost";
      var port = Environment.GetEnvironmentVariable("MYSQL_PORT");
      settings.Port = port == null ? 3306 : UInt32.Parse(port);
      settings.UserID = "root";
      settings.Password = null;
#if NET452
      var memName = Environment.GetEnvironmentVariable("MYSQL_MEM");
      settings.SharedMemoryName = memName == null ? "MySQLSocket" : memName;
      var pipeName = Environment.GetEnvironmentVariable("MYSQL_PIPE");
      settings.PipeName = pipeName == null ? "MySQLSocket" : pipeName;
#endif
      settings.PersistSecurityInfo = true;
      settings.AllowUserVariables = true;
      settings.Pooling = false;
      settings.ConnectionTimeout = 600;
      AdjustConnectionSettings(settings);
      MaxPacketSize = 1000 * 1024;

      RootSettings = new MySqlConnectionStringBuilder(settings.GetConnectionString(true));
      Settings = new MySqlConnectionStringBuilder(settings.GetConnectionString(true));
      Version = GetVersion();
      Debug.Assert(!string.IsNullOrEmpty(BaseDBName));
      InitializeDatabase();

      Connection = GetConnection(false);
      Root = GetConnection(true);
    }

    [OneTimeTearDown]
    public void OneTimeTearDownAttribute()
    {
      MySqlConnection.ClearAllPools();
      CleanupDatabase();

      if (Connection != null && Connection.State == ConnectionState.Open)
        Connection.Close();
      if (Root != null && Root.State == ConnectionState.Open)
        Root.Close();
    }

    [TearDown]
    public void TearDown()
    {
      Cleanup();
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
        ExecuteSQL("SET GLOBAL max_allowed_packet=" + MaxPacketSize, root);  // Need to fix for BlobTest.BlobBiggerThanMaxPacket
        ExecuteSQL("SET GLOBAL SQL_MODE = STRICT_ALL_TABLES", root);
        ExecuteSQL("SET GLOBAL connect_timeout=600", root);
        ExecuteSQL("SET GLOBAL net_read_timeout=6000", root);
        ExecuteSQL("SET GLOBAL net_write_timeout=6000", root);
        ExecuteSQL("SET @@global.time_zone='SYSTEM'", root);

        var data = Utils.FillTable("SHOW DATABASES", root);
        foreach (DataRow row in data.Rows)
        {

          string name = row[0].ToString();
          if (!name.StartsWith(BaseDBName)) continue;
          ExecuteSQL(String.Format("DROP DATABASE IF EXISTS `{0}`", name), root);
        }
        data = Utils.FillTable(String.Format("SELECT user,host FROM mysql.user WHERE user LIKE '{0}%'", BaseUserName), root);
        foreach (DataRow row in data.Rows)
        {
          if (Version >= new Version("5.7"))
            ExecuteSQL(String.Format("DROP USER IF EXISTS '{0}'@'{1}'", row[0].ToString(), row[1].ToString()), root);
          else
            ExecuteSQL(String.Format("DROP USER '{0}'@'{1}'", row[0].ToString(), row[1].ToString()), root);
        }
        ExecuteSQL("FLUSH PRIVILEGES", root);
      }
    }

    public string CreateDatabase(string postfix)
    {
      using (var connection = GetConnection(true))
      {
        string dbName = String.Format("{0}{1}", BaseDBName, postfix);
        ExecuteSQL(String.Format("CREATE DATABASE IF NOT EXISTS `{0}`", dbName), connection);
        return dbName;
      }
    }

    public string CreateUser(string postfix, string password)
    {
      using (var connection = GetConnection(true))
      {
        string userName = String.Format("{0}{1}", BaseUserName, postfix);
        if (Version >= new Version("5.7"))
          ExecuteSQL(String.Format("CREATE USER '{0}'@'localhost' IDENTIFIED WITH mysql_native_password BY '{1}'", userName, password), connection);
        else
          ExecuteSQL(String.Format("CREATE USER '{0}'@'localhost' IDENTIFIED BY '{1}'", userName, password), connection);
        ExecuteSQL(String.Format("GRANT ALL ON *.* TO '{0}'@'localhost'", userName), connection);
        ExecuteSQL("FLUSH PRIVILEGES", connection);
        return userName;
      }
    }

    public string CreateUser(string userName, string password, string plugin, string host = "localhost")
    {
      using (var connection = GetConnection(true))
      {
        if (Version >= new Version("5.7"))
        {
          ExecuteSQL(String.Format("DROP USER IF EXISTS '{0}'@'{1}';", userName, host), connection);
          ExecuteSQL(
          String.Format(
            "CREATE USER '{0}'@'{1}' IDENTIFIED {2} BY '{3}'",
            userName, host,
            (plugin == null ? string.Empty : String.Format("WITH '{0}' ", plugin)), password),
          connection);
        }
        else
        {
          var cmd = connection.CreateCommand();
          cmd.CommandText = String.Format("SELECT count(*) FROM mysql.user WHERE user LIKE '{0}%'", userName);
          if ((long)cmd.ExecuteScalar() > 0)
            ExecuteSQL(String.Format("DROP USER '{0}'@'{1}';", userName, host), connection);
          ExecuteSQL(String.Format("CREATE USER '{0}'@'{1}' IDENTIFIED WITH '{2}'", userName, host, plugin), connection);
          if (plugin == "sha256_password") ExecuteSQL("SET old_passwords = 2", connection);
          ExecuteSQL(String.Format("SET PASSWORD FOR '{0}'@'{1}' = PASSWORD('{2}')", userName, host, password), connection);
        }

        ExecuteSQL(String.Format("GRANT ALL ON *.* TO '{0}'@'{1}'", userName, host), connection);
        ExecuteSQL("FLUSH PRIVILEGES", connection);
        return userName;
      }
    }

    public MySqlConnectionStringBuilder ConnectionSettings
    {
      get { return Settings; }
    }

    internal virtual void AdjustConnectionSettings(MySqlConnectionStringBuilder settings)
    {
    }

    protected virtual void AdjustConnections()
    {
    }

    protected virtual void Cleanup()
    {
    }

    protected void ExecuteSQL(string sql, bool asRoot = false)
    {
      var connection = asRoot ? Root : Connection;
      var cmd = connection.CreateCommand();
      cmd.CommandText = sql;
      cmd.ExecuteNonQuery();
    }

    private void ExecuteSQL(string sql, MySqlConnection connection)
    {
      var cmd = connection.CreateCommand();
      cmd.CommandText = sql;
      cmd.ExecuteNonQuery();
    }

    public MySqlDataReader ExecuteReader(string sql, bool asRoot = false)
    {
      var conn = asRoot ? Root : Connection;
      MySqlCommand cmd = new MySqlCommand(sql, conn);
      return cmd.ExecuteReader();
    }

    public DataTable FillTable(string sql)
    {
      DataTable dt = new DataTable();
      MySqlDataAdapter da = new MySqlDataAdapter(sql, Connection);
      da.Fill(dt);
      return dt;
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

    internal protected void KillConnection(MySqlConnection c)
    {
      int threadId = c.ServerThread;
      var root = GetConnection(true);
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
      while (true)
      {
        bool processStillAlive = false;
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
      root.Close();
    }

    internal protected void KillPooledConnection(string connStr)
    {
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      KillConnection(c);
    }
  }
}
