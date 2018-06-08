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

using System;
using Mysqlx.Expr;
using Mysqlx.Datatypes;
using Mysqlx.Crud;
using Google.Protobuf;
using System.Collections.Generic;
using System.Reflection;

namespace MySqlX.Protocol.X
{
  internal class ExprUtil
  {
    /**
     * Proto-buf helper to build a LITERAL Expr with a Scalar NULL type.
     */
    public static Expr BuildLiteralNullScalar()
    {
      return BuildLiteralExpr(NullScalar());
    }

    /**
     * Proto-buf helper to build a LITERAL Expr with a Scalar DOUBLE type (wrapped in Any).
     */
    public static Expr BuildLiteralScalar(double d)
    {
      return BuildLiteralExpr(ScalarOf(d));
    }

    /**
     * Proto-buf helper to build a LITERAL Expr with a Scalar SINT (signed int) type (wrapped in Any).
     */
    public static Expr BuildLiteralScalar(long l)
    {
      return BuildLiteralExpr(ScalarOf(l));
    }

    /**
     * Proto-buf helper to build a LITERAL Expr with a Scalar UINT (unsigned int) type (wrapped in Any).
     */
    public static Expr BuildLiteralScalar(ulong l)
    {
      return BuildLiteralExpr(ScalarOf(l));
    }

    /**
     * Proto-buf helper to build a LITERAL Expr with a Scalar STRING type (wrapped in Any).
     */
    public static Expr BuildLiteralScalar(String str)
    {
      return BuildLiteralExpr(ScalarOf(str));
    }

    /**
     * Proto-buf helper to build a LITERAL Expr with a Scalar OCTETS type (wrapped in Any).
     */
    public static Expr BuildLiteralScalar(byte[] bytes)
    {
      return BuildLiteralExpr(ScalarOf(bytes));
    }

    /**
     * Proto-buf helper to build a LITERAL Expr with a Scalar BOOL type (wrapped in Any).
     */
    public static Expr BuildLiteralScalar(Boolean b)
    {
      return BuildLiteralExpr(ScalarOf(b));
    }

    /**
     * Wrap an Any value in a LITERAL expression.
     */
    public static Expr BuildLiteralExpr(Scalar scalar)
    {
      return new Expr() { Type = Expr.Types.Type.Literal, Literal = scalar };
    }

    public static Scalar NullScalar()
    {
      return new Scalar() { Type = Scalar.Types.Type.VNull };
    }

    public static Scalar ScalarOf(double d)
    {
      return new Scalar() { Type = Scalar.Types.Type.VDouble, VDouble = d };
    }

    public static Scalar ScalarOf(long l)
    {
      return new Scalar() { Type = Scalar.Types.Type.VSint, VSignedInt = l};
    }

    public static Scalar ScalarOf(ulong ul)
    {
      return new Scalar() { Type = Scalar.Types.Type.VUint, VUnsignedInt = ul};
    }

    public static Scalar ScalarOf(String str)
    {
      Scalar.Types.String strValue = new Scalar.Types.String() { Value = ByteString.CopyFromUtf8(str) };
      return new Scalar() { Type = Scalar.Types.Type.VString, VString = strValue };
    }

    public static Scalar ScalarOf(byte[] bytes)
    {
      return new Scalar() { Type = Scalar.Types.Type.VOctets, VOctets = new Scalar.Types.Octets() { Value = ByteString.CopyFrom(bytes) } };
    }

    public static Scalar ScalarOf(Boolean b)
    {
      return new Scalar() { Type = Scalar.Types.Type.VBool, VBool = b };
    }

    /**
     * Build an Any with a string value.
     */
    public static Any BuildAny(String str)
    {
      // same as Expr
      Scalar.Types.String sstr = new Scalar.Types.String();
      sstr.Value = ByteString.CopyFromUtf8(str);
      Scalar s = new Scalar();
      s.Type = Scalar.Types.Type.VString;
      s.VString = sstr;
      Any a = new Any();
      a.Type = Any.Types.Type.Scalar;
      a.Scalar = s;
      return a;
    }

