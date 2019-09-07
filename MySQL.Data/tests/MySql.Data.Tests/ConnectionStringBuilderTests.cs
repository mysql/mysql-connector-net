// Copyright (c) 2013, 2019, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class ConnectionStringBuilderTests : TestBase
  {
    public ConnectionStringBuilderTests(TestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void Simple()
    {
      MySqlConnectionStringBuilder sb = null;
      sb = new MySqlConnectionStringBuilder();
      sb.ConnectionString = "server=localhost;uid=reggie;pwd=pass;port=1111;" +
          "connection timeout=23; pooling=true; min pool size=33; " +
          "max pool size=66;keepalive=1";
      Assert.Equal("localhost", sb.Server);
      Assert.Equal("reggie", sb.UserID);
      Assert.Equal("pass", sb.Password);
      Assert.Equal(1111, Convert.ToInt32(sb.Port));
      Assert.Equal(23, Convert.ToInt32(sb.ConnectionTimeout));
      Assert.True(sb.Pooling);
      Assert.Equal(33, Convert.ToInt32(sb.MinimumPoolSize));
      Assert.Equal(66, Convert.ToInt32(sb.MaximumPoolSize));
      Assert.Equal(1, Convert.ToInt32(sb.Keepalive));
      Exception  ex = Assert.Throws<ArgumentException>(()=> (sb.ConnectionString = "server=localhost;badkey=badvalue"));
#if !NETCOREAPP3_0
      Assert.Equal($"Option not supported.{Environment.NewLine}Parameter name: badkey", ex.Message);        
#else
      Assert.Equal("Option not supported. (Parameter 'badkey')", ex.Message);
#endif
      sb.Clear();
      Assert.Equal(15, Convert.ToInt32(sb.ConnectionTimeout));
      Assert.True(sb.Pooling);
      Assert.True(sb.Pooling);
      Assert.Equal(3306, Convert.ToInt32(sb.Port));
      Assert.Equal(String.Empty, sb.Server);
      Assert.False(sb.PersistSecurityInfo);
      Assert.Equal(0, Convert.ToInt32(sb.ConnectionLifeTime));
      Assert.False(sb.ConnectionReset);
      Assert.Equal(0, Convert.ToInt32(sb.MinimumPoolSize));
      Assert.Equal(100, Convert.ToInt32(sb.MaximumPoolSize));
      Assert.Equal(String.Empty, sb.UserID);
      Assert.Equal(String.Empty, sb.Password);
      Assert.False(sb.UseUsageAdvisor);
      Assert.Equal(String.Empty, sb.CharacterSet);
      Assert.False(sb.UseCompression);
      Assert.Equal("MYSQL", sb.PipeName);
      Assert.False(sb.Logging);
      Assert.True(sb.AllowBatch);
      Assert.False(sb.ConvertZeroDateTime);
      Assert.Equal("MYSQL", sb.SharedMemoryName);
      Assert.Equal(String.Empty, sb.Database);
      Assert.Equal(MySqlConnectionProtocol.Sockets, sb.ConnectionProtocol);
      Assert.False(sb.AllowZeroDateTime);
      Assert.False(sb.UsePerformanceMonitor);
      Assert.Equal(25, Convert.ToInt32(sb.ProcedureCacheSize));
      Assert.Equal(0, Convert.ToInt32(sb.Keepalive));
    }

    /// <summary>
    /// Bug #37955 Connector/NET keeps adding the same option to the connection string
    /// </summary>
    [Fact]
    public void SettingValueMultipeTimes()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      s["database"] = "test";
      s["database"] = "test2";
      Assert.Equal("database=test2", s.ConnectionString);
    }

    /// <summary>
    /// Bug #51209	error on parameter without value on connectionstring
    /// </summary>
    [Fact]
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
    [Fact]
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

    [Fact]
    public void SettingCheckParameters()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder("server=localhost;check parameters=false");
      Assert.False(s.CheckParameters);
    }

    [Fact]
    public void SettingInvalidKeyThrowsArgumentException()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      Exception ex = Assert.Throws<ArgumentException>(() => (s["foo keyword"] = "foo"));
