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
using System.Data;
using System.Reflection;

namespace MySql.Data.MySqlClient.Tests
{
  public class SimpleTransactions  : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;

    public void SetFixture(SetUpClass data)
    {
      st = data;
      st.createTable("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))", "INNODB");
    }

    public void Dispose()
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");      
    }

    [Fact]
    public void TestReader()
    {
      st.execSQL("INSERT INTO Test VALUES('P', 'Test1', 'Test2')");

      MySqlTransaction txn = st.conn.BeginTransaction();
      MySqlConnection c = txn.Connection;
      Assert.Equal(st.conn, c);
      MySqlCommand cmd = new MySqlCommand("SELECT name, name2 FROM Test WHERE key2='P'",
        st.conn, txn);
      MySqlTransaction t2 = cmd.Transaction;
      Assert.Equal(txn, t2);
      MySqlDataReader reader = null;
      try
      {
        reader = cmd.ExecuteReader();
        reader.Close();
        txn.Commit();
      }
      catch (Exception ex)
      {
        Assert.False(ex.Message != string.Empty, ex.Message);
        txn.Rollback();
      }
      finally
      {
        if (reader != null) reader.Close();
      }
    }

    /// <summary>
    /// Bug #22400 Nested transactions 
    /// </summary>
    [Fact]
    public void NestedTransactions()
    {
      MySqlTransaction t1 = st.conn.BeginTransaction();
      //try
      //{
        Exception ex = Assert.Throws<InvalidOperationException>(() => { st.conn.BeginTransaction(); });
        Assert.Equal(ex.Message, "Nested transactions are not supported.");
        ////Assert.Fail("Exception should have been thrown");
        //t2.Rollback();
      //}
      //catch (InvalidOperationException)
      //{
      //}
      //finally
      //{
        t1.Rollback();
      //}
    }

    [Fact]
    public void BeginTransactionOnPreviouslyOpenConnection()
    {
      string connStr = st.GetConnectionString(true);
      MySqlConnection c = new MySqlConnection(connStr);
      c.Open();
      c.Close();
      try
      {
        c.BeginTransaction();
      }
      catch (Exception ex)
      {
        Assert.Equal("The connection is not open.", ex.Message);
      }
    }

    /// <summary>
    /// Bug #37991 Connection fails when trying to close after a commit while network to db is bad
    /// This test is not a perfect test of this bug as the kill connection is not quite the
    /// same as unplugging the network but it's the best I've figured out so far
    /// </summary>
    [Fact]
    public void CommitAfterConnectionDead()
    {
      st.execSQL("DROP TABLE IF EXISTS Test");
      st.execSQL("CREATE TABLE Test(id INT, name VARCHAR(20))");

      string connStr = st.GetConnectionString(true) + ";pooling=false";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlTransaction trans = c.BeginTransaction();

        using (MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (1, 'boo')", c))
        {
          cmd.ExecuteNonQuery();
        }
        st.KillConnection(c);
        //try
        //{
        Exception ex = Assert.Throws<InvalidOperationException>(() => trans.Commit());
        Assert.Equal(ex.Message, "Connection must be valid and open to commit transaction");
        //}
        //catch (Exception)
        //{
        //}
        Assert.Equal(ConnectionState.Closed, c.State);
        c.Close();    // this should work even though we are closed
      }
    }

    /// <summary>
    /// Bug #39817	Transaction Dispose does not roll back
    /// </summary>
    [Fact]
    public void DisposingCallsRollback()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES ('a', 'b', 'c')", st.conn);
      MySqlTransaction txn = st.conn.BeginTransaction();
      using (txn)
      {
        cmd.ExecuteNonQuery();
      }
      // the txn should be closed now as a rollback should have happened.
      Type t = txn.GetType();
      FieldInfo fi = t.GetField("open", BindingFlags.Instance | BindingFlags.NonPublic);
      bool isOpen = (bool)fi.GetValue(txn);
      Assert.False(isOpen);
    }

  }
}
