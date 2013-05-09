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
	
	public class DropTrigger
	{
		[Fact]
		public void SimpleNoSchema()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql("DROP TRIGGER trigger1");
			/*
			Assert.Equal(1, statements.Count);
			Assert.True(statements[0] is DropTriggerStatement);
			DropTriggerStatement ds = statements[0] as DropTriggerStatement;
			Assert.Null(ds.TriggerToDrop.Database);
			Assert.Equal("trigger1", ds.TriggerToDrop.Name.Text);
			 * */
		}

		[Fact]
		public void SimpleSchema()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql("DROP TRIGGER schema1.trigger1");
			/*
			Assert.Equal(1, statements.Count);
			Assert.True(statements[0] is DropTriggerStatement);
			DropTriggerStatement ds = statements[0] as DropTriggerStatement;
			Assert.Equal("schema1", ds.TriggerToDrop.Database.Text);
			Assert.Equal("trigger1", ds.TriggerToDrop.Name.Text);
			*/

			r = Utility.ParseSql("DROP TRIGGER `schema2`.`trigger2`");
			/*
			Assert.Equal(1, statements.Count);
			Assert.True(statements[0] is DropTriggerStatement);
			ds = statements[0] as DropTriggerStatement;
			Assert.Equal("`schema2`", ds.TriggerToDrop.Database.Text);
			Assert.Equal("`trigger2`", ds.TriggerToDrop.Name.Text);
			 * */
		}

		[Fact]
		public void MissingDbName()
		{
			MySQL51Parser.program_return r = Utility.ParseSql("DROP TRIGGER", true);
		}

		[Fact]
		public void IfExists()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql("DROP TRIGGER IF EXISTS `trigger1`");
			/*
			Assert.Equal(1, statements.Count);
			Assert.True(statements[0] is DropTriggerStatement);
			DropTriggerStatement ds = statements[0] as DropTriggerStatement;
			Assert.Equal("`trigger1`", ds.TriggerToDrop.Name.Text);
			Assert.True(ds.IfExists);
			 * */
		}
	}
}
