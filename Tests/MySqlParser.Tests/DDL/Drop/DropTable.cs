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

namespace MySql.Parser.Tests.DDL.Drop
{
	
	public class DropTable
	{
		[Fact]
		public void SimpleNoSchema()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql("DROP TABLE `tablename`");
			/*
			Assert.Equal(1, statements.Count);
			Assert.True(statements[0] is DropTableStatement);
			DropTableStatement ds = statements[0] as DropTableStatement;
			Assert.Equal(1, ds.ToDrop.Count);
			Assert.Null(ds.ToDrop[0].Database);
			Assert.Equal("`tablename`", ds.ToDrop[0].Name.Text);*/
		}

		[Fact]
		public void SimpleWithSchema()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql("DROP TABLE `schema1`.`tablename`");
			/*
			Assert.Equal(1, statements.Count);
			Assert.True(statements[0] is DropTableStatement);
			DropTableStatement ds = statements[0] as DropTableStatement;
			Assert.Equal(1, ds.ToDrop.Count);
			Assert.Equal("`schema1`", ds.ToDrop[0].Database.Text);
			Assert.Equal("`tablename`", ds.ToDrop[0].Name.Text);
			 * */
		}

		[Fact]
		public void MissingTableName()
		{
			MySQL51Parser.program_return r = Utility.ParseSql("DROP TABLE", true);			
		}

		[Fact]
		public void MultipleTables()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql("DROP TABLE `table1`, schema2.table2, `schema3`.`table3`, table4");
			/*
			Assert.Equal(1, statements.Count);
			Assert.True(statements[0] is DropTableStatement);
			DropTableStatement ds = statements[0] as DropTableStatement;
			Assert.Equal(4, ds.ToDrop.Count);

			Assert.Null(ds.ToDrop[0].Database);
			Assert.Equal("`table1`", ds.ToDrop[0].Name.Text);

			Assert.Equal("schema2", ds.ToDrop[1].Database.Text);
			Assert.Equal("table2", ds.ToDrop[1].Name.Text);

			Assert.Equal("`schema3`", ds.ToDrop[2].Database.Text);
			Assert.Equal("`table3`", ds.ToDrop[2].Name.Text);

			Assert.Null(ds.ToDrop[3].Database);
			Assert.Equal("table4", ds.ToDrop[3].Name.Text);
			 * */
		}

		[Fact]
		public void IfExists()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql("DROP TABLE IF EXISTS `tablename`");
			/*
			Assert.Equal(1, statements.Count);
			Assert.True(statements[0] is DropTableStatement);
			DropTableStatement ds = statements[0] as DropTableStatement;
			Assert.Equal(1, ds.ToDrop.Count);
			Assert.Equal("`tablename`", ds.ToDrop[0].Name.Text);
			Assert.True(ds.IfExists);
			 * */
		}

		[Fact]
		public void Temporary()
		{
			MySQL51Parser.program_return r = Utility.ParseSql("DROP TEMPORARY TABLE IF EXISTS `tablename`");
			/*
			Assert.Equal(1, statements.Count);
			Assert.True(statements[0] is DropTableStatement);
			DropTableStatement ds = statements[0] as DropTableStatement;
			Assert.Equal(1, ds.ToDrop.Count);
			Assert.Equal("`tablename`", ds.ToDrop[0].Name.Text);
			Assert.True(ds.Temporary);
			 * */
		}

		[Fact]
		public void CascadeOrRestrict()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql("DROP TABLE IF EXISTS `tablename` CASCADE");
			/*
			DropTableStatement ds = statements[0] as DropTableStatement;
			Assert.True(ds.Cascade);
			Assert.False(ds.Restrict);
			*/
			r = Utility.ParseSql("DROP TABLE IF EXISTS `tablename` RESTRICT");
			/*
			ds = statements[0] as DropTableStatement;
			Assert.False(ds.Cascade);
			Assert.True(ds.Restrict);
			*/
			
		}

		[Fact]
		public void CascadeAndRestrict()
		{
			MySQL51Parser.program_return r = 
				Utility.ParseSql("DROP TABLE IF EXISTS `tablename` RESTRICT CASCADE", true);
		}
	}
}
