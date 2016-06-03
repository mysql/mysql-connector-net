// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.ComponentModel;
using System.Diagnostics;

namespace MySql.Data.MySqlClient.Tests
{
  public class PreparedStatements : SpecialFixtureWithCustomConnectionString
  {
    protected override string OnGetConnectionStringInfo()
    {
      return ";ignore prepare=false;";
    }

    protected override void Dispose(bool disposing)
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");
      base.Dispose(disposing);
    }

    [Fact]
    public void Simple()
    {
      st.execSQL("CREATE TABLE Test (id INT, dec1 DECIMAL(5,2), name VARCHAR(100))");
      st.execSQL("INSERT INTO Test VALUES (1, 345.12, 'abcd')");

      //using (MySqlConnection cnn = new MySqlConnection(conn))
      //{
        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1,345.12,'abcd')", st.conn);
        cmd.Prepare();
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM Test";
        cmd.Prepare();
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.Equal(1, reader.GetInt32(0));
          Assert.Equal((decimal)345.12, reader.GetDecimal(1));
          Assert.Equal("abcd", reader.GetString(2));
        }
      //}
    }


    [Fact]
    public void SimplePrepareBeforeParms()
    {
      st.execSQL("CREATE TABLE Test (one INTEGER, two INTEGER)");
      st.execSQL("INSERT INTO Test VALUES (1, 2)");
      
        // create the command and prepare the statement
        IDbCommand cmd = st.conn.CreateCommand();
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
          Assert.Equal(1, reader.GetInt32(0));
          Assert.Equal(2, reader.GetInt32(1));
        }      
    }

    [Fact]
    public void DateAndTimes()
    {

      if (st.Version < new Version(5, 6))
        st.execSQL("CREATE TABLE Test (id INT NOT NULL, d DATE, dt DATETIME, tm TIME, ts TIMESTAMP, PRIMARY KEY(id))");
      else
        st.execSQL("CREATE TABLE Test (id INT NOT NULL, d DATE, dt DATETIME, tm TIME(6), ts TIMESTAMP, PRIMARY KEY(id))");

      //using (MySqlConnection cnn = new MySqlConnection(conn))
      //{
        string sql = "INSERT INTO Test VALUES(?id, ?d, ?dt, ?tm, NULL)";
        MySqlCommand cmd = new MySqlCommand(sql, st.conn);

        DateTime dt = DateTime.Now;
        dt = dt.AddMilliseconds(dt.Millisecond * -1);
        TimeSpan ts = new TimeSpan(8, 11, 44, 56, 501);

        cmd.Parameters.AddWithValue("?id", 1);
        cmd.Parameters.AddWithValue("?d", dt);
        cmd.Parameters.AddWithValue("?dt", dt);
        cmd.Parameters.AddWithValue("?tm", ts);
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
          Assert.Equal(dt.Date, dt2.Date);
          Assert.Equal(dt.Hour, dt2.Hour);
          Assert.Equal(dt.Minute, dt2.Minute);
          Assert.Equal(dt.Second, dt2.Second);

          TimeSpan ts2 = reader.GetTimeSpan(3);
          Assert.Equal(ts.Days, ts2.Days);
          Assert.Equal(ts.Hours, ts2.Hours);
          Assert.Equal(ts.Minutes, ts2.Minutes);
          Assert.Equal(ts.Seconds, ts2.Seconds);
          Assert.True(dt.Date == reader.GetDateTime(4).Date, "Timestamp column");
        }
      //}
    }

    [Fact]
    public void ResetCommandText()
    {
      st.execSQL("CREATE TABLE Test (id int, name varchar(100))");
      st.execSQL("INSERT INTO Test VALUES (1, 'Test')");
      
      MySqlCommand cmd = new MySqlCommand("SELECT id FROM Test", st.conn);
      cmd.Prepare();
      object o = cmd.ExecuteScalar();
      Assert.Equal(1, o);

      cmd.CommandText = "SELECT name FROM Test";
      cmd.Prepare();
      o = cmd.ExecuteScalar();
      Assert.Equal("Test", o);

    }

    [Fact]
    public void DifferentParameterOrder()
    {
      st.execSQL("CREATE TABLE Test (id int NOT NULL AUTO_INCREMENT, " +
          "id2 int NOT NULL, name varchar(50) DEFAULT NULL, " +
          "id3 int DEFAULT NULL, PRIMARY KEY (id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, id2, name, id3) " +
                        "VALUES(?id, ?id2, ?name,?id3)", st.conn);

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
      Assert.Equal(1, cmd.ExecuteNonQuery());

      cmd.Parameters.Clear();

      id3.Value = DBNull.Value;
      name.Value = DBNull.Value;
      cmd.Parameters.Add(id);
      cmd.Parameters.Add(id2);
      cmd.Parameters.Add(id3);
      cmd.Parameters.Add(name);

      cmd.Prepare();
      Assert.Equal(1, cmd.ExecuteNonQuery());

      cmd.CommandText = "SELECT id3 FROM Test WHERE id=1";
      Assert.Equal(3, cmd.ExecuteScalar());

      cmd.CommandText = "SELECT name FROM Test WHERE id=2";
      Assert.Equal(DBNull.Value, cmd.ExecuteScalar());
    }

    [Fact]
    public void Blobs()
    {
      st.execSQL("CREATE TABLE Test (id INT, blob1 LONGBLOB, text1 LONGTEXT)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?blob1, ?text1)", st.conn);

      byte[] bytes = Utils.CreateBlob(400000);
      string inStr = "This is my text";

      cmd.Parameters.AddWithValue("?id", 1);
      cmd.Parameters.AddWithValue("?blob1", bytes);
      cmd.Parameters.AddWithValue("?text1", inStr);
      cmd.Prepare();
      int count = cmd.ExecuteNonQuery();
      Assert.Equal(1, count);

      cmd.CommandText = "SELECT * FROM Test";
      cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal(bytes.Length, reader.GetBytes(1, 0, null, 0, 0));
        byte[] outBytes = new byte[bytes.Length];
        reader.GetBytes(1, 0, outBytes, 0, bytes.Length);
        for (int x = 0; x < bytes.Length; x++)
          Assert.Equal(bytes[x], outBytes[x]);
        Assert.Equal(inStr, reader.GetString(2));
      }
    }

    [Fact]
    public void SimpleTest2()
    {
      st.execSQL("CREATE TABLE Test (one integer, two integer, three integer, four integer, five integer, six integer, seven integer)");
      st.execSQL("INSERT INTO Test VALUES (1, 2, 3, 4, 5, 6, 7)");

      // create the command and prepare the statement
      IDbCommand cmd = st.conn.CreateCommand();
      cmd.CommandText = "SELECT one, two, three, four, five, six, seven FROM Test";
      cmd.Prepare();
      // Execute the reader
      using (IDataReader reader = cmd.ExecuteReader())
      {
        // Fetch the first record
        reader.Read();

        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal(2, reader.GetInt32(1));
        Assert.Equal(3, reader.GetInt32(2));
        Assert.Equal(4, reader.GetInt32(3));
        Assert.Equal(5, reader.GetInt32(4));
        Assert.Equal(6, reader.GetInt32(5));
        Assert.Equal(7, reader.GetInt32(6));
      }
    }

    [Fact]
    public void Bug6271()
    {
      if (st.Version < new Version(4, 1)) return;

      // Create the table again
      st.execSQL("CREATE TABLE `Test2` (id INT unsigned NOT NULL auto_increment, " +
        "`xpDOSG_Name` text,`xpDOSG_Desc` text, `Avatar` MEDIUMBLOB, `dtAdded` DATETIME, `dtTime` TIMESTAMP, " +
        "PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=latin1");

      string sql = "INSERT INTO `Test2` (`xpDOSG_Name`,`dtAdded`, `xpDOSG_Desc`,`Avatar`, `dtTime`) " +
        "VALUES(?name, ?dt, ?desc, ?Avatar, NULL)";

      MySqlCommand cmd = new MySqlCommand(sql, st.conn);

      DateTime dt = DateTime.Now;
      dt = dt.AddMilliseconds(dt.Millisecond * -1);

      byte[] xpDOSG_Avatar = Utils.CreateBlob(13000);
      cmd.Parameters.AddWithValue("?name", "Ceci est un nom");

      cmd.Parameters.AddWithValue("?desc", "Ceci est une description facile à plantouiller");
      cmd.Parameters.AddWithValue("?avatar", xpDOSG_Avatar);
      cmd.Parameters.AddWithValue("?dt", dt);
      cmd.Prepare();
      int count = cmd.ExecuteNonQuery();
      Assert.Equal(1, count);

      cmd.CommandText = "SELECT * FROM Test2";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.Equal("Ceci est un nom", reader.GetString(1));
        Assert.Equal(dt.ToString("G"), reader.GetDateTime(4).ToString("G"));
        Assert.Equal("Ceci est une description facile à plantouiller", reader.GetString(2));

        long len = reader.GetBytes(3, 0, null, 0, 0);
        Assert.Equal(xpDOSG_Avatar.Length, len);
        byte[] outBytes = new byte[len];
        reader.GetBytes(3, 0, outBytes, 0, (int)len);

        for (int x = 0; x < xpDOSG_Avatar.Length; x++)
          Assert.Equal(xpDOSG_Avatar[x], outBytes[x]);
      }
    }

    [Fact]
    public void SimpleTest()
    {
      st.execSQL("CREATE TABLE Test (one integer, two integer )");
      st.execSQL("INSERT INTO Test VALUES( 1, 2)");
      // create the command and prepare the statement
      IDbCommand cmd = st.conn.CreateCommand();
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
    [Fact]
    public void InsertAccentedCharacters()
    {
      if (st.Version < new Version(4, 1)) return;

       st.execSQL("CREATE TABLE Test (id INT UNSIGNED NOT NULL PRIMARY KEY " +
        "AUTO_INCREMENT, input TEXT NOT NULL) CHARACTER SET UTF8");
      // COLLATE " +
      //"utf8_bin");
      using (MySqlConnection conn2 = new MySqlConnection(st.GetConnectionString(true) +
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
        Assert.Equal("irache martínez@yahoo.es aol.com",
          cmd2.ExecuteScalar());
      }
    }

    /// <summary>
    /// Bug #13541  	Prepare breaks if a parameter is used more than once
    /// </summary>
    [Fact]
    public void UsingParametersTwice()
    {
      if (st.Version < new Version(4, 1)) return;

        st.execSQL(@"CREATE TABLE IF NOT EXISTS Test (input TEXT NOT NULL, 
        UNIQUE (input(100)), state INT NOT NULL, score INT NOT NULL)");

      MySqlCommand cmd = new MySqlCommand(@"Insert into Test (input, 
        state, score) VALUES (?input, ?st, ?sc) ON DUPLICATE KEY 
        UPDATE state=state|?st;", st.conn);
      cmd.Parameters.Add(new MySqlParameter("?input", ""));
      cmd.Parameters.Add(new MySqlParameter("?st", Convert.ToInt32(0)));
      cmd.Parameters.Add(new MySqlParameter("?sc", Convert.ToInt32(0)));
      cmd.Prepare();

      cmd.Parameters["?input"].Value = "test";
      cmd.Parameters["?st"].Value = 1;
      cmd.Parameters["?sc"].Value = 42;
      int result = cmd.ExecuteNonQuery();
      Assert.Equal(1, result);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("test", dt.Rows[0]["input"]);
      Assert.Equal(1, dt.Rows[0]["state"]);
      Assert.Equal(42, dt.Rows[0]["score"]);
    }

    /// <summary>
    /// Bug #19261  	Supplying Input Parameters
    /// </summary>
    [Fact]
    public void MoreParametersOutOfOrder()
    {
      if (st.Version < new Version(4, 1)) return;

          st.execSQL("CREATE TABLE `Test` (`BlackListID` int(11) NOT NULL auto_increment, " +
          "`SubscriberID` int(11) NOT NULL, `Phone` varchar(50) default NULL, " +
          "`ContactID` int(11) default NULL, " +
          "`AdminJunk` tinyint(1) NOT NULL default '0', " +
          "PRIMARY KEY  (`BlackListID`), KEY `SubscriberID` (`SubscriberID`))");

      IDbCommand cmd = st.conn.CreateCommand();
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
      Assert.Equal(1, cnt);
    }

    /// <summary>
    /// Bug #16627 Index and length must refer to a location within the string." when executing c
    /// </summary>
    [Fact]
    public void ParameterLengths()
    {
      if (st.Version < new Version(4, 1)) return;
        st.execSQL("CREATE TABLE Test (id int, name VARCHAR(255))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?name)", st.conn);
      cmd.Parameters.Add("?id", MySqlDbType.Int32);
      cmd.Parameters.Add("?name", MySqlDbType.VarChar);
      cmd.Parameters[1].Size = 255;
      cmd.Prepare();

      cmd.Parameters[0].Value = 1;
      cmd.Parameters[1].Value = "short string";
      cmd.ExecuteNonQuery();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal(1, dt.Rows[0]["id"]);
      Assert.Equal("short string", dt.Rows[0]["name"]);
    }

    /// <summary>
    /// Bug #18570  	Unsigned tinyint (NET byte) incorrectly determined param type from param val
    /// </summary>
    [Fact]
    public void UnsignedTinyInt()
    {
      if (st.Version < new Version(4, 1)) return;

        st.execSQL("CREATE TABLE Test(ID TINYINT UNSIGNED NOT NULL, " +
        "Name VARCHAR(50) NOT NULL,	PRIMARY KEY (ID), UNIQUE (ID), " +
        "UNIQUE (Name))");
      st.execSQL("INSERT INTO Test VALUES ('127', 'name1')");
      st.execSQL("INSERT INTO Test VALUES ('128', 'name2')");
      st.execSQL("INSERT INTO Test VALUES ('255', 'name3')");

      string sql = " SELECT count(*) FROM Test WHERE ID = ?id";

      MySqlCommand command = new MySqlCommand();
      command.CommandText = sql;
      command.CommandType = CommandType.Text;
      command.Connection = (MySqlConnection)st.conn;

      command.Parameters.AddWithValue("?id", (byte)127);
      command.Prepare();
      int count = Convert.ToInt32(command.ExecuteScalar());
      Assert.Equal(1, count);

      command.Parameters.Clear();
      command.Parameters.AddWithValue("?id", (byte)128);
      count = Convert.ToInt32(command.ExecuteScalar());
      Assert.Equal(1, count);

      command.Parameters.Clear();
      command.Parameters.AddWithValue("?id", (byte)255);
      count = Convert.ToInt32(command.ExecuteScalar());
      Assert.Equal(1, count);

      command.Parameters.Clear();
      command.Parameters.AddWithValue("?id", "255");
      count = Convert.ToInt32(command.ExecuteScalar());
      Assert.Equal(1, count);
    }

    /// <summary>
    /// Bug #16934 Unsigned values > 2^63 (UInt64) cannot be used in prepared statements
    /// </summary>
    [Fact]
    public void UnsignedValues()
    {
      if (st.Version < new Version(4, 1)) return;

      st.execSQL("CREATE TABLE Test (ulVal BIGINT UNSIGNED, lVal INT UNSIGNED, " +
        "mVal MEDIUMINT UNSIGNED, sVal SMALLINT UNSIGNED)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?ulVal, " +
        "?lVal, ?mVal, ?sVal)", st.conn);
      cmd.Parameters.Add("?ulVal", MySqlDbType.UInt64);
      cmd.Parameters.Add("?lVal", MySqlDbType.UInt32);
      cmd.Parameters.Add("?mVal", MySqlDbType.UInt24);
      cmd.Parameters.Add("?sVal", MySqlDbType.UInt16);
      cmd.Prepare();
      cmd.Parameters[0].Value = UInt64.MaxValue;
      cmd.Parameters[1].Value = UInt32.MaxValue;
      cmd.Parameters[2].Value = 16777215;
      cmd.Parameters[3].Value = UInt16.MaxValue;
      Assert.Equal(1, cmd.ExecuteNonQuery());
      cmd.CommandText = "SELECT * FROM Test";
      cmd.CommandType = CommandType.Text;
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal(UInt64.MaxValue, reader.GetUInt64(0));
        Assert.Equal(UInt32.MaxValue, reader.GetUInt32(1));
        Assert.Equal(16777215, Convert.ToInt32(reader.GetUInt32(2)));
        Assert.Equal(UInt16.MaxValue, reader.GetUInt16(3));
      }
    }

    /// <summary>
    /// Bug #18391 Better error handling for the .NET class "MySqlCommand" needed. 
    /// </summary>
    [Fact]
    public void PrepareEmptyString()
    {      
      MySqlCommand cmd = new MySqlCommand("", st.conn);
      cmd.Prepare();
      Exception ex = Assert.Throws<InvalidOperationException>(() => cmd.ExecuteNonQuery());
      Assert.Equal(ex.Message, "The CommandText property has not been properly initialized.");      
    }

    /// <summary>
    /// Bug #14115 Prepare() with compound statements breaks 
    /// </summary>
    [Fact]
    public void CompoundStatements()
    {
        st.execSQL("CREATE TABLE IF NOT EXISTS Test (" +
        "id INT UNSIGNED NOT NULL PRIMARY KEY AUTO_INCREMENT," +
        "test1 INT UNSIGNED, test2 INT UNSIGNED)");

      MySqlCommand cmd = st.conn.CreateCommand();
      cmd.CommandText = "INSERT INTO Test VALUES (NULL, ?t1, ?t2);" +
        "SELECT last_insert_id()";
      cmd.Parameters.Add("?t1", MySqlDbType.Int32);
      cmd.Parameters.Add("?t2", MySqlDbType.Int32);
      Exception ex = Assert.Throws<MySqlException>(() => cmd.Prepare());
      Assert.True(ex.Message.Contains("You have an error in your SQL syntax; check the manual that corresponds to your MySQL server version"));      
    }

    [Fact]
    public void SchemaOnly()
    {
      st.execSQL("CREATE TABLE Test (id INT, name VARCHAR(50))");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", st.conn);
      cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
      {
        reader.Read();
      }
    }

    private int GetPreparedStatementCount()
    {
      MySqlCommand cmd = new MySqlCommand("SHOW GLOBAL STATUS LIKE 'Prepared%'", st.conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        string s = reader.GetString(1);
        return Int32.Parse(s);
      }
    }

    [Fact]
    public void ClosingCommandsProperly()
    {
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE TABLE Test (id INT, name VARCHAR(50))");

      string connStr = st.GetConnectionString(true) +
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
        Assert.Equal(initialCount, GetPreparedStatementCount());
      }
    }

    /// <summary>
    /// Bug #37968 Prepared statements byte/tinyint causes data corruption.
    /// </summary>
    [Fact]
    public void InsertingUnsignedTinyInt()
    {
      st.execSQL("DROP TABLE IF EXISTS Test");
      st.execSQL(@"CREATE TABLE Test(id TINYINT UNSIGNED NOT NULL, 
        id2 INT UNSIGNED, id3 TINYINT UNSIGNED, id4 INT UNSIGNED NOT NULL)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?id2, ?id3, ?id4)", st.conn);
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

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal(127, Convert.ToInt32(dt.Rows[0][0]));
      Assert.Equal(1, Convert.ToInt32(dt.Rows[0][1]));
      Assert.Equal(2, Convert.ToInt32(dt.Rows[0][2]));
      Assert.Equal(3, Convert.ToInt32(dt.Rows[0][3]));
    }

    /// <summary>
    /// Bug #39275	Inserting negative time value through the use of MySqlParameter throws exception
    /// Bug #39294	Reading negative time value greater than -01:00:00 return positive value
    /// </summary>
    [Fact]
    public void NegativeTimePrepared()
    {
      NegativeTime(true);
      ReadNegativeTime(true);
    }

    /// <summary>
    /// Bug #39275	Inserting negative time value through the use of MySqlParameter throws exception
    /// Bug #39294	Reading negative time value greater than -01:00:00 return positive value
    /// </summary>
    [Fact]
    public void NegativeTimeNonPrepared()
    {
      NegativeTime(false);
      ReadNegativeTime(false);
    }

    public void NegativeTime(bool prepared)
    {
      st.execSQL("DROP TABLE IF EXISTS Test");
      st.execSQL(@"CREATE TABLE Test(id int, t time)");

      MySqlCommand cmd = new MySqlCommand(@"INSERT INTO Test VALUES (1, @t)", st.conn);
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
          Assert.Equal(ts, t);
        }
      }
    }

    private void ReadNegativeTime(bool prepared)
    {
      st.execSQL("DROP TABLE IF EXISTS Test");
      st.execSQL("CREATE TABLE Test(id int, t time)");
      st.execSQL("INSERT INTO Test VALUES (1, '-00:10:00')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", st.conn);
      if (prepared)
        cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        TimeSpan ts = reader.GetTimeSpan(1);
        Assert.Equal(0, ts.Hours);
        Assert.Equal(-10, ts.Minutes);
        Assert.Equal(0, ts.Seconds);
      }
    }

    [Fact]
    public void SprocOutputParams()
    {
      if (st.Version < new Version(6, 0, 8)) return;

      st.execSQL("CREATE PROCEDURE spTest(id INT, OUT age INT) BEGIN SET age=id; END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
      cmd.Parameters.Add("@id", MySqlDbType.Int32);
      cmd.Parameters.Add("@age", MySqlDbType.Int32).Direction = ParameterDirection.Output;
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Prepare();

      cmd.Parameters[0].Value = 20;
      Assert.Equal(0, cmd.ExecuteNonQuery());
      Assert.Equal(20, cmd.Parameters[1].Value);

      st.execSQL("DROP PROCEDURE IF EXISTS spTest");
      st.execSQL("CREATE PROCEDURE spTest(id INT, OUT age INT) BEGIN SET age=age*2; END");

      cmd.Parameters[0].Value = 1;
      cmd.Parameters[1].Value = 20;
      Assert.Equal(0, cmd.ExecuteNonQuery());
      Assert.Equal(20, cmd.Parameters[1].Value);
    }

    [Fact]
    public void SprocInputOutputParams()
    {
      if (st.Version < new Version(6, 0, 8)) return;

      st.execSQL("CREATE PROCEDURE spTest(id INT, INOUT age INT) BEGIN SET age=age*2; END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
      cmd.Parameters.Add("@id", MySqlDbType.Int32);
      cmd.Parameters.Add("@age", MySqlDbType.Int32).Direction = ParameterDirection.InputOutput;
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Prepare();

      cmd.Parameters[0].Value = 1;
      cmd.Parameters[1].Value = 20;
      Assert.Equal(0, cmd.ExecuteNonQuery());
      Assert.Equal(40, cmd.Parameters[1].Value);
    }

    /// <summary>
    /// Bug #49794	MySqlDataReader.GetUInt64 doesn't work for large BIGINT UNSIGNED
    /// </summary>
    [Fact]
    public void BigIntUnsigned()
    {
      st.execSQL("DROP TABLE IF EXISTS test");
      st.execSQL(@"CREATE TABLE test(id int(10) unsigned NOT NULL, testValue bigint(20) unsigned NOT NULL,
            PRIMARY KEY  USING BTREE (Id)) ENGINE=InnoDB DEFAULT CHARSET=latin1");
      st.execSQL("INSERT INTO test(Id,TestValue) VALUES(1, 3000000000)");

      MySqlCommand cmd = new MySqlCommand("SELECT testValue FROM test WHERE id=@Id", st.conn);
      cmd.Parameters.Add("@id", MySqlDbType.UInt32);
      cmd.Prepare();

      cmd.Parameters["@id"].Value = 1;
      using (MySqlDataReader rdr = cmd.ExecuteReader())
      {
        rdr.Read();
        UInt64 v = rdr.GetUInt64(0);
        Assert.Equal(3000000000, v);
      }
    }
  }
}
