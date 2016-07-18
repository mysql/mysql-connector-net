// Copyright © 2013, 2015 Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.Types;
using System.Data;
using System.Globalization;
using System.Threading;

namespace MySql.Data.MySqlClient.Tests
{
  public class DateTimeTests : TestBase
  {
    protected TestSetup ts;

    public DateTimeTests(TestSetup setup) : base(setup, "datetimetests")
    {
      ts = setup;
      executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");
    }
    
    [Fact]
    public void ConvertZeroDateTime()
    {
      executeSQL("INSERT INTO Test VALUES(1, '0000-00-00', '0000-00-00', " +
        "'00:00:00', NULL)");

      string connStr = ts.GetConnection(false).ConnectionString;
      connStr += ";convert zero datetime=yes";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.Equal(DateTime.MinValue.Date, reader.GetDateTime(1).Date);
          Assert.Equal(DateTime.MinValue.Date, reader.GetDateTime(2).Date);
        }
      }
    }

    [Fact]
    public void TestNotAllowZerDateAndTime()
    {
      executeSQL("SET SQL_MODE=''");
      executeSQL("INSERT INTO Test VALUES(1, 'Test', '0000-00-00', '0000-00-00', '00:00:00')");
      executeSQL("INSERT INTO Test VALUES(2, 'Test', '2004-11-11', '2004-11-11', '06:06:06')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());

        MySqlDateTime testDate = reader.GetMySqlDateTime(2);
        Assert.False(testDate.IsValidDateTime, "IsZero is false");

        Exception ex = Assert.Throws<MySqlConversionException>(() => reader.GetValue(2));
        Assert.Equal(ex.Message, "Unable to convert MySQL date/time value to System.DateTime");
        
        Assert.True(reader.Read());

        DateTime dt2 = (DateTime)reader.GetValue(2);
        Assert.Equal(new DateTime(2004, 11, 11).Date, dt2.Date);
      }
    }

    [Fact]
    public void DateAdd()
    {
      MySqlCommand cmd = new MySqlCommand("select date_add(?someday, interval 1 hour)",
        connection);
      DateTime now = DateTime.Now;
      DateTime later = now.AddHours(1);
      later = later.AddMilliseconds(later.Millisecond * -1);
      cmd.Parameters.AddWithValue("?someday", now);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        DateTime dt = reader.GetDateTime(0);
        Assert.Equal(later.Date, dt.Date);
        Assert.Equal(later.Hour, dt.Hour);
        Assert.Equal(later.Minute, dt.Minute);
        Assert.Equal(later.Second, dt.Second);
      }
    }

    /// <summary>
    /// Bug #9619 Cannot update row using DbDataAdapter when row contains an invalid date 
    /// Bug #15112 MySqlDateTime Constructor 
    /// </summary>
    [Fact]
    public void TestAllowZeroDateTime()
    {
      executeSQL("TRUNCATE TABLE Test");
      executeSQL("INSERT INTO Test (id, d, dt) VALUES (1, '0000-00-00', '0000-00-00 00:00:00')");

      using (MySqlConnection c = new MySqlConnection(
        connection.ConnectionString + ";pooling=false;AllowZeroDatetime=true"))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", c);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();

          Assert.True(reader.GetValue(1) is MySqlDateTime);
          Assert.True(reader.GetValue(2) is MySqlDateTime);

          Assert.False(reader.GetMySqlDateTime(1).IsValidDateTime);
          Assert.False(reader.GetMySqlDateTime(2).IsValidDateTime);

          Exception ex = Assert.Throws<MySqlConversionException>(() =>reader.GetDateTime(1));
          Assert.Equal(ex.Message, "Unable to convert MySQL date/time value to System.DateTime");
        }

        DataTable dt = new DataTable();
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", c);
        MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
        da.Fill(dt);
        dt.Rows[0]["id"] = 2;
        DataRow row = dt.NewRow();
        row["id"] = 3;
        row["d"] = new MySqlDateTime("2003-9-24");
        row["dt"] = new MySqlDateTime("0000/0/00 00:00:00");
        dt.Rows.Add(row);

        da.Update(dt);

        dt.Clear();
        da.Fill(dt);
        Assert.Equal(2, dt.Rows.Count);
        MySqlDateTime date = (MySqlDateTime)dt.Rows[1]["d"];
        Assert.Equal(2003, date.Year);
        Assert.Equal(9, date.Month);
        Assert.Equal(24, date.Day);
        cb.Dispose();
      }
    }

    [Fact]
    public void InsertDateTimeValue()
    {
      using (MySqlConnection c = new MySqlConnection(connection.ConnectionString +
        ";allow zero datetime=yes"))
      {
        c.Open();
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT id, dt FROM Test", c);
        MySqlCommandBuilder cb = new MySqlCommandBuilder(da);

        DataTable dt = new DataTable();
        dt.Columns.Add(new DataColumn("id", typeof(int)));
        dt.Columns.Add(new DataColumn("dt", typeof(DateTime)));

        da.Fill(dt);

        DateTime now = DateTime.Now;
        DataRow row = dt.NewRow();
        row["id"] = 1;
        row["dt"] = now;
        dt.Rows.Add(row);
        da.Update(dt);

        dt.Clear();
        da.Fill(dt);
        cb.Dispose();

        Assert.Equal(1, dt.Rows.Count);
        Assert.Equal(now.Date, ((DateTime)dt.Rows[0]["dt"]).Date);
      }
    }

    [Fact]
    public void SortingMySqlDateTimes()
    {
      executeSQL("INSERT INTO Test (id, dt) VALUES (1, '2004-10-01')");
      executeSQL("INSERT INTO Test (id, dt) VALUES (2, '2004-10-02')");
      executeSQL("INSERT INTO Test (id, dt) VALUES (3, '2004-11-01')");
      executeSQL("INSERT INTO Test (id, dt) VALUES (4, '2004-11-02')");

      CultureInfo curCulture = Thread.CurrentThread.CurrentCulture;
      CultureInfo curUICulture = Thread.CurrentThread.CurrentUICulture;
      CultureInfo cul = new CultureInfo("en-GB");
      Thread.CurrentThread.CurrentCulture = cul;
      Thread.CurrentThread.CurrentUICulture = cul;

      using (MySqlConnection c = new MySqlConnection(connection.ConnectionString + ";allow zero datetime=yes"))
      {
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT dt FROM Test", c);
        DataTable dt = new DataTable();
        da.Fill(dt);

        DataView dv = dt.DefaultView;
        dv.Sort = "dt ASC";

        Assert.Equal(new DateTime(2004, 10, 1).Date, Convert.ToDateTime(dv[0]["dt"]).Date);
        Assert.Equal(new DateTime(2004, 10, 2).Date, Convert.ToDateTime(dv[1]["dt"]).Date);
        Assert.Equal(new DateTime(2004, 11, 1).Date, Convert.ToDateTime(dv[2]["dt"]).Date);
        Assert.Equal(new DateTime(2004, 11, 2).Date, Convert.ToDateTime(dv[3]["dt"]).Date);

        Thread.CurrentThread.CurrentCulture = curCulture;
        Thread.CurrentThread.CurrentUICulture = curUICulture;
      }
    }

    [Fact]
    public void TestZeroDateTimeException()
    {
      executeSQL("INSERT INTO Test (id, d, dt) VALUES (1, '0000-00-00', '0000-00-00 00:00:00')");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {        
        reader.Read();
        Exception ex = Assert.Throws<MySqlConversionException>(() =>reader.GetDateTime(2));
        Assert.Equal(ex.Message, "Unable to convert MySQL date/time value to System.DateTime");       
      }
    }

    /// <summary>
    /// Bug #8929  	Timestamp values with a date > 10/29/9997 cause problems
    /// </summary>
    [Fact]
    public void LargeDateTime()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, dt) VALUES(?id,?dt)", connection);
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
        Assert.Equal(DateTime.Parse("9997-10-29").Date, reader.GetDateTime(1).Date);
        Assert.True(reader.Read());
        Assert.Equal(DateTime.Parse("9997-10-30").Date, reader.GetDateTime(1).Date);
        Assert.True(reader.Read());
        Assert.Equal(DateTime.Parse("9999-12-31").Date, reader.GetDateTime(1).Date);
      }
    }

    [Fact]
    public void UsingDatesAsStrings()
    {
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test (id, dt) VALUES (1, ?dt)", connection);
      cmd.Parameters.Add("?dt", MySqlDbType.Date);
      cmd.Parameters[0].Value = "2005-03-04";
      cmd.ExecuteNonQuery();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", connection);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);
      DateTime date = (DateTime)dt.Rows[0]["dt"];
      Assert.Equal(2005, date.Year);
      Assert.Equal(3, date.Month);
      Assert.Equal(4, date.Day);
    }

    /// <summary>
    /// Bug #19481 Where clause with datetime throws exception [any warning causes the exception]
    /// </summary>
    [Fact]
    public void Bug19481()
    {
      executeSQL("DROP TABLE Test");
      executeSQL("CREATE TABLE Test(ID INT NOT NULL AUTO_INCREMENT, " +
        "SATELLITEID VARCHAR(3) NOT NULL, ANTENNAID INT, AOS_TIMESTAMP DATETIME NOT NULL, " +
        "TEL_TIMESTAMP DATETIME, LOS_TIMESTAMP DATETIME, PRIMARY KEY (ID))");
      executeSQL("INSERT INTO Test VALUES (NULL,'224','0','2005-07-24 00:00:00'," +
        "'2005-07-24 00:02:00','2005-07-24 00:22:00')");
      executeSQL("INSERT INTO Test VALUES (NULL,'155','24','2005-07-24 03:00:00'," +
        "'2005-07-24 03:02:30','2005-07-24 03:20:00')");
      executeSQL("INSERT INTO Test VALUES (NULL,'094','34','2005-07-24 09:00:00'," +
        "'2005-07-24 09:00:30','2005-07-24 09:15:00')");
      executeSQL("INSERT INTO Test VALUES (NULL,'224','54','2005-07-24 12:00:00'," +
        "'2005-07-24 12:01:00','2005-07-24 12:33:00')");
      executeSQL("INSERT INTO Test VALUES (NULL,'155','25','2005-07-24 15:00:00'," +
        "'2005-07-24 15:02:00','2005-07-24 15:22:00')");
      executeSQL("INSERT INTO Test VALUES (NULL,'094','0','2005-07-24 17:00:00'," +
        "'2005-07-24 17:02:12','2005-07-24 17:20:00')");
      executeSQL("INSERT INTO Test VALUES (NULL,'224','24','2005-07-24 19:00:00'," +
        "'2005-07-24 19:02:00','2005-07-24 19:27:00')");
      executeSQL("INSERT INTO Test VALUES (NULL,'155','34','2005-07-24 21:00:00'," +
        "'2005-07-24 21:02:33','2005-07-24 21:22:55')");
      executeSQL("INSERT INTO Test VALUES (NULL,'094','55','2005-07-24 23:00:00'," +
        "'2005-07-24 23:00:45','2005-07-24 23:22:23')");

      DateTime date = DateTime.Parse("7/24/2005", CultureInfo.GetCultureInfo("en-us"));
      StringBuilder sql = new StringBuilder();
      sql.AppendFormat(CultureInfo.InvariantCulture,
        @"SELECT ID, ANTENNAID, TEL_TIMESTAMP, LOS_TIMESTAMP FROM Test 
        WHERE TEL_TIMESTAMP >= '{0}'", date.ToString("u"));
      MySqlDataAdapter da = new MySqlDataAdapter(sql.ToString(), connection);
      DataSet dataSet = new DataSet();
      da.Fill(dataSet);
    }

    /// <summary>
    /// Bug #17736 Selecting a row with with empty date '0000-00-00' results in Read() hanging. 
    /// </summary>
    [Fact]
    public void PreparedZeroDateTime()
    {
      if (ts.version < new Version(4, 1)) return;

      executeSQL("INSERT INTO Test VALUES(1, Now(), '0000-00-00', NULL, NULL)");
      MySqlCommand cmd = new MySqlCommand("SELECT d FROM Test WHERE id=?id", connection);
      cmd.Parameters.AddWithValue("?id", 1);
      cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
      }
    }

    [Fact]
    public void DateTimeInDataTable()
    {
      executeSQL("INSERT INTO Test VALUES(1, Now(), '0000-00-00', NULL, NULL)");

      using (MySqlConnection c = new MySqlConnection(
        connection.ConnectionString + ";pooling=false;AllowZeroDatetime=true"))
      {
        c.Open();

        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", c);
        MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
        DataTable dt = new DataTable();

        da.Fill(dt);
        DataRow row = dt.NewRow();
        row["id"] = 2;
        row["dt"] = new MySqlDateTime(DateTime.Now);
        row["d"] = new MySqlDateTime(DateTime.Now);
        row["t"] = new TimeSpan(1, 1, 1);
        row["ts"] = DBNull.Value;
        dt.Rows.Add(row);
        da.Update(dt);

        dt.Rows.Clear();
        da.Fill(dt);
        Assert.Equal(2, dt.Rows.Count);
        cb.Dispose();
      }
    }

    /// <summary>
    /// Bug #32010 Connector return incorrect value when pulling 0 datetime 
    /// </summary>
    [Fact]
    public void MySqlDateTimeFormatting()
    {
      DateTime dt = DateTime.Now;
      MySqlDateTime mdt = new MySqlDateTime(dt);
      Assert.Equal(dt.ToString(), mdt.ToString());
    }

    /// <summary>
    /// Bug #41021	DateTime format incorrect
    /// </summary>
    [Fact]
    public void DateFormat()
    {
      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, ?dt, NULL, NULL, NULL)", connection);
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

    #region DateTimeTypeTests

    [Fact]
    public void CanUpdateMicroseconds()
    {
      if (ts.version < new Version(5, 6)) return;
      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand();

      executeSQL("DROP TABLE Test");
      executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(6), d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      cmd.Connection = connection;
      cmd.CommandText = "INSERT INTO Test VALUES(1, ?dt, NULL, NULL, NULL)";
      cmd.Parameters.AddWithValue("?dt", dt);
      cmd.ExecuteNonQuery();

      //Update value
      cmd.Parameters.Clear();
      cmd.Connection = connection;
      cmd.CommandText = "UPDATE Test SET dt=?dt";
      cmd.Parameters.Add(new MySqlParameter("?dt", "2011-01-01 12:34:56.123456"));
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT dt FROM Test";
      cmd.Parameters.Clear();
      cmd.Connection = connection;

      MySqlDataReader rdr = cmd.ExecuteReader();

      while (rdr.Read())
      {
        Assert.Equal("12:34:56.123456", rdr.GetDateTime(0).ToString("hh:mm:ss.ffffff"));
      }
      rdr.Close();
    }

    #endregion

    [Fact]
    public void CanUpdateMicrosecondsWithIgnorePrepareOnFalse()
    {
      if (ts.version < new Version(5, 6)) return;
      MySqlCommand cmd = new MySqlCommand();

      using (MySqlConnection c = new MySqlConnection(connection.ConnectionString + ";ignore prepare=False;"))
      {
        c.Open();

        executeSQL("DROP TABLE Test");
        executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(6), d DATE, " +
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
          Assert.Equal("12:34:59.999999", rdr.GetDateTime(0).ToString("hh:mm:ss.ffffff"));
        }
        rdr.Close();
      }
    }

    #region TimeTypeTests

    [Fact]
    // reference http://msdn.microsoft.com/en-us/library/system.timespan.frommilliseconds.aspx
    public void CanUpdateMillisecondsUsingTimeType()
    {
      if (ts.version < new Version(5, 6)) return;
      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand();

      executeSQL("DROP TABLE Test");
      executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(6), d DATE, " +
        "t TIME(6), ts TIMESTAMP(6), PRIMARY KEY(id))");

      cmd.Connection = connection;
      cmd.CommandText = "INSERT INTO Test VALUES(1, NULL, NULL, ?t, NULL)";

      MySqlParameter timeinsert = new MySqlParameter();
      timeinsert.ParameterName = "?t";
      timeinsert.MySqlDbType = MySqlDbType.Time;
      timeinsert.Value = TimeSpan.FromMilliseconds(12345.6);
      cmd.Parameters.Add(timeinsert);
      cmd.ExecuteNonQuery();


      cmd.CommandText = "SELECT Time(t) FROM Test";
      cmd.Parameters.Clear();
      cmd.Connection = connection;

      MySqlDataReader rdr = cmd.ExecuteReader();

      while (rdr.Read())
      {
        Assert.Equal(346, rdr.GetTimeSpan(0).Milliseconds);
      }
      rdr.Close();
    }

    [Fact]
    // reference http://msdn.microsoft.com/en-us/library/system.timespan.frommilliseconds.aspx
    public void CanUpdateMillisecondsUsingTimeTypeOnPrepareStatements()
    {
      if (ts.version < new Version(5, 6)) return;
      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand();

      executeSQL("DROP TABLE Test");
      executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(6), d DATE, " +
        "t TIME(6), ts TIMESTAMP(6), PRIMARY KEY(id))");


      using (MySqlConnection c = new MySqlConnection(connection.ConnectionString + ";ignore prepare=False;"))
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
        cmd.Connection = connection;
        cmd.Prepare();

        MySqlDataReader rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
          Assert.Equal(2, rdr.GetTimeSpan(0).Milliseconds);
        }
        rdr.Close();
      }
    }

    #endregion

    #region TimeStampTests
    [Fact]
    public void CanUpdateMillisecondsUsingTimeStampType()
    {
      if (ts.version < new Version(5, 6)) return;
      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand();

      using (MySqlConnection c = new MySqlConnection(connection.ConnectionString))
      {
        c.Open();

        executeSQL("DROP TABLE Test");
        executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(6), d DATE, " +
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
        cmd.Connection = connection;

        MySqlDataReader rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
          Assert.Equal(123456, rdr.GetMySqlDateTime(0).Microsecond);
        }
        rdr.Close();
      }

    }


    [Fact]
    public void CanUpdateMillisecondsUsingTimeStampTypeWithPrepare()
    {
      if (ts.version < new Version(5, 6)) return;
      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand();

      using (MySqlConnection c = new MySqlConnection(connection.ConnectionString + ";ignore prepare=False;"))
      {
        c.Open();

        executeSQL("DROP TABLE Test");
        executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(6), d DATE, " +
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
        cmd.Connection = connection;

        using (MySqlDataReader rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.Equal(123456, rdr.GetMySqlDateTime(0).Microsecond);
          }
        }
      }
    }
    #endregion

    /// <summary>
    /// Bug #63812	MySqlDateTime.GetDateTime() does not specify Timezone for TIMESTAMP fields
    /// </summary>
    [Fact]
    public void TimestampValuesAreLocal()
    {
      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES(1, ?dt, NULL, NULL, NULL)", connection);
      cmd.Parameters.AddWithValue("@dt", dt);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT dt,ts FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        DateTime dt1 = reader.GetDateTime(0);
        DateTime ts = reader.GetDateTime(1);
        Assert.Equal(DateTimeKind.Unspecified, dt1.Kind);
        Assert.Equal(DateTimeKind.Local, ts.Kind);
      }
    }

    /// <summary>
    /// Bug #66964	TIMESTAMP values are mistakenly represented as DateTime with Kind = Local
    /// </summary>
    [Fact]
    public void TimestampCorrectTimezone()
    {
      DateTime dt = DateTime.Now;
      MySqlCommand cmd = new MySqlCommand("select timediff( curtime(), utc_time() )", connection);
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
        connection.Close();
        connection.Open();
      }
      try
      {
        cmd.CommandText = string.Format("INSERT INTO `{0}`.Test VALUES(1, curdate(), NULL, NULL, current_timestamp())", connection.Database); ;
        cmd.ExecuteNonQuery();
        cmd.CommandText = string.Format("SELECT dt,ts FROM `{0}`.Test", connection.Database);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          DateTime ts = reader.GetDateTime(1);
          Assert.Equal(ts.Kind, DateTimeKind.Utc);
        }
        // Now set it to non-UTC
        cmd.CommandText = "set @@global.time_zone = '+5:00'";
        cmd.ExecuteNonQuery();
        // Refresh time_zone value
        connection.Close();
        connection.Open();
        cmd.CommandText = string.Format("SELECT dt,ts FROM `{0}`.Test", connection.Database);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          reader.Read();
          DateTime ts = reader.GetDateTime(1);
          Assert.Equal(ts.Kind, DateTimeKind.Local);
        }
      }
      finally
      {
        if (!string.IsNullOrEmpty(prevTimeZone))
        {
          // restore modified time zone if any
          cmd.CommandText = string.Format("set @@global.time_zone = '{0}'", prevTimeZone);
          cmd.ExecuteNonQuery();
          connection.Close();
          connection.Open();
        }
      }
    }

    ///<summary>
    /// Bug #13881444 DateTime(3) column definition on 
    /// 5.6.x server is not processing Milliseconds value
    /// correctly
    /// </summary>
    [Fact]
    public void CanSaveMillisecondsPrecision3WithPrepare()
    {

      if (ts.version < new Version(5, 6)) return;
      DateTime dt = new DateTime(2012, 3, 18, 23, 9, 7, 6);
      MySqlCommand cmd = new MySqlCommand();

      executeSQL("DROP TABLE Test");
      executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(3), PRIMARY KEY(id))");

      using (MySqlConnection c = new MySqlConnection(connection.ConnectionString + ";ignore prepare=False;"))
      {
        c.Open();
        cmd.Connection = c;
        cmd.CommandText = "INSERT INTO Test VALUES(1, ?dt)";
        cmd.Parameters.AddWithValue("?dt", dt);
        cmd.Prepare();
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT dt FROM Test";
        cmd.Parameters.Clear();
        cmd.Connection = connection;
        MySqlDataReader rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
          Assert.Equal("11:09:07.0060", rdr.GetDateTime(0).ToString("hh:mm:ss.ffff"));
        }
        rdr.Close();
      }
    }

    [Fact]
    public void CanSaveMillisecondsPrecision3()
    {

      if (ts.version < new Version(5, 6)) return;
      DateTime dt = new DateTime(2012, 3, 18, 23, 9, 7, 6);
      MySqlCommand cmd = new MySqlCommand();

      executeSQL("DROP TABLE Test");
      executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(3), PRIMARY KEY(id))");
      cmd.Connection = connection;
      cmd.CommandText = "INSERT INTO Test VALUES(1, ?dt)";
      cmd.Parameters.AddWithValue("?dt", dt);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT dt FROM Test";
      cmd.Parameters.Clear();
      cmd.Connection = connection;
      MySqlDataReader rdr = cmd.ExecuteReader();

      while (rdr.Read())
      {
        Assert.Equal("11:09:07.0060", rdr.GetDateTime(0).ToString("hh:mm:ss.ffff"));
      }
      rdr.Close();
    }

    [Fact]
    public void CanSaveMicrosecondsPrecision4()
    {

      if (ts.version < new Version(5, 6)) return;
      DateTime dt = new DateTime(2012, 3, 18, 23, 9, 7, 6);
      MySqlCommand cmd = new MySqlCommand();

      executeSQL("DROP TABLE Test");
      executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME(4), PRIMARY KEY(id))");
      cmd.Connection = connection;
      cmd.CommandText = "INSERT INTO Test VALUES(1, ?dt)";
      cmd.Parameters.AddWithValue("?dt", dt);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT dt FROM Test";
      cmd.Parameters.Clear();
      cmd.Connection = connection;
      MySqlDataReader rdr = cmd.ExecuteReader();

      while (rdr.Read())
      {
        Assert.Equal(dt.ToString("hh:mm:ss.ffff"), rdr.GetDateTime(0).ToString("hh:mm:ss.ffff"));
      }
      rdr.Close();
    }

    [Fact]
    public void ShowMicrosecondError()
    {
      MySqlCommand cmd = new MySqlCommand();
      cmd.CommandText = "SELECT NOW() + INTERVAL 123456 MICROSECOND";
      cmd.Parameters.Clear();
      cmd.Connection = connection;
      string date = cmd.ExecuteScalar().ToString();
      DateTime temp;
      Assert.True(DateTime.TryParse(date, out temp));
    }

