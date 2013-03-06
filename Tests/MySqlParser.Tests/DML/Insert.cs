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
//using MySQLParser;

namespace MySql.Parser.Tests
{
	[TestFixture]
	public class Insert
	{
		[Test]
		public void Simple()
		{			
			
			MySQL51Parser.program_return r = Utility.ParseSql(
				"insert into tableA ( col1, col2, col3 ) values ( 'a', tableB.colx, 4.55 )");
		}

		[Test]
		public void WithSelect()
		{
			MySQL51Parser.program_return r = Utility.ParseSql(
				"insert into tableA ( col1, col2, col3 ) select 'a', tableB.colx, 4.55 from tableB");
		}

[Test]
		public void WithSelect2()
		{
			MySQL51Parser.program_return r = Utility.ParseSql(
                @"INSERT INTO t2
SELECT
a.X +
a.AUTO_INCR_PK - 
b.FIRST_KEY_IN_SERIES AS ID
FROM
t1
INNER JOIN
(
SELECT
X,
MIN(AUTO_INCR_PK) AS FIRST_KEY_IN_SERIES
FROM
t1
GROUP BY
X
) AS b
USING
(X)");
		}

      [Test]
	  public void WithSelect3()
	  {
		  MySQL51Parser.program_return r = Utility.ParseSql(
              "INSERT INTO table2 (field1, field2, field3, field4) (SELECT 'value1 from user input', field1, field2, field3 from table1)");
	  }

      [Test]
	  public void WithoutColumns()
	  {
		  MySQL51Parser.program_return r = Utility.ParseSql(
              "insert into test3 values (1), (2), (3)");
	  }

      [Test]
      public void WithPartition_55()
      {
        StringBuilder sb;
        MySQL51Parser.program_return r = Utility.ParseSql(
          @"INSERT INTO employees_copy SELECT * FROM employees PARTITION (p2);	", true, out sb, new Version(5, 5));
        Assert.IsTrue(sb.ToString().IndexOf("missing endoffile", StringComparison.OrdinalIgnoreCase) != -1);
      }

      [Test]
      public void WithPartition_56()
      {
        StringBuilder sb;
        MySQL51Parser.program_return r = Utility.ParseSql(
          @"INSERT INTO employees_copy SELECT * FROM employees PARTITION (p2);	", false, out sb, new Version(5, 6));
      }

      [Test]
      public void WithPartition_2_55()
      {
        StringBuilder sb;
        MySQL51Parser.program_return r = Utility.ParseSql(
          @"INSERT INTO employees PARTITION (p3) VALUES (20, 'Jan', 'Jones', 1, 3);	", true, out sb, new Version(5, 5));
        Assert.IsTrue(sb.ToString().IndexOf("no viable alternative at input 'PARTITION'", StringComparison.OrdinalIgnoreCase) != -1);
      }

      [Test]
      public void WithPartition_2_56()
      {
        StringBuilder sb;
        MySQL51Parser.program_return r = Utility.ParseSql(
          @"INSERT INTO employees PARTITION (p3) VALUES (20, 'Jan', 'Jones', 1, 3);	", false, out sb, new Version(5, 6));
      }
	}
}
