// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Properties;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class MySqlConnectionTests : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;
    private bool disposed = false; 

    public void SetFixture(SetUpClass data)
    {
      st = data;
    }

    [Fact]
    public void TestConnectionStrings()
    {
      MySqlConnection c = new MySqlConnection();

      // public properties            
      Assert.True(15 == c.ConnectionTimeout, "ConnectionTimeout");
      Assert.True(String.Empty == c.Database, "Database");
      Assert.True(String.Empty == c.DataSource, "DataSource");
      Assert.True(false == c.UseCompression, "Use Compression");
      Assert.True(System.Data.ConnectionState.Closed == c.State, "State");

      c = new MySqlConnection("connection timeout=25; user id=myuser; " +
          "password=mypass; database=Test;server=myserver; use compression=true; " +
          "pooling=false;min pool size=5; max pool size=101");

      // public properties
      Assert.True(25 == c.ConnectionTimeout, "ConnectionTimeout");
      Assert.True("Test" == c.Database, "Database");
      Assert.True("myserver" == c.DataSource, "DataSource");
      Assert.True(true == c.UseCompression, "Use Compression");
      Assert.True(System.Data.ConnectionState.Closed == c.State, "State");

      c.ConnectionString = "connection timeout=15; user id=newuser; " +
          "password=newpass; port=3308; database=mydb; data source=myserver2; " +
          "use compression=true; pooling=true; min pool size=3; max pool size=76";

      // public properties
      Assert.True(15 == c.ConnectionTimeout, "ConnectionTimeout");
      Assert.True("mydb" == c.Database, "Database");
      Assert.True("myserver2" == c.DataSource, "DataSource");
      Assert.True(true == c.UseCompression, "Use Compression");
      Assert.True(System.Data.ConnectionState.Closed == c.State, "State");
      
    }

