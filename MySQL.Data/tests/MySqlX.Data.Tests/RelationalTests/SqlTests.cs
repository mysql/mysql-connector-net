// Copyright © 2015, 2024, Oracle and/or its affiliates.
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

using Google.Protobuf;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Collections.Generic;

namespace MySqlX.Data.Tests.RelationalTests
{
  public class SqlTests : BaseTest
  {
    [TearDown]
    public void TearDown()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test");
    }

    [Test]
    public void ReturnSimpleScalar()
    {
      ExecuteSQL("DROP TABLE IF EXISTS test.test");
      ExecuteSQL("CREATE TABLE test.test(id INT)");
      ExecuteSQL("INSERT INTO test.test VALUES (1)");
      using (var ss = MySQLX.GetSession(ConnectionString))
      {
        SqlResult r = ss.SQL("SELECT * FROM test.test").Execute();
        Assert.That(r.Next());
        Assert.That(r[0], Is.EqualTo(1));
        Assert.That(r.NextResult(), Is.False);
      }

    }

    [Test]
    public void ExecuteStoredProcedure()
    {
      ExecuteSQL("CREATE PROCEDURE `my_proc` () BEGIN SELECT 5; END");

      Session session = GetSession(true);
      var result = ExecuteSQLStatement(session.SQL("CALL my_proc()"));
      Assert.That(result.HasData);
      var row = result.FetchOne();
      Assert.That(row, Is.Not.Null);
      Assert.That(row[0], Is.EqualTo((sbyte)5));
      Assert.That(result.Next(), Is.False);
      Assert.That(result.FetchOne(), Is.Null);
      Assert.That(result.NextResult(), Is.False);
    }

    [Test]
    public void ExecuteStoredProcedureMultipleResults()
    {
      ExecuteSQL("drop procedure if exists my_proc");
      ExecuteSQL("CREATE PROCEDURE `my_proc` () BEGIN SELECT 5; SELECT 'A'; SELECT 5 * 2; END");

      Session session = GetSession(true);
      var result = ExecuteSQLStatement(session.SQL("CALL my_proc()"));
      Assert.That(result.HasData);
      var row = result.FetchOne();
      Assert.That(row, Is.Not.Null);
      Assert.That(row[0], Is.EqualTo((sbyte)5));
      Assert.That(result.Next(), Is.False);
      Assert.That(result.FetchOne(), Is.Null);

      Assert.That(result.NextResult());
      row = result.FetchOne();
      Assert.That(row, Is.Not.Null);
      Assert.That(row[0], Is.EqualTo("A"));
      Assert.That(result.Next(), Is.False);
      Assert.That(result.FetchOne(), Is.Null);

      Assert.That(result.NextResult());
      row = result.FetchOne();
      Assert.That(row, Is.Not.Null);
      Assert.That(row[0], Is.EqualTo((sbyte)10));
      Assert.That(result.Next(), Is.False);
      Assert.That(result.FetchOne(), Is.Null);

      Assert.That(result.NextResult(), Is.False);
    }

    [Test]
    public void Bind()
    {
      ExecuteSQL("drop table if exists test.test");
      ExecuteSQL("CREATE TABLE test.test(id INT, letter varchar(1))");
      for (int i = 1; i <= 10; i++)
        ExecuteSQLStatement(GetSession(true).SQL("INSERT INTO test.test VALUES (?, ?), (?, ?)")
          .Bind(i, ((char)('@' + i)).ToString())
          .Bind(++i, ((char)('@' + i)).ToString()));

      SqlResult result = ExecuteSQLStatement(GetSession(true).SQL("select * from test.test where id=?").Bind(5));
      Assert.That(result.Next());
      Assert.That(result.Rows, Has.One.Items);
      Assert.That(result[0], Is.EqualTo(5));
      Assert.That(result[1], Is.EqualTo("E"));
    }

    [Test]
    public void BindNull()
    {
      ExecuteSQL("drop table if exists test.test");
      ExecuteSQL("CREATE TABLE test.test(id INT, letter varchar(1))");

      var session = GetSession(true);
      var result = ExecuteSQLStatement(session.SQL("INSERT INTO test.test VALUES(1, ?), (2, 'B');").Bind(null));
      Assert.That(result.AffectedItemsCount, Is.EqualTo(2ul));

      var sqlResult = ExecuteSQLStatement(session.SQL("SELECT * FROM test.test WHERE letter is ?").Bind(null)).FetchAll();
      Assert.That(sqlResult, Has.One.Items);
      Assert.That(sqlResult[0][0], Is.EqualTo(1));
      Assert.That(sqlResult[0][1], Is.Null);
    }

