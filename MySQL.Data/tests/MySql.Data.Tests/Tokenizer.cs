// Copyright © 2013, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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
using NUnit.Framework.Legacy;

namespace MySql.Data.MySqlClient.Tests
{
  public class Tokenizer : TestBase
  {
    [Test]
    public void Simple()
    {
      SqlTokenizer tokenizer = new SqlTokenizer("SELECT * FROM Test");
      Assert.That(tokenizer.NextToken(), Is.EqualTo("SELECT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("*"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("FROM"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("Test"));
      Assert.That(tokenizer.NextToken(), Is.Null);
    }

    [Test]
    public void DashSingleLineComment()
    {
      string comment = "-- this is my comment\r\n";
      string sql = String.Format("SELECT {0} * FROM Test", comment);
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.That(tokenizer.NextToken(), Is.EqualTo("SELECT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo(comment));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("*"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("FROM"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("Test"));
      Assert.That(tokenizer.NextToken(), Is.Null);

      tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.That(tokenizer.NextToken(), Is.EqualTo("SELECT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("*"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("FROM"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("Test"));
      Assert.That(tokenizer.NextToken(), Is.Null);
    }

    [Test]
    public void HashSingleLineComment()
    {
      string comment = "#this is my comment\r\n";
      string sql = String.Format("SELECT {0} * FROM Test", comment);
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.That(tokenizer.NextToken(), Is.EqualTo("SELECT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo(comment));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("*"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("FROM"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("Test"));
      Assert.That(tokenizer.NextToken(), Is.Null);

      tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.That(tokenizer.NextToken(), Is.EqualTo("SELECT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("*"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("FROM"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("Test"));
      Assert.That(tokenizer.NextToken(), Is.Null);
    }

    [Test]
    public void MultiLineComment()
    {
      string comment = "/* this is my comment \r\n lines 2 \r\n line 3*/";
      string sql = String.Format("SELECT{0} * FROM Test", comment);
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.That(tokenizer.NextToken(), Is.EqualTo("SELECT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo(comment.Trim()));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("*"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("FROM"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("Test"));
      Assert.That(tokenizer.NextToken(), Is.Null);

      tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.That(tokenizer.NextToken(), Is.EqualTo("SELECT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("*"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("FROM"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("Test"));
      Assert.That(tokenizer.NextToken(), Is.Null);
    }

    [Test]
    public void Parameter()
    {
      string sql = "SELECT * FROM Test WHERE id=@id AND id2=?id2";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.That(tokenizer.NextToken(), Is.EqualTo("SELECT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("*"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("FROM"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("Test"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("WHERE"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("id"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("="));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("@id"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("AND"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("id2"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("="));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("?id2"));
      Assert.That(tokenizer.NextToken(), Is.Null);
    }

    [Test]
    public void NextParameter()
    {
      string sql = "SELECT * FROM Test WHERE id=@id AND id2=?id2";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.That(tokenizer.NextParameter(), Is.EqualTo("@id"));
      Assert.That(tokenizer.NextParameter(), Is.EqualTo("?id2"));
      Assert.That(tokenizer.NextParameter(), Is.Null);
    }

    [Test]
    public void ParameterWithSpecialCharacters()
    {
      string sql = "SELECT * FROM Test WHERE id=@id_$123";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.That(tokenizer.NextToken(), Is.EqualTo("SELECT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("*"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("FROM"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("Test"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("WHERE"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("id"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("="));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("@id_$123"));
      Assert.That(tokenizer.NextToken(), Is.Null);
    }

    [Test]
    public void StringLiteral()
    {
      string sql = "SELECT 'a', 1, 'b'";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.That(tokenizer.NextToken(), Is.EqualTo("SELECT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("'a'"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo(","));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("1"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo(","));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("'b'"));
      Assert.That(tokenizer.NextToken(), Is.Null);
    }

    [Test]
    public void UserVariable()
    {
      string sql = "SELECT 'a', 1, @@myVar";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.That(tokenizer.NextToken(), Is.EqualTo("SELECT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("'a'"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo(","));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("1"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo(","));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("@@myVar"));
      Assert.That(tokenizer.NextToken(), Is.Null);
    }

    [Test]
    public void AnsiQuotes()
    {
      string sql = "SELECT 'a', \"a\", `a`";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.AnsiQuotes = false;
      Assert.That(tokenizer.NextToken(), Is.EqualTo("SELECT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("'a'"));
      Assert.That(tokenizer.Quoted);
      Assert.That(tokenizer.NextToken(), Is.EqualTo(","));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("\"a\""));
      Assert.That(tokenizer.Quoted);
      Assert.That(tokenizer.NextToken(), Is.EqualTo(","));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("`a`"));
      Assert.That(tokenizer.Quoted);
      Assert.That(tokenizer.NextToken(), Is.Null);
    }

    [Test]
    public void ParseProcBody()
    {
      string sql = "CREATE PROCEDURE spTest(testid INT, testname VARCHAR(20)) BEGIN SELECT 1; END";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.AnsiQuotes = false;
      Assert.That(tokenizer.NextToken(), Is.EqualTo("CREATE"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("PROCEDURE"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("spTest"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("("));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("testid"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("INT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo(","));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("testname"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("VARCHAR"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("("));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("20"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo(")"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo(")"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("BEGIN"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("SELECT"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("1"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo(";"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("END"));
      Assert.That(tokenizer.NextToken(), Is.Null);
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
      Assert.That(o, Is.EqualTo("test -- test"));

      cmd.CommandText = "UPDATE Test SET name='Can you explain this ?';";
      cmd.ExecuteNonQuery();
      cmd.CommandText = "SELECT name FROM Test";
      o = cmd.ExecuteScalar();
      Assert.That(o, Is.EqualTo("Can you explain this ?"));
    }

    [Test]
    public void Slash()
    {
      string sql = "AND // OR";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.AnsiQuotes = false;
      Assert.That(tokenizer.NextToken(), Is.EqualTo("AND"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("/"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("/"));
      Assert.That(tokenizer.NextToken(), Is.EqualTo("OR"));
      Assert.That(tokenizer.NextToken(), Is.Null);
    }

    [Test]
    public void SqlServerMode()
    {
      string sql = "SELECT `a`, [id], [name] FROM [Test]";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.SqlServerMode = true;
      tokenizer.NextToken();
      Assert.That(tokenizer.NextToken(), Is.EqualTo("`a`"));
      Assert.That(tokenizer.Quoted);
      tokenizer.NextToken();  // read ,
      Assert.That(tokenizer.NextToken(), Is.EqualTo("[id]"));
      Assert.That(tokenizer.Quoted);
      tokenizer.NextToken();  // read ,
      Assert.That(tokenizer.NextToken(), Is.EqualTo("[name]"));
      Assert.That(tokenizer.Quoted);
      tokenizer.NextToken();  // read FROM
      Assert.That(tokenizer.NextToken(), Is.EqualTo("[Test]"));
      Assert.That(tokenizer.Quoted);
    }
  }
}
