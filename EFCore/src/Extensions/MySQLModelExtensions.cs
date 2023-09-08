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
using MySql.EntityFrameworkCore.Metadata;
using MySql.EntityFrameworkCore.Metadata.Internal;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  ///   Extension methods for <see cref="IModel" /> for MySQL-specific metadata.
  /// </summary>
  internal static class MySQLModelExtensions
  {
    #region ValueGeneration

    /// <summary>
    ///   Returns the <see cref="MySQLValueGenerationStrategy" /> to use for properties
    ///   of keys in the model, unless the property has a strategy explicitly set.
    /// </summary>
    /// <param name="model"> The model. </param>
    /// <returns> The default <see cref="MySQLValueGenerationStrategy" />. </returns>
    public static MySQLValueGenerationStrategy? GetValueGenerationStrategy([NotNull] this IModel model)
      => (MySQLValueGenerationStrategy?)model[MySQLAnnotationNames.ValueGenerationStrategy];

    /// <summary>
    ///   Attempts to set the <see cref="MySQLValueGenerationStrategy" /> to use for properties
    ///   of keys in the model that do not have a strategy explicitly set.
    /// </summary>
    /// <param name="model"> The model. </param>
    /// <param name="value"> The value to set. </param>
    public static void SetValueGenerationStrategy([NotNull] this IMutableModel model, MySQLValueGenerationStrategy? value)
      => model.SetOrRemoveAnnotation(MySQLAnnotationNames.ValueGenerationStrategy, value);

    /// <summary>
    ///   Attempts to set the <see cref="MySQLValueGenerationStrategy" /> to use for properties
    ///   of keys in the model that do not have a strategy explicitly set.
    /// </summary>
    /// <param name="model"> The model. </param>
    /// <param name="value"> The value to set. </param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
    public static void SetValueGenerationStrategy(
      [NotNull] this IConventionModel model, MySQLValueGenerationStrategy? value, bool fromDataAnnotation = false)
      => model.SetOrRemoveAnnotation(MySQLAnnotationNames.ValueGenerationStrategy, value, fromDataAnnotation);

    /// <summary>
    ///   Returns the <see cref="ConfigurationSource" /> for the default <see cref="MySQLValueGenerationStrategy" />.
    /// </summary>
    /// <param name="model"> The model. </param>
    /// <returns> The <see cref="ConfigurationSource" /> for the default <see cref="MySQLValueGenerationStrategy" />. </returns>
    public static ConfigurationSource? GetValueGenerationStrategyConfigurationSource([NotNull] this IConventionModel model)
      => model.FindAnnotation(MySQLAnnotationNames.ValueGenerationStrategy)?.GetConfigurationSource();

    #endregion

    #region Charset

    /// <summary>
    ///   Returns the character set to use as the default for the model or database.
    /// </summary>
    /// <param name="model"> The model. </param>
    /// <returns> The default character set. </returns>
    public static string? GetCharSet([NotNull] this IModel model)
      => model[MySQLAnnotationNames.Charset] as string;

    /// <summary>
    ///   Attempts to set the character set to use as the default for the model or database.
    /// </summary>
    /// <param name="model"> The model. </param>
    /// <param name="charSet"> The default character set. </param>
    public static void SetCharSet([NotNull] this IMutableModel model, string charSet)
      => model.SetOrRemoveAnnotation(MySQLAnnotationNames.Charset, charSet);

    /// <summary>
    ///   Attempts to set the character set to use as the default for the model or database.
    /// </summary>
    /// <param name="model"> The model. </param>
    /// <param name="charSet"> The default character set. </param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
    public static string SetCharSet([NotNull] this IConventionModel model, string charSet, bool fromDataAnnotation = false)
    {
      model.SetOrRemoveAnnotation(MySQLAnnotationNames.Charset, charSet, fromDataAnnotation);

      return charSet;
    }

    /// <summary>
    ///   Returns the <see cref="ConfigurationSource" /> for the default character set of the model or database.
    /// </summary>
    /// <param name="model"> The model. </param>
    /// <returns> The <see cref="ConfigurationSource" /> for the default character set. </returns>
    public static ConfigurationSource? GetCharSetConfigurationSource([NotNull] this IConventionModel model)
      => model.FindAnnotation(MySQLAnnotationNames.Charset)?.GetConfigurationSource();

    #endregion
  }
}
