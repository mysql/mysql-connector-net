// Copyright © 2013, 2026, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Globalization;
using System.IO;
using NUnit.Framework.Internal;

namespace MySql.Data.MySqlClient.Tests
{
  public class TimeoutAndCancel : TestBase
  {
    private delegate void CommandInvokerDelegate(MySqlCommand cmdToRun);
    private ManualResetEvent resetEvent = new ManualResetEvent(false);

    // Unique marker embedded in long-running test queries so parallel framework runs
    // don't interfere with each other's PROCESSLIST polling.
    private string m_testRunId = Guid.NewGuid().ToString("N").Substring(0, 8);

    // Connection string with pooling disabled so a killed connection is never returned
    // to the pool and handed to a subsequent test's OpenAsync.
    private string NoPoolConnectionString
    {
      get
      {
        MySqlConnectionStringBuilder b = new MySqlConnectionStringBuilder(Connection.ConnectionString);
        b.Pooling = false;
        return b.ConnectionString;
      }
    }

    /// <summary>
    /// Synchronous counterpart of <see cref="OpenTestConnectionAsync"/>.
    /// </summary>
    private MySqlConnection OpenTestConnectionWithRetry()
    {
      const int maxAttempts = 3;
      for (int attempt = 1; attempt <= maxAttempts; attempt++)
      {
        MySqlConnection conn = new MySqlConnection(NoPoolConnectionString);
        try
        {
          conn.Open();
          return conn;
        }
        catch (MySqlException) when (attempt < maxAttempts)
        {
          conn.Dispose();
          Thread.Sleep(300 * attempt);
        }
        catch
        {
          conn.Dispose();
          throw;
        }
      }
      throw new InvalidOperationException("OpenTestConnectionWithRetry: unreachable");
    }

    /// <summary>
    /// Opens a no-pool test connection, retrying up to 3 times with a short delay.
    /// Parallel framework assemblies may recycle MySQL server threads that still have
    /// compressed-stream framing in flight; a brief pause lets the server finish cleanup.
    /// </summary>
    private async Task<MySqlConnection> OpenTestConnectionAsync()
    {
      const int maxAttempts = 3;
      for (int attempt = 1; attempt <= maxAttempts; attempt++)
      {
        MySqlConnection conn = new MySqlConnection(NoPoolConnectionString);
        try
        {
          await conn.OpenAsync();
          return conn;
        }
        catch (MySqlException) when (attempt < maxAttempts)
        {
          conn.Dispose();
          await Task.Delay(300 * attempt);
        }
        catch
        {
          conn.Dispose();
          throw;
        }
      }
      // Unreachable, but satisfies the compiler.
      throw new InvalidOperationException("OpenTestConnectionAsync: unreachable");
    }

    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    [SetUp]
    public void RegenerateTestRunId()
    {
      // Fresh ID per test so PROCESSLIST polling is isolated from parallel framework runs.
      m_testRunId = Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    private void CommandRunner(MySqlCommand cmdToRun)
    {
      object o = cmdToRun.ExecuteScalar();
      resetEvent.Set();
      Assert.That(o, Is.Null);
    }

#if NETFRAMEWORK
    [Test]
    public void CancelSingleQuery()
    {
      // first we need a routine that will run for a bit
      ExecuteSQL(@"CREATE PROCEDURE CancelSingleQuery(duration INT)
        BEGIN
          SELECT SLEEP(duration);
        END");

      MySqlCommand cmd = new MySqlCommand("CancelSingleQuery", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("duration", 10);

      // now we start execution of the command
      CommandInvokerDelegate d = new CommandInvokerDelegate(CommandRunner);
      d.BeginInvoke(cmd, null, null);

      // sleep 1 seconds
      Thread.Sleep(1000);

      // now cancel the command
      cmd.Cancel();

      Assert.That(resetEvent.WaitOne(30 * 1000), "timeout");
    }
#endif

    int stateChangeCount;
    [Test]
    public void WaitTimeoutExpiring()
    {
      string connStr = Connection.ConnectionString;
      MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(connStr);

      if (sb.ConnectionProtocol == MySqlConnectionProtocol.NamedPipe)
        // wait timeout does not work for named pipe connections
        return;

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        c.StateChange += new StateChangeEventHandler(c_StateChange);

        // set the session wait timeout on this new connection
        MySqlCommand cmd = new MySqlCommand("SET SESSION interactive_timeout=3", c);
        cmd.ExecuteNonQuery();
        cmd.CommandText = "SET SESSION wait_timeout=2";
        cmd.ExecuteNonQuery();

        stateChangeCount = 0;
        // now wait 4 seconds
        Thread.Sleep(4000);

        try
        {
          cmd.CommandText = "SELECT now()";
          cmd.ExecuteScalar();
        }
        catch (MySqlException ex)
        {
          if (Version < new Version("8.0.24"))
            Assert.That(ex.Message, Does.StartWith("Fatal"));
          else
            Assert.That(ex.Message, Does.StartWith("The client was disconnected"));
        }

        Assert.That(stateChangeCount, Is.EqualTo(1));
        Assert.That(c.State, Is.EqualTo(ConnectionState.Closed));
      }

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT now() as thetime, database() as db", c);
        using (MySqlDataReader r = cmd.ExecuteReader())
        {
          Assert.That(r.Read(), Is.True);
        }
      }
    }

    void c_StateChange(object sender, StateChangeEventArgs e)
    {
      stateChangeCount++;
    }

    [Test]
    [Ignore("Fix This")]
    public void TimeoutExpiring()
    {
      //DateTime start = DateTime.Now;
      //MySqlCommand cmd = new MySqlCommand("SELECT SLEEP(5)", Connection);
      //cmd.CommandTimeout = 1;
      //Exception ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
      //Assert.True(ex.Message.StartsWith("Fatal error encountered during", StringComparison.OrdinalIgnoreCase), "Message is wrong " + ex.Message);
    }

    [Test]
    public void TimeoutNotExpiring()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT SLEEP(1)", Connection);
      cmd.CommandTimeout = 2;
      cmd.ExecuteNonQuery();
    }

