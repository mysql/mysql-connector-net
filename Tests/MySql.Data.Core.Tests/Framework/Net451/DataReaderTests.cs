// Copyright © 2004, 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Diagnostics;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using NUnit.Framework;

namespace MySql.Data.MySqlClient.Tests
{
  /// <summary>
  /// Summary description for ConnectionTests.
  /// </summary>
  [TestFixture]
  public class DataReaderTests : BaseTest
  {
    private void CreateDefaultTable()
    {
      execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), d DATE, dt DATETIME, b1 LONGBLOB, PRIMARY KEY(id))");
    }

    [Test]
    public void TestMultipleResultsets()
    {
      CreateDefaultTable();

      MySqlCommand cmd = new MySqlCommand("", conn);
      // insert 100 records
      cmd.CommandText = "INSERT INTO Test (id,name) VALUES (?id, 'test')";
      cmd.Parameters.Add(new MySqlParameter("?id", 1));
      for (int i = 1; i <= 100; i++)
      {
        cmd.Parameters[0].Value = i;
        cmd.ExecuteNonQuery();
      }

      // execute it one time
      cmd = new MySqlCommand("SELECT id FROM Test WHERE id<50; SELECT * FROM Test WHERE id >= 50;", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.IsNotNull(reader);
        Assert.AreEqual(true, reader.HasRows);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(1, reader.FieldCount);
        Assert.IsTrue(reader.NextResult());
        Assert.AreEqual(true, reader.HasRows);
        Assert.AreEqual(5, reader.FieldCount);
      }

      // now do it again
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.IsNotNull(reader);
        Assert.AreEqual(true, reader.HasRows);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(1, reader.FieldCount);
        Assert.IsTrue(reader.NextResult());
        Assert.AreEqual(true, reader.HasRows);
        Assert.AreEqual(5, reader.FieldCount);
      }
    }

    [Test]
    public void GetBytes()
    {
      CreateDefaultTable();
      int len = 50000;
      byte[] bytes = Utils.CreateBlob(len);
      MySqlCommand cmd = new MySqlCommand(
        "INSERT INTO Test (id, name, b1) VALUES(1, 'Test', ?b1)", conn);
      cmd.Parameters.AddWithValue("?b1", bytes);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();

        long sizeBytes = reader.GetBytes(4, 0, null, 0, 0);
        Assert.AreEqual(len, sizeBytes);

        byte[] buff1 = new byte[len / 2];
        byte[] buff2 = new byte[len - (len / 2)];
        long buff1cnt = reader.GetBytes(4, 0, buff1, 0, len / 2);
        long buff2cnt = reader.GetBytes(4, buff1cnt, buff2, 0, buff2.Length);
        Assert.AreEqual(buff1.Length, buff1cnt);
        Assert.AreEqual(buff2.Length, buff2cnt);

        for (int i = 0; i < buff1.Length; i++)
          Assert.AreEqual(bytes[i], buff1[i]);

        for (int i = 0; i < buff2.Length; i++)
          Assert.AreEqual(bytes[buff1.Length + i], buff2[i]);
      }

      //  now check with sequential access
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
      {
        Assert.IsTrue(reader.Read());
        int mylen = len;
        byte[] buff = new byte[8192];
        int startIndex = 0;
        while (mylen > 0)
        {
          int readLen = Math.Min(mylen, buff.Length);
          int retVal = (int)reader.GetBytes(4, startIndex, buff, 0, readLen);
          Assert.AreEqual(readLen, retVal);
          for (int i = 0; i < readLen; i++)
            Assert.AreEqual(bytes[startIndex + i], buff[i]);
          startIndex += readLen;
          mylen -= readLen;
        }
      }
    }

    [Test]
    public void TestSingleResultSetBehavior()
    {
      CreateDefaultTable();
      execSQL("INSERT INTO Test (id, name, b1) VALUES (1, 'Test1', NULL)");
      execSQL("INSERT INTO Test (id, name, b1) VALUES (2, 'Test1', NULL)");

      MySqlCommand cmd = new MySqlCommand(
        "SELECT * FROM Test WHERE id=1; SELECT * FROM Test WHERE id=2", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleResult))
      {
        bool result = reader.Read();
        Assert.AreEqual(true, result);

        result = reader.NextResult();
        Assert.AreEqual(false, result);
      }
    }

