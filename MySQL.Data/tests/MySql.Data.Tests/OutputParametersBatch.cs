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

namespace MySql.Data.MySqlClient.Tests
{
  public class OutputParametersBatch : TestBase
  {
    protected bool prepare;

    internal override void AdjustConnectionSettings(MySqlConnectionStringBuilder settings)
    {
      settings.AllowBatch = true;
    }

    protected override void Cleanup()
    {
      ExecuteSQL(String.Format("DROP TABLE IF EXISTS `{0}`.Test", Connection.Database));
      ExecuteSQL("DROP PROCEDURE IF EXISTS spTest");
      ExecuteSQL("DROP FUNCTION IF EXISTS fnTest");
      Connection.ProcedureCache.Clear();
    }

    /// <summary>
    /// Bug #17814 Stored procedure fails unless DbType set explicitly
    /// Bug #23749 VarChar field size over 255 causes a System.OverflowException 
    /// </summary>
    [Test]
    public void OutputParameters()
    {
      // we don't want to run this test under no access
      Assert.True(Settings.CheckParameters);

      // create our procedure
      ExecuteSQL("CREATE PROCEDURE spTest(out value VARCHAR(350), OUT intVal INT, " +
          "OUT dateVal TIMESTAMP, OUT floatVal FLOAT, OUT noTypeVarChar VARCHAR(20), " +
          "OUT noTypeInt INT) " +
          "BEGIN  SET value='42';  SET intVal=33; SET dateVal='2004-06-05 07:58:09'; " +
          "SET floatVal = 1.2; SET noTypeVarChar='test'; SET noTypeInt=66; END");

      // we use rootConn here since we are using parameters
      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.Add(new MySqlParameter("?value", MySqlDbType.VarChar));
      cmd.Parameters.Add(new MySqlParameter("?intVal", MySqlDbType.Int32));
      cmd.Parameters.Add(new MySqlParameter("?dateVal", MySqlDbType.DateTime));
      cmd.Parameters.Add(new MySqlParameter("?floatVal", MySqlDbType.Float));
      MySqlParameter vcP = new MySqlParameter();
      vcP.ParameterName = "?noTypeVarChar";
      vcP.Direction = ParameterDirection.Output;
      cmd.Parameters.Add(vcP);
      MySqlParameter vcI = new MySqlParameter();
      vcI.ParameterName = "?noTypeInt";
      vcI.Direction = ParameterDirection.Output;
      cmd.Parameters.Add(vcI);
      cmd.Parameters[0].Direction = ParameterDirection.Output;
      cmd.Parameters[1].Direction = ParameterDirection.Output;
      cmd.Parameters[2].Direction = ParameterDirection.Output;
      cmd.Parameters[3].Direction = ParameterDirection.Output;
      if (prepare) cmd.Prepare();
      int rowsAffected = cmd.ExecuteNonQuery();

      Assert.AreEqual(0, rowsAffected);
      Assert.AreEqual("42", cmd.Parameters[0].Value);
      Assert.AreEqual(33, cmd.Parameters[1].Value);
      Assert.AreEqual(new DateTime(2004, 6, 5, 7, 58, 9),
               Convert.ToDateTime(cmd.Parameters[2].Value));
      Assert.AreEqual((decimal)1.2, (decimal)(float)cmd.Parameters[3].Value);
      Assert.AreEqual("test", cmd.Parameters[4].Value);
      Assert.AreEqual(66, cmd.Parameters[5].Value);
    }

