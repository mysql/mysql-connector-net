// Copyright © 2015, 2016, Oracle and/or its affiliates. All rights reserved.
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

using Mysqlx.Datatypes;
using Mysqlx.Expr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MySqlX.Protocol.X
{
  internal class ExprUnparser
  {
    /**
     * List of operators which will be serialized as infix operators.
     */
    static HashSet<string> infixOperators = new HashSet<string>();

    static ExprUnparser()
    {
      infixOperators.Add("and");
      infixOperators.Add("or");
    }

    // /**
    //  * Convert an "Any" (scalar) to a string.
    //  */
    // static string anyToString(Any e) {
    //     switch (e.getType()) {
    //         case SCALAR:
    //             return scalarToString(e.getScalar());
    //         default:
    //             throw new IllegalArgumentException("Unknown type tag: " + e.getType());
    //     }
    // }

    /**
     * Scalar to string.
     */
    static string ScalarToString(Scalar e)
    {
      switch (e.Type)
      {
        case Scalar.Types.Type.VSint:
          return "" + e.VSignedInt;
        case Scalar.Types.Type.VOctets:
          return "\"" + EscapeLiteral(e.VOctets.Value.ToStringUtf8()) + "\"";
        case Scalar.Types.Type.VString:
          return "\"" + EscapeLiteral(e.VString.Value.ToStringUtf8()) + "\"";
        case Scalar.Types.Type.VDouble:
          return e.VDouble.ToString();
        case Scalar.Types.Type.VBool:
          return e.VBool ? "TRUE" : "FALSE";
        case Scalar.Types.Type.VNull:
          return "NULL";
        default:
          throw new ArgumentException("Unknown type tag: " + e.Type);
      }
    }

    /**
     * JSON document path to string.
     */
    static string DocumentPathToString(IList<DocumentPathItem> items)
    {
      StringBuilder docPathString = new StringBuilder();
      foreach (DocumentPathItem item in items)
      {
        switch (item.Type)
        {
          case DocumentPathItem.Types.Type.Member:
            docPathString.Append(".").Append(QuoteDocumentPathMember(item.Value));
            break;
          case DocumentPathItem.Types.Type.MemberAsterisk:
            docPathString.Append(".*");
            break;
          case DocumentPathItem.Types.Type.ArrayIndex:
            docPathString.Append("[").Append("" + item.Index).Append("]");
            break;
          case DocumentPathItem.Types.Type.ArrayIndexAsterisk:
            docPathString.Append("[*]");
            break;
          case DocumentPathItem.Types.Type.DoubleAsterisk:
            docPathString.Append("**");
            break;
        }
      }
      return docPathString.ToString();
    }

    /**
     * Column identifier (or JSON path) to string.
     */
    static string ColumnIdentifierToString(ColumnIdentifier e)
    {
      string s = QuoteIdentifier(e.Name);
      if (!string.IsNullOrEmpty(e.TableName))
      {
        s = QuoteIdentifier(e.TableName) + "." + s;
      }
      if (!string.IsNullOrEmpty(e.SchemaName))
      {
        s = QuoteIdentifier(e.SchemaName) + "." + s;
      }
      if (e.DocumentPath.Count > 0)
      {
        s = s + "$" + DocumentPathToString(e.DocumentPath);
      }
      return s;
    }

    /**
     * Function call to string.
     */
    static string FunctionCallToString(FunctionCall e)
    {
      Identifier i = e.Name;
      string s = QuoteIdentifier(i.Name);
      if (!string.IsNullOrEmpty(i.SchemaName))
      {
        s = QuoteIdentifier(i.SchemaName) + "." + s;
      }
      s = s + "(";
      foreach (Expr p in e.Param)
      {
        s += ExprToString(p) + ", ";
      }
      s = System.Text.RegularExpressions.Regex.Replace(s, ", $", "");
      s += ")";
      return s;
    }

    /**
     * Create a string from a list of (already stringified) parameters. Surround by parens and separate by commas.
     */
    static string ParamListToString(List<string> parameters)
    {
      string s = "(";
      bool first = true;
      foreach (string param in parameters)
      {
        if (!first)
        {
          s += ", ";
        }
        first = false;
        s += param;
      }
      return s + ")";
    }

    /**
     * Convert an operator to a string. Includes special cases for chosen infix operators (AND, OR) and special forms such as LIKE and BETWEEN.
     */
    static string OperatorToString(Operator e)
    {
      string name = e.Name;
      List<string> parameters = new List<string>();
      foreach (Expr p in e.Param)
      {
        parameters.Add(ExprToString(p));
      }
      if ("between".Equals(name) || "not_between".Equals(name))
      {
        name = name.Replace("not_between", "not between");
        return string.Format("({0} {1} {2} AND {3})", parameters[0], name, parameters[1], parameters[2]);
      }
      else if ("in".Equals(name) || "not_in".Equals(name))
      {
        name = name.Replace("not_in", "not in");
        return string.Format("{0} {1}{2}", parameters[0], name, ParamListToString(parameters.GetRange(1, parameters.Count - 1)));
      }
      else if ("like".Equals(name) || "not_like".Equals(name))
      {
        name = name.Replace("not_like", "not like");
        string s = string.Format("{0} {1} {2}", parameters[0], name, parameters[1]);
        if (parameters.Count == 3)
        {
          s += " ESCAPE " + parameters[2];
        }
        return s;
      }
      else if ("regexp".Equals(name) || "not_regexp".Equals("name"))
      {
        name = name.Replace("not_regexp", "not regexp");
        return string.Format("({0} {1} {2})", parameters[0], name, parameters[1]);
      }
      else if ("cast".Equals(name))
      {
        return string.Format("cast({0} AS {1})", parameters[0], parameters[1].Replace("\"", ""));
      }
      else if ((name.Length < 3 || infixOperators.Contains(name)) && parameters.Count == 2)
      {
        return string.Format("({0} {1} {2})", parameters[0], name, parameters[1]);
      }
      else if (parameters.Count == 1)
      {
        return string.Format("{0}{1}", name, parameters[0]);
      }
      else if (parameters.Count == 0)
      {
        return name;
      }
      else
      {
        return name + ParamListToString(parameters);
      }
    }

    static string ObjectToString(Mysqlx.Expr.Object o)
    {
      var fields = o.Fld;
      var selectAsString = fields.Select(f => string.Format("'{0}':{1}", QuoteJsonKey(f.Key), ExprToString(f.Value)));
      return "{" + string.Join(", ", selectAsString) + "}";
    }

    /**
     * Escape a string literal.
     */
    public static string EscapeLiteral(string s)
    {
      return s.Replace("\"", "\"\"");
    }

    /**
     * Quote a named identifer.
     */
    public static string QuoteIdentifier(string ident)
    {
      // TODO: make sure this is correct
      if (ident.Contains("`") || ident.Contains("\"") || ident.Contains("'") || ident.Contains("$") || ident.Contains("."))
      {
        return "`" + ident.Replace("`", "``") + "`";
      }
      return ident;
    }

    public static string QuoteJsonKey(string key)
    {
      return key.Replace("'", "\\\\'");
    }

    public static string QuoteDocumentPathMember(string member)
    {
      if (!Regex.IsMatch(member, "[a-zA-Z0-9_]*"))
      {
        return "\"" + member.Replace("\"", "\\\\\"") + "\"";
      }
      return member;
    }

    /**
     * Serialize an expression to a string.
     */
    public static string ExprToString(Expr e)
    {
      switch (e.Type)
      {
        case Expr.Types.Type.Literal:
          return ScalarToString(e.Literal);
        case Expr.Types.Type.Ident:
          return ColumnIdentifierToString(e.Identifier);
        case Expr.Types.Type.FuncCall:
          return FunctionCallToString(e.FunctionCall);
        case Expr.Types.Type.Operator:
          return OperatorToString(e.Operator);
        case Expr.Types.Type.Variable:
          return "@" + QuoteIdentifier(e.Variable);
        case Expr.Types.Type.Placeholder:
          return ":" + e.Position;
        case Expr.Types.Type.Object:
          return ObjectToString(e.Object);
        default:
          throw new ArgumentException("Unknown type tag: " + e.Type);
      }
    }
  }
}
