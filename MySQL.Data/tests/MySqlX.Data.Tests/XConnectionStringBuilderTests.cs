// Copyright (c) 2018, 2022, Oracle and/or its affiliates.
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
using NUnit.Framework;
using System;
using System.Data;

namespace MySqlX.Data.Tests
{
  /// <summary>
  /// The purpose of this class is to incorporate MySqlBaseConnectionStringBuilder, MySqlConnectionStringBuilder and MySqlXConnectionStringBuilder
  /// tests that aren't affected by previously opened connections/sessions.
  /// </summary>
  public class XConnectionStringBuilderTests
  {
    private static string _connectionString;
    private static string _xConnectionURI;
    private static string _connectionStringWithSslMode;

    static XConnectionStringBuilderTests()
    {
      _connectionString = $"server={BaseTest.Host};user={BaseTest.RootUser};password={BaseTest.RootPassword};port={BaseTest.Port};";
      _xConnectionURI = $"mysqlx://{BaseTest.RootUser}:{BaseTest.RootPassword}@{BaseTest.Host}:{BaseTest.XPort}";
      _connectionStringWithSslMode = _connectionString + "sslmode=required;";
    }

    [Test]
    public void SessionCanBeOpened()
    {
      using (var session = MySQLX.GetSession(_xConnectionURI))
        Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
    }

    [Test]
    public void ConnectionAfterSessionCanBeOpened()
    {
      if (!Platform.IsWindows()) Assert.Ignore("Check for Linux OS");

      using (var session = MySQLX.GetSession(_xConnectionURI))
        Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);

      using (var connection = new MySqlConnection(_connectionStringWithSslMode))
      {
        connection.Open();
        Assert.AreEqual(ConnectionState.Open, connection.State);
      }

      using (var session = MySQLX.GetSession(_xConnectionURI + "?sslca=client.pfx&certificatepassword=pass"))
        Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
    }

#if !NET452
    [TestCase(";tls-version=TlSv1.3", "Tls13")]
    [TestCase(";tls-version=TlSv1.2, tLsV11, TLS13, tls1.0", "Tls12, Tls13")]
#endif
    [TestCase(";tls-version=TlSv1.2, tLsV11, tls1.0", "Tls12")]
    [TestCase(";tls-version=TlSv1.2, SsLv3", "Tls12")]
    public void ValidateTlsVersionOptionAsString(string options, string result)
    {
      MySqlXConnectionStringBuilder builder = new MySqlXConnectionStringBuilder(_connectionString + options);
      Assert.AreEqual(result, builder.TlsVersion);
    }

#if !NET452
    [TestCase("TlSv1.2, tLsV11, TLS13, tls1.0", "Tls12, Tls13")]
    [TestCase("TlSv1.2, TLS13, SsLv3", "Tls12, Tls13")]
#endif
    [TestCase("TlSv1.2, tLsV11, tls1.0", "Tls12")]
    [TestCase("TlSv1.2, SsLv3", "Tls12")]
    public void ValidateTlsVersionOptionAsProperty(string options, string result)
    {
      MySqlXConnectionStringBuilder builder = new MySqlXConnectionStringBuilder(_connectionString);

      builder.TlsVersion = options;
      Assert.AreEqual(result, builder.TlsVersion);
    }

#if !NET452
    [TestCase(MySqlSslMode.Prefered, "TlSv1.2, tLsV11, TLS13, tls1.0", "Tls12, Tls13")]
    [TestCase(MySqlSslMode.Disabled, "TlSv1.2, tLsV11, TLS13, tls1.0", "TLS12, TLS13")]
    [TestCase(null, "TlSv1.2, tLsV11, TLS13, tls1.0", "Tls12, Tls13")]
#endif
    [TestCase(MySqlSslMode.Prefered, "TlSv1.2, tLsV11, tls1.0", "Tls12")]
    [TestCase(MySqlSslMode.Disabled, "TlSv1.2, tLsV11, tls1.0", "Tls12")]
    [TestCase(null, "TlSv1.2, tLsV11, tls1.0", "Tls12")]
    public void ValidateTlsVersionOptionAndSslMode(MySqlSslMode? sslMode1, string options, string result)
    {
      MySqlXConnectionStringBuilder builder = new MySqlXConnectionStringBuilder(_connectionString);

      if (sslMode1.HasValue)
        builder.SslMode = sslMode1.Value;

      if (result != null)
      {
        builder.TlsVersion = options;
        StringAssert.AreEqualIgnoringCase(result, builder.TlsVersion);
      }
      else
        Assert.Throws<ArgumentException>(() => { builder.TlsVersion = options; });
    }
  }
}
