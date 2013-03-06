// Copyright © 2004, 2011, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Data;
using System.IO;
using NUnit.Framework;

namespace MySql.Data.MySqlClient.Tests
{
  [TestFixture]
  public class ConnectionStringBuilder : BaseTest
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
      Assert.AreEqual(1111, sb.Port);
      Assert.AreEqual(23, sb.ConnectionTimeout);
      Assert.IsTrue(sb.Pooling);
      Assert.AreEqual(33, sb.MinimumPoolSize);
      Assert.AreEqual(66, sb.MaximumPoolSize);
      Assert.AreEqual(sb.Keepalive, 1);

      try
      {
        sb.ConnectionString = "server=localhost;badkey=badvalue";
        Assert.Fail("This should not work");
      }
      catch (ArgumentException)
      {
      }

      sb.Clear();
      Assert.AreEqual(15, sb.ConnectionTimeout);
      Assert.AreEqual(true, sb.Pooling);
      Assert.AreEqual(3306, sb.Port);
      Assert.AreEqual(String.Empty, sb.Server);
      Assert.AreEqual(false, sb.PersistSecurityInfo);
      Assert.AreEqual(0, sb.ConnectionLifeTime);
      Assert.IsFalse(sb.ConnectionReset);
      Assert.AreEqual(0, sb.MinimumPoolSize);
      Assert.AreEqual(100, sb.MaximumPoolSize);
      Assert.AreEqual(String.Empty, sb.UserID);
      Assert.AreEqual(String.Empty, sb.Password);
      Assert.AreEqual(false, sb.UseUsageAdvisor);
      Assert.AreEqual(String.Empty, sb.CharacterSet);
      Assert.AreEqual(false, sb.UseCompression);
      Assert.AreEqual("MYSQL", sb.PipeName);
      Assert.IsFalse(sb.Logging);
      Assert.IsFalse(sb.UseOldSyntax);
      Assert.IsTrue(sb.AllowBatch);
      Assert.IsFalse(sb.ConvertZeroDateTime);
      Assert.AreEqual("MYSQL", sb.SharedMemoryName);
      Assert.AreEqual(String.Empty, sb.Database);
      Assert.AreEqual(MySqlConnectionProtocol.Sockets, sb.ConnectionProtocol);
      Assert.IsFalse(sb.AllowZeroDateTime);
      Assert.IsFalse(sb.UsePerformanceMonitor);
      Assert.AreEqual(25, sb.ProcedureCacheSize);
      Assert.AreEqual(0, sb.Keepalive);
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

#if !CF
    [Test]
    public void EncryptKeyword()
    {
      string connStr = "database=test;uid=root;server=localhost;encrypt=yes";
      MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(connStr);
    }
#endif

    /// <summary>
    /// Bug #51209	error on parameter without value on connectionstring
    /// </summary>
    [Test]
    public void NoValueGivenForConnectionStringOption()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      s.ConnectionString = "compress=;pooling=";
      Assert.IsFalse(s.UseCompression);
      Assert.IsTrue(s.Pooling);
    }

#if !CF
    /// <summary>
    /// Bug #59835	.Net Connector MySqlConnectionStringBuilder wrong result ContainsKey function
    /// </summary>
    [Test]
    public void ContainsKey()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      s["database"] = "test";
      Assert.IsTrue(s.ContainsKey("initial catalog"));
      s["server"] = "myserver";
      Assert.IsTrue(s.ContainsKey("server"));
      Assert.IsTrue(s.ContainsKey("host"));
      Assert.IsFalse(s.ContainsKey("badkey"));
    }
#endif

    [Test]
    public void UseProcedureBodiesSettingCheckParameters()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder("server=localhost;use procedure bodies=false");
      Assert.IsFalse(s.CheckParameters);
    }

#if !CF
    [Test]
    public void EncrpytSslmode()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder("server=localhost;encrypt=true");
      Assert.AreEqual(s.SslMode, MySqlSslMode.Preferred);
    }
#endif

    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void SettingInvalidKeyThrowsArgumentException()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      s["foo keyword"] = "foo";
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
      Assert.AreEqual(null, obj);
    }

  }
}
