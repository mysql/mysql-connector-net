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

using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using static MySql.EntityFrameworkCore.Utils.CodeAnnotations;

namespace MySql.EntityFrameworkCore.Utils
{
  [DebuggerStepThrough]
  internal static class Check
  {
    [ContractAnnotation("value:null => halt")]
    public static T NotNull<T>([NoEnumeration] T value, [InvokerParameterName][NotNull] string parameterName)
    {
      if (value == null)
      {
        NotEmpty(parameterName, nameof(parameterName));

        throw new ArgumentNullException(parameterName);
      }

      return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static T NotNull<T>(
      [NoEnumeration] T value,
      [InvokerParameterName][NotNull] string parameterName,
      [NotNull] string propertyName)
    {
      if (value == null)
      {
        NotEmpty(parameterName, nameof(parameterName));
        NotEmpty(propertyName, nameof(propertyName));

        throw new ArgumentException(CoreStrings.ArgumentPropertyNull(propertyName, parameterName));
      }

      return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static IReadOnlyList<T> NotEmpty<T>(IReadOnlyList<T> value, [InvokerParameterName][NotNull] string parameterName)
    {
      NotNull(value, parameterName);

      if (value.Count == 0)
      {
        NotEmpty(parameterName, nameof(parameterName));

        throw new ArgumentException(CoreStrings.ArgumentPropertyNull(value, parameterName));
      }

      return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static string NotEmpty(string value, [InvokerParameterName][NotNull] string parameterName)
    {
      Exception? e = null;
      if (value is null)
      {
        e = new ArgumentNullException(parameterName);
      }
      else if (value.Trim().Length == 0)
      {
        e = new ArgumentException(CoreStrings.ArgumentPropertyNull(value, parameterName));
      }

      if (e != null)
      {
        NotEmpty(parameterName, nameof(parameterName));

        throw e;
      }

      return value!;
    }

    public static string? NullButNotEmpty(string value, [InvokerParameterName][NotNull] string parameterName)
    {
      if (value is object
        && value.Length == 0)
      {
        NotEmpty(parameterName, nameof(parameterName));

        throw new ArgumentException(CoreStrings.ArgumentPropertyNull(value, parameterName));
      }

      return value;
    }

    public static TEnum? NullOrEnumValue<TEnum>(TEnum? value, [InvokerParameterName][NotNull] string parameterName) where TEnum : struct
    {
      if (value is not null)
      {
        if (!Enum.IsDefined(typeof(TEnum), value))
        {
          throw new ArgumentOutOfRangeException(parameterName, value, null);
        }
      }

      return value;
    }

    public static IReadOnlyList<T> HasNoNulls<T>(IReadOnlyList<T> value, [InvokerParameterName][NotNull] string parameterName)
      where T : class
    {
      NotNull(value, parameterName);

      if (value.Any(e => e == null))
      {
        NotEmpty(parameterName, nameof(parameterName));

        throw new ArgumentException(parameterName);
      }

      return value;
    }

    public static T IsDefined<T>(T value, [InvokerParameterName][NotNull] string parameterName)
      where T : struct
    {
      if (!Enum.IsDefined(typeof(T), value))
      {
        NotEmpty(parameterName, nameof(parameterName));

        throw new ArgumentException(CoreStrings.InvalidEnumValue(value, parameterName, typeof(T)));
      }

      return value;
    }

    public static Type ValidEntityType(Type value, [InvokerParameterName][NotNull] string parameterName)
    {
      if (!value.GetTypeInfo().IsClass)
      {
        NotEmpty(parameterName, nameof(parameterName));

        throw new ArgumentException(CoreStrings.InvalidEntityType(value));
      }

      return value;
    }

    [Conditional("DEBUG")]
    public static void DebugAssert([DoesNotReturnIf(false)] bool condition, string message)
    {
      if (!condition)
      {
        throw new Exception($"Check.DebugAssert failed: {message}");
      }
    }

    internal static IEnumerable<bool> GetTrueValuesArray(int counter)
        => Enumerable.Repeat(true, counter);

    internal static IEnumerable<bool> GetFalseValuesArray(int counter)
      => Enumerable.Repeat(false, counter);

  }
}
