// Copyright (c) 2019, 2020, Oracle and/or its affiliates. All rights reserved.
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
using NUnit.Framework;
using MySql.Data.Failover;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  public class ClientSideFailoverTests : TestBase
  {
    internal override void AdjustConnectionSettings(MySqlConnectionStringBuilder setttings)
    {
      setttings.ConnectionTimeout = 7;
    }

    [TestCase("localhost")] // Single host
    [TestCase("10.10.10.10, localhost, 20.20.20.20, 30.30.30.30")] // Multiple hosts
    [TestCase("10.10.10.10:3306, localhost, 20.20.20.20:3305, 30.30.30.30:3305")] // Multiple hosts with port number
    [TestCase("10.10.10.10, ::1, 20.20.20.20, 30.30.30.30")] // Multiple hosts, one with IPv6
    [TestCase("10.10.10.10, 10.11.12.13, 20.20.20.20, 30.30.30.30", false)] // Multiple hosts, should fail
    public void RandomMethod(string server, bool shouldPass = true)
    {
      Settings.Pooling = false;
      Settings.Server = server;

      if (!shouldPass)
      {
        Exception ex = Assert.Throws<MySqlException>(() => TryConnection(Settings.ConnectionString));
        Assert.AreEqual("Unable to connect to any of the specified MySQL hosts.", ex.Message);
      }
      else
        Assert.AreEqual(ConnectionState.Open, TryConnection(Settings.ConnectionString));
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

    [Test]
    public void PriorityMethod()
    {
#if NETCOREAPP3_1 || NET5_0
      if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) Assert.Ignore();
#endif
      // Multiple hosts and validate proper order assigned to hosts.
      Settings.Pooling = false;
      Settings.Server = "(address=server.example,priority=100),(address=127.0.0.1,priority=25),(address=192.0.10.56,priority=75)";
      using (MySqlConnection conn = new MySqlConnection(Settings.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.State);
        Assert.AreEqual("127.0.0.1", conn.Settings.Server);
        Assert.AreEqual("server.example", FailoverManager.FailoverGroup.Hosts[0].Host);
        Assert.AreEqual("192.0.10.56", FailoverManager.FailoverGroup.Hosts[1].Host);
        Assert.AreEqual("127.0.0.1", FailoverManager.FailoverGroup.Hosts[2].Host);
      }

      // Multiple hosts with IPv6
      if (Version > new Version(5, 6, 0))
      {
        Settings.Server = "(address=server.example,priority=50),(address=::1,priority=100)";

        using (MySqlConnection conn = new MySqlConnection(Settings.ConnectionString))
        {
          conn.Open();
          Assert.AreEqual(ConnectionState.Open, conn.State);
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

      Settings.Server = hostList;

      using (MySqlConnection conn = new MySqlConnection(Settings.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.State);
        priority = 100;
        foreach (var host in FailoverManager.FailoverGroup.Hosts)
        {
          Assert.AreEqual(priority != 0 ? priority-- : 0, host.Priority);
        }
      }
    }

    [TestCase("(address=server.example,priority=-20),(address=127.0.0.1,priority=100)", "The priority must be between 0 and 100.", "argument")] // Priority outside the 0-100 allowed range
    [TestCase("(address=server.example,priority=-50),(address=127.0.0.1,priority=101)", "The priority must be between 0 and 100.", "argument")] // Priority outside the 0-100 allowed range
    [TestCase("(address=server.example),(address=127.0.0.1,priority=100)", "You must either assign no priority to any of the hosts or give a priority for every host.", "argument")] // Set priority for a subset of the hosts.
    [TestCase("(address=server.example,priority=50),(address=127.0.0.1,priority=100),(address=server.example)", "You must either assign no priority to any of the hosts or give a priority for every host.", "argument")] // Set priority for a subset of the hosts.
    [TestCase("(address=server.example,priority=100),(address=10.10.10.10,priority=25),(address=192.0.10.56,priority=75)", "Unable to connect to any of the specified MySQL hosts.", "mysql")] // Multiple hosts. All attempts fail.
    public void PriorityMethodConnectionFail(string server, string exceptionMessage, string exceptionType)
    {
      Settings.Server = server;
      using (MySqlConnection conn = new MySqlConnection(Settings.ConnectionString))
      {
        Exception ex;
        if (exceptionType == "argument")
          ex = Assert.Throws<ArgumentException>(() => conn.Open());
        else
          ex = Assert.Throws<MySqlException>(() => conn.Open());

        Assert.AreEqual(exceptionMessage, ex.Message);
      }
    }

    [TestCase("10.10.10.10,20.20.20.20,localhost")] // Random
    [TestCase("(address=server.example,priority=100),(address=localhost,priority=25),(address=192.0.10.56,priority=75)")] // Priority
    public void Pooling(string server)
    {
      Settings.Pooling = true;
      Settings.MinimumPoolSize = 10;
      Settings.Server = server;

      MySqlConnection[] connArray = new MySqlConnection[10];
      for (int i = 0; i < connArray.Length; i++)
      {
        connArray[i] = new MySqlConnection(Settings.ConnectionString);
        connArray[i].Open();
        Assert.AreEqual(ConnectionState.Open, connArray[i].State);
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

      // close connections
      for (int i = 0; i < connArray.Length; i++)
        connArray[i].Close();
    }
  }
}