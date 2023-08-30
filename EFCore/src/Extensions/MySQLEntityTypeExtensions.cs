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
using MySql.EntityFrameworkCore.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  ///   MySQL specific extension methods for entity types.
  /// </summary>
  public static class MySQLEntityTypeExtensions
  {
    #region CharSet

    /// <summary>
    ///   Get the MySQL character set for the table associated with this entity.
    /// </summary>
    /// <param name="entityType"> The entity type. </param>
    /// <returns> The name of the character set. </returns>
    public static string? GetCharSet([NotNull] this IReadOnlyEntityType entityType)
      => entityType[MySQLAnnotationNames.Charset] as string;

    /// <summary>
    ///   Sets the MySQL character set on the table associated with this entity. When you only specify the character set, MySQL implicitly
    ///   sets the proper collation as well.
    /// </summary>
    /// <param name="entityType"> The entity type. </param>
    /// <param name="charSet"> The name of the character set. </param>
    public static void SetCharSet(
      [NotNull] this IMutableEntityType entityType,
      [CanBeNull] string charSet)
    {
      Check.NullButNotEmpty(charSet, nameof(charSet));

      entityType.SetOrRemoveAnnotation(MySQLAnnotationNames.Charset, charSet);
    }

    /// <summary>
    ///   Sets the MySQL character set on the table associated with this entity. When you only specify the character set, MySQL implicitly
    ///   sets the proper collation as well.
    /// </summary>
    /// <param name="entityType"> The entity type. </param>
    /// <param name="charSet"> The name of the character set. </param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
    /// <returns> The configured value. </returns>
    public static string SetCharSet(
      [NotNull] this IConventionEntityType entityType,
      [CanBeNull] string charSet,
      bool fromDataAnnotation = false)
    {
      Check.NullButNotEmpty(charSet, nameof(charSet));

      entityType.SetOrRemoveAnnotation(MySQLAnnotationNames.Charset, charSet, fromDataAnnotation);

      return charSet;
    }

    /// <summary>
    ///   Gets the configuration source for the character-set mode.
    /// </summary>
    /// <param name="entityType"> The entity type. </param>
    /// <returns> The configuration source. </returns>
    public static ConfigurationSource? GetCharSetConfigurationSource([NotNull] this IConventionEntityType entityType)
      => entityType.FindAnnotation(MySQLAnnotationNames.Charset)?.GetConfigurationSource();

    #endregion CharSet

    #region Collation

    /// <summary>
    ///   Get the MySQL collation for the table associated with this entity.
    /// </summary>
    /// <param name="entityType"> The entity type. </param>
    /// <returns> The name of the collation. </returns>
    public static string? GetCollation([NotNull] this IReadOnlyEntityType entityType)
      => entityType[RelationalAnnotationNames.Collation] as string;

    /// <summary>
    ///   Sets the MySQL collation on the table associated with this entity. When you specify the collation, MySQL implicitly sets the
    ///   proper character set as well.
    /// </summary>
    /// <param name="entityType"> The entity type. </param>
    /// <param name="collation"> The name of the collation. </param>
    public static void SetCollation(
      [NotNull] this IMutableEntityType entityType,
      [CanBeNull] string collation)
    {
      Check.NullButNotEmpty(collation, nameof(collation));

      entityType.SetOrRemoveAnnotation(RelationalAnnotationNames.Collation, collation);
    }

    /// <summary>
    ///   Sets the MySQL collation on the table associated with this entity. When you specify the collation, MySQL implicitly sets the
    ///   proper character set as well.
    /// </summary>
    /// <param name="entityType"> The entity type. </param>
    /// <param name="collation"> The name of the collation. </param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
    /// <returns> The configured value. </returns>
    public static string SetCollation(
      [NotNull] this IConventionEntityType entityType,
      [CanBeNull] string collation,
      bool fromDataAnnotation = false)
    {
      Check.NullButNotEmpty(collation, nameof(collation));

      entityType.SetOrRemoveAnnotation(RelationalAnnotationNames.Collation, collation, fromDataAnnotation);

      return collation;
    }

    /// <summary>
    ///   Gets the configuration source for the collation mode.
    /// </summary>
    /// <param name="entityType"> The entity type. </param>
    /// <returns> The configuration source. </returns>
    public static ConfigurationSource? GetCollationConfigurationSource([NotNull] this IConventionEntityType entityType)
      => entityType.FindAnnotation(RelationalAnnotationNames.Collation)?.GetConfigurationSource();

    #endregion Collation

    #region StoreOptions

    /// <summary>
    ///   Gets the MySQL table options for the table associated with this entity.
    /// </summary>
    /// <param name="entityType"> The entity type. </param>
    /// <returns> A dictionary of table options. </returns>
    public static Dictionary<string, string> GetTableOptions([NotNull] this IReadOnlyEntityType entityType)
      => DeserializeTableOptions(entityType[MySQLAnnotationNames.StoreOptions] as string);

    /// <summary>
    ///   Sets the MySQL table options for the table associated with this entity.
    /// </summary>
    /// <param name="entityType"> The entity type. </param>
    /// <param name="options"> A dictionary of table options. </param>
    public static void SetTableOptions(
      [NotNull] this IMutableEntityType entityType,
      [CanBeNull] Dictionary<string, string> options)
      => entityType.SetOrRemoveAnnotation(MySQLAnnotationNames.StoreOptions, SerializeTableOptions(options));

    /// <summary>
    ///   Sets the MySQL table options for the table associated with this entity.
    /// </summary>
    /// <param name="entityType"> The entity type. </param>
    /// <param name="options"> A dictionary of table options. </param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
    /// <returns> The configured value. </returns>
    public static Dictionary<string, string> SetTableOptions(
      [NotNull] this IConventionEntityType entityType,
      [CanBeNull] Dictionary<string, string> options,
      bool fromDataAnnotation = false)
    {
      entityType.SetOrRemoveAnnotation(MySQLAnnotationNames.StoreOptions, SerializeTableOptions(options), fromDataAnnotation);

      return options;
    }

    /// <summary>
    ///   Gets the configuration source for the table options.
    /// </summary>
    /// <param name="entityType"> The entity type. </param>
    /// <returns> The configuration source. </returns>
    public static ConfigurationSource? GetTableOptionsConfigurationSource([NotNull] this IConventionEntityType entityType)
      => entityType.FindAnnotation(MySQLAnnotationNames.StoreOptions)?.GetConfigurationSource();

    internal static string? SerializeTableOptions(Dictionary<string, string> options)
    {
      var tableOptionsString = new StringBuilder();

      if (options is not null)
      {
        foreach (var (key, value) in options)
        {
          if (string.IsNullOrWhiteSpace(key) ||
          key.Contains(',') ||
          key.Contains('=') ||
          string.IsNullOrWhiteSpace(value))
          {
            throw new ArgumentException(nameof(options));
          }

          tableOptionsString
          .Append(key.Trim())
          .Append('=')
          .Append(value.Trim().Replace(",", ",,"))
          .Append(',');
        }
      }

      if (tableOptionsString.Length == 0)
        return null;

      tableOptionsString.Remove(tableOptionsString.Length - 1, 1);
      return tableOptionsString.ToString();
    }

    internal static Dictionary<string, string> DeserializeTableOptions(string? optionsString)
    {
      var options = new Dictionary<string, string>();

      if (!string.IsNullOrEmpty(optionsString))
      {
        var tableOptionParts = Regex.Split(optionsString, @"(?<=(?:$|[^,])(?:,,)*),(?!,)");

        foreach (var part in tableOptionParts)
        {
          var firstEquals = part.IndexOf('=');

          if (firstEquals > 0 &&
          firstEquals < part.Length - 1)
          {
            var key = part[..firstEquals];
            var value = part[(firstEquals + 1)..].Replace(",,", ",");

            options[key] = value;
          }
        }
      }

      return options;
    }

    #endregion StoreOptions
  }
}
