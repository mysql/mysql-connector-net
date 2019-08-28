// Copyright (c) 2009, 2018, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;


namespace MySql.Data.Common
{
  internal class QueryNormalizer
  {
    private static readonly List<string> Keywords = new List<string>();
    private readonly List<Token> _tokens = new List<Token>();
    private int _pos;
    private string _fullSql;
    private string _queryType;

    static QueryNormalizer()
    {
      var assembly = Assembly.GetExecutingAssembly();
      String resourceName = @"MySql.Data.keywords.txt";
      using (var stream = assembly.GetManifestResourceStream(resourceName))
      {
        if (stream == null)
          throw new Exception($"Resource {resourceName} not found in {assembly.FullName}.");
        using (var reader = new StreamReader(stream))
        {
          string keyword = reader.ReadLine();
          while (keyword != null)
          {
            Keywords.Add(keyword);
            keyword = reader.ReadLine();
          }
        }
      }
    }

    public string QueryType => _queryType;

    public string Normalize(string sql)
    {
      _tokens.Clear();
      StringBuilder newSql = new StringBuilder();
      _fullSql = sql;

      TokenizeSql(sql);
      DetermineStatementType(_tokens);
      ProcessMathSymbols(_tokens);
      CollapseValueLists(_tokens);
      CollapseInLists(_tokens);
      CollapseWhitespace(_tokens);

      foreach (Token t in _tokens.Where(t => t.Output))
        newSql.Append(t.Text);

      return newSql.ToString();
    }

    private void DetermineStatementType(List<Token> tok)
    {
      foreach (Token t in tok.Where(t => t.Type == TokenType.Keyword))
      {
        _queryType = t.Text.ToUpperInvariant();
        break;
      }
    }

    /// <summary>
    /// Mark - or + signs that are unary ops as no output
    /// </summary>
    /// <param name="tok"></param>
    private static void ProcessMathSymbols(List<Token> tok)
    {
      Token lastToken = null;

      foreach (Token t in tok)
      {
        if (t.Type == TokenType.Symbol &&
            (t.Text == "-" || t.Text == "+"))
        {
          if (lastToken != null &&
              lastToken.Type != TokenType.Number &&
              lastToken.Type != TokenType.Identifier &&
              (lastToken.Type != TokenType.Symbol || lastToken.Text != ")"))
            t.Output = false;
        }
        if (t.IsRealToken)
          lastToken = t;
      }
    }

    private static void CollapseWhitespace(List<Token> tok)
    {
      Token lastToken = null;

      foreach (Token t in tok)
      {
        if (t.Output &&
            t.Type == TokenType.Whitespace &&
            lastToken != null &&
            lastToken.Type == TokenType.Whitespace)
        {
          t.Output = false;
        }
        if (t.Output)
          lastToken = t;
      }
    }

    private void CollapseValueLists(List<Token> tok)
    {
      int pos = -1;
      while (++pos < tok.Count)
      {
        Token t = tok[pos];
        if (t.Type != TokenType.Keyword) continue;
        if (!t.Text.StartsWith("VALUE", StringComparison.OrdinalIgnoreCase)) continue;
        CollapseValueList(tok, ref pos);
      }
    }

    private void CollapseValueList(List<Token> tok, ref int pos)
    {
      List<int> parenIndices = new List<int>();

      // this while loop will find all closing parens in this value list
      while (true)
      {
        // find the close ')'
        while (++pos < tok.Count)
        {
          if (tok[pos].Type == TokenType.Symbol && tok[pos].Text == ")")
            break;
          if (pos == tok.Count - 1)
            break;
        }
        parenIndices.Add(pos);

        // now find the next "real" token
        while (++pos < tok.Count)
          if (tok[pos].IsRealToken) break;
        if (pos == tok.Count) break;

        if (tok[pos].Text == ",") continue;
        pos--;
        break;
      }

      // if we only have 1 value then we don't collapse
      if (parenIndices.Count < 2) return;
      int index = parenIndices[0];
      tok[++index] = new Token(TokenType.Whitespace, " ");
      tok[++index] = new Token(TokenType.Comment, "/* , ... */");
      index++;

      // now mark all the other tokens as no output
      while (index <= parenIndices[parenIndices.Count - 1])
        tok[index++].Output = false;
    }

    private void CollapseInLists(List<Token> tok)
    {
      int pos = -1;
      while (++pos < tok.Count)
      {
        Token t = tok[pos];
        if (t.Type != TokenType.Keyword) continue;
        if (t.Text != "IN") continue;
        CollapseInList(tok, ref pos);
      }
    }

    private static Token GetNextRealToken(List<Token> tok, ref int pos)
    {
      while (++pos < tok.Count)
      {
        if (tok[pos].IsRealToken) return tok[pos];
      }
      return null;
    }

