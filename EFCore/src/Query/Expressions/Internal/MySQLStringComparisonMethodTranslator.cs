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
using MySql.EntityFrameworkCore.Properties;
using MySql.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static MySql.EntityFrameworkCore.Utils.Statics;

namespace MySql.EntityFrameworkCore.Query.Expressions.Internal
{
  internal class MySQLStringComparisonMethodTranslator : IMethodCallTranslator
  {
    private static readonly MethodInfo? _equalsMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.Equals), new[] { typeof(string), typeof(StringComparison) });
    private static readonly MethodInfo? _staticEqualsMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.Equals), new[] { typeof(string), typeof(string), typeof(StringComparison) });
    private static readonly MethodInfo? _startsWithMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.StartsWith), new[] { typeof(string), typeof(StringComparison) });
    private static readonly MethodInfo? _endsWithMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.EndsWith), new[] { typeof(string), typeof(StringComparison) });
    private static readonly MethodInfo? _containsMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.Contains), new[] { typeof(string), typeof(StringComparison) });
    private static readonly MethodInfo? _indexOfMethodInfo
      = typeof(string).GetRuntimeMethod(nameof(string.IndexOf), new[] { typeof(string), typeof(StringComparison) });

    private readonly SqlExpression _caseSensitiveComparisons;

    private readonly MySQLSqlExpressionFactory _sqlExpressionFactory;

    public MySQLStringComparisonMethodTranslator(ISqlExpressionFactory sqlExpressionFactory)
    {
      _sqlExpressionFactory = (MySQLSqlExpressionFactory)sqlExpressionFactory;
      _caseSensitiveComparisons = _sqlExpressionFactory.Constant(
        new[]
        {
      StringComparison.Ordinal,
      StringComparison.CurrentCulture,
      StringComparison.InvariantCulture
        });
    }

    public SqlExpression? Translate(SqlExpression? instance,
                    MethodInfo method,
                    IReadOnlyList<SqlExpression> arguments,
                    IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
      if (Equals(method, _equalsMethodInfo) && instance != null)
      {
        return MakeStringEqualsExpression(
          instance,
          arguments[0],
          arguments[1]
        );
      }
      else if (Equals(method, _staticEqualsMethodInfo))
      {
        return MakeStringEqualsExpression(
          arguments[0],
          arguments[1],
          arguments[2]
        );
      }
      else if (Equals(method, _startsWithMethodInfo) && instance != null)
      {
        return MakeStartsWithExpression(
          instance,
          arguments[0],
          arguments[1]
        );
      }
      else if (Equals(method, _endsWithMethodInfo) && instance != null)
      {
        return MakeEndsWithExpression(
          instance,
          arguments[0],
          arguments[1]
        );
      }
      else if (Equals(method, _containsMethodInfo) && instance != null)
      {
        return MakeContainsExpression(
          instance,
          arguments[0],
          arguments[1]
        );
      }
      else if (Equals(method, _indexOfMethodInfo) && instance != null)
      {
        return MakeIndexOfExpression(
          instance,
          arguments[0],
          arguments[1]
        );
      }

      return null;
    }

    public SqlExpression? MakeStringEqualsExpression(
      [NotNull] SqlExpression leftValue,
      [NotNull] SqlExpression rightValue,
      [NotNull] SqlExpression stringComparison)
    {
      if (TryGetExpressionValue<StringComparison>(stringComparison, out var cmp))
      {
        return CreateExpressionForCaseSensitivity(
          cmp,
          () =>
          {
            if (leftValue is ColumnExpression)
            {
              // Applying the binary operator to the non-column value enables SQL to
              // utilize an index if one exists.
              return _sqlExpressionFactory.Equal(
            leftValue,
            Utf8Bin(rightValue)
          );
            }
            else
            {
              return _sqlExpressionFactory.Equal(
                Utf8Bin(leftValue),
                rightValue
              );
            }
          },
          () =>
            _sqlExpressionFactory.Equal(
              LCase(leftValue),
              Utf8Bin(LCase(rightValue))
            )
        );
      }
      else
      {
      #if !NET8_0
        return new CaseExpression(
          new[]
          {
        new CaseWhenClause(
          _sqlExpressionFactory.In(stringComparison, _caseSensitiveComparisons, false),
          // Case sensitive, accent sensitive
          _sqlExpressionFactory.Equal(
            leftValue,
            Utf8Bin(rightValue)
          )
        )
          },
          // Case insensitive, accent sensitive
          _sqlExpressionFactory.Equal(
            LCase(leftValue),
            Utf8Bin(LCase(rightValue))
          )
        );
#else
        return new CaseExpression(
          new[]
          {
        new CaseWhenClause(
          _sqlExpressionFactory.In(stringComparison,(IReadOnlyList<SqlExpression>)_caseSensitiveComparisons),
          // Case sensitive, accent sensitive
          _sqlExpressionFactory.Equal(
            leftValue,
            Utf8Bin(rightValue)
          )
        )
          },
          // Case insensitive, accent sensitive
          _sqlExpressionFactory.Equal(
            LCase(leftValue),
            Utf8Bin(LCase(rightValue))
          )
        );
#endif
      }
    }

    public SqlExpression? MakeStartsWithExpression(
      [NotNull] SqlExpression target,
      [NotNull] SqlExpression prefix,
      [NotNull] SqlExpression stringComparison)
    {
      if (TryGetExpressionValue<StringComparison>(stringComparison, out var cmp))
      {
        return CreateExpressionForCaseSensitivity(
          cmp,
          () =>
            MakeStartsWithExpressionImpl(
              target,
              Utf8Bin(prefix),
              originalPrefix: prefix
            ),
          () =>
            MakeStartsWithExpressionImpl(
              LCase(target),
              Utf8Bin(LCase(prefix))
            )
        );
      }
      else
      {
#if !NET8_0
        return new CaseExpression(
          new[]
          {
        new CaseWhenClause(
          _sqlExpressionFactory.In(stringComparison, _caseSensitiveComparisons, false),
          // Case sensitive, accent sensitive
          MakeStartsWithExpressionImpl(
            target,
            Utf8Bin(prefix),
            originalPrefix: prefix
          )
        )
          },
          // Case insensitive, accent sensitive
          MakeStartsWithExpressionImpl(
            LCase(target),
            Utf8Bin(LCase(prefix))
          )
        );
#else
        return new CaseExpression(
          new[]
          {
        new CaseWhenClause(
          _sqlExpressionFactory.In(stringComparison,(IReadOnlyList<SqlExpression>) _caseSensitiveComparisons),
          // Case sensitive, accent sensitive
          MakeStartsWithExpressionImpl(
            target,
            Utf8Bin(prefix),
            originalPrefix: prefix
          )
        )
          },
          // Case insensitive, accent sensitive
          MakeStartsWithExpressionImpl(
            LCase(target),
            Utf8Bin(LCase(prefix))
          )
        );
#endif
      }
    }

    private SqlBinaryExpression MakeStartsWithExpressionImpl(
      SqlExpression target,
      SqlExpression prefix,
      SqlExpression? originalPrefix = null)
    {
      return _sqlExpressionFactory.AndAlso(
        _sqlExpressionFactory.Like(
          target,
          _sqlExpressionFactory.ApplyDefaultTypeMapping(_sqlExpressionFactory.Function(
            "CONCAT",
            new[] { originalPrefix ?? prefix, _sqlExpressionFactory.Constant("%") },
            nullable: true,
            argumentsPropagateNullability: TrueArrays[2],
            typeof(string)))),
        _sqlExpressionFactory.Equal(
          _sqlExpressionFactory.Function(
            "LEFT",
            new[]
            {
          target,
          CharLength(prefix)
            },
            nullable: true,
            argumentsPropagateNullability: TrueArrays[2],
            typeof(string)),
          prefix
        ));
    }

    public SqlExpression? MakeEndsWithExpression(
      [NotNull] SqlExpression target,
      [NotNull] SqlExpression suffix,
      [NotNull] SqlExpression stringComparison)
    {
      if (TryGetExpressionValue<StringComparison>(stringComparison, out var cmp))
      {
        return CreateExpressionForCaseSensitivity(
          cmp,
          () =>
            MakeEndsWithExpressionImpl(
              target,
              Utf8Bin(suffix),
              suffix
            ),
          () =>
            MakeEndsWithExpressionImpl(
              LCase(target),
              Utf8Bin(LCase(suffix)),
              suffix
            )
        );
      }
      else
      {
#if !NET8_0
        return new CaseExpression(
          new[]
          {
        new CaseWhenClause(
          _sqlExpressionFactory.In(stringComparison, _caseSensitiveComparisons, false),
          // Case sensitive, accent sensitive
          MakeEndsWithExpressionImpl(
            target,
            Utf8Bin(suffix),
            suffix
          )
        )
          },
          // Case insensitive, accent sensitive
          MakeEndsWithExpressionImpl(
            LCase(target),
            Utf8Bin(LCase(suffix)),
            suffix
          )
        );
#else
        return new CaseExpression(
          new[]
          {
        new CaseWhenClause(
          _sqlExpressionFactory.In(stringComparison,(IReadOnlyList<SqlExpression>) _caseSensitiveComparisons),
          // Case sensitive, accent sensitive
          MakeEndsWithExpressionImpl(
            target,
            Utf8Bin(suffix),
            suffix
          )
        )
          },
          // Case insensitive, accent sensitive
          MakeEndsWithExpressionImpl(
            LCase(target),
            Utf8Bin(LCase(suffix)),
            suffix
          )
        );
#endif
      }
    }

    private SqlExpression MakeEndsWithExpressionImpl(
      SqlExpression target,
      SqlExpression suffix,
      SqlExpression originalSuffix)
    {
      var endsWithExpression =
        _sqlExpressionFactory.Equal(
          _sqlExpressionFactory.Function(
            "RIGHT",
            new[]
            {
          target,
          CharLength(suffix)
            },
            nullable: true,
            argumentsPropagateNullability: TrueArrays[2],
            target.Type,
            null),
          suffix);

      if (originalSuffix is SqlConstantExpression constantSuffix)
      {
        return constantSuffix.Value != null && ((string)constantSuffix.Value) == string.Empty
          ? _sqlExpressionFactory.Constant(true)
          : endsWithExpression;
      }
      else
      {
        return _sqlExpressionFactory.OrElse(
          endsWithExpression,
          _sqlExpressionFactory.Equal(originalSuffix, _sqlExpressionFactory.Constant(string.Empty)));
      }
    }

    public SqlExpression? MakeContainsExpression(
      SqlExpression target,
      SqlExpression search,
      SqlExpression? stringComparison = null)
    {
      if (stringComparison == null)
      {
        return MakeContainsExpressionImpl(
          target,
          e => e,
          search,
          e => e);
      }

      if (TryGetExpressionValue<StringComparison>(stringComparison, out var cmp))
      {
        return CreateExpressionForCaseSensitivity(
          cmp,
          () =>
            MakeContainsExpressionImpl(
              target,
              e => e,
              search,
              e => Utf8Bin(e)
            ),
          () =>
            MakeContainsExpressionImpl(
              target,
              e => LCase(e),
              search,
              e => Utf8Bin(LCase(e))
            )
        );
      }
      else
      {
#if !NET8_0
        return new CaseExpression(
          new[]
          {
        new CaseWhenClause(
          _sqlExpressionFactory.In(stringComparison, _caseSensitiveComparisons, false),
          // Case sensitive, accent sensitive
          MakeContainsExpressionImpl(
            target,
            e => e,
            search,
            e => Utf8Bin(e)
          )
        )
          },
          // Case insensitive, accent sensitive
          MakeContainsExpressionImpl(
              target,
              e => LCase(e),
              search,
              e => Utf8Bin(LCase(e))
          )
        );
#else
        return new CaseExpression(
          new[]
          {
        new CaseWhenClause(
          _sqlExpressionFactory.In(stringComparison,(IReadOnlyList<SqlExpression>) _caseSensitiveComparisons),
          // Case sensitive, accent sensitive
          MakeContainsExpressionImpl(
            target,
            e => e,
            search,
            e => Utf8Bin(e)
          )
        )
          },
          // Case insensitive, accent sensitive
          MakeContainsExpressionImpl(
              target,
              e => LCase(e),
              search,
              e => Utf8Bin(LCase(e))
          )
        );
#endif
      }
    }
    private SqlExpression MakeContainsExpressionImpl(
          SqlExpression target,
          Func<SqlExpression, SqlExpression> targetTransform,
          SqlExpression pattern,
          Func<SqlExpression, SqlExpression> patternTransform)
    {
      var stringTypeMapping = ExpressionExtensions.InferTypeMapping(target, pattern);
      target = _sqlExpressionFactory.ApplyTypeMapping(target, stringTypeMapping);
      pattern = _sqlExpressionFactory.ApplyTypeMapping(pattern, stringTypeMapping);

      if (pattern is SqlConstantExpression constantPatternExpression)
      {
        if (constantPatternExpression.Value is string constantPatternString)
        {
          return constantPatternString == string.Empty
            ? (SqlExpression)_sqlExpressionFactory.Constant(true)
            : _sqlExpressionFactory.Like(
              targetTransform(target),
              patternTransform(_sqlExpressionFactory.Constant('%' + EscapeLikePattern(constantPatternString) + '%')));
        }


        return _sqlExpressionFactory.Like(target, _sqlExpressionFactory.Constant(null, stringTypeMapping));
      }


      return _sqlExpressionFactory.OrElse(
        _sqlExpressionFactory.Like(
          pattern,
          _sqlExpressionFactory.Constant(string.Empty, stringTypeMapping)),
        _sqlExpressionFactory.GreaterThan(
          _sqlExpressionFactory.NullableFunction(
            "LOCATE",
            new[] { patternTransform(pattern), targetTransform(target) },
            typeof(int)),
            _sqlExpressionFactory.Constant(0)));
    }



    public SqlExpression? MakeIndexOfExpression(
        SqlExpression? target,
        SqlExpression search,
        SqlExpression? stringComparison = null)
    {
      if (stringComparison == null)
      {
        return MakeIndexOfExpressionImpl(
          target,
          e => e,
          search,
          e => e);
      }

      if (TryGetExpressionValue<StringComparison>(stringComparison, out var cmp))
      {
        return CreateExpressionForCaseSensitivity(
          cmp,
          () => MakeIndexOfExpressionImpl(
            target,
            e => e,
            search,
            e => Utf8Bin(e)),
          () => MakeIndexOfExpressionImpl(
            target,
            e => LCase(e),
            search,
            e => Utf8Bin(LCase(e))));
      }

#if !NET8_0
      return _sqlExpressionFactory.Case(
        new[]
        {
      new CaseWhenClause(
        _sqlExpressionFactory.In(stringComparison, _caseSensitiveComparisons, false),
        // Case sensitive, accent sensitive
        MakeIndexOfExpressionImpl(
          target,
          e => e,
          search,
          e => Utf8Bin(e)))
        },
        // Case insensitive, accent sensitive
        MakeIndexOfExpressionImpl(
          target,
          e => LCase(e),
          search,
          e => Utf8Bin(LCase(e))));
#else
      return _sqlExpressionFactory.Case(
              new[]
              {
      new CaseWhenClause(
        _sqlExpressionFactory.In(stringComparison, (IReadOnlyList<SqlExpression>)_caseSensitiveComparisons),
        // Case sensitive, accent sensitive
        MakeIndexOfExpressionImpl(
          target,
          e => e,
          search,
          e => Utf8Bin(e)))
              },
              // Case insensitive, accent sensitive
              MakeIndexOfExpressionImpl(
                target,
                e => LCase(e),
                search,
                e => Utf8Bin(LCase(e))));
#endif
    }

    private SqlExpression MakeIndexOfExpressionImpl(
      SqlExpression? target,
      [NotNull] Func<SqlExpression, SqlExpression> targetTransform,
      SqlExpression pattern,
      [NotNull] Func<SqlExpression, SqlExpression> patternTransform)
    {
      return _sqlExpressionFactory.Subtract(
      _sqlExpressionFactory.NullableFunction(
        "LOCATE",
        new[] { patternTransform(pattern), targetTransform(target!) },
        typeof(int)),
      _sqlExpressionFactory.Constant(1));
    }

    private static bool TryGetExpressionValue<T>(SqlExpression expression, out T value)
    {
      if (expression.Type != typeof(T))
      {
        throw new ArgumentException(
          MySQLStrings.ExpressionTypeMismatch,
          nameof(expression)
        );
      }

      if (expression is SqlConstantExpression constant)
      {
        value = (T)constant.Value!;
        return true;
      }
      else
      {
        value = default!;
        return false;
      }
    }

    private static SqlExpression? CreateExpressionForCaseSensitivity(
      StringComparison cmp,
      Func<SqlExpression> ifCaseSensitive,
      Func<SqlExpression> ifCaseInsensitive)
    {
      switch (cmp)
      {
        case StringComparison.Ordinal:
        case StringComparison.CurrentCulture:
        case StringComparison.InvariantCulture:
          return ifCaseSensitive();
        case StringComparison.OrdinalIgnoreCase:
        case StringComparison.CurrentCultureIgnoreCase:
        case StringComparison.InvariantCultureIgnoreCase:
          return ifCaseInsensitive();
        default:
          return default;
      }
    }

    private SqlExpression LCase(SqlExpression value)
    {
      return _sqlExpressionFactory.NullableFunction("LCASE", new[] { value }, value.Type);
    }

    private SqlExpression Utf8Bin(SqlExpression value)
    {
      return _sqlExpressionFactory.Collate(value, "utf8mb4", "utf8mb4_bin");
    }

    private SqlExpression CharLength(SqlExpression value)
    {
      return _sqlExpressionFactory.NullableFunction("CHAR_LENGTH", new[] { value }, typeof(int));
    }

    private const char LikeEscapeChar = '\\';

    private static bool IsLikeWildChar(char c) => c == '%' || c == '_';

    private static string EscapeLikePattern(string pattern)
    {
      var builder = new StringBuilder();
      foreach (var c in pattern)
      {
        if (IsLikeWildChar(c) ||
          c == LikeEscapeChar)
        {
          builder.Append(LikeEscapeChar);
        }

        builder.Append(c);
      }

      return builder.ToString();
    }
  }
}
