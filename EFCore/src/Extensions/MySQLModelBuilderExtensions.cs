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
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySql.EntityFrameworkCore.Metadata.Internal;
using MySql.EntityFrameworkCore.Utils;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  /// MySQL-specific extension methods for <see cref="ModelBuilder"/>.
  /// </summary>
  public static class MySQLModelBuilderExtensions
  {
    #region CharSet

    /// <summary>
    ///   Sets the default character set to use for the model or database.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="charSet">The name of the character set to use.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static ModelBuilder HasCharSet(
      [NotNull] this ModelBuilder modelBuilder,
      [CanBeNull] string charSet)
    {
      Check.NotNull(modelBuilder, nameof(modelBuilder));
      Check.NullButNotEmpty(charSet, nameof(charSet));

      modelBuilder.Model.SetCharSet(charSet);

      return modelBuilder;
    }

    /// <summary>
    ///   Sets the default character set to use for the model or database.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="charSet">The name of the character set to use.</param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
    /// <returns>
    ///   The same builder instance if the configuration was applied,
    ///   <see langword="null" /> otherwise.
    /// </returns>
    public static IConventionModelBuilder? HasCharSet(
      [NotNull] this IConventionModelBuilder modelBuilder,
      [CanBeNull] string charSet,
      bool fromDataAnnotation = false)
    {
      Check.NotNull(modelBuilder, nameof(modelBuilder));
      Check.NullButNotEmpty(charSet, nameof(charSet));

      if (modelBuilder.CanSetCharSet(charSet, fromDataAnnotation))
      {
        modelBuilder.Metadata.SetCharSet(charSet, fromDataAnnotation);
        return modelBuilder;
      }

      return null;
    }

    /// <summary>
    ///   Returns a value indicating whether the given character set can be set as default.
    /// </summary>
    /// <param name="modelBuilder"> The model builder. </param>
    /// <param name="charSet"> The character set. </param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
    /// <returns> <see langword="true" /> if the given character set can be set as default. </returns>
    public static bool CanSetCharSet(
      [NotNull] this IConventionModelBuilder modelBuilder,
      [CanBeNull] string charSet,
      bool fromDataAnnotation = false)
    {
      Check.NotNull(modelBuilder, nameof(modelBuilder));
      Check.NullButNotEmpty(charSet, nameof(charSet));

      return modelBuilder.CanSetAnnotation(MySQLAnnotationNames.Charset, charSet, fromDataAnnotation);
    }

    #endregion CharSet

    #region Collation

    /// <summary>
    ///   Configures the database collation, which will be used by all columns without an explicit collation. Also finely controls
    ///   where to apply the collation recursively (including this model or database).
    /// </summary>
    /// <param name="modelBuilder"> The model builder. </param>
    /// <param name="collation"> The collation. </param>
    /// <returns> The same builder instance so that multiple calls can be chained. </returns>
    public static ModelBuilder UseCollation(
      [NotNull] this ModelBuilder modelBuilder,
      string collation)
    {
      Check.NotNull(modelBuilder, nameof(modelBuilder));
      Check.NullButNotEmpty(collation, nameof(collation));

      modelBuilder.Model.SetCollation(collation);

      return modelBuilder;
    }

    /// <summary>
    ///   Configures the database collation, which will be used by all columns without an explicit collation. Also finely controls
    ///   where to apply the collation recursively (including this model or database).
    /// </summary>
    /// <param name="modelBuilder"> The model builder. </param>
    /// <param name="collation"> The collation. </param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
    /// <returns>
    ///   The same builder instance if the configuration was applied,
    ///   <see langword="null" /> otherwise.
    /// </returns>
    public static IConventionModelBuilder? UseCollation(
      [NotNull] this IConventionModelBuilder modelBuilder,
      string collation,
      bool fromDataAnnotation = false)
    {
      Check.NotNull(modelBuilder, nameof(modelBuilder));
      Check.NullButNotEmpty(collation, nameof(collation));

      if (modelBuilder.CanSetCollation(collation, fromDataAnnotation))
      {
        modelBuilder.Metadata.SetCollation(collation, fromDataAnnotation);
        return modelBuilder;
      }

      return null;
    }

    #endregion Collation
  }
}
