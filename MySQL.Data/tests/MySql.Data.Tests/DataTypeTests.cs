// Copyright (c) 2013, 2022, Oracle and/or its affiliates.
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

using MySql.Data.Types;
using NUnit.Framework;
using System;
using System.Data;
using System.Text;

namespace MySql.Data.MySqlClient.Tests
{
  public partial class DataTypeTests : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.datatypes", Connection.Database));
    }

    [Test]
    public void BytesAndBooleans()
    {
      InternalBytesAndBooleans(false);
    }

    [Test]
    public void BytesAndBooleansPrepared()
    {
      InternalBytesAndBooleans(true);
    }

    private void InternalBytesAndBooleans(bool prepare)
    {
      ExecuteSQL("CREATE TABLE Test (id TINYINT, idu TINYINT UNSIGNED, i INT UNSIGNED)");
      ExecuteSQL("INSERT INTO Test VALUES (-98, 140, 20)");
      ExecuteSQL("INSERT INTO Test VALUES (0, 0, 0)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      if (prepare) cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.Throws<OverflowException>(() => reader.GetByte(0));
        Assert.AreEqual(140, reader.GetByte(1));
        Assert.True(reader.GetBoolean(1));
        Assert.AreEqual(20, Convert.ToInt32(reader.GetUInt32(2)));
        Assert.AreEqual(20, reader.GetInt32(2));

        Assert.True(reader.Read());
        Assert.AreEqual(0, reader.GetByte(0));
        Assert.AreEqual(0, reader.GetByte(1));
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
    [Test]
    public void TreatTinyAsBool()
    {
      ExecuteSQL("CREATE TABLE Test2(i TINYINT(1))");
      ExecuteSQL("INSERT INTO Test2 VALUES(1)");
      ExecuteSQL("INSERT INTO Test2 VALUES(0)");
      ExecuteSQL("INSERT INTO Test2 VALUES(2)");
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

    [Test]
    public void TestFloat()
    {
      InternalTestFloats(false);
    }

    [Test]
    public void TestFloatPrepared()
    {
      InternalTestFloats(true);
    }

    private void InternalTestFloats(bool prepared)
    {
      ExecuteSQL("CREATE TABLE Test (fl FLOAT, db DOUBLE, dec1 DECIMAL(5,2))");

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
      Assert.AreEqual(1, count);

      cmd.Parameters[0].Value = 1.5;
      cmd.Parameters[1].Value = 47.85;
      cmd.Parameters[2].Value = 123.85;
      count = cmd.ExecuteNonQuery();
      Assert.AreEqual(1, count);

      cmd.CommandText = "SELECT * FROM Test";
      if (prepared)
        cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.True((decimal)2.3 == (decimal)reader.GetFloat(0));
        Assert.AreEqual(4.6, reader.GetDouble(1));
        Assert.True((decimal)23.82 == reader.GetDecimal(2));

        Assert.True(reader.Read());
        Assert.True((decimal)1.5 == (decimal)reader.GetFloat(0));
        Assert.AreEqual(47.85, reader.GetDouble(1));
        Assert.True((decimal)123.85 == reader.GetDecimal(2));
      }
    }

    [Test]
    public void TestTime()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), d DATE, dt DATETIME, tm TIME,  PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, tm) VALUES (1, '00:00')", Connection);
      cmd.ExecuteNonQuery();
      cmd.CommandText = "INSERT INTO Test (id, tm) VALUES (2, '512:45:17')";
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();

        object value = reader["tm"];
        Assert.AreEqual(typeof(TimeSpan), value.GetType());
        TimeSpan ts = (TimeSpan)reader["tm"];
        Assert.AreEqual(0, ts.Hours);
        Assert.AreEqual(0, ts.Minutes);
        Assert.AreEqual(0, ts.Seconds);

        reader.Read();
        value = reader["tm"];
        Assert.AreEqual(typeof(TimeSpan), value.GetType());
        ts = (TimeSpan)reader["tm"];
        Assert.AreEqual(21, ts.Days);
        Assert.AreEqual(8, ts.Hours);
        Assert.AreEqual(45, ts.Minutes);
        Assert.AreEqual(17, ts.Seconds);
      }
    }

    [Test]
    public void YearType()
    {
      ExecuteSQL("CREATE TABLE Test (yr YEAR)");
      ExecuteSQL("INSERT INTO Test VALUES (98)");
      ExecuteSQL("INSERT INTO Test VALUES (1990)");
      ExecuteSQL("INSERT INTO Test VALUES (2004)");
      ExecuteSQL("SET SQL_MODE=''");
      ExecuteSQL("INSERT INTO Test VALUES (111111111111111111111)");

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

    [Test]
    public void TypeCoercion()
    {
      MySqlParameter p = new MySqlParameter("?test", 1);
      Assert.AreEqual(DbType.Int32, p.DbType);
      Assert.AreEqual(MySqlDbType.Int32, p.MySqlDbType);

      p.DbType = DbType.Int64;
      Assert.AreEqual(DbType.Int64, p.DbType);
      Assert.AreEqual(MySqlDbType.Int64, p.MySqlDbType);

      p.MySqlDbType = MySqlDbType.Int16;
      Assert.AreEqual(DbType.Int16, p.DbType);
      Assert.AreEqual(MySqlDbType.Int16, p.MySqlDbType);
    }

    [Test]
    public void AggregateTypesTest()
    {
      ExecuteSQL("CREATE TABLE foo (abigint bigint, aint int)");
      ExecuteSQL("INSERT INTO foo VALUES (1, 2)");
      ExecuteSQL("INSERT INTO foo VALUES (2, 3)");
      ExecuteSQL("INSERT INTO foo VALUES (3, 4)");
      ExecuteSQL("INSERT INTO foo VALUES (3, 5)");

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

    [Test]
    public void BitAndDecimal()
    {
      ExecuteSQL("CREATE TABLE Test (bt1 BIT(2), bt4 BIT(4), bt11 BIT(11), bt23 BIT(23), bt32 BIT(32)) engine=myisam");
      ExecuteSQL("INSERT INTO Test VALUES (2, 3, 120, 240, 1000)");
      ExecuteSQL("INSERT INTO Test VALUES (NULL, NULL, 100, NULL, NULL)");

      string connStr = Connection.ConnectionString + ";treat tiny as boolean=false";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.AreEqual(2, reader.GetInt32(0));
          Assert.AreEqual(3, reader.GetInt32(1));
          Assert.AreEqual(120, reader.GetInt32(2));
          Assert.AreEqual(240, reader.GetInt32(3));
          Assert.AreEqual(1000, reader.GetInt32(4));
          Assert.True(reader.Read());
          Assert.True(reader.IsDBNull(0));
          Assert.True(reader.IsDBNull(1));
          Assert.AreEqual(100, reader.GetInt32(2));
          Assert.True(reader.IsDBNull(3));
          Assert.True(reader.IsDBNull(4));
        }
      }
    }

    [Test]
    public void DecimalTests()
    {
      ExecuteSQL("CREATE TABLE Test (val decimal(10,1))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(?dec)", Connection);
      cmd.Parameters.AddWithValue("?dec", (decimal)2.4);
      Assert.AreEqual(1, cmd.ExecuteNonQuery());

      cmd.Prepare();
      Assert.AreEqual(1, cmd.ExecuteNonQuery());

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.True(reader[0] is Decimal);
        Assert.AreEqual((decimal)2.4, Convert.ToDecimal(reader[0]));

        Assert.True(reader.Read());
        Assert.True(reader[0] is Decimal);
        Assert.AreEqual((decimal)2.4, Convert.ToDecimal(reader[0]));

        Assert.False(reader.Read());
        Assert.False(reader.NextResult());
      }
    }

    [Test]
    public void DecimalTests2()
    {
      ExecuteSQL("CREATE TABLE Test (val decimal(10,1))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(?dec)", Connection);
      cmd.Parameters.AddWithValue("?dec", (decimal)2.4);
      Assert.AreEqual(1, cmd.ExecuteNonQuery());

      cmd.Prepare();
      Assert.AreEqual(1, cmd.ExecuteNonQuery());

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.True(reader[0] is Decimal);
        Assert.AreEqual((decimal)2.4, Convert.ToDecimal(reader[0]));

        Assert.True(reader.Read());
        Assert.True(reader[0] is Decimal);
        Assert.AreEqual((decimal)2.4, Convert.ToDecimal(reader[0]));

        Assert.False(reader.Read());
        Assert.False(reader.NextResult());
      }
    }

    [Test]
    public void Bit()
    {
      ExecuteSQL("CREATE TABLE Test (bit1 BIT, bit2 BIT(5), bit3 BIT(10))");

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
        Assert.AreEqual(1, Convert.ToInt32(reader[0]));
        Assert.AreEqual(2, Convert.ToInt32(reader[1]));
        Assert.AreEqual(3, Convert.ToInt32(reader[2]));
      }
    }

    /// <summary>
    /// Bug #29959095 incorrect integer value using prepared statement with MySqlDbType.INT24
    /// </summary>
    [TestCase(true, 1234567)]
    [TestCase(false, 1234567)]
    [TestCase(true, -1)]
    public void UsingInt24InPreparedStatement(bool prepare, int value)
    {
        ExecuteSQL("CREATE TABLE Test(data MEDIUMINT)");
        using (var command = new MySqlCommand(@"INSERT INTO Test(data) VALUES(@data);", Connection))
        {
          command.Parameters.AddWithValue("@data", value).MySqlDbType = MySqlDbType.Int24;
          if (prepare) command.Prepare();
          var rowsAffected = command.ExecuteNonQuery();
          Assert.AreEqual(1, rowsAffected);
          command.CommandText = "SELECT data FROM Test";
          var data = command.ExecuteScalar();
          Assert.AreEqual(value, data);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void OverflowInt24InPreparedStatement(bool prepare)
    {
      void insertInt24()
      {
        ExecuteSQL("CREATE TABLE Test(data MEDIUMINT)");
        using (var command = new MySqlCommand(@"INSERT INTO Test(data) VALUES(@data);", Connection))
        {
          command.Parameters.AddWithValue("@data", 12345678910).MySqlDbType = MySqlDbType.Int24;
          if (prepare) command.Prepare();
          command.ExecuteNonQuery();
        }
      }
      Assert.Catch(insertInt24);
    }

    /// <summary>
    /// Bug #25912 selecting negative time values gets wrong results 
    /// </summary>
    [Test]
    public void TestNegativeTime()
    {
      ExecuteSQL("CREATE TABLE Test (t time)");
      ExecuteSQL("INSERT INTO Test SET T='-07:24:00'");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        reader.Read();
        TimeSpan ts = reader.GetTimeSpan("t");
        Assert.AreEqual(-7, ts.Hours);
        Assert.AreEqual(-24, ts.Minutes);
        Assert.AreEqual(0, ts.Seconds);
      }
    }

    /// <summary>
    /// Bug #25605 BINARY and VARBINARY is returned as a string 
    /// </summary>
    [Test]
    public void BinaryAndVarBinary()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT BINARY 'something' AS BinaryData", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        byte[] buffer = new byte[2];
        long read = reader.GetBytes(0, 0, buffer, 0, 2);
        Assert.AreEqual('s', (char)buffer[0]);
        Assert.AreEqual('o', (char)buffer[1]);
        Assert.AreEqual(2, read);

      }
    }

    [Test]
    public void NumericAsBinary()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT IFNULL(NULL,0) AS MyServerID", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual("BIGINT", reader.GetDataTypeName(0));
        Assert.AreEqual(typeof(Int64), reader.GetFieldType(0));
        Assert.AreEqual("System.Int64", reader.GetValue(0).GetType().FullName);
        Assert.AreEqual(0, Convert.ToInt32(reader.GetValue(0)));
      }
    }

    [Test]
    public void BinaryTypes()
    {
      ExecuteSQL(@"CREATE TABLE Test (c1 VARCHAR(20), c2 VARBINARY(20),
        c3 TEXT, c4 BLOB, c5 VARCHAR(20) CHARACTER SET BINARY)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual(typeof(String), reader.GetFieldType("c1"));
        Assert.AreEqual(typeof(byte[]), reader.GetFieldType("c2"));
        Assert.AreEqual(typeof(String), reader.GetFieldType("c3"));
        Assert.AreEqual(typeof(byte[]), reader.GetFieldType("c4"));
        Assert.AreEqual(typeof(byte[]), reader.GetFieldType("c5"));
      }
    }

    [Test]
    public void ShowColumns()
    {
      MySqlCommand cmd = new MySqlCommand(
        @"SELECT TRIM(TRAILING ' unsigned' FROM 
          TRIM(TRAILING ' zerofill' FROM COLUMN_TYPE)) AS MYSQL_TYPE, 
          IF(COLUMN_DEFAULT IS NULL, NULL, 
          IF(ASCII(COLUMN_DEFAULT) = 1 OR COLUMN_DEFAULT = '1', 1, 0))
          AS TRUE_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS
          WHERE TABLE_SCHEMA='Test' AND TABLE_NAME='Test'", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual(typeof(string), reader.GetFieldType(0));
        Assert.AreEqual(typeof(Int64), reader.GetFieldType(1));
      }
    }

    [Test]
    public void RespectBinaryFlag()
    {
      ExecuteSQL("CREATE TABLE Test (col1 VARBINARY(20), col2 BLOB)");

      string connStr = Connection.ConnectionString + ";respect binary flags=false";

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c);
        using (var reader = cmd.ExecuteReader())
        {
          reader.Read();
          Assert.AreEqual(typeof(string), reader.GetFieldType(0));
          Assert.AreEqual(typeof(System.Byte[]), reader.GetFieldType(1));
        }
      }
    }

    /// <summary>
    /// Bug #27959 Bool datatype is not returned as System.Boolean by MySqlDataAdapter 
    /// </summary>
    [Test]
    public void Boolean()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, `on` BOOLEAN, v TINYINT(2))");
      ExecuteSQL("INSERT INTO Test VALUES (1,1,1), (2,0,0)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual(typeof(Boolean), reader.GetFieldType(1));
        Assert.AreEqual(typeof(SByte), reader.GetFieldType(2));
        Assert.True(reader.GetBoolean(1));
        Assert.AreEqual(1, Convert.ToInt32(reader.GetValue(2)));

        reader.Read();
        Assert.False(reader.GetBoolean(1));
        Assert.AreEqual(0, Convert.ToInt32(reader.GetValue(2)));
      }
    }

    [Test]
    public void Binary16AsGuid()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, g BINARY(16), c VARBINARY(16), c1 BINARY(255))");

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
          Assert.AreEqual(typeof(Guid), reader.GetFieldType(1));
          Assert.AreEqual(typeof(byte[]), reader.GetFieldType(2));
          Assert.AreEqual(typeof(byte[]), reader.GetFieldType(3));
          Assert.AreEqual(g, reader.GetGuid(1));
        }

        string s = BitConverter.ToString(bytes);

        s = s.Replace("-", "");
        string sql = String.Format("TRUNCATE TABLE Test;INSERT INTO Test VALUES(1,0x{0},NULL,NULL)", s);
        ExecuteSQL(sql);

        cmd.CommandText = "SELECT * FROM Test";
        cmd.Parameters.Clear();
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          Guid g1 = reader.GetGuid(1);
          Assert.AreEqual(g, g1);
        }
      }
    }

    /// <summary>
    /// Bug #35041 'Binary(16) as GUID' - columns lose IsGuid value after a NULL value found 
    /// </summary>
    [Test]
    public void Binary16AsGuidWithNull()
    {
      ExecuteSQL(@"CREATE TABLE Test (id int(10) NOT NULL AUTO_INCREMENT,
            AGUID binary(16), PRIMARY KEY (id))");
      Guid g = new Guid();
      byte[] guid = g.ToByteArray();
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (NULL, @g)", Connection);
      cmd.Parameters.AddWithValue("@g", guid);
      cmd.ExecuteNonQuery();
      ExecuteSQL("insert into Test (AGUID) values (NULL)");
      cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Bug #36313 BIT result is lost in the left outer join 
    /// </summary>
    [Test]
    public void BitInLeftOuterJoin()
    {
      ExecuteSQL(@"CREATE TABLE Main (Id int(10) unsigned NOT NULL AUTO_INCREMENT,
        Descr varchar(45) NOT NULL, PRIMARY KEY (`Id`)) 
        ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=latin1");
      ExecuteSQL(@"INSERT INTO Main (Id,Descr) VALUES (1,'AAA'), (2,'BBB'), (3, 'CCC')");

      ExecuteSQL(@"CREATE TABLE Child (Id int(10) unsigned NOT NULL AUTO_INCREMENT,
        MainId int(10) unsigned NOT NULL, Value int(10) unsigned NOT NULL,
        Enabled bit(1) NOT NULL, PRIMARY KEY (`Id`)) 
        ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=latin1");
      ExecuteSQL(@"INSERT INTO Child (Id, MainId, Value, Enabled) VALUES (1,2,12345,0x01)");

      MySqlCommand cmd = new MySqlCommand(
        @"SELECT m.Descr, c.Value, c.Enabled FROM Main m 
        LEFT OUTER JOIN Child c ON m.Id=c.MainId ORDER BY m.Descr", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.AreEqual("AAA", reader.GetString(0));
        Assert.True(reader.IsDBNull(1));
        Assert.True(reader.IsDBNull(2));

        Assert.True(reader.Read());
        Assert.AreEqual("BBB", reader.GetString(0));
        Assert.AreEqual(12345, Convert.ToInt32(reader.GetValue(1)));
        Assert.AreEqual(1, Convert.ToInt32(reader.GetValue(2)));

        Assert.True(reader.Read());
        Assert.AreEqual("CCC", reader.GetString(0));
        Assert.True(reader.IsDBNull(1));
        Assert.True(reader.IsDBNull(2));

        Assert.False(reader.Read());
      }
    }

    /// <summary>
    /// Bug #36081 Get Unknown Datatype in C# .Net 
    /// </summary>
    [Test]
    public void GeometryType()
    {
      ExecuteSQL(@"CREATE TABLE Test (ID int(11) NOT NULL, ogc_geom geometry NOT NULL,
        PRIMARY KEY  (`ID`))");

      if (Connection.driver.Version.isAtLeast(8, 0, 1))
        ExecuteSQL(@"INSERT INTO Test VALUES (1, 
          ST_GeomFromText('GeometryCollection(Point(1 1), LineString(2 2, 3 3))'))");
      else
        ExecuteSQL(@"INSERT INTO Test VALUES (1, 
          GeomFromText('GeometryCollection(Point(1 1), LineString(2 2, 3 3))'))");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
      }
    }

    #region MySqlGeometry Tests

    [Test]
    public void CanParseGeometryValueString()
    {
      var v = MySqlGeometry.Parse("POINT (47.37 -122.21)");
      Assert.AreEqual("POINT(47.37 -122.21)", v.ToString());
    }

    [Test]
    public void CanTryParseGeometryValueString()
    {
      MySqlGeometry v = new MySqlGeometry(0, 0);
      MySqlGeometry.TryParse("POINT (47.37 -122.21)", out v);
      Assert.AreEqual("POINT(47.37 -122.21)", v.ToString());
    }

    [Test]
    public void CanTryParseGeometryValueStringWithSRIDValue()
    {
      var mysqlGeometryResult = new MySqlGeometry(0, 0);
      MySqlGeometry.TryParse("SRID=101;POINT (47.37 -122.21)", out mysqlGeometryResult);
      Assert.AreEqual("SRID=101;POINT(47.37 -122.21)", mysqlGeometryResult.ToString());
    }

    [Test]
    public void StoringAndRetrievingGeometry()
    {
      ExecuteSQL("CREATE TABLE Test (v Geometry NOT NULL)");

      MySqlCommand cmd = new MySqlCommand(Connection.driver.Version.isAtLeast(8, 0, 1) ?
        "INSERT INTO Test VALUES (ST_GeomFromText(?v))" :
        "INSERT INTO Test VALUES (GeomFromText(?v))"
      , Connection);
      cmd.Parameters.Add("?v", MySqlDbType.String);
      cmd.Parameters[0].Value = "POINT(47.37 -122.21)";
      cmd.ExecuteNonQuery();

      cmd.CommandText = Connection.driver.Version.isAtLeast(8, 0, 1) ?
        "SELECT ST_AsText(v) FROM Test" :
        "SELECT AsText(v) FROM Test";

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        var val = reader.GetValue(0);
      }
    }

    [Test]
    public void CanFetchGeometryAsBinary()
    {
      ExecuteSQL("CREATE TABLE Test (v Geometry NOT NULL)");

      MySqlGeometry v = new MySqlGeometry(47.37, -122.21);

      var par = new MySqlParameter("?v", MySqlDbType.Geometry);
      par.Value = v;

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?v)", Connection);
      cmd.Parameters.Add(par);
      cmd.ExecuteNonQuery();

      cmd.CommandText = Connection.driver.Version.isAtLeast(8, 0, 1) ?
        "SELECT ST_AsBinary(v) FROM Test" :
        "SELECT AsBinary(v) FROM Test";

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        var val = reader.GetValue(0) as Byte[];
        var MyGeometry = new MySqlGeometry(MySqlDbType.Geometry, val);
        Assert.AreEqual("POINT(47.37 -122.21)", MyGeometry.ToString());
      }
    }

    [Test]
    public void CanSaveSridValueOnGeometry()
    {
      ExecuteSQL("CREATE TABLE Test (v Geometry NOT NULL)");

      MySqlGeometry v = new MySqlGeometry(47.37, -122.21, 101);
      var par = new MySqlParameter("?v", MySqlDbType.Geometry);
      par.Value = v;

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?v)", Connection);
      cmd.Parameters.Add(par);
      cmd.ExecuteNonQuery();

      cmd.CommandText = Connection.driver.Version.isAtLeast(8, 0, 1) ?
        "SELECT ST_SRID(v) FROM Test" :
        "SELECT SRID(v) FROM Test";

      using var reader = cmd.ExecuteReader();
      reader.Read();
      var val = reader.GetInt32(0);
      Assert.AreEqual(101, val);
    }

    [Test]
    public void CanFetchGeometryAsText()
    {
      ExecuteSQL("CREATE TABLE Test (v Geometry NOT NULL)");

      MySqlGeometry v = new MySqlGeometry(47.37, -122.21);
      var par = new MySqlParameter("?v", MySqlDbType.Geometry);
      par.Value = v;

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?v)", Connection);
      cmd.Parameters.Add(par);
      cmd.ExecuteNonQuery();

      cmd.CommandText = Connection.driver.Version.isAtLeast(8, 0, 1) ?
        "SELECT ST_AsText(v) FROM Test" :
        "SELECT AsText(v) FROM Test";

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        var val = reader.GetString(0);
        Assert.AreEqual("POINT(47.37 -122.21)", val);
      }
    }

    [Test]
    public void CanUseReaderGetMySqlGeometry()
    {
      ExecuteSQL("CREATE TABLE Test (v Geometry NOT NULL)");

      MySqlGeometry v = new MySqlGeometry(47.37, -122.21);
      var par = new MySqlParameter("?v", MySqlDbType.Geometry);
      par.Value = v;

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?v)", Connection);
      cmd.Parameters.Add(par);
      cmd.ExecuteNonQuery();

      // reading as binary
      cmd.CommandText = Connection.driver.Version.isAtLeast(8, 0, 1) ?
        "SELECT ST_AsBinary(v) as v FROM Test" :
        "SELECT AsBinary(v) as v FROM Test";

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        var val = reader.GetMySqlGeometry(0);
        var valWithName = reader.GetMySqlGeometry("v");
        Assert.AreEqual("POINT(47.37 -122.21)", val.ToString());
        Assert.AreEqual("POINT(47.37 -122.21)", valWithName.ToString());
      }

      // reading as geometry
      cmd.CommandText = "SELECT v as v FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        var val = reader.GetMySqlGeometry(0);
        var valWithName = reader.GetMySqlGeometry("v");
        Assert.AreEqual("POINT(47.37 -122.21)", val.ToString());
        Assert.AreEqual("POINT(47.37 -122.21)", valWithName.ToString());
      }

    }

    [Test]
    public void CanGetToStringFromMySqlGeometry()
    {
      MySqlGeometry v = new MySqlGeometry(47.37, -122.21);
      var valToString = v.ToString();
      Assert.AreEqual("POINT(47.37 -122.21)", valToString);
    }

    /// <summary>
    /// Bug #86974 Cannot create instance of MySqlGeometry for empty geometry collection 
    /// </summary>
    [Test]
    public void CanCreateMySqlGeometryFromEmptyGeometryCollection()
    {
      var bytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
      MySqlGeometry v = new MySqlGeometry(MySqlDbType.Geometry, bytes);
#if !NETFRAMEWORK
      Assert.AreEqual("POINT(3.5E-323 0)", v.ToString());
#else
      Assert.AreEqual("POINT(3.45845952088873E-323 0)", v.ToString());
#endif
    }

    /// <summary>
    /// Bug #86974 Cannot create instance of MySqlGeometry for empty geometry collection 
    /// </summary>
    [Test]
    public void CanGetMySqlGeometryFromEmptyGeometryCollection()
    {
      if (Version.CompareTo(new Version(5, 7)) == -1) return;

      ExecuteSQL("CREATE TABLE Test (v Geometry NOT NULL)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (ST_GeometryCollectionFromText(\"GEOMETRYCOLLECTION()\"))", Connection);
      cmd.ExecuteNonQuery();

      // reading as binary
      cmd.CommandText = "SELECT ST_AsBinary(v) as v FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        var val = reader.GetMySqlGeometry(0);
        var valWithName = reader.GetMySqlGeometry("v");
        Assert.AreEqual("POINT(0 0)", val.ToString());
        Assert.AreEqual("POINT(0 0)", valWithName.ToString());
      }

      // reading as geometry
      cmd.CommandText = "SELECT v as v FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        var val = reader.GetMySqlGeometry(0);
        var valWithName = reader.GetMySqlGeometry("v");
