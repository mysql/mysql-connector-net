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
using MySql.EntityFrameworkCore.Metadata.Internal;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  ///   Extension methods for <see cref="IKey" /> for MySQL-specific metadata.
  /// </summary>
  public static class MySQLKeyExtensions
  {
    /// <summary>
    ///   Returns prefix lengths for the key.
    /// </summary>
    /// <param name="key"> The key. </param>
    /// <returns> The prefix lengths.
    /// A value of `0` indicates, that the full length should be used for that column. </returns>
    public static int[] PrefixLength([NotNull] this IKey key)
        => (int[])key[MySQLAnnotationNames.IndexPrefixLength]!;

    /// <summary>
    ///   Sets prefix lengths for the key.
    /// </summary>
    /// <param name="values"> The prefix lengths to set.
    /// A value of `0` indicates, that the full length should be used for that column. </param>
    /// <param name="key"> The key. </param>
    public static void SetPrefixLength([NotNull] this IMutableKey key, int[] values)
      => key.SetOrRemoveAnnotation(MySQLAnnotationNames.IndexPrefixLength, values);

    /// <summary>
    ///   Sets prefix lengths for the key.
    /// </summary>
    /// <param name="values"> The prefix lengths to set.
    /// A value of `0` indicates, that the full length should be used for that column. </param>
    /// <param name="key"> The key. </param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
    public static void SetPrefixLength([NotNull] this IConventionKey key, int[] values, bool fromDataAnnotation = false)
      => key.SetOrRemoveAnnotation(MySQLAnnotationNames.IndexPrefixLength, values, fromDataAnnotation);

    /// <summary>
    ///   Returns the <see cref="ConfigurationSource" /> for prefix lengths of the key.
    /// </summary>
    /// <param name="property"> The property. </param>
    /// <returns> The <see cref="ConfigurationSource" /> for prefix lengths of the key. </returns>
    public static ConfigurationSource? GetPrefixLengthConfigurationSource([NotNull] this IConventionKey property)
      => property.FindAnnotation(MySQLAnnotationNames.IndexPrefixLength)?.GetConfigurationSource();
  }
}
