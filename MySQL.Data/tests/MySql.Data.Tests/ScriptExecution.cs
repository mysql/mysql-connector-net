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
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class ScriptExecution : TestBase
  {
    public ScriptExecution(TestFixture fixture) : base (fixture)
    {
    }

    private int statementCount;

    private string statementTemplate1 = @"CREATE PROCEDURE `spTest1-{0}`() NOT DETERMINISTIC
          CONTAINS SQL SQL SECURITY DEFINER COMMENT '' 
          BEGIN
            SELECT 1,2,3;
          END{1}";

    [Fact]
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
      Assert.Equal(10, count);

      MySqlCommand cmd = new MySqlCommand(
        String.Format(@"SELECT COUNT(*) FROM information_schema.routines WHERE
        routine_schema = '{0}' AND routine_name LIKE 'spTest1-%'",
        Connection.Database), Connection);
      Assert.Equal(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    void ExecuteScriptWithProcedures_QueryExecuted(object sender, MySqlScriptEventArgs e)
    {
      string stmt = String.Format(statementTemplate1, statementCount++, null);
      Assert.Equal(stmt, e.StatementText);
    }


    private string statementTemplate2 = @"INSERT INTO Test (id, name) VALUES ({0}, 'a "" na;me'){1}";

    [Fact]
    public void ExecuteScriptWithInserts()
    {
      executeSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
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
      Assert.Equal(10, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Assert.Equal(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    void ExecuteScriptWithInserts_StatementExecuted(object sender, MySqlScriptEventArgs e)
    {
      string stmt = String.Format(statementTemplate2, statementCount++, null);
      Assert.Equal(stmt, e.StatementText);
    }

    [Fact(Skip="Fix This")]
    public void ExecuteScriptContinueOnError()
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
      Assert.Equal((int)10, count);
      Assert.Equal((int)1, statementCount);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Assert.Equal(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    void ExecuteScript_ContinueOnError(object sender, MySqlScriptErrorEventArgs args)
    {
      args.Ignore = true;
      statementCount++;
    }

    [Fact(Skip="Fix This")]
    public void ExecuteScriptNotContinueOnError()
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
      Assert.Equal(5, count);
      Assert.Equal(1, statementCount);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", Connection);
      Assert.Equal(5, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    void ExecuteScript_NotContinueOnError(object sender, MySqlScriptErrorEventArgs args)
    {
      args.Ignore = false;
      statementCount++;
    }

    [Fact]
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
        Assert.Equal(1, count);
      }
    }

    /// <summary>
    /// Bug #46429 use DELIMITER command in MySql.Data.MySqlClient.MySqlScript  
    /// </summary>
    [Fact(Skip = "Fix Me")]
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
    [Fact]
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
    [Fact]
    public void EmptyLastLineWithScriptExecute()
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine("DROP FUNCTION IF EXISTS `BlaBla`;");
      sb.AppendLine("DELIMITER ;;");
      MySqlScript script = new MySqlScript(Connection, sb.ToString());
      // InvalidOperationException : The CommandText property has not been properly initialized.
      script.Execute();
    }

    [Fact]
    public void DelimiterCommandDoesNotThrow()
    {
      MySqlScript script = new MySqlScript(Connection, "DELIMITER ;");
      script.Execute(); 
    }

    #region Async

    [Fact]
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
        Assert.Equal(stmt, e.StatementText);
      });
      script.Connection = Connection;
      script.Delimiter = "$$";
      int count = await script.ExecuteAsync();
      Assert.Equal(10, count);

      MySqlCommand cmd = new MySqlCommand(
        String.Format(@"SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = '{0}' AND routine_name LIKE 'SEScriptWithProceduresAsyncSpTest%'", 
        Connection.Database), Connection);
      Assert.Equal(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Fact]
    public async Task ExecuteScriptWithInsertsAsync()
    {
      executeSQL("CREATE TABLE SEScriptWithInsertsAsyncTest (id int, name varchar(50))");
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
        Assert.Equal(stmt, e.StatementText);
      });
      int count = await script.ExecuteAsync();
      Assert.Equal(10, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM SEScriptWithInsertsAsyncTest", Connection);
      Assert.Equal(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }
    #endregion
  }
}
