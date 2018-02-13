// Copyright © 2013, 2018, Oracle and/or its affiliates. All rights reserved.
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
#if !NETCOREAPP1_1
using System.Data.SqlTypes;
#endif

namespace MySql.Data.MySqlClient.Tests
{
  public partial class MySqlDataReaderTests : TestBase
  {
    public MySqlDataReaderTests(TestFixture fixture) : base(fixture)
    {
    }


    private void CreateDefaultTable()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), d DATE, dt DATETIME, b1 LONGBLOB, PRIMARY KEY(id))");
    }

    [Fact]
    public void TestMultipleResultsets()
    {
      CreateDefaultTable();

      MySqlCommand cmd = new MySqlCommand("", Connection);
      // insert 100 records
      cmd.CommandText = "INSERT INTO Test (id,name) VALUES (?id, 'test')";
      cmd.Parameters.Add(new MySqlParameter("?id", 1));
      for (int i = 1; i <= 100; i++)
      {
        cmd.Parameters[0].Value = i;
        cmd.ExecuteNonQuery();
      }

      // execute it one time
      cmd = new MySqlCommand("SELECT id FROM Test WHERE id<50; SELECT * FROM Test WHERE id >= 50;", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.NotNull(reader);
        Assert.Equal(true, reader.HasRows);
        Assert.True(reader.Read());
        Assert.Equal(1, reader.FieldCount);
        Assert.True(reader.NextResult());
        Assert.Equal(true, reader.HasRows);
        Assert.Equal(5, reader.FieldCount);
      }

      // now do it again
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.NotNull(reader);
        Assert.Equal(true, reader.HasRows);
        Assert.True(reader.Read());
        Assert.Equal(1, reader.FieldCount);
        Assert.True(reader.NextResult());
        Assert.Equal(true, reader.HasRows);
        Assert.Equal(5, reader.FieldCount);
      }
    }

    [Fact]
    public void GetBytes()
    {
      CreateDefaultTable();
      int len = 50000;
      byte[] bytes = Utils.CreateBlob(len);
      MySqlCommand cmd = new MySqlCommand(
        "INSERT INTO Test (id, name, b1) VALUES(1, 'Test', ?b1)", Connection);
      cmd.Parameters.AddWithValue("?b1", bytes);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();

        long sizeBytes = reader.GetBytes(4, 0, null, 0, 0);
        Assert.Equal(len, sizeBytes);

        byte[] buff1 = new byte[len / 2];
        byte[] buff2 = new byte[len - (len / 2)];
        long buff1cnt = reader.GetBytes(4, 0, buff1, 0, len / 2);
        long buff2cnt = reader.GetBytes(4, buff1cnt, buff2, 0, buff2.Length);
        Assert.Equal(buff1.Length, buff1cnt);
        Assert.Equal(buff2.Length, buff2cnt);

        for (int i = 0; i < buff1.Length; i++)
          Assert.Equal(bytes[i], buff1[i]);

        for (int i = 0; i < buff2.Length; i++)
          Assert.Equal(bytes[buff1.Length + i], buff2[i]);
      }

      //  now check with sequential access
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
      {
        Assert.True(reader.Read());
        int mylen = len;
        byte[] buff = new byte[8192];
        int startIndex = 0;
        while (mylen > 0)
        {
          int readLen = Math.Min(mylen, buff.Length);
          int retVal = (int)reader.GetBytes(4, startIndex, buff, 0, readLen);
          Assert.Equal(readLen, retVal);
          for (int i = 0; i < readLen; i++)
            Assert.Equal(bytes[startIndex + i], buff[i]);
          startIndex += readLen;
          mylen -= readLen;
        }
      }
    }

    [Fact]
    public void TestSingleResultSetBehavior()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test (id, name, b1) VALUES (1, 'Test1', NULL)");
      executeSQL("INSERT INTO Test (id, name, b1) VALUES (2, 'Test1', NULL)");

      MySqlCommand cmd = new MySqlCommand(
        "SELECT * FROM Test WHERE id=1; SELECT * FROM Test WHERE id=2", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleResult))
      {
        bool result = reader.Read();
        Assert.Equal(true, result);

        result = reader.NextResult();
        Assert.Equal(false, result);
      }
    }


