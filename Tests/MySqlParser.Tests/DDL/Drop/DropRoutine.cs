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

namespace MySql.Parser.Tests.DDL.Drop
{
	[TestFixture]
	public class DropRoutine
	{		
		[Test]
		public void SimpleNoSchema()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql("DROP PROCEDURE procname");
			/*
			Assert.AreEqual(1, statements.Count);
			Assert.IsTrue(statements[0] is DropRoutineStatement);
			DropRoutineStatement ds = statements[0] as DropRoutineStatement;
			Assert.IsNull(ds.RoutineToDrop.Database);
			Assert.AreEqual("procname", ds.RoutineToDrop.Name.Text);
			 * */

			r = Utility.ParseSql("DROP FUNCTION `funcname`");
			/*
			Assert.AreEqual(1, statements.Count);
			Assert.IsTrue(statements[0] is DropRoutineStatement);
			ds = statements[0] as DropRoutineStatement;
			Assert.IsNull(ds.RoutineToDrop.Database);
			Assert.AreEqual("`funcname`", ds.RoutineToDrop.Name.Text);
			 * */
		}

		[Test]
		public void Simple()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql("DROP PROCEDURE schema1.procname");
			/*
			Assert.AreEqual(1, statements.Count);
			Assert.IsTrue(statements[0] is DropRoutineStatement);
			DropRoutineStatement ds = statements[0] as DropRoutineStatement;
			Assert.AreEqual("schema1", ds.RoutineToDrop.Database.Text);
			Assert.AreEqual("procname", ds.RoutineToDrop.Name.Text);
			 * */

			r = Utility.ParseSql("DROP FUNCTION `schema1`.`funcname`");
			/*
			Assert.AreEqual(1, statements.Count);
			Assert.IsTrue(statements[0] is DropRoutineStatement);
			ds = statements[0] as DropRoutineStatement;
			Assert.AreEqual("`schema1`", ds.RoutineToDrop.Database.Text);
			Assert.AreEqual("`funcname`", ds.RoutineToDrop.Name.Text);
			 * */
		}

		[Test]
		public void MissingRoutineName()
		{
			MySQL51Parser.program_return r = Utility.ParseSql("DROP PROCEDURE", true);
		}

		[Test]
		public void IfExists()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql("DROP PROCEDURE IF EXISTS `procname`");
			/*
			Assert.AreEqual(1, statements.Count);
			Assert.IsTrue(statements[0] is DropRoutineStatement);
			DropRoutineStatement ds = statements[0] as DropRoutineStatement;
			Assert.AreEqual("`procname`", ds.RoutineToDrop.Name.Text);
			Assert.IsTrue(ds.IfExists);*/
		}
	}
}
