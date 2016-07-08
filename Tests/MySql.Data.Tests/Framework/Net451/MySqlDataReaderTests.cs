// Copyright © 2013, 2014, Oracle and/or its affiliates. All rights reserved.
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
#if !RT
using System.Data.SqlTypes;
#endif

namespace MySql.Data.MySqlClient.Tests
{
  public class MySqlDataReaderTests : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;

    public void SetFixture(SetUpClass data)
    {
      st = data;
    }

    public void Dispose()
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");
    }

    private void CreateDefaultTable()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), d DATE, dt DATETIME, b1 LONGBLOB, PRIMARY KEY(id))");
    }

    [Fact]
    public void TestMultipleResultsets()
    {
      CreateDefaultTable();

      MySqlCommand cmd = new MySqlCommand("", st.conn);
      // insert 100 records
      cmd.CommandText = "INSERT INTO Test (id,name) VALUES (?id, 'test')";
      cmd.Parameters.Add(new MySqlParameter("?id", 1));
      for (int i = 1; i <= 100; i++)
      {
        cmd.Parameters[0].Value = i;
        cmd.ExecuteNonQuery();
      }

      // execute it one time
      cmd = new MySqlCommand("SELECT id FROM Test WHERE id<50; SELECT * FROM Test WHERE id >= 50;", st.conn);
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
        "INSERT INTO Test (id, name, b1) VALUES(1, 'Test', ?b1)", st.conn);
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
      st.execSQL("INSERT INTO Test (id, name, b1) VALUES (1, 'Test1', NULL)");
      st.execSQL("INSERT INTO Test (id, name, b1) VALUES (2, 'Test1', NULL)");

      MySqlCommand cmd = new MySqlCommand(
        "SELECT * FROM Test WHERE id=1; SELECT * FROM Test WHERE id=2", st.conn);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleResult))
      {
        bool result = reader.Read();
        Assert.Equal(true, result);

        result = reader.NextResult();
        Assert.Equal(false, result);
      }
    }


#if !RT
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

      st.execSQL(sql);
      st.execSQL("INSERT INTO test2 VALUES(1,'Test', 'Test', 1.0, now(), 20.0, 12.324, True)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM test2", st.conn);
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
      st.execSQL("INSERT INTO Test(id,name) VALUES(1,'test')");

      using (MySqlConnection c2 = new MySqlConnection(st.conn.ConnectionString))
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
      st.execSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");
      st.execSQL("INSERT INTO Test(id,name) VALUES(2,'test2')");
      st.execSQL("INSERT INTO Test(id,name) VALUES(3,'test3')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", st.conn);
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
      st.execSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");
      st.execSQL("INSERT INTO Test(id,name) VALUES(2,'test2')");
      st.execSQL("INSERT INTO Test(id,name) VALUES(3,'test3')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test LIMIT 2", st.conn);
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
      st.execSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", st.conn);
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
      st.execSQL("INSERT INTO Test (id, name, dt) VALUES (1, 'Test', NULL)");
      st.execSQL("INSERT INTO Test (id, name, dt) VALUES (2, NULL, now())");
      st.execSQL("INSERT INTO Test (id, name, dt) VALUES (3, 'Test2', NULL)");

      MySqlCommand cmd = new MySqlCommand("SELECT id, name, dt FROM Test", st.conn);
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
#if !RT
        Exception ex = Assert.Throws<SqlNullValueException>(() => reader.GetString(1));
        Assert.Equal(ex.Message, "Data is Null. This method or property cannot be called on Null values.");       
#endif
        Assert.False(reader.IsDBNull(2));
        reader.Read();
        Assert.Equal(3, reader.GetValue(0));
        Assert.Equal("Test2", reader.GetValue(1));
        Assert.Equal("Test2", reader.GetString(1));
        Assert.Equal(DBNull.Value, reader.GetValue(2));
