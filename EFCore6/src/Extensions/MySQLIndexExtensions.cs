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

using Microsoft.EntityFrameworkCore.Metadata;
using MySql.EntityFrameworkCore.Metadata.Internal;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  ///     Extension methods for <see cref="IIndex" /> for SQL Server-specific metadata.
  /// </summary>
  public static class MySQLIndexExtensions
  {
    /// <summary>
    ///     Returns a value indicating whether the index is full text.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns><see langword="true"/> if the index is clustered; otherwise, <see langword="false"/>.</returns>
    public static bool? IsFullText([NotNull] this IIndex index)
        => (bool?)index[MySQLAnnotationNames.FullTextIndex];

    /// <summary>
    ///     Sets a value indicating whether the index is full text.
    /// </summary>
    /// <param name="value">The value to set.</param>
    /// <param name="index">The index.</param>
    public static void SetIsFullText([NotNull] this IMutableIndex index, bool? value)
        => index.SetOrRemoveAnnotation(
            MySQLAnnotationNames.FullTextIndex,
            value);

    /// <summary>
    ///     Sets a value indicating whether the index is full text.
    /// </summary>
    /// <param name="value">The value to set.</param>
    /// <param name="index">The index.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    public static void SetIsFullText(
        [NotNull] this IConventionIndex index, bool? value, bool fromDataAnnotation = false)
        => index.SetOrRemoveAnnotation(
            MySQLAnnotationNames.FullTextIndex,
            value,
            fromDataAnnotation);

    /// <summary>
    ///     Returns the <see cref="ConfigurationSource" /> for whether the index is full text.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The <see cref="ConfigurationSource" /> for whether the index is full text.</returns>
    public static ConfigurationSource? GetIsFullTextConfigurationSource([NotNull] this IConventionIndex property)
        => property.FindAnnotation(MySQLAnnotationNames.FullTextIndex)?.GetConfigurationSource();


    /// <summary>
    ///     Returns a value indicating whether the index is spartial.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns><see langword="true"/> if the index is clustered; otherwise, <see langword="false"/></returns>
    public static bool? IsSpatial([NotNull] this IIndex index)
        => (bool?)index[MySQLAnnotationNames.SpatialIndex];

    /// <summary>
    ///     Sets a value indicating whether the index is spartial.
    /// </summary>
    /// <param name="value">The value to set.</param>
    /// <param name="index">The index.</param>
    public static void SetIsSpatial([NotNull] this IMutableIndex index, bool? value)
        => index.SetOrRemoveAnnotation(
            MySQLAnnotationNames.SpatialIndex,
            value);

    /// <summary>
    ///     Sets a value indicating whether the index is spartial.
    /// </summary>
    /// <param name="value">The value to set.</param>
    /// <param name="index">The index.</param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation.</param>
    public static void SetIsSpatial(
        [NotNull] this IConventionIndex index, bool? value, bool fromDataAnnotation = false)
        => index.SetOrRemoveAnnotation(
            MySQLAnnotationNames.SpatialIndex,
            value,
            fromDataAnnotation);

    /// <summary>
    ///     Indicates whether the index is spatial.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The <see cref="ConfigurationSource" /> to show if the index is spartial.</returns>
    public static ConfigurationSource? GetIsSpatialConfigurationSource([NotNull] this IConventionIndex property)
        => property.FindAnnotation(MySQLAnnotationNames.SpatialIndex)?.GetConfigurationSource();
  }
}