    [Test]
    public void InputOutputParameters()
    {
      // create our procedure
      ExecuteSQL("CREATE PROCEDURE spTest( INOUT strVal VARCHAR(50), INOUT numVal INT, OUT outVal INT UNSIGNED ) " +
          "BEGIN  SET strVal = CONCAT(strVal,'ending'); SET numVal=numVal * 2;  SET outVal=99; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?strVal", "beginning");
      cmd.Parameters.AddWithValue("?numVal", 33);
      cmd.Parameters.AddWithValue("?outVal", MySqlDbType.Int32);
      cmd.Parameters[0].Direction = ParameterDirection.InputOutput;
      cmd.Parameters[1].Direction = ParameterDirection.InputOutput;
      cmd.Parameters[2].Direction = ParameterDirection.Output;
      if (prepare) cmd.Prepare();
      int rowsAffected = cmd.ExecuteNonQuery();
      Assert.AreEqual(0, rowsAffected);
      Assert.AreEqual("beginningending", cmd.Parameters[0].Value);
      Assert.AreEqual(66, cmd.Parameters[1].Value);
      Assert.AreEqual(99, cmd.Parameters[2].Value);
    }

    [Test]
    public void ExecuteScalar()
    {
      // create our procedure
      ExecuteSQL("CREATE PROCEDURE spTest(IN valin VARCHAR(50), OUT valout VARCHAR(50) ) " +
          "BEGIN SET valout=valin; SELECT 'Test'; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?valin", "valuein");
      cmd.Parameters.Add(new MySqlParameter("?valout", MySqlDbType.VarChar));
      cmd.Parameters[1].Direction = ParameterDirection.Output;
      if (prepare) cmd.Prepare();
      object result = cmd.ExecuteScalar();
      Assert.AreEqual("Test", result);
      Assert.AreEqual("valuein", cmd.Parameters[1].Value);
    }

    [Test]
    public void ExecuteReaderTest()
    {
      // create our procedure
      ExecuteSQL("CREATE PROCEDURE spTest(OUT p INT) " +
          "BEGIN SELECT 1; SET p=2; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.Parameters.Add("?p", MySqlDbType.Int32);
      cmd.Parameters[0].Direction = ParameterDirection.Output;
      cmd.CommandType = CommandType.StoredProcedure;
      if (prepare) cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.True(reader.Read());
        Assert.False(reader.NextResult());
        Assert.False(reader.Read());
      }
      Assert.AreEqual(2, cmd.Parameters[0].Value);
    }

    [Test]
    public void FunctionNoParams()
    {
      ExecuteSQL("CREATE FUNCTION fnTest() RETURNS CHAR(50)" +
          " LANGUAGE SQL DETERMINISTIC BEGIN  RETURN \"Test\"; END");

      MySqlCommand cmd = new MySqlCommand("SELECT fnTest()", Connection);
      cmd.CommandType = CommandType.Text;
      if (prepare) cmd.Prepare();
      object result = cmd.ExecuteScalar();
      Assert.AreEqual("Test", result);
    }

    [Test]
    public void FunctionParams()
    {
      ExecuteSQL("CREATE FUNCTION fnTest( val1 INT, val2 CHAR(40) ) RETURNS INT " +
          " LANGUAGE SQL DETERMINISTIC BEGIN  RETURN val1 + LENGTH(val2);  END");

      MySqlCommand cmd = new MySqlCommand("SELECT fnTest(22, 'Test')", Connection);
      cmd.CommandType = CommandType.Text;
      if (prepare) cmd.Prepare();
      object result = cmd.ExecuteScalar();
      Assert.AreEqual(26, result);
    }

    /// <summary>
    /// Bug #10644 Cannot call a stored function directly from Connector/NET
    /// Bug #25013 Return Value parameter not found 
    /// </summary>
    [Test]
    public void CallingStoredFunctionasProcedure()
    {
      ExecuteSQL("CREATE FUNCTION fnTest(valin int) RETURNS INT " +
          " LANGUAGE SQL DETERMINISTIC BEGIN return valin * 2; END");
      MySqlCommand cmd = new MySqlCommand("fnTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?valin", 22);
      MySqlParameter retVal = cmd.CreateParameter();
      retVal.ParameterName = "?retval";
      retVal.MySqlDbType = MySqlDbType.Int32;
      retVal.Direction = ParameterDirection.ReturnValue;
      cmd.Parameters.Add(retVal);
      if (prepare) cmd.Prepare();
      cmd.ExecuteNonQuery();
      Assert.AreEqual(44, cmd.Parameters[1].Value);
    }

