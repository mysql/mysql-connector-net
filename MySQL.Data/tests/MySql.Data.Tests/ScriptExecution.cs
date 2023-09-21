// Copyright (c) 2013, 2021, Oracle and/or its affiliates.
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
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MySql.Data.MySqlClient.Tests
{
  public class ScriptExecution : TestBase
  {
    private int statementCount;

    private string statementTemplate1 = @"CREATE PROCEDURE `spTest1-{0}`() NOT DETERMINISTIC
          CONTAINS SQL SQL SECURITY DEFINER COMMENT '' 
          BEGIN
            SELECT 1,2,3;
          END{1}";

    [Test]
    public void ExecuteScriptWithProcedures()
    {
      statementCount = 0;
      string scriptText = String.Empty;
      for (int i = 0; i < 10; i++)
      {
        scriptText += String.Format(statementTemplate1, i, "$$");
      }
      MySqlScript script = new MySqlScript(scriptText);
      script.StatementExecuted += new MySqlStatementExecutedEventHandler(ExecuteScriptWithProcedures_QueryExecuted);
      script.Connection = Connection;
      script.Delimiter = "$$";
      int count = script.Execute();
      Assert.AreEqual(10, count);

      MySqlCommand cmd = new MySqlCommand(
        String.Format(@"SELECT COUNT(*) FROM information_schema.routines WHERE
        routine_schema = '{0}' AND routine_name LIKE 'spTest1-%'",
        Connection.Database), Connection);
      Assert.AreEqual(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    void ExecuteScriptWithProcedures_QueryExecuted(object sender, MySqlScriptEventArgs e)
    {
      string stmt = String.Format(statementTemplate1, statementCount++, null);
      Assert.AreEqual(stmt, e.StatementText);
    }

    private string statementTemplate2 = @"INSERT INTO Test (id, name) VALUES ({0}, 'a "" na;me'){1}";

    [Test]
    public void ExecuteScriptWithInserts()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
      statementCount = 0;
      string scriptText = String.Empty;
      for (int i = 0; i < 10; i++)
      {
        scriptText += String.Format(statementTemplate2, i, ";");
      }
      MySqlScript script = new MySqlScript(scriptText);
      script.Connection = Connection;
      script.StatementExecuted += new MySqlStatementExecutedEventHandler(ExecuteScriptWithInserts_StatementExecuted);
      int count = script.Execute();
      Assert.AreEqual(10, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Assert.AreEqual(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    void ExecuteScriptWithInserts_StatementExecuted(object sender, MySqlScriptEventArgs e)
    {
      string stmt = String.Format(statementTemplate2, statementCount++, null);
      Assert.AreEqual(stmt, e.StatementText);
    }

    [Test]
    [Ignore("Fix this")]
    public void ExecuteScriptContinueOnFailure()
    {
      statementCount = 0;
      string scriptText = String.Empty;
      for (int i = 0; i < 5; i++)
        scriptText += String.Format(statementTemplate2, i, ";");
      scriptText += "bogus statement;";
      for (int i = 5; i < 10; i++)
        scriptText += String.Format(statementTemplate2, i, ";");
      MySqlScript script = new MySqlScript(scriptText);
      script.Connection = Connection;
      script.Error += new MySqlScriptErrorEventHandler(ExecuteScript_ContinueOnError);
      int count = script.Execute();
      Assert.AreEqual((int)10, count);
      Assert.AreEqual((int)1, statementCount);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Assert.AreEqual(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    void ExecuteScript_ContinueOnError(object sender, MySqlScriptErrorEventArgs args)
    {
      args.Ignore = true;
      statementCount++;
    }

    [Test]
    [Ignore("Fix this")]
    public void ExecuteScriptNotContinueOnFailure()
    {
      statementCount = 0;
      string scriptText = String.Empty;
      for (int i = 0; i < 5; i++)
        scriptText += String.Format(statementTemplate2, i, ";");
      scriptText += "bogus statement;";
      for (int i = 5; i < 10; i++)
        scriptText += String.Format(statementTemplate2, i, ";");
      MySqlScript script = new MySqlScript(scriptText);
      script.Connection = Connection;
      script.Error += new MySqlScriptErrorEventHandler(ExecuteScript_NotContinueOnError);
      int count = script.Execute();
      Assert.AreEqual(5, count);
      Assert.AreEqual(1, statementCount);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Assert.AreEqual(5, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    void ExecuteScript_NotContinueOnError(object sender, MySqlScriptErrorEventArgs args)
    {
      args.Ignore = false;
      statementCount++;
    }

    [Test]
    public void ExecuteScriptWithUserVariables()
    {
      string connStr = Connection.ConnectionString.ToLowerInvariant();
      connStr = connStr.Replace("allow user variables=true",
        "allow user variables=false");
      using (MySqlConnection c = new MySqlConnection(connStr))
      {
        c.Open();
        string scriptText = "SET @myvar = 1";
        MySqlScript script = new MySqlScript(scriptText);
        script.Connection = c;
        int count = script.Execute();
        Assert.AreEqual(1, count);
      }
    }

    /// <summary>
    /// Bug #46429 use DELIMITER command in MySql.Data.MySqlClient.MySqlScript  
    /// </summary>
    [Test]
    [Ignore("Fix for non-windows OS")]
    public void ScriptWithDelimiterStatements()
    {
      StringBuilder sql = new StringBuilder();

      sql.AppendFormat(@"{0}DELIMITER $${0}
        SELECT 1,2,3$${0}
        DELIMITER //{0}
        SELECT 4,5,6//{0}", Environment.NewLine);

      MySqlScript s = new MySqlScript();
      s.Query = sql.ToString();
      s.Delimiter = "XX";
      s.Connection = Connection;
      int count = s.Execute();
    }

    /// <summary>
    /// Bug #46429	use DELIMITER command in MySql.Data.MySqlClient.MySqlScript
    /// </summary>
    [Test]
    public void DelimiterInScriptV2()
    {
      StringBuilder sql = new StringBuilder();

      sql.AppendLine("DELIMITER MySuperDelimiter");
      sql.AppendLine("CREATE PROCEDURE TestProcedure1()");
      sql.AppendLine("BEGIN");
      sql.AppendLine("  SELECT * FROM mysql.proc;");
      sql.AppendLine("END MySuperDelimiter");
      sql.AppendLine("CREATE PROCEDURE TestProcedure2()");
      sql.AppendLine("BEGIN");
      sql.AppendLine("  SELECT * FROM mysql.proc;");
      sql.AppendLine("END mysuperdelimiter");

      sql.AppendLine("DELIMITER ;");

      MySqlScript script = new MySqlScript(Connection, sql.ToString());
      script.Execute();
    }

    /// <summary>
    /// Bug #50344	MySqlScript.Execute() throws InvalidOperationException
    /// </summary>
    [Test]
    public void EmptyLastLineWithScriptExecute()
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine("DROP FUNCTION IF EXISTS `BlaBla`;");
      sb.AppendLine("DELIMITER ;;");
      MySqlScript script = new MySqlScript(Connection, sb.ToString());
      // InvalidOperationException : The CommandText property has not been properly initialized.
      script.Execute();
    }

    [Test]
    public void DelimiterCommandDoesNotThrow()
    {
      MySqlScript script = new MySqlScript(Connection, "DELIMITER ;");
      script.Execute();
    }

    #region Async

    [Test]
    public async Task ExecuteScriptWithProceduresAsync()
    {
      string spTpl = @"CREATE PROCEDURE `SEScriptWithProceduresAsyncSpTest{0}`() NOT DETERMINISTIC CONTAINS SQL SQL SECURITY DEFINER COMMENT ''  BEGIN SELECT 1,2,3; END{1}";
      statementCount = 0;
      string scriptText = String.Empty;
      for (int i = 0; i < 10; i++)
      {
        scriptText += String.Format(spTpl, i, "$$");
      }
      MySqlScript script = new MySqlScript(scriptText);
      script.StatementExecuted += new MySqlStatementExecutedEventHandler(delegate(object sender, MySqlScriptEventArgs e)
      {
        string stmt = String.Format(spTpl, statementCount++, null);
        Assert.AreEqual(stmt, e.StatementText);
      });
      script.Connection = Connection;
      script.Delimiter = "$$";
      int count = await script.ExecuteAsync();
      Assert.AreEqual(10, count);

      MySqlCommand cmd = new MySqlCommand(
        String.Format(@"SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = '{0}' AND routine_name LIKE 'SEScriptWithProceduresAsyncSpTest%'",
        Connection.Database), Connection);
      Assert.AreEqual(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Test]
    public async Task ExecuteScriptWithInsertsAsync()
    {
      ExecuteSQL("CREATE TABLE SEScriptWithInsertsAsyncTest (id int, name varchar(50))");
      string queryTpl = @"INSERT INTO SEScriptWithInsertsAsyncTest (id, name) VALUES ({0}, 'a "" na;me'){1}";
      statementCount = 0;
      string scriptText = String.Empty;
      for (int i = 0; i < 10; i++)
      {
        scriptText += String.Format(queryTpl, i, ";");
      }
      MySqlScript script = new MySqlScript(scriptText);
      script.Connection = Connection;
      script.StatementExecuted += new MySqlStatementExecutedEventHandler(delegate(object sender, MySqlScriptEventArgs e)
      {
        string stmt = String.Format(queryTpl, statementCount++, null);
        Assert.AreEqual(stmt, e.StatementText);
      });
      int count = await script.ExecuteAsync();
      Assert.AreEqual(10, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM SEScriptWithInsertsAsyncTest", Connection);
      Assert.AreEqual(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }
    #endregion

    #region WL14389

    [Test,Description("MySQL Delimiter ran with Automation")]
    public void ScriptDelimiter()
    {
      var sql = "DROP PROCEDURE IF EXISTS test_routine??" +
                "CREATE PROCEDURE test_routine() " +
                "BEGIN " +
                "SELECT 1;" +
                "END??" +
                "CALL test_routine()??";
      using (var conn = new MySqlConnection(Connection.ConnectionString))
      {
        conn.Open();
        var script = new MySqlScript(conn);
        script.Query = sql;
        script.Delimiter = "??";
        var count = script.Execute();
        Assert.AreEqual(3, count);
      }
    }

    [Test, Description("Test Script Async")]
    public async Task ScriptDelimiterAsync()
    {
      using (var conn = new MySqlConnection(Connection.ConnectionString))
      {
        conn.Open();
        var sql = "DROP PROCEDURE IF EXISTS test_routine??" +
                  "CREATE PROCEDURE test_routine() " +
                  "BEGIN " +
                  "SELECT 1;" +
                  "END??" +
                  "CALL test_routine()";
        var script = new MySqlScript(conn);
        script.Query = sql;
        script.Delimiter = "??";
        var count = await script.ExecuteAsync();
      }
    }

    #endregion WL14389

  }
}
