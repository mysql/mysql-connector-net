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
using System.Data;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Properties;
using Xunit;
#if !RT
using System.Configuration;
using System.Security.Permissions;
#endif
#if NET_45_OR_GREATER
using System.Threading.Tasks;
#endif


namespace MySql.Data.MySqlClient.Tests
{
  public class MySqlConnectionTests : IUseFixture<SetUpClass>, IDisposable
  {
      private SetUpClass st;

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
#if !RT
      Assert.True(System.Data.ConnectionState.Closed == c.State, "State");
#endif

          c = new MySqlConnection("connection timeout=25; user id=myuser; " +
              "password=mypass; database=Test;server=myserver; use compression=true; " +
              "pooling=false;min pool size=5; max pool size=101");

          // public properties
          Assert.True(25 == c.ConnectionTimeout, "ConnectionTimeout");
          Assert.True("Test" == c.Database, "Database");
          Assert.True("myserver" == c.DataSource, "DataSource");
          Assert.True(true == c.UseCompression, "Use Compression");
#if !RT
      Assert.True(System.Data.ConnectionState.Closed == c.State, "State");
#endif

          c.ConnectionString = "connection timeout=15; user id=newuser; " +
              "password=newpass; port=3308; database=mydb; data source=myserver2; " +
              "use compression=true; pooling=true; min pool size=3; max pool size=76";

          // public properties
          Assert.True(15 == c.ConnectionTimeout, "ConnectionTimeout");
          Assert.True("mydb" == c.Database, "Database");
          Assert.True("myserver2" == c.DataSource, "DataSource");
          Assert.True(true == c.UseCompression, "Use Compression");
#if !RT
      Assert.True(System.Data.ConnectionState.Closed == c.State, "State");
#endif
      }

#if !RT  //No Security.Principal on CF

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
              Assert.False(true, "This should not work");
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

          try
          {
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
          finally
          {
            st.execSQL("DROP TABLE IF EXISTS `test`");
          }
      }

      /// <summary>
      /// Bug #10281 Clone issue with MySqlConnection 
      /// Bug #27269 MySqlConnection.Clone does not mimic SqlConnection.Clone behaviour 
      /// </summary>
      [Fact]
      public void TestConnectionClone()
      {
#if !RT
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
#endif
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

#if !RT
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

#if !RT
      AppDomain appDomain = FullTrustSandbox.CreateFullTrustDomain();
      

      FullTrustSandbox sandbox = (FullTrustSandbox)appDomain.CreateInstanceAndUnwrap(
          typeof(FullTrustSandbox).Assembly.FullName,
          typeof(FullTrustSandbox).FullName);
#endif

          try
          {
              for (int i = 0; i < 200; i++)
              {
#if RT
                  MySqlConnection connection = new MySqlConnection(st.GetPoolingConnectionString());
                  connection.Open();
#else
          MySqlConnection connection = sandbox.TryOpenConnection(st.GetPoolingConnectionString());
#endif
                  Assert.NotNull(connection);
                  Assert.True(connection.State == ConnectionState.Open);
                  connection.Close();
              }
          }
          finally
          {
#if !RT
        AppDomain.Unload(appDomain);
#endif
          }
          r = cmd.ExecuteReader();
          r.Read();
          int numClientsAborted2 = r.GetInt32(1);
          r.Close();
          Assert.Equal(numClientsAborted, numClientsAborted2);
          con.Close();
      }

      /// <summary>
      /// Test for Connect attributes feature used in MySql Server > 5.6.6
      /// (Stores client connection data on server)
      /// </summary>
      [Fact]
      public void ConnectAttributes()
      {
          if (st.Version < new Version(5, 6, 6)) return;
          using (MySqlConnection connection = new MySqlConnection(st.GetConnectionString(st.rootUser, st.rootPassword, false)))
          {
              connection.Open();
              if (connection.driver.SupportsConnectAttrs)
              {
                  MySqlCommand cmd = new MySqlCommand("SELECT * FROM performance_schema.session_connect_attrs WHERE PROCESSLIST_ID = connection_id()", connection);
                  MySqlDataReader dr = cmd.ExecuteReader();
                  Assert.True(dr.HasRows, "No session_connect_attrs found");
                  MySqlConnectAttrs connectAttrs = new MySqlConnectAttrs();
                  bool isValidated = false;
                  while (dr.Read())
                  {
                      if (dr.GetString(1) == "_client_name")
                      {
                          Assert.Equal(connectAttrs.ClientName, dr.GetString(2));
                          isValidated = true;
                          break;
                      }
                  }
                  Assert.True(isValidated, "Missing _client_version attribute");
              }
          }
      }

