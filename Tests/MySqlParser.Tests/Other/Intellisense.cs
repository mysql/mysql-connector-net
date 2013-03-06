// Copyright © 2012, Oracle and/or its affiliates. All rights reserved.
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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using MySql.Parser;

namespace MySql.Parser.Tests
{
  /// <summary>
  /// Tests to verify the error reporting interface for Intellisense client.
  /// </summary>
  [TestFixture]
  public class Intellisense
  {
    [Test]
    public void SelectSimpleTableCompletion()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(
        expectedToken == "table_factor" ||
        expectedToken == "simple_table_ref_no_alias_existing");
    }

    [Test]
    public void SelectSimpleTableCompletionWitBeginEnd()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("begin select * from;  end", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(
        expectedToken == "table_factor" ||
        expectedToken == "simple_table_ref_no_alias_existing");
    }

    [Test]
    public void SelectSimpleTableCompletionWitBeginEnd2()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("begin select * from  ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(
        expectedToken == "table_factor" ||
        expectedToken == "simple_table_ref_no_alias_existing");
    }

    [Test]
    public void SelectSimpleTableCompletionWitBeginEnd3()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("begin select * from end", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(
        expectedToken == "table_factor" ||
        expectedToken == "simple_table_ref_no_alias_existing");
    }

    [Test]
    public void SelectJoinTableCompletion()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select facility.Id from facility inner join t2 on true left join ",
        true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(
        expectedToken == "table_factor" ||
        expectedToken == "simple_table_ref_no_alias_existing");
    }

    [Test]
    public void TruncateTableCompletion()
    {
      TestTableExpected("truncate table ");
    }

    [Test]
    public void TruncateTableCompletion2()
    {
      //TestTableExpected("truncate table db.t");
    }

    [Test]
    public void ShowCreateTableCompletion()
    {
      TestTableExpected("show create table ");
    }

    [Test]
    public void DropTableCompletion()
    {
      TestTableExpected("drop table ");
    }

    [Test]
    public void UpdateTableCompletion()
    {
      TestTableExpected("update ");
    }

    [Test]
    public void DeleteFromTableCompletion()
    {
      TestTableExpected("delete from ");
    }

    [Test]
    public void InsertIntoTableCompletion()
    {
      TestTableExpected("insert into ");
    }

    [Test]
    public void RenameTableCompletion()
    {
      TestTableExpected("rename table ");
    }

    private void TestTableExpected(string sql)
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(
        expectedToken == "table_factor" ||
        expectedToken == "simple_table_ref_no_alias_existing");
    }

    [Test]
    public void CallProcedureNameCompletion()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql( "call", true, out sb );
      //Assert.True(sb.ToString().EndsWith("no viable alternative at input '<EOF>'\r\n"));
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "proc_name");
      //Assert.True(((CommonErrorNode)r.Tree).Text == "call");
    }

    [Test]
    public void SelectColumnCompletionWithTables()
    {
      StringBuilder sb;
      string sql = "select *, fromtable.name, from fromtable inner join computer";
      MySQL51Parser.program_return r =
        Utility.ParseSql( sql, true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      List<string> tablesUsed = new string[] { "fromtable", "computer" }.ToList();
      Assert.AreEqual(tablesUsed.Count, tables.Count);
      foreach (TableWithAlias ta in tables)
      {
        Assert.True(tablesUsed.Contains(ta.TableName.ToLower()));
      }
      //Assert.True(sb.ToString().EndsWith("no viable alternative at input 'FROM'\r\n",
      //  StringComparison.CurrentCultureIgnoreCase));
      //Match m = new Regex(@"select (?<columns>.*) (?<from>from .*$)").Match(sql);
      //if (m.Success)
      //{
      //  sql = string.Format("select c {0}", m.Groups["from"].Value);
      //  r = Utility.ParseSql(sql, false, out sb);
      //  List<TableWithAlias> tables = new List<TableWithAlias>();
      //  ParserUtils.GetTables((ITree)r.Tree, tables);
      //  List<string> tablesUsed = new string[] { "fromtable", "computer" }.ToList();
      //  Assert.AreEqual(tablesUsed.Count, tables.Count);        
      //  foreach (TableWithAlias ta in tables)
      //  {
      //    Assert.True(tablesUsed.Contains(ta.TableName.ToLower() ));
      //  }
      //}
    }    

    [Test]
    public void SelectColumnCompletionWithTableWithAlias()
    {
      StringBuilder sb;
      string sql = "select *, fromtable.name, from fromtable as a inner join computer as B";
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      List<TableWithAlias> tablesUsed = new List<TableWithAlias>();
      tablesUsed.Add(new TableWithAlias("fromtable", "a"));
      tablesUsed.Add(new TableWithAlias("computer", "B"));
      Assert.AreEqual(tablesUsed.Count, tables.Count);
      foreach (TableWithAlias ta in tablesUsed)
      {
        Assert.True(tables.Contains(ta));
      }
      Assert.AreEqual(tablesUsed.Count, tables.Count);      
      //Assert.True(sb.ToString().EndsWith("no viable alternative at input 'FROM'\r\n",
      //  StringComparison.CurrentCultureIgnoreCase));
      //Match m = new Regex(@"select (?<columns>.*) (?<from>from .*$)").Match(sql);
      //if (m.Success)
      //{
      //  sql = string.Format("select c {0}", m.Groups["from"].Value);
      //  r = Utility.ParseSql(sql, false, out sb);
      //  List<TableWithAlias> tables = new List<TableWithAlias>();
      //  ParserUtils.GetTables((ITree)r.Tree, tables);
      //  List<TableWithAlias> tablesUsed = new List<TableWithAlias>();
      //  tablesUsed.Add( new TableWithAlias( "fromtable", "a" ) );
      //  tablesUsed.Add( new TableWithAlias( "computer", "B" ) );
      //  Assert.AreEqual(tablesUsed.Count, tables.Count);
      //  foreach (TableWithAlias ta in tablesUsed)
      //  {          
      //    Assert.True(tables.Contains(ta));
      //  }
      //}
    }

    [Test]
    public void SelectColumnCompletionWithTableWithDatabase()
    {
      StringBuilder sb;
      string sql = "select *, fromtable.name, from test2.fromtable inner join test1.computer as B";
      MySQL51Parser.program_return r =
        Utility.ParseSql(sql, true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      List<TableWithAlias> tablesUsed = new List<TableWithAlias>();
      tablesUsed.Add(new TableWithAlias("test2", "fromtable", ""));
      tablesUsed.Add(new TableWithAlias("test1", "computer", "B"));
      Assert.AreEqual(tablesUsed.Count, tables.Count);
      foreach (TableWithAlias ta in tablesUsed)
      {
        Assert.True(tables.Contains(ta));
      }
      Assert.AreEqual(tablesUsed.Count, tables.Count);      
      //Assert.True(sb.ToString().EndsWith("no viable alternative at input 'FROM'\r\n",
      //  StringComparison.CurrentCultureIgnoreCase));
      //Match m = new Regex(@"select (?<columns>.*) (?<from>from .*$)").Match(sql);
      //if (m.Success)
      //{
      //  sql = string.Format("select c {0}", m.Groups["from"].Value);
      //  r = Utility.ParseSql(sql, false, out sb);
      //  List<TableWithAlias> tables = new List<TableWithAlias>();
      //  ParserUtils.GetTables((ITree)r.Tree, tables);
      //  List<TableWithAlias> tablesUsed = new List<TableWithAlias>();
      //  tablesUsed.Add(new TableWithAlias( "test2", "fromtable", ""));
      //  tablesUsed.Add(new TableWithAlias( "test1", "computer", "B"));
      //  Assert.AreEqual(tablesUsed.Count, tables.Count);
      //  foreach (TableWithAlias ta in tablesUsed)
      //  {
      //    Assert.True(tables.Contains(ta));
      //  }
      //}
    }

    [Test]
    public void SelectColumnCompletionWithoutFrom()
    {
      // "select" 
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name" );
      //Assert.True(r.Tree is CommonErrorNode);
    }

    [Test]
    public void SelectColumnCompletionWithoutFrom2()
    {
      // "select" 
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select a, ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      Assert.True((r.Tree as ITree).GetChild( 0 ).Text.Equals( 
        "select", StringComparison.CurrentCultureIgnoreCase ));
    }

    [Test]
    public void SelectTableCompletionIncorrect()
    {
      // "select" 
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` as inner join `fromtable`", true, out sb);
      Assert.True( sb.ToString().EndsWith( 
        "no viable alternative at input 'inner'\r\n", StringComparison.CurrentCultureIgnoreCase) );
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.False(expectedToken == "column_name");
    }

    [Test]
    public void ColumnCompletionAtSelectWhere()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select a from t where ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionAtUpdateWhere()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("update t set c = 5 where  ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionAtUpdateWhereWithMinus()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("update t set c = 5 where - ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionAtUpdateWhereWithMinus2()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("update t set c = 5 where a = ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }    
    
    [Test]
    public void ColumnCompletionOnExpression()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` = ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression2()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` between ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression3()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` between c and ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression4()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` & ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression5()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` >> ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression6()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` * ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression7()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` ^ ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression8()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` = 1 and binary ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression9()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` = 1 and interval ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression10()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` = 1 and ( ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression11()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` = 1 and { id ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression12()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` = 1 and match ( ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void NonColumnCompletionOnExpression12()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` = 1 and match ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken != "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count == 1);
    }

    [Test]
    public void ColumnCompletionOnExpression13()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` = 1 and match ( a, b, c ) against ( ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void NonColumnCompletionOnExpression13()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` = 1 and match ( a, b, c ) against ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken != "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression14()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` = 1 and match ( a, b, ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression15()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` = 1 and case when ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression16()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` = 1 and case when true then ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression17()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("select * from `fromtable` where `fromtable`.`Id` = 1 and case when ( a = b ) then x + 1 else ", 
        true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionOnExpression18()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("case ",
        true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count == 0);
    }

    [Test]
    public void ColumnCompletionOnExpression19()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("case when ",
        true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count == 0);
    }
    
    [Test]
    public void RegressionTest2()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("-", true, out sb);
      Assert.True( sb.ToString().EndsWith( "no viable alternative at input '-'\r\n" ));
    }

    [Test]
    public void ColumnCompletionAtDeleteWhere()
    {
      // "delete from t where "
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("delete from t where ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionAtInsert()
    {
      // "insert into ta("
      // "insert into ta() values"
      // "insert into ta select "
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("insert into ta(", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionAtInsertPartialList()
    {
      // "insert into ta( a,  "
      // "insert into ta( a, b "
      // "insert into ta( a, b, "
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("insert into ta( a, ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionAtInsertSelect()
    {      
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("insert into ta select ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionAtInsertSelectWithFrom()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("insert into ta select *, from a inner join b on true", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionAtUpdate()
    {
      // "update t set "
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("update t set ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionBeginEnd()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("CREATE PROCEDURE doiterate(p1 INT) begin select * from t where;  end ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count != 0);
    }

    [Test]
    public void ColumnCompletionBeginEnd2()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r =
        Utility.ParseSql("CREATE PROCEDURE doiterate(p1 INT) begin select ;  end ", true, out sb);
      string expectedToken =
          new Regex(@"Expected (?<item>.*)\.").Match(sb.ToString()).Groups["item"].Value;
      Assert.True(expectedToken == "column_name");
      List<TableWithAlias> tables = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, tables);
      Assert.True(tables.Count == 0);
    }
  }
}
