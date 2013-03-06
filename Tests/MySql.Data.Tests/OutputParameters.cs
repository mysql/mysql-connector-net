// Copyright © 2011, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System.Globalization;
using System.Threading;
using MySql.Data.Types;
using System.Data.Common;

namespace MySql.Data.MySqlClient.Tests
{
  /// <summary>
  /// Summary description for StoredProcedure.
  /// </summary>
  [TestFixture]
  public class OutputParametersBatch : BaseTest
  {
    protected bool prepare;

    protected override string GetConnectionInfo()
    {
      return string.Format("allow batch=true; ignore prepare = false; port={0}", this.port);
    }

    /// <summary>
    /// Bug #17814 Stored procedure fails unless DbType set explicitly
    /// Bug #23749 VarChar field size over 255 causes a System.OverflowException 
    /// </summary>
    [Test]
    public void OutputParameters()
    {
      if (Version < new Version(5, 0)) return;

      // we don't want to run this test under no access
      string connInfo = GetConnectionInfo();
      if (connInfo.IndexOf("use procedure bodies=false") != -1) return;

      // create our procedure
      execSQL("CREATE PROCEDURE spTest(out value VARCHAR(350), OUT intVal INT, " +
          "OUT dateVal TIMESTAMP, OUT floatVal FLOAT, OUT noTypeVarChar VARCHAR(20), " +
          "OUT noTypeInt INT) " +
          "BEGIN  SET value='42';  SET intVal=33; SET dateVal='2004-06-05 07:58:09'; " +
          "SET floatVal = 1.2; SET noTypeVarChar='test'; SET noTypeInt=66; END");

      // we use rootConn here since we are using parameters
      MySqlCommand cmd = new MySqlCommand("spTest", conn);
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

      //TODO fix this
      //Assert.AreEqual(0, rowsAffected);
      Assert.AreEqual("42", cmd.Parameters[0].Value);
      Assert.AreEqual(33, cmd.Parameters[1].Value);
      Assert.AreEqual(new DateTime(2004, 6, 5, 7, 58, 9),
               Convert.ToDateTime(cmd.Parameters[2].Value));
      Assert.AreEqual(1.2, (decimal)(float)cmd.Parameters[3].Value);
      Assert.AreEqual("test", cmd.Parameters[4].Value);
      Assert.AreEqual(66, cmd.Parameters[5].Value);
    }

