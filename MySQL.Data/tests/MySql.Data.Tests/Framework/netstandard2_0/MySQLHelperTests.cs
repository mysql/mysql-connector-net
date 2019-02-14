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
using System.Threading;

namespace MySql.Data.MySqlClient.Tests
{
  public class MySQLHelperTests : TestBase
  {
    public MySQLHelperTests(TestFixture fixture) : base(fixture)
    {
    }

    /// <summary>
    /// Bug #62585	MySql Connector/NET 6.4.3+ Doesn't escape quotation mark (U+0022)
    /// </summary>
    [Fact(Skip = "Not compatible with linux")]
    public void EscapeStringMethodCanEscapeQuotationMark()
    {
      executeSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO test VALUES (1,\"firstname\")", Connection);
      cmd.ExecuteNonQuery();

      cmd = new MySqlCommand("UPDATE test SET name = \"" + MySqlHelper.EscapeString("test\"name\"") + "\";", Connection);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT name FROM Test WHERE id=1";
      string name = (string)cmd.ExecuteScalar();

      Assert.True("test\"name\"" == name, "Update result with quotation mark");
    }

    #region Async
    [Fact]
    public async Task ExecuteNonQueryAsync()
    {
      executeSQL("CREATE TABLE MSHNonQueryAsyncTest (id int)");
      executeSQL("CREATE PROCEDURE MSHNonQueryAsyncSpTest() BEGIN SET @x=0; REPEAT INSERT INTO MSHNonQueryAsyncTest VALUES(@x); SET @x=@x+1; UNTIL @x = 100 END REPEAT; END");

      try
      {
        int result = await MySqlHelper.ExecuteNonQueryAsync(Connection, "call MSHNonQueryAsyncSpTest", null);
        Assert.NotEqual(-1, result);

        MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM MSHNonQueryAsyncTest;", Connection);
        cmd.CommandType = System.Data.CommandType.Text;
        object cnt = cmd.ExecuteScalar();
        Assert.Equal(100, Convert.ToInt32(cnt));
      }
      finally
      {
        executeSQL("DROP PROCEDURE MSHNonQueryAsyncSpTest");
        executeSQL("DROP TABLE MSHNonQueryAsyncTest");
      }
    }

    [Fact]
    public async Task ExecuteDataSetAsync()
    {
      executeSQL("CREATE TABLE MSHDataSetAsyncTable1 (`key` INT, PRIMARY KEY(`key`))");
      executeSQL("CREATE TABLE MSHDataSetAsyncTable2 (`key` INT, PRIMARY KEY(`key`))");
      executeSQL("INSERT INTO MSHDataSetAsyncTable1 VALUES (1)");
      executeSQL("INSERT INTO MSHDataSetAsyncTable2 VALUES (1)");

      try
      {
        string sql = "SELECT MSHDataSetAsyncTable1.key FROM MSHDataSetAsyncTable1 WHERE MSHDataSetAsyncTable1.key=1; " +
                     "SELECT MSHDataSetAsyncTable2.key FROM MSHDataSetAsyncTable2 WHERE MSHDataSetAsyncTable2.key=1";
        DataSet ds = await MySqlHelper.ExecuteDatasetAsync(Connection, sql, null);
        Assert.Equal(2, ds.Tables.Count);
        Assert.Equal(1, ds.Tables[0].Rows.Count);
        Assert.Equal(1, ds.Tables[1].Rows.Count);
        Assert.Equal(1, ds.Tables[0].Rows[0]["key"]);
        Assert.Equal(1, ds.Tables[1].Rows[0]["key"]);

        ds = await MySqlHelper.ExecuteDatasetAsync(Connection, sql);
        Assert.Equal(2, ds.Tables.Count);
        Assert.Equal(1, ds.Tables[0].Rows.Count);
        Assert.Equal(1, ds.Tables[1].Rows[0]["key"]);

        ds = await MySqlHelper.ExecuteDatasetAsync(Connection.ConnectionString, sql, null);
        Assert.Equal(1, ds.Tables[0].Rows.Count);
        Assert.Equal(1, ds.Tables[1].Rows.Count);
        Assert.Equal(1, ds.Tables[0].Rows[0]["key"]);

        ds = await MySqlHelper.ExecuteDatasetAsync(Connection.ConnectionString, sql);
        Assert.Equal(2, ds.Tables.Count);
        Assert.Equal(1, ds.Tables[0].Rows.Count);
        Assert.Equal(1, ds.Tables[1].Rows[0]["key"]);

        ds = await MySqlHelper.ExecuteDatasetAsync(Connection.ConnectionString, sql, CancellationToken.None, null);
        Assert.Equal(2, ds.Tables.Count);
        Assert.Equal(1, ds.Tables[0].Rows.Count);

        ds = await MySqlHelper.ExecuteDatasetAsync(Connection, sql, CancellationToken.None);
        Assert.Equal(2, ds.Tables.Count);
        Assert.Equal(1, ds.Tables[0].Rows[0]["key"]);
      }
      finally
      {
        executeSQL("DROP TABLE MSHDataSetAsyncTable1");
        executeSQL("DROP TABLE MSHDataSetAsyncTable2");
      }
    }

