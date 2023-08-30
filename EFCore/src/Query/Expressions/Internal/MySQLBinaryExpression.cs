// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Linq.Expressions;

namespace MySql.EntityFrameworkCore.Query.Expressions.Internal
{
  internal enum MySQLBinaryExpressionOperatorType
  {
    IntegerDivision,
    NonOptimizedEqual
  }

  internal class MySQLBinaryExpression : SqlExpression
  {
    public MySQLBinaryExpression(
      MySQLBinaryExpressionOperatorType operatorType,
      SqlExpression left,
      SqlExpression right,
      Type type,
      RelationalTypeMapping? typeMapping)
      : base(type, typeMapping)
    {
      Check.NotNull(left, nameof(left));
      Check.NotNull(right, nameof(right));

      OperatorType = operatorType;

      Left = left;
      Right = right;
    }

    public virtual MySQLBinaryExpressionOperatorType OperatorType { get; }
    public virtual SqlExpression Left { get; }
    public virtual SqlExpression Right { get; }

    protected override Expression Accept(ExpressionVisitor visitor)
      => visitor is MySQLQuerySqlGenerator mySqlQuerySqlGenerator
        ? mySqlQuerySqlGenerator.VisitMySQLBinaryExpression(this)
        : base.Accept(visitor);

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
      var left = (SqlExpression)visitor.Visit(Left);
      var right = (SqlExpression)visitor.Visit(Right);

      return Update(left, right);
    }

    public virtual MySQLBinaryExpression Update(SqlExpression left, SqlExpression right)
      => left != Left || right != Right
        ? new MySQLBinaryExpression(OperatorType, left, right, Type, TypeMapping)
        : this;

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
      var requiresBrackets = RequiresBrackets(Left);

      if (requiresBrackets)
      {
        expressionPrinter.Append("(");
      }

      expressionPrinter.Visit(Left);

      if (requiresBrackets)
      {
        expressionPrinter.Append(")");
      }

      switch (OperatorType)
      {
        case MySQLBinaryExpressionOperatorType.IntegerDivision:
          expressionPrinter.Append(" DIV ");
          break;
      }

      requiresBrackets = RequiresBrackets(Right);

      if (requiresBrackets)
      {
        expressionPrinter.Append("(");
      }

      expressionPrinter.Visit(Right);

      if (requiresBrackets)
      {
        expressionPrinter.Append(")");
      }
    }

    private bool RequiresBrackets(SqlExpression expression)
    {
      return expression is SqlBinaryExpression sqlBinary
        && sqlBinary.OperatorType != ExpressionType.Coalesce
        || expression is LikeExpression;
    }

    public override bool Equals(object? obj)
      => obj != null
      && (ReferenceEquals(this, obj)
        || obj is MySQLBinaryExpression sqlBinaryExpression
          && Equals(sqlBinaryExpression));

    private bool Equals(MySQLBinaryExpression sqlBinaryExpression)
      => base.Equals(sqlBinaryExpression)
      && OperatorType == sqlBinaryExpression.OperatorType
      && Left.Equals(sqlBinaryExpression.Left)
      && Right.Equals(sqlBinaryExpression.Right);

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), OperatorType, Left, Right);
  }
}
