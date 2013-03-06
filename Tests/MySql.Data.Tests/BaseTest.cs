// Copyright © 2004, 2011, Oracle and/or its affiliates. All rights reserved.
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
using NUnit.Framework;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;
using System.Reflection;
using System.Diagnostics;
using System.Resources;
using System.IO;

namespace MySql.Data.MySqlClient.Tests
{
  /// <summary>
  /// Summary description for BaseTest.
  /// </summary>
  public class BaseTest
  {
    protected int maxPacketSize;
    protected MySqlConnection rootConn;
    protected string host;
    protected string user;
    protected string password;
    protected int port;
    protected string pipeName;
    protected string memoryName;
    protected string rootUser;
    protected string rootPassword;
    protected string database0;
    protected string database1;
    protected Version version;

    protected string table;
    protected string csAdditions = String.Empty;
    protected MySqlConnection conn;
    protected bool accessToMySqlDb;
    private int numProcessesRunning;

    public BaseTest()
    {
      LoadBaseConfiguration();
      Initialize();
    }

    protected void LoadBaseConfiguration()
    {
      user = "test";
      password = "test";
      string portString = null;

#if !CF
      rootUser = ConfigurationManager.AppSettings["rootuser"];
      rootPassword = ConfigurationManager.AppSettings["rootpassword"];
      host = ConfigurationManager.AppSettings["host"];
      portString = ConfigurationManager.AppSettings["port"];
      pipeName = ConfigurationManager.AppSettings["pipename"];
      memoryName = ConfigurationManager.AppSettings["memory_name"];
#endif
      if (string.IsNullOrEmpty(rootUser))
        rootUser = "root";
      if (string.IsNullOrEmpty(rootPassword))
        rootPassword = string.Empty;
      if (string.IsNullOrEmpty(host))
        host = "localhost";
      if (string.IsNullOrEmpty(portString))
        port = 3306;
      else
        port = int.Parse(portString);
      if (string.IsNullOrEmpty(pipeName))
        pipeName = "MYSQL";
      if (string.IsNullOrEmpty(memoryName))
        memoryName = "MYSQL";
    }

    protected virtual void Initialize()
    {
      // we don't use FileVersion because it's not available
      // on the compact framework
      string fullname = Assembly.GetExecutingAssembly().FullName;
      string[] parts = fullname.Split(new char[] { '=' });
      string[] versionParts = parts[1].Split(new char[] { '.' });
      database0 = String.Format("db{0}{1}{2}-a", versionParts[0], versionParts[1], port - 3300);
      database1 = String.Format("db{0}{1}{2}-b", versionParts[0], versionParts[1], port - 3300);
      MySqlConnection.ClearAllPools();
    }

    #region Properties

    protected Version Version
    {
      get
      {
        if (version == null)
        {
          string versionString = rootConn.ServerVersion;
          int i = 0;
          while (i < versionString.Length &&
              (Char.IsDigit(versionString[i]) || versionString[i] == '.'))
            i++;
          version = new Version(versionString.Substring(0, i));
        }
        return version;
      }
    }

    #endregion

    protected virtual string GetConnectionInfo()
    {
      return String.Format("protocol=sockets;port={0};", port);
    }

    protected string GetConnectionString(string userId, string pw, bool persistSecurityInfo, bool includedb)
    {
      Debug.Assert(userId != null);
      string connStr = String.Format("server={0};user id={1};pooling=false;" +
           "persist security info={2};connection reset=true;allow user variables=true;",
           host, userId, persistSecurityInfo.ToString().ToLower());
      if (pw != null)
        connStr += String.Format(";password={0};", pw);
      if (includedb)
        connStr += String.Format("database={0};", database0);
      connStr += GetConnectionInfo();
      connStr += csAdditions;
      return connStr;
    }

    protected string GetConnectionString(string userId, string pw, bool includedb)
    {
      return GetConnectionString(userId, pw, true, includedb);
    }

    protected string GetConnectionString(bool includedb)
    {
      return GetConnectionString(user, password, includedb);
    }

    protected string GetPoolingConnectionString()
    {
      string s = GetConnectionString(true);
      s = s.Replace("pooling=false", "pooling=true");
      return s;
    }

    protected void Open()
    {
      string connString = GetConnectionString(true);
      conn = new MySqlConnection(connString);
      conn.Open();
    }

