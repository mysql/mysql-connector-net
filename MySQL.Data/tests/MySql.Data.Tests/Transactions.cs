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

using NUnit.Framework;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace MySql.Data.MySqlClient.Tests
{
  public class Transactions : TestBase
  {
    void TransactionScopeInternal(bool commit)
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))");
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
        Assert.AreEqual(commit ? 1 : 0, Convert.ToInt32(count));
      }
    }

    [Test]
    public void TransactionScopeRollback()
    {
      TransactionScopeInternal(false);
    }

    [Test]
    public void TransactionScopeCommit()
    {
      TransactionScopeInternal(true);
    }

    /// <summary>
    /// Bug #34448 Connector .Net 5.2.0 with Transactionscope doesn´t use specified IsolationLevel 
    /// </summary>
    [Test]
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
            "SHOW VARIABLES LIKE 'transaction_isolation'" :
            "SHOW VARIABLES LIKE 'tx_isolation'"
          , myconn);
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            reader.Read();
            string level = reader.GetString(1);
            Assert.AreEqual("READ-COMMITTED", level);
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
            Assert.AreEqual("READ-UNCOMMITTED", level);
          }
        }
      }
    }

    [Test]
    public void TransactionReadOnlyIsAvailable()
    {
      string connStr = Connection.ConnectionString;
      using (MySqlConnection myconn = new MySqlConnection(connStr))
      {
        myconn.Open();
        MySqlCommand cmd = new MySqlCommand(Connection.driver.Version.isAtLeast(8, 0, 1) ?
          "SHOW VARIABLES LIKE 'transaction_read_only'" :
          "SHOW VARIABLES LIKE 'tx_read_only'"
        , myconn);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          if (Connection.driver.Version.isAtLeast(5, 7, 0))
            Assert.AreEqual("OFF", reader.GetString(1));
        }
      }
    }

    /// <summary>
    /// Bug #27289 Transaction is not rolledback when connection close 
    /// </summary>
    [Test]
    public void RollingBackOnClose()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (id INT) ENGINE=InnoDB");

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
        Assert.AreEqual(0, Convert.ToInt32(count));
      }

      MySqlConnection connection = new MySqlConnection(connStr);
      connection.Open();
      KillConnection(connection);
    }

    /// <summary>
    /// Bug #22042 mysql-connector-net-5.0.0-alpha BeginTransaction 
    /// </summary>
    [Test]
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
    /// Bug #26754 EnlistTransaction throws false MySqlExeption "Already enlisted"
    /// </summary>
    [Test]
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
    /// Bug #26754 EnlistTransaction throws false MySqlExeption "Already enlisted"
    /// </summary>
    [Test]
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

    [Test]
    public void ManualEnlistment()
    {
#if !NETFRAMEWORK
      if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) Assert.Ignore();
#endif
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))");
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
        ExecuteSQL("UNLOCK TABLES");
      }
      MySqlCommand cmd2 = new MySqlCommand("LOCK TABLES test READ; SELECT COUNT(*) FROM test", c);
      Assert.AreEqual(1, Convert.ToInt32(cmd2.ExecuteScalar()));
      ExecuteSQL("UNLOCK TABLES");
      c.Dispose();
      KillPooledConnection(connStr);
    }

    private void ManuallyEnlistingInitialConnection(bool complete)
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))");
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

    [Test]
    public void ManuallyEnlistingInitialConnectionTest()
    {
      ManuallyEnlistingInitialConnection(true);
    }

    [Test]
    public void ManuallyEnlistingInitialConnectionNoComplete()
    {
      ManuallyEnlistingInitialConnection(false);
    }

    [Test]
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

    [Test]
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
      ExecuteSQL("CREATE TABLE T(str varchar(10))");
      try
      {
        using (TransactionScope outer = new TransactionScope())
        {
          string connStr = Connection.ConnectionString;
          using (MySqlConnection c1 = new MySqlConnection(connStr))
          {
            c1.Open();
            MySqlCommand cmd1 = new MySqlCommand("INSERT INTO T VALUES ('outer')", c1);
            cmd1.ExecuteNonQuery();

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

              MySqlCommand cmd2 = new MySqlCommand("INSERT INTO T VALUES ('inner')", c2);
              cmd2.ExecuteNonQuery();

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
        Assert.AreEqual(innerChangesVisible, expectInnerChangesVisible);
        Assert.AreEqual(outerChangesVisible, expectOuterChangesVisible);
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
    [Test]
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
      ExecuteSQL("TRUNCATE TABLE Test");

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
          Assert.AreEqual(c1Thread, c2.ServerThread);
        }

        if (complete)
          ts.Complete();
      }

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(dt);
      if (complete)
      {
        Assert.AreEqual(2, dt.Rows.Count);
        Assert.AreEqual("a", dt.Rows[0][0]);
        Assert.AreEqual("b", dt.Rows[1][0]);
      }
      else
      {
        Assert.AreEqual(0, dt.Rows.Count);
      }
    }

    [Test]
    public void ReusingSameConnectionTest()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))");
      ReusingSameConnection(true, true);
      ReusingSameConnection(true, false); ;
      ReusingSameConnection(false, true);
      ReusingSameConnection(false, false);
    }

    /// <summary>
    /// bug#35330 - even if transaction scope has expired, rows can be inserted into
    /// the table, due to race condition with the thread doing rollback
    /// </summary>
    [Test]
    public void ScopeTimeoutWithMySqlHelper()
    {
#if !NETFRAMEWORK
      if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) Assert.Ignore();
