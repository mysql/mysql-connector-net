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
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using NUnit.Framework;


namespace MySql.Parser.Tests
{
  [TestFixture]
  public class Replace
  {
    [Test]
    public void Simple()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"REPLACE INTO T SELECT * FROM T;");
    }

    [Test]
    public void Simple2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"REPLACE DELAYED INTO `online_users` 
SET `session_id`='3580cc4e61117c0785372c426eddd11c', 
`user_id` = 'XXX', `page` = '/', `lastview` = NOW();");
    }

    [Test]
    public void WithPartition_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"replace into employees PARTITION (p3) VALUES (20, 'Jan', 'Jones', 1, 3);", true, out sb, new Version(5, 5));
      Assert.IsTrue(sb.ToString().IndexOf("partition", StringComparison.OrdinalIgnoreCase ) != -1);
    }

    [Test]
    public void WithPartition_56()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        @"replace into employees PARTITION (p3) VALUES (20, 'Jan', 'Jones', 1, 3);", false, out sb, new Version(5, 6));
    }
  }
}
