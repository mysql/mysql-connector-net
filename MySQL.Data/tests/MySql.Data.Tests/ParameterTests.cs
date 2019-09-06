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
using System.Diagnostics;
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  public partial class ParameterTests : TestBase
  {

    public ParameterTests(TestFixture fixture) : base(fixture)
    {
    }    

    [Fact]
    public void TestQuoting()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("", Connection);
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
        Assert.Equal("my ' value", reader.GetString(1));
        reader.Read();
        Assert.Equal(@"my "" value", reader.GetString(1));
        reader.Read();
        Assert.Equal("my ` value", reader.GetString(1));
        reader.Read();
        Assert.Equal("my ´ value", reader.GetString(1));
        reader.Read();
        Assert.Equal(@"my \ value", reader.GetString(1));
      }
      catch (Exception ex)
      {
        Assert.False(ex.Message == String.Empty, ex.Message);        
      }
      finally
      {
        if (reader != null) reader.Close();
      }
    }

    [Fact]
    public void TestDateTimeParameter()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("", Connection);

      TimeSpan time = new TimeSpan(0, 1, 2, 3);
      DateTime dt = new DateTime(2003, 11, 11, 1, 2, 3);
      cmd.CommandText = "INSERT INTO Test VALUES (1, 'test', ?dt, ?time, CURRENT_TIMESTAMP)";
      cmd.Parameters.Add(new MySqlParameter("?time", time));
      cmd.Parameters.Add(new MySqlParameter("?dt", dt));
      int cnt = cmd.ExecuteNonQuery();
      Assert.True(cnt == 1, "Insert count");

      cmd = new MySqlCommand("SELECT tm, dt, ts FROM Test WHERE id=1", Connection);
      MySqlDataReader reader = cmd.ExecuteReader();
      reader.Read();
      TimeSpan time2 = (TimeSpan)reader.GetValue(0);
      Assert.Equal(time, time2);

      DateTime dt2 = reader.GetDateTime(1);
      Assert.Equal(dt, dt2);

      DateTime ts2 = reader.GetDateTime(2);
      reader.Close();

      // now check the timestamp column.  We won't check the minute or second for obvious reasons
      DateTime now = DateTime.Now;
      Assert.Equal(now.Year, ts2.Year);
      Assert.Equal(now.Month, ts2.Month);
      Assert.Equal(now.Day, ts2.Day);
      Assert.Equal(now.Hour, ts2.Hour);

      // now we'll set some nulls and see how they are handled
      cmd = new MySqlCommand("UPDATE Test SET tm=?ts, dt=?dt WHERE id=1", Connection);
      cmd.Parameters.Add(new MySqlParameter("?ts", DBNull.Value));
      cmd.Parameters.Add(new MySqlParameter("?dt", DBNull.Value));
      cnt = cmd.ExecuteNonQuery();
      Assert.True(cnt == 1, "Update null count");

      cmd = new MySqlCommand("SELECT tm, dt FROM Test WHERE id=1", Connection);
      reader = cmd.ExecuteReader();
      reader.Read();
      object tso = reader.GetValue(0);
      object dto = reader.GetValue(1);
      Assert.True(tso == DBNull.Value, "Time column");
      Assert.True(dto == DBNull.Value, "DateTime column");

      reader.Close();

      cmd.CommandText = "DELETE FROM Test WHERE id=1";
      cmd.ExecuteNonQuery();
    }

    [Fact]
    public void NestedQuoting()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) " +
        "VALUES(1, 'this is ?\"my value\"')", Connection);
      int count = cmd.ExecuteNonQuery();
      Assert.Equal(1, count);
    }

    [Fact]
    public void SetDbType()
    {
      IDbCommand cmd = Connection.CreateCommand();
      IDbDataParameter prm = cmd.CreateParameter();
      prm.DbType = DbType.Int64;
      Assert.Equal(DbType.Int64, prm.DbType);
      prm.Value = 3;
      Assert.Equal(DbType.Int64, prm.DbType);

      MySqlParameter p = new MySqlParameter("name", MySqlDbType.Int64);
      Assert.Equal(DbType.Int64, p.DbType);
      Assert.Equal(MySqlDbType.Int64, p.MySqlDbType);
      p.Value = 3;
      Assert.Equal(DbType.Int64, p.DbType);
      Assert.Equal(MySqlDbType.Int64, p.MySqlDbType);
    }

    [Fact]
    public void NullParameterObject()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (1, ?name)", Connection);
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
    [Fact]
    public void AllowUnnamedParameters()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES (?id, ?name)", Connection);

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
      Assert.Equal(1, cmd.ExecuteScalar());

      cmd.CommandText = "SELECT name FROM Test";
      Assert.Equal("test", cmd.ExecuteScalar());
    }

    [Fact]
    public void NullParameterValue()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (1, ?name)", Connection);
      cmd.Parameters.Add(new MySqlParameter("?name", null));
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT name FROM Test WHERE id=1";
      object name = cmd.ExecuteScalar();
      Assert.Equal(DBNull.Value, name);
    }

    /// <summary>
    /// Bug #12646  	Parameters are defaulted to Decimal
    /// </summary>
    [Fact]
    public void DefaultType()
    {
      IDbCommand cmd = Connection.CreateCommand();
      IDbDataParameter p = cmd.CreateParameter();
      p.ParameterName = "?boo";
      p.Value = "test";
      MySqlParameter mp = (MySqlParameter)p;
      Assert.Equal(MySqlDbType.VarChar, mp.MySqlDbType);
    }

    [Fact]
    public void OddCharsInParameterNames()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (1, ?nam$es)", Connection);
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
      Assert.Equal("Test", name);

      cmd.CommandText = "SELECT name FROM Test WHERE id=2";
      name = cmd.ExecuteScalar();
      Assert.Equal("Test2", name);

      cmd.CommandText = "SELECT name FROM Test WHERE id=3";
      name = cmd.ExecuteScalar();
      Assert.Equal("Test3", name);
    }

    /// <summary>
    /// Bug #24565 Inferring DbType fails when reusing commands and the first time the value is nul 
    /// </summary>
    [Fact]
    public void UnTypedParameterBeingReused()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, dt) VALUES (?id, ?dt)", Connection);
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
        Assert.True(reader.IsDBNull(2));
        reader.Read();
        Assert.False(reader.IsDBNull(2));
        Assert.False(reader.Read());
      }
    }

    [Fact]
    public void ParameterCacheNotClearing()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (?id, ?name)", Connection);
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

    [Fact]
    public void WithAndWithoutMarker()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (?id, ?name)", Connection);
      cmd.Parameters.AddWithValue("id", 1);
      Assert.Equal(-1, cmd.Parameters.IndexOf("?id"));
      cmd.Parameters.AddWithValue("name", "test");
      cmd.ExecuteNonQuery();

      cmd.Parameters.Clear();
      cmd.Parameters.AddWithValue("?id", 2);
      Assert.Equal(-1, cmd.Parameters.IndexOf("id"));
      cmd.Parameters.AddWithValue("?name", "test2");
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT COUNT(*) FROM Test";
      object count = cmd.ExecuteScalar();
      Assert.Equal(2, Convert.ToInt32(count));
    }

    [Fact]
    public void DoubleAddingParameters()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (?id, ?name)", Connection);
      cmd.Parameters.AddWithValue("id", 1);
      Assert.Equal(-1, cmd.Parameters.IndexOf("?id"));
      Assert.Equal(-1, cmd.Parameters.IndexOf("@id"));
      cmd.Parameters.AddWithValue("name", "test");
      Exception ex = Assert.Throws<MySqlException>(() => cmd.Parameters.AddWithValue("?id", 2));
      Assert.Equal("Parameter '?id' has already been defined.", ex.Message);
    }

    /// <summary>
    /// Bug #26904 MySqlParameterCollection fails to add MySqlParameter that previously removed 
    /// </summary>
    [Fact]
    public void AddingParameterPreviouslyRemoved()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

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
    [Fact]
    public void AddingParametersUsingInsert()
    {
      MySqlCommand cmd = new MySqlCommand();
      cmd.Parameters.Insert(0, new MySqlParameter("?id", MySqlDbType.Int32));
      MySqlParameter p = cmd.Parameters["?id"];
      Assert.Equal("?id", p.ParameterName);
    }

    /// <summary>
    /// Bug #27187 cmd.Parameters.RemoveAt("Id") will cause an error if the last item is requested 
    /// </summary>
    [Fact]
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
      Assert.Equal("?id6", p.ParameterName);
    }

    /// <summary>
    /// Bug #29312  	System.FormatException if parameter not found
    /// </summary>
    [Fact]
    public void MissingParameter()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test(id) VALUES (?id)", Connection);
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
    [Fact]
    public void StringParameterSizeSetAfterValue()
    {
      executeSQL("CREATE TABLE Test (v VARCHAR(10))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?p1)", Connection);
      cmd.Parameters.Add("?p1", MySqlDbType.VarChar);
      cmd.Parameters[0].Value = "123";
      cmd.Parameters[0].Size = 10;
      cmd.ExecuteNonQuery();

#if !NETCOREAPP1_1
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal("123", dt.Rows[0][0]);
#endif

      cmd.Parameters.Clear();
      cmd.Parameters.Add("?p1", MySqlDbType.VarChar);
      cmd.Parameters[0].Value = "123456789012345";
      cmd.Parameters[0].Size = 10;
      cmd.ExecuteNonQuery();

#if NETCOREAPP1_1
      MySqlCommand newValueCommand = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader dr = newValueCommand.ExecuteReader())
      {
        Assert.True(dr.Read());
        Assert.Equal("123", dr.GetString(0));
        Assert.True(dr.Read());
        Assert.Equal("1234567890", dr.GetString(0));
      }