#if !RT
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
      MySqlCommand cmd = new MySqlCommand("USE `" + st.database0 + "`; SHOW TABLES", st.conn);
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
      st.execSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", st.conn);
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
      st.execSQL("CREATE TABLE Test (id int, t1 TEXT)");
      st.execSQL("INSERT INTO Test VALUES (1, 'Text value')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", st.conn);
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
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", st.conn);
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
      st.execSQL("INSERT INTO Test (id, name) VALUES (1, 'a')");
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", st.conn);
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
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES (1,'Test')", st.conn);
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
      MySqlCommand cmd = new MySqlCommand("", st.conn);
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
        "SELECT b1 FROM Test WHERE id >= ?param1;", st.conn);

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


    [Fact]
    public void TestMultipleResultsWithQueryCacheOn()
    {
      CreateDefaultTable();
      st.execSQL("SET SESSION query_cache_type = ON");
      st.execSQL("INSERT INTO Test (id,name) VALUES (1, 'Test')");
      st.execSQL("INSERT INTO Test (id,name) VALUES (51, 'Test')");

      // execute it one time
      MySqlCommand cmd = new MySqlCommand("SELECT id FROM Test WHERE id<50; SELECT * FROM Test	WHERE id >= 50;", st.conn);

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
    /// Bug #8630  	Executing a query with the SchemaOnly option reads the entire resultset
    /// </summary>
    [Fact]
    public void SchemaOnly()
    {
        CreateDefaultTable();
        st.execSQL("INSERT INTO Test (id,name) VALUES(1,'test1')");
        st.execSQL("INSERT INTO Test (id,name) VALUES(2,'test2')");
        st.execSQL("INSERT INTO Test (id,name) VALUES(3,'test3')");

        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", st.conn);
        using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
        {
#if RT
            Assert.Equal(5, reader.FieldCount);
#else
        DataTable table = reader.GetSchemaTable();
        Assert.Equal(5, table.Rows.Count);
        Assert.Equal(22, table.Columns.Count);
#endif

            Assert.False(reader.Read());
        }
    }

    /// <summary>
    /// Bug #9237  	MySqlDataReader.AffectedRecords not set to -1
    /// </summary>
    [Fact]
    public void AffectedRows()
    {
      MySqlCommand cmd = new MySqlCommand("SHOW TABLES", st.conn);
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
      st.execSQL("CREATE TABLE Test (tm TIMESTAMP)");
      st.execSQL("INSERT INTO Test VALUES (NULL)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test WHERE tm = '7/1/2005 12:00:00 AM'", st.conn);
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
        st.execSQL("CREATE TABLE Test (id int, PRIMARY KEY(id))");
        MySqlCommand cmd = new MySqlCommand(
        String.Format("SHOW INDEX FROM Test FROM `{0}`", st.database0), st.conn);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
            reader.Read();
#if RT
            Assert.Throws<MySqlNullValueException>(() => reader.GetString(reader.GetOrdinal("Sub_part")));
#else
        Assert.Throws<System.Data.SqlTypes.SqlNullValueException>(()=> reader.GetString(reader.GetOrdinal("Sub_part")));                              
#endif
        }
    }


#if !RT
    /// <summary>
    /// Bug #23538 Exception thrown when GetSchemaTable is called and "fields" is null. 
    /// </summary>
    [Fact]
    public void GetSchemaTableOnEmptyResultset()
    {
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE PROCEDURE spTest() BEGIN END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
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
      st.execSQL("INSERT INTO Test (id, name) VALUES (1, '')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", st.conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.False(reader.IsDBNull(1));
      }
    }


#if !RT
    /// <summary>
    /// Bug #30204  	Incorrect ConstraintException
    /// </summary>
    [Fact]
    public void ConstraintWithLoadingReader()
    {
      st.execSQL(@"CREATE TABLE Test (ID_A int(11) NOT NULL,
        ID_B int(11) NOT NULL, PRIMARY KEY (ID_A,ID_B)
        ) ENGINE=MyISAM DEFAULT CHARSET=latin1;");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", st.conn);
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
      st.execSQL("INSERT INTO Test(id,name) VALUES(1,'test')");

      using (MySqlConnection c2 = new MySqlConnection(st.conn.ConnectionString))
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

      MySqlCommand cmd = new MySqlCommand("select * from doesnotexist", st.conn);
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
      st.execSQL("INSERT INTO Test (id, name) VALUES (1, 'test')");

      MySqlCommand cmd = new MySqlCommand("SELECT a.name, a.name FROM Test a", st.conn);
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
      MySqlCommand cmd = new MySqlCommand("SELECT 1 as c1", st.conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Type t = reader.GetFieldType("c1");
        Exception ex = Assert.Throws<IndexOutOfRangeException>(() => reader.GetOrdinal("nocol"));
        Assert.True(ex.Message.IndexOf("nocol") != -1);        
      }
    }

