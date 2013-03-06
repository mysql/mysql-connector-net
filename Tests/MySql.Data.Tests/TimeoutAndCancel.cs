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
using System.IO;
using System.Threading;
using NUnit.Framework;
using System.Globalization;

namespace MySql.Data.MySqlClient.Tests
{
  [TestFixture]
  public class TimeoutAndCancel : BaseTest
  {
    private delegate void CommandInvokerDelegate(MySqlCommand cmdToRun);
    private ManualResetEvent resetEvent = new ManualResetEvent(false);

    private void CommandRunner(MySqlCommand cmdToRun)
    {
      object o = cmdToRun.ExecuteScalar();
      resetEvent.Set();
      Assert.IsNull(o);
    }

    [Test]
    public void CancelSingleQuery()
    {
      if (Version < new Version(5, 0)) return;

      // first we need a routine that will run for a bit
      execSQL(@"CREATE PROCEDURE spTest(duration INT) 
				BEGIN 
					SELECT SLEEP(duration);
				END");

      MySqlCommand cmd = new MySqlCommand("spTest", conn);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("duration", 10);

      // now we start execution of the command
      CommandInvokerDelegate d = new CommandInvokerDelegate(CommandRunner);
      d.BeginInvoke(cmd, null, null);

      // sleep 1 seconds
      Thread.Sleep(1000);

      // now cancel the command
      cmd.Cancel();

      resetEvent.WaitOne();
    }

    int stateChangeCount;
    [Test]
    public void WaitTimeoutExpiring()
    {
      string connStr = GetConnectionString(true);
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
        catch (Exception ex)
        {
          Assert.IsTrue(ex.Message.StartsWith("Fatal", StringComparison.OrdinalIgnoreCase));
        }

        Assert.AreEqual(1, stateChangeCount);
        Assert.AreEqual(ConnectionState.Closed, c.State);
      }

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT now() as thetime, database() as db", c);
        using (MySqlDataReader r = cmd.ExecuteReader())
        {
          Assert.IsTrue(r.Read());
        }
      }
    }

    void c_StateChange(object sender, StateChangeEventArgs e)
    {
      stateChangeCount++;
    }

    [Test]
    public void TimeoutExpiring()
    {
      if (version < new Version(5, 0)) return;

      DateTime start = DateTime.Now;
      try
      {
        MySqlCommand cmd = new MySqlCommand("SELECT SLEEP(200)", conn);
        cmd.CommandTimeout = 1;
        cmd.ExecuteReader(CommandBehavior.SingleRow);
        Assert.Fail("Should not get to this point");
      }
      catch (MySqlException ex)
      {
        TimeSpan ts = DateTime.Now.Subtract(start);
        Assert.IsTrue(ts.TotalSeconds <= 3);
        Assert.IsTrue(ex.Message.StartsWith("Timeout expired", StringComparison.OrdinalIgnoreCase), "Message is wrong " + ex.Message);
      }

      long x = (long)(new MySqlCommand("select 10", conn).ExecuteScalar());
      Assert.AreEqual(10, x);

    }

    [Test]
    public void TimeoutNotExpiring()
    {
      if (Version < new Version(5, 0)) return;

      MySqlCommand cmd = new MySqlCommand("SELECT SLEEP(1)", conn);
      cmd.CommandTimeout = 2;
      cmd.ExecuteNonQuery();
    }

    [Test]
    public void TimeoutNotExpiring2()
    {
      if (Version < new Version(5, 0)) return;

      MySqlCommand cmd = new MySqlCommand("SELECT SLEEP(1)", conn);
      cmd.CommandTimeout = 0; // infinite timeout
      cmd.ExecuteNonQuery();
    }

