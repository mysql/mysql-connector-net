// Copyright (c) 2013, 2023, Oracle and/or its affiliates.
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
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Tests
{
  public class ConnectionTests : TestBase
  {
    const string _EXPIRED_USER = "expireduser";

    [Test]
    public void TestConnectionStrings()
    {
      MySqlConnection c = new MySqlConnection();

      // public properties
      Assert.True(15 == c.ConnectionTimeout, "ConnectionTimeout");
      Assert.True(String.Empty == c.Database, "Database");
      Assert.True(String.Empty == c.DataSource, "DataSource");
      Assert.True(false == c.UseCompression, "Use Compression");
      Assert.True(ConnectionState.Closed == c.State, "State");

      c = new MySqlConnection("connection timeout=25; user id=myuser; " +
          "password=mypass; database=Test;server=myserver; use compression=true; " +
          "pooling=false;min pool size=5; max pool size=101");

      // public properties
      Assert.True(25 == c.ConnectionTimeout, "ConnectionTimeout");
      Assert.True("Test" == c.Database, "Database");
      Assert.True("myserver" == c.DataSource, "DataSource");
      Assert.True(true == c.UseCompression, "Use Compression");
      Assert.True(ConnectionState.Closed == c.State, "State");

      c.ConnectionString = "connection timeout=15; user id=newuser; " +
          "password=newpass; port=3308; database=mydb; data source=myserver2; " +
          "use compression=true; pooling=true; min pool size=3; max pool size=76";

      // public properties
      Assert.True(15 == c.ConnectionTimeout, "ConnectionTimeout");
      Assert.True("mydb" == c.Database, "Database");
      Assert.True("myserver2" == c.DataSource, "DataSource");
      Assert.True(true == c.UseCompression, "Use Compression");
      Assert.True(ConnectionState.Closed == c.State, "State");

      // Bug #30791289 - MYSQLCONNECTION(NULL) NOW THROWS NULLREFERENCEEXCEPTION
      var conn = new MySqlConnection($"server={Host};");
      conn.ConnectionString = null;
      Assert.AreEqual(string.Empty, conn.ConnectionString);
    }

    [Test]
    public void ChangeDatabase()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      using (c)
      {
        c.Open();
        Assert.True(c.State == ConnectionState.Open);
        Assert.AreEqual(connStr.Database, c.Database);

        string dbName = CreateDatabase("db1");
        c.ChangeDatabase(dbName);
        Assert.AreEqual(dbName, c.Database);
      }
    }

    /// <summary>
    /// Bug#35731216 Pool exhaustion after timeouts in transactions.
    /// </summary>
    [Test]
    public void ConnectionPoolExhaustion()
    {
      for (var i = 0; i <= 11; i++)
      {
        var ex = Assert.Catch<MySqlException>(() => CreateCommandTimeoutException());
        //Prior to the fix the exception thrown was 'error connecting: Timeout expired.  The timeout period elapsed prior to obtaining a connection from the pool.  This may have occurred because all pooled connections were in use and max pool size was reached.' after the 10th execution.
        Assert.AreEqual("Fatal error encountered during command execution.", ex.Message);
      }
    }

    private void CreateCommandTimeoutException()
    {
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      settings.Pooling = true;
      settings.MaximumPoolSize = 10;
      using (var conn = new MySqlConnection(settings.GetConnectionString(true)))
      {
        conn.Open();
        using (var tran = conn.BeginTransaction())
        {
          using (var cmd = conn.CreateCommand())
          {
            cmd.CommandText = "DO SLEEP(5);";
            cmd.CommandTimeout = 1;
            cmd.ExecuteNonQuery();
          }
        }
      }
    }

    /// <summary>
    /// Bug#35827809 Connector/Net allows a connection that has been disposed to be reopened.
    /// </summary>
    [Test]
    public void ReOpenDisposedConnection()
    {
      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString))
      {
        c.Open();
        c.Close();
        c.Dispose();
        Assert.Throws<InvalidOperationException>(() => c.Open());
      }
    }

    [Test]
    public void ConnectingAsUTF8()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
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
    /// Bug #13658 connection.state does not update on Ping()
    /// </summary>
    [Test]
    public void PingUpdatesState()
    {
      var conn2 = GetConnection();
      KillConnection(conn2);
      Assert.False(conn2.Ping());
      Assert.True(conn2.State == ConnectionState.Closed);
      conn2.Open();
      conn2.Close();
    }

    /// <summary>
    /// Bug #16659  	Can't use double quotation marks(") as password access server by Connector/NET
    /// </summary>
    [Test]
    [Ignore("Fix for 8.0.5")]
    public void ConnectWithQuotePassword()
    {
      ExecuteSQL("GRANT ALL ON *.* to 'quotedUser'@'%' IDENTIFIED BY '\"'", true);
      ExecuteSQL($"GRANT ALL ON *.* to 'quotedUser'@'{Host}' IDENTIFIED BY '\"'", true);
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      settings.UserID = "quotedUser";
      settings.Password = "\"";
      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString))
      {
        c.Open();
      }
    }

    /// <summary>
    /// Bug #24802 Error Handling 
    /// </summary>
    [Test]
    public void TestConnectingSocketBadHostName()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStr.Server = "badHostName";
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      var ex = Assert.Throws<MySqlException>(() => c.Open());
      if (Platform.IsWindows()) Assert.IsTrue(ex.InnerException.GetType() == typeof(ArgumentException));
    }

    /// <summary>
    /// Bug #29123  	Connection String grows with each use resulting in OutOfMemoryException
    /// </summary>
    [Test]
    public void ConnectionStringNotAffectedByChangeDatabase()
    {
      for (int i = 0; i < 10; i++)
      {
        string connStr = Connection.ConnectionString + ";pooling=false";
        connStr = connStr.Replace("database", "Initial Catalog");
        connStr = connStr.Replace("persist security info=true",
            "persist security info=false");
        using (MySqlConnection c = new MySqlConnection(connStr))
        {
          c.Open();
          string str = c.ConnectionString;
          int index = str.IndexOf("Database=");
          Assert.AreEqual(-1, index);
        }
      }
    }

    [Test]
    [Ignore("dotnet core seems to keep objects alive")] // reference https://github.com/dotnet/coreclr/issues/13490
    public void ConnectionCloseByGC()
    {
      int threadId;
      ConnectionClosedCheck check = new ConnectionClosedCheck();

      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStr.Pooling = false;
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      c.StateChange += new StateChangeEventHandler(check.stateChangeHandler);
      c.Open();
      threadId = c.ServerThread;
      WeakReference wr = new WeakReference(c);
      Assert.True(wr.IsAlive);
      c = null;
      GC.Collect();
      GC.WaitForPendingFinalizers();
      Assert.False(wr.IsAlive);
      Assert.True(check.closed);

      MySqlCommand cmd = new MySqlCommand("KILL " + threadId, Connection);
      cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Bug #31262 NullReferenceException in MySql.Data.MySqlClient.NativeDriver.ExecuteCommand
    /// </summary>
    [Test]
    public void ConnectionNotOpenThrowningBadException()
    {
      var c2 = new MySqlConnection(Connection.ConnectionString);
      MySqlCommand command = new MySqlCommand();
      command.Connection = c2;

      MySqlCommand cmdCreateTable = new MySqlCommand("DROP TABLE IF EXISTS `test`.`contents_catalog`", c2);
      cmdCreateTable.CommandType = CommandType.Text;
      cmdCreateTable.CommandTimeout = 0;
      Assert.Throws<InvalidOperationException>(() => cmdCreateTable.ExecuteNonQuery());
    }

    /// <summary>
    /// Bug #35619 creating a MySql connection from toolbox generates an error
    /// </summary>
    [Test]
    public void NullConnectionString()
    {
      MySqlConnection c = new MySqlConnection();
      c.ConnectionString = null;
    }

    /// <summary>
    /// Bug #53097  	Connection.Ping() closes connection if executed on a connection with datareader
    /// </summary>
    [Test]
    public void PingWhileReading()
    {
      using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
      {
        conn.Open();
        MySqlCommand command = new MySqlCommand("SELECT 1", conn);

        using (MySqlDataReader reader = command.ExecuteReader())
        {
          reader.Read();
          Assert.Throws<MySqlException>(() => conn.Ping());
        }
      }
    }

#if NET452
    /// <summary>
    /// Test if keepalive parameters work.
    /// </summary>
    [Test]
    public void Keepalive()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStr.Keepalive = 1;
      using (MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true)))
      {
        c.Open();
      }
    }
