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
using MySql.EntityFrameworkCore.Query.Expressions.Internal;
using MySql.EntityFrameworkCore.Utils;

namespace MySql.EntityFrameworkCore.Query.Internal
{
  internal class MySQLSqlNullabilityProcessor : SqlNullabilityProcessor
  {
    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    /// <summary>
    /// Creates a new instance of the <see cref="MySQLSqlNullabilityProcessor" />.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this class.</param>
    /// <param name="useRelationalNulls">A bool value indicating whether relational null semantics are in use.</param>
    public MySQLSqlNullabilityProcessor(
      RelationalParameterBasedSqlProcessorDependencies dependencies,
      bool useRelationalNulls)
      : base(dependencies, useRelationalNulls)
      => _sqlExpressionFactory = dependencies.SqlExpressionFactory;

    /// <inheritdoc />
    protected override SqlExpression VisitCustomSqlExpression(
      SqlExpression sqlExpression, bool allowOptimizedExpansion, out bool nullable)
      => sqlExpression switch
      {
        MySQLBinaryExpression binaryExpression => VisitBinary(binaryExpression, allowOptimizedExpansion, out nullable),
        MySQLCollateExpression collateExpression => VisitCollate(collateExpression, allowOptimizedExpansion, out nullable),
        MySQLComplexFunctionArgumentExpression complexFunctionArgumentExpression => VisitComplexFunctionArgument(complexFunctionArgumentExpression, allowOptimizedExpansion, out nullable),
        _ => base.VisitCustomSqlExpression(sqlExpression, allowOptimizedExpansion, out nullable)
      };

    /// <summary>
    /// Visits a <see cref="MySQLBinaryExpression" /> and computes its nullability.
    /// </summary>
    /// <param name="binaryExpression">A <see cref="MySQLBinaryExpression" /> expression to visit.</param>
    /// <param name="allowOptimizedExpansion">A bool value indicating if optimized expansion which considers null value as false value is allowed.</param>
    /// <param name="nullable">A bool value indicating whether the sql expression is nullable.</param>
    /// <returns>An optimized sql expression.</returns>
    protected virtual SqlExpression VisitBinary(
      MySQLBinaryExpression binaryExpression,
      bool allowOptimizedExpansion,
      out bool nullable)
    {
      Check.NotNull(binaryExpression, nameof(binaryExpression));

      var left = Visit(binaryExpression.Left, allowOptimizedExpansion, out var leftNullable);
      var right = Visit(binaryExpression.Right, allowOptimizedExpansion, out var rightNullable);

      nullable = leftNullable || rightNullable;

      return binaryExpression.Update(left, right);
    }

    /// <summary>
    /// Visits a <see cref="MySQLCollateExpression" /> and computes its nullability.
    /// </summary>
    /// <param name="collateExpression">A <see cref="MySQLCollateExpression" /> expression to visit.</param>
    /// <param name="allowOptimizedExpansion">A bool value indicating if optimized expansion which considers null value as false value is allowed.</param>
    /// <param name="nullable">A bool value indicating whether the sql expression is nullable.</param>
    /// <returns>An optimized sql expression.</returns>
    protected virtual SqlExpression VisitCollate(
      MySQLCollateExpression collateExpression,
      bool allowOptimizedExpansion,
      out bool nullable)
    {
      Check.NotNull(collateExpression, nameof(collateExpression));

      var valueExpression = Visit(collateExpression.ValueExpression, allowOptimizedExpansion, out nullable);

      return collateExpression.Update(valueExpression);
    }

    /// <summary>
    /// Visits a <see cref="MySQLComplexFunctionArgumentExpression" /> and computes its nullability.
    /// </summary>
    /// <param name="complexFunctionArgumentExpression">A <see cref="MySQLComplexFunctionArgumentExpression" /> expression to visit.</param>
    /// <param name="allowOptimizedExpansion">A bool value indicating if optimized expansion which considers null value as false value is allowed.</param>
    /// <param name="nullable">A bool value indicating whether the sql expression is nullable.</param>
    /// <returns>An optimized sql expression.</returns>
    protected virtual SqlExpression VisitComplexFunctionArgument(
      MySQLComplexFunctionArgumentExpression complexFunctionArgumentExpression,
      bool allowOptimizedExpansion,
      out bool nullable)
    {
      Check.NotNull(complexFunctionArgumentExpression, nameof(complexFunctionArgumentExpression));
      nullable = false;
      var argumentParts = new SqlExpression[complexFunctionArgumentExpression.ArgumentParts.Count];

      for (var i = 0; i < argumentParts.Length; i++)
      {
        argumentParts[i] = Visit(complexFunctionArgumentExpression.ArgumentParts[i], allowOptimizedExpansion, out var argumentPartNullable);
        nullable |= argumentPartNullable;
      }

      return complexFunctionArgumentExpression.Update(argumentParts, complexFunctionArgumentExpression.Delimiter);
    }
  }
}
