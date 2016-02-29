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

using Mysqlx.Crud;
using Mysqlx.Expr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySqlX.Protocol.X
{
  // Grammar includes precedence & associativity of binary operators:
  // (^ refers to the preceding production)
  // (c.f. https://dev.mysql.com/doc/refman/5.7/en/operator-precedence.html)
  //
  // AtomicExpr: [Unary]OpExpr | Identifier | FunctionCall | '(' Expr ')'
  //
  // AddSubIntervalExpr: ^ (ADD/SUB ^)* | (ADD/SUB 'INTERVAL' ^ UNIT)*
  //
  // MulDivExpr: ^ (STAR/SLASH/MOD ^)*
  //
  // ShiftExpr: ^ (LSHIFT/RSHIFT ^)*
  //
  // BitExpr: ^ (BITAND/BITOR/BITXOR ^)*
  //
  // CompExpr: ^ (GE/GT/LE/LT/EQ/NE ^)*
  //
  // IlriExpr(ilri=IS/LIKE/REGEXP/IN/BETWEEN): ^ (ilri ^)
  //
  // AndExpr: ^ (AND ^)*
  //
  // OrExpr: ^ (OR ^)*
  //
  // Expr: ^
  //
  /**
   * Expression parser for MySQL-X protocol.
   */
  internal class ExprParser
  {
    /** string being parsed. */
    string stringValue;
    /** Token stream produced by lexer. */
    internal List<Token> tokens = new List<Token>();
    /** Parser's position in token stream. */
    int tokenPos = 0;
    /**
     * Mapping of names to positions for named placeholders. Used for both string values ":arg" and numeric values ":2".
     */
    internal Dictionary<string, int> placeholderNameToPosition = new Dictionary<string, int>();
    /** Number of positional placeholders. */
    internal int positionalPlaceholderCount = 0;

    /** Are relational columns identifiers allowed? */
    private bool allowRelationalColumns;

    public ExprParser(string s)
      : this(s, true)
    {

    }

    public ExprParser(string s, bool allowRelationalColumns)
    {
      this.stringValue = s;
      Lex();
      // java.util.stream.IntStream.range(0, this.tokens.size()).forEach(i -> System.err.println("[" + i + "] = " + this.tokens.get(i)));
      this.allowRelationalColumns = allowRelationalColumns;
    }

    /**
     * Token types used by the lexer.
     */
    public enum TokenType
    {
      NOT, AND, ANDAND, OR, OROR, XOR, IS, LPAREN, RPAREN, LSQBRACKET, RSQBRACKET, BETWEEN, TRUE, NULL, FALSE, IN, LIKE, INTERVAL, REGEXP, ESCAPE, IDENT,
      LSTRING, LNUM_INT, LNUM_DOUBLE, DOT, AT, COMMA, EQ, NE, GT, GE, LT, LE, BITAND, BITOR, BITXOR, LSHIFT, RSHIFT, PLUS, MINUS, STAR, SLASH, HEX,
      BIN, NEG, BANG, EROTEME, MICROSECOND, SECOND, MINUTE, HOUR, DAY, WEEK, MONTH, QUARTER, YEAR, SECOND_MICROSECOND, MINUTE_MICROSECOND,
      MINUTE_SECOND, HOUR_MICROSECOND, HOUR_SECOND, HOUR_MINUTE, DAY_MICROSECOND, DAY_SECOND, DAY_MINUTE, DAY_HOUR, YEAR_MONTH, DOUBLESTAR, MOD,
      COLON, ORDERBY_ASC, ORDERBY_DESC, AS, LCURLY, RCURLY, DOTSTAR, CAST, DECIMAL, UNSIGNED, SIGNED, INTEGER, DATE, TIME, DATETIME, CHAR, BINARY, JSON
    }

    /**
     * Token. Includes type and string value of the token.
     */
    internal class Token
    {
      internal TokenType type;
      internal string value;

      public Token(TokenType x, char c)
      {
        this.type = x;
        this.value = c.ToString();
      }

      public Token(TokenType t, string v)
      {
        this.type = t;
        this.value = v;
      }

      public override string ToString()
      {
        if (this.type == TokenType.IDENT || this.type == TokenType.LNUM_INT || this.type == TokenType.LNUM_DOUBLE || this.type == TokenType.LSTRING)
        {
          return this.type.ToString() + "(" + this.value + ")";
        }
        else
        {
          return this.type.ToString();
        }
      }
    }

    /** Mapping of reserved words to token types. */
    static private Dictionary<string, TokenType> reservedWords = new Dictionary<string, TokenType>();
    static ExprParser ()
    {
      reservedWords.Add("and", TokenType.AND);
      reservedWords.Add("or", TokenType.OR);
      reservedWords.Add("xor", TokenType.XOR);
      reservedWords.Add("is", TokenType.IS);
      reservedWords.Add("not", TokenType.NOT);
      reservedWords.Add("like", TokenType.LIKE);
      reservedWords.Add("in", TokenType.IN);
      reservedWords.Add("regexp", TokenType.REGEXP);
      reservedWords.Add("between", TokenType.BETWEEN);
      reservedWords.Add("interval", TokenType.INTERVAL);
      reservedWords.Add("escape", TokenType.ESCAPE);
      reservedWords.Add("div", TokenType.SLASH);
      reservedWords.Add("hex", TokenType.HEX);
      reservedWords.Add("bin", TokenType.BIN);
      reservedWords.Add("true", TokenType.TRUE);
      reservedWords.Add("false", TokenType.FALSE);
      reservedWords.Add("null", TokenType.NULL);
      reservedWords.Add("microsecond", TokenType.MICROSECOND);
      reservedWords.Add("second", TokenType.SECOND);
      reservedWords.Add("minute", TokenType.MINUTE);
      reservedWords.Add("hour", TokenType.HOUR);
      reservedWords.Add("day", TokenType.DAY);
      reservedWords.Add("week", TokenType.WEEK);
      reservedWords.Add("month", TokenType.MONTH);
      reservedWords.Add("quarter", TokenType.QUARTER);
      reservedWords.Add("year", TokenType.YEAR);
      reservedWords.Add("second_microsecond", TokenType.SECOND_MICROSECOND);
      reservedWords.Add("minute_microsecond", TokenType.MINUTE_MICROSECOND);
      reservedWords.Add("minute_second", TokenType.MINUTE_SECOND);
      reservedWords.Add("hour_microsecond", TokenType.HOUR_MICROSECOND);
      reservedWords.Add("hour_second", TokenType.HOUR_SECOND);
      reservedWords.Add("hour_minute", TokenType.HOUR_MINUTE);
      reservedWords.Add("day_microsecond", TokenType.DAY_MICROSECOND);
      reservedWords.Add("day_second", TokenType.DAY_SECOND);
      reservedWords.Add("day_minute", TokenType.DAY_MINUTE);
      reservedWords.Add("day_hour", TokenType.DAY_HOUR);
      reservedWords.Add("year_month", TokenType.YEAR_MONTH);
      reservedWords.Add("asc", TokenType.ORDERBY_ASC);
      reservedWords.Add("desc", TokenType.ORDERBY_DESC);
      reservedWords.Add("as", TokenType.AS);
      reservedWords.Add("cast", TokenType.CAST);
      reservedWords.Add("decimal", TokenType.DECIMAL);
      reservedWords.Add("unsigned", TokenType.UNSIGNED);
      reservedWords.Add("signed", TokenType.SIGNED);
      reservedWords.Add("integer", TokenType.INTEGER);
      reservedWords.Add("date", TokenType.DATE);
      reservedWords.Add("time", TokenType.TIME);
      reservedWords.Add("datetime", TokenType.DATETIME);
      reservedWords.Add("char", TokenType.CHAR);
      reservedWords.Add("binary", TokenType.BINARY);
      reservedWords.Add("json", TokenType.BINARY);
    }

    /**
     * Does the next character equal the given character? (respects bounds)
     */
    bool NextCharEquals(int i, char c)
    {
      return (i + 1 < this.stringValue.Length) && this.stringValue[i + 1] == c;
    }

    /**
     * Helper function to match integer or floating point numbers. This function should be called when the position is on the first character of the number (a
     * digit or '.').
     *
     * @param i The current position in the string
     * @return the next position in the string after the number.
     */
    private int LexNumber(int i)
    {
      bool isInt = true;
      char c;
      int start = i;
      for (; i < this.stringValue.Length; ++i)
      {
        c = this.stringValue[i];
        if (c == '.')
        {
          isInt = false;
        }
        else if (c == 'e' || c == 'E')
        {
          isInt = false;
          if (NextCharEquals(i, '-') || NextCharEquals(i, '+'))
          {
            i++;
          }
        }
        else if (!Char.IsDigit(c))
        {
          break;
        }
      }
      if (isInt)
      {
        this.tokens.Add(new Token(TokenType.LNUM_INT, this.stringValue.Substring(start, i - start)));
      }
      else
      {
        this.tokens.Add(new Token(TokenType.LNUM_DOUBLE, this.stringValue.Substring(start, i - start)));
      }
      --i;
      return i;
    }

    /**
     * Lexer for MySQL-X expression language.
     */
    void Lex()
    {
      for (int i = 0; i < this.stringValue.Length; ++i)
      {
        int start = i; // for routines that consume more than one char
        char c = this.stringValue[i];
        if (Char.IsWhiteSpace(c))
        {
          // ignore
        }
        else if (Char.IsDigit(c))
        {
          i = LexNumber(i);
        }
        else if (!(c == '_' || Char.IsLetter(c)))
        {
          // non-identifier, e.g. operator or quoted literal
          switch (c)
          {
            case ':':
              this.tokens.Add(new Token(TokenType.COLON, c));
              break;
            case '+':
              this.tokens.Add(new Token(TokenType.PLUS, c));
              break;
            case '-':
              this.tokens.Add(new Token(TokenType.MINUS, c));
              break;
            case '*':
              if (NextCharEquals(i, '*'))
              {
                i++;
                this.tokens.Add(new Token(TokenType.DOUBLESTAR, "**"));
              }
              else
              {
                this.tokens.Add(new Token(TokenType.STAR, c));
              }
              break;
            case '/':
              this.tokens.Add(new Token(TokenType.SLASH, c));
              break;
            case '@':
              this.tokens.Add(new Token(TokenType.AT, c));
              break;
            case '%':
              this.tokens.Add(new Token(TokenType.MOD, c));
              break;
            case '=':
              if (NextCharEquals(i, '='))
              {
                i++;
              }
              this.tokens.Add(new Token(TokenType.EQ, "=="));
              break;
            case '&':
              if (NextCharEquals(i, '&'))
              {
                i++;
                this.tokens.Add(new Token(TokenType.ANDAND, "&&"));
              }
              else
              {
                this.tokens.Add(new Token(TokenType.BITAND, c));
              }
              break;
            case '|':
              if (NextCharEquals(i, '|'))
              {
                i++;
                this.tokens.Add(new Token(TokenType.OROR, "||"));
              }
              else
              {
                this.tokens.Add(new Token(TokenType.BITOR, c));
              }
              break;
            case '^':
              this.tokens.Add(new Token(TokenType.BITXOR, c));
              break;
            case '(':
              this.tokens.Add(new Token(TokenType.LPAREN, c));
              break;
            case ')':
              this.tokens.Add(new Token(TokenType.RPAREN, c));
              break;
            case '[':
              this.tokens.Add(new Token(TokenType.LSQBRACKET, c));
              break;
            case ']':
              this.tokens.Add(new Token(TokenType.RSQBRACKET, c));
              break;
            case '{':
              this.tokens.Add(new Token(TokenType.LCURLY, c));
              break;
            case '}':
              this.tokens.Add(new Token(TokenType.RCURLY, c));
              break;
            case '~':
              this.tokens.Add(new Token(TokenType.NEG, c));
              break;
            case ',':
              this.tokens.Add(new Token(TokenType.COMMA, c));
              break;
            case '!':
              if (NextCharEquals(i, '='))
              {
                i++;
                this.tokens.Add(new Token(TokenType.NE, "!="));
              }
              else
              {
                this.tokens.Add(new Token(TokenType.BANG, c));
              }
              break;
            case '?':
              this.tokens.Add(new Token(TokenType.EROTEME, c));
              break;
            case '<':
              if (NextCharEquals(i, '<'))
              {
                i++;
                this.tokens.Add(new Token(TokenType.LSHIFT, "<<"));
              }
              else if (NextCharEquals(i, '='))
              {
                i++;
                this.tokens.Add(new Token(TokenType.LE, "<="));
              }
              else
              {
                this.tokens.Add(new Token(TokenType.LT, c));
              }
              break;
            case '>':
              if (NextCharEquals(i, '>'))
              {
                i++;
                this.tokens.Add(new Token(TokenType.RSHIFT, ">>"));
              }
              else if (NextCharEquals(i, '='))
              {
                i++;
                this.tokens.Add(new Token(TokenType.GE, ">="));
              }
              else
              {
                this.tokens.Add(new Token(TokenType.GT, c));
              }
              break;
            case '.':
              if (NextCharEquals(i, '*'))
              {
                i++;
                this.tokens.Add(new Token(TokenType.DOTSTAR, ".*"));
              }
              else if (i + 1 < this.stringValue.Length && Char.IsDigit(this.stringValue[i + 1]))
              {
                i = LexNumber(i);
              }
              else
              {
                this.tokens.Add(new Token(TokenType.DOT, c));
              }
              break;
            case '"':
            case '\'':
            case '`':
              char quoteChar = c;
              StringBuilder val = new StringBuilder();
              try
              {
                for (c = this.stringValue[++i]; c != quoteChar || (i + 1 < this.stringValue.Length && this.stringValue[i + 1] == quoteChar);
                     c = this.stringValue[++i])
                {
                  if (c == '\\' || c == quoteChar)
                  {
                    ++i;
                  }
                  val.Append(this.stringValue[i]);
                }
              }
              catch (IndexOutOfRangeException ex)
              {
                throw new ArgumentException("Unterminated string starting at " + start);
              }
              this.tokens.Add(new Token(quoteChar == '`' ? TokenType.IDENT : TokenType.LSTRING, val.ToString()));
              break;
            default:
              throw new ArgumentException("Can't parse at pos: " + i);
          }
        }
        else
        {
          // otherwise, it's an identifier
          for (; i < this.stringValue.Length && (Char.IsLetterOrDigit(this.stringValue[i]) || this.stringValue[i] == '_'); ++i)
          {
          }
          string val = this.stringValue.Substring(start, i - start);
          string valLower = val.ToLowerInvariant();
          if (i < this.stringValue.Length)
          {
            // last char, this logic is artifact of the preceding loop
            --i;
          }
          if (reservedWords.ContainsKey(valLower))
          {
            // Map operator names to values the server understands
            if ("and".Equals(valLower))
            {
              this.tokens.Add(new Token(reservedWords[valLower], "&&"));
            }
            else if ("or".Equals(valLower))
            {
              this.tokens.Add(new Token(reservedWords[valLower], "||"));
            }
            else
            {
              // we case-normalize reserved words
              this.tokens.Add(new Token(reservedWords[valLower], valLower));
            }
          }
          else
          {
            this.tokens.Add(new Token(TokenType.IDENT, val));
          }
        }
      }
    }

    /**
     * Assert that the token at <i>pos</i> is of type <i>type</i>.
     */
    void AssertTokenAt(int pos, TokenType type)
    {
      if (this.tokens.Count <= pos)
      {
        throw new ArgumentException("No more tokens when expecting " + type + " at token pos " + pos);
      }
      if (this.tokens[pos].type != type)
      {
        throw new ArgumentException("Expected token type " + type + " at token pos " + pos);
      }
    }

    /**
     * Does the current token have type `t'?
     */
    bool CurrentTokenTypeEquals(TokenType t)
    {
      return PosTokenTypeEquals(this.tokenPos, t);
    }

    /**
     * Does the next token have type `t'?
     */
    bool NextTokenTypeEquals(TokenType t)
    {
      return PosTokenTypeEquals(this.tokenPos + 1, t);
    }

    /**
     * Does the token at position `pos' have type `t'?
     */
    bool PosTokenTypeEquals(int pos, TokenType t)
    {
      return this.tokens.Count > pos && this.tokens[pos].type == t;
    }

    /**
     * Consume token.
     *
     * @return the string value of the consumed token
     */
    string ConsumeToken(TokenType t)
    {
      AssertTokenAt(this.tokenPos, t);
      string value = this.tokens[this.tokenPos].value;
      this.tokenPos++;
      return value;
    }

    /**
     * Parse a paren-enclosed expression list. This is used for function params or IN params.
     *
     * @return a List of expressions
     */
    List<Expr> ParenExprList()
    {
      List<Expr> exprs = new List<Expr>();
      ConsumeToken(TokenType.LPAREN);
      if (!CurrentTokenTypeEquals(TokenType.RPAREN))
      {
        exprs.Add(GetExpr());
        while (CurrentTokenTypeEquals(TokenType.COMMA))
        {
          ConsumeToken(TokenType.COMMA);
          exprs.Add(GetExpr());
        }
      }
      ConsumeToken(TokenType.RPAREN);
      return exprs;
    }

    /**
     * Parse a function call of the form: IDENTIFIER PAREN_EXPR_LIST.
     *
     * @return an Expr representing the function call.
     */
    Expr ParseFunctionCall()
    {
      Identifier id = ParseIdentifier();
      FunctionCall.Builder b = FunctionCall.CreateBuilder();
      b.SetName(id);
      b.AddRangeParam(ParenExprList());
      return Expr.CreateBuilder().SetType(Expr.Types.Type.FUNC_CALL).SetFunctionCall(b.Build()).Build();
    }

    Expr StarOperator()
    {
      Operator op = Operator.CreateBuilder().SetName("*").Build();
      return Expr.CreateBuilder().SetType(Expr.Types.Type.OPERATOR).SetOperator(op).Build();
    }
    /**
     * Parse an identifier for a function call: [schema.]name
     */
    Identifier ParseIdentifier()
    {
      Identifier.Builder builder = Identifier.CreateBuilder();
      AssertTokenAt(this.tokenPos, TokenType.IDENT);
      if (NextTokenTypeEquals(TokenType.DOT))
      {
        builder.SetSchemaName(this.tokens[this.tokenPos].value);
        ConsumeToken(TokenType.IDENT);
        ConsumeToken(TokenType.DOT);
        AssertTokenAt(this.tokenPos, TokenType.IDENT);
      }
      builder.SetName(this.tokens[this.tokenPos].value);
      ConsumeToken(TokenType.IDENT);
      return builder.Build();
    }

    /**
     * Parse a document path member.
     */
    DocumentPathItem DocPathMember()
    {
      ConsumeToken(TokenType.DOT);
      Token t = this.tokens[this.tokenPos];
      string memberName;
      if (CurrentTokenTypeEquals(TokenType.IDENT))
      {
        // this shouldn't be allowed to be quoted with backticks, but the lexer allows it
        if (!t.value.Equals(ExprUnparser.QuoteIdentifier(t.value)))
        {
          throw new ArgumentException("'" + t.value + "' is not a valid JSON/ECMAScript identifier");
        }
        ConsumeToken(TokenType.IDENT);
        memberName = t.value;
      }
      else if (CurrentTokenTypeEquals(TokenType.LSTRING))
      {
        ConsumeToken(TokenType.LSTRING);
        memberName = t.value;
      }
      else
      {
        throw new ArgumentException("Expected token type IDENT or LSTRING in JSON path at token pos " + this.tokenPos);
      }
      DocumentPathItem.Builder item = DocumentPathItem.CreateBuilder();
      item.SetType(DocumentPathItem.Types.Type.MEMBER);
      item.SetValue(memberName);
      return item.Build();
    }

    /**
     * Parse a document path array index.
     */
    DocumentPathItem DocPathArrayLoc()
    {
      DocumentPathItem.Builder builder = DocumentPathItem.CreateBuilder();
      ConsumeToken(TokenType.LSQBRACKET);
      if (CurrentTokenTypeEquals(TokenType.STAR))
      {
        ConsumeToken(TokenType.STAR);
        ConsumeToken(TokenType.RSQBRACKET);
        return builder.SetType(DocumentPathItem.Types.Type.ARRAY_INDEX_ASTERISK).Build();
      }
      else if (CurrentTokenTypeEquals(TokenType.LNUM_INT))
      {
        uint v = uint.Parse(this.tokens[this.tokenPos].value);
        if (v < 0)
        {
          throw new ArgumentException("Array index cannot be negative at " + this.tokenPos);
        }
        ConsumeToken(TokenType.LNUM_INT);
        ConsumeToken(TokenType.RSQBRACKET);
        return builder.SetType(DocumentPathItem.Types.Type.ARRAY_INDEX).SetIndex(v).Build();
      }
      else
      {
        throw new ArgumentException("Expected token type STAR or LNUM_INT in JSON path array index at token pos " + this.tokenPos);
      }
    }

    /**
     * Parse a JSON-style document path, like WL#7909, but prefix by @. instead of $.
     */
    internal List<DocumentPathItem> DocumentPath()
    {
      List<DocumentPathItem> items = new List<DocumentPathItem>();
      while (true)
      {
        if (CurrentTokenTypeEquals(TokenType.DOT))
        {
          items.Add(DocPathMember());
        }
        else if (CurrentTokenTypeEquals(TokenType.DOTSTAR))
        {
          ConsumeToken(TokenType.DOTSTAR);
          items.Add(DocumentPathItem.CreateBuilder().SetType(DocumentPathItem.Types.Type.MEMBER_ASTERISK).Build());
        }
        else if (CurrentTokenTypeEquals(TokenType.LSQBRACKET))
        {
          items.Add(DocPathArrayLoc());
        }
        else if (CurrentTokenTypeEquals(TokenType.DOUBLESTAR))
        {
          ConsumeToken(TokenType.DOUBLESTAR);
          items.Add(DocumentPathItem.CreateBuilder().SetType(DocumentPathItem.Types.Type.DOUBLE_ASTERISK).Build());
        }
        else
        {
          break;
        }
      }
      if (items.Count > 0 && items[items.Count - 1].Type == DocumentPathItem.Types.Type.DOUBLE_ASTERISK)
      {
        throw new ArgumentException("JSON path may not end in '**' at " + this.tokenPos);
      }
      return items;
    }

    /**
     * Parse a document field.
     */
    internal Expr DocumentField()
    {
      ColumnIdentifier.Builder builder = ColumnIdentifier.CreateBuilder();
      if (CurrentTokenTypeEquals(TokenType.IDENT))
      {
        builder.AddDocumentPath(DocumentPathItem.CreateBuilder().SetType(
                        DocumentPathItem.Types.Type.MEMBER).SetValue(ConsumeToken(TokenType.IDENT)).Build());
      }
      builder.AddRangeDocumentPath(DocumentPath());
      return Expr.CreateBuilder().SetType(Expr.Types.Type.IDENT).SetIdentifier(builder.Build()).Build();
    }

    /**
     * Parse a column identifier (which may optionally include a JSON document path).
     */
    Expr ParseColumnIdentifier()
    {
      List<string> parts = new List<string>();
      parts.Add(ConsumeToken(TokenType.IDENT));
      while (CurrentTokenTypeEquals(TokenType.DOT))
      {
        ConsumeToken(TokenType.DOT);
        parts.Add(ConsumeToken(TokenType.IDENT));
        // identifier can be at most three parts
        if (parts.Count == 3)
        {
          break;
        }
      }
      parts.Reverse();
      ColumnIdentifier.Builder id = ColumnIdentifier.CreateBuilder();
      for (int i = 0; i < parts.Count; ++i)
      {
        switch (i)
        {
          case 0:
            id.SetName(parts[0]);
            break;
          case 1:
            id.SetTableName(parts[1]);
            break;
          case 2:
            id.SetSchemaName(parts[2]);
            break;
        }
      }
      if (CurrentTokenTypeEquals(TokenType.AT))
      {
        ConsumeToken(TokenType.AT);
        id.AddRangeDocumentPath(DocumentPath());
        if (id.DocumentPathCount == 0)
        {
          throw new ArgumentException("Invalid document path at " + this.tokenPos);
        }
      }
      return Expr.CreateBuilder().SetType(Expr.Types.Type.IDENT).SetIdentifier(id.Build()).Build();
    }

    /**
     * Build a unary operator expression.
     */
    Expr BuildUnaryOp(string name, Expr param)
    {
      Operator op = Operator.CreateBuilder().SetName(name).AddParam(param).Build();
      return Expr.CreateBuilder().SetType(Expr.Types.Type.OPERATOR).SetOperator(op).Build();
    }

    /**
     * Parse an atomic expression. (c.f. grammar at top)
     */
    Expr AtomicExpr()
    { // constant, identifier, variable, function call, etc
      if (this.tokenPos >= this.tokens.Count)
      {
        throw new ArgumentException("No more tokens when expecting one at token pos " + this.tokenPos);
      }
      Token t = this.tokens[this.tokenPos];
      this.tokenPos++; // consume
      switch (t.type)
      {
        case TokenType.EROTEME:
        case TokenType.COLON:
          {
            string placeholderName;
            if (CurrentTokenTypeEquals(TokenType.LNUM_INT))
            {
              // int pos = Integer.valueOf(consumeToken(TokenType.LNUM_INT));
              // return Expr.CreateBuilder().setType(Expr.Type.PLACEHOLDER).setPosition(pos).Build();
              placeholderName = ConsumeToken(TokenType.LNUM_INT);
            }
            else if (CurrentTokenTypeEquals(TokenType.IDENT))
            {
              placeholderName = ConsumeToken(TokenType.IDENT);
            }
            else if (t.type == TokenType.EROTEME)
            {
              placeholderName = this.positionalPlaceholderCount.ToString();
            }
            else
            {
              throw new ArgumentException("Invalid placeholder name at token pos " + this.tokenPos);
            }
            placeholderName = placeholderName.ToLowerInvariant();
            Expr.Builder placeholder = Expr.CreateBuilder().SetType(Expr.Types.Type.PLACEHOLDER);
            if (this.placeholderNameToPosition.ContainsKey(placeholderName))
            {
              placeholder.SetPosition((uint)this.placeholderNameToPosition[placeholderName]);
            }
            else
            {
              placeholder.SetPosition((uint)this.positionalPlaceholderCount);
              this.placeholderNameToPosition.Add(placeholderName, this.positionalPlaceholderCount);
              this.positionalPlaceholderCount++;
            }
            return placeholder.Build();
          }
        case TokenType.LPAREN:
          Expr e = GetExpr();
          ConsumeToken(TokenType.RPAREN);
          return e;
        case TokenType.LCURLY:  // JSON object
          {
            Mysqlx.Expr.Object.Builder builder = Mysqlx.Expr.Object.CreateBuilder();
            if (CurrentTokenTypeEquals(TokenType.LSTRING))
            {
              ParseCommaSeparatedList(() =>
              {
                string key = ConsumeToken(TokenType.LSTRING);
                ConsumeToken(TokenType.COLON);
                Expr value = GetExpr();
                Mysqlx.Expr.Object.Types.ObjectField.Builder objectField = Mysqlx.Expr.Object.Types.ObjectField.CreateBuilder();
                objectField.SetKey(key);
                objectField.SetValue(value);
                return objectField.Build();
              }).ForEach(f => builder.AddFld(f));
            }
            ConsumeToken(TokenType.RCURLY);
            return Expr.CreateBuilder().SetType(Expr.Types.Type.OBJECT).SetObject(builder.Build()).Build();
          }
        case TokenType.CAST:
          {
            ConsumeToken(TokenType.LPAREN);
            Operator.Builder builder = Operator.CreateBuilder().SetName(TokenType.CAST.ToString().ToLowerInvariant());
            builder.AddParam(GetExpr());
            ConsumeToken(TokenType.AS);
            StringBuilder typeStr = new StringBuilder(this.tokens[this.tokenPos].value.ToUpperInvariant());
            // ensure next token is a valid type argument to CAST
            if (CurrentTokenTypeEquals(TokenType.DECIMAL))
            {
              this.tokenPos++;
              if (CurrentTokenTypeEquals(TokenType.LPAREN))
              {
                typeStr.Append(ConsumeToken(TokenType.LPAREN));
                typeStr.Append(ConsumeToken(TokenType.LNUM_INT));
                if (CurrentTokenTypeEquals(TokenType.COMMA))
                {
                  typeStr.Append(ConsumeToken(TokenType.COMMA));
                  typeStr.Append(ConsumeToken(TokenType.LNUM_INT));
                }
                typeStr.Append(ConsumeToken(TokenType.RPAREN));
              }
            }
            else if (CurrentTokenTypeEquals(TokenType.CHAR) || CurrentTokenTypeEquals(TokenType.BINARY))
            {
              this.tokenPos++;
              if (CurrentTokenTypeEquals(TokenType.LPAREN))
              {
                typeStr.Append(ConsumeToken(TokenType.LPAREN));
                typeStr.Append(ConsumeToken(TokenType.LNUM_INT));
                typeStr.Append(ConsumeToken(TokenType.RPAREN));
              }
            }
            else if (CurrentTokenTypeEquals(TokenType.UNSIGNED) || CurrentTokenTypeEquals(TokenType.SIGNED))
            {
              this.tokenPos++;
              if (CurrentTokenTypeEquals(TokenType.INTEGER))
              {
                // don't add optional INTEGER to type string argument
                ConsumeToken(TokenType.INTEGER);
              }
            }
            else if (CurrentTokenTypeEquals(TokenType.JSON) || CurrentTokenTypeEquals(TokenType.DATE) || CurrentTokenTypeEquals(TokenType.DATETIME) ||
                  CurrentTokenTypeEquals(TokenType.TIME))
            {
              this.tokenPos++;
            }
            else
            {
              throw new ArgumentException("Expected valid CAST type argument at " + this.tokenPos);
            }
            ConsumeToken(TokenType.RPAREN);
            // TODO charset?
            builder.AddParam(ExprUtil.BuildLiteralScalar(Encoding.UTF8.GetBytes(typeStr.ToString())));
            return Expr.CreateBuilder().SetType(Expr.Types.Type.OPERATOR).SetOperator(builder.Build()).Build();
          }
        case TokenType.PLUS:
        case TokenType.MINUS:
          {
            if (CurrentTokenTypeEquals(TokenType.LNUM_INT) || CurrentTokenTypeEquals(TokenType.LNUM_DOUBLE))
            {
              // unary operators are handled inline making positive or negative numeric literals
              this.tokens[this.tokenPos].value = t.value + this.tokens[this.tokenPos].value;
              return AtomicExpr();
            }
          }
          return BuildUnaryOp(t.value, AtomicExpr());
        case TokenType.NOT:
        case TokenType.NEG:
        case TokenType.BANG:
          return BuildUnaryOp(t.value, AtomicExpr());
        case TokenType.LSTRING:
          return ExprUtil.BuildLiteralScalar(t.value);
        case TokenType.NULL:
          return ExprUtil.BuildLiteralNullScalar();
        case TokenType.LNUM_INT:
          return ExprUtil.BuildLiteralScalar(long.Parse(t.value));
        case TokenType.LNUM_DOUBLE:
          return ExprUtil.BuildLiteralScalar(double.Parse(t.value));
        case TokenType.TRUE:
        case TokenType.FALSE:
          return ExprUtil.BuildLiteralScalar(t.type == TokenType.TRUE);
        case TokenType.AT:
          return DocumentField();
        case TokenType.STAR:
          // special "0-ary" consideration of "*" as an operator (for COUNT(*), etc)
          return StarOperator();
        case TokenType.IDENT:
          this.tokenPos--; // stay on the identifier
          // check for function call which may be: func(...) or schema.func(...)
          if (NextTokenTypeEquals(TokenType.LPAREN)
                  || (PosTokenTypeEquals(this.tokenPos + 1, TokenType.DOT) && PosTokenTypeEquals(this.tokenPos + 2, TokenType.IDENT) && PosTokenTypeEquals(
                          this.tokenPos + 3, TokenType.LPAREN)))
          {
            return ParseFunctionCall();
          }
          else
          {
            if (this.allowRelationalColumns)
            {
              return ParseColumnIdentifier();
            }
            else
            {
              return DocumentField();
            }
          }
      }
      throw new ArgumentException("Cannot find atomic expression at token pos: " + (this.tokenPos - 1));
    }

    /**
     * Parse a left-associated binary operator.
     *
     * @param types
     *            The token types that denote this operator.
     * @param innerParser
     *            The inner parser that should be called to parse operands.
     * @return an expression tree of the binary operator or a single operand
     */
    Expr ParseLeftAssocBinaryOpExpr(TokenType[] types, Func<Expr> innerParser)
    {
      Expr lhs = innerParser.Invoke();
      while (this.tokenPos < this.tokens.Count && types.ToList().Contains(this.tokens[this.tokenPos].type))
      {
        Operator.Builder builder = Operator.CreateBuilder().SetName(this.tokens[this.tokenPos].value).AddParam(lhs);
        this.tokenPos++;
        builder.AddParam(innerParser.Invoke());
        lhs = Expr.CreateBuilder().SetType(Expr.Types.Type.OPERATOR).SetOperator(builder.Build()).Build();
      }
      return lhs;
    }

    Expr AddSubIntervalExpr()
    {
      Expr lhs = AtomicExpr();
      while ((CurrentTokenTypeEquals(TokenType.PLUS) || CurrentTokenTypeEquals(TokenType.MINUS)) && NextTokenTypeEquals(TokenType.INTERVAL))
      {
        Token op = this.tokens[this.tokenPos];
        this.tokenPos++;
        Operator.Builder builder = Operator.CreateBuilder().AddParam(lhs);

        // INTERVAL expression
        ConsumeToken(TokenType.INTERVAL);

        if (op.type == TokenType.PLUS)
        {
          builder.SetName("date_add");
        }
        else
        {
          builder.SetName("date_sub");
        }

        builder.AddParam(BitExpr()); // amount

        // ensure next token is an interval unit
        if (CurrentTokenTypeEquals(TokenType.MICROSECOND) || CurrentTokenTypeEquals(TokenType.SECOND) || CurrentTokenTypeEquals(TokenType.MINUTE)
                || CurrentTokenTypeEquals(TokenType.HOUR) || CurrentTokenTypeEquals(TokenType.DAY) || CurrentTokenTypeEquals(TokenType.WEEK)
                || CurrentTokenTypeEquals(TokenType.MONTH) || CurrentTokenTypeEquals(TokenType.QUARTER) || CurrentTokenTypeEquals(TokenType.YEAR)
                || CurrentTokenTypeEquals(TokenType.SECOND_MICROSECOND) || CurrentTokenTypeEquals(TokenType.MINUTE_MICROSECOND)
                || CurrentTokenTypeEquals(TokenType.MINUTE_SECOND) || CurrentTokenTypeEquals(TokenType.HOUR_MICROSECOND)
                || CurrentTokenTypeEquals(TokenType.HOUR_SECOND) || CurrentTokenTypeEquals(TokenType.HOUR_MINUTE)
                || CurrentTokenTypeEquals(TokenType.DAY_MICROSECOND) || CurrentTokenTypeEquals(TokenType.DAY_SECOND)
                || CurrentTokenTypeEquals(TokenType.DAY_MINUTE) || CurrentTokenTypeEquals(TokenType.DAY_HOUR)
                || CurrentTokenTypeEquals(TokenType.YEAR_MONTH))
        {
        }
        else
        {
          throw new ArgumentException("Expected interval units at " + this.tokenPos);
        }
        // xplugin demands that intervals be sent uppercase
        // TODO: we need to propagate the appropriate encoding here? it's ascii but it might not *always* be a superset encoding??
        builder.AddParam(ExprUtil.BuildLiteralScalar(Encoding.UTF8.GetBytes(this.tokens[this.tokenPos].value.ToUpperInvariant())));
        this.tokenPos++;

        lhs = Expr.CreateBuilder().SetType(Expr.Types.Type.OPERATOR).SetOperator(builder.Build()).Build();
      }
      return lhs;
    }

    Expr MulDivExpr()
    {
      return ParseLeftAssocBinaryOpExpr(new TokenType[] { TokenType.STAR, TokenType.SLASH, TokenType.MOD }, this.AddSubIntervalExpr);
    }

    Expr AddSubExpr()
    {
      return ParseLeftAssocBinaryOpExpr(new TokenType[] { TokenType.PLUS, TokenType.MINUS }, this.MulDivExpr);
    }

    Expr ShiftExpr()
    {
      return ParseLeftAssocBinaryOpExpr(new TokenType[] { TokenType.LSHIFT, TokenType.RSHIFT }, this.AddSubExpr);
    }

    Expr BitExpr()
    {
      return ParseLeftAssocBinaryOpExpr(new TokenType[] { TokenType.BITAND, TokenType.BITOR, TokenType.BITXOR }, this.ShiftExpr);
    }

    Expr CompExpr()
    {
      return ParseLeftAssocBinaryOpExpr(new TokenType[] { TokenType.GE, TokenType.GT, TokenType.LE, TokenType.LT, TokenType.EQ, TokenType.NE }, this.BitExpr);
    }

    Expr IlriExpr()
    {
      Expr lhs = CompExpr();
      List<TokenType> expected = new List<TokenType>(new TokenType[] { TokenType.IS, TokenType.IN, TokenType.LIKE, TokenType.BETWEEN, TokenType.REGEXP, TokenType.NOT });
      while (this.tokenPos < this.tokens.Count && expected.Contains(this.tokens[this.tokenPos].type))
      {
        bool isNot = false;
        if (CurrentTokenTypeEquals(TokenType.NOT))
        {
          ConsumeToken(TokenType.NOT);
          isNot = true;
        }
        if (this.tokenPos < this.tokens.Count)
        {
          List<Expr> parameters = new List<Expr>();
          parameters.Add(lhs);
          string opName = this.tokens[this.tokenPos].value.ToLowerInvariant();
          switch (this.tokens[this.tokenPos].type)
          {
            case TokenType.IS: // for IS, NOT comes AFTER
              ConsumeToken(TokenType.IS);
              if (CurrentTokenTypeEquals(TokenType.NOT))
              {
                ConsumeToken(TokenType.NOT);
                opName = "is_not";
              }
              parameters.Add(CompExpr());
              break;
            case TokenType.IN:
              ConsumeToken(TokenType.IN);
              parameters.AddRange(ParenExprList());
              break;
            case TokenType.LIKE:
              ConsumeToken(TokenType.LIKE);
              parameters.Add(CompExpr());
              if (CurrentTokenTypeEquals(TokenType.ESCAPE))
              {
                ConsumeToken(TokenType.ESCAPE);
                // add as a third (optional) param
                parameters.Add(CompExpr());
              }
              break;
            case TokenType.BETWEEN:
              ConsumeToken(TokenType.BETWEEN);
              parameters.Add(CompExpr());
              AssertTokenAt(this.tokenPos, TokenType.AND);
              ConsumeToken(TokenType.AND);
              parameters.Add(CompExpr());
              break;
            case TokenType.REGEXP:
              ConsumeToken(TokenType.REGEXP);
              parameters.Add(CompExpr());
              break;
            default:
              throw new ArgumentException("Unknown token after NOT at pos: " + this.tokenPos);
          }
          if (isNot)
          {
            opName = "not_" + opName;
          }
          Operator.Builder builder = Operator.CreateBuilder().SetName(opName).AddRangeParam(parameters);
          lhs = Expr.CreateBuilder().SetType(Expr.Types.Type.OPERATOR).SetOperator(builder.Build()).Build();
        }
      }
      return lhs;
    }

    Expr AndExpr()
    {
      return ParseLeftAssocBinaryOpExpr(new TokenType[] { TokenType.AND, TokenType.ANDAND }, this.IlriExpr);
    }

    Expr OrExpr()
    {
      return ParseLeftAssocBinaryOpExpr(new TokenType[] { TokenType.OR, TokenType.OROR }, this.AndExpr);
    }

    Expr GetExpr()
    {
      Expr e = OrExpr();
      return e;
    }

    /**
     * Parse the entire string as an expression.
     *
     * @return an X-protocol expression tree
     */
    public Expr Parse()
    {
      try
      {
        Expr e = GetExpr();
        if (this.tokenPos != this.tokens.Count)
        {
          throw new ArgumentException("Only " + this.tokenPos + " tokens consumed, out of " + this.tokens.Count);
        }
        return e;
      }
      catch (Exception ex)
      {
        throw new ArgumentException("Unable to parse query '" + this.stringValue + "'", ex);
      }
    }

    /**
     * Utility method to wrap a parser of a list of elements separated by comma.
     *
     * @param <T> the type of element to be parsed
     * @param elementParser the single element parser
     * @return a list of elements parsed
     */
    private List<T> ParseCommaSeparatedList<T>(Func<T> elementParser)
    {
      List<T> elements = new List<T>();
      bool first = true;
      while (first || CurrentTokenTypeEquals(TokenType.COMMA))
      {
        if (!first)
        {
          ConsumeToken(TokenType.COMMA);
        }
        else
        {
          first = false;
        }
        elements.Add(elementParser.Invoke());
      }
      return elements;
    }

    /**
     * Parse an ORDER BY specification which is a comma-separated list of expressions, each may be optionally suffixed by ASC/DESC.
     */
    internal List<Order> ParseOrderSpec()
    {
      return ParseCommaSeparatedList(() =>
      {
        Order.Builder builder = Order.CreateBuilder();
        builder.SetExpr(GetExpr());
        if (CurrentTokenTypeEquals(TokenType.ORDERBY_ASC))
        {
          ConsumeToken(TokenType.ORDERBY_ASC);
          builder.SetDirection(Order.Types.Direction.ASC);
        }
        else if (CurrentTokenTypeEquals(TokenType.ORDERBY_DESC))
        {
          ConsumeToken(TokenType.ORDERBY_DESC);
          builder.SetDirection(Order.Types.Direction.DESC);
        }
        return builder.Build();
      });
    }

    /**
     * Parse a SELECT projection which is a comma-separated list of expressions, each optionally suffixed with a target alias.
     */
    internal List<Projection> ParseTableSelectProjection()
    {
      return ParseCommaSeparatedList(() =>
      {
        Projection.Builder builder = Projection.CreateBuilder();
        builder.SetSource(GetExpr());
        if (CurrentTokenTypeEquals(TokenType.AS))
        {
          ConsumeToken(TokenType.AS);
          builder.SetAlias(ConsumeToken(TokenType.IDENT));
        }
        return builder.Build();
      });
    }

    /**
     * Parse an INSERT field name.
     * @todo unit test
     */
    internal Column ParseTableInsertField()
    {
      return Column.CreateBuilder().SetName(ConsumeToken(TokenType.IDENT)).Build();
    }

    /**
     * Parse an UPDATE field which can include can document paths.
     */
    internal ColumnIdentifier ParseTableUpdateField()
    {
      return ParseColumnIdentifier().Identifier;
    }

    /**
     * Parse a document projection which is similar to SELECT but with document paths as the target alias.
     */
    internal List<Projection> ParseDocumentProjection()
    {
      this.allowRelationalColumns = false;
      return ParseCommaSeparatedList(() =>
      {
        Projection.Builder builder = Projection.CreateBuilder();
        builder.SetSource(GetExpr());
        // alias is not optional for document projection
        ConsumeToken(TokenType.AS);
        builder.SetAlias(ConsumeToken(TokenType.IDENT));
        return builder.Build();
      });
    }

    /**
     * Parse a list of expressions used for GROUP BY.
     */
    internal List<Expr> ParseExprList()
    {
      return ParseCommaSeparatedList(this.GetExpr);
    }

    /**
     * @return the number of positional placeholders in the expression.
     */
    public int GetPositionalPlaceholderCount()
    {
      return this.positionalPlaceholderCount;
    }

    /**
     * @return a mapping of parameter names to positions.
     */
    public Dictionary<string, int> GetPlaceholderNameToPositionMap()
    {
      return this.placeholderNameToPosition;
    }
  }
}
