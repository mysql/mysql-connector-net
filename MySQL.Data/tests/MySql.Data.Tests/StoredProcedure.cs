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
using System.Data.Common;
using System.Globalization;
using System.Threading;

namespace MySql.Data.MySqlClient.Tests
{
  public class StoredProcedure : TestBase
  {
    protected override void Cleanup()
    {
      ExecuteSQL("DROP PROCEDURE IF EXISTS spTest");
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
      ExecuteSQL("DROP DATABASE IF EXISTS `dotnet3.1`");
      Connection.ProcedureCache.Clear();
    }

    /// <summary>
    /// Bug #7623  	Adding MySqlParameter causes error if MySqlDbType is Decimal
    /// </summary>
    [Test]
    public void ReturningResultset()
    {
      // create our procedure
      ExecuteSQL("CREATE PROCEDURE spTest(val decimal(10,3)) begin select val; end");

      using (MySqlCommand cmd = new MySqlCommand("spTest", Connection))
      {
        cmd.CommandType = CommandType.StoredProcedure;

        MySqlParameter p = cmd.Parameters.Add("?val", MySqlDbType.Decimal);
        p.Precision = 10;
        p.Scale = 3;
        p.Value = 21;

        decimal id = (decimal)cmd.ExecuteScalar();
        Assert.AreEqual(21, id);
      }
    }

    [Test]
    public void NonQuery()
    {
      ExecuteSQL("CREATE TABLE Test(id INT, name VARCHAR(20))");
      ExecuteSQL(@"CREATE PROCEDURE spTest(IN value INT) 
        BEGIN INSERT INTO Test VALUES(value, 'Test'); END");

      //setup testing data
      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?value", 2);
      int rowsAffected = cmd.ExecuteNonQuery();
      Assert.AreEqual(1, rowsAffected);

      cmd.CommandText = "SELECT * FROM Test";
      cmd.CommandType = CommandType.Text;
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.AreEqual(2, reader.GetInt32(0));
        Assert.AreEqual("Test", reader.GetString(1));
        Assert.False(reader.Read());
        Assert.False(reader.NextResult());
      }
    }

