// Copyright (c) 2013, 2022, Oracle and/or its affiliates.
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
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;

namespace MySql.Data.MySqlClient.Tests
{
  public class PoolingTests : TestBase
  {
    internal override void AdjustConnectionSettings(MySqlConnectionStringBuilder settings)
    {
      settings.Pooling = true;
    }

    [Test]
    public void BasicConnection()
    {

      MySqlConnection c = new MySqlConnection(Settings.ConnectionString);
      c.Open();
      int serverThread = c.ServerThread;
      c.Close();

      // first test that only a single connection get's used
      for (int i = 0; i < 10; i++)
      {
        c = new MySqlConnection(Settings.ConnectionString);
        c.Open();
        Assert.AreEqual(serverThread, c.ServerThread);
        c.Close();
      }

      c.Open();
      KillConnection(c);
      c.Close();

      string poolingCS = Settings.ConnectionString + ";Min Pool Size=10";
      MySqlConnection[] connArray = new MySqlConnection[10];
      for (int i = 0; i < connArray.Length; i++)
      {
        connArray[i] = new MySqlConnection(poolingCS);
        connArray[i].Open();
      }

      // now make sure all the server ids are different
      for (int i = 0; i < connArray.Length; i++)
      {
        for (int j = 0; j < connArray.Length; j++)
        {
          if (i != j)
            Assert.True(connArray[i].ServerThread != connArray[j].ServerThread);
        }
      }

      for (int i = 0; i < connArray.Length; i++)
      {
        KillConnection(connArray[i]);
        connArray[i].Close();
      }
    }

    [Test]
    public void OpenKilled()
    {
      string connStr = Settings.ConnectionString + ";min pool size=1; max pool size=1";
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      int threadId = c.ServerThread;
      // thread gets killed right here
      KillConnection(c);
      c.Close();

      c.Dispose();

      c = new MySqlConnection(connStr);
      c.Open();
      int secondThreadId = c.ServerThread;
      KillConnection(c);
      c.Close();
      Assert.False(threadId == secondThreadId);
    }

    [Test]
    public void ReclaimBrokenConnection()
    {
      // now create a new connection string only allowing 1 connection in the pool
      string connStr = Settings.ConnectionString + ";connectiontimeout=5;max pool size=1";

      // now use up that connection
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();

      // now attempting to open a connection should fail
      MySqlConnection c2 = new MySqlConnection(connStr);
      Exception ex = Assert.Throws<MySqlException>(() => c2.Open());
      StringAssert.Contains("error connecting: Timeout expired.  The timeout period elapsed prior to obtaining a connection from the pool.", ex.Message);

      // we now kill the first connection to simulate a server stoppage
      KillConnection(c);

      // now we do something on the first connection

      ex = Assert.Throws<InvalidOperationException>(() => c.ChangeDatabase("mysql"));
      StringAssert.Contains("The connection is not open.", ex.Message);

      // Opening a connection now should work
      MySqlConnection connection = new MySqlConnection(connStr);
      connection.Open();
      KillConnection(connection);
      connection.Close();
    }

    [Test]
    public void TestUserReset()
    {
      string connStr = Settings.ConnectionString + ";connection reset=true;";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("SET @testvar='5'", c);
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT @testvar";
        object var = cmd.ExecuteScalar();
        Assert.AreEqual("5", var);
        c.Close();

        c.Open();
        object var2 = cmd.ExecuteScalar();
        Assert.AreEqual(DBNull.Value, var2);
        KillConnection(c);
      }
    }

    // Test that thread does not come to pool after abort
    [Test]
    public void TestAbort()
    {
      string connStr = Settings.ConnectionString;
      int threadId;

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        threadId = c.ServerThread;
        c.AbortAsync(false).GetAwaiter().GetResult();
      }