#if !RT
    /// <summary>
    /// Tests fix for bug "ConstraintException when filling a datatable" (MySql bug #65065).
    /// </summary>
    [Fact]
    public void ConstraintExceptionOnLoad()
    {
      MySqlConnection con = new MySqlConnection();
      try
      {
        con.ConnectionString = st.GetConnectionString(true);
        con.Open();

        MySqlCommand cmd = new MySqlCommand();

        cmd.Connection = con;

        cmd.CommandText = "DROP TABLE IF EXISTS trx";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE IF EXISTS camn";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE `camn` (" +
          "`id_camn` int(10) unsigned NOT NULL AUTO_INCREMENT," +
          "`no` int(4) unsigned NOT NULL," +
          "`marq` varchar(45) COLLATE utf8_bin DEFAULT NULL," +
          "`modl` varchar(45) COLLATE utf8_bin DEFAULT NULL," +
          "`no_serie` varchar(17) COLLATE utf8_bin DEFAULT NULL," +
          "`no_plaq` varchar(7) COLLATE utf8_bin DEFAULT NULL," +
          "PRIMARY KEY (`id_camn`)," +
          "UNIQUE KEY `id_camn_UNIQUE` (`id_camn`)," +
          "UNIQUE KEY `no_UNIQUE` (`no`)," +
          "UNIQUE KEY `no_serie_UNIQUE` (`no_serie`)," +
          "UNIQUE KEY `no_plaq_UNIQUE` (`no_plaq`)" +
          ") ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8 COLLATE=utf8_bin";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE `trx` (" +
          "`id_trx` int(10) unsigned NOT NULL AUTO_INCREMENT," +
          "`mnt` decimal(9,2) NOT NULL," +
          "`dat_trx` date NOT NULL," +
          "`typ_trx` varchar(45) COLLATE utf8_bin DEFAULT NULL," +
          "`descr` tinytext COLLATE utf8_bin," +
          "`id_camn` int(10) unsigned DEFAULT NULL," +
          "PRIMARY KEY (`id_trx`)," +
          "UNIQUE KEY `id_trx_UNIQUE` (`id_trx`)," +
          "KEY `fk_trx_camn` (`id_camn`)," +
          "CONSTRAINT `fk_trx_camn` FOREIGN KEY (`id_camn`) REFERENCES `camn` (`id_camn`) ON DELETE NO ACTION ON UPDATE NO ACTION" +
          ") ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8 COLLATE=utf8_bin";

        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO camn(id_camn, no, marq, modl, no_serie, no_plaq) VALUES(9, 3327, null, null, null, null);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO trx(id_trx, mnt, dat_trx, typ_trx, descr, id_camn) VALUES(1, 10, '2012-04-30', null, null, 9);";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO trx(id_trx, mnt, dat_trx, typ_trx, descr, id_camn) VALUES(2, 10, '2012-04-15', null, null, 9);";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO trx(id_trx, mnt, dat_trx, typ_trx, descr, id_camn) VALUES(3, 10, '2012-04-15', null, null, null);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT cam.no_serie, t.mnt FROM trx t LEFT JOIN camn cam USING(id_camn) ";
        MySqlDataReader dr = cmd.ExecuteReader();
        DataTable dataTable = new DataTable();
        DataSet ds = new DataSet();
        dataTable.Load( dr );
        dr.Close();
      }
      finally
      {
        con.Close();
      }
    }
#endif
  }
}
