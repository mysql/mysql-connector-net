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
using MySql.EntityFrameworkCore.Query.Expressions.Internal;
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Linq.Expressions;

namespace MySql.EntityFrameworkCore.Query
{
  internal class MySQLQuerySqlGenerator : QuerySqlGenerator
  {
    private const ulong LimitUpperBound = 18446744073709551610;

    public MySQLQuerySqlGenerator([NotNull] QuerySqlGeneratorDependencies dependencies)
      : base(dependencies)
    {
    }

    protected override void GenerateLimitOffset([NotNull] SelectExpression selectExpression)
    {
      Check.NotNull(selectExpression, nameof(selectExpression));

      if (selectExpression.Limit != null)
      {
        Sql.AppendLine().Append("LIMIT ");
        Visit(selectExpression.Limit);
      }

      if (selectExpression.Offset != null)
      {
        if (selectExpression.Limit == null)
        {
          // if we want to use Skip() without Take() we have to define the upper limit of LIMIT
          Sql.AppendLine().Append("LIMIT ").Append(LimitUpperBound.ToString());
        }

        Sql.Append(" OFFSET ");
        Visit(selectExpression.Offset);
      }
    }

    public Expression VisitMySQLComplexFunctionArgumentExpression(MySQLComplexFunctionArgumentExpression mySqlComplexFunctionArgumentExpression)
    {
      Check.NotNull(mySqlComplexFunctionArgumentExpression, nameof(mySqlComplexFunctionArgumentExpression));

      var first = true;
      foreach (var argument in mySqlComplexFunctionArgumentExpression.ArgumentParts)
      {
        if (first)
        {
          first = false;
        }
        else
        {
          Sql.Append(" ");
        }

        Visit(argument);
      }

      return mySqlComplexFunctionArgumentExpression;
    }

    public Expression VisitMySQLBinaryExpression(MySQLBinaryExpression mySqlBinaryExpression)
    {
      Sql.Append("(");
      Visit(mySqlBinaryExpression.Left);
      Sql.Append(")");

      switch (mySqlBinaryExpression.OperatorType)
      {
        case MySQLBinaryExpressionOperatorType.IntegerDivision:
          Sql.Append(" DIV ");
          break;

        default:
          throw new ArgumentOutOfRangeException();
      }

      Sql.Append("(");
      Visit(mySqlBinaryExpression.Right);
      Sql.Append(")");

      return mySqlBinaryExpression;
    }

    public Expression VisitMySQLCollateExpression(MySQLCollateExpression mySqlCollateExpression)
    {
      Check.NotNull(mySqlCollateExpression, nameof(mySqlCollateExpression));

      Sql.Append("CONVERT(");

      Visit(mySqlCollateExpression.ValueExpression);

      Sql.Append($" USING {mySqlCollateExpression.Charset}) COLLATE {mySqlCollateExpression.Collation}");

      return mySqlCollateExpression;
    }
  }
}
