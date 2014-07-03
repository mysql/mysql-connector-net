// Copyright © 2013, 2014 Oracle and/or its affiliates. All rights reserved.
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
#if NET_45_OR_GREATER
using System.Threading.Tasks;
#endif
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class ScriptExecution : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;
    
    private int statementCount;
    private string statementTemplate1 = @"CREATE PROCEDURE `spTest{0}`() NOT DETERMINISTIC
          CONTAINS SQL SQL SECURITY DEFINER COMMENT '' 
          BEGIN
            SELECT 1,2,3;
          END{1}";
    
    private string statementTemplate2 = @"INSERT INTO Test (id, name) VALUES ({0}, 'a "" na;me'){1}";

    public void SetFixture(SetUpClass data)
    {
      st = data;
      st.execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");      
    }

    public void Dispose()
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");
      st.execSQL("DROP PROCEDURE IF EXISTS spTest1");
      st.execSQL("DROP PROCEDURE IF EXISTS spTest2");
    }

    [Fact]
    public void ExecuteScriptWithProcedures()
    {
      if (st.Version < new Version(5, 0)) return;

      statementCount = 0;
      string scriptText = String.Empty;
      for (int i = 0; i < 10; i++)
      {
        scriptText += String.Format(statementTemplate1, i, "$$");
      }
      MySqlScript script = new MySqlScript(scriptText);
      script.StatementExecuted += new MySqlStatementExecutedEventHandler(ExecuteScriptWithProcedures_QueryExecuted);
      script.Connection = st.conn;
      script.Delimiter = "$$";
      int count = script.Execute();
      Assert.Equal(10, count);

      MySqlCommand cmd = new MySqlCommand(
        String.Format(@"SELECT COUNT(*) FROM information_schema.routines WHERE
        routine_schema = '{0}' AND routine_name LIKE 'spTest%'",
        st.database0), st.conn);
      Assert.Equal(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    void ExecuteScriptWithProcedures_QueryExecuted(object sender, MySqlScriptEventArgs e)
    {
      string stmt = String.Format(statementTemplate1, statementCount++, null);
      Assert.Equal(stmt, e.StatementText);
    }

    
    [Fact]
    public void ExecuteScriptWithInserts()
    {
      statementCount = 0;
      string scriptText = String.Empty;
      for (int i = 0; i < 10; i++)
      {
        scriptText += String.Format(statementTemplate2, i, ";");
      }
      MySqlScript script = new MySqlScript(scriptText);
      script.Connection = st.conn;
      script.StatementExecuted += new MySqlStatementExecutedEventHandler(ExecuteScriptWithInserts_StatementExecuted);
      int count = script.Execute();
      Assert.Equal(10, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", st.conn);
      Assert.Equal(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    void ExecuteScriptWithInserts_StatementExecuted(object sender, MySqlScriptEventArgs e)
    {
      string stmt = String.Format(statementTemplate2, statementCount++, null);
      Assert.Equal(stmt, e.StatementText);
    }

    [Fact]
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
      script.Connection = st.conn;
      script.Error += new MySqlScriptErrorEventHandler(ExecuteScript_ContinueOnError);
      int count = script.Execute();
      Assert.Equal((int)10, count);
      Assert.Equal((int)1, statementCount);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", st.conn);
      Assert.Equal(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    void ExecuteScript_ContinueOnError(object sender, MySqlScriptErrorEventArgs args)
    {
      args.Ignore = true;
      statementCount++;
    }

    [Fact]
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
      script.Connection = st.conn;
      script.Error += new MySqlScriptErrorEventHandler(ExecuteScript_NotContinueOnError);
      int count = script.Execute();
      Assert.Equal(5, count);
      Assert.Equal(1, statementCount);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", st.conn);
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
      string connStr = st.conn.ConnectionString.ToLowerInvariant();
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
    [Fact]
    public void ScriptWithDelimiterStatements()
    {
      if (st.Version < new Version(5, 0)) return;

      StringBuilder sql = new StringBuilder();

      sql.AppendFormat("{0}DELIMITER $${0}", Environment.NewLine);
      sql.AppendFormat(statementTemplate1, 1, "$$");
      sql.AppendFormat("{0}DELIMITER //{0}", Environment.NewLine);
      sql.AppendFormat(statementTemplate1, 2, "//");

      MySqlScript s = new MySqlScript();
      s.Query = sql.ToString();
      s.Delimiter = "XX";
      s.Connection = st.conn;
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

      MySqlScript script = new MySqlScript(st.conn, sql.ToString());
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
      MySqlScript script = new MySqlScript(st.conn, sb.ToString());
      // InvalidOperationException : The CommandText property has not been properly initialized.
      script.Execute();
    }

    [Fact]
    public void DelimiterCommandDoesNotThrow()
    {
      MySqlScript script = new MySqlScript(st.conn, "DELIMITER ;");
      Assert.DoesNotThrow(delegate { script.Execute(); });
    }

#if NET_45_OR_GREATER
    #region Async
    [Fact]
    public async Task ExecuteScriptWithProceduresAsync()
    {
      if (st.Version < new Version(5, 0)) return;
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
      script.Connection = st.conn;
      script.Delimiter = "$$";
      int count = await script.ExecuteAsync();
      Assert.Equal(10, count);

      MySqlCommand cmd = new MySqlCommand(
        String.Format(@"SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = '{0}' AND routine_name LIKE 'SEScriptWithProceduresAsyncSpTest%'", st.database0), st.conn);
      Assert.Equal(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    [Fact]
    public async Task ExecuteScriptWithInsertsAsync()
    {
      st.execSQL("CREATE TABLE SEScriptWithInsertsAsyncTest (id int, name varchar(50))");
      string queryTpl = @"INSERT INTO SEScriptWithInsertsAsyncTest (id, name) VALUES ({0}, 'a "" na;me'){1}";
      statementCount = 0;
      string scriptText = String.Empty;
      for (int i = 0; i < 10; i++)
      {
        scriptText += String.Format(queryTpl, i, ";");
      }
      MySqlScript script = new MySqlScript(scriptText);
      script.Connection = st.conn;
      script.StatementExecuted += new MySqlStatementExecutedEventHandler(delegate(object sender, MySqlScriptEventArgs e)
      {
        string stmt = String.Format(queryTpl, statementCount++, null);
        Assert.Equal(stmt, e.StatementText);
      });
      int count = await script.ExecuteAsync();
      Assert.Equal(10, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM SEScriptWithInsertsAsyncTest", st.conn);
      Assert.Equal(10, Convert.ToInt32(cmd.ExecuteScalar()));
    }
    #endregion
#endif
  }
}