      using (MySqlConnection c1 = new MySqlConnection(connStr))
      {
        c1.Open();
        Assert.True(c1.ServerThread != threadId);
        KillConnection(c1);
      }
    }

    /// <summary>
    /// Bug #25614 After connection is closed, and opened again UTF-8 characters are not read well 
    /// </summary>
    [Test]
    public void UTF8AfterClosing()
    {
      string originalValue = "??????????";
      ExecuteSQL("DROP TABLE IF EXISTS test");


      ExecuteSQL("CREATE TABLE test (id int(11) NOT NULL, " +
        "value varchar(100) NOT NULL, PRIMARY KEY  (`id`) " +
        ") ENGINE=MyISAM DEFAULT CHARSET=utf8");

      string connStr = Settings.ConnectionString + ";charset=utf8";
      using (MySqlConnection con = new MySqlConnection(connStr))
      {
        con.Open();

        MySqlCommand cmd = new MySqlCommand("INSERT INTO test VALUES (1, '??????????')", con);
        cmd.ExecuteNonQuery();

        cmd = new MySqlCommand("SELECT value FROM test WHERE id = 1", con);
        string firstS = cmd.ExecuteScalar().ToString();
        Assert.AreEqual(originalValue, firstS);

        con.Close();
        con.Open();

        //Does not work:
        cmd = new MySqlCommand("SELECT value FROM test WHERE id = 1", con);
        string secondS = cmd.ExecuteScalar().ToString();

        KillConnection(con);
        con.Close();
        Assert.AreEqual(firstS, secondS);
      }
    }

    private void PoolingWorker(object cn)
    {
      MySqlConnection conn = (cn as MySqlConnection);

      Thread.Sleep(5000);
      conn.Close();
    }

    /// <summary>
    /// Bug #24373 High CPU utilization when no idle connection 
    /// </summary>
    [Test]
    public void MultipleThreads()
    {
      string connStr = Settings.ConnectionString + ";max pool size=1";
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();

      ParameterizedThreadStart pts = new ParameterizedThreadStart(PoolingWorker);
      Thread t = new Thread(pts);
      t.Start(c);

      using (MySqlConnection c2 = new MySqlConnection(connStr))
      {
        c2.Open();
        KillConnection(c2);
      }
      c.Close();
    }

    [Test]
    public void NewTest()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (id INT, name VARCHAR(50))");
      ExecuteSQL("CREATE PROCEDURE spTest(theid INT) BEGIN SELECT * FROM Test WHERE id=theid; END");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'First')");
      ExecuteSQL("INSERT INTO Test VALUES (2, 'Second')");
      ExecuteSQL("INSERT INTO Test VALUES (3, 'Third')");
      ExecuteSQL("INSERT INTO Test VALUES (4, 'Fourth')");

      string connStr = Settings.ConnectionString;

      for (int i = 1; i < 5; i++)
      {
        using (MySqlConnection con = new MySqlConnection(connStr))
        {
          con.Open();
          MySqlCommand reccmd = new MySqlCommand("spTest", con);
          reccmd.CommandTimeout = 0;
          reccmd.CommandType = CommandType.StoredProcedure;
          MySqlParameter par = new MySqlParameter("@theid", MySqlDbType.String);
          par.Value = i;
          reccmd.Parameters.Add(par);
          using (MySqlDataReader recdr = reccmd.ExecuteReader())
          {
            if (recdr.Read())
            {
              int x = recdr.GetOrdinal("name");
              Assert.AreEqual(1, x);
            }
          }
        }
      }
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      KillConnection(c);
    }

    bool IsConnectionAlive(int serverThread)
    {
      MySqlCommand cmd = new MySqlCommand("SHOW PROCESSLIST", Connection);
      using (MySqlDataReader dr = cmd.ExecuteReader())
      {
        while (dr.Read())
        {
          if (dr.GetInt64("id") == serverThread)
            return true;
        }
      }
      return false;
    }