    [Test]
    public void Alias()
    {
      var session = GetSession(true);
      var stmt = ExecuteSQLStatement(session.SQL("SELECT 1 AS UNO"));
      var result = stmt.FetchAll();
      Assert.That(stmt.Columns[0].ColumnLabel, Is.EqualTo("UNO"));
    }

    #region WL14389

    [Test, Description("call after failed procedure")]
    public void ProcedureWithNoTable()
    {
      ExecuteSQL("create procedure newproc (in p1 int,in p2 char(20)) begin select 1; select 'XXX' from notab; end;");

      var session = MySQLX.GetSession(ConnectionString + ";database=test;");


      var sqlRes = session.SQL("call newproc(?, ?)").Bind(10).Bind("X").Execute();
      var ex = Assert.Throws<MySqlException>(() => session.SQL("drop procedure if exists newproc ").Execute());
      Assert.That(ex.Message, Is.EqualTo("Table 'test.notab' doesn't exist").IgnoreCase);
    }

    [Test, Description("Stored Procedure Table Positive using Session")]
    public void TablePositiveSession()
    {
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      var connectionStringObject = new { connection = "server=" + sb.Server + ";user=" + sb.UserID + ";port=" + sb.Port + ";password=" + sb.Password + ";sslmode=" + MySqlSslMode.Required + ";" };
      using (Session sessionPlain = MySQLX.GetSession(connectionStringObject.connection))
      {
        sessionPlain.SQL("DROP DATABASE IF EXISTS DBName").Execute();
        sessionPlain.SQL("CREATE DATABASE DBName").Execute();
        sessionPlain.SQL("USE DBName").Execute();
        sessionPlain.SQL("CREATE TABLE address" +
                    "(address_number  INT NOT NULL AUTO_INCREMENT, " +
                    "building_name  VARCHAR(100) NOT NULL, " +
                    "district VARCHAR(100) NOT NULL, PRIMARY KEY (address_number)" + ");").Execute();
        sessionPlain.SQL("INSERT INTO address" +
                    "(address_number,building_name,district)" +
                    " VALUES " +
                    "(1573,'MySQL','BGL');").Execute();
        string procI = "CREATE PROCEDURE my_add_one_procedure " +
                    " (IN address_id INT) " +
                    "BEGIN " +
                    "select * from address as a where a.address_number = address_id;" +
                    "END;";
        sessionPlain.SQL(procI).Execute();
        var res = sessionPlain.SQL("CALL my_add_one_procedure(1573);").Execute();
        if (res.HasData)
        {
          var row = res.FetchOne();
          if (row != null)
          {
            do
            {
              if (row[0] != null)
                Assert.That(row[0].ToString(), Is.Not.Null);

              if (row[1] != null)
                Assert.That(row[1].ToString(), Is.Not.Null);

              if (row[2] != null)
                Assert.That(row[2].ToString(), Is.Not.Null);

            } while (res.Next()); while (res.NextResult()) ;
          }
        }
        sessionPlain.SQL("DROP DATABASE DBName").Execute();
      }
    }