#if !NETFRAMEWORK
        Assert.AreEqual("POINT(3.5E-323 0)", val.ToString());
        Assert.AreEqual("POINT(3.5E-323 0)", valWithName.ToString());
#else
        Assert.AreEqual("POINT(3.45845952088873E-323 0)", val.ToString());
        Assert.AreEqual("POINT(3.45845952088873E-323 0)", valWithName.ToString());
#endif
      }
    }

    /// <summary>
    /// Bug #30169716 MYSQLEXCEPTION WHEN INSERTING A MYSQLGEOMETRY VALUE
    /// Bug #30169715	WHERE CLAUSE USING MYSQLGEOMETRY AS PARAMETER FINDS NO ROWS
    /// </summary>
    [Test]
    public void Bug30169716()
    {
      ExecuteSQL("DROP TABLE IF EXISTS geometries");
      ExecuteSQL("CREATE TABLE geometries(id INTEGER NOT NULL PRIMARY KEY AUTO_INCREMENT, data GEOMETRY)");

      var geometry = new MySqlGeometry(1, 1);

      using (var command = Connection.CreateCommand())
      {
        command.CommandText = "INSERT INTO geometries(data) VALUES(@data); ";
        command.Parameters.AddWithValue("@data", geometry);
        int result = command.ExecuteNonQuery();
        Assert.AreEqual(1, result);
      }
    }

    #endregion

    /// <summary>
    /// Bug #33322 Incorrect Double/Single value saved to MySQL database using MySQL Connector for
    /// </summary>
    [Test]
    public void StoringAndRetrievingDouble()
    {
      ExecuteSQL("CREATE TABLE Test (v DOUBLE(25,20) NOT NULL)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?v)", Connection);
      cmd.Parameters.Add("?v", MySqlDbType.Double);
      cmd.Parameters[0].Value = Math.PI;
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        double d = reader.GetDouble(0);
        Assert.AreEqual(Math.PI, d);
      }
    }

    /// <summary>
    /// Bug #40571  	Add GetSByte to the list of public methods supported by MySqlDataReader
    /// </summary>
    [Test]
    public void SByteFromReader()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (c1 TINYINT, c2 TINYINT UNSIGNED)");
      ExecuteSQL("INSERT INTO Test VALUES (99, 217)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual(99, reader.GetSByte(0));
        Assert.AreEqual(217, reader.GetByte(1));
        Assert.AreEqual(99, reader.GetByte(0));
      }
    }

    [Test]
    public void NewGuidDataType()
    {
      ExecuteSQL("CREATE TABLE Test(id INT, g BINARY(16))");

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
          Assert.AreEqual(1, reader.GetValue(0));
          Assert.AreEqual(guid, reader.GetGuid(1));
        }
      }
    }

    /// <summary>
    /// Bug #44507 Binary(16) considered as Guid 
    /// </summary>
    [Test]
    public void ReadBinary16AsBinary()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, guid BINARY(16))");

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
          Assert.AreEqual(16, size);
        }
      }
    }

    [Test]
    public void ReadingUUIDAsGuid()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, guid CHAR(36))");
      ExecuteSQL("INSERT INTO Test VALUES (1, UUID())");

      MySqlCommand cmd = new MySqlCommand("SELECT CONCAT('A', guid) FROM Test", Connection);
      string serverGuidStr = cmd.ExecuteScalar().ToString().Substring(1);
      Guid serverGuid = new Guid(serverGuidStr);

      cmd.CommandText = "SELECT guid FROM Test";
      Guid g = (Guid)cmd.ExecuteScalar();
      Assert.AreEqual(serverGuid, g);
    }

    [Test]
    public void NewGuidType()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, guid CHAR(36))");

      Guid g = Guid.NewGuid();
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, @g)", Connection);
      cmd.Parameters.AddWithValue("@g", g);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT guid FROM Test";
      Guid readG = (Guid)cmd.ExecuteScalar();
      Assert.AreEqual(g, readG);
    }

    /// <summary>
    /// Bug #29963760 - FIRST QUERY AFTER APPLICATION RESTART ALWAYS FAILS WITH GUID ERROR
    /// This bug was treating all the columns with length of 36 as GUID which was incorrect. 
    /// </summary>
    [Test]
    public void NotGuidType()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, val CHAR(36), guid CHAR(36))");
      string s = "1234567890 1234567890 1234567890 123";
      Guid g = Guid.NewGuid();
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, @s, @g)", Connection);
      cmd.Parameters.AddWithValue("@s", s);
      cmd.Parameters.AddWithValue("@g", g);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.True(reader["id"] is int);
        Assert.True(reader["val"] is string);
        Assert.True(reader["guid"] is Guid);
      }
    }

    /// <summary>
    /// Bug #47928 Old Guids=true setting is lost after null value is
    /// encountered in a Binary(16) 
    /// </summary>
    [Test]
    public void OldGuidsWithNull()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, guid BINARY(16))");

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
    [Test]
    public void UTF8Char12AsGuid()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, name CHAR(12) CHARSET utf8)");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'Name')");

      string connStr = Connection.ConnectionString + ";charset=utf8";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          string s = reader.GetString(1);
          Assert.AreEqual("Name", s);
        }
      }
    }

    /// <summary>
    /// Bug #48100	Impossible to retrieve decimal value if it doesn't fit into .Net System.Decimal
    /// </summary>
    [Test]
    public void MySqlDecimal()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, dec1 DECIMAL(36,2))");
      ExecuteSQL("INSERT INTO Test VALUES (1, 9999999999999999999999999999999999.99)");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        MySqlDecimal dec = reader.GetMySqlDecimal(1);
        string s = dec.ToString();
        Assert.AreEqual(9999999999999999999999999999999999.99, dec.ToDouble());
        Assert.AreEqual("9999999999999999999999999999999999.99", dec.ToString());

        void Value() { _ = dec.Value; }

        Exception ex = Assert.Throws<OverflowException>(() => Value());
        Assert.AreEqual("Value was either too large or too small for a Decimal.", ex.Message);
      }
    }

    /// <summary>
    /// Bug #55644 Value was either too large or too small for a Double 
    /// </summary>
    [Test]
    public void DoubleMinValue()
    {
      ExecuteSQL("CREATE TABLE Test(dbl double)");
      MySqlCommand cmd = new MySqlCommand("insert into Test values(?param1)");
      cmd.Connection = Connection;
      cmd.Parameters.Add(new MySqlParameter("?param1", MySqlDbType.Double));
      cmd.Parameters["?param1"].Value = Double.MinValue;
      cmd.ExecuteNonQuery();
      cmd.Parameters["?param1"].Value = Double.MaxValue;
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        double d = reader.GetDouble(0);
        Assert.AreEqual(d, double.MinValue);
        reader.Read();
        d = reader.GetDouble(0);
        Assert.AreEqual(d, double.MaxValue);
      }
    }

    /// <summary>
    /// Bug #58373	ReadInteger problem
    /// </summary>
    [Test]
    public void BigIntAutoInc()
    {
      ExecuteSQL("CREATE TABLE Test(ID bigint unsigned AUTO_INCREMENT NOT NULL PRIMARY KEY, name VARCHAR(20))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (@id, 'boo')", Connection);
      ulong val = UInt64.MaxValue;
      val -= 100;
      cmd.Parameters.AddWithValue("@id", val);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "INSERT INTO Test (name) VALUES ('boo2')";
      cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Bug # 13708884 timediff function
    /// Executing a simple query that generates a time difference that has a 
    /// fractional second value throws an exception
    /// </summary>
    [Test]
    public void Timediff()
    {
      MySqlCommand cmd = new MySqlCommand("select timediff('2 0:1:1.0', '4 1:2:3.123456')", Connection);
      var result = cmd.ExecuteScalar();
      Assert.AreEqual(new TimeSpan(new TimeSpan(-2, -1, -1, -2).Ticks - 1234560), result);
    }

    [Test]
    public void CanReadJsonValue()
    {
      if (Version < new Version(5, 7)) Assert.Ignore("This test is for MySql 5.7 or higher"); ;
      ExecuteSQL("CREATE TABLE Test(Id int NOT NULL PRIMARY KEY, jsoncolumn JSON)");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (@id, '[1]')", Connection);
      cmd.Parameters.AddWithValue("@id", 1);
      cmd.ExecuteNonQuery();
      string command = @"INSERT INTO Test VALUES (@id, '[""a"", {""b"": [true, false]}, [10, 20]]')";
      cmd = new MySqlCommand(command, Connection);
      cmd.Parameters.AddWithValue("@id", 2);
      cmd.ExecuteNonQuery();
      cmd = new MySqlCommand("SELECT jsoncolumn from Test where id = 2 ", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.AreEqual("[\"a\", {\"b\": [true, false]}, [10, 20]]", reader.GetString(0));
      }

      ExecuteSQL("delete from Test");
      cmd = new MySqlCommand("INSERT INTO Test VALUES (@id, '[1]')", Connection);
      cmd.Parameters.AddWithValue("@id", 1);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand(@"INSERT INTO Test VALUES(@id,' { ""name"" : ""Bob"" , ""age"" : 25 } ')", Connection);
      cmd.Parameters.AddWithValue("@id", 2);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand(@"INSERT INTO Test VALUES(@id,' { ""name"" : ""Test"" , ""age"" : 100000 } ')", Connection);
      cmd.Parameters.AddWithValue("@id", 3);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand(@"INSERT INTO Test VALUES(@id,' { ""age"" : 200, ""name"" : ""check""  } ')", Connection);
      cmd.Parameters.AddWithValue("@id", 4);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand(@"INSERT INTO Test VALUES(@id,' { ""age"" : 200,""zage"" : 300,""bage"" : 400, ""name"" : ""check""  } ')", Connection);
      cmd.Parameters.AddWithValue("@id", 5);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("SELECT jsoncolumn from Test where id = 2", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        Assert.AreEqual(true, reader.Read(), "Matching the values");
        var checkValue = "{\"age\": 25, \"name\": \"Bob\"}";
        Assert.AreEqual(checkValue, reader.GetString(0), "Matching the values");
      }
      cmd = new MySqlCommand("SELECT count(*) from Test", Connection);
      var count = cmd.ExecuteScalar();
      Assert.AreEqual(5, count);

      cmd = new MySqlCommand(@"INSERT INTO Test VALUES(@id,' { ""name"" : ""harald"",""Date"": ""2013-08-07"",""Time"": ""11:18:29.000000"",""DateTimeOfRegistration"": ""2013-08-07 12:18:29.000000""} ')",
                            Connection);
      cmd.Parameters.AddWithValue("@id", 1000);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("SELECT jsoncolumn from Test where id=1000", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        Assert.AreEqual(true, reader.Read(), "Matching the values");
        var checkValue =
            "{\"Date\": \"2013-08-07\", \"Time\": \"11:18:29.000000\", \"name\": \"harald\", \"DateTimeOfRegistration\": \"2013-08-07 12:18:29.000000\"}";
        Assert.AreEqual(checkValue, reader.GetString(0), "Matching the values");
      }

      //Multiple Columns
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test (Id int NOT NULL PRIMARY KEY, jsoncolumn1 JSON,jsoncolumn2 JSON,jsoncolumn3 JSON)");

      cmd = new MySqlCommand(@"INSERT INTO Test VALUES(@id,'{ ""name"" : ""bob""}', '{ ""marks"" : 97}','{ ""distinction"" : true}')",
          Connection);
      cmd.Parameters.AddWithValue("@id", 100000);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("SELECT jsoncolumn1 from Test where id=100000", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        Assert.AreEqual(true, reader.Read(), "Matching the values");
        var checkValue = "{\"name\": \"bob\"}";
        Assert.AreEqual(checkValue, reader.GetString(0), "Matching the values");
      }

      cmd = new MySqlCommand("SELECT jsoncolumn2 from Test where id=100000", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        Assert.AreEqual(true, reader.Read(), "Matching the values");
        var checkValue = "{\"marks\": 97}";
        Assert.AreEqual(checkValue, reader.GetString(0), "Matching the values");
      }

      cmd = new MySqlCommand("SELECT jsoncolumn3 from Test where id=100000", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        Assert.AreEqual(true, reader.Read(), "Matching the values");
        var checkValue = "{\"distinction\": true}";
        Assert.AreEqual(checkValue, reader.GetString(0), "Matching the values");
      }
    }

    [Test]
    public void CanUpdateJsonValue()
    {
      if (Version < new Version(5, 7)) return;

      ExecuteSQL("CREATE TABLE Test(id int NOT NULL PRIMARY KEY, jsoncolumn JSON)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (@id, '[1]')", Connection);
      cmd.Parameters.AddWithValue("@id", 1);
      cmd.ExecuteNonQuery();

      string command = @"UPDATE Test set jsoncolumn = '[""a"", {""b"": [true, false]}, [10, 20]]' where id = 1";
      cmd = new MySqlCommand(command, Connection);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("SELECT jsoncolumn from Test where id = 1 ", Connection);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.AreEqual("[\"a\", {\"b\": [true, false]}, [10, 20]]", reader.GetString(0));
      }

      cmd = new MySqlCommand(@"INSERT INTO Test VALUES(@id,' { ""name"" : ""bob"",""Date"": ""2015-10-09"",""Time"": ""12:18:29.000000"",""DateTimeOfRegistration"": ""2015-10-09 12:18:29.000000""} ')",
                             Connection);
      cmd.Parameters.AddWithValue("@id", 100000);
      cmd.ExecuteNonQuery();

      command = @"UPDATE Test set jsoncolumn = ' { ""name"" : ""harald"",""Date"": ""2013-08-07"",""Time"": ""11:18:29.000000"",""DateTimeOfRegistration"": ""2013-08-07 12:18:29.000000""} ' where id = 100000";
      cmd = new MySqlCommand(command, Connection);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("SELECT jsoncolumn from Test where id=100000", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        Assert.AreEqual(true, reader.Read(), "Matching the values");
        var checkValue =
            "{\"Date\": \"2013-08-07\", \"Time\": \"11:18:29.000000\", \"name\": \"harald\", \"DateTimeOfRegistration\": \"2013-08-07 12:18:29.000000\"}";
        Assert.AreEqual(checkValue, reader.GetString(0), "Matching the values");
      }

    }

    /// Testing out Generated Columns
    /// Using a case sensitive collation on a column
    /// and an insensitive serch with a generated column
    /// WL #411 
    ///
    [Test]
    public void CanUseGeneratedColumns()
    {
      if (Version < new Version(5, 7)) return;

      ExecuteSQL("CREATE TABLE `Test` (`ID` int NOT NULL AUTO_INCREMENT PRIMARY KEY, `Name` char(35) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (Name) VALUES ('Berlin')", Connection);
      cmd.ExecuteNonQuery();
      cmd = new MySqlCommand("INSERT INTO Test (Name) VALUES ('London')", Connection);
      cmd.ExecuteNonQuery();
      cmd = new MySqlCommand("INSERT INTO Test (Name) VALUES ('France')", Connection);
      cmd.ExecuteNonQuery();
      cmd = new MySqlCommand("INSERT INTO Test (Name) VALUES ('United Kingdom')", Connection);
      cmd.ExecuteNonQuery();
      cmd = new MySqlCommand("INSERT INTO Test (Name) VALUES ('Italy')", Connection);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("ALTER TABLE Test ADD COLUMN Name_ci char(35) CHARACTER SET utf8 AS (Name) STORED;", Connection);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("ALTER TABLE Test ADD INDEX (Name_ci);", Connection);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("SELECT Name FROM Test WHERE Name_ci='berlin'", Connection);

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.True(reader.GetString(0).Equals("Berlin", StringComparison.CurrentCulture));
      }
    }

    /// <summary>
    /// Bug #31598178 - SQL WITH DATETIME PARAMETER RETURNS STRING VALUE
    /// </summary>
    [Test]
    public void DateTimeTreatedAsVarChar()
    {
      string sql = "SELECT ?p0 as value";

      using (MySqlCommand cmd = new MySqlCommand(sql, Connection))
      {
        cmd.Parameters.AddWithValue("?p0", DateTime.Now);

        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          using (DataTable schema = reader.GetSchemaTable())
          {
            MySqlDbType providerType = (MySqlDbType)(int)schema.Rows[0]["ProviderType"];
            Assert.AreEqual(MySqlDbType.DateTime, providerType);
          }
        }
      }
    }

    /// <summary>
    /// Bug #32049837 - CAN'T QUERY CHAR(36) COLUMN CONTAINING NULL
    /// </summary>
    [Test]
    public void NullGuid()
    {
      ExecuteSQL("CREATE TABLE `Test` (value CHAR(36)); INSERT INTO Test(value) VALUES(NULL);");

      MySqlCommand cmd = new MySqlCommand("SELECT value FROM Test", Connection);
      using var reader = cmd.ExecuteReader();
      while (reader.Read())
      {
        Assert.True(reader.IsDBNull(0));
      }
    }

    /// <summary>
    /// Bug #32938630 - CAN'T READ CHAR(36) COLUMN IF MYSQLCOMMAND IS PREPARED
    /// </summary>
    [Test]
    public void ReadChar36ColumnPrepared()
    {
      string guid = "3e22b63e-8077-43ab-8cee-17aa1db80861";
      ExecuteSQL($"CREATE TABLE `Test` (value CHAR(36)); INSERT INTO Test(value) VALUES('{guid}');");

      MySqlCommand cmd = new MySqlCommand("SELECT value FROM Test", Connection);
      cmd.Prepare();
      using var reader = cmd.ExecuteReader();
      while (reader.Read())
      {
        StringAssert.AreEqualIgnoringCase(guid, reader.GetGuid(0).ToString());
      }
    }

    /// <summary>
    /// Bug 26876582 UNEXPECTED COLUMNSIZE FOR CHAR(36) AND BLOB COLUMNS IN GETSCHEMATABLE
    /// </summary>
    [Test]
    public void UnexpectedColumnSize()
    {
      var cmd = new MySqlCommand("create table datatypes(char36 char(36),char37 char(37),`tinyblob` tinyblob,`blob` blob); ", Connection);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("insert into datatypes values('test', 'test', _binary'test',_binary'test'); ", Connection);
      cmd.ExecuteNonQuery();

      using (cmd = Connection.CreateCommand())
      {
        cmd.CommandText = "SELECT * FROM datatypes;";
        using (var reader = cmd.ExecuteReader())
        {
          var schemaTable = reader.GetSchemaTable();
          Assert.AreEqual("36", schemaTable.Rows[0]["ColumnSize"].ToString(), "Matching the Column Size");
          Assert.AreEqual("37", schemaTable.Rows[1]["ColumnSize"].ToString(), "Matching the Column Size");
          Assert.AreEqual("255", schemaTable.Rows[2]["ColumnSize"].ToString(), "Matching the Column Size");
          Assert.AreEqual("65535", schemaTable.Rows[3]["ColumnSize"].ToString(), "Matching the Column Size");
        }
      }
    }

    [Test, Description("Test Can Read long JSON Values")]
    public void ReadJSONLongValues()
    {
      if (Version < new Version(5, 7)) Assert.Ignore("This test is for MySql 5.7 or higher.");
      var sb = new StringBuilder("0");
      for (int x = 1; x <= 575; x++)
      {
        sb.Append($"TestingaLongString{x}");
      }

      ExecuteSQL("CREATE TABLE Test (Id int NOT NULL PRIMARY KEY, jsoncolumn JSON)");

      string jsonTest = null;
      var i = 1000000000;
      jsonTest = @"{ ""age"" : " + i + "}";
      var cmd = new MySqlCommand("INSERT INTO Test VALUES (@id, @jsoncolumn)", Connection);
      cmd.Parameters.AddWithValue("@id", i);
      cmd.Parameters.AddWithValue("@jsoncolumn", jsonTest);
      cmd.ExecuteNonQuery();
      cmd = new MySqlCommand("SELECT jsoncolumn from Test where id = " + i, Connection);
      using (var reader = cmd.ExecuteReader())
      {
        Assert.AreEqual(true, reader.Read(), "Matching the values");
        var checkValue = @"{""age"": " + i + "}";
        Assert.AreEqual(checkValue, reader.GetString(0), "Matching the values");
      }

      // long string
      cmd = new MySqlCommand(
        @"INSERT INTO Test VALUES(@id,'{""name"":""" + sb.ToString() + @"""}')",
        Connection);
      cmd.Parameters.AddWithValue("@id", 2);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("SELECT jsoncolumn from Test where id = 2", Connection);
      using (var reader = cmd.ExecuteReader())
      {
        Assert.AreEqual(true, reader.Read(), "Matching the values");
        var checkValue =
            @"{""name"": """ + sb.ToString() + @"""}";
        Assert.AreEqual(checkValue, reader.GetString(0), "Matching the values");
      }
    }

    [Test, Description("Test Can Read JSON Value Stress")]
    public void ReadJSONValueStress()
    {
      if (Version < new Version(5, 7)) Assert.Ignore("This test is for MySql 5.7 or higher.");
      ExecuteSQL("CREATE TABLE Test (Id int NOT NULL PRIMARY KEY, jsoncolumn JSON)");
      string jsonTest = null;
      for (var i = 0; i < 1000; i++)
      {
        jsonTest = @"{ ""age"" : " + i + "}";
        var cmd = new MySqlCommand("INSERT INTO Test VALUES (@id, @jsoncolumn)", Connection);
        cmd.Parameters.AddWithValue("@id", i);
        cmd.Parameters.AddWithValue("@jsoncolumn", jsonTest);
        cmd.ExecuteNonQuery();

        cmd = new MySqlCommand("SELECT jsoncolumn from Test where id = " + i, Connection);

        using (var reader = cmd.ExecuteReader())
        {
          Assert.AreEqual(true, reader.Read(), "Matching the values");
          var checkValue = @"{""age"": " + i + "}";
          Assert.AreEqual(checkValue, reader.GetString(0), "Matching the values");
        }
      }
    }

    /// <summary>
    /// Bug #33470147 - Bug #91752 is marked as fixed in v8.0.16, but still present in v8.0.26
    /// When reading a zero time value, it didn't reset the value of the new row hence the exception
    /// </summary>
    [Test]
    public void ZeroTimeValues()
    {
      ExecuteSQL(@"CREATE TABLE Test (tm TIME NOT NULL); 
        INSERT INTO Test VALUES('00:00:00'); 
        INSERT INTO Test VALUES('01:01:01');
        INSERT INTO Test VALUES('00:00:00');");

      using (var command = new MySqlCommand(@"SELECT tm FROM Test", Connection))
      {
        command.Prepare();
        using (var reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.AreEqual("00:00:00", reader.GetValue(0).ToString());
          Assert.AreEqual("00:00:00", reader.GetTimeSpan(0).ToString());

          Assert.True(reader.Read());
          Assert.AreEqual("01:01:01", reader.GetValue(0).ToString());
          Assert.AreEqual("01:01:01", reader.GetTimeSpan(0).ToString());

          Assert.True(reader.Read());
          Assert.AreEqual("00:00:00", reader.GetValue(0).ToString());
          Assert.AreEqual("00:00:00", reader.GetTimeSpan(0).ToString());
        }
      }
    }

    /// <summary>
    /// Bug #32933120 - CAN'T USE TIMESPAN VALUE WITH MICROSECONDS USING PREPARED STATEMENT
    /// At the moment of writing the time value, the calculation of microseconds was wrong
    /// </summary>
    [Test]
    public void TimespanWithMicrosecondsPrepared()
    {
      ExecuteSQL("CREATE TABLE Test (tm TIME(4))");
      var value = new TimeSpan(0, 0, 0, 1, 234) + TimeSpan.FromTicks(5000);

      using var cmd = new MySqlCommand();
      cmd.Connection = Connection;
      cmd.Parameters.AddWithValue("@value", value);

      // Try the INSERT
      cmd.CommandText = "INSERT INTO Test VALUES(@value)";
      cmd.Prepare();
      Assert.AreEqual(1, cmd.ExecuteNonQuery());

      // Try the SELECT
      cmd.CommandText = "SELECT tm FROM Test WHERE tm = @value;";
      cmd.Prepare();
      using var reader = cmd.ExecuteReader();
      Assert.IsTrue(reader.Read());
      Assert.AreEqual(value, reader.GetValue(0));
    }

    /// <summary>
    /// Bug #31087580	[UNEXPECTED RETURN VALUE GETTING INTEGER FOR TINYINT(1) COLUMN]
    /// </summary>
    [TestCase(true, true)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(false, false)]
    public void GetIntForTinyInt(bool treatAsBool, bool isPrepared)
    {
      ExecuteSQL(@"CREATE TABLE Test (value tinyint(1)); INSERT INTO Test VALUES (-2);");
      string connString = Connection.ConnectionString + $";treattinyasboolean={treatAsBool};";

      using var conn = new MySqlConnection(connString);
      conn.Open();

      using var cmd = new MySqlCommand("SELECT * FROM Test", conn);
      if (isPrepared) cmd.Prepare();
      using var reader = cmd.ExecuteReader();
      reader.Read();

      Assert.AreEqual(treatAsBool ? 1 : -2, reader.GetSByte(0));
      Assert.Throws<OverflowException>(() => reader.GetByte(0));
      Assert.AreEqual(treatAsBool ? 1 : -2, reader.GetInt16(0));
      Assert.AreEqual(treatAsBool ? 1 : -2, reader.GetInt32(0));
      Assert.AreEqual(treatAsBool ? 1 : -2, reader.GetInt64(0));

      Assert.AreEqual(treatAsBool ? 1 : -2, reader.GetFieldValue<sbyte>(0));
      Assert.Throws<OverflowException>(() => reader.GetFieldValue<byte>(0));
      Assert.AreEqual(treatAsBool ? 1 : -2, reader.GetFieldValue<short>(0));
      Assert.AreEqual(treatAsBool ? 1 : -2, reader.GetFieldValue<int>(0));
      Assert.AreEqual(treatAsBool ? 1 : -2, reader.GetFieldValue<long>(0));
    }

    [Test]
    public void OldGetStringBehaviorOff()
    {
      ExecuteSQL(@"CREATE TABLE Test (value1 int, value2 DATETIME); INSERT INTO Test VALUES (1, '2023-10-30');");

      using var cmd = new MySqlCommand();
      cmd.Connection = Connection;
      cmd.CommandText = "SELECT * FROM Test";
      using var reader = cmd.ExecuteReader();
      reader.Read();
      Assert.Throws<InvalidCastException>(() => reader.GetString(0));
      Assert.Throws<InvalidCastException>(() => reader.GetString(1));
   }

    [Test]
    public void OldGetStringBehaviorOn()
    {
      ExecuteSQL(@"CREATE TABLE Test (value1 int, value2 DECIMAL(10,2)); INSERT INTO Test VALUES (1, 20.4);");
      
      string connString = Connection.ConnectionString + $";oldgetstringbehavior=true;";
      using var conn = new MySqlConnection(connString);
      conn.Open();

      using var cmd = new MySqlCommand();
      cmd.Connection = conn;
      cmd.CommandText = "SELECT * FROM Test";
      using var reader = cmd.ExecuteReader();
      reader.Read();
      Assert.AreEqual("1", reader.GetString(0));
      Assert.AreEqual("20.40", reader.GetString(1));
   }

  }
}