#else
      dt.Clear();
      da.Fill(dt);
      Assert.Equal("1234567890", dt.Rows[1][0]);
#endif
    }

#if !NETCOREAPP1_1
    /// <summary>
    /// Bug #32093 MySqlParameter Constructor does not allow Direction of anything other than Input 
    /// </summary>
    [Fact]
    public void NonInputParametersToCtor()
    {
      MySqlParameter p = new MySqlParameter("?p1", MySqlDbType.VarChar, 20,
          ParameterDirection.InputOutput, true, 0, 0, "id", DataRowVersion.Current, 0);
      Assert.Equal(ParameterDirection.InputOutput, p.Direction);

      MySqlParameter p1 = new MySqlParameter("?p1", MySqlDbType.VarChar, 20,
          ParameterDirection.Output, true, 0, 0, "id", DataRowVersion.Current, 0);
      Assert.Equal(ParameterDirection.Output, p1.Direction);
    }
#endif

    [Fact]
    public void UseAtSignForParameters()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, name) VALUES (@id, @name)", Connection);
      cmd.Parameters.AddWithValue("@id", 33);
      cmd.Parameters.AddWithValue("@name", "Test");
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal(33, reader.GetInt32(0));
        Assert.Equal("Test", reader.GetString(1));
      }
    }

