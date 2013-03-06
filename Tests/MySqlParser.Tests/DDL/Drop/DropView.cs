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
	public class DropView
	{
		[Test]
		public void SimpleNoSchema()
		{
			MySQL51Parser.program_return r = Utility.ParseSql("DROP VIEW `viewname`");
			/*
			Assert.AreEqual(1, statements.Count);
			Assert.IsTrue(statements[0] is DropViewStatement);
			DropViewStatement ds = statements[0] as DropViewStatement;
			Assert.AreEqual(1, ds.ToDrop.Count);
			Assert.AreEqual("`viewname`", ds.ToDrop[0].Name.Text);
			 * */
		}

		[Test]
		public void SimpleWithSchema()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql("DROP VIEW `schema1`.`viewname`");
			/*
			Assert.AreEqual(1, statements.Count);
			Assert.IsTrue(statements[0] is DropViewStatement);
			DropViewStatement ds = statements[0] as DropViewStatement;
			Assert.AreEqual(1, ds.ToDrop.Count);
			Assert.AreEqual("`schema1`", ds.ToDrop[0].Database.Text);
			Assert.AreEqual("`viewname`", ds.ToDrop[0].Name.Text);
			 * */
		}

		[Test]
		public void MissingViewName()
		{
          try
          {
            Utility.ParseSql("DROP VIEW", true);
          }
          catch (Exception e)
          {
          }
		}

		[Test]
		public void MultipleViews()
		{			
			MySQL51Parser.program_return r = Utility.ParseSql("DROP VIEW `view1`, schema2.view2, `schema3`.`view3`, view4");
			/*
			Assert.AreEqual(1, statements.Count);
			Assert.IsTrue(statements[0] is DropViewStatement);
			DropViewStatement ds = statements[0] as DropViewStatement;
			Assert.AreEqual(4, ds.ToDrop.Count);

			Assert.IsNull(ds.ToDrop[0].Database);
			Assert.AreEqual("`view1`", ds.ToDrop[0].Name.Text);

			Assert.AreEqual("schema2", ds.ToDrop[1].Database.Text);
			Assert.AreEqual("view2", ds.ToDrop[1].Name.Text);

			Assert.AreEqual("`schema3`", ds.ToDrop[2].Database.Text);
			Assert.AreEqual("`view3`", ds.ToDrop[2].Name.Text);

			Assert.IsNull(ds.ToDrop[3].Database);
			Assert.AreEqual("view4", ds.ToDrop[3].Name.Text);
			 * */
		}

		[Test]
		public void IfExists()
		{
			MySQL51Parser.program_return r = Utility.ParseSql("DROP VIEW IF EXISTS `viewname`");
			/*
			Assert.AreEqual(1, statements.Count);
			Assert.IsTrue(statements[0] is DropViewStatement);
			DropViewStatement ds = statements[0] as DropViewStatement;
			Assert.AreEqual(1, ds.ToDrop.Count);
			Assert.AreEqual("`viewname`", ds.ToDrop[0].Name.Text);
			Assert.IsTrue(ds.IfExists);
			 * */
		}

		[Test]
		public void CascadeOrRestrict()
		{
			MySQL51Parser.program_return r = Utility.ParseSql("DROP VIEW IF EXISTS `viewname` CASCADE");
			/*
			DropViewStatement ds = statements[0] as DropViewStatement;
			Assert.IsTrue(ds.Cascade);
			Assert.IsFalse(ds.Restrict);
			*/
			r = Utility.ParseSql("DROP VIEW IF EXISTS `viewname` RESTRICT");
			/*
			ds = statements[0] as DropViewStatement;
			Assert.IsFalse(ds.Cascade);
			Assert.IsTrue(ds.Restrict);
			*/

			r = Utility.ParseSql("DROP VIEW IF EXISTS `viewname` RESTRICT CASCADE", true);
		}
	}
}
