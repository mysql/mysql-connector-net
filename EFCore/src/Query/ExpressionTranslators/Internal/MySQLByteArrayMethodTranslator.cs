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
using MySql.EntityFrameworkCore.Query.Internal;
using MySql.EntityFrameworkCore.Storage.Internal;
using MySql.EntityFrameworkCore.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MySql.EntityFrameworkCore.Query.ExpressionTranslators.Internal
{
  internal class MySQLByteArrayMethodTranslator : IMethodCallTranslator
  {
    private readonly MySQLSqlExpressionFactory _sqlExpressionFactory;

    private static readonly MethodInfo _containsMethod = typeof(Enumerable)
        .GetTypeInfo()
        .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);

    private static readonly MethodInfo _firstWithoutPredicate = typeof(Enumerable)
        .GetTypeInfo()
        .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Single(mi => mi.Name == nameof(Enumerable.First) && mi.GetParameters().Length == 1);

    public MySQLByteArrayMethodTranslator([NotNull] MySQLSqlExpressionFactory sqlExpressionFactory)
    {
      _sqlExpressionFactory = sqlExpressionFactory;
    }

    public virtual SqlExpression? Translate(
      SqlExpression? instance,
      MethodInfo method,
      IReadOnlyList<SqlExpression> arguments,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
      Check.NotNull(method, nameof(method));
      Check.NotNull(arguments, nameof(arguments));
      Check.NotNull(logger, nameof(logger));

      if (method.IsGenericMethod &&
          arguments[0].TypeMapping is MySQLByteArrayTypeMapping)
      {
        if (method.GetGenericMethodDefinition().Equals(_containsMethod))
        {
          var source = arguments[0];
          var sourceTypeMapping = source.TypeMapping;

          var value = arguments[1] is SqlConstantExpression constantValue
              ? (SqlExpression)_sqlExpressionFactory.Constant(new[] { (byte)constantValue.Value! }, sourceTypeMapping)
              : _sqlExpressionFactory.Convert(arguments[1], typeof(byte[]), sourceTypeMapping);

          return _sqlExpressionFactory.GreaterThan(
              _sqlExpressionFactory.NullableFunction(
                  "LOCATE",
                  new[] { value, source },
                  typeof(int)),
              _sqlExpressionFactory.Constant(0));
        }

        if (method.GetGenericMethodDefinition().Equals(_firstWithoutPredicate))
        {
          return _sqlExpressionFactory.NullableFunction(
              "ASCII",
              new[] { arguments[0] },
              typeof(byte));
        }
      }

      return null;
    }
  }
}
