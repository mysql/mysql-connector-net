// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using System;
using System.IO;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class ClientSideFailoverTests : BaseTest
  {
    [Fact]
    public void SequentialMethodWithBasicFormatConnectionString()
    {
      int connectionTimeout = 1;

      // Single host.
      using (var session = MySQLX.GetSession(ConnectionString))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts. First attempt fails, second is succesful.
      using (var session = MySQLX.GetSession("server=10.10.10.10, localhost;port=" + XPort + ";uid=test;password=test;connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts using synonyms for "server" connection option. First attempt fails, second is succesful.
      using (var session = MySQLX.GetSession("address=10.10.10.10, localhost;port=" + XPort + ";uid=test;password=test;connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts. All attempts fail.
      Exception ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession("server= 10.10.10.10, 20.20.20.20 ;port=" + XPort + ";uid=test;password=test;connectiontimeout=" + connectionTimeout));
      Assert.Equal("Unable to connect to any of the specified MySQL hosts.", ex.Message);

      // Providing port number as part of the host name.
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("server= 10.10.10.10:33060, 20.20.20.20:33060 ;port=" + XPort + ";uid=test;password=test;connectiontimeout=" + connectionTimeout));
      Assert.Equal("Providing a port number as part of the host address isn't supported when using connection strings in basic format or anonymous objects. Use URI format instead.", ex.Message);

      // Use default port.
      using (var session = MySQLX.GetSession("server=localhost;uid=test;password=test;connectiontimeout=" + connectionTimeout + ";"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("localhost", session.Settings.Server);
      }
    }

    [Fact]
    public void SequentialMethodWithUriFormatConnectionString()
    {
      int connectionTimeout = 1;

      // Single host.
      using (var session = MySQLX.GetSession("mysqlx://test:test@localhost:" + XPort + "?connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Single host as an array. Successful connection.
      using (var session = MySQLX.GetSession("mysqlx://test:test@[127.0.0.1]?connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Single host and port as an array. Successful connection.
      using (var session = MySQLX.GetSession("mysqlx://test:test@[127.0.0.1:" + XPort + "]?connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Single host as an array. Failed connection.
      Exception ex = Assert.Throws<IOException>(() => MySQLX.GetSession("mysqlx://test:test@[192.1.10.10:" + XPort + "]?connectiontimeout=" + connectionTimeout));
      Assert.Equal("The operation is not allowed on non-connected sockets.", ex.Message);

      // Multiple hosts. First attempt fails, second is succesful.
      using (var session = MySQLX.GetSession("mysqlx://test:test@[192.1.10.10,127.0.0.1:" + XPort + "]?connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts and a schema. First attempt fails, second is succesful.
      using (var session = MySQLX.GetSession("mysqlx://test:test@[192.1.10.10,127.0.0.1:" + XPort + "]/test?connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts which may or may not contain a port number. First and second attempts fail, third attempt is succesful.
      using (var session = MySQLX.GetSession("mysqlx://test:test@[192.1.10.10,120.0.0.2:22000,[::1]:" + XPort + "]/test?connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Fact]
    public void SequentialMethodWithAnonymousTypes()
    {
      int connectionTimeout = 1;
      string uid = "test";
      string password = "test";

      // Single host.
      using (var session = MySQLX.GetSession(new { server = "localhost", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Use default port.
      using (var session = MySQLX.GetSession(new { server = "localhost", uid = uid, password = password, connectiontimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts. First attempt fails, second is succesful.
      using (var session = MySQLX.GetSession(new { server = "10.10.10.10, localhost", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts using synonyms for "server" connection option. First attempt fails, second is succesful.
      using (var session = MySQLX.GetSession(new { address = "10.10.10.10, localhost", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts. All attempts fail.
      Exception ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(new { server = "10.10.10.10, 20.20.20.20", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }));
      Assert.Equal("Unable to connect to any of the specified MySQL hosts.", ex.Message);

      // Providing port number as part of the host name.
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(new { server = "10.10.10.10:33060, 20.20.20.20:33060", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }));
      Assert.Equal("Providing a port number as part of the host address isn't supported when using connection strings in basic format or anonymous objects. Use URI format instead.", ex.Message);
    }

    [Fact]
    public void PriorityMethodWithBasicFormatConnectionStrng()
    {
      int connectionTimeout = 1;

      using (var session = MySQLX.GetSession("server=(address=127.0.0.1,priority=100);port=" + XPort + ";uid=test;password=test;connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("server=(address=server.example,priority=50),(address=127.0.0.1,priority=100);port=" + XPort + ";uid=test;password=test;connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("server=(address=server.example,priority=100),(address=127.0.0.1,priority=25),(address=192.0.10.56,priority=75);port=" + XPort + ";uid=test;password=test;connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
        Assert.Equal("server.example", Failover.FailoverManager.FailoverGroup.Hosts[0].Host);
        Assert.Equal("192.0.10.56", Failover.FailoverManager.FailoverGroup.Hosts[1].Host);
        Assert.Equal("127.0.0.1", Failover.FailoverManager.FailoverGroup.Hosts[2].Host);
      }

      // Use default port.
      using (var session = MySQLX.GetSession("server=(address=localhost,priority=100);uid=test;password=test;connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("localhost", session.Settings.Server);
      }

      // Priority outside the 0-100 allowed range.
      Exception ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("server=(address=server.example,priority=-20),(address=127.0.0.1,priority=100);port=" + XPort + ";uid=test;password=test;connectiontimeout=" + connectionTimeout));
      Assert.Equal("The priority must be between 0 and 100.", ex.Message);

      ex = Assert.Throws<ArgumentException>(() =>  MySQLX.GetSession("server=(address=server.example,priority=-50),(address=127.0.0.1,priority=101);port=" + XPort + ";uid=test;password=test;connectiontimeout=" + connectionTimeout));
      Assert.Equal("The priority must be between 0 and 100.", ex.Message);

      // Set priority for a subset of the hosts.
      ex = Assert.Throws<ArgumentException>(() =>  MySQLX.GetSession("server=(address=server.example),(address=127.0.0.1,priority=100);port=" + XPort + ";uid=test;password=test;connectiontimeout=" + connectionTimeout));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() =>  MySQLX.GetSession("server=(address=server.example,priority=50),(address=127.0.0.1);port=" + XPort + ";uid=test;password=test;connectiontimeout=" + connectionTimeout));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() =>  MySQLX.GetSession("server=(address=server.example,priority=50),(address=127.0.0.1,priority=100),(address=server.example);port=" + XPort + ";uid=test;password=test;connectiontimeout=" + connectionTimeout));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);

      // Automatically set priority if no priority is given.
      string hostList = string.Empty;
      int priority = 100;
      for (int i = 1; i <= 105; i++)
      {
        hostList += "(address=server" + i + ".example,priority=" + ( priority!=0 ? priority-- : 0) + "),";
        if (i==105) hostList += "(address=localhost,priority=0)";
      }

      using (var session = MySQLX.GetSession("server=" + hostList + ";port=" + XPort + ";uid=test;password=test;connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        priority = 100;
        foreach (var host in Failover.FailoverManager.FailoverGroup.Hosts)
        {
          Assert.Equal(priority!=0 ? priority-- : 0, host.Priority);
        }
      }
    }

    [Fact]
    public void PriorityMethodWithUriFormatConnectonString()
    {
      int connectionTimeout = 1;

      using (var session = MySQLX.GetSession("mysqlx://test:test@[(address=server.example,priority=50),(address=127.0.0.1,priority=100)]?connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("mysqlx://test:test@[(address=server.example,priority=50),(address=127.0.0.1,priority=100)]?connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("mysqlx://test:test@[(address=server.example,priority=100),(address=127.0.0.1,priority=25),(address=192.0.10.56,priority=75)]?connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
        Assert.Equal("server.example", Failover.FailoverManager.FailoverGroup.Hosts[0].Host);
        Assert.Equal("192.0.10.56", Failover.FailoverManager.FailoverGroup.Hosts[1].Host);
        Assert.Equal("127.0.0.1", Failover.FailoverManager.FailoverGroup.Hosts[2].Host);
      }

      // Priority outside the 0-100 allowed range.
      Exception ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("mysqlx://test:test@[(address=server.example,priority=-20),(address=127.0.0.1,priority=100)]?connectiontimeout=" + connectionTimeout));
      Assert.Equal("The priority must be between 0 and 100.", ex.Message);

      ex = Assert.Throws<ArgumentException>(() =>  MySQLX.GetSession("mysqlx://test:test@[(address=server.example,priority=50),(address=127.0.0.1,priority=101)]?connectiontimeout=" + connectionTimeout));
      Assert.Equal("The priority must be between 0 and 100.", ex.Message);

      // Set priority for a subset of the hosts.
      ex = Assert.Throws<ArgumentException>(() =>  MySQLX.GetSession("mysqlx://test:test@[(address=server.example),(address=127.0.0.1,priority=100)]?connectiontimeout=" + connectionTimeout));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() =>  MySQLX.GetSession("mysqlx://test:test@[(address=server.example,priority=100),(address=127.0.0.1)]?connectiontimeout=" + connectionTimeout));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() =>  MySQLX.GetSession("mysqlx://test:test@[(address=server.example),(address=127.0.0.1),(address=server2.example,priority=100)]?connectiontimeout=" + connectionTimeout));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);

      // Automatically set priority if no priority is given.
      string hostList = string.Empty;
      int priority = 100;
      for (int i = 1; i <= 105; i++)
      {
        hostList += "(address=server" + i + ".example,priority=" + ( priority!=0 ? priority-- : 0) + "),";
        if (i==105) hostList += "(address=localhost,priority=0)";
      }

      using (var session = MySQLX.GetSession("mysqlx://test:test@[" + hostList + "]?connectiontimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        priority = 100;
        foreach (var host in Failover.FailoverManager.FailoverGroup.Hosts)
        {
          Assert.Equal(priority!=0 ? priority-- : 0, host.Priority);
        }
      }
    }

    [Fact]
    public void PriorityMethodWithAnonymousTypes()
    {
      int connectionTimeout = 1;
      string uid = "test";
      string password = "test";

      using (var session = MySQLX.GetSession(new { server = "(address=127.0.0.1,priority=100)", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
      }

      // Use default port.
      using (var session = MySQLX.GetSession(new { server = "(address=127.0.0.1,priority=100)", uid = uid, password = password, connectiontimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
      }

      using (var session = MySQLX.GetSession(new { server = "(address=server.example,priority=50),(address=127.0.0.1,priority=100)", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
      }

      using (var session = MySQLX.GetSession(new { server = "(address=server.example,priority=100),(address=127.0.0.1,priority=25),(address=192.0.10.56,priority=75)", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
        Assert.Equal("server.example", Failover.FailoverManager.FailoverGroup.Hosts[0].Host);
        Assert.Equal("192.0.10.56", Failover.FailoverManager.FailoverGroup.Hosts[1].Host);
        Assert.Equal("127.0.0.1", Failover.FailoverManager.FailoverGroup.Hosts[2].Host);
      }

      using (var session = MySQLX.GetSession(new { host = "(address=127.0.0.1,priority=2),(address=localhost,priority=3)", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Priority outside the 0-100 allowed range.
      Exception ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(new { server = "(address=server.example,priority=-20),(address=127.0.0.1,priority=100)", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }));
      Assert.Equal("The priority must be between 0 and 100.", ex.Message);

      ex = Assert.Throws<ArgumentException>(() =>  MySQLX.GetSession(new { server = "(address=server.example,priority=-50),(address=127.0.0.1,priority=101)", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }));
      Assert.Equal("The priority must be between 0 and 100.", ex.Message);

      // Set priority for a subset of the hosts.
      ex = Assert.Throws<ArgumentException>(() =>  MySQLX.GetSession(new { server = "(address=server.example),(address=127.0.0.1,priority=100)", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() =>  MySQLX.GetSession(new { server = "(address=server.example,priority=50),(address=127.0.0.1)", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() =>  MySQLX.GetSession(new { server = "(address=server.example,priority=50),(address=127.0.0.1,priority=100),(address=server.example)", port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);

      // Automatically set priority if no priority is given.
      string hostList = string.Empty;
      int priority = 100;
      for (int i = 1; i <= 105; i++)
      {
        hostList += "(address=server" + i + ".example,priority=" + ( priority!=0 ? priority-- : 0) + "),";
        if (i==105) hostList += "(address=localhost,priority=0)";
      }

      using (var session = MySQLX.GetSession(new { server = hostList, port = XPort, uid = uid, password = password, connectiontimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        priority = 100;
        foreach (var host in Failover.FailoverManager.FailoverGroup.Hosts)
        {
          Assert.Equal(priority!=0 ? priority-- : 0, host.Priority);
        }
      }
    }
  }
}
