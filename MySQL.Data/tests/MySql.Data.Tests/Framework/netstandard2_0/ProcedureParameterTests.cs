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

namespace MySql.Data.MySqlClient.Tests
{
  public class ProcedureParameterTests : TestBase
  {
    public ProcedureParameterTests(TestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void ProcedureParameters()
    {
      executeSQL("CREATE PROCEDURE ProcedureParameters (id int, name varchar(50)) BEGIN SELECT 1; END");

      string[] restrictions = new string[5];
      restrictions[1] = Connection.Database;
      restrictions[2] = "ProcedureParameters";
      DataTable dt = Connection.GetSchema("Procedure Parameters", restrictions);
      Assert.True(dt.Rows.Count == 2, "Actual result " + dt.Rows.Count);
      Assert.Equal("Procedure Parameters", dt.TableName);
      Assert.Equal(Connection.Database, dt.Rows[0]["SPECIFIC_SCHEMA"].ToString());
      Assert.Equal("ProcedureParameters", dt.Rows[0]["SPECIFIC_NAME"].ToString());
      Assert.Equal("id", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[0]["PARAMETER_MODE"]);

      restrictions[4] = "name";
      dt.Clear();
      dt = Connection.GetSchema("Procedure Parameters", restrictions);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal(Connection.Database, dt.Rows[0]["SPECIFIC_SCHEMA"].ToString());
      Assert.Equal("ProcedureParameters", dt.Rows[0]["SPECIFIC_NAME"].ToString());
      Assert.Equal("name", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(2, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[0]["PARAMETER_MODE"]);

      executeSQL("DROP FUNCTION IF EXISTS spFunc");
      executeSQL("CREATE FUNCTION spFunc (id int) RETURNS INT BEGIN RETURN 1; END");

      restrictions[4] = null;
      restrictions[1] = Connection.Database;
      restrictions[2] = "spFunc";
      dt = Connection.GetSchema("Procedure Parameters", restrictions);
      Assert.True(dt.Rows.Count == 2);
      Assert.Equal("Procedure Parameters", dt.TableName);
      Assert.Equal(Connection.Database.ToLower(), dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("spfunc", dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal(0, dt.Rows[0]["ORDINAL_POSITION"]);

      Assert.Equal(Connection.Database.ToLower(), dt.Rows[1]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("spfunc", dt.Rows[1]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("id", dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(1, dt.Rows[1]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[1]["PARAMETER_MODE"]);
    }

    /// <summary>
    /// Bug #6902 Errors in parsing stored procedure parameters 
    /// </summary>
    [Fact]
    public void ProcedureParameters2()
    {
      executeSQL("DROP PROCEDURE IF EXISTS spTest");
      executeSQL(@"CREATE PROCEDURE spTest(`/*id*/` /* before type 1 */ varchar(20), 
           /* after type 1 */ OUT result2 DECIMAL(/*size1*/10,/*size2*/2) /* p2 */) 
           BEGIN SELECT action, result; END");

      string[] restrictions = new string[5];
      restrictions[1] = Connection.Database;
      restrictions[2] = "spTest";
      DataTable dt = Connection.GetSchema("Procedure Parameters", restrictions);

      Assert.True(dt.Rows.Count == 2, "Actual result " + dt.Rows.Count);
      Assert.Equal(Connection.Database.ToLower(), dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("/*id*/", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[0]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(20, dt.Rows[0]["CHARACTER_MAXIMUM_LENGTH"]);
      Assert.Equal(20, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(Connection.Database.ToLower(), dt.Rows[1]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[1]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("result2", dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(2, dt.Rows[1]["ORDINAL_POSITION"]);
      Assert.Equal("OUT", dt.Rows[1]["PARAMETER_MODE"]);
      Assert.Equal("DECIMAL", dt.Rows[1]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(10, Convert.ToInt32(dt.Rows[1]["NUMERIC_PRECISION"]));
      Assert.Equal(2, dt.Rows[1]["NUMERIC_SCALE"]);
    }

    [Fact]
    public void ProcedureParameters3()
    {
      executeSQL("DROP PROCEDURE IF EXISTS spTest");
      executeSQL(@"CREATE  PROCEDURE spTest (_ACTION varchar(20),
          `/*dumb-identifier-1*/` int, `#dumb-identifier-2` int,
          `--dumb-identifier-3` int, 
          _CLIENT_ID int, -- ABC
          _LOGIN_ID  int, # DEF
          _WHERE varchar(2000), 
          _SORT varchar(2000),
          out _SQL varchar(/* inline right here - oh my gosh! */ 8000),
          _SONG_ID int,
          _NOTES varchar(2000),
          out _RESULT varchar(10)
          /*
          ,    -- Generic result parameter
          out _PERIOD_ID int,         -- Returns the period_id. Useful when using @PREDEFLINK to return which is the last period
          _SONGS_LIST varchar(8000),
          _COMPOSERID int,
          _PUBLISHERID int,
          _PREDEFLINK int        -- If the user is accessing through a predefined link: 0=none  1=last period
          */) BEGIN SELECT 1; END");

      string[] restrictions = new string[5];
      restrictions[1] = Connection.Database;
      restrictions[2] = "spTest";
      DataTable dt = Connection.GetSchema("Procedure Parameters", restrictions);

      Assert.True(dt.Rows.Count == 12, "Rows count failed");
      Assert.Equal(Connection.Database.ToLower(), dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_action", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[0]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(20, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(Connection.Database.ToLower(), dt.Rows[1]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[1]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("/*dumb-identifier-1*/", dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(2, dt.Rows[1]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[1]["PARAMETER_MODE"]);
      Assert.Equal("INT", dt.Rows[1]["DATA_TYPE"].ToString().ToUpper());

      Assert.Equal(Connection.Database.ToLower(), dt.Rows[2]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[2]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("#dumb-identifier-2", dt.Rows[2]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(3, dt.Rows[2]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[2]["PARAMETER_MODE"]);
      Assert.Equal("INT", dt.Rows[2]["DATA_TYPE"].ToString().ToUpper());

      Assert.Equal(Connection.Database.ToLower(), dt.Rows[3]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[3]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("--dumb-identifier-3", dt.Rows[3]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(4, dt.Rows[3]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[3]["PARAMETER_MODE"]);
      Assert.Equal("INT", dt.Rows[3]["DATA_TYPE"].ToString().ToUpper());

      Assert.Equal(Connection.Database.ToLower(), dt.Rows[4]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[4]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_client_id", dt.Rows[4]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(5, dt.Rows[4]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[4]["PARAMETER_MODE"]);
      Assert.Equal("INT", dt.Rows[4]["DATA_TYPE"].ToString().ToUpper());

      Assert.Equal(Connection.Database.ToLower(), dt.Rows[5]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[5]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_login_id", dt.Rows[5]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(6, dt.Rows[5]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[5]["PARAMETER_MODE"]);
      Assert.Equal("INT", dt.Rows[5]["DATA_TYPE"].ToString().ToUpper());

      Assert.Equal(Connection.Database.ToLower(), dt.Rows[6]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[6]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_where", dt.Rows[6]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(7, dt.Rows[6]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[6]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[6]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(2000, dt.Rows[6]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(Connection.Database.ToLower(), dt.Rows[7]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[7]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_sort", dt.Rows[7]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(8, dt.Rows[7]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[7]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[7]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(2000, dt.Rows[7]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(Connection.Database.ToLower(), dt.Rows[8]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[8]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_sql", dt.Rows[8]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(9, dt.Rows[8]["ORDINAL_POSITION"]);
      Assert.Equal("OUT", dt.Rows[8]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[8]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(8000, dt.Rows[8]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(Connection.Database.ToLower(), dt.Rows[9]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[9]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_song_id", dt.Rows[9]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(10, dt.Rows[9]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[9]["PARAMETER_MODE"]);
      Assert.Equal("INT", dt.Rows[9]["DATA_TYPE"].ToString().ToUpper());

      Assert.Equal(Connection.Database.ToLower(), dt.Rows[10]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[10]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_notes", dt.Rows[10]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(11, dt.Rows[10]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[10]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[10]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(2000, dt.Rows[10]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(Connection.Database.ToLower(), dt.Rows[11]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[11]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_result", dt.Rows[11]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(12, dt.Rows[11]["ORDINAL_POSITION"]);
      Assert.Equal("OUT", dt.Rows[11]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[11]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(10, dt.Rows[11]["CHARACTER_OCTET_LENGTH"]);
    }

    [Fact]
    public void ProcedureParameters4()
    {
      executeSQL(@"CREATE  PROCEDURE ProcedureParameters4 (name VARCHAR(1200) 
          CHARACTER /* hello*/ SET utf8) BEGIN SELECT name; END");

      string[] restrictions = new string[5];
      restrictions[1] = Connection.Database;
      restrictions[2] = "ProcedureParameters4";
      DataTable dt = Connection.GetSchema("Procedure Parameters", restrictions);

      Assert.True(dt.Rows.Count == 1, "Actual Result " + dt.Rows.Count);
      Assert.Equal(Connection.Database, dt.Rows[0]["SPECIFIC_SCHEMA"].ToString());
      Assert.Equal("ProcedureParameters4", dt.Rows[0]["SPECIFIC_NAME"].ToString());
      Assert.Equal("name", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[0]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(1200, dt.Rows[0]["CHARACTER_MAXIMUM_LENGTH"]);
      Assert.Equal(3600, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);
      //else
      //  Assert.Equal(4800, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);
      Assert.Equal("utf8", dt.Rows[0]["CHARACTER_SET_NAME"]);
      Assert.Equal("utf8_general_ci", dt.Rows[0]["COLLATION_NAME"]);
    }

    [Fact]
    public void ProcedureParameters5()
    {
      executeSQL(@"CREATE  PROCEDURE ProcedureParameters5 (name VARCHAR(1200) ASCII BINARY, 
          name2 TEXT UNICODE) BEGIN SELECT name; END");

      string[] restrictions = new string[5];
      restrictions[1] = Connection.Database;
      restrictions[2] = "ProcedureParameters5";
      DataTable dt = Connection.GetSchema("Procedure Parameters", restrictions);

      Assert.True(dt.Rows.Count == 2);
      Assert.Equal(Connection.Database, dt.Rows[0]["SPECIFIC_SCHEMA"].ToString());
      Assert.Equal("ProcedureParameters5", dt.Rows[0]["SPECIFIC_NAME"].ToString());
      Assert.Equal("name", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[0]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal("latin1", dt.Rows[0]["CHARACTER_SET_NAME"]);
      Assert.Equal(1200, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(Connection.Database, dt.Rows[1]["SPECIFIC_SCHEMA"].ToString());
      Assert.Equal("ProcedureParameters5", dt.Rows[1]["SPECIFIC_NAME"].ToString());
      Assert.Equal("name2", dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(2, dt.Rows[1]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[1]["PARAMETER_MODE"]);
      Assert.Equal("TEXT", dt.Rows[1]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal("ucs2", dt.Rows[1]["CHARACTER_SET_NAME"]);
    }

    [Fact]
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
        Assert.Equal("INT(10) UNSIGNED ZEROFILL",
          dt.Rows[0]["DTD_IDENTIFIER"].ToString().ToUpper());
        Assert.Equal("DECIMAL(10,2)",
          dt.Rows[1]["DTD_IDENTIFIER"].ToString().ToUpper());
        Assert.Equal("VARCHAR(20)",
          dt.Rows[2]["DTD_IDENTIFIER"].ToString().ToUpper());
        Assert.Equal("TINYTEXT",
          dt.Rows[3]["DTD_IDENTIFIER"].ToString().ToUpper());
        Assert.Equal("ENUM('A','B','C')",
          dt.Rows[4]["DTD_IDENTIFIER"].ToString().ToUpper());
        Assert.Equal("SET('1','2','3')",
          dt.Rows[5]["DTD_IDENTIFIER"].ToString().ToUpper());
        conn.Close();
      }
    }

    /// <summary>
    /// Bug #48586	Expose defined possible enum values
    /// </summary>
    [Fact]
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
        Assert.Equal("a", t2.PossibleValues[0]);
        Assert.Equal("b", t2.PossibleValues[1]);
        Assert.Equal("c", t2.PossibleValues[2]);
        MySqlParameter t3 = cmd.Parameters["@t3"];
        Assert.NotNull(t3.PossibleValues);
        Assert.Equal("1", t3.PossibleValues[0]);
        Assert.Equal("2", t3.PossibleValues[1]);
        Assert.Equal("3", t3.PossibleValues[2]);
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
        Assert.Equal("a", t2.PossibleValues[0]);
        Assert.Equal("b", t2.PossibleValues[1]);
        Assert.Equal("c", t2.PossibleValues[2]);
        MySqlParameter t3 = cmd.Parameters["@t3"];
        Assert.NotNull(t3.PossibleValues);
        Assert.Equal("1", t3.PossibleValues[0]);
        Assert.Equal("2", t3.PossibleValues[1]);
        Assert.Equal("3", t3.PossibleValues[2]);
        conn.Close();
      }
    }

    /// <summary>
    /// Bug #62416	IndexOutOfRangeException when using return parameter with no name
    /// </summary>
    [Fact]
    public void UnnamedReturnValue()
    {
      executeSQL("DROP FUNCTION IF EXISTS spFunc");
      executeSQL("CREATE FUNCTION spFunc() RETURNS DATETIME BEGIN RETURN NOW(); END");
      MySqlCommand cmd = new MySqlCommand("spFunc", Connection);
      cmd.CommandType = CommandType.StoredProcedure;
      MySqlParameter p1 = new MySqlParameter("", MySqlDbType.DateTime);
      p1.Direction = ParameterDirection.ReturnValue;
      cmd.Parameters.Add(p1);
      cmd.ExecuteNonQuery();
    }
  }
}
