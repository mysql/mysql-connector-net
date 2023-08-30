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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using MySql.EntityFrameworkCore.Query.Expressions.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MySql.EntityFrameworkCore.Query.Internal
{
  internal class MySQLStringMethodTranslator : IMethodCallTranslator
  {
    private static readonly MethodInfo _indexOfMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.IndexOf), new[] { typeof(string) })!;
    private static readonly MethodInfo _replaceMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.Replace), new[] { typeof(string), typeof(string) })!;
    private static readonly MethodInfo _toLowerMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.ToLower), Array.Empty<Type>())!;
    private static readonly MethodInfo _toUpperMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.ToUpper), Array.Empty<Type>())!;
    private static readonly MethodInfo _substringMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.Substring), new[] { typeof(int), typeof(int) })!;
    private static readonly MethodInfo _isNullOrWhiteSpaceMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.IsNullOrWhiteSpace), new[] { typeof(string) })!;
    private static readonly MethodInfo _trimStartMethodInfoWithoutArgs
      = typeof(string).GetRuntimeMethod(nameof(string.TrimStart), Array.Empty<Type>())!;
    private static readonly MethodInfo _trimStartMethodInfoWithCharArg
      = typeof(string).GetRuntimeMethod(nameof(string.TrimStart), new[] { typeof(char) })!;
    private static readonly MethodInfo _trimEndMethodInfoWithoutArgs
      = typeof(string).GetRuntimeMethod(nameof(string.TrimEnd), Array.Empty<Type>())!;
    private static readonly MethodInfo _trimEndMethodInfoWithCharArg
      = typeof(string).GetRuntimeMethod(nameof(string.TrimEnd), new[] { typeof(char) })!;
    private static readonly MethodInfo _trimMethodInfoWithoutArgs
      = typeof(string).GetRuntimeMethod(nameof(string.Trim), Array.Empty<Type>())!;
    private static readonly MethodInfo _trimMethodInfoWithCharArg
      = typeof(string).GetRuntimeMethod(nameof(string.Trim), new[] { typeof(char) })!;
    private static readonly MethodInfo _trimStartMethodInfoWithCharArrayArg
      = typeof(string).GetRuntimeMethod(nameof(string.TrimStart), new[] { typeof(char[]) })!;
    private static readonly MethodInfo _trimEndMethodInfoWithCharArrayArg
      = typeof(string).GetRuntimeMethod(nameof(string.TrimEnd), new[] { typeof(char[]) })!;
    private static readonly MethodInfo _trimMethodInfoWithCharArrayArg
      = typeof(string).GetRuntimeMethod(nameof(string.Trim), new[] { typeof(char[]) })!;
    private static readonly MethodInfo _startsWithMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
    private static readonly MethodInfo _containsMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.Contains), new[] { typeof(string) })!;
    private static readonly MethodInfo _endsWithMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.EndsWith), new[] { typeof(string) })!;
    private static readonly MethodInfo _padLeftWithOneArg
      = typeof(string).GetRuntimeMethod(nameof(string.PadLeft), new[] { typeof(int) })!;
    private static readonly MethodInfo _padRightWithOneArg
      = typeof(string).GetRuntimeMethod(nameof(string.PadRight), new[] { typeof(int) })!;
    private static readonly MethodInfo _padLeftWithTwoArgs
      = typeof(string).GetRuntimeMethod(nameof(string.PadLeft), new[] { typeof(int), typeof(char) })!;
    private static readonly MethodInfo _padRightWithTwoArgs
      = typeof(string).GetRuntimeMethod(nameof(string.PadRight), new[] { typeof(int), typeof(char) })!;

    private readonly MySQLSqlExpressionFactory _sqlExpressionFactory;

    public MySQLStringMethodTranslator(ISqlExpressionFactory sqlExpressionFactory)
    {
      _sqlExpressionFactory = (MySQLSqlExpressionFactory)sqlExpressionFactory;
    }

    public virtual SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
      if (_indexOfMethodInfo.Equals(method))
      {
        return new MySQLStringComparisonMethodTranslator(_sqlExpressionFactory)
          .MakeIndexOfExpression(instance, arguments[0]);
      }

      if (_replaceMethodInfo.Equals(method))
      {
        var stringTypeMapping = ExpressionExtensions.InferTypeMapping(instance!, arguments[0], arguments[1]);

        return _sqlExpressionFactory.NullableFunction(
          "REPLACE",
          new[]
          {
        _sqlExpressionFactory.ApplyTypeMapping(instance!, stringTypeMapping),
        _sqlExpressionFactory.ApplyTypeMapping(arguments[0], stringTypeMapping),
        _sqlExpressionFactory.ApplyTypeMapping(arguments[1], stringTypeMapping)
          },
          method.ReturnType,
          stringTypeMapping);
      }

      if (_toLowerMethodInfo.Equals(method)
        || _toUpperMethodInfo.Equals(method))
      {
        return _sqlExpressionFactory.NullableFunction(
          _toLowerMethodInfo.Equals(method) ? "LOWER" : "UPPER",
          new[] { instance! },
          method.ReturnType,
          instance!.TypeMapping);
      }

      if (_substringMethodInfo.Equals(method))
      {
        return _sqlExpressionFactory.NullableFunction(
          "SUBSTRING",
          new[]
          {
        instance!,
        _sqlExpressionFactory.Add(
          arguments[0],
          _sqlExpressionFactory.Constant(1)),
        arguments[1]
          },
          method.ReturnType,
          instance!.TypeMapping);
      }

      if (_isNullOrWhiteSpaceMethodInfo.Equals(method))
      {
        return _sqlExpressionFactory.OrElse(
        _sqlExpressionFactory.IsNull(arguments[0]),
        _sqlExpressionFactory.Equal(
          ProcessTrimMethod(arguments[0], null, null),
          _sqlExpressionFactory.Constant(string.Empty)));
      }

      if (_trimStartMethodInfoWithoutArgs?.Equals(method) == true
        || _trimStartMethodInfoWithCharArg?.Equals(method) == true
        || _trimStartMethodInfoWithCharArrayArg.Equals(method))
      {
        return ProcessTrimMethod(instance!, arguments.Count > 0 ? arguments[0] : null, "LEADING");
      }

      if (_trimEndMethodInfoWithoutArgs?.Equals(method) == true
        || _trimEndMethodInfoWithCharArg?.Equals(method) == true
        || _trimEndMethodInfoWithCharArrayArg.Equals(method))
      {
        return ProcessTrimMethod(instance!, arguments.Count > 0 ? arguments[0] : null, "TRAILING");
      }

      if (_trimMethodInfoWithoutArgs?.Equals(method) == true
        || _trimMethodInfoWithCharArg?.Equals(method) == true
        || _trimMethodInfoWithCharArrayArg.Equals(method))
      {
        return ProcessTrimMethod(instance!, arguments.Count > 0 ? arguments[0] : null, null);
      }

      if (_containsMethodInfo.Equals(method))
      {
        return new MySQLStringComparisonMethodTranslator(_sqlExpressionFactory)
          .MakeContainsExpression(instance!, arguments[0]);
      }

      if (_startsWithMethodInfo.Equals(method))
      {
        return new MySQLStringComparisonMethodTranslator(_sqlExpressionFactory)
          .MakeStartsWithExpression(instance!, arguments[0], _sqlExpressionFactory.Constant(StringComparison.CurrentCulture));
      }

      if (_endsWithMethodInfo.Equals(method))
      {
        return new MySQLStringComparisonMethodTranslator(_sqlExpressionFactory)
          .MakeEndsWithExpression(instance!, arguments[0], _sqlExpressionFactory.Constant(StringComparison.CurrentCulture));
      }

      if (_padLeftWithOneArg.Equals(method))
      {
        return TranslatePadLeftRight(
          true,
          instance!,
          arguments[0],
          _sqlExpressionFactory.Constant(" "),
          method.ReturnType);
      }

      if (_padRightWithOneArg.Equals(method))
      {
        return TranslatePadLeftRight(
          false,
          instance!,
          arguments[0],
          _sqlExpressionFactory.Constant(" "),
          method.ReturnType);
      }

      if (_padLeftWithTwoArgs.Equals(method))
      {
        return TranslatePadLeftRight(
          true,
          instance!,
          arguments[0],
          arguments[1],
          method.ReturnType);
      }

      if (_padRightWithTwoArgs.Equals(method))
      {
        return TranslatePadLeftRight(
          false,
          instance!,
          arguments[0],
          arguments[1],
          method.ReturnType);
      }

      return null;
    }

    private SqlExpression? TranslatePadLeftRight(bool leftPad, SqlExpression instance, SqlExpression length, SqlExpression padString, Type returnType)
      => length is SqlConstantExpression && padString is SqlConstantExpression
        ? _sqlExpressionFactory.NullableFunction(
            leftPad ? "LPAD" : "RPAD",
            new[] {
            instance,
            length,
            padString
            },
            returnType,
            false)
          : null;

    private SqlExpression ProcessTrimMethod(SqlExpression instance, SqlExpression? trimChar, string? locationSpecifier)
    {
      // Builds a TRIM({BOTH | LEADING | TRAILING} remstr FROM str) expression.

      var sqlArguments = new List<SqlExpression>();

      if (locationSpecifier != null)
      {
        sqlArguments.Add(_sqlExpressionFactory.Fragment(locationSpecifier));
      }

      if (trimChar != null)
      {
        var constantValue = (trimChar as SqlConstantExpression)?.Value;

        if (constantValue is char singleChar)
        {
          sqlArguments.Add(_sqlExpressionFactory.Constant(singleChar));
        }
        else if (constantValue is char[] charArray && charArray.Length <= 1)
        {
          if (charArray.Length == 1)
          {
            sqlArguments.Add(_sqlExpressionFactory.Constant(charArray[0]));
          }
        }
      }

      if (sqlArguments.Count > 0)
      {
        sqlArguments.Add(_sqlExpressionFactory.Fragment("FROM"));
      }

      sqlArguments.Add(instance);

      return _sqlExpressionFactory.NullableFunction(
        "TRIM",
        new[] { _sqlExpressionFactory.ComplexFunctionArgument(
        sqlArguments.ToArray(),
        " ",
        typeof(string)),
        },
        typeof(string));
    }
  }
}