    [Test, Description("Stored Procedure Table-StringBuilder and Session")]
    public void StoredProcTablePositiveStringBuilderSession()
    {
      Assume.That(Platform.IsWindows(), "This test is only for Windows OS.");

      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      var connectionStringObject = new { connection = "server=" + sb.Server + ";user=" + sb.UserID + ";port=" + sb.Port + ";password=" + sb.Password };

      using (MySqlConnection mysql = new MySqlConnection(ConnectionStringRoot))
      {
        mysql.Open();
        System.Text.StringBuilder sql = new System.Text.StringBuilder();
        sql.AppendLine("DROP DATABASE IF EXISTS DBName");
        MySqlScript script = new MySqlScript(mysql, sql.ToString());
        script.Execute();
        sql = new System.Text.StringBuilder();
        sql.AppendLine("CREATE DATABASE DBName");
        script = new MySqlScript(mysql, sql.ToString());
        script.Execute();
        sql = new System.Text.StringBuilder();
        sql.AppendLine("USE DBName");
        script = new MySqlScript(mysql, sql.ToString());
        script.Execute();
        sql = new System.Text.StringBuilder();
        sql.AppendLine("CREATE TABLE address" +
                    "(address_number  INT NOT NULL AUTO_INCREMENT, " +
                    "building_name  VARCHAR(100) NOT NULL, " +
                    "district VARCHAR(100) NOT NULL, PRIMARY KEY (address_number)" + ");"
                    );
        script = new MySqlScript(mysql, sql.ToString());
        script.Execute();
        sql = new System.Text.StringBuilder();
        sql.AppendLine("INSERT INTO address" +
                    "(address_number,building_name,district)" +
                    " VALUES " +
                    "(1573,'MySQL','BGL');");
        script = new MySqlScript(mysql, sql.ToString());
        script.Execute();
        sql = new System.Text.StringBuilder();
        sql.AppendLine("INSERT INTO address" +
                    "(address_number,building_name,district)" +
                    " VALUES " +
                    "(1,'MySQLTest1','BGLTest1');");
        script = new MySqlScript(mysql, sql.ToString());
        script.Execute();
        sql = new System.Text.StringBuilder();
        sql.AppendLine("INSERT INTO address" +
                    "(address_number,building_name,district)" +
                    " VALUES " +
                    "(2,'MySQLTest2','BGLTest2');");
        script = new MySqlScript(mysql, sql.ToString());
        script.Execute();
        sql = new System.Text.StringBuilder();
        sql.AppendLine("DELIMITER //");
        sql.AppendLine("CREATE PROCEDURE my_add_one_procedure " +
                    " (IN address_id INT) " +
                    "BEGIN " +
                    "select * from address as a where a.address_number = address_id;" +
                    "END//");
        script = new MySqlScript(mysql, sql.ToString());
        script.Execute();
        sql = new System.Text.StringBuilder();
        sql.AppendLine("DELIMITER ;");
        script = new MySqlScript(mysql, sql.ToString());
        script.Execute();
      }

      using (Session sessionPlain = MySQLX.GetSession(connectionStringObject.connection))
      {
        sessionPlain.SQL("USE DBName").Execute();
        var res = sessionPlain.SQL("CALL my_add_one_procedure(1573);").Execute();
        if (res.HasData)
        {
          var row = res.FetchOne();
          if (row != null)
          {
            do
            {
              if (row[0] != null)
                Assert.That(row[0].ToString(), Is.Not.Null);

              if (row[1] != null)
                Assert.That(row[1].ToString(), Is.Not.Null);

              if (row[2] != null)
                Assert.That(row[2].ToString(), Is.Not.Null);

            } while (res.Next()); while (res.NextResult()) ;
          }
        }
        sessionPlain.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        sessionPlain.SQL("DROP TABLE address;").Execute();
        sessionPlain.SQL("DROP DATABASE DBName;").Execute();
      }
    }

