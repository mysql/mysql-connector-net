// Copyright © 2017, 2019, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.XDevAPI;
using System;
using Xunit;
using MySql.Data.Failover;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using MySql.Data;

namespace MySqlX.Data.Tests
{
  public class ClientSideFailoverTests : BaseTest
  {
    [Fact]
    public void RandomMethodWithBasicFormatConnectionString()
    {
      int connectionTimeout = 1000;

      // Single host.
      using (var session = MySQLX.GetSession(ConnectionString))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts.
      using (var session = MySQLX.GetSession("server=10.10.10.10, localhost, 20.20.20.20, 30.30.30.30;port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts with IPv6
      using (var session = MySQLX.GetSession("server=10.10.10.10, ::1, 20.20.20.20, 30.30.30.30;port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts using synonyms for "server" connection option.
      using (var session = MySQLX.GetSession("address=10.10.10.10, localhost;port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts. All attempts fail.
      Exception ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession("server= 10.10.10.10, 20.20.20.20 ;port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout));
      Assert.Equal("Unable to connect to any of the specified MySQL hosts.", ex.Message);

      // Providing port number as part of the host name.
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("server= 10.10.10.10:33050, 20.20.20.20:33060, localhost:33060 ;port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout));
      Assert.Equal("Providing a port number as part of the host address isn't supported when using connection strings in basic format or anonymous objects. Use URI format instead.", ex.Message);
    }

    [Fact]
    public void RandomMethodWithUriFormatConnectionString()
    {
      int connectionTimeout = 1000;

      // Single host.
      using (var session = MySQLX.GetSession("mysqlx://test:test@localhost:" + XPort + "?connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Single host and port as an array. Successful connection.
      using (var session = MySQLX.GetSession("mysqlx://test:test@[127.0.0.1:" + XPort + "]?connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Single host as an array. Failed connection.
      Exception ex = Assert.Throws<TimeoutException>(() => MySQLX.GetSession("mysqlx://test:test@[192.1.10.10:" + XPort + "]?connecttimeout=" + connectionTimeout));

      // Multiple hosts.
      using (var session = MySQLX.GetSession("mysqlx://test:test@[192.1.10.10,127.0.0.1:" + XPort + "]?connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts and a schema.
      using (var session = MySQLX.GetSession("mysqlx://test:test@[192.1.10.10,127.0.0.1:" + XPort + "]/test?connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts which may or may not contain a port number.
      using (var session = MySQLX.GetSession("mysqlx://test:test@[192.1.10.10,120.0.0.2:22000,[::1]:" + XPort + "]/test?connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Fact]
    public void RandomMethodWithAnonymousTypes()
    {
      int connectionTimeout = 1000;
      string uid = "test";
      string password = "test";

      // Single host.
      using (var session = MySQLX.GetSession(new { server = "localhost", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts.
      using (var session = MySQLX.GetSession(new { server = "10.10.10.10, localhost", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts with IPv6
      using (var session = MySQLX.GetSession(new { server = "10.10.10.10, ::1", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts using synonyms for "server" connection option. First attempt fails, second is succesful.
      using (var session = MySQLX.GetSession(new { address = "10.10.10.10, localhost", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Multiple hosts. All attempts fail.
      Exception ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(new { server = "10.10.10.10, 20.20.20.20", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }));
      Assert.Equal("Unable to connect to any of the specified MySQL hosts.", ex.Message);

      // Providing port number as part of the host name.
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(new { server = "10.10.10.10:33060, 20.20.20.20:33060", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }));
      Assert.Equal("Providing a port number as part of the host address isn't supported when using connection strings in basic format or anonymous objects. Use URI format instead.", ex.Message);
    }

    [Fact]
    public void PriorityMethodWithBasicFormatConnectionString()
    {
      int connectionTimeout = 1000;

      // Single host with max_connections.
      try
      {
        List<Session> sessions = new List<Session>();
        ExecuteSqlAsRoot("SET @@global.mysqlx_max_connections = 2");
        for (int i = 0; i <= 2; i++)
        {
          Session newSession = MySQLX.GetSession("server=(address=127.0.0.1,priority=100);port=" + XPort + ";uid=test;password=test;");
          sessions.Add(newSession);
        }
        Assert.False(true, "MySqlException should be thrown");
      }
      catch (MySqlException exception)
      {
        Assert.Equal(ResourcesX.UnableToOpenSession, exception.Message);
      }
      finally
      {
        ExecuteSqlAsRoot("SET @@global.mysqlx_max_connections = 100");
      }

      using (var session = MySQLX.GetSession("server=(address=127.0.0.1,priority=100);port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("server=(address=server.example,priority=50),(address=127.0.0.1,priority=100);port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("server=(address=server.example,priority=100),(address=127.0.0.1,priority=25),(address=192.0.10.56,priority=75);port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
        Assert.Equal("server.example", FailoverManager.FailoverGroup.Hosts[0].Host);
        Assert.Equal("192.0.10.56", FailoverManager.FailoverGroup.Hosts[1].Host);
        Assert.Equal("127.0.0.1", FailoverManager.FailoverGroup.Hosts[2].Host);
      }

      // Priority outside the 0-100 allowed range.
      Exception ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("server=(address=server.example,priority=-20),(address=127.0.0.1,priority=100);port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout));
      Assert.Equal("The priority must be between 0 and 100.", ex.Message);

      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("server=(address=server.example,priority=-50),(address=127.0.0.1,priority=101);port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout));
      Assert.Equal("The priority must be between 0 and 100.", ex.Message);

      // Set priority for a subset of the hosts.
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("server=(address=server.example),(address=127.0.0.1,priority=100);port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("server=(address=server.example,priority=50),(address=127.0.0.1);port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("server=(address=server.example,priority=50),(address=127.0.0.1,priority=100),(address=server.example);port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);

      // Automatically set priority if no priority is given.
      string hostList = string.Empty;
      int priority = 100;
      for (int i = 1; i <= 105; i++)
      {
        hostList += "(address=server" + i + ".example,priority=" + (priority != 0 ? priority-- : 0) + "),";
        if (i == 105) hostList += "(address=localhost,priority=0)";
      }

      using (var session = MySQLX.GetSession("server=" + hostList + ";port=" + XPort + ";uid=test;password=test;connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        priority = 100;
        foreach (var host in FailoverManager.FailoverGroup.Hosts)
        {
          Assert.Equal(priority != 0 ? priority-- : 0, host.Priority);
        }
      }
    }

    [Fact]
    public void PriorityMethodWithUriFormatConnectonString()
    {
      int connectionTimeout = 1000;

      // Single host with max_connections.
      try
      {
        List<Session> sessions = new List<Session>();
        ExecuteSqlAsRoot("SET @@global.mysqlx_max_connections = 2");
        for (int i = 0; i <= 2; i++)
        {
          Session newSession = MySQLX.GetSession("mysqlx://test:test@[(address=localhost:"+XPort+",priority=50)]?connecttimeout=" + connectionTimeout);
          sessions.Add(newSession);
        }
        Assert.False(true, "MySqlException should be thrown");
      }
      catch (MySqlException exception)
      {
        Assert.Equal(ResourcesX.UnableToOpenSession, exception.Message);
      }
      finally
      {
        ExecuteSqlAsRoot("SET @@global.mysqlx_max_connections = 100");
      }

      using (var session = MySQLX.GetSession($"mysqlx://test:test@[(address=server.example,priority=50),(address=127.0.0.1:{XPort},priority=100)]?connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
      }

      using (var session = MySQLX.GetSession($"mysqlx://test:test@[(address=server.example,priority=50),(address=127.0.0.1:{XPort},priority=100)]?connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
      }

      using (var session = MySQLX.GetSession($"mysqlx://test:test@[(address=server.example,priority=100),(address=127.0.0.1:{XPort},priority=25),(address=192.0.10.56,priority=75)]?connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
        Assert.Equal("server.example", FailoverManager.FailoverGroup.Hosts[0].Host);
        Assert.Equal("192.0.10.56", FailoverManager.FailoverGroup.Hosts[1].Host);
        Assert.Equal("127.0.0.1", FailoverManager.FailoverGroup.Hosts[2].Host);
      }

      // Priority outside the 0-100 allowed range.
      Exception ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"mysqlx://test:test@[(address=server.example,priority=-20),(address=127.0.0.1:{XPort},priority=100)]?connecttimeout=" + connectionTimeout));
      Assert.Equal("The priority must be between 0 and 100.", ex.Message);

      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"mysqlx://test:test@[(address=server.example,priority=50),(address=127.0.0.1:{XPort},priority=101)]?connecttimeout=" + connectionTimeout));
      Assert.Equal("The priority must be between 0 and 100.", ex.Message);

      // Set priority for a subset of the hosts.
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"mysqlx://test:test@[(address=server.example),(address=127.0.0.1:{XPort},priority=100)]?connecttimeout=" + connectionTimeout));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"mysqlx://test:test@[(address=server.example,priority=100),(address=127.0.0.1:{XPort})]?connecttimeout=" + connectionTimeout));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession($"mysqlx://test:test@[(address=server.example),(address=127.0.0.1:{XPort}),(address=server2.example,priority=100)]?connecttimeout=" + connectionTimeout));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);

      // Automatically set priority if no priority is given.
      string hostList = string.Empty;
      int priority = 100;
      for (int i = 1; i <= 105; i++)
      {
        hostList += "(address=server" + i + ".example,priority=" + (priority != 0 ? priority-- : 0) + "),";
        if (i == 105) hostList += $"(address=localhost:{XPort},priority=0)";
      }

      using (var session = MySQLX.GetSession($"mysqlx://test:test@[{hostList}]?connecttimeout=" + connectionTimeout))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        priority = 100;
        foreach (var host in FailoverManager.FailoverGroup.Hosts)
        {
          Assert.Equal(priority != 0 ? priority-- : 0, host.Priority);
        }
      }
    }

    [Fact]
    public void PriorityMethodWithAnonymousTypes()
    {
      int connectionTimeout = 1000;
      string uid = "test";
      string password = "test";

      // Single host with max_connections.
      try
      {
        List<Session> sessions = new List<Session>();
        ExecuteSqlAsRoot("SET @@global.mysqlx_max_connections = 2");
        for (int i = 0; i <= 2; i++)
        {
          Session newSession = MySQLX.GetSession(new { server = "(address=127.0.0.1,priority=100)", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout });
          sessions.Add(newSession);
        }
        Assert.False(true, "MySqlException should be thrown");
      }
      catch (MySqlException exception)
      {
        Assert.Equal(ResourcesX.UnableToOpenSession, exception.Message);
      }
      finally
      {
        ExecuteSqlAsRoot("SET @@global.mysqlx_max_connections = 100");
      }

      using (var session = MySQLX.GetSession(new { server = "(address=127.0.0.1,priority=100)", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
      }

      using (var session = MySQLX.GetSession(new { server = "(address=server.example,priority=50),(address=127.0.0.1,priority=100)", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
      }

      using (var session = MySQLX.GetSession(new { server = "(address=server.example,priority=100),(address=127.0.0.1,priority=25),(address=192.0.10.56,priority=75)", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal("127.0.0.1", session.Settings.Server);
        Assert.Equal("server.example", FailoverManager.FailoverGroup.Hosts[0].Host);
        Assert.Equal("192.0.10.56", FailoverManager.FailoverGroup.Hosts[1].Host);
        Assert.Equal("127.0.0.1", FailoverManager.FailoverGroup.Hosts[2].Host);
      }

      using (var session = MySQLX.GetSession(new { host = "(address=127.0.0.1,priority=2),(address=localhost,priority=3)", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // Priority outside the 0-100 allowed range.
      Exception ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(new { server = "(address=server.example,priority=-20),(address=127.0.0.1,priority=100)", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }));
      Assert.Equal("The priority must be between 0 and 100.", ex.Message);

      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(new { server = "(address=server.example,priority=-50),(address=127.0.0.1,priority=101)", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }));
      Assert.Equal("The priority must be between 0 and 100.", ex.Message);

      // Set priority for a subset of the hosts.
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(new { server = "(address=server.example),(address=127.0.0.1,priority=100)", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(new { server = "(address=server.example,priority=50),(address=127.0.0.1)", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(new { server = "(address=server.example,priority=50),(address=127.0.0.1,priority=100),(address=server.example)", port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }));
      Assert.Equal("You must either assign no priority to any of the hosts or give a priority for every host.", ex.Message);

      // Automatically set priority if no priority is given.
      string hostList = string.Empty;
      int priority = 100;
      for (int i = 1; i <= 105; i++)
      {
        hostList += "(address=server" + i + ".example,priority=" + (priority != 0 ? priority-- : 0) + "),";
        if (i == 105) hostList += "(address=localhost,priority=0)";
      }

      using (var session = MySQLX.GetSession(new { server = hostList, port = XPort, uid = uid, password = password, connecttimeout = connectionTimeout }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        priority = 100;
        foreach (var host in FailoverManager.FailoverGroup.Hosts)
        {
          Assert.Equal(priority != 0 ? priority-- : 0, host.Priority);
        }
      }
    }
  }
}
