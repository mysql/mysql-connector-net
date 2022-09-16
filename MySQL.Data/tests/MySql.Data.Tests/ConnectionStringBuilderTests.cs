// Copyright (c) 2013, 2022, Oracle and/or its affiliates.
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
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  public class ConnectionStringBuilderTests : TestBase
  {
    private string _sslCa;

    public ConnectionStringBuilderTests()
    {
      string cPath = TestContext.CurrentContext.TestDirectory + "\\";

      _sslCa = cPath + "ca.pem";
    }

    [Test]
    public void Simple()
    {
      MySqlConnectionStringBuilder sb = null;
      sb = new MySqlConnectionStringBuilder();
      sb.ConnectionString = "server=localhost;uid=reggie;pwd=pass;port=1111;" +
          "connection timeout=23; pooling=true; min pool size=33; " +
          "max pool size=66;keepalive=1";
      Assert.AreEqual("localhost", sb.Server);
      Assert.AreEqual("reggie", sb.UserID);
      Assert.AreEqual("pass", sb.Password);
      Assert.AreEqual(1111, Convert.ToInt32(sb.Port));
      Assert.AreEqual(23, Convert.ToInt32(sb.ConnectionTimeout));
      Assert.True(sb.Pooling);
      Assert.AreEqual(33, Convert.ToInt32(sb.MinimumPoolSize));
      Assert.AreEqual(66, Convert.ToInt32(sb.MaximumPoolSize));
      Assert.AreEqual(1, Convert.ToInt32(sb.Keepalive));
      Exception ex = Assert.Throws<ArgumentException>(() => sb.ConnectionString = "server=localhost;badkey=badvalue");
#if NETFRAMEWORK
      Assert.AreEqual($"Option not supported.{Environment.NewLine}Parameter name: badkey", ex.Message);
#else
      Assert.AreEqual("Option not supported. (Parameter 'badkey')", ex.Message);
#endif
      sb.Clear();
      Assert.AreEqual(15, Convert.ToInt32(sb.ConnectionTimeout));
      Assert.True(sb.Pooling);
      Assert.True(sb.Pooling);
      Assert.AreEqual(3306, Convert.ToInt32(sb.Port));
      Assert.AreEqual(String.Empty, sb.Server);
      Assert.False(sb.PersistSecurityInfo);
      Assert.AreEqual(0, Convert.ToInt32(sb.ConnectionLifeTime));
      Assert.False(sb.ConnectionReset);
      Assert.AreEqual(0, Convert.ToInt32(sb.MinimumPoolSize));
      Assert.AreEqual(100, Convert.ToInt32(sb.MaximumPoolSize));
      Assert.AreEqual(String.Empty, sb.UserID);
      Assert.AreEqual(String.Empty, sb.Password);
      Assert.False(sb.UseUsageAdvisor);
      Assert.AreEqual(String.Empty, sb.CharacterSet);
      Assert.False(sb.UseCompression);
      Assert.AreEqual("MYSQL", sb.PipeName);
      Assert.False(sb.Logging);
      Assert.True(sb.AllowBatch);
      Assert.False(sb.ConvertZeroDateTime);
      Assert.AreEqual("MYSQL", sb.SharedMemoryName);
      Assert.AreEqual(String.Empty, sb.Database);
      Assert.AreEqual(MySqlConnectionProtocol.Sockets, sb.ConnectionProtocol);
      Assert.False(sb.AllowZeroDateTime);
      Assert.False(sb.UsePerformanceMonitor);
      Assert.AreEqual(25, Convert.ToInt32(sb.ProcedureCacheSize));
      Assert.AreEqual(0, Convert.ToInt32(sb.Keepalive));
    }

    /// <summary>
    /// Bug #37955 Connector/NET keeps adding the same option to the connection string
    /// </summary>
    [Test]
    public void SettingValueMultipeTimes()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      s["database"] = "test";
      s["database"] = "test2";
      Assert.AreEqual("database=test2", s.ConnectionString);
    }

    /// <summary>
    /// Bug #51209	error on parameter without value on connectionstring
    /// </summary>
    [Test]
    public void NoValueGivenForConnectionStringOption()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      s.ConnectionString = "compress=;pooling=";
      Assert.False(s.UseCompression);
      Assert.True(s.Pooling);
    }

    /// <summary>
    /// Bug #59835	.Net Connector MySqlConnectionStringBuilder wrong result ContainsKey function
    /// </summary>
    [Test]
    public void ContainsKey()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      s["database"] = "test";
      Assert.True(s.ContainsKey("initial catalog"));
      s["server"] = "myserver";
      Assert.True(s.ContainsKey("server"));
      Assert.True(s.ContainsKey("host"));
      Assert.False(s.ContainsKey("badkey"));
    }

    [Test]
    public void SettingCheckParameters()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder("server=localhost;check parameters=false");
      Assert.False(s.CheckParameters);
    }

    [TestCase("foo keyword")]
    [TestCase("password4")]
    public void SettingInvalidKeyThrowsArgumentException(string invalidKey)
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      Exception ex = Assert.Throws<ArgumentException>(() => s[invalidKey] = "foo");
#if NETFRAMEWORK
      Assert.AreEqual($"Option not supported.{Environment.NewLine}Parameter name: {invalidKey}", ex.Message);
