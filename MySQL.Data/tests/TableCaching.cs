// Copyright © 2013, 2017 Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using System.Threading;
using System.Diagnostics;

namespace MySql.Data.MySqlClient.Tests
{
  public class TableCaching : TestBase
  {

    public TableCaching(TestFixture fixture) : base(fixture)
    {
    }
    
    [Fact]
    public void SimpleTableCaching()
    {
      executeSQL("CREATE TABLE test (id INT, name VARCHAR(20), name2 VARCHAR(20))");
      executeSQL("INSERT INTO test VALUES (1, 'boo', 'hoo'), (2, 'first', 'last'), (3, 'fred', 'flintstone')");

#if !NETCOREAPP1_1
      MySqlTrace.Listeners.Clear();
      MySqlTrace.Switch.Level = SourceLevels.All;
      GenericListener listener = new GenericListener();

      MySqlTrace.Listeners.Add(listener);


      string connStr = Connection.ConnectionString + ";logging=true;table cache=true";
#else
      string connStr = Connection.ConnectionString + ";table cache=true";
#endif

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("test", c);
        cmd.CommandType = CommandType.TableDirect;
        ConsumeReader(cmd);
        // now run the query again but this time it shouldn't generate a call to the database
        ConsumeReader(cmd);
      }
#if !NETCOREAPP1_1
      Assert.Equal(1, listener.Find("Resultset Opened: field(s) = 3"));
#endif
    }

    [Fact(Skip="Fix This")]
    public void ConnectionStringExpiry()
    {
      executeSQL("CREATE TABLE test3 (id INT, name VARCHAR(20), name2 VARCHAR(20))");
      executeSQL("INSERT INTO test3 VALUES (1, 'boo', 'hoo'), (2, 'first', 'last'), (3, 'fred', 'flintstone')");

#if !NETCOREAPP1_1
      MySqlTrace.Listeners.Clear();
      MySqlTrace.Switch.Level = SourceLevels.All;
      GenericListener listener = new GenericListener();
      MySqlTrace.Listeners.Add(listener);
      string connStr = Connection.ConnectionString + ";logging=true;table cache=true;default table cache age=1";
#else
      string connStr = Connection.ConnectionString + ";table cache=true;default table cache age=1";
#endif
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("test3", c);
        cmd.CommandType = CommandType.TableDirect;
        ConsumeReader(cmd);
        Thread.Sleep(1500);
        // now run the query again but this time it should generate a call to the database
        // since our next query is past the cache age of 1 second
        ConsumeReader(cmd);
      }
#if !NETCOREAPP1_1
      Assert.Equal(2, listener.Find("Resultset Opened: field(s) = 3"));
#endif
    }

    [Fact]
    public void SettingAgeOnCommand()
    {
      executeSQL("CREATE TABLE test2 (id INT, name VARCHAR(20), name2 VARCHAR(20))");
      executeSQL("INSERT INTO test2 VALUES (1, 'boo', 'hoo'), (2, 'first', 'last'), (3, 'fred', 'flintstone')");

#if !NETCOREAPP1_1
      MySqlTrace.Listeners.Clear();
      MySqlTrace.Switch.Level = SourceLevels.All;
      GenericListener listener = new GenericListener();
      MySqlTrace.Listeners.Add(listener);
      string connStr = Connection.ConnectionString + ";logging=true;table cache=true;default table cache age=1";
#else
      string connStr = Connection.ConnectionString + ";table cache=true;default table cache age=1";
#endif

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("test2", c);
        cmd.CommandType = CommandType.TableDirect;
        cmd.CacheAge = 20;
        ConsumeReader(cmd);
        Thread.Sleep(1000);
        // now run the query again but this time it shouldn't generate a call to the database
        // since we have overriden the connection string cache age of 1 second and set it
        // to 20 seconds on our command
        ConsumeReader(cmd);
      }
#if !NETCOREAPP1_1
      Assert.Equal(1, listener.Find("Resultset Opened: field(s) = 3"));
#endif
    }

    private void ConsumeReader(MySqlCommand cmd)
    {
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("boo", reader.GetString(1));
        Assert.Equal("hoo", reader.GetString(2));
        reader.Read();
        Assert.Equal(2, reader.GetInt32(0));
        Assert.Equal("first", reader.GetString(1));
        Assert.Equal("last", reader.GetString(2));
        reader.Read();
        Assert.Equal(3, reader.GetInt32(0));
        Assert.Equal("fred", reader.GetString(1));
        Assert.Equal("flintstone", reader.GetString(2));
        Assert.False(reader.Read());
      }
    }
  }
}