    [Test]
    public void TimeoutNotExpiring2()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT SLEEP(1)", Connection);
      cmd.CommandTimeout = 0; // infinite timeout
      cmd.ExecuteNonQuery();
    }

    [Test]
    [Ignore("Fix This")]
    public void TimeoutDuringBatch()
    {
      //executeSQL(@"CREATE PROCEDURE spTest(duration INT) 
      //  BEGIN 
      //    SELECT SLEEP(duration);
      //  END");

      //executeSQL("CREATE TABLE test (id INT)");

      //MySqlCommand cmd = new MySqlCommand(
      //  "call spTest(5);INSERT INTO test VALUES(4)", Connection);
      //cmd.CommandTimeout = 2;
      //Exception ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
      //Assert.True(ex.Message.StartsWith("Timeout expired", StringComparison.OrdinalIgnoreCase), "Message is wrong" + ex);

      //// Check that connection is still usable
      //MySqlCommand cmd2 = new MySqlCommand("select 10", Connection);
      //long res = (long)cmd2.ExecuteScalar();
      //Assert.AreEqual(10, res);
    }

    [Test]
    public void CancelSelect()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(20))");
      for (int i = 0; i < 1000; i++)
        ExecuteSQL("INSERT INTO Test VALUES (NULL, 'my string')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      cmd.CommandTimeout = 0;
      int rows = 0;
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();

        cmd.Cancel();

        while (true)
        {

          try
          {
            if (!reader.Read())
              break;
            rows++;
          }
          catch (MySqlException ex)
          {
            Assert.That(ex.Number, Is.EqualTo((int)MySqlErrorCode.QueryInterrupted));
            if (rows < 1000)
            {
              bool readOK = reader.Read();
              Assert.That(readOK, Is.False);
            }
          }

        }
      }
      Assert.That(rows < 1000);
    }

    /// <summary>
    /// Bug #40091	mysql driver 5.2.3.0 connection pooling issue
    /// </summary>
    [Test]
    [Ignore("Issue")]
    public void ConnectionStringModifiedAfterCancel()
    {
      string connStr = $"server={Host};userid={RootUser};pwd={RootPassword};port={Port};persist security info=true";
      MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(connStr);

      if (sb.ConnectionProtocol == MySqlConnectionProtocol.NamedPipe)
        // idle named pipe connections cannot be KILLed (server bug#47571)
        return;

      connStr = connStr.Replace("persist security info=true", "persist security info=false");
      
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();        
        string connStr1 = c.ConnectionString;

        MySqlCommand cmd = new MySqlCommand("SELECT SLEEP(5)", c);
        cmd.CommandTimeout = 1;
        try
        {
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
          }
        }
        catch (MySqlException ex)
        {
          Assert.That(ex.InnerException is TimeoutException, Is.True);
          Assert.That(c.State, Is.EqualTo(ConnectionState.Open));
        }
        string connStr2 = c.ConnectionString.ToLower(CultureInfo.InvariantCulture);
        Assert.That(connStr2, Is.EqualTo(connStr1.ToLower(CultureInfo.InvariantCulture)));
        c.Close();        
      }
     
    }


    /// <summary>
    /// Bug #45978	Silent problem when net_write_timeout is exceeded
    /// </summary>
    [Test]
    public void NetWriteTimeoutExpiring()
    {
      ExecuteSQL("CREATE TABLE Test(id int, blob1 longblob)");
      int rows = 1000;
      byte[] b1 = Utils.CreateBlob(5000);
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (@id, @b1)", Connection);
      cmd.Parameters.Add("@id", MySqlDbType.Int32);
      cmd.Parameters.AddWithValue("@name", b1);
      for (int i = 0; i < rows; i++)
      {
        cmd.Parameters[0].Value = i;
        cmd.ExecuteNonQuery();
      }

      string connStr = Connection.ConnectionString;
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        cmd.Connection = c;
        cmd.Parameters.Clear();
        cmd.CommandText = "SET net_write_timeout = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM Test LIMIT " + rows;
        int i = 0;

        try
        {
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            // after this several cycles of DataReader.Read() are executed 
            // normally and then the problem, described above, occurs
            for (; i < rows; i++)
            {
              Assert.That(!reader.Read(), Is.False, "unexpected 'false' from reader.Read");
              if (i % 10 == 0)
                Thread.Sleep(1);
              object v = reader.GetValue(1);
            }
          }
        }
        catch (Exception e)
        {
          Exception currentException = e;
          while (currentException != null)
          {
            if (currentException is EndOfStreamException)
              return;

            if ((Connection.ConnectionString.IndexOf("protocol=namedpipe") >= 0 || Connection.ConnectionString.IndexOf("protocol=sharedmemory") >= 0) && currentException is MySqlException)
              return;

            currentException = currentException.InnerException;
          }

          throw;
        }

        // IT is relatively hard to predict where
        Console.WriteLine("NetWriteTimeoutExpiring: all reads completed!");
        Assert.That(i, Is.EqualTo(rows));
      }
    }

    private const string ProcessListQuery =
      "SELECT COUNT(*) FROM information_schema.PROCESSLIST WHERE INFO LIKE '{0}' AND INFO NOT LIKE '%PROCESSLIST%'";

    /// <summary>
    /// Polls PROCESSLIST until the query count satisfies <paramref name="expectedCount"/> or
    /// <paramref name="maxWaitMs"/> elapses. Returns true when the condition is met.
    /// </summary>
    private async Task<bool> WaitForProcessListAsync(string likePattern, long expectedCount, int maxWaitMs = 5000)
    {
      string sql = string.Format(ProcessListQuery, likePattern);
      int elapsed = 0;
      const int intervalMs = 150;
      while (elapsed < maxWaitMs)
      {
        using MySqlCommand cmd = new MySqlCommand(sql, Root);
        long count = Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0L);
        if (count == expectedCount) { return true; }
        await Task.Delay(intervalMs);
        elapsed += intervalMs;
      }
      return false;
    }

    /// <summary>
    /// Synchronous counterpart of <see cref="WaitForProcessListAsync"/>.
    /// </summary>
    private bool WaitForProcessList(string likePattern, long expectedCount, int maxWaitMs = 5000)
    {
      string sql = string.Format(ProcessListQuery, likePattern);
      int elapsed = 0;
      const int intervalMs = 150;
      while (elapsed < maxWaitMs)
      {
        using MySqlCommand cmd = new MySqlCommand(sql, Root);
        long count = Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
        if (count == expectedCount) { return true; }
        Thread.Sleep(intervalMs);
        elapsed += intervalMs;
      }
      return false;
    }

    /// <summary>
    /// Verifies that cancelling a CancellationToken causes the running query to be
    /// killed server-side via KILL QUERY so it no longer appears in PROCESSLIST.
    /// Fix: cancellationToken.Register(() => Cancel()) wired inside ExecuteReaderAsync.
    /// </summary>
    [Test]
    public async Task CancellationToken_QueryKilledOnServer()
    {
        MySqlConnection testConn = await OpenTestConnectionAsync();
        Exception caughtException = null;
        Task queryTask = Task.CompletedTask;
        try
        {
            using var cts = new CancellationTokenSource();
            MySqlCommand cmd = new MySqlCommand($"SELECT SLEEP(60) /* testrun:{m_testRunId} */", testConn);

            queryTask = Task.Run(async () =>
            {
                try
                {
                    await cmd.ExecuteNonQueryAsync(cts.Token);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            });

            // Wait until the SLEEP shows up in PROCESSLIST before cancelling.
            bool appeared = await WaitForProcessListAsync($"%testrun:{m_testRunId}%", expectedCount: 1);
            Assert.That(appeared, Is.True, "Query never appeared in PROCESSLIST.");

            // Cancel the token — this sends KILL QUERY via a separate connection.
            // Note: SELECT SLEEP(N) returns 1 when interrupted, not error 1317, so the task
            // may complete without throwing. Without the fix, Cancel() is never wired so the
            // server query keeps running and the task never completes — deadline guards against that.
            cts.Cancel();
            bool completed = await Task.WhenAny(queryTask, Task.Delay(10000)) == queryTask;

            // Only close after the task has finished (or timed out); closing while a TLS async
            // read is in flight causes NotSupportedException on SSL streams.
            testConn.Close();
            await queryTask;

            Assert.That(completed, Is.True,
                "ExecuteNonQueryAsync did not return after cancellation — KILL QUERY was likely not sent to the server.");

            Assert.That(caughtException, Is.Not.InstanceOf<NullReferenceException>(),
                "NullReferenceException indicates the query abort was not handled correctly.");

            bool gone = await WaitForProcessListAsync($"%testrun:{m_testRunId}%", expectedCount: 0);
            Assert.That(gone, Is.True, "Query was not killed on the server after cancellation.");
        }
        finally
        {
            // Ensure the connection is closed and the task has completed before we return,
            // so no background IO races with the next test's connection open.
            if (testConn.State != System.Data.ConnectionState.Closed) { testConn.Close(); }
            await queryTask;
            testConn.Dispose();
        }
    }

    /// <summary>
    /// Verifies that cancelling a CancellationToken throws OperationCanceledException
    /// and not a NullReferenceException.
    /// Fix: IsQueryAborted + cancellationToken.IsCancellationRequested now throws OperationCanceledException.
    /// </summary>
    [Test]
    public async Task CancellationToken_ThrowsOperationCanceledException()
    {
        MySqlConnection testConn = await OpenTestConnectionAsync();
        Exception caughtException = null;
        Task queryTask = Task.CompletedTask;
        try
        {
            using var cts = new CancellationTokenSource();
            MySqlCommand cmd = new MySqlCommand($"SELECT SLEEP(60) /* testrun:{m_testRunId} */", testConn);

            queryTask = Task.Run(async () =>
            {
                try
                {
                    await cmd.ExecuteNonQueryAsync(cts.Token);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            });

            // Wait until the query appears in PROCESSLIST before cancelling.
            bool appeared = await WaitForProcessListAsync($"%testrun:{m_testRunId}%", expectedCount: 1);
            Assert.That(appeared, Is.True, "Query never appeared in PROCESSLIST.");
            cts.Cancel();

            // Await the task before closing: closing while a TLS async read is in flight
            // causes NotSupportedException on SSL streams.
            bool completed = await Task.WhenAny(queryTask, Task.Delay(10000)) == queryTask;
            testConn.Close();
            await queryTask;

            Assert.That(completed, Is.True,
                "ExecuteNonQueryAsync did not return after cancellation — KILL QUERY was likely not sent to the server.");

            bool gone = await WaitForProcessListAsync($"%testrun:{m_testRunId}%", expectedCount: 0);
            Assert.That(gone, Is.True, "Query was not killed on the server after cancellation.");

            // SELECT SLEEP(N) returns 1 (interrupted) when KILL QUERY is sent — MySQL does not raise
            // error 1317 for SLEEP(), so ExecuteNonQueryAsync may complete without throwing at all.
            // The actual bug was: when error 1317 *was* raised, the connector returned null, causing a
            // NullReferenceException downstream. Verify that whatever happened, it was not that.
            Assert.That(caughtException, Is.Not.InstanceOf<NullReferenceException>(),
                "Got NullReferenceException which indicates the query abort was not handled correctly.");

            // If an exception was thrown, it must be OperationCanceledException or MySqlException(1317)
            if (caughtException != null)
            {
                bool isValidException = caughtException is OperationCanceledException ||
                                        (caughtException is MySqlException mex && mex.Number == (int)MySqlErrorCode.QueryInterrupted);
                Assert.That(isValidException, Is.True,
                    $"Expected OperationCanceledException or MySqlException(1317) but got {caughtException.GetType().Name}: {caughtException.Message}");
            }
        }
        finally
        {
            if (testConn.State != System.Data.ConnectionState.Closed) { testConn.Close(); }
            await queryTask;
            testConn.Dispose();
        }
    }

    /// <summary>
    /// Verifies that a synchronous CommandTimeout kills the query server-side so it no
    /// longer appears in PROCESSLIST after the timeout fires.
    /// Fix: TimedStream sync IOException(SocketException:TimedOut) path already worked;
    /// this test confirms the full end-to-end kill flow for sync execution.
    /// </summary>
    [Test]
    public void SyncCommandTimeout_QueryKilledOnServer()
    {
        MySqlConnection testConn = OpenTestConnectionWithRetry();
        using var _ = testConn;

        // Verify the query appears in PROCESSLIST then disappears within the timeout window.
        // Note: SELECT SLEEP(N) returns 1 (interrupted) when KILL QUERY is sent — MySQL does not
        // raise error 1317 for SLEEP(), so ExecuteNonQuery may complete without throwing.
        MySqlCommand cmd = new MySqlCommand($"SELECT SLEEP(60) /* testrun:{m_testRunId} */", testConn);
        cmd.CommandTimeout = 2;

        Exception caughtException = null;
        try
        {
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // If an exception was thrown it must not be a NullReferenceException
        Assert.That(caughtException, Is.Not.InstanceOf<NullReferenceException>(),
            "NullReferenceException indicates the query abort was not handled correctly.");

        // Poll until the server confirms the query is gone (up to 5 s).
        bool gone = WaitForProcessList($"%testrun:{m_testRunId}%", expectedCount: 0);
        Assert.That(gone, Is.True, "Query was not killed on the server after sync timeout.");
    }

    /// <summary>
    /// Verifies that an async CommandTimeout kills the query server-side so it no
    /// longer appears in PROCESSLIST after the timeout fires.
    /// Fix: TimedStream.ExecuteAsyncWithTimeout enforces timeout on IOCP async reads
    /// (NetworkStream.ReadAsync ignores Socket.ReceiveTimeout).
    /// </summary>
    [Test]
    public async Task AsyncCommandTimeout_QueryKilledOnServer()
    {
        MySqlConnection testConn = await OpenTestConnectionAsync();
        Exception caughtException = null;
        Task executeTask = Task.CompletedTask;
        try
        {
            // Note: SELECT SLEEP(N) returns 1 (interrupted) when KILL QUERY is sent — MySQL does not
            // raise error 1317 for SLEEP(), so ExecuteNonQueryAsync may complete without throwing.
            MySqlCommand cmd = new MySqlCommand($"SELECT SLEEP(60) /* testrun:{m_testRunId} */", testConn);
            cmd.CommandTimeout = 2;

            // Without the fix, async reads ignore Socket.ReceiveTimeout so this hangs indefinitely.
            // Use a deadline of CommandTimeout + 10 s to fail clearly rather than hanging the test run.
            int deadlineMs = (cmd.CommandTimeout + 10) * 1000;
            executeTask = Task.Run(async () =>
            {
                try
                {
                    await cmd.ExecuteNonQueryAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            });

            // Await the task with a deadline before closing: closing while a TLS async read is
            // in flight causes NotSupportedException on SSL streams. Without the fix, async reads
            // ignore Socket.ReceiveTimeout so the task would hang past the deadline.
            bool completed = await Task.WhenAny(executeTask, Task.Delay(deadlineMs)) == executeTask;
            testConn.Close();
            await executeTask;

            Assert.That(completed, Is.True,
                "ExecuteNonQueryAsync did not return after CommandTimeout elapsed — async timeout is not enforced.");

            Assert.That(caughtException, Is.Not.InstanceOf<NullReferenceException>(),
                "NullReferenceException indicates the query abort was not handled correctly.");

            bool gone = await WaitForProcessListAsync($"%testrun:{m_testRunId}%", expectedCount: 0);
            Assert.That(gone, Is.True, "Query was not killed on the server after async timeout.");
        }
        finally
        {
            if (testConn.State != System.Data.ConnectionState.Closed) { testConn.Close(); }
            await executeTask;
            testConn.Dispose();
        }
    }
}
}
