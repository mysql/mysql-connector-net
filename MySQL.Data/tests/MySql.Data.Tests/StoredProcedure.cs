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
using System.Globalization;
using System.Threading;
#if !NETSTANDARD
using System.Data.Common;
#endif

namespace MySql.Data.MySqlClient.Tests
{
  public class StoredProcedure : TestBase
  {
    public StoredProcedure(TestFixture fixture) : base(fixture)
    {
      Cleanup();
    }

    protected override void Cleanup()
    {
      executeSQL("DROP PROCEDURE IF EXISTS spTest");
    }

    /// <summary>
    /// Bug #7623  	Adding MySqlParameter causes error if MySqlDbType is Decimal
    /// </summary>
    [Fact]
    public void ReturningResultset()
    {
      // create our procedure
      executeSQL("CREATE PROCEDURE spTest(val decimal(10,3)) begin select val; end");

      using (MySqlCommand cmd = new MySqlCommand("spTest", Connection))
      {
        cmd.CommandType = CommandType.StoredProcedure;

        MySqlParameter p = cmd.Parameters.Add("?val", MySqlDbType.Decimal);
        p.Precision = 10;
        p.Scale = 3;
        p.Value = 21;

        decimal id = (decimal)cmd.ExecuteScalar();
        Assert.Equal(21, id);
      }
    }

    [Fact]
    public void NonQuery()
    {
      executeSQL("CREATE TABLE Test(id INT, name VARCHAR(20))");
      executeSQL(@"CREATE PROCEDURE spTest(IN value INT) 
        BEGIN INSERT INTO Test VALUES(value, 'Test'); END");

      //setup testing data
      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?value", 2);
      int rowsAffected = cmd.ExecuteNonQuery();
      Assert.Equal(1, rowsAffected);

      cmd.CommandText = "SELECT * FROM Test";
      cmd.CommandType = CommandType.Text;
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.Equal(2, reader.GetInt32(0));
        Assert.Equal("Test", reader.GetString(1));
        Assert.False(reader.Read());
        Assert.False(reader.NextResult());
      }
    }

