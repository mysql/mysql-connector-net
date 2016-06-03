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


namespace MySql.Data.MySqlClient.Tests
{
  public class MySqlCommandBuilderTests : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;

    public void SetFixture(SetUpClass data)
    {
      st = data;
    }

    [Fact]
    public void MultiWord()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);

      DataRow row = dt.NewRow();
      row["id"] = 1;
      row["name"] = "Name";
      row["dt"] = DBNull.Value;
      row["tm"] = DBNull.Value;
      row["multi word"] = 2;
      dt.Rows.Add(row);
      da.Update(dt);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal(2, dt.Rows[0]["multi word"]);

      dt.Rows[0]["multi word"] = 3;
      da.Update(dt);
      cb.Dispose();
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal(3, dt.Rows[0]["multi word"]);
    }

    [Fact]
    public void LastOneWins()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");
      st.execSQL("INSERT INTO Test (id, name) VALUES (1, 'Test')");

      MySqlCommandBuilder cb = new MySqlCommandBuilder(
          new MySqlDataAdapter("SELECT * FROM Test", st.conn));
      MySqlDataAdapter da = cb.DataAdapter;
      cb.ConflictOption = ConflictOption.OverwriteChanges;
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);

      st.execSQL("UPDATE Test SET name='Test2' WHERE id=1");

      dt.Rows[0]["name"] = "Test3";
      Assert.Equal(1, da.Update(dt));

      dt.Rows.Clear();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("Test3", dt.Rows[0]["name"]);
    }

    [Fact]
    public void NotLastOneWins()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");
      st.execSQL("INSERT INTO Test (id, name) VALUES (1, 'Test')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      cb.ConflictOption = ConflictOption.CompareAllSearchableValues;
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);

      st.execSQL("UPDATE Test SET name='Test2' WHERE id=1");
      dt.Rows[0]["name"] = "Test3";
      Exception ex = Assert.Throws<DBConcurrencyException>(() => (da.Update(dt)));
      Assert.Equal(ex.Message, "Concurrency violation: the UpdateCommand affected 0 of the expected 1 records.");

      dt.Rows.Clear();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("Test2", dt.Rows[0]["name"]);
    }

    /// <summary>
    /// Bug #8574 - MySqlCommandBuilder unable to support sub-queries
    /// Bug #11947 - MySQLCommandBuilder mishandling CONCAT() aliased column
    /// </summary>
    [Fact]
    public void UsingFunctions()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");
      st.execSQL("INSERT INTO Test (id, name) VALUES (1,'test1')");
      st.execSQL("INSERT INTO Test (id, name) VALUES (2,'test2')");
      st.execSQL("INSERT INTO Test (id, name) VALUES (3,'test3')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT id, name, now() as ServerTime FROM Test", st.conn);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);

      dt.Rows[0]["id"] = 4;
      da.Update(dt);

      da.SelectCommand.CommandText = "SELECT id, name, CONCAT(name, '  boo') as newname from Test where id=4";
      dt.Clear();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("test1", dt.Rows[0]["name"]);
      Assert.Equal("test1  boo", dt.Rows[0]["newname"]);

      dt.Rows[0]["id"] = 5;
      da.Update(dt);

      dt.Clear();
      da.SelectCommand.CommandText = "SELECT * FROM Test WHERE id=5";
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal("test1", dt.Rows[0]["name"]);

      da.SelectCommand.CommandText = "SELECT *, now() as stime FROM Test WHERE id<4";
      cb = new MySqlCommandBuilder(da);
      cb.ConflictOption = ConflictOption.OverwriteChanges;
      da.InsertCommand = cb.GetInsertCommand();
    }

    /// <summary>
    /// Bug #8382  	Commandbuilder does not handle queries to other databases than the default one-
    /// </summary>
    [Fact]
    public void DifferentDatabase()
    {
      if (st.Version < new Version(4, 1)) return;

      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");
      st.execSQL("INSERT INTO Test (id, name) VALUES (1,'test1')");
      st.execSQL("INSERT INTO Test (id, name) VALUES (2,'test2')");
      st.execSQL("INSERT INTO Test (id, name) VALUES (3,'test3')");

      st.conn.ChangeDatabase(st.database1);

      MySqlDataAdapter da = new MySqlDataAdapter(
          String.Format("SELECT id, name FROM `{0}`.Test", st.database0), st.conn);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataSet ds = new DataSet();
      da.Fill(ds);

      ds.Tables[0].Rows[0]["id"] = 4;
      DataSet changes = ds.GetChanges();
      da.Update(changes);
      ds.Merge(changes);
      ds.AcceptChanges();
      cb.Dispose();

      st.conn.ChangeDatabase(st.database0);
    }

    /// <summary>
    /// Bug #13036  	Returns error when field names contain any of the following chars %<>()/ etc
    /// </summary>
    [Fact]
    public void SpecialCharactersInFieldNames()
    {
      st.execSQL("CREATE TABLE Test (`col%1` int PRIMARY KEY, `col()2` int, `col<>3` int, `col/4` int)");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      cb.ToString();  // keep the compiler happy
      DataTable dt = new DataTable();
      da.Fill(dt);
      DataRow row = dt.NewRow();
      row[0] = 1;
      row[1] = 2;
      row[2] = 3;
      row[3] = 4;
      dt.Rows.Add(row);
      da.Update(dt);
    }

    /// <summary>
    /// Bug #14631  	"#42000Query was empty"
    /// </summary>
    [Fact]
    public void SemicolonAtEndOfSQL()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), PRIMARY KEY(id))");
      st.execSQL("INSERT INTO Test VALUES(1, 'Data')");

      DataSet ds = new DataSet();
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM `Test`;", st.conn);
      da.FillSchema(ds, SchemaType.Source, "Test");

      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);
      dt.Rows[0]["id"] = 2;
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      cb.Dispose();
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal(2, dt.Rows[0]["id"]);
    }

    /// <summary>
    /// Bug #23862 Problem with CommandBuilder 'GetInsertCommand' method 
    /// </summary>
    [Fact]
    public void AutoIncrementColumnsOnInsert()
    {
      st.execSQL("CREATE TABLE Test (id INT UNSIGNED NOT NULL AUTO_INCREMENT, " +
          "name VARCHAR(100), PRIMARY KEY(id))");
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);

      da.InsertCommand = cb.GetInsertCommand();
      da.InsertCommand.CommandText += "; SELECT last_insert_id()";
      da.InsertCommand.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;

      DataTable dt = new DataTable();
      da.Fill(dt);
      dt.Columns[0].AutoIncrement = true;
      Assert.True(dt.Columns[0].AutoIncrement);
      dt.Columns[0].AutoIncrementSeed = -1;
      dt.Columns[0].AutoIncrementStep = -1;
      DataRow row = dt.NewRow();
      row["name"] = "Test";

      dt.Rows.Add(row);
      da.Update(dt);

      dt.Clear();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal(1, dt.Rows[0]["id"]);
      Assert.Equal("Test", dt.Rows[0]["name"]);
      cb.Dispose();
    }

    /// <summary>
    /// Bug #25569 UpdateRowSource.FirstReturnedRecord does not work 
    /// </summary>
    [Fact]
    public void AutoIncrementColumnsOnInsert2()
    {
      st.execSQL("CREATE TABLE Test (id INT UNSIGNED NOT NULL " +
          "AUTO_INCREMENT PRIMARY KEY, name VARCHAR(20))");
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);

      MySqlCommand cmd = (MySqlCommand)(cb.GetInsertCommand() as ICloneable).Clone();
      cmd.CommandText += "; SELECT last_insert_id() as id";
      cmd.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;
      da.InsertCommand = cmd;

      DataTable dt = new DataTable();
      da.Fill(dt);
      dt.Rows.Clear();

      DataRow row = dt.NewRow();
      row["name"] = "Test";
      dt.Rows.Add(row);
      da.Update(dt);
      Assert.Equal(1, Convert.ToInt32(dt.Rows[0]["id"]));
      Assert.Equal("Test", dt.Rows[0]["name"]);

      row = dt.NewRow();
      row["name"] = "Test2";
      dt.Rows.Add(row);
      da.Update(dt);
      Assert.Equal(2, Convert.ToInt32(dt.Rows[1]["id"]));
      Assert.Equal("Test2", dt.Rows[1]["name"]);

      Assert.Equal(1, Convert.ToInt32(dt.Rows[0]["id"]));
    }

    [Fact]
    public void MultiUpdate()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100), dt DATETIME, tm TIME,  `multi word` int, PRIMARY KEY(id))");
      st.execSQL("INSERT INTO  Test (id, name) VALUES (1, 'test1')");
      st.execSQL("INSERT INTO  Test (id, name) VALUES (2, 'test2')");
      st.execSQL("INSERT INTO  Test (id, name) VALUES (3, 'test3')");
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
      MySqlCommandBuilder cb = new MySqlCommandBuilder(da);
      DataTable dt = new DataTable();
      da.Fill(dt);

      dt.Rows[0]["id"] = 4;
      dt.Rows[0]["name"] = "test4";
      dt.Rows[1]["id"] = 5;
      dt.Rows[1]["name"] = "test5";
      dt.Rows[2]["id"] = 6;
      dt.Rows[2]["name"] = "test6";
      DataTable changes = dt.GetChanges();
      da.Update(changes);
      dt.AcceptChanges();

      dt.Rows[0]["id"] = 7;
      dt.Rows[0]["name"] = "test7";
      dt.Rows[1]["id"] = 8;
      dt.Rows[1]["name"] = "test8";
      dt.Rows[2]["id"] = 9;
      dt.Rows[2]["name"] = "test9";
      changes = dt.GetChanges();
      da.Update(changes);
      dt.AcceptChanges();
      cb.Dispose();
    }

    /// <summary>
    /// Bug #30077  	MySqlDataAdapter.Update() exception due to date field format
    /// </summary>
    [Fact]
    public void UpdatingWithDateInKey()
    {
      st.execSQL("CREATE TABLE Test (cod INT, dt DATE, PRIMARY KEY(cod, dt))");

      st.execSQL("INSERT INTO Test (cod, dt) VALUES (1, '2006-1-1')");
      st.execSQL("INSERT INTO Test (cod, dt) VALUES (2, '2006-1-2')");
      st.execSQL("INSERT INTO Test (cod, dt) VALUES (3, '2006-1-3')");
      st.execSQL("INSERT INTO Test (cod, dt) VALUES (4, '2006-1-4')");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test ORDER BY cod", st.conn);
      MySqlCommandBuilder bld = new MySqlCommandBuilder(da);
      bld.ConflictOption = ConflictOption.OverwriteChanges;
      DataTable dt = new DataTable();
      da.Fill(dt);
      dt.Rows[0]["cod"] = 6;
      da.Update(dt);

      dt.Clear();
      da.SelectCommand.CommandText = "SELECT * FROM Test WHERE cod=6";
      da.Fill(dt);
      Assert.Equal(6, dt.Rows[0]["cod"]);
    }

    /// <summary>
    /// Bug #35492 Please implement DbCommandBuilder.QuoteIdentifier 
    /// </summary>
    [Fact]
    public void QuoteAndUnquoteIdentifiers()
    {
      MySqlCommandBuilder cb = new MySqlCommandBuilder();
      Assert.Equal("`boo`", cb.QuoteIdentifier("boo"));
      Assert.Equal("`bo``o`", cb.QuoteIdentifier("bo`o"));
      Assert.Equal("`boo`", cb.QuoteIdentifier("`boo`"));

      // now do the unquoting
      Assert.Equal("boo", cb.UnquoteIdentifier("`boo`"));
      Assert.Equal("`boo", cb.UnquoteIdentifier("`boo"));
      Assert.Equal("bo`o", cb.UnquoteIdentifier("`bo``o`"));
    }

    public void Dispose()
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");    
    }
  }
}
