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

using NUnit.Framework;
using System;
using System.Data;
using System.IO;

namespace MySql.Data.MySqlClient.Tests
{
  public class Syntax : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
    }

    [Test]
    public void ShowCreateTable()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      DataTable dt = Utils.FillTable("SHOW CREATE TABLE Test", Connection);

      Assert.That(dt.Rows, Has.One.Items);
      Assert.AreEqual(2, dt.Columns.Count);
    }

    [Test]
    public void ProblemCharsInSQLUTF8()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), mt MEDIUMTEXT, " +
            "PRIMARY KEY(id)) CHAR SET utf8");

      MySqlConnectionStringBuilder cb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      cb.CharacterSet = "utf8";
      using (MySqlConnection c = new MySqlConnection(cb.ConnectionString))
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
          Assert.AreEqual(1, reader.GetInt32(0));
          Assert.AreEqual("This is my;test ? string–’‘’“”…", reader.GetString(1));
          Assert.AreEqual("My MT string: ?", reader.GetString(2));
        }
      }
    }


    [Test]
    public void ProblemCharsInSQL()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), mt MEDIUMTEXT, " +
            "PRIMARY KEY(id))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (?id, ?text, ?mt)", Connection);
      cmd.Parameters.AddWithValue("?id", 1);
      cmd.Parameters.AddWithValue("?text", "This is my;test ? string-'''\"\".");
      cmd.Parameters.AddWithValue("?mt", "My MT string: ?");
      cmd.ExecuteNonQuery();

      cmd.CommandText = "SELECT * FROM Test";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.AreEqual(1, reader.GetInt32(0));
        Assert.AreEqual("This is my;test ? string-'''\"\".", reader.GetString(1));
        Assert.AreEqual("My MT string: ?", reader.GetString(2));
      }
    }

    [Test]
    public void LoadDataLocalInfile()
    {
      if (Version >= new Version(8, 0, 2)) ExecuteSQL("SET GLOBAL local_infile = 1");

      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");

      string path = Path.GetTempFileName();
      StreamWriter sw = new StreamWriter(new FileStream(path, FileMode.Create));
      for (int i = 0; i < 200000; i++)
        sw.WriteLine(i + ",'Test'");
      sw.Flush();
      sw.Dispose();

      path = path.Replace(@"\", @"\\");

      LoadFile(new MySqlConnection(Connection.ConnectionString + ";allowloadlocalinfile=true;"), path);
      Assert.Throws<MySqlException>(() => LoadFile(new MySqlConnection(Connection.ConnectionString), path));
    }

    private void LoadFile(MySqlConnection conn, string path)
    {
      using (conn)
      {
        conn.Open();


        MySqlCommand cmd = new MySqlCommand(
          "LOAD DATA LOCAL INFILE '" + path + "' INTO TABLE Test FIELDS TERMINATED BY ','", conn);
        cmd.CommandTimeout = 0;

        object cnt = 0;
        cnt = cmd.ExecuteNonQuery();
        Assert.AreEqual(200000, Convert.ToInt32(cnt));

        cmd.CommandText = "SELECT COUNT(*) FROM Test";
        cnt = cmd.ExecuteScalar();
        Assert.AreEqual(200000, Convert.ToInt32(cnt));
      }
    }

    [Test]
    public void ShowTablesInNonExistentDb()
    {
      MySqlCommand cmd = new MySqlCommand("SHOW TABLES FROM dummy", Connection);
      Assert.Throws<MySqlException>(() => cmd.ExecuteReader());
    }

    [Test]
    public void Bug6135()
    {
      MySqlCommand cmd = null;
      string sql = null;

      // Updating the default charset for servers 8.0+.
      if (Connection.driver.Version.isAtLeast(8, 0, 1))
      {
        sql = "SET NAMES 'latin1' COLLATE 'latin1_swedish_ci'";
        cmd = new MySqlCommand(sql, Connection);
        cmd.ExecuteNonQuery();
      }

      sql = @"CREATE TABLE `KLANT` (`KlantNummer` int(11) NOT NULL auto_increment, 
        `Username` varchar(50) NOT NULL default '', `Password` varchar(100) NOT NULL default '', 
        `Naam` varchar(100) NOT NULL default '', `Voornaam` varchar(100) NOT NULL default '',
        `Straat` varchar(100) NOT NULL default '', `StraatNr` varchar(10) NOT NULL default '',
        `Gemeente` varchar(100) NOT NULL default '', `Postcode` varchar(10) NOT NULL default '',
        `DefaultMail` varchar(255) default '', 	`BtwNr` varchar(50) default '',
        `ReceiveMail` tinyint(1) NOT NULL default '0',	`Online` tinyint(1) NOT NULL default '0',
        `LastVisit` timestamp NOT NULL, `Categorie` int(11) NOT NULL default '0',
        PRIMARY KEY  (`KlantNummer`),	UNIQUE KEY `UniqueUsername` (`Username`),
        UNIQUE KEY `UniqueDefaultMail` (`DefaultMail`)	)";
      ExecuteSQL(sql);

      cmd = new MySqlCommand("SELECT * FROM KLANT", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.Read()) { }
      }
    }

    [Test]
    public void Sum()
    {
      ExecuteSQL("CREATE TABLE Test (field1 mediumint(9) default '0', field2 float(9,3) " +
        "default '0.000', field3 double(15,3) default '0.000') engine=innodb ");
      ExecuteSQL("INSERT INTO Test values (1,1,1)");

      MySqlCommand cmd2 = new MySqlCommand("SELECT sum(field2) FROM Test", Connection);
      using (MySqlDataReader reader = cmd2.ExecuteReader())
      {
        reader.Read();
        object o = reader[0];
        Assert.AreEqual(1, Convert.ToInt32(o));
      }
    }

    [Test]
    public void Sum2()
    {
      ExecuteSQL("CREATE TABLE Test (id int, count int)");
      ExecuteSQL("INSERT INTO Test VALUES (1, 21)");
      ExecuteSQL("INSERT INTO Test VALUES (1, 33)");
      ExecuteSQL("INSERT INTO Test VALUES (1, 16)");
      ExecuteSQL("INSERT INTO Test VALUES (1, 40)");

      MySqlCommand cmd = new MySqlCommand("SELECT id, SUM(count) FROM Test GROUP BY id", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual(1, reader.GetInt32(0));
        Assert.AreEqual(110, reader.GetDouble(1));
      }
    }

    [Test]
    public void ForceWarnings()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      MySqlCommand cmd = new MySqlCommand(
        "SELECT * FROM Test; DROP TABLE IF EXISTS test2; SELECT * FROM Test", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        while (reader.NextResult()) { }
      }
    }

    [Test]
    public void SettingAutoIncrementColumns()
    {
      ExecuteSQL("CREATE TABLE Test (id int auto_increment, name varchar(100), primary key(id))");
      ExecuteSQL("INSERT INTO Test VALUES (1, 'One')");
      ExecuteSQL("INSERT INTO Test VALUES (3, 'Two')");

      MySqlCommand cmd = new MySqlCommand("SELECT name FROM Test WHERE id=1", Connection);
      object name = cmd.ExecuteScalar();
      Assert.AreEqual("One", name);

      cmd.CommandText = "SELECT name FROM Test WHERE id=3";
      name = cmd.ExecuteScalar();
      Assert.AreEqual("Two", name);
      Assert.Throws<MySqlException>(() => ExecuteSQL("INSERT INTO Test (id, name2) values (5, 'Three')"));
    }

    /// <summary>
    /// Bug #16645 FOUND_ROWS() Bug 
    /// </summary>
    [Test]
    public void FoundRows()
    {
      ExecuteSQL("CREATE TABLE Test (testID int(11) NOT NULL auto_increment, testName varchar(100) default '', " +
          "PRIMARY KEY  (testID)) ENGINE=InnoDB DEFAULT CHARSET=latin1");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (NULL, 'test')", Connection);
      for (int i = 0; i < 1000; i++)
        cmd.ExecuteNonQuery();
      cmd.CommandText = "SELECT SQL_CALC_FOUND_ROWS * FROM Test LIMIT 0, 10";
      cmd.ExecuteNonQuery();
      cmd.CommandText = "SELECT FOUND_ROWS()";
      object cnt = cmd.ExecuteScalar();
      Assert.AreEqual(1000, Convert.ToInt32(cnt));
    }

    [Test]
    public void AutoIncrement()
    {
      ExecuteSQL("CREATE TABLE Test (testID int(11) NOT NULL auto_increment, testName varchar(100) default '', " +
          "PRIMARY KEY  (testID)) ENGINE=InnoDB DEFAULT CHARSET=latin1");
      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test VALUES (NULL, 'test')", Connection);
      cmd.ExecuteNonQuery();
      cmd.CommandText = "SELECT @@IDENTITY as 'Identity'";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        int ident = Int32.Parse(reader.GetValue(0).ToString());
        Assert.AreEqual(1, ident);
      }
    }

    /// <summary>
    /// Bug #21521 # Symbols not allowed in column/table names. 
    /// </summary>
    [Test]
    public void CommentSymbolInTableName()
    {
      ExecuteSQL("CREATE TABLE Test (`PO#` int(11) NOT NULL auto_increment, " +
        "`PODate` date default NULL, PRIMARY KEY  (`PO#`))");
      ExecuteSQL("INSERT INTO Test ( `PO#`, `PODate` ) " +
        "VALUES ( NULL, '2006-01-01' )");

      string sql = "SELECT `PO#` AS PurchaseOrderNumber, " +
        "`PODate` AS OrderDate FROM  Test";
      DataTable dt = Utils.FillTable(sql, Connection);
      Assert.That(dt.Rows, Has.One.Items);
    }

    /// <summary>
    /// Bug #25178 Addition message in error 
    /// </summary>
    [Test]
    public void ErrorMessage()
    {
      MySqlCommand cmd = new MySqlCommand("SELEKT NOW() as theTime", Connection);
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
    [Test]
    public void Describe()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      var reader = ExecuteReader("DESCRIBE Test");
      using (reader)
      {
        Assert.True(reader.GetFieldType(0) == typeof(string));
        Assert.True(reader.GetFieldType(1) == typeof(string));
        Assert.True(reader.GetFieldType(2) == typeof(string));
        Assert.True(reader.GetFieldType(3) == typeof(string));
        Assert.True(reader.GetFieldType(4) == typeof(string));
        Assert.True(reader.GetFieldType(5) == typeof(string));
      }
    }

    [Test]
    public void ShowTableStatus()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      var reader = ExecuteReader("DESCRIBE Test");
      using (reader)
      {
        reader.Read();
        Assert.True(reader[0].GetType() == typeof(string));
      }
    }

    /// <summary>
    /// Bug #26960 Connector .NET 5.0.5 / Visual Studio Plugin 1.1.2 
    /// </summary>
    [Test]
    public void NullAsAType()
    {
      DataTable dt = Utils.FillTable(@"SELECT 'localhost' as SERVER_NAME,
        null as CATALOG_NAME, database() as SCHEMA_NAME", Connection);
      Assert.True(dt.Rows[0][0].GetType() == typeof(string));
      Assert.AreEqual(DBNull.Value, dt.Rows[0][1]);
      Assert.True(dt.Rows[0][2].GetType() == typeof(string));
    }

    [Test]
    public void SpaceInDatabaseName()
    {
      string fullDbName = CreateDatabase("x y");

      MySqlConnectionStringBuilder cb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      cb.Database = fullDbName;
      using (var c = new MySqlConnection(cb.ConnectionString))
      {
        c.Open();
      }
    }

    /// <summary>
    /// Bug #28448  	show processlist; returns byte arrays in the resulting data table
    /// </summary>
    [Test]
    public void ShowProcessList()
    {
      MySqlConnectionStringBuilder cb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      cb.RespectBinaryFlags = false;
      MySqlConnection c = new MySqlConnection(cb.ConnectionString);
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

    [Test]
    public void SemisAtStartAndEnd()
    {
      using (MySqlCommand cmd = new MySqlCommand(";;SELECT 1;;;", Connection))
      {
        Assert.AreEqual(1, Convert.ToInt32(cmd.ExecuteScalar()));
      }
    }

    /// <summary>
    /// Bug #51610	Exception thrown inside Connector.NET
    /// </summary>
    [Test]
    public void Bug51610()
    {
      MySqlCommand cmd = new MySqlCommand("SELECT 'ABC', (0/`QOH`) from (SELECT 1 as `QOH`) `d1`", Connection);
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual("ABC", reader.GetString(0));
        Assert.AreEqual(0, reader.GetInt32(1));
      }

      cmd.CommandText = "SELECT 'ABC', (0-`QOH`) from (SELECT 1 as `QOH`) `d1`";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual("ABC", reader.GetString(0));
        Assert.AreEqual(-1, reader.GetInt32(1));
      }

      cmd.CommandText = "SELECT 'test 2010-03-04 @ 10:14'";
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual("test 2010-03-04 @ 10:14", reader.GetString(0));
      }

    }

    /// <summary>
    /// Bug #51788	Error in SQL syntax not reported. A CLR exception was thrown instead,
    /// </summary>
    [Test]
    public void NonTerminatedString()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test(id INT, name1 VARCHAR(20), name2 VARCHAR(20))");

      try
      {
        MySqlCommand cmd = new MySqlCommand(
          "INSERT INTO test VALUES (1, 'test 2010-03-04 @ 10:14, name2=' joe')", Connection);
        cmd.ExecuteNonQuery();
      }
      catch (MySqlException)
      {
      }
    }

    /// <summary>
    /// Bug #53865 : crash in QueryNormalizer, "IN" clause incomplete
    /// </summary>
    [Test]
    public void QueryNormalizerCrash1()
    {

      ExecuteSQL(
        "CREATE TABLE  extable_1 (x_coord int, y_coord int, z_coord int," +
        "edge_id int, life_id int)");
      ExecuteSQL("CREATE TABLE  extable_2 (daset_id int, sect_id int, " +
        "xyz_id  int, edge_id int, life_id int, another_id int, yetanother_id int)");

      MySqlConnectionStringBuilder cb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      cb.Logging = true;
      using (MySqlConnection c = new MySqlConnection(cb.ConnectionString))
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
    [Test]
    public void QueryNormalizerCrash2()
    {
      ExecuteSQL("CREATE TABLE bug54152 (id INT, expr INT,name VARCHAR(20)," +
        "fld4 VARCHAR(10), fld5 VARCHAR(10), fld6 VARCHAR(10)," +
        "fld7 VARCHAR(10), fld8 VARCHAR(10), fld9 VARCHAR(10)," +
        "fld10 VARCHAR(10), PRIMARY KEY(id))");


      MySqlConnectionStringBuilder cb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      cb.Logging = true;
      using (MySqlConnection c = new MySqlConnection(cb.ConnectionString))
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

    /// <summary>
    /// Bug #54386 : expression with parentheses in INSERT leads to invalid
    /// query when using batching
    /// </summary>
    [Test]
    public void TokenizerBatching()
    {
#if !NETFRAMEWORK
      if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) Assert.Ignore();
#endif

      ExecuteSQL("CREATE TABLE Test (id INT, expr INT,name VARCHAR(20), PRIMARY KEY(id))");

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test",
        Connection);
      MySqlCommand ins = new MySqlCommand(
        "INSERT INTO test (id, expr, name) VALUES(?p1, (?p2 * 2) + 3, ?p3)",
        Connection);
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

  }
}
