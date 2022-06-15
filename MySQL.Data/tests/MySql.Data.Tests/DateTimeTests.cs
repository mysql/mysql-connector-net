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
using System.Globalization;
using System.Text;

namespace MySql.Data.MySqlClient.Tests
{
  public partial class DateTimeTests : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    [Test]
    public void ConvertZeroDateTime()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      ExecuteSQL("INSERT INTO Test VALUES(1, '0000-00-00', '0000-00-00', " +
        "'00:00:00', NULL)");

      string connStr = Connection.ConnectionString;
      connStr += ";convert zero datetime=yes";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.AreEqual(DateTime.MinValue.Date, reader.GetDateTime(1).Date);
          Assert.AreEqual(DateTime.MinValue.Date, reader.GetDateTime(2).Date);
        }
      }
    }

    [Test]
    public void TestNotAllowZeroDateAndTime()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");
      var sql_mode = MySqlHelper.ExecuteScalar(Connection, "SELECT @@session.sql_mode");
      ExecuteSQL("SET SQL_MODE=''");
      ExecuteSQL("INSERT INTO Test VALUES(1, 'Test', '0000-00-00', '0000-00-00', '00:00:00')");
      ExecuteSQL("INSERT INTO Test VALUES(2, 'Test', '2004-11-11', '2004-11-11', '06:06:06')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());

        MySqlDateTime testDate = reader.GetMySqlDateTime(2);
        Assert.False(testDate.IsValidDateTime, "IsZero is false");

        Exception ex = Assert.Throws<MySqlConversionException>(() => reader.GetValue(2));
        Assert.AreEqual("Unable to convert MySQL date/time value to System.DateTime", ex.Message);

        Assert.True(reader.Read());

        DateTime dt2 = (DateTime)reader.GetValue(2);
        Assert.AreEqual(new DateTime(2004, 11, 11).Date, dt2.Date);
      }

      ExecuteSQL($"SET SQL_MODE = '{sql_mode}'");
    }

    [Test]
    public void DateAdd()
    {
      MySqlCommand cmd = new MySqlCommand("select date_add(?someday, interval 1 hour)",
        Connection);
      DateTime now = DateTime.Now;
      DateTime later = now.AddHours(1);
      later = later.AddMilliseconds(later.Millisecond * -1);
      cmd.Parameters.AddWithValue("?someday", now);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        DateTime dt = reader.GetDateTime(0);
        Assert.AreEqual(later.Date, dt.Date);
        Assert.AreEqual(later.Hour, dt.Hour);
        Assert.AreEqual(later.Minute, dt.Minute);
        Assert.AreEqual(later.Second, dt.Second);
      }
    }

    [Test]
    public void TestZeroDateTimeException()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      ExecuteSQL("INSERT INTO Test (id, d, dt) VALUES (1, '0000-00-00', '0000-00-00 00:00:00')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Exception ex = Assert.Throws<MySqlConversionException>(() => reader.GetDateTime(2));
        Assert.AreEqual("Unable to convert MySQL date/time value to System.DateTime", ex.Message);
      }
    }

    /// <summary>
    /// Bug #8929  	Timestamp values with a date > 10/29/9997 cause problems
    /// </summary>
    [Test]
    public void LargeDateTime()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, dt) VALUES(?id,?dt)", Connection);
      cmd.Parameters.Add(new MySqlParameter("?id", 1));
      cmd.Parameters.Add(new MySqlParameter("?dt", DateTime.Parse("9997-10-29")));
      cmd.ExecuteNonQuery();
      cmd.Parameters[0].Value = 2;
      cmd.Parameters[1].Value = DateTime.Parse("9997-10-30");
      cmd.ExecuteNonQuery();
      cmd.Parameters[0].Value = 3;
      cmd.Parameters[1].Value = DateTime.Parse("9999-12-31");
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT id,dt FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.AreEqual(DateTime.Parse("9997-10-29").Date, reader.GetDateTime(1).Date);
        Assert.True(reader.Read());
        Assert.AreEqual(DateTime.Parse("9997-10-30").Date, reader.GetDateTime(1).Date);
        Assert.True(reader.Read());
        Assert.AreEqual(DateTime.Parse("9999-12-31").Date, reader.GetDateTime(1).Date);
      }
    }

    [Test]
    public void UsingDatesAsStrings()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, dt) VALUES (1, ?dt)", Connection);
      cmd.Parameters.Add("?dt", MySqlDbType.Date);
      cmd.Parameters[0].Value = "2005-03-04";
      cmd.ExecuteNonQuery();

      MySqlCommand cmd2 = new MySqlCommand("SELECT * FROM Test", Connection);
      using (var reader = cmd2.ExecuteReader())
      {
        Assert.True(reader.Read());
        DateTime dt = reader.GetDateTime("dt");
        Assert.AreEqual(2005, dt.Year);
        Assert.AreEqual(3, dt.Month);
        Assert.AreEqual(4, dt.Day);
        Assert.False(reader.Read());
      }
    }

    /// <summary>
    /// Bug #19481 Where clause with datetime throws exception [any warning causes the exception]
    /// </summary>
    [Test]
    public void Bug19481()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test(ID INT NOT NULL AUTO_INCREMENT, " +
        "SATELLITEID VARCHAR(3) NOT NULL, ANTENNAID INT, AOS_TIMESTAMP DATETIME NOT NULL, " +
        "TEL_TIMESTAMP DATETIME, LOS_TIMESTAMP DATETIME, PRIMARY KEY (ID))");
      ExecuteSQL("INSERT INTO Test VALUES (NULL,'224','0','2005-07-24 00:00:00'," +
        "'2005-07-24 00:02:00','2005-07-24 00:22:00')");
      ExecuteSQL("INSERT INTO Test VALUES (NULL,'155','24','2005-07-24 03:00:00'," +
        "'2005-07-24 03:02:30','2005-07-24 03:20:00')");
      ExecuteSQL("INSERT INTO Test VALUES (NULL,'094','34','2005-07-24 09:00:00'," +
        "'2005-07-24 09:00:30','2005-07-24 09:15:00')");
      ExecuteSQL("INSERT INTO Test VALUES (NULL,'224','54','2005-07-24 12:00:00'," +
        "'2005-07-24 12:01:00','2005-07-24 12:33:00')");
      ExecuteSQL("INSERT INTO Test VALUES (NULL,'155','25','2005-07-24 15:00:00'," +
        "'2005-07-24 15:02:00','2005-07-24 15:22:00')");
      ExecuteSQL("INSERT INTO Test VALUES (NULL,'094','0','2005-07-24 17:00:00'," +
        "'2005-07-24 17:02:12','2005-07-24 17:20:00')");
      ExecuteSQL("INSERT INTO Test VALUES (NULL,'224','24','2005-07-24 19:00:00'," +
        "'2005-07-24 19:02:00','2005-07-24 19:27:00')");
      ExecuteSQL("INSERT INTO Test VALUES (NULL,'155','34','2005-07-24 21:00:00'," +
        "'2005-07-24 21:02:33','2005-07-24 21:22:55')");
      ExecuteSQL("INSERT INTO Test VALUES (NULL,'094','55','2005-07-24 23:00:00'," +
        "'2005-07-24 23:00:45','2005-07-24 23:22:23')");

      CultureInfo cultureInfo = new CultureInfo("en-us");
      DateTime date = DateTime.Parse("7/24/2005", cultureInfo);
      StringBuilder sql = new StringBuilder();
      sql.AppendFormat(CultureInfo.InvariantCulture,
        @"SELECT ID, ANTENNAID, TEL_TIMESTAMP, LOS_TIMESTAMP FROM Test 
        WHERE TEL_TIMESTAMP >= '{0}'", date.ToString("u"));
      MySqlCommand cmd = new MySqlCommand(sql.ToString(), Connection);
      using (var reader = cmd.ExecuteReader())
      {
        while (reader.Read()) { }
      }
    }

    /// <summary>
    /// Bug #17736 Selecting a row with with empty date '0000-00-00' results in Read() hanging. 
    /// </summary>
    [Test]
    public void PreparedZeroDateTime()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      ExecuteSQL("INSERT INTO Test VALUES(1, Now(), '0000-00-00', NULL, NULL)");
      MySqlCommand cmd = new MySqlCommand("SELECT d FROM Test WHERE id=?id", Connection);
      cmd.Parameters.AddWithValue("?id", 1);
      cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
      }
    }

    /// <summary>
    /// Bug #32010 Connector return incorrect value when pulling 0 datetime 
    /// </summary>
    [Test]
    public void MySqlDateTimeFormatting()
    {
      DateTime dt = DateTime.Now;
      MySqlDateTime mdt = new MySqlDateTime(dt);
      Assert.AreEqual(dt.ToString(CultureInfo.InvariantCulture), mdt.ToString());
    }

    /// <summary>
    /// Bug #41021	DateTime format incorrect
    /// </summary>
    [Test]
    public void DateFormat()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, ?dt, NULL, NULL, NULL)", Connection);
      cmd.Parameters.AddWithValue("?dt", dt);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT dt FROM Test WHERE DATE_FORMAT(DATE(dt), GET_FORMAT(DATETIME, 'ISO'))=?datefilter";
      cmd.Parameters.Clear();
      cmd.Parameters.AddWithValue("?datefilter", dt.Date);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
      }
    }

    /// <summary>
    /// Bug #28393733 TIME(N) COLUMN LOSES MICROSECONDS WITH PREPARED COMMAND
    /// Bug #31623730	TIME(N) COLUMN DESERIALIZES MILLISECONDS INCORRECTLY WITH PREPARED COMMAND
    /// </summary>
    [Test]
    public void TimeColumnWithMicrosecondsOnPrepare()
    {
      ExecuteSQL(@"DROP TABLE IF EXISTS test_time;
              CREATE TABLE test_time(data TIME(6) NOT NULL);");
      TimeSpan time = new TimeSpan(1, 2, 3, 4, 567);
      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString))
      {
        c.Open();
        using (var cmd = new MySqlCommand(@"INSERT INTO test_time VALUES(@data);", c))
        {
          cmd.Parameters.AddWithValue("@data", time);
          cmd.ExecuteNonQuery();
        }

        using (var command = new MySqlCommand(@"SELECT data FROM test_time", c))
        {
          command.Prepare();
          var result = (TimeSpan)command.ExecuteScalar();
          Assert.True(result.ToString() == "1.02:03:04.5670000");
        }

        ExecuteSQL(@"Delete from test_time;");
        TimeSpan time2 = new TimeSpan(1, 2, 3, 4) + TimeSpan.FromTicks(5600);
        using (var cmd = new MySqlCommand(@"INSERT INTO test_time VALUES(@data);", c))
        {
          cmd.Parameters.AddWithValue("@data", time2);
          cmd.ExecuteNonQuery();
        }

        using (var command = new MySqlCommand(@"SELECT data FROM test_time", c))
        {
          command.Prepare();
          var result = (TimeSpan)command.ExecuteScalar();
          Assert.True(result.ToString() == "1.02:03:04.0005600");
        }

      }
    }

    #region DateTimeTypeTests

    [Test]
    public void CanUpdateMicroseconds()
    {
      if (Version < new Version(5, 6)) return;
      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand();

      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(6), d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      cmd.Connection = Connection;
      cmd.CommandText = "INSERT INTO Test VALUES(1, ?dt, NULL, NULL, NULL)";
      cmd.Parameters.AddWithValue("?dt", dt);
      cmd.ExecuteNonQuery();

      //Update value
      cmd.Parameters.Clear();
      cmd.Connection = Connection;
      cmd.CommandText = "UPDATE Test SET dt=?dt";
      cmd.Parameters.Add(new MySqlParameter("?dt", "2011-01-01 12:34:56.123456"));
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT dt FROM Test";
      cmd.Parameters.Clear();
      cmd.Connection = Connection;

      MySqlDataReader rdr = cmd.ExecuteReader();

      while (rdr.Read())
      {
        Assert.AreEqual("12:34:56.123456", rdr.GetDateTime(0).ToString("hh:mm:ss.ffffff"));
      }
      rdr.Close();
    }

    #endregion

    [Test]
    public void CanUpdateMicrosecondsWithPrepare()
    {
      if (Version < new Version(5, 6)) return;

      MySqlCommand cmd = new MySqlCommand();

      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString))
      {
        c.Open();

        ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(6), d DATE, " +
          "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

        cmd.Connection = c;
        cmd.CommandText = "INSERT INTO Test VALUES(?id, ?dt, NULL, NULL, NULL)";
        cmd.Parameters.Add(new MySqlParameter("?id", 1));

        MySqlParameter datetimeinsert = new MySqlParameter();
        datetimeinsert.ParameterName = "?dt";
        datetimeinsert.MySqlDbType = MySqlDbType.DateTime;
        datetimeinsert.Value = "2011-01-01 12:34:59.123456";
        cmd.Parameters.Add(datetimeinsert);

        cmd.Prepare();

        cmd.ExecuteNonQuery();

        cmd.Parameters.Clear();

        MySqlParameter datetimepar = new MySqlParameter();
        datetimepar.ParameterName = "?dt";
        datetimepar.MySqlDbType = MySqlDbType.DateTime;
        datetimepar.Value = "1999-01-01 12:34:59.999999";

        cmd.Connection = c;
        cmd.CommandText = "UPDATE Test SET dt=?dt WHERE id =1";
        cmd.Parameters.Add(datetimepar);
        cmd.Prepare();
        cmd.ExecuteNonQuery();



        cmd.CommandText = "SELECT dt FROM Test WHERE id = 1";
        cmd.Parameters.Clear();
        cmd.Connection = c;
        cmd.Prepare();
        MySqlDataReader rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
          Assert.AreEqual("12:34:59.999999", rdr.GetDateTime(0).ToString("hh:mm:ss.ffffff"));
        }
        rdr.Close();
      }
    }

    #region TimeTypeTests

    [Test]
    // reference http://msdn.microsoft.com/en-us/library/system.timespan.frommilliseconds.aspx
    public void CanUpdateMillisecondsUsingTimeType()
    {
      if (Version < new Version(5, 6)) return;
      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand();

      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(6), d DATE, " +
        "t TIME(6), ts TIMESTAMP(6), PRIMARY KEY(id))");

      cmd.Connection = Connection;
      cmd.CommandText = "INSERT INTO Test VALUES(1, NULL, NULL, ?t, NULL)";

      MySqlParameter timeinsert = new MySqlParameter();
      timeinsert.ParameterName = "?t";
      timeinsert.MySqlDbType = MySqlDbType.Time;
      timeinsert.Value = TimeSpan.FromMilliseconds(12345.6);
      cmd.Parameters.Add(timeinsert);
      cmd.ExecuteNonQuery();


      cmd.CommandText = "SELECT Time(t) FROM Test";
      cmd.Parameters.Clear();
      cmd.Connection = Connection;

      using (MySqlDataReader rdr = cmd.ExecuteReader())
      {
        while (rdr.Read())
        {
#if !NETFRAMEWORK
          Assert.AreEqual(345, rdr.GetTimeSpan(0).Milliseconds);
#else
          Assert.AreEqual(346, rdr.GetTimeSpan(0).Milliseconds);
#endif
        }
      }
    }

    [Test]
    [Ignore("Fix this")]
    // reference http://msdn.microsoft.com/en-us/library/system.timespan.frommilliseconds.aspx
    public void CanUpdateMillisecondsUsingTimeTypeOnPrepareStatements()
    {
      if (Version < new Version(5, 6)) return;
      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand();

      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(6), d DATE, " +
        "t TIME(6), ts TIMESTAMP(6), PRIMARY KEY(id))");


      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString))
      {
        c.Open();
        cmd.Connection = c;
        cmd.CommandText = "INSERT INTO Test VALUES(1, NULL, NULL, ?t, NULL)";

        MySqlParameter timeinsert = new MySqlParameter();
        timeinsert.ParameterName = "?t";
        timeinsert.MySqlDbType = MySqlDbType.Time;
        timeinsert.Value = TimeSpan.FromMilliseconds(1.5);
        cmd.Parameters.Add(timeinsert);

        cmd.Prepare();

        cmd.ExecuteNonQuery();


        cmd.CommandText = "SELECT Time(t) FROM Test";
        cmd.Parameters.Clear();
        cmd.Connection = Connection;
        cmd.Prepare();

        MySqlDataReader rdr = cmd.ExecuteReader();
        using (rdr)
        {
          while (rdr.Read())
          {
            Assert.AreEqual(2, rdr.GetTimeSpan(0).Milliseconds);
          }
        }
      }
    }

    #endregion

    #region TimeStampTests
    [Test]
    public void CanUpdateMillisecondsUsingTimeStampType()
    {
      if (Version < new Version(5, 6)) return;
      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand();

      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString))
      {
        c.Open();

        ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(6), d DATE, " +
          "t TIME(6), ts TIMESTAMP(6), PRIMARY KEY(id))");

        cmd.Connection = c;
        cmd.CommandText = "INSERT INTO Test VALUES(1, NULL, NULL, NULL, ?ts)";

        MySqlParameter timeinsert = new MySqlParameter();
        timeinsert.ParameterName = "?ts";
        timeinsert.MySqlDbType = MySqlDbType.Timestamp;
        timeinsert.Value = "2011-01-01 12:34:56.123456";
        cmd.Parameters.Add(timeinsert);

        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT ts FROM Test";
        cmd.Parameters.Clear();
        cmd.Connection = Connection;

        MySqlDataReader rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
          Assert.AreEqual(123456, rdr.GetMySqlDateTime(0).Microsecond);
        }
        rdr.Close();
      }

    }


    [Test]
    public void CanUpdateMillisecondsUsingTimeStampTypeWithPrepare()
    {
      if (Version < new Version(5, 6)) return;
      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand();

      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString))
      {
        c.Open();

        ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(6), d DATE, " +
          "t TIME(6), ts TIMESTAMP(6), PRIMARY KEY(id))");

        cmd.Connection = c;
        cmd.CommandText = "INSERT INTO Test VALUES(1, NULL, NULL, NULL, ?ts)";

        MySqlParameter timeinsert = new MySqlParameter();
        timeinsert.ParameterName = "?ts";
        timeinsert.MySqlDbType = MySqlDbType.Timestamp;
        timeinsert.Value = "2011-01-01 12:34:56.123456";
        cmd.Parameters.Add(timeinsert);

        cmd.Prepare();

        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT ts FROM Test";
        cmd.Parameters.Clear();
        cmd.Connection = Connection;

        using (MySqlDataReader rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.AreEqual(123456, rdr.GetMySqlDateTime(0).Microsecond);
          }
        }
      }
    }
    #endregion

    /// <summary>
    /// Bug #63812	MySqlDateTime.GetDateTime() does not specify Timezone for TIMESTAMP fields
    /// </summary>
    [Test]
    public void TimestampValuesAreLocal()
    {
      ExecuteSQL("CREATE TABLE TimestampValuesAreLocal (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand("INSERT INTO TimestampValuesAreLocal VALUES(1, ?dt, NULL, NULL, CURRENT_TIMESTAMP)", Connection);
      cmd.Parameters.AddWithValue("@dt", dt);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT dt,ts FROM TimestampValuesAreLocal";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        DateTime dt1 = reader.GetDateTime(0);
        DateTime ts = reader.GetDateTime(1);
        Assert.AreEqual(DateTimeKind.Unspecified, dt1.Kind);
        Assert.AreEqual(DateTimeKind.Local, ts.Kind);
      }
    }

    /// <summary>
    /// Bug #66964	TIMESTAMP values are mistakenly represented as DateTime with Kind = Local
    /// </summary>
    [Test]
    public void TimestampCorrectTimezone()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand("select timediff( curtime(), utc_time() )", Connection);
      string s = cmd.ExecuteScalar().ToString();
      int curroffset = int.Parse(s.Substring(0, s.IndexOf(':')));
      string prevTimeZone = "";
      // Ensure timezone is UTC
      if (curroffset != 0)
      {
        cmd.CommandText = "SELECT @@global.time_zone";
        prevTimeZone = cmd.ExecuteScalar().ToString();
        cmd.CommandText = "set @@global.time_zone = '+0:00'";
        cmd.ExecuteNonQuery();
        // Refresh time_zone value
        Connection.Close();
        Connection.Open();
      }
      try
      {
        cmd.CommandText = string.Format("INSERT INTO `{0}`.Test VALUES(1, curdate(), NULL, NULL, current_timestamp())", Connection.Database); ;
        cmd.ExecuteNonQuery();
        cmd.CommandText = string.Format("SELECT dt,ts FROM `{0}`.Test", Connection.Database);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          DateTime ts = reader.GetDateTime(1);
          Assert.AreEqual(DateTimeKind.Utc, ts.Kind);
        }
        // Now set it to non-UTC
        cmd.CommandText = "set @@global.time_zone = '+5:00'";
        cmd.ExecuteNonQuery();
        // Refresh time_zone value
        Connection.Close();
        Connection.Open();
        cmd.CommandText = string.Format("SELECT dt,ts FROM `{0}`.Test", Connection.Database);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          DateTime ts = reader.GetDateTime(1);
          Assert.AreEqual(DateTimeKind.Local, ts.Kind);
        }
      }
      finally
      {
        if (!string.IsNullOrEmpty(prevTimeZone))
        {
          // restore modified time zone if any
          cmd.CommandText = string.Format("set @@global.time_zone = '{0}'", prevTimeZone);
          cmd.ExecuteNonQuery();
          Connection.Close();
          Connection.Open();
        }
      }
    }

    ///<summary>
    /// Bug #13881444 DateTime(3) column definition on 
    /// 5.6.x server is not processing Milliseconds value
    /// correctly
    /// </summary>
    [Test]
    public void CanSaveMillisecondsPrecision3WithPrepare()
    {

      if (Version < new Version(5, 6)) return;
      DateTime dt = new DateTime(2012, 3, 18, 23, 9, 7, 6);
      MySqlCommand cmd = new MySqlCommand();

      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(3), PRIMARY KEY(id))");

      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString))
      {
        c.Open();
        cmd.Connection = c;
        cmd.CommandText = "INSERT INTO Test VALUES(1, ?dt)";
        cmd.Parameters.AddWithValue("?dt", dt);
        cmd.Prepare();
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT dt FROM Test";
        cmd.Parameters.Clear();
        cmd.Connection = Connection;
        MySqlDataReader rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
          Assert.AreEqual("11:09:07.0060", rdr.GetDateTime(0).ToString("hh:mm:ss.ffff"));
        }
        rdr.Close();
      }
    }

    [Test]
    public void CanSaveMillisecondsPrecision3()
    {

      if (Version < new Version(5, 6)) return;
      DateTime dt = new DateTime(2012, 3, 18, 23, 9, 7, 6);
      MySqlCommand cmd = new MySqlCommand();

      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(3), PRIMARY KEY(id))");
      cmd.Connection = Connection;
      cmd.CommandText = "INSERT INTO Test VALUES(1, ?dt)";
      cmd.Parameters.AddWithValue("?dt", dt);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT dt FROM Test";
      cmd.Parameters.Clear();
      cmd.Connection = Connection;
      MySqlDataReader rdr = cmd.ExecuteReader();

      while (rdr.Read())
      {
        Assert.AreEqual("11:09:07.0060", rdr.GetDateTime(0).ToString("hh:mm:ss.ffff"));
      }
      rdr.Close();
    }

    [Test]
    public void CanSaveMicrosecondsPrecision4()
    {

      if (Version < new Version(5, 6)) return;
      DateTime dt = new DateTime(2012, 3, 18, 23, 9, 7, 6);
      MySqlCommand cmd = new MySqlCommand();

      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(4), PRIMARY KEY(id))");
      cmd.Connection = Connection;
      cmd.CommandText = "INSERT INTO Test VALUES(1, ?dt)";
      cmd.Parameters.AddWithValue("?dt", dt);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT dt FROM Test";
      cmd.Parameters.Clear();
      cmd.Connection = Connection;
      MySqlDataReader rdr = cmd.ExecuteReader();

      while (rdr.Read())
      {
        Assert.AreEqual(dt.ToString("hh:mm:ss.ffff"), rdr.GetDateTime(0).ToString("hh:mm:ss.ffff"));
      }
      rdr.Close();
    }

    [Test]
    public void ShowMicrosecondError()
    {
      MySqlCommand cmd = new MySqlCommand();
      cmd.CommandText = "SELECT NOW() + INTERVAL 123456 MICROSECOND";
      cmd.Parameters.Clear();
      cmd.Connection = Connection;
      string date = cmd.ExecuteScalar().ToString();
      DateTime temp;
      Assert.True(DateTime.TryParse(date, out temp));
    }

    /// <summary>
    /// Testing new functionality for Server 5.6 
    /// On WL 5874
    /// </summary>
    [Test]
    public void CanDefineCurrentTimeStampAsDefaultOnDateTime()
    {
      if (Version < new Version(5, 6, 5)) return;
      MySqlCommand cmd = new MySqlCommand();
      cmd.CommandText = " CREATE TABLE t1 (id int, a DATETIME DEFAULT CURRENT_TIMESTAMP );";
      cmd.Parameters.Clear();

      cmd.Connection = Connection;
      var result = cmd.ExecuteNonQuery();

      cmd.CommandText = " INSERT INTO t1 (id) values(1);";
      cmd.ExecuteNonQuery();

      cmd.CommandText = " SELECT a from t1";
      var reader = cmd.ExecuteReader();

      DateTime tempDate = new DateTime();

      while (reader.Read())
      {
        Assert.True(DateTime.TryParse(reader.GetDateTime(0).ToString(), out tempDate));
      }
      reader.Close();
    }

    [Test]
    public void ReadAndWriteMicroseconds()
    {
      if (Version < new Version(5, 6, 5)) return;
      MySqlCommand cmd = new MySqlCommand();
      cmd.CommandText = "CREATE TABLE ReadAndWriteMicroseconds (id int, t3 TIME(3), t6 TIME(6), d6 DATETIME(6));";
      cmd.Connection = Connection;
      var result = cmd.ExecuteNonQuery();

      DateTime milliseconds = new DateTime(1, 1, 1, 15, 45, 23, 123);
      DateTime microseconds = milliseconds.AddTicks(4560);

      cmd.CommandText = "INSERT INTO ReadAndWriteMicroseconds (id, t3, t6, d6) values(1, @t3, @t6, @d6);";
      cmd.Parameters.AddWithValue("t3", new TimeSpan(milliseconds.Ticks));
      cmd.Parameters.AddWithValue("t6", new TimeSpan(microseconds.Ticks));
      cmd.Parameters.AddWithValue("d6", microseconds);
      cmd.ExecuteNonQuery();

      cmd.CommandText = " SELECT * from ReadAndWriteMicroseconds";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.AreEqual(milliseconds.Ticks, reader.GetTimeSpan(1).Ticks);
        Assert.AreEqual(microseconds.Ticks, reader.GetTimeSpan(2).Ticks);
        Assert.AreEqual(microseconds.Ticks, reader.GetDateTime(3).Ticks);
        Assert.AreEqual(microseconds.Millisecond, reader.GetDateTime(3).Millisecond);
        Assert.AreEqual(microseconds.Millisecond, reader.GetMySqlDateTime(3).Millisecond);
        Assert.AreEqual((microseconds.Ticks % 10000000) / 10, reader.GetMySqlDateTime(3).Microsecond);
      }
    }

    [Test]
    public void TimeZoneOffset()
    {
      string timeZone = "-12:00";
      int timeZoneHours = -12;
      if (DateTime.UtcNow.Hour >= 12)
      {
        timeZone = "+13:00";
        timeZoneHours = 13;
      }

      ExecuteSQL(string.Format("SET @@global.time_zone='{0}'", timeZone));

      try
      {
        using (MySqlConnection conn2 = GetConnection())
        {
          Assert.AreEqual(timeZoneHours, conn2.driver.timeZoneOffset);
        }
      }
      finally
      {
        ExecuteSQL("SET @@global.time_zone='SYSTEM'");
      }
    }

    /// <summary>
    /// Bug 28156187 NET/CONNECTOR MYSQLDATAREADER FETCHES WRONG TIMEZONE
    /// </summary>
    [Test]
    public void TimeZoneOffsetUsingReader()
    {
      ExecuteSQL(@"CREATE TABLE `timeZoneOffsetTable` (`id` int(11) unsigned NOT NULL AUTO_INCREMENT,
        `name` varchar(40) DEFAULT NULL, `mytimestampcolumn` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
        PRIMARY KEY(`id`)) ENGINE = InnoDB AUTO_INCREMENT = 1 DEFAULT CHARSET = utf8; ");

      ExecuteSQL("INSERT INTO `timeZoneOffsetTable` (`name`) VALUES ('Name1')");

      using (var conn = GetConnection())
      using (var cmd = new MySqlCommand("SELECT mytimestampcolumn FROM timeZoneOffsetTable;", conn))
      {
        var reader = cmd.ExecuteReader();
        reader.Read();

        var myTimestampSb = (DateTime)reader["mytimestampcolumn"];
        var myTimestampGdt = reader.GetDateTime("mytimestampcolumn");

        Assert.True(myTimestampSb.Kind == myTimestampGdt.Kind);
        Assert.True(conn.driver.timeZoneOffset == ((DateTimeOffset)myTimestampSb).Offset.Hours, $"Driver: {conn.driver.timeZoneOffset}; Sb: {((DateTimeOffset)myTimestampSb).Offset.Hours}");
        Assert.True(conn.driver.timeZoneOffset == ((DateTimeOffset)myTimestampGdt).Offset.Hours);

        reader.Close();
      }
    }

    #region WL14389
    /// <summary>
    ///   Bug 17924388
    /// </summary>
    [Test, Description("MySQL Datetime Milliseconds ")]
    public void MilisecondsWithTimeColumn()
    {

      using (var conn = new MySqlConnection(Settings.ConnectionString))
      {
        var cmd = new MySqlCommand();
        var timeValue = "00:01:32.123456789";
        conn.Open();
        cmd.Connection = conn;
        cmd.CommandText = "DROP TABLE IF EXISTS T";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE T (dt TIME(6));";
        cmd.ExecuteNonQuery();

        cmd.CommandText = $"INSERT INTO T (dt) VALUES('{timeValue}');";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT dt FROM T;";
        cmd.ExecuteNonQuery();

        using (var reader = cmd.ExecuteReader())
        {
          reader.Read();
          var val = reader.GetValue(0);
          StringAssert.StartsWith(timeValue.Substring(0, 14), val.ToString());
        }

        cmd.CommandText = "DROP TABLE IF EXISTS T";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE T (dt TIME(3));";
        cmd.ExecuteNonQuery();

        cmd.CommandText = $"INSERT INTO T (dt) VALUES('{timeValue}');";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT dt FROM T;";
        cmd.ExecuteNonQuery();

        using (var reader = cmd.ExecuteReader())
        {
          reader.Read();
          var val = reader.GetTimeSpan(0);
          StringAssert.StartsWith(timeValue.Substring(0, 12), val.ToString());
        }
      }
    }
    #endregion WL14389

    /// <summary>
    /// Bug #33539844	- Parser for TIME values differ between Plain and Prepared Statements (Server bug)
    /// </summary>
    [Test]
    public void TimeParserForPlainAndPreparedStmts()
    {
      string timeValue = "10-11-12";
      ExecuteSQL("CREATE TABLE Test (t TIME)");

      // Plain statement
      Assert.Throws<MySqlException>(() => ExecuteSQL($"INSERT INTO Test VALUES ('{timeValue}')"));
      // Prepare statement
      using (var cmd = new MySqlCommand("INSERT INTO Test VALUES (?)", Connection))
      {
        cmd.Parameters.AddWithValue("t", timeValue);
        cmd.Prepare();
        Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
      }
    }

    /// <summary>
    /// Bug #24495619	- MySqlDateTime type bug in .NET provider
    /// Added missing implementation of the IConvertible interface
    /// </summary>
    [Test]
    public void IConvertibleImplementation()
    {
      var mySqlDateTime = new MySqlDateTime(DateTime.Now);

      Assert.AreEqual(TypeCode.DateTime, ((IConvertible)mySqlDateTime).GetTypeCode());
      Assert.NotNull(((IConvertible)mySqlDateTime).ToString());
      Assert.NotNull(Convert.ToString(mySqlDateTime));
    }
  }
}
