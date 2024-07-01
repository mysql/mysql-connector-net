// Copyright © 2013, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using NUnit.Framework.Legacy;
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
      Assert.That(dt.Rows.Count == 2, "Actual result " + dt.Rows.Count);
      Assert.That(dt.TableName, Is.EqualTo("Procedure Parameters"));
      Assert.That(dt.Rows[0]["SPECIFIC_SCHEMA"].ToString(), Is.EqualTo(Connection.Database));
      Assert.That(dt.Rows[0]["SPECIFIC_NAME"].ToString(), Is.EqualTo("ProcedureParameters"));
      Assert.That(dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("id"));
      Assert.That(dt.Rows[0]["ORDINAL_POSITION"], Is.EqualTo(1));
      Assert.That(dt.Rows[0]["PARAMETER_MODE"], Is.EqualTo("IN"));

      restrictions[4] = "name";
      dt.Clear();
      dt = Connection.GetSchema("Procedure Parameters", restrictions);
      Assert.That(dt.Rows.Count, Is.EqualTo(1));
      Assert.That(dt.Rows[0]["SPECIFIC_SCHEMA"].ToString(), Is.EqualTo(Connection.Database));
      Assert.That(dt.Rows[0]["SPECIFIC_NAME"].ToString(), Is.EqualTo("ProcedureParameters"));
      Assert.That(dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("name"));
      Assert.That(dt.Rows[0]["ORDINAL_POSITION"], Is.EqualTo(2));
      Assert.That(dt.Rows[0]["PARAMETER_MODE"], Is.EqualTo("IN"));

      ExecuteSQL("DROP FUNCTION IF EXISTS spFunc");
      ExecuteSQL("CREATE FUNCTION spFunc (id int) RETURNS INT BEGIN RETURN 1; END");

      restrictions[4] = null;
      restrictions[1] = Connection.Database;
      restrictions[2] = "spFunc";
      dt = Connection.GetSchema("Procedure Parameters", restrictions);
      Assert.That(dt.Rows.Count == 2);
      Assert.That(dt.TableName, Is.EqualTo("Procedure Parameters"));
      Assert.That(dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("spfunc"));
      Assert.That(dt.Rows[0]["ORDINAL_POSITION"], Is.EqualTo(0));

      Assert.That(dt.Rows[1]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[1]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("spfunc"));
      Assert.That(dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("id"));
      Assert.That(dt.Rows[1]["ORDINAL_POSITION"], Is.EqualTo(1));
      Assert.That(dt.Rows[1]["PARAMETER_MODE"], Is.EqualTo("IN"));
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

      Assert.That(dt.Rows.Count == 2, "Actual result " + dt.Rows.Count);
      Assert.That(dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("/*id*/"));
      Assert.That(dt.Rows[0]["ORDINAL_POSITION"], Is.EqualTo(1));
      Assert.That(dt.Rows[0]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[0]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("VARCHAR"));
      Assert.That(dt.Rows[0]["CHARACTER_MAXIMUM_LENGTH"], Is.EqualTo(20));
      Assert.That(dt.Rows[0]["CHARACTER_OCTET_LENGTH"], Is.EqualTo(20));

      Assert.That(dt.Rows[1]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[1]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("result2"));
      Assert.That(dt.Rows[1]["ORDINAL_POSITION"], Is.EqualTo(2));
      Assert.That(dt.Rows[1]["PARAMETER_MODE"], Is.EqualTo("OUT"));
      Assert.That(dt.Rows[1]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("DECIMAL"));
      Assert.That(Convert.ToInt32(dt.Rows[1]["NUMERIC_PRECISION"]), Is.EqualTo(10));
      Assert.That(dt.Rows[1]["NUMERIC_SCALE"], Is.EqualTo(2));
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

      Assert.That(dt.Rows.Count == 12, Is.True, "Rows count failed");
      Assert.That(dt.Rows[0]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[0]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("_action"));
      Assert.That(dt.Rows[0]["ORDINAL_POSITION"], Is.EqualTo(1));
      Assert.That(dt.Rows[0]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[0]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("VARCHAR"));
      Assert.That(dt.Rows[0]["CHARACTER_OCTET_LENGTH"], Is.EqualTo(20));

      Assert.That(dt.Rows[1]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[1]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("/*dumb-identifier-1*/"));
      Assert.That(dt.Rows[1]["ORDINAL_POSITION"], Is.EqualTo(2));
      Assert.That(dt.Rows[1]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[1]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("INT"));

      Assert.That(dt.Rows[2]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[2]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[2]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("#dumb-identifier-2"));
      Assert.That(dt.Rows[2]["ORDINAL_POSITION"], Is.EqualTo(3));
      Assert.That(dt.Rows[2]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[2]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("INT"));

      Assert.That(dt.Rows[3]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[3]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[3]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("--dumb-identifier-3"));
      Assert.That(dt.Rows[3]["ORDINAL_POSITION"], Is.EqualTo(4));
      Assert.That(dt.Rows[3]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[3]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("INT"));

      Assert.That(dt.Rows[4]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[4]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[4]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("_client_id"));
      Assert.That(dt.Rows[4]["ORDINAL_POSITION"], Is.EqualTo(5));
      Assert.That(dt.Rows[4]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[4]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("INT"));

      Assert.That(dt.Rows[5]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[5]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[5]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("_login_id"));
      Assert.That(dt.Rows[5]["ORDINAL_POSITION"], Is.EqualTo(6));
      Assert.That(dt.Rows[5]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[5]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("INT"));

      Assert.That(dt.Rows[6]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[6]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[6]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("_where"));
      Assert.That(dt.Rows[6]["ORDINAL_POSITION"], Is.EqualTo(7));
      Assert.That(dt.Rows[6]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[6]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("VARCHAR"));
      Assert.That(dt.Rows[6]["CHARACTER_OCTET_LENGTH"], Is.EqualTo(2000));

      Assert.That(dt.Rows[7]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[7]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[7]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("_sort"));
      Assert.That(dt.Rows[7]["ORDINAL_POSITION"], Is.EqualTo(8));
      Assert.That(dt.Rows[7]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[7]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("VARCHAR"));
      Assert.That(dt.Rows[7]["CHARACTER_OCTET_LENGTH"], Is.EqualTo(8000));

      Assert.That(dt.Rows[8]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[8]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[8]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("_sql"));
      Assert.That(dt.Rows[8]["ORDINAL_POSITION"], Is.EqualTo(9));
      Assert.That(dt.Rows[8]["PARAMETER_MODE"], Is.EqualTo("OUT"));
      Assert.That(dt.Rows[8]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("VARCHAR"));
      Assert.That(dt.Rows[8]["CHARACTER_OCTET_LENGTH"], Is.EqualTo(8000));

      Assert.That(dt.Rows[9]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[9]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[9]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("_song_id"));
      Assert.That(dt.Rows[9]["ORDINAL_POSITION"], Is.EqualTo(10));
      Assert.That(dt.Rows[9]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[9]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("INT"));

      Assert.That(dt.Rows[10]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[10]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[10]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("_notes"));
      Assert.That(dt.Rows[10]["ORDINAL_POSITION"], Is.EqualTo(11));
      Assert.That(dt.Rows[10]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[10]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("VARCHAR"));
      Assert.That(dt.Rows[10]["CHARACTER_OCTET_LENGTH"], Is.EqualTo(2000));

      Assert.That(dt.Rows[11]["SPECIFIC_SCHEMA"].ToString().ToLower(), Is.EqualTo(Connection.Database.ToLower()));
      Assert.That(dt.Rows[11]["SPECIFIC_NAME"].ToString().ToLower(), Is.EqualTo("sptest"));
      Assert.That(dt.Rows[11]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("_result"));
      Assert.That(dt.Rows[11]["ORDINAL_POSITION"], Is.EqualTo(12));
      Assert.That(dt.Rows[11]["PARAMETER_MODE"], Is.EqualTo("OUT"));
      Assert.That(dt.Rows[11]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("VARCHAR"));
      Assert.That(dt.Rows[11]["CHARACTER_OCTET_LENGTH"], Is.EqualTo(10));
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

      Assert.That(dt.Rows.Count == 1, Is.True, "Actual Result " + dt.Rows.Count);
      Assert.That(dt.Rows[0]["SPECIFIC_SCHEMA"].ToString(), Is.EqualTo(Connection.Database));
      Assert.That(dt.Rows[0]["SPECIFIC_NAME"].ToString(), Is.EqualTo("ProcedureParameters4"));
      Assert.That(dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("name"));
      Assert.That(dt.Rows[0]["ORDINAL_POSITION"], Is.EqualTo(1));
      Assert.That(dt.Rows[0]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[0]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("VARCHAR"));
      Assert.That(dt.Rows[0]["CHARACTER_MAXIMUM_LENGTH"], Is.EqualTo(1200));
      Assert.That(dt.Rows[0]["CHARACTER_OCTET_LENGTH"], Is.EqualTo(3600));
      Assert.That(dt.Rows[0]["CHARACTER_SET_NAME"], Is.EqualTo(charset));
      Assert.That(dt.Rows[0]["COLLATION_NAME"], Is.EqualTo($"{charset}_general_ci"));
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

      Assert.That(dt.Rows.Count == 2, Is.True);
      Assert.That(dt.Rows[0]["SPECIFIC_SCHEMA"].ToString(), Is.EqualTo(Connection.Database));
      Assert.That(dt.Rows[0]["SPECIFIC_NAME"].ToString(), Is.EqualTo("ProcedureParameters5"));
      Assert.That(dt.Rows[0]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("name"));
      Assert.That(dt.Rows[0]["ORDINAL_POSITION"], Is.EqualTo(1));
      Assert.That(dt.Rows[0]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[0]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("VARCHAR"));
      Assert.That(dt.Rows[0]["CHARACTER_SET_NAME"], Is.EqualTo("latin1"));
      Assert.That(dt.Rows[0]["CHARACTER_OCTET_LENGTH"], Is.EqualTo(1200));

      Assert.That(dt.Rows[1]["SPECIFIC_SCHEMA"].ToString(), Is.EqualTo(Connection.Database));
      Assert.That(dt.Rows[1]["SPECIFIC_NAME"].ToString(), Is.EqualTo("ProcedureParameters5"));
      Assert.That(dt.Rows[1]["PARAMETER_NAME"].ToString().ToLower(), Is.EqualTo("name2"));
      Assert.That(dt.Rows[1]["ORDINAL_POSITION"], Is.EqualTo(2));
      Assert.That(dt.Rows[1]["PARAMETER_MODE"], Is.EqualTo("IN"));
      Assert.That(dt.Rows[1]["DATA_TYPE"].ToString().ToUpper(), Is.EqualTo("TEXT"));
      Assert.That(dt.Rows[1]["CHARACTER_SET_NAME"], Is.EqualTo("ucs2"));
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

        Assert.That(dt.Rows.Count == 6, Is.True, "Actual Result " + dt.Rows.Count);
        Assert.That(dt.Rows[0]["DTD_IDENTIFIER"].ToString().ToUpper(), Is.EqualTo("INT(10) UNSIGNED ZEROFILL"));
        Assert.That(dt.Rows[1]["DTD_IDENTIFIER"].ToString().ToUpper(), Is.EqualTo("DECIMAL(10,2)"));
        Assert.That(dt.Rows[2]["DTD_IDENTIFIER"].ToString().ToUpper(), Is.EqualTo("VARCHAR(20)"));
        Assert.That(dt.Rows[3]["DTD_IDENTIFIER"].ToString().ToUpper(), Is.EqualTo("TINYTEXT"));
        Assert.That(dt.Rows[4]["DTD_IDENTIFIER"].ToString().ToUpper(), Is.EqualTo("ENUM('A','B','C')"));
        Assert.That(dt.Rows[5]["DTD_IDENTIFIER"].ToString().ToUpper(), Is.EqualTo("SET('1','2','3')"));
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
        Assert.That(cmd.Parameters["@id"].PossibleValues, Is.Null);
        Assert.That(cmd.Parameters["@dec1"].PossibleValues, Is.Null);
        Assert.That(cmd.Parameters["@name"].PossibleValues, Is.Null);
        Assert.That(cmd.Parameters["@t1"].PossibleValues, Is.Null);
        MySqlParameter t2 = cmd.Parameters["@t2"];
        Assert.That(t2.PossibleValues, Is.Not.Null);
        Assert.That(t2.PossibleValues[0], Is.EqualTo("a"));
        Assert.That(t2.PossibleValues[1], Is.EqualTo("b"));
        Assert.That(t2.PossibleValues[2], Is.EqualTo("c"));
        MySqlParameter t3 = cmd.Parameters["@t3"];
        Assert.That(t3.PossibleValues, Is.Not.Null);
        Assert.That(t3.PossibleValues[0], Is.EqualTo("1"));
        Assert.That(t3.PossibleValues[1], Is.EqualTo("2"));
        Assert.That(t3.PossibleValues[2], Is.EqualTo("3"));
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
        Assert.That(cmd.Parameters["@id"].PossibleValues, Is.Null);
        Assert.That(cmd.Parameters["@dec1"].PossibleValues, Is.Null);
        Assert.That(cmd.Parameters["@name"].PossibleValues, Is.Null);
        Assert.That(cmd.Parameters["@t1"].PossibleValues, Is.Null);
        MySqlParameter t2 = cmd.Parameters["@t2"];
        Assert.That(t2.PossibleValues, Is.Not.Null);
        Assert.That(t2.PossibleValues[0], Is.EqualTo("a"));
        Assert.That(t2.PossibleValues[1], Is.EqualTo("b"));
        Assert.That(t2.PossibleValues[2], Is.EqualTo("c"));
        MySqlParameter t3 = cmd.Parameters["@t3"];
        Assert.That(t3.PossibleValues, Is.Not.Null);
        Assert.That(t3.PossibleValues[0], Is.EqualTo("1"));
        Assert.That(t3.PossibleValues[1], Is.EqualTo("2"));
        Assert.That(t3.PossibleValues[2], Is.EqualTo("3"));
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
        Assert.That(ds.Tables.Count > 0, Is.True);

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
          Assert.That(ds.Tables.Count > 0, Is.True);
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
              Assert.That(stdout1, Is.EqualTo(2));
              var schema = reader.GetSchemaTable();
              Assert.That(schema, Is.Not.Null);
              Assert.That(reader.HasRows, Is.True);
              Assert.That(reader.FieldCount, Is.EqualTo(1));
            }
          }
          var outparam1 = _outputParam.Value;
          Assert.That(outparam1, Is.EqualTo(1));
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
            Assert.That(schema, Is.Null);
            Assert.That(reader.HasRows, Is.False);
            Assert.That(reader.FieldCount, Is.EqualTo(0));
          }
          var outparam1 = _outputParam2.Value;
          Assert.That(outparam1, Is.EqualTo(1));
        }
      }
    }

  }
}
