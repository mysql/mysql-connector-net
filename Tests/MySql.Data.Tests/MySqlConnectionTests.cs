// Copyright © 2013, 2016 Oracle and/or its affiliates. All rights reserved.
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
using System.Threading.Tasks;
using Xunit;


namespace MySql.Data.MySqlClient.Tests
{
  public class MySqlConnectionTests : TestBase
  {

    protected TestSetup ts;
    public MySqlConnectionTests(TestSetup setup) : base(setup, "connection")
    {
      ts = setup;
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


    [Fact]
    public void TestConnectingSocketBadUserName()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connection.ConnectionString);
      connStr.UserID = "bad_one";
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      var exception = Record.Exception(() => c.Open());
      Assert.NotNull(exception);
      Assert.IsType<MySqlException>(exception);
    }

    [Fact]
    public void TestConnectingSocketBadDbName()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connection.ConnectionString);
      connStr.Password = "bad_pwd";
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      var exception = Record.Exception(() => c.Open());
      Assert.NotNull(exception);
      Assert.IsType<MySqlException>(exception);
    }

    [Fact]
    public void TestPersistSecurityInfoCachingPasswords()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connection.ConnectionString);

      // Persist Security Info = true means that it should be returned
      connStr.PersistSecurityInfo = true;
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();
      MySqlConnectionStringBuilder afterOpenSettings = new MySqlConnectionStringBuilder(c.ConnectionString);
      Assert.Equal(connStr.Password, afterOpenSettings.Password);

      // Persist Security Info = false means that it should not be returned
      connStr.PersistSecurityInfo = false;
      c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();
      afterOpenSettings = new MySqlConnectionStringBuilder(c.ConnectionString);
      Assert.True(String.IsNullOrEmpty(afterOpenSettings.Password));
    }

    [Fact]
    public void ChangeDatabase()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connection.ConnectionString);
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      using (c)
      {
        c.Open();
        Assert.True(c.State == ConnectionState.Open);
        Assert.Equal(connStr.Database, c.Database);

        string dbName = CreateDatabase("db1");
        c.ChangeDatabase(dbName);
        Assert.Equal(dbName, c.Database);
      }
    }

    [Fact]
    public void ConnectionTimeout()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connection.ConnectionString);
      connStr.Server = "bad_host";
      connStr.ConnectionTimeout = 5;
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));

      DateTime start = DateTime.Now;
      var exception = Record.Exception(() => c.Open());
      Assert.NotNull(exception);
      TimeSpan diff = DateTime.Now.Subtract(start);
      Assert.True(diff.TotalSeconds < 10, "Timeout exceeded");
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
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connection.ConnectionString);
      connStr.Database = null;
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();

      executeAsRoot("GRANT ALL ON *.* to 'nopass'@'%'");
      executeAsRoot("GRANT ALL ON *.* to 'nopass'@'localhost'");
      executeAsRoot("FLUSH PRIVILEGES");

      // connect with no password
      connStr.UserID = "nopass";
      connStr.Password = null;
      c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();

      connStr.Password = "";
      c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();
    }

    [Fact]
    public void ConnectingAsUTF8()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connection.ConnectionString);
      connStr.CharacterSet = "utf8";
      using (MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true)))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand(
            "CREATE TABLE test (id varbinary(16), active bit) CHARACTER SET utf8", c);
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO test (id, active) VALUES (CAST(0x1234567890 AS Binary), true)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO test (id, active) VALUES (CAST(0x123456789a AS Binary), true)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO test (id, active) VALUES (CAST(0x123456789b AS Binary), true)";
        cmd.ExecuteNonQuery();
      }

      using (MySqlConnection d = new MySqlConnection(connStr.GetConnectionString(true)))
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
    public void TestConnectionCloneRetainsPassword()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connection.ConnectionString);
      connStr.PersistSecurityInfo = false;

      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();
      MySqlConnection clone = (MySqlConnection)c.Clone();
      clone.Open();
      clone.Close();
    }

    /// <summary>
    /// Bug #13321  	Persist security info does not woek
    /// </summary>
    [Fact]
    public void PersistSecurityInfo()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connection.ConnectionString);
      connStr.PersistSecurityInfo = false;

      Assert.False(String.IsNullOrEmpty(connStr.Password));
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();
      connStr = new MySqlConnectionStringBuilder(c.ConnectionString);
      Assert.True(String.IsNullOrEmpty(connStr.Password));
    }

    /// <summary>
    /// Bug #13658  	connection.state does not update on Ping()
    /// </summary>
    [Fact]
    public void PingUpdatesState()
    {
      var conn2 = GetConnection();
      conn2.Open();
      KillConnection(conn2);
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
      executeAsRoot("GRANT ALL ON *.* to 'quotedUser'@'%' IDENTIFIED BY '\"'");
      executeAsRoot("GRANT ALL ON *.* to 'quotedUser'@'localhost' IDENTIFIED BY '\"'");
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(connection.ConnectionString);
      settings.UserID = "quotedUser";
      settings.Password = "\"";
      using (MySqlConnection c = new MySqlConnection(connection.ConnectionString))
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
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connection.ConnectionString);
      connStr.Server = "foobar";
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      var exception = Record.Exception(() => c.Open());
      Assert.NotNull(exception);
      Assert.IsType<MySqlException>(exception);
      MySqlException ex = exception as MySqlException;
      Assert.Equal((int)MySqlErrorCode.UnableToConnectToHost, ex.Number);
    }

    /// <summary>
    /// Bug #29123  	Connection String grows with each use resulting in OutOfMemoryException
    /// </summary>
    [Fact]
    public void ConnectionStringNotAffectedByChangeDatabase()
    {
      for (int i = 0; i < 10; i++)
      {
        string connStr = connection.ConnectionString + ";pooling=false";
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

      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connection.ConnectionString);
      connStr.Pooling = false;
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      c.StateChange += new StateChangeEventHandler(check.stateChangeHandler);
      c.Open();
      threadId = c.ServerThread;
      c = null;
      GC.Collect();
      GC.WaitForPendingFinalizers();
      Assert.True(check.closed);
    }

    //    /// <summary>
    //    /// Bug #30964 StateChange imperfection 
    //    /// </summary>
    //    MySqlConnection rqConnection;


    //    [Fact]
    //    public void RunningAQueryFromStateChangeHandler()
    //    {
    //      string connStr = st.GetConnectionString(true);
    //      using (rqConnection = new MySqlConnection(connStr))
    //      {
    //        rqConnection.StateChange += new StateChangeEventHandler(RunningQueryStateChangeHandler);
    //        rqConnection.Open();
    //      }
    //    }

    //    void RunningQueryStateChangeHandler(object sender, StateChangeEventArgs e)
    //    {
    //      if (e.CurrentState == ConnectionState.Open)
    //      {
    //        MySqlCommand cmd = new MySqlCommand("SELECT 1", rqConnection);
    //        object o = cmd.ExecuteScalar();
    //        Assert.Equal(1, Convert.ToInt32(o));
    //      }
    //    }

    /// <summary>
    /// Bug #31262 NullReferenceException in MySql.Data.MySqlClient.NativeDriver.ExecuteCommand 
    /// </summary>
    [Fact]
    public void ConnectionNotOpenThrowningBadException()
    {
      var c2 = GetConnection();
      //conn.Open();                      << REM
      MySqlCommand command = new MySqlCommand();
      command.Connection = c2;

      MySqlCommand cmdCreateTable = new MySqlCommand("DROP TABLE IF EXISTS `test`.`contents_catalog`", c2);
      cmdCreateTable.CommandType = CommandType.Text;
      cmdCreateTable.CommandTimeout = 0;
      var exception = Record.Exception(() => cmdCreateTable.ExecuteNonQuery());
      Assert.NotNull(exception);
      Assert.IsType<InvalidOperationException>(exception);
    }

    /// <summary>
    /// Bug #31433 Username incorrectly cached for logon where case sensitive 
    /// </summary>
    [Fact]
    public void CaseSensitiveUserId()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connection.ConnectionString);
      string original_uid = connStr.UserID;
      connStr.UserID = connStr.UserID.ToUpper();
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      var exception = Record.Exception(() => c.Open());
      Assert.NotNull(exception);
      Assert.IsType<MySqlException>(exception);

      connStr.UserID = original_uid;
      c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();
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
      using (MySqlConnection conn = new MySqlConnection(connection.ConnectionString))
      {
        conn.Open();
        MySqlCommand command = new MySqlCommand("SELECT 1", conn);

        using (MySqlDataReader reader = command.ExecuteReader())
        {
          reader.Read();
          var exception = Record.Exception(() => conn.Ping());
          Assert.NotNull(exception);
          Assert.IsType<MySqlException>(exception);
        }
      }
    }

    /// <summary>
    /// Test if keepalive parameters work.
    /// </summary>
    [Fact]
    public void Keepalive()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(connection.ConnectionString);
      connStr.Keepalive = 1;
      using (MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true)))
      {
        c.Open();
      }
    }

    //    [Fact]
    //    public void CanOpenConnectionInMediumTrust()
    //    {
    //      AppDomain appDomain = PartialTrustSandbox.CreatePartialTrustDomain();

    //      PartialTrustSandbox sandbox = (PartialTrustSandbox)appDomain.CreateInstanceAndUnwrap(
    //          typeof(PartialTrustSandbox).Assembly.FullName,
    //          typeof(PartialTrustSandbox).FullName);

    //      try
    //      {
    //        MySqlConnection connection = sandbox.TryOpenConnection(st.GetConnectionString(true));
    //        Assert.True(null != connection);

    //        Assert.True(connection.State == ConnectionState.Open);
    //        connection.Close();

    //        //Now try with logging enabled
    //        connection = sandbox.TryOpenConnection(st.GetConnectionString(true) + ";logging=true");
    //        Assert.True(null != connection);
    //        Assert.True(connection.State == ConnectionState.Open);
    //        connection.Close();

    //        //Now try with Usage Advisor enabled
    //        connection = sandbox.TryOpenConnection(st.GetConnectionString(true) + ";Use Usage Advisor=true");
    //        Assert.True(null != connection);
    //        Assert.True(connection.State == ConnectionState.Open);
    //        connection.Close();
    //      }
    //      finally
    //      {
    //        AppDomain.Unload(appDomain);
    //      }
    //    }

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
      string connstr = connection.ConnectionString;
      connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        c.Open();
        Assert.Equal(ConnectionState.Open, c.State);
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", c);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.StartsWith("TLSv1", reader.GetString(1));
        }
      }
    }

    [Fact]
    public void CanOpenConnectionAfterAborting()
    {
      MySqlConnection connection = new MySqlConnection(ts.GetConnection(false).ConnectionString);
      connection.Open();
      Assert.Equal(ConnectionState.Open, connection.State);

      connection.Abort();
      Assert.Equal(ConnectionState.Closed, connection.State);

      connection.Open();
      Assert.Equal(ConnectionState.Open, connection.State);

      connection.Close();
    }


    ///// <summary>
    ///// Fix for bug http://bugs.mysql.com/bug.php?id=63942 (Connections not closed properly when using pooling)
    ///// </summary>
    //[Fact]
    //public void ReleasePooledConnectionsProperly()
    //{
    //    MySqlConnection con = new MySqlConnection(st.GetConnectionString(true));
    //    MySqlCommand cmd = new MySqlCommand("show global status like 'aborted_clients'", con);
    //    con.Open();
    //    MySqlDataReader r = cmd.ExecuteReader();
    //    r.Read();
    //    int numClientsAborted = r.GetInt32(1);
    //    r.Close();

    //    AppDomain appDomain = FullTrustSandbox.CreateFullTrustDomain();


    //    FullTrustSandbox sandbox = (FullTrustSandbox)appDomain.CreateInstanceAndUnwrap(
    //        typeof(FullTrustSandbox).Assembly.FullName,
    //        typeof(FullTrustSandbox).FullName);

    //    try
    //    {
    //        for (int i = 0; i < 200; i++)
    //        {
    //            MySqlConnection connection = sandbox.TryOpenConnection(st.GetPoolingConnectionString());
    //            Assert.NotNull(connection);
    //            Assert.True(connection.State == ConnectionState.Open);
    //            connection.Close();
    //        }
    //    }
    //    finally
    //    {
    //        AppDomain.Unload(appDomain);
    //    }
    //    r = cmd.ExecuteReader();
    //    r.Read();
    //    int numClientsAborted2 = r.GetInt32(1);
    //    r.Close();
    //    Assert.Equal(numClientsAborted, numClientsAborted2);
    //    con.Close();
    //}

    /// <summary>
    /// Test for Connect attributes feature used in MySql Server > 5.6.6
    /// (Stores client connection data on server)
    /// </summary>
    [Fact]
    public void ConnectAttributes()
    {
      if (ts.version < new Version(5, 6, 6)) return;
      using (MySqlConnection connection = new MySqlConnection(ts.GetConnection(true).ConnectionString))
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
            if (dr.GetString(1).ToLowerInvariant().Contains("_client_name"))
            {
              Assert.Equal(connectAttrs.ClientName, dr.GetString(2));
              isValidated = true;
              break;
            }
          }
          Assert.True(isValidated, "Missing _client_name attribute");
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

      using (MySqlConnection conn = ts.GetConnection(true))
      {
        conn.Open();
        if (ts.version >= new Version(5, 6, 6))
        {
          MySqlCommand cmd = new MySqlCommand("", conn);

          // creates expired user
          cmd.CommandText = string.Format("SELECT COUNT(*) FROM mysql.user WHERE user='{0}' AND host='{1}'", expireduser, expiredhost);
          long count = (long)cmd.ExecuteScalar();
          if (count > 0)
            ts.executeInternal(string.Format("DROP USER " + expiredfull), conn);

          ts.executeInternal(string.Format("CREATE USER {0} IDENTIFIED BY '{1}1'", expiredfull, expireduser), conn);
          ts.executeInternal(string.Format("GRANT SELECT ON `{0}`.* TO {1}", conn.Database, expiredfull), conn);

          ts.executeInternal(string.Format("ALTER USER {0} PASSWORD EXPIRE", expiredfull), conn);
          conn.Close();

          // validates expired user
          var cnstrBuilder = new MySqlConnectionStringBuilder(ts.GetConnection(true).ConnectionString);
          cnstrBuilder.UserID = expireduser;
          cnstrBuilder.Password = expireduser + "1";
          conn.ConnectionString = cnstrBuilder.ConnectionString;
          conn.Open();

          cmd.CommandText = "SELECT 1";
          MySqlException ex = Assert.Throws<MySqlException>(() => cmd.ExecuteScalar());
          Assert.Equal(1820, ex.Number);

          if (ts.version >= new Version(5, 7, 6))
            cmd.CommandText = string.Format("SET PASSWORD = '{0}1'", expireduser);
          else
            cmd.CommandText = string.Format("SET PASSWORD = PASSWORD('{0}1')", expireduser);

          cmd.ExecuteNonQuery();
          cmd.CommandText = "SELECT 1";
          cmd.ExecuteScalar();
          conn.Close();
          conn.ConnectionString = ts.GetConnection(true).ConnectionString;
          conn.Open();
          ts.executeInternal(string.Format("DROP USER " + expiredfull), conn);
          conn.Close();
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

      if (ts.version > new Version(5, 6)) return;
      //get value of flag 'old_passwords'
      MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder(ts.GetConnection(false).ConnectionString);
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
      if (old_passwords == 0)
      {
        //System.Diagnostics.Debug.Write("This test must be ran with old_passwords=0");
        ts.executeInternal("set old_passwords=1;", con);
        //return;
      }
      // create user
      cmd.CommandText = "select count( * ) from mysql.user where user = 'myoldpassuser' and host = 'localhost'";
      cmd.Connection = ts.GetConnection(true);
      int n = Convert.ToInt32(cmd.ExecuteScalar());
      if (n != 0)
      {
        ts.executeInternal("drop user 'myoldpassuser'@'localhost'", con);
      }
      // user with old password is different depending upon the version.
      if (ts.version.Minor >= 6)
      {
        ts.executeInternal("create user 'myoldpassuser'@'localhost' IDENTIFIED with 'mysql_old_password'", con);
      }
      else
      {
        ts.executeInternal("create user 'myoldpassuser'@'localhost' ", con);
      }
      // setup user with old password, attempt to open connection with it, must fail
      ts.executeInternal(string.Format("grant all on `{0}`.* to 'myoldpassuser'@'localhost'", db), con);
      ts.executeInternal("set password for 'myoldpassuser'@'localhost' = old_password( '123' )", con);
      con.Close();
      //con.Settings.UserID = "myoldpassuser";
      //con.Settings.Password = "123";
      csb.UserID = "myoldpassuser";
      csb.Password = "123";
      con.ConnectionString = csb.ToString();
      Exception ex = Assert.Throws<MySqlException>(() => con.Open());
      Assert.Equal(Resources.OldPasswordsNotSupported, ex.Message);

      if (old_passwords == 0)
      {
        ts.executeInternal("set old_passwords=0;", con);
      }
      ts.executeInternal("drop user 'myoldpassuser'@'localhost'", con);

      con.Close();

    }

    [Fact]
    public void TestNonSupportedOptions()
    {
      string connstr = ts.GetConnection(true).ConnectionString;
      connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        c.Open();
        Assert.Equal(ConnectionState.Open, c.State);
      }
    }

    #region Async
    [Fact]
    public async Task TransactionAsync()
    {
      executeSQL("CREATE TABLE test(key2 varchar(50), name varchar(50), name2 varchar(50))");

      var conn = GetConnection();
      using (conn)
      {
        conn.Open();
        var txn = conn.BeginTransaction();
        Assert.Equal(conn, txn.Connection);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SET AUTOCOMMIT=0";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO test VALUES ('P', 'Test1', 'Test2')";
        cmd.ExecuteNonQuery();
        txn.Rollback();
        cmd.CommandText = "SELECT COUNT(*) FROM test";
        long cnt = (long)cmd.ExecuteScalar();
        Assert.True(cnt == 0);
      }
    }

    [Fact]
    public async Task ChangeDataBaseAsync()
    {
      string dbName = CreateDatabase("db1");
      executeAsRoot(String.Format(
        "CREATE TABLE `{0}`.`footest` (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))", dbName));

      var conn = GetConnection();
      using (conn)
      {
        conn.Open();
        await conn.ChangeDataBaseAsync(dbName);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM footest";
        var count = cmd.ExecuteScalar();
      }
    }

    [Fact]
    public async Task OpenAndCloseConnectionAsync()
    {
      var conn = GetConnection();
      await conn.OpenAsync();
      Assert.True(conn.State == ConnectionState.Open);
      await conn.CloseAsync();
      Assert.True(conn.State == ConnectionState.Closed);
    }

    [Fact]
    public async Task ClearPoolAsync()
    {
      MySqlConnection c1 = GetConnection();
      MySqlConnection c2 = GetConnection();
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
      MySqlConnection c1 = GetConnection();
      MySqlConnection c2 = GetConnection();
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
      var schemaColl = await connection.GetSchemaCollectionAsync("MetaDataCollections", null);
      Assert.NotNull(schemaColl);
    }

    #endregion

    [Fact]
    public void SslPreferredByDefault()
    {
      MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder();
      csb.Server = Settings.Server;
      csb.Port = Settings.Port;
      csb.UserID = Settings.UserID;
      csb.Password = Settings.Password;
      using (MySqlConnection connection = new MySqlConnection(csb.ToString()))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.StartsWith("TLSv1", reader.GetString(1));
        }
      }
    }

    [Fact]
    public void SslOverrided()
    {
      var cstrBuilder = new MySqlConnectionStringBuilder(ts.GetConnection(true).ConnectionString);
      cstrBuilder.SslMode = MySqlSslMode.None;
      using (MySqlConnection connection = new MySqlConnection(cstrBuilder.ConnectionString))
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
