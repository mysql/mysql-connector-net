// Copyright (c) 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;

namespace MySqlX.Serialization
{
  /// <summary>
  /// Main class for parsing json strings.
  /// </summary>
  public  class JsonParser
  {
    private int _pos = 0;
    private String _input;

    /// <summary>
    /// Initializes a new instance of the JsonParser class.
    /// </summary>
    public JsonParser()
    {}

    /// <summary>
    /// Parses the received string into a dictionary.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>A <see cref="Dictionary{TKey, TValue}"/> object that represents the parsed string.</returns>
    public static Dictionary<string, object> Parse(string s)
    {
      JsonParser p = new JsonParser();
      return p.ParseInternal(s);
    }

    private Dictionary<string, object> ParseInternal(string s)
    {
      _input = s;
      Dictionary<string, object> dic = ReadGroup();
      if (_pos != _input.Length)
        throw new IndexOutOfRangeException("It's not the end of stream.");
      return dic;
    }

    private Dictionary<string, object> ReadGroup()
    {
      Dictionary<string, object> values = new Dictionary<string, object>();

      RequireToken('{');
      if (PeekToken() != '}')
        while (true)
        {
          string key = ReadQuotedToken();
          if (key == null) break;
          RequireToken(':');
          var obj = ReadValue();
          values[key] = obj;
          if (PeekToken() == '}') break;
          RequireToken(',');
        }
      RequireToken('}');
      return values;
    }

    private object ReadValue()
    {
      char t = PeekToken();
      if (t == '"') return ReadQuotedToken();
      if (t == '{') return ReadGroup();
      if (t == '[') return ReadArray();
      string stringValue = ReadUntilToken(',', '}', ']');
      bool flag;
      if (bool.TryParse(stringValue, out flag)) return flag;
      if (stringValue.Trim() == "null") return null;
      int intValue;
      long longValue;
      double doubleValue;
      if (int.TryParse(stringValue, out intValue)) return intValue;
      if (long.TryParse(stringValue, out longValue)) return longValue;
      if (double.TryParse(
        stringValue,
        System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture,
        out doubleValue))
        return doubleValue;

      // May be a function.
      int openingParen = 0;
      for (int i=0; i<stringValue.Length; i++)
      {
        if (stringValue[i] == '(') openingParen++;
        else if (stringValue[i] == ')') openingParen--;
      }

      while (openingParen>0)
      {
        stringValue += ReadUntilToken(')') + ")";
        RequireToken(')');
        openingParen--;
      }

      return new MySqlExpression(stringValue);
    }

    private object[] ReadArray()
    {
      List<object> values = new List<object>();

      RequireToken('[');
      while (true)
      {
        values.Add(ReadValue());
        if (PeekToken() == ']') break;
        RequireToken(',');
      }
      RequireToken(']');
      return values.ToArray();
    }

    private char PeekToken()
    {
      SkipWhite();
      if (_pos == _input.Length)
        throw new Exception("Unexpected end of stream found.");
      return _input[_pos];
    }

    private string ReadQuotedToken()
    {
      RequireToken('"');
      string val = ReadUntilToken('"');
      RequireToken('"');
      return val;
    }

    private bool TokenInGroup(char[] tokens, char c)
    {
      foreach (char token in tokens)
        if (token == c) return true;
      return false;
    }

    private string ReadUntilToken(params char[] end)
    {
      string val = "";
      bool escapedQuoteExpected = false;
      while (_pos < _input.Length)
      {
        char c = _input[_pos++];

        if (!escapedQuoteExpected)
        {
          if (c == 92)
            escapedQuoteExpected = true;
          else if (TokenInGroup(end, c))
          {
            _pos--;
            return val;
          }
        }
        else if (c == 34)
          escapedQuoteExpected = false;

        val += c;
      }
      throw new Exception("Failed to find ending '\"' while reading stream.");
    }

    private void RequireToken(char token)
    {
      if (!ReadToken(token))
        throw new Exception("Expected token '" + token + "'");
    }

    private bool ReadToken(char token)
    {
      SkipWhite();
      if (_pos == _input.Length) return false;
      char c = _input[_pos++];
      return c == token;
    }

    private void SkipWhite()
    {
      while (Char.IsWhiteSpace(_input[_pos]))
        _pos++;
    }
  }
}
