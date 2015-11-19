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
using System.Globalization;
using System.Threading;
#if !RT
using System.Data.Common;
#endif

namespace MySql.Data.MySqlClient.Tests
{
  public class StoredProcedure : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;

    private static string fillError = null;


    public void SetFixture(SetUpClass data)
    {
      st = data;
      st.accessToMySqlDb = true;

      if (st.conn.connectionState != ConnectionState.Open)
        st.conn.Open();
    }

    public void Dispose()
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");
      st.execSQL("DROP PROCEDURE IF EXISTS spTest");
      st.conn.Close();
    }

    /// <summary>
    /// Bug #7623  	Adding MySqlParameter causes error if MySqlDbType is Decimal
    /// </summary>
    [Fact]
    public void ReturningResultset()
    {
      if (st.Version < new Version(5, 0)) return;

      // create our procedure
      st.execSQL("CREATE PROCEDURE spTest(val decimal(10,3)) begin select val; end");

      using (MySqlCommand cmd = new MySqlCommand("spTest", st.conn))
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE TABLE Test(id INT, name VARCHAR(20))");
      st.execSQL(@"CREATE PROCEDURE spTest(IN value INT) 
        BEGIN INSERT INTO Test VALUES(value, 'Test'); END");

      //setup testing data
      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
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
      if (st.Version < new Version(5, 0)) return;

      //try
      //{
        MySqlCommand cmd = new MySqlCommand("spTest;select * from Test", st.conn);
        cmd.CommandType = CommandType.StoredProcedure;
        Exception ex = Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());
      //  Assert.Fail("Should have thrown an exception");
      //}
      //catch (Exception)
      //{
      //}
    }

    [Fact]
    public void WrongParameters()
    {
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE PROCEDURE spTest(p1 INT) BEGIN SELECT 1; END");
      try
      {
        MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("?p2", 1);
        Exception ex = Assert.Throws<Exception>(() =>cmd.ExecuteNonQuery());
        //Assert.Fail("Should have thrown an exception");
      }
      catch (Exception)
      {
      }
    }

    [Fact]
    public void NoInOutMarker()
    {
      if (st.Version < new Version(5, 0)) return;

      // create our procedure
      st.execSQL("CREATE PROCEDURE spTest( valin varchar(50) ) BEGIN  SELECT valin;  END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?valin", "myvalue");
      object val = cmd.ExecuteScalar();
      Assert.Equal("myvalue", val);
    }

    [Fact]
    public void NoSPOnPre50()
    {
      if (st.Version < new Version(5, 0)) return;

      //try
      //{
        MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
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
      if (st.Version < new Version(5, 0)) return;

      // create our procedure
      st.execSQL("CREATE PROCEDURE spTest() " +
         "BEGIN  DECLARE myVar1 INT; SET myVar1 := 1; SELECT myVar1; END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
      cmd.CommandType = CommandType.StoredProcedure;
      object result = cmd.ExecuteScalar();
      Assert.Equal(1, result);
      Assert.True(result is Int32);
    }

    [Fact]
    public void MultipleResultsets()
    {
      if (st.Version < new Version(5, 0)) return;

      MultipleResultsetsImpl(false);
    }

    [Fact]
    public void MultipleResultsetsPrepared()
    {
      if (st.Version < new Version(5, 0)) return;

      MultipleResultsetsImpl(true);
    }

    private void MultipleResultsetsImpl(bool prepare)
    {
      if (st.Version < new Version(5, 0)) return;

      // create our procedure
      st.execSQL("CREATE PROCEDURE spTest() " +
        "BEGIN  SELECT 1; SELECT 2; END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
      if (prepare) cmd.Prepare();
      cmd.CommandType = CommandType.StoredProcedure;
      MySqlDataReader reader = cmd.ExecuteReader();
      Assert.Equal(true, reader.Read());
      Assert.Equal(true, reader.NextResult());
      Assert.Equal(true, reader.Read());
      Assert.Equal(false, reader.NextResult());
      Assert.Equal(false, reader.Read());
      reader.Close();

#if !RT
      DataSet ds = new DataSet();
      MySqlCommand cmd2 = new MySqlCommand("spTest", st.conn);
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

#if !RT
    private static void da_FillError(object sender, FillErrorEventArgs e)
    {
      fillError = e.Errors.Message;
      e.Continue = true;
    }
#endif

    [Fact]
    public void ExecuteWithCreate()
    {
      if (st.Version < new Version(5, 0)) return;

      // create our procedure
      string sql = "CREATE PROCEDURE spTest(IN var INT) BEGIN  SELECT var; END; call spTest(?v)";

      MySqlCommand cmd = new MySqlCommand(sql, st.conn);
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
      if (st.Version < new Version(5, 0)) return;

      // create our procedure
      st.execSQL("CREATE PROCEDURE spTest(IN \r\nvalin DECIMAL(10,2),\nIN val2 INT) " +
        "SQL SECURITY INVOKER BEGIN  SELECT valin; END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?valin", 20.4);
      cmd.Parameters.AddWithValue("?val2", 4);
      decimal val = (decimal)cmd.ExecuteScalar();
      Decimal d = new Decimal(20.4);
      Assert.Equal(d, val);

      // create our second procedure
      st.execSQL("DROP PROCEDURE IF EXISTS spTest");
      st.execSQL("CREATE PROCEDURE spTest( \r\n) BEGIN  SELECT 4; END");
      cmd.Parameters.Clear();
      object val1 = cmd.ExecuteScalar();
      Assert.Equal(4, Convert.ToInt32(val1));
    }

    /// <summary>
    /// Bug #11450  	Connector/Net, current database and stored procedures
    /// </summary>
    [Fact]
    public void NoDefaultDatabase()
    {
      if (st.Version < new Version(5, 0)) return;

      // create our procedure
      st.execSQL("CREATE PROCEDURE spTest() BEGIN  SELECT 4; END");

      string newConnStr = st.GetConnectionString(false);
      using (MySqlConnection c = new MySqlConnection(newConnStr))
      {
        c.Open();
        MySqlCommand cmd2 = new MySqlCommand(String.Format("use `{0}`", st.database0), c);
        cmd2.ExecuteNonQuery();

        MySqlCommand cmd = new MySqlCommand("spTest", c);
        cmd.CommandType = CommandType.StoredProcedure;
        object val = cmd.ExecuteScalar();
        Assert.Equal(4, Convert.ToInt32(val));

        cmd2.CommandText = String.Format("use `{0}`", st.database1);
        cmd2.ExecuteNonQuery();

        cmd.CommandText = String.Format("`{0}`.spTest", st.database0);
        val = cmd.ExecuteScalar();
        Assert.Equal(4, Convert.ToInt32(val));
      }
    }

    /// <summary>
    /// Bug #13590  	ExecuteScalar returns only Int64 regardless of actual SQL type
    /// </summary>
    /*		[Fact]
        public void TestSelectingInts()
        {
          st.execSQL("CREATE PROCEDURE spTest() BEGIN DECLARE myVar INT; " +
            "SET MyVar := 1; SELECT CAST(myVar as SIGNED); END");

          MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE PROCEDURE spTest(IN d DECIMAL(19,4)) BEGIN SELECT d; END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE PROCEDURE spTest(P longtext character set utf8) " +
        "BEGIN SELECT P; END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("SET sql_mode=ANSI_QUOTES");
      try
      {
        st.execSQL("CREATE PROCEDURE spTest(\"@Param1\" text) BEGIN SELECT \"@Param1\"; END");

        MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
        cmd.Parameters.AddWithValue("@Param1", "This is my value");
        cmd.CommandType = CommandType.StoredProcedure;

        string val = (string)cmd.ExecuteScalar();
        Assert.Equal("This is my value", val);
      }
      finally
      {
        st.execSQL("SET sql_mode=\"\"");
      }
    }

    [Fact]
    public void CallingSPWithPrepare()
    {
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE PROCEDURE spTest(P int) BEGIN SELECT P; END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE TABLE Test (id INT, name VARCHAR(20))");
      st.execSQL("CREATE PROCEDURE spTest(id int, str VARCHAR(45)) " +
           "BEGIN INSERT INTO Test VALUES(id, str); END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
      cmd.CommandType = CommandType.StoredProcedure;

      cmd.Parameters.AddWithValue("?id", 1);
      cmd.Parameters.AddWithValue("?str", "First record");
      cmd.ExecuteNonQuery();

      cmd.Parameters.Clear();
      cmd.Parameters.AddWithValue("?id", 2);
      cmd.Parameters.AddWithValue("?str", "Second record");
      cmd.ExecuteNonQuery();

#if RT
      MySqlCommand cmdSelect = new MySqlCommand("SELECT * FROM Test", st.conn);
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
      MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM Test", st.conn);
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE TABLE Test (id integer(9), state varchar(2))");
      st.execSQL("CREATE PROCEDURE spTest(IN p1 integer(9), IN p2 varchar(2)) " +
        "BEGIN " +
        "INSERT INTO Test (id, state) VALUES (p1, p2); " +
        "END");

      MySqlCommand cmd = st.conn.CreateCommand();
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.CommandText = "spTest";
      cmd.Parameters.Add("?p1", MySqlDbType.UInt16, 9);
      cmd.Parameters["?p1"].Value = 44;
      cmd.Parameters.Add("?p2", MySqlDbType.VarChar, 2);
      cmd.Parameters["?p2"].Value = "ss";
      cmd.ExecuteNonQuery();
    }

#if !RT
    //[Explicit]
    [Fact]
    public void ProcedureCache()
    {
      if (st.Version < new Version(5, 0)) return;

      // open a new connection using a procedure cache
      string connStr = st.GetConnectionString(true);
      connStr += ";procedure cache size=25;logging=true";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        // install our custom trace listener
        GenericListener myListener = new GenericListener();
        System.Diagnostics.Trace.Listeners.Add(myListener);

        for (int x = 0; x < 10; x++)
        {
          st.execSQL("CREATE PROCEDURE spTest" + x + "() BEGIN SELECT 1; END");
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE PROCEDURE spTest(p int) BEGIN SELECT p; END");
      MySqlParameter param1;
      MySqlCommand command = new MySqlCommand("spTest", st.conn);
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE TABLE  Test (id int(10) unsigned NOT NULL default '0', " +
         "val int(10) unsigned default NULL, PRIMARY KEY (id)) " +
         "ENGINE=InnoDB DEFAULT CHARSET=utf8");
      st.execSQL("CREATE PROCEDURE spTest (IN pp INTEGER) " +
            "select * from Test where id > pp ");

      MySqlCommand c = new MySqlCommand("spTest", st.conn);
      c.CommandType = CommandType.StoredProcedure;
#if RT
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

#if !RT

    /// <summary>
    /// Bug #22452 MySql.Data.MySqlClient.MySqlException: 
    /// </summary>
    [Fact]
    public void TurkishStoredProcs()
    {
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE PROCEDURE spTest(IN p_paramname INT) BEGIN SELECT p_paramname; END");
      CultureInfo uiCulture = Thread.CurrentThread.CurrentUICulture;
      CultureInfo culture = Thread.CurrentThread.CurrentCulture;
      Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
      Thread.CurrentThread.CurrentUICulture = new CultureInfo("tr-TR");

      try
      {
        MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE TABLE Test(str VARCHAR(50), e ENUM ('P','R','F','E'), i INT(6))");
      st.execSQL("CREATE PROCEDURE spTest(IN p_enum ENUM('P','R','F','E')) BEGIN " +
        "INSERT INTO Test (str, e, i) VALUES (null, p_enum, 55);  END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
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

#if !RT
    /// <summary>
    /// Bug #25609 MySqlDataAdapter.FillSchema 
    /// </summary>
    [Fact]
    public void GetSchema()
    {
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE PROCEDURE spTest() BEGIN SELECT * FROM Test; END");
      st.execSQL(@"CREATE TABLE Test(id INT AUTO_INCREMENT, name VARCHAR(20), PRIMARY KEY (id)) ");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
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
          st.execSQL("CREATE TABLE Test (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(200))");
          st.execSQL("INSERT INTO Test VALUES (NULL, 'Test1')");
          st.execSQL("CREATE PROCEDURE spTest() BEGIN " +
            "INSERT INTO Test VALUES (NULL, 'test'); END");

          MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL(@"CREATE TABLE Test(f1 bigint(20) unsigned NOT NULL,
            PRIMARY KEY(f1)) ENGINE=InnoDB DEFAULT CHARSET=utf8");

      st.execSQL(@"CREATE PROCEDURE spTest(in _val bigint unsigned)
            BEGIN insert into  Test set f1=_val; END");

#if RT
      MySqlCommand cmd = new MySqlCommand();
      MySqlParameter param = cmd.CreateParameter();
      param.MySqlDbType = MySqlDbType.UInt64;
#else
      DbCommand cmd = new MySqlCommand();
      DbParameter param = cmd.CreateParameter();
      param.DbType = DbType.UInt64;
#endif
      cmd.Connection = st.conn;
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
      if (st.Version < new Version(5, 0)) return;

      // make sure this test is valid
      Assert.True(st.database0.IndexOf('-') != -1);

      MySqlCommand cmd = new MySqlCommand("CREATE PROCEDURE spTest() BEGIN SELECT 1; END", st.conn);
      cmd.ExecuteNonQuery();

      cmd.CommandText = "spTest";
      cmd.CommandType = CommandType.StoredProcedure;
      Assert.Equal(1, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Fact]
    public void ComplexDefinition()
    {
      if (st.Version < new Version(5, 0)) return;

      st.execSQL(@"CREATE PROCEDURE `spTest`() NOT DETERMINISTIC
          CONTAINS SQL SQL SECURITY DEFINER COMMENT '' 
          BEGIN
            SELECT 1,2,3;
          END");
      MySqlCommand command = new MySqlCommand("spTest", st.conn);
      command.CommandType = CommandType.StoredProcedure;
      using (MySqlDataReader reader = command.ExecuteReader())
      {
      }
    }

#if !RT
    [Fact]
    public void AmbiguousColumns()
    {
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE TABLE t1 (id INT)");
      st.execSQL("CREATE TABLE t2 (id1 INT, id INT)");
      st.execSQL(@"CREATE PROCEDURE spTest() BEGIN SELECT * FROM t1; 
            SELECT id FROM t1 JOIN t2 on t1.id=t2.id; 
            SELECT * FROM t2; END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE PROCEDURE spTest(myparam decimal  (8,2)) BEGIN SELECT 1; END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
      cmd.Parameters.Add("@myparam", MySqlDbType.Decimal).Value = 20;
      cmd.CommandType = CommandType.StoredProcedure;
      object o = cmd.ExecuteScalar();
      Assert.Equal(1, Convert.ToInt32(o));
    }

    private void ParametersInReverseOrderInternal(bool isOwner)
    {
      if (st.Version.Major < 5) return;

      st.execSQL(@"CREATE PROCEDURE spTest(IN p_1 VARCHAR(5), IN p_2 VARCHAR(5))
            BEGIN SELECT p_1 AS P1, p_2 AS P2; END");
      string spName = "spTest";

      string connStr = st.GetConnectionString(true);
      if (!isOwner)
        connStr += ";use procedure bodies=false";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand(spName, c);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("?p_2", ("World"));
        cmd.Parameters[0].Direction = ParameterDirection.Input;
        cmd.Parameters.AddWithValue("?p_1", ("Hello"));
        cmd.Parameters[1].Direction = ParameterDirection.Input;
#if RT
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

#if !RT
    [Fact]
    public void DeriveParameters()
    {
      if (st.Version < new Version(5, 0)) return;
      if (st.Version > new Version(6, 0, 6)) return;

      st.execSQL(@"CREATE  PROCEDURE spTest (id INT, name VARCHAR(20))
          BEGIN SELECT name; END");

      MySqlCommand cmd = new MySqlCommand("spTest", st.conn);
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
      st.execSQL("CREATE PROCEDURE spTest(id INT) BEGIN SELECT 1; END");

      string connStr = st.GetConnectionString(true) + ";procedure cache size=25";
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        MySqlCommand cmd = new MySqlCommand("spTest", c);
        cmd.Parameters.AddWithValue("@id", 1);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.ExecuteScalar();

        st.execSQL("DROP PROCEDURE spTest");
        st.execSQL("CREATE PROCEDURE spTest(id INT, id2 INT, id3 INT) BEGIN SELECT 1; END");

        cmd.Parameters.AddWithValue("@id2", 2);
        cmd.Parameters.AddWithValue("@id3", 3);
        cmd.ExecuteScalar();
      }
    }

    /// <summary>
    /// Verifies that GetProcedureParameters does not require SELECT permission on mysql.proc table.
    /// </summary>
    [Fact]
    public void GetProcedureParametersDoesNotRequireSelectFromMySqlProceduresTable()
    {
      if (st.Version < new Version(5, 5, 3)) return;

      st.suExecSQL(String.Format("GRANT ALL ON `{0}`.* to 'simpleuser' identified by 'simpleuser'", st.database0));
      st.execSQL("DROP PROCEDURE IF EXISTS spTest");
      st.execSQL(@"CREATE  PROCEDURE spTest(id INT, name VARCHAR(20))
          BEGIN SELECT name; END");

      string connStr = st.GetConnectionString("simpleuser", "simpleuser", true) + ";use procedure bodies=false";

      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        string[] restrictions = new string[4];
        restrictions[1] = c.Database;
        restrictions[2] = "spTest";
#if RT
        string procTable = "table";
#else
        DataTable procTable = c.GetSchema("procedures", restrictions);
#endif
        ISSchemaProvider isp = new ISSchemaProvider(c);
        string[] rest = isp.CleanRestrictions(restrictions);

        MySqlSchemaCollection parametersTable = isp.GetProcedureParameters(rest, new MySqlSchemaCollection( procTable ));

        Assert.NotNull(parametersTable);
      }
    }

    /// <summary>
    /// Validates a stored procedure call without the "call" statement
    /// Bug #14008699
    /// </summary>
    [Fact]
    public void CallStoredProcedure()
    {
      st.execSQL("CREATE PROCEDURE GetCount() BEGIN SELECT 5; END");

      MySqlCommand cmd = new MySqlCommand("GetCount", st.conn);
      cmd.CommandType = CommandType.Text;

      Assert.Equal(5, Convert.ToInt32(cmd.ExecuteScalar()));
    }

  }
}
