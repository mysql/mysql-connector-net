// Copyright © 2013, 2014 Oracle and/or its affiliates. All rights reserved.
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
using Xunit;
using System.Data;
using System.Threading.Tasks;


namespace MySql.Data.MySqlClient.Tests
{
    public class AsyncTests : TestBase
  {
    protected TestSetup ts;

    public AsyncTests(TestSetup setup) : base(setup, "async")
    {
      ts = setup;
      ts.CreateDatabase("1");
    }
        
    [Fact]
    public void ExecuteNonQuery()
    {      
      if (ts.version < new Version(5, 0)) return;

      if (connection.State != ConnectionState.Open)
        connection.Open();
      
      executeSQL("CREATE TABLE test (id int)");

      executeSQL("CREATE PROCEDURE spTest() BEGIN SET @x=0; REPEAT INSERT INTO test VALUES(@x); " +
        "SET @x=@x+1; UNTIL @x = 300 END REPEAT; END");

      MySqlCommand proc = new MySqlCommand("spTest", connection);
      proc.CommandType = CommandType.StoredProcedure;
      IAsyncResult iar = proc.BeginExecuteNonQuery();
      int count = 0;
      while (!iar.IsCompleted)
      {
        count++;
        System.Threading.Thread.Sleep(20);
      }
      proc.EndExecuteNonQuery(iar);


      Assert.True(count > 0);

      proc.CommandType = CommandType.Text;
      proc.CommandText = "SELECT COUNT(*) FROM test";
      object cnt = proc.ExecuteScalar();
      Assert.Equal(300, Convert.ToInt32(cnt));
    }

    [Fact]
    public void ExecuteReader()
    {
      if (ts.version < new Version(5, 0)) return;

      if (connection.State != ConnectionState.Open)
        connection.Open();
      
      executeSQL("CREATE TABLE test (id int)");
      executeSQL("CREATE PROCEDURE spTest() BEGIN INSERT INTO test VALUES(1); " +
        "SELECT SLEEP(2); SELECT 'done'; END");

      MySqlCommand proc = new MySqlCommand("spTest", connection);
      proc.CommandType = CommandType.StoredProcedure;
      IAsyncResult iar = proc.BeginExecuteReader();
      int count = 0;
      while (!iar.IsCompleted)
      {
        count++;
        System.Threading.Thread.Sleep(20);
      }

      using (MySqlDataReader reader = proc.EndExecuteReader(iar))
      {
        Assert.NotNull(reader);
        Assert.True(count > 0, "count > 0");
        Assert.True(reader.Read(), "can read");
        Assert.True(reader.NextResult());
        Assert.True(reader.Read());
        Assert.Equal("done", reader.GetString(0));
        reader.Close();

        proc.CommandType = CommandType.Text;
        proc.CommandText = "SELECT COUNT(*) FROM test";
        object cnt = proc.ExecuteScalar();
        Assert.Equal(1, Convert.ToInt32(cnt));
      }
    }

    [Fact]
    public void ThrowingExceptions()
    {
      if (connection.State != ConnectionState.Open)
        connection.Open();

      MySqlCommand cmd = new MySqlCommand("SELECT xxx", connection);
      IAsyncResult r = cmd.BeginExecuteReader();      
      Exception ex = Assert.Throws<MySqlException>(() => cmd.EndExecuteReader(r));
      Assert.Equal("Unknown column 'xxx' in 'field list'", ex.Message);
    }


