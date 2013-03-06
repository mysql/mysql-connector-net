// Copyright © 2004, 2010, Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Common;
using System.Reflection;

namespace MySql.Data.MySqlClient.Tests
{
  [TestFixture]
  public class SimpleTransactions : BaseTest
  {
    public override void Setup()
    {
      base.Setup();
      createTable("CREATE TABLE Test (key2 VARCHAR(1), name VARCHAR(100), name2 VARCHAR(100))", "INNODB");
    }

    [Test]
    public void TestReader()
    {
      execSQL("INSERT INTO Test VALUES('P', 'Test1', 'Test2')");

      MySqlTransaction txn = conn.BeginTransaction();
      MySqlConnection c = txn.Connection;
      Assert.AreEqual(conn, c);
      MySqlCommand cmd = new MySqlCommand("SELECT name, name2 FROM Test WHERE key2='P'",
        conn, txn);
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
        Assert.Fail(ex.Message);
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
      MySqlTransaction t1 = conn.BeginTransaction();
      try
      {
        MySqlTransaction t2 = conn.BeginTransaction();
        Assert.Fail("Exception should have been thrown");
        t2.Rollback();
      }
      catch (InvalidOperationException)
      {
      }
      finally
      {
        t1.Rollback();
      }
    }

    [Test]
    public void BeginTransactionOnPreviouslyOpenConnection()
    {
      string connStr = GetConnectionString(true);
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
      execSQL("DROP TABLE IF EXISTS Test");
      execSQL("CREATE TABLE Test(id INT, name VARCHAR(20))");

      string connStr = GetConnectionString(true) + ";pooling=false";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlTransaction trans = c.BeginTransaction();

        using (MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (1, 'boo')", c))
        {
          cmd.ExecuteNonQuery();
        }
        KillConnection(c);
        try
        {
          trans.Commit();
          Assert.Fail("Should have thrown an exception");
        }
        catch (Exception)
        {
        }
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
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES ('a', 'b', 'c')", conn);
      MySqlTransaction txn = conn.BeginTransaction();
      using (txn)
      {
        cmd.ExecuteNonQuery();
      }
      // the txn should be closed now as a rollback should have happened.
      Type t = txn.GetType();
      FieldInfo fi = t.GetField("open", BindingFlags.Instance | BindingFlags.NonPublic);
      bool isOpen = (bool)fi.GetValue(txn);
      Assert.IsFalse(isOpen);
    }
  }
}