#if !NETCOREAPP1_1
    /// <summary>
    /// Bug #62194	MySQL Parameter constructor doesn't set
    /// all properties: IsNullable, Precision and Scale
    /// </summary>
    [Fact]
    public void CanCreateMySQLParameterWithNullability()
    {

      MySqlParameter p = new MySqlParameter("?id", MySqlDbType.Decimal, 2,
                                          ParameterDirection.Input, true, 1, 1, "sourceColumn", DataRowVersion.Default, 1);

      Assert.True(p.IsNullable);
    }

    /// <summary>
    /// Bug #62194	MySQL Parameter constructor doesn't set
    /// all properties: IsNullable, Precision and Scale
    /// </summary>
    [Fact]
    public void CanCreateMySQLParameterWithPrecision()
    {
      MySqlParameter p = new MySqlParameter("?id", MySqlDbType.Decimal, 2,
                                          ParameterDirection.Input, true, Byte.MaxValue, 1, "sourceColumn", DataRowVersion.Default, 1);

      Assert.Equal(p.Precision, Byte.MaxValue);
    }


    /// <summary>
    /// Bug #62194	MySQL Parameter constructor doesn't set
    /// all properties: IsNullable, Precision and Scale
    /// </summary>
    [Fact]
    public void CanCreateMySQLParameterWithScale()
    {

      MySqlParameter p = new MySqlParameter("?id", MySqlDbType.Decimal, 2,
                                          ParameterDirection.Input, true, 1, Byte.MaxValue, "sourceColumn", DataRowVersion.Default, 1);

      Assert.Equal(p.Scale, Byte.MaxValue);
    }
