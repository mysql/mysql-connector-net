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

using NUnit.Framework;
using System.Diagnostics;
using System;

namespace MySql.Data.MySqlClient.Tests
{
  public class UsageAdvisorTests : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    internal override void AdjustConnectionSettings(MySqlConnectionStringBuilder settings)
    {
      settings.UseUsageAdvisor = true;
    }

    [Test]
    public void NotReadingEveryField()
    {
      ExecuteSQL("CREATE TABLE Test (id int, name VARCHAR(200))");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'Test1')");
      ExecuteSQL("INSERT INTO Test VALUES (2, 'Test2')");
      ExecuteSQL("INSERT INTO Test VALUES (3, 'Test3')");
      ExecuteSQL("INSERT INTO Test VALUES (4, 'Test4')");

      MySqlTrace.Listeners.Clear();
      MySqlTrace.Switch.Level = SourceLevels.All;
      GenericListener listener = new GenericListener();
      MySqlTrace.Listeners.Add(listener);
      string sql = "SELECT * FROM Test; SELECT * FROM Test WHERE id > 2";
      MySqlCommand cmd = new MySqlCommand(sql, Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        reader.GetInt32(0);  // access  the first field
        reader.Read();
        Assert.True(reader.NextResult());
        reader.Read();
        Assert.AreEqual("Test3", reader.GetString(1));
        Assert.False(reader.NextResult());
      }

