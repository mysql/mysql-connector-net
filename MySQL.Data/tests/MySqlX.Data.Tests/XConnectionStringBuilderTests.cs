// Copyright (c) 2018, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

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
      _connectionString = $"server=localhost;user=root;port={BaseTest.Port};";
      _xConnectionURI = $"mysqlx://root@localhost:{BaseTest.XPort}";
      _connectionStringWithSslMode = _connectionString + "sslmode=required;";
    }

    [Fact]
    public void SessionCanBeOpened()
    {
      Session session = null;
      session = MySQLX.GetSession(_xConnectionURI);
    }

    [Fact]
    public void ConnectionAfterSessionCanBeOpened()
    {
      Session session = null;
      session = MySQLX.GetSession(_xConnectionURI);

      var connection = new MySqlConnection(_connectionStringWithSslMode);
      connection.Open();
      connection.Close();

      session = MySQLX.GetSession(_xConnectionURI + "?sslca=../../../../MySql.Data.Tests/client.pfx&certificatepassword=pass");
      session.Close();
    }

    [Fact]
    public void Bug28151070_3()
    {
      var connection = new MySqlConnection(_connectionStringWithSslMode);
      connection.Open();
      connection.Close();

      Session session = null;
      session = MySQLX.GetSession(_xConnectionURI);
    }

    [Fact]
    public void Bug28151070_4()
    {
      var connection = new MySqlConnection(_connectionStringWithSslMode);
      connection.Open();
      connection.Close();
    }

    [Fact]
    public void Bug28151070_5()
    {
      Session session = null;
      session = MySQLX.GetSession(_xConnectionURI);

      var builder = new MySqlXConnectionStringBuilder();
      builder.Auth = MySqlAuthenticationMode.AUTO;
      builder.SslCa = "";
    }

    [Theory]
#if NET48 || NETCOREAPP3_0
    [InlineData(";tls-version=TlSv1.3", "Tls13")]
    [InlineData(";tls-version=TlSv1.2, tLsV11, TLS13, tls1.0", "Tls, Tls11, Tls12, Tls13")]
#endif
    [InlineData(";tls-version=TlSv1.2, tLsV11, tls1.0", "Tls, Tls11, Tls12")]
    [InlineData(";tls-version=TlSv1.2, SsLv3", null, "SsLv3")]
    public void ValidateTlsVersionOptionAsString(string options, string result, string error = null)
    {
      if (result != null)
      {
        MySqlXConnectionStringBuilder builder = new MySqlXConnectionStringBuilder(_connectionString + options);
        Assert.Equal(result, builder.TlsVersion);
      }
      else
      {
        string info = string.Empty;
#if NET48 || NETCOREAPP3_0
        info = ", TLSv1.3";
#endif
        Assert.ThrowsAny<ArgumentException>(() => { new MySqlXConnectionStringBuilder(_connectionString + options); });
      }
    }

    [Theory]
#if NET48 || NETCOREAPP3_0
    [InlineData("TlSv1.2, tLsV11, TLS13, tls1.0", "Tls, Tls11, Tls12, Tls13")]
    [InlineData("TlSv1.2, TLS13, SsLv3", null, "SsLv3")]
#endif
    [InlineData("TlSv1.2, tLsV11, tls1.0", "Tls, Tls11, Tls12")]
    [InlineData("TlSv1.2, SsLv3", null, "SsLv3")]
    public void ValidateTlsVersionOptionAsProperty(string options, string result, string error = null)
    {
      MySqlXConnectionStringBuilder builder = new MySqlXConnectionStringBuilder(_connectionString);
      if (result != null)
      {
        builder.TlsVersion = options;
        Assert.Equal(result, builder.TlsVersion);
      }
      else
      {
        string info = string.Empty;
#if NET48 || NETCOREAPP3_0
        info = ", TLSv1.3";
#endif
        Assert.ThrowsAny<ArgumentException>(() => { builder.TlsVersion = options; });
      }
    }

    [Theory]
#if NET48 || NETCOREAPP3_0
    [InlineData(MySqlSslMode.Prefered, "TlSv1.2, tLsV11, TLS13, tls1.0", "Tls, Tls11, Tls12, Tls13", null)]
    [InlineData(MySqlSslMode.None, "TlSv1.2, tLsV11, TLS13, tls1.0", null, null)]
    [InlineData(null, "TlSv1.2, tLsV11, TLS13, tls1.0", "Tls, Tls11, Tls12, Tls13", MySqlSslMode.None)]
#endif
    [InlineData(MySqlSslMode.Prefered, "TlSv1.2, tLsV11, tls1.0", "Tls, Tls11, Tls12", null)]
    [InlineData(MySqlSslMode.None, "TlSv1.2, tLsV11, tls1.0", null, null)]
    [InlineData(null, "TlSv1.2, tLsV11, tls1.0", "Tls, Tls11, Tls12", MySqlSslMode.None)]
    public void ValidateTlsVersionOptionAndSslMode(MySqlSslMode? sslMode1, string options, string result, MySqlSslMode? sslMode2)
    {
      MySqlXConnectionStringBuilder builder = new MySqlXConnectionStringBuilder(_connectionString);
      if (sslMode1.HasValue)
        builder.SslMode = sslMode1.Value;
      if (result != null)
      {
        builder.TlsVersion = options;
        Assert.Equal(result, builder.TlsVersion);
      }
      else
        Assert.ThrowsAny<ArgumentException>(() =>
        { builder.TlsVersion = options; });
      if (sslMode2.HasValue)
      {
        Assert.ThrowsAny<ArgumentException>(() =>
        { builder.SslMode = sslMode2.Value; });
      }
    }
  }
}
