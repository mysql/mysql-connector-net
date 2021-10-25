// Copyright (c) 2015, 2021, Oracle and/or its affiliates.
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

using Google.Protobuf;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
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
        Assert.True(r.Next());
        Assert.AreEqual(1, r[0]);
        Assert.False(r.NextResult());
      }

    }

    [Test]
    public void ExecuteStoredProcedure()
    {
      ExecuteSQL("CREATE PROCEDURE `my_proc` () BEGIN SELECT 5; END");

      Session session = GetSession(true);
      var result = ExecuteSQLStatement(session.SQL("CALL my_proc()"));
      Assert.True(result.HasData);
      var row = result.FetchOne();
      Assert.NotNull(row);
      Assert.AreEqual((sbyte)5, row[0]);
      Assert.False(result.Next());
      Assert.Null(result.FetchOne());
      Assert.False(result.NextResult());
    }

    [Test]
    public void ExecuteStoredProcedureMultipleResults()
    {
      ExecuteSQL("drop procedure if exists my_proc");
      ExecuteSQL("CREATE PROCEDURE `my_proc` () BEGIN SELECT 5; SELECT 'A'; SELECT 5 * 2; END");

      Session session = GetSession(true);
      var result = ExecuteSQLStatement(session.SQL("CALL my_proc()"));
      Assert.True(result.HasData);
      var row = result.FetchOne();
      Assert.NotNull(row);
      Assert.AreEqual((sbyte)5, row[0]);
      Assert.False(result.Next());
      Assert.Null(result.FetchOne());

      Assert.True(result.NextResult());
      row = result.FetchOne();
      Assert.NotNull(row);
      Assert.AreEqual("A", row[0]);
      Assert.False(result.Next());
      Assert.Null(result.FetchOne());

      Assert.True(result.NextResult());
      row = result.FetchOne();
      Assert.NotNull(row);
      Assert.AreEqual((sbyte)10, row[0]);
      Assert.False(result.Next());
      Assert.Null(result.FetchOne());

      Assert.False(result.NextResult());
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
      Assert.True(result.Next());
      Assert.That(result.Rows, Has.One.Items);
      Assert.AreEqual(5, result[0]);
      Assert.AreEqual("E", result[1]);
    }

    [Test]
    public void BindNull()
    {
      ExecuteSQL("drop table if exists test.test");
      ExecuteSQL("CREATE TABLE test.test(id INT, letter varchar(1))");

      var session = GetSession(true);
      var result = ExecuteSQLStatement(session.SQL("INSERT INTO test.test VALUES(1, ?), (2, 'B');").Bind(null));
      Assert.AreEqual(2ul, result.AffectedItemsCount);

      var sqlResult = ExecuteSQLStatement(session.SQL("SELECT * FROM test.test WHERE letter is ?").Bind(null)).FetchAll();
      Assert.That(sqlResult, Has.One.Items);
      Assert.AreEqual(1, sqlResult[0][0]);
      Assert.Null(sqlResult[0][1]);
    }

    [Test]
    public void Alias()
    {
      var session = GetSession(true);
      var stmt = ExecuteSQLStatement(session.SQL("SELECT 1 AS UNO"));
      var result = stmt.FetchAll();
      Assert.AreEqual("UNO", stmt.Columns[0].ColumnLabel);
    }

    #region WL14389

    [Test, Description("call after failed procedure")]
    public void ProcedureWithNoTable()
    {
      ExecuteSQL("create procedure newproc (in p1 int,in p2 char(20)) begin select 1; select 'XXX' from notab; end;");

      var session = MySQLX.GetSession(ConnectionString + ";database=test;");


      var sqlRes = session.SQL("call newproc(?, ?)").Bind(10).Bind("X").Execute();
      var ex = Assert.Throws<MySqlException>(() => session.SQL("drop procedure if exists newproc ").Execute());
      StringAssert.AreEqualIgnoringCase("Table 'test.notab' doesn't exist", ex.Message);
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
                Assert.IsNotNull(row[0].ToString());

              if (row[1] != null)
                Assert.IsNotNull(row[1].ToString());

              if (row[2] != null)
                Assert.IsNotNull(row[2].ToString());

            } while (res.Next()); while (res.NextResult()) ;
          }
        }
        sessionPlain.SQL("DROP DATABASE DBName").Execute();
      }
    }

    [Test, Description("Stored Procedure Table-StringBuilder and Session")]
    public void StoredProcTablePositiveStringBuilderSession()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");

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
                Assert.IsNotNull(row[0].ToString());

              if (row[1] != null)
                Assert.IsNotNull(row[1].ToString());

              if (row[2] != null)
                Assert.IsNotNull(row[2].ToString());

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
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");

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
          Assert.IsNull(row);
        }
        res.Next();
        res.NextResult();
      }
    }

    //SQLTests
    [Test, Description("Bind Support for Session SQL Numeric Datatypes- integer,JSON,tinyint,smallint,mediumint,bigint,float,double,decimal")]
    public void BindSupportSessionSQLNumericDatatypes()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }
    }


    [Test, Description("Bind Support for Session SQL DateTime Types- date,datetime,timestamp,time,year(M)")]
    public void BindSupportSessionSQLDateTimetypes()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }
    }


    [Test, Description("Bind Support for Session SQL String Types- CHAR(M),VARCHAR(M),BLOB,TINYBLOB,MEDIUMBLOB,LONGBLOB,ENUM")]
    public void BindSupportSessionSQLStringtypes()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
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
        Assert.IsNotNull(row[0].ToString());
        sessionTest.SQL("DROP DATABASE DBName").Execute();
      }
    }

    [Test, Description("Bind Support for Session SQL Negative Tests-Null")]
    public void BindSupportSessionSQLNegativeTest1()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");
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
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");

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
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");
      ExecuteSQL("Drop table if exists test");
      ExecuteSQL("CREATE TABLE test(c1 float(14,8),c2 double GENERATED ALWAYS AS (c1*101/102) Stored COMMENT 'First Gen Col')");
      ExecuteSQL("INSERT INTO test(c1) VALUES (22.7)");
      ExecuteSQL("INSERT INTO test(c1) VALUES (-100000.38984)");
      ExecuteSQL("INSERT INTO test(c1) VALUES (0)");

      RowResult r = session.GetSchema("test").GetTable("test").Select("c1").Execute();
      var rows = r.FetchAll();
      Assert.AreEqual(1, r.Columns.Count, "Matching");
      Assert.AreEqual(typeof(float).ToString(), r.Columns[0].ClrType.ToString(), "Matching");
      Assert.AreEqual(MySqlDbType.Float.ToString(), r.Columns[0].Type.ToString(), "Matching");
      Assert.AreEqual(14, (int)r.Columns[0].Length, "Matching");
      Assert.AreEqual(8, (int)r.Columns[0].FractionalDigits, "Matching");
      Assert.AreEqual(3, rows.Count, "Matching");
      Assert.AreEqual(22.7f, (float)rows[0][0], "Matching");
      Assert.AreEqual(-100000.38984f, (float)rows[1][0], "Matching");
      Assert.AreEqual(0f, (float)rows[2][0], "Matching");
    }

    [Test, Description("Test MySQLX plugin MySQL Date Time Bug")]
    public void DateTimeCheck()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");
      ExecuteSQL("CREATE TABLE test.test1212(dt DATETIME(6))");
      ExecuteSQL("INSERT INTO test.test1212 VALUES('2015-10-21 18:01:00.12345678')");

      RowResult r = session.GetSchema("test").GetTable("test1212").Select("dt").Execute();
      var rows = r.FetchAll();
      Assert.AreEqual(1, r.Columns.Count, "Matching Coulumn Count");

    }

    [Test, Description("Test MySQLX plugin MySQL Datetime JSON")]
    public void DateTimeJSON()
    {
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");
      ExecuteSQL("DROP TABLE IF EXISTS test.test");
      ExecuteSQL("CREATE TABLE test.test(Id int NOT NULL PRIMARY KEY, jsoncolumn JSON)");
      ExecuteSQL(@"INSERT INTO test.test VALUES(100000,' { ""name"" : ""bob"",""Date"": ""2015-10-09"",""Time"": ""12:18:29.000000"",""DateTimeOfRegistration"": ""2015-10-09 12:18:29.000000"",""age"":12} ')");
      RowResult r = session.GetSchema("test").GetTable("test").Select("jsoncolumn").Execute();
      var rows = r.FetchAll();
      Assert.AreEqual(1, r.Columns.Count, "Matching");
    }

    [Test, Description("Test MySQLX plugin JSON Variant")]
    public void JSONVariant()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE test (Id int NOT NULL PRIMARY KEY, jsoncolumn JSON)");
      ExecuteSQL("INSERT INTO test VALUES (1, '[1]')");
      ExecuteSQL(@"INSERT INTO test VALUES (2, '[""a"", {""b"": [true, false]}, [10, 20]]')");
      ExecuteSQL(@"INSERT INTO test VALUES (3, '{""id"":1,""name"":""test""}')");
      var r = ExecuteSQL(@"SELECT JSON_EXTRACT('{""id"": 1, ""name"": ""test""}','$.name')").FetchOne();
      Assert.AreEqual("\"test\"", r[0]);
    }

    [Test, Description("Test MySQLX plugin big int as PK")]
    public void BigIntasPK()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");

      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE test (Id bigint NOT NULL PRIMARY KEY, jsoncolumn JSON)");
      var res = ExecuteSQL("INSERT INTO test VALUES (934157136952, '[1]')");
      Assert.AreEqual(1, res.AffectedItemsCount);
      res = ExecuteSQL(@"INSERT INTO test VALUES (9223372036854775807, '[""a"", {""b"": [true, false]}, [10, 20]]')");
      Assert.AreEqual(1, res.AffectedItemsCount);
      Assert.Throws<MySqlException>(() => ExecuteSQL("INSERT INTO test VALUES ('str1', '[1]')"));
    }

    [Test, Description("Test MySQLX plugin tiny int as PK")]
    public void TinyIntasPK()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");

      using (var ss = MySQLX.GetSession(ConnectionString))
      {
        ss.SQL("DROP TABLE IF EXISTS test.test").Execute();
        ss.SQL("CREATE TABLE test.test (Id tinyint NOT NULL PRIMARY KEY, jsoncolumn JSON)").Execute();
        var res = ss.SQL("INSERT INTO test.test VALUES (1, '[1]')").Execute();
        Assert.AreEqual(1, res.AffectedItemsCount);
        res = ss.SQL(@"INSERT INTO test.test VALUES (2, '[""a"", {""b"": [true, false]}, [10, 20]]')").Execute();
        Assert.AreEqual(1, res.AffectedItemsCount);
        Assert.Throws<MySqlException>(() => ss.SQL("INSERT INTO test.test VALUES ('str1', '[1]')").Execute());
      }
    }

    [Test, Description("Test MySQLX plugin small int as PK")]
    public void SmallIntasPK()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");

      using (var ss = MySQLX.GetSession(ConnectionString))
      {
        ss.SQL("DROP TABLE IF EXISTS test.test").Execute();
        ss.SQL("CREATE TABLE test.test (Id smallint NOT NULL PRIMARY KEY, jsoncolumn JSON)").Execute();
        var res = ss.SQL("INSERT INTO test.test VALUES (99, '[1]')").Execute();
        Assert.AreEqual(1, res.AffectedItemsCount);
        res = ss.SQL(@"INSERT INTO test.test VALUES (1, '[""a"", {""b"": [true, false]}, [10, 20]]')").Execute();
        Assert.AreEqual(1, res.AffectedItemsCount);
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
          Assert.True(expecteddataValue.Contains(result[0].ToString()));
        }
      }
    }
    #endregion
  }
}
