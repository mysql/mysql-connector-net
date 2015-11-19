// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.Text;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class Tokenizer : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;

    public void SetFixture(SetUpClass data)
    {
      st = data;
    }

    public void Dispose()
    {
      st.execSQL("DROP TABLE IF EXISTS TEST");
    }

    [Fact]
    public void Simple()
    {
      SqlTokenizer tokenizer = new SqlTokenizer("SELECT * FROM Test");
      Assert.Equal("SELECT", tokenizer.NextToken());
      Assert.Equal("*", tokenizer.NextToken());
      Assert.Equal("FROM", tokenizer.NextToken());
      Assert.Equal("Test", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
    }

    [Fact]
    public void DashSingleLineComment()
    {
      string comment = "-- this is my comment\r\n";
      string sql = String.Format("SELECT {0} * FROM Test", comment);
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.Equal("SELECT", tokenizer.NextToken());
      Assert.Equal(comment, tokenizer.NextToken());
      Assert.Equal("*", tokenizer.NextToken());
      Assert.Equal("FROM", tokenizer.NextToken());
      Assert.Equal("Test", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());

      tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.Equal("SELECT", tokenizer.NextToken());
      Assert.Equal("*", tokenizer.NextToken());
      Assert.Equal("FROM", tokenizer.NextToken());
      Assert.Equal("Test", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
    }

    [Fact]
    public void HashSingleLineComment()
    {
      string comment = "#this is my comment\r\n";
      string sql = String.Format("SELECT {0} * FROM Test", comment);
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.Equal("SELECT", tokenizer.NextToken());
      Assert.Equal(comment, tokenizer.NextToken());
      Assert.Equal("*", tokenizer.NextToken());
      Assert.Equal("FROM", tokenizer.NextToken());
      Assert.Equal("Test", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());

      tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.Equal("SELECT", tokenizer.NextToken());
      Assert.Equal("*", tokenizer.NextToken());
      Assert.Equal("FROM", tokenizer.NextToken());
      Assert.Equal("Test", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
    }

    [Fact]
    public void MultiLineComment()
    {
      string comment = "/* this is my comment \r\n lines 2 \r\n line 3*/";
      string sql = String.Format("SELECT{0} * FROM Test", comment);
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.Equal("SELECT", tokenizer.NextToken());
      Assert.Equal(comment.Trim(), tokenizer.NextToken());
      Assert.Equal("*", tokenizer.NextToken());
      Assert.Equal("FROM", tokenizer.NextToken());
      Assert.Equal("Test", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());

      tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.Equal("SELECT", tokenizer.NextToken());
      Assert.Equal("*", tokenizer.NextToken());
      Assert.Equal("FROM", tokenizer.NextToken());
      Assert.Equal("Test", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
    }

    [Fact]
    public void Parameter()
    {
      string sql = "SELECT * FROM Test WHERE id=@id AND id2=?id2";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.Equal("SELECT", tokenizer.NextToken());
      Assert.Equal("*", tokenizer.NextToken());
      Assert.Equal("FROM", tokenizer.NextToken());
      Assert.Equal("Test", tokenizer.NextToken());
      Assert.Equal("WHERE", tokenizer.NextToken());
      Assert.Equal("id", tokenizer.NextToken());
      Assert.Equal("=", tokenizer.NextToken());
      Assert.Equal("@id", tokenizer.NextToken());
      Assert.Equal("AND", tokenizer.NextToken());
      Assert.Equal("id2", tokenizer.NextToken());
      Assert.Equal("=", tokenizer.NextToken());
      Assert.Equal("?id2", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
    }

    [Fact]
    public void NextParameter()
    {
      string sql = "SELECT * FROM Test WHERE id=@id AND id2=?id2";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.Equal("@id", tokenizer.NextParameter());
      Assert.Equal("?id2", tokenizer.NextParameter());
      Assert.Null(tokenizer.NextParameter());
    }

    [Fact]
    public void ParameterWithSpecialCharacters()
    {
      string sql = "SELECT * FROM Test WHERE id=@id_$123";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = true;
      Assert.Equal("SELECT", tokenizer.NextToken());
      Assert.Equal("*", tokenizer.NextToken());
      Assert.Equal("FROM", tokenizer.NextToken());
      Assert.Equal("Test", tokenizer.NextToken());
      Assert.Equal("WHERE", tokenizer.NextToken());
      Assert.Equal("id", tokenizer.NextToken());
      Assert.Equal("=", tokenizer.NextToken());
      Assert.Equal("@id_$123", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
    }

    [Fact]
    public void StringLiteral()
    {
      string sql = "SELECT 'a', 1, 'b'";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.Equal("SELECT", tokenizer.NextToken());
      Assert.Equal("'a'", tokenizer.NextToken());
      Assert.Equal(",", tokenizer.NextToken());
      Assert.Equal("1", tokenizer.NextToken());
      Assert.Equal(",", tokenizer.NextToken());
      Assert.Equal("'b'", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
    }

    [Fact]
    public void UserVariable()
    {
      string sql = "SELECT 'a', 1, @@myVar";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.ReturnComments = false;
      Assert.Equal("SELECT", tokenizer.NextToken());
      Assert.Equal("'a'", tokenizer.NextToken());
      Assert.Equal(",", tokenizer.NextToken());
      Assert.Equal("1", tokenizer.NextToken());
      Assert.Equal(",", tokenizer.NextToken());
      Assert.Equal("@@myVar", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
    }

    [Fact]
    public void AnsiQuotes()
    {
      string sql = "SELECT 'a', \"a\", `a`";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.AnsiQuotes = false;
      Assert.Equal("SELECT", tokenizer.NextToken());
      Assert.Equal("'a'", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
      Assert.Equal(",", tokenizer.NextToken());
      Assert.Equal("\"a\"", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
      Assert.Equal(",", tokenizer.NextToken());
      Assert.Equal("`a`", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
      Assert.Null(tokenizer.NextToken());
    }

    [Fact]
    public void ParseProcBody()
    {
      string sql = "CREATE PROCEDURE spTest(testid INT, testname VARCHAR(20)) BEGIN SELECT 1; END";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.AnsiQuotes = false;
      Assert.Equal("CREATE", tokenizer.NextToken());
      Assert.Equal("PROCEDURE", tokenizer.NextToken());
      Assert.Equal("spTest", tokenizer.NextToken());
      Assert.Equal("(", tokenizer.NextToken());
      Assert.Equal("testid", tokenizer.NextToken());
      Assert.Equal("INT", tokenizer.NextToken());
      Assert.Equal(",", tokenizer.NextToken());
      Assert.Equal("testname", tokenizer.NextToken());
      Assert.Equal("VARCHAR", tokenizer.NextToken());
      Assert.Equal("(", tokenizer.NextToken());
      Assert.Equal("20", tokenizer.NextToken());
      Assert.Equal(")", tokenizer.NextToken());
      Assert.Equal(")", tokenizer.NextToken());
      Assert.Equal("BEGIN", tokenizer.NextToken());
      Assert.Equal("SELECT", tokenizer.NextToken());
      Assert.Equal("1", tokenizer.NextToken());
      Assert.Equal(";", tokenizer.NextToken());
      Assert.Equal("END", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
    }

    /// <summary>
    /// Bug #44318	Tokenizer
    /// </summary>
    [Fact]
    public void NoSpaceAroundEquals()
    {
      st.execSQL("DROP TABLE IF EXISTS Test");
      st.execSQL("CREATE TABLE Test(name VARCHAR(40))");

      MySqlCommand cmd = new MySqlCommand("INSERT INTO Test SET name='test -- test';", st.conn);
      cmd.ExecuteNonQuery();
      cmd.CommandText = "SELECT name FROM Test";
      object o = cmd.ExecuteScalar();
      Assert.Equal("test -- test", o);

      cmd.CommandText = "UPDATE Test SET name='Can you explain this ?';";
      cmd.ExecuteNonQuery();
      cmd.CommandText = "SELECT name FROM Test";
      o = cmd.ExecuteScalar();
      Assert.Equal("Can you explain this ?", o);
    }

    [Fact]
    public void Slash()
    {
      string sql = "AND // OR";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.AnsiQuotes = false;
      Assert.Equal("AND", tokenizer.NextToken());
      Assert.Equal("/", tokenizer.NextToken());
      Assert.Equal("/", tokenizer.NextToken());
      Assert.Equal("OR", tokenizer.NextToken());
      Assert.Null(tokenizer.NextToken());
    }

    [Fact]
    public void SqlServerMode()
    {
      string sql = "SELECT `a`, [id], [name] FROM [Fact]";
      SqlTokenizer tokenizer = new SqlTokenizer(sql);
      tokenizer.SqlServerMode = true;
      tokenizer.NextToken();
      Assert.Equal("`a`", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
      tokenizer.NextToken();  // read ,
      Assert.Equal("[id]", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
      tokenizer.NextToken();  // read ,
      Assert.Equal("[name]", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
      tokenizer.NextToken();  // read FROM
      Assert.Equal("[Fact]", tokenizer.NextToken());
      Assert.True(tokenizer.Quoted);
    }
  }
}
