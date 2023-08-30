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
using System;
using System.Linq.Expressions;

namespace MySql.EntityFrameworkCore.Query.Expressions.Internal
{
  /// <summary>
  ///   Represents a MySQL JSON array index (x[y]).
  /// </summary>
  internal class MySQLJsonArrayIndexExpression : SqlExpression, IEquatable<MySQLJsonArrayIndexExpression>
  {
    [NotNull]
    public virtual SqlExpression Expression { get; }

    public MySQLJsonArrayIndexExpression(
      [NotNull] SqlExpression expression,
      [NotNull] Type type,
      [CanBeNull] RelationalTypeMapping typeMapping)
      : base(type, typeMapping)
    {
      Expression = expression;
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
      => Update((SqlExpression)visitor.Visit(Expression));

    public virtual MySQLJsonArrayIndexExpression Update(
      [NotNull] SqlExpression expression)
      => expression == Expression
        ? this
        : new MySQLJsonArrayIndexExpression(expression, Type, TypeMapping!);

    public override bool Equals(object? obj)
      => Equals(obj as MySQLJsonArrayIndexExpression);

    public virtual bool Equals(MySQLJsonArrayIndexExpression? other)
      => ReferenceEquals(this, other) ||
         other != null &&
         base.Equals(other) &&
         Equals(Expression, other.Expression);

    public override int GetHashCode()
      => HashCode.Combine(base.GetHashCode(), Expression);

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
      expressionPrinter.Append("[");
      expressionPrinter.Visit(Expression);
      expressionPrinter.Append("]");
    }

    public override string ToString()
      => $"[{Expression}]";

    public virtual SqlExpression ApplyTypeMapping(RelationalTypeMapping typeMapping)
      => new MySQLJsonArrayIndexExpression(Expression, Type, typeMapping);
  }
}
