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
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Reflection;
using System.Threading;
using System.Data;
using System.Collections;
#if !RT
using System.Timers;
#endif
#if NET_40_OR_GREATER
using System.Threading.Tasks;
#endif
using System.Diagnostics;


namespace MySql.Data.MySqlClient.Tests
{
  public class PoolingTests : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;

#if CLR4
    private System.Timers.Timer timer; 
    private int callbacksCount { get; set; }
    private int threadId { get; set; }
    private bool isConnectionAlive { get; set; }
#endif

    public void SetFixture(SetUpClass data)
    {
      st = data;
      
      if (st.conn.connectionState != ConnectionState.Closed)
        st.conn.Close();
  
      st.conn.Open();
      st.execSQL("CREATE TABLE Test (id INT, name VARCHAR(100))");
    }

    public void Dispose()
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");
      st.execSQL("DROP PROCEDURE IF EXISTS spTest");      
    }

    [Fact]
    public void Connection()
    {
      string connStr = st.GetPoolingConnectionString();

      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      int serverThread = c.ServerThread;
      c.Close();

      // first test that only a single connection get's used
      for (int i = 0; i < 10; i++)
      {
        c = new MySqlConnection(connStr);
        c.Open();
        Assert.Equal(serverThread, c.ServerThread);
        c.Close();
      }

      c.Open();
      st.KillConnection(c);
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
            Assert.True(connArray[i].ServerThread != connArray[j].ServerThread);
        }
      }

      for (int i = 0; i < connArray.Length; i++)
      {
        st.KillConnection(connArray[i]);
        connArray[i].Close();
      }
    }

    [Fact]
    public void OpenKilled()
    {
      string connStr = st.GetPoolingConnectionString() + ";min pool size=1; max pool size=1";
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      int threadId = c.ServerThread;
      // thread gets killed right here
      st.KillConnection(c);
      c.Close();

      c.Dispose();

      c = new MySqlConnection(connStr);
      c.Open();
      int secondThreadId = c.ServerThread;
      st.KillConnection(c);
      c.Close();
      Assert.False(threadId == secondThreadId);
    }

#if !RT
    [Fact]
    public void ReclaimBrokenConnection()
    {
      // now create a new connection string only allowing 1 connection in the pool
      string connStr = st.GetPoolingConnectionString() + ";connect timeout=2;max pool size=1";

      // now use up that connection
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();

      // now attempting to open a connection should fail
      MySqlConnection c2 = new MySqlConnection(connStr);
      Exception ex = Assert.Throws<MySqlException>(() => c2.Open());
      Assert.True(ex.Message.Contains("error connecting: Timeout expired.  The timeout period elapsed prior to obtaining a connection from the pool."));
        
      // we now kill the first connection to simulate a server stoppage
      st.KillConnection(c);

      // now we do something on the first connection
      
      ex = Assert.Throws<InvalidOperationException>(() => c.ChangeDatabase("mysql"));
      Assert.True(ex.Message.Contains("The connection is not open."));
  
      // Opening a connection now should work
      MySqlConnection connection = new MySqlConnection(connStr);
      connection.Open();
      st.KillConnection(connection);
      connection.Close();
    }
#endif

    [Fact]
    public void TestUserReset()
    {
      string connStr = st.GetPoolingConnectionString();
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("SET @testvar='5'", c);
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT @testvar";
        object var = cmd.ExecuteScalar();
        Assert.Equal("5", var);
        c.Close();

        c.Open();
        object var2 = cmd.ExecuteScalar();
        Assert.Equal(DBNull.Value, var2);
        st.KillConnection(c);
      }
    }

#if !RT
    // Test that thread does not come to pool after abort
    [Fact]
    public void TestAbort()
    {
      string connStr = st.GetPoolingConnectionString();
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
        Assert.True(c1.ServerThread != threadId);
        st.KillConnection(c1);
      }
    }
#endif

    /// <summary>
    /// Bug #25614 After connection is closed, and opened again UTF-8 characters are not read well 
    /// </summary>
    [Fact]
    public void UTF8AfterClosing()
    {
      string originalValue = "??????????";
      st.execSQL("DROP TABLE IF EXISTS test");


      st.execSQL("CREATE TABLE test (id int(11) NOT NULL, " +
        "value varchar(100) NOT NULL, PRIMARY KEY  (`id`) " +
        ") ENGINE=MyISAM DEFAULT CHARSET=utf8");

      string connStr = st.GetPoolingConnectionString() + ";charset=utf8";
      using (MySqlConnection con = new MySqlConnection(connStr))
      {
        con.Open();

        MySqlCommand cmd = new MySqlCommand("INSERT INTO test VALUES (1, '??????????')", con);
        cmd.ExecuteNonQuery();

        cmd = new MySqlCommand("SELECT value FROM test WHERE id = 1", con);
        string firstS = cmd.ExecuteScalar().ToString();
        Assert.Equal(originalValue, firstS);

        con.Close();
        con.Open();

        //Does not work:
        cmd = new MySqlCommand("SELECT value FROM test WHERE id = 1", con);
        string secondS = cmd.ExecuteScalar().ToString();

        st.KillConnection(con);
        con.Close();
        Assert.Equal(firstS, secondS);
      }
    }

#if !RT

    private void PoolingWorker(object cn)
    {
      MySqlConnection conn = (cn as MySqlConnection);

      Thread.Sleep(5000);
      conn.Close();
    }

    /// <summary>
    /// Bug #24373 High CPU utilization when no idle connection 
    /// </summary>
    [Fact]
    public void MultipleThreads()
    {
      string connStr = st.GetPoolingConnectionString() + ";max pool size=1";
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();

      ParameterizedThreadStart ts = new ParameterizedThreadStart(PoolingWorker);
      Thread t = new Thread(ts);
      t.Start(c);

      using (MySqlConnection c2 = new MySqlConnection(connStr))
      {
        c2.Open();
        st.KillConnection(c2);
      }
      c.Close();
    }

