// Copyright © 2013, 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Text;
using System.Configuration;
using MySql.Data.MySqlClient;
using Xunit;
using System.Reflection;
using System.Data;
using System.IO;
using System.Diagnostics;

namespace MySql.Data.MySqlClient.Tests
{
  public class SetUpClass : IDisposable
  {
    internal protected int maxPacketSize;
    internal protected MySqlConnection rootConn;
    internal protected string host;
    internal protected string user;
    internal protected string password;
    internal protected int port;
    internal protected string pipeName;
    internal protected string memoryName;
    internal protected string rootUser;
    internal protected string rootPassword;
    internal protected string database0;
    internal protected string database1;
    internal protected Version version;

    internal protected string table;
    internal protected string csAdditions = String.Empty;
    internal protected MySqlConnection conn;
    internal protected bool accessToMySqlDb;
    private int numProcessesRunning;
    private bool disposed = false;
    private string initialSql;

    #region Properties

    internal protected Version Version
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

    public SetUpClass()
    {
      Init();
    }

    internal protected virtual void Init()
    {
      MyInit();
    }

    internal protected void MyInit()
    {
      LoadBaseConfiguration();
      Initialize();

      SetupRootConnection();

      if (rootConn.ServerVersion.StartsWith("5", StringComparison.Ordinal))
      {
        // run all tests in strict mode
        MySqlCommand cmd = new MySqlCommand("SET GLOBAL SQL_MODE=STRICT_ALL_TABLES", rootConn);
        cmd.ExecuteNonQuery();
      }

      Assembly executingAssembly = Assembly.GetExecutingAssembly();
      Stream stream = executingAssembly.GetManifestResourceStream("MySql.Data.MySqlClient.Tests.Properties.Setup.sql");
      StreamReader sr = new StreamReader(stream);
      string sql = sr.ReadToEnd();
      sr.Close();

      SetAccountPerms(accessToMySqlDb);
      sql = sql.Replace("[database0]", database0);
      sql = sql.Replace("[database1]", database1);
      initialSql = sql;

      ExecuteSQLAsRoot(sql);
      Open();
      numProcessesRunning = CountProcesses();
    }

    public void SetupTest()
    {
      if (rootConn != null && rootConn.State == ConnectionState.Open)
        rootConn.Dispose();
      if (conn != null && conn.State == ConnectionState.Open)
        conn.Dispose();
      SetupRootConnection();
      ExecuteSQLAsRoot(initialSql);
      Open();
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



    protected void LoadBaseConfiguration()
    {
      user = "test";
      password = "test";
      string portString = null;

      rootUser = ConfigurationManager.AppSettings["rootuser"];
      rootPassword = ConfigurationManager.AppSettings["rootpassword"];
      host = ConfigurationManager.AppSettings["host"];
      portString = ConfigurationManager.AppSettings["port"];
      memoryName = ConfigurationManager.AppSettings["memory_name"];
      pipeName = ConfigurationManager.AppSettings["memory_name"];

      if (string.IsNullOrEmpty(rootUser))
        rootUser = "root";
      if (string.IsNullOrEmpty(rootPassword))
        rootPassword = "";
      if (string.IsNullOrEmpty(host))
        host = "localhost";
      if (string.IsNullOrEmpty(portString))
        port = 3305;
      else
        port = int.Parse(portString);
      if (string.IsNullOrEmpty(pipeName))
        pipeName = "MySQLSocket";
      if (string.IsNullOrEmpty(memoryName))
        memoryName = "MySQLSocket";

    }

    internal protected virtual void Initialize()
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

    internal protected virtual string GetConnectionInfo()
    {
      if (OnGetConnectionStringInfo != null)
        return OnGetConnectionStringInfo();
      return String.Format(";protocol=sockets;port={0};", port);
    }

    public delegate string GetConnectionStringInfoCallback();

    public event GetConnectionStringInfoCallback OnGetConnectionStringInfo;

    internal protected string GetConnectionString(string userId, string pw, bool persistSecurityInfo, bool includedb)
    {
      string connStr = String.Format("server={0};user id={1};pooling=false;" +
           "persist security info={2};connection reset=true;allow user variables=true;port={3};",
           host, userId, persistSecurityInfo.ToString().ToLower(), port);
      if (pw != null)
        connStr += String.Format(";password={0};", pw);
      if (includedb)
        connStr += String.Format("database={0};", database0);
      connStr += GetConnectionInfo();
      connStr += csAdditions;
      return connStr;
    }

    internal protected string GetConnectionString(string userId, string pw, bool includedb)
    {
      return GetConnectionString(userId, pw, true, includedb);
    }

    internal protected string GetConnectionString(bool includedb)
    {
      return GetConnectionString(user, password, includedb);
    }

    public void SetupRootConnection()
    {
      string connStr = GetConnectionString(rootUser, rootPassword, false);
      rootConn = new MySqlConnection(connStr + ";database=mysql");
      rootConn.Open();
    }

    protected void Open()
    {
      string connString = GetConnectionString(true);
      conn = new MySqlConnection(connString);
      conn.Open();
    }

    internal protected void suExecSQL(string sql)
    {
      if (rootConn.State != ConnectionState.Open)
        rootConn.Open();

      MySqlCommand cmd = new MySqlCommand(sql, rootConn);
      cmd.ExecuteNonQuery();
    }


    internal protected bool TableExists(string tableName)
    {
      string[] restrictions = new string[4];
      restrictions[2] = tableName;
      DataTable dt = conn.GetSchema("Tables", restrictions);
      return dt.Rows.Count > 0;
    }


    protected int CountProcesses()
    {
      MySqlDataAdapter da = new MySqlDataAdapter("SHOW PROCESSLIST", rootConn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      return dt.Rows.Count;
    }

    internal protected DataTable FillTable(string sql)
    {
      MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      return dt;
    }



    protected void DropDatabase(string name)
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
    }

    protected void CheckOrphanedConnections()
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
        Assert.True(numProcessesRunning == temp, "Killing orphaned connections failed");
        Assert.True(numProcessesRunning == procs, "Too many processes still running");
      }
    }