      /// <summary>
      /// Test for password expiration feature in MySql Server 5.6 or higher
      /// </summary>
      [Fact]
      public void PasswordExpiration()
      {
          const string expireduser = "expireduser";
          const string expiredhost = "localhost";
          string expiredfull = string.Format("'{0}'@'{1}'", expireduser, expiredhost);

          using (MySqlConnection conn = new MySqlConnection(st.GetConnectionString(st.rootUser, st.rootPassword, true)))
          {
              conn.Open();
              if (st.Version >= new Version(5, 6, 6))
              {
                  MySqlCommand cmd = new MySqlCommand("", conn);

                  // creates expired user
                  cmd.CommandText = string.Format("SELECT COUNT(*) FROM mysql.user WHERE user='{0}' AND host='{1}'", expireduser, expiredhost);
                  long count = (long)cmd.ExecuteScalar();
                  if (count > 0)
                      st.suExecSQL(string.Format("DROP USER " + expiredfull));

                  st.suExecSQL(string.Format("CREATE USER {0} IDENTIFIED BY '{1}1'", expiredfull, expireduser));
                  st.suExecSQL(string.Format("GRANT SELECT ON `{0}`.* TO {1}", conn.Database, expiredfull));

                  st.suExecSQL(string.Format("ALTER USER {0} PASSWORD EXPIRE", expiredfull));
                  conn.Close();

                  // validates expired user
                  conn.ConnectionString = st.GetConnectionString(expireduser, expireduser + "1", true);
                  conn.Open();

                  cmd.CommandText = "SELECT 1";
                  MySqlException ex = Assert.Throws<MySqlException>(() => cmd.ExecuteScalar());
                  Assert.Equal(1820, ex.Number);

                  if (st.Version >= new Version(5, 7, 6))
                    cmd.CommandText = string.Format("SET PASSWORD = '{0}1'", expireduser);
                  else
                    cmd.CommandText = string.Format("SET PASSWORD = PASSWORD('{0}1')", expireduser);

                  cmd.ExecuteNonQuery();
                  conn.Close();

                  conn.Open();
                  cmd.CommandText = "SELECT 1";
                  cmd.ExecuteScalar();

                  st.suExecSQL(string.Format("DROP USER " + expiredfull));
              }
              else
              {
                  System.Diagnostics.Debug.WriteLine("Password expire not supported in this server version.");
              }
          }
      }

      /// <summary>
      /// As part of feedback from bug http://bugs.mysql.com/bug.php?id=66647 (Arithmetic operation resulted in an overflow).
      /// </summary>
      [Fact]
      public void OldPasswordNotSupported()
      {

          if (st.Version > new Version(5, 6)) return;
          //get value of flag 'old_passwords'
          MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder(st.GetConnectionString(true));
          MySqlConnection con = new MySqlConnection(csb.ToString());
          MySqlCommand cmd = new MySqlCommand("show variables like 'old_passwords'", con);
          string db = con.Settings.Database;
          con.Open();
          MySqlDataReader r = cmd.ExecuteReader();
          r.Read();
          object o = r.GetValue(1);
          if (o.ToString() == "OFF")
              o = "0";
          int old_passwords = Convert.ToInt32(o);
          r.Close();
          con.Close();
          if (old_passwords == 0)
          {
              //System.Diagnostics.Debug.Write("This test must be ran with old_passwords=0");
              st.ExecuteSQLAsRoot("set old_passwords=1;");
              //return;
          }
          // create user
          cmd.CommandText = "select count( * ) from mysql.user where user = 'myoldpassuser' and host = 'localhost'";
          cmd.Connection = st.rootConn;
          int n = Convert.ToInt32(cmd.ExecuteScalar());
          if (n != 0)
          {
              st.ExecuteSQLAsRoot("drop user 'myoldpassuser'@'localhost'");
          }
          // user with old password is different depending upon the version.
          if (st.Version.Minor >= 6)
          {
              st.ExecuteSQLAsRoot("create user 'myoldpassuser'@'localhost' IDENTIFIED with 'mysql_old_password'");
          }
          else
          {
              st.ExecuteSQLAsRoot("create user 'myoldpassuser'@'localhost' ");
          }
          // setup user with old password, attempt to open connection with it, must fail
          st.ExecuteSQLAsRoot(string.Format("grant all on `{0}`.* to 'myoldpassuser'@'localhost'", db));
          st.ExecuteSQLAsRoot("set password for 'myoldpassuser'@'localhost' = old_password( '123' )");
          //con.Settings.UserID = "myoldpassuser";
          //con.Settings.Password = "123";
          csb.UserID = "myoldpassuser";
          csb.Password = "123";
          con.ConnectionString = csb.ToString();
          Exception ex = Assert.Throws<MySqlException>(() => con.Open());
          Assert.Equal(Resources.OldPasswordsNotSupported, ex.Message);
          con.Close();


          if (old_passwords == 0)
          {
              st.ExecuteSQLAsRoot("set old_passwords=0;");
          }
          st.ExecuteSQLAsRoot("drop user 'myoldpassuser'@'localhost'");

      }