#if !NETCOREAPP1_1
    /// <summary>
    /// Bug #59989	MysqlDataReader.GetSchemaTable returns incorrect Values an types
    /// </summary>
    [Fact]
    public void GetSchema()
    {
      string sql = @"CREATE TABLE test2(id INT UNSIGNED AUTO_INCREMENT 
        NOT NULL, name VARCHAR(255) NOT NULL, name2 VARCHAR(40), fl FLOAT, 
        dt DATETIME, `udec` DECIMAL(20,6) unsigned,
        `dec` DECIMAL(44,3), bt boolean, PRIMARY KEY(id))";

      executeSQL(sql);
      executeSQL("INSERT INTO test2 VALUES(1,'Test', 'Test', 1.0, now(), 20.0, 12.324, True)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM test2", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        DataTable dt = reader.GetSchemaTable();
        Assert.True(true == (Boolean)dt.Rows[0]["IsAutoIncrement"], "Checking auto increment");
        Assert.False((bool)dt.Rows[0]["IsUnique"], "Checking IsUnique");
        Assert.True((bool)dt.Rows[0]["IsKey"]);
        Assert.True(false == (Boolean)dt.Rows[0]["AllowDBNull"], "Checking AllowDBNull");
        Assert.True(false == (Boolean)dt.Rows[1]["AllowDBNull"], "Checking AllowDBNull");
        Assert.Equal(255, dt.Rows[1]["ColumnSize"]);
        Assert.Equal(40, dt.Rows[2]["ColumnSize"]);

        // udec column
        Assert.Equal(21, dt.Rows[5]["ColumnSize"]);
        Assert.Equal(20, dt.Rows[5]["NumericPrecision"]);
        Assert.Equal(6, dt.Rows[5]["NumericScale"]);

        // dec column
        Assert.Equal(46, dt.Rows[6]["ColumnSize"]);
        Assert.Equal(44, dt.Rows[6]["NumericPrecision"]);
        Assert.Equal(3, dt.Rows[6]["NumericScale"]);
      }
    }
