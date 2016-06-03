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
using System.IO;

namespace MySql.Data.MySqlClient.Tests
{
  public class Syntax : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;

    private static string fillError = null;


    public void SetFixture(SetUpClass data)
    {
      st = data;
     
    }

    public void Dispose()
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");      
    }

    [Fact]
    public void ShowCreateTable()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      MySqlDataAdapter da = new MySqlDataAdapter("SHOW CREATE TABLE Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal(2, dt.Columns.Count);
    }

    [Fact]
    public void ProblemCharsInSQLUTF8()
    {
      if (st.Version < new Version(4, 1)) return;

      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), mt MEDIUMTEXT, " +
            "PRIMARY KEY(id)) CHAR SET utf8");

      using (MySqlConnection c = new MySqlConnection(st.GetConnectionString(true) + ";charset=utf8"))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?text, ?mt)", c);
        cmd.Parameters.AddWithValue("?id", 1);
        cmd.Parameters.AddWithValue("?text", "This is my;test ? string–’‘’“”…");
        cmd.Parameters.AddWithValue("?mt", "My MT string: ?");
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM Test";
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.Equal(1, reader.GetInt32(0));
          Assert.Equal("This is my;test ? string–’‘’“”…", reader.GetString(1));
          Assert.Equal("My MT string: ?", reader.GetString(2));
        }
      }
    }


    [Fact]
    public void ProblemCharsInSQL()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), mt MEDIUMTEXT, " +
            "PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?text, ?mt)", st.conn);
      cmd.Parameters.AddWithValue("?id", 1);
      cmd.Parameters.AddWithValue("?text", "This is my;test ? string-'''\"\".");
      cmd.Parameters.AddWithValue("?mt", "My MT string: ?");
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("This is my;test ? string-'''\"\".", reader.GetString(1));
        Assert.Equal("My MT string: ?", reader.GetString(2));
      }
    }

    [Fact]
    public void LoadDataLocalInfile()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      string connString = st.conn.ConnectionString + ";pooling=false";
      MySqlConnection c = new MySqlConnection(connString);
      c.Open();

      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(path);
      for (int i = 0; i < 2000000; i++)
        sw.WriteLine(i + ",'Test'");
      sw.Flush();
      sw.Close();

      path = path.Replace(@"\", @"\\");
      MySqlCommand cmd = new MySqlCommand(
        "LOAD DATA LOCAL INFILE '" + path + "' INTO TABLE Test FIELDS TERMINATED BY ','", st.conn);
      cmd.CommandTimeout = 0;

      object cnt = 0;
      cnt = cmd.ExecuteNonQuery();
      Assert.Equal(2000000, Convert.ToInt32(cnt));

      cmd.CommandText = "SELECT COUNT(*) FROM Test";
      cnt = cmd.ExecuteScalar();
      Assert.Equal(2000000, Convert.ToInt32(cnt));

      c.Close();
    }

    [Fact]
    public void ShowTablesInNonExistentDb()
    {
      MySqlCommand cmd = new MySqlCommand("SHOW TABLES FROM dummy", st.conn);
      Assert.Throws<MySqlException>(() => cmd.ExecuteReader());
      //try
      //{
      //  using (MySqlDataReader reader = cmd.ExecuteReader())
      //  {
      //    Assert.Fail("ExecuteReader should not succeed");
      //  }
      //}
      //catch (MySqlException)
      //{
      //  Assert.Equal(ConnectionState.Open, st.conn.State);
      //}
    }

    [Fact]
    public void Bug6135()
    {
      string sql = @"CREATE TABLE `KLANT` (`KlantNummer` int(11) NOT NULL auto_increment, 
        `Username` varchar(50) NOT NULL default '', `Password` varchar(100) NOT NULL default '', 
        `Naam` varchar(100) NOT NULL default '', `Voornaam` varchar(100) NOT NULL default '',
        `Straat` varchar(100) NOT NULL default '', `StraatNr` varchar(10) NOT NULL default '',
        `Gemeente` varchar(100) NOT NULL default '', `Postcode` varchar(10) NOT NULL default '',
        `DefaultMail` varchar(255) default '', 	`BtwNr` varchar(50) default '',
        `ReceiveMail` tinyint(1) NOT NULL default '0',	`Online` tinyint(1) NOT NULL default '0',
        `LastVisit` timestamp NOT NULL, `Categorie` int(11) NOT NULL default '0',
        PRIMARY KEY  (`KlantNummer`),	UNIQUE KEY `UniqueUsername` (`Username`),
        UNIQUE KEY `UniqueDefaultMail` (`DefaultMail`)	)";
      st.createTable(sql, "MyISAM");

      MySqlCommand cmd = new MySqlCommand("SELECT * FROM KLANT", st.conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.Read()) { }
      }
    }

    [Fact]
    public void Sum()
    {
      st.execSQL("CREATE TABLE Test (field1 mediumint(9) default '0', field2 float(9,3) " +
        "default '0.000', field3 double(15,3) default '0.000') engine=innodb ");
      st.execSQL("INSERT INTO Test values (1,1,1)");

      MySqlCommand cmd2 = new MySqlCommand("SELECT sum(field2) FROM Test", st.conn);
      using (MySqlDataReader reader = cmd2.ExecuteReader())
      {
        reader.Read();
        object o = reader[0];
        Assert.Equal(1, Convert.ToInt32(o));
      }
    }

    [Fact]
    public void Sum2()
    {
      st.execSQL("CREATE TABLE Test (id int, count int)");
      st.execSQL("INSERT INTO Test VALUES (1, 21)");
      st.execSQL("INSERT INTO Test VALUES (1, 33)");
      st.execSQL("INSERT INTO Test VALUES (1, 16)");
      st.execSQL("INSERT INTO Test VALUES (1, 40)");

      MySqlCommand cmd = new MySqlCommand("SELECT id, SUM(count) FROM Test GROUP BY id", st.conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal(110, reader.GetDouble(1));
      }
    }

    [Fact]
    public void ForceWarnings()
    {
      if (st.Version < new Version(4, 1)) return;

      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      MySqlCommand cmd = new MySqlCommand(
        "SELECT * FROM Test; DROP TABLE IF EXISTS test2; SELECT * FROM Test", st.conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.NextResult()) { }
      }
    }

    [Fact]
    public void SettingAutoIncrementColumns()
    {
      st.execSQL("CREATE TABLE Test (id int auto_increment, name varchar(100), primary key(id))");
      st.execSQL("INSERT INTO Test VALUES (1, 'One')");
      st.execSQL("INSERT INTO Test VALUES (3, 'Two')");

      MySqlCommand cmd = new MySqlCommand("SELECT name FROM Test WHERE id=1", st.conn);
      object name = cmd.ExecuteScalar();
      Assert.Equal("One", name);

      cmd.CommandText = "SELECT name FROM Test WHERE id=3";
      name = cmd.ExecuteScalar();
      Assert.Equal("Two", name);

      //try
      //{
        Assert.Throws<MySqlException>(() => st.execSQL("INSERT INTO Test (id, name2) values (5, 'Three')"));
      //  Assert.Fail("This should have failed");
      //}
      //catch (MySqlException)
      //{
      //}
    }

    /// <summary>
    /// Bug #16645 FOUND_ROWS() Bug 
    /// </summary>
    [Fact]
    public void FoundRows()
    {
      st.execSQL("CREATE TABLE Test (testID int(11) NOT NULL auto_increment, testName varchar(100) default '', " +
          "PRIMARY KEY  (testID)) ENGINE=InnoDB DEFAULT CHARSET=latin1");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (NULL, 'test')", st.conn);
      for (int i = 0; i < 1000; i++)
        cmd.ExecuteNonQuery();
      cmd.CommandText = "SELECT SQL_CALC_FOUND_ROWS * FROM Test LIMIT 0, 10";
      cmd.ExecuteNonQuery();
      cmd.CommandText = "SELECT FOUND_ROWS()";
      object cnt = cmd.ExecuteScalar();
      Assert.Equal(1000, Convert.ToInt32(cnt));
    }

    [Fact]
    public void AutoIncrement()
    {
      st.execSQL("CREATE TABLE Test (testID int(11) NOT NULL auto_increment, testName varchar(100) default '', " +
          "PRIMARY KEY  (testID)) ENGINE=InnoDB DEFAULT CHARSET=latin1");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (NULL, 'test')", st.conn);
      cmd.ExecuteNonQuery();
      cmd.CommandText = "SELECT @@IDENTITY as 'Identity'";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        int ident = Int32.Parse(reader.GetValue(0).ToString());
        Assert.Equal(1, ident);
      }
    }

    /// <summary>
    /// Bug #21521 # Symbols not allowed in column/table names. 
    /// </summary>
    [Fact]
    public void CommentSymbolInTableName()
    {
      st.execSQL("CREATE TABLE Test (`PO#` int(11) NOT NULL auto_increment, " +
        "`PODate` date default NULL, PRIMARY KEY  (`PO#`))");
      st.execSQL("INSERT INTO Test ( `PO#`, `PODate` ) " +
        "VALUES ( NULL, '2006-01-01' )");

      string sql = "SELECT `PO#` AS PurchaseOrderNumber, " +
        "`PODate` AS OrderDate FROM  Test";
      MySqlCommand cmd = new MySqlCommand(sql, st.conn);
      MySqlDataAdapter da = new MySqlDataAdapter(cmd);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.Equal(1, dt.Rows.Count);
    }

    /// <summary>
    /// Bug #25178 Addition message in error 
    /// </summary>
    [Fact]
    public void ErrorMessage()
    {
      MySqlCommand cmd = new MySqlCommand("SELEKT NOW() as theTime", st.conn);
      try
      {
        cmd.ExecuteScalar();
      }
      catch (MySqlException ex)
      {
        string s = ex.Message;
        Assert.False(s.StartsWith("#", StringComparison.Ordinal));
      }
    }

    /// <summary>
    /// Bug #27221 describe SQL command returns all byte array on MySQL versions older than 4.1.15 
    /// </summary>
    [Fact]
    public void Describe()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      MySqlDataAdapter da = new MySqlDataAdapter("DESCRIBE Test", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      Assert.True(dt.Columns[0].DataType == typeof(string));
      Assert.True(dt.Columns[1].DataType == typeof(string));
      Assert.True(dt.Columns[2].DataType == typeof(string));
      Assert.True(dt.Columns[3].DataType == typeof(string));
      Assert.True(dt.Columns[4].DataType == typeof(string));
      Assert.True(dt.Columns[5].DataType == typeof(string));
    }

    [Fact]
    public void ShowTableStatus()
    {
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      MySqlDataAdapter da = new MySqlDataAdapter(
        String.Format("SHOW TABLE STATUS FROM `{0}` LIKE 'Test'",
        st.database0), st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);

      Assert.True(dt.Rows[0][0].GetType() == typeof(string));
    }

    /// <summary>
    /// Bug #26960 Connector .NET 5.0.5 / Visual Studio Plugin 1.1.2 
    /// </summary>
    [Fact]
    public void NullAsAType()
    {
      MySqlDataAdapter da = new MySqlDataAdapter(
        @"SELECT 'localhost' as SERVER_NAME, 
        null as CATALOG_NAME, database() as SCHEMA_NAME", st.conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      Assert.True(dt.Rows[0][0].GetType() == typeof(string));
      Assert.Equal(DBNull.Value, dt.Rows[0][1]);
      Assert.True(dt.Rows[0][2].GetType() == typeof(string));
    }

    [Fact]
    public void SpaceInDatabaseName()
    {
      string dbName = System.IO.Path.GetFileNameWithoutExtension(
        System.IO.Path.GetTempFileName()) + " x";
      try
      {
        st.suExecSQL(String.Format("CREATE DATABASE `{0}`", dbName));
        st.suExecSQL(String.Format("GRANT ALL ON `{0}`.* to 'test'@'localhost' identified by 'test'",
          dbName));
        st.suExecSQL(String.Format("GRANT ALL ON `{0}`.* to 'test'@'%' identified by 'test'",
          dbName));
        st.suExecSQL("FLUSH PRIVILEGES");

        string connStr = st.GetConnectionString(false) + ";database=" + dbName;
        MySqlConnection c = new MySqlConnection(connStr);
        c.Open();
        c.Close();
      }
      finally
      {
        st.suExecSQL(String.Format("DROP DATABASE `{0}`", dbName));
      }
    }

    /// <summary>
    /// Bug #28448  	show processlist; returns byte arrays in the resulting data table
    /// </summary>
    [Fact]
    public void ShowProcessList()
    {
      string connStr = st.GetConnectionString(true) + ";respect binary flags=false;";
      MySqlConnection c = new MySqlConnection(connStr);
      using (c)
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("show processlist", c);
        DataTable dt = new DataTable();

        using (MySqlDataReader rdr = cmd.ExecuteReader())
        {
          dt.Load(rdr);
        }
        DataRow row = dt.Rows[0];

        Assert.True(row["User"].GetType().Name == "String");
        Assert.True(row["Host"].GetType().Name == "String");
        Assert.True(row["Command"].GetType().Name == "String");
      }
    }

    [Fact]
    public void SemisAtStartAndEnd()
    {
      using (MySqlCommand cmd = new MySqlCommand(";;SELECT 1;;;", st.conn))
      {
        Assert.Equal(1, Convert.ToInt32(cmd.ExecuteScalar()));
      }
    }

    /// <summary>
    /// Bug #51610	Exception thrown inside Connector.NET
    /// </summary>
    [Fact]
    public void Bug51610()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT 'ABC', (0/`QOH`) from (SELECT 1 as `QOH`) `d1`", st.conn);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal("ABC", reader.GetString(0));
        Assert.Equal(0, reader.GetInt32(1));
      }

      cmd.CommandText = "SELECT 'ABC', (0-`QOH`) from (SELECT 1 as `QOH`) `d1`";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal("ABC", reader.GetString(0));
        Assert.Equal(-1, reader.GetInt32(1));
      }

      cmd.CommandText = "SELECT 'test 2010-03-04 @ 10:14'";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.Equal("test 2010-03-04 @ 10:14", reader.GetString(0));
      }

    }

    /// <summary>
    /// Bug #54386 : expression with parentheses in INSERT leads to invalid
    /// query when using batching
    /// </summary>
    [Fact]
    public void TokenizerBatching()
    {
      st.execSQL("CREATE TABLE Test (id INT, expr INT,name VARCHAR(20), PRIMARY KEY(id))");


      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test",
        st.conn);
      MySqlCommand ins = new MySqlCommand(
        "INSERT INTO test (id, expr, name) VALUES(?p1, (?p2 * 2) + 3, ?p3)",
        st.conn);
      da.InsertCommand = ins;
      ins.UpdatedRowSource = UpdateRowSource.None;
      ins.Parameters.Add("?p1", MySqlDbType.Int32).SourceColumn = "id";
      ins.Parameters.Add("?p2", MySqlDbType.Int32).SourceColumn = "expr";
      ins.Parameters.Add("?p3", MySqlDbType.VarChar, 20).SourceColumn = "name";

      DataTable dt = new DataTable();
      da.Fill(dt);

      for (int i = 1; i <= 100; i++)
      {
        DataRow row = dt.NewRow();
        row["id"] = i;
        row["expr"] = i;
        row["name"] = "name " + i;
        dt.Rows.Add(row);
      }

      da.UpdateBatchSize = 10;
      da.Update(dt);

    }
    /// <summary>
    /// Bug #51788	Error in SQL syntax not reported. A CLR exception was thrown instead,
    /// </summary>
    [Fact]
    public void NonTerminatedString()
    {
      st.execSQL("DROP TABLE IF EXISTS Test");
      st.execSQL("CREATE TABLE Test(id INT, name1 VARCHAR(20), name2 VARCHAR(20))");

      try
      {
        MySqlCommand cmd = new MySqlCommand(
          "INSERT INTO test VALUES (1, 'test 2010-03-04 @ 10:14, name2=' joe')", st.conn);
        cmd.ExecuteNonQuery();
      }
      catch (MySqlException)
      {
      }
    }

    /// <summary>
    /// Bug #53865 : crash in QueryNormalizer, "IN" clause incomplete
    /// </summary>
    [Fact]
    public void QueryNormalizerCrash1()
    {

      st.execSQL(
        "CREATE TABLE  extable_1 (x_coord int, y_coord int, z_coord int," +
        "edge_id int, life_id int)");
      st.execSQL("CREATE TABLE  extable_2 (daset_id int, sect_id int, " +
        "xyz_id  int, edge_id int, life_id int, another_id int, yetanother_id int)");

      string connStr = st.GetConnectionString(true) + ";logging=true";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand(
          "SELECT tblb.x_coord, tblb.y_coord, tblb.z_coord, " +
          "tbl_c.x_coord, tbl_c.y_coord, tbl_c.z_coord, tbl_a.edge_id, " +
          "tbl_a.life_id, tbl_a.daset_id, tbl_a.sect_id, tbl_a.yetanother_id," +
          " IFNULL(tbl_a.xyz_id,0) FROM extable_2 tbl_a, extable_1 tblb, " +
          "extable_1 tbl_c WHERE tbl_a.daset_id=208 AND tbl_a.sect_id IN " +
          "(1,2,3,4,5,6,7)",
          c);
        Console.WriteLine(cmd.ExecuteScalar());
      }
    }

    /// <summary>
    /// Bug #54152 : Crash in QueryNormalizer, VALUES incomplete
    /// </summary>
    [Fact]
    public void QueryNormalizerCrash2()
    {
      st.execSQL("CREATE TABLE bug54152 (id INT, expr INT,name VARCHAR(20)," +
        "fld4 VARCHAR(10), fld5 VARCHAR(10), fld6 VARCHAR(10)," +
        "fld7 VARCHAR(10), fld8 VARCHAR(10), fld9 VARCHAR(10)," +
        "fld10 VARCHAR(10), PRIMARY KEY(id))");

      string connStr = st.GetConnectionString(true) + ";logging=true";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        string query =
          "INSERT INTO bug54152 VALUES " +
          "(1,1, 'name 1',1,1,1,1,1,1,1)," +
          "(2,2,'name 2',2,2,2,2,2,2,2)," +
          "(3,3,'name 3',3,3,3,3,3,3,3)," +
          "(4,4,'name 4',4,4,4,4,4,4,4)," +
          "(5,5,'name 5',5,5,5,5,5,5,5)," +
          "(6,6,'name 6',6,6,6,6,6,6,6)," +
          "(7,7,'name 7',7,7,7,7,7,7,7)," +
          "(8,8,'name 8',8,8,8,8,8,8,8)," +
          "(9,9,'name 9',9,9,9,9,9,9,9)," +
          "(10,10,'name 10',10,10,10,10,10,10,10)";
        MySqlCommand cmd = new MySqlCommand(query, c);
        cmd.ExecuteNonQuery();
      }
    }
  }
}
