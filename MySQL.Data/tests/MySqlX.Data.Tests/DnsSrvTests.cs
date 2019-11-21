// Copyright (c) 2019, Oracle and/or its affiliates. All rights reserved.
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
  /// <summary>
  /// WL13368 - DNS SRV Support
  /// </summary>
  public class DnsSrvTests : BaseTest
  {
    [Theory]
    [InlineData("server=localhost;port=33060;dns-srv=true;", "Specifying a port number with DNS SRV lookup is not permitted.")]
    [InlineData("server=localhost,10.10.10.10;dns-srv=true;", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [InlineData("address=localhost,10.10.10.10;dns-srv=TRUE;", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [InlineData("server=(address=localhost,priority=100), (address=10.10.10.10,priority=90);dns-srv=true;", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [InlineData("server=localhost;protocol=unix;Dns-Srv=true;", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    [InlineData("server=localhost;protocol=unixSocket;dns-srv=true;", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    [InlineData("server=localhost;connectionprotocol=unix;DnsSrv=true;", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    public void DnsSrvConnectionStringInvalidConfiguration(string connString, string exceptionMessage)
    {
      var exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connString));
      Assert.Equal(exceptionMessage, exception.Message);
    }

    [Theory]
    [InlineData("server=localhost;port=33060;dns-srv=false;uid=test;password=test;")]
    [InlineData("server=localhost,10.10.10.10;dns-srv=false;uid=test;password=test;")]
    [InlineData("server=localhost,10.10.10.10;dns-srv=False;;uid=test;password=test")]
    [InlineData("server=(address=localhost,priority=100);DnsSrv=FALSE;uid=test;password=test;")]
    [InlineData("server=(address=localhost,priority=100),;dns-srv=false;uid=test;password=test;")]
    public void DnsSrvConnectionStringValidConfiguration(string connString)
    {
      using (var session = MySQLX.GetSession(connString))
        Assert.NotNull(session);
    }

    [Fact]
    public void DnsSrvConnectionAnonymousTypeInvalidConfiguration()
    {
      var sb = new MySqlXConnectionStringBuilder();
      sb.DnsSrv = true;
      sb.Port = 3306;
      sb.Server = "localhost";
      var exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(sb.ConnectionString));
      Assert.Equal(MySql.Data.Resources.DnsSrvInvalidConnOptionPort, exception.Message);
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetClient(sb.ConnectionString, new { pooling = new { enabled = true } }));
      Assert.Equal(MySql.Data.Resources.DnsSrvInvalidConnOptionPort, exception.Message);

      sb = new MySqlXConnectionStringBuilder();
      sb.DnsSrv = true;
      sb.Server = "localhost, 10.10.10.10";
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(sb.ConnectionString));
      Assert.Equal(MySql.Data.Resources.DnsSrvInvalidConnOptionMultihost, exception.Message);
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetClient(sb.ConnectionString, new { pooling = new { enabled = true } }));
      Assert.Equal(MySql.Data.Resources.DnsSrvInvalidConnOptionMultihost, exception.Message);

      sb = new MySqlXConnectionStringBuilder();
      sb.DnsSrv = true;
      sb.Server = "(address=localhost,priority=100), (address=10.10.10.10,priority=90)";
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(sb.ConnectionString));
      Assert.Equal(MySql.Data.Resources.DnsSrvInvalidConnOptionMultihost, exception.Message);

      var connDataHost = new { server = "(address=localhost,priority=100), (address=10.10.10.10,priority=90)", dnssrv = true };
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetClient(connDataHost, new { pooling = new { enabled = true } }));
      Assert.Equal(MySql.Data.Resources.DnsSrvInvalidConnOptionMultihost, exception.Message);

      var connDataPort = new { server = "localhost", port = 33060, dnssrv = true };
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetClient(connDataPort, new { pooling = new { enabled = true } }));
      Assert.Equal(MySql.Data.Resources.DnsSrvInvalidConnOptionPort, exception.Message);

      var connDataUnix = new { server = "localhost", protocol = "unix", dnssrv = true };
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetClient(connDataUnix, new { pooling = new { enabled = true } }));
      Assert.Equal(MySql.Data.Resources.DnsSrvInvalidConnOptionUnixSocket, exception.Message);
    }

    [Theory]
    [InlineData("mysqlx+srv://test:test@localhost:33060", "Specifying a port number with DNS SRV lookup is not permitted.")]
    [InlineData("mysqlx+srv://test:test@[192.1.10.10,127.0.0.1]", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [InlineData("mysqlx+srv://test:test@[192.1.10.10,127.0.0.1:33060]/test", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [InlineData("mysqlx+srv://test:test@[(address = server.example, priority = 50),(address = 127.0.0.1:33060,priority=100)]", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [InlineData("mysqlx+srv://test:test@./tmp/mysql.sock?protocol=unix", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    [InlineData("mysqlx+srv://test:test@localhost?dns-srv=false;", "'dns-srv' cannot be set to false with DNS SRV lookup enabled.")]
    public void DnsSrvConnectionStringUriInvalidConfiguration(string connStringUri, string exceptionMessage)
    {
      var exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connStringUri));
      Assert.Equal(exceptionMessage, exception.Message);

      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetClient(connStringUri, new { pooling = new { enabled = true } }));
      Assert.Equal(exceptionMessage, exception.Message);
    }

    [Theory]
    [InlineData("mysqlx+srv://test:test@localhost")]
    [InlineData("mysqlx+srv://test:test@[localhost]")]
    [InlineData("mysqlx+srv://test:test@[(address=localhost,priority=100)]")]
    [InlineData("mysqlx+srv://test:test@localhost?dns-srv=true;")]
    public void DnsResolverNoHosts(string connString)
    {
      var ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(connString));
      Assert.Equal(string.Format(MySql.Data.Resources.DnsSrvNoHostsAvailable, "localhost"), ex.Message);
    }

    [Fact]
    public void DnsResolverNoHostsPooling()
    {
      using (var client = MySQLX.GetClient("mysqlx+srv://test:test@127.0.0.1?dns-srv=true;", new { pooling = new { enabled = true } }))
      {
        var ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.Equal(string.Format(MySql.Data.Resources.DnsSrvNoHostsAvailable, "127.0.0.1"), ex.Message);
      }
    }
  }
}