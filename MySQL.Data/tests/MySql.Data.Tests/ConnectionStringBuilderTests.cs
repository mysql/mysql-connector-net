// Copyright © 2013, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using NUnit.Framework.Legacy;
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
      Assert.That(sb.Server, Is.EqualTo("localhost"));
      Assert.That(sb.UserID, Is.EqualTo("reggie"));
      Assert.That(sb.Password, Is.EqualTo("pass"));
      Assert.That(Convert.ToInt32(sb.Port), Is.EqualTo(1111));
      Assert.That(Convert.ToInt32(sb.ConnectionTimeout), Is.EqualTo(23));
      Assert.That(sb.Pooling);
      Assert.That(Convert.ToInt32(sb.MinimumPoolSize), Is.EqualTo(33));
      Assert.That(Convert.ToInt32(sb.MaximumPoolSize), Is.EqualTo(66));
      Assert.That(Convert.ToInt32(sb.Keepalive), Is.EqualTo(1));
      Exception ex = Assert.Throws<ArgumentException>(() => sb.ConnectionString = "server=localhost;badkey=badvalue");
#if NETFRAMEWORK
      Assert.That(ex.Message, Is.EqualTo($"Option not supported{Environment.NewLine}Parameter name: badkey"));
#else
      Assert.That(ex.Message, Is.EqualTo("Option not supported (Parameter 'badkey')"));
#endif
      sb.Clear();
      Assert.That(Convert.ToInt32(sb.ConnectionTimeout), Is.EqualTo(15));
      Assert.That(sb.Pooling);
      Assert.That(sb.Pooling);
      Assert.That(Convert.ToInt32(sb.Port), Is.EqualTo(3306));
      Assert.That(sb.Server, Is.EqualTo(String.Empty));
      Assert.That(sb.PersistSecurityInfo, Is.False);
      Assert.That(Convert.ToInt32(sb.ConnectionLifeTime), Is.EqualTo(0));
      Assert.That(sb.ConnectionReset, Is.False);
      Assert.That(Convert.ToInt32(sb.MinimumPoolSize), Is.EqualTo(0));
      Assert.That(Convert.ToInt32(sb.MaximumPoolSize), Is.EqualTo(100));
      Assert.That(sb.UserID, Is.EqualTo(String.Empty));
      Assert.That(sb.Password, Is.EqualTo(String.Empty));
      Assert.That(sb.UseUsageAdvisor, Is.False);
      Assert.That(sb.CharacterSet, Is.EqualTo(String.Empty));
      Assert.That(sb.UseCompression, Is.False);
      Assert.That(sb.PipeName, Is.EqualTo("MYSQL"));
      Assert.That(sb.Logging, Is.False);
      Assert.That(sb.AllowBatch);
      Assert.That(sb.ConvertZeroDateTime, Is.False);
      Assert.That(sb.SharedMemoryName, Is.EqualTo("MYSQL"));
      Assert.That(sb.Database, Is.EqualTo(String.Empty));
      Assert.That(sb.ConnectionProtocol, Is.EqualTo(MySqlConnectionProtocol.Sockets));
      Assert.That(sb.AllowZeroDateTime, Is.False);
      Assert.That(sb.UsePerformanceMonitor, Is.False);
      Assert.That(Convert.ToInt32(sb.ProcedureCacheSize), Is.EqualTo(25));
      Assert.That(Convert.ToInt32(sb.Keepalive), Is.EqualTo(0));
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
      Assert.That(s.ConnectionString, Is.EqualTo("database=test2"));
    }

    /// <summary>
    /// Bug #51209	error on parameter without value on connectionstring
    /// </summary>
    [Test]
    public void NoValueGivenForConnectionStringOption()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      s.ConnectionString = "compress=;pooling=";
      Assert.That(s.UseCompression, Is.False);
      Assert.That(s.Pooling);
    }

    /// <summary>
    /// Bug #59835	.Net Connector MySqlConnectionStringBuilder wrong result ContainsKey function
    /// </summary>
    [Test]
    public void ContainsKey()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      s["database"] = "test";
      Assert.That(s.ContainsKey("initial catalog"));
      s["server"] = "myserver";
      Assert.That(s.ContainsKey("server"));
      Assert.That(s.ContainsKey("host"));
      Assert.That(s.ContainsKey("badkey"), Is.False);
    }

    [Test]
    public void SettingCheckParameters()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder("server=localhost;check parameters=false");
      Assert.That(s.CheckParameters, Is.False);
    }

    [TestCase("foo keyword")]
    [TestCase("password4")]
    public void SettingInvalidKeyThrowsArgumentException(string invalidKey)
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      Exception ex = Assert.Throws<ArgumentException>(() => s[invalidKey] = "foo");
#if NETFRAMEWORK
      Assert.That(ex.Message, Is.EqualTo($"Option not supported{Environment.NewLine}Parameter name: {invalidKey}"));
