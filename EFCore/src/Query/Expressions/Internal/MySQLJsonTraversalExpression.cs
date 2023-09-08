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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MySql.EntityFrameworkCore.Query.Expressions.Internal
{
  /// <summary>
  /// Represents a MySQL JSON operator traversing a JSON document with a path (x->y or x->>y)
  /// </summary>
  internal class MySQLJsonTraversalExpression : SqlExpression, IEquatable<MySQLJsonTraversalExpression>
  {
    /// <summary>
    /// The JSON column.
    /// </summary>
    [NotNull]
    public virtual SqlExpression Expression { get; }

    /// <summary>
    /// The path inside the JSON column.
    /// </summary>
    [NotNull]
    public virtual IReadOnlyList<SqlExpression> Path { get; }

    /// <summary>
    /// Whether the text-returning operator (x->>y) or the object-returning operator (x->y) is used.
    /// </summary>
    public virtual bool ReturnsText { get; }

    public MySQLJsonTraversalExpression(
    [NotNull] SqlExpression expression,
    bool returnsText,
    [NotNull] Type type,
    [CanBeNull] RelationalTypeMapping typeMapping)
    : this(expression, new SqlExpression[0], returnsText, type, typeMapping)
    {
    }

    protected MySQLJsonTraversalExpression(
    [NotNull] SqlExpression expression,
    [NotNull] IReadOnlyList<SqlExpression> path,
    bool returnsText,
    [NotNull] Type type,
    [CanBeNull] RelationalTypeMapping typeMapping)
    : base(type, typeMapping)
    {
      if (returnsText && !TypeReturnsText(type))
        throw new ArgumentException($"{nameof(type)} is not a type that returns text", nameof(type));

      Expression = expression;
      Path = path;
      ReturnsText = returnsText;
    }

    public virtual MySQLJsonTraversalExpression Clone(bool returnsText, Type type, RelationalTypeMapping typeMapping)
    => new MySQLJsonTraversalExpression(Expression, Path, returnsText, type, typeMapping);

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    => Update((SqlExpression)visitor.Visit(Expression),
      Path.Select(p => (SqlExpression)visitor.Visit(p)).ToArray());

    public virtual MySQLJsonTraversalExpression Update(
    [NotNull] SqlExpression expression,
    [NotNull] IReadOnlyList<SqlExpression> path)
    => expression == Expression &&
    path.Count == Path.Count &&
    path.Zip(Path, (x, y) => (x, y)).All(tup => tup.x == tup.y)
    ? this
    : new MySQLJsonTraversalExpression(expression, path, ReturnsText, Type, TypeMapping);

    public virtual MySQLJsonTraversalExpression Append([NotNull] SqlExpression pathComponent, Type returnType, RelationalTypeMapping typeMapping)
    {
      var newPath = new SqlExpression[Path.Count + 1];
      for (var i = 0; i < Path.Count; i++)
      {
        newPath[i] = Path[i];
      }
      newPath[newPath.Length - 1] = pathComponent;
      return new MySQLJsonTraversalExpression(Expression, newPath, ReturnsText, returnType.UnwrapNullableType(), typeMapping);
    }

    public override bool Equals(object? obj)
    => Equals(obj as MySQLJsonTraversalExpression);

    public virtual bool Equals(MySQLJsonTraversalExpression? other)
    => ReferenceEquals(this, other) ||
    other is object &&
    base.Equals(other) &&
    Equals(Expression, other.Expression) &&
    Path.Count == other.Path.Count &&
    Path.Zip(other.Path, (x, y) => (x, y)).All(tup => tup.x == tup.y);

    public override int GetHashCode()
    => HashCode.Combine(base.GetHashCode(), Expression, Path);

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
      expressionPrinter.Visit(Expression);
      expressionPrinter.Append(ReturnsText ? "->>" : "->");
      expressionPrinter.Append("'$");
      foreach (var location in Path)
      {
        expressionPrinter.Append(".");
        expressionPrinter.Visit(location);
      }
      expressionPrinter.Append("'");
    }

    public override string ToString()
    => $"{Expression}{(ReturnsText ? "->>" : "->")}{Path}";

    public static bool TypeReturnsText(Type type)
    => type == typeof(string) ||
    type == typeof(Guid);
  }
}
