// Copyright © 2013, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System.Text;
using Xunit;
using MySql.Data.Types;
using System.Data;
using System.Globalization;
using System.Threading;

namespace MySql.Data.MySqlClient.Tests
{
  public partial class DateTimeTests 
  {
    /// <summary>
    /// Bug #9619 Cannot update row using DbDataAdapter when row contains an invalid date 
    /// Bug #15112 MySqlDateTime Constructor 
    /// </summary>
    [Fact]
    public void TestAllowZeroDateTime()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");
      executeSQL("INSERT INTO Test (id, d, dt) VALUES (1, '0000-00-00', '0000-00-00 00:00:00')");

      using (MySqlConnection c = new MySqlConnection(
        Connection.ConnectionString + ";pooling=false;AllowZeroDatetime=true"))
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
          Assert.Equal("Unable to convert MySQL date/time value to System.DateTime", ex.Message);
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
    public void SortingMySqlDateTimes()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      executeSQL("INSERT INTO Test (id, dt) VALUES (1, '2004-10-01')");
      executeSQL("INSERT INTO Test (id, dt) VALUES (2, '2004-10-02')");
      executeSQL("INSERT INTO Test (id, dt) VALUES (3, '2004-11-01')");
      executeSQL("INSERT INTO Test (id, dt) VALUES (4, '2004-11-02')");

      CultureInfo curCulture = Thread.CurrentThread.CurrentCulture;
      CultureInfo curUICulture = Thread.CurrentThread.CurrentUICulture;
      CultureInfo cul = new CultureInfo("en-GB");
      Thread.CurrentThread.CurrentCulture = cul;
      Thread.CurrentThread.CurrentUICulture = cul;

      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString + ";allow zero datetime=yes"))
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
    public void InsertDateTimeValue()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString +
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
    public void DateTimeInDataTable()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, dt DATETIME, d DATE, " +
        "t TIME, ts TIMESTAMP, PRIMARY KEY(id))");

      executeSQL("INSERT INTO Test VALUES(1, Now(), '0000-00-00', NULL, NULL)");

      using (MySqlConnection c = new MySqlConnection(
        Connection.ConnectionString + ";pooling=false;AllowZeroDatetime=true"))
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
  }
}