#endif

    [Fact]
    public void CloseConnectionBehavior()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test(id,name) VALUES(1,'test')");

      using (MySqlConnection c2 = new MySqlConnection(Connection.ConnectionString))
      {
        c2.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c2);
        using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
        {
          Assert.True(reader.Read());
          reader.Close();
          Assert.True(c2.State == ConnectionState.Closed);
        }
      }
    }

    [Fact]
    public void SingleRowBehavior()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");
      executeSQL("INSERT INTO Test(id,name) VALUES(2,'test2')");
      executeSQL("INSERT INTO Test(id,name) VALUES(3,'test3')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.True(reader.Read(), "First read");
        Assert.False(reader.Read(), "Second read");
        Assert.False(reader.NextResult(), "Trying NextResult");
      }

      cmd.CommandText = "SELECT * FROM Test where id=1";
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.True(reader.Read());
        Assert.Equal("test1", reader.GetString(1));
        Assert.False(reader.Read());
        Assert.False(reader.NextResult());
      }
    }

    [Fact]
    public void SingleRowBehaviorWithLimit()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");
      executeSQL("INSERT INTO Test(id,name) VALUES(2,'test2')");
      executeSQL("INSERT INTO Test(id,name) VALUES(3,'test3')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test LIMIT 2", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.True(reader.Read(), "First read");
        Assert.False(reader.Read(), "Second read");
        Assert.False(reader.NextResult(), "Trying NextResult");
      }

      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.True(reader.Read(), "First read");
        Assert.False(reader.Read(), "Second read");
        Assert.False(reader.NextResult(), "Trying NextResult");
      }

      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.True(reader.Read(), "First read");
        Assert.False(reader.Read(), "Second read");
        Assert.False(reader.NextResult(), "Trying NextResult");
      }
    }

    [Fact]
    public void SimpleSingleRow()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read(), "First read");
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("test1", reader.GetString(1));
        Assert.False(reader.Read(), "Second read");
        Assert.False(reader.NextResult(), "Trying NextResult");
      }
    }

    [Fact]
    public void ConsecutiveNulls()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test (id, name, dt) VALUES (1, 'Test', NULL)");
      executeSQL("INSERT INTO Test (id, name, dt) VALUES (2, NULL, now())");
      executeSQL("INSERT INTO Test (id, name, dt) VALUES (3, 'Test2', NULL)");

      MySqlCommand cmd = new MySqlCommand("SELECT id, name, dt FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal(1, reader.GetValue(0));
        Assert.Equal("Test", reader.GetValue(1));
        Assert.Equal("Test", reader.GetString(1));
        Assert.Equal(DBNull.Value, reader.GetValue(2));
        reader.Read();
        Assert.Equal(2, reader.GetValue(0));
        Assert.Equal(DBNull.Value, reader.GetValue(1));
#if !NETCOREAPP1_1
        Exception ex = Assert.Throws<SqlNullValueException>(() => reader.GetString(1));
        Assert.Equal(ex.Message, "Data is Null. This method or property cannot be called on Null values.");
#endif
        Assert.False(reader.IsDBNull(2));
        reader.Read();
        Assert.Equal(3, reader.GetValue(0));
        Assert.Equal("Test2", reader.GetValue(1));
        Assert.Equal("Test2", reader.GetString(1));
        Assert.Equal(DBNull.Value, reader.GetValue(2));
#if !NETCOREAPP1_1
        ex = Assert.Throws<SqlNullValueException>(() => reader.GetMySqlDateTime(2));
        Assert.Equal(ex.Message, "Data is Null. This method or property cannot be called on Null values.");
#endif
        Assert.False(reader.Read());
        Assert.False(reader.NextResult());
      }
    }

    [Fact]
    public void HungDataReader()
    {
      MySqlCommand cmd = new MySqlCommand("USE `" + Connection.Database + "`; SHOW TABLES", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.Read())
        {
          reader.GetString(0);
        }
      }
    }

    /// <summary>
    /// Added test for IsDBNull from bug# 7399
    /// </summary>
    [Fact]
    public void SequentialAccessBehavior()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
      {
        Assert.True(reader.Read());
        Assert.False(reader.IsDBNull(0));
        int i = reader.GetInt32(0);
        string s = reader.GetString(1);
        Assert.Equal(1, i);
        Assert.Equal("test1", s);

        // this next line should throw an exception
        Exception ex = Assert.Throws<MySqlException>(() => i = reader.GetInt32(0));
        Assert.Equal(ex.Message, "Invalid attempt to read a prior column using SequentialAccess");
      }
    }


    [Fact]
    public void ReadingTextFields()
    {
      executeSQL("CREATE TABLE Test (id int, t1 TEXT)");
      executeSQL("INSERT INTO Test VALUES (1, 'Text value')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string s = reader["t1"].ToString();
        Assert.Equal("Text value", s);
      }
    }

    [Fact]
    public void ReadingFieldsBeforeRead()
    {
      CreateDefaultTable();
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        try
        {
          reader.GetInt32(0);
        }
        catch (MySqlException)
        {
        }
      }
    }

    [Fact]
    public void GetChar()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test (id, name) VALUES (1, 'a')");
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        char achar = reader.GetChar(1);
        Assert.Equal('a', achar);
      }
    }

    [Fact]
    public void ReaderOnNonQuery()
    {
      CreateDefaultTable();
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES (1,'Test')", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.False(reader.Read());
        reader.Close();

        cmd.CommandText = "SELECT name FROM Test";
        object v = cmd.ExecuteScalar();
        Assert.Equal("Test", v);
      }
    }

    [Fact]
    public void TestManyDifferentResultsets()
    {
      CreateDefaultTable();
      MySqlCommand cmd = new MySqlCommand("", Connection);
      // insert 100 records
      cmd.CommandText = "INSERT INTO Test (id,name,dt,b1) VALUES (?id, 'test','2004-12-05 12:57:00','long blob data')";
      cmd.Parameters.Add(new MySqlParameter("?id", 1));
      for (int i = 1; i <= 100; i++)
      {
        cmd.Parameters[0].Value = i;
        cmd.ExecuteNonQuery();
      }

      cmd = new MySqlCommand("SELECT id FROM Test WHERE id<?param1; " +
        "SELECT id,name FROM Test WHERE id = -50; " +
        "SELECT * FROM Test WHERE id >= ?param1; " +
        "SELECT id, dt, b1 FROM Test WHERE id = -50; " +
        "SELECT b1 FROM Test WHERE id = -50; " +
        "SELECT id, dt, b1 FROM Test WHERE id < ?param1; " +
        "SELECT b1 FROM Test WHERE id >= ?param1;", Connection);

      cmd.Parameters.AddWithValue("?param1", 50);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        //First ResultSet, should have 49 rows.
        //SELECT id FROM Test WHERE id<?param1;
        Assert.Equal(true, reader.HasRows);
        Assert.Equal(1, reader.FieldCount);
        for (int i = 0; i < 49; i++)
        {
          Assert.True(reader.Read());
        }
        Assert.Equal(false, reader.Read());

        //Second ResultSet, should have no rows.
        //SELECT id,name FROM Test WHERE id = -50;
        Assert.True(reader.NextResult());
        Assert.Equal(false, reader.HasRows);
        Assert.Equal(2, reader.FieldCount);
        Assert.Equal(false, reader.Read());


        //Third ResultSet, should have 51 rows.
        //SELECT * FROM Test WHERE id >= ?param1;
        Assert.True(reader.NextResult());
        Assert.Equal(true, reader.HasRows);
        Assert.Equal(5, reader.FieldCount);
        for (int i = 0; i < 51; i++)
        {
          Assert.True(reader.Read());
        }
        Assert.Equal(false, reader.Read());


        //Fourth ResultSet, should have no rows.
        //SELECT id, dt, b1 FROM Test WHERE id = -50;
        Assert.True(reader.NextResult());
        Assert.Equal(false, reader.HasRows);
        Assert.Equal(3, reader.FieldCount); //Will Fail if uncommented expected 3 returned 5
        Assert.Equal(false, reader.Read());

        //Fifth ResultSet, should have no rows.
        //SELECT b1 FROM Test WHERE id = -50;
        Assert.True(reader.NextResult());
        Assert.Equal(false, reader.HasRows);
        Assert.Equal(1, reader.FieldCount); //Will Fail if uncommented expected 1 returned 5
        Assert.Equal(false, reader.Read());

        //Sixth ResultSet, should have 49 rows.
        //SELECT id, dt, b1 FROM Test WHERE id < ?param1;
        Assert.True(reader.NextResult());
        Assert.Equal(true, reader.HasRows);
        Assert.Equal(3, reader.FieldCount); //Will Fail if uncommented expected 3 returned 5
        for (int i = 0; i < 49; i++)
        {
          Assert.True(reader.Read());
        }
        Assert.Equal(false, reader.Read());

        //Seventh ResultSet, should have 51 rows.
        //SELECT b1 FROM Test WHERE id >= ?param1;
        Assert.True(reader.NextResult());
        Assert.Equal(true, reader.HasRows);
        Assert.Equal(1, reader.FieldCount); //Will Fail if uncommented expected 1 returned 5
        for (int i = 0; i < 51; i++)
        {
          Assert.True(reader.Read());
        }
        Assert.Equal(false, reader.Read());
      }
    }


    [Fact(Skip="Fix This")]
    public void TestMultipleResultsWithQueryCacheOn()
    {
      //query_cache_type was deprecated in server 5.7.20.
      if (Connection.driver.Version.isAtLeast(5,7,20)) return;

      CreateDefaultTable();
      executeSQL("SET SESSION query_cache_type = ON");
      executeSQL("INSERT INTO Test (id,name) VALUES (1, 'Test')");
      executeSQL("INSERT INTO Test (id,name) VALUES (51, 'Test')");

      // execute it one time
      MySqlCommand cmd = new MySqlCommand("SELECT id FROM Test WHERE id<50; SELECT * FROM Test	WHERE id >= 50;", Connection);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.NotNull(reader);
        Assert.Equal(true, reader.HasRows);
        Assert.True(reader.Read());
        Assert.Equal(1, reader.FieldCount);
        Assert.True(reader.NextResult());
        Assert.Equal(true, reader.HasRows);
        Assert.Equal(5, reader.FieldCount);
      }

      // now do it again
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.NotNull(reader);
        Assert.Equal(true, reader.HasRows);
        Assert.True(reader.Read());
        Assert.Equal(1, reader.FieldCount);
        Assert.True(reader.NextResult());
        Assert.Equal(true, reader.HasRows);
        Assert.Equal(5, reader.FieldCount);
      }
    }

    /// <summary>
    /// Bug #9237  	MySqlDataReader.AffectedRecords not set to -1
    /// </summary>
    [Fact]
    public void AffectedRows()
    {
      MySqlCommand cmd = new MySqlCommand("SHOW TABLES", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        reader.Close();
        Assert.Equal(-1, reader.RecordsAffected);
      }
    }

    /// <summary>
    /// Bug #11873  	Invalid timestamp in query produces incorrect reader exception
    /// </summary>
    [Fact]
    public void InvalidTimestamp()
    {
      executeSQL("CREATE TABLE Test (tm TIMESTAMP)");
      executeSQL("INSERT INTO Test VALUES (NULL)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test WHERE tm = '7/1/2005 12:00:00 AM'", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
      }
    }

    /// <summary>
    /// Bug #19294 IDataRecord.GetString method should return null for null values
    /// </summary>
    [Fact]
    public void GetStringOnNull()
    {
      executeSQL("CREATE TABLE Test (id int, PRIMARY KEY(id))");
      MySqlCommand cmd = new MySqlCommand(
      String.Format("SHOW INDEX FROM Test FROM `{0}`", Connection.Database), Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Throws<System.Data.SqlTypes.SqlNullValueException>(() => reader.GetString(reader.GetOrdinal("Sub_part")));
      }
    }


