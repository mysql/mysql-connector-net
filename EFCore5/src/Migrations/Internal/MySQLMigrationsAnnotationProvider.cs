// Copyright (c) 2020 Oracle and/or its affiliates.
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

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using MySql.EntityFrameworkCore.Metadata.Internal;
using MySql.EntityFrameworkCore.Extensions;
using MySql.EntityFrameworkCore.Metadata;
using System.Linq;


namespace MySql.EntityFrameworkCore.Migrations.Internal
{
  /// <summary>
  ///     <para>
  ///         This is an internal API that supports the Entity Framework Core infrastructure and not subject to
  ///         the same compatibility standards as public APIs. It may be changed or removed without notice in
  ///         any release. You should only use it directly in your code with extreme caution and knowing that
  ///         doing so can result in application failures when updating to a new Entity Framework Core release.
  ///     </para>
  ///     <para>
  ///         The service lifetime is <see cref="ServiceLifetime.Singleton" />. This means a single instance
  ///         is used by many <see cref="DbContext" /> instances. The implementation must be thread-safe.
  ///         This service cannot depend on services registered as <see cref="ServiceLifetime.Scoped" />.
  ///     </para>
  /// </summary>
  internal class MySQLMigrationsAnnotationProvider : RelationalAnnotationProvider
  {
    /// <summary>
    ///     Initializes a new instance of this class.
    /// </summary>
    /// <param name="dependencies"> Parameter object containing dependencies for this service. </param>
    public MySQLMigrationsAnnotationProvider([NotNull] RelationalAnnotationProviderDependencies dependencies)
  : base(dependencies)
    {
    }

    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public override IEnumerable<IAnnotation> For(IRelationalModel model)
    {
      return base.For(model);
    }

    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public override IEnumerable<IAnnotation> For(ITable table)
    {
      return base.For(table);
    }

    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public override IEnumerable<IAnnotation> For(ITableIndex index)
    {
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

    public override IEnumerable<IAnnotation> For(IColumn column)
    {
      var property = column.PropertyMappings.Select(m => m.Property).ToArray().FirstOrDefault();

      if (property.GetValueGenerationStrategy() == MySQLValueGenerationStrategy.IdentityColumn)
      {
        yield return new Annotation(
            MySQLAnnotationNames.ValueGenerationStrategy,
            MySQLValueGenerationStrategy.IdentityColumn);
      }

      if (property.GetValueGenerationStrategy() == MySQLValueGenerationStrategy.ComputedColumn)
      {
        yield return new Annotation(
            MySQLAnnotationNames.ValueGenerationStrategy,
            MySQLValueGenerationStrategy.ComputedColumn);
      }
    }

  }
}
