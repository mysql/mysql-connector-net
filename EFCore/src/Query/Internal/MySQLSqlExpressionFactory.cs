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
using MySql.EntityFrameworkCore.Storage.Internal;
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySql.EntityFrameworkCore.Query.Internal
{
  internal class MySQLSqlExpressionFactory : SqlExpressionFactory
  {
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly RelationalTypeMapping? _boolTypeMapping;

    public MySQLSqlExpressionFactory(SqlExpressionFactoryDependencies dependencies)
      : base(dependencies)
    {
      _typeMappingSource = dependencies.TypeMappingSource;
      _boolTypeMapping = _typeMappingSource.FindMapping(typeof(bool));
    }

    public virtual SqlFunctionExpression NullableFunction(
      string name,
      IEnumerable<SqlExpression> arguments,
      Type returnType,
      bool onlyNullWhenAnyNullPropagatingArgumentIsNull)
      => NullableFunction(name, arguments, returnType, null, onlyNullWhenAnyNullPropagatingArgumentIsNull);

    public virtual SqlFunctionExpression NullableFunction(
      string name,
      IEnumerable<SqlExpression> arguments,
      Type returnType,
      RelationalTypeMapping? typeMapping = null,
      bool onlyNullWhenAnyNullPropagatingArgumentIsNull = true,
      IEnumerable<bool>? argumentsPropagateNullability = null)
    {
      Check.NotEmpty(name, nameof(name));
      Check.NotNull(arguments, nameof(arguments));
      Check.NotNull(returnType, nameof(returnType));

      var typeMappedArguments = new List<SqlExpression>();

      foreach (var argument in arguments)
      {
        typeMappedArguments.Add(ApplyDefaultTypeMapping(argument));
      }

      return new SqlFunctionExpression(
        name,
        typeMappedArguments,
        true,
        onlyNullWhenAnyNullPropagatingArgumentIsNull
        ? (argumentsPropagateNullability ?? Statics.GetTrueValues(typeMappedArguments.Count))
        : Statics.GetFalseValues(typeMappedArguments.Count),
        returnType,
        typeMapping);
    }

    public virtual SqlFunctionExpression NonNullableFunction(
      string name,
      IEnumerable<SqlExpression> arguments,
      Type returnType,
      RelationalTypeMapping? typeMapping = null)
    {
      Check.NotEmpty(name, nameof(name));
      Check.NotNull(arguments, nameof(arguments));
      Check.NotNull(returnType, nameof(returnType));

      var typeMappedArguments = new List<SqlExpression>();

      foreach (var argument in arguments)
      {
        typeMappedArguments.Add(ApplyDefaultTypeMapping(argument));
      }

      return new SqlFunctionExpression(
        name,
        typeMappedArguments,
        false,
        Statics.GetFalseValues(typeMappedArguments.Count),
        returnType,
        typeMapping);
    }

    public MySQLComplexFunctionArgumentExpression ComplexFunctionArgument(
      IEnumerable<SqlExpression> argumentParts,
      string delimiter,
      Type argumentType,
      RelationalTypeMapping? typeMapping = null)
    {
      var typeMappedArgumentParts = new List<SqlExpression>();

      foreach (var argument in argumentParts)
      {
        typeMappedArgumentParts.Add(ApplyDefaultTypeMapping(argument));
      }

      return (MySQLComplexFunctionArgumentExpression)ApplyTypeMapping(
        new MySQLComplexFunctionArgumentExpression(
          typeMappedArgumentParts,
          delimiter,
          argumentType,
          typeMapping!),
        typeMapping);
    }

    public MySQLCollateExpression Collate(
      SqlExpression valueExpression,
      string charset,
      string collation)
      => (MySQLCollateExpression)ApplyDefaultTypeMapping(
        new MySQLCollateExpression(
          valueExpression,
          charset,
          collation,
          null));

    public virtual MySQLBinaryExpression MySqlIntegerDivide(
      SqlExpression left,
      SqlExpression right,
      RelationalTypeMapping? typeMapping = null)
      => MakeBinary(
        MySQLBinaryExpressionOperatorType.IntegerDivision,
        left,
        right,
        typeMapping);

    public virtual MySQLBinaryExpression NonOptimizedEqual(
      SqlExpression left,
      SqlExpression right,
      RelationalTypeMapping? typeMapping = null)
      => MakeBinary(
        MySQLBinaryExpressionOperatorType.NonOptimizedEqual,
        left,
        right,
        typeMapping);

    public virtual MySQLBinaryExpression MakeBinary(
      MySQLBinaryExpressionOperatorType operatorType,
      SqlExpression left,
      SqlExpression right,
      RelationalTypeMapping? typeMapping)
    {
      var returnType = left.Type;

      return (MySQLBinaryExpression)ApplyTypeMapping(
        new MySQLBinaryExpression(
          operatorType,
          left,
          right,
          returnType,
          null),
        typeMapping);
    }

    public virtual MySQLMatchExpression MakeMatch(
      SqlExpression match,
      SqlExpression against,
      MySQLMatchSearchMode searchMode)
    {
      return (MySQLMatchExpression)ApplyDefaultTypeMapping(
        new MySQLMatchExpression(
          match,
          against,
          searchMode,
          null));
    }

    public virtual MySQLRegexpExpression Regexp(
      SqlExpression match,
      SqlExpression pattern)
      => (MySQLRegexpExpression)ApplyDefaultTypeMapping(
        new MySQLRegexpExpression(
          match,
          pattern,
          null));

    public override SqlExpression ApplyTypeMapping(SqlExpression? sqlExpression, RelationalTypeMapping? typeMapping)
      => sqlExpression is not { TypeMapping: null }
      ? sqlExpression!
      : ApplyNewTypeMapping(sqlExpression, typeMapping!);

    private SqlExpression ApplyNewTypeMapping(SqlExpression sqlExpression, RelationalTypeMapping typeMapping)
      => sqlExpression switch
      {
        // Customize handling for binary expressions.
        SqlBinaryExpression e => ApplyTypeMappingOnSqlBinary(e, typeMapping),

        // MySQL specific expression types:
        MySQLComplexFunctionArgumentExpression e => ApplyTypeMappingOnComplexFunctionArgument(e),
        MySQLCollateExpression e => ApplyTypeMappingOnCollate(e),
        MySQLRegexpExpression e => ApplyTypeMappingOnRegexp(e),
        MySQLBinaryExpression e => ApplyTypeMappingOnMySqlBinary(e, typeMapping),
        MySQLMatchExpression e => ApplyTypeMappingOnMatch(e),
        MySQLJsonArrayIndexExpression e => e.ApplyTypeMapping(typeMapping),
        _ => base.ApplyTypeMapping(sqlExpression, typeMapping)
      };

    private SqlBinaryExpression ApplyTypeMappingOnSqlBinary(SqlBinaryExpression sqlBinaryExpression, RelationalTypeMapping typeMapping)
    {
      // The default SqlExpressionFactory behavior is to assume that the two operands have the same type, and so to infer one side's
      // mapping from the other if needed. Here we take care of some heterogeneous operand cases where this doesn't work.

      var left = sqlBinaryExpression.Left;
      var right = sqlBinaryExpression.Right;

      var newSqlBinaryExpression = (SqlBinaryExpression)base.ApplyTypeMapping(sqlBinaryExpression, typeMapping);

      // Handle the special case, that a JSON value is compared to a string (e.g. when used together with
      // JSON_EXTRACT()).
      // The string argument should not be interpreted as a JSON value, which it normally would due to inference
      // if its type mapping hasn't been explicitly set before, but just as a string.
      if (newSqlBinaryExpression.Left.TypeMapping is MySQLJsonTypeMapping newLeftTypeMapping &&
          newLeftTypeMapping.ClrType == typeof(string) &&
          right.TypeMapping is null &&
          right.Type == typeof(string))
      {
        newSqlBinaryExpression = new SqlBinaryExpression(
          sqlBinaryExpression.OperatorType,
          ApplyTypeMapping(left, newLeftTypeMapping),
          ApplyTypeMapping(right, _typeMappingSource.FindMapping(right.Type)),
          newSqlBinaryExpression.Type,
          newSqlBinaryExpression.TypeMapping);
      }
      else if (newSqlBinaryExpression.Right.TypeMapping is MySQLJsonTypeMapping newRightTypeMapping &&
        newRightTypeMapping.ClrType == typeof(string) &&
        left.TypeMapping is null &&
        left.Type == typeof(string))
      {
        newSqlBinaryExpression = new SqlBinaryExpression(
          sqlBinaryExpression.OperatorType,
          ApplyTypeMapping(left, _typeMappingSource.FindMapping(left.Type)),
          ApplyTypeMapping(right, newRightTypeMapping),
          newSqlBinaryExpression.Type,
          newSqlBinaryExpression.TypeMapping);
      }

      return newSqlBinaryExpression;
    }

    private MySQLComplexFunctionArgumentExpression ApplyTypeMappingOnComplexFunctionArgument(MySQLComplexFunctionArgumentExpression complexFunctionArgumentExpression)
    {
      var inferredTypeMapping = ExpressionExtensions.InferTypeMapping(complexFunctionArgumentExpression.ArgumentParts.ToArray())
        ?? _typeMappingSource.FindMapping(complexFunctionArgumentExpression.Type);

      return new MySQLComplexFunctionArgumentExpression(
        complexFunctionArgumentExpression.ArgumentParts,
        complexFunctionArgumentExpression.Delimiter,
        complexFunctionArgumentExpression.Type,
        inferredTypeMapping ?? complexFunctionArgumentExpression.TypeMapping!);
    }

    private MySQLCollateExpression ApplyTypeMappingOnCollate(MySQLCollateExpression collateExpression)
    {
      var inferredTypeMapping = ExpressionExtensions.InferTypeMapping(collateExpression.ValueExpression)
        ?? _typeMappingSource.FindMapping(collateExpression.ValueExpression.Type);

      return new MySQLCollateExpression(
        ApplyTypeMapping(collateExpression.ValueExpression, inferredTypeMapping),
        collateExpression.Charset,
        collateExpression.Collation,
        inferredTypeMapping ?? collateExpression.TypeMapping);
    }

    private SqlExpression ApplyTypeMappingOnRegexp(MySQLRegexpExpression regexpExpression)
    {
      var inferredTypeMapping = ExpressionExtensions.InferTypeMapping(regexpExpression.Match)
        ?? _typeMappingSource.FindMapping(regexpExpression.Match.Type);

      return new MySQLRegexpExpression(
        ApplyTypeMapping(regexpExpression.Match, inferredTypeMapping),
        ApplyTypeMapping(regexpExpression.Pattern, inferredTypeMapping),
        _boolTypeMapping!);
    }

    private SqlExpression ApplyTypeMappingOnMySqlBinary(
      MySQLBinaryExpression sqlBinaryExpression,
      RelationalTypeMapping? typeMapping)
    {
      var left = sqlBinaryExpression.Left;
      var right = sqlBinaryExpression.Right;

      Type resultType;
      RelationalTypeMapping? resultTypeMapping;
      RelationalTypeMapping? inferredTypeMapping;

      switch (sqlBinaryExpression.OperatorType)
      {
        case MySQLBinaryExpressionOperatorType.NonOptimizedEqual:
          inferredTypeMapping = ExpressionExtensions.InferTypeMapping(left, right)
            ?? (left.Type != typeof(object)
            ? _typeMappingSource.FindMapping(left.Type)
            : _typeMappingSource.FindMapping(right.Type));
          resultType = typeof(bool);
          resultTypeMapping = _boolTypeMapping;
          break;
        case MySQLBinaryExpressionOperatorType.IntegerDivision:
          {
            inferredTypeMapping = typeMapping ?? ExpressionExtensions.InferTypeMapping(left, right);
            resultType = left.Type;
            resultTypeMapping = inferredTypeMapping;
          }
          break;

        default:
          throw new InvalidOperationException("Incorrect OperatorType for MySQLBinaryExpression");
      }

      return new MySQLBinaryExpression(
        sqlBinaryExpression.OperatorType,
        ApplyTypeMapping(left, inferredTypeMapping),
        ApplyTypeMapping(right, inferredTypeMapping),
        resultType,
        resultTypeMapping);
    }

    private SqlExpression ApplyTypeMappingOnMatch(MySQLMatchExpression matchExpression)
    {
      var inferredTypeMapping = ExpressionExtensions.InferTypeMapping(matchExpression.Match) ??
        _typeMappingSource.FindMapping(matchExpression.Match.Type);

      return new MySQLMatchExpression(
        ApplyTypeMapping(matchExpression.Match, inferredTypeMapping),
        ApplyTypeMapping(matchExpression.Against, inferredTypeMapping),
        matchExpression.SearchMode,
        _boolTypeMapping!);
    }
  }
}
