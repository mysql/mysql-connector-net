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
	
	public class DropDatabase
	{
		[Fact]
		public void Simple()
		{
			MySQL51Parser.program_return r = Utility.ParseSql("DROP DATABASE dbname");
			/*
			Assert.Equal(1, statements.Count);
			Assert.True(statements[0] is DropDatabaseStatement);
			DropDatabaseStatement ds = statements[0] as DropDatabaseStatement;
			Assert.Equal("dbname", ds.DatabaseToDrop.Text);
			*/
			r = Utility.ParseSql("DROP SCHEMA `schemaname`");
			/*
			Assert.Equal(1, statements.Count);
			Assert.True(statements[0] is DropDatabaseStatement);
			ds = statements[0] as DropDatabaseStatement;
			Assert.Equal("`schemaname`", ds.DatabaseToDrop.Text);
			 * */
		}

		[Fact]
		public void MissingDbName()
		{
			MySQL51Parser.program_return r = Utility.ParseSql("DROP DATABASE", true);
			/*
			try
			{
				MySqlParser p = new MySqlParser();
				p.Parse("DROP DATABASE");
				Assert.Fail("This should have thrown an exception");
			}
			catch (MySqlParserException mpe)
			{
				Assert.Equal(MySqlParserExceptionCode.IdentifierExpected, mpe.Code);
			}
			 * */
		}

		[Fact]
		public void IfExists()
		{
			MySQL51Parser.program_return r = Utility.ParseSql("DROP DATABASE IF EXISTS `dbname`");
			/*
			Assert.Equal(1, statements.Count);
			Assert.True(statements[0] is DropDatabaseStatement);
			DropDatabaseStatement ds = statements[0] as DropDatabaseStatement;
			Assert.Equal("`dbname`", ds.DatabaseToDrop.Text);
			Assert.True(ds.IfExists);
			 */
		}
	}
}