    [Test]
    public void ReturningEmptyResultSet()
    {
      ExecuteSQL("CREATE TABLE test1 (id int AUTO_INCREMENT NOT NULL, " +
           "Name VARCHAR(100) NOT NULL, PRIMARY KEY(id))");
      ExecuteSQL("CREATE TABLE test2 (id int AUTO_INCREMENT NOT NULL, " +
           "id1 INT NOT NULL, id2 INT NOT NULL, PRIMARY KEY(id))");

      ExecuteSQL("INSERT INTO test1 (Id, Name) VALUES (1, 'Item1')");
      ExecuteSQL("INSERT INTO test1 (Id, Name) VALUES (2, 'Item2')");
      ExecuteSQL("INSERT INTO test2 (Id, Id1, Id2) VALUES (1, 1, 1)");
      ExecuteSQL("INSERT INTO test2 (Id, Id1, Id2) VALUES (2, 2, 1)");

      ExecuteSQL("CREATE PROCEDURE spTest(Name VARCHAR(100), OUT Table1Id INT) " +
           "BEGIN SELECT t1.Id INTO Table1Id FROM test1 t1 WHERE t1.Name LIKE Name; " +
           "SELECT t3.Id2 FROM test2 t3 WHERE t3.Id1 = Table1Id; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?Name", "Item3");
      cmd.Parameters.Add("?Table1Id", MySqlDbType.Int32);
      cmd.Parameters["?Table1Id"].Direction = ParameterDirection.Output;

      DataSet ds = new DataSet();
      if (prepare) cmd.Prepare();
      MySqlDataAdapter da = new MySqlDataAdapter(cmd);
      try
      {
        da.Fill(ds);
      }
      catch (MySqlException)
      {
        // on 5.1 this throws an exception that no rows were returned.
      }
    }

