// Copyright (c) 2013, 2020, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using NUnit.Framework;

namespace MySql.Data.MySqlClient.Tests
{
  public class Tokenizer : TestBase
  {
    [Test]
    public void Simple()
    {
      SqlTokenizer tokenizer = new SqlTokenizer("SELECT * FROM Test");
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("*", tokenizer.NextToken());
      Assert.AreEqual("FROM", tokenizer.NextToken());
      Assert.AreEqual("Test", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
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
      Assert.Null(tokenizer.NextToken());

      tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("*", tokenizer.NextToken());
      Assert.AreEqual("FROM", tokenizer.NextToken());
      Assert.AreEqual("Test", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
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
      Assert.Null(tokenizer.NextToken());

      tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("*", tokenizer.NextToken());
      Assert.AreEqual("FROM", tokenizer.NextToken());
      Assert.AreEqual("Test", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
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
      Assert.Null(tokenizer.NextToken());

      tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("*", tokenizer.NextToken());
      Assert.AreEqual("FROM", tokenizer.NextToken());
      Assert.AreEqual("Test", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
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
      Assert.Null(tokenizer.NextToken());
    }

    [Test]
    public void NextParameter()
    {
      string sql = "SELECT * FROM Test WHERE id=@id AND id2=?id2";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.AreEqual("@id", tokenizer.NextParameter());
      Assert.AreEqual("?id2", tokenizer.NextParameter());
      Assert.Null(tokenizer.NextParameter());
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
      Assert.Null(tokenizer.NextToken());
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
      Assert.Null(tokenizer.NextToken());
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
      Assert.Null(tokenizer.NextToken());
    }

    [Test]
    public void AnsiQuotes()
    {
      string sql = "SELECT 'a', \"a\", `a`";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.AnsiQuotes = false;
      Assert.AreEqual("SELECT", tokenizer.NextToken());
      Assert.AreEqual("'a'", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
      Assert.AreEqual(",", tokenizer.NextToken());
      Assert.AreEqual("\"a\"", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
      Assert.AreEqual(",", tokenizer.NextToken());
      Assert.AreEqual("`a`", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
      Assert.Null(tokenizer.NextToken());
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
      Assert.Null(tokenizer.NextToken());
    }

    /// <summary>
    /// Bug #44318	Tokenizer
    /// </summary>
    [Test]
    public void NoSpaceAroundEquals()
    {
      ExecuteSQL("DROP TABLE IF EXISTS Test");
      ExecuteSQL("CREATE TABLE Test(name VARCHAR(40))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test SET name='test -- test';", Connection);
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
      Assert.Null(tokenizer.NextToken());
    }

    [Test]
    public void SqlServerMode()
    {
      string sql = "SELECT `a`, [id], [name] FROM [Test]";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.SqlServerMode = true;
      tokenizer.NextToken();
      Assert.AreEqual("`a`", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
      tokenizer.NextToken();  // read ,
      Assert.AreEqual("[id]", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
      tokenizer.NextToken();  // read ,
      Assert.AreEqual("[name]", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
      tokenizer.NextToken();  // read FROM
      Assert.AreEqual("[Test]", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
    }
  }
}
