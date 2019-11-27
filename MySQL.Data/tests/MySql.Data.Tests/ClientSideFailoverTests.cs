// Copyright © 2019, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.Failover;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  public class ClientSideFailoverTests : TestBase
  {
    MySqlConnectionStringBuilder _sb;

    public ClientSideFailoverTests(TestFixture fixture) : base(fixture)
    {
      _sb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      _sb.ConnectionTimeout = 7;
    }
    
    [Theory]
    [Trait("Category", "Security")]
    [InlineData("localhost")] // Single host
    [InlineData("10.10.10.10, localhost, 20.20.20.20, 30.30.30.30")] // Multiple hosts
    [InlineData("10.10.10.10:3306, localhost, 20.20.20.20:3305, 30.30.30.30:3305")] // Multiple hosts with port number
    [InlineData("10.10.10.10, ::1, 20.20.20.20, 30.30.30.30")] // Multiple hosts, one with IPv6
    [InlineData("10.10.10.10, 10.11.12.13, 20.20.20.20, 30.30.30.30", false)] // Multiple hosts, should fail
    public void RandomMethod(string server, bool shouldPass = true)
    {
      _sb.Pooling = false;
      _sb.Server = server;

      if (!shouldPass)
      {
        Exception ex = Assert.Throws<MySqlException>(() => TryConnection(_sb.ConnectionString));
        Assert.Equal("Unable to connect to any of the specified MySQL hosts.", ex.Message);
      }
      else
        Assert.Equal(ConnectionState.Open, TryConnection(_sb.ConnectionString));
    }

    private ConnectionState TryConnection(string connString)
    {
      ConnectionState state;
      using (MySqlConnection conn = new MySqlConnection(connString))
      {
        conn.Open();
        state = conn.State;
      }
      return state;
    }

    [Fact]
    [Trait("Category", "Security")]
    public void PriorityMethod()
    {
      _sb.Pooling = false;

      _sb.Server = "(address=server.example,priority=100),(address=127.0.0.1,priority=100),(address=192.0.10.56,priority=100)";
      using (MySqlConnection conn = new MySqlConnection(_sb.ConnectionString))
      {
        conn.Open();
        Assert.Equal(ConnectionState.Open, conn.State);
        Assert.Equal("127.0.0.1", conn.Settings.Server);
      }

      // Multiple hosts and validate proper order assigned to hosts.
      _sb.Server = "(address=server.example,priority=100),(address=127.0.0.1,priority=25),(address=192.0.10.56,priority=75)";
      using (MySqlConnection conn = new MySqlConnection(_sb.ConnectionString))
      {
        conn.Open();
        Assert.Equal(ConnectionState.Open, conn.State);
        Assert.Equal("127.0.0.1", conn.Settings.Server);
        Assert.Equal("server.example", FailoverManager.FailoverGroup.Hosts[0].Host);
        Assert.Equal("192.0.10.56", FailoverManager.FailoverGroup.Hosts[1].Host);
        Assert.Equal("127.0.0.1", FailoverManager.FailoverGroup.Hosts[2].Host);
      }

      // Priority outside the 0-100 allowed range
      _sb.Server = "(address=server.example,priority=-20),(address=127.0.0.1,priority=100)";
      using (MySqlConnection conn = new MySqlConnection(_sb.ConnectionString))
      {
        Exception ex = Assert.Throws<ArgumentException>(() => conn.Open());
        Assert.Equal("The priority must be between 0 and 100.", ex.Message);
      }

      _sb.Server = "(address=server.example,priority=-50),(address=127.0.0.1,priority=101)";
      using (MySqlConnection conn = new MySqlConnection(_sb.ConnectionString))
      {
        Exception ex = Assert.Throws<ArgumentException>(() => conn.Open());
        Assert.Equal("The priority must be between 0 and 100.", ex.Message);
      }

      // Set priority for a subset of the hosts.
      _sb.Server = "(address=server.example),(address=127.0.0.1,priority=100)";
      using (MySqlConnection conn = new MySqlConnection(_sb.ConnectionString))
      {
        Exception ex = Assert.Throws<ArgumentException>(() => conn.Open());
        Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      }

      _sb.Server = "(address=server.example,priority=50),(address=127.0.0.1,priority=100),(address=server.example)";
      using (MySqlConnection conn = new MySqlConnection(_sb.ConnectionString))
      {
        Exception ex = Assert.Throws<ArgumentException>(() => conn.Open());
        Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      }

      // Multiple hosts with IPv6
      if (Fixture.Version > new Version(5, 6, 0))
      {
        _sb.Server = "(address=server.example,priority=50),(address=::1,priority=100)";
        using (MySqlConnection conn = new MySqlConnection(_sb.ConnectionString))
        {
          conn.Open();
          Assert.Equal(ConnectionState.Open, conn.State);
        }
      }

      // Automatically set priority if no priority is given.
      string hostList = string.Empty;
      int priority = 100;
      for (int i = 1; i <= 105; i++)
      {
        hostList += "(address=server" + i + ".example,priority=" + (priority != 0 ? priority-- : 0) + "),";
        if (i == 105) hostList += "(address=localhost,priority=0)";
      }

      _sb.Server = hostList;
      using (MySqlConnection conn = new MySqlConnection(_sb.ConnectionString))
      {
        conn.Open();
        Assert.Equal(ConnectionState.Open, conn.State);
        priority = 100;
        foreach (var host in FailoverManager.FailoverGroup.Hosts)
        {
          Assert.Equal(priority != 0 ? priority-- : 0, host.Priority);
        }
      }

      // Multiple hosts. All attempts fail.
      _sb.Server = "(address=server.example,priority=100),(address=10.10.10.10,priority=25),(address=192.0.10.56,priority=75)";
      using (MySqlConnection conn = new MySqlConnection(_sb.ConnectionString))
      {
        Exception ex = Assert.Throws<MySqlException>(() => conn.Open());
        Assert.Equal("Unable to connect to any of the specified MySQL hosts.", ex.Message);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void Pooling()
    {
      _sb.Pooling = true;
      _sb.MinimumPoolSize = 10;

      // Random Method
      _sb.Server = "10.10.10.10,20.20.20.20,localhost";
      MySqlConnection[] connArray = new MySqlConnection[10];
      for (int i = 0; i < connArray.Length; i++)
      {
        connArray[i] = new MySqlConnection(_sb.ConnectionString);
        connArray[i].Open();
        Assert.Equal(ConnectionState.Open, connArray[i].State);
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

      //Priority Method
      _sb.Server = "(address=server.example,priority=100),(address=localhost,priority=25),(address=192.0.10.56,priority=75)";
      connArray = new MySqlConnection[10];
      for (int i = 0; i < connArray.Length; i++)
      {
        connArray[i] = new MySqlConnection(_sb.ConnectionString);
        using (connArray[i])
        {
          connArray[i].Open();
          Assert.Equal(ConnectionState.Open, connArray[i].State);
        }
      }
    }
  }
}