#endif


    [Fact]
    public void NewTest()
    {
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("DROP TABLE IF EXISTS test");
      st.execSQL("CREATE TABLE Test (id INT, name VARCHAR(50))");
      st.execSQL("CREATE PROCEDURE spTest(theid INT) BEGIN SELECT * FROM test WHERE id=theid; END");
      st.execSQL("INSERT INTO test VALUES (1, 'First')");
      st.execSQL("INSERT INTO test VALUES (2, 'Second')");
      st.execSQL("INSERT INTO test VALUES (3, 'Third')");
      st.execSQL("INSERT INTO test VALUES (4, 'Fourth')");

      string connStr = st.GetPoolingConnectionString();

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
              Assert.Equal(1, x);
            }
          }
        }
      }
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      st.KillConnection(c);
    }  

    bool IsConnectionAlive(int serverThread)
    {
#if RT
      MySqlCommand cmd = new MySqlCommand("SHOW PROCESSLIST", st.conn);
      using (MySqlDataReader dr = cmd.ExecuteReader())
      {
        while (dr.Read())
        {
          if (dr.GetInt64("id") == serverThread)
            return true;
        }
      }
      return false;
#else
      MySqlDataAdapter da = new MySqlDataAdapter("SHOW PROCESSLIST", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      foreach (DataRow row in dt.Rows)
        if ((long)row["Id"] == serverThread)
          return true;
      return false;
#endif
    }

#if CLR4
    [Fact] 
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

    //[Fact]
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
    //  Assert.Equal(9, idleList.Count);      
      
    //  FieldInfo inUsePool = poolType.GetField("inUsePool", BindingFlags.NonPublic | BindingFlags.Instance);
    //  ICollection inUseList = (ICollection)inUsePool.GetValue(poolHash[settings.ConnectionString]);
    //  Assert.Equal(1, inUseList.Count);

    //  // now open 4 more of these.  Now we shoudl have 5 open and five
    //  // still in the pool
    //  for (int i = 1; i < 5; i++)
    //  {
    //    connections[i] = new MySqlConnection(connStr);
    //    connections[i].Open();
    //  }

    //  Assert.Equal(5, inUseList.Count);
    //  Assert.Equal(5, idleList.Count);

    //  clearingPools = GetClearingPools();
    //  Assert.Equal(0, clearingPools.Count);

    //  // now tell this connection to clear its pool
    //  MySqlConnection.ClearPool(connections[0]);
    //  Assert.Equal(1, clearingPools.Count);
    //  Assert.Equal(0, idleList.Count);

    //  for (int i = 0; i < 5; i++)
    //    connections[i].Close();
    //  Assert.Equal(0, clearingPools.Count);
    //}

    private static List<MySqlPool> GetClearingPools()
    {
      Type poolManagerType = typeof(MySqlPoolManager);
#if RT
      FieldInfo clearingPoolsFI = poolManagerType.GetRuntimeField("clearingPools");
#else
      FieldInfo clearingPoolsFI = poolManagerType.GetField("clearingPools",
        BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
#endif
      return clearingPoolsFI.GetValue(null) as List<MySqlPool>;
    }

    [Fact]
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
          Assert.Equal(ex.Message, "Unable to connect to any of the specified MySQL hosts.");          
        }
        Thread.Sleep(50);
      }
      MySqlConnection.ClearAllPools();
    }

    /// <summary>
    /// Bug #42801  	ClearPool .Net connector : NullReferenceException
    /// </summary>
    [Fact]
    public void DoubleClearingConnectionPool()
    {
      MySqlConnection c1 = new MySqlConnection(st.GetConnectionString(true));
      MySqlConnection c2 = new MySqlConnection(st.GetConnectionString(true));
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
    [Fact]
    public void OpenSecondPooledConnectionWithoutDatabase()
    {
      string connectionString = st.GetPoolingConnectionString();

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
    [Fact]
    public void ConnectionResetAfterUnicode()
    {
      st.execSQL("DROP TABLE IF EXISTS test");
      st.execSQL("CREATE TABLE test (id INT, name VARCHAR(20) CHARSET UCS2)");
      st.execSQL("INSERT INTO test VALUES (1, 'test')");
      
      string connStr = st.GetPoolingConnectionString() + ";connection reset=true;min pool size=1; max pool size=1";
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
          st.KillConnection(c);
        }
      }
    }

#if !RT
    private void CacheServerPropertiesInternal(bool cache)
    {
      string connStr = st.GetPoolingConnectionString() +
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
          st.KillConnection(c2);
        }
        st.KillConnection(c);
      }
      int count = listener.CountLinesContaining("SHOW VARIABLES");
      Assert.Equal(cache ? 1 : 2, count);
    }

    [Fact]
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
    [Fact]
    public void CacheServerPropertiesCausePacketTooLarge()
    {
      st.execSQL("DROP TABLE IF EXISTS test");
      st.execSQL("CREATE TABLE test (id INT(10), image BLOB)");
      
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

      using (MySqlConnection c1 = new MySqlConnection(st.GetPoolingConnectionString() + ";logging=true;cache server properties=true"))
      {
        c1.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT Count(*) from test", c1);
        var count = cmd.ExecuteScalar();
        Assert.Equal(3, Convert.ToInt32(count));
      }

      st.execSQL("DROP TABLE test ");
    }


    /// <summary>
    /// Util method for CacheServerPropertiesCausePacketTooLarge Test Method
    /// </summary>
    void InsertSmallBlobInTestTableUsingPoolingConnection()
    {
      string connStr = st.GetPoolingConnectionString() +
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