      public void Dispose()
      {
      }

      [Fact]
      public void TestNonSupportedOptions()
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

#if NET_45_OR_GREATER
      #region Async
      [Fact]
      public async Task TransactionAsync()
      {
        st.execSQL("Create Table CONNTransactionAsyncTest(key2 varchar(50), name varchar(50), name2 varchar(50))");
        st.execSQL("INSERT INTO CONNTransactionAsyncTest VALUES('P', 'Test1', 'Test2')");

        try
        {
          MySqlTransaction txn = st.conn.BeginTransactionAsync().Result;
          MySqlConnection c = txn.Connection;
          Assert.Equal(st.conn, c);
          MySqlCommand cmd = new MySqlCommand("SELECT name, name2 FROM CONNTransactionAsyncTest WHERE key2='P'", st.conn, txn);
          MySqlTransaction t2 = cmd.Transaction;
          Assert.Equal(txn, t2);
          MySqlDataReader reader = null;
          try
          {
            reader = cmd.ExecuteReader();
            reader.Close();
            txn.Commit();
          }
          catch (Exception ex)
          {
            Assert.False(ex.Message != string.Empty, ex.Message);
            txn.Rollback();
          }
          finally
          {
            if (reader != null) reader.Close();
          }
        }
        finally
        {
          st.execSQL("DROP TABLE `CONNTransactionAsyncTest`;");
        }
      }

      [Fact]
      public async Task ChangeDataBaseAsync()
      {
        if (st.Version < new Version(4, 1)) return;

        st.execSQL("CREATE TABLE CONNChangeDBAsyncTest (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");
        st.execSQL("INSERT INTO CONNChangeDBAsyncTest (id, name) VALUES (1,'test1')");
        st.execSQL("INSERT INTO CONNChangeDBAsyncTest (id, name) VALUES (2,'test2')");
        st.execSQL("INSERT INTO CONNChangeDBAsyncTest (id, name) VALUES (3,'test3')");

        try
        {
          await st.conn.ChangeDataBaseAsync(st.database1);

          MySqlDataAdapter da = new MySqlDataAdapter(
              String.Format("SELECT id, name FROM `{0}`.CONNChangeDBAsyncTest", st.database0), st.conn);
          MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
          DataSet ds = new DataSet();
          da.Fill(ds);

          ds.Tables[0].Rows[0]["id"] = 4;
          DataSet changes = ds.GetChanges();
          da.Update(changes);
          ds.Merge(changes);
          ds.AcceptChanges();
          cb.Dispose();

          await st.conn.ChangeDataBaseAsync(st.database0);
        }
        finally
        {
          st.execSQL("DROP TABLE CONNChangeDBAsyncTest");
        }
      }

      [Fact]
      public async Task OpenAndCloseConnectionAsync()
      {
        string connStr2 = st.GetConnectionString(false);
        MySqlConnection c = new MySqlConnection(connStr2);
        await c.OpenAsync();
        await c.CloseAsync();
      }

      [Fact]
      public async Task ClearPoolAsync()
      {
        MySqlConnection c1 = new MySqlConnection(st.GetConnectionString(true));
        MySqlConnection c2 = new MySqlConnection(st.GetConnectionString(true));
        c1.Open();
        c2.Open();
        c1.Close();
        c2.Close();
        await c1.ClearPoolAsync(c1);
        await c2.ClearPoolAsync(c1);
      }

      [Fact]
      public async Task ClearAllPoolsAsync()
      {
        MySqlConnection c1 = new MySqlConnection(st.GetConnectionString(true));
        MySqlConnection c2 = new MySqlConnection(st.GetConnectionString(true));
        c1.Open();
        c2.Open();
        c1.Close();
        c2.Close();
        await c1.ClearAllPoolsAsync();
        await c2.ClearAllPoolsAsync();
      }

      [Fact]
      public async Task GetSchemaCollectionAsync()
      {
        MySqlConnection c1 = new MySqlConnection(st.GetConnectionString(true));
        c1.Open();
        var schemaColl = await c1.GetSchemaCollectionAsync(SchemaProvider.MetaCollection, null);
        c1.Close();
        Assert.NotNull(schemaColl);
      }
      #endregion
#endif

    [Fact]
    public void SslPreferredByDefault()
    {
      string connectionString = st.GetConnectionString(true);
      Assert.DoesNotContain("ssl", connectionString, StringComparison.OrdinalIgnoreCase);
      using (MySqlConnection connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.Equal("TLSv1", reader.GetString(1));
        }
      }
    }

    [Fact]
    public void SslOverrided()
    {
      string connectionString = st.GetConnectionString(true) + ";Ssl mode=None";
      using (MySqlConnection connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.Equal(string.Empty, reader.GetString(1));
        }
      }
    }
  }
}