#endif

    #region Async

    [Test]
    public async Task TransactionAsync()
    {
      ExecuteSQL("Create Table TranAsyncTest(key2 varchar(50), name varchar(50), name2 varchar(50))");
      ExecuteSQL("INSERT INTO TranAsyncTest VALUES('P', 'Test1', 'Test2')");

      MySqlTransaction txn = await Connection.BeginTransactionAsync();
      MySqlConnection c = txn.Connection;
      Assert.AreEqual(Connection, c);
      MySqlCommand cmd = new MySqlCommand("SELECT name, name2 FROM TranAsyncTest WHERE key2='P'", Connection, txn);
      MySqlTransaction t2 = cmd.Transaction;
      Assert.AreEqual(txn, t2);
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

    [Test]
    public async Task ChangeDataBaseAsync()
    {
      string dbName = CreateDatabase("db2");
      ExecuteSQL(String.Format(
        "CREATE TABLE `{0}`.`footest` (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))", dbName), true);

      await Connection.ChangeDatabaseAsync(dbName);

      var cmd = Connection.CreateCommand();
      cmd.CommandText = "SELECT COUNT(*) FROM footest";
      var count = cmd.ExecuteScalar();
    }

    [Test]
    public async Task OpenAndCloseConnectionAsync()
    {
      var conn = new MySqlConnection(Connection.ConnectionString);
      await conn.OpenAsync();
      Assert.True(conn.State == ConnectionState.Open);
      await conn.CloseAsync();
      Assert.True(conn.State == ConnectionState.Closed);
    }

    [Test]
    public async Task ClearPoolAsync()
    {
      MySqlConnection c1 = new MySqlConnection(Connection.ConnectionString);
      MySqlConnection c2 = new MySqlConnection(Connection.ConnectionString);
      c1.Open();
      c2.Open();
      c1.Close();
      c2.Close();
      await c1.ClearPoolAsync(c1);
      await c2.ClearPoolAsync(c1);
    }

    [Test]
    public async Task ClearAllPoolsAsync()
    {
      MySqlConnection c1 = new MySqlConnection(Connection.ConnectionString);
      MySqlConnection c2 = new MySqlConnection(Connection.ConnectionString);
      c1.Open();
      c2.Open();
      c1.Close();
      c2.Close();
      await c1.ClearAllPoolsAsync();
      await c2.ClearAllPoolsAsync();
    }

    [Test]
    public async Task GetSchemaCollectionAsync()
    {
      var schemaColl = await Connection.GetSchemaCollectionAsync("MetaDataCollections", null);
      Assert.NotNull(schemaColl);
    }

    #endregion

    #region Connection Attributes/Options

    [Test]
    [Property("Category", "Security")]
    public void TestConnectingSocketBadUserName()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStr.UserID = "bad_one";
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      Assert.Throws<MySqlException>(() => c.Open());
    }

    [Test]
    [Property("Category", "Security")]
    public void TestConnectingSocketBadDbName()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStr.Password = "bad_pwd";
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      Assert.Throws<MySqlException>(() => c.Open());
    }

    [Test]
    [Property("Category", "Security")]
    public void TestPersistSecurityInfoCachingPasswords()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);

      // Persist Security Info = true means that it should be returned
      connStr.PersistSecurityInfo = true;
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();
      MySqlConnectionStringBuilder afterOpenSettings = new MySqlConnectionStringBuilder(c.ConnectionString);
      Assert.AreEqual(connStr.Password, afterOpenSettings.Password);

      // Persist Security Info = false means that it should not be returned
      connStr.PersistSecurityInfo = false;
      c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();
      afterOpenSettings = new MySqlConnectionStringBuilder(c.ConnectionString);
      Assert.True(String.IsNullOrEmpty(afterOpenSettings.Password));
    }

    /// <summary>
    /// Bug #30502718  MYSQLCONNECTION.CLONE DISCLOSES CONNECTION PASSWORD
    /// </summary>
    [Test]
    [Property("Bug", "30502718")]
    public void CloneConnectionDisclosePassword()
    {
      // Verify original connection doesn't show password before and after open connection
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStr.PersistSecurityInfo = false;
      MySqlConnection c = new MySqlConnection(connStr.ConnectionString);

      // The password, is not returned as part of the connection if the connection is open or has ever been in an open state
      StringAssert.Contains("password", c.ConnectionString);

      // After open password should not be displayed
      c.Open();
      StringAssert.DoesNotContain("password", c.ConnectionString);

      // Verify clone from open connection should not show password
      var cloneConnection = (MySqlConnection)c.Clone();
      StringAssert.DoesNotContain("password", cloneConnection.ConnectionString);

      // After close connection the password should not be displayed
      c.Close();
      StringAssert.DoesNotContain("password", c.ConnectionString);

      // Verify clone connection doesn't show password after open connection
      cloneConnection.Open();
      StringAssert.DoesNotContain("password", cloneConnection.ConnectionString);

      // Verify clone connection doesn't show password after close connection
      cloneConnection.Close();
      StringAssert.DoesNotContain("password", cloneConnection.ConnectionString);

      // Verify password for a clone of closed connection, password should appears
      var closedConnection = new MySqlConnection(connStr.ConnectionString);
      var cloneClosed = (MySqlConnection)closedConnection.Clone();
      StringAssert.Contains("password", cloneClosed.ConnectionString);

      // Open connection of a closed connection clone, password should be empty
      Assert.False(cloneClosed.hasBeenOpen);
      cloneClosed.Open();
      StringAssert.DoesNotContain("password", cloneClosed.ConnectionString);
      Assert.True(cloneClosed.hasBeenOpen);

      // Close connection of a closed connection clone, password should be empty
      cloneClosed.Close();
      StringAssert.DoesNotContain("password", cloneClosed.ConnectionString);

      // Clone Password shloud be present if PersistSecurityInfo is true
      connStr.PersistSecurityInfo = true;
      c = new MySqlConnection(connStr.ConnectionString);
      cloneConnection = (MySqlConnection)c.Clone();
      StringAssert.Contains("password", cloneConnection.ConnectionString);
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectionTimeout()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStr.Server = "10.20.30.40";
      connStr.Port = 3000;
      connStr.ConnectionTimeout = 5;
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));

      DateTime start = DateTime.Now;
      var ex = Assert.Throws<MySqlException>(() => c.Open());
      Assert.True(ex.InnerException.InnerException is TimeoutException);
      TimeSpan diff = DateTime.Now.Subtract(start);
      Assert.True(diff.TotalSeconds < 6, $"Timeout exceeded: {diff.TotalSeconds}");
    }

    [Test]
    [Ignore("Fix for 8.0.5")]
    [Property("Category", "Security")]
    public void ConnectInVariousWays()
    {
      // connect with no db
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStr.Database = null;
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();

      ExecuteSQL("GRANT ALL ON *.* to 'nopass'@'%'", true);
      ExecuteSQL($"GRANT ALL ON *.* to 'nopass'@'{Host}'", true);
      ExecuteSQL("FLUSH PRIVILEGES", true);

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

    /// <summary>
    /// Bug #10281 Clone issue with MySqlConnection
    /// Bug #27269 MySqlConnection.Clone does not mimic SqlConnection.Clone behaviour
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void TestConnectionCloneRetainsPassword()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStr.PersistSecurityInfo = false;

      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();
      MySqlConnection clone = (MySqlConnection)c.Clone();
      clone.Open();
      clone.Close();
    }

    /// <summary>
    /// Bug #13321 Persist security info does not woek
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void PersistSecurityInfo()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStr.PersistSecurityInfo = false;

      Assert.False(String.IsNullOrEmpty(connStr.Password));
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();
      connStr = new MySqlConnectionStringBuilder(c.ConnectionString);
      Assert.True(String.IsNullOrEmpty(connStr.Password));
    }

    /// <summary>
    /// Bug #31433 Username incorrectly cached for logon where case sensitive
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void CaseSensitiveUserId()
    {
      MySqlConnectionStringBuilder connStr = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      string original_uid = connStr.UserID;
      connStr.UserID = connStr.UserID.ToUpper();
      MySqlConnection c = new MySqlConnection(connStr.GetConnectionString(true));
      Assert.Throws<MySqlException>(() => c.Open());

      connStr.UserID = original_uid;
      c = new MySqlConnection(connStr.GetConnectionString(true));
      c.Open();
      c.Close();
    }

    [Test]
    [Property("Category", "Security")]
    public void CanOpenConnectionAfterAborting()
    {
      MySqlConnection connection = new MySqlConnection(Connection.ConnectionString);
      connection.Open();
      Assert.AreEqual(ConnectionState.Open, connection.State);

      connection.AbortAsync(false).GetAwaiter().GetResult();
      Assert.AreEqual(ConnectionState.Closed, connection.State);

      connection.Open();
      Assert.AreEqual(ConnectionState.Open, connection.State);

      connection.Close();
    }

    /// <summary>
    /// Test for Connect attributes feature used in MySql Server > 5.6.6
    /// (Stores client connection data on server)
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void ConnectAttributes()
    {
      if (Version < new Version(5, 6, 6)) return;
      if (!Connection.driver.SupportsConnectAttrs) return;

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM performance_schema.session_connect_attrs WHERE PROCESSLIST_ID = connection_id()", Connection);
      MySqlDataReader dr = cmd.ExecuteReader();
      Assert.True(dr.HasRows, "No session_connect_attrs found");
      MySqlConnectAttrs connectAttrs = new MySqlConnectAttrs();
      bool isValidated = false;
      using (dr)
      {
        while (dr.Read())
        {
          if (dr.GetString(1).ToLowerInvariant().Contains("_client_name"))
          {
            Assert.AreEqual(connectAttrs.ClientName, dr.GetString(2));
            isValidated = true;
            break;
          }
        }
      }
      Assert.True(isValidated, "Missing _client_name attribute");
    }

    /// <summary>
    /// Test for password expiration feature in MySql Server 5.6 or higher
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void PasswordExpiration()
    {
      if ((Version < new Version(5, 6, 6)) || (Version >= new Version(8, 0, 17))) return;

      string expiredfull = string.Format("'{0}'@'{1}'", _EXPIRED_USER, Host);

      using (MySqlConnection conn = new MySqlConnection(Settings.ToString()))
      {
        MySqlCommand cmd = new MySqlCommand("", conn);
        string expiredPwd = _EXPIRED_USER + "1";

        // creates expired user
        SetupExpiredPasswordUser(expiredPwd);

        // validates expired user
        var cnstrBuilder = new MySqlConnectionStringBuilder(Root.ConnectionString);
        cnstrBuilder.UserID = _EXPIRED_USER;
        cnstrBuilder.Password = expiredPwd;
        conn.ConnectionString = cnstrBuilder.ConnectionString;
        conn.Open();

        cmd.CommandText = "SELECT 1";
        MySqlException ex = Assert.Throws<MySqlException>(() => cmd.ExecuteScalar());
        Assert.AreEqual(1820, ex.Number);

        if (Version >= new Version(5, 7, 6))
          cmd.CommandText = string.Format("SET PASSWORD = '{0}1'", _EXPIRED_USER);
        else
          cmd.CommandText = string.Format("SET PASSWORD = PASSWORD('{0}1')", _EXPIRED_USER);

        cmd.ExecuteNonQuery();
        cmd.CommandText = "SELECT 1";
        cmd.ExecuteScalar();
        conn.Close();
        conn.ConnectionString = Root.ConnectionString;
        conn.Open();
        MySqlHelper.ExecuteNonQuery(conn, String.Format("DROP USER " + expiredfull));
        conn.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void TestNonSupportedOptions()
    {
      string connstr = Root.ConnectionString;
      connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        c.Open();
        Assert.AreEqual(ConnectionState.Open, c.State);
      }
    }

    #endregion

    [Test]
    public void IPv6Connection()
    {
      if (Version < new Version(5, 6, 0)) return;

      MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      sb.Server = GetMySqlServerIp(true);

      if (sb.Server == string.Empty) Assert.Ignore("No IPv6 available.");

      using (MySqlConnection conn = new MySqlConnection(sb.ToString()))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.State);
      }
    }

    //[InlineData("SET NAMES 'latin1'")]
    [TestCase("SELECT VERSION()")]
    [TestCase("SHOW VARIABLES LIKE '%audit%'")]
    [Property("Category", "Security")]
    public void ExpiredPassword(string sql)
    {
      if (Version < new Version(8, 0, 18))
        return;

      MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      sb.UserID = _EXPIRED_USER;
      sb.Password = _EXPIRED_USER + "1";
      SetupExpiredPasswordUser(sb.Password);
      using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.State);
        MySqlCommand cmd = new MySqlCommand(sql, conn);
        var ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
        Assert.AreEqual(1820, ex.Number);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void ExpiredPwdWithOldPassword()
    {
      if ((Version < new Version(5, 6, 6)) || (Version >= new Version(8, 0, 17))) return;

      string expiredUser = _EXPIRED_USER;
      string expiredPwd = _EXPIRED_USER + 1;
      string newPwd = "newPwd";
      string host = Settings.Server;
      uint port = Settings.Port;

      SetupExpiredPasswordUser(expiredPwd);

      var sb = new MySqlConnectionStringBuilder();
      sb.Server = host;
      sb.Port = port;
      sb.UserID = expiredUser;
      sb.Password = expiredPwd;
      using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
      {
        conn.Open();
        string password = $"'{newPwd}'";
        if (Version < new Version(5, 7, 6))
          password = $"PASSWORD({password})";

        MySqlCommand cmd = new MySqlCommand($"SET PASSWORD FOR '{expiredUser}'@'{host}' = {password}", conn);
        cmd.ExecuteNonQuery();
      }

      sb.Password = newPwd;
      using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
      {
        conn.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT 8", conn);
        StringAssert.StartsWith("8", cmd.ExecuteScalar().ToString());
      }

      sb.Password = expiredPwd;
      using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
      {
        Assert.Throws<MySqlException>(() => { conn.Open(); });
      }
    }

    /// <summary>
    /// Bug#32853205 - UNABLE TO CONNECT USING NAMED PIPES AND SHARED MEMORY
    /// To be able to connect using Named Pipes, it requires to start the server supporting the protocol
    /// mysqld --standalone --console --enable-named-pipe
    /// MySQL Server needs to be running as a Windows Service.
    /// </summary>    
    [Test]
    [Ignore("To be able to connect using Named Pipes, it requires to start the server supporting the protocol")]
    public void ConnectUsingNamedPipes()
    {
      var sb = new MySqlConnectionStringBuilder()
      {
        Server = Host,
        Pooling = false,
        UserID = RootUser,
        ConnectionProtocol = MySqlConnectionProtocol.NamedPipe,
        SslMode = MySqlSslMode.Required
      };

      // Named Pipes connection protocol is not allowed to use SSL connections.
      using var conn = new MySqlConnection(sb.ConnectionString);
      var ex = Assert.Throws<MySqlException>(() => conn.Open());
      StringAssert.AreEqualIgnoringCase(string.Format(Resources.SslNotAllowedForConnectionProtocol, sb.ConnectionProtocol), ex.Message);
    }

    /// <summary>
    /// Bug#32853205 - UNABLE TO CONNECT USING NAMED PIPES AND SHARED MEMORY
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingSharedMemory()
    {
      if (!Platform.IsWindows()) Assert.Ignore("Shared Memory is only supported on Windows.");

      var sb = new MySqlConnectionStringBuilder()
      {
        Server = Host,
        Pooling = false,
        UserID = RootUser,
        ConnectionProtocol = MySqlConnectionProtocol.SharedMemory,
        SharedMemoryName = "MySQLSocket"
      };

      using (var conn = new MySqlConnection(sb.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.State);
      }

      // Shared Memory connection protocol is not allowed to use SSL connections.
      sb.SslMode = MySqlSslMode.Required;
      using (var conn = new MySqlConnection(sb.ConnectionString))
      {
        var ex = Assert.Throws<MySqlException>(() => conn.Open());
        StringAssert.AreEqualIgnoringCase(string.Format(Resources.SslNotAllowedForConnectionProtocol, sb.ConnectionProtocol), ex.Message);
      }
    }

