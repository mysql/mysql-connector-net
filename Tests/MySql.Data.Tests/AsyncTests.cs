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
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Properties;
using Xunit;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  public class AsyncTests : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;

    public void SetFixture(SetUpClass data)
    {
      st = data;    
    }

    [Fact]
    public void ExecuteNonQuery()
    {      
      if (st.Version < new Version(5, 0)) return;

      if (st.conn.State != ConnectionState.Open)
        st.conn.Open();
      
      st.execSQL("CREATE TABLE test (id int)");

      st.execSQL("CREATE PROCEDURE spTest() BEGIN SET @x=0; REPEAT INSERT INTO test VALUES(@x); " +
        "SET @x=@x+1; UNTIL @x = 300 END REPEAT; END");

      MySqlCommand proc = new MySqlCommand("spTest", st.conn);
      proc.CommandType = CommandType.StoredProcedure;
      IAsyncResult iar = proc.BeginExecuteNonQuery();
      int count = 0;
      while (!iar.IsCompleted)
      {
        count++;
        System.Threading.Thread.Sleep(20);
      }
      proc.EndExecuteNonQuery(iar);


      Assert.True(count > 0);

      proc.CommandType = CommandType.Text;
      proc.CommandText = "SELECT COUNT(*) FROM test";
      object cnt = proc.ExecuteScalar();
      Assert.Equal(300, Convert.ToInt32(cnt));
    }

    [Fact]
    public void ExecuteReader()
    {
      if (st.Version < new Version(5, 0)) return;

      if (st.conn.State != ConnectionState.Open)
        st.conn.Open();
      
      st.execSQL("CREATE TABLE test (id int)");
      st.execSQL("CREATE PROCEDURE spTest() BEGIN INSERT INTO test VALUES(1); " +
        "SELECT SLEEP(2); SELECT 'done'; END");

      MySqlCommand proc = new MySqlCommand("spTest", st.conn);
      proc.CommandType = CommandType.StoredProcedure;
      IAsyncResult iar = proc.BeginExecuteReader();
      int count = 0;
      while (!iar.IsCompleted)
      {
        count++;
        System.Threading.Thread.Sleep(20);
      }

      using (MySqlDataReader reader = proc.EndExecuteReader(iar))
      {
        Assert.NotNull(reader);
        Assert.True(count > 0, "count > 0");
        Assert.True(reader.Read(), "can read");
        Assert.True(reader.NextResult());
        Assert.True(reader.Read());
        Assert.Equal("done", reader.GetString(0));
        reader.Close();

        proc.CommandType = CommandType.Text;
        proc.CommandText = "SELECT COUNT(*) FROM test";
        object cnt = proc.ExecuteScalar();
        Assert.Equal(1, Convert.ToInt32(cnt));
      }
    }

    [Fact]
    public void ThrowingExceptions()
    {
      if (st.conn.State != ConnectionState.Open)
        st.conn.Open();

      MySqlCommand cmd = new MySqlCommand("SELECT xxx", st.conn);
      IAsyncResult r = cmd.BeginExecuteReader();      
      Exception ex = Assert.Throws<MySqlException>(() => cmd.EndExecuteReader(r));
      Assert.Equal("Unknown column 'xxx' in 'field list'", ex.Message);
    }

    public void Dispose()
    {
      st.execSQL("DROP TABLE IF EXISTS test");
      st.execSQL("DROP PROCEDURE IF EXISTS spTest");
    }
  }
}
