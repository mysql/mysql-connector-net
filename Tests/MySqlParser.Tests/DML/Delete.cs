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
using System.Linq;
using System.Text;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Xunit;


namespace MySql.Parser.Tests
{
  
  public class Delete
  {
    [Fact]
    public void MissingTableDeleteTest()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("delete from ", true);
    }

    [Fact]
    public void DangerousDeleteTest()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("delete quick from a");
      /*
      Assert.Equal(1, statements.Count);
      Assert.True(statements[0] is DeleteStatement);
      DeleteStatement ds = statements[0] as DeleteStatement;
      Assert.Equal("a", ds.Table.Name.Text);
      Assert.Equal(false, ds.Ignore);
      Assert.Equal(true, ds.Quick);
      Assert.Equal(false, ds.LowPriority);
       * */
    }

    [Fact]
    public void DeleteSimpleTest()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("delete ignore from Table1 where ( Flag is null );");
      /*
      Assert.Equal(1, statements.Count);
      Assert.True(statements[0] is DeleteStatement);
      DeleteStatement ds = statements[0] as DeleteStatement;
      Assert.Equal("Table1", ds.Table.Name.Text);
      // Where
      Assert.Equal("Flag",
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
      Assert.Equal(BooleanExpressionPrimaryOperator.IsNull,
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Rest.Operator);
      Assert.Equal(true, ds.Ignore);
      Assert.Equal(false, ds.Quick);
      Assert.Equal(false, ds.LowPriority);
       * */
    }

    [Fact]
    public void DeleteWithClausules()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        "delete ignore quick low_priority from Table2 where ( Id <> 1 ) order by Id desc limit 100");
      /*
      Assert.Equal(1, statements.Count);
      Assert.True(statements[0] is DeleteStatement);
      DeleteStatement ds = statements[0] as DeleteStatement;
      Assert.Equal("Table2", ds.Table.Name.Text);
      // Where
      Assert.Equal("Id",
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
      Assert.Equal(BooleanExpressionPrimaryOperator.DistinctThan,
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Rest.Operator);
      Assert.Equal("1",
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Rest.Rest.Predicate.Literal.Value);
      // Order By
      Assert.Equal("Id", ds.Order.ExprList[0].Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
      Assert.Equal(OrderByDirection.Desc, ds.Order.DirList[0]);
      // Limit
      Assert.Equal(ds.Limit.RowCount, 100);
      // keywords
      Assert.Equal(true, ds.Ignore);
      Assert.Equal(true, ds.Quick);
      Assert.Equal(true, ds.LowPriority);
       * */
    }

    [Fact]
    public void DeleteMultiTableTest()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"delete from Table1, Table2.*, Table3.* using Table1, Table4 inner join Table5
        on Table4.KeyGuid = Table5.ForeignKeyGuid where ( IdKey <> 1 )");
      /*
      Assert.Equal(1, statements.Count);
      Assert.True(statements[0] is DeleteStatement);
      DeleteStatement ds = statements[0] as DeleteStatement;
      // Where
      Assert.Equal("IdKey",
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
      Assert.Equal(BooleanExpressionPrimaryOperator.DistinctThan,
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Rest.Operator);
      Assert.Equal("1",
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Rest.Rest.Predicate.Literal.Value);
      // MultiTable syntax
      Assert.Equal(3, ds.Tables.Count);
      Assert.Equal("Table1", ds.Tables[0].Name.Text);
      Assert.Equal("Table2", ds.Tables[1].Name.Text);
      Assert.Equal("Table3", ds.Tables[2].Name.Text);
      Assert.Equal(2, ds.TableReferences.TablesReferences.Count);
      Assert.Equal("Table1", ds.TableReferences.TablesReferences[0].Factor.TableName.Name.Text);
      Assert.Equal("Table4", ds.TableReferences.TablesReferences[1].Factor.TableName.Name.Text);
      Assert.Equal("Table5", ds.TableReferences.TablesReferences[1].Join.Reference.TableName.Name.Text);
      // on clause
      ColumnReference c =
        ds.TableReferences.TablesReferences[1].Join.Conditional.Term.Reference.
        Predicate.BitExprLeft.Column;
      Assert.Equal("Table4.KeyGuid", string.Format("{0}.{1}", c.Table.Text, c.Name.Text));
      c = ds.TableReferences.TablesReferences[1].Join.Conditional.Term.Reference.Rest.Rest.
        Predicate.BitExprLeft.Column;
      Assert.Equal("Table5.ForeignKeyGuid", string.Format("{0}.{1}", c.Table.Text, c.Name.Text));
      Assert.Equal(BooleanExpressionPrimaryOperator.Equal, ds.TableReferences.TablesReferences[1].Join.Conditional.Term.Reference.Rest.Operator);
       * */
    }

    [Fact]
    public void DeleteMultiTableTest2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"delete Table1.*, Table2, Table3 from Table1, Table2 inner join Table3 
        on Table2.KeyId = Table3.ForeignKeyId where ( IdKey = 2 )");
      /*
      Assert.Equal(1, statements.Count);
      Assert.True(statements[0] is DeleteStatement);
      DeleteStatement ds = statements[0] as DeleteStatement;
      // Where
      Assert.Equal("IdKey",
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
      Assert.Equal(BooleanExpressionPrimaryOperator.Equal,
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Rest.Operator);
      Assert.Equal("2",
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Rest.Rest.Predicate.Literal.Value);
      // MultiTable syntax
      Assert.Equal(3, ds.Tables.Count);
      Assert.Equal("Table1", ds.Tables[0].Name.Text);
      Assert.Equal("Table2", ds.Tables[1].Name.Text);
      Assert.Equal("Table3", ds.Tables[2].Name.Text);
      Assert.Equal(2, ds.TableReferences.TablesReferences.Count);
      Assert.Equal("Table1", ds.TableReferences.TablesReferences[0].Factor.TableName.Name.Text);
      Assert.Equal("Table2", ds.TableReferences.TablesReferences[1].Factor.TableName.Name.Text);
      Assert.Equal("Table3", ds.TableReferences.TablesReferences[1].Join.Reference.TableName.Name.Text);
      // on clause
      ColumnReference c =
        ds.TableReferences.TablesReferences[1].Join.Conditional.Term.Reference.
        Predicate.BitExprLeft.Column;
      Assert.Equal("Table2.KeyId", string.Format("{0}.{1}", c.Table.Text, c.Name.Text));
      c = ds.TableReferences.TablesReferences[1].Join.Conditional.Term.Reference.Rest.Rest.
        Predicate.BitExprLeft.Column;
      Assert.Equal("Table3.ForeignKeyId", string.Format("{0}.{1}", c.Table.Text, c.Name.Text));
      Assert.Equal(BooleanExpressionPrimaryOperator.Equal,
        ds.TableReferences.TablesReferences[1].Join.Conditional.Term.Reference.Rest.Operator);
       * */
    }

    [Fact]
    public void DeleteMultiTableWrongTest()
    {
      // TODO: Check if effectively is the multitable syntax disallowed in combination with order by.
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"delete Table1.*, Table2, Table3 from Table1, Table2 inner join Table3 
        on Table2.KeyId = Table3.ForeignKeyId where ( Id <> 1 ) order by Id desc", true);
      /*
      Assert.Equal(1, statements.Count);
      Assert.True(statements[0] is DeleteStatement);
      DeleteStatement ds = statements[0] as DeleteStatement;
      // Where
      Assert.Equal("Id",
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
      Assert.Equal(BooleanExpressionPrimaryOperator.DistinctThan,
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Rest.Operator);
      Assert.Equal("1",
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Rest.Rest.Predicate.Literal.Value);
      // MultiTable syntax
      Assert.Equal(3, ds.Tables.Count);
      Assert.Equal("Table1", ds.Tables[0].Name.Text);
      Assert.Equal("Table2", ds.Tables[1].Name.Text);
      Assert.Equal("Table3", ds.Tables[2].Name.Text);
      Assert.Equal(2, ds.TableReferences.TablesReferences.Count);
      Assert.Equal("Table1", ds.TableReferences.TablesReferences[0].Factor.TableName.Name.Text);
      Assert.Equal("Table2", ds.TableReferences.TablesReferences[1].Factor.TableName.Name.Text);
      Assert.Equal("Table3", ds.TableReferences.TablesReferences[1].Join.Reference.TableName.Name.Text);
      // on clause
      ColumnReference c =
        ds.TableReferences.TablesReferences[1].Join.Conditional.Term.Reference.Predicate.BitExprLeft.Column;
      Assert.Equal("Table2.KeyId", string.Format("{0}.{1}", c.Table.Text, c.Name.Text));
      c = ds.TableReferences.TablesReferences[1].Join.Conditional.Term.Reference.Predicate.BitExprRight.Column;
      Assert.Equal("Table3.ForeignKeyId", string.Format("{0}.{1}", c.Table.Text, c.Name.Text));
      Assert.Equal(BooleanExpressionPrimaryOperator.Equal,
        ds.TableReferences.TablesReferences[1].Join.Conditional.Term.Reference.Rest.Operator);			
      */
    }

    [Fact]
    public void DeleteMultiTableWrongTest2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"delete Table1.*, Table2, Table3 from Table1, Table2 inner join Table3 
        on Table2.KeyId = Table3.ForeignKeyId where ( Id <> 1 ) limit 1000", true);
      /*
      Assert.Equal(1, statements.Count);
      Assert.True(statements[0] is DeleteStatement);
      DeleteStatement ds = statements[0] as DeleteStatement;
      // Where
      Assert.Equal("IdKey",
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
      Assert.Equal(BooleanExpressionPrimaryOperator.Equal,
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Rest.Operator);
      Assert.Equal("2",
        ds.Where.Term.Reference.Predicate.Expression.Term.Reference.Rest.Rest.Predicate.Literal.Value);
      // MultiTable syntax
      Assert.Equal(3, ds.Tables.Count);
      Assert.Equal("Table1", ds.Tables[0].Name.Text);
      Assert.Equal("Table2", ds.Tables[1].Name.Text);
      Assert.Equal("Table3", ds.Tables[2].Name.Text);
      Assert.Equal(2, ds.TableReferences.TablesReferences.Count);
      Assert.Equal("Table1", ds.TableReferences.TablesReferences[0].Factor.TableName.Name.Text);
      Assert.Equal("Table2", ds.TableReferences.TablesReferences[1].Factor.TableName.Name.Text);
      Assert.Equal("Table3", ds.TableReferences.TablesReferences[1].Join.Reference.TableName.Name.Text);
      // on clause
      ColumnReference c =
        ds.TableReferences.TablesReferences[1].Join.Conditional.Term.Reference.Predicate.BitExprLeft.Column;
      Assert.Equal("Table2.KeyId", string.Format("{0}.{1}", c.Table.Text, c.Name.Text));
      c = ds.TableReferences.TablesReferences[1].Join.Conditional.Term.Reference.Predicate.BitExprRight.Column;
      Assert.Equal("Table3.ForeignKeyId", string.Format("{0}.{1}", c.Table.Text, c.Name.Text));
      Assert.Equal(BooleanExpressionPrimaryOperator.Equal,
        ds.TableReferences.TablesReferences[1].Join.Conditional.Term.Reference.Rest.Operator);
       * */
    }

        [Fact]
    public void Subquery()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
                @"DELETE FROM t1
WHERE s11 > ANY
 (SELECT COUNT(*) /* no hint */ FROM t2
  WHERE NOT EXISTS
   (SELECT * FROM t3
    WHERE ROW(5*t2.s1,77)=
     (SELECT 50,11*s1 FROM t4 UNION SELECT 50,77 FROM
      (SELECT * FROM t5) AS t5)));", true);
    }

        [Fact]
        public void WithPartition_55()
        {
          StringBuilder sb;
          MySQL51Parser.program_return r = Utility.ParseSql(
            @"DELETE FROM employees PARTITION (p0, p1) WHERE fname LIKE 'j%';", true, out sb, new Version(5, 5));
          Assert.True(sb.ToString().IndexOf("partition", StringComparison.OrdinalIgnoreCase) != -1);
        }

        [Fact]
        public void WithPartition_56()
        {
          StringBuilder sb;
          MySQL51Parser.program_return r = Utility.ParseSql(
            @"DELETE FROM employees PARTITION (p0, p1) WHERE fname LIKE 'j%';", false, out sb, new Version(5, 6));
        }
  }
}