    [Test]
    public void InputOutputParameters()
    {
      if (Version < new Version(5, 0)) return;

      // create our procedure
      execSQL("CREATE PROCEDURE spTest( INOUT strVal VARCHAR(50), INOUT numVal INT, OUT outVal INT UNSIGNED ) " +
          "BEGIN  SET strVal = CONCAT(strVal,'ending'); SET numVal=numVal * 2;  SET outVal=99; END");

      MySqlCommand cmd = new MySqlCommand("spTest", conn);
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
      if (Version < new Version(5, 0)) return;

      // create our procedure
      execSQL("CREATE PROCEDURE spTest( IN valin VARCHAR(50), OUT valout VARCHAR(50) ) " +
          "BEGIN  SET valout=valin;  SELECT 'Test'; END");

      MySqlCommand cmd = new MySqlCommand("spTest", conn);
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
    public void ExecuteReader()
    {
      if (Version < new Version(5, 0)) return;

      // create our procedure
      execSQL("CREATE PROCEDURE spTest(OUT p INT) " +
          "BEGIN SELECT 1; SET p=2; END");

      MySqlCommand cmd = new MySqlCommand("spTest", conn);
      cmd.Parameters.Add("?p", MySqlDbType.Int32);
      cmd.Parameters[0].Direction = ParameterDirection.Output;
      cmd.CommandType = CommandType.StoredProcedure;
      if (prepare) cmd.Prepare();
      using (MySqlDataReader reader = cmd.ExecuteReader())
      {
        Assert.AreEqual(true, reader.Read());
        Assert.AreEqual(false, reader.NextResult());
        Assert.AreEqual(false, reader.Read());
      }
      Assert.AreEqual(2, cmd.Parameters[0].Value);
    }

    [Test]
    public void FunctionNoParams()
    {
      if (Version < new Version(5, 0)) return;

      execSQL("CREATE FUNCTION fnTest() RETURNS CHAR(50)" +
          " LANGUAGE SQL DETERMINISTIC BEGIN  RETURN \"Test\"; END");

      MySqlCommand cmd = new MySqlCommand("SELECT fnTest()", conn);
      cmd.CommandType = CommandType.Text;
      if (prepare) cmd.Prepare();
      object result = cmd.ExecuteScalar();
      Assert.AreEqual("Test", result);
    }

    [Test]
    public void FunctionParams()
    {
      if (Version < new Version(5, 0)) return;

      execSQL("CREATE FUNCTION fnTest( val1 INT, val2 CHAR(40) ) RETURNS INT " +
          " LANGUAGE SQL DETERMINISTIC BEGIN  RETURN val1 + LENGTH(val2);  END");

      MySqlCommand cmd = new MySqlCommand("SELECT fnTest(22, 'Test')", conn);
      cmd.CommandType = CommandType.Text;
      if (prepare) cmd.Prepare();
      object result = cmd.ExecuteScalar();
      Assert.AreEqual(26, result);
    }

    /// <summary>
    /// Bug #10644 Cannot call a stored function directly from Connector/Net 
    /// Bug #25013 Return Value parameter not found 
    /// </summary>
    [Test]
    public void CallingStoredFunctionasProcedure()
    {
      if (Version < new Version(5, 0)) return;

      execSQL("CREATE FUNCTION fnTest(valin int) RETURNS INT " +
          " LANGUAGE SQL DETERMINISTIC BEGIN return valin * 2; END");
      MySqlCommand cmd = new MySqlCommand("fnTest", conn);
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
      if (Version < new Version(5, 0)) return;

      execSQL("CREATE TABLE test1 (id int AUTO_INCREMENT NOT NULL, " +
           "Name VARCHAR(100) NOT NULL, PRIMARY KEY(id))");
      execSQL("CREATE TABLE test2 (id int AUTO_INCREMENT NOT NULL, " +
           "id1 INT NOT NULL, id2 INT NOT NULL, PRIMARY KEY(id))");

      execSQL("INSERT INTO test1 (Id, Name) VALUES (1, 'Item1')");
      execSQL("INSERT INTO test1 (Id, Name) VALUES (2, 'Item2')");
      execSQL("INSERT INTO test2 (Id, Id1, Id2) VALUES (1, 1, 1)");
      execSQL("INSERT INTO test2 (Id, Id1, Id2) VALUES (2, 2, 1)");

      execSQL("CREATE PROCEDURE spTest(Name VARCHAR(100), OUT Table1Id INT) " +
           "BEGIN SELECT t1.Id INTO Table1Id FROM test1 t1 WHERE t1.Name LIKE Name; " +
           "SELECT t3.Id2 FROM test2 t3 WHERE t3.Id1 = Table1Id; END");

      MySqlCommand cmd = conn.CreateCommand();
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.CommandText = "spTest";
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
      if (Version < new Version(5, 0)) return;

      execSQL("CREATE TABLE  Test (id INT(10) UNSIGNED AUTO_INCREMENT, PRIMARY KEY (id)) ");
      execSQL("CREATE PROCEDURE spTest (OUT id BIGINT UNSIGNED) " +
                "BEGIN INSERT INTO Test VALUES (NULL); SET id=LAST_INSERT_ID(); END");

      MySqlCommand cmd = new MySqlCommand("spTest", conn);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.Add("?id", MySqlDbType.UInt64);
      cmd.Parameters[0].Direction = ParameterDirection.Output;
      if (prepare) cmd.Prepare();
      cmd.ExecuteNonQuery();

      object o = cmd.Parameters[0].Value;
      Assert.IsTrue(o is ulong);
      Assert.AreEqual(1, o);
    }

    /// <summary>
    /// Bug #25625 Crashes when calling with CommandType set to StoredProcedure 
    /// </summary>
    [Test]
    public void RunWithoutSelectPrivsThrowException()
    {
      if (Version < new Version(5, 0)) return;

      // we don't want this test to run in our all access fixture
      string connInfo = GetConnectionInfo();
      if (connInfo.IndexOf("use procedure bodies=false") == -1)
        return;

      suExecSQL(String.Format(
          "GRANT ALL ON `{0}`.* to 'testuser'@'%' identified by 'testuser'",
          database0));
      suExecSQL(String.Format(
          "GRANT ALL ON `{0}`.* to 'testuser'@'localhost' identified by 'testuser'",
          database0));

      execSQL("DROP PROCEDURE IF EXISTS spTest");
      execSQL("CREATE PROCEDURE spTest(id int, OUT outid int, INOUT inoutid int) " +
          "BEGIN SET outid=id+inoutid; SET inoutid=inoutid+id; END");

      string s = GetConnectionString("testuser", "testuser", true);
      MySqlConnection c = new MySqlConnection(s);
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
        Assert.IsTrue(iex.Message.StartsWith("Unable to retrieve", StringComparison.Ordinal));
      }
      finally
      {
        if (c != null)
          c.Close();
        suExecSQL("DELETE FROM mysql.user WHERE user = 'testuser'");
      }
    }

    [Test]
    public void CallingFunctionWithoutReturnParameter()
    {
      if (Version < new Version(5, 0)) return;

      execSQL("CREATE FUNCTION fnTest (p_kiosk bigint(20), " +
          "p_user bigint(20)) returns double begin declare v_return double; " +
          "set v_return = 3.6; return v_return; end");

      MySqlCommand cmd = new MySqlCommand("fnTest", conn);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.AddWithValue("?p_kiosk", 2);
      cmd.Parameters.AddWithValue("?p_user", 4);
      try
      {
        if (prepare) cmd.Prepare();
        cmd.ExecuteNonQuery();
        Assert.Fail("This should fail");
      }
      catch (InvalidOperationException)
      {
      }
    }

