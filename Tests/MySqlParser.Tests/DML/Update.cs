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
  public class Update
  {
    [Fact]
    public void UpdateSimpleTest()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"update Table1 
          set col1 = 20, col2 = a and b, col3 = col4, col4 = true, col5 = null, col6 = 'string' 
          where Id = 30");
      /*
      SqlStatementList l = p.Parse(sql);
      Assert.Equal(1, l.Count);
      UpdateStatement upd = l[0] as UpdateStatement;
      Assert.Equal(6, upd.Columns.Count);
      Assert.Equal(6, upd.Values.Count);
      Assert.Equal("Table1", upd.Table.Factor.TableName.Name.Text);
      // Where condition
      Assert.Equal("Id", upd.Where.Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
      Assert.Equal(BooleanExpressionPrimaryOperator.Equal, upd.Where.Term.Reference.Rest.Operator);
      Assert.Equal("30", upd.Where.Term.Reference.Rest.Rest.Predicate.Literal.Value);
      // Column 0
      Assert.Equal("col1", upd.Columns[0].Name.Text);
      Assert.Equal("20", upd.Values[0].Term.Reference.Predicate.Literal.Value);
      // Column 1
      Assert.Equal("col2", upd.Columns[1].Name.Text);
      Assert.Equal("a", upd.Values[1].Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
      Assert.Equal(BooleanExpressionOperator.And, upd.Values[1].Rest.Operator);
      Assert.Equal("b", upd.Values[1].Rest.Subexpression.Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
      // Column 2
      Assert.Equal("col3", upd.Columns[2].Name.Text);
      Assert.Equal("col4", upd.Values[2].Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
      // Column 3
      Assert.Equal("col4", upd.Columns[3].Name.Text);
      Assert.Equal("true", upd.Values[3].Term.Reference.Predicate.Literal.Value);
      // Column 4
      Assert.Equal("col5", upd.Columns[4].Name.Text);
      Assert.Equal("null", upd.Values[4].Term.Reference.Predicate.Literal.Value);
      // Column 5
      Assert.Equal("col6", upd.Columns[5].Name.Text);
      Assert.Equal("'string'", upd.Values[5].Term.Reference.Predicate.Literal.Value);
       * */
    }

    [Fact]
    public void UpdateMoreComplexText()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"update low_priority ignore T set deleted = 1, ToArchive = false, DateStamp = '10-10-2000' 
        where DateCreated < '09-10-2000' order by Id asc limit 1000");
      /*
      Assert.Equal(1, l.Count);
      UpdateStatement upd = l[0] as UpdateStatement;
      Assert.Equal(3, upd.Columns.Count);
      Assert.Equal(3, upd.Values.Count);
      Assert.Equal("T", upd.Table.Factor.TableName.Name.Text);
      // Where condition
      Assert.Equal("DateCreated", upd.Where.Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
      Assert.Equal(BooleanExpressionPrimaryOperator.LesserThan, upd.Where.Term.Reference.Rest.Operator);
      Assert.Equal("'09-10-2000'", upd.Where.Term.Reference.Rest.Rest.Predicate.Literal.Value);
      // Column 0
      Assert.Equal("deleted", upd.Columns[0].Name.Text);
      Assert.Equal("1", upd.Values[0].Term.Reference.Predicate.Literal.Value);
      // Column 1
      Assert.Equal("ToArchive", upd.Columns[1].Name.Text);
      Assert.Equal("false", upd.Values[1].Term.Reference.Predicate.Literal.Value);
      // Column 2
      Assert.Equal("DateStamp", upd.Columns[2].Name.Text);
      Assert.Equal("'10-10-2000'", upd.Values[2].Term.Reference.Predicate.Literal.Value);
      // Order by
      Assert.Equal(1, upd.Order.ExprList.Count);
      Assert.Equal(1, upd.Order.DirList.Count);
      Assert.Equal("Id", upd.Order.ExprList[0].Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
      Assert.Equal(OrderByDirection.Asc, upd.Order.DirList[0]);
      // Limit
      Assert.Equal(upd.Limit.RowCount, 1000);
      Assert.Equal(upd.Limit.Offset, 113);
       * */
      r = Utility.ParseSql(
        @"update low_priority ignore T set deleted = 1, ToArchive = false, DateStamp = '10-10-2000' 
        where DateCreated < '09-10-2000' order by Id asc limit 1000 offset 113", true );
    }

    [Fact]
    public void UpdateMultiTable()
    {			
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"update T1, T2 inner join T3 on T2.KeyId = T3.ForeignKeyId 
          set Col1 = 3.1416, T1.Col3 = T2.Col3, T1.Col2 = T3.Col2  
          where ( T1.Id = T2.Id ) ");
      //Assert.Equal(1, l.Count);
    }

[Fact]
        public void Subquery()
        {
          MySQL51Parser.program_return r = Utility.ParseSql(@"UPDATE books SET author = ( SELECT author FROM volumes WHERE volumes.id = books.volume_id );");
        }

        [Fact]
        public void Subquery2()
        {
          MySQL51Parser.program_return r = Utility.ParseSql(
@"UPDATE people, 
(SELECT count(*) as votecount, person_id 
FROM votes GROUP BY person_id) as tally
SET people.votecount = tally.votecount 
WHERE people.person_id = tally.person_id;");
        }

        [Fact]
        public void WithPartition_55()
        {
          StringBuilder sb;
          MySQL51Parser.program_return r = Utility.ParseSql(
            @"UPDATE employees PARTITION (p0) SET store_id = 2 WHERE fname = 'Jill';", true, out sb, new Version(5, 5));
          Assert.True(sb.ToString().IndexOf("mismatched input", StringComparison.OrdinalIgnoreCase) != -1);
        }

        [Fact]
        public void WithPartition_56()
        {
          StringBuilder sb;
          MySQL51Parser.program_return r = Utility.ParseSql(
            @"UPDATE employees PARTITION (p0) SET store_id = 2 WHERE fname = 'Jill';", false, out sb, new Version(5, 6));
        }
  }
}