#if NET_45_OR_GREATER
    /// <summary>
    /// Testing new functionality for Server 5.6 
    /// On WL 5874
    /// </summary>
    [Fact]
    public void CanDefineCurrentTimeStampAsDefaultOnDateTime()
    {
      if (ts.version < new Version(5, 6, 5)) return;
      MySqlCommand cmd = new MySqlCommand();
      cmd.CommandText = " CREATE TABLE t1 (id int, a DATETIME DEFAULT CURRENT_TIMESTAMP );";
      cmd.Parameters.Clear();

      cmd.Connection = connection;
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
#endif

    [Fact]
    public void ReadAndWriteMicroseconds()
    {
      if (ts.version < new Version(5, 6, 5)) return;
      MySqlCommand cmd = new MySqlCommand();
      cmd.CommandText = "CREATE TABLE t1 (id int, t3 TIME(3), t6 TIME(6), d6 DATETIME(6));";
      cmd.Connection = connection;
      var result = cmd.ExecuteNonQuery();

      DateTime milliseconds = new DateTime(1, 1, 1, 15, 45, 23, 123);
      DateTime microseconds = milliseconds.AddTicks(4560);

      cmd.CommandText = "INSERT INTO t1 (id, t3, t6, d6) values(1, @t3, @t6, @d6);";
      cmd.Parameters.AddWithValue("t3", new TimeSpan(milliseconds.Ticks));
      cmd.Parameters.AddWithValue("t6", new TimeSpan(microseconds.Ticks));
      cmd.Parameters.AddWithValue("d6", microseconds);
      cmd.ExecuteNonQuery();

      cmd.CommandText = " SELECT * from t1";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.Equal(milliseconds.Ticks, reader.GetTimeSpan(1).Ticks);
        Assert.Equal(microseconds.Ticks, reader.GetTimeSpan(2).Ticks);
        Assert.Equal(microseconds.Ticks, reader.GetDateTime(3).Ticks);
        Assert.Equal(microseconds.Millisecond, reader.GetDateTime(3).Millisecond);
        Assert.Equal(microseconds.Millisecond, reader.GetMySqlDateTime(3).Millisecond);
        Assert.Equal((microseconds.Ticks % 10000000) / 10, reader.GetMySqlDateTime(3).Microsecond);
      }
    }

    [Fact]
    public void TimeZoneOffset()
    {
      string timeZone = "-12:00";
      int timeZoneHours = -12;
      if (DateTime.UtcNow.Hour >= 12)
      {
        timeZone = "+13:00";
        timeZoneHours = 13;
      }

      executeSQL(string.Format("SET @@global.time_zone='{0}'", timeZone));

      try
      {
        using (MySqlConnection conn2 = (MySqlConnection)connection.Clone())
        {
          conn2.Open();
          Assert.Equal(timeZoneHours, conn2.driver.timeZoneOffset);
        }
      }
      finally
      {
        executeSQL("SET @@global.time_zone=@@session.time_zone");
      }
    }
  }
}