    [Test]
    public void UnsignedOutputParameters()
    {
      ExecuteSQL("CREATE TABLE  Test (id INT(10) UNSIGNED AUTO_INCREMENT, PRIMARY KEY (id)) ");
      ExecuteSQL("CREATE PROCEDURE spTest (OUT id BIGINT UNSIGNED) " +
                "BEGIN INSERT INTO Test VALUES (NULL); SET id=LAST_INSERT_ID(); END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.Add("?id", MySqlDbType.UInt64);
      cmd.Parameters[0].Direction = ParameterDirection.Output;
      if (prepare) cmd.Prepare();
      cmd.ExecuteNonQuery();

      object o = cmd.Parameters[0].Value;
      Assert.True(o is ulong);
      Assert.AreEqual(1, Convert.ToInt32(o));
    }

    [Test]
    public void CallingFunctionWithoutReturnParameter()
    {
      ExecuteSQL("CREATE FUNCTION fnTest (p_kiosk bigint(20), " +
          "p_user bigint(20)) returns double begin declare v_return double; " +
          "set v_return = 3.6; return v_return; end");

      MySqlCommand cmd = new MySqlCommand("fnTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?p_kiosk", 2);
      cmd.Parameters.AddWithValue("?p_user", 4);
      Exception ex = Assert.Throws<InvalidOperationException>(() => { if (prepare) cmd.Prepare(); cmd.ExecuteNonQuery(); });
      Assert.AreEqual(ex.Message, "Attempt to call stored function 'fnTest' without specifying a return parameter");
    }

    /// <summary>
    /// Bug #27668 FillSchema and Stored Proc with an out parameter
    /// </summary>
    [Test]
    public void GetSchema2()
    {
      ExecuteSQL(@"CREATE TABLE Test(id INT AUTO_INCREMENT, PRIMARY KEY (id)) ");
      ExecuteSQL(@"CREATE PROCEDURE spTest (OUT id INT)
        BEGIN INSERT INTO Test VALUES (NULL); SET id=520; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.Add("?id", MySqlDbType.Int32);
      cmd.Parameters[0].Direction = ParameterDirection.Output;

      MySqlDataAdapter da = new MySqlDataAdapter(cmd);
      DataTable dt = new DataTable();
      if (prepare) cmd.Prepare();
      cmd.ExecuteNonQuery();
      da.Fill(dt);
      da.FillSchema(dt, SchemaType.Mapped);
    }

    [Test]
    public void BinaryAndVarBinaryParameters()
    {
      ExecuteSQL("CREATE PROCEDURE spTest(OUT out1 BINARY(20), OUT out2 VARBINARY(20)) " +
          "BEGIN SET out1 = 'out1'; SET out2='out2'; END");

      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.Add("out1", MySqlDbType.Binary);
      cmd.Parameters[0].Direction = ParameterDirection.Output;
      cmd.Parameters.Add("out2", MySqlDbType.VarBinary);
      cmd.Parameters[1].Direction = ParameterDirection.Output;
      if (prepare) cmd.Prepare();
      cmd.ExecuteNonQuery();

      byte[] out1 = (byte[])cmd.Parameters[0].Value;
      Assert.AreEqual('o', (char)out1[0]);
      Assert.AreEqual('u', (char)out1[1]);
      Assert.AreEqual('t', (char)out1[2]);
      Assert.AreEqual('1', (char)out1[3]);

      out1 = (byte[])cmd.Parameters[1].Value;
      Assert.AreEqual('o', (char)out1[0]);
      Assert.AreEqual('u', (char)out1[1]);
      Assert.AreEqual('t', (char)out1[2]);
      Assert.AreEqual('2', (char)out1[3]);
    }

    /// <summary>
    /// Bug #31930 Stored procedures with "ambiguous column name" error cause lock-ups 
    /// </summary>
    [Test]
    public void CallingFunction()
    {
      ExecuteSQL(@"CREATE FUNCTION `GetSupplierBalance`(SupplierID_ INTEGER(11))
                    RETURNS double NOT DETERMINISTIC CONTAINS SQL SQL SECURITY DEFINER
                    COMMENT '' 
                    BEGIN
                      RETURN 1.0;
                    END");

      MySqlCommand command = new MySqlCommand("GetSupplierBalance", Connection);
      command.CommandType = CommandType.StoredProcedure;
      command.Parameters.Add("?SupplierID_", MySqlDbType.Int32).Value = 1;
      command.Parameters.Add("?Balance", MySqlDbType.Double).Direction = ParameterDirection.ReturnValue;
      if (prepare) command.Prepare();
      command.ExecuteNonQuery();
      double balance = Convert.ToDouble(command.Parameters["?Balance"].Value);
      Assert.AreEqual(1.0, balance);
    }

    /// <summary>
    /// </summary>
    [Test]
    public void OutputParametersWithNewParamHandling()
    {
      // create our procedure
      ExecuteSQL("CREATE PROCEDURE spTest(out val1 VARCHAR(350)) " +
          "BEGIN  SET val1 = '42';  END");

      var connStr = Connection.ConnectionString.Replace("allow user variables=true", "allow user variables=false");
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("spTest", c);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new MySqlParameter("@val1", MySqlDbType.VarChar)).Direction = ParameterDirection.Output;
        if (prepare) cmd.Prepare();
        int rowsAffected = cmd.ExecuteNonQuery();

        Assert.AreEqual(0, rowsAffected);
        Assert.AreEqual("42", cmd.Parameters[0].Value);
      }
    }

    /// <summary>
    /// </summary>
    [Test]
    public void FunctionWithNewParamHandling()
    {
      // create our procedure
      ExecuteSQL("CREATE FUNCTION fnTest(`value` INT) RETURNS INT " +
          "BEGIN RETURN value; END");

      var connStr = Connection.ConnectionString.Replace("allow user variables=true", "allow user variables=false");
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("fnTest", c);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new MySqlParameter("@value", MySqlDbType.Int32)).Value = 22;
        cmd.Parameters.Add(new MySqlParameter("@returnvalue", MySqlDbType.Int32)).Direction = ParameterDirection.ReturnValue;
        if (prepare) cmd.Prepare();
        int rowsAffected = cmd.ExecuteNonQuery();

        Assert.AreEqual(0, rowsAffected);
        Assert.AreEqual(22, cmd.Parameters[1].Value);
      }
    }

