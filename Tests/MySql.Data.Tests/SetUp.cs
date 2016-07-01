// Copyright © 2013, 2015 Oracle and/or its affiliates. All rights reserved.
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
using Xunit;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  public class TestSetup : IDisposable
  {
    public MySqlConnectionStringBuilder Settings;
    public MySqlConnectionStringBuilder RootSettings;
    public Version version;
    public bool firstTime = true;
    internal protected int maxPacketSize;
    public string baseDBName;
    public string baseUserName;
    public string testNameSpace;
    public MySqlConnection root;

    internal protected string table;
    internal protected string csAdditions = String.Empty;
    internal protected bool accessToMySqlDb;
    private bool disposed = false;


    public TestSetup()
    {
    }

    public void Init(string ns)
    {
      if (String.IsNullOrEmpty(ns))
        throw new Exception("No namespace given");
      testNameSpace = ns;
      if (firstTime)
        LoadBaseConfiguration();
      MySqlConnection.ClearAllPools();
      InitializeDatabase();
      firstTime = false;

      //Assembly executingAssembly = Assembly.GetExecutingAssembly();
      //Stream stream = executingAssembly.GetManifestResourceStream("MySql.Data.MySqlClient.Tests.Properties.Setup.sql");
      //StreamReader sr = new StreamReader(stream);
      //string sql = sr.ReadToEnd();
      //sr.Close();

      //SetAccountPerms(accessToMySqlDb);
      //sql = sql.Replace("[database0]", database0);
      //sql = sql.Replace("[database1]", database1);

      //ExecuteSQLAsRoot(sql);
      //Open();
    }

    private void LoadBaseConfiguration()
    {
      baseDBName = "db-" + testNameSpace + "-";
      baseUserName = "user-" + testNameSpace + "-";

      var s = new MySqlConnectionStringBuilder();
      s.UserID = "root";
      s.Password = null;
      s.Server = "localhost";
      s.SharedMemoryName = "memory";
      s.PipeName = "pipe";
      s.PersistSecurityInfo = true;
      s.AllowUserVariables = true;
      s.Pooling = false;
      s.Port = 3305;

      RootSettings = new MySqlConnectionStringBuilder(s.GetConnectionString(true));
      Settings = new MySqlConnectionStringBuilder(s.GetConnectionString(true));
      Settings.Password = "pwd";
      root = GetConnection(true);
      root.Open();
      SetVersion(root.ServerVersion);
      executeInternal("SET GLOBAL max_allowed_packet=" + 1000 * 1024, root);
      executeInternal("SET GLOBAL SQL_MODE = STRICT_ALL_TABLES", root);
    }

    private void InitializeDatabase()
    {
      CleanupDatabase();
      Settings.Database = CreateDatabase("0");
      Settings.UserID = CreateUser("0", "pwd");
    }

    private void CleanupDatabase()
    {
      var data = Utils.FillTable("SHOW DATABASES", root);
      foreach (DataRow row in data.Rows)
      {

        string name = row[0].ToString();
        if (!name.StartsWith(baseDBName)) continue;
        executeInternal(String.Format("DROP DATABASE IF EXISTS `{0}`", name), root);
      }

      data = Utils.FillTable("SELECT user,host FROM mysql.user WHERE user <> 'root'", root);
      foreach (DataRow row in data.Rows)
      {
        executeInternal(String.Format("DROP USER IF EXISTS '{0}'@'{1}'", row[0].ToString(), row[1].ToString()), root);
      }
      executeInternal("FLUSH PRIVILEGES", root);
    }

    private void SetVersion(string versionString)
    {
      int i = 0;
      while (i < versionString.Length &&
          (Char.IsDigit(versionString[i]) || versionString[i] == '.'))
        i++;

      version = new Version(versionString.Substring(0, i));
    }

    public MySqlConnection GetConnection(bool asRoot = false)
    {
      var s = asRoot ? RootSettings : Settings;
      return new MySqlConnection(s.GetConnectionString(true));
    }

    public string CreateDatabase(string postfix)
    {
      string dbName = String.Format("{0}{1}", baseDBName, postfix);
      executeInternal(String.Format("CREATE DATABASE `{0}`", dbName), root);
      return dbName;
    }

    public string CreateUser(string postfix, string password)
    {
      string userName = String.Format("{0}{1}", baseUserName, postfix);
      executeInternal(String.Format("CREATE USER '{0}'@'localhost' IDENTIFIED BY '{1}'", userName, password), root);
      executeInternal(String.Format("GRANT ALL ON *.* TO '{0}'@'localhost'", userName), root);
      executeInternal("FLUSH PRIVILEGES", root);
      return userName;
    }

    public MySqlDataReader ExecuteReader(string sql, MySqlConnection conn)
    {
      MySqlCommand cmd = new MySqlCommand(sql, conn);
      return cmd.ExecuteReader();
    }

    public void executeInternal(string sql, MySqlConnection conn)
    {
      var cmd = conn.CreateCommand();
      cmd.CommandText = sql;
      cmd.ExecuteNonQuery();
    }

    //internal protected virtual void Initialize()
    //{
    //  // we don't use FileVersion because it's not available
    //  // on the compact framework
    //  string fullname = Assembly.GetExecutingAssembly().FullName;

    //  string[] parts = fullname.Split(new char[] { '=' });
    //  string[] versionParts = parts[1].Split(new char[] { '.' });
    //  database0 = String.Format("db{0}{1}{2}-a", versionParts[0], versionParts[1], port - 3300);
    //  database1 = String.Format("db{0}{1}{2}-b", versionParts[0], versionParts[1], port - 3300);
    //  MySqlConnection.ClearAllPools();
    //}

    //internal protected virtual string GetConnectionInfo()
    //{
    //  if (OnGetConnectionStringInfo != null)
    //    return OnGetConnectionStringInfo();
    //  return String.Format(";protocol=sockets;port={0};", port);
    //}

    //public delegate string GetConnectionStringInfoCallback();

    //public event GetConnectionStringInfoCallback OnGetConnectionStringInfo;

    //internal protected string GetConnectionString(string userId, string pw, bool persistSecurityInfo, bool includedb)
    //{
    //  string connStr = String.Format("server={0};user id={1};pooling=false;" +
    //       "persist security info={2};connection reset=true;allow user variables=true;port={3};",
    //       host, userId, persistSecurityInfo.ToString().ToLower(), port);
    //  if (pw != null)
    //    connStr += String.Format(";password={0};", pw);
    //  if (includedb)
    //    connStr += String.Format("database={0};", database0);
    //  connStr += GetConnectionInfo();
    //  connStr += csAdditions;
    //  return connStr;
    //}

    //internal protected string GetConnectionString(string userId, string pw, bool includedb)
    //{
    //  return GetConnectionString(userId, pw, true, includedb);
    //}

    //internal protected string GetConnectionString(bool includedb)
    //{
    //  return GetConnectionString(user, password, includedb);
    //}

    //protected void Open()
    //{
    //  string connString = GetConnectionString(true);
    //  conn = new MySqlConnection(connString);
    //  conn.Open();
    //}

    //internal protected void suExecSQL(string sql)
    //{
    //  if (rootConn.State != ConnectionState.Open)
    //    rootConn.Open();

    //  MySqlCommand cmd = new MySqlCommand(sql, rootConn);
    //  cmd.ExecuteNonQuery();
    //}


    //internal protected bool TableExists(string tableName)
    //{
    //  string[] restrictions = new string[4];
    //  restrictions[2] = tableName;
    //  DataTable dt = conn.GetSchema("Tables", restrictions);
    //  return dt.Rows.Count > 0;
    //}


    //protected int CountProcesses()
    //{
    //  MySqlDataAdapter da = new MySqlDataAdapter("SHOW PROCESSLIST", rootConn);
    //  DataTable dt = new DataTable();
    //  da.Fill(dt);
    //  return dt.Rows.Count;
    //}

    //internal protected DataTable FillTable(string sql)
    //{
    //  MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
    //  DataTable dt = new DataTable();
    //  da.Fill(dt);
    //  return dt;
    //}



    //protected void DropDatabase(string name)
    //{
    //  for (int i = 0; i < 5; i++)
    //  {
    //    try
    //    {
    //      executeInternal(String.Format("DROP DATABASE IF EXISTS `{0}`", name), root);
    //      return;
    //    }
    //    catch (Exception)
    //    {
    //      System.Threading.Thread.Sleep(1000);
    //    }
    //  }
    //}

    //protected void CheckOrphanedConnections()
    //{
    //  // wait up to 5 seconds for our connection to close
    //  int procs = CountProcesses();
    //  for (int x = 0; x < 50; x++)
    //  {
    //    if (procs == numProcessesRunning) break;
    //    System.Threading.Thread.Sleep(100);
    //    procs = CountProcesses();
    //  }
    //  if (procs > numProcessesRunning)
    //  {
    //    KillOrphanedConnections();
    //    int temp = CountProcesses();
    //    Assert.True(numProcessesRunning == temp, "Killing orphaned connections failed");
    //    Assert.True(numProcessesRunning == procs, "Too many processes still running");
    //  }
    //}

    //protected void KillOrphanedConnections()
    //{
    //  MySqlDataAdapter da = new MySqlDataAdapter("SHOW PROCESSLIST", rootConn);
    //  DataTable dt = new DataTable();
    //  da.Fill(dt);
    //  foreach (DataRow row in dt.Rows)
    //  {
    //    long id = (long)row[0];
    //    if (id == rootConn.ServerThread) continue;
    //    if (id == conn.ServerThread) continue;
    //    suExecSQL(String.Format("KILL {0}", id));
    //  }
    //}



    //internal protected string GetPoolingConnectionString()
    //{
    //  string s = GetConnectionString(true);
    //  s = s.Replace("pooling=false", "pooling=true");
    //  return s;
    //}


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

    internal protected void KillPooledConnection(string connStr)
    {
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      KillConnection(c);
    }

    //internal protected void execSQL(string sql)
    //{
    //  MySqlCommand cmd = new MySqlCommand(sql, conn);
    //  cmd.ExecuteNonQuery();
    //}

    //internal protected MySqlDataReader execReader(string sql)
    //{
    //  MySqlCommand cmd = new MySqlCommand(sql, conn);
    //  return cmd.ExecuteReader();
    //}


    //internal protected void ExecuteSQLAsRoot(string sql)
    //{
    //  MySqlScript s = new MySqlScript(rootConn, sql);
    //  s.Execute();
    //}

    protected virtual void Dispose(bool disposing)
    {
      if (disposed)
        return;

      if (disposing)
      {
        MySqlConnection.ClearAllPools();
        CleanupDatabase();

        //CheckOrphanedConnections();
        //if (rootConn.State == ConnectionState.Closed)
        //  rootConn.Open();

        //DropDatabase(database0);
        //DropDatabase(database1);

        //DropDatabase("Perm");

        //rootConn.Close();
        //conn.Close();
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
  //public class SetUpClassPerTestInit : SetUpClass
  //{
  //  public SetUpClassPerTestInit()
  //  {
  //    // No initialization
  //  }

  //  protected internal override void Init()
  //  {
  //    // Does nothing, so constructor of base class does nothing.
  //  }
  //}

  /// <summary>
  /// This is a companion of the previous class to allow customization before inialization code.
  /// </summary>
  //public class SpecialFixtureWithCustomConnectionString : IClassFixture<SetUpClassPerTestInit>, IDisposable
  //{
  //  protected SetUpClassPerTestInit st;

  //  public virtual void SetFixture(SetUpClassPerTestInit data)
  //  {
  //    st = data;
  //    st.OnGetConnectionStringInfo += new SetUpClass.GetConnectionStringInfoCallback(OnGetConnectionStringInfo);
  //    st.MyInit();
  //    //if (st.conn.State != ConnectionState.Open)
  //    //  st.conn.Open();
  //  }

  //  /// <summary>
  //  /// Override to provide special connect options like using pipes, compression, etc.
  //  /// </summary>
  //  /// <returns></returns>
  //  protected virtual string OnGetConnectionStringInfo()
  //  {
  //    return "protocol=sockets;";
  //  }

  //  public void Dispose()
  //  {
  //    Dispose(true);
  //  }

  //  protected virtual void Dispose(bool disposing)
  //  {
  //    if (!disposing) return;
  //    st.Dispose();
  //  }
  //}
}
