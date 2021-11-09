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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MySql.EntityFrameworkCore.Metadata;
using MySql.EntityFrameworkCore.Metadata.Internal;
using MySql.EntityFrameworkCore.Properties;
using MySql.EntityFrameworkCore.Storage.Internal;
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Linq;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  ///     Extension methods for <see cref="IProperty" /> for MySQL Server metadata.
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
    public static MySQLValueGenerationStrategy? GetValueGenerationStrategy([NotNull] this IReadOnlyProperty property, StoreObjectIdentifier storeObject = default)
    {
      var annotation = property[MySQLAnnotationNames.ValueGenerationStrategy];
      if (annotation != null)
      {
        return (MySQLValueGenerationStrategy)annotation;
      }

      if (property.GetContainingForeignKeys().Any(fk => !fk.IsBaseLinking()) ||
          property.TryGetDefaultValue(storeObject, out _) ||
          property.GetDefaultValueSql() != null ||
          property.GetComputedColumnSql() != null)
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
    /// Indicates whether the property is compatible with <see cref="MySQLValueGenerationStrategy.IdentityColumn"/>.
    /// </summary>
    /// <param name="property"> The property. </param>
    /// <returns><see langword="true"/> if compatible; otherwise, <see langword="false"/>.</returns>
    public static bool IsCompatibleIdentityColumn(IReadOnlyProperty property)
            => IsCompatibleAutoIncrementColumn(property) ||
               IsCompatibleCurrentTimestampColumn(property);

    /// <summary>
    ///     Returns a value indicating whether the property is compatible with an `AUTO_INCREMENT` column.
    /// </summary>
    /// <param name="property"> The property. </param>
    /// <returns> <see langword="true"/> if compatible. </returns>
    public static bool IsCompatibleAutoIncrementColumn(IReadOnlyProperty property)
    {
      var valueConverter = GetConverter(property);
      var type = (valueConverter?.ProviderClrType ?? property.ClrType).UnwrapNullableType();
      return type.IsInteger() ||
             type == typeof(decimal);
    }

    /// <summary>
    ///     Returns a value indicating whether the property is compatible with a `CURRENT_TIMESTAMP` column default.
    /// </summary>
    /// <param name="property"> The property. </param>
    /// <returns> <see langword="true"/> if compatible. </returns>
    public static bool IsCompatibleCurrentTimestampColumn(IReadOnlyProperty property)
    {
      var valueConverter = GetConverter(property);
      var type = (valueConverter?.ProviderClrType ?? property.ClrType).UnwrapNullableType();
      return type == typeof(DateTime) ||
             type == typeof(DateTimeOffset);
    }

    /// <summary>
    /// Indicates whether the property is compatible with <see cref="MySQLValueGenerationStrategy.ComputedColumn"/>.
    /// </summary>
    /// <param name="property"> The property. </param>
    /// <returns><see langword="true"/> if compatible; otherwise, <see langword="false"/>.</returns>
    public static bool IsCompatibleComputedColumn(IReadOnlyProperty property)
    {
      var type = property.ClrType;

      // RowVersion uses byte[] and the BytesToDateTimeConverter.
      return (type == typeof(DateTime) || type == typeof(DateTimeOffset)) && !HasConverter(property)
             || type == typeof(byte[]) && !HasExternalConverter(property);
    }

    private static bool HasConverter(IReadOnlyProperty property)
    => GetConverter(property) != null;

    private static bool HasExternalConverter(IReadOnlyProperty property)
    {
      var converter = GetConverter(property);
      return converter != null && !(converter is BytesToDateTimeConverter);
    }

    private static ValueConverter? GetConverter(IReadOnlyProperty property)
        => property.FindTypeMapping()?.Converter ?? property.GetValueConverter();

    public static void SetCharSet([NotNull] this IMutableProperty property, string charSet)
        => property.SetOrRemoveAnnotation(MySQLAnnotationNames.Charset, charSet);

    /// <summary>
    /// Sets the <see cref="MySQLValueGenerationStrategy" /> to use for the property.
    /// </summary>
    /// <param name="property">The property. </param>
    /// <param name="value">The strategy to use. </param>
    public static void SetValueGenerationStrategy([NotNull] this IMutableProperty property, MySQLValueGenerationStrategy? value)
    {
      CheckValueGenerationStrategy(property, value);

      property.SetOrRemoveAnnotation(MySQLAnnotationNames.ValueGenerationStrategy, value);
    }

    private static void CheckValueGenerationStrategy(IReadOnlyProperty property, MySQLValueGenerationStrategy? value)
    {
      if (value != null)
      {
        var propertyType = property.ClrType;

        if (value == MySQLValueGenerationStrategy.IdentityColumn && !IsCompatibleIdentityColumn(property))
        {
          throw new ArgumentException(string.Format(MySQLStrings.WrongIdentityType,
          property.DeclaringEntityType.DisplayName(), property.Name, propertyType.ShortDisplayName()));
        }

        if (value == MySQLValueGenerationStrategy.ComputedColumn && !IsCompatibleComputedColumn(property))
        {
          throw new ArgumentException(string.Format(MySQLStrings.WrongComputedType,
          property.DeclaringEntityType.DisplayName(), property.Name, propertyType.ShortDisplayName()));
        }
      }
    }
  }
}