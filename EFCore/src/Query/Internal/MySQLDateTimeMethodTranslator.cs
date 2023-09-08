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
using System;
using System.Collections.Generic;
using System.Reflection;
using static MySql.EntityFrameworkCore.Utils.Statics;

namespace MySql.EntityFrameworkCore.Query.Internal
{
  internal class MySQLDateTimeMethodTranslator : IMethodCallTranslator
  {
    private readonly Dictionary<MethodInfo, string> _methodInfoDatePartMapping = new Dictionary<MethodInfo, string>
    {
      { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddYears), new[] { typeof(int) })!, "year" },
      { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddMonths), new[] { typeof(int) })!, "month" },
      { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddDays), new[] { typeof(double) })!, "day" },
      { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddHours), new[] { typeof(double) })!, "hour" },
      { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddMinutes), new[] { typeof(double) })!, "minute" },
      { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddSeconds), new[] { typeof(double) })!, "second" },
      { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddMilliseconds), new[] { typeof(double) })!, "microsecond" },
      { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddYears), new[] { typeof(int) })!, "year" },
      { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddMonths), new[] { typeof(int) })!, "month" },
      { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddDays), new[] { typeof(double) })!, "day" },
      { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddHours), new[] { typeof(double) })!, "hour" },
      { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddMinutes), new[] { typeof(double) })!, "minute" },
      { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddSeconds), new[] { typeof(double) })!, "second" },
      { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddMilliseconds), new[] { typeof(double) })!, "microsecond" },
      { typeof(DateOnly).GetRuntimeMethod(nameof(DateOnly.AddYears), new[] { typeof(int) })!, "year" },
      { typeof(DateOnly).GetRuntimeMethod(nameof(DateOnly.AddMonths), new[] { typeof(int) })!, "month" },
      { typeof(DateOnly).GetRuntimeMethod(nameof(DateOnly.AddDays), new[] { typeof(int) })!, "day" },
      { typeof(TimeOnly).GetRuntimeMethod(nameof(TimeOnly.AddHours), new[] { typeof(double) })!, "hour" },
      { typeof(TimeOnly).GetRuntimeMethod(nameof(TimeOnly.AddMinutes), new[] { typeof(double) })!, "minute" }
    };

    private static readonly MethodInfo _timeOnlyAddTimeSpanMethod = typeof(TimeOnly).GetRuntimeMethod(nameof(TimeOnly.Add), new[] { typeof(TimeSpan) })!;
    private static readonly MethodInfo _timeOnlyIsBetweenMethod = typeof(TimeOnly).GetRuntimeMethod(nameof(TimeOnly.IsBetween), new[] { typeof(TimeOnly), typeof(TimeOnly) })!;

    private static readonly MethodInfo _dateOnlyFromDateTimeMethod = typeof(DateOnly).GetRuntimeMethod(nameof(DateOnly.FromDateTime), new[] { typeof(DateTime) })!;
    private static readonly MethodInfo _dateOnlyToDateTimeMethod = typeof(DateOnly).GetRuntimeMethod(nameof(DateOnly.ToDateTime), new[] { typeof(TimeOnly) })!;

    private MySQLSqlExpressionFactory _sqlExpressionFactory;

    public MySQLDateTimeMethodTranslator(ISqlExpressionFactory sqlExpressionFactory)
    {
      _sqlExpressionFactory = (MySQLSqlExpressionFactory)sqlExpressionFactory;
    }

    public virtual SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
      if (_methodInfoDatePartMapping.TryGetValue(method, out var datePart))
      {
        return !datePart.Equals("year")
          && !datePart.Equals("month")
          && arguments[0] is SqlConstantExpression sqlConstant
          && ((double)sqlConstant.Value! >= int.MaxValue
          || (double)sqlConstant.Value <= int.MinValue)
          ? null
          : _sqlExpressionFactory.Function(
            "DATE_ADD",
            new[]
            {
        instance!,
        _sqlExpressionFactory.ComplexFunctionArgument(new SqlExpression[]
        {
          _sqlExpressionFactory.Fragment("INTERVAL"),
          _sqlExpressionFactory.Convert(arguments[0], typeof(int)),
          _sqlExpressionFactory.Fragment(datePart)
        },
        " ",
        typeof(string))
            },
            nullable: true,
            argumentsPropagateNullability: TrueArrays[1],
            instance!.Type,
            instance.TypeMapping);
      }

      if (method.DeclaringType == typeof(TimeOnly))
      {
        if (method == _timeOnlyAddTimeSpanMethod)
        {
          return _sqlExpressionFactory.Add(instance!, arguments[0]);
        }

        if (method == _timeOnlyIsBetweenMethod)
        {
          return _sqlExpressionFactory.And(
              _sqlExpressionFactory.GreaterThanOrEqual(instance!, arguments[0]),
              _sqlExpressionFactory.LessThan(instance!, arguments[1]));
        }
      }

      if (method.DeclaringType == typeof(DateOnly))
      {
        if (method == _dateOnlyFromDateTimeMethod)
        {
          return _sqlExpressionFactory.NullableFunction(
            "DATE",
            new[] { arguments[0] },
            method.ReturnType);
        }

        if (method == _dateOnlyToDateTimeMethod)
        {
          var convertExpression = _sqlExpressionFactory.Convert(
            instance!,
            method.ReturnType);

          if (arguments[0] is SqlConstantExpression sqlConstantExpression &&
            sqlConstantExpression.Value is TimeOnly timeOnly &&
            timeOnly == default)
          {
            return convertExpression;
          }

          return _sqlExpressionFactory.NullableFunction(
            "ADDTIME",
            new[]
            {
              convertExpression,
              arguments[0]
            },
            method.ReturnType);
        }
      }

      return null;
    }
  }
}