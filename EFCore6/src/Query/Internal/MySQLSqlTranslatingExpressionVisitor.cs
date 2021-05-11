// Copyright (c) 2021, Oracle and/or its affiliates.
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
using MySql.EntityFrameworkCore.Utils;
using System.Linq.Expressions;

namespace MySql.EntityFrameworkCore.Query.Internal
{
  internal class MySQLSqlTranslatingExpressionVisitor : RelationalSqlTranslatingExpressionVisitor
  {
    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    public MySQLSqlTranslatingExpressionVisitor(
        RelationalSqlTranslatingExpressionVisitorDependencies dependencies,
        QueryCompilationContext model,
        QueryableMethodTranslatingExpressionVisitor queryableMethodTranslatingExpressionVisitor)
        : base(dependencies, model, queryableMethodTranslatingExpressionVisitor)
    {
      _sqlExpressionFactory = dependencies.SqlExpressionFactory;
    }

    protected override Expression VisitBinary(BinaryExpression binaryExpression)
    {
      // Returning null forces client projection.
      var visitedExpression = (SqlExpression)base.VisitBinary(binaryExpression);

      if (visitedExpression is SqlBinaryExpression visitedBinaryExpression)
      {
        switch (visitedBinaryExpression.OperatorType)
        {
          case ExpressionType.Add:
          case ExpressionType.Subtract:
          case ExpressionType.Multiply:
          case ExpressionType.Divide:
          case ExpressionType.Modulo:
            if (IsDateTimeBasedOperation(visitedBinaryExpression)!) return visitedBinaryExpression;
            break;
        }
      }

      return visitedExpression;
    }

    private static bool IsDateTimeBasedOperation(SqlBinaryExpression binaryExpression)
    {
      if (binaryExpression.TypeMapping != null
          && (binaryExpression.TypeMapping.StoreType.StartsWith("date") || binaryExpression.TypeMapping.StoreType.StartsWith("time")))
      {
        return true;
      }

      return false;
    }
  }
}