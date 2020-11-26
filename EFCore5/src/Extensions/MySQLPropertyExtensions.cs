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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MySql.EntityFrameworkCore.Metadata;
using MySql.EntityFrameworkCore.Metadata.Internal;
using MySql.EntityFrameworkCore.Storage.Internal;
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  ///     Extension methods for <see cref="IProperty" /> for MySQL Server-specific metadata.
  /// </summary>
  public static class MySQLPropertyExtensions
  {
    /// <summary>
    ///     <para>
    ///         Returns the <see cref="MySQLValueGenerationStrategy" /> to use for the property.
    ///     </para>
    ///     <para>
    ///         If no strategy is set for the property, then the strategy to use will be taken from the <see cref="IModel" />.
    ///     </para>
    /// </summary>
    /// <returns> The strategy, or <see cref="SqlServerValueGenerationStrategy.None" /> if none was set. </returns>
    public static MySQLValueGenerationStrategy? GetValueGenerationStrategy([NotNull] this IProperty property, StoreObjectIdentifier storeObject = default)
    {
      var annotation = property[MySQLAnnotationNames.ValueGenerationStrategy];
      if (annotation != null)
      {
        return (MySQLValueGenerationStrategy)annotation;
      }

      if (property.GetDefaultValue() != null
        || property.GetDefaultValueSql() != null
        || property.GetComputedColumnSql() != null)
      {
        return null;
      }

      if (storeObject != default &&
          property.ValueGenerated == ValueGenerated.Never )
      {
        return property.FindSharedStoreObjectRootProperty(storeObject)?.GetValueGenerationStrategy(storeObject);
      }

      if (IsCompatibleIdentityColumn(property) && property.ValueGenerated == ValueGenerated.OnAdd)
      {
        return MySQLValueGenerationStrategy.IdentityColumn;
      }

      if (IsCompatibleComputedColumn(property) && property.ValueGenerated == ValueGenerated.OnAddOrUpdate)
      {
        return MySQLValueGenerationStrategy.ComputedColumn;
      }

      return null;
    }


    /// <summary>
    /// Returns a value indicating whether the property is compatible with <see cref="MySQLValueGenerationStrategy.IdentityColumn"/>.
    /// </summary>
    /// <param name="property"> The property. </param>
    /// <returns> <c>true</c> if compatible. </returns>
    public static bool IsCompatibleIdentityColumn(IProperty property)
    {
      var type = property.ClrType;

      return (type.IsInteger()
                  || type == typeof(decimal)
                  || type == typeof(DateTime)
                  || type == typeof(DateTimeOffset))
             && !HasConverter(property);
    }

    /// <summary>
    /// Returns a value indicating whether the property is compatible with <see cref="MySQLValueGenerationStrategy.ComputedColumn"/>.
    /// </summary>
    /// <param name="property"> The property. </param>
    /// <returns> <c>true</c> if compatible. </returns>
    public static bool IsCompatibleComputedColumn(IProperty property)
    {
      var type = property.ClrType;

      // RowVersion uses byte[] and the BytesToDateTimeConverter.
      return (type == typeof(DateTime) || type == typeof(DateTimeOffset)) && !HasConverter(property)
             || type == typeof(byte[]) && !HasExternalConverter(property);
    }

    private static bool HasConverter(IProperty property)
    => GetConverter(property) != null;

    private static bool HasExternalConverter(IProperty property)
    {
      var converter = GetConverter(property);
      return converter != null && !(converter is BytesToDateTimeConverter);
    }

    private static ValueConverter GetConverter(IProperty property)
        => property.FindTypeMapping()?.Converter ?? property.GetValueConverter();

    public static void SetCharSet([NotNull] this IMutableProperty property, string charSet)
        => property.SetOrRemoveAnnotation(MySQLAnnotationNames.Charset, charSet);

  }
}