// Copyright © 2013, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using NUnit.Framework.Legacy;
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
        Assert.That(reader.NextResult(), Is.True);
        reader.Read();
        Assert.That(reader.GetString(1), Is.EqualTo("Test3"));
        Assert.That(reader.NextResult(), Is.False);
      }

      Assert.That(listener.Strings.Count, Is.EqualTo(12));
      Assert.That(listener.Strings[0], Does.Contain("Query Opened: SELECT * FROM Test; SELECT * FROM Test WHERE id > 2"));
      Assert.That(listener.Strings[1], Does.Contain("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1"));
      Assert.That(listener.Strings[2], Does.Contain("Usage Advisor Warning: Query does not use an index"));
      Assert.That(listener.Strings[3], Does.Contain("Usage Advisor Warning: Skipped 2 rows. Consider a more focused query"));
      Assert.That(listener.Strings[4], Does.Contain("Usage Advisor Warning: The following columns were not accessed: name"));
      Assert.That(listener.Strings[5], Does.Contain("Resultset Closed. Total rows=4, skipped rows=2, size (bytes)=32"));
      Assert.That(listener.Strings[6], Does.Contain("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1"));
      Assert.That(listener.Strings[7], Does.Contain("Usage Advisor Warning: Query does not use an index"));
      Assert.That(listener.Strings[8], Does.Contain("Usage Advisor Warning: Skipped 1 rows. Consider a more focused query"));
      Assert.That(listener.Strings[9], Does.Contain("Usage Advisor Warning: The following columns were not accessed: id"));
      Assert.That(listener.Strings[10], Does.Contain("Resultset Closed. Total rows=2, skipped rows=1, size (bytes)=16"));
      Assert.That(listener.Strings[11], Does.Contain("Query Closed"));
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
        Assert.That(reader.NextResult(), Is.True);
        reader.Read();
        reader.Read();
        Assert.That(reader.NextResult(), Is.False);
      }

      Assert.That(listener.Strings, Has.Count.EqualTo(11));
      Assert.That(listener.Strings[0], Does.Contain("Query Opened: SELECT * FROM Test; SELECT * FROM Test WHERE id > 2"));
      Assert.That(listener.Strings[1], Does.Contain("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1"));
      Assert.That(listener.Strings[2], Does.Contain("Usage Advisor Warning: Query does not use an index"));
      Assert.That(listener.Strings[3], Does.Contain("Usage Advisor Warning: Skipped 2 rows. Consider a more focused query"));
      Assert.That(listener.Strings[4], Does.Contain("Usage Advisor Warning: The following columns were not accessed: id,name"));
      Assert.That(listener.Strings[5], Does.Contain("Resultset Closed. Total rows=4, skipped rows=2, size (bytes)=32"));
      Assert.That(listener.Strings[6], Does.Contain("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1"));
      Assert.That(listener.Strings[7], Does.Contain("Usage Advisor Warning: Query does not use an index"));
      Assert.That(listener.Strings[8], Does.Contain("Usage Advisor Warning: The following columns were not accessed: id,name"));
      Assert.That(listener.Strings[9], Does.Contain("Resultset Closed. Total rows=2, skipped rows=0, size (bytes)=16"));
      Assert.That(listener.Strings[10], Does.Contain("Query Closed"));
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

      Assert.That(listener.Strings.Count, Is.EqualTo(6));
      Assert.That(listener.Strings[0], Does.Contain("Query Opened: SELECT * FROM Test"));
      Assert.That(listener.Strings[1], Does.Contain("Resultset Opened: field(s) = 2, affected rows = -1, inserted id = -1"));
      Assert.That(listener.Strings[2], Does.Contain("Usage Advisor Warning: Query does not use an index"));
      Assert.That(listener.Strings[3], Does.Contain("Usage Advisor Warning: The field 'id' was converted to the following types: Int16,Int64"));
      Assert.That(listener.Strings[4], Does.Contain("Resultset Closed. Total rows=1, skipped rows=0, size (bytes)=8"));
      Assert.That(listener.Strings[5], Does.Contain("Query Closed"));
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

      Assert.That(listener.Strings.Count, Is.EqualTo(6));
      Assert.That(listener.Strings[0], Does.Contain("Query Opened: SELECT name FROM Test WHERE id=3"));
      Assert.That(listener.Strings[1], Does.Contain("Resultset Opened: field(s) = 1, affected rows = -1, inserted id = -1"));
      Assert.That(listener.Strings[2], Does.Contain("Usage Advisor Warning: Query does not use an index"));
      Assert.That(listener.Strings[3], Does.Contain("Usage Advisor Warning: The following columns were not accessed: name"));
      Assert.That(listener.Strings[4], Does.Contain("Resultset Closed. Total rows=1, skipped rows=0, size (bytes)=6"));
      Assert.That(listener.Strings[5], Does.Contain("Query Closed"));
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

      Assert.That(listener.Strings.Count, Is.EqualTo(6));
      Assert.That(listener.Strings[0], Does.Contain("Query Opened: SELECT name FROM Test WHERE id=3"));
      Assert.That(listener.Strings[1], Does.Contain("Resultset Opened: field(s) = 1, affected rows = -1, inserted id = -1"));
      Assert.That(listener.Strings[2], Does.Contain("Usage Advisor Warning: Query does not use an index"));
      Assert.That(listener.Strings[3], Does.Contain("Usage Advisor Warning: The following columns were not accessed: name"));
      Assert.That(listener.Strings[4], Does.Contain("Resultset Closed. Total rows=1, skipped rows=0, size (bytes)=6"));
      Assert.That(listener.Strings[5], Does.Contain("Query Closed"));
    }
  }
}