    /// <summary>
    /// Bug #27668 FillSchema and Stored Proc with an out parameter
    /// </summary>
    [Test]
    public void GetSchema2()
    {
      if (Version.Major < 5) return;

      execSQL(@"CREATE TABLE Test(id INT AUTO_INCREMENT, PRIMARY KEY (id)) ");
      execSQL(@"CREATE PROCEDURE spTest (OUT id INT)
        BEGIN INSERT INTO Test VALUES (NULL); SET id=520; END");

      MySqlCommand cmd = new MySqlCommand("spTest", conn);
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
    public void NoAccessToProcedureBodies()
    {
      if (Version < new Version(5, 0)) return;

      string sql = String.Format("CREATE PROCEDURE `{0}`.`spTest`(in1 INT, INOUT inout1 INT, OUT out1 INT ) " +
          "BEGIN SET inout1 = inout1+2; SET out1=inout1-3; SELECT in1; END", database0);
      execSQL(sql);

      string connStr = GetConnectionString(true) + "; use procedure bodies=false";
      using (MySqlConnection c = new MySqlConnection(connStr))
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

    [Test]
    public void BinaryAndVarBinaryParameters()
    {
      if (Version < new Version(5, 0)) return;

      execSQL("CREATE PROCEDURE spTest(OUT out1 BINARY(20), OUT out2 VARBINARY(20)) " +
          "BEGIN SET out1 = 'out1'; SET out2='out2'; END");

      MySqlCommand cmd = new MySqlCommand("spTest", conn);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.Add("out1", MySqlDbType.Binary);
      cmd.Parameters[0].Direction = ParameterDirection.Output;
      cmd.Parameters.Add("out2", MySqlDbType.VarBinary);
      cmd.Parameters[1].Direction = ParameterDirection.Output;
      if (prepare) cmd.Prepare();
      cmd.ExecuteNonQuery();

      byte[] out1 = (byte[])cmd.Parameters[0].Value;
      Assert.AreEqual('o', out1[0]);
      Assert.AreEqual('u', out1[1]);
      Assert.AreEqual('t', out1[2]);
      Assert.AreEqual('1', out1[3]);

      out1 = (byte[])cmd.Parameters[1].Value;
      Assert.AreEqual('o', out1[0]);
      Assert.AreEqual('u', out1[1]);
      Assert.AreEqual('t', out1[2]);
      Assert.AreEqual('2', out1[3]);
    }

    /// <summary>
    /// Bug #31930 Stored procedures with "ambiguous column name" error cause lock-ups 
    /// </summary>
    [Test]
    public void CallingFunction()
    {
      if (Version < new Version(5, 0)) return;

      execSQL(@"CREATE FUNCTION `GetSupplierBalance`(SupplierID_ INTEGER(11))
        RETURNS double NOT DETERMINISTIC CONTAINS SQL SQL SECURITY DEFINER
        COMMENT '' 
        BEGIN
          RETURN 1.0;
        END");

      MySqlCommand command = new MySqlCommand("GetSupplierBalance", conn);
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
      if (Version < new Version(5, 0)) return;

      // create our procedure
      execSQL("CREATE PROCEDURE spTest(out val1 VARCHAR(350)) " +
          "BEGIN  SET val1 = '42';  END");

      string connStr = GetConnectionString(true);
      connStr = connStr.Replace("allow user variables=true", "allow user variables=false");
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
      if (Version < new Version(5, 0)) return;

      // create our procedure
      execSQL("CREATE FUNCTION spTest(`value` INT) RETURNS INT " +
          "BEGIN RETURN value; END");

      string connStr = GetConnectionString(true);
      connStr = connStr.Replace("allow user variables=true", "allow user variables=false");
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();

        MySqlCommand cmd = new MySqlCommand("spTest", c);
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
      execSQL(@"CREATE PROCEDURE `spTest`(out x bit(1))
        BEGIN
        Set x = 1; -- Outparameter value is 49
        Set x = 0; -- Outparameter value is 48
        END");
      MySqlCommand cmd = new MySqlCommand("spTest", conn);
      cmd.CommandType = CommandType.StoredProcedure;
      cmd.Parameters.Add("x", MySqlDbType.Bit).Direction = ParameterDirection.Output;
      if (prepare) cmd.Prepare();
      cmd.ExecuteNonQuery();
      Assert.AreEqual(0, cmd.Parameters[0].Value);
    }
  }

  #region Configs

  public class OutputParametersNoBatch : OutputParametersBatch
  {
    protected override string GetConnectionInfo()
    {
      return string.Format("allow batch=false; port={0}", this.port);
    }
  }

  public class OutputParametersBatchPrepared : OutputParametersBatch
  {
    protected override string GetConnectionInfo()
    {
      prepare = true;
      return string.Format("allow batch=true; ignore prepare=false; port={0}", this.port);
    }
  }

  public class OutputParametersNoBatchPrepared : OutputParametersBatch
  {
    protected override string GetConnectionInfo()
    {
      prepare = true;
      return string.Format("allow batch=false; ignore prepare=false; port={0}", this.port);
    }
  }

  #endregion
}