    public static Mysqlx.Datatypes.Object.Types.ObjectField BuildObject(string key, object value, bool evaluateStringExpression)
    {
      Mysqlx.Datatypes.Object.Types.ObjectField item = new Mysqlx.Datatypes.Object.Types.ObjectField();
      item.Key = key;
      item.Value = evaluateStringExpression ? BuildAny(value) : BuildAnyWithoutEvaluationExpression(value);
      return item;
    }

    public static Any BuildEmptyAny(Any.Types.Type type)
    {
      return new Any() { Type = type, Obj = new Mysqlx.Datatypes.Object() };
    }

    public static Any BuildAny(Boolean b)
    {
      return new Any() { Type = Any.Types.Type.Scalar, Scalar = ScalarOf(b) };
    }

    public static Any BuildAny(object value)
    {
      return new Any() { Type = Any.Types.Type.Scalar, Scalar = ExprUtil.ArgObjectToScalar(value) };
    }

    public static Any BuildAnyWithoutEvaluationExpression(object value)
    {
      return new Any() { Type = Any.Types.Type.Scalar, Scalar = ExprUtil.ArgObjectToScalar(value, false) };
    }

    public static Collection BuildCollection(String schemaName, String collectionName)
    {
      return new Collection() { Schema = schemaName, Name = collectionName };
    }

    public static Scalar ArgObjectToScalar(System.Object value, Boolean evaluateStringExpression = true)
    {
      return ArgObjectToExpr(value, false, evaluateStringExpression).Literal;
    }

    public static Expr ArgObjectToExpr(System.Object value, Boolean allowRelationalColumns, Boolean evaluateStringExpresssion = true)
    {
      if (value == null)
        return BuildLiteralNullScalar();

      if (value is Dictionary<string, object>)
        value = new XDevAPI.DbDoc(value).ToString();

      if (value is XDevAPI.MySqlExpression)
        value = (value as XDevAPI.MySqlExpression).value;

      if (value is bool)
        return BuildLiteralScalar(Convert.ToBoolean(value));
      else if (value is byte || value is short || value is int || value is long)
        return BuildLiteralScalar(Convert.ToInt64(value));
      else if (value is ushort || value is uint || value is ulong)
        return BuildLiteralScalar(Convert.ToUInt64(value));
      else if (value is float || value is double)
        return BuildLiteralScalar(Convert.ToDouble(value));
      else if (value is string)
      {
        try
        {
          // try to parse expressions
          var stringValue = (string) value;
          if (!evaluateStringExpresssion) return BuildLiteralScalar((string)value);

          Expr expr = new ExprParser(stringValue).Parse();
          if (expr.Identifier != null)
            return BuildLiteralScalar(stringValue);
          return expr;
        }
        catch
        {
          // if can't parse, returns as literal
          return BuildLiteralScalar((string)value);
        }
      }
      else if (value is XDevAPI.DbDoc)
        return (BuildLiteralScalar(value.ToString()));

      throw new NotSupportedException("Value of type " + value.GetType() + " is not currently supported.");
    }

    public static string JoinString(string[] values)
    {
      if (values == null) return string.Empty;
      return string.Join(", ", values);
    }

    /// <summary>
    /// Parses an anonymous object into a dictionary.
    /// </summary>
    /// <param name="value">The object to parse.</param>
    /// <returns>A dictionary if the provided object is an anonymous object; otherwise, <c>null</c>.</returns>
    public static Dictionary<string, object> ParseAnonymousObject(object value)
    {
      if (value == null)
        return null;

      Type type = value.GetType();
      if (type.Name.Contains("Anonymous"))
      {
        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        PropertyInfo[] props = type.GetProperties();
        foreach (PropertyInfo prop in props)
          dictionary.Add(prop.Name, prop.GetValue(value, null));

        return dictionary;
      }

      return null;
    }
  }
}
