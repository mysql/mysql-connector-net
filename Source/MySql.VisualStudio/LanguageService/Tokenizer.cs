// Copyright © 2008, 2011, Oracle and/or its affiliates. All rights reserved.
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

namespace MySql.Data.VisualStudio
{
  class Tokenizer
  {
    private string text;
    private int Pos;
    private string LastToken;
    private int _startIndex;
    private int _stopIndex;
    private bool _lineComment;
    private bool _quoted;
    private bool enableTaggingSupport;

    public bool AnsiQuotes = false;
    public bool BackslashEscapes = true;
    public bool ReturnComments;
    public bool BlockComment;

    internal Tokenizer() { }

    internal Tokenizer(bool enableTaggingSupport)
    {
      this.enableTaggingSupport = enableTaggingSupport;
    }

    #region Properties

    public string Text
    {
      get { return text; }
      set { text = value; Pos = 0; }
    }

    public int StartIndex
    {
      get { return _startIndex; }
      private set { _startIndex = value; }
    }

    public int StopIndex
    {
      get { return _stopIndex; }
      private set { _stopIndex = value; }
    }

    public bool LineComment
    {
      get { return _lineComment; }
      private set { _lineComment = value; }
    }

    public bool Quoted
    {
      get { return _quoted; }
      private set { _quoted = value; }
    }

    #endregion

    public string NextToken()
    {
      if (LastToken == "*/" && BlockComment)
        BlockComment = false;

      LastToken = GetNextToken();
      return LastToken;
    }

    private string GetNextToken()
    {
      if (Text == null) return null;

      StartIndex = StopIndex = 0;
      Quoted = false;
      LineComment = false;

      while (Pos < Text.Length)
      {
        if (BlockComment)
          return ExtractComment();

        char c = Text[Pos++];
        if (Char.IsWhiteSpace(c) && StartIndex == 0) continue;

        StartIndex = Pos - 1;

        if (IsQuoteCharacter(c) && !BlockComment)
          return ExtractQuotedToken(c);

        string comment = null;
        if (Pos != Text.Length)
        {
          comment = ReadComment(c);
        }
        if (comment != null && ReturnComments) return comment;

        return ExtractUnquotedToken();
      }
      return null;
    }

    #region Private methods

    private string ReadComment(char startingChar)
    {
      if (startingChar != '/' && startingChar != '#' && startingChar != '-') return null;
      if (startingChar == '-' && Text.Length == (Pos + 1)) return null;
      if (startingChar == '/' && Text.Length == Pos) return null;

      if ((startingChar == '-' && Text[Pos] == '-' && Text[Pos + 1] == ' ') ||
          startingChar == '#')
      {
        LineComment = true;
        return ExtractComment();
      }

      if (startingChar == '/' && Text[Pos] == '*')
      {
        BlockComment = true;
        return ExtractComment();
      }

      return null;
    }

    private string ExtractComment()
    {
      char lastChar = Char.MinValue;

      while (Pos < Text.Length)
      {
        char c = Text[Pos++];
        if ((c == '\n' && LineComment) ||
            (c == '/' && lastChar == '*' && BlockComment))
          return ExtractToken(true);
        lastChar = c;
      }
      return ExtractToken(false);
    }

    private string ExtractUnquotedToken()
    {
      while (Pos < Text.Length)
      {
        char c = Text[Pos++];
        if (IsTokenTerminator(c))
          return ExtractToken(true);
      }
      return ExtractToken(false);
    }

    private string ExtractQuotedToken(char quoteChar)
    {
      bool escaped = false;
      while (Pos < Text.Length)
      {
        char c = Text[Pos++];
        if (c == quoteChar && !escaped)
        {
          Quoted = true;
          return ExtractToken(true);
        }
        if (escaped) escaped = false;
        if (c == '\\' && BackslashEscapes)
          escaped = true;
      }
      return ExtractToken(false);
    }

    private bool IsQuoteCharacter(char c)
    {
      return c == '\'' || c == '`' || (c == '\"' && AnsiQuotes);
    }

    private bool IsTokenTerminator(char c)
    {
      if (c == '\n') return true;
      if (c == '#' || c == '-' || c == '/' || c == '\\') return true;
      if (c == '(' || c == ',') return true;
      if (Char.IsWhiteSpace(c)) return true;
      return false;
    }

    private string ExtractToken(bool preserveLast)
    {
      StopIndex = Math.Min(Text.Length - 1, Pos - 1);

      if (preserveLast && !enableTaggingSupport)
      {
        Pos--;
      }

      return Text.Substring(StartIndex, StopIndex - StartIndex + 1);
    }

    #endregion
  }
}