#if !NETCOREAPP1_1
    /// <summary>
    /// Bug #23538 Exception thrown when GetSchemaTable is called and "fields" is null. 
    /// </summary>
    [Fact]
    public void GetSchemaTableOnEmptyResultset()
    {
      executeSQL("CREATE PROCEDURE spTest() BEGIN END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        DataTable dt = reader.GetSchemaTable();
        Assert.Null(dt);
      }
    }
#endif

    /// <summary>
    /// Bug #24765  	Retrieving empty fields results in check for isDBNull
    /// </summary>
    [Fact]
    public void IsDbNullOnNonNullFields()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test (id, name) VALUES (1, '')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.False(reader.IsDBNull(1));
      }
    }


#if !NETCOREAPP1_1
    /// <summary>
    /// Bug #30204  	Incorrect ConstraintException
    /// </summary>
    [Fact]
    public void ConstraintWithLoadingReader()
    {
      executeSQL(@"CREATE TABLE Test (ID_A int(11) NOT NULL,
        ID_B int(11) NOT NULL, PRIMARY KEY (ID_A,ID_B)
        ) ENGINE=MyISAM DEFAULT CHARSET=latin1;");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        dt.Load(reader);
      }

      DataRow row = dt.NewRow();
      row["ID_A"] = 2;
      row["ID_B"] = 3;
      dt.Rows.Add(row);

      row = dt.NewRow();
      row["ID_A"] = 2;
      row["ID_B"] = 4;
      dt.Rows.Add(row);
    }
