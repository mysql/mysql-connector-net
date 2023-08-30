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
using MySql.EntityFrameworkCore.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using static MySql.EntityFrameworkCore.Utils.Statics;

namespace MySql.EntityFrameworkCore.Query.Internal
{
  internal class MySQLDateDiffFunctionsTranslator : IMethodCallTranslator
  {
    private readonly Dictionary<MethodInfo, string> _methodInfoDateDiffMapping
      = new()
      {
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffYear),
        new[] { typeof(DbFunctions), typeof(DateTime), typeof(DateTime) })!,
      "YEAR"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffYear),
        new[] { typeof(DbFunctions), typeof(DateTime?), typeof(DateTime?) })!,
      "YEAR"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffYear),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) })!,
      "YEAR"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffYear),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset?), typeof(DateTimeOffset?) })!,
      "YEAR"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffYear),
        new[] { typeof(DbFunctions), typeof(DateOnly), typeof(DateOnly) })!,
      "YEAR"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffYear),
        new[] { typeof(DbFunctions), typeof(DateOnly?), typeof(DateOnly?) })!,
      "YEAR"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMonth),
        new[] { typeof(DbFunctions), typeof(DateTime), typeof(DateTime) })!,
      "MONTH"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMonth),
        new[] { typeof(DbFunctions), typeof(DateTime?), typeof(DateTime?) })!,
      "MONTH"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMonth),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) })!,
      "MONTH"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMonth),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset?), typeof(DateTimeOffset?) })!,
      "MONTH"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMonth),
        new[] { typeof(DbFunctions), typeof(DateOnly), typeof(DateOnly) })!,
      "MONTH"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMonth),
        new[] { typeof(DbFunctions), typeof(DateOnly?), typeof(DateOnly?) })!,
      "MONTH"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffDay),
        new[] { typeof(DbFunctions), typeof(DateTime), typeof(DateTime) })!,
      "DAY"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffDay),
        new[] { typeof(DbFunctions), typeof(DateTime?), typeof(DateTime?) })!,
      "DAY"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffDay),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) })!,
      "DAY"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffDay),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset?), typeof(DateTimeOffset?) })!,
      "DAY"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffDay),
        new[] { typeof(DbFunctions), typeof(DateOnly), typeof(DateOnly) })!,
      "DAY"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffDay),
        new[] { typeof(DbFunctions), typeof(DateOnly?), typeof(DateOnly?) })!,
      "DAY"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffHour),
        new[] { typeof(DbFunctions), typeof(DateTime), typeof(DateTime) })!,
      "HOUR"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffHour),
        new[] { typeof(DbFunctions), typeof(DateTime?), typeof(DateTime?) })!,
      "HOUR"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffHour),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) })!,
      "HOUR"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffHour),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset?), typeof(DateTimeOffset?) })!,
      "HOUR"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMinute),
        new[] { typeof(DbFunctions), typeof(DateTime), typeof(DateTime) })!,
      "MINUTE"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMinute),
        new[] { typeof(DbFunctions), typeof(DateTime?), typeof(DateTime?) })!,
      "MINUTE"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMinute),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) })!,
      "MINUTE"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMinute),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset?), typeof(DateTimeOffset?) })!,
      "MINUTE"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffSecond),
        new[] { typeof(DbFunctions), typeof(DateTime), typeof(DateTime) })!,
      "SECOND"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffSecond),
        new[] { typeof(DbFunctions), typeof(DateTime?), typeof(DateTime?) })!,
      "SECOND"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffSecond),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) })!,
      "SECOND"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffSecond),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset?), typeof(DateTimeOffset?) })!,
      "SECOND"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMicrosecond),
        new[] { typeof(DbFunctions), typeof(DateTime), typeof(DateTime) })!,
      "MICROSECOND"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMicrosecond),
        new[] { typeof(DbFunctions), typeof(DateTime?), typeof(DateTime?) })!,
      "MICROSECOND"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMicrosecond),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) })!,
      "MICROSECOND"
      },
      {
      typeof(MySQLDbFunctionsExtensions).GetRuntimeMethod(
        nameof(MySQLDbFunctionsExtensions.DateDiffMicrosecond),
        new[] { typeof(DbFunctions), typeof(DateTimeOffset?), typeof(DateTimeOffset?) })!,
      "MICROSECOND"
      }
      };

    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    public MySQLDateDiffFunctionsTranslator(ISqlExpressionFactory sqlExpressionFactory)
    {
      _sqlExpressionFactory = sqlExpressionFactory;
    }

    public virtual SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
      if (_methodInfoDateDiffMapping.TryGetValue(method, out var datePart))
      {
        var startDate = arguments[1];
        var endDate = arguments[2];
        var typeMapping = ExpressionExtensions.InferTypeMapping(startDate, endDate);

        startDate = _sqlExpressionFactory.ApplyTypeMapping(startDate, typeMapping);
        endDate = _sqlExpressionFactory.ApplyTypeMapping(endDate, typeMapping);

        return _sqlExpressionFactory.Function(
          "TIMESTAMPDIFF",
          new[]
          {
        _sqlExpressionFactory.Fragment(datePart),
        startDate,
        endDate
          },
          nullable: true,
          argumentsPropagateNullability: TrueArrays[3],
          typeof(int));
      }

      return null;
    }
  }
}