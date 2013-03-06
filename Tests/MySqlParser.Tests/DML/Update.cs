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
using System.Linq;
using System.Text;
using NUnit.Framework;
using Antlr.Runtime;
using Antlr.Runtime.Tree;

namespace MySql.Parser.Tests
{
	[TestFixture]
	public class Update
	{
		[Test]
		public void UpdateSimpleTest()
		{
			MySQL51Parser.program_return r = Utility.ParseSql(
				@"update Table1 
					set col1 = 20, col2 = a and b, col3 = col4, col4 = true, col5 = null, col6 = 'string' 
					where Id = 30");
			/*
			SqlStatementList l = p.Parse(sql);
			Assert.AreEqual(1, l.Count);
			UpdateStatement upd = l[0] as UpdateStatement;
			Assert.AreEqual(6, upd.Columns.Count);
			Assert.AreEqual(6, upd.Values.Count);
			Assert.AreEqual("Table1", upd.Table.Factor.TableName.Name.Text);
			// Where condition
			Assert.AreEqual("Id", upd.Where.Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
			Assert.AreEqual(BooleanExpressionPrimaryOperator.Equal, upd.Where.Term.Reference.Rest.Operator);
			Assert.AreEqual("30", upd.Where.Term.Reference.Rest.Rest.Predicate.Literal.Value);
			// Column 0
			Assert.AreEqual("col1", upd.Columns[0].Name.Text);
			Assert.AreEqual("20", upd.Values[0].Term.Reference.Predicate.Literal.Value);
			// Column 1
			Assert.AreEqual("col2", upd.Columns[1].Name.Text);
			Assert.AreEqual("a", upd.Values[1].Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
			Assert.AreEqual(BooleanExpressionOperator.And, upd.Values[1].Rest.Operator);
			Assert.AreEqual("b", upd.Values[1].Rest.Subexpression.Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
			// Column 2
			Assert.AreEqual("col3", upd.Columns[2].Name.Text);
			Assert.AreEqual("col4", upd.Values[2].Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
			// Column 3
			Assert.AreEqual("col4", upd.Columns[3].Name.Text);
			Assert.AreEqual("true", upd.Values[3].Term.Reference.Predicate.Literal.Value);
			// Column 4
			Assert.AreEqual("col5", upd.Columns[4].Name.Text);
			Assert.AreEqual("null", upd.Values[4].Term.Reference.Predicate.Literal.Value);
			// Column 5
			Assert.AreEqual("col6", upd.Columns[5].Name.Text);
			Assert.AreEqual("'string'", upd.Values[5].Term.Reference.Predicate.Literal.Value);
			 * */
		}

		[Test]
		public void UpdateMoreComplexText()
		{
			MySQL51Parser.program_return r = Utility.ParseSql(
				@"update low_priority ignore T set deleted = 1, ToArchive = false, DateStamp = '10-10-2000' 
				where DateCreated < '09-10-2000' order by Id asc limit 1000");
			/*
			Assert.AreEqual(1, l.Count);
			UpdateStatement upd = l[0] as UpdateStatement;
			Assert.AreEqual(3, upd.Columns.Count);
			Assert.AreEqual(3, upd.Values.Count);
			Assert.AreEqual("T", upd.Table.Factor.TableName.Name.Text);
			// Where condition
			Assert.AreEqual("DateCreated", upd.Where.Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
			Assert.AreEqual(BooleanExpressionPrimaryOperator.LesserThan, upd.Where.Term.Reference.Rest.Operator);
			Assert.AreEqual("'09-10-2000'", upd.Where.Term.Reference.Rest.Rest.Predicate.Literal.Value);
			// Column 0
			Assert.AreEqual("deleted", upd.Columns[0].Name.Text);
			Assert.AreEqual("1", upd.Values[0].Term.Reference.Predicate.Literal.Value);
			// Column 1
			Assert.AreEqual("ToArchive", upd.Columns[1].Name.Text);
			Assert.AreEqual("false", upd.Values[1].Term.Reference.Predicate.Literal.Value);
			// Column 2
			Assert.AreEqual("DateStamp", upd.Columns[2].Name.Text);
			Assert.AreEqual("'10-10-2000'", upd.Values[2].Term.Reference.Predicate.Literal.Value);
			// Order by
			Assert.AreEqual(1, upd.Order.ExprList.Count);
			Assert.AreEqual(1, upd.Order.DirList.Count);
			Assert.AreEqual("Id", upd.Order.ExprList[0].Term.Reference.Predicate.BitExprLeft.Column.Name.Text);
			Assert.AreEqual(OrderByDirection.Asc, upd.Order.DirList[0]);
			// Limit
			Assert.AreEqual(upd.Limit.RowCount, 1000);
			Assert.AreEqual(upd.Limit.Offset, 113);
			 * */
			r = Utility.ParseSql(
				@"update low_priority ignore T set deleted = 1, ToArchive = false, DateStamp = '10-10-2000' 
				where DateCreated < '09-10-2000' order by Id asc limit 1000 offset 113", true );
		}

		[Test]
		public void UpdateMultiTable()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql(
				@"update T1, T2 inner join T3 on T2.KeyId = T3.ForeignKeyId 
					set Col1 = 3.1416, T1.Col3 = T2.Col3, T1.Col2 = T3.Col2  
					where ( T1.Id = T2.Id ) ");
			//Assert.AreEqual(1, l.Count);
		}

[Test]
        public void Subquery()
        {
          MySQL51Parser.program_return r = Utility.ParseSql(@"UPDATE books SET author = ( SELECT author FROM volumes WHERE volumes.id = books.volume_id );");
        }

        [Test]
        public void Subquery2()
        {
          MySQL51Parser.program_return r = Utility.ParseSql(
@"UPDATE people, 
(SELECT count(*) as votecount, person_id 
FROM votes GROUP BY person_id) as tally
SET people.votecount = tally.votecount 
WHERE people.person_id = tally.person_id;");
        }

        [Test]
        public void WithPartition_55()
        {
          StringBuilder sb;
          MySQL51Parser.program_return r = Utility.ParseSql(
            @"UPDATE employees PARTITION (p0) SET store_id = 2 WHERE fname = 'Jill';", true, out sb, new Version(5, 5));
          Assert.IsTrue(sb.ToString().IndexOf("mismatched input", StringComparison.OrdinalIgnoreCase) != -1);
        }

        [Test]
        public void WithPartition_56()
        {
          StringBuilder sb;
          MySQL51Parser.program_return r = Utility.ParseSql(
            @"UPDATE employees PARTITION (p0) SET store_id = 2 WHERE fname = 'Jill';", false, out sb, new Version(5, 6));
        }
	}
}