#if NET452
    /// <summary>
    ///  Fix for aborted connections MySQL bug 80997 OraBug 23346197
    /// </summary>
    [Test]
    public void MarkConnectionAsClosedProperlyWhenDisposing()
    {
      MySqlConnection con = new MySqlConnection(Connection.ConnectionString);
      con.Open();
      var cmd = new MySqlCommand("show global status like 'aborted_clients'", con);
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
        MySqlConnection connection = sandbox.TryOpenConnection(Connection.ConnectionString);
        Assert.NotNull(connection);
        Assert.True(connection.State == ConnectionState.Open);
      }
      finally
      {
        AppDomain.Unload(appDomain);
      }

      r = cmd.ExecuteReader();
      r.Read();
      int numClientsAborted2 = r.GetInt32(1);
      r.Close();
      Assert.AreEqual(numClientsAborted, numClientsAborted);
      con.Close();
    }
#endif

    /*
    [Test]
    public void AnonymousLogin()
    {
      suExecSQL(String.Format("GRANT ALL ON *.* to ''@'{0}' IDENTIFIED BY 'set_to_blank'", host));
      suExecSQL("UPDATE mysql.user SET password='' WHERE password='set_to_blank'");

      MySqlConnection c = new MySqlConnection(String.Empty);
      c.Open();
      c.Close();
    }
    */

    //    /// <summary>
    //    /// Bug #30964 StateChange imperfection
    //    /// </summary>
    //    MySqlConnection rqConnection;


    //    [Test]
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
    //        Assert.AreEqual(1, Convert.ToInt32(o));
    //      }
    //    }

    //    [Test]
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

    ///// <summary>
    ///// Fix for bug http://bugs.mysql.com/bug.php?id=63942 (Connections not closed properly when using pooling)
    ///// </summary>
    //[Test]
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
    //    Assert.AreEqual(numClientsAborted, numClientsAborted2);
    //    con.Close();
    //}

    class ConnectionClosedCheck
    {
      public bool closed = false;
      public void stateChangeHandler(object sender, StateChangeEventArgs e)
      {
        if (e.CurrentState == ConnectionState.Closed)
          closed = true;
      }
    }

    /// <summary>
    /// Bug #33380176	Malformed communication packet while using MEDIUMTEXT
    /// </summary>
    [Test]
    public void MediumTextMalformedPkg()
    {
      ExecuteSQL("SET GLOBAL max_allowed_packet=25165824");
      ExecuteSQL($"CREATE TABLE `{Settings.Database}`.`testmalformed` (caseref VARCHAR(12) NOT NULL, fieldId INT NOT NULL, " +
        "fieldtext MEDIUMTEXT, PRIMARY KEY (caseref, fieldId))");

      int rowMax = 40000; //Maximum allowed for prepared statement 21846
      var query = $"INSERT INTO `{Settings.Database}`.`testmalformed` (caseref, fieldId, fieldtext) VALUES {{0}} ON DUPLICATE KEY UPDATE caseref = caseref;";

      List<MySqlParameter> mySqlParameters = new();
      StringBuilder sb = new();
      string[] fieldValues = {
            "0010EF7V002",
            "1",
            "Text, text, and more text."
        };

      for (int i = 0; i < rowMax; i++)
      {
        mySqlParameters.Add(new MySqlParameter(i + "caseref", fieldValues[0]));
        mySqlParameters.Add(new MySqlParameter(i + "fieldid_1", int.Parse(fieldValues[1]) + i));
        mySqlParameters.Add(new MySqlParameter(i + "fieldtext_1", fieldValues[2]));
        sb.AppendFormat("(@{0}caseref, @{0}fieldid_1, @{0}fieldtext_1), ", i);
      }

      string fullQuery = string.Format(query, sb.ToString(0, sb.Length - 2));
      var res = MySqlHelper.ExecuteNonQuery(Connection.ConnectionString + ";ssl-mode=none", fullQuery, mySqlParameters.ToArray());
      Assert.AreEqual(res, 40000);

      ExecuteSQL("SET GLOBAL max_allowed_packet=1024000");
      ExecuteSQL($"DROP TABLE `{Settings.Database}`.`testmalformed`;");
    }

    [Test, Description("Verify Compression in classic protocol where default connection string is used without any option")]
    public void CompressionUnit()
    {
      if (Version < new Version(8, 0, 0)) Assert.Ignore("This test is for MySql 8.0 or higher.");
      using (var dbConn = new MySqlConnection(Connection.ConnectionString + ";UseCompression=True"))
      {
        var cmd = new MySqlCommand();
        dbConn.Open();
        cmd.Connection = dbConn;
        cmd.CommandText = "select * from performance_schema.session_status where variable_name like '%COMPRESSION%' order by 1";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          string[] compressionValues = new string[] { "Compression", "Compression_algorithm", "Compression_level" };
          int i = 0;
          if (reader.HasRows)
          {
            while (reader.Read())
            {
              if (i == 0)
              {
                Assert.AreEqual("ON", reader.GetString(1));
              }
              Assert.IsNotNull(reader.GetString(1));
              i++;
            }
          }
        }

        cmd.CommandText = "select @@protocol_compression_algorithms";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          string[] compressionValues = new string[] { "@@protocol_compression_algorithms" };
          int i = 0;
          if (reader.HasRows)
          {
            while (reader.Read())
            {
              Assert.IsNotNull(reader.GetString(0));
              i++;
            }
          }
        }
        dbConn.Close();
      }

      using (var dbConn = new MySqlConnection(Connection.ConnectionString + ";UseCompression=False"))
      {
        dbConn.Open();
        var cmd = new MySqlCommand();
        cmd.Connection = dbConn;
        cmd.CommandText = "select * from performance_schema.session_status where variable_name like '%COMPRESSION%' order by 1";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          string[] compressionValues = new string[] { "Compression", "Compression_algorithm", "Compression_level" };
          int i = 0;
          if (reader.HasRows)
          {
            while (reader.Read())
            {
              if (i == 0)
              {
                Assert.AreEqual("OFF", reader.GetString(1));
              }
              Assert.IsNotNull(reader.GetString(1));
              i++;
            }
          }
        }

        cmd.Connection = dbConn;
        cmd.CommandText = "select @@protocol_compression_algorithms";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          string[] compressionValues = new string[] { "@@protocol_compression_algorithms" };
          int i = 0;
          if (reader.HasRows)
          {
            while (reader.Read())
            {
              Assert.IsNotNull(reader.GetString(0));
              i++;
            }
          }
        }
        dbConn.Close();
      }
    }

    [Test, Description("Verify Compression in classic protocol where default connection string is used without any option")]
    public void CompressionValidationInClassicProtocol()
    {
      if (Version < new Version(8, 0, 0)) Assert.Ignore("This test is for MySql 8.0 or higher.");
      string[] compressionAlgorithms = new string[] { "zlib", "zstd", "uncompressed", "uncompressed,zlib", "uncompressed,zstd", "zstd,zlib", "zstd,zlib,uncompressed" };
      for (int k = 0; k < compressionAlgorithms.Length; k++)
      {
        ExecuteSQL($"SET GLOBAL protocol_compression_algorithms = \"{compressionAlgorithms[k]}\"");
        using (var dbConn = new MySqlConnection(Connection.ConnectionString + ";UseCompression=True"))
        {
          if (k == 1)
          {
            Assert.Throws<MySqlException>(() => dbConn.Open());
            continue;
          }

          dbConn.Open();
          Assert.AreEqual(ConnectionState.Open, dbConn.State);
          MySqlCommand cmd = new MySqlCommand();
          cmd.Connection = dbConn;
          cmd.CommandText = "select * from performance_schema.session_status where variable_name like 'COMPRESSION%' order by 1";
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            int i = 0;
            if (reader.HasRows)
            {
              while (reader.Read())
              {
                if (i == 0)
                {
                  if (k == 2 || k == 4)
                  {
                    // Compression should have been set to OFF as UseCompression is set to True in connection string but server is 
                    // started with uncompressed and uncompressed/zstd
                    Assert.AreEqual("OFF", reader.GetString(1));
                  }
                  else
                  {
                    Assert.AreEqual("ON", reader.GetString(1));
                  }
                }
                Assert.IsNotNull(reader.GetString(1));
                i++;
              }
            }
          }

          cmd.CommandText = "select @@protocol_compression_algorithms";
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            string[] compressionValues = new string[] { "@@protocol_compression_algorithms" };
            int i = 0;
            if (reader.HasRows)
            {
              while (reader.Read())
              {
                Assert.AreEqual(compressionAlgorithms[k], reader.GetString(0));
                i++;
              }
            }
          }

          dbConn.Close();
          using (MySqlConnection c = new MySqlConnection(dbConn.ConnectionString + ";max pool size = 1"))
          {
            c.Open();
            ParameterizedThreadStart pts = new ParameterizedThreadStart(PoolingWorker);
            Thread t = new Thread(pts);
            t.Start(c);
          }

          using (MySqlConnection c2 = new MySqlConnection(dbConn.ConnectionString + ";max pool size = 1"))
          {
            c2.Open();
            KillConnection(c2, true);
          }
        }

        using (var dbConn = new MySqlConnection(Connection.ConnectionString + ";UseCompression=false"))
        {
          if (k == 0 || k == 1 || k == 5)
          {
            Assert.Throws<MySqlException>(() => dbConn.Open());
            continue;
          }

          dbConn.Open();
          Assert.AreEqual(ConnectionState.Open, dbConn.State);
          MySqlCommand cmd = new MySqlCommand();
          cmd.Connection = dbConn;
          cmd.CommandText = "select * from performance_schema.session_status where variable_name like 'COMPRESSION%' order by 1";
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            int i = 0;
            if (reader.HasRows)
            {
              while (reader.Read())
              {
                if (i == 0)
                {
                  Assert.AreEqual("OFF", reader.GetString(1));
                }
                Assert.IsNotNull(reader.GetString(1));
                i++;
              }
            }
          }

          cmd.CommandText = "select @@protocol_compression_algorithms";
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            string[] compressionValues = new string[] { "@@protocol_compression_algorithms" };
            int i = 0;
            if (reader.HasRows)
            {
              while (reader.Read())
              {
                Assert.AreEqual(compressionAlgorithms[k], reader.GetString(0));
                i++;
              }
            }
          }

          dbConn.Close();
          using (MySqlConnection c = new MySqlConnection(dbConn.ConnectionString + ";max pool size = 1"))
          {
            c.Open();
            ParameterizedThreadStart pts = new ParameterizedThreadStart(PoolingWorker);
            Thread t = new Thread(pts);
            t.Start(c);
          }

          using (MySqlConnection c2 = new MySqlConnection(dbConn.ConnectionString + ";max pool size = 1"))
          {
            c2.Open();
            KillConnection(c2);
          }
        }
      }
      ExecuteSQL($"SET GLOBAL protocol_compression_algorithms = \"zlib,zstd,uncompressed\"");
    }

    [Test, Description("Test MySql Password Expiration with blank password")]
    public void ExpiredBlankPassword()
    {
      if (Version < new Version(8, 0, 0)) Assert.Ignore("This test is for MySql 8.0 or higher.");

      string host = Host == "localhost" ? Host : "%";
      MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      sb.UserID = _EXPIRED_USER;
      string[] pwds = new string[] { _EXPIRED_USER + "1", "" };

      for (int i = 0; i < pwds.Length; i++)
      {
        //wrong password
        sb.Password = "wrongpassword";
        using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
        {
          Assert.Throws<MySqlException>(() => conn.Open());
        }

        sb.Password = pwds[i];
        SetupExpirePasswordExecuteQueriesFail("SELECT VERSION()", pwds[i]);
        SetupExpirePasswordExecuteQueriesFail("SHOW VARIABLES LIKE '%audit%'", pwds[i]);
        SetupExpirePasswordExecuteQueriesFail($"USE `{sb.Database}`", pwds[i]);
        ExecuteQueriesFail("SELECT VERSION()", _EXPIRED_USER, pwds[i]);
        ExecuteQueriesFail("SHOW VARIABLES LIKE '%audit%'", _EXPIRED_USER, pwds[i]);
        ExecuteQueriesFail("select 1", _EXPIRED_USER, pwds[i]);

        //reactivate user
        ExecuteSQL($"ALTER USER '{_EXPIRED_USER}'@'{host}' Identified BY '{sb.Password}'");
        ExecuteQueriesSuccess("SELECT VERSION()", sb.Password);
        ExecuteQueriesSuccess("SHOW VARIABLES LIKE '%audit%'", sb.Password);
        ExecuteQueriesSuccess("select 1", sb.Password);
      }
    }

    [Test, Description("Test MySql Password Expiration with query variables")]
    public void ExpiredPasswordBug2()
    {
      if (Version < new Version(8, 0, 0)) Assert.Ignore("This test is for MySql 8.0 or higher.");
      var _expiredPwd = "expiredPwd";
      SetupExpiredPasswordUser(_expiredPwd);

      var sb = new MySqlConnectionStringBuilder(Root.ConnectionString);
      sb.UserID = _EXPIRED_USER;
      sb.Password = _expiredPwd;
      sb.AllowUserVariables = true;
      using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
      {
        conn.Open();
        MySqlCommand cmd = new MySqlCommand("SHOW VARIABLES LIKE '%audit%'", conn);
        Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());

        cmd = new MySqlCommand("SELECT VERSION();", conn);
        Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());

        cmd = new MySqlCommand("select @data := 3, @data * 4;", conn);
        Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
      }
    }

    [Test, Description("Test MySql Password Expiration with IsPasswordExpired validation")]
    public void ExpiredPasswordBug3()
    {
      if (Version < new Version(8, 0, 0)) Assert.Ignore("This test is for MySql 8.0 or higher.");
      string host = Host == "localhost" ? Host : "%";

      var _expiredPwd = "expiredPwd";
      var _newPwd = "newPwd";
      var expiredFull = $"'{_EXPIRED_USER}'@'{host}'";
      var testStr = "show create user " + expiredFull;

      SetupExpiredPasswordUser(_expiredPwd);

      var sb = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      sb.UserID = _EXPIRED_USER;
      sb.Password = _expiredPwd;
      using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
      {
        conn.Open();
        Assert.True(conn.IsPasswordExpired);
      }

      ExecuteSQL($"ALTER USER '{_EXPIRED_USER}'@'{host}' Identified BY '{_newPwd}'");

      sb = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      sb.UserID = _EXPIRED_USER;
      sb.Password = _newPwd;
      using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
      {
        conn.Open();
        MySqlCommand cmd = new MySqlCommand("select version();", conn);
        cmd.ExecuteNonQuery();

        cmd = new MySqlCommand(testStr, conn);
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
            Assert.IsNotNull(rdr[0].ToString());
        }
        Assert.False(conn.IsPasswordExpired);
      }
    }

    [Test, Description("Test MySql Password Expiration and set password")]
    public void ExpiredPasswordBug4()
    {
      if (Version < new Version(8, 0, 0)) Assert.Ignore("This test is for MySql 8.0 or higher.");
      string host = Host == "localhost" ? Host : "%";

      var _expiredPwd = "expiredPwd";
      var _newPwd = "newPwd";
      var expiredFull = $"'{_EXPIRED_USER}'@'{host}'";
      var testStr = "show create user " + expiredFull;
      SetupExpiredPasswordUser(_expiredPwd);

      var sb = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      sb.UserID = _EXPIRED_USER;
      sb.Password = _expiredPwd;
      using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
      {
        conn.Open();
        Assert.True(conn.IsPasswordExpired);
      }

      ExecuteSQL($"set password for {expiredFull}='{_newPwd}'");

      for (int i = 0; i < 50; i++)
      {
        sb = new MySqlConnectionStringBuilder(Settings.ConnectionString);
        sb.UserID = _EXPIRED_USER;
        sb.Password = _newPwd;
        using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
        {
          conn.Open();
          MySqlCommand cmd = new MySqlCommand("select version();", conn);
          cmd.ExecuteNonQuery();
          cmd = new MySqlCommand(testStr, conn);
          using (var rdr = cmd.ExecuteReader())
          {
            while (rdr.Read())
              Assert.IsNotNull(rdr[0].ToString());
          }
          Assert.False(conn.IsPasswordExpired);
        }

        sb.UserID = _EXPIRED_USER;
        sb.Password = _expiredPwd;
        using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
        {
          Assert.Throws<MySqlException>(() => conn.Open());
        }
      }
    }

    [Test, Description("Test connection paramter connection in classic")]
    public void InvalidConnectTimeoutParameter()
    {
      var connStr = $"server={Settings.Server};user={Settings.UserID};port={Settings.Port};password={Settings.Password};connect-timeout=10000";
      Assert.Throws<ArgumentException>(() => new MySqlConnection(connStr));

      connStr = $"server={Settings.Server};user={Settings.UserID};port={Settings.Port};password={Settings.Password};connectiontimeout=10000";
      using (var conn = new MySqlConnection(connStr))
      {
        Assert.IsInstanceOf<MySqlConnection>(conn);
      }
    }

    [Test, Description("MySQL Dispose verification without calling close")]
    public void ConnectionDispose()
    {
      MySqlConnection conn = new MySqlConnection(Settings.ConnectionString);
      conn.Open();
      Assert.AreEqual(ConnectionState.Open, conn.connectionState);
      conn.Dispose();
      Assert.AreEqual(ConnectionState.Closed, conn.connectionState);
    }

    [Test]
    public void ConnectionPasswordException()
    {
      int code = 0;
      var myConnectionString = $"server={Host};user={Settings.UserID};port={Port};password={Settings.Password}";
      using (var conn = new MySqlConnection(myConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.State);
      }

      myConnectionString = $"server={Host};user={Settings.UserID};port={Port};password=wrong";
      using (var conn = new MySqlConnection(myConnectionString))
      {
        var ex = Assert.Throws<MySqlException>(() => conn.Open());
        code = ((MySqlException)ex.GetBaseException()).Number;
        Assert.AreEqual(1045, code);
      }
    }

    /// <summary>
    /// Bug #33781447	[CancellationToken doesn't cancel MySqlConnection.OpenAsync]
    /// This is a regression from 8.0.27 introduced by the fix of Bug #28662512.
    /// </summary>
    [Test]
    public void OpenAsyncNotCancellingOperation()
    {
      using var conn = new MySqlConnection(Connection.ConnectionString);
      using var cts = new CancellationTokenSource();
      cts.Cancel();

      Assert.ThrowsAsync<OperationCanceledException>(async () => await conn.OpenAsync(cts.Token));
    }


    /// <summary>
    /// Bug # 35307501 [Opening two MySqlConnections simultaneously can crash]
    /// </summary>
    [Test]
    public void OpenMultipleConnectionsOnMultipleThreads()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                using (var connection = new MySqlConnection(Connection.ConnectionString))
                {
                    connection.Open();
                }
            }));
        }
        Task.WaitAll(tasks.ToArray());
    }

            #region Methods

            private void ExecuteQueriesSuccess(string sql, string password)
    {
      if (Version < new Version(8, 0, 17)) return;
      var sb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      sb.UserID = _EXPIRED_USER;
      sb.Password = password;
      using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.State);
        MySqlCommand cmd = new MySqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
      }
    }

    private void ExecuteQueriesFail(string sql, string user, string password)
    {
      if (Version < new Version(8, 0, 17)) return;
      var sb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      sb.UserID = user;
      sb.Password = password;
      using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.State);
        MySqlCommand cmd = new MySqlCommand(sql, conn);
        Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
      }
    }

    private void SetupExpirePasswordExecuteQueriesFail(string sql, string password)
    {
      if (Version < new Version(8, 0, 17)) return;
      var sb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      sb.UserID = _EXPIRED_USER;
      sb.Password = password;
      SetupExpiredPasswordUser(password);
      using (MySqlConnection conn = new MySqlConnection(sb.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.State);
        MySqlCommand cmd = new MySqlCommand(sql, conn);
        Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
      }
    }

    private void SetupExpiredPasswordUser(string password)
    {
      string host = Host == "localhost" ? Host : "%";
      string expiredFull = $"'{_EXPIRED_USER}'@'{host}'";

      using (MySqlConnection conn = new MySqlConnection(Root.ConnectionString))
      {
        conn.Open();
        MySqlCommand cmd = conn.CreateCommand();

        // creates expired user
        cmd.CommandText = $"SELECT COUNT(*) FROM mysql.user WHERE user='{_EXPIRED_USER}'";
        long count = (long)cmd.ExecuteScalar();

        if (count > 0)
          MySqlHelper.ExecuteNonQuery(conn, $"DROP USER {expiredFull}");

        MySqlHelper.ExecuteNonQuery(conn, $"CREATE USER {expiredFull} IDENTIFIED BY '{password}'");
        MySqlHelper.ExecuteNonQuery(conn, $"GRANT ALL ON `{Settings.Database}`.* TO {expiredFull}");
        MySqlHelper.ExecuteNonQuery(conn, $"ALTER USER {expiredFull} PASSWORD EXPIRE");
      }
    }
    private void PoolingWorker(object cn)
    {
      MySqlConnection conn = (cn as MySqlConnection);

      Thread.Sleep(5000);
      conn.Close();
    }

    #endregion
  }
}