    #region Async
    #region PrivateMembers
    private int statementCount;
    private string statementTemplate1 = @"CREATE PROCEDURE `spTest{0}`() NOT DETERMINISTIC
          CONTAINS SQL SQL SECURITY DEFINER COMMENT '' 
          BEGIN
            SELECT 1,2,3;
          END{1}";
    private string statementTemplate2 = @"INSERT INTO Test (id, name) VALUES ({0}, 'a "" na;me'){1}";
    private void CreateDefaultTable()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL AUTO_INCREMENT, " +
        "id2 INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, " +
        "ts TIMESTAMP, OriginalId INT, PRIMARY KEY(id, id2))");
    }
    void ExecuteScriptWithInserts_StatementExecuted(object sender, MySqlScriptEventArgs e)
    {
      string stmt = String.Format(statementTemplate2, statementCount++, null);
      Assert.Equal(stmt, e.StatementText);
    }
    void ExecuteScriptWithProcedures_QueryExecuted(object sender, MySqlScriptEventArgs e)
    {
      string stmt = String.Format(statementTemplate1, statementCount++, null);
      Assert.Equal(stmt, e.StatementText);
    }
    #endregion

    #region BulkLoad
    [Fact]
    public async Task BulkLoadAsync()
    {
      executeSQL("CREATE TABLE BulkLoadTest (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      string path = System.IO.Path.GetTempFileName();
      System.IO.StreamWriter sw = new System.IO.StreamWriter(path);
      for (int i = 0; i < 500; i++)
        sw.WriteLine(i + "\t'Test'");
      sw.Flush();
      sw.Close();

      MySqlBulkLoader loader = new MySqlBulkLoader(connection);
      loader.TableName = "BulkLoadTest";
      loader.FileName = path;
      loader.Timeout = 0;

      var result = await loader.LoadAsync();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM BulkLoadTest", connection);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(500, dt.Rows.Count);
      Assert.Equal("'Test'", dt.Rows[0][1].ToString().Trim());
    }
    #endregion

    #region Command
    [Fact]
    public async Task ExecuteNonQueryAsync()
    {
      if (ts.version < new Version(5, 0)) return;

      executeSQL("CREATE TABLE NonQueryAsyncTest (id int)");
      executeSQL("CREATE PROCEDURE NonQueryAsyncSpTest() BEGIN SET @x=0; REPEAT INSERT INTO NonQueryAsyncTest VALUES(@x); SET @x=@x+1; UNTIL @x = 100 END REPEAT; END");

      MySqlCommand proc = new MySqlCommand("NonQueryAsyncSpTest", connection);
      proc.CommandType = CommandType.StoredProcedure;
      int result = await proc.ExecuteNonQueryAsync();

      Assert.NotEqual(-1, result);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM NonQueryAsyncTest;", connection);
      cmd.CommandType = CommandType.Text;
      object cnt = cmd.ExecuteScalar();
      Assert.Equal(100, Convert.ToInt32(cnt));
    }

    [Fact]
    public async Task ExecuteReaderAsync()
    {
      if (ts.version < new Version(5, 0)) return;

      if (connection.State != ConnectionState.Open)
        connection.Open();

      executeSQL("CREATE TABLE ReaderAsyncTest (id int)");
      executeSQL("CREATE PROCEDURE ReaderAsyncSpTest() BEGIN INSERT INTO ReaderAsyncTest VALUES(1); SELECT SLEEP(2); SELECT 'done'; END");

      MySqlCommand proc = new MySqlCommand("ReaderAsyncSpTest", connection);
      proc.CommandType = CommandType.StoredProcedure;

      using (MySqlDataReader reader = await proc.ExecuteReaderAsync() as MySqlDataReader)
      {
        Assert.NotNull(reader);
        Assert.True(reader.Read(), "can read");
        Assert.True(reader.NextResult());
        Assert.True(reader.Read());
        Assert.Equal("done", reader.GetString(0));
        reader.Close();

        proc.CommandType = CommandType.Text;
        proc.CommandText = "SELECT COUNT(*) FROM ReaderAsyncTest";
        object cnt = proc.ExecuteScalar();
        Assert.Equal(1, Convert.ToInt32(cnt));
      }
    }

    [Fact]
    public async Task ExecuteScalarAsync()
    {
      if (ts.version < new Version(5, 0)) return;

      if (connection.State != ConnectionState.Open)
        connection.Open();

      executeSQL("CREATE PROCEDURE ScalarAsyncSpTest( IN valin VARCHAR(50), OUT valout VARCHAR(50) ) BEGIN  SET valout=valin;  SELECT 'Test'; END");

      MySqlCommand cmd = new MySqlCommand("ScalarAsyncSpTest", connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?valin", "valuein");
      cmd.Parameters.Add(new MySqlParameter("?valout", MySqlDbType.VarChar));
      cmd.Parameters[1].Direction = ParameterDirection.Output;

      object result = await cmd.ExecuteScalarAsync();
      Assert.Equal("Test", result);
      Assert.Equal("valuein", cmd.Parameters[1].Value);
    }
    #endregion

    #region Connection
    [Fact]
    public async Task TransactionAsync()
    {
      executeSQL("Create Table TranAsyncTest(key2 varchar(50), name varchar(50), name2 varchar(50))");
      executeSQL("INSERT INTO TranAsyncTest VALUES('P', 'Test1', 'Test2')");

      MySqlTransaction txn = await connection.BeginTransactionAsync();
      MySqlConnection c = txn.Connection;
      Assert.Equal(connection, c);
      MySqlCommand cmd = new MySqlCommand("SELECT name, name2 FROM TranAsyncTest WHERE key2='P'", connection, txn);
      MySqlTransaction t2 = cmd.Transaction;
      Assert.Equal(txn, t2);
      MySqlDataReader reader = null;
      try
      {
        reader = cmd.ExecuteReader();
        reader.Close();
        txn.Commit();
      }
      catch (Exception ex)
      {
        Assert.False(ex.Message != string.Empty, ex.Message);
        txn.Rollback();
      }
      finally
      {
        if (reader != null) reader.Close();
      }
    }

    [Fact]
    public async Task ChangeDataBaseAsync()
    {
      if (ts.version < new Version(4, 1)) return;

      executeSQL("CREATE TABLE ChangeDBAsyncTest (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");
      executeSQL("INSERT INTO ChangeDBAsyncTest (id, name) VALUES (1,'test1')");
      executeSQL("INSERT INTO ChangeDBAsyncTest (id, name) VALUES (2,'test2')");
      executeSQL("INSERT INTO ChangeDBAsyncTest (id, name) VALUES (3,'test3')");

      await connection.ChangeDataBaseAsync(ts.baseDBName + "1");

      MySqlDataAdapter da = new MySqlDataAdapter(String.Format("SELECT id, name FROM `{0}`.ChangeDBAsyncTest", ts.baseDBName + "0"), connection);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataSet ds = new DataSet();
      da.Fill(ds);

      Assert.Equal(3, ds.Tables[0].Rows.Count);

      ds.Tables[0].Rows[0]["id"] = 4;
      DataSet changes = ds.GetChanges();
      da.Update(changes);
      ds.Merge(changes);
      ds.AcceptChanges();
      cb.Dispose();
      await connection.ChangeDataBaseAsync(ts.baseDBName + "0");
    }

    [Fact]
    public async Task OpenAndCloseConnectionAsync()
    {
      string connStr2 = ts.GetConnection(false).ConnectionString;
      MySqlConnection c = new MySqlConnection(connStr2);
      await c.OpenAsync();
      await c.CloseAsync();
    }

    [Fact]
    public async Task ClearPoolAsync()
    {
      MySqlConnection c1 = new MySqlConnection(ts.GetConnection(true).ConnectionString);
      MySqlConnection c2 = new MySqlConnection(ts.GetConnection(true).ConnectionString);
      c1.Open();
      c2.Open();
      c1.Close();
      c2.Close();
      await c1.ClearPoolAsync(c1);
      await c2.ClearPoolAsync(c1);
    }

    [Fact]
    public async Task ClearAllPoolsAsync()
    {
      MySqlConnection c1 = new MySqlConnection(ts.GetConnection(true).ConnectionString);
      MySqlConnection c2 = new MySqlConnection(ts.GetConnection(true).ConnectionString);
      c1.Open();
      c2.Open();
      c1.Close();
      c2.Close();
      await c1.ClearAllPoolsAsync();
      await c2.ClearAllPoolsAsync();
    }

    [Fact]
    public async Task GetSchemaCollectionAsync()
    {
      MySqlConnection c1 = new MySqlConnection(ts.GetConnection(true).ConnectionString);
      c1.Open();
      MySqlSchemaCollection schemaColl = await c1.GetSchemaCollectionAsync(SchemaProvider.MetaCollection, null);
      c1.Close();
      Assert.NotNull(schemaColl);
    }

    #endregion

    #region Adapter
    [Fact]
    public async Task FillAsync()
    {
      executeSQL("CREATE TABLE FillAsyncTest (id INT NOT NULL AUTO_INCREMENT, id2 INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, OriginalId INT, PRIMARY KEY(id, id2))");
      executeSQL("INSERT INTO FillAsyncTest (id, id2, name, dt) VALUES (NULL, 1, 'Name 1', Now())");
      executeSQL("INSERT INTO FillAsyncTest (id, id2, name, dt) VALUES (NULL, 2, NULL, Now())");
      executeSQL("INSERT INTO FillAsyncTest (id, id2, name, dt) VALUES (NULL, 3, '', Now())");

      MySqlDataAdapter da = new MySqlDataAdapter("select * from FillAsyncTest", connection);
      DataSet ds = new DataSet();
      await da.FillAsync(ds, "FillAsyncTest");

      Assert.Equal(1, ds.Tables.Count);
      Assert.Equal(3, ds.Tables[0].Rows.Count);

      Assert.Equal(1, ds.Tables[0].Rows[0]["id2"]);
      Assert.Equal(2, ds.Tables[0].Rows[1]["id2"]);
      Assert.Equal(3, ds.Tables[0].Rows[2]["id2"]);

      Assert.Equal("Name 1", ds.Tables[0].Rows[0]["name"]);
      Assert.Equal(DBNull.Value, ds.Tables[0].Rows[1]["name"]);
      Assert.Equal(String.Empty, ds.Tables[0].Rows[2]["name"]);
    }

    [Fact]
    public async Task FillSchemaAsync()
    {
      if (ts.version < new Version(5, 0)) return;

      executeSQL("CREATE PROCEDURE FillSchemaAsyncSpTest() BEGIN SELECT * FROM FillSchemaAsyncTest; END");
      executeSQL(@"CREATE TABLE FillSchemaAsyncTest(id INT AUTO_INCREMENT, name VARCHAR(20), PRIMARY KEY (id)) ");

      MySqlCommand cmd = new MySqlCommand("FillSchemaAsyncSpTest", connection);
      cmd.CommandType = CommandType.StoredProcedure;

      MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
      reader.Read();
      reader.Close();

      MySqlDataAdapter da = new MySqlDataAdapter(cmd);
      DataTable schema = new DataTable();
      await da.FillSchemaAsync(schema, SchemaType.Source);
      Assert.Equal(2, schema.Columns.Count);
    }

    [Fact]
    public async Task UpdateAsync()
    {
      executeSQL("CREATE TABLE UpdateAsyncTest (id INT NOT NULL AUTO_INCREMENT, id2 INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME, ts TIMESTAMP, OriginalId INT, PRIMARY KEY(id, id2))");
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM UpdateAsyncTest", connection);
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
      cb.Dispose();
    }

    #endregion

    #region MySqlScript
    [Fact]
    public async Task ExecuteScriptWithProceduresAsync()
    {
      if (ts.version < new Version(5, 0)) return;

      string spTpl = @"CREATE PROCEDURE `ScriptWithProceduresAsyncSpTest{0}`() NOT DETERMINISTIC CONTAINS SQL SQL SECURITY DEFINER COMMENT ''  BEGIN SELECT 1,2,3; END{1}";
      statementCount = 0;
      string scriptText = String.Empty;
      for (int i = 0; i < 10; i++)
      {
        scriptText += String.Format(spTpl, i, "$$");
      }
      MySqlScript script = new MySqlScript(scriptText);
      script.StatementExecuted += new MySqlStatementExecutedEventHandler(delegate(object sender, MySqlScriptEventArgs e)
      {
        string stmt = String.Format(spTpl, statementCount++, null);
        Assert.Equal(stmt, e.StatementText);
      });
      script.Connection = connection;
      script.Delimiter = "$$";
      int count = await script.ExecuteAsync();
      Assert.Equal(10, count);

      MySqlCommand cmd = new MySqlCommand(String.Format(@"SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = '{0}' AND routine_name LIKE 'ScriptWithProceduresAsyncSpTest%'", ts.baseDBName + "0"), connection);
      Assert.Equal(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Fact]
    public async Task ExecuteScriptWithInsertsAsync()
    {
      executeSQL("CREATE TABLE ScriptWithInsertsAsyncTest (id int, name varchar(50))");
      string queryTpl = @"INSERT INTO ScriptWithInsertsAsyncTest (id, name) VALUES ({0}, 'a "" na;me'){1}";
      statementCount = 0;
      string scriptText = String.Empty;
      for (int i = 0; i < 10; i++)
      {
        scriptText += String.Format(queryTpl, i, ";");
      }
      MySqlScript script = new MySqlScript(scriptText);
      script.Connection = connection;
      script.StatementExecuted += new MySqlStatementExecutedEventHandler(delegate(object sender, MySqlScriptEventArgs e)
      {
        string stmt = String.Format(queryTpl, statementCount++, null);
        Assert.Equal(stmt, e.StatementText);
      });

      int count = await script.ExecuteAsync();
      Assert.Equal(10, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM ScriptWithInsertsAsyncTest", connection);
      Assert.Equal(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    #endregion

    #region MySqlHelper
    [Fact]
    public async Task MSH_ExecuteNonQueryAsync()
    {
      if (ts.version < new Version(5, 0)) return;
      executeSQL("CREATE TABLE HelperNonQueryAsyncTest (id int)");
      executeSQL("CREATE PROCEDURE HelperNonQueryAsyncSpTest() BEGIN SET @x=0; REPEAT INSERT INTO HelperNonQueryAsyncTest VALUES(@x); SET @x=@x+1; UNTIL @x = 100 END REPEAT; END");
      
      int result = await MySqlHelper.ExecuteNonQueryAsync(connection, "call HelperNonQueryAsyncSpTest", null);
      Assert.NotEqual(-1, result);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM HelperNonQueryAsyncTest;", connection);
      cmd.CommandType = System.Data.CommandType.Text;
      object cnt = cmd.ExecuteScalar();
      Assert.Equal(100, Convert.ToInt32(cnt));
    }

    [Fact]
    public async Task MSH_ExecuteDataSetAsync()
    {
      executeSQL("CREATE TABLE HelperDataSetAsyncTable1 (`key` INT, PRIMARY KEY(`key`))");
      executeSQL("CREATE TABLE HelperDataSetAsyncTable2 (`key` INT, PRIMARY KEY(`key`))");
      executeSQL("INSERT INTO HelperDataSetAsyncTable1 VALUES (1)");
      executeSQL("INSERT INTO HelperDataSetAsyncTable2 VALUES (1)");

      string sql = "SELECT HelperDataSetAsyncTable1.key FROM HelperDataSetAsyncTable1 WHERE HelperDataSetAsyncTable1.key=1; SELECT HelperDataSetAsyncTable2.key FROM HelperDataSetAsyncTable2 WHERE HelperDataSetAsyncTable2.key=1";
      DataSet ds = await MySqlHelper.ExecuteDatasetAsync(connection, sql, null);

      Assert.Equal(2, ds.Tables.Count);
      Assert.Equal(1, ds.Tables[0].Rows.Count);
      Assert.Equal(1, ds.Tables[1].Rows.Count);
      Assert.Equal(1, ds.Tables[0].Rows[0]["key"]);
      Assert.Equal(1, ds.Tables[1].Rows[0]["key"]);
    }

    [Fact]
    public async Task MSH_ExecuteReaderAsync()
    {
      if (ts.version < new Version(5, 0)) return;

      if (connection.State != ConnectionState.Open)
        connection.Open();

      executeSQL("CREATE TABLE HelperReaderAsyncTest (id int)");
      executeSQL("CREATE PROCEDURE HelperReaderAsyncSpTest() BEGIN INSERT INTO HelperReaderAsyncTest VALUES(1); SELECT SLEEP(2); SELECT 'done'; END");

      using (MySqlDataReader reader = await MySqlHelper.ExecuteReaderAsync(connection, "call HelperReaderAsyncSpTest"))
      {
        Assert.NotNull(reader);
        Assert.True(reader.Read(), "can read");
        Assert.True(reader.NextResult());
        Assert.True(reader.Read());
        Assert.Equal("done", reader.GetString(0));
        reader.Close();

        MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM HelperReaderAsyncTest", connection);
        cmd.CommandType = CommandType.Text;
        object cnt = cmd.ExecuteScalar();
        Assert.Equal(1, Convert.ToInt32(cnt));
      }
    }

    [Fact]
    public async Task MSH_ExecuteScalarAsync()
    {
      if (ts.version < new Version(5, 0)) return;

      if (connection.State != ConnectionState.Open)
        connection.Open();

      executeSQL("CREATE TABLE HelperScalarAsyncTest (`key` INT, PRIMARY KEY(`key`))");
      executeSQL("INSERT INTO HelperScalarAsyncTest VALUES (1)");

      object result = await MySqlHelper.ExecuteScalarAsync(connection, "SELECT HelperScalarAsyncTest.key FROM HelperScalarAsyncTest WHERE HelperScalarAsyncTest.key=1;");
      Assert.Equal(1, int.Parse(result.ToString()));
    }
    #endregion

    #endregion
  }
}