    [Fact]
    public async Task ExecuteReaderAsync()
    {
      executeSQL("CREATE TABLE MSHReaderAsyncTest (id int)");
      executeSQL("CREATE PROCEDURE MSHReaderAsyncSpTest() BEGIN INSERT INTO MSHReaderAsyncTest VALUES(1); SELECT SLEEP(2); SELECT 'done'; END");

      try
      {
        using (MySqlDataReader reader = await MySqlHelper.ExecuteReaderAsync(Connection, "call MSHReaderAsyncSpTest"))
        {
          Assert.NotNull(reader);
          Assert.True(reader.Read(), "can read");
          Assert.True(reader.NextResult());
          Assert.True(reader.Read());
          Assert.Equal("done", reader.GetString(0));
          reader.Close();

          MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM MSHReaderAsyncTest", Connection);
          cmd.CommandType = CommandType.Text;
          object cnt = cmd.ExecuteScalar();
          Assert.Equal(1, Convert.ToInt32(cnt));
        }
      }
      finally
      {
        executeSQL("DROP PROCEDURE MSHReaderAsyncSpTest");
        executeSQL("DROP TABLE MSHReaderAsyncTest");
      }
    }

    [Fact]
    public async Task ExecuteScalarAsync()
    {
      executeSQL("CREATE TABLE MSHScalarAsyncTable1 (`key` INT, PRIMARY KEY(`key`))");
      executeSQL("INSERT INTO MSHScalarAsyncTable1 VALUES (1)");

      try
      {
        object result = await MySqlHelper.ExecuteScalarAsync(Connection, "SELECT MSHScalarAsyncTable1.key FROM MSHScalarAsyncTable1 WHERE MSHScalarAsyncTable1.key=1;");
        Assert.Equal(1, int.Parse(result.ToString()));
      }
      finally
      {
        executeSQL("DROP TABLE MSHScalarAsyncTable1");
      }
    }

    [Fact]
    public async Task ExecuteDataRowAsync()
    {
      executeSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100))");
      executeSQL("INSERT INTO Test VALUES (1, 'name')");

      try
      {
        DataRow result = await MySqlHelper.ExecuteDataRowAsync(Connection.ConnectionString, "SELECT name FROM Test WHERE id=1", null);
        Assert.Equal("name", result[0]);

      }
      finally
      {
        executeSQL("DROP TABLE Test");
      }
    }

    [Fact]
    public void UpdateDataSet()
    {
      executeSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100), PRIMARY KEY(`id`))");
      executeSQL("INSERT INTO Test VALUES (1, 'name')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      da.TableMappings.Add("Table", "Test");
      DataSet ds = new DataSet();
      da.Fill(ds);
      ds.Tables["Test"].Rows[0][1] = "updatedName";

      MySqlHelper.UpdateDataSet(Connection.ConnectionString, "SELECT * FROM Test", ds, "Test");
      DataRow result = ds.Tables["Test"].Rows[0];
      Assert.Equal("updatedName", result["name"]);
    }

    [Fact]
    public async Task UpdateDataSetAsync()
    {
      executeSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100), PRIMARY KEY(`id`))");
      executeSQL("INSERT INTO Test VALUES (1, 'name')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      da.TableMappings.Add("Table", "Test");
      DataSet ds = new DataSet();
      da.Fill(ds);
      ds.Tables["Test"].Rows[0][1] = "updatedName";

      try
      {
        await MySqlHelper.UpdateDataSetAsync(Connection.ConnectionString, "SELECT * FROM Test", ds, "Test");
        DataRow result = ds.Tables["Test"].Rows[0];
        Assert.Equal("updatedName", result["name"]);
      }
      finally
      {
        executeSQL("DROP TABLE Test");
      }
    }

    [Fact]
    public void ExecuteDataset()
    {
      executeSQL("CREATE TABLE Test (id int NOT NULL, name VARCHAR(100), PRIMARY KEY(`id`))");
      executeSQL("INSERT INTO Test VALUES (1, 'name')");
      executeSQL("INSERT INTO Test VALUES (2, 'name2')");

      DataSet ds = MySqlHelper.ExecuteDataset(Connection, "SELECT * FROM Test");
      Assert.Single(ds.Tables);
      Assert.Equal(2, ds.Tables[0].Rows.Count);
      Assert.Equal("name", ds.Tables[0].Rows[0][1]);

      MySqlParameter mySqlParameter = new MySqlParameter("@id", 2);
      ds = MySqlHelper.ExecuteDataset(Connection, "SELECT * FROM Test WHERE id = @id", mySqlParameter);
      Assert.Single(ds.Tables);
      Assert.Equal(1, ds.Tables[0].Rows.Count);
      Assert.Equal("name2", ds.Tables[0].Rows[0][1]);

      ds = MySqlHelper.ExecuteDataset(Connection.ConnectionString, "SELECT * FROM Test", null);
      Assert.Single(ds.Tables);
      Assert.Equal(2, ds.Tables[0].Rows.Count);
      Assert.Equal("name", ds.Tables[0].Rows[0][1]);
      Assert.Equal("name2", ds.Tables[0].Rows[1][1]);
    }
    #endregion
  }
}
