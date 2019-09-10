// Copyright (c) 2013, 2018, Oracle and/or its affiliates. All rights reserved.
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

using System;
using Xunit;
using System.Transactions;
using System.Data.Common;
using System.Data;
using System.Threading;

namespace MySql.Data.MySqlClient.Tests
{
  public class Transactions : TestBase
  {
    public Transactions(TestFixture fixture) : base(fixture)
    {
    }        

    void TransactionScopeInternal(bool commit)
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))");
      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString))
      {
        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES ('a', 'name', 'name2')", c);

        using (TransactionScope ts = new TransactionScope())
        {
          c.Open();          
          cmd.ExecuteNonQuery();
          if (commit)
            ts.Complete();
        }

        cmd.CommandText = "SELECT COUNT(*) FROM Test";
        object count = cmd.ExecuteScalar();
        Assert.Equal(commit ? 1 : 0, Convert.ToInt32(count));
      }
    }

    [Fact]
    public void TransactionScopeRollback()
    {      
      TransactionScopeInternal(false);
    }

    [Fact]
    public void TransactionScopeCommit()
    {
      TransactionScopeInternal(true);
    }

    /// <summary>
    /// Bug #34448 Connector .Net 5.2.0 with Transactionscope doesn´t use specified IsolationLevel 
    /// </summary>
    [Fact]
    public void TransactionScopeWithIsolationLevel()
    {
      TransactionOptions opts = new TransactionOptions();
      opts.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;

      using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, opts))
      {
        string connStr = Connection.ConnectionString;
        using (MySqlConnection myconn = new MySqlConnection(connStr))
        {
          myconn.Open();
          MySqlCommand cmd = new MySqlCommand(Connection.driver.Version.isAtLeast(8, 0, 1) ?
            "SHOW VARIABLES LIKE 'transaction_isolation'":
            "SHOW VARIABLES LIKE 'tx_isolation'"
          , myconn);
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            reader.Read();
            string level = reader.GetString(1);
            Assert.Equal("READ-COMMITTED", level);
          }
        }
      }

      opts.IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted;
      using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, opts))
      {
        string connStr = Connection.ConnectionString;
        using (MySqlConnection myconn = new MySqlConnection(connStr))
        {
          myconn.Open();
          MySqlCommand cmd = new MySqlCommand(Connection.driver.Version.isAtLeast(8, 0, 1) ?
            "SHOW VARIABLES LIKE 'transaction_isolation'" :
            "SHOW VARIABLES LIKE 'tx_isolation'"
          , myconn);
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            reader.Read();
            string level = reader.GetString(1);
            Assert.Equal("READ-UNCOMMITTED", level);
          }
        }
      }
    }

    [Fact]
    public void TransactionReadOnlyIsAvailable()
    {
      string connStr = Connection.ConnectionString;
      using (MySqlConnection myconn = new MySqlConnection(connStr))
      {
        myconn.Open();
        MySqlCommand cmd = new MySqlCommand(Connection.driver.Version.isAtLeast(8, 0, 1) ?
          "SHOW VARIABLES LIKE 'transaction_read_only'":
          "SHOW VARIABLES LIKE 'tx_read_only'"
        , myconn);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          if (Connection.driver.Version.isAtLeast(5, 7, 0))
            Assert.Equal("OFF", reader.GetString(1));
        }
      }
    }

    /// <summary>
    /// Bug #27289 Transaction is not rolledback when connection close 
    /// </summary>
    [Fact]
    public void RollingBackOnClose()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id INT) ENGINE=InnoDB");

      MySqlConnectionStringBuilder connStrBuilder = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      connStrBuilder.Pooling = true;
      connStrBuilder.ConnectionReset = true;
      string connStr = connStrBuilder.GetConnectionString(true);

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (1)", c);
        c.BeginTransaction();
        cmd.ExecuteNonQuery();
      }

      using (MySqlConnection c2 = new MySqlConnection(connStr))
      {
        c2.Open();
        MySqlCommand cmd2 = new MySqlCommand("SELECT COUNT(*) from Test", c2);
        c2.BeginTransaction();
        object count = cmd2.ExecuteScalar();
        Assert.Equal(0, Convert.ToInt32(count));
      }

      MySqlConnection connection = new MySqlConnection(connStr);
      connection.Open();
      KillConnection(connection);
    }

    /// <summary>
    /// NullReferenceException thrown on TransactionScope dispose
    /// </summary>
    //    [Fact]
    //    public void LockedTable()
    //    {

    //      Debug.Print("Enter LockedTable");

    //      string connStr = st.GetConnectionString(true);

    //      executeSQL("DROP TABLE IF EXISTS t1");
    //      executeSQL("DROP TABLE IF EXISTS t2");

    //      connStr = String.Format(@"Use Affected Rows=true;allow user variables=yes;Server=localhost;Port={0};
    //            Database={1};Uid=root;Connect Timeout=35;default command timeout=90;charset=utf8", st.port, st.database0);


    //      executeSQL(@"CREATE TABLE `t1` (
    //                `Key` int(10) unsigned NOT NULL auto_increment,
    //                `Val` varchar(100) NOT NULL,
    //                `Val2` varchar(100) NOT NULL default '',
    //                PRIMARY KEY  (`Key`)
    //                ) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=latin1");
    //      executeSQL(@"CREATE TABLE `t2` (
    //                `Key` int(10) unsigned NOT NULL auto_increment,
    //                `Val` varchar(100) NOT NULL,
    //                PRIMARY KEY  (`Key`)
    //                ) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=latin1");

    //      executeSQL("lock tables t2 read");      

    //      using (TransactionScope scope = new TransactionScope())
    //      {
    //        using (MySqlConnection conn = new MySqlConnection(connStr))
    //        using (MySqlCommand cmd = conn.CreateCommand())
    //        {
    //          conn.Open();                
    //          cmd.CommandText = @"insert into t1 (Val,Val2) values (?value1, ?value2)"; ;
    //          cmd.CommandTimeout = 5;
    //          cmd.Parameters.AddWithValue("?value1", new Random().Next());
    //          cmd.Parameters.AddWithValue("?value2", new Random().Next());
    //          cmd.ExecuteNonQuery();
    //        }

    //        using (MySqlConnection conn = new MySqlConnection(connStr))
    //        using (MySqlCommand cmd = conn.CreateCommand())
    //        {
    //          conn.Open();          
    //          cmd.CommandText = @"insert into t2 (Val) values (?value)";
    //          cmd.CommandTimeout = 5;
    //          cmd.Parameters.AddWithValue("?value", new Random().Next());
    //          Exception ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
    //          Assert.Equal(ex.Message, "Timeout expired.  The timeout period elapsed prior to completion of the operation or the server is not responding.");         
    //        }               

    //        scope.Complete();
    //      }     

    //      MySqlPoolManager.ClearAllPools();

    //      Debug.Print("Out LockedTable");
    //    }


