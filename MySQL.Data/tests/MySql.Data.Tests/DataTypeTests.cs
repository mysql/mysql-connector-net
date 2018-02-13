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
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Data;
using MySql.Data.Types;
using System.Data.Common;

namespace MySql.Data.MySqlClient.Tests
{
  public partial class DataTypeTests : TestBase
  {
    public DataTypeTests(TestFixture fixture) : base(fixture)
    {
    }    
  
    [Fact]
    public void BytesAndBooleans()
    {
      InternalBytesAndBooleans(false);
    }

    [Fact]
    public void BytesAndBooleansPrepared()
    {
      InternalBytesAndBooleans(true);
    }

    private void InternalBytesAndBooleans(bool prepare)
    {
      executeSQL("CREATE TABLE Test (id TINYINT, idu TINYINT UNSIGNED, i INT UNSIGNED)");
      executeSQL("INSERT INTO Test VALUES (-98, 140, 20)");
      executeSQL("INSERT INTO Test VALUES (0, 0, 0)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      if (prepare) cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.Equal(-98, (sbyte)reader.GetByte(0));
        Assert.Equal(140, reader.GetByte(1));
        Assert.True(reader.GetBoolean(1));
        Assert.Equal(20, Convert.ToInt32(reader.GetUInt32(2)));
        Assert.Equal(20, reader.GetInt32(2));

        Assert.True(reader.Read());
        Assert.Equal(0, reader.GetByte(0));
        Assert.Equal(0, reader.GetByte(1));
        Assert.False(reader.GetBoolean(1));

        Assert.False(reader.Read());
      }
    }

    /// <summary>
    /// Bug#46205 - tinyint as boolean does not work for utf8 default database character set.
    /// </summary>
    ///<remarks>
    /// Original bug occured only with mysqld started with --default-character-set=utf8.
    /// It does not seem  possible to reproduce the original buggy behavior´otherwise
    /// Neither "set global character_set_server = utf8" , nor  "create table /database with character set "
    /// were sufficient.
    ///</remarks>
    [Fact]
    public void TreatTinyAsBool()
    {
      executeSQL("CREATE TABLE Test2(i TINYINT(1))");
      executeSQL("INSERT INTO Test2 VALUES(1)");
      executeSQL("INSERT INTO Test2 VALUES(0)");
      executeSQL("INSERT INTO Test2 VALUES(2)");
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      Assert.True(builder.TreatTinyAsBoolean);

      MySqlCommand cmd = new MySqlCommand("SELECT * from Test2", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        bool b;
        Assert.True(reader.Read());
        b = (bool)reader.GetValue(0);
        Assert.True(b);
        Assert.True(reader.Read());
        b = (bool)reader.GetValue(0);
        Assert.False(b);
        Assert.True(reader.Read());
        b = (bool)reader.GetValue(0);
        Assert.True(b);
      }
    }

    [Fact]
    public void TestFloat()
    {
      InternalTestFloats(false);
    }

    [Fact]
    public void TestFloatPrepared()
    {
      InternalTestFloats(true);
    }

    private void InternalTestFloats(bool prepared)
    {
      executeSQL("CREATE TABLE Test (fl FLOAT, db DOUBLE, dec1 DECIMAL(5,2))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?fl, ?db, ?dec)", Connection);
      cmd.Parameters.Add("?fl", MySqlDbType.Float);
      cmd.Parameters.Add("?db", MySqlDbType.Double);
      cmd.Parameters.Add("?dec", MySqlDbType.Decimal);
      cmd.Parameters[0].Value = 2.3;
      cmd.Parameters[1].Value = 4.6;
      cmd.Parameters[2].Value = 23.82;
      if (prepared)
        cmd.Prepare();
      int count = cmd.ExecuteNonQuery();
      Assert.Equal(1, count);

      cmd.Parameters[0].Value = 1.5;
      cmd.Parameters[1].Value = 47.85;
      cmd.Parameters[2].Value = 123.85;
      count = cmd.ExecuteNonQuery();
      Assert.Equal(1, count);

      cmd.CommandText = "SELECT * FROM Test";
      if (prepared)
        cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.True((decimal)2.3 == (decimal)reader.GetFloat(0));
        Assert.Equal(4.6, reader.GetDouble(1));
        Assert.True((decimal)23.82 == reader.GetDecimal(2));

        Assert.True(reader.Read());
        Assert.True((decimal)1.5 == (decimal)reader.GetFloat(0));
        Assert.Equal(47.85, reader.GetDouble(1));
        Assert.True((decimal)123.85 == reader.GetDecimal(2));
      }
    }

