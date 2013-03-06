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
using System.IO;
using NUnit.Framework;

namespace MySql.Data.MySqlClient.Tests
{
  [TestFixture]
  public class PreparedStatements : BaseTest
  {
    public PreparedStatements()
    {
      csAdditions += ";ignore prepare=false;";
    }

    [Test]
    public void Simple()
    {
      execSQL("CREATE TABLE Test (id INT, dec1 DECIMAL(5,2), name VARCHAR(100))");
      execSQL("INSERT INTO Test VALUES (1, 345.12, 'abcd')");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1,345.12,'abcd')", conn);
      cmd.Prepare();
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(1, reader.GetInt32(0));
        Assert.AreEqual(345.12, reader.GetDecimal(1));
        Assert.AreEqual("abcd", reader.GetString(2));
      }
    }


    [Test]
    public void SimplePrepareBeforeParms()
    {
      execSQL("CREATE TABLE Test (one INTEGER, two INTEGER)");
      execSQL("INSERT INTO Test VALUES (1, 2)");

      // create the command and prepare the statement
      IDbCommand cmd = conn.CreateCommand();
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
        execSQL("CREATE TABLE Test (id INT NOT NULL, d DATE, dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");
      else
        execSQL("CREATE TABLE Test (id INT NOT NULL, d DATE, dt DATETIME, tm TIME(6), ts TIMESTAMP, PRIMARY KEY(id))");
      

      string sql = "INSERT INTO Test VALUES(?id, ?d, ?dt, ?tm, NULL)";
      MySqlCommand cmd = new MySqlCommand(sql, conn);

      DateTime dt = DateTime.Now;
      dt = dt.AddMilliseconds(dt.Millisecond * -1);
      TimeSpan ts = new TimeSpan(8, 11, 44, 56, 501);

      cmd.Parameters.AddWithValue("?id", 1);
      cmd.Parameters.AddWithValue("?d", dt);
      cmd.Parameters.AddWithValue("?dt", dt);
      cmd.Parameters.AddWithValue("?tm", ts);
      cmd.Prepare();
      int count = cmd.ExecuteNonQuery();
      Assert.AreEqual(1, count, "Records affected by insert");

      cmd.CommandText = "SELECT * FROM Test";
      cmd.Prepare();

      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual(1, reader.GetInt32(0), "Id column");
        Assert.AreEqual(dt.Date, reader.GetDateTime(1).Date, "Date column");

        DateTime dt2 = reader.GetDateTime(2);
        Assert.AreEqual(dt.Date, dt2.Date);
        Assert.AreEqual(dt.Hour, dt2.Hour);
        Assert.AreEqual(dt.Minute, dt2.Minute);
        Assert.AreEqual(dt.Second, dt2.Second);

        TimeSpan ts2 = reader.GetTimeSpan(3);
        Assert.AreEqual(ts.Days, ts2.Days);
        Assert.AreEqual(ts.Hours, ts2.Hours);
        Assert.AreEqual(ts.Minutes, ts2.Minutes);
        Assert.AreEqual(ts.Seconds, ts2.Seconds);

        Assert.AreEqual(dt.Date, reader.GetDateTime(4).Date, "Timestamp column");
      }
    }

    [Test]
    public void ResetCommandText()
    {
      execSQL("CREATE TABLE Test (id int, name varchar(100))");
      execSQL("INSERT INTO Test VALUES (1, 'Test')");

      MySqlCommand cmd = new MySqlCommand("SELECT id FROM Test", conn);
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
      execSQL("CREATE TABLE Test (id int NOT NULL AUTO_INCREMENT, " +
          "id2 int NOT NULL, name varchar(50) DEFAULT NULL, " +
          "id3 int DEFAULT NULL, PRIMARY KEY (id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, id2, name, id3) " +
                        "VALUES(?id, ?id2, ?name,?id3)", conn);

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
      execSQL("CREATE TABLE Test (id INT, blob1 LONGBLOB, text1 LONGTEXT)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?blob1, ?text1)", conn);

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
        Assert.IsTrue(reader.Read());
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
      execSQL("CREATE TABLE Test (one integer, two integer, three integer, four integer, five integer, six integer, seven integer)");
      execSQL("INSERT INTO Test VALUES (1, 2, 3, 4, 5, 6, 7)");

      // create the command and prepare the statement
      IDbCommand cmd = conn.CreateCommand();
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
    public void Bug6271()
    {
      if (Version < new Version(4, 1)) return;

      // Create the table again
      execSQL("CREATE TABLE `Test2` (id INT unsigned NOT NULL auto_increment, " +
        "`xpDOSG_Name` text,`xpDOSG_Desc` text, `Avatar` MEDIUMBLOB, `dtAdded` DATETIME, `dtTime` TIMESTAMP, " +
        "PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=latin1");

      string sql = "INSERT INTO `Test2` (`xpDOSG_Name`,`dtAdded`, `xpDOSG_Desc`,`Avatar`, `dtTime`) " +
        "VALUES(?name, ?dt, ?desc, ?Avatar, NULL)";

      MySqlCommand cmd = new MySqlCommand(sql, conn);

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
        Assert.IsTrue(reader.Read());
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
      execSQL("CREATE TABLE Test (one integer, two integer )");
      execSQL("INSERT INTO Test VALUES( 1, 2)");
      // create the command and prepare the statement
      IDbCommand cmd = conn.CreateCommand();
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
      try
      {
        reader = cmd.ExecuteReader();
        // Fetch the first record
        reader.Read();
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

    /// <summary>
    /// Bug #13662  	Prepare() truncates accented character input
    /// </summary>
    [Test]
    public void InsertAccentedCharacters()
    {
      if (Version < new Version(4, 1)) return;

      execSQL("CREATE TABLE Test (id INT UNSIGNED NOT NULL PRIMARY KEY " +
        "AUTO_INCREMENT, input TEXT NOT NULL) CHARACTER SET UTF8");
      // COLLATE " +
      //"utf8_bin");
      using (MySqlConnection conn2 = new MySqlConnection(GetConnectionString(true) +
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
      if (Version < new Version(4, 1)) return;

      execSQL(@"CREATE TABLE IF NOT EXISTS Test (input TEXT NOT NULL, 
				UNIQUE (input(100)), state INT NOT NULL, score INT NOT NULL)");

      MySqlCommand cmd = new MySqlCommand(@"Insert into Test (input, 
				state, score) VALUES (?input, ?st, ?sc) ON DUPLICATE KEY 
				UPDATE state=state|?st;", conn);
      cmd.Parameters.Add(new MySqlParameter("?input", ""));
      cmd.Parameters.Add(new MySqlParameter("?st", Convert.ToInt32(0)));
      cmd.Parameters.Add(new MySqlParameter("?sc", Convert.ToInt32(0)));
      cmd.Prepare();

      cmd.Parameters["?input"].Value = "test";
      cmd.Parameters["?st"].Value = 1;
      cmd.Parameters["?sc"].Value = 42;
      int result = cmd.ExecuteNonQuery();
      Assert.AreEqual(1, result);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.AreEqual(1, dt.Rows.Count);
      Assert.AreEqual("test", dt.Rows[0]["input"]);
      Assert.AreEqual(1, dt.Rows[0]["state"]);
      Assert.AreEqual(42, dt.Rows[0]["score"]);
    }

    /// <summary>
    /// Bug #19261  	Supplying Input Parameters
    /// </summary>
    [Test]
    public void MoreParametersOutOfOrder()
    {
      if (Version < new Version(4, 1)) return;

      execSQL("CREATE TABLE `Test` (`BlackListID` int(11) NOT NULL auto_increment, " +
          "`SubscriberID` int(11) NOT NULL, `Phone` varchar(50) default NULL, " +
          "`ContactID` int(11) default NULL, " +
          "`AdminJunk` tinyint(1) NOT NULL default '0', " +
          "PRIMARY KEY  (`BlackListID`), KEY `SubscriberID` (`SubscriberID`))");

      IDbCommand cmd = conn.CreateCommand();
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
      if (Version < new Version(4, 1)) return;
      execSQL("CREATE TABLE Test (id int, name VARCHAR(255))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?name)", conn);
      cmd.Parameters.Add("?id", MySqlDbType.Int32);
      cmd.Parameters.Add("?name", MySqlDbType.VarChar);
      cmd.Parameters[1].Size = 255;
      cmd.Prepare();

      cmd.Parameters[0].Value = 1;
      cmd.Parameters[1].Value = "short string";
      cmd.ExecuteNonQuery();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.AreEqual(1, dt.Rows.Count);
      Assert.AreEqual(1, dt.Rows[0]["id"]);
      Assert.AreEqual("short string", dt.Rows[0]["name"]);
    }

    /// <summary>
    /// Bug #18570  	Unsigned tinyint (NET byte) incorrectly determined param type from param val
    /// </summary>
    [Test]
    public void UnsignedTinyInt()
    {
      if (Version < new Version(4, 1)) return;

      execSQL("CREATE TABLE Test(ID TINYINT UNSIGNED NOT NULL, " +
        "Name VARCHAR(50) NOT NULL,	PRIMARY KEY (ID), UNIQUE (ID), " +
        "UNIQUE (Name))");
      execSQL("INSERT INTO Test VALUES ('127', 'name1')");
      execSQL("INSERT INTO Test VALUES ('128', 'name2')");
      execSQL("INSERT INTO Test VALUES ('255', 'name3')");

      string sql = " SELECT count(*) FROM Test WHERE ID = ?id";

      MySqlCommand command = new MySqlCommand();
      command.CommandText = sql;
      command.CommandType = CommandType.Text;
      command.Connection = (MySqlConnection)conn;

      command.Parameters.AddWithValue("?id", (byte)127);
      command.Prepare();
      object count = command.ExecuteScalar();
      Assert.AreEqual(1, count);

      command.Parameters.Clear();
      command.Parameters.AddWithValue("?id", (byte)128);
      count = command.ExecuteScalar();
      Assert.AreEqual(1, count);

      command.Parameters.Clear();
      command.Parameters.AddWithValue("?id", (byte)255);
      count = command.ExecuteScalar();
      Assert.AreEqual(1, count);

      command.Parameters.Clear();
      command.Parameters.AddWithValue("?id", "255");
      count = command.ExecuteScalar();
      Assert.AreEqual(1, count);
    }

    /// <summary>
    /// Bug #16934 Unsigned values > 2^63 (UInt64) cannot be used in prepared statements
    /// </summary>
    [Test]
    public void UnsignedValues()
    {
      if (Version < new Version(4, 1)) return;

      execSQL("CREATE TABLE Test (ulVal BIGINT UNSIGNED, lVal INT UNSIGNED, " +
        "mVal MEDIUMINT UNSIGNED, sVal SMALLINT UNSIGNED)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?ulVal, " +
        "?lVal, ?mVal, ?sVal)", conn);
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
        Assert.AreEqual(16777215, reader.GetUInt32(2));
        Assert.AreEqual(UInt16.MaxValue, reader.GetUInt16(3));
      }
    }

    /// <summary>
    /// Bug #18391 Better error handling for the .NET class "MySqlCommand" needed. 
    /// </summary>
    [Test]
    public void PrepareEmptyString()
    {
      try
      {
        MySqlCommand cmd = new MySqlCommand("", conn);
        cmd.Prepare();
        cmd.ExecuteNonQuery();
        Assert.Fail("Should not get here");
      }
      catch (InvalidOperationException)
      {
      }
    }

    /// <summary>
    /// Bug #14115 Prepare() with compound statements breaks 
    /// </summary>
    [Test]
    public void CompoundStatements()
    {
      execSQL("CREATE TABLE IF NOT EXISTS Test (" +
        "id INT UNSIGNED NOT NULL PRIMARY KEY AUTO_INCREMENT," +
        "test1 INT UNSIGNED, test2 INT UNSIGNED)");

      try
      {
        MySqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Test VALUES (NULL, ?t1, ?t2);" +
          "SELECT last_insert_id()";
        cmd.Parameters.Add("?t1", MySqlDbType.Int32);
        cmd.Parameters.Add("?t2", MySqlDbType.Int32);
        cmd.Prepare();
        Assert.Fail("Should not reach here");
      }
      catch (Exception)
      {
      }
    }

    [Test]
    public void SchemaOnly()
    {
      execSQL("CREATE TABLE Test (id INT, name VARCHAR(50))");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
      cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
      {
        reader.Read();
      }
    }

    private int GetPreparedStatementCount()
    {
      MySqlCommand cmd = new MySqlCommand("SHOW GLOBAL STATUS LIKE 'Prepared%'", conn);
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
      if (Version < new Version(5, 0)) return;

      execSQL("CREATE TABLE Test (id INT, name VARCHAR(50))");

      string connStr = GetConnectionString(true) +
        ";ignore prepare=false";
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
      execSQL("DROP TABLE IF EXISTS Test");
      execSQL(@"CREATE TABLE Test(id TINYINT UNSIGNED NOT NULL, 
				id2 INT UNSIGNED, id3 TINYINT UNSIGNED, id4 INT UNSIGNED NOT NULL)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?id2, ?id3, ?id4)", conn);
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

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.AreEqual(1, dt.Rows.Count);
      Assert.AreEqual(127, dt.Rows[0][0]);
      Assert.AreEqual(1, dt.Rows[0][1]);
      Assert.AreEqual(2, dt.Rows[0][2]);
      Assert.AreEqual(3, dt.Rows[0][3]);
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

    public void NegativeTime(bool prepared)
    {
      execSQL("DROP TABLE IF EXISTS Test");
      execSQL(@"CREATE TABLE Test(id int, t time)");

      MySqlCommand cmd = new MySqlCommand(@"INSERT INTO Test VALUES (1, @t)", conn);
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
      execSQL("DROP TABLE IF EXISTS Test");
      execSQL("CREATE TABLE Test(id int, t time)");
      execSQL("INSERT INTO Test VALUES (1, '-00:10:00')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", conn);
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

    [Test]
    public void SprocOutputParams()
    {
      if (Version < new Version(6, 0, 8)) return;

      execSQL("CREATE PROCEDURE spTest(id INT, OUT age INT) BEGIN SET age=id; END");

      MySqlCommand cmd = new MySqlCommand("spTest", conn);
      cmd.Parameters.Add("@id", MySqlDbType.Int32);
      cmd.Parameters.Add("@age", MySqlDbType.Int32).Direction = ParameterDirection.Output;
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Prepare();

      cmd.Parameters[0].Value = 20;
      Assert.AreEqual(0, cmd.ExecuteNonQuery());
      Assert.AreEqual(20, cmd.Parameters[1].Value);

      execSQL("DROP PROCEDURE IF EXISTS spTest");
      execSQL("CREATE PROCEDURE spTest(id INT, OUT age INT) BEGIN SET age=age*2; END");

      cmd.Parameters[0].Value = 1;
      cmd.Parameters[1].Value = 20;
      Assert.AreEqual(0, cmd.ExecuteNonQuery());
      Assert.AreEqual(20, cmd.Parameters[1].Value);
    }

    [Test]
    public void SprocInputOutputParams()
    {
      if (Version < new Version(6, 0, 8)) return;

      execSQL("CREATE PROCEDURE spTest(id INT, INOUT age INT) BEGIN SET age=age*2; END");

      MySqlCommand cmd = new MySqlCommand("spTest", conn);
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
      execSQL("DROP TABLE IF EXISTS test");
      execSQL(@"CREATE TABLE test(id int(10) unsigned NOT NULL, testValue bigint(20) unsigned NOT NULL,
						PRIMARY KEY  USING BTREE (Id)) ENGINE=InnoDB DEFAULT CHARSET=latin1");
      execSQL("INSERT INTO test(Id,TestValue) VALUES(1, 3000000000)");

      MySqlCommand cmd = new MySqlCommand("SELECT testValue FROM test WHERE id=@Id", conn);
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
  }

  #region Configs
#if !CF
  [Category("Compressed")]
  public class PreparedStatementsSocketCompressed : PreparedStatements
  {
    protected override string GetConnectionInfo()
    {
      return String.Format("port={0};compress=true", port);
    }
  }

  [Category("Pipe")]
  public class PreparedStatementsPipe : PreparedStatements
  {
    protected override string GetConnectionInfo()
    {
      return String.Format("protocol=pipe;pipe name={0}", pipeName);
    }
  }

  [Category("Compressed")]
  [Category("Pipe")]
  public class PreparedStatementsPipeCompressed : PreparedStatements
  {
    protected override string GetConnectionInfo()
    {
      return String.Format("protocol=pipe;pipe name={0};compress=true", pipeName);
    }
  }

  [Category("SharedMemory")]
  public class PreparedStatementsSharedMemory : PreparedStatements
  {
    protected override string GetConnectionInfo()
    {
      return String.Format("protocol=memory; shared memory name={0}", memoryName);
    }
  }

  [Category("Compressed")]
  [Category("SharedMemory")]
  public class PreparedStatementsSharedMemoryCompressed : PreparedStatements
  {
    protected override string GetConnectionInfo()
    {
      return String.Format("protocol=memory; shared memory name={0};compress=true", memoryName);
    }
  }
#endif
  #endregion

}