    [Test]
    public void TimeoutDuringBatch()
    {
      if (Version < new Version(5, 0)) return;

      execSQL(@"CREATE PROCEDURE spTest(duration INT) 
				BEGIN 
					SELECT SLEEP(duration);
				END");

      execSQL("CREATE TABLE test (id INT)");

      MySqlCommand cmd = new MySqlCommand(
        "call spTest(5);INSERT INTO test VALUES(4)", conn);
      cmd.CommandTimeout = 2;
      try
      {
        cmd.ExecuteNonQuery();
        Assert.Fail("This should have timed out");
      }
      catch (MySqlException ex)
      {
        Assert.IsTrue(ex.Message.StartsWith("Timeout expired", StringComparison.OrdinalIgnoreCase), "Message is wrong" + ex);
      }

      // Check that connection is still usable
      MySqlCommand cmd2 = new MySqlCommand("select 10", conn);
      long res = (long)cmd2.ExecuteScalar();
      Assert.AreEqual(10, res);
    }

    [Test]
    public void CancelSelect()
    {
      if (Version < new Version(5, 0)) return;

      execSQL("CREATE TABLE Test (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(20))");
      for (int i = 0; i < 1000; i++)
        execSQL("INSERT INTO Test VALUES (NULL, 'my string')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
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
            Assert.IsTrue(ex.Number == (int)MySqlErrorCode.QueryInterrupted);
            if (rows < 1000)
            {
              bool readOK = reader.Read();
              Assert.IsFalse(readOK);
            }
          }

        }
      }
      Assert.IsTrue(rows < 1000);
    }

    /// <summary>
    /// Bug #40091	mysql driver 5.2.3.0 connection pooling issue
    /// </summary>
    [Test]
    public void ConnectionStringModifiedAfterCancel()
    {
      if (Version.Major < 5) return;

      string connStr = GetPoolingConnectionString();
      MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(connStr);

      if (sb.ConnectionProtocol == MySqlConnectionProtocol.NamedPipe)
        // idle named pipe connections cannot be KILLed (server bug#47571)
        return;

      connStr = connStr.Replace("persist security info=true", "persist security info=false");

      int threadId;
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        threadId = c.ServerThread;
        string connStr1 = c.ConnectionString;

        MySqlCommand cmd = new MySqlCommand("SELECT SLEEP(2)", c);
        cmd.CommandTimeout = 1;

        try
        {
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
          }
        }
        catch (MySqlException ex)
        {
          Assert.IsTrue(ex.InnerException is TimeoutException);
          Assert.IsTrue(c.State == ConnectionState.Open);
        }
        string connStr2 = c.ConnectionString.ToLower(CultureInfo.InvariantCulture);
        Assert.AreEqual(connStr1.ToLower(CultureInfo.InvariantCulture), connStr2);
      }
      execSQL("kill " + threadId);
    }


    /// <summary>
    /// Bug #45978	Silent problem when net_write_timeout is exceeded
    /// </summary>
    [Test]
    public void NetWriteTimeoutExpiring()
    {
      // net_write_timeout did not apply to named pipes connections before MySQL 5.1.41.
      if (Version < new Version(5, 1, 41) && (GetConnectionInfo().IndexOf("protocol=namedpipe") >= 0 || GetConnectionInfo().IndexOf("protocol=sharedmemory") >= 0)) return;

      execSQL("CREATE TABLE Test(id int, blob1 longblob)");
      int rows = 1000;
      byte[] b1 = Utils.CreateBlob(5000);
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (@id, @b1)", conn);
      cmd.Parameters.Add("@id", MySqlDbType.Int32);
      cmd.Parameters.AddWithValue("@name", b1);
      for (int i = 0; i < rows; i++)
      {
        cmd.Parameters[0].Value = i;
        cmd.ExecuteNonQuery();
      }

      string connStr = GetConnectionString(true);
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

              if (!reader.Read())
                Assert.Fail("unexpected 'false' from reader.Read");
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

            if ((GetConnectionInfo().IndexOf("protocol=namedpipe") >= 0 || GetConnectionInfo().IndexOf("protocol=sharedmemory") >= 0) && currentException is MySqlException)
              return;

            currentException = currentException.InnerException;
          }

          throw e;
        }

        // IT is relatively hard to predict where
        Console.WriteLine("Warning: all reads completed!");
        Assert.IsTrue(i == rows);
      }
    }
  }

  #region Configs

  public class TimeoutAndCancelSocketCompressed : TimeoutAndCancel
  {
    protected override string GetConnectionInfo()
    {
      return String.Format("port={0};compress=true", port);
    }
  }
#if !CF
  [Category("Pipe")]
  public class TimeoutAndCancelPipe : TimeoutAndCancel
  {
    protected override string GetConnectionInfo()
    {
      return String.Format("protocol=namedpipe;pipe name={0}", pipeName);
    }
  }

  [Category("SharedMemory")]
  public class TimeoutAndCancelSharedMemory : TimeoutAndCancel
  {
    protected override string GetConnectionInfo()
    {
      return String.Format("protocol=sharedmemory; shared memory name={0}",
        memoryName);
    }
  }
#endif

  #endregion

}