#if !RT
    /// <summary>
    /// Bug #59989	MysqlDataReader.GetSchemaTable returns incorrect Values an types
    /// </summary>
    [Test]
    public void GetSchema()
    {
      string sql = @"CREATE TABLE test2(id INT UNSIGNED AUTO_INCREMENT 
				NOT NULL, name VARCHAR(255) NOT NULL, name2 VARCHAR(40), fl FLOAT, 
				dt DATETIME, `udec` DECIMAL(20,6) unsigned,
				`dec` DECIMAL(44,3), bt boolean, PRIMARY KEY(id))";

      execSQL(sql);
      execSQL("INSERT INTO test2 VALUES(1,'Test', 'Test', 1.0, now(), 20.0, 12.324, True)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM test2", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        DataTable dt = reader.GetSchemaTable();
        Assert.AreEqual(true, dt.Rows[0]["IsAutoIncrement"], "Checking auto increment");
        Assert.IsFalse((bool)dt.Rows[0]["IsUnique"], "Checking IsUnique");
        Assert.IsTrue((bool)dt.Rows[0]["IsKey"]);
        Assert.AreEqual(false, dt.Rows[0]["AllowDBNull"], "Checking AllowDBNull");
        Assert.AreEqual(false, dt.Rows[1]["AllowDBNull"], "Checking AllowDBNull");
        Assert.AreEqual(255, dt.Rows[1]["ColumnSize"]);
        Assert.AreEqual(40, dt.Rows[2]["ColumnSize"]);

        // udec column
        Assert.AreEqual(21, dt.Rows[5]["ColumnSize"]);
        Assert.AreEqual(20, dt.Rows[5]["NumericPrecision"]);
        Assert.AreEqual(6, dt.Rows[5]["NumericScale"]);

        // dec column
        Assert.AreEqual(46, dt.Rows[6]["ColumnSize"]);
        Assert.AreEqual(44, dt.Rows[6]["NumericPrecision"]);
        Assert.AreEqual(3, dt.Rows[6]["NumericScale"]);
      }
    }
