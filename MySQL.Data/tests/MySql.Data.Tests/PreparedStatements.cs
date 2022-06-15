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

using NUnit.Framework;
using System;
using System.Data;
using System.Linq;

namespace MySql.Data.MySqlClient.Tests
{
  public class PreparedStatements : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    [Test]
    public void Simple()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, dec1 DECIMAL(5,2), name VARCHAR(100), year YEAR)");
      ExecuteSQL("INSERT INTO Test VALUES (1, 345.12, 'abcd', 2019)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1,345.12,'abcd',2019)", Connection);
      cmd.Prepare();
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.AreEqual(1, reader.GetInt32(0));
        Assert.AreEqual((decimal)345.12, reader.GetDecimal(1));
        Assert.AreEqual("abcd", reader.GetString(2));
        Assert.AreEqual(2019, reader.GetInt16(3));
      }
    }

    [Test]
    public void SimplePrepareBeforeParms()
    {
      ExecuteSQL("CREATE TABLE Test (one INTEGER, two INTEGER)");
      ExecuteSQL("INSERT INTO Test VALUES (1, 2)");

      // create the command and prepare the statement
      IDbCommand cmd = Connection.CreateCommand();
      cmd.CommandText = "SELECT * FROM Test WHERE one = ?p1";

      // create the parameter
      IDbDataParameter p1 = cmd.CreateParameter();
      p1.ParameterName = "?p1";
      p1.DbType = DbType.Int32;
      p1.Precision = (byte)10;
      p1.Scale = (byte)0;
      p1.Size = 4;
      cmd.Parameters.Add(p1);
      p1.Value = 1;

      cmd.Prepare();
      // Execute the reader
      using (IDataReader reader = cmd.ExecuteReader())
      {
        // Fetch the first record
        reader.Read();
        Assert.AreEqual(1, reader.GetInt32(0));
        Assert.AreEqual(2, reader.GetInt32(1));
      }
    }

    [Test]
    public void DateAndTimes()
    {

      if (Version < new Version(5, 6))
        ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, d DATE, dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");
      else
        ExecuteSQL(@"CREATE TABLE Test (id INT NOT NULL, d DATE, dt DATETIME, tm TIME(6), 
                     ts TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, PRIMARY KEY(id))");

      string timeStampValue = "NULL";
      if (Version >= new Version(8, 0, 2)) timeStampValue = "NOW()";

      string sql = "INSERT INTO Test VALUES(?id, ?d, ?dt, ?tm, " + timeStampValue + ")";
      MySqlCommand cmd = new MySqlCommand(sql, Connection);

      DateTime dt = DateTime.Now;
      dt = dt.AddMilliseconds(dt.Millisecond * -1);
      TimeSpan timeSp = new TimeSpan(8, 11, 44, 56, 501);

      cmd.Parameters.AddWithValue("?id", 1);
      cmd.Parameters.AddWithValue("?d", dt);
      cmd.Parameters.AddWithValue("?dt", dt);
      cmd.Parameters.AddWithValue("?tm", timeSp);
      cmd.Prepare();
      int count = cmd.ExecuteNonQuery();
      Assert.True(count == 1, "Records affected by insert");

      cmd.CommandText = "SELECT * FROM Test";
      cmd.Prepare();

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.True(reader.GetInt32(0) == 1, "Id column");
        Assert.True(dt.Date == reader.GetDateTime(1).Date, "Date column");

        DateTime dt2 = reader.GetDateTime(2);
        Assert.AreEqual(dt.Date, dt2.Date);
        Assert.AreEqual(dt.Hour, dt2.Hour);
        Assert.AreEqual(dt.Minute, dt2.Minute);
        Assert.AreEqual(dt.Second, dt2.Second);

        TimeSpan ts2 = reader.GetTimeSpan(3);
        Assert.AreEqual(timeSp.Days, ts2.Days);
        Assert.AreEqual(timeSp.Hours, ts2.Hours);
        Assert.AreEqual(timeSp.Minutes, ts2.Minutes);
        Assert.AreEqual(timeSp.Seconds, ts2.Seconds);
        Assert.True(dt.Date == reader.GetDateTime(4).Date, "Timestamp column");
      }
    }

    [Test]
    public void ResetCommandText()
    {
      ExecuteSQL("CREATE TABLE Test (id int, name varchar(100))");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'Test')");

      MySqlCommand cmd = new MySqlCommand("SELECT id FROM Test", Connection);
      cmd.Prepare();
      object o = cmd.ExecuteScalar();
      Assert.AreEqual(1, o);

      cmd.CommandText = "SELECT name FROM Test";
      cmd.Prepare();
      o = cmd.ExecuteScalar();
      Assert.AreEqual("Test", o);

    }

    [Test]
    public void DifferentParameterOrder()
    {
      ExecuteSQL("CREATE TABLE Test (id int NOT NULL AUTO_INCREMENT, " +
          "id2 int NOT NULL, name varchar(50) DEFAULT NULL, " +
          "id3 int DEFAULT NULL, PRIMARY KEY (id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, id2, name, id3) " +
                        "VALUES(?id, ?id2, ?name,?id3)", Connection);

      MySqlParameter id = new MySqlParameter();
      id.ParameterName = "?id";
      id.DbType = DbType.Int32;
      id.Value = DBNull.Value;

      MySqlParameter id2 = new MySqlParameter();
      id2.ParameterName = "?id2";
      id2.DbType = DbType.Int32;
      id2.Value = 2;

      MySqlParameter name = new MySqlParameter();
      name.ParameterName = "?name";
      name.DbType = DbType.String;
      name.Value = "Test";

      MySqlParameter id3 = new MySqlParameter();
      id3.ParameterName = "?id3";
      id3.DbType = DbType.Int32;
      id3.Value = 3;

      cmd.Parameters.Add(id);
      cmd.Parameters.Add(id2);
      cmd.Parameters.Add(name);
      cmd.Parameters.Add(id3);
      cmd.Prepare();
      Assert.AreEqual(1, cmd.ExecuteNonQuery());

      cmd.Parameters.Clear();

      id3.Value = DBNull.Value;
      name.Value = DBNull.Value;
      cmd.Parameters.Add(id);
      cmd.Parameters.Add(id2);
      cmd.Parameters.Add(id3);
      cmd.Parameters.Add(name);

      cmd.Prepare();
      Assert.AreEqual(1, cmd.ExecuteNonQuery());

      cmd.CommandText = "SELECT id3 FROM Test WHERE id=1";
      Assert.AreEqual(3, cmd.ExecuteScalar());

      cmd.CommandText = "SELECT name FROM Test WHERE id=2";
      Assert.AreEqual(DBNull.Value, cmd.ExecuteScalar());
    }

    [Test]
    public void Blobs()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, blob1 LONGBLOB, text1 LONGTEXT)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?blob1, ?text1)", Connection);

      byte[] bytes = Utils.CreateBlob(400000);
      string inStr = "This is my text";

      cmd.Parameters.AddWithValue("?id", 1);
      cmd.Parameters.AddWithValue("?blob1", bytes);
      cmd.Parameters.AddWithValue("?text1", inStr);
      cmd.Prepare();
      int count = cmd.ExecuteNonQuery();
      Assert.AreEqual(1, count);

      cmd.CommandText = "SELECT * FROM Test";
      cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.AreEqual(1, reader.GetInt32(0));
        Assert.AreEqual(bytes.Length, reader.GetBytes(1, 0, null, 0, 0));
        byte[] outBytes = new byte[bytes.Length];
        reader.GetBytes(1, 0, outBytes, 0, bytes.Length);
        for (int x = 0; x < bytes.Length; x++)
          Assert.AreEqual(bytes[x], outBytes[x]);
        Assert.AreEqual(inStr, reader.GetString(2));
      }
    }

    [Test]
    public void SimpleTest2()
    {
      ExecuteSQL("CREATE TABLE Test (one integer, two integer, three integer, four integer, five integer, six integer, seven integer)");
      ExecuteSQL("INSERT INTO Test VALUES (1, 2, 3, 4, 5, 6, 7)");

      // create the command and prepare the statement
      IDbCommand cmd = Connection.CreateCommand();
      cmd.CommandText = "SELECT one, two, three, four, five, six, seven FROM Test";
      cmd.Prepare();
      // Execute the reader
      using (IDataReader reader = cmd.ExecuteReader())
      {
        // Fetch the first record
        reader.Read();

        Assert.AreEqual(1, reader.GetInt32(0));
        Assert.AreEqual(2, reader.GetInt32(1));
        Assert.AreEqual(3, reader.GetInt32(2));
        Assert.AreEqual(4, reader.GetInt32(3));
        Assert.AreEqual(5, reader.GetInt32(4));
        Assert.AreEqual(6, reader.GetInt32(5));
        Assert.AreEqual(7, reader.GetInt32(6));
      }
    }

    [Test]
    [Ignore("Fix this")]
    public void Bug6271()
    {
      MySqlCommand cmd = null;
      string sql = null;

      // Updating the default charset for servers 8.0+.
      if (Connection.driver.Version.isAtLeast(8, 0, 1))
      {
        sql = "SET NAMES 'latin1' COLLATE 'latin1_swedish_ci'";
        cmd = new MySqlCommand(sql, Connection);
        cmd.ExecuteNonQuery();
      }

      // Create the table again
      ExecuteSQL("CREATE TABLE `Test2` (id INT unsigned NOT NULL auto_increment, " +
        "`xpDOSG_Name` text,`xpDOSG_Desc` text, `Avatar` MEDIUMBLOB, `dtAdded` DATETIME, `dtTime` TIMESTAMP, " +
        "PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=latin1");

      sql = "INSERT INTO `Test2` (`xpDOSG_Name`,`dtAdded`, `xpDOSG_Desc`,`Avatar`, `dtTime`) " +
        "VALUES(?name, ?dt, ?desc, ?avatar, NULL)";

      cmd = new MySqlCommand(sql, Connection);

      DateTime dt = DateTime.Now;
      dt = dt.AddMilliseconds(dt.Millisecond * -1);

      byte[] xpDOSG_Avatar = Utils.CreateBlob(13000);
      cmd.Parameters.AddWithValue("?name", "Ceci est un nom");

      cmd.Parameters.AddWithValue("?desc", "Ceci est une description facile à plantouiller");
      cmd.Parameters.AddWithValue("?avatar", xpDOSG_Avatar);
      cmd.Parameters.AddWithValue("?dt", dt);
      cmd.Prepare();
      int count = cmd.ExecuteNonQuery();
      Assert.AreEqual(1, count);

      cmd.CommandText = "SELECT * FROM Test2";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.AreEqual("Ceci est un nom", reader.GetString(1));
        Assert.AreEqual(dt.ToString("G"), reader.GetDateTime(4).ToString("G"));
        Assert.AreEqual("Ceci est une description facile à plantouiller", reader.GetString(2));

        long len = reader.GetBytes(3, 0, null, 0, 0);
        Assert.AreEqual(xpDOSG_Avatar.Length, len);
        byte[] outBytes = new byte[len];
        reader.GetBytes(3, 0, outBytes, 0, (int)len);

        for (int x = 0; x < xpDOSG_Avatar.Length; x++)
          Assert.AreEqual(xpDOSG_Avatar[x], outBytes[x]);
      }
    }

    [Test]
    public void SimpleTest()
    {
      ExecuteSQL("CREATE TABLE Test (one integer, two integer )");
      ExecuteSQL("INSERT INTO Test VALUES( 1, 2)");
      // create the command and prepare the statement
      IDbCommand cmd = Connection.CreateCommand();
      cmd.CommandText = "SELECT * FROM Test where one = ?p1";
      // create the parameter
      IDbDataParameter p1 = cmd.CreateParameter();
      p1.ParameterName = "?p1";
      p1.DbType = DbType.Int32;
      p1.Precision = (byte)10;
      p1.Scale = (byte)0;
      p1.Size = 4;
      cmd.Parameters.Add(p1);
      // prepare the command
      cmd.Prepare();
      // set the parameter value
      p1.Value = 1;
      // Execute the reader
      IDataReader reader = null;
      reader = cmd.ExecuteReader();
      // Fetch the first record
      reader.Read();
      if (reader != null) reader.Close();

    }

    /// <summary>
    /// Bug #13662  	Prepare() truncates accented character input
    /// </summary>
    [Test]
    public void InsertAccentedCharacters()
    {
      ExecuteSQL("CREATE TABLE Test (id INT UNSIGNED NOT NULL PRIMARY KEY " +
       "AUTO_INCREMENT, input TEXT NOT NULL) CHARACTER SET UTF8");
      // COLLATE " +
      //"utf8_bin");
      using (MySqlConnection conn2 = new MySqlConnection(Connection.ConnectionString +
        ";charset=utf8"))
      {
        conn2.Open();

        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test(input) " +
          "VALUES (?input) ON DUPLICATE KEY UPDATE " +
          "id=LAST_INSERT_ID(id)", conn2);
        cmd.Parameters.Add(new MySqlParameter("?input", ""));
        cmd.Prepare();
        cmd.Parameters[0].Value = "irache martínez@yahoo.es aol.com";
        cmd.ExecuteNonQuery();

        MySqlCommand cmd2 = new MySqlCommand("SELECT input FROM Test", conn2);
        Assert.AreEqual("irache martínez@yahoo.es aol.com",
          cmd2.ExecuteScalar());
      }
    }

    /// <summary>
    /// Bug #13541  	Prepare breaks if a parameter is used more than once
    /// </summary>
    [Test]
    public void UsingParametersTwice()
    {
      ExecuteSQL(@"CREATE TABLE IF NOT EXISTS Test (input TEXT NOT NULL, 
        UNIQUE (input(100)), state INT NOT NULL, score INT NOT NULL)");

      MySqlCommand cmd = new MySqlCommand(@"Insert into Test (input, 
        state, score) VALUES (?input, ?st, ?sc) ON DUPLICATE KEY 
        UPDATE state=state|?st;", Connection);
      cmd.Parameters.Add(new MySqlParameter("?input", ""));
      cmd.Parameters.Add(new MySqlParameter("?st", Convert.ToInt32(0)));
      cmd.Parameters.Add(new MySqlParameter("?sc", Convert.ToInt32(0)));
      cmd.Prepare();

      cmd.Parameters["?input"].Value = "test";
      cmd.Parameters["?st"].Value = 1;
      cmd.Parameters["?sc"].Value = 42;
      int result = cmd.ExecuteNonQuery();
      Assert.AreEqual(1, result);

      MySqlCommand cmd2 = new MySqlCommand("SELECT * FROM Test", Connection);
      using (var reader = cmd2.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.AreEqual("test", reader.GetString("input"));
        Assert.AreEqual(1, reader.GetValue(reader.GetOrdinal("state")));
        Assert.AreEqual(42, reader.GetValue(reader.GetOrdinal("score")));

        Assert.False(reader.Read());
      }
    }

    /// <summary>
    /// Bug #19261  	Supplying Input Parameters
    /// </summary>
    [Test]
    public void MoreParametersOutOfOrder()
    {
      ExecuteSQL("CREATE TABLE `Test` (`BlockListID` int(11) NOT NULL auto_increment, " +
      "`SubscriberID` int(11) NOT NULL, `Phone` varchar(50) default NULL, " +
      "`ContactID` int(11) default NULL, " +
      "`AdminJunk` tinyint(1) NOT NULL default '0', " +
      "PRIMARY KEY  (`BlockListID`), KEY `SubscriberID` (`SubscriberID`))");

      IDbCommand cmd = Connection.CreateCommand();
      cmd.CommandText = "INSERT INTO `Test`(`SubscriberID`,`Phone`,`ContactID`, " +
        "`AdminJunk`) VALUES (?SubscriberID,?Phone,?ContactID, ?AdminJunk);";

      MySqlParameter oParameterSubscriberID = new MySqlParameter();
      oParameterSubscriberID.ParameterName = "?SubscriberID";
      oParameterSubscriberID.DbType = DbType.Int32;
      oParameterSubscriberID.Value = 1;

      MySqlParameter oParameterPhone = new MySqlParameter();
      oParameterPhone.ParameterName = "?Phone";
      oParameterPhone.DbType = DbType.String;
      oParameterPhone.Value = DBNull.Value;

      MySqlParameter oParameterContactID = new MySqlParameter();
      oParameterContactID.ParameterName = "?ContactID";
      oParameterContactID.DbType = DbType.Int32;
      oParameterContactID.Value = DBNull.Value;

      MySqlParameter oParameterAdminJunk = new MySqlParameter();
      oParameterAdminJunk.ParameterName = "?AdminJunk";
      oParameterAdminJunk.DbType = DbType.Boolean;
      oParameterAdminJunk.Value = true;

      cmd.Parameters.Add(oParameterSubscriberID);
      cmd.Parameters.Add(oParameterPhone);
      cmd.Parameters.Add(oParameterAdminJunk);
      cmd.Parameters.Add(oParameterContactID);

      cmd.Prepare();
      int cnt = cmd.ExecuteNonQuery();
      Assert.AreEqual(1, cnt);
    }

    /// <summary>
    /// Bug #16627 Index and length must refer to a location within the string." when executing c
    /// </summary>
    [Test]
    public void ParameterLengths()
    {
      ExecuteSQL("CREATE TABLE Test (id int, name VARCHAR(255))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?name)", Connection);
      cmd.Parameters.Add("?id", MySqlDbType.Int32);
      cmd.Parameters.Add("?name", MySqlDbType.VarChar);
      cmd.Parameters[1].Size = 255;
      cmd.Prepare();

      cmd.Parameters[0].Value = 1;
      cmd.Parameters[1].Value = "short string";
      cmd.ExecuteNonQuery();

      MySqlCommand cmd2 = new MySqlCommand("SELECT * FROM Test", Connection);
      using (var reader = cmd2.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.AreEqual(1, reader.GetValue(reader.GetOrdinal("id")));
        Assert.AreEqual("short string", reader.GetString("name"));

        Assert.False(reader.Read());
      }
    }

    /// <summary>
    /// Bug #18570  	Unsigned tinyint (NET byte) incorrectly determined param type from param val
    /// </summary>
    [Test]
    public void UnsignedTinyInt()
    {
      ExecuteSQL("CREATE TABLE Test(ID TINYINT UNSIGNED NOT NULL, " +
      "Name VARCHAR(50) NOT NULL,	PRIMARY KEY (ID), UNIQUE (ID), " +
      "UNIQUE (Name))");
      ExecuteSQL("INSERT INTO Test VALUES ('127', 'name1')");
      ExecuteSQL("INSERT INTO Test VALUES ('128', 'name2')");
      ExecuteSQL("INSERT INTO Test VALUES ('255', 'name3')");

      string sql = " SELECT count(*) FROM Test WHERE ID = ?id";

      MySqlCommand command = new MySqlCommand();
      command.CommandText = sql;
      command.CommandType = CommandType.Text;
      command.Connection = (MySqlConnection)Connection;

      command.Parameters.AddWithValue("?id", (byte)127);
      command.Prepare();
      int count = Convert.ToInt32(command.ExecuteScalar());
      Assert.AreEqual(1, count);

      command.Parameters.Clear();
      command.Parameters.AddWithValue("?id", (byte)128);
      count = Convert.ToInt32(command.ExecuteScalar());
      Assert.AreEqual(1, count);

      command.Parameters.Clear();
      command.Parameters.AddWithValue("?id", (byte)255);
      count = Convert.ToInt32(command.ExecuteScalar());
      Assert.AreEqual(1, count);

      command.Parameters.Clear();
      command.Parameters.AddWithValue("?id", "255");
      count = Convert.ToInt32(command.ExecuteScalar());
      Assert.AreEqual(1, count);
    }

    /// <summary>
    /// Bug #16934 Unsigned values > 2^63 (UInt64) cannot be used in prepared statements
    /// </summary>
    [Test]
    public void UnsignedValues()
    {
      ExecuteSQL("CREATE TABLE Test (ulVal BIGINT UNSIGNED, lVal INT UNSIGNED, " +
        "mVal MEDIUMINT UNSIGNED, sVal SMALLINT UNSIGNED)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?ulVal, " +
        "?lVal, ?mVal, ?sVal)", Connection);
      cmd.Parameters.Add("?ulVal", MySqlDbType.UInt64);
      cmd.Parameters.Add("?lVal", MySqlDbType.UInt32);
      cmd.Parameters.Add("?mVal", MySqlDbType.UInt24);
      cmd.Parameters.Add("?sVal", MySqlDbType.UInt16);
      cmd.Prepare();
      cmd.Parameters[0].Value = UInt64.MaxValue;
      cmd.Parameters[1].Value = UInt32.MaxValue;
      cmd.Parameters[2].Value = 16777215;
      cmd.Parameters[3].Value = UInt16.MaxValue;
      Assert.AreEqual(1, cmd.ExecuteNonQuery());
      cmd.CommandText = "SELECT * FROM Test";
      cmd.CommandType = CommandType.Text;
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual(UInt64.MaxValue, reader.GetUInt64(0));
        Assert.AreEqual(UInt32.MaxValue, reader.GetUInt32(1));
        Assert.AreEqual(16777215, Convert.ToInt32(reader.GetUInt32(2)));
        Assert.AreEqual(UInt16.MaxValue, reader.GetUInt16(3));
      }
    }

    [Test]
    /// <summary>
    /// Bug #29959124 PREPARED COMMANDS EXECUTE WITH ERROR ON MYSQL SERVER
    /// Above bug, was introduced in ConnectorNet as result of change on MySQL Server
    /// Released in version 8.0.13, fixing Bug#27591525 - commit 69e990f35449bbc493ae9df2b2ed83ac62ed1720
    /// </summary>
    public void PreparedStmtJsonParamBug()
    {
      if (Version < new Version(8, 0)) return;
      ExecuteSQL(@"CREATE TABLE `example` (
      `one` varchar(26) NOT NULL,
      `two` int(1) NOT NULL,
      `three` int(1) NOT NULL,
      `four` tinyint(1) NOT NULL,
      `five` json NOT NULL,
      `six` datetime DEFAULT NULL,
      PRIMARY KEY(`one`)
      ) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci");

      ExecuteSQL(@"INSERT INTO `example` (`one`, `two`, `three`, `four`, `five`, `six`)
      VALUES ('test', '9', '8', '0', '{""name"":""test""}', '2018-07-09 22:30:13')");

      using (var cmd = new MySqlCommand("UPDATE example SET two = @Two, three = @Three, four = @Four, five = @Five, six = @Six WHERE one = @One", Connection))
      {
        cmd.Parameters.AddWithValue("@Two", 0).MySqlDbType = MySqlDbType.Int32;
        cmd.Parameters.AddWithValue("@Three", 0).MySqlDbType = MySqlDbType.Int32;
        cmd.Parameters.AddWithValue("@Four", false).MySqlDbType = MySqlDbType.Byte;
        cmd.Parameters.AddWithValue("@Five", "[]").MySqlDbType = MySqlDbType.JSON;
        cmd.Parameters.AddWithValue("@Six", DateTime.Now).MySqlDbType = MySqlDbType.DateTime;
        cmd.Parameters.AddWithValue("@One", "test").MySqlDbType = MySqlDbType.VarChar;
        cmd.Prepare();
        var result = cmd.ExecuteNonQuery();
        Assert.AreEqual(1, result);
      }
    }

    /// <summary>
    /// Bug #18391 Better error handling for the .NET class "MySqlCommand" needed. 
    /// </summary>
    [Test]
    public void PrepareEmptyString()
    {
      MySqlCommand cmd = new MySqlCommand("", Connection);
      cmd.Prepare();
      Exception ex = Assert.Throws<InvalidOperationException>(() => cmd.ExecuteNonQuery());
      Assert.AreEqual("The CommandText property has not been properly initialized.", ex.Message);
    }

    /// <summary>
    /// Bug #14115 Prepare() with compound statements breaks 
    /// </summary>
    [Test]
    public void CompoundStatements()
    {
      ExecuteSQL("CREATE TABLE IF NOT EXISTS Test (" +
      "id INT UNSIGNED NOT NULL PRIMARY KEY AUTO_INCREMENT," +
      "test1 INT UNSIGNED, test2 INT UNSIGNED)");

      MySqlCommand cmd = Connection.CreateCommand();
      cmd.CommandText = "INSERT INTO Test VALUES (NULL, ?t1, ?t2);" +
        "SELECT last_insert_id()";
      cmd.Parameters.Add("?t1", MySqlDbType.Int32);
      cmd.Parameters.Add("?t2", MySqlDbType.Int32);
      Exception ex = Assert.Throws<MySqlException>(() => cmd.Prepare());
      StringAssert.Contains("You have an error in your SQL syntax; check the manual that corresponds to your MySQL server version", ex.Message);
    }

    [Test]
    public void SchemaOnly()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, name VARCHAR(50))");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
      {
        reader.Read();
      }
    }

    private int GetPreparedStatementCount()
    {
      MySqlCommand cmd = new MySqlCommand("SHOW GLOBAL STATUS LIKE 'Prepared%'", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string s = reader.GetString(1);
        return Int32.Parse(s);
      }
    }

    [Test]
    public void ClosingCommandsProperly()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, name VARCHAR(50))");

      string connStr = Connection.ConnectionString;
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        int initialCount = GetPreparedStatementCount();

        for (int i = 0; i < 10; i++)
        {
          using (MySqlCommand cmd =
            new MySqlCommand("INSERT INTO Test VALUES (?id, ?name)", c))
          {
            cmd.Parameters.Add("?id", MySqlDbType.Int32);
            cmd.Parameters.Add("?name", MySqlDbType.VarChar);
            cmd.Prepare();
            cmd.Parameters[0].Value = i;
            cmd.Parameters[1].Value = "foobar";
            cmd.ExecuteNonQuery();
          }
        }
        c.Ping();
        Assert.AreEqual(initialCount, GetPreparedStatementCount());
      }
    }

    /// <summary>
    /// Bug #37968 Prepared statements byte/tinyint causes data corruption.
    /// </summary>
    [Test]
    public void InsertingUnsignedTinyInt()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL(@"CREATE TABLE Test(id TINYINT UNSIGNED NOT NULL, 
        id2 INT UNSIGNED, id3 TINYINT UNSIGNED, id4 INT UNSIGNED NOT NULL)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?id2, ?id3, ?id4)", Connection);
      cmd.Parameters.Add("?id", MySqlDbType.UByte);
      cmd.Parameters.Add("?id2", MySqlDbType.UByte);
      cmd.Parameters.Add("?id3", MySqlDbType.UByte);
      cmd.Parameters.Add("?id4", MySqlDbType.UByte);
      cmd.Prepare();

      cmd.Parameters[0].Value = 127;
      cmd.Parameters[1].Value = 1;
      cmd.Parameters[2].Value = 2;
      cmd.Parameters[3].Value = 3;
      cmd.ExecuteNonQuery();

      MySqlCommand cmd2 = new MySqlCommand("SELECT * FROM Test", Connection);
      using (var reader = cmd2.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.AreEqual(127, Convert.ToInt32(reader.GetValue(0)));
        Assert.AreEqual(1, Convert.ToInt32(reader.GetValue(1)));
        Assert.AreEqual(2, Convert.ToInt32(reader.GetValue(2)));
        Assert.AreEqual(3, Convert.ToInt32(reader.GetValue(3)));

        Assert.False(reader.Read());
      }
    }

    /// <summary>
    /// Bug #39275	Inserting negative time value through the use of MySqlParameter throws exception
    /// Bug #39294	Reading negative time value greater than -01:00:00 return positive value
    /// </summary>
    [Test]
    public void NegativeTimePrepared()
    {
      NegativeTime(true);
      ReadNegativeTime(true);
    }

    /// <summary>
    /// Bug #39275	Inserting negative time value through the use of MySqlParameter throws exception
    /// Bug #39294	Reading negative time value greater than -01:00:00 return positive value
    /// </summary>
    [Test]
    public void NegativeTimeNonPrepared()
    {
      NegativeTime(false);
      ReadNegativeTime(false);
    }

    internal void NegativeTime(bool prepared)
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL(@"CREATE TABLE Test(id int, t time)");

      MySqlCommand cmd = new MySqlCommand(@"INSERT INTO Test VALUES (1, @t)", Connection);
      cmd.Parameters.Add("@t", MySqlDbType.Time);

      TimeSpan[] times = new TimeSpan[8] {
        new TimeSpan(-10, 0, 0), new TimeSpan(2, -5, 10, 20),
        new TimeSpan(20, -10, 10), new TimeSpan(0, -15, 25),
        new TimeSpan(-4, -10, 20, -10), new TimeSpan(3, 17, 23, 6),
        new TimeSpan(-1,-2,-3,-4), new TimeSpan(0,0,0,-15) };
      if (prepared)
        cmd.Prepare();
      foreach (TimeSpan ts in times)
      {
        cmd.Parameters[0].Value = ts;
        cmd.ExecuteNonQuery();
      }

      cmd.CommandText = "SELECT * FROM Test";
      cmd.Parameters.Clear();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        foreach (TimeSpan ts in times)
        {
          reader.Read();
          TimeSpan t = reader.GetTimeSpan(1);
          Assert.AreEqual(ts, t);
        }
      }
    }

    private void ReadNegativeTime(bool prepared)
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test(id int, t time)");
      ExecuteSQL("INSERT INTO Test VALUES (1, '-00:10:00')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      if (prepared)
        cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        TimeSpan ts = reader.GetTimeSpan(1);
        Assert.AreEqual(0, ts.Hours);
        Assert.AreEqual(-10, ts.Minutes);
        Assert.AreEqual(0, ts.Seconds);
      }
    }

    /// <summary>
    /// Bug #28383726 00:00:00 IS CONVERTED TO NULL WITH PREPARED COMMAND
    /// </summary>
    [Test]
    public void ZeroTimePrepared()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL(@"CREATE TABLE Test(id int, t time NOT NULL)");
      ExecuteSQL(@"INSERT INTO Test VALUES(1, 0)");

      MySqlCommand cmd = new MySqlCommand(@"SELECT t FROM Test", Connection);
      cmd.Prepare();

      using (var reader = cmd.ExecuteReader())
      {
        reader.Read();
        var t = reader.GetValue(0);
        Assert.AreEqual("00:00:00", t.ToString());

        TimeSpan timeSpan = reader.GetTimeSpan(0);
        Assert.AreEqual(0, timeSpan.Hours);
        Assert.AreEqual(0, timeSpan.Minutes);
        Assert.AreEqual(0, timeSpan.Seconds);
      }
    }


    [Test]
    public void SprocOutputParams()
    {
      ExecuteSQL("CREATE PROCEDURE spOutTest(id INT, OUT age INT) BEGIN SET age=id; END");

      MySqlCommand cmd = new MySqlCommand("spOutTest", Connection);
      cmd.Parameters.Add("@id", MySqlDbType.Int32);
      cmd.Parameters.Add("@age", MySqlDbType.Int32).Direction = ParameterDirection.Output;
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Prepare();

      cmd.Parameters[0].Value = 20;
      Assert.AreEqual(0, cmd.ExecuteNonQuery());
      Assert.AreEqual(20, cmd.Parameters[1].Value);

      ExecuteSQL("DROP PROCEDURE IF EXISTS spOutTest");
      ExecuteSQL("CREATE PROCEDURE spOutTest(id INT, OUT age INT) BEGIN SET age=age*2; END");

      cmd.Parameters[0].Value = 1;
      cmd.Parameters[1].Value = 20;
      Assert.AreEqual(0, cmd.ExecuteNonQuery());

      Assert.IsInstanceOf<DBNull>(cmd.Parameters[1].Value);
    }


    [Test]
    public void SprocInputOutputParams()
    {
      ExecuteSQL("CREATE PROCEDURE spInOutTest(id INT, INOUT age INT) BEGIN SET age=age*2; END");

      MySqlCommand cmd = new MySqlCommand("spInOutTest", Connection);
      cmd.Parameters.Add("@id", MySqlDbType.Int32);
      cmd.Parameters.Add("@age", MySqlDbType.Int32).Direction = ParameterDirection.InputOutput;
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Prepare();

      cmd.Parameters[0].Value = 1;
      cmd.Parameters[1].Value = 20;
      Assert.AreEqual(0, cmd.ExecuteNonQuery());
      Assert.AreEqual(40, cmd.Parameters[1].Value);
    }

    /// <summary>
    /// Bug #49794	MySqlDataReader.GetUInt64 doesn't work for large BIGINT UNSIGNED
    /// </summary>
    [Test]
    public void BigIntUnsigned()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test");
      ExecuteSQL(@"CREATE TABLE test(id int(10) unsigned NOT NULL, testValue bigint(20) unsigned NOT NULL,
            PRIMARY KEY  USING BTREE (Id)) ENGINE=InnoDB DEFAULT CHARSET=latin1");
      ExecuteSQL("INSERT INTO test(Id,TestValue) VALUES(1, 3000000000)");

      MySqlCommand cmd = new MySqlCommand("SELECT testValue FROM test WHERE id=@id", Connection);
      cmd.Parameters.Add("@id", MySqlDbType.UInt32);
      cmd.Prepare();

      cmd.Parameters["@id"].Value = 1;
      using (MySqlDataReader rdr = cmd.ExecuteReader())
      {
        rdr.Read();
        UInt64 v = rdr.GetUInt64(0);
        Assert.AreEqual(3000000000, v);
      }
    }

    /// <summary>
    /// Server Bug
    /// Bug #31667061	- INCONSISTENT BEHAVIOR OF @@SQL_SELECT_LIMIT WITH PREPARED STATEMENTS
    /// </summary>
    [Test]
    public void InconsistentBehaviorForSelectLimit()
    {
      ExecuteSQL("CREATE TABLE Test (id INT)");
      ExecuteSQL("INSERT INTO Test VALUES (1), (2), (3)");
      ExecuteSQL("set @@sql_select_limit=1");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test WHERE id > ?p1", Connection);
      cmd.Parameters.AddWithValue("?p1", 0);
      cmd.Prepare();

      ExecuteSQL("set @@sql_select_limit=DEFAULT");

      using (MySqlDataReader result = cmd.ExecuteReader())
      {
        int rows = 0;
        while (result.Read())
          rows += 1;
        Assert.AreEqual(3, rows);
      }
    }

    /// <summary>
    /// Bug #33827735	["Incorrect arguments to mysqld_stmt_execute" with MySqlDbType.Enum]
    /// </summary>
    [Test]
    public void MySqlDbTypeEnumParameter()
    {
      ExecuteSQL("CREATE TABLE Test(data ENUM('small', 'medium', 'large'));");

      string[] dataEnum = new string[] { "small", "medium", "large" };

      using var command = new MySqlCommand("INSERT INTO Test(data) VALUES (@data),(@data2),(@data3);", Connection);
      var parameter = new MySqlParameter("@data", MySqlDbType.Enum);
      parameter.Value = "medium";
      command.Parameters.Add(parameter);
      parameter = new MySqlParameter("@data2", MySqlDbType.Enum);
      parameter.Value = 1;
      command.Parameters.Add(parameter);
      parameter = new MySqlParameter("@data3", "large");
      command.Parameters.Add(parameter);
      command.Prepare();
      command.ExecuteNonQuery();

      command.CommandText = "SELECT * FROM Test";
      using var reader = command.ExecuteReader();
      while (reader.Read())
        Assert.True(dataEnum.Contains(reader.GetString(0)));
    }
  }
}
