// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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

namespace MySqlX.Serialization
{
  /// <summary>
  /// Main class for parsing json strings
  /// </summary>
  public  class JsonParser
  {
    private int _pos = 0;
    private String _input;

    public static Dictionary<string, object> Parse(string s)
    {
      JsonParser p = new JsonParser();
      return p.ParseInternal(s);
    }

    private Dictionary<string, object> ParseInternal(string s)
    {
      _input = s;
      return ReadGroup();
    }

    private Dictionary<string, object> ReadGroup()
    {
      Dictionary<string, object> values = new Dictionary<string, object>();

      RequireToken('{');
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
      string stringValue = ReadUntilToken(',', '}');
      int intValue;
      long longValue;
      double doubleValue;
      if (int.TryParse(stringValue, out intValue)) return intValue;
      if (long.TryParse(stringValue, out longValue)) return longValue;
      if (double.TryParse(stringValue, out doubleValue)) return doubleValue;
      return stringValue;
    }

    private Dictionary<string,object>[] ReadArray()
    {
      List<Dictionary<string, object>> values = new List<Dictionary<string, object>>();

      RequireToken('[');
      while (true)
      {
        values.Add(ReadGroup());
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
      while (_pos < _input.Length)
      {
        char c = _input[_pos++];
        if (TokenInGroup(end, c))
        {
          _pos--;
          return val;
        }
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