    [Fact]
    public void NoBatch()
    {
      MySqlCommand cmd = new MySqlCommand("spTest;select * from Test", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      Exception ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
    }

    [Fact]
    public void WrongParameters()
    {
      executeSQL("CREATE PROCEDURE spTest(p1 INT) BEGIN SELECT 1; END");
      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?p2", 1);
      Exception ex = Assert.Throws<ArgumentException>(() =>cmd.ExecuteNonQuery());
    }

    [Fact]
    public void NoInOutMarker()
    {
      // create our procedure
      executeSQL("CREATE PROCEDURE spTest( valin varchar(50) ) BEGIN  SELECT valin;  END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?valin", "myvalue");
      object val = cmd.ExecuteScalar();
      Assert.Equal("myvalue", val);
    }

    [Fact]
    public void NoSPOnPre50()
    {
      //try
      //{
        MySqlCommand cmd = new MySqlCommand("spTest", Connection);
        cmd.CommandType = CommandType.StoredProcedure;
        Exception ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
        //Assert.Fail("This should not have worked");
      //}
      //catch (Exception)
      //{
      //}
    }

    /// <summary>
    /// Bug #13590  	ExecuteScalar returns only Int64 regardless of actual SQL type
    /// </summary>
    [Fact]
    public void ExecuteScalar2()
    {
      // create our procedure
      executeSQL("CREATE PROCEDURE spTest() " +
         "BEGIN  DECLARE myVar1 INT; SET myVar1 := 1; SELECT myVar1; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      object result = cmd.ExecuteScalar();
      Assert.Equal(1, result);
      Assert.True(result is Int32);
    }

    [Fact]
    public void MultipleResultsets()
    {
      MultipleResultsetsImpl(false);
    }

    [Fact(Skip="Fix This")]
    public void MultipleResultsetsPrepared()
    {
      MultipleResultsetsImpl(true);
    }

    private void MultipleResultsetsImpl(bool prepare)
    {
      executeSQL("DROP PROCEDURE IF EXISTS multiResults");
      // create our procedure
      executeSQL("CREATE PROCEDURE multiResults() BEGIN  SELECT 1;  SELECT 2; END");

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

#if !NETCOREAPP1_1
      DataSet ds = new DataSet();
      MySqlCommand cmd2 = new MySqlCommand("multiResults", Connection);
      cmd2.CommandType = CommandType.StoredProcedure;
      MySqlDataAdapter da = new MySqlDataAdapter(cmd2);
      da.FillError += new FillErrorEventHandler(da_FillError);
      fillError = null;
      da.Fill(ds);
      Assert.Equal(2, ds.Tables.Count);
      Assert.Equal(1, Convert.ToInt32(ds.Tables[0].Rows.Count));
      Assert.Equal(1, Convert.ToInt32(ds.Tables[1].Rows.Count));
      Assert.Equal(1, Convert.ToInt32(ds.Tables[0].Rows[0][0]));
      Assert.Equal(2, Convert.ToInt32(ds.Tables[1].Rows[0][0]));
      Assert.Null(fillError);
#endif
    }

#if !NETCOREAPP1_1

    private static string fillError = null;

    private static void da_FillError(object sender, FillErrorEventArgs e)
    {
      fillError = e.Errors.Message;
      e.Continue = true;
    }
#endif

    [Fact]
    public void ExecuteWithCreate()
    {
      // create our procedure
      string sql = "CREATE PROCEDURE spTest(IN var INT) BEGIN  SELECT var; END; call spTest(?v)";

      MySqlCommand cmd = new MySqlCommand(sql, Connection);
      cmd.Parameters.Add(new MySqlParameter("?v", 33));
      object val = cmd.ExecuteScalar();
      Assert.Equal(33, val);
    }

    /// <summary>
    /// Bug #9722 Connector does not recognize parameters separated by a linefeed 
    /// </summary>
    [Fact]
    public void OtherProcSigs()
    {
      // create our procedure
      executeSQL("CREATE PROCEDURE spTest(IN \r\nvalin DECIMAL(10,2),\nIN val2 INT) " +
        "SQL SECURITY INVOKER BEGIN  SELECT valin; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?valin", 20.4);
      cmd.Parameters.AddWithValue("?val2", 4);
      decimal val = (decimal)cmd.ExecuteScalar();
      Decimal d = new Decimal(20.4);
      Assert.Equal(d, val);

      // create our second procedure
      executeSQL("DROP PROCEDURE IF EXISTS spTest");
      executeSQL("CREATE PROCEDURE spTest( \r\n) BEGIN  SELECT 4; END");
      cmd.Parameters.Clear();
      object val1 = cmd.ExecuteScalar();
      Assert.Equal(4, Convert.ToInt32(val1));
    }

    /// <summary>
    /// Bug #11450  	Connector/NET, current database and stored procedures
    /// </summary>
    [Fact]
    public void NoDefaultDatabase()
    {
      // create our procedure
      executeSQL("CREATE PROCEDURE spTest() BEGIN  SELECT 4; END");
      string dbName = Fixture.CreateDatabase("1");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      object val = cmd.ExecuteScalar();
      Assert.Equal(4, Convert.ToInt32(val));

      cmd.CommandText = String.Format("USE `{0}`", dbName);
      cmd.CommandType = CommandType.Text;
      cmd.ExecuteNonQuery();

      cmd.CommandText = String.Format("`{0}`.spTest", Connection.Database);
      cmd.CommandType = CommandType.StoredProcedure;
      val = cmd.ExecuteScalar();
      Assert.Equal(4, Convert.ToInt32(val));
    }

    /// <summary>
    /// Bug #13590  	ExecuteScalar returns only Int64 regardless of actual SQL type
    /// </summary>
    /*		[Fact]
        public void TestSelectingInts()
        {
          executeSQL("CREATE PROCEDURE spTest() BEGIN DECLARE myVar INT; " +
            "SET MyVar := 1; SELECT CAST(myVar as SIGNED); END");

          MySqlCommand cmd = new MySqlCommand("spTest", connection);
          cmd.CommandType = CommandType.StoredProcedure;
          object val = cmd.ExecuteScalar();
          Assert.Equal(1, val, "Checking value");
          Assert.True(val is Int32, "Checking type");
        }
    */

    /// <summary>
    /// Bug #11386  	Numeric parameters with Precision and Scale not taken into account by Connector
    /// </summary>
    [Fact]
    public void DecimalAsParameter()
    {
      executeSQL("CREATE PROCEDURE spTest(IN d DECIMAL(19,4)) BEGIN SELECT d; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?d", 21);
      decimal d = (decimal)cmd.ExecuteScalar();
      Assert.Equal(21, d);
    }

    /// <summary>
    /// Bug #6902  	Errors in parsing stored procedure parameters
    /// </summary>
    [Fact]
    public void ParmWithCharacterSet()
    {
      executeSQL("CREATE PROCEDURE spTest(P longtext character set utf8) " +
        "BEGIN SELECT P; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?P", "This is my value");
      string p = (string)cmd.ExecuteScalar();
      Assert.Equal("This is my value", p);
    }

    /// <summary>
    /// Bug #13753  	Exception calling stored procedure with special characters in parameters
    /// </summary>
    [Fact]
    public void SpecialCharacters()
    {
      executeSQL("SET sql_mode=ANSI_QUOTES");
      try
      {
        executeSQL("CREATE PROCEDURE spTest(\"@Param1\" text) BEGIN SELECT \"@Param1\"; END");

        MySqlCommand cmd = new MySqlCommand("spTest", Connection);
        cmd.Parameters.AddWithValue("@Param1", "This is my value");
        cmd.CommandType = CommandType.StoredProcedure;

        string val = (string)cmd.ExecuteScalar();
        Assert.Equal("This is my value", val);
      }
      finally
      {
        executeSQL("SET sql_mode=\"\"");
      }
    }

    [Fact]
    public void CallingSPWithPrepare()
    {
      executeSQL("CREATE PROCEDURE spTest(P int) BEGIN SELECT P; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?P", 33);
      cmd.Prepare();

      int p = (int)cmd.ExecuteScalar();
      Assert.Equal(33, p);
    }

    /// <summary>
    /// Bug #13927  	Multiple Records to same Table in Transaction Problem
    /// </summary>
    [Fact]
    public void MultipleRecords()
    {
      executeSQL("CREATE TABLE Test (id INT, name VARCHAR(20))");
      executeSQL("CREATE PROCEDURE spTest(id int, str VARCHAR(45)) " +
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

#if NETCOREAPP1_1
      MySqlCommand cmdSelect = new MySqlCommand("SELECT * FROM Test", Connection);
      using (MySqlDataReader dr = cmdSelect.ExecuteReader())
      {
        Assert.True(dr.Read());
        Assert.Equal(1, dr.GetInt32("id"));
        Assert.Equal("First record", dr.GetString("name"));

        Assert.True(dr.Read());
        Assert.Equal(2, dr.GetInt32("id"));
        Assert.Equal("Second record", dr.GetString("name"));
      }
#else
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", Connection);
      DataTable dt = new DataTable();
      da.Fill(dt);

      Assert.Equal(1, dt.Rows[0]["id"]);
      Assert.Equal(2, dt.Rows[1]["id"]);
      Assert.Equal("First record", dt.Rows[0]["name"]);
      Assert.Equal("Second record", dt.Rows[1]["name"]);
#endif
    }

    /// <summary>
    /// Bug #16788 Only byte arrays and strings can be serialized by MySqlBinary 
    /// </summary>
    [Fact]
    public void Bug16788()
    {
      executeSQL("CREATE TABLE Test (id integer(9), state varchar(2))");
      executeSQL("CREATE PROCEDURE spTest(IN p1 integer(9), IN p2 varchar(2)) " +
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

#if !NETCOREAPP1_1
    //[Explicit]
    [Fact]
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
          executeSQL("CREATE PROCEDURE spTest" + x + "() BEGIN SELECT 1; END");
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
        Assert.Equal(190, myListener.Find("from procedure cache"));
        Assert.Equal(10, myListener.Find("from server"));
      }
    }
#endif

    /// <summary>
    /// Bug #20581 Null Reference Exception when closing reader after stored procedure. 
    /// </summary>
    [Fact]
    public void Bug20581()
    {
      executeSQL("CREATE PROCEDURE spTest(p int) BEGIN SELECT p; END");
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
    [Fact]
    public void PreparedReader()
    {
      executeSQL("CREATE TABLE  Test (id int(10) unsigned NOT NULL default '0', " +
         "val int(10) unsigned default NULL, PRIMARY KEY (id)) " +
         "ENGINE=InnoDB DEFAULT CHARSET=utf8");
      executeSQL("CREATE PROCEDURE spTest (IN pp INTEGER) " +
            "select * from Test where id > pp ");

      MySqlCommand c = new MySqlCommand("spTest", Connection);
      c.CommandType = CommandType.StoredProcedure;
#if NETCOREAPP1_1
      MySqlParameter p = c.CreateParameter();
#else
      IDataParameter p = c.CreateParameter();
#endif
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

#if !NETCOREAPP1_1

    /// <summary>
    /// Bug #22452 MySql.Data.MySqlClient.MySqlException: 
    /// </summary>
    [Fact]
    public void TurkishStoredProcs()
    {
      executeSQL("CREATE PROCEDURE spTest(IN p_paramname INT) BEGIN SELECT p_paramname; END");
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

#endif

    /// <summary>
    /// Bug #23268 System.FormatException when invoking procedure with ENUM input parameter 
    /// </summary>
    [Fact]
    public void ProcEnumParamTest()
    {
      executeSQL("CREATE TABLE Test(str VARCHAR(50), e ENUM ('P','R','F','E'), i INT(6))");
      executeSQL("CREATE PROCEDURE spTest(IN p_enum ENUM('P','R','F','E')) BEGIN " +
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
        Assert.Equal("P", reader.GetString(0));
      }
    }

#if !NETCOREAPP1_1
    /// <summary>
    /// Bug #25609 MySqlDataAdapter.FillSchema 
    /// </summary>
    [Fact]
    public void GetSchema()
    {
      executeSQL("CREATE PROCEDURE GetSchema() BEGIN SELECT * FROM Test; END");
      executeSQL(@"CREATE TABLE Test(id INT AUTO_INCREMENT, name VARCHAR(20), PRIMARY KEY (id)) ");

      MySqlCommand cmd = new MySqlCommand("GetSchema", Connection);
      cmd.CommandType = CommandType.StoredProcedure;

      MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
      reader.Read();
      reader.Close();

      MySqlDataAdapter da = new MySqlDataAdapter(cmd);
      DataTable schema = new DataTable();
      da.FillSchema(schema, SchemaType.Source);
      Assert.Equal(2, schema.Columns.Count);
    }
#endif

    /// <summary>
    /// Bug #26139 MySqlCommand.LastInsertedId doesn't work for stored procedures 
    /// Currently this is borked on the server so we are marking this as notworking
    /// until the server has this fixed.
    /// </summary>
    /*        [Fact]
        public void LastInsertId()
        {
          executeSQL("CREATE TABLE Test (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(200))");
          executeSQL("INSERT INTO Test VALUES (NULL, 'Test1')");
          executeSQL("CREATE PROCEDURE spTest() BEGIN " +
            "INSERT INTO Test VALUES (NULL, 'test'); END");

          MySqlCommand cmd = new MySqlCommand("spTest", connection);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.ExecuteNonQuery();
          Assert.Equal(2, cmd.LastInsertedId);
        }
        */

    /// <summary>
    /// Bug #27093 Exception when using large values in IN UInt64 parameters 
    /// </summary>
    [Fact]
    public void UsingUInt64AsParam()
    {
      executeSQL(@"CREATE TABLE Test(f1 bigint(20) unsigned NOT NULL,
            PRIMARY KEY(f1)) ENGINE=InnoDB DEFAULT CHARSET=utf8");

      executeSQL(@"CREATE PROCEDURE spTest(in _val bigint unsigned)
            BEGIN insert into  Test set f1=_val; END");

#if NETCOREAPP1_1
      MySqlCommand cmd = new MySqlCommand();
      MySqlParameter param = cmd.CreateParameter();
      param.MySqlDbType = MySqlDbType.UInt64;
#else
      DbCommand cmd = new MySqlCommand();
      DbParameter param = cmd.CreateParameter();
      param.DbType = DbType.UInt64;
#endif
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
    [Fact]
    public void CatalogWithHyphens()
    {
      // make sure this test is valid
      Assert.True(Connection.Database.IndexOf('-') != -1);

      MySqlCommand cmd = new MySqlCommand("CREATE PROCEDURE spTest() BEGIN SELECT 1; END", Connection);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "spTest";
      cmd.CommandType = CommandType.StoredProcedure;
      Assert.Equal(1, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Fact]
    public void ComplexDefinition()
    {
      Cleanup();
      executeSQL(@"CREATE PROCEDURE `spTest`() NOT DETERMINISTIC
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

#if !NETCOREAPP1_1
    [Fact]
    public void AmbiguousColumns()
    {
      executeSQL("CREATE TABLE t1 (id INT)");
      executeSQL("CREATE TABLE t2 (id1 INT, id INT)");
      executeSQL(@"CREATE PROCEDURE spTest() BEGIN SELECT * FROM t1; 
            SELECT id FROM t1 JOIN t2 on t1.id=t2.id; 
            SELECT * FROM t2; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.CommandTimeout = 0;
      MySqlDataAdapter da = new MySqlDataAdapter(cmd);
      DataSet ds = new DataSet();
      //try
      //{
        Exception ex = Assert.Throws<MySqlException>(() => da.Fill(ds));
        //Assert.Fail("The above should have thrown an exception");
      //}
      //catch (Exception)
      //{
      //}
    }
#endif

    /// <summary>
    /// Bug #41034 .net parameter not found in the collection
    /// </summary>
    [Fact]
    public void SPWithSpaceInParameterType()
    {
      executeSQL("CREATE PROCEDURE spTest(myparam decimal  (8,2)) BEGIN SELECT 1; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.Parameters.Add("@myparam", MySqlDbType.Decimal).Value = 20;
      cmd.CommandType = CommandType.StoredProcedure;
      object o = cmd.ExecuteScalar();
      Assert.Equal(1, Convert.ToInt32(o));
    }

    private void ParametersInReverseOrderInternal(bool isOwner)
    {
      executeSQL(@"CREATE PROCEDURE spTest(IN p_1 VARCHAR(5), IN p_2 VARCHAR(5))
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
#if NETCOREAPP1_1
        cmd.Parameters[0].MySqlDbType = MySqlDbType.String;
        cmd.Parameters[1].MySqlDbType = MySqlDbType.String;

        using (MySqlDataReader dr = cmd.ExecuteReader())
        {
          Assert.True(dr.Read());
          if (!isOwner)
          {
            Assert.Equal("World", dr.GetValue(0));
            Assert.Equal("Hello", dr.GetValue(1));
          }
          else
          {
            Assert.Equal("Hello", dr.GetString("P1"));
            Assert.Equal("World", dr.GetString("P2"));
          }
        }
#else
        cmd.Parameters[0].DbType = DbType.AnsiString;
        cmd.Parameters[1].DbType = DbType.AnsiString;
        MySqlDataAdapter da = new MySqlDataAdapter(cmd);
        DataTable dt = new DataTable();
        da.Fill(dt);
        if (!isOwner)
        {
          Assert.Equal("World", dt.Rows[0][0]);
          Assert.Equal("Hello", dt.Rows[0][1]);
        }
        else
        {
          Assert.Equal("Hello", dt.Rows[0]["P1"]);
          Assert.Equal("World", dt.Rows[0]["P2"]);
        }
#endif
      }
    }

    [Fact]
    public void ParametersInReverseOrderNotOwner()
    {
      ParametersInReverseOrderInternal(false);
    }

    [Fact]
    public void ParametersInReverseOrderOwner()
    {
      ParametersInReverseOrderInternal(true);
    }

#if !NETCOREAPP1_1
    [Fact]
    public void DeriveParameters()
    {
      executeSQL(@"CREATE  PROCEDURE spTest (id INT, name VARCHAR(20))
          BEGIN SELECT name; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      MySqlCommandBuilder.DeriveParameters(cmd);
      Assert.Equal(2, cmd.Parameters.Count);
    }
#endif

    /// <summary>
    /// Bug #52562 Sometimes we need to reload cached function parameters 
    /// </summary>
    [Fact]
    public void ProcedureCacheMiss()
    {
      executeSQL("CREATE PROCEDURE spTest(id INT) BEGIN SELECT 1; END");

      string connStr = Connection.ConnectionString + ";procedure cache size=25";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("spTest", c);
        cmd.Parameters.AddWithValue("@id", 1);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.ExecuteScalar();

        executeSQL("DROP PROCEDURE spTest");
        executeSQL("CREATE PROCEDURE spTest(id INT, id2 INT, id3 INT) BEGIN SELECT 1; END");

        cmd.Parameters.AddWithValue("@id2", 2);
        cmd.Parameters.AddWithValue("@id3", 3);
        cmd.ExecuteScalar();
      }
    }

#if !NETCOREAPP1_1
    /// <summary>
    /// Verifies that GetProcedureParameters does not require SELECT permission on mysql.proc table.
    /// </summary>
    [Fact]
    public void GetProcedureParametersDoesNotRequireSelectFromMySqlProceduresTable()
    {
      executeSQL(@"CREATE  PROCEDURE spTest(id INT, name VARCHAR(20))
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

        MySqlSchemaCollection parametersTable = isp.GetProcedureParameters(rest, new MySqlSchemaCollection( procTable ));

        Assert.NotNull(parametersTable);
      }
    }
#endif

    /// <summary>
    /// Validates a stored procedure call without the "call" statement
    /// Bug #14008699
    /// </summary>
    [Fact]
    public void CallStoredProcedure()
    {
      executeSQL("CREATE PROCEDURE GetCount() BEGIN SELECT 5; END");

      MySqlCommand cmd = new MySqlCommand("GetCount", Connection);
      cmd.CommandType = CommandType.Text;

      Assert.Equal(5, Convert.ToInt32(cmd.ExecuteScalar()));
    }

  }
}
