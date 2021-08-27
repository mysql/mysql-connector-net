// Copyright (c) 2013, 2021, Oracle and/or its affiliates.
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
using NUnit.Framework;
using System.Data;
using System.Reflection;

namespace MySql.Data.MySqlClient.Tests
{
  public class SimpleTransactions : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    [Test]
    public void TestReader()
    {
      ExecuteSQL("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))");
      ExecuteSQL("INSERT INTO Test VALUES('P', 'Test1', 'Test2')");

      MySqlTransaction txn = Connection.BeginTransaction();
      MySqlConnection c = txn.Connection;
      Assert.AreEqual(Connection, c);
      MySqlCommand cmd = new MySqlCommand("SELECT name, name2 FROM Test WHERE key2='P'",
        Connection, txn);
      MySqlTransaction t2 = cmd.Transaction;
      Assert.AreEqual(txn, t2);
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
    [Test]
    public void NestedTransactions()
    {
      MySqlTransaction t1 = Connection.BeginTransaction();

      Exception ex = Assert.Throws<InvalidOperationException>(() => { Connection.BeginTransaction(); });
      Assert.AreEqual("Nested transactions are not supported.", ex.Message);

      t1.Rollback();
    }

    [Test]
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
        Assert.AreEqual("The connection is not open.", ex.Message);
      }
    }

    /// <summary>
    /// Bug #37991 Connection fails when trying to close after a commit while network to db is bad
    /// This test is not a perfect test of this bug as the kill connection is not quite the
    /// same as unplugging the network but it's the best I've figured out so far
    /// </summary>
    [Test]
    public void CommitAfterConnectionDead()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test(id INT, name VARCHAR(20))");

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
        Assert.AreEqual("Connection must be valid and open to commit transaction", ex.Message);
        //}
        //catch (Exception)
        //{
        //}
        Assert.AreEqual(ConnectionState.Closed, c.State);
        c.Close();    // this should work even though we are closed
      }
    }

    /// <summary>
    /// Bug #39817	Transaction Dispose does not roll back
    /// </summary>
    [Test]
    public void DisposingCallsRollback()
    {
      ExecuteSQL("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))");
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

    [Test]
    public void SimpleRollback()
    {
      try
      {
        MySqlTransaction trans = Connection.BeginTransaction();
        trans.Rollback();
      }
      catch (Exception) { }
    }

    #region WL14389

    [Test, Description("Transaction Scope")]
    public void TransactionScope()
    {
      using (var myConn = new MySqlConnection(Connection.ConnectionString))
      {
        myConn.Open();
        var cmdtoexec = myConn.CreateCommand();
        var myTransaction = myConn.BeginTransaction();
        cmdtoexec.Transaction = myTransaction;

        cmdtoexec.CommandText = "SET autocommit = 0";
        cmdtoexec.ExecuteNonQuery();

        cmdtoexec.CommandText = "DROP TABLE IF EXISTS transactiontable;";
        cmdtoexec.ExecuteNonQuery();

        cmdtoexec.CommandText =
            "CREATE TABLE  transactiontable(Id int(10) unsigned NOT NULL default '0', PRIMARY KEY  (Id))ENGINE=InnoDB";
        cmdtoexec.ExecuteNonQuery();

        for (var i = 0; i < 50; i++)
          MySqlHelper.ExecuteNonQuery(myConn, string.Format("INSERT INTO transactiontable VALUES({0})", i));

        myTransaction.Rollback(); // to rollback actions
        cmdtoexec.CommandText = "select count(*) from transactiontable";
        var count = cmdtoexec.ExecuteScalar();
        Assert.AreEqual(0,count);
      }
    }

    #endregion WL14389

  }
}