    [Test, Description("Stored Procedure Table-Negative(procedure returns null)")]
    public void StoredProcReturnsNull()
    {
      Assume.That(Platform.IsWindows(), "This test is only for Windows OS.");

      MySqlConnection mysql = new MySqlConnection(ConnectionStringRoot);
      mysql.Open();
      System.Text.StringBuilder sql = new System.Text.StringBuilder();
      sql.AppendLine("DROP DATABASE IF EXISTS DBName");
      MySqlScript script = new MySqlScript(mysql, sql.ToString());
      script.Execute();
      sql = new System.Text.StringBuilder();
      sql.AppendLine("CREATE DATABASE DBName");
      script = new MySqlScript(mysql, sql.ToString());
      script.Execute();
      sql = new System.Text.StringBuilder();
      sql.AppendLine("USE DBName");
      script = new MySqlScript(mysql, sql.ToString());
      script.Execute();
      sql = new System.Text.StringBuilder();
      sql.AppendLine("CREATE TABLE address" +
                  "(address_number  INT NOT NULL AUTO_INCREMENT, " +
                  "building_name  VARCHAR(100) NOT NULL, " +
                  "district VARCHAR(100) NOT NULL, PRIMARY KEY (address_number)" + ");"
                  );
      script = new MySqlScript(mysql, sql.ToString());
      script.Execute();
      sql = new System.Text.StringBuilder();
      sql.AppendLine("INSERT INTO address" +
                  "(address_number,building_name,district)" +
                  " VALUES " +
                  "(1573,'MySQL','BGL');");
      script = new MySqlScript(mysql, sql.ToString());
      script.Execute();
      sql = new System.Text.StringBuilder();
      sql.AppendLine("INSERT INTO address" +
                  "(address_number,building_name,district)" +
                  " VALUES " +
                  "(1,'MySQLTest1','BGLTest1');");
      script = new MySqlScript(mysql, sql.ToString());
      script.Execute();
      sql = new System.Text.StringBuilder();
      sql.AppendLine("INSERT INTO address" +
                  "(address_number,building_name,district)" +
                  " VALUES " +
                  "(2,'MySQLTest2','BGLTest2');");
      script = new MySqlScript(mysql, sql.ToString());
      script.Execute();
      sql = new System.Text.StringBuilder();
      sql.AppendLine("DELIMITER //");
      sql.AppendLine("CREATE PROCEDURE my_add_one_procedure " +
                  " (IN address_id INT) " +
                  "BEGIN " +
                  "select * from address as a where a.address_number = address_id;" +
                  "END//");
      script = new MySqlScript(mysql, sql.ToString());
      script.Execute();
      sql = new System.Text.StringBuilder();
      sql.AppendLine("DELIMITER ;");
      script = new MySqlScript(mysql, sql.ToString());
      script.Execute();

      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      var connectionStringObject = new { connection = "server=" + sb.Server + ";user=" + sb.UserID + ";port=" + sb.Port + ";password=" + sb.Password + ";sslmode=" + MySqlSslMode.Required + ";" };
      using (Session sessionPlain = MySQLX.GetSession(connectionStringObject.connection))
      {
        sessionPlain.SQL("USE DBName").Execute();

        var res = sessionPlain.SQL("CALL my_add_one_procedure(1000);").Execute();
        if (res.HasData)
        {
          var row = res.FetchOne();
          Assert.That(row, Is.Null);
        }
        res.Next();
        res.NextResult();
      }
    }

    //SQLTests
    [Test, Description("Bind Support for Session SQL Numeric Datatypes- integer,JSON,tinyint,smallint,mediumint,bigint,float,double,decimal")]
    public void BindSupportSessionSQLNumericDatatypes()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      var connectionString = ConnectionString + ";sslmode=" + MySqlSslMode.Required;
      SqlResult myResult;
      Row row;