#endif

    [Fact]
    public void CloseConnectionBehavior2()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test(id,name) VALUES(1,'test')");

      using (MySqlConnection c2 = new MySqlConnection(Connection.ConnectionString))
      {
        c2.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c2);
        using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
        {
          Assert.True(reader.Read());
        }
      }
    }

    /// <summary>
    /// Test that using command behavior SchemaOnly does not hose the st.connection
    /// by leaving SQL_SELECT_LIMIT set to 0 after the error (and in normal
    /// case too)
    /// 
    /// Bug#30518
    /// </summary>
    [Fact]
    public void CommandBehaviorSchemaOnly()
    {

      MySqlCommand cmd = new MySqlCommand("select * from doesnotexist", Connection);
      MySqlDataReader reader;
      Exception ex = Assert.Throws<MySqlException>(() => reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly));
      Assert.True(ex.Message.Contains(".doesnotexist' doesn't exist"));

      // Check that failed ExecuteReader did not leave SQL_SELECT_LIMIT
      // set to 0.
      cmd.CommandText = "select now()";
      reader = cmd.ExecuteReader();
      Assert.True(reader.Read());
      reader.Close();


      // Check that CommandBehavior.SchemaOnly does not return rows
      reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
      Assert.False(reader.Read());
      reader.Close();


      reader = cmd.ExecuteReader();
      // Check that prior setting of CommandBehavior did not 
      // leave SQL_SELECT_LIMIT set to 0
      Assert.True(reader.Read());
      reader.Close();
    }

    /// <summary>
    /// Bug #37239 MySqlReader GetOrdinal performance changes break existing functionality
    /// </summary>
    [Fact]
    public void ColumnsWithSameName()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test (id, name) VALUES (1, 'test')");

      MySqlCommand cmd = new MySqlCommand("SELECT a.name, a.name FROM Test a", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string name1 = reader.GetString(0);
        string name2 = reader.GetString(1);
        Assert.Equal(name1, name2);
        Assert.Equal(name1, "test");
      }

      cmd.CommandText = "SELECT 'a' AS XYZ, 'b' as Xyz";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string name1 = reader.GetString(0);
        string name2 = reader.GetString(1);
      }
    }

    /// <summary>
    /// Bug #47467	Two simple changes for DataReader
    /// </summary>
    [Fact]
    public void Bug47467()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT 1 as c1", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Type t = reader.GetFieldType("c1");
        Exception ex = Assert.Throws<IndexOutOfRangeException>(() => reader.GetOrdinal("nocol"));
        Assert.True(ex.Message.IndexOf("nocol") != -1);
      }
    }

  }
}