#endif
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (id int)");
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
      Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Variation of previous test, with a single connection and maual enlistment.
    /// Checks that  transaction rollback leaves the connection intact (does not close it) 
    /// and  checks that no command is possible after scope has expired and 
    /// rollback by timer thread is finished.
    /// </summary>
    [Test]
    public void AttemptToUseConnectionAfterScopeTimeout()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (id int)");
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
          Assert.Throws<TransactionAbortedException>(() => cmd.ExecuteNonQuery());
        }
        Assert.True(c.State == ConnectionState.Open);
        cmd.CommandText = "select count(*) from Test";
        long count = (long)cmd.ExecuteScalar();
        Assert.AreEqual(0, count);
      }
    }


    /// <summary>
    /// Bug#54681 : Null Reference exception when using transaction
    /// scope in more that one thread
    /// </summary>
    [Test]
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

    [Test]
    public void SnapshotIsolationLevelThrowsNotSupportedException()
    {
      using (MySqlConnection newcon = new MySqlConnection(Connection.ConnectionString))
      {
        newcon.Open();
        var ex = Assert.Throws<NotSupportedException>(() => newcon.BeginTransaction(System.Data.IsolationLevel.Snapshot));
        Assert.AreEqual("Snapshot isolation level is not supported.", ex.Message);
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

    [Test]
    /// <summary>
    ///  Bug #26035791	WRONG ISOLATION LEVEL FOR BEGIN TRANSACTION
    /// </summary>
    public void Bug26035791()
    {
      using (MySqlConnection db = new MySqlConnection(Connection.ConnectionString))
      {

        ExecuteSQL("Create Table TransTest(id int, name varchar(50))");
        ExecuteSQL("INSERT INTO TransTest VALUES(1, 'Test1')");
        db.Open();
        string initialLevel = string.Empty;
        string finalLevel = string.Empty;

        MySqlCommand cmd = new MySqlCommand(@"SHOW VARIABLES WHERE variable_name like '%isolation%'", db);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          initialLevel = reader.GetString(1);
        }

        using (MySqlTransaction transaction = db.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
        {
          MySqlCommand cmd2 = new MySqlCommand(@"INSERT INTO TransTest VALUES(2, 'second')", db, transaction);
          cmd2.ExecuteNonQuery();

          cmd2 = new MySqlCommand(@"select count(*) from TransTest", db, transaction);
          int.TryParse(cmd2.ExecuteScalar().ToString(), out int n1); // If ReadUncommitted is applied we should be able to read inserted record before commit
          Assert.True(n1 == 2);
          transaction.Commit();
        }
        ExecuteSQL("drop table TransTest");
        cmd = new MySqlCommand(@"SHOW VARIABLES WHERE variable_name like '%isolation%'", db);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          finalLevel = reader.GetString(1);
        }
        Assert.AreEqual(initialLevel, finalLevel); // Isolation level should be the same after the transaction
      }
    }

    /// <summary>
    /// Related to bug http://bugs.mysql.com/bug.php?id=71502
    /// </summary>
    [Test]
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

    /// <summary>
    /// Bug #33123597	IF AUTOCOMMIT=0, "SHOW COLLATION" VIA OPEN() WILL CAUSE BEGINTRANSACTION() TO FAIL
    /// </summary>
    [Test]
    public void TransactionWithAutoCommit()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("create table Test(id int)");
      ExecuteSQL("set autocommit = 0;");

      MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      sb.PersistSecurityInfo = true;
      sb.Pooling = true;
      sb.SslMode = MySqlSslMode.Disabled;

      using (var connection = new MySqlConnection(sb.ConnectionString))
      {
        connection.Open();
        var transaction = connection.BeginTransaction();
        Assert.IsNotNull(transaction);
        var myCommand = connection.CreateCommand();
        myCommand.CommandText = "INSERT INTO `Test` VALUES (1);";
        var result = myCommand.ExecuteNonQuery();
        transaction.Commit();
      }

      ExecuteSQL("set autocommit = 1;");
    }

    /// <summary>
    /// Bug #28662512	ASYNC, TRANSACTIONSCOPE.DISPOSE: "ALREADY AN OPEN DATAREADER ASSOCIATED"
    /// </summary>
    [Test]
    [Ignore("If required run manually")]
    public void TransactionScopeDispose()
    {
      ExecuteSQL(@"CREATE TABLE `keyvalue` (
      `id` bigint(20) unsigned NOT NULL,
      `name1` varchar(250),
      `name2` varchar(250),
      `name3` varchar(250),
      `name4` varchar(250),
      PRIMARY KEY(`id`, name1(10))
      ) ENGINE = innodb; ");

      for (int i = 0; i < 1000; i++)
      {
        var id = i < 5 ? i + 1 : i + 1000;
        var sql = $"insert into `keyvalue` values ({id}, md5(rand() * 1000000), md5(rand() * 1000000), md5(rand() * 1000000), md5(rand() * 1000000));";
        ExecuteSQL(sql);
      }
      var cs = $"server={Host};port={Port};Database={Connection.Settings.Database};Uid={Connection.Settings.UserID};password={Connection.Settings.Password};ssl-mode=none; ";
      PerformQueriesAtIntervals(TimeSpan.FromMilliseconds(70),
      connectionString: cs, tableName: "keyvalue")
      .GetAwaiter().GetResult();
    }

    /// <summary>
    /// Bug#34107186 [System.NullReferenceException]
    /// When the connection was Aborted due to an exception, this was closed and hence the transaction couldn't be rolled back.
    /// </summary>
    [Test]
    public void TransactionScopeNullReference()
    {
      Assert.DoesNotThrow(() => PerformSelectThatTimesOut(Connection.ConnectionString));
    }

    private void PerformSelectThatTimesOut(string connectionString)
    {
      using (TransactionScope scope = new TransactionScope())
      {
        using (var connection = new MySqlConnection(connectionString))
        {
          connection.Open();
          using (var cmd = connection.CreateCommand())
          {
            cmd.CommandText = "SELECT * from INFORMATION_SCHEMA.TABLES LIMIT 1; SELECT SLEEP(5);";
            cmd.CommandTimeout = 1;
            cmd.ExecuteNonQuery();
          }
        }
      }
    }
    private static async Task PerformQueriesAtIntervals(TimeSpan interval, string connectionString, string tableName)
    {
      for (int i = 0; i < 151; i++)
      {
        Task.Run(() => PerformQuery(connectionString, tableName));
        await Task.Delay(interval);
      }
    }

    private static async Task PerformQuery(string connectionString, string tableName)
    {
      try
      {
        using (var transactionScope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions()
            {
              IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
              Timeout = TimeSpan.FromMinutes(10)
            },
        TransactionScopeAsyncFlowOption.Enabled))
        {
          await SelectStarLimit1(connectionString, tableName);
          var delay = TimeSpan.FromSeconds(10);
          await Task.Delay(delay);
          transactionScope.Complete();
        }
      }
      catch (Exception e)
      {
        Assert.Fail(e.Message);
      }
    }

    private static async Task SelectStarLimit1(string connectionString, string tableName)
    {
      using (var connection = new MySqlConnection(connectionString))
      using (var command = connection.CreateCommand())
      {
        command.CommandText = $"SELECT * FROM {tableName} LIMIT 1";
        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
      }
    }
  }
}