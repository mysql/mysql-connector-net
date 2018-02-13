// Copyright (c) 2004, 2016, Oracle and/or its affiliates.  All rights reserved.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MySql.Data.Common
{
  internal class MySqlTokenizer
  {
    private string _sql;

    public MySqlTokenizer()
    {
      BackslashEscapes = true;
      MultiLine = true;
      Position = 0;
    }

    public MySqlTokenizer(string input)
      : this()
    {
      _sql = input;
    }

    #region Properties

    public string Text
    {
      get { return _sql; }
      set { _sql = value; Position = 0; }
    }

    public bool AnsiQuotes { get; set; }

    public bool BackslashEscapes { get; set; }

    public bool MultiLine { get; set; }

    public bool SqlServerMode { get; set; }

    public bool Quoted { get; private set; }

    public bool IsComment { get; private set; }

    public int StartIndex { get; set; }

    public int StopIndex { get; set; }

    public int Position { get; set; }

    public bool ReturnComments { get; set; }

    #endregion

    public List<string> GetAllTokens()
    {
      List<string> tokens = new List<string>();
      string token = NextToken();
      while (token != null)
      {
        tokens.Add(token);
        token = NextToken();
      }
      return tokens;
    }

    public string NextToken()
    {
      while (FindToken())
      {
        string token = _sql.Substring(StartIndex, StopIndex - StartIndex);
        return token;
      }
      return null;
    }

    public static bool IsParameter(string s)
    {
      if (String.IsNullOrEmpty(s)) return false;
      if (s[0] == '?') return true;
      return s.Length > 1 && s[0] == '@' && s[1] != '@';
    }

    public string NextParameter()
    {
      while (FindToken())
      {
        if ((StopIndex - StartIndex) < 2) continue;
        char c1 = _sql[StartIndex];
        char c2 = _sql[StartIndex + 1];
        if (c1 == '?' ||
            (c1 == '@' && c2 != '@'))
          return _sql.Substring(StartIndex, StopIndex - StartIndex);
      }
      return null;
    }

    public bool FindToken()
    {
      IsComment = Quoted = false;  // reset our flags
      StartIndex = StopIndex = -1;

      while (Position < _sql.Length)
      {
        char c = _sql[Position++];
        if (Char.IsWhiteSpace(c)) continue;

        if (c == '`' || c == '\'' || c == '"' || (c == '[' && SqlServerMode))
          ReadQuotedToken(c);
        else if (c == '#' || c == '-' || c == '/')
        {
          if (!ReadComment(c))
            ReadSpecialToken();
        }
        else
          ReadUnquotedToken();
        if (StartIndex != -1) return true;
      }
      return false;
    }

    public string ReadParenthesis()
    {
      StringBuilder sb = new StringBuilder("(");
      int start = StartIndex;
      string token = NextToken();
      while (true)
      {
        if (token == null)
          throw new InvalidOperationException("Unable to parse SQL");
        sb.Append(token);
        if (token == ")" && !Quoted) break;
        token = NextToken();
      }
      return sb.ToString();
    }

    private bool ReadComment(char c)
    {
      // make sure the comment starts correctly
      if (c == '/' && (Position >= _sql.Length || _sql[Position] != '*')) return false;
      if (c == '-' && ((Position + 1) >= _sql.Length || _sql[Position] != '-' || _sql[Position + 1] != ' ')) return false;

      string endingPattern = "\n";
      if (_sql[Position] == '*')
        endingPattern = "*/";

      int startingIndex = Position - 1;

      int index = _sql.IndexOf(endingPattern, Position);
      if (endingPattern == "\n")
        index = _sql.IndexOf('\n', Position);
      if (index == -1)
        index = _sql.Length - 1;
      else
        index += endingPattern.Length;

      Position = index;
      if (ReturnComments)
      {
        StartIndex = startingIndex;
        StopIndex = index;
        IsComment = true;
      }
      return true;
    }

    private void CalculatePosition(int start, int stop)
    {
      StartIndex = start;
      StopIndex = stop;
      if (!MultiLine) return;
    }

    private void ReadUnquotedToken()
    {
      StartIndex = Position - 1;

      if (!IsSpecialCharacter(_sql[StartIndex]))
      {
        while (Position < _sql.Length)
        {
          char c = _sql[Position];
          if (Char.IsWhiteSpace(c)) break;
          if (IsSpecialCharacter(c)) break;
          Position++;
        }
      }

      Quoted = false;
      StopIndex = Position;
    }

    private void ReadSpecialToken()
    {
      StartIndex = Position - 1;

      Debug.Assert(IsSpecialCharacter(_sql[StartIndex]));

      StopIndex = Position;
      Quoted = false;
    }

    /// <summary>
    ///  Read a single quoted identifier from the stream
    /// </summary>
    /// <param name="quoteChar"></param>
    /// <returns></returns>
    private void ReadQuotedToken(char quoteChar)
    {
      if (quoteChar == '[')
        quoteChar = ']';
      StartIndex = Position - 1;
      bool escaped = false;

      bool found = false;
      while (Position < _sql.Length)
      {
        char c = _sql[Position];

        if (c == quoteChar && !escaped)
        {
          found = true;
          break;
        }

        if (escaped)
          escaped = false;
        else if (c == '\\' && BackslashEscapes)
          escaped = true;
        Position++;
      }
      if (found) Position++;
      Quoted = found;
      StopIndex = Position;
    }

    private bool IsQuoteChar(char c)
    {
      return c == '`' || c == '\'' || c == '\"';
    }

    private bool IsParameterMarker(char c)
    {
      return c == '@' || c == '?';
    }

    private bool IsSpecialCharacter(char c)
    {
      if (Char.IsLetterOrDigit(c) ||
          c == '$' || c == '_' || c == '.') return false;
      if (IsParameterMarker(c)) return false;
      return true;
    }
  }
}