#if !CF  //No Security.Principal on CF

    [Fact]
    public void TestIntegratedSecurityNoPoolingWithoutUser()
    {
      TestIntegratedSecurityWithUser(null, false);
    }

    [Fact]
    public void TestIntegratedSecurityPoolingWithoutUser()
    {
      TestIntegratedSecurityWithUser(null, true);
    }

    public void TestIntegratedSecurityWithUser(string user, bool pooling)
    {
      if (st.Version < new Version(5, 5)) return;

      const string PluginName = "authentication_windows";
      string UserName = "auth_windows";
      if (user != null)
        UserName = user;

      // Check if server has windows authentication plugin is installed			
      MySqlCommand cmd = new MySqlCommand("show plugins", st.rootConn);

      bool haveWindowsAuthentication = false;
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        while (r.Read())
        {
          string name = (string)r["Name"];
          if (name == PluginName)
          {
            haveWindowsAuthentication = true;
            break;
          }
        }
      }
      if (!haveWindowsAuthentication)
        return;

      bool haveAuthWindowsUser = false;
      string pluginName = null;
      string authenticationString = "";

      // Check if predefined proxy user exists
      cmd.CommandText = string.Format(
        "select plugin, authentication_string from mysql.user where user='{0}'",
        UserName);
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        if (r.Read())
        {
          haveAuthWindowsUser = true;
          pluginName = (string)r["plugin"];
          authenticationString =
            (string)((r["authentication_string"] == DBNull.Value) ?
            "" : r["authentication_string"]);
        }
      }

      // Create mapping for current Windows user=>foo_user
      String windowsUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
      windowsUser = windowsUser.Replace("\\", "\\\\");
      string userMapping = windowsUser + "=foo_user";

      try
      {
        if (!haveAuthWindowsUser)
        {
          st.suExecSQL(
            "CREATE USER " + UserName + " IDENTIFIED WITH " + PluginName + " as '" +
             userMapping + "'");
        }
        else
        {
          // extend mapping string for current user
          st.suExecSQL(
            "UPDATE mysql.user SET authentication_string='" + userMapping +
            "," + authenticationString + "' where user='" + UserName + "'");
        }
        st.suExecSQL("create user foo_user identified by 'pass'");
        st.suExecSQL("grant all privileges on *.* to 'foo_user'@'%'");
        st.suExecSQL("grant proxy on foo_user to " + UserName);


        // Finally, use IntegratedSecurity=true for the newly created user
        string connStr = st.GetConnectionString(true) + ";Integrated Security=SSPI";

        MySqlConnectionStringBuilder sb =
            new MySqlConnectionStringBuilder(connStr);
        sb.UserID = user;
        connStr = sb.ConnectionString;

        /* If pooling is requested, we'll  run test twice, with connection reset in between */
        if (pooling)
        {
          connStr += ";Connection Reset=true;Pooling=true";
        }
        int testIterations = pooling ? 2 : 1;

        int threadId = -1;
        for (int i = 0; i < testIterations; i++)
        {
          using (MySqlConnection c = new MySqlConnection(connStr))
          {
            c.Open();
            threadId = c.ServerThread;
            MySqlCommand command = new MySqlCommand("SELECT 1", c);
            long ret = (long)command.ExecuteScalar();
            Assert.Equal(ret, 1);

            command.CommandText = "select user()";
            string myUser = (string)command.ExecuteScalar();
            // Check if proxy user is correct
            Assert.True(myUser.StartsWith(UserName + "@"));

            // check if mysql user is correct 
            // (foo_user is mapped to current  OS user)
            command.CommandText = "select current_user()";
            string currentUser = (string)command.ExecuteScalar();
            Assert.True(currentUser.StartsWith("foo_user@"));
          }
        }

        if (pooling)
        {
          st.suExecSQL("KILL " + threadId);
        }
      }
      finally
      {
        // Cleanup

        // Drop test user
        st.suExecSQL("drop user foo_user");
        if (!haveAuthWindowsUser)
        {
          // drop proxy user if we created it
          st.suExecSQL("drop user " + UserName);
        }
        else
        {
          // revert changes in the mapping string
          st.suExecSQL("UPDATE mysql.user SET authentication_string='" +
            authenticationString + "' where user='" + UserName + "'");
        }
      }
    }

    [Fact]
    public void TestIntegratedSecurityNoPoolingWithUser()
    {
      TestIntegratedSecurityWithUser("myuser1", false);
    }

    [Fact]
    public void TestIntegratedSecurityPoolingWithUser()
    {
      TestIntegratedSecurityWithUser("myuser1", true);
    }

    public void TestIntegratedSecurityWithoutProxy(string user, bool pooling)
    {
      if (st.Version < new Version(5, 5)) return;

      const string PluginName = "authentication_windows";
      string UserName = "auth_windows";
      if (user != null)
        UserName = user;

      // Check if server has windows authentication plugin is installed			
      MySqlCommand cmd = new MySqlCommand("show plugins", st.rootConn);

      bool haveWindowsAuthentication = false;
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        while (r.Read())
        {
          string name = (string)r["Name"];
          if (name == PluginName)
          {
            haveWindowsAuthentication = true;
            break;
          }
        }
      }
      if (!haveWindowsAuthentication)
        return;

      bool haveAuthWindowsUser = false;
      string pluginName = null;
      string authenticationString = "";

      // Check if predefined proxy user exists
      cmd.CommandText = string.Format(
        "select plugin, authentication_string from mysql.user where user='{0}'",
        UserName);
      using (MySqlDataReader r = cmd.ExecuteReader())
      {
        if (r.Read())
        {
          haveAuthWindowsUser = true;
          pluginName = (string)r["plugin"];
          authenticationString =
            (string)((r["authentication_string"] == DBNull.Value) ?
            "" : r["authentication_string"]);
        }
      }

      // Create mapping for current Windows user=>foo_user
      String windowsUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
      windowsUser = windowsUser.Replace("\\", "\\\\");
      string userMapping = "fergs, Administrators";

      try
      {
        if (!haveAuthWindowsUser)
        {
          st.suExecSQL(
            "CREATE USER " + UserName + " IDENTIFIED WITH " + PluginName + " as '" +
             userMapping + "'");
        }
        else
        {
          // extend mapping string for current user
          st.suExecSQL(
            "UPDATE mysql.user SET authentication_string='" + userMapping +
            "," + authenticationString + "' where user='" + UserName + "'");
        }
        st.suExecSQL(string.Format("grant all privileges on *.* to '{0}'@'%'", UserName));

        // Finally, use IntegratedSecurity=true for the newly created user
        string connStr = st.GetConnectionString(true) + ";Integrated Security=SSPI";

        MySqlConnectionStringBuilder sb =
            new MySqlConnectionStringBuilder(connStr);
        sb.UserID = user;
        connStr = sb.ConnectionString;

        /* If pooling is requested, we'll  run test twice, with connection reset in between */
        if (pooling)
        {
          connStr += ";Connection Reset=true;Pooling=true";
        }
        int testIterations = pooling ? 2 : 1;

        int threadId = -1;
        for (int i = 0; i < testIterations; i++)
        {
          using (MySqlConnection c = new MySqlConnection(connStr))
          {
            c.Open();
            threadId = c.ServerThread;
            MySqlCommand command = new MySqlCommand("SELECT 1", c);
            long ret = (long)command.ExecuteScalar();
            Assert.Equal(ret, 1);

            command.CommandText = "select user()";
            string myUser = (string)command.ExecuteScalar();
            // Check if proxy user is correct
            Assert.True(myUser.StartsWith(UserName + "@"));

            // check if mysql user is correct 
            // (foo_user is mapped to current  OS user)
            command.CommandText = "select current_user()";
            string currentUser = (string)command.ExecuteScalar();
            Assert.True(currentUser.StartsWith(UserName));
          }
        }

        if (pooling)
        {
          st.suExecSQL("KILL " + threadId);
        }
      }
      finally
      {
        // Cleanup

        // Drop test user
        st.suExecSQL(string.Format("drop user {0}", UserName));
      }
    }

    [Fact]
    public void TestWinAuthWithoutProxyNoUserNoPooling()
    {
      TestIntegratedSecurityWithoutProxy(null, false);
    }

    [Fact]
    public void TestWinAuthWithoutProxyNoUserPooling()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", true);
    }

    [Fact]
    public void TestWinAuthWithoutProxyAndUser()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", false);
    }

    [Fact]
    public void TestWinAuthWithoutProxyAndUserPooling()
    {
      TestIntegratedSecurityWithoutProxy("myuser1", true);
    }
