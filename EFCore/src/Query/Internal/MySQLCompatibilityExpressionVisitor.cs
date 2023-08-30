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

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using System;
using System.Linq.Expressions;

namespace MySql.EntityFrameworkCore.Query.Internal
{
  internal class MySQLCompatibilityExpressionVisitor : ExpressionVisitor
  {
    private readonly IMySQLOptions _options;

    public MySQLCompatibilityExpressionVisitor(IMySQLOptions options)
    {
      _options = options;
    }

    protected override Expression VisitExtension(Expression extensionExpression)
    {
      switch (extensionExpression)
      {
        case RowNumberExpression rowNumberExpression:
          return VisitRowNumber(rowNumberExpression);
        case CrossApplyExpression crossApplyExpression:
          return VisitCrossApply(crossApplyExpression);
        case OuterApplyExpression outerApplyExpression:
          return VisitOuterApply(outerApplyExpression);
        case ExceptExpression exceptExpression:
          return VisitExcept(exceptExpression);
        case IntersectExpression intersectExpression:
          return VisitIntercept(intersectExpression);
        case ShapedQueryExpression shapedQueryExpression:
          return shapedQueryExpression.Update(Visit(shapedQueryExpression.QueryExpression), shapedQueryExpression.ShaperExpression);
        default:
          return base.VisitExtension(extensionExpression);
      }

      return base.VisitExtension(extensionExpression);
    }

    protected virtual Expression VisitRowNumber(RowNumberExpression rowNumberExpression)
      => CheckSupport(rowNumberExpression, true);

    protected virtual Expression VisitOuterApply(OuterApplyExpression outerApplyExpression)
      => CheckSupport(outerApplyExpression, true);

    protected virtual Expression VisitCrossApply(CrossApplyExpression crossApplyExpression)
      => CheckSupport(crossApplyExpression, true);
    protected virtual Expression VisitExcept(ExceptExpression exceptExpression)
      => CheckSupport(exceptExpression, false);

    protected virtual Expression VisitIntercept(IntersectExpression intersectExpression)
      => CheckSupport(intersectExpression, false);

    protected virtual Expression CheckSupport(Expression expression, bool isSupported)
      => CheckTranslated(
        isSupported
          ? base.VisitExtension(expression)
          : null,
        expression);

    protected virtual Expression CheckTranslated(Expression? translated, Expression original)
    {
      if (translated == null)
      {
        throw new InvalidOperationException(
          CoreStrings.TranslationFailed(original.Print()));
      }

      return translated;
    }
  }
}