#endif

    /// <summary>
    /// Bug #66060 #14499549 "Parameter '?' must be defined" error, when using unnamed parameters
    /// </summary>
    [Fact]
    public void CanIdentifyParameterWithOutName()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES (?, ?)", Connection);

      cmd.Parameters.AddWithValue("", 1);
      cmd.Parameters.AddWithValue("", "test");

      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT id FROM Test";
      Assert.Equal(1, cmd.ExecuteScalar());

      cmd.CommandText = "SELECT name FROM Test";
      Assert.Equal("test", cmd.ExecuteScalar());
    }

    /// <summary>
    /// Bug #66060  #14499549  "Parameter '?' must be defined" error, when using unnamed parameters
    /// </summary>
    [Fact]   
    public void CanThrowAnExceptionWhenMixingParameterNaming()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES (?Id, ?name, ?)", Connection);
      cmd.Parameters.AddWithValue("?Id", 1);
      cmd.Parameters.AddWithValue("?name", "test");
      Exception ex = Assert.Throws<MySqlException>(() =>cmd.ExecuteNonQuery());
      Assert.Equal("Fatal error encountered during command execution.", ex.Message);
    }

    /// <summary>
    /// Bug #22101727 CONNECTOR MODIFIES RESULT TYPE AFTER PARENT TINYINT VALUE IS NULL
    /// </summary>
    [Fact]
    public void TreatTinyAsBooleanWhenNull()
    {
      executeSQL("CREATE TABLE testbool (id INT (10) UNSIGNED NOT NULL AUTO_INCREMENT, testcol TINYINT(1) DEFAULT NULL, PRIMARY KEY(id))");
      executeSQL("INSERT INTO testbool(testcol) VALUES(0),(1),(1),(NULL),(0),(0),(1)");

      using (var conn = new MySqlConnection(ConnectionSettings.ConnectionString))
      {
        conn.Open();
        MySqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM testbool";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            if (!(reader["testcol"] is DBNull))
              Assert.True(reader["testcol"] is bool);
  }
}
      }
    }

    /// <summary>
    /// Bug #27113566 MYSQLCOMMAND.PREPARE STOPS TINYINT(1) FROM BEING TREATED AS A .NET BOOL
    /// </summary>
    [Fact]
    public void TreatTinyAsBooleanWhenCallingPrepare()
    {
      executeSQL("CREATE TABLE `mysql_bug_test` (`test_key` varchar(10) NOT NULL, `test_val` tinyint(1) NOT NULL, PRIMARY KEY(`test_key`)) ENGINE = InnoDB DEFAULT CHARSET = utf8; ");
      executeSQL("LOCK TABLES `mysql_bug_test` WRITE;");
      executeSQL("INSERT INTO `mysql_bug_test` VALUES ('mykey',0);");
      executeSQL("UNLOCK TABLES;");

      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.IgnorePrepare = false;
      builder.CharacterSet = "utf8";
      builder.UseCompression = true;
      builder.TreatTinyAsBoolean = false;

      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        using (var cmd = new MySqlCommand("SELECT * FROM mysql_bug_test WHERE test_key = @TestKey", connection))
        {
          cmd.Parameters.AddWithValue("@TestKey", "mykey").MySqlDbType = MySqlDbType.VarChar;
          cmd.Prepare();
          using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
          {
            reader.Read();
            Assert.False(reader["test_val"] is bool);
          }
        }

        connection.Close();
      }

      builder.TreatTinyAsBoolean = true;

      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        using (var cmd = new MySqlCommand("SELECT * FROM mysql_bug_test WHERE test_key = @TestKey", connection))
        {
          cmd.Parameters.AddWithValue("@TestKey", "mykey").MySqlDbType = MySqlDbType.VarChar;
          cmd.Prepare();
          using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
          {
            reader.Read();
            Assert.True(reader["test_val"] is bool);
          }
        }

        connection.Close();
      }
    }
  }
}
