// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using Xunit;
using System.Diagnostics;
using MySql.Data;

namespace MySql.Replication.Tests
{
 public class ReplicationTest : IUseFixture<SetUp>, IDisposable
  {

   private SetUp st;

   public void SetFixture(SetUp data)
    {
      st = data;
    }

   public void Dispose()
   {
     MySqlConnectionStringBuilder connString = new MySqlConnectionStringBuilder(st.ConnectionStringRootMaster);
     connString.Database = st.databaseName;
     using (MySqlConnection conn = new MySqlConnection(connString.ToString()))
     {
       conn.Open();
       st.ExecuteNonQuery(conn, "DELETE FROM orders");
       st.ExecuteNonQuery(conn, "DELETE FROM order_details");
     }
   }

    /// <summary>
    /// Validates that the slave is readonly
    /// </summary>
    [Fact]
    public void _SlaveAsReadOnly()
    {
      using (MySqlConnection conn = new MySqlConnection(st.ConnectionStringSlave))
      {
        conn.Open();
        MySqlException ex = Assert.Throws<MySqlException>(() => st.ExecuteNonQuery(conn, "INSERT INTO orders VALUES(null, 1, 'James')"));       
        // Error 1290: The MySQL server is running with the --read-only option so
        // it cannot execute this statement
        Assert.Equal(1290, ex.Number);        
      }
    }

    /// <summary>
    /// Validates that each request for a connection is switched between the
    /// master and slave; also validates that all non-read statements are sent
    /// to the master
    /// </summary>
    [Fact]
    public void RoundRobinWritting()
    {
      using (MySqlConnection conn = new MySqlConnection(st.ConnectionString))
      {
        conn.Open();

        st.ExecuteNonQuery(conn, "INSERT INTO orders VALUES(null, 1, 'James')");
        st.ExecuteNonQuery(conn, "INSERT INTO order_details VALUES(1, 1, 1, 0, 0)");
        st.ExecuteNonQuery(conn, "INSERT INTO order_details VALUES(1, 2, 1, 0, 0)");

        Assert.Equal(st.slavePort, GetPort(conn));
        Assert.Equal(st.masterPort, GetPort(conn));
        Assert.Equal(st.slavePort, GetPort(conn));
        Assert.Equal(st.masterPort, GetPort(conn));

        st.ExecuteNonQuery(conn, "INSERT INTO order_details VALUES(1, 3, 1, 0, 0)");
      }
    }

    /// <summary>
    /// Test for a master connection failure
    /// </summary>
    [Fact]
    public void RoundRobinReadOnly()
    {
      using (MySqlConnection conn = new MySqlConnection("server=Group2;"))
      {
        conn.Open();

        
         MySqlException ex = Assert.Throws<MySqlException>(() => st.ExecuteNonQuery(conn, "INSERT INTO orders VALUES(null, 1, 'James')"));
        
        Assert.Equal(Resources.Replication_NoAvailableServer, ex.Message);

        int currentPort = GetPort(conn);
        Assert.True(currentPort != (currentPort = GetPort(conn)), "1st try");
        Assert.True(currentPort != (currentPort = GetPort(conn)), "2nd try");
        Assert.True(currentPort != (currentPort = GetPort(conn)), "3rd try");
        Assert.True(currentPort != (currentPort = GetPort(conn)), "4th try");
      }
    }

    /// <summary>
    /// Validates that data inserted in master is replicated into slave
    /// </summary>
    [Fact]
    public void RoundRobinValidateSlaveData()
    {
      using (MySqlConnection conn = new MySqlConnection(st.ConnectionString))
      {
        conn.Open();

        st.ExecuteNonQuery(conn, "INSERT INTO orders VALUES(null, 2, 'Bruce')");
      }
      
      // Waits for replication on slave
      System.Threading.Thread.Sleep(3000);

      // validates data on slave
      using (MySqlConnection slaveConn = new MySqlConnection(st.ConnectionStringSlave))
      {
        slaveConn.Open();
        MySqlDataReader dr = st.ExecuteQuery(slaveConn, "SELECT * FROM orders");
        Assert.True(dr.Read());
        Assert.Equal(2, dr.GetValue(1));
        Assert.Equal("Bruce", dr.GetValue(2));
      }
    }

