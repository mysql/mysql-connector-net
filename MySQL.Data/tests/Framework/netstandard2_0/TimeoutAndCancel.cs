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
using Xunit;
using System.Threading;
using System.Data;
using System.Globalization;
using System.IO;

namespace MySql.Data.MySqlClient.Tests
{
  public class TimeoutAndCancel : TestBase
  {
    private delegate void CommandInvokerDelegate(MySqlCommand cmdToRun);
    private ManualResetEvent resetEvent = new ManualResetEvent(false);

    public TimeoutAndCancel(TestFixture fixture) : base (fixture)
    {
    }

    private void CommandRunner(MySqlCommand cmdToRun)
    {
      object o = cmdToRun.ExecuteScalar();
      resetEvent.Set();
      Assert.Null(o);
    }

#if !NETCOREAPP2_0
    [Fact]
    public void CancelSingleQuery()
    {
      // first we need a routine that will run for a bit
      executeSQL(@"CREATE PROCEDURE CancelSingleQuery(duration INT) 
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

      Assert.True(resetEvent.WaitOne(30 * 1000), "timeout");
    }
#endif

    int stateChangeCount;
    [Fact]
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
        catch (Exception ex)
        {
          Assert.True(ex.Message.StartsWith("Fatal", StringComparison.OrdinalIgnoreCase));
        }

        Assert.Equal(1, stateChangeCount);
        Assert.Equal(ConnectionState.Closed, c.State);
      }

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT now() as thetime, database() as db", c);
        using (MySqlDataReader r = cmd.ExecuteReader())
        {
          Assert.True(r.Read());
        }
      }
    }

    void c_StateChange(object sender, StateChangeEventArgs e)
    {
      stateChangeCount++;
    }

    [Fact(Skip="Fix This")]
    public void TimeoutExpiring()
    {
      //DateTime start = DateTime.Now;
      //MySqlCommand cmd = new MySqlCommand("SELECT SLEEP(5)", Connection);
      //cmd.CommandTimeout = 1;
      //Exception ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
      //Assert.True(ex.Message.StartsWith("Fatal error encountered during", StringComparison.OrdinalIgnoreCase), "Message is wrong " + ex.Message);
    }

    [Fact]
    public void TimeoutNotExpiring()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT SLEEP(1)", Connection);
      cmd.CommandTimeout = 2;
      cmd.ExecuteNonQuery();
    }

    [Fact]
    public void TimeoutNotExpiring2()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT SLEEP(1)", Connection);
      cmd.CommandTimeout = 0; // infinite timeout
      cmd.ExecuteNonQuery();
    }

    [Fact(Skip="Fix This")]
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
      //Assert.Equal(10, res);
    }

    [Fact]
    public void CancelSelect()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(20))");
      for (int i = 0; i < 1000; i++)
        executeSQL("INSERT INTO Test VALUES (NULL, 'my string')");

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
            Assert.True(ex.Number == (int)MySqlErrorCode.QueryInterrupted);
            if (rows < 1000)
            {
              bool readOK = reader.Read();
              Assert.False(readOK);
            }
          }

        }
      }
      Assert.True(rows < 1000);
    }

    /// <summary>
    /// Bug #40091	mysql driver 5.2.3.0 connection pooling issue
    /// </summary>
    [Fact (Skip = "Issue")]
    public void ConnectionStringModifiedAfterCancel()
    {
      string connStr = "server=localhost;userid=root;pwd=;database=test;port=3305;persist security info=true";
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
          Assert.True(ex.InnerException is TimeoutException);
          Assert.True(c.State == ConnectionState.Open);
        }
        string connStr2 = c.ConnectionString.ToLower(CultureInfo.InvariantCulture);
        Assert.Equal(connStr1.ToLower(CultureInfo.InvariantCulture), connStr2);
        c.Close();        
      }
     
    }


    /// <summary>
    /// Bug #45978	Silent problem when net_write_timeout is exceeded
    /// </summary>
    [Fact]
    public void NetWriteTimeoutExpiring()
    {
      executeSQL("CREATE TABLE Test(id int, blob1 longblob)");
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
              Assert.False(!reader.Read(),"unexpected 'false' from reader.Read");
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

          throw e;
        }

        // IT is relatively hard to predict where
        Console.WriteLine("Warning: all reads completed!");
        Assert.True(i == rows);
      }
    }
  }
}
