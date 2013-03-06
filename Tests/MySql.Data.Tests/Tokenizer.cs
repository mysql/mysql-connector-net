// Copyright © 2004, 2010, Oracle and/or its affiliates. All rights reserved.
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
using System.Data;
using System.IO;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MySql.Data.MySqlClient.Tests
{
  [TestFixture]
  public class Tokenizer : BaseTest
  {

#if !CF
    [Test]
    public void Simple()
    {
      SqlTokenizer tokenizer = new SqlTokenizer("SELECT * FROM Test");
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("*", tokenizer.NextToken());
      Assert.AreEqual("FROM", tokenizer.NextToken());
      Assert.AreEqual("Test", tokenizer.NextToken());
      Assert.IsNull(tokenizer.NextToken());
    }

    [Test]
    public void DashSingleLineComment()
    {
      string comment = "-- this is my comment\r\n";
      string sql = String.Format("SELECT {0} * FROM Test", comment);
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual(comment, tokenizer.NextToken());
      Assert.AreEqual("*", tokenizer.NextToken());
      Assert.AreEqual("FROM", tokenizer.NextToken());
      Assert.AreEqual("Test", tokenizer.NextToken());
      Assert.IsNull(tokenizer.NextToken());

      tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("*", tokenizer.NextToken());
      Assert.AreEqual("FROM", tokenizer.NextToken());
      Assert.AreEqual("Test", tokenizer.NextToken());
      Assert.IsNull(tokenizer.NextToken());
    }

    [Test]
    public void HashSingleLineComment()
    {
      string comment = "#this is my comment\r\n";
      string sql = String.Format("SELECT {0} * FROM Test", comment);
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual(comment, tokenizer.NextToken());
      Assert.AreEqual("*", tokenizer.NextToken());
      Assert.AreEqual("FROM", tokenizer.NextToken());
      Assert.AreEqual("Test", tokenizer.NextToken());
      Assert.IsNull(tokenizer.NextToken());

      tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("*", tokenizer.NextToken());
      Assert.AreEqual("FROM", tokenizer.NextToken());
      Assert.AreEqual("Test", tokenizer.NextToken());
      Assert.IsNull(tokenizer.NextToken());
    }

    [Test]
    public void MultiLineComment()
    {
      string comment = "/* this is my comment \r\n lines 2 \r\n line 3*/";
      string sql = String.Format("SELECT{0} * FROM Test", comment);
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual(comment.Trim(), tokenizer.NextToken());
      Assert.AreEqual("*", tokenizer.NextToken());
      Assert.AreEqual("FROM", tokenizer.NextToken());
      Assert.AreEqual("Test", tokenizer.NextToken());
      Assert.IsNull(tokenizer.NextToken());

      tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("*", tokenizer.NextToken());
      Assert.AreEqual("FROM", tokenizer.NextToken());
      Assert.AreEqual("Test", tokenizer.NextToken());
      Assert.IsNull(tokenizer.NextToken());
    }

    [Test]
    public void Parameter()
    {
      string sql = "SELECT * FROM Test WHERE id=@id AND id2=?id2";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("*", tokenizer.NextToken());
      Assert.AreEqual("FROM", tokenizer.NextToken());
      Assert.AreEqual("Test", tokenizer.NextToken());
      Assert.AreEqual("WHERE", tokenizer.NextToken());
      Assert.AreEqual("id", tokenizer.NextToken());
      Assert.AreEqual("=", tokenizer.NextToken());
      Assert.AreEqual("@id", tokenizer.NextToken());
      Assert.AreEqual("AND", tokenizer.NextToken());
      Assert.AreEqual("id2", tokenizer.NextToken());
      Assert.AreEqual("=", tokenizer.NextToken());
      Assert.AreEqual("?id2", tokenizer.NextToken());
      Assert.IsNull(tokenizer.NextToken());
    }

    [Test]
    public void NextParameter()
    {
      string sql = "SELECT * FROM Test WHERE id=@id AND id2=?id2";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.AreEqual("@id", tokenizer.NextParameter());
      Assert.AreEqual("?id2", tokenizer.NextParameter());
      Assert.IsNull(tokenizer.NextParameter());
    }

    [Test]
    public void ParameterWithSpecialCharacters()
    {
      string sql = "SELECT * FROM Test WHERE id=@id_$123";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("*", tokenizer.NextToken());
      Assert.AreEqual("FROM", tokenizer.NextToken());
      Assert.AreEqual("Test", tokenizer.NextToken());
      Assert.AreEqual("WHERE", tokenizer.NextToken());
      Assert.AreEqual("id", tokenizer.NextToken());
      Assert.AreEqual("=", tokenizer.NextToken());
      Assert.AreEqual("@id_$123", tokenizer.NextToken());
      Assert.IsNull(tokenizer.NextToken());
    }

    [Test]
    public void StringLiteral()
    {
      string sql = "SELECT 'a', 1, 'b'";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("'a'", tokenizer.NextToken());
      Assert.AreEqual(",", tokenizer.NextToken());
      Assert.AreEqual("1", tokenizer.NextToken());
      Assert.AreEqual(",", tokenizer.NextToken());
      Assert.AreEqual("'b'", tokenizer.NextToken());
      Assert.IsNull(tokenizer.NextToken());
    }

    [Test]
    public void UserVariable()
    {
      string sql = "SELECT 'a', 1, @@myVar";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("'a'", tokenizer.NextToken());
      Assert.AreEqual(",", tokenizer.NextToken());
      Assert.AreEqual("1", tokenizer.NextToken());
      Assert.AreEqual(",", tokenizer.NextToken());
      Assert.AreEqual("@@myVar", tokenizer.NextToken());
      Assert.IsNull(tokenizer.NextToken());
    }

    [Test]
    public void AnsiQuotes()
    {
      string sql = "SELECT 'a', \"a\", `a`";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.AnsiQuotes = false;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("'a'", tokenizer.NextToken());
      Assert.IsTrue(tokenizer.Quoted);
      Assert.AreEqual(",", tokenizer.NextToken());
      Assert.AreEqual("\"a\"", tokenizer.NextToken());
      Assert.IsTrue(tokenizer.Quoted);
      Assert.AreEqual(",", tokenizer.NextToken());
      Assert.AreEqual("`a`", tokenizer.NextToken());
      Assert.IsTrue(tokenizer.Quoted);
      Assert.IsNull(tokenizer.NextToken());
    }

    [Test]
    public void ParseProcBody()
    {
      string sql = "CREATE PROCEDURE spTest(testid INT, testname VARCHAR(20)) BEGIN SELECT 1; END";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.AnsiQuotes = false;
      Assert.AreEqual("CREATE", tokenizer.NextToken());
      Assert.AreEqual("PROCEDURE", tokenizer.NextToken());
      Assert.AreEqual("spTest", tokenizer.NextToken());
      Assert.AreEqual("(", tokenizer.NextToken());
      Assert.AreEqual("testid", tokenizer.NextToken());
      Assert.AreEqual("INT", tokenizer.NextToken());
      Assert.AreEqual(",", tokenizer.NextToken());
      Assert.AreEqual("testname", tokenizer.NextToken());
      Assert.AreEqual("VARCHAR", tokenizer.NextToken());
      Assert.AreEqual("(", tokenizer.NextToken());
      Assert.AreEqual("20", tokenizer.NextToken());
      Assert.AreEqual(")", tokenizer.NextToken());
      Assert.AreEqual(")", tokenizer.NextToken());
      Assert.AreEqual("BEGIN", tokenizer.NextToken());
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("1", tokenizer.NextToken());
      Assert.AreEqual(";", tokenizer.NextToken());
      Assert.AreEqual("END", tokenizer.NextToken());
      Assert.IsNull(tokenizer.NextToken());
    }
#endif

    /// <summary>
    /// Bug #44318	Tokenizer
    /// </summary>
    [Test]
    public void NoSpaceAroundEquals()
    {
      execSQL("DROP TABLE IF EXISTS Test");
      execSQL("CREATE TABLE Test(name VARCHAR(40))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test SET name='test -- test';", conn);
      cmd.ExecuteNonQuery();
      cmd.CommandText = "SELECT name FROM Test";
      object o = cmd.ExecuteScalar();
      Assert.AreEqual("test -- test", o);

      cmd.CommandText = "UPDATE Test SET name='Can you explain this ?';";
      cmd.ExecuteNonQuery();
      cmd.CommandText = "SELECT name FROM Test";
      o = cmd.ExecuteScalar();
      Assert.AreEqual("Can you explain this ?", o);
    }

    [Test]
    public void Slash()
    {
      string sql = "AND // OR";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.AnsiQuotes = false;
      Assert.AreEqual("AND", tokenizer.NextToken());
      Assert.AreEqual("/", tokenizer.NextToken());
      Assert.AreEqual("/", tokenizer.NextToken());
      Assert.AreEqual("OR", tokenizer.NextToken());
      Assert.IsNull(tokenizer.NextToken());
    }

    [Test]
    public void SqlServerMode()
    {
      string sql = "SELECT `a`, [id], [name] FROM [test]";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.SqlServerMode = true;
      tokenizer.NextToken();
      Assert.AreEqual("`a`", tokenizer.NextToken());
      Assert.IsTrue(tokenizer.Quoted);
      tokenizer.NextToken();  // read ,
      Assert.AreEqual("[id]", tokenizer.NextToken());
      Assert.IsTrue(tokenizer.Quoted);
      tokenizer.NextToken();  // read ,
      Assert.AreEqual("[name]", tokenizer.NextToken());
      Assert.IsTrue(tokenizer.Quoted);
      tokenizer.NextToken();  // read FROM
      Assert.AreEqual("[test]", tokenizer.NextToken());
      Assert.IsTrue(tokenizer.Quoted);
    }
  }
}
