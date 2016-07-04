// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Internal;
using MySQL.Data.Entity.Metadata;

namespace MySQL.Data.Entity
{
  public static class MySQLMetadataExtensions
  {
    public static RelationalEntityTypeAnnotations MySQL(this IEntityType entityType)
    {
      ThrowIf.Argument.IsNull(entityType, "entityType");

      return new RelationalEntityTypeAnnotations(entityType, MySQLAnnotationNames.Prefix);
    }

    public static RelationalEntityTypeAnnotations MySQL([NotNull] this EntityType entityType)
    {
      return (RelationalEntityTypeAnnotations)MySQL((IEntityType)entityType);
    }

    public static RelationalModelAnnotations MySQL([NotNull] this Model model)
    {
      return MySQL((IModel)model);
    }

    public static RelationalModelAnnotations MySQL([NotNull] this IModel model)
    {
      ThrowIf.Argument.IsNull(model, "model");
      return new RelationalModelAnnotations(model, MySQLAnnotationNames.Prefix);
    }

    public static RelationalKeyAnnotations MySQL([NotNull] this Key key)
    {
      return (RelationalKeyAnnotations)MySQL((IKey)key);
    }

    public static RelationalKeyAnnotations MySQL([NotNull] this IKey key)
    {
      ThrowIf.Argument.IsNull(key, "key");
      return new RelationalKeyAnnotations(key, MySQLAnnotationNames.Prefix);
    }

    public static IRelationalForeignKeyAnnotations MySQL([NotNull] this IForeignKey foreignKey)
    {
      ThrowIf.Argument.IsNull(foreignKey, "foreignKey");
      return new RelationalForeignKeyAnnotations(foreignKey, MySQLAnnotationNames.Prefix);
    }

    public static RelationalForeignKeyAnnotations MySQL([NotNull] this ForeignKey foreignKey)
    {
      return (RelationalForeignKeyAnnotations)MySQL((IForeignKey)foreignKey);
    }

    public static RelationalIndexAnnotations MySQL([NotNull] this Index index)
    {
      return (RelationalIndexAnnotations)MySQL((IIndex)index);
    }

    public static IRelationalIndexAnnotations MySQL([NotNull] this IIndex index)
    {
      ThrowIf.Argument.IsNull(index, "index");
      return new RelationalIndexAnnotations(index, MySQLAnnotationNames.Prefix);
    }

    public static MySQLPropertyAnnotations MySQL(this IProperty property)
    {
      ThrowIf.Argument.IsNull(property, "property");

      return new MySQLPropertyAnnotations(property);
    }

    public static MySQLPropertyAnnotations MySQL([NotNull] this Property property)
    {
      return (MySQLPropertyAnnotations)MySQL((IProperty)property);
    }
  }
}
