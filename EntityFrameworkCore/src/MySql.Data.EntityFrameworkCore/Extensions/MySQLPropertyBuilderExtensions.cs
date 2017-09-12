// Copyright © 2016, 2017 Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
    /// Defines a column data type.
    /// </summary>
    /// <param name="propertyBuilder">Entity property to be set.</param>
    /// <param name="typeName">MySQL column type as string.</param>
    /// <returns>Property builder of a MySQL column data type.</returns>
    public static PropertyBuilder ForMySQLHasColumnType(
      [NotNull] this PropertyBuilder propertyBuilder,
      [CanBeNull] string typeName)      
    {

      ThrowIf.Argument.IsNull(propertyBuilder, "propertyBuilder");

      propertyBuilder.Metadata.MySQL().ColumnType = typeName;

      return propertyBuilder;
    }

    /// <summary>
    /// Defines a column default value.
    /// </summary>
    /// <param name="propertyBuilder">Entity property to be set.</param>
    /// <param name="sql">Default value expression.</param>
    /// <returns>Property builder of a MySQL column with a default value.</returns>
    public static PropertyBuilder ForMySQLHasDefaultValue(
            [NotNull] this PropertyBuilder propertyBuilder,
            [CanBeNull] string sql)
    {
      ThrowIf.Argument.IsNull(propertyBuilder, "propertyBuilder");

      if (sql != null && sql.Length == 0)
        ThrowIf.Argument.IsEmpty(sql, "sql");

      propertyBuilder.ValueGeneratedOnAdd();
      //propertyBuilder.Metadata.MySQL().GeneratedValueSql = sql;

      propertyBuilder.Metadata.MySQL().DefaultValueSql = sql;
      return propertyBuilder;

    }
  }
}
