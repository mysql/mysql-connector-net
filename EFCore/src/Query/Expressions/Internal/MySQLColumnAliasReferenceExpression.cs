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
  /// Enables referencing an alias from within the same SELECT statement, such as in a HAVING clause.
  /// </summary>
  internal class MySQLColumnAliasReferenceExpression : SqlExpression, IEquatable<MySQLColumnAliasReferenceExpression>
  {
    [NotNull]
    public virtual string Alias { get; }

    [NotNull]
    public virtual SqlExpression Expression { get; }

    public MySQLColumnAliasReferenceExpression(
    [NotNull] string alias,
    [NotNull] SqlExpression expression,
    [NotNull] Type type,
    [CanBeNull] RelationalTypeMapping typeMapping)
    : base(type, typeMapping)
    {
      Alias = alias;
      Expression = expression;
    }

    protected override Expression VisitChildren(ExpressionVisitor? visitor)
    => this;

    public virtual MySQLColumnAliasReferenceExpression Update(
    [NotNull] string alias,
    [NotNull] SqlExpression expression)
    => alias == Alias &&
    expression.Equals(Expression)
    ? this
    : new MySQLColumnAliasReferenceExpression(alias, expression, Type, TypeMapping!);

    public override bool Equals(object? obj)
    => Equals(obj as MySQLColumnAliasReferenceExpression);

    public virtual bool Equals(MySQLColumnAliasReferenceExpression? other)
    => ReferenceEquals(this, other) ||
    other != null &&
    base.Equals(other) &&
    Equals(Expression, other.Expression);

    public override int GetHashCode()
    => HashCode.Combine(base.GetHashCode(), Expression);

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
      expressionPrinter.Append("`");
      expressionPrinter.Append(Alias);
      expressionPrinter.Append("`");
    }

    public override string ToString()
    => $"`{Alias}`";

    public virtual SqlExpression ApplyTypeMapping(RelationalTypeMapping? typeMapping)
    => new MySQLColumnAliasReferenceExpression(Alias, Expression, Type, typeMapping!);
  }
}
