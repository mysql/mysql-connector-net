// Copyright © 2015, 2016 Oracle and/or its affiliates. All rights reserved.
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
using Mysqlx.Expr;
using Mysqlx.Datatypes;
using Mysqlx.Crud;
using Google.ProtocolBuffers;

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
      return Expr.CreateBuilder().SetType(Expr.Types.Type.LITERAL).SetLiteral(scalar).Build();
    }

    public static Scalar NullScalar()
    {
      return Scalar.CreateBuilder().SetType(Scalar.Types.Type.V_NULL).Build();
    }

    public static Scalar ScalarOf(double d)
    {
      return Scalar.CreateBuilder().SetType(Scalar.Types.Type.V_DOUBLE).SetVDouble(d).Build();
    }

    public static Scalar ScalarOf(long l)
    {
      return Scalar.CreateBuilder().SetType(Scalar.Types.Type.V_SINT).SetVSignedInt(l).Build();
    }

    public static Scalar ScalarOf(String str)
    {
      Scalar.Types.String strValue = Scalar.Types.String.CreateBuilder().SetValue(ByteString.CopyFromUtf8(str)).Build();
      return Scalar.CreateBuilder().SetType(Scalar.Types.Type.V_STRING).SetVString(strValue).Build();
    }

    public static Scalar ScalarOf(byte[] bytes)
    {
      return Scalar.CreateBuilder().SetType(Scalar.Types.Type.V_OCTETS).SetVOpaque(ByteString.CopyFrom(bytes)).Build();
    }

    public static Scalar ScalarOf(Boolean b)
    {
      return Scalar.CreateBuilder().SetType(Scalar.Types.Type.V_BOOL).SetVBool(b).Build();
    }

    /**
     * Build an Any with a string value.
     */
    public static Any BuildAny(String str)
    {
      // same as Expr
      Scalar.Types.String sstr = Scalar.Types.String.CreateBuilder().SetValue(ByteString.CopyFromUtf8(str)).Build();
      Scalar s = Scalar.CreateBuilder().SetType(Scalar.Types.Type.V_STRING).SetVString(sstr).Build();
      Any a = Any.CreateBuilder().SetType(Any.Types.Type.SCALAR).SetScalar(s).Build();
      return a;
    }

    public static Any BuildAny(Boolean b)
    {
      return Any.CreateBuilder().SetType(Any.Types.Type.SCALAR).SetScalar(ScalarOf(b)).Build();
    }

    public static Any BuildAny(object value)
    {
      return Any.CreateBuilder().SetType(Any.Types.Type.SCALAR).SetScalar(ExprUtil.ArgObjectToScalar(value)).Build();
    }

    public static Collection BuildCollection(String schemaName, String collectionName)
    {
      return Collection.CreateBuilder().SetSchema(schemaName).SetName(collectionName).Build();
    }

    public static Scalar ArgObjectToScalar(System.Object value)
    {
      return ArgObjectToExpr(value, false).Literal;
    }

    public static Expr ArgObjectToExpr(System.Object value, Boolean allowRelationalColumns)
    {
      if (value == null)
        return BuildLiteralNullScalar();

      if (value is bool)
        return BuildLiteralScalar(Convert.ToBoolean(value));
      else if (value is byte || value is short || value is int || value is long)
        return BuildLiteralScalar(Convert.ToInt64(value));
      else if (value is float || value is double)
        return BuildLiteralScalar(Convert.ToDouble(value));
      else if (value is string)
      {
        try
        {
          // try to parse expressions
          Expr expr = new ExprParser((string)value).Parse();
          if (expr.HasIdentifier)
            return BuildLiteralScalar((string)value);
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
  }
}
