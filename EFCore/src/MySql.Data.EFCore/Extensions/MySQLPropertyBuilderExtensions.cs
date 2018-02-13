// Copyright © 2016, 2017, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySql.Data.EntityFrameworkCore.Metadata.Internal;
using System;

namespace MySql.Data.EntityFrameworkCore.Extensions
{
  /// <summary>
  /// Represents the implementation of MySQL property-builder extensions used in Fluent API.
  /// </summary>
  public static class MySQLPropertyBuilderExtensions
  {
    /// <summary>
    /// Defines a MySQL auto-increment column.
    /// </summary>
    /// <param name="propertyBuilder">Entity property to be set.</param>
    /// <param name="typeName">MySQL column type as string.</param>
    /// <returns>Property builder of the auto-increment column.</returns>
    public static PropertyBuilder UseMySQLAutoIncrementColumn(
      [NotNull] this PropertyBuilder propertyBuilder,
      [CanBeNull] string typeName)
    {
      ThrowIf.Argument.IsNull(propertyBuilder, "propertyBuilder");

      propertyBuilder.ValueGeneratedOnAdd();
      return propertyBuilder;
    }

    /// <summary>
    /// Defines a column default value expression.
    /// </summary>
    /// <param name="propertyBuilder">Entity property to be set.</param>
    /// <param name="sql">Default value expression.</param>
    /// <returns>Property builder of a MySQL column with a default value.</returns>
    public static PropertyBuilder ForMySQLHasDefaultValueSql(
      [NotNull] this PropertyBuilder propertyBuilder,
      [CanBeNull] string sql)
    {
      ThrowIf.Argument.IsNull(propertyBuilder, "propertyBuilder");

      if (sql != null && sql.Length == 0)
        ThrowIf.Argument.IsEmpty(sql, nameof(sql));

      propertyBuilder.ValueGeneratedOnAdd();
      propertyBuilder.Metadata.Relational().DefaultValueSql = sql;

      return propertyBuilder;
    }

    /// <summary>
    /// Defines a column default value.
    /// </summary>
    /// <param name="propertyBuilder">Entity property to be set.</param>
    /// <param name="sql">Default value.</param>
    /// <returns>Property builder of a MySQL column with a default value.</returns>
    public static PropertyBuilder ForMySQLHasDefaultValue(
      [NotNull] this PropertyBuilder propertyBuilder,
      [CanBeNull] object value = null)
    {
      ThrowIf.Argument.IsNull(propertyBuilder, "propertyBuilder");

      propertyBuilder.ValueGeneratedOnAdd();
      propertyBuilder.Metadata.Relational().DefaultValue = value;// ?? DBNull.Value;

      return propertyBuilder;
    }

    /// <summary>
    /// Adds the character set to an entity property.
    /// </summary>
    /// <param name="propertyBuilder">Property builder.</param>
    /// <param name="charset">MySQL character set to use.</param>
    /// <returns>Property builder with a character set.</returns>
    public static PropertyBuilder ForMySQLHasCharset(
      [NotNull] this PropertyBuilder propertyBuilder,
      [NotNull] string charset)
    {
      propertyBuilder.Metadata.AddAnnotation(MySQLAnnotationNames.Charset, charset);
      return propertyBuilder;
    }

    /// <summary>
    /// Adds the character set to an entity.
    /// </summary>
    /// <param name="entityTypeBuilder">Entity type builder.</param>
    /// <param name="charset">MySQL character set to use.</param>
    /// <returns>Entity type builder with a character set.</returns>
    public static EntityTypeBuilder ForMySQLHasCharset(
      [NotNull] this EntityTypeBuilder entityTypeBuilder,
      [NotNull] string charset)
    {
      entityTypeBuilder.Metadata.AddAnnotation(MySQLAnnotationNames.Charset, charset);
      return entityTypeBuilder;
    }

    /// <summary>
    /// Adds the collation to an entity property.
    /// </summary>
    /// <param name="propertyBuilder">Property builder.</param>
    /// <param name="collation">MySQL collation to use.</param>
    /// <returns>Property builder with a collation.</returns>
    public static PropertyBuilder ForMySQLHasCollation(
      [NotNull] this PropertyBuilder propertyBuilder,
      [NotNull] string collation)
    {
      propertyBuilder.Metadata.AddAnnotation(MySQLAnnotationNames.Collation, collation);
      return propertyBuilder;
    }

    /// <summary>
    /// Adds the collation to an entity.
    /// </summary>
    /// <param name="entityTypeBuilder">Entity type builder.</param>
    /// <param name="collation">MySQL collation to use.</param>
    /// <returns>Entity type builder with a collation.</returns>
    public static EntityTypeBuilder ForMySQLHasCollation(
      [NotNull] this EntityTypeBuilder entityTypeBuilder,
      [NotNull] string collation)
    {
      entityTypeBuilder.Metadata.AddAnnotation(MySQLAnnotationNames.Collation, collation);
      return entityTypeBuilder;
    }
  }
}
