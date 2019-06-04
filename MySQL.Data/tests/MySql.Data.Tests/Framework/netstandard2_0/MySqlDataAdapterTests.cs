// Copyright (c) 2013, 2018, Oracle and/or its affiliates. All rights reserved.
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
using Xunit;
using System.Data;
using System.Threading.Tasks;
using System.Data.Common;
using System.Threading;

namespace MySql.Data.MySqlClient.Tests
{
  public class MySqlDataAdapterTests : TestBase
  {
    public MySqlDataAdapterTests(TestFixture fixture) : base(fixture)
    {
    }

    private void CreateDefaultTable()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL AUTO_INCREMENT, " +
        "id2 INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, " +
        "ts TIMESTAMP, OriginalId INT, PRIMARY KEY(id, id2))");
    }

    [Fact]
    public void TestFill()
    {
      FillImpl(false);
    }

    [Fact]
    public void TestFillPrepared()
    {
      FillImpl(true);
    }

    private void FillImpl(bool prepare)
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test (id, id2, name, dt) VALUES (NULL, 1, 'Name 1', Now())");
      executeSQL("INSERT INTO Test (id, id2, name, dt) VALUES (NULL, 2, NULL, Now())");
      executeSQL("INSERT INTO Test (id, id2, name, dt) VALUES (NULL, 3, '', Now())");

      MySqlDataAdapter da = new MySqlDataAdapter("select * from Test", Connection);
      if (prepare) da.SelectCommand.Prepare();
      DataSet ds = new DataSet();
      da.Fill(ds, "Test");

      Assert.Single(ds.Tables);
      Assert.Equal(3, ds.Tables[0].Rows.Count);

      Assert.Equal(1, ds.Tables[0].Rows[0]["id2"]);
      Assert.Equal(2, ds.Tables[0].Rows[1]["id2"]);
      Assert.Equal(3, ds.Tables[0].Rows[2]["id2"]);

      Assert.Equal("Name 1", ds.Tables[0].Rows[0]["name"]);
      Assert.Equal(DBNull.Value, ds.Tables[0].Rows[1]["name"]);
      Assert.Equal(String.Empty, ds.Tables[0].Rows[2]["name"]);
    }

    [Fact]
    public void TestUpdate()
    {
      CreateDefaultTable();
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);

      DataRow dr = dt.NewRow();
      dr["id2"] = 2;
      dr["name"] = "TestName1";
      dt.Rows.Add(dr);
      int count = da.Update(dt);

      // make sure our refresh of auto increment values worked
      Assert.True(count == 1, "checking insert count");
      Assert.True(dt.Rows[dt.Rows.Count - 1]["id"] != null, "Checking auto increment column");

      dt.Rows.Clear();
      da.Fill(dt);
      dt.Rows[0]["id2"] = 3;
      dt.Rows[0]["name"] = "TestName2";
      dt.Rows[0]["ts"] = DBNull.Value;
      DateTime day1 = new DateTime(2003, 1, 16, 12, 24, 0);
      dt.Rows[0]["dt"] = day1;
      dt.Rows[0]["tm"] = day1.TimeOfDay;
      count = da.Update(dt);

      Assert.True(dt.Rows[0]["ts"] != null, "checking refresh of record");
      Assert.True(dt.Rows[0]["id2"] != null, "checking refresh of primary column");

      dt.Rows.Clear();
      da.Fill(dt);

      Assert.True(count == 1, "checking update count");
      DateTime dateTime = (DateTime)dt.Rows[0]["dt"];
      Assert.True(day1.Date == dateTime.Date, "checking date");
      Assert.True(day1.TimeOfDay == (TimeSpan)dt.Rows[0]["tm"], "checking time");

      dt.Rows[0].Delete();
      count = da.Update(dt);

      Assert.True(count == 1, "checking insert count");

      dt.Rows.Clear();
      da.Fill(dt);
      Assert.True(dt.Rows.Count == 0, "checking row count");
      cb.Dispose();
    }

    [Fact]
    public void OriginalInName()
    {
      CreateDefaultTable();
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      cb.ToString();  // keep the compiler happy
      DataTable dt = new DataTable();
      da.Fill(dt);

      DataRow row = dt.NewRow();
      row["id"] = DBNull.Value;
      row["id2"] = 1;
      row["name"] = "Test";
      row["dt"] = DBNull.Value;
      row["tm"] = DBNull.Value;
      row["ts"] = DBNull.Value;
      row["OriginalId"] = 2;
      dt.Rows.Add(row);
      da.Update(dt);

      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal(2, dt.Rows[0]["OriginalId"]);
    }

    [Fact]
    public void UseAdapterPropertyOfCommandBuilder()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 1, 'Test')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder();
      cb.DataAdapter = da;

      DataTable dt = new DataTable();
      da.Fill(dt);

      dt.Rows[0]["name"] = "Test Update";
      int updateCnt = da.Update(dt);

      Assert.Equal(1, updateCnt);

      dt.Rows.Clear();
      da.Fill(dt);

      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("Test Update", dt.Rows[0]["name"]);
    }

    [Fact]
    public void UpdateNullTextFieldToEmptyString()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (1, 1, NULL)");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      cb.ToString();  // keep the compiler happy

      DataTable dt = new DataTable();
      da.Fill(dt);

      dt.Rows[0]["name"] = "";
      int updateCnt = da.Update(dt);

      Assert.Equal(1, updateCnt);

      dt.Rows.Clear();
      da.Fill(dt);

      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("", dt.Rows[0]["name"]);
    }

    [Fact]
    public void UpdateExtendedTextFields()
    {
      executeSQL("CREATE TABLE Test (id int, notes MEDIUMTEXT, PRIMARY KEY(id))");
      executeSQL("INSERT INTO Test VALUES(1, 'This is my note')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      cb.ToString();  // keep the compiler happy
      DataTable dt = new DataTable();
      da.Fill(dt);

      dt.Rows[0]["notes"] = "This is my new note";
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.Equal("This is my new note", dt.Rows[0]["notes"]);
    }

    [Fact]
    public void SelectMoreThan252Rows()
    {
      CreateDefaultTable();
      for (int i = 0; i < 500; i++)
        executeSQL("INSERT INTO Test(id, id2) VALUES(NULL, " + i + ")");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(dt);

      Assert.Equal(500, dt.Rows.Count);
    }

    [Fact]
    public void DiscreteValues()
    {
      executeSQL("CREATE TABLE Test (id int, name varchar(200), dt DATETIME, b1 TEXT)");
      executeSQL("INSERT INTO Test VALUES (1, 'Test', '2004-08-01', 'Text 1')");
      executeSQL("INSERT INTO Test VALUES (2, 'Test 1', '2004-07-02', 'Text 2')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(dt);

      Assert.Equal("Test", dt.Rows[0]["name"]);
      Assert.Equal("Test 1", dt.Rows[1]["name"]);

      Assert.Equal("Text 1", dt.Rows[0]["b1"]);
      Assert.Equal("Text 2", dt.Rows[1]["b1"]);

      Assert.Equal(new DateTime(2004, 8, 1, 0, 0, 0).ToString(), dt.Rows[0]["dt"].ToString());
      Assert.Equal(new DateTime(2004, 7, 2, 0, 0, 0).ToString(), dt.Rows[1]["dt"].ToString());
    }

    [Fact]
    public void Bug5798()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (1, 1, '')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      cb.ToString();  // keep the compiler happy
      DataTable dt = new DataTable();
      da.Fill(dt);

      Assert.Equal(String.Empty, dt.Rows[0]["name"]);

      dt.Rows[0]["name"] = "Test";
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.Equal("Test", dt.Rows[0]["name"]);
    }

    [Fact]
    public void ColumnMapping()
    {
      executeSQL("CREATE TABLE Test (id int, dcname varchar(100), primary key(id))");
      executeSQL("INSERT INTO Test VALUES (1, 'Test1')");
      executeSQL("INSERT INTO Test VALUES (2, 'Test2')");
      executeSQL("INSERT INTO Test VALUES (3, 'Test3')");
      executeSQL("INSERT INTO Test VALUES (4, 'Test4')");

    }

    [Fact]
    public void TestFillWithHelper()
    {
      executeSQL("CREATE TABLE table1 (`key` INT, PRIMARY KEY(`key`))");
      executeSQL("CREATE TABLE table2 (`key` INT, PRIMARY KEY(`key`))");
      executeSQL("INSERT INTO table1 VALUES (1)");
      executeSQL("INSERT INTO table2 VALUES (1)");

      string sql = "SELECT table1.key FROM table1 WHERE table1.key=1; " +
        "SELECT table2.key FROM table2 WHERE table2.key=1";
      DataSet ds = MySqlHelper.ExecuteDataset(Connection, sql, null);
      Assert.Equal(2, ds.Tables.Count);
      Assert.Equal(1, ds.Tables[0].Rows.Count);
      Assert.Equal(1, ds.Tables[1].Rows.Count);
      Assert.Equal(1, ds.Tables[0].Rows[0]["key"]);
      Assert.Equal(1, ds.Tables[1].Rows[0]["key"]);
    }

    /// <summary>
    /// Bug #8509 - MySqlDataAdapter.FillSchema does not interpret unsigned integer
    /// </summary>
    [Fact]
    public void AutoIncrementColumns()
    {
      executeSQL("CREATE TABLE Test (id int(10) unsigned NOT NULL auto_increment primary key)");
      executeSQL("INSERT INTO Test VALUES(NULL)");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataSet ds = new DataSet();
      da.Fill(ds);
      Assert.Equal(1, Convert.ToInt32(ds.Tables[0].Rows[0]["id"]));
      DataRow row = ds.Tables[0].NewRow();
      ds.Tables[0].Rows.Add(row);

      da.Update(ds);

      ds.Clear();
      da.Fill(ds);
      Assert.Equal(1, Convert.ToInt32(ds.Tables[0].Rows[0]["id"]));
      Assert.Equal(2, Convert.ToInt32(ds.Tables[0].Rows[1]["id"]));
      cb.Dispose();
    }

    /// <summary>
    /// Bug #8292  	GROUP BY / WITH ROLLUP with DataSet causes System.Data.ConstraintException
    /// </summary>
    [Fact]
    public void Rollup()
    {
      executeSQL("CREATE TABLE Test ( id INT NOT NULL, amount INT )");
      executeSQL("INSERT INTO Test VALUES (1, 44)");
      executeSQL("INSERT INTO Test VALUES (2, 88)");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test GROUP BY id WITH ROLLUP", Connection);
      DataSet ds = new DataSet();
      da.Fill(ds);

      Assert.Single(ds.Tables);
      Assert.Equal(3, ds.Tables[0].Rows.Count);
      Assert.Equal(88, ds.Tables[0].Rows[2]["amount"]);
      Assert.Equal(DBNull.Value, ds.Tables[0].Rows[2]["id"]);
    }

    /// <summary>
    /// Bug #16307 @@Identity returning incorrect value 
    /// </summary>
    [Fact]
    public void Bug16307()
    {
      executeSQL("CREATE TABLE Test (OrgNum int auto_increment, CallReportNum int, Stamp varchar(50), " +
        "WasRealCall varchar(50), WasHangup varchar(50), primary key(orgnum))");

      string strSQL = "INSERT INTO Test(OrgNum, CallReportNum, Stamp, WasRealCall, WasHangup) " +
        "VALUES (?OrgNum, ?CallReportNum, ?Stamp, ?WasRealCall, ?WasHangup)";

      MySqlCommand cmd = new MySqlCommand(strSQL, Connection);
      MySqlParameterCollection pc = cmd.Parameters;

      pc.Add("?OrgNum", MySqlDbType.Int32, 0, "OrgNum");
      pc.Add("?CallReportNum", MySqlDbType.Int32, 0, "CallReportNum");
      pc.Add("?Stamp", MySqlDbType.VarChar, 0, "Stamp");
      pc.Add("?WasRealCall", MySqlDbType.VarChar, 0, "WasRealCall");
      pc.Add("?WasHangup", MySqlDbType.VarChar, 0, "WasHangup");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      da.InsertCommand = cmd;

      DataSet ds = new DataSet();
      da.Fill(ds);

      DataRow row = ds.Tables[0].NewRow();
      row["CallReportNum"] = 1;
      row["Stamp"] = "stamp";
      row["WasRealCall"] = "yes";
      row["WasHangup"] = "no";
      ds.Tables[0].Rows.Add(row);

      da.Update(ds.Tables[0]);

      strSQL = "SELECT @@IDENTITY AS 'Identity';";
      MySqlCommand cmd2 = new MySqlCommand(strSQL, Connection);
      using (MySqlDataReader reader = cmd2.ExecuteReader())
      {
        reader.Read();
        int intCallNum = Int32.Parse(reader.GetValue(0).ToString());
        Assert.Equal(1, intCallNum);
      }
    }

    /// <summary>
    /// Bug #8131 Data Adapter doesn't close connection 
    /// </summary>
    [Fact]
    public void QuietOpenAndClose()
    {
      executeSQL("CREATE TABLE Test (id INT, PRIMARY KEY(id))");
      executeSQL("INSERT INTO Test VALUES(1)");

      using (MySqlConnection c = new MySqlConnection(ConnectionSettings.ConnectionString))
      {
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", c);
        MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
        Assert.True(c.State == ConnectionState.Closed);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Assert.True(c.State == ConnectionState.Closed);
        Assert.Equal(1, dt.Rows.Count);

        dt.Rows[0][0] = 2;
        DataRow[] rows = new DataRow[1];
        rows[0] = dt.Rows[0];
        da.Update(dt);
        Assert.True(c.State == ConnectionState.Closed);

        dt.Clear();
        c.Open();
        Assert.True(c.State == ConnectionState.Open);
        da.Fill(dt);
        Assert.True(c.State == ConnectionState.Open);
        Assert.Equal(1, dt.Rows.Count);
        cb.Dispose();
      }
    }

    [Fact]
    public void RangeFill()
    {
      executeSQL("CREATE TABLE Test (id INT)");
      executeSQL("INSERT INTO Test VALUES (1)");
      executeSQL("INSERT INTO Test VALUES (2)");
      executeSQL("INSERT INTO Test VALUES (3)");
      executeSQL("INSERT INTO Test VALUES (4)");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataSet ds = new DataSet();
      da.Fill(ds, 1, 2, "Test");
    }

    [Fact]
    public void FillWithNulls()
    {
      executeSQL(@"CREATE TABLE Test (id INT UNSIGNED NOT NULL AUTO_INCREMENT, 
            name VARCHAR(100), PRIMARY KEY(id))");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);
      dt.Columns[0].AutoIncrement = true;
      dt.Columns[0].AutoIncrementSeed = -1;
      dt.Columns[0].AutoIncrementStep = -1;
      DataRow row = dt.NewRow();
      row["name"] = "Test1";

      dt.Rows.Add(row);
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal(1, dt.Rows[0]["id"]);
      Assert.Equal("Test1", dt.Rows[0]["name"]);

      row = dt.NewRow();
      row["name"] = System.DBNull.Value;

      dt.Rows.Add(row);
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.Equal(2, dt.Rows.Count);
      Assert.Equal(2, dt.Rows[1]["id"]);
      Assert.Equal(DBNull.Value, dt.Rows[1]["name"]);

      row = dt.NewRow();
      row["name"] = "Test3";

      dt.Rows.Add(row);
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.Equal(3, dt.Rows.Count);
      Assert.Equal(3, dt.Rows[2]["id"]);
      Assert.Equal("Test3", dt.Rows[2]["name"]);
      cb.Dispose();
    }

    [Fact]
    public void PagingFill()
    {
      CreateDefaultTable();
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 1, 'Name 1')");
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 2, 'Name 2')");
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 3, 'Name 3')");
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 4, 'Name 4')");
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 5, 'Name 5')");
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 6, 'Name 6')");
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 7, 'Name 7')");
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 8, 'Name 8')");
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 9, 'Name 9')");
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 10, 'Name 10')");
      executeSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 11, 'Name 11')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(0, 10, new DataTable[] { dt });
      Assert.Equal(10, dt.Rows.Count);
    }

    private string MakeLargeString(int len)
    {
      System.Text.StringBuilder sb = new System.Text.StringBuilder(len);
      while (len-- > 0)
        sb.Append('a');
      return sb.ToString();
    }

    [Fact]
    public void SkippingRowsLargerThan1024()
    {
      executeSQL("CREATE TABLE Test (id INT, name TEXT)");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?name)", Connection);
      cmd.Parameters.Add("?id", MySqlDbType.Int32);
      cmd.Parameters.Add("?name", MySqlDbType.Text);
      for (int i = 0; i < 5; i++)
      {
        cmd.Parameters[0].Value = i;
        cmd.Parameters[1].Value = MakeLargeString(2000);
        cmd.ExecuteNonQuery();
      }

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(0, 2, new DataTable[] { dt });
    }

    [Fact(Skip = "Not compatible with linux")]
    public void TestBatchingInserts()
    {
      executeSQL("CREATE TABLE Test (id INT, name VARCHAR(20), PRIMARY KEY(id))");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommand ins = new MySqlCommand("INSERT INTO test (id, name) VALUES (?p1, ?p2)", Connection);
      da.InsertCommand = ins;
      ins.UpdatedRowSource = UpdateRowSource.None;
      ins.Parameters.Add("?p1", MySqlDbType.Int32).SourceColumn = "id";
      ins.Parameters.Add("?p2", MySqlDbType.VarChar, 20).SourceColumn = "name";

      DataTable dt = new DataTable();
      da.Fill(dt);

      for (int i = 1; i <= 100; i++)
      {
        DataRow row = dt.NewRow();
        row["id"] = i;
        row["name"] = "name " + i;
        dt.Rows.Add(row);
      }

      da.UpdateBatchSize = 10;
      da.Update(dt);

      dt.Rows.Clear();
      da.Fill(dt);
      Assert.Equal(100, dt.Rows.Count);
      for (int i = 0; i < 100; i++)
      {
        Assert.Equal(i + 1, dt.Rows[i]["id"]);
        Assert.Equal("name " + (i + 1), dt.Rows[i]["name"]);
      }
    }

    [Fact]
    public void TestBatchingUpdates()
    {
      executeSQL("CREATE TABLE Test (id INT, name VARCHAR(20), PRIMARY KEY(id))");
      executeSQL("INSERT INTO Test VALUES (1, 'Test 1')");
      executeSQL("INSERT INTO Test VALUES (2, 'Test 2')");
      executeSQL("INSERT INTO Test VALUES (3, 'Test 3')");

      MySqlDataAdapter dummyDA = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(dummyDA);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test ORDER BY id ASC", Connection);
      da.UpdateCommand = cb.GetUpdateCommand();
      da.UpdateCommand.UpdatedRowSource = UpdateRowSource.None;

      DataTable dt = new DataTable();
      da.Fill(dt);

      dt.Rows[0]["id"] = 4;
      dt.Rows[1]["name"] = "new test value";
      dt.Rows[2]["id"] = 6;
      dt.Rows[2]["name"] = "new test value #2";

      da.UpdateBatchSize = 0;
      da.Update(dt);

      dt.Rows.Clear();
      da.Fill(dt);
      Assert.Equal(3, dt.Rows.Count);
      Assert.Equal(2, dt.Rows[0]["id"]);
      Assert.Equal(4, dt.Rows[1]["id"]);
      Assert.Equal(6, dt.Rows[2]["id"]);
      Assert.Equal("new test value", dt.Rows[0]["name"]);
      Assert.Equal("Test 1", dt.Rows[1]["name"]);
      Assert.Equal("new test value #2", dt.Rows[2]["name"]);
    }

    [Fact]
    public void TestBatchingMixed()
    {
      executeSQL("CREATE TABLE Test (id INT, name VARCHAR(20), PRIMARY KEY(id))");
      executeSQL("INSERT INTO Test VALUES (1, 'Test 1')");
      executeSQL("INSERT INTO Test VALUES (2, 'Test 2')");
      executeSQL("INSERT INTO Test VALUES (3, 'Test 3')");

      MySqlDataAdapter dummyDA = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(dummyDA);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test ORDER BY id", Connection);
      da.UpdateCommand = cb.GetUpdateCommand();
      da.InsertCommand = cb.GetInsertCommand();
      da.DeleteCommand = cb.GetDeleteCommand();
      da.UpdateCommand.UpdatedRowSource = UpdateRowSource.None;
      da.InsertCommand.UpdatedRowSource = UpdateRowSource.None;
      da.DeleteCommand.UpdatedRowSource = UpdateRowSource.None;

      DataTable dt = new DataTable();
      da.Fill(dt);

      dt.Rows[0]["id"] = 4;
      dt.Rows[1]["name"] = "new test value";
      dt.Rows[2]["id"] = 6;
      dt.Rows[2]["name"] = "new test value #2";

      DataRow row = dt.NewRow();
      row["id"] = 7;
      row["name"] = "foobar";
      dt.Rows.Add(row);

      dt.Rows[1].Delete();

      da.UpdateBatchSize = 0;
      da.Update(dt);

      dt.Rows.Clear();
      da.Fill(dt);
      Assert.Equal(3, dt.Rows.Count);
      Assert.Equal(4, dt.Rows[0]["id"]);
      Assert.Equal(6, dt.Rows[1]["id"]);
      Assert.Equal(7, dt.Rows[2]["id"]);
      Assert.Equal("Test 1", dt.Rows[0]["name"]);
      Assert.Equal("new test value #2", dt.Rows[1]["name"]);
      Assert.Equal("foobar", dt.Rows[2]["name"]);
    }

    [Fact(Skip = "Fix This")]
    public void TestBatchingInsertsMoreThanMaxPacket()
    {
      int blobSize = 64000;

      executeSQL("CREATE TABLE TestBatchingInsertsMoreThanMaxPacket (id INT, img BLOB, PRIMARY KEY(id))");

      int numRows = (Fixture.MaxPacketSize / blobSize) * 2;

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM TestBatchingInsertsMoreThanMaxPacket", Connection);
      MySqlCommand ins = new MySqlCommand("INSERT INTO TestBatchingInsertsMoreThanMaxPacket (id, img) VALUES (@p1, @p2)", Connection);
      da.InsertCommand = ins;
      ins.UpdatedRowSource = UpdateRowSource.None;
      ins.Parameters.Add("@p1", MySqlDbType.Int32).SourceColumn = "id";
      ins.Parameters.Add("@p2", MySqlDbType.Blob).SourceColumn = "img";

      DataTable dt = new DataTable();
      da.Fill(dt);

      for (int i = 0; i < numRows; i++)
      {
        DataRow row = dt.NewRow();
        row["id"] = i;
        row["img"] = Utils.CreateBlob(blobSize);
        dt.Rows.Add(row);
      }

      da.UpdateBatchSize = 0;
      da.Update(dt);

      dt.Rows.Clear();
      da.Fill(dt);
      Assert.Equal(numRows, dt.Rows.Count);
      for (int i = 0; i < numRows; i++)
        Assert.Equal(i, dt.Rows[i]["id"]);
    }

    [Fact]
    public void FunctionsReturnString()
    {
      string connStr = ConnectionSettings.ConnectionString + ";functions return string=yes";

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT CONCAT(1,2)", c);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Assert.Equal(1, dt.Rows.Count);
        Assert.Equal("12", dt.Rows[0][0]);
        Assert.True(dt.Rows[0][0] is string);
      }
    }

    /// <summary> 
    /// Bug #34657	MySqlDataAdapter.Update(DataRow[] rows) fails with MySqlCommandBuilder 
    /// </summary> 
    [Fact]
    public void ConnectionNotOpenForInsert()
    {
      executeSQL("DROP TABLE IF EXISTS Test");
      executeSQL(@"CREATE TABLE Test (id int(11) NOT NULL default '0', 
        txt varchar(100) default NULL, val decimal(11,2) default NULL, 
        PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=latin1");
      executeSQL("INSERT INTO Test VALUES (1, 'name', 23.2)");

      using (MySqlConnection c = new MySqlConnection(ConnectionSettings.ConnectionString))
      {
        string sql = "SELECT * FROM Test";
        MySqlDataAdapter da = new MySqlDataAdapter(sql, c);
        MySqlCommandBuilder bld = new MySqlCommandBuilder(da);
        DataSet ds = new DataSet();
        da.Fill(ds);

        ds.Tables[0].Rows[0]["val"] = 99.9M;
        da.Update(new DataRow[] { ds.Tables[0].Rows[0] });

        DataRow r = ds.Tables[0].NewRow();
        r["id"] = 4;
        r["txt"] = "sds";
        r["val"] = 113.2M;
        ds.Tables[0].Rows.Add(r);
        da.Update(new DataRow[] { r });
      }
    }

    /// <summary>
    /// Bug#54863 : several datadapter.Update()s  with DataTable changes in
    /// between can result into ConcurrencyException
    /// </summary>
    [Fact]
    public void AdapterConcurrentException()
    {
      executeSQL(
        "CREATE TABLE T (" +
        "id_auto int(11) NOT NULL AUTO_INCREMENT," +
        "field varchar(50) DEFAULT NULL," +
        "PRIMARY KEY (id_auto))");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM T", Connection);
      da.InsertCommand = Connection.CreateCommand();
      da.InsertCommand.CommandText = @"INSERT INTO T(field) VALUES (@p_field); 
                      SELECT * FROM T WHERE id_auto=@@IDENTITY";
      da.InsertCommand.Parameters.Add("@p_field", MySqlDbType.VarChar, 50, "field");
      da.InsertCommand.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;

      da.DeleteCommand = Connection.CreateCommand();
      da.DeleteCommand.CommandText = "DELETE FROM T WHERE id_auto=@id_auto";
      da.DeleteCommand.Parameters.Add("@id_auto", MySqlDbType.Int32, 4, "id_auto");

      DataSet ds = new DataSet();
      da.Fill(ds, "T");

      DataTable table = ds.Tables["T"];
      DataRow r = table.NewRow();
      r["field"] = "row";
      table.Rows.Add(r);
      da.Update(table);

      Assert.Equal(DataRowState.Unchanged, r.RowState);

      table.Rows[0].Delete();

      r = table.NewRow();
      r["field"] = "row2";
      table.Rows.Add(r);

      da.Update(table); // here was concurrencyviolation
      da.Fill(ds);
      Assert.Equal(1, ds.Tables["T"].Rows.Count);
      Assert.Equal("row2", ds.Tables["T"].Rows[0]["field"]);
    }

    /// <summary>
    /// Bug #38411, using closed connection with data adapter.
    /// </summary>
    [Fact(Skip = "Not compatible with netcoreapp2.0")]
    public void BatchingConnectionClosed()
    {
      executeSQL("CREATE TABLE Test (id INT, name VARCHAR(20), PRIMARY KEY(id))");

      string connStr = ConnectionSettings.ConnectionString;
      MySqlConnection c = new MySqlConnection(connStr);
      MySqlConnection c2 = new MySqlConnection(connStr);
      MySqlConnection c3 = new MySqlConnection(connStr);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", c);
      MySqlCommand ins = new MySqlCommand("INSERT INTO test (id, name) VALUES (?p1, ?p2)", c);
      da.InsertCommand = ins;
      ins.UpdatedRowSource = UpdateRowSource.None;
      ins.Parameters.Add("?p1", MySqlDbType.Int32).SourceColumn = "id";
      ins.Parameters.Add("?p2", MySqlDbType.VarChar, 20).SourceColumn = "name";

      MySqlCommand del = new MySqlCommand("delete from test where id=?p1", c2);
      da.DeleteCommand = del;
      del.UpdatedRowSource = UpdateRowSource.None;
      del.Parameters.Add("?p1", MySqlDbType.Int32).SourceColumn = "id";


      MySqlCommand upd = new MySqlCommand("update test set id=?p1, name=?p2  where id=?p1", c3);
      da.UpdateCommand = upd;
      upd.UpdatedRowSource = UpdateRowSource.None;
      upd.Parameters.Add("?p1", MySqlDbType.Int32).SourceColumn = "id";
      upd.Parameters.Add("?p2", MySqlDbType.VarChar, 20).SourceColumn = "name";

      DataTable dt = new DataTable();
      da.Fill(dt);

      for (int i = 1; i <= 100; i++)
      {
        DataRow row = dt.NewRow();
        row["id"] = i;
        row["name"] = "name " + i;
        dt.Rows.Add(row);
      }

      da.UpdateBatchSize = 10;
      da.Update(dt);

      dt.Rows.Clear();
      da.Fill(dt);
      Assert.Equal(100, dt.Rows.Count);
      for (int i = 0; i < 100; i++)
      {
        Assert.Equal(i + 1, dt.Rows[i]["id"]);
        Assert.Equal("name " + (i + 1), dt.Rows[i]["name"]);
      }

      foreach (DataRow row in dt.Rows)
      {
        row["name"] = row["name"] + "_xxx";
      }
      da.Update(dt);
      for (int i = 0; i < 100; i++)
      {
        dt.Rows[i].Delete();
      }
      da.Update(dt);
      dt.Rows.Clear();
      da.Fill(dt);
      Assert.Equal(0, dt.Rows.Count);

    }
    /// <summary>
    /// Bug#54895
    /// ConcurrencyException when trying to use UpdateRowSource.FirstReturnedRecord 
    /// with UpdateCommand and stored procedure.
    /// </summary>
    [Fact]
    public void UpdateReturnFirstRecord()
    {
      string createTable =
      "CREATE TABLE `bugtable` ( " +
        "`id_auto` int(11) NOT NULL AUTO_INCREMENT," +
        "`field` varchar(50) DEFAULT NULL," +
        "counter int NOT NULL DEFAULT 0," +
        "PRIMARY KEY (`id_auto`)" +
      ")";

      string procGetAll =
      "CREATE PROCEDURE sp_getall_bugtable()" +
      " BEGIN " +
        "select * from bugtable;" +
      " END ";

      string procUpdate =
      "CREATE PROCEDURE sp_updatebugtable(" +
        "in p_id_auto int, " +
        "in p_field varchar(50)) " +
      "BEGIN " +
        "update bugtable set field = p_field, counter = counter+1 where id_auto=p_id_auto; " +
        "select * from bugtable where id_auto=p_id_auto; " + /*retrieve updated row*/
      "END ";

      executeSQL(createTable);
      executeSQL(procGetAll);
      executeSQL(procUpdate);


      /* Add one row to the table */
      MySqlCommand cmd = new MySqlCommand(
        "insert into bugtable(field) values('x')", Connection);
      cmd.ExecuteNonQuery();


      DataSet ds = new DataSet();
      MySqlDataAdapter da = new MySqlDataAdapter();

      da.SelectCommand = Connection.CreateCommand();
      da.SelectCommand.CommandType = CommandType.StoredProcedure;
      da.SelectCommand.CommandText = "sp_getall_bugtable";

      da.UpdateCommand = Connection.CreateCommand();
      da.UpdateCommand.CommandType = CommandType.StoredProcedure;
      da.UpdateCommand.CommandText = "sp_updatebugtable";
      da.UpdateCommand.Parameters.Add("p_id_auto", MySqlDbType.Int32, 4, "id_auto");
      da.UpdateCommand.Parameters.Add("p_field", MySqlDbType.VarChar, 4, "field");
      da.UpdateCommand.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;


      da.Fill(ds, "bugtable");
      DataTable table = ds.Tables["bugtable"];
      DataRow row = table.Rows[0];
      row["field"] = "newvalue";
      Assert.Equal(DataRowState.Modified, row.RowState);
      Assert.Equal(0, (int)row["counter"]);

      da.Update(table);

      // Verify that "counter" field was changed by updating stored procedure.
      Assert.Equal(1, (int)row["counter"]);
    }

    [Fact]
    public void FillFromStoredProcedureMultipleTimesCreatesExpectedRows()
    {
      executeSQL("CREATE PROCEDURE SimpleSelect() BEGIN SELECT 'ADummyVal' as DummyVal; END");

      MySqlConnectionStringBuilder cb = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      cb.Pooling = true;

      using (MySqlConnection connection = new MySqlConnection(cb.ConnectionString))
      {
        MySqlDataAdapter adapter = new MySqlDataAdapter("SimpleSelect", connection);
        adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
        DataTable table = new DataTable();
        adapter.Fill(table);
        Assert.Equal(1, table.Rows.Count);

        adapter = new MySqlDataAdapter("SimpleSelect", connection);
        adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
        table = new DataTable();
        adapter.Fill(table);
        Assert.Equal(1, table.Rows.Count);

        MySqlConnection.ClearPool(connection);
      }
    }

    [Fact]
    public void ChangeStoredProcedureBasedSelectCommandDoesNotThrow()
    {
      executeSQL("CREATE PROCEDURE SimpleSelect1() BEGIN SELECT 'ADummyVal' as DummyVal; END");
      executeSQL("CREATE PROCEDURE SimpleSelect2() BEGIN SELECT 'ADummyVal' as DummyVal; END");

      MySqlConnectionStringBuilder cb = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      cb.Pooling = true;

      using (MySqlConnection connection = new MySqlConnection(cb.ConnectionString))
      {
        MySqlDataAdapter adapter = new MySqlDataAdapter("SimpleSelect1", connection);
        adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
        DataTable table = new DataTable();
        adapter.Fill(table);
        Assert.Equal(1, table.Rows.Count);

        adapter.SelectCommand = new MySqlCommand("SimpleSelect2", connection);
        adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
        table = new DataTable();

        try
        {
          adapter.Fill(table);
          Assert.Equal(1, table.Rows.Count);
        }
        finally
        {
          MySqlConnection.ClearPool(connection);
        }
      }
    }

    #region Async
    [Fact]
    public async Task FillAsyncDataSet()
    {
      executeSQL("CREATE TABLE DAFillAsyncTest (id INT NOT NULL AUTO_INCREMENT, id2 INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, OriginalId INT, PRIMARY KEY(id, id2))");
      executeSQL("INSERT INTO DAFillAsyncTest (id, id2, name, dt) VALUES (NULL, 1, 'Name 1', Now())");
      executeSQL("INSERT INTO DAFillAsyncTest (id, id2, name, dt) VALUES (NULL, 2, NULL, Now())");
      executeSQL("INSERT INTO DAFillAsyncTest (id, id2, name, dt) VALUES (NULL, 3, '', Now())");

      MySqlDataAdapter da = new MySqlDataAdapter("select * from DAFillAsyncTest", Connection);
      DataSet ds = new DataSet();
      await da.FillAsync(ds, "DAFillAsyncTest");

      Assert.Single(ds.Tables);
      Assert.Equal(3, ds.Tables[0].Rows.Count);

      Assert.Equal(1, ds.Tables[0].Rows[0]["id2"]);
      Assert.Equal(2, ds.Tables[0].Rows[1]["id2"]);
      Assert.Equal(3, ds.Tables[0].Rows[2]["id2"]);

      Assert.Equal("Name 1", ds.Tables[0].Rows[0]["name"]);
      Assert.Equal(DBNull.Value, ds.Tables[0].Rows[1]["name"]);
      Assert.Equal(String.Empty, ds.Tables[0].Rows[2]["name"]);

      ds.Reset();
      await da.FillAsync(ds);

      Assert.Single(ds.Tables);
      Assert.Equal(3, ds.Tables[0].Rows.Count);
      Assert.Equal("Name 1", ds.Tables[0].Rows[0]["name"]);

      ds.Reset();
      await da.FillAsync(ds, 1, 2, "DAFillAsyncTest");

      Assert.Single(ds.Tables);
      Assert.Equal(2, ds.Tables[0].Rows.Count);
      Assert.Equal(2, ds.Tables[0].Rows[0]["id2"]);
      Assert.Equal(DBNull.Value, ds.Tables[0].Rows[0]["name"]);
      Assert.Equal(String.Empty, ds.Tables[0].Rows[1]["name"]);

      ds.Reset();
      using (MySqlCommand cmd = new MySqlCommand("select * from DAFillAsyncTest", Connection))
      using (MySqlDataReader reader = cmd.ExecuteReader())
        await da.FillAsync(ds, "DAFillAsyncTest", reader, 0, 1);

      Assert.Single(ds.Tables);
      Assert.Equal(1, ds.Tables[0].Rows.Count);
      Assert.Equal(1, ds.Tables[0].Rows[0]["id2"]);
      Assert.Equal("Name 1", ds.Tables[0].Rows[0]["name"]);

      ds.Reset();
      using (MySqlCommand cmd = new MySqlCommand("select * from DAFillAsyncTest", Connection))
        await da.FillAsync(ds, 0, 2, "DAFillAsyncTest", cmd, CommandBehavior.Default);

      Assert.Single(ds.Tables);
      Assert.Equal(2, ds.Tables[0].Rows.Count);
      Assert.Equal(1, ds.Tables[0].Rows[0]["id2"]);
      Assert.Equal(DBNull.Value, ds.Tables[0].Rows[1]["name"]);
    }

    [Fact]
    public async Task FillAsyncDataTable()
    {
      executeSQL("CREATE TABLE DAFillAsyncDtTest (id INT NOT NULL AUTO_INCREMENT, id2 INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, OriginalId INT, PRIMARY KEY(id, id2))");
      executeSQL("INSERT INTO DAFillAsyncDtTest (id, id2, name, dt) VALUES (NULL, 1, 'Name 1', Now())");
      executeSQL("INSERT INTO DAFillAsyncDtTest (id, id2, name, dt) VALUES (NULL, 2, NULL, Now())");
      executeSQL("INSERT INTO DAFillAsyncDtTest (id, id2, name, dt) VALUES (NULL, 3, '', Now())");

      MySqlDataAdapter da = new MySqlDataAdapter("select * from DAFillAsyncDtTest", Connection);

      DataTable dt = new DataTable();
      await da.FillAsync(dt);

      Assert.Equal(3, dt.Rows.Count);
      Assert.Equal(7, dt.Columns.Count);
      Assert.Equal("Name 1", dt.Rows[0]["name"]);
      Assert.Equal(DBNull.Value, dt.Rows[1]["name"]);
      Assert.Equal(String.Empty, dt.Rows[2]["name"]);

      dt.Reset();
      using (MySqlCommand cmd = new MySqlCommand("select * from DAFillAsyncDtTest", Connection))
      using (MySqlDataReader reader = cmd.ExecuteReader())
        await da.FillAsync(dt, reader);

      Assert.Equal(3, dt.Rows.Count);
      Assert.Equal(7, dt.Columns.Count);
      Assert.Equal(1, dt.Rows[0]["id2"]);
      Assert.Equal(2, dt.Rows[1]["id2"]);
      Assert.Equal(3, dt.Rows[2]["id2"]);

      dt.Reset();
      using (MySqlCommand cmd = new MySqlCommand("select * from DAFillAsyncDtTest", Connection))
        await da.FillAsync(dt, cmd, CommandBehavior.Default);

      Assert.Equal(3, dt.Rows.Count);
      Assert.Equal(7, dt.Columns.Count);
      Assert.Equal(DBNull.Value, dt.Rows[1]["name"]);
      Assert.Equal(2, dt.Rows[1]["id2"]);
      Assert.Equal(3, dt.Rows[2]["id2"]);

      dt.Reset();
      DataTable[] dataTables = { dt };
      await da.FillAsync(0, 1, dataTables);

      Assert.Single(dataTables);
      Assert.Equal(1, dataTables[0].Rows.Count);
      Assert.Equal(1, dataTables[0].Rows[0]["id2"]);
      Assert.Equal("Name 1", dataTables[0].Rows[0]["name"]);

      dt.Reset();
      using (MySqlCommand cmd = new MySqlCommand("select * from DAFillAsyncDtTest", Connection))
        await da.FillAsync(dataTables, 1, 2, cmd, CommandBehavior.Default);

      Assert.Single(dataTables);
      Assert.Equal(2, dataTables[0].Rows.Count);
      Assert.Equal(2, dataTables[0].Rows[0]["id2"]);
      Assert.Equal(DBNull.Value, dataTables[0].Rows[0]["name"]);
      Assert.Equal(String.Empty, dataTables[0].Rows[1]["name"]);
    }

    [Fact]
    public async Task FillSchemaAsync()
    {
      executeSQL("CREATE PROCEDURE DAFillSchemaAsyncSpTest() BEGIN SELECT * FROM DAFillSchemaAsyncTest; END");
      executeSQL(@"CREATE TABLE DAFillSchemaAsyncTest(id INT AUTO_INCREMENT, name VARCHAR(20), PRIMARY KEY (id)) ");

      MySqlCommand cmd = new MySqlCommand("DAFillSchemaAsyncSpTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;

      MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
      reader.Read();
      reader.Close();

      MySqlDataAdapter da = new MySqlDataAdapter(cmd);
      DataTable schema = new DataTable();
      await da.FillSchemaAsync(schema, SchemaType.Source);
      Assert.Equal(2, schema.Columns.Count);

      DataSet ds = new DataSet();
      await da.FillSchemaAsync(ds, SchemaType.Source);
      Assert.True(ds.Tables.Count == 1);
      Assert.Equal(2, ds.Tables[0].Columns.Count);

      ds.Reset();
      using (cmd)
      using (reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
        await da.FillSchemaAsync(ds, SchemaType.Source, "DAFillSchemaAsyncTest", reader);

      Assert.True(ds.Tables.Count == 1);
      Assert.Equal(2, ds.Tables[0].Columns.Count);

      ds.Reset();
      using (cmd)
        await da.FillSchemaAsync(ds, SchemaType.Source, cmd, "DAFillSchemaAsyncTest", CommandBehavior.SchemaOnly);

      Assert.True(ds.Tables.Count == 1);
      Assert.Equal(2, ds.Tables[0].Columns.Count);

      ds.Reset();
      using (cmd)
        await da.FillSchemaAsync(ds, SchemaType.Source, "DAFillSchemaAsyncTest", CancellationToken.None);

      Assert.True(ds.Tables.Count == 1);
      Assert.Equal(2, ds.Tables[0].Columns.Count);

      DataTable dataTable = new DataTable();
      using (cmd)
      using (reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
        await da.FillSchemaAsync(dataTable, SchemaType.Source, reader);

      Assert.Equal(2, dataTable.Columns.Count);

      dataTable.Reset();
      using (cmd)
        await da.FillSchemaAsync(dataTable, SchemaType.Source, cmd, CommandBehavior.SchemaOnly);

      Assert.Equal(2, dataTable.Columns.Count);
      Assert.Equal("id", dataTable.Columns[0].ColumnName);
    }

    [Fact]
    public async Task UpdateAsync()
    {
      executeSQL("CREATE TABLE DAUpdateAsyncTest (id INT NOT NULL AUTO_INCREMENT, id2 INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, OriginalId INT, PRIMARY KEY(id, id2))");
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM DAUpdateAsyncTest;", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);

      DataRow dr = dt.NewRow();
      dr["id2"] = 2;
      dr["name"] = "TestName1";
      dt.Rows.Add(dr);
      int count = await da.UpdateAsync(dt);

      Assert.True(count == 1, "checking insert count");
      Assert.True(dt.Rows[dt.Rows.Count - 1]["id"] != null, "Checking auto increment column");

      dt.Rows.Clear();
      da.Fill(dt);
      dt.Rows[0]["id2"] = 3;
      dt.Rows[0]["name"] = "TestName2";
      dt.Rows[0]["ts"] = DBNull.Value;
      DateTime day1 = new DateTime(2003, 1, 16, 12, 24, 0);
      dt.Rows[0]["dt"] = day1;
      dt.Rows[0]["tm"] = day1.TimeOfDay;
      count = await da.UpdateAsync(dt);

      Assert.True(dt.Rows[0]["ts"] != null, "checking refresh of record");
      Assert.True(dt.Rows[0]["id2"] != null, "checking refresh of primary column");

      dt.Rows.Clear();
      da.Fill(dt);

      Assert.True(count == 1, "checking update count");
      DateTime dateTime = (DateTime)dt.Rows[0]["dt"];
      Assert.True(day1.Date == dateTime.Date, "checking date");
      Assert.True(day1.TimeOfDay == (TimeSpan)dt.Rows[0]["tm"], "checking time");

      dt.Rows[0].Delete();
      count = await da.UpdateAsync(dt);

      Assert.True(count == 1, "checking insert count");

      dt.Rows.Clear();
      da.Fill(dt);
      Assert.True(dt.Rows.Count == 0, "checking row count");

      dr = dt.NewRow();
      dr["id2"] = 3;
      dr["name"] = "TestName2";
      dt.Rows.Add(dr);

      DataRow[] dataRows = dt.Select(null, null, DataViewRowState.Added);
      count = await da.UpdateAsync(dataRows);
      Assert.True(count == 1, "checking update count");

      DataSet ds = new DataSet();
      da.Fill(ds);
      dr = ds.Tables[0].NewRow();
      dr["id2"] = 6;
      dr["name"] = "TestName6";
      ds.Tables[0].Rows.Add(dr);

      count = await da.UpdateAsync(ds);
      Assert.True(count == 1, "checking update count");

      dr = dt.NewRow();
      dr["id2"] = 4;
      dr["name"] = "TestName4";
      dt.Rows.Add(dr);

      DataTableMapping mapping = new DataTableMapping();
      mapping.SourceTable = "DAUpdateAsyncTest";
      mapping.DataSetTable = "DAUpdateAsyncTest";

      dataRows = dt.Select(null, null, DataViewRowState.Added);
      count = await da.UpdateAsync(dataRows, mapping);
      Assert.True(count == 1, "checking update count");

      ds.Reset();
      da.FillSchema(ds, SchemaType.Mapped);
      dr = ds.Tables[0].NewRow();
      dr["id2"] = 6;
      dr["name"] = "TestName6";
      dr["ts"] = DateTime.Now;
      ds.Tables[0].Rows.Add(dr);

      count = await da.UpdateAsync(ds, "Table");
      Assert.True(count == 1, "checking update count");

      cb.Dispose();
    }
    #endregion
  }
}