#endif

    [Test]
    public void CloseConnectionBehavior()
    {
      CreateDefaultTable();
      execSQL("INSERT INTO Test(id,name) VALUES(1,'test')");

      using (MySqlConnection c2 = new MySqlConnection(conn.ConnectionString))
      {
        c2.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c2);
        using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
        {
          Assert.IsTrue(reader.Read());
          reader.Close();
          Assert.IsTrue(c2.State == ConnectionState.Closed);
        }
      }
    }

    [Test]
    public void SingleRowBehavior()
    {
      CreateDefaultTable();
      execSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");
      execSQL("INSERT INTO Test(id,name) VALUES(2,'test2')");
      execSQL("INSERT INTO Test(id,name) VALUES(3,'test3')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.IsTrue(reader.Read(), "First read");
        Assert.IsFalse(reader.Read(), "Second read");
        Assert.IsFalse(reader.NextResult(), "Trying NextResult");
      }

      cmd.CommandText = "SELECT * FROM Test where id=1";
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.IsTrue(reader.Read());
        Assert.AreEqual("test1", reader.GetString(1));
        Assert.IsFalse(reader.Read());
        Assert.IsFalse(reader.NextResult());
      }
    }

    [Test]
    public void SingleRowBehaviorWithLimit()
    {
      CreateDefaultTable();
      execSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");
      execSQL("INSERT INTO Test(id,name) VALUES(2,'test2')");
      execSQL("INSERT INTO Test(id,name) VALUES(3,'test3')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test LIMIT 2", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.IsTrue(reader.Read(), "First read");
        Assert.IsFalse(reader.Read(), "Second read");
        Assert.IsFalse(reader.NextResult(), "Trying NextResult");
      }

      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.IsTrue(reader.Read(), "First read");
        Assert.IsFalse(reader.Read(), "Second read");
        Assert.IsFalse(reader.NextResult(), "Trying NextResult");
      }

      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.IsTrue(reader.Read(), "First read");
        Assert.IsFalse(reader.Read(), "Second read");
        Assert.IsFalse(reader.NextResult(), "Trying NextResult");
      }
    }

    [Test]
    public void SimpleSingleRow()
    {
      CreateDefaultTable();
      execSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.IsTrue(reader.Read(), "First read");
        Assert.AreEqual(1, reader.GetInt32(0));
        Assert.AreEqual("test1", reader.GetString(1));
        Assert.IsFalse(reader.Read(), "Second read");
        Assert.IsFalse(reader.NextResult(), "Trying NextResult");
      }
    }

    [Test]
    public void ConsecutiveNulls()
    {
      CreateDefaultTable();
      execSQL("INSERT INTO Test (id, name, dt) VALUES (1, 'Test', NULL)");
      execSQL("INSERT INTO Test (id, name, dt) VALUES (2, NULL, now())");
      execSQL("INSERT INTO Test (id, name, dt) VALUES (3, 'Test2', NULL)");

      MySqlCommand cmd = new MySqlCommand("SELECT id, name, dt FROM Test", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual(1, reader.GetValue(0));
        Assert.AreEqual("Test", reader.GetValue(1));
        Assert.AreEqual("Test", reader.GetString(1));
        Assert.AreEqual(DBNull.Value, reader.GetValue(2));
        reader.Read();
        Assert.AreEqual(2, reader.GetValue(0));
        Assert.AreEqual(DBNull.Value, reader.GetValue(1));
        try
        {
          reader.GetString(1);
          Assert.Fail("Should not get here");
        }
        catch (Exception) { }
        Assert.IsFalse(reader.IsDBNull(2));
        reader.Read();
        Assert.AreEqual(3, reader.GetValue(0));
        Assert.AreEqual("Test2", reader.GetValue(1));
        Assert.AreEqual("Test2", reader.GetString(1));
        Assert.AreEqual(DBNull.Value, reader.GetValue(2));
        try
        {
          reader.GetMySqlDateTime(2);
          Assert.Fail("Should not get here");
        }
        catch (Exception) { }
        Assert.IsFalse(reader.Read());
        Assert.IsFalse(reader.NextResult());
      }
    }

    [Test]
    public void HungDataReader()
    {
      MySqlCommand cmd = new MySqlCommand("USE `" + database0 + "`; SHOW TABLES", conn);
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
    [Test]
    public void SequentialAccessBehavior()
    {
      CreateDefaultTable();
      execSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
      {
        Assert.IsTrue(reader.Read());
        Assert.IsFalse(reader.IsDBNull(0));
        int i = reader.GetInt32(0);
        string s = reader.GetString(1);
        Assert.AreEqual(1, i);
        Assert.AreEqual("test1", s);

        // this next line should throw an exception
        try
        {
          i = reader.GetInt32(0);
          Assert.Fail("This line should not execute");
        }
        catch (MySqlException)
        {
        }
      }
    }


    [Test]
    public void ReadingTextFields()
    {
      execSQL("CREATE TABLE Test (id int, t1 TEXT)");
      execSQL("INSERT INTO Test VALUES (1, 'Text value')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string s = reader["t1"].ToString();
        Assert.AreEqual("Text value", s);
      }
    }

    [Test]
    public void ReadingFieldsBeforeRead()
    {
      CreateDefaultTable();
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
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

    [Test]
    public void GetChar()
    {
      CreateDefaultTable();
      execSQL("INSERT INTO Test (id, name) VALUES (1, 'a')");
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        char achar = reader.GetChar(1);
        Assert.AreEqual('a', achar);
      }
    }

    [Test]
    public void ReaderOnNonQuery()
    {
      CreateDefaultTable();
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES (1,'Test')", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.IsFalse(reader.Read());
        reader.Close();

        cmd.CommandText = "SELECT name FROM Test";
        object v = cmd.ExecuteScalar();
        Assert.AreEqual("Test", v);
      }
    }

    [Test]
    public void TestManyDifferentResultsets()
    {
      CreateDefaultTable();
      MySqlCommand cmd = new MySqlCommand("", conn);
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
        "SELECT b1 FROM Test WHERE id >= ?param1;", conn);

      cmd.Parameters.AddWithValue("?param1", 50);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        //First ResultSet, should have 49 rows.
        //SELECT id FROM Test WHERE id<?param1;
        Assert.AreEqual(true, reader.HasRows);
        Assert.AreEqual(1, reader.FieldCount);
        for (int i = 0; i < 49; i++)
        {
          Assert.IsTrue(reader.Read());
        }
        Assert.AreEqual(false, reader.Read());

        //Second ResultSet, should have no rows.
        //SELECT id,name FROM Test WHERE id = -50;
        Assert.IsTrue(reader.NextResult());
        Assert.AreEqual(false, reader.HasRows);
        Assert.AreEqual(2, reader.FieldCount);
        Assert.AreEqual(false, reader.Read());


        //Third ResultSet, should have 51 rows.
        //SELECT * FROM Test WHERE id >= ?param1;
        Assert.IsTrue(reader.NextResult());
        Assert.AreEqual(true, reader.HasRows);
        Assert.AreEqual(5, reader.FieldCount);
        for (int i = 0; i < 51; i++)
        {
          Assert.IsTrue(reader.Read());
        }
        Assert.AreEqual(false, reader.Read());


        //Fourth ResultSet, should have no rows.
        //SELECT id, dt, b1 FROM Test WHERE id = -50;
        Assert.IsTrue(reader.NextResult());
        Assert.AreEqual(false, reader.HasRows);
        Assert.AreEqual(3, reader.FieldCount); //Will Fail if uncommented expected 3 returned 5
        Assert.AreEqual(false, reader.Read());

        //Fifth ResultSet, should have no rows.
        //SELECT b1 FROM Test WHERE id = -50;
        Assert.IsTrue(reader.NextResult());
        Assert.AreEqual(false, reader.HasRows);
        Assert.AreEqual(1, reader.FieldCount); //Will Fail if uncommented expected 1 returned 5
        Assert.AreEqual(false, reader.Read());

        //Sixth ResultSet, should have 49 rows.
        //SELECT id, dt, b1 FROM Test WHERE id < ?param1;
        Assert.IsTrue(reader.NextResult());
        Assert.AreEqual(true, reader.HasRows);
        Assert.AreEqual(3, reader.FieldCount); //Will Fail if uncommented expected 3 returned 5
        for (int i = 0; i < 49; i++)
        {
          Assert.IsTrue(reader.Read());
        }
        Assert.AreEqual(false, reader.Read());

        //Seventh ResultSet, should have 51 rows.
        //SELECT b1 FROM Test WHERE id >= ?param1;
        Assert.IsTrue(reader.NextResult());
        Assert.AreEqual(true, reader.HasRows);
        Assert.AreEqual(1, reader.FieldCount); //Will Fail if uncommented expected 1 returned 5
        for (int i = 0; i < 51; i++)
        {
          Assert.IsTrue(reader.Read());
        }
        Assert.AreEqual(false, reader.Read());
      }
    }


    [Test]
    public void TestMultipleResultsWithQueryCacheOn()
    {
      CreateDefaultTable();
      execSQL("SET SESSION query_cache_type = ON");
      execSQL("INSERT INTO Test (id,name) VALUES (1, 'Test')");
      execSQL("INSERT INTO Test (id,name) VALUES (51, 'Test')");

      // execute it one time
      MySqlCommand cmd = new MySqlCommand("SELECT id FROM Test WHERE id<50; SELECT * FROM Test	WHERE id >= 50;", conn);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.IsNotNull(reader);
        Assert.AreEqual(true, reader.HasRows);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(1, reader.FieldCount);
        Assert.IsTrue(reader.NextResult());
        Assert.AreEqual(true, reader.HasRows);
        Assert.AreEqual(5, reader.FieldCount);
      }

      // now do it again
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.IsNotNull(reader);
        Assert.AreEqual(true, reader.HasRows);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(1, reader.FieldCount);
        Assert.IsTrue(reader.NextResult());
        Assert.AreEqual(true, reader.HasRows);
        Assert.AreEqual(5, reader.FieldCount);
      }
    }

    /// <summary>
    /// Bug #8630  	Executing a query with the SchemaOnly option reads the entire resultset
    /// </summary>
    [Test]
    public void SchemaOnly()
    {
      CreateDefaultTable();
      execSQL("INSERT INTO Test (id,name) VALUES(1,'test1')");
      execSQL("INSERT INTO Test (id,name) VALUES(2,'test2')");
      execSQL("INSERT INTO Test (id,name) VALUES(3,'test3')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
      {
#if RT
        Assert.AreEqual(5, reader.FieldCount);
#else
        DataTable table = reader.GetSchemaTable();
        Assert.AreEqual(5, table.Rows.Count);
        Assert.AreEqual(22, table.Columns.Count);
#endif
        Assert.IsFalse(reader.Read());
      }
    }

    /// <summary>
    /// Bug #9237  	MySqlDataReader.AffectedRecords not set to -1
    /// </summary>
    [Test]
    public void AffectedRows()
    {
      MySqlCommand cmd = new MySqlCommand("SHOW TABLES", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        reader.Close();
        Assert.AreEqual(-1, reader.RecordsAffected);
      }
    }

    /// <summary>
    /// Bug #11873  	Invalid timestamp in query produces incorrect reader exception
    /// </summary>
    [Test]
    public void InvalidTimestamp()
    {
      execSQL("CREATE TABLE Test (tm TIMESTAMP)");
      execSQL("INSERT INTO Test VALUES (NULL)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test WHERE tm = '7/1/2005 12:00:00 AM'", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
      }
    }

    /// <summary>
    /// Bug #19294 IDataRecord.GetString method should return null for null values
    /// </summary>
    [Test]
    public void GetStringOnNull()
    {
      execSQL("CREATE TABLE Test (id int, PRIMARY KEY(id))");
      MySqlCommand cmd = new MySqlCommand(
        String.Format("SHOW INDEX FROM Test FROM `{0}`", database0), conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        try
        {
          reader.GetString(reader.GetOrdinal("Sub_part"));
          Assert.Fail("We should not get here");
        }
#if RT
        catch (MySqlNullValueException)
        {
        }
#else
        catch (System.Data.SqlTypes.SqlNullValueException)
        {
        }
#endif
      }
    }

#if !RT
    /// <summary>
    /// Bug #23538 Exception thrown when GetSchemaTable is called and "fields" is null. 
    /// </summary>
    [Test]
    public void GetSchemaTableOnEmptyResultset()
    {
      if (Version < new Version(5, 0)) return;

      execSQL("CREATE PROCEDURE spTest() BEGIN END");

      MySqlCommand cmd = new MySqlCommand("spTest", conn);
      cmd.CommandType = CommandType.StoredProcedure;
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        DataTable dt = reader.GetSchemaTable();
        Assert.IsNull(dt);
      }
    }