#endif

    [Fact]
    public void TestConnectingSocketBadUserName()
    {
      st.suExecSQL("DELETE FROM mysql.user WHERE length(user) = 0");
      st.suExecSQL("FLUSH PRIVILEGES");

      string connStr = "server={0};user id=dummy;password=;database=Test;pooling=false";
      MySqlConnection c = new MySqlConnection(
          String.Format(connStr, st.host));
      try
      {
        c.Open();
        c.Close();
        throw new Exception("Open should not have worked");
      }
      catch (MySqlException)
      {
      }
    }

    [Fact]
    public void TestConnectingSocketBadDbName()
    {
      string connStr = "server={0};user id={1};password={2};database=dummy; " +
          "pooling=false";
      MySqlConnection c = new MySqlConnection(
          String.Format(connStr, st.host, st.user, st.password));
      try
      {
        c.Open();
        c.Close();
        throw new Exception("Open should not have worked");
      }
      catch (MySqlException)
      {
      }
    }

    [Fact]
    public void TestPersistSecurityInfoCachingPasswords()
    {
      string connStr = st.GetConnectionString(true);
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      c.Close();

      // this shouldn't work
      connStr = st.GetConnectionString(st.user, "bad_password", true);
      c = new MySqlConnection(connStr);
      try
      {
        c.Open();
        Assert.False(true, "Thn is should not work");
        c.Close();
        return;
      }
      catch (MySqlException)
      {
      }

      // this should work
      connStr = st.GetConnectionString(true);
      c = new MySqlConnection(connStr);
      c.Open();
      c.Close();
    }

    [Fact]
    public void ChangeDatabase()
    {
      string connStr = st.GetConnectionString(true);
      MySqlConnection c = new MySqlConnection(connStr + ";pooling=false");
      c.Open();
      Assert.True(c.State == ConnectionState.Open);

      Assert.Equal(st.database0.ToLower(), c.Database.ToLower());

      c.ChangeDatabase(st.database1);

      Assert.Equal(st.database1.ToLower(), c.Database.ToLower());

      c.Close();
    }

    [Fact]
    public void ConnectionTimeout()
    {
      MySqlConnection c = new MySqlConnection(
          "server=1.1.1.1;user id=bogus;pwd=bogus;Connection timeout=5;" +
          "pooling=false");
      DateTime start = DateTime.Now;
      try
      {
        c.Open();
      }
      catch (Exception)
      {
        TimeSpan diff = DateTime.Now.Subtract(start);
        Assert.True(diff.TotalSeconds < 15, "Timeout exceeded");
      }
    }

    /*        [Fact]
            public void AnonymousLogin()
            {
                suExecSQL(String.Format("GRANT ALL ON *.* to ''@'{0}' IDENTIFIED BY 'set_to_blank'", host));
                suExecSQL("UPDATE mysql.user SET password='' WHERE password='set_to_blank'");

                MySqlConnection c = new MySqlConnection(String.Empty);
                c.Open();
                c.Close();
            }
            */
    [Fact]
    public void ConnectInVariousWays()
    {
      // connect with no db
      string connStr2 = st.GetConnectionString(false);
      MySqlConnection c = new MySqlConnection(connStr2);
      c.Open();
      c.Close();

      st.suExecSQL("GRANT ALL ON *.* to 'nopass'@'%'");
      st.suExecSQL("GRANT ALL ON *.* to 'nopass'@'localhost'");
      st.suExecSQL("FLUSH PRIVILEGES");

      // connect with no password
      connStr2 = st.GetConnectionString("nopass", null, false);
      c = new MySqlConnection(connStr2);
      c.Open();
      c.Close();

      connStr2 = st.GetConnectionString("nopass", "", false);
      c = new MySqlConnection(connStr2);
      c.Open();
      c.Close();
    }

    [Fact]
    public void ConnectingAsUTF8()
    {
      if (st.Version < new Version(4, 1)) return;

      string connStr = st.GetConnectionString(true) + ";charset=utf8";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand(
            "CREATE TABLE test (id varbinary(16), active bit) CHARACTER SET utf8", st.conn);
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO test (id, active) VALUES (CAST(0x1234567890 AS Binary), true)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO test (id, active) VALUES (CAST(0x123456789a AS Binary), true)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO test (id, active) VALUES (CAST(0x123456789b AS Binary), true)";
        cmd.ExecuteNonQuery();
      }

      using (MySqlConnection d = new MySqlConnection(connStr))
      {
        d.Open();

        MySqlCommand cmd2 = new MySqlCommand("SELECT id, active FROM test", d);
        using (MySqlDataReader reader = cmd2.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.True(reader.GetBoolean(1));
        }
      }
    }

    /// <summary>
    /// Bug #10281 Clone issue with MySqlConnection 
    /// Bug #27269 MySqlConnection.Clone does not mimic SqlConnection.Clone behaviour 
    /// </summary>
    [Fact]
    public void TestConnectionClone()
    {
      MySqlConnection c = new MySqlConnection();
      MySqlConnection clone = (MySqlConnection)((ICloneable)c).Clone();
      clone.ToString();

      string connStr = st.GetConnectionString(true);
      connStr = connStr.Replace("persist security info=true", "persist security info=false");
      c = new MySqlConnection(connStr);
      c.Open();
      c.Close();
      MySqlConnection c2 = (MySqlConnection)((ICloneable)c).Clone();
      c2.Open();
      c2.Close();
    }

    /// <summary>
    /// Bug #13321  	Persist security info does not woek
    /// </summary>
    [Fact]
    public void PersistSecurityInfo()
    {
      string s = st.GetConnectionString(true).ToLower();
      int start = s.IndexOf("persist security info");
      int end = s.IndexOf(";", start);
      string connStr = s.Substring(0, start);
      connStr += s.Substring(end, s.Length - (end));

      string p = "password";
      if (connStr.IndexOf("pwd") != -1)
        p = "pwd";
      else if (connStr.IndexOf("passwd") != -1)
        p = "passwd";

      string newConnStr = connStr + ";persist security info=true";
      MySqlConnection conn2 = new MySqlConnection(newConnStr);
      Assert.True(conn2.ConnectionString.IndexOf(p) != -1);
      conn2.Open();
      conn2.Close();
      Assert.True(conn2.ConnectionString.IndexOf(p) != -1);

      newConnStr = connStr + ";persist security info=false";
      conn2 = new MySqlConnection(newConnStr);
      Assert.True(conn2.ConnectionString.IndexOf(p) != -1);
      conn2.Open();
      conn2.Close();
      Assert.True(conn2.ConnectionString.IndexOf(p) == -1);
    }

    /// <summary>
    /// Bug #13658  	connection.state does not update on Ping()
    /// </summary>
    [Fact]
    public void PingUpdatesState()
    {
      MySqlConnection conn2 = new MySqlConnection(st.GetConnectionString(true));
      conn2.Open();
      st.KillConnection(conn2);
      Assert.False(conn2.Ping());
      Assert.True(conn2.State == ConnectionState.Closed);
      conn2.Open();
      conn2.Close();
    }

    /// <summary>
    /// Bug #16659  	Can't use double quotation marks(") as password access server by Connector/NET
    /// </summary>
    [Fact]
    public void ConnectWithQuotePassword()
    {
      st.suExecSQL("GRANT ALL ON *.* to 'quotedUser'@'%' IDENTIFIED BY '\"'");
      st.suExecSQL("GRANT ALL ON *.* to 'quotedUser'@'localhost' IDENTIFIED BY '\"'");
      string connStr = st.GetConnectionString("quotedUser", null, false);
      connStr += ";pwd='\"'";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
      }
    }

    /// <summary>
    /// Bug #24802 Error Handling 
    /// </summary>
    [Fact]
    public void TestConnectingSocketBadHostName()
    {
      string connStr = "server=foobar;user id=foouser;password=;database=Test;" +
          "pooling=false";
      try
      {
        using (MySqlConnection c = new MySqlConnection(connStr))
        {
          c.Open();
        }
      }
      catch (MySqlException ex)
      {
        Assert.Equal((int)MySqlErrorCode.UnableToConnectToHost, ex.Number);
      }
    }

    /// <summary>
    /// Bug #29123  	Connection String grows with each use resulting in OutOfMemoryException
    /// </summary>
    [Fact]
    public void ConnectionStringNotAffectedByChangeDatabase()
    {
      for (int i = 0; i < 10; i++)
      {
        string connStr = st.GetConnectionString(true) + ";pooling=false";
        connStr = connStr.Replace("database", "Initial Catalog");
        connStr = connStr.Replace("persist security info=true",
            "persist security info=false");
        using (MySqlConnection c = new MySqlConnection(connStr))
        {
          c.Open();
          string str = c.ConnectionString;
          int index = str.IndexOf("Database=");
          Assert.Equal(-1, index);
        }
      }
    }

    class ConnectionClosedCheck
    {
      public bool closed = false;
      public void stateChangeHandler(object sender, StateChangeEventArgs e)
      {
        if (e.CurrentState == ConnectionState.Closed)
          closed = true;
      }
    }
    [Fact]
    public void ConnectionCloseByGC()
    {
      int threadId;
      ConnectionClosedCheck check = new ConnectionClosedCheck();
      string connStr = st.GetConnectionString(true) + ";pooling=true";
      MySqlConnection c = new MySqlConnection(connStr);
      c.StateChange += new StateChangeEventHandler(check.stateChangeHandler);
      c.Open();
      threadId = c.ServerThread;
      c = null;
      GC.Collect();
      GC.WaitForPendingFinalizers();
      Assert.True(check.closed);

      MySqlCommand cmd = new MySqlCommand("KILL " + threadId, st.conn);
      cmd.ExecuteNonQuery();
    }
    /// <summary>
    /// Bug #30964 StateChange imperfection 
    /// </summary>
    MySqlConnection rqConnection;


    [Fact]
    public void RunningAQueryFromStateChangeHandler()
    {
      string connStr = st.GetConnectionString(true);
      using (rqConnection = new MySqlConnection(connStr))
      {
        rqConnection.StateChange += new StateChangeEventHandler(RunningQueryStateChangeHandler);
        rqConnection.Open();
      }
    }

    void RunningQueryStateChangeHandler(object sender, StateChangeEventArgs e)
    {
      if (e.CurrentState == ConnectionState.Open)
      {
        MySqlCommand cmd = new MySqlCommand("SELECT 1", rqConnection);
        object o = cmd.ExecuteScalar();
        Assert.Equal(1, Convert.ToInt32(o));
      }
    }

    /// <summary>
    /// Bug #31262 NullReferenceException in MySql.Data.MySqlClient.NativeDriver.ExecuteCommand 
    /// </summary>
    [Fact]
    public void ConnectionNotOpenThrowningBadException()
    {
      MySqlConnection c2 = new MySqlConnection();
      c2.ConnectionString = st.GetConnectionString(true); // "DataSource=localhost;Database=test;UserID=root;Password=********;PORT=3306;Allow Zero Datetime=True;logging=True;";
      //conn.Open();                      << REM
      MySqlCommand command = new MySqlCommand();
      command.Connection = c2;

      MySqlCommand cmdCreateTable = new MySqlCommand("DROP TABLE IF EXISTS `test`.`contents_catalog`", c2);
      cmdCreateTable.CommandType = CommandType.Text;
      cmdCreateTable.CommandTimeout = 0;
      try
      {
        cmdCreateTable.ExecuteNonQuery();
      }
      catch (InvalidOperationException)
      {
      }
    }

    /// <summary>
    /// Bug #31433 Username incorrectly cached for logon where case sensitive 
    /// </summary>
    [Fact]
    public void CaseSensitiveUserId()
    {
      string connStr = st.GetConnectionString("Test", "test", true);
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        try
        {
          c.Open();
        }
        catch (MySqlException)
        {
        }
      }

      connStr = st.GetConnectionString("test", "test", true);
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
      }
    }

    /// <summary>
    /// Bug #35619 creating a MySql connection from toolbox generates an error 
    /// </summary>
    [Fact]
    public void NullConnectionString()
    {
      MySqlConnection c = new MySqlConnection();
      c.ConnectionString = null;
    }

    /// <summary>
    /// Bug #53097  	Connection.Ping() closes connection if executed on a connection with datareader
    /// </summary>
    [Fact]
    public void PingWhileReading()
    {
      MySqlCommand command = new MySqlCommand("SELECT 1", st.conn);

      using (MySqlDataReader reader = command.ExecuteReader())
      {
        reader.Read();
        try
        {
          st.conn.Ping();
          Assert.False(true, "Test Failed.");
        }
        catch (MySqlException ex)
        {
          Assert.Equal(Resources.DataReaderOpen, ex.Message);
        }
      }
    }

    /// <summary>
    /// Test if keepalive parameters work.
    /// </summary>
    [Fact]
    public void Keepalive()
    {
      string connstr = st.GetConnectionString("test", "test", true);
      connstr += ";keepalive=1;";
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        c.Open();
      }
    }

