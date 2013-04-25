// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Linq;
using System.Text;
using NUnit.Framework;
using MySql.Data.MySqlClient;

namespace MySql.LoadBalancing.Tests
{
  class LoadBalancingTest : BaseTest
  {
    /// <summary>
    /// Validates that the slave is readonly
    /// </summary>
    [Test]
    public void _SlaveAsReadOnly()
    {
      using (MySqlConnection conn = new MySqlConnection(ConnectionStringSlave))
      {
        conn.Open();
        try
        {
          ExecuteNonQuery(conn, "INSERT INTO orders VALUES(null, 1, 'James')");
          Assert.Fail("Slave is not readonly.");
        }
        catch (MySqlException ex)
        {
          // Error 1290: The MySQL server is running with the --read-only option so
          // it cannot execute this statement
          Assert.AreEqual(1290, ex.Number);
        }
      }
    }

    /// <summary>
    /// Validates that each request for a connection is switched between the
    /// master and slave; also validates that all non-read statements are sent
    /// to the master
    /// </summary>
    [Test]
    public void RoundRobinWithWritting()
    {
      using (MySqlConnection conn = new MySqlConnection(ConnectionString))
      {
        conn.Open();

        ExecuteNonQuery(conn, "INSERT INTO orders VALUES(null, 1, 'James')");
        ExecuteNonQuery(conn, "INSERT INTO order_details VALUES(1, 1, 1, 0, 0)");
        ExecuteNonQuery(conn, "INSERT INTO order_details VALUES(1, 2, 1, 0, 0)");

        Assert.AreEqual(slavePort, GetPort(conn));
        Assert.AreEqual(masterPort, GetPort(conn));
        Assert.AreEqual(slavePort, GetPort(conn));
        Assert.AreEqual(masterPort, GetPort(conn));

        ExecuteNonQuery(conn, "INSERT INTO order_details VALUES(1, 3, 1, 0, 0)");
      }
    }

    /// <summary>
    /// Test for a master connection failure
    /// </summary>
    [Test]
    public void RoundRobinReadOnly()
    {
      using (MySqlConnection conn = new MySqlConnection("server=Group2;"))
      {
        conn.Open();

        Assert.AreEqual(slavePort, GetPort(conn));
        Assert.AreEqual(masterPort, GetPort(conn));
        Assert.AreEqual(slavePort, GetPort(conn));
        Assert.AreEqual(masterPort, GetPort(conn));

        try
        {
          ExecuteNonQuery(conn, "INSERT INTO orders VALUES(null, 1, 'James')");
          Assert.Fail("Exception should be thrown");
        }
        catch (MySqlException ex)
        {
          Assert.AreEqual(MySql.Data.MySqlClient.Properties.Resources.LoadBalancing_NoAvailableServer, ex.Message);
        }
      }
    }

    #region Private methods

    private int GetPort(MySqlConnection connection)
    {
      MySqlDataReader dr = ExecuteQuery(connection, "SHOW VARIABLES LIKE 'port';");

      dr.Read();
      int port = dr.GetInt32(1);
      dr.Close();

      return port;
    }

    #endregion
  }
}