    protected void KillOrphanedConnections()
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


    internal protected string GetPoolingConnectionString()
    {
      string s = GetConnectionString(true);
      s = s.Replace("pooling=false", "pooling=true");
      return s;
    }


    internal protected void KillConnection(MySqlConnection c)
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
      while (true)
      {
        bool processStillAlive = false;
        MySqlCommand cmdProcess = new MySqlCommand("SHOW PROCESSLIST", conn);
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

    internal protected void KillPooledConnection(string connStr)
    {
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        KillConnection(c);
      }
    }

    internal protected void execSQL(string sql)
    {
      MySqlCommand cmd = new MySqlCommand(sql, conn);
      cmd.ExecuteNonQuery();
    }

    internal protected MySqlDataReader execReader(string sql)
    {
      MySqlCommand cmd = new MySqlCommand(sql, conn);
      return cmd.ExecuteReader();
    }


    internal protected void createTable(string sql, string engine)
    {
      if (Version >= new Version(4, 1))
        sql += " ENGINE=" + engine;
      else
        sql += " TYPE=" + engine;
      execSQL(sql);
    }

    internal protected void ExecuteSQLAsRoot(string sql)
    {
      MySqlScript s = new MySqlScript(rootConn, sql);
      s.Execute();
    }

    internal protected string CreateUser(string userName, string password, string plugin)
        {
          using (var connection = conn)
          {
            if (Version >= new Version("5.7"))
            {
              ExecuteSQLAsRoot(string.Format("DROP USER IF EXISTS '{0}'@'localhost';", userName));
              ExecuteSQLAsRoot(
                string.Format(
                  "CREATE USER '{0}'@'localhost' IDENTIFIED {1} BY '{2}'",
                  userName,
                  (plugin == null ? string.Empty : String.Format("WITH '{0}' ", plugin)), password));
            }
            else
            {
              ExecuteSQLAsRoot(string.Format("GRANT SELECT ON mysql.user TO '{0}'@'localhost'", connection.Settings.UserID));
              var cmd = connection.CreateCommand();
              cmd.CommandText = string.Format("SELECT count(*) FROM mysql.user WHERE user LIKE '{0}%'", userName);
              if ((long)cmd.ExecuteScalar() > 0)
                ExecuteSQLAsRoot(string.Format("DROP USER '{0}'@'localhost';", userName));
              ExecuteSQLAsRoot(string.Format("CREATE USER '{0}'@'localhost' IDENTIFIED WITH '{1}'", userName, plugin));
              if (plugin=="sha256_password") execSQL("SET old_passwords = 2");
              ExecuteSQLAsRoot(string.Format("SET PASSWORD FOR '{0}'@'localhost' = PASSWORD('{1}')", userName, password));
            }

            ExecuteSQLAsRoot(string.Format("GRANT ALL ON *.* TO '{0}'@'localhost'", userName));
            ExecuteSQLAsRoot("FLUSH PRIVILEGES");
            return userName;
          }
        }

    public virtual void Close()
    {
      MySqlConnection.ClearAllPools();

      //CheckOrphanedConnections();

      if (rootConn.State == ConnectionState.Closed)
        rootConn.Open();

      DropDatabase(database0);
      DropDatabase(database1);

      DropDatabase("Perm");

      rootConn.Dispose();
      conn.Dispose();
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposed)
        return;

      if (disposing)
      {
        Close();
      }

      disposed = true;
    }

    public virtual void Dispose()
    {
      Dispose(true);
    }
  }

  /// <summary>
  /// This Setup class changes the initialization semantic to be exeucuted once per unit test, instead of once per Fixture class.
  /// </summary>
  public class SetUpClassPerTestInit : SetUpClass
  {
    public SetUpClassPerTestInit()
    {
      // No initialization
    }

    //protected internal override void Init()
    //{
    //  // Does nothing, so constructor of base class does nothing.
    //}
  }

  public class BaseFixture : IUseFixture<SetUpClassPerTestInit>, IDisposable
  {
    protected SetUpClassPerTestInit _fixture;

    public virtual void SetFixture(SetUpClassPerTestInit fixture)
    {
      _fixture = fixture;
      _fixture.SetupTest();
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          // TODO: dispose managed state (managed objects).
          _fixture.Close();
        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.

        disposedValue = true;
      }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~BaseFixture() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      // GC.SuppressFinalize(this);
    }
    #endregion
  }

  /// <summary>
  /// This is a companion of the previous class to allow customization before inialization code.
  /// </summary>
  public class SpecialFixtureWithCustomConnectionString : IUseFixture<SetUpClassPerTestInit>, IDisposable
  {
    protected SetUpClassPerTestInit st;

    public virtual void SetFixture(SetUpClassPerTestInit data)
    {
      st = data;
      st.OnGetConnectionStringInfo += new SetUpClass.GetConnectionStringInfoCallback(OnGetConnectionStringInfo);
      st.SetupTest();
      //if (st.conn.State != ConnectionState.Open)
      //  st.conn.Open();
    }

    /// <summary>
    /// Override to provide special connect options like using pipes, compression, etc.
    /// </summary>
    /// <returns></returns>
    protected virtual string OnGetConnectionStringInfo()
    {
      return "protocol=sockets;";
    }

    public void Dispose()
    {
      Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposing)
        st.Close();
    }
  }
}
