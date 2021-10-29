// Copyright (c) 2019, 2021, Oracle and/or its affiliates.
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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using NUnit.Framework;
using System;

namespace MySqlX.Data.Tests
{
  /// <summary>
  /// WL13368 - DNS SRV Support
  /// </summary>
  public class DnsSrvTests : BaseTest
  {
    [TestCase("server=localhost;port=33060;dns-srv=true;", "Specifying a port number with DNS SRV lookup is not permitted.")]
    [TestCase("server=localhost,10.10.10.10;dns-srv=true;", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [TestCase("host=localhost,10.10.10.10;dns-srv=TRUE;", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [TestCase("server=(address=localhost,priority=100), (address=10.10.10.10,priority=90);dns-srv=true;", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [TestCase("server=localhost;protocol=unix;Dns-Srv=true;", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    [TestCase("server=localhost;protocol=unixSocket;dns-srv=true;", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    [TestCase("server=localhost;connectionprotocol=unix;DnsSrv=true;", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    public void DnsSrvConnectionStringInvalidConfiguration(string connString, string exceptionMessage)
    {
      connString = connString.Replace("localhost", Host).Replace("33060", XPort);
      var exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connString));
      Assert.AreEqual(exceptionMessage, exception.Message);
    }

    [TestCase("server=localhost;port=33060;dns-srv=false;uid=test;password=test;")]
    [TestCase("server=localhost,10.10.10.10;dns-srv=false;uid=test;password=test;")]
    [TestCase("server=localhost,10.10.10.10;dns-srv=False;;uid=test;password=test")]
    [TestCase("server=(address=localhost,priority=100);DnsSrv=FALSE;uid=test;password=test;")]
    [TestCase("server=(address=localhost,priority=100),;dns-srv=false;uid=test;password=test;")]
    public void DnsSrvConnectionStringValidConfiguration(string connString)
    {
      connString = connString.Replace("localhost", Host).Replace("33060", XPort);
      using (var session = MySQLX.GetSession(connString))
        Assert.NotNull(session);
    }

    [Test]
    public void DnsSrvConnectionAnonymousTypeInvalidConfiguration()
    {
      var sb = new MySqlXConnectionStringBuilder();
      sb.DnsSrv = true;
      sb.Port = UInt32.Parse(XPort);
      sb.Server = Host;
      var exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(sb.ConnectionString));
      Assert.AreEqual(MySql.Data.Resources.DnsSrvInvalidConnOptionPort, exception.Message);
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetClient(sb.ConnectionString, new { pooling = new { enabled = true } }));
      Assert.AreEqual(MySql.Data.Resources.DnsSrvInvalidConnOptionPort, exception.Message);

      sb = new MySqlXConnectionStringBuilder();
      sb.DnsSrv = true;
      sb.Server = $"{Host}, 10.10.10.10";
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(sb.ConnectionString));
      Assert.AreEqual(MySql.Data.Resources.DnsSrvInvalidConnOptionMultihost, exception.Message);
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetClient(sb.ConnectionString, new { pooling = new { enabled = true } }));
      Assert.AreEqual(MySql.Data.Resources.DnsSrvInvalidConnOptionMultihost, exception.Message);

      sb = new MySqlXConnectionStringBuilder();
      sb.DnsSrv = true;
      sb.Server = $"(address={Host},priority=100), (address=10.10.10.10,priority=90)";
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(sb.ConnectionString));
      Assert.AreEqual(MySql.Data.Resources.DnsSrvInvalidConnOptionMultihost, exception.Message);

      var connDataHost = new { server = $"(address={Host},priority=100), (address=10.10.10.10,priority=90)", dnssrv = true };
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetClient(connDataHost, new { pooling = new { enabled = true } }));
      Assert.AreEqual(MySql.Data.Resources.DnsSrvInvalidConnOptionMultihost, exception.Message);

      var connDataPort = new { server = Host, port = XPort, dnssrv = true };
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetClient(connDataPort, new { pooling = new { enabled = true } }));
      Assert.AreEqual(MySql.Data.Resources.DnsSrvInvalidConnOptionPort, exception.Message);

      var connDataUnix = new { server = Host, protocol = "unix", dnssrv = true };
      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetClient(connDataUnix, new { pooling = new { enabled = true } }));
      Assert.AreEqual(MySql.Data.Resources.DnsSrvInvalidConnOptionUnixSocket, exception.Message);
    }

    [TestCase("mysqlx+srv://test:test@localhost:33060", "Specifying a port number with DNS SRV lookup is not permitted.")]
    [TestCase("mysqlx+srv://test:test@[192.1.10.10,localhost]", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [TestCase("mysqlx+srv://test:test@[192.1.10.10,localhost:33060]/test", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [TestCase("mysqlx+srv://test:test@[(address = server.example, priority = 50),(address = localhost:33060,priority=100)]", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [TestCase("mysqlx+srv://test:test@./tmp/mysql.sock?protocol=unix", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    [TestCase("mysqlx+srv://test:test@localhost?dns-srv=false;", "'dns-srv' cannot be set to false with DNS SRV lookup enabled.")]
    public void DnsSrvConnectionStringUriInvalidConfiguration(string connStringUri, string exceptionMessage)
    {
      connStringUri = connStringUri.Replace("localhost", Host).Replace("33060", XPort);
      var exception = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connStringUri));
      Assert.AreEqual(exceptionMessage, exception.Message);

      exception = Assert.Throws<ArgumentException>(() => MySQLX.GetClient(connStringUri, new { pooling = new { enabled = true } }));
      Assert.AreEqual(exceptionMessage, exception.Message);
    }

    [TestCase("mysqlx+srv://test:test@localhost")]
    [TestCase("mysqlx+srv://test:test@[localhost]")]
    [TestCase("mysqlx+srv://test:test@[(address=localhost,priority=100)]")]
    [TestCase("mysqlx+srv://test:test@localhost?dns-srv=true;")]
    [TestCase("server=www.google.com;user=test;password=test;dns-srv=true;")]
    public void DnsResolverNoHosts(string connString)
    {
      connString = connString.Replace("localhost", Host);
      var ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(connString));
      Assert.AreEqual(string.Format(MySql.Data.Resources.DnsSrvNoHostsAvailable, Host), ex.Message);
    }

    [Test]
    public void DnsResolverNoHostsPooling()
    {
      using (var client = MySQLX.GetClient($"mysqlx+srv://test:test@{Host}?dns-srv=true;", new { pooling = new { enabled = true } }))
      {
        var ex = Assert.Throws<MySqlException>(() => client.GetSession());
        Assert.AreEqual(string.Format(MySql.Data.Resources.DnsSrvNoHostsAvailable, Host), ex.Message);
      }
    }
  }
}