#if !CF
    [Fact]
    public void CanOpenConnectionInMediumTrust()
    {
      AppDomain appDomain = PartialTrustSandbox.CreatePartialTrustDomain();

      PartialTrustSandbox sandbox = (PartialTrustSandbox)appDomain.CreateInstanceAndUnwrap(
          typeof(PartialTrustSandbox).Assembly.FullName,
          typeof(PartialTrustSandbox).FullName);

      try
      {
        MySqlConnection connection = sandbox.TryOpenConnection(st.GetConnectionString(true));
        Assert.True(null != connection);

        Assert.True(connection.State == ConnectionState.Open);
        connection.Close();

        //Now try with logging enabled
        connection = sandbox.TryOpenConnection(st.GetConnectionString(true) + ";logging=true");
        Assert.True(null != connection);
        Assert.True(connection.State == ConnectionState.Open);
        connection.Close();

        //Now try with Usage Advisor enabled
        connection = sandbox.TryOpenConnection(st.GetConnectionString(true) + ";Use Usage Advisor=true");
        Assert.True(null != connection);
        Assert.True(connection.State == ConnectionState.Open);
        connection.Close();
      }
      finally
      {
        AppDomain.Unload(appDomain);
      }
    }

    /// <summary>
    /// A client can connect to MySQL server using SSL and a pfx file.
    /// <remarks>
    /// This test requires starting the server with SSL support. 
    /// For instance, the following command line enables SSL in the server:
    /// mysqld --no-defaults --standalone --console --ssl-ca='MySQLServerDir'\mysql-test\std_data\cacert.pem --ssl-cert='MySQLServerDir'\mysql-test\std_data\server-cert.pem --ssl-key='MySQLServerDir'\mysql-test\std_data\server-key.pem
    /// </remarks>
    /// </summary>
    [Fact]
    public void CanConnectUsingFileBasedCertificate()
    {
      if (st.Version < new Version(5, 1)) return;

      string connstr = st.GetConnectionString(true);
      connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        c.Open();
        Assert.Equal(ConnectionState.Open, c.State);
      }
    }
