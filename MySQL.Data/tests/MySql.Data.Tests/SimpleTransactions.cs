// Copyright (c) 2013, 2018, Oracle and/or its affiliates. All rights reserved.
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

using System;
using Xunit;
using System.Data;
using System.Reflection;

namespace MySql.Data.MySqlClient.Tests
{
  public class SimpleTransactions  : TestBase
  {
    public SimpleTransactions(TestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void TestReader()
    {
      executeSQL("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))");
      executeSQL("INSERT INTO Test VALUES('P', 'Test1', 'Test2')");

      MySqlTransaction txn = Connection.BeginTransaction();
      MySqlConnection c = txn.Connection;
      Assert.Equal(Connection, c);
      MySqlCommand cmd = new MySqlCommand("SELECT name, name2 FROM Test WHERE key2='P'",
        Connection, txn);
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
      MySqlTransaction t1 = Connection.BeginTransaction();
      //try
      //{
        Exception ex = Assert.Throws<InvalidOperationException>(() => { Connection.BeginTransaction(); });
        Assert.Equal("Nested transactions are not supported.", ex.Message);
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
      string connStr = Connection.ConnectionString;
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
    [Fact(Skip = "Temporary Skip")]
    public void CommitAfterConnectionDead()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test(id INT, name VARCHAR(20))");

      string connStr = Connection.ConnectionString + ";pooling=false";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlTransaction trans = c.BeginTransaction();

        using (MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (1, 'boo')", c))
        {
          cmd.ExecuteNonQuery();
        }
        KillConnection(c);
        //try
        //{
        Exception ex = Assert.Throws<InvalidOperationException>(() => trans.Commit());
        Assert.Equal("Connection must be valid and open to commit transaction", ex.Message);
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
      executeSQL("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES ('a', 'b', 'c')", Connection);
      MySqlTransaction txn = Connection.BeginTransaction();
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

    [Fact]
    public void SimpleRollback()
    {
      try
      {
        MySqlTransaction trans = Connection.BeginTransaction();
        trans.Rollback();
      }
      catch (Exception) { }
    }
  }
}