#else
      Assert.AreEqual($"Option not supported. (Parameter '{invalidKey}')", ex.Message);
#endif
    }

    /// <summary>
    /// Bug #66880	Keyword not supported. Parameter name: AttachDbFilename.
    /// </summary>
    [Test]
    public void SafeTryGetValue()
    {
      object obj;
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder("server=localhost;");
      s.TryGetValue("unknownproperty", out obj);
      Assert.Null(obj);
    }

#if !NETFRAMEWORK
    [Test]
    public void DotnetCoreNotCurrentlySupported()
    {
      List<string> options = new List<string>(new string[] { "useperformancemonitor", });
      if (Platform.IsWindows())
        options.Add("integratedsecurity");

      foreach (string option in options)
      {
        PlatformNotSupportedException ex = Assert.Throws<PlatformNotSupportedException>(() =>
        {
          MySqlConnectionStringBuilder connString = new MySqlConnectionStringBuilder($"server=localhost;user=root;password=;{option}=dummy");
        });
      }

      MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder();
      if (Platform.IsWindows())
        Assert.Throws<PlatformNotSupportedException>(() => csb.IntegratedSecurity = true);
      Assert.Throws<PlatformNotSupportedException>(() => csb.UsePerformanceMonitor = true);
    }
#endif

#if !NETFRAMEWORK
    [Test]
    public void NonWindowsOSNotCurrentlySupported()
    {
      if (Platform.IsWindows()) Assert.Ignore("This test is for non Windows OS only.");

      List<string> options = new List<string>(new string[]
      {
        "sharedmemoryname",
        "pipe",
        "kerberosauthmode"
      });

      foreach (string option in options)
      {
        PlatformNotSupportedException ex = Assert.Throws<PlatformNotSupportedException>(() =>
        {
          MySqlConnectionStringBuilder connString = new MySqlConnectionStringBuilder($"server=localhost;user=root;password=;{option}=dummy");
        });
      }

      MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder();
      Assert.Throws<PlatformNotSupportedException>(() => csb.SharedMemoryName = "dummy");
      Assert.Throws<PlatformNotSupportedException>(() => csb.PipeName = "dummy");
      csb.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      Assert.Throws<PlatformNotSupportedException>(() => csb.ConnectionProtocol = MySqlConnectionProtocol.SharedMemory);
      Assert.Throws<PlatformNotSupportedException>(() => csb.ConnectionProtocol = MySqlConnectionProtocol.NamedPipe);
      Assert.Throws<PlatformNotSupportedException>(() => csb.KerberosAuthMode = KerberosAuthMode.AUTO);
      Assert.Throws<PlatformNotSupportedException>(() => csb.KerberosAuthMode = KerberosAuthMode.GSSAPI);
      Assert.Throws<PlatformNotSupportedException>(() => csb.KerberosAuthMode = KerberosAuthMode.SSPI);
    }
