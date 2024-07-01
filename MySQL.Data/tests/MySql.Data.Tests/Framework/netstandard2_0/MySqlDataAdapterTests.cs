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

using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Tests
{
  public class MySqlDataAdapterTests : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    private void CreateDefaultTable()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL AUTO_INCREMENT, " +
        "id2 INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, " +
        "ts TIMESTAMP, OriginalId INT, PRIMARY KEY(id, id2))");
    }

    [Test]
    public void TestFill()
    {
      FillImpl(false);
    }

    [Test]
    public void TestFillPrepared()
    {
      FillImpl(true);
    }

    private void FillImpl(bool prepare)
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test (id, id2, name, dt) VALUES (NULL, 1, 'Name 1', Now())");
      ExecuteSQL("INSERT INTO Test (id, id2, name, dt) VALUES (NULL, 2, NULL, Now())");
      ExecuteSQL("INSERT INTO Test (id, id2, name, dt) VALUES (NULL, 3, '', Now())");

      MySqlDataAdapter da = new MySqlDataAdapter("select * from Test", Connection);
      if (prepare) da.SelectCommand.Prepare();
      DataSet ds = new DataSet();
      da.Fill(ds, "Test");

      Assert.That(ds.Tables, Has.One.Items);
      Assert.That(ds.Tables[0].Rows.Count, Is.EqualTo(3));

      Assert.That(ds.Tables[0].Rows[0]["id2"], Is.EqualTo(1));
      Assert.That(ds.Tables[0].Rows[1]["id2"], Is.EqualTo(2));
      Assert.That(ds.Tables[0].Rows[2]["id2"], Is.EqualTo(3));

      Assert.That(ds.Tables[0].Rows[0]["name"], Is.EqualTo("Name 1"));
      Assert.That(ds.Tables[0].Rows[1]["name"], Is.EqualTo(DBNull.Value));
      Assert.That(ds.Tables[0].Rows[2]["name"], Is.EqualTo(String.Empty));
    }

    [Test]
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
      Assert.That(count == 1, "checking insert count");
      Assert.That(dt.Rows[dt.Rows.Count - 1]["id"] != null, "Checking auto increment column");

      dt.Rows.Clear();
      da.Fill(dt);
      dt.Rows[0]["id2"] = 3;
      dt.Rows[0]["name"] = "TestName2";
      dt.Rows[0]["ts"] = DBNull.Value;
      DateTime day1 = new DateTime(2003, 1, 16, 12, 24, 0);
      dt.Rows[0]["dt"] = day1;
      dt.Rows[0]["tm"] = day1.TimeOfDay;
      count = da.Update(dt);

      Assert.That(dt.Rows[0]["ts"] != null, "checking refresh of record");
      Assert.That(dt.Rows[0]["id2"] != null, "checking refresh of primary column");

      dt.Rows.Clear();
      da.Fill(dt);

      Assert.That(count == 1, "checking update count");
      DateTime dateTime = (DateTime)dt.Rows[0]["dt"];
      Assert.That(day1.Date == dateTime.Date, "checking date");
      Assert.That(day1.TimeOfDay == (TimeSpan)dt.Rows[0]["tm"], "checking time");

      dt.Rows[0].Delete();
      count = da.Update(dt);

      Assert.That(count == 1, "checking insert count");

      dt.Rows.Clear();
      da.Fill(dt);
      Assert.That(dt.Rows.Count == 0, "checking row count");
      cb.Dispose();
    }

    [Test]
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

      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["OriginalId"], Is.EqualTo(2));
    }

    [Test]
    public void UseAdapterPropertyOfCommandBuilder()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 1, 'Test')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder();
      cb.DataAdapter = da;

      DataTable dt = new DataTable();
      da.Fill(dt);

      dt.Rows[0]["name"] = "Test Update";
      int updateCnt = da.Update(dt);

      Assert.That(updateCnt, Is.EqualTo(1));

      dt.Rows.Clear();
      da.Fill(dt);

      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("Test Update"));
    }

    [Test]
    public void UpdateNullTextFieldToEmptyString()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (1, 1, NULL)");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      cb.ToString();  // keep the compiler happy

      DataTable dt = new DataTable();
      da.Fill(dt);

      dt.Rows[0]["name"] = "";
      int updateCnt = da.Update(dt);

      Assert.That(updateCnt, Is.EqualTo(1));

      dt.Rows.Clear();
      da.Fill(dt);

      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo(""));
    }

    [Test]
    public void UpdateExtendedTextFields()
    {
      ExecuteSQL("CREATE TABLE Test (id int, notes MEDIUMTEXT, PRIMARY KEY(id))");
      ExecuteSQL("INSERT INTO Test VALUES(1, 'This is my note')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      cb.ToString();  // keep the compiler happy
      DataTable dt = new DataTable();
      da.Fill(dt);

      dt.Rows[0]["notes"] = "This is my new note";
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.That(dt.Rows[0]["notes"], Is.EqualTo("This is my new note"));
    }

    [Test]
    public void SelectMoreThan252Rows()
    {
      CreateDefaultTable();
      for (int i = 0; i < 500; i++)
        ExecuteSQL("INSERT INTO Test(id, id2) VALUES(NULL, " + i + ")");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(dt);

      Assert.That(dt.Rows.Count, Is.EqualTo(500));
    }

    [Test]
    public void DiscreteValues()
    {
      ExecuteSQL("CREATE TABLE Test (id int, name varchar(200), dt DATETIME, b1 TEXT)");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'Test', '2004-08-01', 'Text 1')");
      ExecuteSQL("INSERT INTO Test VALUES (2, 'Test 1', '2004-07-02', 'Text 2')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(dt);

      Assert.That(dt.Rows[0]["name"], Is.EqualTo("Test"));
      Assert.That(dt.Rows[1]["name"], Is.EqualTo("Test 1"));

      Assert.That(dt.Rows[0]["b1"], Is.EqualTo("Text 1"));
      Assert.That(dt.Rows[1]["b1"], Is.EqualTo("Text 2"));

      Assert.That(dt.Rows[0]["dt"].ToString(), Is.EqualTo(new DateTime(2004, 8, 1, 0, 0, 0).ToString()));
      Assert.That(dt.Rows[1]["dt"].ToString(), Is.EqualTo(new DateTime(2004, 7, 2, 0, 0, 0).ToString()));
    }

    [Test]
    public void Bug5798()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (1, 1, '')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      cb.ToString();  // keep the compiler happy
      DataTable dt = new DataTable();
      da.Fill(dt);

      Assert.That(dt.Rows[0]["name"], Is.EqualTo(String.Empty));

      dt.Rows[0]["name"] = "Test";
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("Test"));
    }

    [Test]
    public void ColumnMapping()
    {
      ExecuteSQL("CREATE TABLE Test (id int, dcname varchar(100), primary key(id))");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'Test1')");
      ExecuteSQL("INSERT INTO Test VALUES (2, 'Test2')");
      ExecuteSQL("INSERT INTO Test VALUES (3, 'Test3')");
      ExecuteSQL("INSERT INTO Test VALUES (4, 'Test4')");

    }

    [Test]
    public void TestFillWithHelper()
    {
      ExecuteSQL("CREATE TABLE table1 (`key` INT, PRIMARY KEY(`key`))");
      ExecuteSQL("CREATE TABLE table2 (`key` INT, PRIMARY KEY(`key`))");
      ExecuteSQL("INSERT INTO table1 VALUES (1)");
      ExecuteSQL("INSERT INTO table2 VALUES (1)");

      string sql = "SELECT table1.key FROM table1 WHERE table1.key=1; " +
        "SELECT table2.key FROM table2 WHERE table2.key=1";
      DataSet ds = MySqlHelper.ExecuteDataset(Connection, sql, null);
      Assert.That(ds.Tables.Count, Is.EqualTo(2));
      Assert.That(ds.Tables[0].Rows.Count, Is.EqualTo(1));
      Assert.That(ds.Tables[1].Rows.Count, Is.EqualTo(1));
      Assert.That(ds.Tables[0].Rows[0]["key"], Is.EqualTo(1));
      Assert.That(ds.Tables[1].Rows[0]["key"], Is.EqualTo(1));
    }

    /// <summary>
    /// Bug #8509 - MySqlDataAdapter.FillSchema does not interpret unsigned integer
    /// </summary>
    [Test]
    public void AutoIncrementColumns()
    {
      ExecuteSQL("CREATE TABLE Test (id int(10) unsigned NOT NULL auto_increment primary key)");
      ExecuteSQL("INSERT INTO Test VALUES(NULL)");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataSet ds = new DataSet();
      da.Fill(ds);
      Assert.That(Convert.ToInt32(ds.Tables[0].Rows[0]["id"]), Is.EqualTo(1));
      DataRow row = ds.Tables[0].NewRow();
      ds.Tables[0].Rows.Add(row);

      da.Update(ds);

      ds.Clear();
      da.Fill(ds);
      Assert.That(Convert.ToInt32(ds.Tables[0].Rows[0]["id"]), Is.EqualTo(1));
      Assert.That(Convert.ToInt32(ds.Tables[0].Rows[1]["id"]), Is.EqualTo(2));
      cb.Dispose();
    }

    /// <summary>
    /// Bug #8292  	GROUP BY / WITH ROLLUP with DataSet causes System.Data.ConstraintException
    /// </summary>
    [Test]
    public void Rollup()
    {
      ExecuteSQL("CREATE TABLE Test ( id INT NOT NULL, amount INT )");
      ExecuteSQL("INSERT INTO Test VALUES (1, 44)");
      ExecuteSQL("INSERT INTO Test VALUES (2, 88)");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test GROUP BY id WITH ROLLUP", Connection);
      DataSet ds = new DataSet();
      da.Fill(ds);

      Assert.That(ds.Tables, Has.One.Items);
      Assert.That(ds.Tables[0].Rows.Count, Is.EqualTo(3));
      Assert.That(ds.Tables[0].Rows[2]["amount"], Is.EqualTo(88));
      Assert.That(ds.Tables[0].Rows[2]["id"], Is.EqualTo(DBNull.Value));
    }

    /// <summary>
    /// Bug #16307 @@Identity returning incorrect value 
    /// </summary>
    [Test]
    public void Bug16307()
    {
      ExecuteSQL("CREATE TABLE Test (OrgNum int auto_increment, CallReportNum int, Stamp varchar(50), " +
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
        Assert.That(intCallNum, Is.EqualTo(1));
      }
    }

    /// <summary>
    /// Bug #8131 Data Adapter doesn't close connection 
    /// </summary>
    [Test]
    public void QuietOpenAndClose()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, PRIMARY KEY(id))");
      ExecuteSQL("INSERT INTO Test VALUES(1)");

      using (MySqlConnection c = new MySqlConnection(Settings.ConnectionString))
      {
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", c);
        MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
        Assert.That(c.State == ConnectionState.Closed);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Assert.That(c.State == ConnectionState.Closed);
        Assert.That(dt.Rows.Count, Is.EqualTo(1));

        dt.Rows[0][0] = 2;
        DataRow[] rows = new DataRow[1];
        rows[0] = dt.Rows[0];
        da.Update(dt);
        Assert.That(c.State == ConnectionState.Closed);

        dt.Clear();
        c.Open();
        Assert.That(c.State == ConnectionState.Open);
        da.Fill(dt);
        Assert.That(c.State == ConnectionState.Open);
        Assert.That(dt.Rows.Count, Is.EqualTo(1));
        cb.Dispose();
      }
    }

    [Test]
    public void RangeFill()
    {
      ExecuteSQL("CREATE TABLE Test (id INT)");
      ExecuteSQL("INSERT INTO Test VALUES (1)");
      ExecuteSQL("INSERT INTO Test VALUES (2)");
      ExecuteSQL("INSERT INTO Test VALUES (3)");
      ExecuteSQL("INSERT INTO Test VALUES (4)");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataSet ds = new DataSet();
      da.Fill(ds, 1, 2, "Test");
    }

    [Test]
    public void FillWithNulls()
    {
      ExecuteSQL(@"CREATE TABLE Test (id INT UNSIGNED NOT NULL AUTO_INCREMENT, 
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
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["id"], Is.EqualTo(1));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("Test1"));

      row = dt.NewRow();
      row["name"] = System.DBNull.Value;

      dt.Rows.Add(row);
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.That(dt.Rows.Count, Is.EqualTo(2));
      Assert.That(dt.Rows[1]["id"], Is.EqualTo(2));
      Assert.That(dt.Rows[1]["name"], Is.EqualTo(DBNull.Value));

      row = dt.NewRow();
      row["name"] = "Test3";

      dt.Rows.Add(row);
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.That(dt.Rows.Count, Is.EqualTo(3));
      Assert.That(dt.Rows[2]["id"], Is.EqualTo(3));
      Assert.That(dt.Rows[2]["name"], Is.EqualTo("Test3"));
      cb.Dispose();
    }

    [Test]
    public void PagingFill()
    {
      CreateDefaultTable();
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 1, 'Name 1')");
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 2, 'Name 2')");
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 3, 'Name 3')");
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 4, 'Name 4')");
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 5, 'Name 5')");
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 6, 'Name 6')");
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 7, 'Name 7')");
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 8, 'Name 8')");
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 9, 'Name 9')");
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 10, 'Name 10')");
      ExecuteSQL("INSERT INTO Test (id, id2, name) VALUES (NULL, 11, 'Name 11')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(0, 10, new DataTable[] { dt });
      Assert.That(dt.Rows.Count, Is.EqualTo(10));
    }

    private string MakeLargeString(int len)
    {
      System.Text.StringBuilder sb = new System.Text.StringBuilder(len);
      while (len-- > 0)
        sb.Append('a');
      return sb.ToString();
    }

    [Test]
    public void SkippingRowsLargerThan1024()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, name TEXT)");

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

    [Test]
    public void TestBatchingInserts()
    {
      if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) Assert.Ignore();

      ExecuteSQL("CREATE TABLE Test (id INT, name VARCHAR(20), PRIMARY KEY(id))");

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
      Assert.That(dt.Rows.Count, Is.EqualTo(100));
      for (int i = 0; i < 100; i++)
      {
        Assert.That(dt.Rows[i]["id"], Is.EqualTo(i + 1));
        Assert.That(dt.Rows[i]["name"], Is.EqualTo("name " + (i + 1)));
      }
    }

    [Test]
    public void TestBatchingUpdates()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, name VARCHAR(20), PRIMARY KEY(id))");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'Test 1')");
      ExecuteSQL("INSERT INTO Test VALUES (2, 'Test 2')");
      ExecuteSQL("INSERT INTO Test VALUES (3, 'Test 3')");

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
      Assert.That(dt.Rows.Count, Is.EqualTo(3));
      Assert.That(dt.Rows[0]["id"], Is.EqualTo(2));
      Assert.That(dt.Rows[1]["id"], Is.EqualTo(4));
      Assert.That(dt.Rows[2]["id"], Is.EqualTo(6));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("new test value"));
      Assert.That(dt.Rows[1]["name"], Is.EqualTo("Test 1"));
      Assert.That(dt.Rows[2]["name"], Is.EqualTo("new test value #2"));
    }

    [Test]
    public void TestBatchingMixed()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, name VARCHAR(20), PRIMARY KEY(id))");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'Test 1')");
      ExecuteSQL("INSERT INTO Test VALUES (2, 'Test 2')");
      ExecuteSQL("INSERT INTO Test VALUES (3, 'Test 3')");

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
      Assert.That(dt.Rows.Count, Is.EqualTo(3));
      Assert.That(dt.Rows[0]["id"], Is.EqualTo(4));
      Assert.That(dt.Rows[1]["id"], Is.EqualTo(6));
      Assert.That(dt.Rows[2]["id"], Is.EqualTo(7));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("Test 1"));
      Assert.That(dt.Rows[1]["name"], Is.EqualTo("new test value #2"));
      Assert.That(dt.Rows[2]["name"], Is.EqualTo("foobar"));
    }

    [Test]
    [Ignore("Fix This")]
    public void TestBatchingInsertsMoreThanMaxPacket()
    {
      int blobSize = 64000;

      ExecuteSQL("CREATE TABLE TestBatchingInsertsMoreThanMaxPacket (id INT, img BLOB, PRIMARY KEY(id))");

      int numRows = (MaxPacketSize / blobSize) * 2;

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
      Assert.That(dt.Rows.Count, Is.EqualTo(numRows));
      for (int i = 0; i < numRows; i++)
        Assert.That(dt.Rows[i]["id"], Is.EqualTo(i));
    }

    [Test]
    public void FunctionsReturnString()
    {
      string connStr = Settings.ConnectionString + ";functions return string=yes";

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlDataAdapter da = new MySqlDataAdapter("SELECT CONCAT(1,2)", c);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Assert.That(dt.Rows.Count, Is.EqualTo(1));
        Assert.That(dt.Rows[0][0], Is.EqualTo("12"));
        Assert.That(dt.Rows[0][0] is string);
      }
    }

    /// <summary> 
    /// Bug #34657	MySqlDataAdapter.Update(DataRow[] rows) fails with MySqlCommandBuilder 
    /// </summary> 
    [Test]
    public void ConnectionNotOpenForInsert()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL(@"CREATE TABLE Test (id int(11) NOT NULL default '0', 
        txt varchar(100) default NULL, val decimal(11,2) default NULL, 
        PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=latin1");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'name', 23.2)");

      using (MySqlConnection c = new MySqlConnection(Settings.ConnectionString))
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
    [Test]
    public void AdapterConcurrentException()
    {
      ExecuteSQL(
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

      Assert.That(r.RowState, Is.EqualTo(DataRowState.Unchanged));

      table.Rows[0].Delete();

      r = table.NewRow();
      r["field"] = "row2";
      table.Rows.Add(r);

      da.Update(table); // here was concurrencyviolation
      da.Fill(ds);
      Assert.That(ds.Tables["T"].Rows.Count, Is.EqualTo(1));
      Assert.That(ds.Tables["T"].Rows[0]["field"], Is.EqualTo("row2"));
    }

    /// <summary>
    /// Bug #38411, using closed connection with data adapter.
    /// </summary>
    [Test]
    public void BatchingConnectionClosed()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, name VARCHAR(20), PRIMARY KEY(id))");

      string connStr = Settings.ConnectionString;
      MySqlConnection c = new MySqlConnection(connStr);
      MySqlConnection c2 = new MySqlConnection(connStr);
      MySqlConnection c3 = new MySqlConnection(connStr);

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", c);
      MySqlCommand ins = new MySqlCommand("INSERT INTO Test (id, name) VALUES (?p1, ?p2)", c);
      da.InsertCommand = ins;
      ins.UpdatedRowSource = UpdateRowSource.None;
      ins.Parameters.Add("?p1", MySqlDbType.Int32).SourceColumn = "id";
      ins.Parameters.Add("?p2", MySqlDbType.VarChar, 20).SourceColumn = "name";

      MySqlCommand del = new MySqlCommand("delete from Test where id=?p1", c2);
      da.DeleteCommand = del;
      del.UpdatedRowSource = UpdateRowSource.None;
      del.Parameters.Add("?p1", MySqlDbType.Int32).SourceColumn = "id";


      MySqlCommand upd = new MySqlCommand("update Test set id=?p1, name=?p2  where id=?p1", c3);
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
      Assert.That(dt.Rows.Count, Is.EqualTo(100));
      for (int i = 0; i < 100; i++)
      {
        Assert.That(dt.Rows[i]["id"], Is.EqualTo(i + 1));
        Assert.That(dt.Rows[i]["name"], Is.EqualTo("name " + (i + 1)));
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
      Assert.That(dt.Rows.Count, Is.EqualTo(0));

    }

    /// <summary>
    /// Bug#54895
    /// ConcurrencyException when trying to use UpdateRowSource.FirstReturnedRecord 
    /// with UpdateCommand and stored procedure.
    /// </summary>
    [Test]
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

      ExecuteSQL(createTable);
      ExecuteSQL(procGetAll);
      ExecuteSQL(procUpdate);


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
      Assert.That(row.RowState, Is.EqualTo(DataRowState.Modified));
      Assert.That((int)row["counter"], Is.EqualTo(0));

      da.Update(table);

      // Verify that "counter" field was changed by updating stored procedure.
      Assert.That((int)row["counter"], Is.EqualTo(1));
    }

    [Test]
    public void FillFromStoredProcedureMultipleTimesCreatesExpectedRows()
    {
      ExecuteSQL("CREATE PROCEDURE SimpleSelect() BEGIN SELECT 'ADummyVal' as DummyVal; END");

      MySqlConnectionStringBuilder cb = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      cb.Pooling = true;

      using (MySqlConnection connection = new MySqlConnection(cb.ConnectionString))
      {
        MySqlDataAdapter adapter = new MySqlDataAdapter("SimpleSelect", connection);
        adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
        DataTable table = new DataTable();
        adapter.Fill(table);
        Assert.That(table.Rows.Count, Is.EqualTo(1));

        adapter = new MySqlDataAdapter("SimpleSelect", connection);
        adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
        table = new DataTable();
        adapter.Fill(table);
        Assert.That(table.Rows.Count, Is.EqualTo(1));

        MySqlConnection.ClearPool(connection);
      }
    }

    [Test]
    public void ChangeStoredProcedureBasedSelectCommandDoesNotThrow()
    {
      ExecuteSQL("CREATE PROCEDURE SimpleSelect1() BEGIN SELECT 'ADummyVal' as DummyVal; END");
      ExecuteSQL("CREATE PROCEDURE SimpleSelect2() BEGIN SELECT 'ADummyVal' as DummyVal; END");

      MySqlConnectionStringBuilder cb = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      cb.Pooling = true;

      using (MySqlConnection connection = new MySqlConnection(cb.ConnectionString))
      {
        MySqlDataAdapter adapter = new MySqlDataAdapter("SimpleSelect1", connection);
        adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
        DataTable table = new DataTable();
        adapter.Fill(table);
        Assert.That(table.Rows.Count, Is.EqualTo(1));

        adapter.SelectCommand = new MySqlCommand("SimpleSelect2", connection);
        adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
        table = new DataTable();

        try
        {
          adapter.Fill(table);
          Assert.That(table.Rows.Count, Is.EqualTo(1));
        }
        finally
        {
          MySqlConnection.ClearPool(connection);
        }
      }
    }

    [Test, Description("CommandBuilder Async ")]
    public async Task CommandBuilderAsync()
    {
      ExecuteSQL("CREATE TABLE DAActor (id INT NOT NULL AUTO_INCREMENT, name VARCHAR(100),PRIMARY KEY(id))");
      ExecuteSQL("INSERT INTO DAActor (name) VALUES ('Name 1')");
      ExecuteSQL("INSERT INTO DAActor (name) VALUES ('Name 2')");
      using (var conn = new MySqlConnection(Settings.ConnectionString))
      {
        await conn.OpenAsync();
        var da = new MySqlDataAdapter("SELECT * FROM DAActor", conn);
        var cb = new MySqlCommandBuilder(da);
        var dt = new DataTable();
        dt.Clear();
        await da.FillAsync(dt); // asynchronous

        dt.Rows[0][1] = "my changed value 1";
        var changes = dt.GetChanges();
        var count = da.Update(changes);
        dt.AcceptChanges();
        Assert.That(count == 1, "checking update count");
        await conn.CloseAsync();
      }

      using (var conn = new MySqlConnection(Settings.ConnectionString))
      {
        await conn.OpenAsync();
        var da = new MySqlDataAdapter("SELECT * FROM DAActor", conn);
        var cb = new MySqlCommandBuilder(da);
        var dt = new DataTable();
        await da.FillAsync(dt);
        await da.UpdateAsync(dt); // asynchronous

        dt.Rows[0][1] = "my changed value 2";
        var changes = dt.GetChanges();
        var count = da.Update(changes);
        dt.AcceptChanges();
        Assert.That(count == 1, "checking update count");
        await conn.CloseAsync();
      }
    }

    /// <summary>
    /// Bug #22913833	- COMMANDS IGNORED AND NO ERROR PRODUCED WHEN PACKET OVER MAX_ALLOWED_PACKET
    /// When the size of the query exceeded the size of the 'max_allowed_packet', it just ignore the query and jump to the next one. This 
    /// behavior changed to raise an exception instead to avoid partial batch inserts
    /// </summary>
    [Test]
    public void BatchOverMaxPacketAllowed()
    {
      // setting the 'max_allowed_packet' to its minimum so we can reproduce the issue easily
      ExecuteSQL("SET GLOBAL max_allowed_packet = 1024", true);
      ExecuteSQL("CREATE TABLE Test (Id INT NOT NULL AUTO_INCREMENT, Col1 VARCHAR(250), Blob1 LONGBLOB, PRIMARY KEY(Id))");

      using (var conn = new MySqlConnection(Connection.ConnectionString))
      {
        conn.Open();

        MySqlDataAdapter dataAdapter = new MySqlDataAdapter("SELECT Col1, Blob1 FROM Test", conn);
        MySqlCommandBuilder mySqlCommandBuilder = new MySqlCommandBuilder(dataAdapter);
        dataAdapter.InsertCommand = mySqlCommandBuilder.GetInsertCommand();
        DataTable dataTable = new();
        dataAdapter.Fill(dataTable);

        StringBuilder stringBuilder = new();
        Random random = new();
        // generate random text
        for (int i = 0; i < 200; i++)
        {
          var _char = (char)random.Next(65, 90);
          stringBuilder.Append(_char);
        }

        // generate random byte array
        byte[] buf = new byte[500];
        random.NextBytes(buf);

        DataRow[] dataRows = new DataRow[2];
        for (int i = 0; i < 2; i++)
        {
          dataRows[i] = dataTable.NewRow();
          dataRows[i][0] = $"Col1Value_{i}_{stringBuilder.ToString()}";
          dataRows[i][1] = buf;
          dataTable.Rows.Add(dataRows[i]);
        }

        dataAdapter.UpdateBatchSize = 2;
        var ex = Assert.Throws<MySqlException>(() => dataAdapter.Update(dataRows));
        Assert.That(ex.Message, Is.EqualTo(Resources.QueryTooLarge).IgnoreCase);
      }

      // setting back to the initial value
      ExecuteSQL($"SET GLOBAL max_allowed_packet = {MaxPacketSize}", true);
    }

    #region Async
    [Test]
    public async Task FillAsyncDataSet()
    {
      ExecuteSQL("CREATE TABLE DAFillAsyncTest (id INT NOT NULL AUTO_INCREMENT, id2 INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, OriginalId INT, PRIMARY KEY(id, id2))");
      ExecuteSQL("INSERT INTO DAFillAsyncTest (id, id2, name, dt) VALUES (NULL, 1, 'Name 1', Now())");
      ExecuteSQL("INSERT INTO DAFillAsyncTest (id, id2, name, dt) VALUES (NULL, 2, NULL, Now())");
      ExecuteSQL("INSERT INTO DAFillAsyncTest (id, id2, name, dt) VALUES (NULL, 3, '', Now())");

      MySqlDataAdapter da = new MySqlDataAdapter("select * from DAFillAsyncTest", Connection);
      DataSet ds = new DataSet();
      await da.FillAsync(ds, "DAFillAsyncTest");

      Assert.That(ds.Tables, Has.One.Items);
      Assert.That(ds.Tables[0].Rows.Count, Is.EqualTo(3));

      Assert.That(ds.Tables[0].Rows[0]["id2"], Is.EqualTo(1));
      Assert.That(ds.Tables[0].Rows[1]["id2"], Is.EqualTo(2));
      Assert.That(ds.Tables[0].Rows[2]["id2"], Is.EqualTo(3));

      Assert.That(ds.Tables[0].Rows[0]["name"], Is.EqualTo("Name 1"));
      Assert.That(ds.Tables[0].Rows[1]["name"], Is.EqualTo(DBNull.Value));
      Assert.That(ds.Tables[0].Rows[2]["name"], Is.EqualTo(String.Empty));

      ds.Reset();
      await da.FillAsync(ds);

      Assert.That(ds.Tables, Has.One.Items);
      Assert.That(ds.Tables[0].Rows.Count, Is.EqualTo(3));
      Assert.That(ds.Tables[0].Rows[0]["name"], Is.EqualTo("Name 1"));

      ds.Reset();
      await da.FillAsync(ds, 1, 2, "DAFillAsyncTest");

      Assert.That(ds.Tables, Has.One.Items);
      Assert.That(ds.Tables[0].Rows.Count, Is.EqualTo(2));
      Assert.That(ds.Tables[0].Rows[0]["id2"], Is.EqualTo(2));
      Assert.That(ds.Tables[0].Rows[0]["name"], Is.EqualTo(DBNull.Value));
      Assert.That(ds.Tables[0].Rows[1]["name"], Is.EqualTo(String.Empty));

      ds.Reset();
      using (MySqlCommand cmd = new MySqlCommand("select * from DAFillAsyncTest", Connection))
      using (MySqlDataReader reader = cmd.ExecuteReader())
        await da.FillAsync(ds, "DAFillAsyncTest", reader, 0, 1);

      Assert.That(ds.Tables, Has.One.Items);
      Assert.That(ds.Tables[0].Rows.Count, Is.EqualTo(1));
      Assert.That(ds.Tables[0].Rows[0]["id2"], Is.EqualTo(1));
      Assert.That(ds.Tables[0].Rows[0]["name"], Is.EqualTo("Name 1"));

      ds.Reset();
      using (MySqlCommand cmd = new MySqlCommand("select * from DAFillAsyncTest", Connection))
        await da.FillAsync(ds, 0, 2, "DAFillAsyncTest", cmd, CommandBehavior.Default);

      Assert.That(ds.Tables, Has.One.Items);
      Assert.That(ds.Tables[0].Rows.Count, Is.EqualTo(2));
      Assert.That(ds.Tables[0].Rows[0]["id2"], Is.EqualTo(1));
      Assert.That(ds.Tables[0].Rows[1]["name"], Is.EqualTo(DBNull.Value));
    }

    [Test]
    public async Task FillAsyncDataTable()
    {
      ExecuteSQL("CREATE TABLE DAFillAsyncDtTest (id INT NOT NULL AUTO_INCREMENT, id2 INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, OriginalId INT, PRIMARY KEY(id, id2))");
      ExecuteSQL("INSERT INTO DAFillAsyncDtTest (id, id2, name, dt) VALUES (NULL, 1, 'Name 1', Now())");
      ExecuteSQL("INSERT INTO DAFillAsyncDtTest (id, id2, name, dt) VALUES (NULL, 2, NULL, Now())");
      ExecuteSQL("INSERT INTO DAFillAsyncDtTest (id, id2, name, dt) VALUES (NULL, 3, '', Now())");

      MySqlDataAdapter da = new MySqlDataAdapter("select * from DAFillAsyncDtTest", Connection);

      DataTable dt = new DataTable();
      await da.FillAsync(dt);

      Assert.That(dt.Rows.Count, Is.EqualTo(3));
      Assert.That(dt.Columns.Count, Is.EqualTo(7));
      Assert.That(dt.Rows[0]["name"], Is.EqualTo("Name 1"));
      Assert.That(dt.Rows[1]["name"], Is.EqualTo(DBNull.Value));
      Assert.That(dt.Rows[2]["name"], Is.EqualTo(String.Empty));

      dt.Reset();
      using (MySqlCommand cmd = new MySqlCommand("select * from DAFillAsyncDtTest", Connection))
      using (MySqlDataReader reader = cmd.ExecuteReader())
        await da.FillAsync(dt, reader);

      Assert.That(dt.Rows.Count, Is.EqualTo(3));
      Assert.That(dt.Columns.Count, Is.EqualTo(7));
      Assert.That(dt.Rows[0]["id2"], Is.EqualTo(1));
      Assert.That(dt.Rows[1]["id2"], Is.EqualTo(2));
      Assert.That(dt.Rows[2]["id2"], Is.EqualTo(3));

      dt.Reset();
      using (MySqlCommand cmd = new MySqlCommand("select * from DAFillAsyncDtTest", Connection))
        await da.FillAsync(dt, cmd, CommandBehavior.Default);

      Assert.That(dt.Rows.Count, Is.EqualTo(3));
      Assert.That(dt.Columns.Count, Is.EqualTo(7));
      Assert.That(dt.Rows[1]["name"], Is.EqualTo(DBNull.Value));
      Assert.That(dt.Rows[1]["id2"], Is.EqualTo(2));
      Assert.That(dt.Rows[2]["id2"], Is.EqualTo(3));

      dt.Reset();
      DataTable[] dataTables = { dt };
      await da.FillAsync(0, 1, dataTables);

      Assert.That(dataTables, Has.One.Items);
      Assert.That(dataTables[0].Rows.Count, Is.EqualTo(1));
      Assert.That(dataTables[0].Rows[0]["id2"], Is.EqualTo(1));
      Assert.That(dataTables[0].Rows[0]["name"], Is.EqualTo("Name 1"));

      dt.Reset();
      using (MySqlCommand cmd = new MySqlCommand("select * from DAFillAsyncDtTest", Connection))
        await da.FillAsync(dataTables, 1, 2, cmd, CommandBehavior.Default);

      Assert.That(dataTables, Has.One.Items);
      Assert.That(dataTables[0].Rows.Count, Is.EqualTo(2));
      Assert.That(dataTables[0].Rows[0]["id2"], Is.EqualTo(2));
      Assert.That(dataTables[0].Rows[0]["name"], Is.EqualTo(DBNull.Value));
      Assert.That(dataTables[0].Rows[1]["name"], Is.EqualTo(String.Empty));
    }

    [Test]
    public async Task FillSchemaAsync()
    {
      ExecuteSQL("CREATE PROCEDURE DAFillSchemaAsyncSpTest() BEGIN SELECT * FROM DAFillSchemaAsyncTest; END");
      ExecuteSQL(@"CREATE TABLE DAFillSchemaAsyncTest(id INT AUTO_INCREMENT, name VARCHAR(20), PRIMARY KEY (id)) ");

      MySqlCommand cmd = new MySqlCommand("DAFillSchemaAsyncSpTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;

      MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
      reader.Read();
      reader.Close();

      MySqlDataAdapter da = new MySqlDataAdapter(cmd);
      DataTable schema = new DataTable();
      await da.FillSchemaAsync(schema, SchemaType.Source);
      Assert.That(schema.Columns.Count, Is.EqualTo(2));

      DataSet ds = new DataSet();
      await da.FillSchemaAsync(ds, SchemaType.Source);
      Assert.That(ds.Tables.Count == 1);
      Assert.That(ds.Tables[0].Columns.Count, Is.EqualTo(2));

      ds.Reset();
      using (cmd)
      using (reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
        await da.FillSchemaAsync(ds, SchemaType.Source, "DAFillSchemaAsyncTest", reader);

      Assert.That(ds.Tables.Count == 1);
      Assert.That(ds.Tables[0].Columns.Count, Is.EqualTo(2));

      ds.Reset();
      using (cmd)
        await da.FillSchemaAsync(ds, SchemaType.Source, cmd, "DAFillSchemaAsyncTest", CommandBehavior.SchemaOnly);

      Assert.That(ds.Tables.Count == 1);
      Assert.That(ds.Tables[0].Columns.Count, Is.EqualTo(2));

      ds.Reset();
      using (cmd)
        await da.FillSchemaAsync(ds, SchemaType.Source, "DAFillSchemaAsyncTest", CancellationToken.None);

      Assert.That(ds.Tables.Count == 1);
      Assert.That(ds.Tables[0].Columns.Count, Is.EqualTo(2));

      DataTable dataTable = new DataTable();
      using (cmd)
      using (reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
        await da.FillSchemaAsync(dataTable, SchemaType.Source, reader);

      Assert.That(dataTable.Columns.Count, Is.EqualTo(2));

      dataTable.Reset();
      using (cmd)
        await da.FillSchemaAsync(dataTable, SchemaType.Source, cmd, CommandBehavior.SchemaOnly);

      Assert.That(dataTable.Columns.Count, Is.EqualTo(2));
      Assert.That(dataTable.Columns[0].ColumnName, Is.EqualTo("id"));
    }

    [Test]
    public async Task UpdateAsync()
    {
      ExecuteSQL("CREATE TABLE DAUpdateAsyncTest (id INT NOT NULL AUTO_INCREMENT, id2 INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, OriginalId INT, PRIMARY KEY(id, id2))");
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM DAUpdateAsyncTest;", Connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);

      DataRow dr = dt.NewRow();
      dr["id2"] = 2;
      dr["name"] = "TestName1";
      dt.Rows.Add(dr);
      int count = await da.UpdateAsync(dt);

      Assert.That(count == 1, "checking insert count");
      Assert.That(dt.Rows[dt.Rows.Count - 1]["id"] != null, "Checking auto increment column");

      dt.Rows.Clear();
      da.Fill(dt);
      dt.Rows[0]["id2"] = 3;
      dt.Rows[0]["name"] = "TestName2";
      dt.Rows[0]["ts"] = DBNull.Value;
      DateTime day1 = new DateTime(2003, 1, 16, 12, 24, 0);
      dt.Rows[0]["dt"] = day1;
      dt.Rows[0]["tm"] = day1.TimeOfDay;
      count = await da.UpdateAsync(dt);

      Assert.That(dt.Rows[0]["ts"] != null, "checking refresh of record");
      Assert.That(dt.Rows[0]["id2"] != null, "checking refresh of primary column");

      dt.Rows.Clear();
      da.Fill(dt);

      Assert.That(count == 1, "checking update count");
      DateTime dateTime = (DateTime)dt.Rows[0]["dt"];
      Assert.That(day1.Date == dateTime.Date, "checking date");
      Assert.That(day1.TimeOfDay == (TimeSpan)dt.Rows[0]["tm"], "checking time");

      dt.Rows[0].Delete();
      count = await da.UpdateAsync(dt);

      Assert.That(count == 1, "checking insert count");

      dt.Rows.Clear();
      da.Fill(dt);
      Assert.That(dt.Rows.Count == 0, "checking row count");

      dr = dt.NewRow();
      dr["id2"] = 3;
      dr["name"] = "TestName2";
      dt.Rows.Add(dr);

      DataRow[] dataRows = dt.Select(null, null, DataViewRowState.Added);
      count = await da.UpdateAsync(dataRows);
      Assert.That(count == 1, "checking update count");

      DataSet ds = new DataSet();
      da.Fill(ds);
      dr = ds.Tables[0].NewRow();
      dr["id2"] = 6;
      dr["name"] = "TestName6";
      ds.Tables[0].Rows.Add(dr);

      count = await da.UpdateAsync(ds);
      Assert.That(count == 1, "checking update count");

      dr = dt.NewRow();
      dr["id2"] = 4;
      dr["name"] = "TestName4";
      dt.Rows.Add(dr);

      DataTableMapping mapping = new DataTableMapping();
      mapping.SourceTable = "DAUpdateAsyncTest";
      mapping.DataSetTable = "DAUpdateAsyncTest";

      dataRows = dt.Select(null, null, DataViewRowState.Added);
      count = await da.UpdateAsync(dataRows, mapping);
      Assert.That(count == 1, "checking update count");

      ds.Reset();
      da.FillSchema(ds, SchemaType.Mapped);
      dr = ds.Tables[0].NewRow();
      dr["id2"] = 6;
      dr["name"] = "TestName6";
      dr["ts"] = DateTime.Now;
      ds.Tables[0].Rows.Add(dr);

      count = await da.UpdateAsync(ds, "Table");
      Assert.That(count == 1, "checking update count");

      cb.Dispose();
    }
    #endregion
  }
}
