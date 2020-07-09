// Copyright (c) 2013, 2020, Oracle and/or its affiliates. All rights reserved.
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
using NUnit.Framework;

namespace MySql.Data.MySqlClient.Tests
{
  public class ConnectionStringBuilderTests : TestBase
  {
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
      Exception  ex = Assert.Throws<ArgumentException>(()=> sb.ConnectionString = "server=localhost;badkey=badvalue");
#if !(NETCOREAPP3_1 || NET5_0)
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

    [Test]
    public void SettingInvalidKeyThrowsArgumentException()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      Exception ex = Assert.Throws<ArgumentException>(() => s["foo keyword"] = "foo");
#if NET452 || NET48
      Assert.AreEqual($"Option not supported.{Environment.NewLine}Parameter name: foo keyword", ex.Message);           
#else
      Assert.AreEqual($"Option not supported. (Parameter 'foo keyword')", ex.Message);
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

#if NETCOREAPP3_1
    [Test]
    public void DotnetCoreNotCurrentlySupported()
    {
      List<string> options = new List<string>(new string[]
      {
        "sharedmemoryname",
        "pipe",
        "useperformancemonitor",
      });
      if (Platform.IsWindows())
        options.Add("integratedsecurity");

      foreach(string option in options)
      {
        PlatformNotSupportedException ex = Assert.Throws<PlatformNotSupportedException>(() => 
        {
          MySqlConnectionStringBuilder connString = new MySqlConnectionStringBuilder($"server=localhost;user=root;password=;{option}=dummy");
        });
      }

      MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder();
      Assert.Throws<PlatformNotSupportedException>(() => csb.SharedMemoryName = "dummy");
      if (Platform.IsWindows())
        Assert.Throws<PlatformNotSupportedException>(() => csb.IntegratedSecurity = true);
      Assert.Throws<PlatformNotSupportedException>(() => csb.PipeName = "dummy");
      Assert.Throws<PlatformNotSupportedException>(() => csb.UsePerformanceMonitor = true);
      csb.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      Assert.Throws<PlatformNotSupportedException>(() => csb.ConnectionProtocol = MySqlConnectionProtocol.SharedMemory);
      Assert.Throws<PlatformNotSupportedException>(() => csb.ConnectionProtocol = MySqlConnectionProtocol.NamedPipe);
    }
#endif

    [Test]
    public void IncorrectAuthOptionThrowsArgumentException()
    {
      string[] values = { "OTHER", "Other", "MYSQL42", "PlaINs" };
      foreach (var value in values)
      {
        Exception ex = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder(String.Format("server=localhost;aUth={0}", value)));
        Assert.AreEqual(String.Format("Value '{0}' is not of the correct type.", value), ex.Message);
      }
    }

    [Test]
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
          Assert.AreEqual((MySqlAuthenticationMode)(i + 1), builder.Auth);
        }
      }
    }

    // Bug #28157737 TABLE CACHING IS NOT SUPPORTED IN THE MYSQLCONNECTIONSTRINGBUILDER CLASS
    [Test]
    public void SettingTableCachingRaisesException()
    {
      var builder = new MySqlConnectionStringBuilder();
      builder.TableCaching = true;
      Assert.True(builder.TableCaching);
    }
  }
}