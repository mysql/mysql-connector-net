// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class MySqlConnectionStringBuilderTests : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;

    public void SetFixture(SetUpClass data)
    {
      st = data;
    }

    public void Dispose()
    {
       //Nothing to clean
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
      Assert.Equal("Keyword not supported.\r\nParameter name: badkey", ex.Message);        
      sb.Clear();
      Assert.Equal(15, Convert.ToInt32(sb.ConnectionTimeout));
      Assert.Equal(true, sb.Pooling);
      Assert.Equal(3306, Convert.ToInt32(sb.Port));
      Assert.Equal(String.Empty, sb.Server);
      Assert.Equal(false, sb.PersistSecurityInfo);
      Assert.Equal(0, Convert.ToInt32(sb.ConnectionLifeTime));
      Assert.False(sb.ConnectionReset);
      Assert.Equal(0, Convert.ToInt32(sb.MinimumPoolSize));
      Assert.Equal(100, Convert.ToInt32(sb.MaximumPoolSize));
      Assert.Equal(String.Empty, sb.UserID);
      Assert.Equal(String.Empty, sb.Password);
      Assert.Equal(false, sb.UseUsageAdvisor);
      Assert.Equal(String.Empty, sb.CharacterSet);
      Assert.Equal(false, sb.UseCompression);
      Assert.Equal("MYSQL", sb.PipeName);
      Assert.False(sb.Logging);
      Assert.False(sb.UseOldSyntax);
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

    [Fact]
    public void EncryptKeyword()
    {
      string connStr = "database=test;uid=root;server=localhost;encrypt=yes";
      MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(connStr);
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
    public void UseProcedureBodiesSettingCheckParameters()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder("server=localhost;use procedure bodies=false");
      Assert.False(s.CheckParameters);
    }

    [Fact]
    public void EncrpytSslmode()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder("server=localhost;encrypt=true");
      Assert.Equal(s.SslMode, MySqlSslMode.Preferred);
    }

    [Fact]    
    public void SettingInvalidKeyThrowsArgumentException()
    {
      MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
      //[ExpectedException(typeof(ArgumentException))]
      Exception ex = Assert.Throws<ArgumentException>(() => (s["foo keyword"] = "foo"));
      Assert.Equal("Keyword not supported.\r\nParameter name: foo keyword", ex.Message);      
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
      Assert.Equal(null, obj);
    }

  }
}
