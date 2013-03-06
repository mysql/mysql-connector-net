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
  public class GatheringTables
  {
    [Test]
    public void InsertWithTableExtraction()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          "INSERT INTO test6.d_table VALUES (1);");
      List<TableWithAlias> twa = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, twa);
      Assert.AreEqual(1, twa.Count);
      Assert.AreEqual("test6", twa[0].Database);
      Assert.AreEqual("d_table", twa[0].TableName);
    }

    [Test]
    public void InsertWithTableExtraction2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          "INSERT INTO `test6`.`d_table` VALUES (1);");
      List<TableWithAlias> twa = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, twa);
      Assert.AreEqual(1, twa.Count);
      Assert.AreEqual("test6", twa[0].Database);
      Assert.AreEqual("d_table", twa[0].TableName);
      Assert.IsTrue(string.IsNullOrEmpty(twa[0].Alias));
    }

    [Test]
    public void InsertWithTableExtraction3()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          "INSERT INTO test6.d_table ( col ) VALUES (1);");
      List<TableWithAlias> twa = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, twa);
      Assert.AreEqual(1, twa.Count);
      Assert.AreEqual("test6", twa[0].Database);
      Assert.AreEqual("d_table", twa[0].TableName);
    }

    [Test]
    public void SelectWithTableExtraction()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          "select * from test6.d_table;");
      List<TableWithAlias> twa = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, twa);
      Assert.AreEqual(1, twa.Count);
      Assert.AreEqual("test6", twa[0].Database);
      Assert.AreEqual("d_table", twa[0].TableName);
      Assert.IsTrue(string.IsNullOrEmpty(twa[0].Alias));
    }

    [Test]
    public void SelectWithTableExtraction2()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          "select * from test6.d_table as T;");
      List<TableWithAlias> twa = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, twa);
      Assert.AreEqual(1, twa.Count);
      Assert.AreEqual("test6", twa[0].Database);
      Assert.AreEqual("d_table", twa[0].TableName);
      Assert.AreEqual("T", twa[0].Alias);
    }

    [Test]
    public void SelectWithTableExtraction3()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          "select * from d_table as T;");
      List<TableWithAlias> twa = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, twa);
      Assert.AreEqual(1, twa.Count);
      Assert.IsTrue(string.IsNullOrEmpty(twa[0].Database));
      Assert.AreEqual("d_table", twa[0].TableName);
      Assert.AreEqual("T", twa[0].Alias);
    }

    [Test]
    public void SelectWithTableExtraction4()
    {
      MySQL51Parser.program_return r = Utility.ParseSql(
          "select * from d_table;");
      List<TableWithAlias> twa = new List<TableWithAlias>();
      ParserUtils.GetTables((ITree)r.Tree, twa);
      Assert.AreEqual(1, twa.Count);
      Assert.IsTrue(string.IsNullOrEmpty(twa[0].Database));
      Assert.AreEqual("d_table", twa[0].TableName);
      Assert.IsTrue(string.IsNullOrEmpty(twa[0].Alias));
    }
  }
}