    [Test]
    public void NoBatch()
    {
      MySqlCommand cmd = new MySqlCommand("spTest;select * from Test", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      Exception ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
    }

    [Test]
    public void WrongParameters()
    {
      ExecuteSQL("CREATE PROCEDURE spTest(p1 INT) BEGIN SELECT 1; END");
      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?p2", 1);
      Exception ex = Assert.Throws<ArgumentException>(() => cmd.ExecuteNonQuery());
    }

    [Test]
    public void NoInOutMarker()
    {
      // create our procedure
      ExecuteSQL("CREATE PROCEDURE spTest(valin varchar(50)) BEGIN  SELECT valin;  END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?valin", "myvalue");
      object val = cmd.ExecuteScalar();
      Assert.AreEqual("myvalue", val);
    }

    [Test]
    public void NoSPOnPre50()
    {
      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      Exception ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
    }

    /// <summary>
    /// Bug #13590  	ExecuteScalar returns only Int64 regardless of actual SQL type
    /// </summary>
    [Test]
    public void ExecuteScalar2()
    {
      // create our procedure
      ExecuteSQL("CREATE PROCEDURE spTest() " +
         "BEGIN  DECLARE myVar1 INT; SET myVar1 := 1; SELECT myVar1; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      object result = cmd.ExecuteScalar();
      Assert.AreEqual(1, result);
      Assert.True(result is Int32);
    }

    [Test]
    public void MultipleResultsets()
    {
      MultipleResultsetsImpl(false);
    }

    [Test]
    [Ignore("Fix this")]
    public void MultipleResultsetsPrepared()
    {
      MultipleResultsetsImpl(true);
    }

    private void MultipleResultsetsImpl(bool prepare)
    {
      ExecuteSQL("DROP PROCEDURE IF EXISTS multiResults");
      // create our procedure
      ExecuteSQL("CREATE PROCEDURE multiResults() BEGIN  SELECT 1;  SELECT 2; END");

      MySqlCommand cmd = new MySqlCommand("multiResults", Connection);
      if (prepare) cmd.Prepare();
      cmd.CommandType = CommandType.StoredProcedure;
      MySqlDataReader reader = cmd.ExecuteReader();
      Assert.True(reader.Read());
      Assert.True(reader.NextResult());
      Assert.True(reader.Read());
      Assert.False(reader.NextResult());
      Assert.False(reader.Read());
      reader.Close();

      DataSet ds = new DataSet();
      MySqlCommand cmd2 = new MySqlCommand("multiResults", Connection);
      cmd2.CommandType = CommandType.StoredProcedure;
      MySqlDataAdapter da = new MySqlDataAdapter(cmd2);
      da.FillError += new FillErrorEventHandler(da_FillError);
      fillError = null;
      da.Fill(ds);
      Assert.AreEqual(2, ds.Tables.Count);
      Assert.AreEqual(1, Convert.ToInt32(ds.Tables[0].Rows.Count));
      Assert.AreEqual(1, Convert.ToInt32(ds.Tables[1].Rows.Count));
      Assert.AreEqual(1, Convert.ToInt32(ds.Tables[0].Rows[0][0]));
      Assert.AreEqual(2, Convert.ToInt32(ds.Tables[1].Rows[0][0]));
      Assert.Null(fillError);
    }

    private static string fillError = null;

    private static void da_FillError(object sender, FillErrorEventArgs e)
    {
      fillError = e.Errors.Message;
      e.Continue = true;
    }

    [Test]
    public void ExecuteWithCreate()
    {
      // create our procedure
      string sql = "CREATE PROCEDURE spTest(IN var INT) BEGIN  SELECT var; END; call spTest(?v)";

      MySqlCommand cmd = new MySqlCommand(sql, Connection);
      cmd.Parameters.Add(new MySqlParameter("?v", 33));
      object val = cmd.ExecuteScalar();
      Assert.AreEqual(33, val);
    }

    /// <summary>
    /// Bug #9722 Connector does not recognize parameters separated by a linefeed 
    /// </summary>
    [Test]
    public void OtherProcSigs()
    {
      // create our procedure
      ExecuteSQL("CREATE PROCEDURE spTest(IN \r\nvalin DECIMAL(10,2),\nIN val2 INT) " +
        "SQL SECURITY INVOKER BEGIN  SELECT valin; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?valin", 20.4);
      cmd.Parameters.AddWithValue("?val2", 4);
      decimal val = (decimal)cmd.ExecuteScalar();
      Decimal d = new Decimal(20.4);
      Assert.AreEqual(d, val);

      // create our second procedure
      ExecuteSQL("DROP PROCEDURE IF EXISTS spTest");
      ExecuteSQL("CREATE PROCEDURE spTest( \r\n) BEGIN  SELECT 4; END");
      cmd.Parameters.Clear();
      object val1 = cmd.ExecuteScalar();
      Assert.AreEqual(4, Convert.ToInt32(val1));
    }

    /// <summary>
    /// Bug #11450 Connector/NET, current database and stored procedures
    /// </summary>
    [Test]
    public void NoDefaultDatabase()
    {
      // create our procedure
      ExecuteSQL("CREATE PROCEDURE spTest() BEGIN  SELECT 4; END");
      string dbName = CreateDatabase("1");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      object val = cmd.ExecuteScalar();
      Assert.AreEqual(4, Convert.ToInt32(val));

      cmd.CommandText = String.Format("USE `{0}`", dbName);
      cmd.CommandType = CommandType.Text;
      cmd.ExecuteNonQuery();

      cmd.CommandText = String.Format("`{0}`.spTest", Connection.Database);
      cmd.CommandType = CommandType.StoredProcedure;
      val = cmd.ExecuteScalar();
      Assert.AreEqual(4, Convert.ToInt32(val));

      cmd.CommandText = String.Format("USE `{0}`", Connection.Database);
      cmd.CommandType = CommandType.Text;
      cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Bug #13590  	ExecuteScalar returns only Int64 regardless of actual SQL type
    /// </summary>
    /*		[Test]
        public void TestSelectingInts()
        {
          executeSQL("CREATE PROCEDURE spTest() BEGIN DECLARE myVar INT; " +
            "SET MyVar := 1; SELECT CAST(myVar as SIGNED); END");

          MySqlCommand cmd = new MySqlCommand("spTest", connection);
          cmd.CommandType = CommandType.StoredProcedure;
          object val = cmd.ExecuteScalar();
          Assert.AreEqual(1, val, "Checking value");
          Assert.True(val is Int32, "Checking type");
        }
    */

    /// <summary>
    /// Bug #11386  	Numeric parameters with Precision and Scale not taken into account by Connector
    /// </summary>
    [Test]
    public void DecimalAsParameter()
    {
      ExecuteSQL("CREATE PROCEDURE spTest(IN d DECIMAL(19,4)) BEGIN SELECT d; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?d", 21);
      decimal d = (decimal)cmd.ExecuteScalar();
      Assert.AreEqual(21, d);
    }

    /// <summary>
    /// Bug #6902  	Errors in parsing stored procedure parameters
    /// </summary>
    [Test]
    public void ParmWithCharacterSet()
    {
      ExecuteSQL("CREATE PROCEDURE spTest(P longtext character set utf8) " +
        "BEGIN SELECT P; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?P", "This is my value");
      string p = (string)cmd.ExecuteScalar();
      Assert.AreEqual("This is my value", p);
    }

    /// <summary>
    /// Bug #13753  	Exception calling stored procedure with special characters in parameters
    /// </summary>
    [Test]
    public void SpecialCharacters()
    {
      ExecuteSQL("SET sql_mode=ANSI_QUOTES");
      try
      {
        ExecuteSQL("CREATE PROCEDURE spTest(\"@Param1\" text) BEGIN SELECT \"@Param1\"; END");

        MySqlCommand cmd = new MySqlCommand("spTest", Connection);
        cmd.Parameters.AddWithValue("@Param1", "This is my value");
        cmd.CommandType = CommandType.StoredProcedure;

        string val = (string)cmd.ExecuteScalar();
        Assert.AreEqual("This is my value", val);
      }
      finally
      {
        ExecuteSQL("SET sql_mode=\"\"");
      }
    }

    [Test]
    public void CallingSPWithPrepare()
    {
      ExecuteSQL("CREATE PROCEDURE spTest(P int) BEGIN SELECT P; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?P", 33);
      cmd.Prepare();

      int p = (int)cmd.ExecuteScalar();
      Assert.AreEqual(33, p);
    }

    /// <summary>
    /// Bug #13927  	Multiple Records to same Table in Transaction Problem
    /// </summary>
    [Test]
    public void MultipleRecords()
    {
      ExecuteSQL("CREATE TABLE Test (id INT, name VARCHAR(20))");
      ExecuteSQL("CREATE PROCEDURE spTest(id int, str VARCHAR(45)) " +
           "BEGIN INSERT INTO Test VALUES(id, str); END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("?id", 1);
      cmd.Parameters.AddWithValue("?str", "First record");
      cmd.ExecuteNonQuery();

      cmd.Parameters.Clear();
      cmd.Parameters.AddWithValue("?id", 2);
      cmd.Parameters.AddWithValue("?str", "Second record");
      cmd.ExecuteNonQuery();

      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(dt);

      Assert.AreEqual(1, dt.Rows[0]["id"]);
      Assert.AreEqual(2, dt.Rows[1]["id"]);
      Assert.AreEqual("First record", dt.Rows[0]["name"]);
      Assert.AreEqual("Second record", dt.Rows[1]["name"]);
    }

    /// <summary>
    /// Bug #16788 Only byte arrays and strings can be serialized by MySqlBinary 
    /// </summary>
    [Test]
    public void Bug16788()
    {
      ExecuteSQL("CREATE TABLE Test (id integer(9), state varchar(2))");
      ExecuteSQL("CREATE PROCEDURE spTest(IN p1 integer(9), IN p2 varchar(2)) " +
        "BEGIN " +
        "INSERT INTO Test (id, state) VALUES (p1, p2); " +
        "END");

      MySqlCommand cmd = Connection.CreateCommand();
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.CommandText = "spTest";
      cmd.Parameters.Add("?p1", MySqlDbType.UInt16, 9);
      cmd.Parameters["?p1"].Value = 44;
      cmd.Parameters.Add("?p2", MySqlDbType.VarChar, 2);
      cmd.Parameters["?p2"].Value = "ss";
      cmd.ExecuteNonQuery();
    }

    [Test]
    public void ProcedureCache()
    {
      // open a new connection using a procedure cache
      string connStr = Connection.ConnectionString;
      connStr += ";procedure cache size=25;logging=true";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        // install our custom trace listener
        GenericListener myListener = new GenericListener();
        System.Diagnostics.Trace.Listeners.Add(myListener);

        for (int x = 0; x < 10; x++)
        {
          ExecuteSQL("CREATE PROCEDURE spTest" + x + "() BEGIN SELECT 1; END");
          MySqlCommand cmd = new MySqlCommand("spTest" + x, c);
          cmd.CommandType = CommandType.StoredProcedure;
          for (int y = 0; y < 20; y++)
          {
            cmd.ExecuteNonQuery();
          }
        }

        // remove our custom trace listener
        System.Diagnostics.Trace.Listeners.Remove(myListener);

        // now see how many times our listener recorded a cache hit
        Assert.AreEqual(190, myListener.Find("from procedure cache"));
        Assert.AreEqual(10, myListener.Find("from server"));
      }
    }

    /// <summary>
    /// Bug #20581 Null Reference Exception when closing reader after stored procedure. 
    /// </summary>
    [Test]
    public void Bug20581()
    {
      ExecuteSQL("CREATE PROCEDURE spTest(p int) BEGIN SELECT p; END");
      MySqlParameter param1;
      MySqlCommand command = new MySqlCommand("spTest", Connection);
      command.CommandType = CommandType.StoredProcedure;

      param1 = command.Parameters.Add("?p", MySqlDbType.Int32);
      param1.Value = 3;

      command.Prepare();
      using (MySqlDataReader reader = command.ExecuteReader())
      {
        reader.Read();
      }
    }

    /// <summary>
    /// Bug #17046 Null pointer access when stored procedure is used 
    /// </summary>
    [Test]
    public void PreparedReader()
    {
      ExecuteSQL("CREATE TABLE  Test (id int(10) unsigned NOT NULL default '0', " +
         "val int(10) unsigned default NULL, PRIMARY KEY (id)) " +
         "ENGINE=InnoDB DEFAULT CHARSET=utf8");
      ExecuteSQL("CREATE PROCEDURE spTest (IN pp INTEGER) " +
            "select * from Test where id > pp ");

      MySqlCommand c = new MySqlCommand("spTest", Connection);
      c.CommandType = CommandType.StoredProcedure;
      IDataParameter p = c.CreateParameter();
      p.ParameterName = "?pp";
      p.Value = 10;
      c.Parameters.Add(p);
      c.Prepare();
      using (MySqlDataReader reader = c.ExecuteReader())
      {
        while (reader.Read())
        {

        }
      }
    }

    /// <summary>
    /// Bug #22452 MySql.Data.MySqlClient.MySqlException: 
    /// </summary>
    [Test]
    public void TurkishStoredProcs()
    {
      ExecuteSQL("CREATE PROCEDURE spTest(IN p_paramname INT) BEGIN SELECT p_paramname; END");
      CultureInfo uiCulture = Thread.CurrentThread.CurrentUICulture;
      CultureInfo culture = Thread.CurrentThread.CurrentCulture;
      Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
      Thread.CurrentThread.CurrentUICulture = new CultureInfo("tr-TR");

      try
      {
        MySqlCommand cmd = new MySqlCommand("spTest", Connection);
        cmd.Parameters.AddWithValue("?p_paramname", 2);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.ExecuteScalar();
      }
      finally
      {
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = uiCulture;
      }
    }

    /// <summary>
    /// Bug #23268 System.FormatException when invoking procedure with ENUM input parameter 
    /// </summary>
    [Test]
    public void ProcEnumParamTest()
    {
      ExecuteSQL("CREATE TABLE Test(str VARCHAR(50), e ENUM ('P','R','F','E'), i INT(6))");
      ExecuteSQL("CREATE PROCEDURE spTest(IN p_enum ENUM('P','R','F','E')) BEGIN " +
        "INSERT INTO Test (str, e, i) VALUES (null, p_enum, 55);  END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?p_enum", "P");
      cmd.Parameters["?p_enum"].Direction = ParameterDirection.Input;
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
      }
      cmd.CommandText = "SELECT e FROM Test";
      cmd.CommandType = CommandType.Text;
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        reader.Read();
        Assert.AreEqual("P", reader.GetString(0));
      }
    }

    /// <summary>
    /// Bug #25609 MySqlDataAdapter.FillSchema 
    /// </summary>
    [Test]
    public void GetSchema()
    {
      ExecuteSQL("CREATE PROCEDURE GetSchema() BEGIN SELECT * FROM Test; END");
      ExecuteSQL(@"CREATE TABLE Test(id INT AUTO_INCREMENT, name VARCHAR(20), PRIMARY KEY (id)) ");

      MySqlCommand cmd = new MySqlCommand("GetSchema", Connection);
      cmd.CommandType = CommandType.StoredProcedure;

      MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
      reader.Read();
      reader.Close();

      MySqlDataAdapter da = new MySqlDataAdapter(cmd);
      DataTable schema = new DataTable();
      da.FillSchema(schema, SchemaType.Source);
      Assert.AreEqual(2, schema.Columns.Count);
    }

    /// <summary>
    /// Bug #26139 MySqlCommand.LastInsertedId doesn't work for stored procedures 
    /// Currently this is borked on the server so we are marking this as notworking
    /// until the server has this fixed.
    /// </summary>
    /*        [Test]
        public void LastInsertId()
        {
          executeSQL("CREATE TABLE Test (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(200))");
          executeSQL("INSERT INTO Test VALUES (NULL, 'Test1')");
          executeSQL("CREATE PROCEDURE spTest() BEGIN " +
            "INSERT INTO Test VALUES (NULL, 'test'); END");

          MySqlCommand cmd = new MySqlCommand("spTest", connection);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.ExecuteNonQuery();
          Assert.AreEqual(2, cmd.LastInsertedId);
        }
        */

    /// <summary>
    /// Bug #27093 Exception when using large values in IN UInt64 parameters 
    /// </summary>
    [Test]
    public void UsingUInt64AsParam()
    {
      ExecuteSQL(@"CREATE TABLE Test(f1 bigint(20) unsigned NOT NULL,
            PRIMARY KEY(f1)) ENGINE=InnoDB DEFAULT CHARSET=utf8");

      ExecuteSQL(@"CREATE PROCEDURE spTest(in _val bigint unsigned)
            BEGIN insert into  Test set f1=_val; END");

      DbCommand cmd = new MySqlCommand();
      DbParameter param = cmd.CreateParameter();
      param.DbType = DbType.UInt64;
      cmd.Connection = Connection;
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.CommandText = "spTest";
      param.Direction = ParameterDirection.Input;
      param.ParameterName = "?_val";
      ulong bigval = long.MaxValue;
      bigval += 1000;
      param.Value = bigval;
      cmd.Parameters.Add(param);
      cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Bug #29526  	syntax error: "show create procedure" with catalog names containing hyphens
    /// </summary>
    [Test]
    public void CatalogWithHyphens()
    {
      // make sure this test is valid
      Assert.True(Connection.Database.IndexOf('-') != -1);

      MySqlCommand cmd = new MySqlCommand("CREATE PROCEDURE spTest() BEGIN SELECT 1; END", Connection);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "spTest";
      cmd.CommandType = CommandType.StoredProcedure;
      Assert.AreEqual(1, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Test]
    public void ComplexDefinition()
    {
      Cleanup();
      ExecuteSQL(@"CREATE PROCEDURE `spTest`() NOT DETERMINISTIC
          CONTAINS SQL SQL SECURITY DEFINER COMMENT '' 
          BEGIN
            SELECT 1,2,3;
          END");
      MySqlCommand command = new MySqlCommand("spTest", Connection);
      command.CommandType = CommandType.StoredProcedure;
      using (MySqlDataReader reader = command.ExecuteReader())
      {
      }
    }

    [Test]
    public void AmbiguousColumns()
    {
      ExecuteSQL("CREATE TABLE t1 (id INT)");
      ExecuteSQL("CREATE TABLE t2 (id1 INT, id INT)");
      ExecuteSQL(@"CREATE PROCEDURE spTest() BEGIN SELECT * FROM t1; 
            SELECT id FROM t1 JOIN t2 on t1.id=t2.id; 
            SELECT * FROM t2; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.CommandTimeout = 0;
      MySqlDataAdapter da = new MySqlDataAdapter(cmd);
      DataSet ds = new DataSet();
      Exception ex = Assert.Throws<MySqlException>(() => da.Fill(ds));
    }

    /// <summary>
    /// Bug #41034 .net parameter not found in the collection
    /// </summary>
    [Test]
    public void SPWithSpaceInParameterType()
    {
      ExecuteSQL("CREATE PROCEDURE spTest(myparam decimal  (8,2)) BEGIN SELECT 1; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.Parameters.Add("@myparam", MySqlDbType.Decimal).Value = 20;
      cmd.CommandType = CommandType.StoredProcedure;
      object o = cmd.ExecuteScalar();
      Assert.AreEqual(1, Convert.ToInt32(o));
    }

    private void ParametersInReverseOrderInternal(bool isOwner)
    {
      ExecuteSQL(@"CREATE PROCEDURE spTest(IN p_1 VARCHAR(5), IN p_2 VARCHAR(5))
            BEGIN SELECT p_1 AS P1, p_2 AS P2; END");
      string spName = "spTest";

      string connStr = Connection.ConnectionString;
      if (!isOwner)
        connStr += ";check parameters=false";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand(spName, c);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("?p_2", ("World"));
        cmd.Parameters[0].Direction = ParameterDirection.Input;
        cmd.Parameters.AddWithValue("?p_1", ("Hello"));
        cmd.Parameters[1].Direction = ParameterDirection.Input;
        cmd.Parameters[0].DbType = DbType.AnsiString;
        cmd.Parameters[1].DbType = DbType.AnsiString;
        MySqlDataAdapter da = new MySqlDataAdapter(cmd);
        DataTable dt = new DataTable();
        da.Fill(dt);
        if (!isOwner)
        {
          Assert.AreEqual("World", dt.Rows[0][0]);
          Assert.AreEqual("Hello", dt.Rows[0][1]);
        }
        else
        {
          Assert.AreEqual("Hello", dt.Rows[0]["P1"]);
          Assert.AreEqual("World", dt.Rows[0]["P2"]);
        }
      }
    }

    [Test]
    public void ParametersInReverseOrderNotOwner()
    {
      ParametersInReverseOrderInternal(false);
    }

    [Test]
    public void ParametersInReverseOrderOwner()
    {
      ParametersInReverseOrderInternal(true);
    }

    [Test]
    public void DeriveParameters()
    {
      ExecuteSQL(@"CREATE  PROCEDURE spTest (id INT, name VARCHAR(20))
          BEGIN SELECT name; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      MySqlCommandBuilder.DeriveParameters(cmd);
      Assert.AreEqual(2, cmd.Parameters.Count);
    }

    /// <summary>
    /// Bug #52562 Sometimes we need to reload cached function parameters 
    /// </summary>
    [Test]
    public void ProcedureCacheMiss()
    {
      if (Version.Major == new Version(5, 7, 43).Major && Version.Minor == new Version(5, 7, 43).Minor) Assert.Ignore("Test temporaly deactivated for MySQL Server version 5.7.x due to a possible bug");

      ExecuteSQL("CREATE PROCEDURE spTest(id INT) BEGIN SELECT 1; END");

      string connStr = Connection.ConnectionString + ";procedure cache size=25;";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("spTest", c);
        cmd.Parameters.AddWithValue("@id", 1);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.ExecuteScalar();

        ExecuteSQL("DROP PROCEDURE spTest");
        ExecuteSQL("CREATE PROCEDURE spTest(id INT, id2 INT, id3 INT) BEGIN SELECT 1; END");

        cmd.Parameters.AddWithValue("@id2", 2);
        cmd.Parameters.AddWithValue("@id3", 3);
        cmd.ExecuteScalar();
      }
    }

    /// <summary>
    /// Verifies that GetProcedureParameters does not require SELECT permission on mysql.proc table.
    /// </summary>
    [Test]
    public void GetProcedureParametersDoesNotRequireSelectFromMySqlProceduresTable()
    {
      ExecuteSQL(@"CREATE  PROCEDURE spTest(id INT, name VARCHAR(20))
          BEGIN SELECT name; END");

      MySqlConnectionStringBuilder cb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      cb.CheckParameters = false;

      using (MySqlConnection c = new MySqlConnection(cb.ConnectionString))
      {
        c.Open();

        string[] restrictions = new string[4];
        restrictions[1] = c.Database;
        restrictions[2] = "spTest";

        DataTable procTable = c.GetSchema("procedures", restrictions);

        ISSchemaProvider isp = new ISSchemaProvider(c);
        string[] rest = isp.CleanRestrictions(restrictions);

        MySqlSchemaCollection parametersTable = isp.GetProcedureParametersAsync(rest, new MySqlSchemaCollection(procTable), false).GetAwaiter().GetResult();

        Assert.NotNull(parametersTable);
      }
    }

    /// <summary>
    /// Bug #31237338	CANNOT CALL STORED PROCEDURES IN DATABASES WHOSE NAME CONTAINS A PERIOD
    /// </summary>
    [Test]
    public void NamesWithPeriods()
    {
      //stored procedure wich name contains "."
      ExecuteSQL("CREATE PROCEDURE `spversion1.2.3`(p int) BEGIN SELECT p; END");
      using (var connection = new MySqlConnection(Connection.ConnectionString))
      {
        connection.Open();
        var strName = "`SPversion1.2.3`";
        using (MySqlCommand cmd = new MySqlCommand(strName, Connection))
        {
          cmd.Parameters.AddWithValue("?p", 2);
          cmd.CommandType = CommandType.StoredProcedure;
          var result = cmd.ExecuteScalar();
          Assert.AreEqual(2, result);
        }
      }
      //Database and stored procedure contains "."
      ExecuteSQL("CREATE DATABASE IF NOT EXISTS `dotnet3.1`;", true);
      ExecuteSQL("CREATE PROCEDURE `dotnet3.1`.`sp_normalname.1`(p int) BEGIN SELECT p; END", true);
      using (MySqlConnection rootConnection = new MySqlConnection($"server={Host};port={Port};user id={RootUser};password={RootPassword};persistsecurityinfo=True;allowuservariables=True;database=dotnet3.1;"))
      {
        rootConnection.Open();
        using (MySqlCommand cmd = new MySqlCommand("`sp_normalname.1`", rootConnection))
        {
          cmd.Parameters.AddWithValue("?p", 3);
          cmd.CommandType = CommandType.StoredProcedure;
          var result = cmd.ExecuteScalar();
          Assert.AreEqual(3, result);
        }
      }
    }

    /// <summary>
    /// Bug #31622907	GETSCHEMA("PROCEDURES") RETURNS ROUTINE_DEFINITION OF "SYSTEM.BYTE[]"
    /// </summary>
    [Test]
    public void GetSchemaProcedures()
    {
      using (var connection = new MySqlConnection(Connection.ConnectionString))
      {
        connection.Open();
        ExecuteSQL($"CREATE PROCEDURE `{connection.Settings.Database}`.`sp_testname1`(p int) BEGIN SELECT p; END", true);
        var table = connection.GetSchema("Procedures");
        if (table.Rows.Count > 0)
        {
          var column = table.Rows[0]["ROUTINE_DEFINITION"];
          Assert.IsNotEmpty(column.ToString());
        }
      }
    }

    /// <summary>
    /// Bug #31669587	- COMMAND.PREPARE() SEND WRONG STATEMENT TO SERVER
    /// </summary>
    [Test]
    public void ProcedureWithoutDbNameInConnString()
    {
      ExecuteSQL($"DROP SCHEMA IF EXISTS test_prepare; CREATE SCHEMA test_prepare", true);
      ExecuteSQL($"CREATE PROCEDURE test_prepare.spTest () BEGIN SELECT 1; END", true);

      using (MySqlConnection conn = new MySqlConnection($"server={Host};user={RootUser};password={RootPassword};port={Port};"))
      {
        conn.Open();

        MySqlCommand command = conn.CreateCommand();
        command.CommandText = $"test_prepare.spTest";
        command.CommandType = CommandType.StoredProcedure;
        Assert.AreEqual(1, command.ExecuteScalar());

        command.CommandText = $"`test_prepare`.`spTest`";
        command.CommandType = CommandType.StoredProcedure;
        Assert.AreEqual(1, command.ExecuteScalar());
      }
    }

    /// <summary>
    /// Bug #31458774	PREPARED STORED PROCEDURE COMMAND DOESN'T VERIFY PARAMETER TYPES
    /// </summary>
    [Test]
    public void VerifyParametersType()
    {
      ExecuteSQL($"CREATE PROCEDURE spTest(OUT value VARCHAR(100)) BEGIN SELECT 'test value' INTO value; END");

      using (var connection = new MySqlConnection(Connection.ConnectionString))
      {
        connection.Open();

        //don't call Prepare() when SP and MySqlParameter have different data types
        var cmd = new MySqlCommand("spTest", connection);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new MySqlParameter
        {
          ParameterName = "@value",
          DbType = DbType.Int32,
          Direction = ParameterDirection.Output
        });
        Assert.Throws<FormatException>(() => cmd.ExecuteNonQuery());

        //call Prepare() when SP and MySqlParameter have the same data types
        cmd = new MySqlCommand("spTest", connection);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new MySqlParameter
        {
          ParameterName = "@value",
          DbType = DbType.String,
          Direction = ParameterDirection.Output
        });
        cmd.Prepare();
        cmd.ExecuteNonQuery();
        var result = cmd.Parameters["@value"].Value;
        Assert.AreEqual("test value", result);

        //call Prepare() when SP and MySqlParameter have different data types
        cmd = new MySqlCommand("spTest", connection);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new MySqlParameter
        {
          ParameterName = "@value",
          DbType = DbType.Int32,
          Direction = ParameterDirection.Output
        });
        cmd.Prepare();
        Assert.Throws<FormatException>(() => cmd.ExecuteNonQuery());
      }
    }

    [Test]
    public void PassJsonParameter()
    {
      if (Version < new Version(5, 7, 8)) Assert.Ignore("JSON data type was included in MySQL Server from v5.7.8");

      ExecuteSQL("CREATE TABLE Test(jsonValue json NOT NULL)");
      ExecuteSQL("CREATE PROCEDURE spTest(IN p_jsonValue JSON) BEGIN INSERT INTO Test VALUES(p_jsonValue); END");

      using (var conn = new MySqlConnection(Connection.ConnectionString))
      {
        conn.Open();

        var cmd = new MySqlCommand("spTest", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        var json = "{\"prop\":[null]}";
        cmd.Parameters.Add(new MySqlParameter { ParameterName = "p_jsonValue", Value = json });
        cmd.Prepare();
        cmd.ExecuteNonQuery();

        cmd = new MySqlCommand("SELECT jsonValue FROM Test", conn);
        cmd.CommandType = CommandType.Text;
        StringAssert.AreEqualIgnoringCase(json, cmd.ExecuteScalar().ToString().Replace(" ", ""));
      }
    }

    [Test]
    public void PassBoolParameter()
    {
      ExecuteSQL("CREATE PROCEDURE spTest(success BOOL) BEGIN SELECT success; END");

      using (var conn = new MySqlConnection(Connection.ConnectionString + ";"))
      {
        conn.Open();

        var cmd = new MySqlCommand("spTest", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new MySqlParameter { ParameterName = "success", Value = true, MySqlDbType = MySqlDbType.Int32 });
        cmd.Prepare();
        Assert.IsTrue(Convert.ToBoolean(cmd.ExecuteScalar()));
      }
    }

    [Test]
    public void PassDateTimeParameter()
    {
      ExecuteSQL($"CREATE PROCEDURE spTest(OUT value DATETIME) BEGIN SELECT '2020-11-27 12:25:59' INTO value; END");

      using (var connection = new MySqlConnection(Connection.ConnectionString))
      {
        connection.Open();

        var cmd = new MySqlCommand("spTest", connection);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new MySqlParameter
        {
          ParameterName = "@value",
          DbType = DbType.DateTime,
          Direction = ParameterDirection.Output
        });

        cmd.Prepare();
        cmd.ExecuteNonQuery();
        var result = cmd.Parameters["@value"].Value;

        DateTime dateTime = new DateTime(2020, 11, 27, 12, 25, 59);
        Assert.AreEqual(dateTime, result);
      }

      ExecuteSQL("DROP PROCEDURE spTest");
      ExecuteSQL("CREATE TABLE Test(DATETIME dateTime)");
      ExecuteSQL("CREATE PROCEDURE spTest(IN p_dateTimeValue DATETIME) BEGIN INSERT INTO Test VALUES(p_dateTimeValue); END");

      using (var conn = new MySqlConnection(Connection.ConnectionString))
      {
        conn.Open();

        var cmd = new MySqlCommand("spTest", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        DateTime dateTime = new DateTime(2020, 11, 27, 12, 25, 59);
        cmd.Parameters.Add(new MySqlParameter { ParameterName = "p_dateTimeValue", Value = dateTime, MySqlDbType = MySqlDbType.DateTime });
        cmd.ExecuteNonQuery();

        cmd = new MySqlCommand("SELECT dateTime FROM Test", conn);
        cmd.CommandType = CommandType.Text;
        Assert.AreEqual(dateTime, cmd.ExecuteScalar());
      }
    }

    public enum TestStatus
    {
      Pending = 1,
      InProgress = 2,
      Cancel = 3
    }

    /// <summary >
    /// Bug #101424	Insert/Update enum error "Value *column name* is not of the correct type".
    /// </summary>
    [Test]
    public void PassEnumParameter()
    {
      ExecuteSQL("CREATE PROCEDURE spTest(data ENUM('Pending','InProgress','Cancel'), ID int) BEGIN SELECT 1; END");
      using (var connection = new MySqlConnection(Settings.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("spTest", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new MySqlParameter("data", TestStatus.InProgress));
        command.Parameters.Add(new MySqlParameter("ID", 1));
        command.ExecuteNonQuery();
      }
    }

    [Test]
    public void EventsStatementsHistory()
    {
      if (Version < new Version(8, 0, 0)) Assert.Ignore("This test is for MySql 8.0 or higher");
      bool testResult = false;
      var spName = "spGetCount";
      var cmd = new MySqlCommand(" show variables like 'general_log'", Connection);
      using (var rdr = cmd.ExecuteReader())
      {
        while (rdr.Read())
        {
          if (rdr.GetString(1) != "ON") Assert.Ignore("general_log is disabled");
        }
      }

      ExecuteSQL("CREATE PROCEDURE spGetCount() BEGIN SELECT 5; END");
      cmd = new MySqlCommand(spName, Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      var cmd2 = new MySqlCommand("truncate table performance_schema.events_statements_history", Connection);
      cmd2.ExecuteNonQuery();

      using (var rdr = cmd.ExecuteReader())
      {
        while (rdr.Read())
        {
          Assert.AreEqual("5", rdr.GetString(0));
        }
      }

      cmd = new MySqlCommand("select SQL_TEXT from performance_schema.events_statements_history where SQL_text is not null;", Connection);
      using (var rdr = cmd.ExecuteReader())
      {
        while (rdr.Read())
        {
          if (rdr.GetString(0).ToString().Contains($"CALL {spName}()"))
          {
            testResult = true;
          }
        }
      }
      Assert.True(testResult);

      cmd = new MySqlCommand($"SELECT count(*) FROM information_schema.routines WHERE 1=1 AND ROUTINE_SCHEMA='{Settings.Database}' AND ROUTINE_NAME='{spName}';", Connection);
      var count = cmd.ExecuteScalar();
      Assert.AreEqual(1, count);

    }

    /// <summary>
    /// Bug #33097912	- FULLY QUALIFIED PROCEDURE OR FUNCTION NAMES FAIL
    /// </summary>
    [Test]
    public void FullyQualifiedProcedures()
    {
      ExecuteSQL("DROP SCHEMA IF EXISTS Test; CREATE SCHEMA Test");
      ExecuteSQL("CREATE PROCEDURE `Test`.`spTest` () BEGIN SELECT 1; END");

      using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
      {
        conn.Open();

        MySqlCommand command = conn.CreateCommand();
        command.CommandText = $"`Test`.`spTest`";
        command.CommandType = CommandType.StoredProcedure;
        Assert.AreEqual(1, command.ExecuteScalar());

        command.CommandText = $"Test.spTest";
        command.CommandType = CommandType.StoredProcedure;
        Assert.AreEqual(1, command.ExecuteScalar());
      }
    }

    /// <summary>
    /// Bug #33338458	- Cannot execute stored procedure with backtick in name
    /// </summary>
    [TestCase("`a``b`", "a`b")]
    [TestCase("`a``b`", "`a``b`")]
    [TestCase("`my``crazy``proc`", "my`crazy`proc")]
    [TestCase("`test.spTest2`", "`test.spTest2`")]
    [TestCase("`my``.``proc`", "my`.`proc")]
    [TestCase("`foo``bar``.quaz`", "foo`bar`.quaz")]
    public void StoredProceduresWithBackticks(string quotedName, string spNameCnet)
    {
      // In this case since the spNameCnet is not well written, the connector will quote everything
      ExecuteSQL($"DROP PROCEDURE IF EXISTS {quotedName}");
      ExecuteSQL($"CREATE PROCEDURE {quotedName} () BEGIN SELECT 1; END");
      using (var conn = new MySqlConnection(Connection.ConnectionString))
      {
        conn.Open();
        MySqlCommand command = conn.CreateCommand();
        command.CommandText = spNameCnet;
        command.CommandType = CommandType.StoredProcedure;

        Assert.AreEqual(1, command.ExecuteScalar());
      }
    }

    [TestCase("`my``.``db`", "`my``.``proc`", "`my``.``db`.`my``.``proc`")]
    [TestCase("`my````schema`", "`my````proc`", "`my````schema`.`my````proc`")]
    [TestCase("`my``schema`", "`my``proc`", "`my``schema`.`my``proc`")]
    [TestCase("`my.schema`", "myproc", "`my.schema`.myproc")]
    [TestCase("foo", "`bar.baz`", "foo.`bar.baz`")]
    [TestCase("foo", "bar", "    foo.bar    ")]
    [TestCase("foo", "bar", "    `foo`.`bar`    ")]
    public void StoredProceduresWithBackticks2(string schema, string spName, string spNameCnet)
    {
      ExecuteSQL($"DROP SCHEMA IF EXISTS {schema}; CREATE SCHEMA {schema}", true);
      ExecuteSQL($"CREATE PROCEDURE {schema}.{spName} () BEGIN SELECT 1; END", true);
      using (var conn = new MySqlConnection(Connection.ConnectionString))
      {
        conn.Open();
        MySqlCommand command = conn.CreateCommand();
        command.CommandText = spNameCnet;
        command.CommandType = CommandType.StoredProcedure;

        Assert.AreEqual(1, command.ExecuteScalar());
      }

      ExecuteSQL($"DROP SCHEMA {schema};", true);
    }

    [TestCase("`myschema`", "`my``proc`", "myschema.`my`proc`")]
    [TestCase("`myschema`", "`my``proc`", "`myschema.my`proc")]
    [TestCase("`myschema`", "`my``proc`", "myschema.my``proc")]
    [TestCase("`myschema`", "`my``proc`", "myschema.`my`proc`")]
    [TestCase("`my``.``schema`", "`my``.``proc`", "my`.`schema.my`.`proc")]
    [TestCase("`my``schema`", "`myproc`", "`my`schema`.myproc")]
    [TestCase("`my``schema`", "`myproc`", "my``schema.myproc")]
    [TestCase("`my.schema`", "`myproc`", "my.schema.myproc")]
    [TestCase("foo", "`bar.baz`", "`foo`.bar.baz")]
    public void StoredProceduresWithBackticksExceptionRaised(string schema, string spName, string spNameCnet)
    {
      ExecuteSQL($"DROP SCHEMA IF EXISTS {schema}; CREATE SCHEMA {schema}", true);
      ExecuteSQL($"CREATE PROCEDURE {schema}.{spName} () BEGIN SELECT 1; END", true);

      using (var conn = new MySqlConnection(Connection.ConnectionString))
      {
        conn.Open();
        MySqlCommand command = conn.CreateCommand();
        command.CommandText = spNameCnet;
        command.CommandType = CommandType.StoredProcedure;

        Assert.Throws<MySqlException>(() => command.ExecuteScalar());
      }

      ExecuteSQL($"DROP SCHEMA {schema};", true);
    }

    [TestCase("test.test.spTest", false)]
    [TestCase("`myschema`.my`table`", false)]
    [TestCase("`db``.`tbl`", false)]
    [TestCase("my``.``db.`my``.``proc`", false)]
    [TestCase("`db`.`my``other```proc`", false)]
    [TestCase("a``b", false)]
    [TestCase("a`b`c", false)]
    [TestCase("my`.`db.my`.`table", false)]
    [TestCase("`myschema.my`proc", false)]
    [TestCase("`foo`bar`", false)]
    [TestCase(" \n`foo`.bar", false)]
    [TestCase("foo.`bar`.baz", false)]
    [TestCase("`foo`.bar.baz", false)]
    [TestCase("foo.`bar.baz`", true)]
    [TestCase("`schema`.`procedure`", true)]
    [TestCase("`a``b`", true)]
    [TestCase("`schema`.proc", true)]
    [TestCase("schema.`proc`", true)]
    [TestCase("`my``.``db`.`my``.``proc`", true)]
    [TestCase("`my``.``proc`", true)]
    [TestCase("`my``db`.myproc", true)]
    [TestCase("`my``db`.`myproc`", true)]
    [TestCase("`my``db``proc`", true)]
    [TestCase("a.b", true)]
    [TestCase("`a.b`", true)]
    [TestCase("test.```spTest``3`", true)]
    [TestCase("`db``.``1`.`tbl`", true)]
    [TestCase("`   foo`.bar", true)]
    public void IsSyntacticallyCorrect(string spName, bool isIt)
    {
      Assert.IsTrue(MySqlClient.StoredProcedure.IsSyntacticallyCorrect(spName) == isIt);
    }
  }
}