      //integer
      using (Session sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("DROP DATABASE IF EXISTS  DBName ").Execute();
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param INT) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param + 1;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind(10).Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //JSON
      using (var sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param Json) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        var jsonParams = "{ \"pages1\" : 30, \"pages2\" : 40 }";
        sessionTest.SQL("SET @my_var = ?;").Bind(jsonParams).Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //TINYINT
      using (var sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param TINYINT) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param + 1;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind(1).Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //SMALLINT
      using (var sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param SMALLINT) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param + 1;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind(11111).Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //MEDIUMINT
      using (var sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param MEDIUMINT) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param + 1;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind(1111).Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //FLOATMD
      using (var sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param FLOAT(10,2)) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param + 1;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind(100.2).Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //DOUBLEMD
      using (var sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param DOUBLE(10,2)) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param + 1;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind(1000.2).Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //DECIMALMD
      using (var sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param DECIMAL(10,2)) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param + 1;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind(10000.2).Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }
    }


    [Test, Description("Bind Support for Session SQL DateTime Types- date,datetime,timestamp,time,year(M)")]
    public void BindSupportSessionSQLDateTimetypes()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      //DATE
      string connectionString = ConnectionString + ";sslmode=" + MySqlSslMode.Required;
      SqlResult myResult;
      Row row;

      using (Session sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("DROP DATABASE IF EXISTS  DBName ").Execute();
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param DATE) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param ;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind("1973-12-30").Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //DATETIME
      using (Session sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param DATETIME) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param ;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind("1981-04-10 15:30:00").Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //TIMESTAMP
      using (Session sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param TIMESTAMP) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param ;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind("20160316153000").Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //TIME
      using (Session sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param TIME) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param ;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind("12:00:00").Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //YEAR
      using (Session sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param YEAR(4)) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param ;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind("2111").Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }
    }


    [Test, Description("Bind Support for Session SQL String Types- CHAR(M),VARCHAR(M),BLOB,TINYBLOB,MEDIUMBLOB,LONGBLOB,ENUM")]
    public void BindSupportSessionSQLStringtypes()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      //CHAR(20)
      string connectionString = ConnectionString + ";sslmode=" + MySqlSslMode.Required;
      SqlResult myResult;
      Row row;

      using (Session sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("DROP DATABASE IF EXISTS  DBName ").Execute();
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param CHAR(20)) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind("ABCDEFGHIJABCDEFGHIJ").Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //VARCHAR(20)
      using (Session sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param VARCHAR(20)) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind("ABCDEFGHIJABCDEFGHIJ").Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //BLOB
      using (Session sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param BLOB) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param ;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind(19731230153000).Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //TINYBLOB
      using (Session sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param TINYBLOB) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param ;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind("12:00:00").Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //MEDIUMBLOB
      using (Session sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param MEDIUMBLOB) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param ;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind(2111).Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //LONGBLOB
      using (Session sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param LONGBLOB) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param ;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind(111232).Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }

      //ENUM
      using (Session sessionTest = MySQLX.GetSession(connectionString))
      {
        sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
        sessionTest.SQL("USE DBName").Execute();
        sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                    " (INOUT incr_param ENUM('x-small', 'small', 'medium', 'large', 'x-large')) " +
                    "BEGIN " +
                    "  SET incr_param = incr_param ;" +
                    "END;").Execute();
        //Uncomment once Bind is implemented in 7.0.2
        sessionTest.SQL("SET @my_var = ?;").Bind("large").Execute();
        sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
        sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
        // Use a SQL query to get the result
        myResult = sessionTest.SQL("SELECT @my_var").Execute();
        // Gets the row and prints the first column
        row = myResult.FetchOne();
        Assert.That(row[0].ToString(), Is.Not.Null);
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }
    }

    [Test, Description("Bind Support for Session SQL Negative Tests-Null")]
    public void BindSupportSessionSQLNegativeTest1()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      SqlResult myResult = null;

      //integer
      string connectionString = ConnectionString + ";sslmode=" + MySqlSslMode.Required;
      Session sessionTest = MySQLX.GetSession(connectionString);
      sessionTest.SQL("DROP DATABASE IF EXISTS  DBName ").Execute();
      sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
      sessionTest.SQL("USE DBName").Execute();
      sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                  " (INOUT incr_param INT) " +
                  "BEGIN " +
                  "  SET incr_param = incr_param+1;" +
                  "END;").Execute();

      sessionTest.SQL("SET @my_var = ?;").Bind(null).Execute();
      sessionTest.SQL("CALL my_add_one_procedure(@my_var);").Execute();
      sessionTest.SQL("DROP PROCEDURE my_add_one_procedure;").Execute();
      // Use a SQL query to get the result
      myResult = sessionTest.SQL("SELECT @my_var").Execute();

      Assert.Throws<InvalidProtocolBufferException>(() => myResult.FetchOne());
      sessionTest.SQL("DROP DATABASE DBName").Execute();
      sessionTest.Close();
      sessionTest.Dispose();
    }

    [Test, Description("Bind Support for Session SQL Negative Tests-Bind Chaining Tests")]
    public void BindSupportSessionSQLNegativeTest2()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");

      //integer
      string connectionString = ConnectionString + ";sslmode=" + MySqlSslMode.Required;
      Session sessionTest = MySQLX.GetSession(connectionString);
      sessionTest.SQL("DROP DATABASE IF EXISTS  DBName ").Execute();
      sessionTest.SQL("CREATE DATABASE IF NOT EXISTS DBName").Execute();
      sessionTest.SQL("USE DBName").Execute();
      sessionTest.SQL("CREATE PROCEDURE my_add_one_procedure " +
                  " (INOUT incr_param INT) " +
                  "BEGIN " +
                  "  SET incr_param = incr_param + 1;" +
                  "END;").Execute();
      sessionTest.SQL("CREATE TABLE DBName.test(id INT, letter varchar(1))").Execute();

      for (int i = 1; i <= 100; i++)

        session.SQL("INSERT INTO DBName.test VALUES (?, ?), (?, ?)")
          .Bind(1, "a")
          .Bind(2, "b")
          .Execute();

      SqlResult result = session.SQL("select * from DBName.test where id=?").Bind(5).Execute();

      //Uncomment once Bind is implemented in 7.0.2
      Assert.Throws<MySqlException>(() => session.SQL("SET @my_var = ?;").Bind(1).Bind(2).Execute());
      sessionTest.SQL("DROP DATABASE DBName").Execute();
      sessionTest.Close();
      sessionTest.Dispose();

    }

    // Aditional Tests
    [Test, Description("Test MySQLX plugin MySQL mixed scenario")]
    public void MixedChainedCommands()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      ExecuteSQL("Drop table if exists test");
      ExecuteSQL("CREATE TABLE test(c1 float(14,8),c2 double GENERATED ALWAYS AS (c1*101/102) Stored COMMENT 'First Gen Col')");
      ExecuteSQL("INSERT INTO test(c1) VALUES (22.7)");
      ExecuteSQL("INSERT INTO test(c1) VALUES (-100000.38984)");
      ExecuteSQL("INSERT INTO test(c1) VALUES (0)");

      RowResult r = session.GetSchema("test").GetTable("test").Select("c1").Execute();
      var rows = r.FetchAll();
      Assert.That(r.Columns.Count, Is.EqualTo(1), "Matching");
      Assert.That(r.Columns[0].ClrType.ToString(), Is.EqualTo(typeof(float).ToString()), "Matching");
      Assert.That(r.Columns[0].Type.ToString(), Is.EqualTo(MySqlDbType.Float.ToString()), "Matching");
      Assert.That((int)r.Columns[0].Length, Is.EqualTo(14), "Matching");
      Assert.That((int)r.Columns[0].FractionalDigits, Is.EqualTo(8), "Matching");
      Assert.That(rows.Count, Is.EqualTo(3), "Matching");
      Assert.That((float)rows[0][0], Is.EqualTo(22.7f), "Matching");
      Assert.That((float)rows[1][0], Is.EqualTo(-100000.38984f), "Matching");
      Assert.That((float)rows[2][0], Is.EqualTo(0f), "Matching");
    }

    [Test, Description("Test MySQLX plugin MySQL Date Time Bug")]
    public void DateTimeCheck()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      ExecuteSQL("CREATE TABLE test.test1212(dt DATETIME(6))");
      ExecuteSQL("INSERT INTO test.test1212 VALUES('2015-10-21 18:01:00.12345678')");

      RowResult r = session.GetSchema("test").GetTable("test1212").Select("dt").Execute();
      var rows = r.FetchAll();
      Assert.That(r.Columns.Count, Is.EqualTo(1), "Matching Coulumn Count");

    }

    [Test, Description("Test MySQLX plugin MySQL Datetime JSON")]
    public void DateTimeJSON()
    {
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      ExecuteSQL("DROP TABLE IF EXISTS test.test");
      ExecuteSQL("CREATE TABLE test.test(Id int NOT NULL PRIMARY KEY, jsoncolumn JSON)");
      ExecuteSQL(@"INSERT INTO test.test VALUES(100000,' { ""name"" : ""bob"",""Date"": ""2015-10-09"",""Time"": ""12:18:29.000000"",""DateTimeOfRegistration"": ""2015-10-09 12:18:29.000000"",""age"":12} ')");
      RowResult r = session.GetSchema("test").GetTable("test").Select("jsoncolumn").Execute();
      var rows = r.FetchAll();
      Assert.That(r.Columns.Count, Is.EqualTo(1), "Matching");
    }

    [Test, Description("Test MySQLX plugin JSON Variant")]
    public void JSONVariant()
    {
      Assume.That(Platform.IsWindows(), "This test is only for Windows OS.");
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE test (Id int NOT NULL PRIMARY KEY, jsoncolumn JSON)");
      ExecuteSQL("INSERT INTO test VALUES (1, '[1]')");
      ExecuteSQL(@"INSERT INTO test VALUES (2, '[""a"", {""b"": [true, false]}, [10, 20]]')");
      ExecuteSQL(@"INSERT INTO test VALUES (3, '{""id"":1,""name"":""test""}')");
      var r = ExecuteSQL(@"SELECT JSON_EXTRACT('{""id"": 1, ""name"": ""test""}','$.name')").FetchOne();
      Assert.That(r[0], Is.EqualTo("\"test\""));
    }

    [Test, Description("Test MySQLX plugin big int as PK")]
    public void BigIntasPK()
    {
      Assume.That(Platform.IsWindows(), "This test is only for Windows OS.");
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");

      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE test (Id bigint NOT NULL PRIMARY KEY, jsoncolumn JSON)");
      var res = ExecuteSQL("INSERT INTO test VALUES (934157136952, '[1]')");
      Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
      res = ExecuteSQL(@"INSERT INTO test VALUES (9223372036854775807, '[""a"", {""b"": [true, false]}, [10, 20]]')");
      Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
      Assert.Throws<MySqlException>(() => ExecuteSQL("INSERT INTO test VALUES ('str1', '[1]')"));
    }

    [Test, Description("Test MySQLX plugin tiny int as PK")]
    public void TinyIntasPK()
    {
      Assume.That(Platform.IsWindows(), "This test is only for Windows OS.");
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");

      using (var ss = MySQLX.GetSession(ConnectionString))
      {
        ss.SQL("DROP TABLE IF EXISTS test.test").Execute();
        ss.SQL("CREATE TABLE test.test (Id tinyint NOT NULL PRIMARY KEY, jsoncolumn JSON)").Execute();
        var res = ss.SQL("INSERT INTO test.test VALUES (1, '[1]')").Execute();
        Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
        res = ss.SQL(@"INSERT INTO test.test VALUES (2, '[""a"", {""b"": [true, false]}, [10, 20]]')").Execute();
        Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
        Assert.Throws<MySqlException>(() => ss.SQL("INSERT INTO test.test VALUES ('str1', '[1]')").Execute());
      }
    }

    [Test, Description("Test MySQLX plugin small int as PK")]
    public void SmallIntasPK()
    {
      Assume.That(Platform.IsWindows(), "This test is only for Windows OS.");
      Assume.That(session.Version.isAtLeast(5, 7, 0), "This test is for MySql 5.7 or higher");

      using (var ss = MySQLX.GetSession(ConnectionString))
      {
        ss.SQL("DROP TABLE IF EXISTS test.test").Execute();
        ss.SQL("CREATE TABLE test.test (Id smallint NOT NULL PRIMARY KEY, jsoncolumn JSON)").Execute();
        var res = ss.SQL("INSERT INTO test.test VALUES (99, '[1]')").Execute();
        Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
        res = ss.SQL(@"INSERT INTO test.test VALUES (1, '[""a"", {""b"": [true, false]}, [10, 20]]')").Execute();
        Assert.That(res.AffectedItemsCount, Is.EqualTo(1));
        Assert.Throws<MySqlException>(() => ss.SQL("INSERT INTO test.test VALUES ('str1', '[1]')").Execute());
        ss.SQL("DROP TABLE IF EXISTS test.test");
      }
    }

    [Test, Description("MySQL sample data insertion and viewing")]
    public void DataValidation()
    {
      ExecuteSQL("DROP TABLE IF EXISTS TEST");
      ExecuteSQL("CREATE TABLE TEST(name VARCHAR(20),id INT NOT NULL,sports VARCHAR(20))");
      ExecuteSQL("INSERT INTO TEST(name,id,sports) VALUES ('Federer',1,'Tennis')");
      ExecuteSQL("INSERT INTO TEST(name,id,sports) VALUES ('Ronaldo',2,'Soccer')");
      ExecuteSQL("INSERT INTO TEST(name,id,sports) VALUES ('Messi',3,'Soccer')");
      var expecteddataValue = new List<string> { "Federer", "Ronaldo", "Messi" };
      using (SqlResult result = ExecuteSQLStatement(session.SQL("SELECT name FROM TEST;")))
      {
        while (result.Next())
        {
          Assert.That(result.Rows, Has.Exactly(3).Items);
          Assert.That(expecteddataValue.Contains(result[0].ToString()));
        }
      }
    }
    #endregion
  }
}