    [Fact]
    public void ValidateLogging()
    {
      MySqlTrace.Listeners.Clear();
      MySqlTrace.Switch.Level = SourceLevels.All;
      MySql.Data.MySqlClient.Tests.GenericListener listener = new MySql.Data.MySqlClient.Tests.GenericListener();
      MySqlTrace.Listeners.Add(listener);
      using (MySqlConnection conn = new MySqlConnection(st.ConnectionString ))
      {
        conn.Open();

        st.ExecuteNonQuery(conn, "INSERT INTO orders VALUES(null, 2, 'Bruce')");
        st.ExecuteNonQuery(conn, "INSERT INTO orders VALUES(null, 1, 'James')");
        st.ExecuteNonQuery(conn, "INSERT INTO order_details VALUES(1, 1, 1, 0, 0)");
        st.ExecuteNonQuery(conn, "INSERT INTO order_details VALUES(1, 2, 1, 0, 0)");
      }

      // Waits for replication on slave
      System.Threading.Thread.Sleep(3000);

      // validates data on slave
      using (MySqlConnection slaveConn = new MySqlConnection(st.ConnectionStringSlave))
      {
        slaveConn.Open();
        MySqlDataReader dr = st.ExecuteQuery(slaveConn, "SELECT * FROM orders");
        Assert.True(dr.Read());
        Assert.Equal(2, dr.GetValue(1));
        Assert.Equal("Bruce", dr.GetValue(2));
      }
      Debug.WriteLine("Start of tracing");
      foreach (string s in listener.Strings)
      {
        Debug.WriteLine(s);
      }
      Debug.WriteLine("End of tracing");
    }
      
    /// <summary>
    /// When using transactions, load balancing must lock on the current server.
    /// </summary>
    [Fact]
    public void ValidateTransactions()
    {
      MySqlTrace.Listeners.Clear();
      MySqlTrace.Switch.Level = SourceLevels.All;
      MySql.Data.MySqlClient.Tests.GenericListener listener = new MySql.Data.MySqlClient.Tests.GenericListener();
      MySqlTrace.Listeners.Add(listener);
      using (MySqlConnection conn = new MySqlConnection(st.ConnectionString))
      {
        conn.Open();
        MySqlTransaction tx = conn.BeginTransaction();
        st.ExecuteNonQuery(conn, "INSERT INTO orders VALUES(null, 2, 'Bruce')");
        st.ExecuteNonQuery(conn, "INSERT INTO orders VALUES(null, 1, 'James')");
        st.ExecuteNonQuery(conn, "INSERT INTO order_details VALUES(1, 1, 1, 0, 0)");
        st.ExecuteNonQuery(conn, "INSERT INTO order_details VALUES(1, 2, 1, 0, 0)");
        tx.Commit();
      }

      // Waits for replication on slave
      System.Threading.Thread.Sleep(3000);

      // validates data on slave
      using (MySqlConnection slaveConn = new MySqlConnection(st.ConnectionStringSlave))
      {
        slaveConn.Open();
        MySqlDataReader dr = st.ExecuteQuery(slaveConn, "SELECT * FROM orders");
        Assert.True(dr.Read());
        Assert.Equal(2, dr.GetValue(1));
        Assert.Equal("Bruce", dr.GetValue(2));
      }
      Debug.WriteLine("Start of tracing");
      foreach (string s in listener.Strings)
      {
        Debug.WriteLine(s);
      }
      Debug.WriteLine("End of tracing");
    }

    #region Private methods

    private int GetPort(MySqlConnection connection)
    {
      MySqlDataReader dr = st.ExecuteQuery(connection, "SHOW VARIABLES LIKE 'port';");

      dr.Read();
      int port = dr.GetInt32(1);
      dr.Close();

      return port;
    }

    #endregion
  }
}