    protected void SetAccountPerms(bool includeProc)
    {
      // now allow our user to access them
      suExecSQL(String.Format(@"GRANT ALL ON `{0}`.* to 'test'@'localhost' 
        identified by 'test'", database0));
      suExecSQL(String.Format(@"GRANT SELECT ON `{0}`.* to 'test'@'localhost' 
        identified by 'test'", database1));
      if (Version.Major >= 5)
        suExecSQL(String.Format(@"GRANT EXECUTE ON `{0}`.* to 'test'@'localhost' 
          identified by 'test'", database1));

      if (includeProc)
      {
        // now allow our user to access them
        suExecSQL(@"GRANT ALL ON mysql.proc to 'test'@'localhost' identified by 'test'");
      }

      suExecSQL("FLUSH PRIVILEGES");
    }

    [SetUp]
    public virtual void Setup()
    {
      SetupRootConnection();

      if (rootConn.ServerVersion.StartsWith("5", StringComparison.Ordinal))
      {
        // run all tests in strict mode
        MySqlCommand cmd = new MySqlCommand("SET GLOBAL SQL_MODE=STRICT_ALL_TABLES", rootConn);
        cmd.ExecuteNonQuery();
      }

      Assembly executingAssembly = Assembly.GetExecutingAssembly();
#if !CF
      Stream stream = executingAssembly.GetManifestResourceStream("MySql.Data.MySqlClient.Tests.Properties.Setup.sql");
#else
      Stream stream = executingAssembly.GetManifestResourceStream("MySql.Data.CF.Tests.Properties.Setup.sql");
#endif
      StreamReader sr = new StreamReader(stream);
      string sql = sr.ReadToEnd();
      sr.Close();

      SetAccountPerms(accessToMySqlDb);
      sql = sql.Replace("[database0]", database0);
      sql = sql.Replace("[database1]", database1);

      ExecuteSQLAsRoot(sql);
      Open();
      numProcessesRunning = CountProcesses();
    }

    protected void ExecuteSQLAsRoot(string sql)
    {
      MySqlScript s = new MySqlScript(rootConn, sql);
      s.Execute();
    }

    private void CheckOrphanedConnections()
    {
      // wait up to 5 seconds for our connection to close
      int procs = CountProcesses();
      for (int x = 0; x < 50; x++)
      {
        if (procs == numProcessesRunning) break;
        System.Threading.Thread.Sleep(100);
        procs = CountProcesses();
      }
      if (procs > numProcessesRunning)
      {
        KillOrphanedConnections();
        int temp = CountProcesses();
        Assert.AreEqual(numProcessesRunning, temp, "Killing orphaned connections failed");
        Assert.AreEqual(numProcessesRunning, procs, "Too many processes still running");
      }
    }

    private void KillOrphanedConnections()
    {
      MySqlDataAdapter da = new MySqlDataAdapter("SHOW PROCESSLIST", rootConn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      foreach (DataRow row in dt.Rows)
      {
        long id = (long)row[0];
        if (id == rootConn.ServerThread) continue;
        if (id == conn.ServerThread) continue;
        suExecSQL(String.Format("KILL {0}", id));
      }
    }

    [TearDown]
    public virtual void Teardown()
    {
      MySqlConnection.ClearAllPools();

      CheckOrphanedConnections();

      conn.Close();
      if (Version.Major < 5)
        suExecSQL("REVOKE ALL PRIVILEGES, GRANT OPTION FROM 'test'");
      else
        suExecSQL("DROP USER 'test'@'localhost'");

      DropDatabase(database0);
      DropDatabase(database1);
      
      DropDatabase("Perm");

      if (rootConn.State == ConnectionState.Open)
        rootConn.Close();
    }

    private void DropDatabase(string name)
    {
      for (int i = 0; i < 5; i++)
      {
        try
        {
          suExecSQL(String.Format("DROP DATABASE IF EXISTS `{0}`", name));
          return;
        }
        catch (Exception)
        {
          System.Threading.Thread.Sleep(1000);
        }
      }
      Assert.Fail("Unable to drop database " + name);
    }

    protected void KillConnection(MySqlConnection c)
    {
      int threadId = c.ServerThread;
      MySqlCommand cmd = new MySqlCommand("KILL " + threadId, conn);
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
        MySqlDataAdapter da = new MySqlDataAdapter("SHOW PROCESSLIST", conn);
        DataTable dt = new DataTable();
        da.Fill(dt);
        foreach (DataRow row in dt.Rows)
          if (row["Id"].Equals(threadId))
            processStillAlive = true;
        if (!processStillAlive) break;
        System.Threading.Thread.Sleep(500);
      }
    }

    protected void KillPooledConnection(string connStr)
    {
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      KillConnection(c);
    }

    protected void createTable(string sql, string engine)
    {
      if (Version >= new Version(4, 1))
        sql += " ENGINE=" + engine;
      else
        sql += " TYPE=" + engine;
      execSQL(sql);
    }

    protected void suExecSQL(string sql)
    {
      Debug.Assert(rootConn != null);
      MySqlCommand cmd = new MySqlCommand(sql, rootConn);
      cmd.ExecuteNonQuery();
    }

    protected void execSQL(string sql)
    {
      MySqlCommand cmd = new MySqlCommand(sql, conn);
      cmd.ExecuteNonQuery();
    }

    protected IDataReader execReader(string sql)
    {
      MySqlCommand cmd = new MySqlCommand(sql, conn);
      return cmd.ExecuteReader();
    }

    protected int CountProcesses()
    {
      MySqlDataAdapter da = new MySqlDataAdapter("SHOW PROCESSLIST", rootConn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      return dt.Rows.Count;
    }

    protected bool TableExists(string tableName)
    {
      string[] restrictions = new string[4];
      restrictions[2] = tableName;
      DataTable dt = conn.GetSchema("Tables", restrictions);
      return dt.Rows.Count > 0;
    }

    protected DataTable FillTable(string sql)
    {
      MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      return dt;
    }

    private void SetupRootConnection()
    {
      string connStr = GetConnectionString(rootUser, rootPassword, false);
      rootConn = new MySqlConnection(connStr + ";database=mysql");
      rootConn.Open();
    }
  }
}
