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
	public class RenameTable
	{
		[Test]
		public void SimpleNoSchema()
		{
			MySQL51Parser.program_return r = Utility.ParseSql("RENAME TABLE `table1` TO `table2`");
			/*
			Assert.AreEqual(1, statements.Count);
			Assert.IsTrue(statements[0] is RenameTableStatement);
			RenameTableStatement ds = statements[0] as RenameTableStatement;
			Assert.AreEqual(1, ds.OldNames.Count);
			Assert.AreEqual(1, ds.NewNames.Count);

			Assert.IsNull(ds.OldNames[0].Database);
			Assert.AreEqual("`table1`", ds.OldNames[0].Name.Text);
			Assert.IsNull(ds.NewNames[0].Database);
			Assert.AreEqual("`table2`", ds.NewNames[0].Name.Text);
			 * */
		}

		[Test]
		public void SimpleWithSchema()
		{
			MySQL51Parser.program_return r = Utility.ParseSql(
				"RENAME TABLE `schema1`.`table1` TO `schema2`.`table2`");
			/*
			Assert.AreEqual(1, statements.Count);
			Assert.IsTrue(statements[0] is RenameTableStatement);
			RenameTableStatement ds = statements[0] as RenameTableStatement;
			Assert.AreEqual(1, ds.OldNames.Count);
			Assert.AreEqual(1, ds.NewNames.Count);

			Assert.AreEqual("`schema1`", ds.OldNames[0].Database.Text);
			Assert.AreEqual("`table1`", ds.OldNames[0].Name.Text);
			Assert.AreEqual("`schema2`", ds.NewNames[0].Database.Text);
			Assert.AreEqual("`table2`", ds.NewNames[0].Name.Text);
			 * */
		}

		[Test]
		public void MissingFromTableName()
		{
			MySQL51Parser.program_return r = Utility.ParseSql("RENAME TABLE", true);
		}

		[Test]
		public void MissingToTableName()
		{
			MySQL51Parser.program_return r = Utility.ParseSql("RENAME TABLE table1 TO", true);
		}

		[Test]
		public void MultipleRenames()
		{
			MySQL51Parser.program_return r = Utility.ParseSql(
				@"RENAME TABLE table1 TO table2, schema1.table4 TO table5, 
				`schema3`.table6 TO `schema7`.table8");
			/*
			Assert.AreEqual(1, statements.Count);
			Assert.IsTrue(statements[0] is RenameTableStatement);
			RenameTableStatement ds = statements[0] as RenameTableStatement;

			Assert.AreEqual(3, ds.OldNames.Count);
			Assert.AreEqual(3, ds.NewNames.Count);

			Assert.IsNull(ds.OldNames[0].Database);
			Assert.AreEqual("table1", ds.OldNames[0].Name.Text);
			Assert.IsNull(ds.NewNames[0].Database);
			Assert.AreEqual("table2", ds.NewNames[0].Name.Text);

			Assert.AreEqual("schema1", ds.OldNames[1].Database.Text);
			Assert.AreEqual("table4", ds.OldNames[1].Name.Text);
			Assert.IsNull(ds.NewNames[1].Database);
			Assert.AreEqual("table5", ds.NewNames[1].Name.Text);

			Assert.AreEqual("`schema3`", ds.OldNames[2].Database.Text);
			Assert.AreEqual("table6", ds.OldNames[2].Name.Text);
			Assert.AreEqual("`schema7`", ds.NewNames[2].Database.Text);
			Assert.AreEqual("table8", ds.NewNames[2].Name.Text);
			 * */
		}
	}
}
