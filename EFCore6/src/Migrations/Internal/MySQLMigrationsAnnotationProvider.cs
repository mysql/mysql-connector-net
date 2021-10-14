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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using MySql.EntityFrameworkCore.Extensions;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using MySql.EntityFrameworkCore.Metadata;
using MySql.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySql.EntityFrameworkCore.Migrations.Internal
{
  /// <summary>
  ///     <para>
  ///         The service lifetime is <see cref="ServiceLifetime.Singleton" />. This means a single instance
  ///         is used by many <see cref="DbContext" /> instances. The implementation must be thread-safe.
  ///         This service cannot depend on services registered as <see cref="ServiceLifetime.Scoped" />.
  ///     </para>
  /// </summary>
  internal class MySQLMigrationsAnnotationProvider : RelationalAnnotationProvider
  {
    [NotNull] private readonly IMySQLOptions _options;
    public MySQLMigrationsAnnotationProvider(
               [NotNull] RelationalAnnotationProviderDependencies dependencies,
               [NotNull] IMySQLOptions options)
               : base(dependencies)
    {
      _options = options;
    }

    /// <summary>
    ///     Initializes a new instance of this class.
    /// </summary>
    /// <param name="dependencies"> Parameter object containing dependencies for this service. </param>
    public MySQLMigrationsAnnotationProvider([NotNull] RelationalAnnotationProviderDependencies dependencies)
  : base(dependencies)
    {
    }

    /// <inheritdoc/>
    public override IEnumerable<IAnnotation> For(IRelationalModel model, bool designTime)
    {
      if (!designTime) return Array.Empty<IAnnotation>();

      return base.For(model, designTime);
    }

    /// <inheritdoc/>
    public override IEnumerable<IAnnotation> For(ITable table, bool designTime)
    {
      if (!designTime) yield break;

      var entityType = table.EntityTypeMappings.First().EntityType;

      foreach (var annotation in entityType.GetAnnotations()
          .Where(a => a.Name is MySQLAnnotationNames.Charset or
                                MySQLAnnotationNames.Collation))
      {
        yield return annotation;
      }
    }

    /// <inheritdoc/>
    public override IEnumerable<IAnnotation> For(ITableIndex index, bool designTime)
    {
      if (!designTime) yield break;

      var modelIndex = index.MappedIndexes.First();
      var isFullText = modelIndex.IsFullText();
      if (isFullText.HasValue)
      {
        yield return new Annotation(
            MySQLAnnotationNames.FullTextIndex,
            isFullText.Value);
      }

      var isSpatial = modelIndex.IsSpatial();
      if (isSpatial.HasValue)
      {
        yield return new Annotation(
            MySQLAnnotationNames.SpatialIndex,
            isSpatial.Value);
      }
    }

    public override IEnumerable<IAnnotation> For(IColumn column, bool designTime)
    {
      if (!designTime) yield break;
      var table = StoreObjectIdentifier.Table(column.Table.Name, column.Table.Schema);
      var properties = column.PropertyMappings.Select(m => m.Property).ToArray();

      if (column.PropertyMappings.Where(
              m => m.TableMapping.IsSharedTablePrincipal &&
                   m.TableMapping.EntityType == m.Property.DeclaringEntityType)
          .Select(m => m.Property)
          .FirstOrDefault(p => p.GetValueGenerationStrategy(table) == MySQLValueGenerationStrategy.IdentityColumn) is IProperty identityProperty)
      {
        var valueGenerationStrategy = identityProperty.GetValueGenerationStrategy(table);
        yield return new Annotation(MySQLAnnotationNames.ValueGenerationStrategy, valueGenerationStrategy);
      }
      else if (properties.FirstOrDefault(p => p.GetValueGenerationStrategy(table) == MySQLValueGenerationStrategy.ComputedColumn) is IProperty computedProperty)
      {
        var valueGenerationStrategy = computedProperty.GetValueGenerationStrategy(table);
        yield return new Annotation(MySQLAnnotationNames.ValueGenerationStrategy, valueGenerationStrategy);
      }
    }
  }
}