#endif

#if CF
        /// <summary>
        /// A client running in .NET Compact Framework can't connect to MySQL server using SSL and a pfx file.
        /// <remarks>
        /// This test requires starting the server with SSL support. 
        /// For instance, the following command line enables SSL in the server:
        /// mysqld --no-defaults --standalone --console --ssl-ca='MySQLServerDir'\mysql-test\std_data\cacert.pem --ssl-cert='MySQLServerDir'\mysql-test\std_data\server-cert.pem --ssl-key='MySQLServerDir'\mysql-test\std_data\server-key.pem
        /// </remarks>
        /// </summary>
        [Fact]
        [ExpectedException(typeof(ArgumentException))]
        public void CannotConnectUsingFileBasedCertificateInCF()
        {
            string connstr = GetConnectionString(true);
            connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";

            MySqlConnection c = new MySqlConnection(connstr);
        }
#endif

    [Fact]
    public void CanOpenConnectionAfterAborting()
    {
      MySqlConnection connection = new MySqlConnection(st.GetConnectionString(true));
      connection.Open();
      Assert.Equal(ConnectionState.Open, connection.State);

      connection.Abort();
      Assert.Equal(ConnectionState.Closed, connection.State);

      connection.Open();
      Assert.Equal(ConnectionState.Open, connection.State);

      connection.Close();
    }

    /// <summary>
    /// A client can authenticate to a server that requires using old passwords.
    /// <remarks>
    /// This test requires starting the server with the old-passwords option.
    /// </remarks>
    /// </summary>
    [Fact]
    public void CanAuthenticateUsingOldPasswords()
    {
      st.suExecSQL(String.Format("GRANT USAGE ON `{0}`.* TO 'oldpassworduser'@'%' IDENTIFIED BY '123456'", st.database0));
      st.suExecSQL(String.Format("GRANT SELECT ON `{0}`.* TO 'oldpassworduser'@'%'", st.database0));

      MySqlConnection connection = null;

      Assert.DoesNotThrow(delegate()
      {
        try
        {
          connection = new MySqlConnection(st.GetConnectionString("oldpassworduser", "123456", true));
          connection.Open();

          Assert.True(connection.State == ConnectionState.Open);
          connection.Close();
        }
        finally
        {
          st.suExecSQL("drop user oldpassworduser");
        }
      });
    }


