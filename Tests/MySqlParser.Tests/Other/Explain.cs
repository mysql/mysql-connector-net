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
  public class Explain
  {
    [Test]
    public void Explain1()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("explain tbl", false);
    }

    [Test]
    public void Explain2()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        "explain extended select * from tbl", false, out sb, new Version(5, 1));
    }

    [Test]
    public void Explain3()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        "explain partitions select * from tbl", true, out sb, new Version(5, 0));
      Assert.IsTrue(sb.ToString().IndexOf("no viable alternative at input 'partitions'", StringComparison.OrdinalIgnoreCase ) != -1);
    }

    [Test]
    public void Explain4()
    {
      MySQL51Parser.program_return r = Utility.ParseSql("explain partitions select * from tbl", false);
    }

    [Test]
    public void ExplainDelete_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        "explain DELETE from t1;", true, out sb, new Version(5, 5));
      Assert.IsTrue(sb.ToString().IndexOf("delete", StringComparison.OrdinalIgnoreCase) != -1);
    }

    [Test]
    public void ExplainDelete_56()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        "explain DELETE from t1;", false, out sb, new Version(5, 6));
    }

    [Test]
    public void ExplainInsert_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        "explain INSERT into t1 ( col1, col2 ) values ( '', 1 );", true, out sb, new Version(5, 5));
      Assert.IsTrue(sb.ToString().IndexOf("insert", StringComparison.OrdinalIgnoreCase) != -1);
    }

    [Test]
    public void ExplainInsert_56()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        "explain INSERT into t1 ( col1, col2 ) values ( '', 1 );", false, out sb, new Version(5, 6));
    }

    [Test]
    public void ExplainReplace_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        "explain format = json REPLACE into t1 ( col1, col2 ) values ( '', 1 );", true, out sb, new Version(5, 5));
      Assert.IsTrue(sb.ToString().IndexOf("missing EndOfFile at '='", StringComparison.OrdinalIgnoreCase) != -1);
    }

    [Test]
    public void ExplainReplace_56()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        "explain format = json REPLACE into t1 ( col1, col2 ) values ( '', 1 );", false, out sb, new Version(5, 6));
    }

    [Test]
    public void ExplainUpdate_55()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        "explain format = traditional UPDATE t1 set col1 = val1, col2 = val2;", true, out sb, new Version(5, 5));
      Assert.IsTrue(sb.ToString().IndexOf("missing EndOfFile at '='", StringComparison.OrdinalIgnoreCase) != -1);
    }

    [Test]
    public void ExplainUpdate_56()
    {
      StringBuilder sb;
      MySQL51Parser.program_return r = Utility.ParseSql(
        "explain format = traditional UPDATE t1 set col1 = val1, col2 = val2;", false, out sb, new Version(5, 6));
    }
  }
}