    [Fact]
    public void TestTime()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), d DATE, dt DATETIME, tm TIME,  PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, tm) VALUES (1, '00:00')", Connection);
      cmd.ExecuteNonQuery();
      cmd.CommandText = "INSERT INTO Test (id, tm) VALUES (2, '512:45:17')";
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();

        object value = reader["tm"];
        Assert.Equal(value.GetType(), typeof(TimeSpan));
        TimeSpan ts = (TimeSpan)reader["tm"];
        Assert.Equal(0, ts.Hours);
        Assert.Equal(0, ts.Minutes);
        Assert.Equal(0, ts.Seconds);

        reader.Read();
        value = reader["tm"];
        Assert.Equal(value.GetType(), typeof(TimeSpan));
        ts = (TimeSpan)reader["tm"];
        Assert.Equal(21, ts.Days);
        Assert.Equal(8, ts.Hours);
        Assert.Equal(45, ts.Minutes);
        Assert.Equal(17, ts.Seconds);
      }
    }

    [Fact]
    public void YearType()
    {
      executeSQL("CREATE TABLE Test (yr YEAR)");
      executeSQL("INSERT INTO Test VALUES (98)");
      executeSQL("INSERT INTO Test VALUES (1990)");
      executeSQL("INSERT INTO Test VALUES (2004)");
      executeSQL("SET SQL_MODE=''");
      executeSQL("INSERT INTO Test VALUES (111111111111111111111)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.True(1998 == reader.GetUInt32(0));
        reader.Read();
        Assert.True(1990 == reader.GetUInt32(0));
        reader.Read();
        Assert.True(2004 == reader.GetUInt32(0));
        reader.Read();
        Assert.True(0 == reader.GetUInt32(0));
      }
    }

    [Fact]
    public void TypeCoercion()
    {
      MySqlParameter p = new MySqlParameter("?test", 1);
      Assert.Equal(DbType.Int32, p.DbType);
      Assert.Equal(MySqlDbType.Int32, p.MySqlDbType);

      p.DbType = DbType.Int64;
      Assert.Equal(DbType.Int64, p.DbType);
      Assert.Equal(MySqlDbType.Int64, p.MySqlDbType);

      p.MySqlDbType = MySqlDbType.Int16;
      Assert.Equal(DbType.Int16, p.DbType);
      Assert.Equal(MySqlDbType.Int16, p.MySqlDbType);
    }

    [Fact]
    public void AggregateTypesTest()
    {
      executeSQL("CREATE TABLE foo (abigint bigint, aint int)");
      executeSQL("INSERT INTO foo VALUES (1, 2)");
      executeSQL("INSERT INTO foo VALUES (2, 3)");
      executeSQL("INSERT INTO foo VALUES (3, 4)");
      executeSQL("INSERT INTO foo VALUES (3, 5)");

      // Try a normal query
      string NORMAL_QRY = "SELECT abigint, aint FROM foo WHERE abigint = {0}";
      string qry = String.Format(NORMAL_QRY, 3);
      MySqlCommand cmd = new MySqlCommand(qry, Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.Read())
        {
          reader.GetInt64(0);
          reader.GetInt32(1); // <--- aint... this succeeds
        }
      }

      cmd.CommandText = "SELECT abigint, max(aint) FROM foo GROUP BY abigint";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.Read())
        {
          reader.GetInt64(0);
          reader.GetInt64(1); // <--- max(aint)... this fails
        }
      }
    }

    [Fact]
    public void BitAndDecimal()
    {
      executeSQL("CREATE TABLE Test (bt1 BIT(2), bt4 BIT(4), bt11 BIT(11), bt23 BIT(23), bt32 BIT(32)) engine=myisam");
      executeSQL("INSERT INTO Test VALUES (2, 3, 120, 240, 1000)");
      executeSQL("INSERT INTO Test VALUES (NULL, NULL, 100, NULL, NULL)");

      string connStr = Connection.ConnectionString + ";treat tiny as boolean=false";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.Equal(2, reader.GetInt32(0));
          Assert.Equal(3, reader.GetInt32(1));
          Assert.Equal(120, reader.GetInt32(2));
          Assert.Equal(240, reader.GetInt32(3));
          Assert.Equal(1000, reader.GetInt32(4));
          Assert.True(reader.Read());
          Assert.True(reader.IsDBNull(0));
          Assert.True(reader.IsDBNull(1));
          Assert.Equal(100, reader.GetInt32(2));
          Assert.True(reader.IsDBNull(3));
          Assert.True(reader.IsDBNull(4));
        }
      }
    }

    [Fact]
    public void DecimalTests()
    {
      executeSQL("CREATE TABLE Test (val decimal(10,1))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(?dec)", Connection);
      cmd.Parameters.AddWithValue("?dec", (decimal)2.4);
      Assert.Equal(1, cmd.ExecuteNonQuery());

      cmd.Prepare();
      Assert.Equal(1, cmd.ExecuteNonQuery());

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.True(reader[0] is Decimal);
        Assert.Equal((decimal)2.4, Convert.ToDecimal(reader[0]));

        Assert.True(reader.Read());
        Assert.True(reader[0] is Decimal);
        Assert.Equal((decimal)2.4, Convert.ToDecimal(reader[0]));

        Assert.False(reader.Read());
        Assert.False(reader.NextResult());
      }
    }

    [Fact]
    public void DecimalTests2()
    {
      executeSQL("CREATE TABLE Test (val decimal(10,1))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(?dec)", Connection);
      cmd.Parameters.AddWithValue("?dec", (decimal)2.4);
      Assert.Equal(1, cmd.ExecuteNonQuery());

      cmd.Prepare();
      Assert.Equal(1, cmd.ExecuteNonQuery());

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.True(reader[0] is Decimal);
        Assert.Equal((decimal)2.4, Convert.ToDecimal(reader[0]));

        Assert.True(reader.Read());
        Assert.True(reader[0] is Decimal);
        Assert.Equal((decimal)2.4, Convert.ToDecimal(reader[0]));

        Assert.False(reader.Read());
        Assert.False(reader.NextResult());
      }
    }

    [Fact]
    public void Bit()
    {
      executeSQL("CREATE TABLE Test (bit1 BIT, bit2 BIT(5), bit3 BIT(10))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?b1, ?b2, ?b3)", Connection);
      cmd.Parameters.Add(new MySqlParameter("?b1", MySqlDbType.Bit));
      cmd.Parameters.Add(new MySqlParameter("?b2", MySqlDbType.Bit));
      cmd.Parameters.Add(new MySqlParameter("?b3", MySqlDbType.Bit));
      cmd.Prepare();
      cmd.Parameters[0].Value = 1;
      cmd.Parameters[1].Value = 2;
      cmd.Parameters[2].Value = 3;
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.Equal(1, Convert.ToInt32(reader[0]));
        Assert.Equal(2, Convert.ToInt32(reader[1]));
        Assert.Equal(3, Convert.ToInt32(reader[2]));
      }
    }

    /// <summary>
    /// Bug #25912 selecting negative time values gets wrong results 
    /// </summary>
    [Fact]
    public void TestNegativeTime()
    {
      executeSQL("CREATE TABLE Test (t time)");
      executeSQL("INSERT INTO Test SET T='-07:24:00'");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        reader.Read();
        TimeSpan ts = reader.GetTimeSpan("t");
        Assert.Equal(-7, ts.Hours);
        Assert.Equal(-24, ts.Minutes);
        Assert.Equal(0, ts.Seconds);
      }
    }

    /// <summary>
    /// Bug #25605 BINARY and VARBINARY is returned as a string 
    /// </summary>
    [Fact]
    public void BinaryAndVarBinary()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT BINARY 'something' AS BinaryData", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        byte[] buffer = new byte[2];
        long read = reader.GetBytes(0, 0, buffer, 0, 2);
        Assert.Equal('s', (char)buffer[0]);
        Assert.Equal('o', (char)buffer[1]);
        Assert.Equal(2, read);

        string s = reader.GetString(0);
        Assert.Equal("something", s);
      }
    }

    [Fact]
    public void NumericAsBinary()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT IFNULL(NULL,0) AS MyServerID", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal("BIGINT", reader.GetDataTypeName(0));
        Assert.Equal(typeof(Int64), reader.GetFieldType(0));
        Assert.Equal("System.Int64", reader.GetValue(0).GetType().FullName);
        Assert.Equal(0, Convert.ToInt32(reader.GetValue(0)));
      }
    }

    [Fact]
    public void BinaryTypes()
    {
      executeSQL(@"CREATE TABLE test (c1 VARCHAR(20), c2 VARBINARY(20),
        c3 TEXT, c4 BLOB, c5 VARCHAR(20) CHARACTER SET BINARY)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM test", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal(typeof(String), reader.GetFieldType("c1"));
        Assert.Equal(typeof(byte[]), reader.GetFieldType("c2"));
        Assert.Equal(typeof(String), reader.GetFieldType("c3"));
        Assert.Equal(typeof(byte[]), reader.GetFieldType("c4"));
        Assert.Equal(typeof(byte[]), reader.GetFieldType("c5"));
      }
    }

    [Fact]
    public void ShowColumns()
    {
      MySqlCommand cmd = new MySqlCommand(
        @"SELECT TRIM(TRAILING ' unsigned' FROM 
          TRIM(TRAILING ' zerofill' FROM COLUMN_TYPE)) AS MYSQL_TYPE, 
          IF(COLUMN_DEFAULT IS NULL, NULL, 
          IF(ASCII(COLUMN_DEFAULT) = 1 OR COLUMN_DEFAULT = '1', 1, 0))
          AS TRUE_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS
          WHERE TABLE_SCHEMA='test' AND TABLE_NAME='test'", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal(typeof(string), reader.GetFieldType(0));
        Assert.Equal(typeof(Int64), reader.GetFieldType(1));
      }
    }

    [Fact]
    public void RespectBinaryFlag()
    {
      executeSQL("CREATE TABLE Test (col1 VARBINARY(20), col2 BLOB)");

      string connStr = Connection.ConnectionString + ";respect binary flags=false";

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c);
        using (var reader = cmd.ExecuteReader())
        {
          reader.Read();
          Assert.Equal(typeof(string), reader.GetFieldType(0));
          Assert.Equal(typeof(System.Byte[]), reader.GetFieldType(1));
        }
      }
    }

    /// <summary>
    /// Bug #27959 Bool datatype is not returned as System.Boolean by MySqlDataAdapter 
    /// </summary>
    [Fact]
    public void Boolean()
    {
      executeSQL("CREATE TABLE Test (id INT, `on` BOOLEAN, v TINYINT(2))");
      executeSQL("INSERT INTO Test VALUES (1,1,1), (2,0,0)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal(typeof(Boolean), reader.GetFieldType(1));
        Assert.Equal(typeof(SByte), reader.GetFieldType(2));
        Assert.Equal(true, reader.GetBoolean(1));
        Assert.Equal(1, Convert.ToInt32(reader.GetValue(2)));

        reader.Read();
        Assert.Equal(false, reader.GetBoolean(1));
        Assert.Equal(0, Convert.ToInt32(reader.GetValue(2)));
      }
    }

    [Fact]
    public void Binary16AsGuid()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id INT, g BINARY(16), c VARBINARY(16), c1 BINARY(255))");

      string connStr = Connection.ConnectionString + ";old guids=true";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        Guid g = Guid.NewGuid();
        byte[] bytes = g.ToByteArray();

        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (1, @g, @c, @c1)", c);
        cmd.Parameters.AddWithValue("@g", bytes);
        cmd.Parameters.AddWithValue("@c", bytes);
        cmd.Parameters.AddWithValue("@c1", g.ToString());
        cmd.ExecuteNonQuery();

        MySqlCommand cmd2 = new MySqlCommand("SELECT * FROM Test", c);
        using (var reader = cmd2.ExecuteReader())
        {
          reader.Read();
          Assert.Equal(typeof(Guid), reader.GetFieldType(1));
          Assert.Equal(typeof(byte[]), reader.GetFieldType(2));
          Assert.Equal(typeof(byte[]), reader.GetFieldType(3));
          Assert.Equal(g, reader.GetGuid(1));
        }

        string s = BitConverter.ToString(bytes);

        s = s.Replace("-", "");
        string sql = String.Format("TRUNCATE TABLE Test;INSERT INTO Test VALUES(1,0x{0},NULL,NULL)", s);
        executeSQL(sql);

        cmd.CommandText = "SELECT * FROM Test";
        cmd.Parameters.Clear();
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          Guid g1 = reader.GetGuid(1);
          Assert.Equal(g, g1);
        }
      }
    }

    /// <summary>
    /// Bug #35041 'Binary(16) as GUID' - columns lose IsGuid value after a NULL value found 
    /// </summary>
    [Fact]
    public void Binary16AsGuidWithNull()
    {
      executeSQL(@"CREATE TABLE Test (id int(10) NOT NULL AUTO_INCREMENT,
            AGUID binary(16), PRIMARY KEY (id))");
      Guid g = new Guid();
      byte[] guid = g.ToByteArray();
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (NULL, @g)", Connection);
      cmd.Parameters.AddWithValue("@g", guid);
      cmd.ExecuteNonQuery();
      executeSQL("insert into Test (AGUID) values (NULL)");
      cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Bug #36313 BIT result is lost in the left outer join 
    /// </summary>
    [Fact]
    public void BitInLeftOuterJoin()
    {
      executeSQL(@"CREATE TABLE Main (Id int(10) unsigned NOT NULL AUTO_INCREMENT,
        Descr varchar(45) NOT NULL, PRIMARY KEY (`Id`)) 
        ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=latin1");
      executeSQL(@"INSERT INTO Main (Id,Descr) VALUES (1,'AAA'), (2,'BBB'), (3, 'CCC')");

      executeSQL(@"CREATE TABLE Child (Id int(10) unsigned NOT NULL AUTO_INCREMENT,
        MainId int(10) unsigned NOT NULL, Value int(10) unsigned NOT NULL,
        Enabled bit(1) NOT NULL, PRIMARY KEY (`Id`)) 
        ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=latin1");
      executeSQL(@"INSERT INTO Child (Id, MainId, Value, Enabled) VALUES (1,2,12345,0x01)");

      MySqlCommand cmd = new MySqlCommand(
        @"SELECT m.Descr, c.Value, c.Enabled FROM Main m 
        LEFT OUTER JOIN Child c ON m.Id=c.MainId ORDER BY m.Descr", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.Equal("AAA", reader.GetString(0));
        Assert.True(reader.IsDBNull(1));
        Assert.True(reader.IsDBNull(2));

        Assert.True(reader.Read());
        Assert.Equal("BBB", reader.GetString(0));
        Assert.Equal(12345, Convert.ToInt32(reader.GetValue(1)));
        Assert.Equal(1, Convert.ToInt32(reader.GetValue(2)));

        Assert.True(reader.Read());
        Assert.Equal("CCC", reader.GetString(0));
        Assert.True(reader.IsDBNull(1));
        Assert.True(reader.IsDBNull(2));

        Assert.False(reader.Read());
      }
    }

    /// <summary>
    /// Bug #36081 Get Unknown Datatype in C# .Net 
    /// </summary>
    [Fact]
    public void GeometryType()
    {
      executeSQL(@"CREATE TABLE Test (ID int(11) NOT NULL, ogc_geom geometry NOT NULL,
        PRIMARY KEY  (`ID`))");

      if (Connection.driver.Version.isAtLeast(8,0,1))
        executeSQL(@"INSERT INTO Test VALUES (1, 
          ST_GeomFromText('GeometryCollection(Point(1 1), LineString(2 2, 3 3))'))");
      else
        executeSQL(@"INSERT INTO Test VALUES (1, 
          GeomFromText('GeometryCollection(Point(1 1), LineString(2 2, 3 3))'))");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
      }
    }

    #region MySqlGeometry Tests

    [Fact]
    public void CanParseGeometryValueString()
    {
      var v = MySqlGeometry.Parse("POINT (47.37 -122.21)");
      Assert.Equal("POINT(47.37 -122.21)", v.ToString());
    }

    [Fact]
    public void CanTryParseGeometryValueString()
    {
      MySqlGeometry v = new MySqlGeometry(0, 0);
      MySqlGeometry.TryParse("POINT (47.37 -122.21)", out v);
      Assert.Equal("POINT(47.37 -122.21)", v.ToString());
    }

    [Fact]
    public void CanTryParseGeometryValueStringWithSRIDValue()
    {
      var mysqlGeometryResult = new MySqlGeometry(0, 0);
      MySqlGeometry.TryParse("SRID=101;POINT (47.37 -122.21)", out mysqlGeometryResult);
      Assert.Equal("SRID=101;POINT(47.37 -122.21)", mysqlGeometryResult.ToString());
    }


    [Fact]
    public void StoringAndRetrievingGeometry()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (v Geometry NOT NULL)");

      MySqlCommand cmd = new MySqlCommand(Connection.driver.Version.isAtLeast(8, 0, 1) ?
        "INSERT INTO Test VALUES (ST_GeomFromText(?v))":
        "INSERT INTO Test VALUES (GeomFromText(?v))"
      , Connection);
      cmd.Parameters.Add("?v", MySqlDbType.String);
      cmd.Parameters[0].Value = "POINT(47.37 -122.21)";
      cmd.ExecuteNonQuery();

      cmd.CommandText = Connection.driver.Version.isAtLeast(8, 0, 1) ?
        "SELECT ST_AsText(v) FROM Test":
        "SELECT AsText(v) FROM Test";

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        var val = reader.GetValue(0);
      }
    }

    [Fact]
    public void CanFetchGeometryAsBinary()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (v Geometry NOT NULL)");

      MySqlGeometry v = new MySqlGeometry(47.37, -122.21);

      var par = new MySqlParameter("?v", MySqlDbType.Geometry);
      par.Value = v;

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?v)", Connection);
      cmd.Parameters.Add(par);
      cmd.ExecuteNonQuery();

      cmd.CommandText = Connection.driver.Version.isAtLeast(8, 0, 1) ?
        "SELECT ST_AsBinary(v) FROM Test":
        "SELECT AsBinary(v) FROM Test";

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        var val = reader.GetValue(0) as Byte[];
        var MyGeometry = new MySqlGeometry(MySqlDbType.Geometry, val);
        Assert.Equal("POINT(47.37 -122.21)", MyGeometry.ToString());
      }
    }


    [Fact]
    public void CanSaveSridValueOnGeometry()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (v Geometry NOT NULL)");

      MySqlGeometry v = new MySqlGeometry(47.37, -122.21, 101);
      var par = new MySqlParameter("?v", MySqlDbType.Geometry);
      par.Value = v;

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?v)", Connection);
      cmd.Parameters.Add(par);
      cmd.ExecuteNonQuery();

      cmd.CommandText = Connection.driver.Version.isAtLeast(8, 0, 1) ?
        "SELECT ST_SRID(v) FROM Test":
        "SELECT SRID(v) FROM Test";

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        var val = reader.GetString(0);
        Assert.Equal("101", val);
      }
    }


    [Fact]
    public void CanFetchGeometryAsText()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (v Geometry NOT NULL)");

      MySqlGeometry v = new MySqlGeometry(47.37, -122.21);
      var par = new MySqlParameter("?v", MySqlDbType.Geometry);
      par.Value = v;

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?v)", Connection);
      cmd.Parameters.Add(par);
      cmd.ExecuteNonQuery();

      cmd.CommandText = Connection.driver.Version.isAtLeast(8, 0, 1) ?
        "SELECT ST_AsText(v) FROM Test":
        "SELECT AsText(v) FROM Test";

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        var val = reader.GetString(0);
        Assert.Equal("POINT(47.37 -122.21)", val);
      }
    }

    [Fact]
    public void CanUseReaderGetMySqlGeometry()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (v Geometry NOT NULL)");

      MySqlGeometry v = new MySqlGeometry(47.37, -122.21);
      var par = new MySqlParameter("?v", MySqlDbType.Geometry);
      par.Value = v;

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?v)", Connection);
      cmd.Parameters.Add(par);
      cmd.ExecuteNonQuery();

      // reading as binary
      cmd.CommandText = Connection.driver.Version.isAtLeast(8, 0, 1) ?
        "SELECT ST_AsBinary(v) as v FROM Test":
        "SELECT AsBinary(v) as v FROM Test";

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        var val = reader.GetMySqlGeometry(0);
        var valWithName = reader.GetMySqlGeometry("v");
        Assert.Equal("POINT(47.37 -122.21)", val.ToString());
        Assert.Equal("POINT(47.37 -122.21)", valWithName.ToString());
      }

      // reading as geometry
      cmd.CommandText = "SELECT v as v FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        var val = reader.GetMySqlGeometry(0);
        var valWithName = reader.GetMySqlGeometry("v");
        Assert.Equal("POINT(47.37 -122.21)", val.ToString());
        Assert.Equal("POINT(47.37 -122.21)", valWithName.ToString());
      }

    }

    public void CanGetToStringFromMySqlGeometry()
    {
      MySqlGeometry v = new MySqlGeometry(47.37, -122.21);
      var valToString = v.ToString();
      Assert.Equal("POINT(47.37 -122.21)", valToString);
    }

    #endregion

    /// <summary>
    /// Bug #33322 Incorrect Double/Single value saved to MySQL database using MySQL Connector for  
    /// </summary>
    [Fact]
    public void StoringAndRetrievingDouble()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (v DOUBLE(25,20) NOT NULL)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?v)", Connection);
      cmd.Parameters.Add("?v", MySqlDbType.Double);
      cmd.Parameters[0].Value = Math.PI;
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        double d = reader.GetDouble(0);
        Assert.Equal(Math.PI, d);
      }
    }

    /// <summary>
    /// Bug #40571  	Add GetSByte to the list of public methods supported by MySqlDataReader
    /// </summary>
    [Fact]
    public void SByteFromReader()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (c1 TINYINT, c2 TINYINT UNSIGNED)");
      executeSQL("INSERT INTO Test VALUES (99, 217)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal(99, reader.GetSByte(0));
        Assert.Equal(217, reader.GetByte(1));
        Assert.Equal(99, reader.GetByte(0));
      }
    }

    [Fact]
    public void NewGuidDataType()
    {
      executeSQL("CREATE TABLE Test(id INT, g BINARY(16))");

      string connStr = Connection.ConnectionString + ";old guids=true";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        Guid guid = Guid.NewGuid();
        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, @g)", c);
        cmd.Parameters.Add(new MySqlParameter("@g", MySqlDbType.Guid));
        cmd.Parameters[0].Value = guid;
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM Test";
        using (var reader = cmd.ExecuteReader())
        {
          reader.Read();
          Assert.Equal(1, reader.GetValue(0));
          Assert.Equal(guid, reader.GetGuid(1));
        }
      }
    }

    /// <summary>
    /// Bug #44507 Binary(16) considered as Guid 
    /// </summary>
    [Fact]
    public void ReadBinary16AsBinary()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id INT, guid BINARY(16))");

      string connStr = Connection.ConnectionString + ";old guids=true";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        Guid g = new Guid("32A48AC5-285A-46c6-A0D4-158E6E39729C");
        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (1, ?guid)", c);
        //MySqlParameter p = new MySqlParameter();
        //p.ParameterName = "guid";
        //p.Value = Guid.NewGuid();
        cmd.Parameters.AddWithValue("guid", Guid.NewGuid());
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM Test";
        cmd.Parameters.Clear();
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();

          object o = reader.GetValue(1);
          Assert.True(o is Guid);

          byte[] bytes = new byte[16];
          long size = reader.GetBytes(1, 0, bytes, 0, 16);
          Assert.Equal(16, size);
        }
      }
    }

    [Fact]
    public void ReadingUUIDAsGuid()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id INT, guid CHAR(36))");
      executeSQL("INSERT INTO Test VALUES (1, UUID())");

      MySqlCommand cmd = new MySqlCommand("SELECT CONCAT('A', guid) FROM Test", Connection);
      string serverGuidStr = cmd.ExecuteScalar().ToString().Substring(1);
      Guid serverGuid = new Guid(serverGuidStr);

      cmd.CommandText = "SELECT guid FROM Test";
      Guid g = (Guid)cmd.ExecuteScalar();
      Assert.Equal(serverGuid, g);
    }

    [Fact]
    public void NewGuidType()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id INT, guid CHAR(36))");

      Guid g = Guid.NewGuid();
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, @g)", Connection);
      cmd.Parameters.AddWithValue("@g", g);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT guid FROM Test";
      Guid readG = (Guid)cmd.ExecuteScalar();
      Assert.Equal(g, readG);
    }

    /// <summary>
    /// Bug #47928 Old Guids=true setting is lost after null value is
    /// encountered in a Binary(16) 
    /// </summary>
    [Fact]
    public void OldGuidsWithNull()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id INT, guid BINARY(16))");

      string connStr = Connection.ConnectionString + ";old guids=true";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (1, ?guid)", c);
        cmd.Parameters.AddWithValue("guid", Guid.NewGuid());
        cmd.ExecuteNonQuery();

        cmd.Parameters["guid"].Value = null;
        cmd.ExecuteNonQuery();
        cmd.Parameters["guid"].Value = Guid.NewGuid();
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT guid FROM Test";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          //In Bug #47928, following loop will crash after encountering
          // null value.
          while (reader.Read())
          {
            object o = reader.GetValue(0);
          }
        }
      }
    }

    /// <summary>
    /// Bug #47985	UTF-8 String Length Issue (guids etc)
    /// </summary>
    [Fact]
    public void UTF8Char12AsGuid()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id INT, name CHAR(12) CHARSET utf8)");
      executeSQL("INSERT INTO Test VALUES (1, 'Name')");

      string connStr = Connection.ConnectionString + ";charset=utf8";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          string s = reader.GetString(1);
          Assert.Equal("Name", s);
        }
      }
    }

    /// <summary>
    /// Bug #48100	Impossible to retrieve decimal value if it doesn't fit into .Net System.Decimal
    /// </summary>
    [Fact]
    public void MySqlDecimal()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL("CREATE TABLE Test (id INT, dec1 DECIMAL(36,2))");
      executeSQL("INSERT INTO Test VALUES (1, 9999999999999999999999999999999999.99)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        MySqlDecimal dec = reader.GetMySqlDecimal(1);
        string s = dec.ToString();
        Assert.Equal(9999999999999999999999999999999999.99, dec.ToDouble());
        Assert.Equal("9999999999999999999999999999999999.99", dec.ToString());
        Exception ex = Assert.Throws<OverflowException>(() => dec.Value);
        Assert.Equal(ex.Message, "Value was either too large or too small for a Decimal.");
      }
    }

    /// <summary>
    /// Bug #55644 Value was either too large or too small for a Double 
    /// </summary>
    [Fact]
    public void DoubleMinValue()
    {
      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL("CREATE TABLE test(dbl double)");
      MySqlCommand cmd = new MySqlCommand("insert into test values(?param1)");
      cmd.Connection = Connection;
      cmd.Parameters.Add(new MySqlParameter("?param1", MySqlDbType.Double));
      cmd.Parameters["?param1"].Value = Double.MinValue;
      cmd.ExecuteNonQuery();
      cmd.Parameters["?param1"].Value = Double.MaxValue;
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("SELECT * FROM test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        double d = reader.GetDouble(0);
        Assert.Equal(d, double.MinValue);
        reader.Read();
        d = reader.GetDouble(0);
        Assert.Equal(d, double.MaxValue);
      }
    }

    /// <summary>
    /// Bug #58373	ReadInteger problem
    /// </summary>
    [Fact]
    public void BigIntAutoInc()
    {
      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL("CREATE TABLE test(ID bigint unsigned AUTO_INCREMENT NOT NULL PRIMARY KEY, name VARCHAR(20))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO test VALUES (@id, 'boo')", Connection);
      ulong val = UInt64.MaxValue;
      val -= 100;
      cmd.Parameters.AddWithValue("@id", val);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO test (name) VALUES ('boo2')";
      cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Bug # 13708884 timediff function
    /// Executing a simple query that generates a time difference that has a 
    /// fractional second value throws an exception
    /// </summary>
    [Fact]
    public void Timediff()
    {
      MySqlCommand cmd = new MySqlCommand("select timediff('2 0:1:1.0', '4 1:2:3.123456')", Connection);
      var result = cmd.ExecuteScalar();
      Assert.Equal(new TimeSpan(new TimeSpan(-2, -1, -1, -2).Ticks - 1234560), result);
    }

    [Fact]
    public void CanReadJsonValue()
    {

      if (Fixture.Version < new Version(5, 7)) return;

      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL("CREATE TABLE test(Id int NOT NULL PRIMARY KEY, jsoncolumn JSON)");
    
      MySqlCommand cmd = new MySqlCommand("INSERT INTO test VALUES (@id, '[1]')", Connection);
      cmd.Parameters.AddWithValue("@id", 1);
      cmd.ExecuteNonQuery();

      string command = @"INSERT INTO test VALUES (@id, '[""a"", {""b"": [true, false]}, [10, 20]]')";
      cmd = new MySqlCommand(command, Connection);
      cmd.Parameters.AddWithValue("@id", 2);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("SELECT jsoncolumn from test where id = 2 ", Connection);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.Equal("[\"a\", {\"b\": [true, false]}, [10, 20]]", reader.GetString(0));
      }    
    }

    [Fact]
    public void CanUpdateJsonValue()
    {
      if (Fixture.Version < new Version(5, 7)) return;

      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL("CREATE TABLE test(Id int NOT NULL PRIMARY KEY, jsoncolumn JSON)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO test VALUES (@id, '[1]')", Connection);
      cmd.Parameters.AddWithValue("@id", 1);
      cmd.ExecuteNonQuery();

      string command = @"UPDATE test set jsoncolumn = '[""a"", {""b"": [true, false]}, [10, 20]]' where id = 1";
      cmd = new MySqlCommand(command, Connection);      
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("SELECT jsoncolumn from test where id = 1 ", Connection);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.Equal("[\"a\", {\"b\": [true, false]}, [10, 20]]", reader.GetString(0));
      }
    }

    /// Testing out Generated Columns
    /// Using a case sensitive collation on a column
    /// and an insensitive serch with a generated column
    /// WL #411 
    ///
    [Fact]
    public void CanUseGeneratedColumns()
    {
      if (Fixture.Version < new Version(5, 7)) return;

      executeSQL("DROP TABLE IF EXISTS test");
      executeSQL("CREATE TABLE `test` (`ID` int NOT NULL AUTO_INCREMENT PRIMARY KEY, `Name` char(35) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO test (Name) VALUES ('Berlin')", Connection);
      cmd.ExecuteNonQuery();
      cmd = new MySqlCommand("INSERT INTO test (Name) VALUES ('London')", Connection);
      cmd.ExecuteNonQuery();
      cmd = new MySqlCommand("INSERT INTO test (Name) VALUES ('France')", Connection);
      cmd.ExecuteNonQuery();
      cmd = new MySqlCommand("INSERT INTO test (Name) VALUES ('United Kingdom')", Connection);
      cmd.ExecuteNonQuery();
      cmd = new MySqlCommand("INSERT INTO test (Name) VALUES ('Italy')", Connection);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("ALTER TABLE test ADD COLUMN Name_ci char(35) CHARACTER SET utf8 AS (Name) STORED;", Connection);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("ALTER TABLE test ADD INDEX (Name_ci);", Connection);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("SELECT Name FROM test WHERE Name_ci='berlin'", Connection);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.True(reader.GetString(0).Equals("Berlin", StringComparison.CurrentCulture));
      }    
    }
  }
}
