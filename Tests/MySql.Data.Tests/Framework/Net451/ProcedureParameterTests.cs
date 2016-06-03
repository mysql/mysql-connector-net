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
  public class ProcedureParameterTests : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;

    public void SetFixture(SetUpClass data)
    {
      st = data;
      if (st.conn.connectionState == ConnectionState.Open)
        st.conn.Close();
      
      st.conn.Open();
    }

    public void Dispose()
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");
      st.execSQL("DROP PROCEDURE IF EXISTS spTest");
      st.conn.Close();
      st.conn.Dispose();
    }

    [Fact]
    public void ProcedureParameters()
    {
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("CREATE PROCEDURE spTest (id int, name varchar(50)) BEGIN SELECT 1; END");

      string[] restrictions = new string[5];
      restrictions[1] = st.database0;
      restrictions[2] = "spTest";
      DataTable dt = st.conn.GetSchema("Procedure Parameters", restrictions);
      Assert.True(dt.Rows.Count == 2, "Actual result " + dt.Rows.Count);
      Assert.Equal("Procedure Parameters", dt.TableName);
      Assert.Equal(st.database0.ToLower(), dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("id", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[0]["PARAMETER_MODE"]);

      restrictions[4] = "name";
      dt.Clear();
      dt = st.conn.GetSchema("Procedure Parameters", restrictions);
      Assert.Equal(1, dt.Rows.Count);
      Assert.Equal(st.database0.ToLower(), dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("name", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(2, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[0]["PARAMETER_MODE"]);

      st.execSQL("DROP FUNCTION IF EXISTS spFunc");
      st.execSQL("CREATE FUNCTION spFunc (id int) RETURNS INT BEGIN RETURN 1; END");

      restrictions[4] = null;
      restrictions[1] = st.database0;
      restrictions[2] = "spFunc";
      dt = st.conn.GetSchema("Procedure Parameters", restrictions);
      Assert.True(dt.Rows.Count == 2);
      Assert.Equal("Procedure Parameters", dt.TableName);
      Assert.Equal(st.database0.ToLower(), dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("spfunc", dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal(0, dt.Rows[0]["ORDINAL_POSITION"]);

      Assert.Equal(st.database0.ToLower(), dt.Rows[1]["SPECIFIC_SCHEMA"].ToString().ToLower());
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL(@"CREATE PROCEDURE spTest(`/*id*/` /* before type 1 */ varchar(20), 
           /* after type 1 */ OUT result2 DECIMAL(/*size1*/10,/*size2*/2) /* p2 */) 
           BEGIN SELECT action, result; END");

      string[] restrictions = new string[5];
      restrictions[1] = st.database0;
      restrictions[2] = "spTest";
      DataTable dt = st.conn.GetSchema("Procedure Parameters", restrictions);

      Assert.True(dt.Rows.Count == 2, "Actual result " + dt.Rows.Count);
      Assert.Equal(st.database0.ToLower(), dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("/*id*/", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[0]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(20, dt.Rows[0]["CHARACTER_MAXIMUM_LENGTH"]);
      Assert.Equal(20, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(st.database0.ToLower(), dt.Rows[1]["SPECIFIC_SCHEMA"].ToString().ToLower());
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL(@"CREATE  PROCEDURE spTest (_ACTION varchar(20),
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
      restrictions[1] = st.database0;
      restrictions[2] = "spTest";
      DataTable dt = st.conn.GetSchema("Procedure Parameters", restrictions);

      Assert.True(dt.Rows.Count == 12, "Rows count failed");
      Assert.Equal(st.database0.ToLower(), dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_action", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[0]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(20, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(st.database0.ToLower(), dt.Rows[1]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[1]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("/*dumb-identifier-1*/", dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(2, dt.Rows[1]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[1]["PARAMETER_MODE"]);
      Assert.Equal("INT", dt.Rows[1]["DATA_TYPE"].ToString().ToUpper());

      Assert.Equal(st.database0.ToLower(), dt.Rows[2]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[2]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("#dumb-identifier-2", dt.Rows[2]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(3, dt.Rows[2]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[2]["PARAMETER_MODE"]);
      Assert.Equal("INT", dt.Rows[2]["DATA_TYPE"].ToString().ToUpper());

      Assert.Equal(st.database0.ToLower(), dt.Rows[3]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[3]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("--dumb-identifier-3", dt.Rows[3]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(4, dt.Rows[3]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[3]["PARAMETER_MODE"]);
      Assert.Equal("INT", dt.Rows[3]["DATA_TYPE"].ToString().ToUpper());

      Assert.Equal(st.database0.ToLower(), dt.Rows[4]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[4]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_client_id", dt.Rows[4]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(5, dt.Rows[4]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[4]["PARAMETER_MODE"]);
      Assert.Equal("INT", dt.Rows[4]["DATA_TYPE"].ToString().ToUpper());

      Assert.Equal(st.database0.ToLower(), dt.Rows[5]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[5]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_login_id", dt.Rows[5]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(6, dt.Rows[5]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[5]["PARAMETER_MODE"]);
      Assert.Equal("INT", dt.Rows[5]["DATA_TYPE"].ToString().ToUpper());

      Assert.Equal(st.database0.ToLower(), dt.Rows[6]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[6]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_where", dt.Rows[6]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(7, dt.Rows[6]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[6]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[6]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(2000, dt.Rows[6]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(st.database0.ToLower(), dt.Rows[7]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[7]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_sort", dt.Rows[7]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(8, dt.Rows[7]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[7]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[7]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(2000, dt.Rows[7]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(st.database0.ToLower(), dt.Rows[8]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[8]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_sql", dt.Rows[8]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(9, dt.Rows[8]["ORDINAL_POSITION"]);
      Assert.Equal("OUT", dt.Rows[8]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[8]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(8000, dt.Rows[8]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(st.database0.ToLower(), dt.Rows[9]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[9]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_song_id", dt.Rows[9]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(10, dt.Rows[9]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[9]["PARAMETER_MODE"]);
      Assert.Equal("INT", dt.Rows[9]["DATA_TYPE"].ToString().ToUpper());

      Assert.Equal(st.database0.ToLower(), dt.Rows[10]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[10]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("_notes", dt.Rows[10]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(11, dt.Rows[10]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[10]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[10]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(2000, dt.Rows[10]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(st.database0.ToLower(), dt.Rows[11]["SPECIFIC_SCHEMA"].ToString().ToLower());
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL(@"CREATE  PROCEDURE spTest (name VARCHAR(1200) 
          CHARACTER /* hello*/ SET utf8) BEGIN SELECT name; END");

      string[] restrictions = new string[5];
      restrictions[1] = st.database0;
      restrictions[2] = "spTest";
      DataTable dt = st.conn.GetSchema("Procedure Parameters", restrictions);

      Assert.True(dt.Rows.Count == 1, "Actual Result " + dt.Rows.Count);
      Assert.Equal(st.database0.ToLower(), dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("name", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[0]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal(1200, dt.Rows[0]["CHARACTER_MAXIMUM_LENGTH"]);
      if (st.Version.Major >= 6)
        Assert.Equal(4800, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);
      else
        Assert.Equal(3600, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);
      Assert.Equal("utf8", dt.Rows[0]["CHARACTER_SET_NAME"]);
      Assert.Equal("utf8_general_ci", dt.Rows[0]["COLLATION_NAME"]);
    }

    [Fact]
    public void ProcedureParameters5()
    {
      if (st.Version < new Version(5, 0)) return;

      st.execSQL(@"CREATE  PROCEDURE spTest (name VARCHAR(1200) ASCII BINARY, 
          name2 TEXT UNICODE) BEGIN SELECT name; END");

      string[] restrictions = new string[5];
      restrictions[1] = st.database0;
      restrictions[2] = "spTest";
      DataTable dt = st.conn.GetSchema("Procedure Parameters", restrictions);

      Assert.True(dt.Rows.Count == 2);
      Assert.Equal(st.database0.ToLower(), dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("name", dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(1, dt.Rows[0]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[0]["PARAMETER_MODE"]);
      Assert.Equal("VARCHAR", dt.Rows[0]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal("latin1", dt.Rows[0]["CHARACTER_SET_NAME"]);
      Assert.Equal(1200, dt.Rows[0]["CHARACTER_OCTET_LENGTH"]);

      Assert.Equal(st.database0.ToLower(), dt.Rows[1]["SPECIFIC_SCHEMA"].ToString().ToLower());
      Assert.Equal("sptest", dt.Rows[1]["SPECIFIC_NAME"].ToString().ToLower());
      Assert.Equal("name2", dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower());
      Assert.Equal(2, dt.Rows[1]["ORDINAL_POSITION"]);
      Assert.Equal("IN", dt.Rows[1]["PARAMETER_MODE"]);
      Assert.Equal("TEXT", dt.Rows[1]["DATA_TYPE"].ToString().ToUpper());
      Assert.Equal("ucs2", dt.Rows[1]["CHARACTER_SET_NAME"]);
    }

    [Fact]
    public void DTD_Identifier()
    {
      if (st.Version < new Version(5, 0)) return;

      using (var conn = new MySqlConnection(st.conn.ConnectionString))
      {
        conn.Open();
        var cmd = new MySqlCommand(@"CREATE PROCEDURE spTest (id INT UNSIGNED ZEROFILL,
          dec1 DECIMAL(10,2), 
          name VARCHAR(20) /* this is a comment */ CHARACTER SET ascii,
          t1 TINYTEXT BINARY, t2 ENUM('a','b','c'),
          t3 /* comment */ SET(/* comment */'1','2','3'))
          BEGIN SELECT name; END", conn);
          cmd.ExecuteNonQuery();

          string[] restrictions = new string[5];
          restrictions[1] = st.database0;
          restrictions[2] = "spTest";
          DataTable dt = st.conn.GetSchema("Procedure Parameters", restrictions);

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
      if (st.Version < new Version(5, 0)) return;

      using (var conn = new MySqlConnection(st.conn.ConnectionString))
      {
        conn.Open();
        var cmd = new MySqlCommand(@"CREATE  PROCEDURE spTest (id INT UNSIGNED ZEROFILL,
          dec1 DECIMAL(10,2), 
          name VARCHAR(20) /* this is a comment */ CHARACTER SET ascii,
          t1 TINYTEXT BINARY, t2 ENUM('a','b','c'),
          t3 /* comment */ SET(/* comment */'1','2','3'))
          BEGIN SELECT name; END", conn);
          cmd.ExecuteNonQuery();
          cmd = new MySqlCommand("spTest", st.conn);
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
      if (st.Version < new Version(5, 0)) return;

      st.execSQL("DROP FUNCTION IF EXISTS spFunc");
      st.execSQL("CREATE FUNCTION spFunc() RETURNS DATETIME BEGIN RETURN NOW(); END");
      MySqlCommand cmd = new MySqlCommand("spFunc", st.conn);
      cmd.CommandType = CommandType.StoredProcedure;
      MySqlParameter p1 = new MySqlParameter("", MySqlDbType.DateTime);
      p1.Direction = ParameterDirection.ReturnValue;
      cmd.Parameters.Add(p1);
      cmd.ExecuteNonQuery();      
    }
  }
}