#if !NETCOREAPP3_0
      Assert.Equal($"Option not supported.{Environment.NewLine}Parameter name: foo keyword", ex.Message);           
#else
      Assert.Equal($"Option not supported. (Parameter 'foo keyword')", ex.Message);
#endif
    }

    /// <summary>
    /// Bug #66880	Keyword not supported. Parameter name: AttachDbFilename.
    /// </summary>
    [Fact]
    public void SafeTryGetValue()
    {
      object obj;
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder("server=localhost;");
      s.TryGetValue("unknownproperty", out obj);
      Assert.Null(obj);
    }

#if !NET452
    [Fact]
    public void DotnetCoreNotCurrentlySupported()
    {
      List<string> options = new List<string>(new string[]
      {
        "sharedmemoryname",
        "pipe",
        "useperformancemonitor",
#if NETCOREAPP1_1
        "logging",
        "useusageadvisor",
        "interactivesession",
        "replication"
#endif
      });
      if (Platform.IsWindows())
        options.Add("integratedsecurity");

      foreach(string option in options)
      {
        PlatformNotSupportedException ex = Assert.ThrowsAny<PlatformNotSupportedException>(() => 
        {
          MySqlConnectionStringBuilder connString = new MySqlConnectionStringBuilder($"server=localhost;user=root;password=;{option}=dummy");
        });
      }

      MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder();
      Assert.ThrowsAny<PlatformNotSupportedException>(() => csb.SharedMemoryName = "dummy");
      if (Platform.IsWindows())
        Assert.ThrowsAny<PlatformNotSupportedException>(() => csb.IntegratedSecurity = true);
      Assert.ThrowsAny<PlatformNotSupportedException>(() => csb.PipeName = "dummy");
      Assert.ThrowsAny<PlatformNotSupportedException>(() => csb.UsePerformanceMonitor = true);
#if NETCOREAPP1_1
      Assert.ThrowsAny<PlatformNotSupportedException>(() => csb.Logging = true);
      Assert.ThrowsAny<PlatformNotSupportedException>(() => csb.UseUsageAdvisor = true);
      Assert.ThrowsAny<PlatformNotSupportedException>(() => csb.Replication = true);
#endif
      csb.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      Assert.ThrowsAny<PlatformNotSupportedException>(() => csb.ConnectionProtocol = MySqlConnectionProtocol.SharedMemory);
      Assert.ThrowsAny<PlatformNotSupportedException>(() => csb.ConnectionProtocol = MySqlConnectionProtocol.NamedPipe);
    }
#endif

    [Fact]
    public void IncorrectAuthOptionThrowsArgumentException()
    {
      string[] values = { "OTHER", "Other", "MYSQL42", "PlaINs" };
      foreach (var value in values)
      {
        Exception ex = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder(String.Format("server=localhost;aUth={0}", value)));
        Assert.Equal(String.Format("Value '{0}' is not of the correct type.", value), ex.Message);
      }
    }

    [Fact]
    public void CaseInsensitiveAuthOption()
    {
      string[,] values = new string[,] {
        { "PLAIN", "plain", "PLAin", "PlaIn" },
        { "MYSQL41", "MySQL41", "mysql41", "mYSqL41" },
        { "EXTERNAL", "external", "exterNAL", "eXtERNal" }
      };

      for (int i = 0; i < values.GetLength(0); i++)
      {
        for (int j = 0; j < values.GetLength(1); j++)
        {
          var builder = new MySqlXConnectionStringBuilder(String.Format("server=localhost;auth={0}", values[i, j]));
          Assert.Equal((MySqlAuthenticationMode)(i + 1), builder.Auth);
        }
      }
    }

    // Bug #28157737 TABLE CACHING IS NOT SUPPORTED IN THE MYSQLCONNECTIONSTRINGBUILDER CLASS
    [Fact]
    public void SettingTableCachingRaisesException()
    {
      var builder = new MySqlConnectionStringBuilder();
      builder.TableCaching = true;
      Assert.True(builder.TableCaching);
    }
  }
}