    /// <summary>
    /// Bug #56756	Output Parameter MySqlDbType.Bit get a wrong Value (48/49 for false or true)
    /// </summary>
    [Test]
    public void BitTypeAsOutParameter()
    {
      ExecuteSQL(@"CREATE PROCEDURE spTest(out x bit(1))
                    BEGIN
                    Set x = 1; -- Outparameter value is 49
                    Set x = 0; -- Outparameter value is 48
                    END");
      MySqlCommand cmd = new MySqlCommand("spTest", Connection);
      cmd.Parameters.Clear();
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.Add("x", MySqlDbType.Bit).Direction = ParameterDirection.Output;
      if (prepare) cmd.Prepare();
      cmd.ExecuteNonQuery();
      Assert.AreEqual(0, Convert.ToInt32(cmd.Parameters[0].Value));
    }

    /// <summary>
    /// Bug #25625 Crashes when calling with CommandType set to StoredProcedure
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void RunWithoutSelectPrivsThrowException()
    {
      // we don't want this test to run in our all access fixture
      string connInfo = Connection.ConnectionString;
      if (connInfo.IndexOf("use procedure bodies=false") == -1)
        return;

      ExecuteSQL(String.Format(
          "GRANT ALL ON `{0}`.* to 'testuser'@'%' identified by 'testuser'",
          Connection.Database));
      ExecuteSQL(String.Format(
          "GRANT ALL ON `{0}`.* to 'testuser'@'{1}' identified by 'testuser'",
          Connection.Database, Host));
      ExecuteSQL("CREATE PROCEDURE spTest(id int, OUT outid int, INOUT inoutid int) " +
          "BEGIN SET outid=id+inoutid; SET inoutid=inoutid+id; END");

      var csb = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      csb.UserID = "testuser";
      csb.Password = "testuser";
      MySqlConnection c = new MySqlConnection(csb.ConnectionString);
      c.Open();

      try
      {
        MySqlCommand cmd = new MySqlCommand("spTest", c);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("?id", 2);
        cmd.Parameters.AddWithValue("?outid", MySqlDbType.Int32);
        cmd.Parameters[1].Direction = ParameterDirection.Output;
        cmd.Parameters.AddWithValue("?inoutid", 4);
        cmd.Parameters[2].Direction = ParameterDirection.InputOutput;
        if (prepare) cmd.Prepare();
        cmd.ExecuteNonQuery();

        Assert.AreEqual(6, cmd.Parameters[1].Value);
        Assert.AreEqual(6, cmd.Parameters[2].Value);
      }
      catch (InvalidOperationException iex)
      {
        StringAssert.StartsWith("Unable to retrieve", iex.Message);
      }
      finally
      {
        if (c != null)
          c.Close();
        ExecuteSQL("DELETE FROM mysql.user WHERE user = 'testuser'");
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void NoAccessToProcedureBodies()
    {
      string sql = String.Format("CREATE PROCEDURE `{0}`.`spTest`(in1 INT, INOUT inout1 INT, OUT out1 INT ) " +
          "BEGIN SET inout1 = inout1+2; SET out1=inout1-3; SELECT in1; END", (Connection.Database));
      ExecuteSQL(sql);

      using (MySqlConnection c = new MySqlConnection(Connection.ConnectionString + ";check parameters=false"))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("spTest", c);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("?in1", 2);
        cmd.Parameters.AddWithValue("?inout1", 4);
        cmd.Parameters.Add("?out1", MySqlDbType.Int32);
        cmd.Parameters[1].Direction = ParameterDirection.InputOutput;
        cmd.Parameters[2].Direction = ParameterDirection.Output;
        if (prepare) cmd.Prepare();
        cmd.ExecuteNonQuery();

        Assert.AreEqual(6, cmd.Parameters[1].Value);
        Assert.AreEqual(3, cmd.Parameters[2].Value);
      }
    }
  }
}