#else
      Assert.That(ex.Message, Is.EqualTo($"Option not supported (Parameter '{invalidKey}')"));
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
      Assert.That(obj, Is.Null);
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
      Assume.That(!Platform.IsWindows(), "This test is for non Windows OS only.");

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
      Assert.That(builder.TableCaching);
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
      Assert.That(ex.Message, Is.EqualTo(string.Format(Resources.AuthenticationMethodNotSupported, method)).IgnoreCase);

      var connStr = $"server=localhost;userid=root;defaultauthenticationplugin={method}";
      ex = Assert.Throws<MySqlException>(() => new MySqlConnection(connStr));
      Assert.That(ex.Message, Is.EqualTo(string.Format(Resources.AuthenticationMethodNotSupported, method)).IgnoreCase);

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
      Assert.That(conn.Settings.Password, Is.EqualTo(value).IgnoreCase);
      Assert.That(conn.Settings.Password2, Is.EqualTo(value).IgnoreCase);
      Assert.That(conn.Settings.Password3, Is.EqualTo(value).IgnoreCase);

      var connBuilder = new MySqlConnectionStringBuilder();
      connBuilder[alias] = value;
      connBuilder["pwd2"] = value;
      connBuilder["pwd3"] = value;
      Assert.That(connBuilder.Password, Is.EqualTo(value).IgnoreCase);
      Assert.That(connBuilder.Password2, Is.EqualTo(value).IgnoreCase);
      Assert.That(connBuilder.Password3, Is.EqualTo(value).IgnoreCase);
    }

    [Test, Description("Session BaseString/MySQLConnectionString Builder")]
    public void ConnectionStringBuilderClassicTests()
    {
      Assume.That(Platform.IsWindows(), "This test is for Windows OS only.");

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
        Assert.That(conn.connectionState, Is.EqualTo(ConnectionState.Open));
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
        Assert.That(conn.connectionState, Is.EqualTo(ConnectionState.Open));
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
        Assert.That(conn.connectionState, Is.EqualTo(ConnectionState.Open));
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
        Assert.That(conn.connectionState, Is.EqualTo(ConnectionState.Open));
      }

      mysql.SslCa = _sslCa;

      using (var conn = new MySqlConnection(mysql.ConnectionString))
      {
        conn.Open();
        Assert.That(conn.connectionState, Is.EqualTo(ConnectionState.Open));
      }

      ////Basic Scenarios
      string connectionstr = "server = " + mysql.Server + "; database = " + mysql.Database + "; protocol = Socket; port = "
          + mysql.Port + "; user id = " + mysql.UserID + "; password = " + mysql.Password +
          "; characterset = utf8mb4; sslmode = Required; connectiontimeout = 10; keepalive = 10; certificatefile = "
          + _sslCa + "; certificatepassword = pass; certificatestorelocation = LocalMachine; certificatethumbprint = ";
      using (var conn = new MySqlConnection(mysql.ConnectionString))
      {
        conn.Open();
        Assert.That(conn.connectionState, Is.EqualTo(ConnectionState.Open));
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

      Assert.That(connStringBuilder.ContainsKey("data source"));
      Assert.That(connStringBuilder.TryGetValue("host", out var server));
      Assert.That((string)server, Is.EqualTo(connStringBuilder.Server).IgnoreCase);

      Assert.That(connStringBuilder.ContainsKey("MinimumPoolSize"));
      Assert.That(connStringBuilder.TryGetValue("Minimum Pool Size", out var minpoolsize));
      Assert.That((uint)minpoolsize, Is.EqualTo(connStringBuilder.MinimumPoolSize));

      // Default value
      Assert.That(connStringBuilder.TryGetValue("allowpublickeyretrieval", out var allowpublickeyretrieval));
      Assert.That(allowpublickeyretrieval, Is.EqualTo(connStringBuilder.GetOption("allowpublickeyretrieval").DefaultValue));

      // Non existing option
      Assert.That(connStringBuilder.TryGetValue("bar", out var nonexistingoption), Is.False);
      Assert.That(nonexistingoption, Is.Null);
    }

    /// <summary>
    /// WL15341 - [Classic] Support MIT Kerberos library on Windows
    /// New "KerberosAuthMode" connection option added.
    /// </summary>
    [Test]
    public void KerberosAuthModeTest()
    {
      Assume.That(Platform.IsWindows(), "This test is for Windows OS only.");

      string connString;
      MySqlConnection conn;

      conn = new MySqlConnection();
      Assert.That(conn.Settings.KerberosAuthMode == KerberosAuthMode.AUTO);

      foreach (KerberosAuthMode mode in Enum.GetValues(typeof(KerberosAuthMode)))
      {
        connString = $"kerberosauthmode={mode}";
        conn = new MySqlConnection(connString);
        Assert.That(conn.Settings.KerberosAuthMode == mode);

        connString = $"kerberos auth mode={mode}";
        conn = new MySqlConnection(connString);
        Assert.That(conn.Settings.KerberosAuthMode == mode);
      }

      connString = "kerberosauthmode=INVALID";
      Assert.Throws<ArgumentException>(() => conn = new MySqlConnection(connString));
    }
  }
}
