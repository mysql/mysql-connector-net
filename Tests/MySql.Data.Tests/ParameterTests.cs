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
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System.Diagnostics;

namespace MySql.Data.MySqlClient.Tests
{
  /// <summary>
  /// Summary description for ConnectionTests.
  /// </summary>
  [TestFixture]
  public class ParameterTests : BaseTest
  {
    [SetUp]
    public override void Setup()
    {
      base.Setup();
      execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");
    }

    [Test]
    public void TestQuoting()
    {
      MySqlCommand cmd = new MySqlCommand("", conn);
      cmd.CommandText = "INSERT INTO Test VALUES (?id, ?name, NULL,NULL,NULL)";
      cmd.Parameters.Add(new MySqlParameter("?id", 1));
      cmd.Parameters.Add(new MySqlParameter("?name", "my ' value"));
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = 2;
      cmd.Parameters[1].Value = @"my "" value";
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = 3;
      cmd.Parameters[1].Value = @"my ` value";
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = 4;
      cmd.Parameters[1].Value = @"my ´ value";
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = 5;
      cmd.Parameters[1].Value = @"my \ value";
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      MySqlDataReader reader = null;
      try
      {
        reader = cmd.ExecuteReader();
        reader.Read();
        Assert.AreEqual("my ' value", reader.GetString(1));
        reader.Read();
        Assert.AreEqual(@"my "" value", reader.GetString(1));
        reader.Read();
        Assert.AreEqual("my ` value", reader.GetString(1));
        reader.Read();
        Assert.AreEqual("my ´ value", reader.GetString(1));
        reader.Read();
        Assert.AreEqual(@"my \ value", reader.GetString(1));
      }
      catch (Exception ex)
      {
        Assert.Fail(ex.Message);
      }
      finally
      {
        if (reader != null) reader.Close();
      }
    }

    [Test]
    public void TestDateTimeParameter()
    {
      MySqlCommand cmd = new MySqlCommand("", conn);

      TimeSpan time = new TimeSpan(0, 1, 2, 3);
      DateTime dt = new DateTime(2003, 11, 11, 1, 2, 3);
      cmd.CommandText = "INSERT INTO Test VALUES (1, 'test', ?dt, ?time, NULL)";
      cmd.Parameters.Add(new MySqlParameter("?time", time));
      cmd.Parameters.Add(new MySqlParameter("?dt", dt));
      int cnt = cmd.ExecuteNonQuery();
      Assert.AreEqual(1, cnt, "Insert count");

      cmd = new MySqlCommand("SELECT tm, dt, ts FROM Test WHERE id=1", conn);
      MySqlDataReader reader = cmd.ExecuteReader();
      reader.Read();
      TimeSpan time2 = (TimeSpan)reader.GetValue(0);
      Assert.AreEqual(time, time2);

      DateTime dt2 = reader.GetDateTime(1);
      Assert.AreEqual(dt, dt2);

      DateTime ts2 = reader.GetDateTime(2);
      reader.Close();

      // now check the timestamp column.  We won't check the minute or second for obvious reasons
      DateTime now = DateTime.Now;
      Assert.AreEqual(now.Year, ts2.Year);
      Assert.AreEqual(now.Month, ts2.Month);
      Assert.AreEqual(now.Day, ts2.Day);
      Assert.AreEqual(now.Hour, ts2.Hour);

      // now we'll set some nulls and see how they are handled
      cmd = new MySqlCommand("UPDATE Test SET tm=?ts, dt=?dt WHERE id=1", conn);
      cmd.Parameters.Add(new MySqlParameter("?ts", DBNull.Value));
      cmd.Parameters.Add(new MySqlParameter("?dt", DBNull.Value));
      cnt = cmd.ExecuteNonQuery();
      Assert.AreEqual(1, cnt, "Update null count");

      cmd = new MySqlCommand("SELECT tm, dt FROM Test WHERE id=1", conn);
      reader = cmd.ExecuteReader();
      reader.Read();
      object tso = reader.GetValue(0);
      object dto = reader.GetValue(1);
      Assert.AreEqual(DBNull.Value, tso, "Time column");
      Assert.AreEqual(DBNull.Value, dto, "DateTime column");

      reader.Close();

      cmd.CommandText = "DELETE FROM Test WHERE id=1";
      cmd.ExecuteNonQuery();
    }

