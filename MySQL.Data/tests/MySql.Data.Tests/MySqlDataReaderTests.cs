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

using MySql.Data.Common;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Text;

namespace MySql.Data.MySqlClient.Tests
{
  public partial class MySqlDataReaderTests : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    private void CreateDefaultTable()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), d DATE, dt DATETIME, b1 LONGBLOB, PRIMARY KEY(id))");
    }

    [Test]
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
        Assert.That(reader, Is.Not.Null);
        Assert.That(reader.HasRows);
        Assert.That(reader.Read());
        Assert.That(reader.FieldCount, Is.EqualTo(1));
        Assert.That(reader.NextResult());
        Assert.That(reader.HasRows);
        Assert.That(reader.FieldCount, Is.EqualTo(5));
      }

      // now do it again
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.That(reader, Is.Not.Null);
        Assert.That(reader.HasRows);
        Assert.That(reader.Read());
        Assert.That(reader.FieldCount, Is.EqualTo(1));
        Assert.That(reader.NextResult());
        Assert.That(reader.HasRows);
        Assert.That(reader.FieldCount, Is.EqualTo(5));
      }
    }

    [Test]
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
        Assert.That(sizeBytes, Is.EqualTo(len));

        byte[] buff1 = new byte[len / 2];
        byte[] buff2 = new byte[len - (len / 2)];
        long buff1cnt = reader.GetBytes(4, 0, buff1, 0, len / 2);
        long buff2cnt = reader.GetBytes(4, buff1cnt, buff2, 0, buff2.Length);
        Assert.That(buff1cnt, Is.EqualTo(buff1.Length));
        Assert.That(buff2cnt, Is.EqualTo(buff2.Length));

        for (int i = 0; i < buff1.Length; i++)
          Assert.That(buff1[i], Is.EqualTo(bytes[i]));

        for (int i = 0; i < buff2.Length; i++)
          Assert.That(buff2[i], Is.EqualTo(bytes[buff1.Length + i]));
      }

      //  now check with sequential access
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
      {
        Assert.That(reader.Read());
        int mylen = len;
        byte[] buff = new byte[8192];
        int startIndex = 0;
        while (mylen > 0)
        {
          int readLen = Math.Min(mylen, buff.Length);
          int retVal = (int)reader.GetBytes(4, startIndex, buff, 0, readLen);
          Assert.That(retVal, Is.EqualTo(readLen));
          for (int i = 0; i < readLen; i++)
            Assert.That(buff[i], Is.EqualTo(bytes[startIndex + i]));
          startIndex += readLen;
          mylen -= readLen;
        }
      }
    }

    [Test]
    public void TestSingleResultSetBehavior()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test (id, name, b1) VALUES (1, 'Test1', NULL)");
      ExecuteSQL("INSERT INTO Test (id, name, b1) VALUES (2, 'Test1', NULL)");

      MySqlCommand cmd = new MySqlCommand(
        "SELECT * FROM Test WHERE id=1; SELECT * FROM Test WHERE id=2", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleResult))
      {
        bool result = reader.Read();
        Assert.That(result);

        result = reader.NextResult();
        Assert.That(result, Is.False);
      }
    }

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

      ExecuteSQL(sql);
      ExecuteSQL("INSERT INTO test2 VALUES(1,'Test', 'Test', 1.0, now(), 20.0, 12.324, True)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM test2", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        DataTable dt = reader.GetSchemaTable();
        Assert.That(true == (Boolean)dt.Rows[0]["IsAutoIncrement"], "Checking auto increment");
        Assert.That((bool)dt.Rows[0]["IsUnique"], Is.False, "Checking IsUnique");
        Assert.That((bool)dt.Rows[0]["IsKey"]);
        Assert.That(false == (Boolean)dt.Rows[0]["AllowDBNull"], "Checking AllowDBNull");
        Assert.That(false == (Boolean)dt.Rows[1]["AllowDBNull"], "Checking AllowDBNull");
        Assert.That(dt.Rows[1]["ColumnSize"], Is.EqualTo(255));
        Assert.That(dt.Rows[2]["ColumnSize"], Is.EqualTo(40));

        // udec column
        Assert.That(dt.Rows[5]["ColumnSize"], Is.EqualTo(21));
        Assert.That(dt.Rows[5]["NumericPrecision"], Is.EqualTo(20));
        Assert.That(dt.Rows[5]["NumericScale"], Is.EqualTo(6));

        // dec column
        Assert.That(dt.Rows[6]["ColumnSize"], Is.EqualTo(46));
        Assert.That(dt.Rows[6]["NumericPrecision"], Is.EqualTo(44));
        Assert.That(dt.Rows[6]["NumericScale"], Is.EqualTo(3));
      }
    }

    [Test]
    public void CloseConnectionBehavior()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test(id,name) VALUES(1,'test')");

      using (MySqlConnection c2 = new MySqlConnection(Connection.ConnectionString))
      {
        c2.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c2);
        using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
        {
          Assert.That(reader.Read());
          reader.Close();
          Assert.That(c2.State == ConnectionState.Closed);
        }
      }
    }

    [Test]
    public void SingleRowBehavior()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");
      ExecuteSQL("INSERT INTO Test(id,name) VALUES(2,'test2')");
      ExecuteSQL("INSERT INTO Test(id,name) VALUES(3,'test3')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.That(reader.Read(), "First read");
        Assert.That(reader.Read(), Is.False, "Second read");
        Assert.That(reader.NextResult(), Is.False, "Trying NextResult");
      }

      cmd.CommandText = "SELECT * FROM Test where id=1";
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.That(reader.Read());
        Assert.That(reader.GetString(1), Is.EqualTo("test1"));
        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.NextResult(), Is.False);
      }
    }

    [Test]
    public void SingleRowBehaviorWithLimit()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");
      ExecuteSQL("INSERT INTO Test(id,name) VALUES(2,'test2')");
      ExecuteSQL("INSERT INTO Test(id,name) VALUES(3,'test3')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test LIMIT 2", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.That(reader.Read(), "First read");
        Assert.That(reader.Read(), Is.False, "Second read");
        Assert.That(reader.NextResult(), Is.False, "Trying NextResult");
      }

      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.That(reader.Read(), "First read");
        Assert.That(reader.Read(), Is.False, "Second read");
        Assert.That(reader.NextResult(), Is.False, "Trying NextResult");
      }

      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
      {
        Assert.That(reader.Read(), "First read");
        Assert.That(reader.Read(), Is.False, "Second read");
        Assert.That(reader.NextResult(), Is.False, "Trying NextResult");
      }
    }

    [Test]
    public void SimpleSingleRow()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.That(reader.Read(), "First read");
        Assert.That(reader.GetInt32(0), Is.EqualTo(1));
        Assert.That(reader.GetString(1), Is.EqualTo("test1"));
        Assert.That(reader.Read(), Is.False, "Second read");
        Assert.That(reader.NextResult(), Is.False, "Trying NextResult");
      }
    }

    [Test]
    public void ConsecutiveNulls()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test (id, name, dt) VALUES (1, 'Test', NULL)");
      ExecuteSQL("INSERT INTO Test (id, name, dt) VALUES (2, NULL, now())");
      ExecuteSQL("INSERT INTO Test (id, name, dt) VALUES (3, 'Test2', NULL)");

      MySqlCommand cmd = new MySqlCommand("SELECT id, name, dt FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.That(reader.GetValue(0), Is.EqualTo(1));
        Assert.That(reader.GetValue(1), Is.EqualTo("Test"));
        Assert.That(reader.GetString(1), Is.EqualTo("Test"));
        Assert.That(reader.GetValue(2), Is.EqualTo(DBNull.Value));
        reader.Read();
        Assert.That(reader.GetValue(0), Is.EqualTo(2));
        Assert.That(reader.GetValue(1), Is.EqualTo(DBNull.Value));
        Exception ex = Assert.Throws<SqlNullValueException>(() => reader.GetString(1));
        Assert.That(ex.Message, Is.EqualTo("Data is Null. This method or property cannot be called on Null values."));
        Assert.That(reader.IsDBNull(2), Is.False);
        reader.Read();
        Assert.That(reader.GetValue(0), Is.EqualTo(3));
        Assert.That(reader.GetValue(1), Is.EqualTo("Test2"));
        Assert.That(reader.GetString(1), Is.EqualTo("Test2"));
        Assert.That(reader.GetValue(2), Is.EqualTo(DBNull.Value));
        ex = Assert.Throws<SqlNullValueException>(() => reader.GetMySqlDateTime(2));
        Assert.That(ex.Message, Is.EqualTo("Data is Null. This method or property cannot be called on Null values."));
        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.NextResult(), Is.False);
      }
    }

    [Test]
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
    [Test]
    public void SequentialAccessBehavior()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test(id,name) VALUES(1,'test1')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
      {
        Assert.That(reader.Read());
        Assert.That(reader.IsDBNull(0), Is.False);
        int i = reader.GetInt32(0);
        string s = reader.GetString(1);
        Assert.That(i, Is.EqualTo(1));
        Assert.That(s, Is.EqualTo("test1"));

        // this next line should throw an exception
        Exception ex = Assert.Throws<MySqlException>(() => i = reader.GetInt32(0));
        Assert.That(ex.Message, Is.EqualTo("Invalid attempt to read a prior column using SequentialAccess"));
      }
    }


    [Test]
    public void ReadingTextFields()
    {
      ExecuteSQL("CREATE TABLE Test (id int, t1 TEXT)");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'Text value')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string s = reader["t1"].ToString();
        Assert.That(s, Is.EqualTo("Text value"));
      }
    }

    [Test]
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

    [Test]
    public void GetChar()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test (id, name) VALUES (1, 'a')");
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        char achar = reader.GetChar(1);
        Assert.That(achar, Is.EqualTo('a'));
      }
    }

    [Test]
    public void ReaderOnNonQuery()
    {
      CreateDefaultTable();
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id,name) VALUES (1,'Test')", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.That(reader.Read(), Is.False);
        reader.Close();

        cmd.CommandText = "SELECT name FROM Test";
        object v = cmd.ExecuteScalar();
        Assert.That(v, Is.EqualTo("Test"));
      }
    }

    [Test]
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
        Assert.That(reader.HasRows);
        Assert.That(reader.FieldCount, Is.EqualTo(1));
        for (int i = 0; i < 49; i++)
        {
          Assert.That(reader.Read());
        }
        Assert.That(reader.Read(), Is.False);

        //Second ResultSet, should have no rows.
        //SELECT id,name FROM Test WHERE id = -50;
        Assert.That(reader.NextResult());
        Assert.That(reader.HasRows, Is.False);
        Assert.That(reader.FieldCount, Is.EqualTo(2));
        Assert.That(reader.Read(), Is.False);


        //Third ResultSet, should have 51 rows.
        //SELECT * FROM Test WHERE id >= ?param1;
        Assert.That(reader.NextResult());
        Assert.That(reader.HasRows);
        Assert.That(reader.FieldCount, Is.EqualTo(5));
        for (int i = 0; i < 51; i++)
        {
          Assert.That(reader.Read());
        }
        Assert.That(reader.Read(), Is.False);


        //Fourth ResultSet, should have no rows.
        //SELECT id, dt, b1 FROM Test WHERE id = -50;
        Assert.That(reader.NextResult());
        Assert.That(reader.HasRows, Is.False);
        Assert.That(reader.FieldCount, Is.EqualTo(3)); //Will Fail if uncommented expected 3 returned 5
        Assert.That(reader.Read(), Is.False);

        //Fifth ResultSet, should have no rows.
        //SELECT b1 FROM Test WHERE id = -50;
        Assert.That(reader.NextResult());
        Assert.That(reader.HasRows, Is.False);
        Assert.That(reader.FieldCount, Is.EqualTo(1)); //Will Fail if uncommented expected 1 returned 5
        Assert.That(reader.Read(), Is.False);

        //Sixth ResultSet, should have 49 rows.
        //SELECT id, dt, b1 FROM Test WHERE id < ?param1;
        Assert.That(reader.NextResult());
        Assert.That(reader.HasRows);
        Assert.That(reader.FieldCount, Is.EqualTo(3)); //Will Fail if uncommented expected 3 returned 5
        for (int i = 0; i < 49; i++)
        {
          Assert.That(reader.Read());
        }
        Assert.That(reader.Read(), Is.False);

        //Seventh ResultSet, should have 51 rows.
        //SELECT b1 FROM Test WHERE id >= ?param1;
        Assert.That(reader.NextResult());
        Assert.That(reader.HasRows);
        Assert.That(reader.FieldCount, Is.EqualTo(1)); //Will Fail if uncommented expected 1 returned 5
        for (int i = 0; i < 51; i++)
        {
          Assert.That(reader.Read());
        }
        Assert.That(reader.Read(), Is.False);
      }
    }


    [Test]
    public void TestMultipleResultsWithQueryCacheOn()
    {
      //query_cache_type was deprecated in server 5.7.20.
      if (Connection.driver.Version.isAtLeast(5, 7, 20)) return;

      CreateDefaultTable();
      ExecuteSQL("SET SESSION query_cache_type = ON");
      ExecuteSQL("INSERT INTO Test (id,name) VALUES (1, 'Test')");
      ExecuteSQL("INSERT INTO Test (id,name) VALUES (51, 'Test')");

      // execute it one time
      MySqlCommand cmd = new MySqlCommand("SELECT id FROM Test WHERE id<50; SELECT * FROM Test	WHERE id >= 50;", Connection);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.That(reader, Is.Not.Null);
        Assert.That(reader.HasRows);
        Assert.That(reader.Read());
        Assert.That(reader.FieldCount, Is.EqualTo(1));
        Assert.That(reader.NextResult());
        Assert.That(reader.HasRows);
        Assert.That(reader.FieldCount, Is.EqualTo(5));
      }

      // now do it again
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.That(reader, Is.Not.Null);
        Assert.That(reader.HasRows);
        Assert.That(reader.Read());
        Assert.That(reader.FieldCount, Is.EqualTo(1));
        Assert.That(reader.NextResult());
        Assert.That(reader.HasRows);
        Assert.That(reader.FieldCount, Is.EqualTo(5));
      }
    }

    /// <summary>
    /// Bug #9237  	MySqlDataReader.AffectedRecords not set to -1
    /// </summary>
    [Test]
    public void AffectedRows()
    {
      MySqlCommand cmd = new MySqlCommand("SHOW TABLES", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        reader.Close();
        Assert.That(reader.RecordsAffected, Is.EqualTo(-1));
      }
    }

    /// <summary>
    /// Bug #11873  	Invalid timestamp in query produces incorrect reader exception
    /// </summary>
    [Test]
    public void InvalidTimestamp()
    {
      ExecuteSQL("CREATE TABLE Test (tm TIMESTAMP)");
      ExecuteSQL("INSERT INTO Test VALUES (NULL)");
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test WHERE tm = '7/1/2005 12:00:00 AM'", Connection);
      MySqlDataReader reader;

      if (Connection.driver.Version.isAtLeast(8, 0, 16))
        Assert.Throws<MySqlException>(() => reader = cmd.ExecuteReader());
      else
      {
        Assert.That(reader = cmd.ExecuteReader(), Is.Empty);
        reader.Close();
      }
    }

    /// <summary>
    /// Bug #19294 IDataRecord.GetString method should return null for null values
    /// </summary>
    [Test]
    public void GetStringOnNull()
    {
      // TODO enable this test when xunit Nuget package is fixed
      // Reference: https://github.com/xunit/xunit/issues/1585
      if (Platform.IsMacOSX()) return;

      ExecuteSQL("CREATE TABLE Test (id int, PRIMARY KEY(id))");
      MySqlCommand cmd = new MySqlCommand(
      String.Format("SHOW INDEX FROM Test FROM `{0}`", Connection.Database), Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Throws<SqlNullValueException>(() => reader.GetString(reader.GetOrdinal("Sub_part")));
      }
    }

    /// <summary>
    /// Bug #23538 Exception thrown when GetSchemaTable is called and "fields" is null. 
    /// </summary>
    [Test]
    public void GetSchemaTableOnEmptyResultset()
    {
      ExecuteSQL("CREATE PROCEDURE spTest() BEGIN END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        DataTable dt = reader.GetSchemaTable();
        Assert.That(dt, Is.Null);
      }
    }

    /// <summary>
    /// Bug #24765  	Retrieving empty fields results in check for isDBNull
    /// </summary>
    [Test]
    public void IsDbNullOnNonNullFields()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test (id, name) VALUES (1, '')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.That(reader.Read());
        Assert.That(reader.IsDBNull(1), Is.False);
      }
    }

    /// <summary>
    /// Bug #30204  	Incorrect ConstraintException
    /// </summary>
    [Test]
    public void ConstraintWithLoadingReader()
    {
      ExecuteSQL(@"CREATE TABLE Test (ID_A int(11) NOT NULL,
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

    [Test]
    public void CloseConnectionBehavior2()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test(id,name) VALUES(1,'test')");

      using (MySqlConnection c2 = new MySqlConnection(Connection.ConnectionString))
      {
        c2.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c2);
        using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
        {
          Assert.That(reader.Read());
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
    [Test]
    public void CommandBehaviorSchemaOnly()
    {

      MySqlCommand cmd = new MySqlCommand("select * from doesnotexist", Connection);
      MySqlDataReader reader;
      Exception ex = Assert.Throws<MySqlException>(() => reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly));
      Assert.That(ex.Message, Does.Contain(".doesnotexist' doesn't exist"));

      // Check that failed ExecuteReader did not leave SQL_SELECT_LIMIT
      // set to 0.
      cmd.CommandText = "select now()";
      reader = cmd.ExecuteReader();
      Assert.That(reader.Read());
      reader.Close();


      // Check that CommandBehavior.SchemaOnly does not return rows
      reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
      Assert.That(reader.Read(), Is.False);
      reader.Close();


      reader = cmd.ExecuteReader();
      // Check that prior setting of CommandBehavior did not 
      // leave SQL_SELECT_LIMIT set to 0
      Assert.That(reader.Read());
      reader.Close();
    }

    /// <summary>
    /// Bug #37239 MySqlReader GetOrdinal performance changes break existing functionality
    /// </summary>
    [Test]
    public void ColumnsWithSameName()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test (id, name) VALUES (1, 'test')");

      MySqlCommand cmd = new MySqlCommand("SELECT a.name, a.name FROM Test a", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string name1 = reader.GetString(0);
        string name2 = reader.GetString(1);
        Assert.That(name2, Is.EqualTo(name1));
        Assert.That(name1, Is.EqualTo("test"));
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
      MySqlCommand cmd = new MySqlCommand("SELECT 1 as c1", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Type t = reader.GetFieldType("c1");
        Exception ex = Assert.Throws<IndexOutOfRangeException>(() => reader.GetOrdinal("nocol"));
        Assert.That(ex.Message.IndexOf("nocol") != -1);
      }
    }

    /// <summary>
    /// Bug #28980953 - MYSQLDATAREADER.GETSTREAM THROWS INDEXOUTOFRANGEEXCEPTION
    /// The implementation of GetStream method was missing, hence the exception.
    /// </summary>
    [Test]
    public void GetStream()
    {
      ExecuteSQL("CREATE TABLE Test (data BLOB)");

      string str = "randomText_12345";
      byte[] val = UTF8Encoding.UTF8.GetBytes(str);

      using (var command = new MySqlCommand(@"INSERT INTO Test VALUES(@data);", Connection))
      {
        command.Parameters.AddWithValue("@data", val);
        command.ExecuteNonQuery();
      }

      using (var command = new MySqlCommand(@"SELECT data FROM Test", Connection))
      using (var reader = command.ExecuteReader())
      {
        reader.Read();
        using (var stream = reader.GetStream(0))
        {
          string result = UTF8Encoding.UTF8.GetString(((MemoryStream)stream).ToArray());
          Assert.That(result, Is.EqualTo(str).IgnoreCase);
        }
      }
    }

    /// <summary>
    /// Bug #33781449	- MySqlDataReader.GetFieldValue<Stream> throws InvalidCastException
    /// Added the handling for Stream type.
    /// </summary>
    [Test]
    public void GetFieldValue()
    {
      ExecuteSQL(@"CREATE TABLE Test (intCol INT, decimalCol DECIMAL(5,2), textCol VARCHAR(10), dateCol DATETIME, boolCol TINYINT(1), blobCol BLOB)");

      string str = "randomText_12345";
      byte[] blob = Encoding.UTF8.GetBytes(str);
      DateTime dateTime = DateTime.Now;

      using (var command = new MySqlCommand(@"INSERT INTO Test VALUES(@int, @decimal, @text, @date, @bit, @blob);", Connection))
      {
        command.Parameters.AddWithValue("@int", 1);
        command.Parameters.AddWithValue("@decimal", 1.23);
        command.Parameters.AddWithValue("@text", "test");
        command.Parameters.AddWithValue("@date", dateTime);
        command.Parameters.AddWithValue("@bit", true);
        command.Parameters.AddWithValue("@blob", blob);
        command.ExecuteNonQuery();
      }

      using (var cmd = new MySqlCommand("SELECT * FROM Test", Connection))
      using (var reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.That(reader.GetFieldValue<int>(0), Is.EqualTo(1));
        Assert.That(reader.GetFieldValue<decimal>(1), Is.EqualTo(1.23));
        Assert.That(reader.GetFieldValue<string>(2), Is.EqualTo("test"));
        Assert.That(reader.GetFieldValue<DateTime>(3).ToShortDateString(), Is.EqualTo(dateTime.ToShortDateString()));
        Assert.That(reader.GetFieldValue<bool>(4), Is.EqualTo(true));
        Assert.That(Encoding.UTF8.GetString(((MemoryStream)reader.GetFieldValue<Stream>(5)).ToArray()), Is.EqualTo(str).IgnoreCase);
      }
    }
  }
}
