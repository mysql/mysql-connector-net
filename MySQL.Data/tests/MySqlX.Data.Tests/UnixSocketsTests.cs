// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using System;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class UnixSocketsTests : BaseTest
  {
    private string defaultUnixSocket;

    public UnixSocketsTests()
    {
      defaultUnixSocket = Environment.GetEnvironmentVariable("MYSQLX_SOCKET") ?? "/var/run/mysqld/mysqlx.sock";
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectionWithUriConnectionString()
    {
      if (Platform.IsWindows()) return;

      using (var session = MySQLX.GetSession("mysqlx://root:@" + defaultUnixSocket + "?protocol=unix&sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("mysqlx://root:@" + defaultUnixSocket.Replace("/", "%2F") + "?protocol=unix&sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectionWithUriConnectionStringIncludingSchema()
    {
      if (Platform.IsWindows()) return;

      using (var session = MySQLX.GetSession("mysqlx://root:@(" + defaultUnixSocket + ")/mysql?protocol=unix&sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectionWithParenthesisEnclosedSockets()
    {
      if (Platform.IsWindows()) return;

      using (var session = MySQLX.GetSession("mysqlx://root:@(" + defaultUnixSocket + ")?protocol=unix&sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("server=(" + defaultUnixSocket + ");uid=root;protocol=unix;sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession(new {
        server = "(" + defaultUnixSocket + ")",
        uid = "root",
        protocol = "unix",
        sslmode = MySqlSslMode.None
        }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectionWithBasicConnectionString()
    {
      if (Platform.IsWindows()) return;

      using (var session = MySQLX.GetSession("server=" + defaultUnixSocket + ";uid=root;protocol=unixsocket;sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectionWithAnonymousObject()
    {
      if (Platform.IsWindows()) return;

      using (var session = MySQLX.GetSession(new {
        server = defaultUnixSocket,
        uid = "root",
        protocol = "unix",
        sslmode = MySqlSslMode.None
        }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SslNotSupported()
    {
      if (Platform.IsWindows()) return;

      Assert.Throws<MySqlException>(() => MySQLX.GetSession("mysqlx://root:@" + defaultUnixSocket + "?protocol=unix"));
    }

    [Fact]
    [Trait("Category", "Security")]
    public void MissingProtocolConnectionOption()
    {
      if (Platform.IsWindows()) return;

      Assert.Throws<AggregateException>(() => MySQLX.GetSession("mysqlx://root:@" + defaultUnixSocket));
    }

    [Fact]
    [Trait("Category", "Security")]
    public void Failover()
    {
      if (Platform.IsWindows()) return;

      using (var session = MySQLX.GetSession("server=/tmp/mysql.sock1, (/tmp/mysql.sock2) ,(%2Ftmp%2Fmysql.sock3) , " + defaultUnixSocket + ";uid=root;protocol=unix;sslmode=none;"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("server=(address=/tmp/mysql.sock1, priority=100),(address=(/tmp/mysql.sock2),priority=99),(address=(%2tmp%2mysql.sock3),priority=98),(address=" + defaultUnixSocket + ",priority=97);uid=root;protocol=unix;sslmode=none;"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("server=(address=" + defaultUnixSocket + ",priority=100);uid=root;protocol=unix;sslmode=none;"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("server=(address=(" + defaultUnixSocket + "),priority=100);uid=root;protocol=unix;sslmode=none;"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("mysqlx://root:@[./tmp/mysql.sock, (../tmp/mysql.sock) ,(%2Ftmpsocket%2Fmysql.sock) , " + defaultUnixSocket + "]?protocol=unix&sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("mysqlx://root:@[(address=./tmp/mysql.sock,priority=100),(address=(../tmp/mysql.sock),priority=99),(address=(%2tmpsocket%2mysql.sock),priority=98),(address=" + defaultUnixSocket + ",priority=97)]?protocol=unix&sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("mysqlx://root:@[(address=" + defaultUnixSocket + ",priority=100)]?protocol=unix&sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession("mysqlx://root:@[(address=(" + defaultUnixSocket + "),priority=100)]?protocol=unix&sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession(new {
        server ="/tmp/mysql.sock1, (/tmp/mysql.sock2) ,(%2Ftmp%2Fmysql.sock3) , " + defaultUnixSocket,
        uid = "root",
        protocol ="unix",
        sslmode = MySqlSslMode.None
      }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession(new
      {
        server = "(address=/tmp/mysql.sock1, priority=100),(address=(/tmp/mysql.sock2),priority=99),(address=(%2Ftmp%2Fmysql.sock3),priority=98),(address=" + defaultUnixSocket + ",priority=97)",
        uid = "root",
        protocol = "unix",
        sslmode = MySqlSslMode.None
      }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession(new
      {
        server = "(address=" + defaultUnixSocket + ",priority=100)",
        uid = "root",
        protocol = "unix",
        sslmode = MySqlSslMode.None
      }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }

      using (var session = MySQLX.GetSession(new
      {
        server = "(address=(" + defaultUnixSocket + "),priority=100)",
        uid = "root",
        protocol = "unix",
        sslmode = MySqlSslMode.None
      }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(defaultUnixSocket, session.Settings.Server);
      }
    }
  }
}