    [Test]
    public void NestedQuoting()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) " +
        "VALUES(1, 'this is ?\"my value\"')", conn);
      int count = cmd.ExecuteNonQuery();
      Assert.AreEqual(1, count);
    }

    [Test]
    public void SetDbType()
    {
      IDbCommand cmd = conn.CreateCommand();
      IDbDataParameter prm = cmd.CreateParameter();
      prm.DbType = DbType.Int64;
      Assert.AreEqual(DbType.Int64, prm.DbType);
      prm.Value = 3;
      Assert.AreEqual(DbType.Int64, prm.DbType);

      MySqlParameter p = new MySqlParameter("name", MySqlDbType.Int64);
      Assert.AreEqual(DbType.Int64, p.DbType);
      Assert.AreEqual(MySqlDbType.Int64, p.MySqlDbType);
      p.Value = 3;
      Assert.AreEqual(DbType.Int64, p.DbType);
      Assert.AreEqual(MySqlDbType.Int64, p.MySqlDbType);
    }

#if !CF
    [Test]
    public void UseOldSyntaxGivesWarning()
    {
      Trace.Listeners.Clear();
      GenericListener listener = new GenericListener();
      Trace.Listeners.Add(listener);

      string connStr = conn.ConnectionString + ";old syntax=yes;pooling=false";
      MySqlConnection conn2 = new MySqlConnection(connStr);
      conn2.Open();

      Assert.IsTrue(listener.Find("Use Old Syntax is now obsolete") != 0);
      conn2.Close();
      Trace.Listeners.Clear();
    }
