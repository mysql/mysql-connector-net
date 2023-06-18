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
  public class ProcedureParameterTests : TestBase
  {
    [Test]
    public void ProcedureParameters()
    {
      ExecuteSQL("CREATE PROCEDURE ProcedureParameters (id int, name varchar(50)) BEGIN SELECT 1; END");

      string[] restrictions = new string[5];
      restrictions[1] = Connection.Database;
      restrictions[2] = "ProcedureParameters";
      DataTable dt = Connection.GetSchema("Procedure Parameters", restrictions);
      Assert.True(dt.Rows.Count == 2, "Actual result " + dt.Rows.Count);
      Assert.AreEqual("Procedure Parameters", dt.TableName);
      Assert.AreEqual(Connection.Database, dt.Rows[0]["SPECIFIC_SCHEMA"].ToString());
      Assert.AreEqual("ProcedureParameters", dt.Rows[0]["SPECIFIC_NAME"].ToString());
      Assert.AreEqual("id", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[0]["PARAMETER_MODE"]);

      restrictions[4] = "name";
      dt.Clear();
      dt = Connection.GetSchema("Procedure Parameters", restrictions);
      Assert.AreEqual(1, dt.Rows.Count);
      Assert.AreEqual(Connection.Database, dt.Rows[0]["SPECIFIC_SCHEMA"].ToString());
      Assert.AreEqual("ProcedureParameters", dt.Rows[0]["SPECIFIC_NAME"].ToString());
      Assert.AreEqual("name", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(2, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[0]["PARAMETER_MODE"]);

      ExecuteSQL("DROP FUNCTION IF EXISTS spFunc");
      ExecuteSQL("CREATE FUNCTION spFunc (id int) RETURNS INT BEGIN RETURN 1; END");

      restrictions[4] = null;
      restrictions[1] = Connection.Database;
      restrictions[2] = "spFunc";
      dt = Connection.GetSchema("Procedure Parameters", restrictions);
      Assert.True(dt.Rows.Count == 2);
      Assert.AreEqual("Procedure Parameters", dt.TableName);
      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("spfunc", dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual(0, dt.Rows[0]["ORDINAL_POSITION"]);

      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[1]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("spfunc", dt.Rows[1]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("id", dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(1, dt.Rows[1]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[1]["PARAMETER_MODE"]);
    }

    /// <summary>
    /// Bug #6902 Errors in parsing stored procedure parameters 
    /// </summary>
    [Test]
    public void ProcedureParameters2()
    {
      ExecuteSQL("DROP PROCEDURE IF EXISTS spTest");
      ExecuteSQL(@"CREATE PROCEDURE spTest(`/*id*/` /* before type 1 */ varchar(20) character set latin1, 
           /* after type 1 */ OUT result2 DECIMAL(/*size1*/10,/*size2*/2) /* p2 */) 
           BEGIN SELECT action, result; END");

      string[] restrictions = new string[5];
      restrictions[1] = Connection.Database;
      restrictions[2] = "spTest";
      DataTable dt = Connection.GetSchema("Procedure Parameters", restrictions);

      Assert.True(dt.Rows.Count == 2, "Actual result " + dt.Rows.Count);
      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("/*id*/", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[0]["PARAMETER_MODE"]);
      Assert.AreEqual("VARCHAR", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual(20, dt.Rows[0]["CHARACTER_MAXIMUM_LENGTH"]);
      Assert.AreEqual(20, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);

      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[1]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[1]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("result2", dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(2, dt.Rows[1]["ORDINAL_POSITION"]);
      Assert.AreEqual("OUT", dt.Rows[1]["PARAMETER_MODE"]);
      Assert.AreEqual("DECIMAL", dt.Rows[1]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual(10, Convert.ToInt32(dt.Rows[1]["NUMERIC_PRECISION"]));
      Assert.AreEqual(2, dt.Rows[1]["NUMERIC_SCALE"]);
    }

    [Test]
    public void ProcedureParameters3()
    {
      ExecuteSQL("DROP PROCEDURE IF EXISTS spTest");
      ExecuteSQL(@"CREATE  PROCEDURE spTest (_ACTION varchar(20) character set latin1,
          `/*dumb-identifier-1*/` int, `#dumb-identifier-2` int,
          `--dumb-identifier-3` int, 
          _CLIENT_ID int, -- ABC
          _LOGIN_ID  int, # DEF
          _WHERE varchar(2000) character set latin1, 
          _SORT varchar(2000) character set utf8mb4,
          out _SQL varchar(/* inline right here - oh my gosh! */ 8000) character set latin1,
          _SONG_ID int,
          _NOTES varchar(2000) character set latin1,
          out _RESULT varchar(10) character set latin1
          /*
          ,    -- Generic result parameter
          out _PERIOD_ID int,         -- Returns the period_id. Useful when using @PREDEFLINK to return which is the last period
          _SONGS_LIST varchar(8000) character set latin1,
          _COMPOSERID int,
          _PUBLISHERID int,
          _PREDEFLINK int        -- If the user is accessing through a predefined link: 0=none  1=last period
          */) BEGIN SELECT 1; END");

      string[] restrictions = new string[5];
      restrictions[1] = Connection.Database;
      restrictions[2] = "spTest";
      DataTable dt = Connection.GetSchema("Procedure Parameters", restrictions);

      Assert.True(dt.Rows.Count == 12, "Rows count failed");
      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("_action", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[0]["PARAMETER_MODE"]);
      Assert.AreEqual("VARCHAR", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual(20, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);

      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[1]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[1]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("/*dumb-identifier-1*/", dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(2, dt.Rows[1]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[1]["PARAMETER_MODE"]);
      Assert.AreEqual("INT", dt.Rows[1]["DATA_TYPE"].ToString().ToUpper());

      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[2]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[2]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("#dumb-identifier-2", dt.Rows[2]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(3, dt.Rows[2]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[2]["PARAMETER_MODE"]);
      Assert.AreEqual("INT", dt.Rows[2]["DATA_TYPE"].ToString().ToUpper());

      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[3]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[3]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("--dumb-identifier-3", dt.Rows[3]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(4, dt.Rows[3]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[3]["PARAMETER_MODE"]);
      Assert.AreEqual("INT", dt.Rows[3]["DATA_TYPE"].ToString().ToUpper());

      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[4]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[4]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("_client_id", dt.Rows[4]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(5, dt.Rows[4]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[4]["PARAMETER_MODE"]);
      Assert.AreEqual("INT", dt.Rows[4]["DATA_TYPE"].ToString().ToUpper());

      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[5]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[5]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("_login_id", dt.Rows[5]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(6, dt.Rows[5]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[5]["PARAMETER_MODE"]);
      Assert.AreEqual("INT", dt.Rows[5]["DATA_TYPE"].ToString().ToUpper());

      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[6]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[6]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("_where", dt.Rows[6]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(7, dt.Rows[6]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[6]["PARAMETER_MODE"]);
      Assert.AreEqual("VARCHAR", dt.Rows[6]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual(2000, dt.Rows[6]["CHARACTER_OCTET_LENGTH"]);

      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[7]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[7]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("_sort", dt.Rows[7]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(8, dt.Rows[7]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[7]["PARAMETER_MODE"]);
      Assert.AreEqual("VARCHAR", dt.Rows[7]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual(8000, dt.Rows[7]["CHARACTER_OCTET_LENGTH"]);

      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[8]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[8]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("_sql", dt.Rows[8]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(9, dt.Rows[8]["ORDINAL_POSITION"]);
      Assert.AreEqual("OUT", dt.Rows[8]["PARAMETER_MODE"]);
      Assert.AreEqual("VARCHAR", dt.Rows[8]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual(8000, dt.Rows[8]["CHARACTER_OCTET_LENGTH"]);

      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[9]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[9]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("_song_id", dt.Rows[9]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(10, dt.Rows[9]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[9]["PARAMETER_MODE"]);
      Assert.AreEqual("INT", dt.Rows[9]["DATA_TYPE"].ToString().ToUpper());

      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[10]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[10]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("_notes", dt.Rows[10]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(11, dt.Rows[10]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[10]["PARAMETER_MODE"]);
      Assert.AreEqual("VARCHAR", dt.Rows[10]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual(2000, dt.Rows[10]["CHARACTER_OCTET_LENGTH"]);

      Assert.AreEqual(Connection.Database.ToLower(), dt.Rows[11]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.AreEqual("sptest", dt.Rows[11]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.AreEqual("_result", dt.Rows[11]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(12, dt.Rows[11]["ORDINAL_POSITION"]);
      Assert.AreEqual("OUT", dt.Rows[11]["PARAMETER_MODE"]);
      Assert.AreEqual("VARCHAR", dt.Rows[11]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual(10, dt.Rows[11]["CHARACTER_OCTET_LENGTH"]);
    }

    [Test]
    public void ProcedureParameters4()
    {
      string charset = Version < new Version(8, 0) ? "utf8" : "utf8mb3";

      ExecuteSQL($@"CREATE PROCEDURE ProcedureParameters4 (name VARCHAR(1200) 
          CHARACTER /* hello*/ SET {charset}) BEGIN SELECT name; END");

      string[] restrictions = new string[5];
      restrictions[1] = Connection.Database;
      restrictions[2] = "ProcedureParameters4";
      DataTable dt = Connection.GetSchema("Procedure Parameters", restrictions);

      Assert.True(dt.Rows.Count == 1, "Actual Result " + dt.Rows.Count);
      Assert.AreEqual(Connection.Database, dt.Rows[0]["SPECIFIC_SCHEMA"].ToString());
      Assert.AreEqual("ProcedureParameters4", dt.Rows[0]["SPECIFIC_NAME"].ToString());
      Assert.AreEqual("name", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[0]["PARAMETER_MODE"]);
      Assert.AreEqual("VARCHAR", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual(1200, dt.Rows[0]["CHARACTER_MAXIMUM_LENGTH"]);
      Assert.AreEqual(3600, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);
      Assert.AreEqual(charset, dt.Rows[0]["CHARACTER_SET_NAME"]);
      Assert.AreEqual($"{charset}_general_ci", dt.Rows[0]["COLLATION_NAME"]);
    }

    [Test]
    public void ProcedureParameters5()
    {
      ExecuteSQL(@"CREATE  PROCEDURE ProcedureParameters5 (name VARCHAR(1200) ASCII BINARY, 
          name2 TEXT UNICODE) BEGIN SELECT name; END");

      string[] restrictions = new string[5];
      restrictions[1] = Connection.Database;
      restrictions[2] = "ProcedureParameters5";
      DataTable dt = Connection.GetSchema("Procedure Parameters", restrictions);

      Assert.True(dt.Rows.Count == 2);
      Assert.AreEqual(Connection.Database, dt.Rows[0]["SPECIFIC_SCHEMA"].ToString());
      Assert.AreEqual("ProcedureParameters5", dt.Rows[0]["SPECIFIC_NAME"].ToString());
      Assert.AreEqual("name", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[0]["PARAMETER_MODE"]);
      Assert.AreEqual("VARCHAR", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual("latin1", dt.Rows[0]["CHARACTER_SET_NAME"]);
      Assert.AreEqual(1200, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);

      Assert.AreEqual(Connection.Database, dt.Rows[1]["SPECIFIC_SCHEMA"].ToString());
      Assert.AreEqual("ProcedureParameters5", dt.Rows[1]["SPECIFIC_NAME"].ToString());
      Assert.AreEqual("name2", dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower());
      Assert.AreEqual(2, dt.Rows[1]["ORDINAL_POSITION"]);
      Assert.AreEqual("IN", dt.Rows[1]["PARAMETER_MODE"]);
      Assert.AreEqual("TEXT", dt.Rows[1]["DATA_TYPE"].ToString().ToUpper());
      Assert.AreEqual("ucs2", dt.Rows[1]["CHARACTER_SET_NAME"]);
    }

    [Test]
    public void DTD_Identifier()
    {
      using (var conn = new MySqlConnection(Connection.ConnectionString))
      {
        conn.Open();
        var cmd = new MySqlCommand(@"CREATE PROCEDURE DTD_Identifier (id INT UNSIGNED ZEROFILL,
          dec1 DECIMAL(10,2), 
          name VARCHAR(20) /* this is a comment */ CHARACTER SET ascii,
          t1 TINYTEXT BINARY, t2 ENUM('a','b','c'),
          t3 /* comment */ SET(/* comment */'1','2','3'))
          BEGIN SELECT name; END", conn);
        cmd.ExecuteNonQuery();

        string[] restrictions = new string[5];
        restrictions[1] = Connection.Database;
        restrictions[2] = "DTD_Identifier";
        DataTable dt = Connection.GetSchema("Procedure Parameters", restrictions);

        Assert.True(dt.Rows.Count == 6, "Actual Result " + dt.Rows.Count);
        Assert.AreEqual("INT(10) UNSIGNED ZEROFILL",
          dt.Rows[0]["DTD_IDENTIFIER"].ToString().ToUpper());
        Assert.AreEqual("DECIMAL(10,2)",
          dt.Rows[1]["DTD_IDENTIFIER"].ToString().ToUpper());
        Assert.AreEqual("VARCHAR(20)",
          dt.Rows[2]["DTD_IDENTIFIER"].ToString().ToUpper());
        Assert.AreEqual("TINYTEXT",
          dt.Rows[3]["DTD_IDENTIFIER"].ToString().ToUpper());
        Assert.AreEqual("ENUM('A','B','C')",
          dt.Rows[4]["DTD_IDENTIFIER"].ToString().ToUpper());
        Assert.AreEqual("SET('1','2','3')",
          dt.Rows[5]["DTD_IDENTIFIER"].ToString().ToUpper());
        conn.Close();
      }
    }

    /// <summary>
    /// Bug #48586	Expose defined possible enum values
    /// </summary>
    [Test]
    public void PossibleValues()
    {
      var builder = new MySqlConnectionStringBuilder(Connection.ConnectionString);
      using (var conn = new MySqlConnection(builder.ConnectionString))
      {
        conn.Open();
        var cmd = new MySqlCommand(@"CREATE PROCEDURE PossibleValues (id INT UNSIGNED ZEROFILL,
          dec1 DECIMAL(10,2), 
          name VARCHAR(20) /* this is a comment */ CHARACTER SET ascii,
          t1 TINYTEXT BINARY, t2 ENUM('a','b','c'),
          t3 /* comment */ SET(/* comment */'1','2','3'))
          BEGIN SELECT name; END", conn);
        cmd.ExecuteNonQuery();
        cmd.CommandText = "PossibleValues";
        cmd.CommandType = CommandType.StoredProcedure;
        MySqlCommandBuilder.DeriveParameters(cmd);
        Assert.Null(cmd.Parameters["@id"].PossibleValues);
        Assert.Null(cmd.Parameters["@dec1"].PossibleValues);
        Assert.Null(cmd.Parameters["@name"].PossibleValues);
        Assert.Null(cmd.Parameters["@t1"].PossibleValues);
        MySqlParameter t2 = cmd.Parameters["@t2"];
        Assert.NotNull(t2.PossibleValues);
        Assert.AreEqual("a", t2.PossibleValues[0]);
        Assert.AreEqual("b", t2.PossibleValues[1]);
        Assert.AreEqual("c", t2.PossibleValues[2]);
        MySqlParameter t3 = cmd.Parameters["@t3"];
        Assert.NotNull(t3.PossibleValues);
        Assert.AreEqual("1", t3.PossibleValues[0]);
        Assert.AreEqual("2", t3.PossibleValues[1]);
        Assert.AreEqual("3", t3.PossibleValues[2]);
        conn.Close();
      }

      // Default to the current database when it isn't specified. 
      var dbName = builder.Database;
      builder.Database = null;
      using (var conn = new MySqlConnection(builder.ConnectionString))
      {
        conn.Open();
        var cmd = new MySqlCommand($"USE `{dbName}`", conn);
        cmd.ExecuteNonQuery();
        cmd.CommandText = "PossibleValues";
        cmd.CommandType = CommandType.StoredProcedure;
        MySqlCommandBuilder.DeriveParameters(cmd);
        Assert.Null(cmd.Parameters["@id"].PossibleValues);
        Assert.Null(cmd.Parameters["@dec1"].PossibleValues);
        Assert.Null(cmd.Parameters["@name"].PossibleValues);
        Assert.Null(cmd.Parameters["@t1"].PossibleValues);
        MySqlParameter t2 = cmd.Parameters["@t2"];
        Assert.NotNull(t2.PossibleValues);
        Assert.AreEqual("a", t2.PossibleValues[0]);
        Assert.AreEqual("b", t2.PossibleValues[1]);
        Assert.AreEqual("c", t2.PossibleValues[2]);
        MySqlParameter t3 = cmd.Parameters["@t3"];
        Assert.NotNull(t3.PossibleValues);
        Assert.AreEqual("1", t3.PossibleValues[0]);
        Assert.AreEqual("2", t3.PossibleValues[1]);
        Assert.AreEqual("3", t3.PossibleValues[2]);
        conn.Close();
      }
    }

    /// <summary>
    /// Bug #62416	IndexOutOfRangeException when using return parameter with no name
    /// </summary>
    [Test]
    public void UnnamedReturnValue()
    {
      ExecuteSQL("DROP FUNCTION IF EXISTS spFunc");
      ExecuteSQL("CREATE FUNCTION spFunc() RETURNS DATETIME BEGIN RETURN NOW(); END");
      MySqlCommand cmd = new MySqlCommand("spFunc", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      MySqlParameter p1 = new MySqlParameter("", MySqlDbType.DateTime);
      p1.Direction = ParameterDirection.ReturnValue;
      cmd.Parameters.Add(p1);
      cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Bug #30029732	ERROR EXECUTING STORED ROUTINES
    /// </summary>
    [TestCase(true)]
    [TestCase(false)]
    public void StoredProcedureWithDifferentUser(bool hasPrivileges)
    {
      //Create Required Objects for all the tests
      string host = Host == "localhost" ? Host : "%";
      string sql = $"use `{Connection.Settings.Database}`; " + $@"CREATE TABLE IF NOT EXISTS hello (
             id int(11) NOT NULL AUTO_INCREMENT,
             string varchar(255) DEFAULT NULL,
             PRIMARY KEY (id)
             ) ENGINE = INNODB;

             CREATE PROCEDURE get_hello(IN p_id int)
             SQL SECURITY INVOKER
             BEGIN
               SELECT * FROM hello
               WHERE id = p_id;
             END;

             INSERT INTO hello (string) VALUES ('Hello World!');
             CREATE USER 'atest'@'{host}' IDENTIFIED BY 'pwd';

             GRANT SELECT ON table hello TO 'atest'@'{host}';

             GRANT EXECUTE ON procedure get_hello TO 'atest'@'{host}';

             CREATE PROCEDURE get_hello2(IN p_id int)
             SQL SECURITY INVOKER
             BEGIN
               SELECT * FROM hello
               WHERE id = p_id;
             END;
             ";

      ExecuteSQL(sql, true);

      Connection.Settings.UserID = hasPrivileges ? Connection.Settings.UserID : "atest";

      using (MySqlConnection c1 = new MySqlConnection(Connection.ConnectionString))
      {
        c1.Open();
        //Test with a user different than root and Granted to execute
        //Test with root and Granted to execute
        MySqlCommand cmd = new MySqlCommand();
        cmd.Connection = c1;
        cmd.CommandText = "get_hello";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_id", 1);
        MySqlDataAdapter da = new MySqlDataAdapter();
        da.SelectCommand = cmd;
        DataSet ds = new DataSet();
        da.Fill(ds);
        Assert.True(ds.Tables.Count > 0);

        //Test with not existing procedure
        cmd.CommandText = "get_hello3";
        da.SelectCommand = cmd;
        Assert.Throws<MySqlException>(() => da.Fill(ds));

        //Test with a user different than root and Not Granted to execute
        cmd.CommandText = "get_hello2";
        da.SelectCommand = cmd;
        if (hasPrivileges)
        {
          da.Fill(ds);
          Assert.True(ds.Tables.Count > 0);
        }
        else
        {
          Assert.Throws<MySqlException>(() => da.Fill(ds));
        }
      }

      sql = $"use `{Connection.Settings.Database}`; " + $@"drop procedure get_hello;
        drop procedure get_hello2;
        drop user 'atest'@'{host}';";
      ExecuteSQL(sql, true);
    }

    /// <summary>
    /// Bug #30444429 GETSCHEMATABLE RETURNS UNEXPECTED TABLE WHEN SPROC HAS OUTPUT PARAMETERS
    /// </summary>
    [Test]
    public void OutputParameterAndResultset()
    {
      using (var connection = new MySqlConnection(Connection.ConnectionString))
      {
        connection.Open();
        //Stored procedure with Output parameter + ResultSet
        ExecuteSQL(@"CREATE PROCEDURE out_int(
                     OUT value INT
                    )
                    BEGIN
                     SELECT 1 INTO value;
                     select value+1;
                    END;");
        using (var cmd = connection.CreateCommand())
        {
          cmd.CommandText = "out_int";
          cmd.CommandType = CommandType.StoredProcedure;
          MySqlParameter _outputParam = cmd.Parameters.Add(new MySqlParameter
          {
            ParameterName = "@value",
            DbType = DbType.Int32,
            Direction = ParameterDirection.Output,
          });

          using (var reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              var stdout1 = reader.GetInt32(0);
              Assert.AreEqual(2, stdout1);
              var schema = reader.GetSchemaTable();
              Assert.NotNull(schema);
              Assert.True(reader.HasRows);
              Assert.AreEqual(1, reader.FieldCount);
            }
          }
          var outparam1 = _outputParam.Value;
          Assert.AreEqual(1, outparam1);
        }


        //Stored procedure with Output parameter Only
        ExecuteSQL(@"CREATE PROCEDURE out_int2(
                     OUT value INT
                    )
                    BEGIN
                     SELECT 1 INTO value;
                    END;");
        using (var cmd = connection.CreateCommand())
        {
          cmd.CommandText = "out_int2";
          cmd.CommandType = CommandType.StoredProcedure;
          MySqlParameter _outputParam2 = cmd.Parameters.Add(new MySqlParameter
          {
            ParameterName = "@value",
            DbType = DbType.Int32,
            Direction = ParameterDirection.Output,
          });

          using (var reader = cmd.ExecuteReader())
          {
            reader.Read();
            var schema = reader.GetSchemaTable();
            Assert.Null(schema);
            Assert.False(reader.HasRows);
            Assert.AreEqual(0, reader.FieldCount);
          }
          var outparam1 = _outputParam2.Value;
          Assert.AreEqual(1, outparam1);
        }
      }
    }

  }
}
