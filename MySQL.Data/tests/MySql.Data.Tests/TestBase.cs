// Copyright (c) 2016, 2022, Oracle and/or its affiliates.
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

using MySql.Data.Common;
using NUnit.Framework;
using System;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace MySql.Data.MySqlClient.Tests
{
  public class TestBase
  {
    #region Properties
    public MySqlConnectionStringBuilder Settings { get; private set; }
    public MySqlConnectionStringBuilder RootSettings { get; private set; }
    public static string UnixSocket { get; private set; } = Environment.GetEnvironmentVariable("MYSQL_SOCKET") ?? "/tmp/mysql.sock";
    public static string Host { get; private set; } = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost";
    public static string Port { get; private set; } = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
    public static string RootUser { get; private set; } = Environment.GetEnvironmentVariable("MYSQL_ROOT_USER") ?? "root";
    public static string RootPassword { get; private set; } = Environment.GetEnvironmentVariable("MYSQL_ROOT_PASSWORD") ?? string.Empty;
    public static string MemName { get; private set; } = Environment.GetEnvironmentVariable("MYSQL_MEM") ?? "MySQLSocket";
    public static string PipeName { get; private set; } = Environment.GetEnvironmentVariable("MYSQL_PIPE") ?? "MySQLSocket";
    public string BaseDBName { get; private set; }
    public string BaseUserName { get; private set; }
    public Version Version { get; private set; }
    public int MaxPacketSize { get; set; }
    public MySqlConnection Connection { get; set; }
    public MySqlConnection Root { get; private set; }
    #endregion

    #region Setup
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      string _namespace = this.GetType().Name.ToLower();
      string ns = _namespace.Length > 10 ? _namespace.Substring(0, 10) : _namespace;
      BaseDBName = "db-" + ns + "-";
      BaseUserName = "u-" + ns + "-";

      var settings = new MySqlConnectionStringBuilder();
      settings.Server = Host;
      settings.Port = UInt32.Parse(Port);
      settings.UserID = RootUser;
      settings.Password = RootPassword;
      settings.PersistSecurityInfo = true;
      settings.AllowUserVariables = true;
      settings.Pooling = false;
      settings.ConnectionTimeout = 600;

      if (Platform.IsWindows())
      {
        settings.SharedMemoryName = MemName;
        settings.PipeName = PipeName;
      }

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
    #endregion

    #region TearDown
    [OneTimeTearDown]
    public void OneTimeTearDownAttribute()
    {
      MySqlConnection.ClearAllPools();
      CleanupDatabase();

      if (Connection != null && Connection.State == ConnectionState.Open)
        Connection.Dispose();
      if (Root != null && Root.State == ConnectionState.Open)
        Root.Dispose();
    }

    [TearDown]
    public void TearDown()
    {
      Cleanup();
    }
    #endregion

    #region Private Methods
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

    private void InitializeDatabase()
    {
      CleanupDatabase();
      Settings.Database = CreateDatabase("0");
      Settings.UserID = CreateUser("0", "pwd");
      Settings.Password = "pwd";
    }

    private void CleanupDatabase()
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
        data = Utils.FillTable(String.Format("SELECT user,host FROM mysql.user WHERE user LIKE '{0}%' OR user LIKE 'test%' OR user LIKE 'expired%'", BaseUserName), root);
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

    private void ExecuteSQL(string sql, MySqlConnection connection)
    {
      var cmd = connection.CreateCommand();
      cmd.CommandText = sql;
      cmd.ExecuteNonQuery();
    }
    #endregion

    #region Virtual Methods
    internal virtual void AdjustConnectionSettings(MySqlConnectionStringBuilder settings) { }

    protected virtual void Cleanup() { }
    #endregion

    #region Public Methods
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
        ExecuteSQL(String.Format("CREATE DATABASE IF NOT EXISTS `{0}`", dbName), connection);
        return dbName;
      }
    }

    public string CreateUser(string postfix, string password)
    {
      string host = Host == "localhost" ? Host : "%";
      using (var connection = GetConnection(true))
      {
        string userName = String.Format("{0}{1}", BaseUserName, postfix);

        ExecuteSQL($"CREATE USER '{userName}'@'{host}' IDENTIFIED WITH mysql_native_password BY '{password}'", connection);
        ExecuteSQL($"GRANT ALL ON *.* TO '{userName}'@'{host}'", connection);
        ExecuteSQL("FLUSH PRIVILEGES", connection);
        return userName;
      }
    }

    public string CreateUser(string userName, string password, string plugin)
    {
      string host = Host == "localhost" ? Host : "%";
      using (var connection = GetConnection(true))
      {
        ExecuteSQL(String.Format("DROP USER IF EXISTS '{0}'@'{1}';", userName, host), connection);
        ExecuteSQL(
          String.Format(
            "CREATE USER '{0}'@'{1}' IDENTIFIED {2} BY '{3}'", userName, host,
            (plugin == null ? string.Empty : String.Format("WITH '{0}' ", plugin)), password),
          connection);

        ExecuteSQL(String.Format("GRANT ALL ON *.* TO '{0}'@'{1}'", userName, host), connection);
        ExecuteSQL("FLUSH PRIVILEGES", connection);
        return userName;
      }
    }

    public void ExecuteSQL(string sql, bool asRoot = false)
    {
      var connection = asRoot ? Root : Connection;
      var cmd = connection.CreateCommand();
      cmd.CommandText = sql;
      cmd.ExecuteNonQuery();
    }

    public object ExecuteScalar(string sql, bool asRoot = false)
    {
      var connection = asRoot ? Root : Connection;
      var cmd = connection.CreateCommand();
      cmd.CommandText = sql;
      return cmd.ExecuteScalar();
    }

    public MySqlDataReader ExecuteReader(string sql, bool asRoot = false)
    {
      var conn = asRoot ? Root : Connection;
      MySqlCommand cmd = new MySqlCommand(sql, conn);
      return cmd.ExecuteReader();
    }

    public void KillConnection(MySqlConnection c, bool useCompression = false)
    {
      int threadId = c.ServerThread;

      var sb = new MySqlConnectionStringBuilder(RootSettings.ConnectionString);
      if (useCompression)
      {
        sb.UseCompression = useCompression;
      }
      var root = new MySqlConnection(sb.GetConnectionString(true));
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

    public void KillPooledConnection(string connStr)
    {
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      KillConnection(c);
    }

    /// <summary>
    /// Method to get the local ip address of the active MySql Server
    /// </summary>
    /// <param name="isIpV6">when is true return IPv6(::1), otherwise return IPv4(127.0.0.1) which is the default</param>
    /// <returns>Return the ip address as string</returns>
    public string GetMySqlServerIp(bool isIpV6 = false)
    {
      string hostname, ipv4 = string.Empty, ipv6 = string.Empty;
      string query = @"SELECT SUBSTRING_INDEX(host, ':', 1) as IP FROM information_schema.processlist WHERE ID = connection_id()";

      using var conn = GetConnection(true);
      using MySqlCommand cmd = new(query, conn);
      hostname = cmd.ExecuteScalar().ToString();

      foreach (var item in Dns.GetHostEntry(hostname).AddressList)
      {
        switch (item.AddressFamily)
        {
          case AddressFamily.InterNetwork:
            ipv4 = item.ToString();
            break;
          case AddressFamily.InterNetworkV6:
            ipv6 = item.ToString();
            break;
        }
      }

      return isIpV6 ? ipv6 : ipv4;
    }
    #endregion
  }
}