    private static void CollapseInList(List<Token> tok, ref int pos)
    {
      Token t = GetNextRealToken(tok, ref pos);
      // Debug.Assert(t.Text == "(");
      if (t == null)
        return;

      // if the first token is a keyword then we likely have a 
      // SELECT .. IN (SELECT ...)
      t = GetNextRealToken(tok, ref pos);
      if (t == null || t.Type == TokenType.Keyword) return;

      int start = pos;
      // first find all the tokens that make up the in list
      while (++pos < tok.Count)
      {
        t = tok[pos];
        if (t.Type == TokenType.CommandComment) return;
        if (!t.IsRealToken) continue;
        if (t.Text == "(") return;
        if (t.Text == ")") break;
      }
      int stop = pos;

      for (int i = stop; i > start; i--)
        tok.RemoveAt(i);
      tok.Insert(++start, new Token(TokenType.Whitespace, " "));
      tok.Insert(++start, new Token(TokenType.Comment, "/* , ... */"));
      tok.Insert(++start, new Token(TokenType.Whitespace, " "));
      tok.Insert(++start, new Token(TokenType.Symbol, ")"));
    }

    private void TokenizeSql(string sql)
    {
      _pos = 0;

      while (_pos < sql.Length)
      {
        char c = sql[_pos];
        if (LetterStartsComment(c) && ConsumeComment())
          continue;
        if (Char.IsWhiteSpace(c))
          ConsumeWhitespace();
        else if (c == '\'' || c == '\"' || c == '`')
          ConsumeQuotedToken(c);
        else if (!IsSpecialCharacter(c))
          ConsumeUnquotedToken();
        else
          ConsumeSymbol();
      }
    }

    private bool LetterStartsComment(char c)
    {
      return c == '#' || c == '/' || c == '-';
    }

    private bool IsSpecialCharacter(char c)
    {
      return !Char.IsLetterOrDigit(c) && c != '$' && c != '_' && c != '.';
    }

    private bool ConsumeComment()
    {
      char c = _fullSql[_pos];
      // make sure the comment starts correctly
      if (c == '/' && ((_pos + 1) >= _fullSql.Length || _fullSql[_pos + 1] != '*')) return false;
      if (c == '-' && ((_pos + 2) >= _fullSql.Length || _fullSql[_pos + 1] != '-' || _fullSql[_pos + 2] != ' ')) return false;

      string endingPattern = "\n";
      if (c == '/')
        endingPattern = "*/";

      int startingIndex = _pos;

      int index = _fullSql.IndexOf(endingPattern, _pos);
      if (index == -1)
        index = _fullSql.Length - 1;
      else
        index += endingPattern.Length;
      string comment = _fullSql.Substring(_pos, index - _pos);
      if (comment.StartsWith("/*!", StringComparison.Ordinal))
        _tokens.Add(new Token(TokenType.CommandComment, comment));
      _pos = index;
      return true;
    }

    private void ConsumeSymbol()
    {
      char c = _fullSql[_pos++];
      _tokens.Add(new Token(TokenType.Symbol, c.ToString()));
    }

    private void ConsumeQuotedToken(char c)
    {
      bool escaped = false;
      int start = _pos;
      _pos++;
      while (_pos < _fullSql.Length)
      {
        char x = _fullSql[_pos];

        if (x == c && !escaped) break;

        if (escaped)
          escaped = false;
        else if (x == '\\')
          escaped = true;
        _pos++;
      }
      _pos++;
      _tokens.Add(c == '\''
        ? new Token(TokenType.String, "?")
        : new Token(TokenType.Identifier, _fullSql.Substring(start, _pos - start)));
    }

    private void ConsumeUnquotedToken()
    {
      int startPos = _pos;
      while (_pos < _fullSql.Length && !IsSpecialCharacter(_fullSql[_pos]))
        _pos++;
      string word = _fullSql.Substring(startPos, _pos - startPos);
      double v;
      if (double.TryParse(
        word,
        System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture,
        out v))
        _tokens.Add(new Token(TokenType.Number, "?"));
      else
      {
        Token t = new Token(TokenType.Identifier, word);
        if (IsKeyword(word))
        {
          t.Type = TokenType.Keyword;
          t.Text = t.Text.ToUpperInvariant();
        }
        _tokens.Add(t);
      }
    }

    private void ConsumeWhitespace()
    {
      _tokens.Add(new Token(TokenType.Whitespace, " "));
      while (_pos < _fullSql.Length && Char.IsWhiteSpace(_fullSql[_pos]))
        _pos++;
    }

    private static bool IsKeyword(string word)
    {
      return Keywords.Contains(word.ToUpperInvariant());
    }
  }

  internal class Token
  {
    public TokenType Type;
    public string Text;
    public bool Output;

    public Token(TokenType type, string text)
    {
      Type = type;
      Text = text;
      Output = true;
    }

    public bool IsRealToken => Type != TokenType.Comment &&
                               Type != TokenType.CommandComment &&
                               Type != TokenType.Whitespace &&
                               Output;
  }

  internal enum TokenType
  {
    Keyword,
    String,
    Number,
    Symbol,
    Identifier,
    Comment,
    CommandComment,
    Whitespace
  }
}