      Assert.AreEqual(12, listener.Strings.Count);
      StringAssert.Contains("Query Opened: SELECT * FROM Test; SELECT * FROM Test WHERE id > 2", listener.Strings[0]);
      StringAssert.Contains("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1", listener.Strings[1]);
      StringAssert.Contains("Usage Advisor Warning: Query does not use an index", listener.Strings[2]);
      StringAssert.Contains("Usage Advisor Warning: Skipped 2 rows. Consider a more focused query.", listener.Strings[3]);
      StringAssert.Contains("Usage Advisor Warning: The following columns were not accessed: name", listener.Strings[4]);
      StringAssert.Contains("Resultset Closed. Total rows=4, skipped rows=2, size (bytes)=32", listener.Strings[5]);
      StringAssert.Contains("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1", listener.Strings[6]);
      StringAssert.Contains("Usage Advisor Warning: Query does not use an index", listener.Strings[7]);
      StringAssert.Contains("Usage Advisor Warning: Skipped 1 rows. Consider a more focused query.", listener.Strings[8]);
      StringAssert.Contains("Usage Advisor Warning: The following columns were not accessed: id", listener.Strings[9]);
      StringAssert.Contains("Resultset Closed. Total rows=2, skipped rows=1, size (bytes)=16", listener.Strings[10]);
      StringAssert.Contains("Query Closed", listener.Strings[11]);
    }

    [Test]
    public void NotReadingEveryRow()
    {
      ExecuteSQL("CREATE TABLE Test (id int, name VARCHAR(200))");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'Test1')");
      ExecuteSQL("INSERT INTO Test VALUES (2, 'Test2')");
      ExecuteSQL("INSERT INTO Test VALUES (3, 'Test3')");
      ExecuteSQL("INSERT INTO Test VALUES (4, 'Test4')");

      MySqlTrace.Listeners.Clear();
      MySqlTrace.Switch.Level = SourceLevels.All;
      GenericListener listener = new GenericListener();
      MySqlTrace.Listeners.Add(listener);

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test; SELECT * FROM Test WHERE id > 2", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        reader.Read();
        Assert.True(reader.NextResult());
        reader.Read();
        reader.Read();
        Assert.False(reader.NextResult());
      }

      Assert.AreEqual(11, listener.Strings.Count);
      StringAssert.Contains("Query Opened: SELECT * FROM Test; SELECT * FROM Test WHERE id > 2", listener.Strings[0]);
      StringAssert.Contains("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1", listener.Strings[1]);
      StringAssert.Contains("Usage Advisor Warning: Query does not use an index", listener.Strings[2]);
      StringAssert.Contains("Usage Advisor Warning: Skipped 2 rows. Consider a more focused query.", listener.Strings[3]);
      StringAssert.Contains("Usage Advisor Warning: The following columns were not accessed: id,name", listener.Strings[4]);
      StringAssert.Contains("Resultset Closed. Total rows=4, skipped rows=2, size (bytes)=32", listener.Strings[5]);
      StringAssert.Contains("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1", listener.Strings[6]);
      StringAssert.Contains("Usage Advisor Warning: Query does not use an index", listener.Strings[7]);
      StringAssert.Contains("Usage Advisor Warning: The following columns were not accessed: id,name", listener.Strings[8]);
      StringAssert.Contains("Resultset Closed. Total rows=2, skipped rows=0, size (bytes)=16", listener.Strings[9]);
      StringAssert.Contains("Query Closed", listener.Strings[10]);
    }

    [Test]
    public void FieldConversion()
    {
      ExecuteSQL("CREATE TABLE Test (id int, name VARCHAR(200))");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'Test1')");

      MySqlTrace.Listeners.Clear();
      MySqlTrace.Switch.Level = SourceLevels.All;
      GenericListener listener = new GenericListener();
      MySqlTrace.Listeners.Add(listener);
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        short s = reader.GetInt16(0);
        long l = reader.GetInt64(0);
        string str = reader.GetString(1);
      }

      Assert.AreEqual(6, listener.Strings.Count);
      StringAssert.Contains("Query Opened: SELECT * FROM Test", listener.Strings[0]);
      StringAssert.Contains("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1", listener.Strings[1]);
      StringAssert.Contains("Usage Advisor Warning: Query does not use an index", listener.Strings[2]);
      StringAssert.Contains("Usage Advisor Warning: The field 'id' was converted to the following types: Int16,Int64", listener.Strings[3]);
      StringAssert.Contains("Resultset Closed. Total rows=1, skipped rows=0, size (bytes)=8", listener.Strings[4]);
      StringAssert.Contains("Query Closed", listener.Strings[5]);
    }

    [Test]
    public void NoIndexUsed()
    {
      ExecuteSQL("CREATE TABLE Test (id int, name VARCHAR(200))");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'Test1')");
      ExecuteSQL("INSERT INTO Test VALUES (2, 'Test1')");
      ExecuteSQL("INSERT INTO Test VALUES (3, 'Test1')");
      ExecuteSQL("INSERT INTO Test VALUES (4, 'Test1')");

      MySqlTrace.Listeners.Clear();
      MySqlTrace.Switch.Level = SourceLevels.All;
      GenericListener listener = new GenericListener();
      MySqlTrace.Listeners.Add(listener);
      MySqlCommand cmd = new MySqlCommand("SELECT name FROM Test WHERE id=3", Connection);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
      }

      Assert.AreEqual(6, listener.Strings.Count);
      StringAssert.Contains("Query Opened: SELECT name FROM Test WHERE id=3", listener.Strings[0]);
      StringAssert.Contains("Resultset Opened: field(s) = 1, affected rows = -1, inserted id = -1", listener.Strings[1]);
      StringAssert.Contains("Usage Advisor Warning: Query does not use an index", listener.Strings[2]);
      StringAssert.Contains("Usage Advisor Warning: The following columns were not accessed: name", listener.Strings[3]);
      StringAssert.Contains("Resultset Closed. Total rows=1, skipped rows=0, size (bytes)=6", listener.Strings[4]);
      StringAssert.Contains("Query Closed", listener.Strings[5]);
    }

    [Test]
    public void BadIndexUsed()
    {
      ExecuteSQL("CREATE TABLE Test(id INT, name VARCHAR(20) PRIMARY KEY)");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'Test1')");
      ExecuteSQL("INSERT INTO Test VALUES (2, 'Test2')");
      ExecuteSQL("INSERT INTO Test VALUES (3, 'Test3')");
      ExecuteSQL("INSERT INTO Test VALUES (4, 'Test4')");

      MySqlTrace.Listeners.Clear();
      MySqlTrace.Switch.Level = SourceLevels.All;
      GenericListener listener = new GenericListener();
      MySqlTrace.Listeners.Add(listener);
      MySqlCommand cmd = new MySqlCommand("SELECT name FROM Test WHERE id=3", Connection);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
      }

      Assert.AreEqual(6, listener.Strings.Count);
      StringAssert.Contains("Query Opened: SELECT name FROM Test WHERE id=3", listener.Strings[0]);
      StringAssert.Contains("Resultset Opened: field(s) = 1, affected rows = -1, inserted id = -1", listener.Strings[1]);
      StringAssert.Contains("Usage Advisor Warning: Query does not use an index", listener.Strings[2]);
      StringAssert.Contains("Usage Advisor Warning: The following columns were not accessed: name", listener.Strings[3]);
      StringAssert.Contains("Resultset Closed. Total rows=1, skipped rows=0, size (bytes)=6", listener.Strings[4]);
      StringAssert.Contains("Query Closed", listener.Strings[5]);
    }
  }
}