#endif

    [Test]
    public void NullParameterObject()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (1, ?name)", conn);
      try
      {
        cmd.Parameters.Add(null);
      }
      catch (ArgumentException)
      {
      }
    }

    /// <summary>
    /// Bug #7398  	MySqlParameterCollection doesn't allow parameters without filled in names
    /// </summary>
    [Test]
    public void AllowUnnamedParameters()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES (?id, ?name)", conn);

      MySqlParameter p = new MySqlParameter();
      p.Value = 1;
      cmd.Parameters.Add(p);
      cmd.Parameters[0].ParameterName = "?id";

      p = new MySqlParameter();
      p.Value = "test";
      cmd.Parameters.Add(p);
      p.ParameterName = "?name";

      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT id FROM Test";
      Assert.AreEqual(1, cmd.ExecuteScalar());

      cmd.CommandText = "SELECT name FROM Test";
      Assert.AreEqual("test", cmd.ExecuteScalar());
    }

    [Test]
    public void NullParameterValue()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (1, ?name)", conn);
      cmd.Parameters.Add(new MySqlParameter("?name", null));
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT name FROM Test WHERE id=1";
      object name = cmd.ExecuteScalar();
      Assert.AreEqual(DBNull.Value, name);
    }

    /// <summary>
    /// Bug #12646  	Parameters are defaulted to Decimal
    /// </summary>
    [Test]
    public void DefaultType()
    {
      IDbCommand cmd = conn.CreateCommand();
      IDbDataParameter p = cmd.CreateParameter();
      p.ParameterName = "?boo";
      p.Value = "test";
      MySqlParameter mp = (MySqlParameter)p;
      Assert.AreEqual(MySqlDbType.VarChar, mp.MySqlDbType);
    }

    [Test]
    public void OddCharsInParameterNames()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (1, ?nam$es)", conn);
      cmd.Parameters.Add(new MySqlParameter("?nam$es", "Test"));
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test (id, name) VALUES (2, ?nam_es)";
      cmd.Parameters.Clear();
      cmd.Parameters.Add(new MySqlParameter("?nam_es", "Test2"));
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test (id, name) VALUES (3, ?nam.es)";
      cmd.Parameters.Clear();
      cmd.Parameters.Add(new MySqlParameter("?nam.es", "Test3"));
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT name FROM Test WHERE id=1";
      object name = cmd.ExecuteScalar();
      Assert.AreEqual("Test", name);

      cmd.CommandText = "SELECT name FROM Test WHERE id=2";
      name = cmd.ExecuteScalar();
      Assert.AreEqual("Test2", name);

      cmd.CommandText = "SELECT name FROM Test WHERE id=3";
      name = cmd.ExecuteScalar();
      Assert.AreEqual("Test3", name);
    }

    /// <summary>
    /// Bug #13276  	Exception on serialize after inserting null value
    /// </summary>
    [Test]
    public void InsertValueAfterNull()
    {
      execSQL("DROP TABLE Test");
      execSQL("CREATE TABLE Test (id int auto_increment primary key, foo int)");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", conn);
      MySqlCommand c = new MySqlCommand("INSERT INTO Test (foo) values (?foo)", conn);
      c.Parameters.Add("?foo", MySqlDbType.Int32, 0, "foo");

      da.InsertCommand = c;
      DataTable dt = new DataTable();
      da.Fill(dt);
      DataRow row = dt.NewRow();
      dt.Rows.Add(row);
      row = dt.NewRow();
      row["foo"] = 2;
      dt.Rows.Add(row);
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.AreEqual(2, dt.Rows.Count);
      Assert.AreEqual(2, dt.Rows[1]["foo"]);
    }

    /// <summary>
    /// Bug #24565 Inferring DbType fails when reusing commands and the first time the value is nul 
    /// </summary>
    [Test]
    public void UnTypedParameterBeingReused()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, dt) VALUES (?id, ?dt)", conn);
      cmd.Parameters.AddWithValue("?id", 1);
      MySqlParameter p = cmd.CreateParameter();
      p.ParameterName = "?dt";
      p.Value = DBNull.Value;
      cmd.Parameters.Add(p);
      cmd.ExecuteNonQuery();

      cmd.Parameters[0].Value = 2;
      p.Value = DateTime.Now;
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      cmd.Parameters.Clear();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.IsTrue(reader.IsDBNull(2));
        reader.Read();
        Assert.IsFalse(reader.IsDBNull(2));
        Assert.IsFalse(reader.Read());
      }
    }

    [Test]
    public void ParameterCacheNotClearing()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (?id, ?name)", conn);
      cmd.Parameters.AddWithValue("?id", 1);
      cmd.Parameters.AddWithValue("?name", "test");
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test (id, name, dt) VALUES (?id1, ?name1, ?id)";
      cmd.Parameters[0].ParameterName = "?id1";
      cmd.Parameters[0].Value = 2;
      cmd.Parameters[1].ParameterName = "?name1";
      cmd.Parameters.AddWithValue("?id", DateTime.Now);
      cmd.ExecuteNonQuery();
    }

    [Test]
    public void WithAndWithoutMarker()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (?id, ?name)", conn);
      cmd.Parameters.AddWithValue("id", 1);
      Assert.AreEqual(-1, cmd.Parameters.IndexOf("?id"));
      cmd.Parameters.AddWithValue("name", "test");
      cmd.ExecuteNonQuery();

      cmd.Parameters.Clear();
      cmd.Parameters.AddWithValue("?id", 2);
      Assert.AreEqual(-1, cmd.Parameters.IndexOf("id"));
      cmd.Parameters.AddWithValue("?name", "test2");
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT COUNT(*) FROM Test";
      object count = cmd.ExecuteScalar();
      Assert.AreEqual(2, count);
    }

    [Test]
    public void DoubleAddingParameters()
    {
      try
      {
        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (?id, ?name)", conn);
        cmd.Parameters.AddWithValue("id", 1);
        Assert.AreEqual(-1, cmd.Parameters.IndexOf("?id"));
        Assert.AreEqual(-1, cmd.Parameters.IndexOf("@id"));
        cmd.Parameters.AddWithValue("name", "test");
        cmd.Parameters.AddWithValue("?id", 2);
        Assert.Fail("Should not get here");
      }
      catch (Exception)
      {
      }
    }

    /// <summary>
    /// Bug #26904 MySqlParameterCollection fails to add MySqlParameter that previously removed 
    /// </summary>
    [Test]
    public void AddingParameterPreviouslyRemoved()
    {
      MySqlCommand cmd = new
      MySqlCommand("Insert into sometable(s1, s2) values(?p1, ?p2)");

      MySqlParameter param1 = cmd.CreateParameter();
      param1.ParameterName = "?p1";
      param1.DbType = DbType.String;
      param1.Value = "Ali Gel";

      cmd.Parameters.Add(param1);
      cmd.Parameters.RemoveAt(0);
      cmd.Parameters.Add(param1);
    }

    /// <summary>
    /// Bug #27135 MySqlParameterCollection and parameters added with Insert Method 
    /// </summary>
    [Test]
    public void AddingParametersUsingInsert()
    {
      MySqlCommand cmd = new MySqlCommand();
      cmd.Parameters.Insert(0, new MySqlParameter("?id", MySqlDbType.Int32));
      MySqlParameter p = cmd.Parameters["?id"];
      Assert.AreEqual("?id", p.ParameterName);
    }

    /// <summary>
    /// Bug #27187 cmd.Parameters.RemoveAt("Id") will cause an error if the last item is requested 
    /// </summary>
    [Test]
    public void FindParameterAfterRemoval()
    {
      MySqlCommand cmd = new MySqlCommand();

      cmd.Parameters.Add("?id1", MySqlDbType.Int32);
      cmd.Parameters.Add("?id2", MySqlDbType.Int32);
      cmd.Parameters.Add("?id3", MySqlDbType.Int32);
      cmd.Parameters.Add("?id4", MySqlDbType.Int32);
      cmd.Parameters.Add("?id5", MySqlDbType.Int32);
      cmd.Parameters.Add("?id6", MySqlDbType.Int32);
      cmd.Parameters.RemoveAt("?id1");
      MySqlParameter p = cmd.Parameters["?id6"];
      Assert.AreEqual("?id6", p.ParameterName);
    }

    /// <summary>
    /// Bug #29312  	System.FormatException if parameter not found
    /// </summary>
    [Test]
    public void MissingParameter()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test(id) VALUES (?id)", conn);
      try
      {
        cmd.ExecuteNonQuery();
      }
      catch (MySqlException)
      {
      }
    }

    /// <summary>
    /// Bug #32094 Size property on string parameter throws an exception 
    /// </summary>
    [Test]
    public void StringParameterSizeSetAfterValue()
    {
      execSQL("DROP TABLE Test");
      execSQL("CREATE TABLE Test (v VARCHAR(10))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?p1)", conn);
      cmd.Parameters.Add("?p1", MySqlDbType.VarChar);
      cmd.Parameters[0].Value = "123";
      cmd.Parameters[0].Size = 10;
      cmd.ExecuteNonQuery();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.AreEqual("123", dt.Rows[0][0]);

      cmd.Parameters.Clear();
      cmd.Parameters.Add("?p1", MySqlDbType.VarChar);
      cmd.Parameters[0].Value = "123456789012345";
      cmd.Parameters[0].Size = 10;
      cmd.ExecuteNonQuery();

      dt.Clear();
      da.Fill(dt);
      Assert.AreEqual("1234567890", dt.Rows[1][0]);
    }

    /// <summary>
    /// Bug #32093 MySqlParameter Constructor does not allow Direction of anything other than Input 
    /// </summary>
    [Test]
    public void NonInputParametersToCtor()
    {
      MySqlParameter p = new MySqlParameter("?p1", MySqlDbType.VarChar, 20,
          ParameterDirection.InputOutput, true, 0, 0, "id", DataRowVersion.Current, 0);
      Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);

      MySqlParameter p1 = new MySqlParameter("?p1", MySqlDbType.VarChar, 20,
          ParameterDirection.Output, true, 0, 0, "id", DataRowVersion.Current, 0);
      Assert.AreEqual(ParameterDirection.Output, p1.Direction);
    }

    /// <summary>
    /// Bug #13991 oldsyntax configuration and ParameterMarker value bug 
    /// </summary>
    [Test]
    public void SetOldSyntaxAfterCommandCreation()
    {
      string connStr = this.GetConnectionString(true);
      MySqlConnection c = new MySqlConnection(connStr);
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id) VALUES (@id)", c);
      c.ConnectionString = connStr += ";old syntax=yes";
      cmd.Parameters.AddWithValue("@id", 2);
      c.Open();
      cmd.ExecuteNonQuery();
      c.Close();
    }

    [Test]
    public void UseAtSignForParameters()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (@id, @name)", conn);
      cmd.Parameters.AddWithValue("@id", 33);
      cmd.Parameters.AddWithValue("@name", "Test");
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual(33, reader.GetInt32(0));
        Assert.AreEqual("Test", reader.GetString(1));
      }
    }

    /// <summary>
    /// Bug #62194	MySQL Parameter constructor doesn't set
    /// all properties: IsNullable, Precision and Scale
    /// </summary>
    [Test]
    public void CanCreateMySQLParameterWithNullability()
    {

      MySqlParameter p = new MySqlParameter("?id", MySqlDbType.Decimal, 2,
                                          ParameterDirection.Input, true, 1, 1, "sourceColumn", DataRowVersion.Default, 1);

      Assert.AreEqual(p.IsNullable, true);
    }

    /// <summary>
    /// Bug #62194	MySQL Parameter constructor doesn't set
    /// all properties: IsNullable, Precision and Scale
    /// </summary>
    [Test]
    public void CanCreateMySQLParameterWithPrecision()
    {
      MySqlParameter p = new MySqlParameter("?id", MySqlDbType.Decimal, 2,
                                          ParameterDirection.Input, true, Byte.MaxValue, 1, "sourceColumn", DataRowVersion.Default, 1);

      Assert.AreEqual(p.Precision, Byte.MaxValue);
    }


    /// <summary>
    /// Bug #62194	MySQL Parameter constructor doesn't set
    /// all properties: IsNullable, Precision and Scale
    /// </summary>
    [Test]
    public void CanCreateMySQLParameterWithScale()
    {

      MySqlParameter p = new MySqlParameter("?id", MySqlDbType.Decimal, 2,
                                          ParameterDirection.Input, true, 1, Byte.MaxValue, "sourceColumn", DataRowVersion.Default, 1);

      Assert.AreEqual(p.Scale, Byte.MaxValue);
    }

    /// <summary>
    /// Bug #66060 #14499549 "Parameter '?' must be defined" error, when using unnamed parameters
    /// </summary>
    [Test]
    public void CanIdentifyParameterWithOutName()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES (?, ?)", conn);

      cmd.Parameters.AddWithValue("", 1);
      cmd.Parameters.AddWithValue("", "test");

      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT id FROM Test";
      Assert.AreEqual(1, cmd.ExecuteScalar());

      cmd.CommandText = "SELECT name FROM Test";
      Assert.AreEqual("test", cmd.ExecuteScalar());
    }

    /// <summary>
    /// Bug #66060  #14499549  "Parameter '?' must be defined" error, when using unnamed parameters
    /// </summary>
    [Test]
    [ExpectedException(typeof(MySqlException))]
    public void CanThrowAnExceptionWhenMixingParameterNaming()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES (?Id, ?name, ?)", conn);
      cmd.Parameters.AddWithValue("?Id", 1);
      cmd.Parameters.AddWithValue("?name", "test");
      cmd.ExecuteNonQuery();
    }

  }
}