#if !NETCOREAPP1_1
    /// <summary>
    /// Bug #22042 mysql-connector-net-5.0.0-alpha BeginTransaction 
    /// </summary>
    [Fact]
    public void Bug22042()
    {
      DbProviderFactory factory =
          new MySql.Data.MySqlClient.MySqlClientFactory();
      using (DbConnection conexion = factory.CreateConnection())
      {
        conexion.ConnectionString = Connection.ConnectionString;
        conexion.Open();
        DbTransaction trans = conexion.BeginTransaction();
        trans.Rollback();
      }
    }

    /// <summary>
    /// Bug #26754  	EnlistTransaction throws false MySqlExeption "Already enlisted"
    /// </summary>
    [Fact]
    public void EnlistTransactionNullTest()
    {
      try
      {
        MySqlCommand cmd = new MySqlCommand();
        cmd.Connection = Connection;
        cmd.Connection.EnlistTransaction(null);
      }
      catch { }

      using (TransactionScope ts = new TransactionScope())
      {
        MySqlCommand cmd = new MySqlCommand();
        cmd.Connection = Connection;
        cmd.Connection.EnlistTransaction(Transaction.Current);
      }
    }

    /// <summary>
    /// Bug #26754  	EnlistTransaction throws false MySqlExeption "Already enlisted"
    /// </summary>
    [Fact]
    public void EnlistTransactionWNestedTrxTest()
    {
      MySqlTransaction t = Connection.BeginTransaction();

      using (TransactionScope ts = new TransactionScope())
      {
        MySqlCommand cmd = new MySqlCommand();
        cmd.Connection = Connection;
        try
        {
          cmd.Connection.EnlistTransaction(Transaction.Current);
        }
        catch (InvalidOperationException)
        {
          /* caught NoNestedTransactions */
        }
      }

      t.Rollback();

      using (TransactionScope ts = new TransactionScope())
      {
        MySqlCommand cmd = new MySqlCommand();
        cmd.Connection = Connection;
        cmd.Connection.EnlistTransaction(Transaction.Current);
      }
    }

    [Fact(Skip = "Not compatible with linux")]
    public void ManualEnlistment()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))");
      string connStr = Connection.ConnectionString + ";auto enlist=false";
      MySqlConnection c = null;
      using (TransactionScope ts = new TransactionScope())
      {
        c = new MySqlConnection(connStr);
        c.Open();
        MySqlCommand cmd = new MySqlCommand("LOCK TABLES test WRITE;", c);
        cmd.ExecuteNonQuery();
        cmd = new MySqlCommand("INSERT INTO Test VALUES ('a', 'name', 'name2')", c);
        cmd.ExecuteNonQuery();
        executeSQL("UNLOCK TABLES");
      }
      MySqlCommand cmd2 = new MySqlCommand("LOCK TABLES test READ; SELECT COUNT(*) FROM test", c);
      Assert.Equal(1, Convert.ToInt32(cmd2.ExecuteScalar()));
      executeSQL("UNLOCK TABLES");
      c.Dispose();
      KillPooledConnection(connStr);
    }

    private void ManuallyEnlistingInitialConnection(bool complete)
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))");
      string connStr = Connection.ConnectionString + ";auto enlist=false";

      using (TransactionScope ts = new TransactionScope())
      {
        using (MySqlConnection c1 = new MySqlConnection(connStr))
        {
          c1.Open();
          c1.EnlistTransaction(Transaction.Current);
          MySqlCommand cmd1 = new MySqlCommand("INSERT INTO Test (key2) VALUES ('a')", c1);
          cmd1.ExecuteNonQuery();
        }

        using (MySqlConnection c2 = new MySqlConnection(connStr))
        {
          c2.Open();
          c2.EnlistTransaction(Transaction.Current);
          MySqlCommand cmd2 = new MySqlCommand("INSERT INTO Test (key2) VALUES ('b')", c2);
          cmd2.ExecuteNonQuery();
        }
        if (complete)
          ts.Complete();
      }

      KillPooledConnection(connStr);
    }

    [Fact]
    public void ManuallyEnlistingInitialConnectionTest()
    { 
      ManuallyEnlistingInitialConnection(true);
    }

    [Fact]
    public void ManuallyEnlistingInitialConnectionNoComplete()
    {
      ManuallyEnlistingInitialConnection(false);
    }

    [Fact]
    public void ManualEnlistmentWithActiveConnection()
    {
      using (TransactionScope ts = new TransactionScope())
      {
        string connStr = Connection.ConnectionString;

        using (MySqlConnection c1 = new MySqlConnection(connStr))
        {
          c1.Open();

          connStr += "; auto enlist=false";
          using (MySqlConnection c2 = new MySqlConnection(connStr))
          {
            c2.Open();
            try
            {
              c2.EnlistTransaction(Transaction.Current);
            }
            catch (NotSupportedException)
            {
            }
          }
        }
      }
    }

    [Fact]
    public void AttemptToEnlistTwoConnections()
    {
      using (TransactionScope ts = new TransactionScope())
      {
        string connStr = Connection.ConnectionString;

        using (MySqlConnection c1 = new MySqlConnection(connStr))
        {
          c1.Open();

          using (MySqlConnection c2 = new MySqlConnection(connStr))
          {
            try
            {
              c2.Open();
            }
            catch (NotSupportedException)
            {
            }
          }
        }
      }
    }


    private void NestedScopeInternalTest(
        TransactionScopeOption nestedOption,
        bool innerComplete,
        bool outerComplete,
        bool expectInnerChangesVisible,
        bool expectOuterChangesVisible)
    {
      executeSQL("CREATE TABLE T(str varchar(10))");
      try
      {
        using (TransactionScope outer = new TransactionScope())
        {
          string connStr = Connection.ConnectionString;
          using (MySqlConnection c1 = new MySqlConnection(connStr))
          {
            c1.Open();
            //MySqlCommand cmd1 = new MySqlCommand("LOCK TABLES T WRITE;", c1);
            //cmd1.ExecuteNonQuery();
            MySqlCommand  cmd1 = new MySqlCommand("INSERT INTO T VALUES ('outer')", c1);
            cmd1.ExecuteNonQuery();
            //cmd1 = new MySqlCommand("UNLOCK TABLES", c1);
            //cmd1.ExecuteNonQuery();          
            using (TransactionScope inner = new TransactionScope(nestedOption))
            {

              MySqlConnection c2;
              if (nestedOption == TransactionScopeOption.Required)
              {
                // inner scope joins already running ambient
                // transaction, we cannot use new connection here
                c2 = c1;
              }
              else
              {
                // when TransactionScopeOption.RequiresNew or 
                // new TransactionScopeOption.Suppress is used,
                // we have to use a new transaction. We create a
                // new connection for it.
                c2 = new MySqlConnection(connStr);
                c2.Open();
              }
              //MySqlCommand cmd2 = new MySqlCommand("LOCK TABLES T WRITE;", c2);
              //cmd2.ExecuteNonQuery();
              MySqlCommand  cmd2 = new MySqlCommand("INSERT INTO T VALUES ('inner')", c2);
              cmd2.ExecuteNonQuery();
              //cmd2 = new MySqlCommand("UNLOCK TABLES", c2);
              //cmd2.ExecuteNonQuery();
              if (innerComplete)
              {
                inner.Complete();                
              }

              // Dispose connection if it was created.
              if (c2 != c1)
                c2.Dispose();
            }          
          }

          if (outerComplete)         
            outer.Complete();            

        }
        bool innerChangesVisible =
           ((long)MySqlHelper.ExecuteScalar(Connection, "select count(*) from T where str = 'inner'") == 1);
        bool outerChangesVisible =
            ((long)MySqlHelper.ExecuteScalar(Connection, "select count(*) from T where str = 'outer'") == 1);
        Assert.Equal(innerChangesVisible, expectInnerChangesVisible);
        Assert.Equal(outerChangesVisible, expectOuterChangesVisible);
      }
      finally
      {
        MySqlHelper.ExecuteNonQuery(Connection, "DROP TABLE T");
      }
    }

    /// <summary>
    /// Test inner/outer scope behavior, with different scope options, 
    /// completing either inner or outer scope, or both.
    /// </summary>
    [Fact]
    public void NestedScope()
    { 
      // inner scope joins the ambient scope, neither inner not outer  scope completes
      // Expect empty table.
      NestedScopeInternalTest(TransactionScopeOption.Required, false, false, false, false);

      // inner scope joins the ambient scope, inner does not complete, outer completes
      // Expect exception while disposing outer transaction
      //try
      //{
        Assert.Throws<TransactionAbortedException>(() => NestedScopeInternalTest(TransactionScopeOption.Required, false, true, false, false));        
      //}
      //catch (TransactionAbortedException)
      //{
      //}

      // inner scope joins the ambient scope, inner completes, outer does not
      // Expect empty table.
      NestedScopeInternalTest(TransactionScopeOption.Required, true, false, false, false);

      // inner scope joins the ambient scope, both complete.
      // Expect table with entries for inner and outer scope
      NestedScopeInternalTest(TransactionScopeOption.Required, true, true, true, true);



      // inner scope creates new transaction, neither inner not outer  scope completes
      // Expect empty table.
      NestedScopeInternalTest(TransactionScopeOption.RequiresNew, false, false, false, false);

      // inner scope creates new transaction, inner does not complete, outer completes
      // Expect changes by outer transaction visible ??
      NestedScopeInternalTest(TransactionScopeOption.RequiresNew, false, true, false, true);

      // inner scope creates new transactiion, inner completes, outer does not
      // Expect changes by inner transaction visible
      NestedScopeInternalTest(TransactionScopeOption.RequiresNew, true, false, true, false);

      // inner scope creates new transaction, both complete
      NestedScopeInternalTest(TransactionScopeOption.RequiresNew, true, true, true, true);


      // inner scope suppresses transaction, neither inner not outer  scope completes
      // Expect changes made by inner scope to be visible
      NestedScopeInternalTest(TransactionScopeOption.Suppress, false, false, true, false);

      // inner scope supresses transaction, inner does not complete, outer completes
      // Expect changes by inner scope to be visible ??
      NestedScopeInternalTest(TransactionScopeOption.Suppress, true, false, true, false);

      // inner scope supresses transaction, inner completes, outer does not
      // Expect changes by inner transaction visible
      NestedScopeInternalTest(TransactionScopeOption.Suppress, true, false, true, false);

      // inner scope supresses transaction, both complete
      NestedScopeInternalTest(TransactionScopeOption.Suppress, true, true, true, true);
    }

    private void ReusingSameConnection(bool pooling, bool complete)
    {
      int c1Thread;
      executeSQL("TRUNCATE TABLE Test");

      using (TransactionScope ts = new TransactionScope(TransactionScopeOption.RequiresNew, TimeSpan.MaxValue))
      {
        string connStr = Connection.ConnectionString;
        if (!pooling)
          connStr += ";pooling=false";

        using (MySqlConnection c1 = new MySqlConnection(connStr))
        {
          c1.Open();
          MySqlCommand cmd1 = new MySqlCommand("INSERT INTO Test (key2) VALUES ('a')", c1);
          cmd1.ExecuteNonQuery();
          c1Thread = c1.ServerThread;
        }

        using (MySqlConnection c2 = new MySqlConnection(connStr))
        {
          c2.Open();
          MySqlCommand cmd2 = new MySqlCommand("INSERT INTO Test (key2) VALUES ('b')", c2);
          cmd2.ExecuteNonQuery();
          Assert.Equal(c1Thread, c2.ServerThread);
        }

        if (complete)
          ts.Complete();
      }

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(dt);
      if (complete)
      {
        Assert.Equal(2, dt.Rows.Count);
        Assert.Equal("a", dt.Rows[0][0]);
        Assert.Equal("b", dt.Rows[1][0]);
      }
      else
      {
        Assert.Equal(0, dt.Rows.Count);
      }
    }

    [Fact]
    public void ReusingSameConnectionTest()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))");
      ReusingSameConnection(true, true);
      //            Assert.Equal(processes + 1, CountProcesses());

      ReusingSameConnection(true, false);
      //          Assert.Equal(processes + 1, CountProcesses());

      ReusingSameConnection(false, true);
      //        Assert.Equal(processes + 1, CountProcesses());

      ReusingSameConnection(false, false);
      //      Assert.Equal(processes + 1, CountProcesses());
    }

    /// <summary>
    /// bug#35330 - even if transaction scope has expired, rows can be inserted into
    /// the table, due to race condition with the thread doing rollback
    /// </summary>
    [Fact(Skip = "Not compatible with linux")]
    public void ScopeTimeoutWithMySqlHelper()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id int)");
      string connStr = Connection.ConnectionString;
      using (new TransactionScope(TransactionScopeOption.RequiresNew, TimeSpan.FromSeconds(1)))
      {
        try
        {
          for (int i = 0; ; i++)
          {
            MySqlHelper.ExecuteNonQuery(connStr, String.Format("INSERT INTO Test VALUES({0})", i)); ;
          }
        }
        catch (Exception)
        {
        }
      }
      long count = (long)MySqlHelper.ExecuteScalar(connStr, "select count(*) from test");
      Assert.Equal(0, count);
    }

    /// <summary>
    /// Variation of previous test, with a single connection and maual enlistment.
    /// Checks that  transaction rollback leaves the connection intact (does not close it) 
    /// and  checks that no command is possible after scope has expired and 
    /// rollback by timer thread is finished.
    /// </summary>
    [Fact]
    public void AttemptToUseConnectionAfterScopeTimeout()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id int)");
      string connStr = Connection.ConnectionString;
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("select 1", c);
        using (new TransactionScope(TransactionScopeOption.RequiresNew,
            TimeSpan.FromSeconds(1)))
        {
          c.EnlistTransaction(Transaction.Current);
          cmd = new MySqlCommand("select 1", c);
          try
          {
            for (int i = 0; ; i++)
            {
              cmd.CommandText = String.Format("INSERT INTO Test VALUES({0})", i);
              cmd.ExecuteNonQuery();
            }
          }
          catch (Exception)
          {
            // Eat exception
          }

          // Here, scope is timed out and rollback is in progress.
          // Wait until timeout thread finishes rollback then try to 
          // use an aborted connection.
          Thread.Sleep(500);
          //try
          //{
            
            Assert.Throws<TransactionAbortedException>(() => cmd.ExecuteNonQuery());
          //}
          //catch (TransactionAbortedException)
          //{
          //}
        }
        Assert.True(c.State == ConnectionState.Open);
        cmd.CommandText = "select count(*) from Test";
        long count = (long)cmd.ExecuteScalar();
        Assert.Equal(0, count);
      }
    }


    /// <summary>
    /// Bug#54681 : Null Reference exception when using transaction
    /// scope in more that one thread
    /// </summary>
    [Fact]
    public void TransactionScopeWithThreads()
    {
      // use transaction scope in the current thread
      DoThreadWork();

      //use transaction scope in another thread (used to crash with null
      // reference exception)
      Thread t = new Thread(new ThreadStart(DoThreadWork));
      t.Start();
      t.Join();
    }

    [Fact]
    public void SnapshotIsolationLevelThrowsNotSupportedException()
    {        
        using (MySqlConnection newcon = new MySqlConnection(Connection.ConnectionString))
        {
          newcon.Open();            
          var ex = Assert.Throws<NotSupportedException>(() => newcon.BeginTransaction(System.Data.IsolationLevel.Snapshot));
          Assert.Equal("Snapshot isolation level is not supported.", ex.Message);            
        }
    }

    private void DoThreadWork()
    {
      using (TransactionScope ts = new TransactionScope())
      {
        string connStr = Connection.ConnectionString;
        using (MySqlConnection c1 = new MySqlConnection(connStr))
        {
          c1.Open();
        }
      }
    }

    /// <summary>
    /// Related to bug http://bugs.mysql.com/bug.php?id=71502
    /// </summary>
    [Fact]
    public void NestedTransactionException()
    {
      MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      sb.Pooling = true;
      sb.ConnectionReset = false;
      string connectionString = sb.ToString();
      for (int i = 0; i < 5; i++)
      {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
          connection.Open();
          DbTransaction trx = connection.BeginTransaction();
          MySqlCommand cmd = new MySqlCommand("update abc", connection);
          Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
          // the following condition validates 2 scenarios
          // when trx.Dispose is not invoked (when i == 0 or 1)
          // and when the connection is closed before calling trx.Dispose
          if (i > 1) connection.Close();
          trx.Dispose();
        }
      }
    }

#endif
  }
}
