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

using System;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  /// <summary>
  /// WL13368 - DNS SRV Support
  /// </summary>
  public class DnsSrvTests : TestBase
  {
    public DnsSrvTests(TestFixture fixture) : base(fixture) { }

    [Theory]
    [InlineData("server=localhost;dns=true;")]
    [InlineData("server=localhost;dns_srv=false;")]
    [InlineData("server=localhost;dns srv=true;")]
    [InlineData("server=localhost;dns-srv=foo;")]
    [InlineData("server=localhost;dnssrv=3;")]
    public void DnsSrvConnectionStringInvalidOptions(string connString)
    {
      var exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(connString));
    }

    [Theory]
    [InlineData("server=localhost;port=33060;dns-srv=true;", "Specifying a port number with DNS SRV lookup is not permitted.")]
    [InlineData("server=localhost,10.10.10.10;dns-srv=true;", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [InlineData("address=localhost,10.10.10.10;dns-srv=TRUE;", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [InlineData("server=(address=localhost,priority=100), (address=10.10.10.10,priority=90);dns-srv=true;", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [InlineData("server=localhost;protocol=unix;Dns-Srv=true;", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    [InlineData("server=localhost;protocol=unixSocket;dns-srv=true;", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    [InlineData("server=localhost;connectionprotocol=unix;DnsSrv=true;", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    [InlineData("server=www.google.com;user=root;password=;dns-srv=false;dns-srv= true;", "Connection option 'dns-srv' is duplicated.")]
    public void DnsSrvConnectionStringInvalidConfiguration(string connString, string exceptionMessage)
    {
      var exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(connString));
      Assert.Equal(exceptionMessage, exception.Message);
    }

    [Theory]
    [InlineData("server=localhost;port=33060;dns-srv=false;")]
    [InlineData("server=localhost,10.10.10.10;dns-srv=false;")]
    [InlineData("server=localhost,10.10.10.10;dns-srv=False;")]
    [InlineData("server=(address=localhost,priority=100);DnsSrv=FALSE;")]
    [InlineData("server=(address=localhost,priority=100),;dns-srv=false;")]
    [InlineData("server=localhost;protocol=unix;DNS-SRV=FALSE;")]
    [InlineData("server=localhost;protocol=unixSocket;dns-srv=false;")]
    [InlineData("server=localhost;protocol=unix;dns-srv=false;")]
    public void DnsSrvConnectionStringValidConfiguration(string connString)
    {
      var conn = new MySqlConnection(connString);
      Assert.NotNull(conn);
    }

    [Fact]
    public void DnsSrvConnectionAnonymousTypeInvalidConfiguration()
    {
      var sb = new MySqlConnectionStringBuilder();
      sb.DnsSrv = true;
      sb.Port = 3306;
      sb.Server = "localhost";
      var exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(sb.ConnectionString));
      Assert.Equal(Resources.DnsSrvInvalidConnOptionPort, exception.Message);

      sb = new MySqlConnectionStringBuilder();
      sb.DnsSrv = true;
      sb.Server = "_mysqlx._tcp.foo.abc.com";
      sb.ConnectionProtocol = MySqlConnectionProtocol.Unix;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(sb.ConnectionString));
      Assert.Equal(Resources.DnsSrvInvalidConnOptionUnixSocket, exception.Message);

      sb = new MySqlConnectionStringBuilder();
      sb.DnsSrv = true;
      sb.Server = "localhost, 10.10.10.10";
      sb.ConnectionProtocol = MySqlConnectionProtocol.Unix;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(sb.ConnectionString));
      Assert.Equal(Resources.DnsSrvInvalidConnOptionMultihost, exception.Message);
    }

    [Fact]
    public void DnsSrvResolverNoHosts()
    {
      var connString = "server=localhost;user id=" + ConnectionSettings.UserID +
        ";password=" + ConnectionSettings.Password + ";database=" + ConnectionSettings.Database + ";dns-srv=true;";

      using (var conn = new MySqlConnection(connString))
      {
        var ex = Assert.Throws<MySqlException>(() => conn.Open());
        Assert.Equal(string.Format(Resources.DnsSrvNoHostsAvailable, conn.Settings.Server), ex.Message);
      }
    }
  }
}