#if CLR4
    [Test] 
    public void CleanIdleConnections()
    {
      string assemblyName = typeof(MySqlConnection).Assembly.FullName;
      string pmName = String.Format("MySql.Data.MySqlClient.MySqlPoolManager, {0}", assemblyName);

      Type poolManager = Type.GetType(pmName, false);
      FieldInfo poolManagerTimerField = poolManager.GetField("timer",
        BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
      FieldInfo poolManagerMaxConnectionIdleTime =
        poolManager.GetField("maxConnectionIdleTime",
        BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

      System.Threading.Timer poolManagerTimer = (System.Threading.Timer)poolManagerTimerField.GetValue(null);
      int origMaxConnectionIdleTime = (int)poolManagerMaxConnectionIdleTime.GetValue(null);


      try
      {
        // Normally, idle connection would expire after 3 minutes and would
        // be cleaned up by timer that also runs every 3 minutes.
        // Since we do not want to wait that long during a unit tests,
        // we use tricks.
        // - temporarily  reduce max.idle time for connections down to 1
        // second
        // - temporarily change cleanup timer to run each second.
        isConnectionAlive = true;
        threadId = -1;
        string connStr = st.GetPoolingConnectionString();
        using (MySqlConnection c = new MySqlConnection(connStr))
        {
          c.Open();          
          callbacksCount = 0;
          threadId = c.ServerThread;
        }

        // Pooled connection should be still alive
        Assert.True(IsConnectionAlive(threadId));
        
        poolManagerMaxConnectionIdleTime.SetValue(null, 3);

        int testIdleTime = (int)poolManagerMaxConnectionIdleTime.GetValue(null);        

        poolManagerTimer.Change((testIdleTime * 1000) + 1000, (testIdleTime * 1000));

        
        //create a second timer to check just right after the first interval is completed        
        timer = new System.Timers.Timer((testIdleTime * 1000) + 1500);

        timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
        timer.Enabled = true; 
        
        // Let the idle connection expire and let cleanup timer run.
        Thread.Sleep((testIdleTime * 1000) + 2000);

        // The removed of the iddle connections should be done in the first callback
        Assert.True(callbacksCount == 1, "Callbacks value was not 1"); 

        //Check the connection was removed
        Assert.False(isConnectionAlive, "IsConnectionAlive failed");
                
      }
      finally
      {
        // restore values for connection idle time and timer interval
        poolManagerMaxConnectionIdleTime.SetValue(null, origMaxConnectionIdleTime);
        poolManagerTimer.Change(origMaxConnectionIdleTime * 1000,
          origMaxConnectionIdleTime * 1000);
        
        timer = null;
      }
    }
#endif

    //[Test]
    //public void ClearPool()
    //{
    //  // Start by clearing clearingPools for a clean test
    //  List<MySqlPool> clearingPools = GetClearingPools();
    //  clearingPools.Clear();

    //  string connStr = st.GetPoolingConnectionString() + ";min pool size=10";
    //  MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(connStr);

    //  MySqlConnection[] connections = new MySqlConnection[10];
    //  connections[0] = new MySqlConnection(connStr);
    //  connections[0].Open();

    //  Type poolManagerType = typeof(MySqlPoolManager);
    //  FieldInfo poolManagerHashTable = poolManagerType.GetField("pools",
    //    BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
    //  Hashtable poolHash = (Hashtable)poolManagerHashTable.GetValue(null);

    //  // now we need to investigate
    //  Type poolType = typeof(MySqlPool);

    //  FieldInfo idlePool = poolType.GetField("idlePool", BindingFlags.NonPublic | BindingFlags.Instance);
    //  ICollection idleList = (ICollection)idlePool.GetValue(poolHash[settings.ConnectionString]);
    //  Debug.Print("Clear Pool test connection string 2 " + settings.ConnectionString);
    //  Assert.AreEqual(9, idleList.Count);      

    //  FieldInfo inUsePool = poolType.GetField("inUsePool", BindingFlags.NonPublic | BindingFlags.Instance);
    //  ICollection inUseList = (ICollection)inUsePool.GetValue(poolHash[settings.ConnectionString]);
    //  Assert.AreEqual(1, inUseList.Count);

    //  // now open 4 more of these.  Now we shoudl have 5 open and five
    //  // still in the pool
    //  for (int i = 1; i < 5; i++)
    //  {
    //    connections[i] = new MySqlConnection(connStr);
    //    connections[i].Open();
    //  }

    //  Assert.AreEqual(5, inUseList.Count);
    //  Assert.AreEqual(5, idleList.Count);

    //  clearingPools = GetClearingPools();
    //  Assert.AreEqual(0, clearingPools.Count);

    //  // now tell this connection to clear its pool
    //  MySqlConnection.ClearPool(connections[0]);
    //  Assert.AreEqual(1, clearingPools.Count);
    //  Assert.AreEqual(0, idleList.Count);

    //  for (int i = 0; i < 5; i++)
    //    connections[i].Close();
    //  Assert.AreEqual(0, clearingPools.Count);
    //}

    private static List<MySqlPool> GetClearingPools()
    {
      Type poolManagerType = typeof(MySqlPoolManager);
      FieldInfo clearingPoolsFI = poolManagerType.GetField("clearingPools",
        BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
      return clearingPoolsFI.GetValue(null) as List<MySqlPool>;
    }

    [Test]
    public void TestBadConnections()
    {
      MySqlConnectionStringBuilder builder = new
      MySqlConnectionStringBuilder();

      builder.Pooling = true;
      builder.Server = "xxxxxxxx"; // one that definitely does not exist.
      builder.UserID = "whoever";
      builder.Password = "whatever";

      int numberOfConnections = 1;

      for (int i = 0; i < numberOfConnections; ++i)
      {
        using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
        {
          Exception ex = Assert.Throws<MySqlException>(() => connection.Open());
          if (Platform.IsWindows()) Assert.True(ex.InnerException.GetType() == typeof(ArgumentException));
        }
        Thread.Sleep(50);
      }
      MySqlConnection.ClearAllPools();
    }

    /// <summary>
    /// Bug #42801  	ClearPool .Net connector : NullReferenceException
    /// </summary>
    [Test]
    public void DoubleClearingConnectionPool()
    {
      MySqlConnection c1 = GetConnection();
      MySqlConnection c2 = GetConnection();
      c1.Close();
      c2.Close();
      MySqlConnection.ClearPool(c1);
      MySqlConnection.ClearPool(c2);
    }

    /// <summary>
    /// Bug #49563  	Mysql Client wrongly communications with server when using pooled connections
    /// </summary>
    [Test]
    public void OpenSecondPooledConnectionWithoutDatabase()
    {
      string connectionString = Settings.ConnectionString;

      using (MySqlConnection c1 = new MySqlConnection(connectionString))
      {
        c1.Open();
        c1.Close();
      }
      using (MySqlConnection c2 = new MySqlConnection(connectionString))
      {
        c2.Open();
        c2.Close();
      }
    }

    /// <summary>
    /// Bug #47153	Connector/NET fails to reset connection when encoding has changed
    /// </summary>
    [Test]
    public void ConnectionResetAfterUnicode()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test");
      ExecuteSQL("CREATE TABLE test (id INT, name VARCHAR(20) CHARSET UCS2)");
      ExecuteSQL("INSERT INTO test VALUES (1, 'test')");

      string connStr = Settings.ConnectionString + ";connection reset=true;min pool size=1; max pool size=1";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM test", c);
        using (MySqlDataReader r = cmd.ExecuteReader())
        {
          r.Read();
        }
        c.Close();

        try
        {
          c.Open();
        }
        finally
        {
          KillConnection(c);
        }
      }
    }

    private void CacheServerPropertiesInternal(bool cache)
    {
      string connStr = Settings.ConnectionString +
        String.Format(";logging=true;cache server properties={0}", cache);

      GenericListener listener = new GenericListener();
      MySqlTrace.Listeners.Add(listener);
      MySqlTrace.Switch.Level = System.Diagnostics.SourceLevels.All;

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        using (MySqlConnection c2 = new MySqlConnection(connStr))
        {
          c2.Open();
          KillConnection(c2);
        }
        KillConnection(c);
      }
      int count = listener.CountLinesContaining("SHOW VARIABLES");
      Assert.AreEqual(cache ? 1 : 2, count);
    }

    [Test]
    public void CacheServerProperties()
    {
      //CacheServerPropertiesInternal(true);
      //CacheServerPropertiesInternal(false);
    }

    /// <summary>
    /// Bug #66578
    /// CacheServerProperties can cause 'Packet too large' error
    /// when query exceeds 1024 bytes
    /// </summary>
    [Test]
    public void CacheServerPropertiesCausePacketTooLarge()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test");
      ExecuteSQL("CREATE TABLE test (id INT(10), image BLOB)");

      InsertSmallBlobInTestTableUsingPoolingConnection();
      InsertSmallBlobInTestTableUsingPoolingConnection();
      InsertSmallBlobInTestTableUsingPoolingConnection();

      using (MySqlConnection c1 = new MySqlConnection(Settings.ConnectionString + ";logging=true;cache server properties=true"))
      {
        c1.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT Count(*) from test", c1);
        var count = cmd.ExecuteScalar();
        Assert.AreEqual(3, Convert.ToInt32(count));
      }

      ExecuteSQL("DROP TABLE test ");
    }

    /// <summary>
    /// Util method for CacheServerPropertiesCausePacketTooLarge Test Method
    /// </summary>
    void InsertSmallBlobInTestTableUsingPoolingConnection()
    {
      string connStr = Settings.ConnectionString +
      String.Format(";logging=true;cache server properties=true;");

      using (MySqlConnection c1 = new MySqlConnection(connStr))
      {
        c1.Open();
        byte[] image = Utils.CreateBlob(7152);
        MySqlCommand cmd = new MySqlCommand("INSERT INTO test VALUES(NULL, ?image)", c1);
        cmd.Parameters.AddWithValue("?image", image);
        cmd.ExecuteNonQuery();
      }
    }

    #region WL14389

    [Test, Description("Check Pooling Connection works correctly")]
    public void MultipleConnectionWithPooling()
    {
      var connectionString = $"server={Host};user={Settings.UserID};password={Settings.Password};port={Port};Max Pool Size=4;Min Pool Size=0;sslmode=none;connectiontimeout=5;";
      var myConnection1 = new MySqlConnection(connectionString);
      var myConnection2 = new MySqlConnection(connectionString);
      var myConnection3 = new MySqlConnection(connectionString);
      var myConnection4 = new MySqlConnection(connectionString);
      var myConnection5 = new MySqlConnection(connectionString);

      // Get a connection out of the pool.
      myConnection1.Open();
      Assert.AreEqual(ConnectionState.Open, myConnection1.State);
      // Get a second connection out of the pool.
      myConnection2.Open();
      Assert.AreEqual(ConnectionState.Open, myConnection2.State);
      // Open a third connection.
      myConnection3.Open();
      Assert.AreEqual(ConnectionState.Open, myConnection3.State);
      // Return the all connections to the pool.
      myConnection1.Close();
      myConnection2.Close();
      myConnection3.Close();

      // Open five connections.
      myConnection1.Open();
      myConnection2.Open();
      myConnection3.Open();
      myConnection4.Open();
      Exception ex = Assert.Throws<MySqlException>(() => myConnection5.Open());
      StringAssert.Contains("Timeout", ex.Message);

      myConnection1.Close();
      myConnection2.Close();
      myConnection3.Close();
      myConnection4.Close();

    }

    /// <summary>
    /// Bug 18665388
    /// </summary>
    [Test, Description("Verify the connection is not in CLOSE_WAIT state after failed connection due to Connection Limit")]
    public void CloseWaitScenario1()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is only for Windows OS");
      var poolSize = 5;
      var connectionString = $"server={Host};user={Settings.UserID};database={Settings.Database};port={Port};password={Settings.Password};Pooling=true;Max Pool Size={poolSize};SSL Mode=None;ConnectionTimeout=5";
      List<MySqlConnection> connectionList = new();
      using (var con = new MySqlConnection(connectionString))
      {
        while (connectionList.Count < poolSize)
        {
          var newConn = new MySqlConnection(connectionString);
          newConn.Open();
          connectionList.Add(newConn);
        }
        Assert.Throws<MySqlException>(() => con.Open());
        var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        var endPoints = ipProperties.GetActiveTcpListeners();
        var tcpConnections = ipProperties.GetActiveTcpConnections();
        foreach (var info in tcpConnections)
          if (info.LocalEndPoint.Address.ToString() == "127.0.0.1" && info.LocalEndPoint.Port.ToString() == Port.ToString())
            Assert.True(info.State.ToString() != "CLOSE_WAIT");
      }
      foreach (var item in connectionList)
      {
        item.Close();
        item.Dispose();
      }
    }

    #endregion WL14389

  }
}
