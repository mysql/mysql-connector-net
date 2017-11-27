// Copyright © 2015, 2017 Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MySql.Data.EntityFrameworkCore.Metadata;
using MySql.Data.EntityFrameworkCore.Metadata.Internal;

namespace MySql.Data.EntityFrameworkCore
{
  internal static class MySQLMetadataExtensions
  {
    public static RelationalEntityTypeAnnotations MySQL(this IEntityType entityType)
    {
      ThrowIf.Argument.IsNull(entityType, "entityType");

      return new RelationalEntityTypeAnnotations(entityType, MySQLFullAnnotationNames.Instance);
    }

    public static RelationalEntityTypeAnnotations MySQL(this EntityType entityType)
    {
      return (RelationalEntityTypeAnnotations)MySQL((IEntityType)entityType);
    }

    public static RelationalModelAnnotations MySQL(this Model model)
    {
      return MySQL((IModel)model);
    }

    public static RelationalModelAnnotations MySQL(this IModel model)
    {
      ThrowIf.Argument.IsNull(model, "model");
      return new RelationalModelAnnotations(model, MySQLFullAnnotationNames.Instance);
    }

    public static RelationalKeyAnnotations MySQL(this Key key)
    {
      return (RelationalKeyAnnotations)MySQL((IKey)key);
    }

    public static RelationalKeyAnnotations MySQL(this IKey key)
    {
      ThrowIf.Argument.IsNull(key, "key");
      return new RelationalKeyAnnotations(key, MySQLFullAnnotationNames.Instance);
    }

    public static IRelationalForeignKeyAnnotations MySQL(this IForeignKey foreignKey)
    {
      ThrowIf.Argument.IsNull(foreignKey, "foreignKey");
      return new RelationalForeignKeyAnnotations(foreignKey, MySQLFullAnnotationNames.Instance);
    }

    public static RelationalForeignKeyAnnotations MySQL(this ForeignKey foreignKey)
    {
      return (RelationalForeignKeyAnnotations)MySQL((IForeignKey)foreignKey);
    }

    public static RelationalIndexAnnotations MySQL(this Index index)
    {
      return (RelationalIndexAnnotations)MySQL((IIndex)index);
    }

    public static IRelationalIndexAnnotations MySQL(this IIndex index)
    {
      ThrowIf.Argument.IsNull(index, "index");
      return new RelationalIndexAnnotations(index, MySQLFullAnnotationNames.Instance);
    }

    public static MySQLPropertyAnnotations MySQL(this IProperty property)
    {
      ThrowIf.Argument.IsNull(property, "property");

      return new MySQLPropertyAnnotations(property);
    }

    public static MySQLPropertyAnnotations MySQL(this Property property)
    {
      return (MySQLPropertyAnnotations)MySQL((IProperty)property);
    }
  }
}
