// Copyright © 2004, 2011, Oracle and/or its affiliates. All rights reserved.
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
using System.Text;
using System.Data;
using System.IO;
using NUnit.Framework;

namespace MySql.Data.MySqlClient.Tests
{
  [TestFixture]
  public class ScriptExecution : BaseTest
  {
    [SetUp]
    public override void Setup()
    {
      base.Setup();
      execSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(250), PRIMARY KEY(id))");
    }

    private int statementCount;
    private string statementTemplate1 = @"CREATE PROCEDURE `spTest{0}`() NOT DETERMINISTIC
					CONTAINS SQL SQL SECURITY DEFINER COMMENT '' 
					BEGIN
						SELECT 1,2,3;
					END{1}";

    [Test]
    public void ExecuteScriptWithProcedures()
    {
      if (Version < new Version(5, 0)) return;

      statementCount = 0;
      string scriptText = String.Empty;
      for (int i = 0; i < 10; i++)
      {
        scriptText += String.Format(statementTemplate1, i, "$$");
      }
      MySqlScript script = new MySqlScript(scriptText);
      script.StatementExecuted += new MySqlStatementExecutedEventHandler(ExecuteScriptWithProcedures_QueryExecuted);
      script.Connection = conn;
      script.Delimiter = "$$";
      int count = script.Execute();
      Assert.AreEqual(10, count);

      MySqlCommand cmd = new MySqlCommand(
        String.Format(@"SELECT COUNT(*) FROM information_schema.routines WHERE
				routine_schema = '{0}' AND routine_name LIKE 'spTest%'",
        database0), conn);
      Assert.AreEqual(10, cmd.ExecuteScalar());
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
      statementCount = 0;
      string scriptText = String.Empty;
      for (int i = 0; i < 10; i++)
      {
        scriptText += String.Format(statementTemplate2, i, ";");
      }
      MySqlScript script = new MySqlScript(scriptText);
      script.Connection = conn;
      script.StatementExecuted += new MySqlStatementExecutedEventHandler(ExecuteScriptWithInserts_StatementExecuted);
      int count = script.Execute();
      Assert.AreEqual(10, count);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", conn);
      Assert.AreEqual(10, cmd.ExecuteScalar());
    }

    void ExecuteScriptWithInserts_StatementExecuted(object sender, MySqlScriptEventArgs e)
    {
      string stmt = String.Format(statementTemplate2, statementCount++, null);
      Assert.AreEqual(stmt, e.StatementText);
    }

    [Test]
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
      script.Connection = conn;
      script.Error += new MySqlScriptErrorEventHandler(ExecuteScript_ContinueOnError);
      int count = script.Execute();
      Assert.AreEqual(10, count);
      Assert.AreEqual(1, statementCount);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", conn);
      Assert.AreEqual(10, cmd.ExecuteScalar());
    }

    void ExecuteScript_ContinueOnError(object sender, MySqlScriptErrorEventArgs args)
    {
      args.Ignore = true;
      statementCount++;
    }

    [Test]
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
      script.Connection = conn;
      script.Error += new MySqlScriptErrorEventHandler(ExecuteScript_NotContinueOnError);
      int count = script.Execute();
      Assert.AreEqual(5, count);
      Assert.AreEqual(1, statementCount);

      MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Test", conn);
      Assert.AreEqual(5, cmd.ExecuteScalar());
    }

    void ExecuteScript_NotContinueOnError(object sender, MySqlScriptErrorEventArgs args)
    {
      args.Ignore = false;
      statementCount++;
    }

    [Test]
    public void ExecuteScriptWithUserVariables()
    {
      string connStr = conn.ConnectionString.ToLowerInvariant();
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
    public void ScriptWithDelimiterStatements()
    {
      if (Version < new Version(5, 0)) return;

      StringBuilder sql = new StringBuilder();

      sql.AppendFormat("{0}DELIMITER $${0}", Environment.NewLine);
      sql.AppendFormat(statementTemplate1, 1, "$$");
      sql.AppendFormat("{0}DELIMITER //{0}", Environment.NewLine);
      sql.AppendFormat(statementTemplate1, 2, "//");

      MySqlScript s = new MySqlScript();
      s.Query = sql.ToString();
      s.Delimiter = "XX";
      s.Connection = conn;
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

      MySqlScript script = new MySqlScript(conn, sql.ToString());
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
      MySqlScript script = new MySqlScript(conn, sb.ToString());
      // InvalidOperationException : The CommandText property has not been properly initialized.
      script.Execute();
    }

    [Test]
    public void DelimiterCommandDoesNotThrow()
    {
      MySqlScript script = new MySqlScript(conn, "DELIMITER ;");
      Assert.DoesNotThrow(delegate { script.Execute(); });
    }
  }
}
