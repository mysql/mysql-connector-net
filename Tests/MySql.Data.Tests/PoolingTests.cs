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
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
#if CLR4
using System.Timers;
using System.Threading.Tasks;
#endif

namespace MySql.Data.MySqlClient.Tests
{
  /// <summary>
  /// Summary description for PoolingTests.
  /// </summary>
  [TestFixture]
  public class PoolingTests : BaseTest
  {
#if CLR4
    private System.Timers.Timer timer; 
    private int callbacksCount { get; set; }
    private int threadId { get; set; }
    private bool isConnectionAlive { get; set; }
#endif

    [Test]
    public void Connection()
    {
      string connStr = GetPoolingConnectionString();

      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      int serverThread = c.ServerThread;
      c.Close();

      // first test that only a single connection get's used
      for (int i = 0; i < 10; i++)
      {
        c = new MySqlConnection(connStr);
        c.Open();
        Assert.AreEqual(serverThread, c.ServerThread);
        c.Close();
      }

      c.Open();
      KillConnection(c);
      c.Close();

      connStr += ";Min Pool Size=10";
      MySqlConnection[] connArray = new MySqlConnection[10];
      for (int i = 0; i < connArray.Length; i++)
      {
        connArray[i] = new MySqlConnection(connStr);
        connArray[i].Open();
      }

      // now make sure all the server ids are different
      for (int i = 0; i < connArray.Length; i++)
      {
        for (int j = 0; j < connArray.Length; j++)
        {
          if (i != j)
            Assert.IsTrue(connArray[i].ServerThread != connArray[j].ServerThread);
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
      string connStr = GetPoolingConnectionString() + ";min pool size=1; max pool size=1";
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
      Assert.IsFalse(threadId == secondThreadId);
    }

    [Test]
    public void ReclaimBrokenConnection()
    {
      // now create a new connection string only allowing 1 connection in the pool
      string connStr = GetPoolingConnectionString() + ";connect timeout=2;max pool size=1";

      // now use up that connection
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();

      // now attempting to open a connection should fail
      try
      {
        MySqlConnection c2 = new MySqlConnection(connStr);
        c2.Open();
        Assert.Fail("Open after using up pool should fail");
      }
      catch (Exception) { }

      // we now kill the first connection to simulate a server stoppage
      base.KillConnection(c);

      // now we do something on the first connection
      try
      {
        c.ChangeDatabase("mysql");
        Assert.Fail("This change database should not work");
      }
      catch (Exception) { }

      // Opening a connection now should work
      MySqlConnection connection = new MySqlConnection(connStr);
      connection.Open();
      KillConnection(connection);
      connection.Close();
    }

    [Test]
    public void TestUserReset()
    {
      string connStr = GetPoolingConnectionString();
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

#if !CF
    // Test that thread does not come to pool after abort
    [Test]
    public void TestAbort()
    {
      string connStr = GetPoolingConnectionString();
      int threadId;
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        threadId = c.ServerThread;
        MethodInfo abort = c.GetType().GetMethod("Abort",
          BindingFlags.NonPublic | BindingFlags.Instance);
        abort.Invoke(c, null);
      }
      using (MySqlConnection c1 = new MySqlConnection(connStr))
      {
        c1.Open();
        Assert.IsTrue(c1.ServerThread != threadId);
        KillConnection(c1);
      }
    }
#endif

    /// <summary>
    /// Bug #25614 After connection is closed, and opened again UTF-8 characters are not read well 
    /// </summary>
    [Test]
    public void UTF8AfterClosing()
    {
      string originalValue = "??????????";

      execSQL("CREATE TABLE test (id int(11) NOT NULL, " +
        "value varchar(100) NOT NULL, PRIMARY KEY  (`id`) " +
        ") ENGINE=MyISAM DEFAULT CHARSET=utf8");

      string connStr = GetPoolingConnectionString() + ";charset=utf8";
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

#if !CF

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
      string connStr = GetPoolingConnectionString() + ";max pool size=1";
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();

      ParameterizedThreadStart ts = new ParameterizedThreadStart(PoolingWorker);
      Thread t = new Thread(ts);
      t.Start(c);

      using (MySqlConnection c2 = new MySqlConnection(connStr))
      {
        c2.Open();
        KillConnection(c2);
      }
      c.Close();
    }

#endif


    [Test]
    public void NewTest()
    {
      if (Version < new Version(5, 0)) return;

      execSQL("CREATE TABLE Test (id INT, name VARCHAR(50))");
      execSQL("CREATE PROCEDURE spTest(theid INT) BEGIN SELECT * FROM test WHERE id=theid; END");
      execSQL("INSERT INTO test VALUES (1, 'First')");
      execSQL("INSERT INTO test VALUES (2, 'Second')");
      execSQL("INSERT INTO test VALUES (3, 'Third')");
      execSQL("INSERT INTO test VALUES (4, 'Fourth')");

      string connStr = GetPoolingConnectionString();

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

    /// <summary>
    /// Bug #29409  	Bug on Open Connection with pooling=true to a MYSQL Server that is shutdown
    /// </summary>
    [Test]
    public void ConnectAfterMaxPoolSizeTimeouts()
    {
      //TODO: refactor test suite to support starting/stopping services
      /*            string connStr = "server=localhost;uid=root;database=test;pooling=true;connect timeout=6; max pool size = 6";
            MySqlConnection c = new MySqlConnection(connStr);
            for (int i = 0; i < 6; i++)
            {
              try
              {
                c.Open();
              }
              catch (Exception ex)
              {
              }
            }
            c.Open();
            c.Close();*/
    }

    bool IsConnectionAlive(int serverThread)
    {
      MySqlDataAdapter da = new MySqlDataAdapter("SHOW PROCESSLIST", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      foreach (DataRow row in dt.Rows)
        if ((long)row["Id"] == serverThread)
          return true;
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
        string connStr = GetPoolingConnectionString();
        using (MySqlConnection c = new MySqlConnection(connStr))
        {
          c.Open();          
          callbacksCount = 0;
          threadId = c.ServerThread;
        }

        // Pooled connection should be still alive
        Assert.IsTrue(IsConnectionAlive(threadId));
        
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
        Assert.IsTrue(callbacksCount == 1, "Callbacks value was not 1"); 

        //Check the connection was removed
        Assert.IsFalse(isConnectionAlive, "IsConnectionAlive failed");
                
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

    [Test]
    public void ClearPool()
    {
      // Start by clearing clearingPools for a clean test
      List<MySqlPool> clearingPools = GetClearingPools();
      clearingPools.Clear();

      string connStr = GetPoolingConnectionString() + ";min pool size=10";
      MySqlConnectionStringBuilder settings = new MySqlConnectionStringBuilder(connStr);

      MySqlConnection[] connections = new MySqlConnection[10];
      connections[0] = new MySqlConnection(connStr);
      connections[0].Open();

      Type poolManagerType = typeof(MySqlPoolManager);
      FieldInfo poolManagerHashTable = poolManagerType.GetField("pools",
        BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
      Hashtable poolHash = (Hashtable)poolManagerHashTable.GetValue(null);

      // now we need to investigate
      Type poolType = typeof(MySqlPool);

      FieldInfo inUsePool = poolType.GetField("inUsePool", BindingFlags.NonPublic | BindingFlags.Instance);
      ICollection inUseList = (ICollection)inUsePool.GetValue(poolHash[settings.ConnectionString]);
      Assert.AreEqual(1, inUseList.Count);

      FieldInfo idlePool = poolType.GetField("idlePool", BindingFlags.NonPublic | BindingFlags.Instance);
      ICollection idleList = (ICollection)idlePool.GetValue(poolHash[settings.ConnectionString]);
      Assert.AreEqual(9, idleList.Count);

      // now open 4 more of these.  Now we shoudl have 5 open and five
      // still in the pool
      for (int i = 1; i < 5; i++)
      {
        connections[i] = new MySqlConnection(connStr);
        connections[i].Open();
      }

      Assert.AreEqual(5, inUseList.Count);
      Assert.AreEqual(5, idleList.Count);

      clearingPools = GetClearingPools();
      Assert.AreEqual(0, clearingPools.Count);

      // now tell this connection to clear its pool
      MySqlConnection.ClearPool(connections[0]);
      Assert.AreEqual(1, clearingPools.Count);
      Assert.AreEqual(0, idleList.Count);

      for (int i = 0; i < 5; i++)
        connections[i].Close();
      Assert.AreEqual(0, clearingPools.Count);
    }

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
          try
          {
            connection.Open();
            Assert.Fail("Connection should throw an exception.");
          }
          catch (Exception)
          {
          }
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
      MySqlConnection c1 = new MySqlConnection(GetConnectionString(true));
      MySqlConnection c2 = new MySqlConnection(GetConnectionString(true));
      c1.Open();
      c2.Open();
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
      string connectionString = GetPoolingConnectionString();

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
      execSQL("CREATE TABLE test (id INT, name VARCHAR(20) CHARSET UCS2)");
      execSQL("INSERT INTO test VALUES (1, 'test')");

      string connStr = GetPoolingConnectionString() + ";connection reset=true;min pool size=1; max pool size=1";
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

#if !CF
    private void CacheServerPropertiesInternal(bool cache)
    {
      string connStr = GetPoolingConnectionString() +
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
      CacheServerPropertiesInternal(true);
      CacheServerPropertiesInternal(false);
    }



    /// <summary>
    /// Bug #66578
    /// CacheServerProperties can cause 'Packet too large' error
    /// when query exceeds 1024 bytes
    /// </summary>
    [Test]
    public void CacheServerPropertiesCausePacketTooLarge()
    {
     
      execSQL("CREATE TABLE test (id INT(10), image BLOB)");
      
      #if CLR4
     
      Parallel.Invoke(
        () =>  { InsertSmallBlobInTestTableUsingPoolingConnection(); },
        () =>  { InsertSmallBlobInTestTableUsingPoolingConnection(); },
        () =>  { InsertSmallBlobInTestTableUsingPoolingConnection(); } );
      
      #else

      InsertSmallBlobInTestTableUsingPoolingConnection();
      InsertSmallBlobInTestTableUsingPoolingConnection();
      InsertSmallBlobInTestTableUsingPoolingConnection();         

      #endif      

      using (MySqlConnection c1 = new MySqlConnection(GetPoolingConnectionString() + ";logging=true;cache server properties=true"))
      {
        c1.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT Count(*) from test", c1);
        var count = cmd.ExecuteScalar();
        Assert.AreEqual(3, count);
      }

      execSQL("DROP TABLE test ");
    }


    /// <summary>
    /// Util method for CacheServerPropertiesCausePacketTooLarge Test Method
    /// </summary>
    void InsertSmallBlobInTestTableUsingPoolingConnection()
    {
      string connStr = GetPoolingConnectionString() +
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

#if CLR4
    private void _timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      callbacksCount++;
      if (callbacksCount == 1)
         isConnectionAlive = IsConnectionAlive(threadId);
    }
#endif

#endif
  }
}
