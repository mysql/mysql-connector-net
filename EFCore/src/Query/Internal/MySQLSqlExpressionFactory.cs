// Copyright (c) 2020 Oracle and/or its affiliates.
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
using MySql.EntityFrameworkCore.Query.Expressions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySql.EntityFrameworkCore.Query.Internal
{
  internal class MySQLSqlExpressionFactory : SqlExpressionFactory
  {
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly RelationalTypeMapping _boolTypeMapping;

    public MySQLSqlExpressionFactory(SqlExpressionFactoryDependencies dependencies)
        : base(dependencies)
    {
      _typeMappingSource = dependencies.TypeMappingSource;
      _boolTypeMapping = _typeMappingSource.FindMapping(typeof(bool));
    }

    public MySQLComplexFunctionArgumentExpression ComplexFunctionArgument(
        IEnumerable<SqlExpression> argumentParts,
        Type argumentType,
        RelationalTypeMapping typeMapping = null)
    {
      var typeMappedArgumentParts = new List<SqlExpression>();

      foreach (var argument in argumentParts)
      {
        typeMappedArgumentParts.Add(ApplyDefaultTypeMapping(argument));
      }

      return new MySQLComplexFunctionArgumentExpression(
          typeMappedArgumentParts,
          argumentType,
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
    RelationalTypeMapping typeMapping = null)
    => MakeBinary(
        MySQLBinaryExpressionOperatorType.IntegerDivision,
        left,
        right,
        typeMapping);

    public virtual MySQLBinaryExpression MakeBinary(
    MySQLBinaryExpressionOperatorType operatorType,
    SqlExpression left,
    SqlExpression right,
    RelationalTypeMapping typeMapping)
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

    public override SqlExpression ApplyTypeMapping(SqlExpression sqlExpression, RelationalTypeMapping typeMapping)
    {
      if (sqlExpression == null
          || sqlExpression.TypeMapping != null)
      {
        return sqlExpression;
      }

      switch (sqlExpression)
      {
        case MySQLCollateExpression e:
          return ApplyTypeMappingOnCollate(e);
        case MySQLComplexFunctionArgumentExpression e:
          return ApplyTypeMappingOnComplexFunctionArgument(e);
        case MySQLBinaryExpression e:
          return ApplyTypeMappingOnMySqlBinary(e, typeMapping);
        default:
          return base.ApplyTypeMapping(sqlExpression, typeMapping);
      }
    }

    private MySQLComplexFunctionArgumentExpression ApplyTypeMappingOnComplexFunctionArgument(MySQLComplexFunctionArgumentExpression complexFunctionArgumentExpression)
    {
      var inferredTypeMapping = ExpressionExtensions.InferTypeMapping(complexFunctionArgumentExpression.ArgumentParts.ToArray())
                                ?? _typeMappingSource.FindMapping(complexFunctionArgumentExpression.Type);

      return new MySQLComplexFunctionArgumentExpression(
          complexFunctionArgumentExpression.ArgumentParts,
          complexFunctionArgumentExpression.Type,
          inferredTypeMapping ?? complexFunctionArgumentExpression.TypeMapping);
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

    private SqlExpression ApplyTypeMappingOnMySqlBinary(
    MySQLBinaryExpression sqlBinaryExpression,
    RelationalTypeMapping typeMapping)
    {
      var left = sqlBinaryExpression.Left;
      var right = sqlBinaryExpression.Right;

      Type resultType;
      RelationalTypeMapping resultTypeMapping;
      RelationalTypeMapping inferredTypeMapping;

      switch (sqlBinaryExpression.OperatorType)
      {
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
  }
}
