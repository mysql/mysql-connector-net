// Copyright (c) 2019, 2022, Oracle and/or its affiliates.
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
using MySql.Data.Common.DnsClient;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySql.Data.MySqlClient.Tests
{
  /// <summary>
  /// WL13368 - DNS SRV Support
  /// </summary>
  public class DnsSrvTests : TestBase
  {
    [TestCase("server=localhost;dns=true;")]
    [TestCase("server=localhost;dns_srv=false;")]
    [TestCase("server=localhost;dns srv=true;")]
    [TestCase("server=localhost;dns-srv=foo;")]
    [TestCase("server=localhost;dnssrv=3;")]
    public void DnsSrvConnectionStringInvalidOptions(string connString)
    {
      var exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(connString));
    }

    [TestCase("server=localhost;port=33060;dns-srv=true;", "Specifying a port number with DNS SRV lookup is not permitted.")]
    [TestCase("server=localhost,10.10.10.10;dns-srv=true;", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [TestCase("host=localhost,10.10.10.10;dns-srv=TRUE;", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [TestCase("server=(address=localhost,priority=100), (address=10.10.10.10,priority=90);dns-srv=true;", "Specifying multiple host names with DNS SRV lookup is not permitted.")]
    [TestCase("server=localhost;protocol=unix;Dns-Srv=true;", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    [TestCase("server=localhost;protocol=unixSocket;dns-srv=true;", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    [TestCase("server=localhost;connectionprotocol=unix;DnsSrv=true;", "Using Unix domain sockets with DNS SRV lookup is not permitted.")]
    public void DnsSrvConnectionStringInvalidConfiguration(string connString, string exceptionMessage)
    {
      var exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(connString));
      Assert.AreEqual(exceptionMessage, exception.Message);
    }

    [TestCase("server=localhost;port=33060;dns-srv=false;")]
    [TestCase("server=localhost,10.10.10.10;dns-srv=false;")]
    [TestCase("server=localhost,10.10.10.10;dns-srv=False;")]
    [TestCase("server=(address=localhost,priority=100);DnsSrv=FALSE;")]
    [TestCase("server=(address=localhost,priority=100),;dns-srv=false;")]
    [TestCase("server=localhost;protocol=unix;DNS-SRV=FALSE;")]
    [TestCase("server=localhost;protocol=unixSocket;dns-srv=false;")]
    [TestCase("server=localhost;protocol=unix;dns-srv=false;")]
    public void DnsSrvConnectionStringValidConfiguration(string connString)
    {
      var conn = new MySqlConnection(connString);
      Assert.NotNull(conn);
    }

    [Test]
    public void DnsSrvConnectionAnonymousTypeInvalidConfiguration()
    {
      var sb = new MySqlConnectionStringBuilder();
      sb.DnsSrv = true;
      sb.Port = 3306;
      sb.Server = "localhost";
      var exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(sb.ConnectionString));
      Assert.AreEqual(Resources.DnsSrvInvalidConnOptionPort, exception.Message);

      sb = new MySqlConnectionStringBuilder();
      sb.DnsSrv = true;
      sb.Server = "_mysqlx._tcp.foo.abc.com";
      sb.ConnectionProtocol = MySqlConnectionProtocol.Unix;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(sb.ConnectionString));
      Assert.AreEqual(Resources.DnsSrvInvalidConnOptionUnixSocket, exception.Message);

      sb = new MySqlConnectionStringBuilder();
      sb.DnsSrv = true;
      sb.Server = "localhost, 10.10.10.10";
      sb.ConnectionProtocol = MySqlConnectionProtocol.Unix;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(sb.ConnectionString));
      Assert.AreEqual(Resources.DnsSrvInvalidConnOptionMultihost, exception.Message);
    }

    [Test]
    public void DnsSrvRecordsTest()
    {
      DnsSrvRecord[] dnsRecords =
      {
        new DnsSrvRecord(3306, 100, "target_1", 0),
        new DnsSrvRecord(3306, 120, "target_2", 0),
        new DnsSrvRecord(3306, 80, "target_3", 0),
        new DnsSrvRecord(3306, 70, "target_4", 10),
        new DnsSrvRecord(3306, 60, "target_5", 50)
      };

      IEnumerable<DnsSrvRecord> expectedOrder = new[]
      {
        new DnsSrvRecord(3306, 60, "target_5", 50),
        new DnsSrvRecord(3306, 70, "target_4", 10),
        new DnsSrvRecord(3306, 80, "target_3", 0),
        new DnsSrvRecord(3306, 100, "target_1", 0),
        new DnsSrvRecord(3306, 120, "target_2", 0)
      };

      var sortedRecords = DnsSrv.SortSrvRecords(dnsRecords.ToList());

      Assert.True(sortedRecords.Select(r => r.Target).SequenceEqual(expectedOrder.Select(r => r.Target)));
    }
  }
}