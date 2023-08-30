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
  internal class MySQLDateTimeMemberTranslator : IMemberTranslator
  {
    private static readonly Dictionary<string, (string Part, int Divisor)> _datePartMapping
      = new Dictionary<string, (string, int)>
      {
        { nameof(DateTime.Year), ("year", 1) },
        { nameof(DateTime.Month), ("month", 1) },
        { nameof(DateTime.Day), ("day", 1) },
        { nameof(DateTime.Hour), ("hour", 1) },
        { nameof(DateTime.Minute), ("minute", 1) },
        { nameof(DateTime.Second), ("second", 1) },
        { nameof(DateTime.Millisecond), ("microsecond", 1000) },
      };

    private readonly MySQLSqlExpressionFactory _sqlExpressionFactory;

    public MySQLDateTimeMemberTranslator(ISqlExpressionFactory sqlExpressionFactory)
    {
      _sqlExpressionFactory = (MySQLSqlExpressionFactory)sqlExpressionFactory;
    }

    public virtual SqlExpression? Translate(
      SqlExpression? instance,
      MemberInfo member,
      Type returnType,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
      var declaringType = member.DeclaringType;

      if (declaringType == typeof(DateTime)
          || declaringType == typeof(DateTimeOffset)
          || declaringType == typeof(DateOnly)
          || declaringType == typeof(TimeOnly))
      {
        var memberName = member.Name;

        if (_datePartMapping.TryGetValue(memberName, out var datePart))
        {
          var extract = _sqlExpressionFactory.Function(
            "EXTRACT",
            new[]
            {
        _sqlExpressionFactory.ComplexFunctionArgument(
          new [] {
          _sqlExpressionFactory.Fragment($"{datePart.Part} FROM"),
          instance!
          },
          " ",
          typeof(string))
            },
            nullable: true,
            argumentsPropagateNullability: TrueArrays[1],
            returnType);

          if (datePart.Divisor != 1)
          {
            return _sqlExpressionFactory.MySqlIntegerDivide(
              extract,
              _sqlExpressionFactory.Constant(datePart.Divisor));
          }

          return extract;
        }

        switch (memberName)
        {
          case nameof(DateTime.DayOfYear):
            return _sqlExpressionFactory.NullableFunction(
              "DAYOFYEAR",
              new[] { instance! },
              returnType,
              false);

          case nameof(DateTime.Date):
            return _sqlExpressionFactory.NullableFunction(
              "CONVERT",
              new[]{
                instance!,
                _sqlExpressionFactory.Fragment("date")
              },
              returnType,
              false);

          case nameof(DateTime.TimeOfDay):
            return _sqlExpressionFactory.Convert(instance!, returnType);

          case nameof(DateTime.Now):
            return _sqlExpressionFactory.NonNullableFunction(
              declaringType == typeof(DateTimeOffset)
              ? "UTC_TIMESTAMP"
              : "CURRENT_TIMESTAMP",
              Array.Empty<SqlExpression>(),
              returnType);

          case nameof(DateTime.UtcNow):
            return _sqlExpressionFactory.NonNullableFunction(
              "UTC_TIMESTAMP",
              Array.Empty<SqlExpression>(),
              returnType);

          case nameof(DateTime.Today):
            return _sqlExpressionFactory.NonNullableFunction(
              declaringType == typeof(DateTimeOffset)
              ? "UTC_DATE"
              : "CURDATE",
              Array.Empty<SqlExpression>(),
              returnType);

          case nameof(DateTime.DayOfWeek):
            return _sqlExpressionFactory.Subtract(
              _sqlExpressionFactory.NullableFunction(
                "DAYOFWEEK",
                new[] { instance! },
                returnType,
                false),
              _sqlExpressionFactory.Constant(1));
        }
      }

      return null;
    }
  }
}