#endif

    /// <summary>
    /// Bug #24765  	Retrieving empty fields results in check for isDBNull
    /// </summary>
    [Test]
    public void IsDbNullOnNonNullFields()
    {
      CreateDefaultTable();
      execSQL("INSERT INTO Test (id, name) VALUES (1, '')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.IsTrue(reader.Read());
        Assert.IsFalse(reader.IsDBNull(1));
      }
    }

#if !RT
    /// <summary>
    /// Bug #30204  	Incorrect ConstraintException
    /// </summary>
    [Test]
    public void ConstraintWithLoadingReader()
    {
      execSQL(@"CREATE TABLE Test (ID_A int(11) NOT NULL,
				ID_B int(11) NOT NULL, PRIMARY KEY (ID_A,ID_B)
				) ENGINE=MyISAM DEFAULT CHARSET=latin1;");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
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

    [Test]
    public void CloseConnectionBehavior2()
    {
      CreateDefaultTable();
      execSQL("INSERT INTO Test(id,name) VALUES(1,'test')");

      using (MySqlConnection c2 = new MySqlConnection(conn.ConnectionString))
      {
        c2.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c2);
        using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
        {
          Assert.IsTrue(reader.Read());
        }
      }
    }

    /// <summary>
    /// Test that using command behavior SchemaOnly does not hose the connection
    /// by leaving SQL_SELECT_LIMIT set to 0 after the error (and in normal
    /// case too)
    /// 
    /// Bug#30518
    /// </summary>
    [Test]
    public void CommandBehaviorSchemaOnly()
    {

      MySqlCommand cmd = new MySqlCommand("select * from doesnotexist", conn);
      MySqlDataReader reader;
      try
      {
        reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
        Assert.Fail("should have failed");
      }
      catch (MySqlException)
      {
      }

      // Check that failed ExecuteReader did not leave SQL_SELECT_LIMIT
      // set to 0.
      cmd.CommandText = "select now()";
      reader = cmd.ExecuteReader();
      Assert.IsTrue(reader.Read());
      reader.Close();


      // Check that CommandBehavior.SchemaOnly does not return rows
      reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
      Assert.IsFalse(reader.Read());
      reader.Close();


      reader = cmd.ExecuteReader();
      // Check that prior setting of CommandBehavior did not 
      // leave SQL_SELECT_LIMIT set to 0
      Assert.IsTrue(reader.Read());
      reader.Close();
    }

    /// <summary>
    /// Bug #37239 MySqlReader GetOrdinal performance changes break existing functionality
    /// </summary>
    [Test]
    public void ColumnsWithSameName()
    {
      CreateDefaultTable();
      execSQL("INSERT INTO Test (id, name) VALUES (1, 'test')");

      MySqlCommand cmd = new MySqlCommand("SELECT a.name, a.name FROM Test a", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string name1 = reader.GetString(0);
        string name2 = reader.GetString(1);
        Assert.AreEqual(name1, name2);
        Assert.AreEqual(name1, "test");
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
    [Test]
    public void Bug47467()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT 1 as c1", conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Type t = reader.GetFieldType("c1");

        try
        {
          reader.GetOrdinal("nocol");
          Assert.Fail("This should have failed");
        }
        catch (IndexOutOfRangeException ex)
        {
          Assert.IsTrue(ex.Message.IndexOf("nocol") != -1);
        }
      }
    }
  }
}
