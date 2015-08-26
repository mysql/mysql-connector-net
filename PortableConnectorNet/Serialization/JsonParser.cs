using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Serialization
{
  public  class JsonParser
  {
    private int _pos = 0;
    private String _input;

    public Dictionary<string, object> Parse(string s)
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
      throw new Exception("Unsupported JSON syntax");
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

    private string ReadUntilToken(char end)
    {
      string val = "";
      while (_pos < _input.Length)
      {
        char c = _input[_pos++];
        if (c == end)
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
