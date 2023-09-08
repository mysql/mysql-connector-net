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

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySql.EntityFrameworkCore.Metadata.Internal;
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Collections.Generic;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  ///   MySQL specific extension methods for <see cref="EntityTypeBuilder" />.
  /// </summary>
  internal static class MySQLEntityTypeBuilderExtensions
  {
    #region CharSet

    /// <summary>
    ///   Sets the MySQL character set on the table associated with this entity. When you only specify the character set, MySQL implicitly
    ///   sets the proper collation as well.
    /// </summary>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="charSet"> The name of the character set. </param>
    /// <returns> The same builder instance so that multiple calls can be chained. </returns>
    public static EntityTypeBuilder HasCharSet(
      [NotNull] this EntityTypeBuilder entityTypeBuilder,
      [CanBeNull] string charSet)
    {
      Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
      Check.NullButNotEmpty(charSet, nameof(charSet));

      entityTypeBuilder.Metadata.SetCharSet(charSet);

      return entityTypeBuilder;
    }

    /// <summary>
    ///   Sets the MySQL character set on the table associated with this entity. When you only specify the character set, MySQL implicitly
    ///   sets the proper collation as well.
    /// </summary>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="charSet"> The name of the character set. </param>
    /// <returns> The same builder instance so that multiple calls can be chained. </returns>
    public static EntityTypeBuilder<TEntity> HasCharSet<TEntity>(
      [NotNull] this EntityTypeBuilder<TEntity> entityTypeBuilder,
      [CanBeNull] string charSet)
      where TEntity : class
      => (EntityTypeBuilder<TEntity>)HasCharSet((EntityTypeBuilder)entityTypeBuilder, charSet);

    /// <summary>
    ///   Sets the MySQL character set on the table associated with this entity. When you only specify the character set, MySQL implicitly
    ///   sets the proper collation as well.
    /// </summary>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="charSet"> The name of the character set. </param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns> The same builder instance so that multiple calls can be chained. </returns>
    public static IConventionEntityTypeBuilder? HasCharSet(
      [NotNull] this IConventionEntityTypeBuilder entityTypeBuilder,
      [CanBeNull] string charSet,
      bool fromDataAnnotation = false)
    {
      Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
      Check.NullButNotEmpty(charSet, nameof(charSet));

      if (entityTypeBuilder.CanSetCharSet(charSet, fromDataAnnotation))
      {
        entityTypeBuilder.Metadata.SetCharSet(charSet, fromDataAnnotation);

        return entityTypeBuilder;
      }

      return null;
    }

    /// <summary>
    ///   Returns a value indicating whether the MySQL character set can be set on the table associated with this entity.
    /// </summary>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="charSet"> The name of the character set. </param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns><see langword="true"/> if the mapped table can be configured with the collation.</returns>
    public static bool CanSetCharSet(
      [NotNull] this IConventionEntityTypeBuilder entityTypeBuilder,
      [CanBeNull] string charSet,
      bool fromDataAnnotation = false)
    {
      Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
      Check.NullButNotEmpty(charSet, nameof(charSet));

      return entityTypeBuilder.CanSetAnnotation(MySQLAnnotationNames.Charset, charSet, fromDataAnnotation);
    }

    #endregion CharSet

    #region Collation

    /// <summary>
    ///   Sets the MySQL collation on the table associated with this entity. When you only specify the collation, MySQL implicitly sets
    ///   the proper character set as well.
    /// </summary>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="collation"> The name of the collation. </param>
    /// <returns> The same builder instance so that multiple calls can be chained. </returns>
    public static EntityTypeBuilder UseCollation(
      [NotNull] this EntityTypeBuilder entityTypeBuilder,
      [CanBeNull] string collation)
    {
      Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
      Check.NullButNotEmpty(collation, nameof(collation));

      entityTypeBuilder.Metadata.SetCollation(collation);

      return entityTypeBuilder;
    }

    /// <summary>
    ///   Sets the MySQL collation on the table associated with this entity. When you only specify the collation, MySQL implicitly sets
    ///   the proper character set as well.
    /// </summary>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="collation"> The name of the collation. </param>
    /// <returns> The same builder instance so that multiple calls can be chained. </returns>
    public static EntityTypeBuilder<TEntity> UseCollation<TEntity>(
      [NotNull] this EntityTypeBuilder<TEntity> entityTypeBuilder,
      [CanBeNull] string collation)
      where TEntity : class
      => (EntityTypeBuilder<TEntity>)UseCollation((EntityTypeBuilder)entityTypeBuilder, collation);

    /// <summary>
    ///   Sets the MySQL collation on the table associated with this entity. When you only specify the collation, MySQL implicitly sets
    ///   the proper character set as well.
    /// </summary>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="collation"> The name of the collation. </param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns> The same builder instance so that multiple calls can be chained. </returns>
    public static IConventionEntityTypeBuilder? UseCollation(
      [NotNull] this IConventionEntityTypeBuilder entityTypeBuilder,
      [CanBeNull] string collation,
      bool fromDataAnnotation = false)
    {
      Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
      Check.NullButNotEmpty(collation, nameof(collation));

      if (entityTypeBuilder.CanSetCollation(collation, fromDataAnnotation))
      {
        entityTypeBuilder.Metadata.SetCollation(collation, fromDataAnnotation);

        return entityTypeBuilder;
      }

      return null;
    }

    /// <summary>
    ///   Returns a value indicating whether the MySQL collation can be set on the table associated with this entity.
    /// </summary>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="collation"> The name of the collation. </param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns><see langword="true"/> if the mapped table can be configured with the collation.</returns>
    public static bool CanSetCollation(
      [NotNull] this IConventionEntityTypeBuilder entityTypeBuilder,
      [CanBeNull] string collation,
      bool fromDataAnnotation = false)
    {
      Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));
      Check.NullButNotEmpty(collation, nameof(collation));

      return entityTypeBuilder.CanSetAnnotation(RelationalAnnotationNames.Collation, collation, fromDataAnnotation);
    }

    /// <summary>
    ///   Returns a value indicating whether the given collation delegation modes can be set.
    /// </summary>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns> <see langword="true" /> if the given collation delegation modes can be set as default. </returns>
    public static bool CanSetCollationDelegation(
      [NotNull] this IConventionEntityTypeBuilder entityTypeBuilder,
      bool fromDataAnnotation = false)
    {
      Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));

      return entityTypeBuilder.CanSetAnnotation(MySQLAnnotationNames.Collation, fromDataAnnotation);
    }

    #endregion Collation

    #region Table options

    /// <summary>
    ///   Sets a table option for the table associated with this entity.
    ///   Can be called more than once to set multiple table options.
    /// </summary>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="name"> The name of the table options. </param>
    /// <param name="value"> The value of the table options. </param>
    /// <returns> The same builder instance so that multiple calls can be chained. </returns>
    public static EntityTypeBuilder HasTableOption(
      [NotNull] this EntityTypeBuilder entityTypeBuilder,
      [CanBeNull] string name,
      [CanBeNull] string value)
    {
      Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));

      var options = entityTypeBuilder.Metadata.GetTableOptions();
      UpdateTableOption(name, value, options);
      entityTypeBuilder.Metadata.SetTableOptions(options);

      return entityTypeBuilder;
    }

    /// <summary>
    ///   Sets a table option for the table associated with this entity.
    ///   Can be called more than once to set multiple table options.
    /// </summary>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="name"> The name of the table options. </param>
    /// <param name="value"> The value of the table options. </param>
    /// <returns> The same builder instance so that multiple calls can be chained. </returns>
    public static EntityTypeBuilder<TEntity> HasTableOption<TEntity>(
      [NotNull] this EntityTypeBuilder<TEntity> entityTypeBuilder,
      [CanBeNull] string name,
      [CanBeNull] string value)
      where TEntity : class
      => (EntityTypeBuilder<TEntity>)HasTableOption((EntityTypeBuilder)entityTypeBuilder, name, value);

    /// <summary>
    ///   Sets a table option for the table associated with this entity.
    ///   Can be called more than once to set multiple table options.
    /// </summary>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="name"> The name of the table options. </param>
    /// <param name="value"> The value of the table options. </param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns> The same builder instance so that multiple calls can be chained. </returns>
    public static IConventionEntityTypeBuilder? HasTableOption(
      [NotNull] this IConventionEntityTypeBuilder entityTypeBuilder,
      [CanBeNull] string name,
      [CanBeNull] string value,
      bool fromDataAnnotation = false)
    {
      Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));

      if (entityTypeBuilder.CanSetTableOption(name, value, fromDataAnnotation))
      {
        var options = entityTypeBuilder.Metadata.GetTableOptions();
        UpdateTableOption(name, value, options);
        entityTypeBuilder.Metadata.SetTableOptions(options, fromDataAnnotation);

        return entityTypeBuilder;
      }

      return null;
    }

    /// <summary>
    ///   Returns a value indicating whether the table options for the table associated with this entity can be set.
    /// </summary>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="name"> The name of the table options. </param>
    /// <param name="value"> The value of the table options. </param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns><see langword="true"/> if the value can be set.</returns>
    public static bool CanSetTableOption(
      [NotNull] this IConventionEntityTypeBuilder entityTypeBuilder,
      [CanBeNull] string name,
      [CanBeNull] string value,
      bool fromDataAnnotation = false)
    {
      Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));

      var options = entityTypeBuilder.Metadata.GetTableOptions();
      UpdateTableOption(name, value, options);
      var optionsString = MySQLEntityTypeExtensions.SerializeTableOptions(options);

      return entityTypeBuilder.CanSetAnnotation(MySQLAnnotationNames.StoreOptions, optionsString, fromDataAnnotation);
    }

    private static void UpdateTableOption(string key, string value, Dictionary<string, string> options)
    {
      if (key == null)
      {
        if (value != null)
        {
          throw new ArgumentException(nameof(value));
        }

        options.Clear();
        return;
      }

      if (value == null)
      {
        options.Remove(key);
        return;
      }

      options[key] = value;
    }

    #endregion Table options
  }
}
