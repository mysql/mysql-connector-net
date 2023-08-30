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
using MySql.EntityFrameworkCore.Extensions;
using MySql.EntityFrameworkCore.Query.Expressions.Internal;
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MySql.EntityFrameworkCore.Query
{
  internal class MySQLQuerySqlGenerator : QuerySqlGenerator
  {
    private const ulong LimitUpperBound = 18446744073709551610;
    private static readonly Dictionary<string, string[]> _castMappings = new Dictionary<string, string[]>
  {
    { "signed", new []{ "tinyint", "smallint", "mediumint", "int", "bigint", "bit" }},
    { "decimal(65,30)", new []{ "decimal" } },
    { "double", new []{ "double" } },
    { "float", new []{ "float" } },
    { "binary", new []{ "binary", "varbinary", "tinyblob", "blob", "mediumblob", "longblob" } },
    { "datetime(6)", new []{ "datetime(6)" } },
    { "datetime", new []{ "datetime" } },
    { "date", new []{ "date" } },
    { "timestamp(6)", new []{ "timestamp(6)" } },
    { "timestamp", new []{ "timestamp" } },
    { "time(6)", new []{ "time(6)" } },
    { "time", new []{ "time" } },
    { "json", new []{ "json" } },
    { "char", new []{ "char", "varchar", "text", "tinytext", "mediumtext", "longtext" } },
    { "nchar", new []{ "nchar", "nvarchar" } },
  };

    public MySQLQuerySqlGenerator([NotNull] QuerySqlGeneratorDependencies dependencies)
      : base(dependencies)
    {
    }

    protected override Expression VisitExtension(Expression extensionExpression)
    => extensionExpression switch
    {
      MySQLJsonTraversalExpression jsonTraversalExpression => VisitJsonPathTraversal(jsonTraversalExpression),
      MySQLColumnAliasReferenceExpression columnAliasReferenceExpression => VisitColumnAliasReference(columnAliasReferenceExpression),
      _ => base.VisitExtension(extensionExpression)
    };

    private Expression VisitColumnAliasReference(MySQLColumnAliasReferenceExpression columnAliasReferenceExpression)
    {
      Sql.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(columnAliasReferenceExpression.Alias));
      return columnAliasReferenceExpression;
    }

    protected virtual Expression VisitJsonPathTraversal(MySQLJsonTraversalExpression expression)
    {
      // If the path contains parameters, then the -> and ->> aliases are not supported by MySQL, because
      // we need to concatenate the path and the parameters.
      // We will use JSON_EXTRACT (and JSON_UNQUOTE if needed) only in this case, because the aliases
      // are much more readable.
      var isSimplePath = expression.Path.All(
        l => l is SqlConstantExpression ||
        l is MySQLJsonArrayIndexExpression e && e.Expression is SqlConstantExpression);

      if (expression.ReturnsText)
        Sql.Append("JSON_UNQUOTE(");

      if (expression.Path.Count > 0)
        Sql.Append("JSON_EXTRACT(");

      Visit(expression.Expression);

      if (expression.Path.Count > 0)
      {
        Sql.Append(", ");

        if (!isSimplePath)
          Sql.Append("CONCAT(");

        Sql.Append("'$");

        foreach (var location in expression.Path)
        {
          if (location is MySQLJsonArrayIndexExpression arrayIndexExpression)
          {
            var isConstantExpression = arrayIndexExpression.Expression is SqlConstantExpression;

            Sql.Append("[");

            if (!isConstantExpression)
              Sql.Append("', ");

            Visit(arrayIndexExpression.Expression);

            if (!isConstantExpression)
              Sql.Append(", '");

            Sql.Append("]");
          }
          else
          {
            Sql.Append(".");
            Visit(location);
          }
        }

        Sql.Append("'");

        if (!isSimplePath)
          Sql.Append(")");

        Sql.Append(")");
      }

      if (expression.ReturnsText)
        Sql.Append(")");

      return expression;
    }

    protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunctionExpression)
    {
      if (sqlFunctionExpression.Name.StartsWith("@@", StringComparison.Ordinal))
      {
        Sql.Append(sqlFunctionExpression.Name);

        return sqlFunctionExpression;
      }

      return base.VisitSqlFunction(sqlFunctionExpression);
    }

    public virtual Expression VisitMySqlMatch(MySQLMatchExpression mySqlMatchExpression)
    {
      Check.NotNull(mySqlMatchExpression, nameof(mySqlMatchExpression));

      Sql.Append("MATCH ");
      Sql.Append("(");
      Visit(mySqlMatchExpression.Match);
      Sql.Append(")");
      Sql.Append(" AGAINST ");
      Sql.Append("(");
      Visit(mySqlMatchExpression.Against);

      switch (mySqlMatchExpression.SearchMode)
      {
        case MySQLMatchSearchMode.NaturalLanguage:
          break;
        case MySQLMatchSearchMode.NaturalLanguageWithQueryExpansion:
          Sql.Append(" WITH QUERY EXPANSION");
          break;
        case MySQLMatchSearchMode.Boolean:
          Sql.Append(" IN BOOLEAN MODE");
          break;
      }

      Sql.Append(")");

      return mySqlMatchExpression;
    }

    protected override Expression VisitSqlBinary(SqlBinaryExpression sqlBinaryExpression)
    {
      Check.NotNull(sqlBinaryExpression, nameof(sqlBinaryExpression));

      if (sqlBinaryExpression.OperatorType == ExpressionType.Add &&
        sqlBinaryExpression.Type == typeof(string) &&
        sqlBinaryExpression.Left.TypeMapping?.ClrType == typeof(string) &&
        sqlBinaryExpression.Right.TypeMapping?.ClrType == typeof(string))
      {
        Sql.Append("CONCAT(");
        Visit(sqlBinaryExpression.Left);
        Sql.Append(", ");
        Visit(sqlBinaryExpression.Right);
        Sql.Append(")");

        return sqlBinaryExpression;
      }

      var requiresBrackets = RequiresBrackets(sqlBinaryExpression.Left);

      if (requiresBrackets)
      {
        Sql.Append("(");
      }

      Visit(sqlBinaryExpression.Left);

      if (requiresBrackets)
      {
        Sql.Append(")");
      }

      Sql.Append(GetOperator(sqlBinaryExpression));

      requiresBrackets = RequiresBrackets(sqlBinaryExpression.Right) ||
               !requiresBrackets &&
               sqlBinaryExpression.Right is SqlUnaryExpression sqlUnaryExpression &&
               (sqlUnaryExpression.OperatorType == ExpressionType.Equal || sqlUnaryExpression.OperatorType == ExpressionType.NotEqual);

      if (requiresBrackets)
      {
        Sql.Append("(");
      }

      Visit(sqlBinaryExpression.Right);

      if (requiresBrackets)
      {
        Sql.Append(")");
      }

      return sqlBinaryExpression;
    }

    private SqlUnaryExpression VisitConvert(SqlUnaryExpression sqlUnaryExpression)
    {
      var castMapping = GetCastStoreType(sqlUnaryExpression.TypeMapping!);

      if (castMapping == "binary")
      {
        Sql.Append("UNHEX(HEX(");
        Visit(sqlUnaryExpression.Operand);
        Sql.Append("))");
        return sqlUnaryExpression;
      }

      var sameInnerCastStoreType = sqlUnaryExpression.Operand is SqlUnaryExpression operandUnary &&
                     operandUnary.OperatorType == ExpressionType.Convert &&
                     castMapping.Equals(GetCastStoreType(operandUnary.TypeMapping!), StringComparison.OrdinalIgnoreCase);

      Visit(sqlUnaryExpression.Operand);

      return sqlUnaryExpression;
    }

    public virtual Expression VisitMySqlRegexp(MySQLRegexpExpression mySqlRegexpExpression)
    {
      Check.NotNull(mySqlRegexpExpression, nameof(mySqlRegexpExpression));

      Visit(mySqlRegexpExpression.Match);
      Sql.Append(" REGEXP ");
      Visit(mySqlRegexpExpression.Pattern);

      return mySqlRegexpExpression;
    }

    private static bool RequiresBrackets(SqlExpression expression)
      => expression is SqlBinaryExpression ||
      expression is LikeExpression;

    protected override Expression VisitSqlUnary(SqlUnaryExpression sqlUnaryExpression)
      => sqlUnaryExpression.OperatorType == ExpressionType.Convert
      ? VisitConvert(sqlUnaryExpression)
      : base.VisitSqlUnary(sqlUnaryExpression);

    private string GetCastStoreType(RelationalTypeMapping typeMapping)
    {
      var storeTypeLower = typeMapping.StoreType.ToLower();
      string? castMapping = null;
      foreach (var kvp in _castMappings)
      {
        foreach (var storeType in kvp.Value)
        {
          if (storeTypeLower.StartsWith(storeType))
          {
            castMapping = kvp.Key;
            break;
          }
        }

        if (castMapping != null)
        {
          break;
        }
      }

      if (castMapping == null)
        throw new InvalidOperationException($"Invalid cast '{typeMapping.StoreType}'");

      if (castMapping == "signed" && storeTypeLower.Contains("unsigned"))
      {
        castMapping = "unsigned";
      }

      return castMapping;
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

    protected override Expression VisitCrossApply(CrossApplyExpression crossApplyExpression)
    {
      Sql.Append("JOIN ");

      if (crossApplyExpression.Table is not TableExpression)
        Sql.Append("LATERAL ");

      Visit(crossApplyExpression.Table);
      Sql.Append(" ON TRUE");

      return crossApplyExpression;
    }

    protected override Expression VisitOuterApply(OuterApplyExpression outerApplyExpression)
    {
      Sql.Append("LEFT JOIN ");

      if (outerApplyExpression.Table is not TableExpression)
        Sql.Append("LATERAL ");

      Visit(outerApplyExpression.Table);
      Sql.Append(" ON TRUE");

      return outerApplyExpression;
    }
  }
}
