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

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.EntityFrameworkCore.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MySql.EntityFrameworkCore.Query.ExpressionTranslators.Internal
{
  internal class MySQLDbFunctionsExtensionsMethodTranslator : IMethodCallTranslator
  {
    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    private static readonly Type[] _supportedTypes = {
            typeof(int),
            typeof(long),
            typeof(DateTime),
            typeof(Guid),
            typeof(bool),
            typeof(byte),
            typeof(byte[]),
            typeof(double),
            typeof(DateTimeOffset),
            typeof(char),
            typeof(short),
            typeof(float),
            typeof(decimal),
            typeof(TimeSpan),
            typeof(uint),
            typeof(ushort),
            typeof(ulong),
            typeof(sbyte),
            typeof(int?),
            typeof(long?),
            typeof(DateTime?),
            typeof(Guid?),
            typeof(bool?),
            typeof(byte?),
            typeof(double?),
            typeof(DateTimeOffset?),
            typeof(char?),
            typeof(short?),
            typeof(float?),
            typeof(decimal?),
            typeof(TimeSpan?),
            typeof(uint?),
            typeof(ushort?),
            typeof(ulong?),
            typeof(sbyte?),
        };

    private static readonly MethodInfo[] _methodInfos
        = typeof(MySQLDbFunctionsExtensions).GetRuntimeMethods()
            .Where(method => method.Name == nameof(MySQLDbFunctionsExtensions.Like)
                             && method.IsGenericMethod
                             && method.GetParameters().Length >= 3 && method.GetParameters().Length <= 4)
            .SelectMany(method => _supportedTypes.Select(type => method.MakeGenericMethod(type))).ToArray();

    public MySQLDbFunctionsExtensionsMethodTranslator(ISqlExpressionFactory sqlExpressionFactory)
    {
      _sqlExpressionFactory = sqlExpressionFactory;
    }

    public SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<Microsoft.EntityFrameworkCore.DbLoggerCategory.Query> logger)
    {
      if (_methodInfos.Any(m => Equals(method, m)))
      {
        var match = _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]);

        var pattern = InferStringTypeMappingOrApplyDefault(
            arguments[2],
            match.TypeMapping!);

        var excapeChar = arguments.Count == 4
            ? InferStringTypeMappingOrApplyDefault(
                arguments[3],
                match.TypeMapping!)
            : null;

        return _sqlExpressionFactory.Like(
            match,
            pattern!,
            excapeChar);
      }

      return null;
    }

    private SqlExpression? InferStringTypeMappingOrApplyDefault(SqlExpression expression, RelationalTypeMapping inferenceSourceTypeMapping)
    {
      if (expression == null)
      {
        return null;
      }

      if (expression.TypeMapping != null)
      {
        return expression;
      }

      if (expression.Type == typeof(string) && inferenceSourceTypeMapping?.ClrType == typeof(string))
      {
        return _sqlExpressionFactory.ApplyTypeMapping(expression, inferenceSourceTypeMapping);
      }

      return _sqlExpressionFactory.ApplyDefaultTypeMapping(expression);
    }
  }
}