#endif

    // Bug #28157737 TABLE CACHING IS NOT SUPPORTED IN THE MYSQLCONNECTIONSTRINGBUILDER CLASS
    [Test]
    public void SettingTableCachingRaisesException()
    {
      var builder = new MySqlConnectionStringBuilder();
      builder.TableCaching = true;
      Assert.True(builder.TableCaching);
    }

    /// <summary>
    /// WL14429 - [Classic] Support for authentication_kerberos_client authentication plugin
    /// Added new connection string option: DefaultAuthenticationPlugin
    /// </summary>
    /// <param name="method"></param>
    [TestCase("3")]
    [TestCase("invalidAuthenticationPlugin")]
    public void SettingInvalidAuthenticationMethod(string method)
    {
      var builder = new MySqlConnectionStringBuilder();
      builder.DefaultAuthenticationPlugin = " ";
      var ex = Assert.Throws<MySqlException>(() => builder.DefaultAuthenticationPlugin = method);
      StringAssert.AreEqualIgnoringCase(string.Format(Resources.AuthenticationMethodNotSupported, method), ex.Message);

      var connStr = $"server=localhost;userid=root;defaultauthenticationplugin={method}";
      ex = Assert.Throws<MySqlException>(() => new MySqlConnection(connStr));
      StringAssert.AreEqualIgnoringCase(string.Format(Resources.AuthenticationMethodNotSupported, method), ex.Message);

      connStr = "server=localhost;userid=root;defaultauthenticationplugin=";
      var conn = new MySqlConnection(connStr);
    }

    /// <summary>
    /// WL14653 - Support for MFA (multi factor authentication) authentication
    /// 'Password1' and 'pwd1' should be interpreted as aliases for 'password' connection option
    /// </summary>
    [TestCase("password1")]
    [TestCase("pwd1")]
    public void UsingPwdAliases(string alias)
    {
      string value = "test";
      var conn = new MySqlConnection($"{alias}={value};pwd2={value};pwd3={value}");
      StringAssert.AreEqualIgnoringCase(value, conn.Settings.Password);
      StringAssert.AreEqualIgnoringCase(value, conn.Settings.Password2);
      StringAssert.AreEqualIgnoringCase(value, conn.Settings.Password3);

      var connBuilder = new MySqlConnectionStringBuilder();
      connBuilder[alias] = value;
      connBuilder["pwd2"] = value;
      connBuilder["pwd3"] = value;
      StringAssert.AreEqualIgnoringCase(value, connBuilder.Password);
      StringAssert.AreEqualIgnoringCase(value, connBuilder.Password2);
      StringAssert.AreEqualIgnoringCase(value, connBuilder.Password3);
    }

    [Test, Description("Session BaseString/MySQLConnectionString Builder")]
    public void ConnectionStringBuilderClassicTests()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only.");

      MySqlConnectionStringBuilder mysql = new MySqlConnectionStringBuilder(Settings.ConnectionString);

      mysql.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      mysql.CharacterSet = "utf8mb4";
      mysql.SslMode = MySqlSslMode.Required;
      mysql.ConnectionTimeout = 10;
      mysql.Keepalive = 10;
      mysql.CertificateFile = _sslCa;
      mysql.CertificatePassword = "pass";
      mysql.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysql.CertificateThumbprint = "";

      using (var conn = new MySqlConnection(mysql.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.connectionState);
      }

      mysql = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      mysql.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      mysql.CharacterSet = "utf8mb4";
      mysql.SslMode = MySqlSslMode.VerifyCA;
      mysql.ConnectionTimeout = 10;
      mysql.Keepalive = 10;
      mysql.CertificateFile = _sslCa;
      mysql.CertificatePassword = "pass";
      mysql.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysql.CertificateThumbprint = "";

      using (var conn = new MySqlConnection(mysql.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.connectionState);
      }

      mysql = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      mysql.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      mysql.CharacterSet = "utf8mb4";
      mysql.SslMode = MySqlSslMode.Required;
      mysql.ConnectionTimeout = 10;
      mysql.Keepalive = 10;

      using (var conn = new MySqlConnection(mysql.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.connectionState);
      }

      ////Scenario-2
      ////MySQL Connection
      mysql = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      mysql.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      mysql.CharacterSet = "utf8mb4";
      mysql.SslMode = MySqlSslMode.Required;
      mysql.ConnectionTimeout = 10;
      mysql.Keepalive = 10;
      mysql.CertificateFile = _sslCa;
      mysql.CertificatePassword = "pass";
      mysql.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysql.CertificateThumbprint = "";
      mysql.AllowPublicKeyRetrieval = true;
      mysql.UseCompression = true;
      mysql.AllowBatch = true;
      mysql.Logging = true;
      mysql.DefaultCommandTimeout = 10;
      mysql.UseDefaultCommandTimeoutForEF = true;
      mysql.PersistSecurityInfo = true;
      mysql.AutoEnlist = true;
      mysql.IncludeSecurityAsserts = true;
      mysql.AllowZeroDateTime = true;
      mysql.ConvertZeroDateTime = true;
      mysql.UseUsageAdvisor = true;
      mysql.ProcedureCacheSize = 20;
      mysql.RespectBinaryFlags = true;
      mysql.TreatTinyAsBoolean = true;
      mysql.AllowUserVariables = true;
      mysql.InteractiveSession = true;
      mysql.FunctionsReturnString = true;
      mysql.UseAffectedRows = true;
      mysql.OldGuids = true;
      mysql.SqlServerMode = true;
      mysql.DefaultTableCacheAge = 20;
      mysql.CheckParameters = true;
      mysql.Replication = true;
      mysql.ConnectionLifeTime = 10;
      mysql.Pooling = true;
      mysql.MinimumPoolSize = 5;
      mysql.MaximumPoolSize = 100;
      mysql.ConnectionReset = true;
      mysql.CacheServerProperties = true;
      mysql.TreatBlobsAsUTF8 = true;
      mysql.BlobAsUTF8IncludePattern = "BLOBI";
      mysql.BlobAsUTF8ExcludePattern = "BLOBE";
      mysql.TableCaching = false;

      using (var conn = new MySqlConnection(mysql.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.connectionState);
      }

      mysql.SslCa = _sslCa;

      using (var conn = new MySqlConnection(mysql.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.connectionState);
      }

      ////Basic Scenarios
      string connectionstr = "server = " + mysql.Server + "; database = " + mysql.Database + "; protocol = Socket; port = "
          + mysql.Port + "; user id = " + mysql.UserID + "; password = " + mysql.Password +
          "; characterset = utf8mb4; sslmode = Required; connectiontimeout = 10; keepalive = 10; certificatefile = "
          + _sslCa + "; certificatepassword = pass; certificatestorelocation = LocalMachine; certificatethumbprint = ";
      using (var conn = new MySqlConnection(mysql.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.connectionState);
      }
    }

    /// <summary>
    /// Bug #33351775 [MySqlConnectionStringBuilder.TryGetValue always returns false]
    /// TryGetValue() method of ConnectionStringBuilder object was not overrided.
    /// </summary>
    [Test]
    public void TryGetValue()
    {
      MySqlConnectionStringBuilder connStringBuilder = new()
      {
        Server = "localhost",
        MinimumPoolSize = 10,
      };

      Assert.IsTrue(connStringBuilder.ContainsKey("data source"));
      Assert.IsTrue(connStringBuilder.TryGetValue("host", out var server));
      StringAssert.AreEqualIgnoringCase(connStringBuilder.Server, (string)server);

      Assert.IsTrue(connStringBuilder.ContainsKey("MinimumPoolSize"));
      Assert.IsTrue(connStringBuilder.TryGetValue("Minimum Pool Size", out var minpoolsize));
      Assert.AreEqual(connStringBuilder.MinimumPoolSize, (uint)minpoolsize);

      // Default value
      Assert.IsTrue(connStringBuilder.TryGetValue("allowpublickeyretrieval", out var allowpublickeyretrieval));
      Assert.AreEqual(connStringBuilder.GetOption("allowpublickeyretrieval").DefaultValue, allowpublickeyretrieval);

      // Non existing option
      Assert.IsFalse(connStringBuilder.TryGetValue("bar", out var nonexistingoption));
      Assert.IsNull(nonexistingoption);
    }

    /// <summary>
    /// WL15341 - [Classic] Support MIT Kerberos library on Windows
    /// New "KerberosAuthMode" connection option added.
    /// </summary>
    [Test]
    public void KerberosAuthModeTest()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only.");

      string connString;
      MySqlConnection conn;

      conn = new MySqlConnection();
      Assert.True(conn.Settings.KerberosAuthMode == KerberosAuthMode.AUTO);

      foreach (KerberosAuthMode mode in Enum.GetValues(typeof(KerberosAuthMode)))
      {
        connString = $"kerberosauthmode={mode}";
        conn = new MySqlConnection(connString);
        Assert.True(conn.Settings.KerberosAuthMode == mode);

        connString = $"kerberos auth mode={mode}";
        conn = new MySqlConnection(connString);
        Assert.True(conn.Settings.KerberosAuthMode == mode);
      }

      connString = "kerberosauthmode=INVALID";
      Assert.Throws<ArgumentException>(() => conn = new MySqlConnection(connString));
    }
  }
}