#if !CF
    /// <summary>
    /// Fix for bug http://bugs.mysql.com/bug.php?id=63942 (Connections not closed properly when using pooling)
    /// </summary>
    [Fact]
    public void ReleasePooledConnectionsProperly()
    {
      MySqlConnection con = new MySqlConnection(st.GetConnectionString(true));
      MySqlCommand cmd = new MySqlCommand("show global status like 'aborted_clients'", con);
      con.Open();
      MySqlDataReader r = cmd.ExecuteReader();
      r.Read();
      int numClientsAborted = r.GetInt32(1);
      r.Close();

      AppDomain appDomain = FullTrustSandbox.CreateFullTrustDomain();


      FullTrustSandbox sandbox = (FullTrustSandbox)appDomain.CreateInstanceAndUnwrap(
          typeof(FullTrustSandbox).Assembly.FullName,
          typeof(FullTrustSandbox).FullName);

      try
      {
        for (int i = 0; i < 200; i++)
        {
          MySqlConnection connection = sandbox.TryOpenConnection(st.GetPoolingConnectionString());
          Assert.True(null != connection);
          Assert.True(connection.State == ConnectionState.Open);
          connection.Close();
        }
      }
      finally
      {
        AppDomain.Unload(appDomain);
      }
      r = cmd.ExecuteReader();
      r.Read();
      int numClientsAborted2 = r.GetInt32(1);
      r.Close();
      Assert.Equal(numClientsAborted, numClientsAborted2);
      con.Close();
    }
#endif

    public void Dispose()
    {
      if (st.conn.State != ConnectionState.Open)
        st.conn.Open();
      try
      {
        if (st.Version.Major < 5)
          st.suExecSQL("REVOKE ALL PRIVILEGES, GRANT OPTION FROM 'test'");
        else
          st.suExecSQL("DROP USER 'test'@'localhost'");
      }
      catch (MySqlException)
      { }          
    }
    
  }
}
