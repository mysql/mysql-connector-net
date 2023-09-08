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

using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using MySql.EntityFrameworkCore.Internal;
using MySql.EntityFrameworkCore.Metadata;
using System.Collections.Generic;
using System.Linq;

namespace MySql.EntityFrameworkCore.Migrations.Internal
{
  
  internal class MySQLMigrationsModelDiffer : MigrationsModelDiffer
  {
#if NET6_0
    public MySQLMigrationsModelDiffer(
          [NotNull] IRelationalTypeMappingSource typeMappingSource,
          [NotNull] IMigrationsAnnotationProvider migrationsAnnotations,
          [NotNull] IChangeDetector changeDetector,
          [NotNull] IUpdateAdapterFactory updateAdapterFactory,
          [NotNull] CommandBatchPreparerDependencies commandBatchPreparerDependencies)
          : base(
            typeMappingSource,
            migrationsAnnotations,
            changeDetector,
            updateAdapterFactory,
            commandBatchPreparerDependencies)
    {
    }

    protected override IEnumerable<MigrationOperation> Add(IColumn target, DiffContext diffContext, bool inline = false)
    {
      var _property = target.PropertyMappings.ToArray().FirstOrDefault()!.Property;
      if (_property.FindTypeMapping() is RelationalTypeMapping storeType)
      {
        var valueGenerationStrategy = MySQLValueGenerationStrategyCompatibility.GetValueGenerationStrategy(MigrationsAnnotations.ForRemove(target).ToArray());
        // Ensure that null will be set for the columns default value, if CURRENT_TIMESTAMP has been required,
        // or when the store type of the column does not support default values at all.
        inline = inline ||
             (storeType.StoreTypeNameBase == "datetime" ||
              storeType.StoreTypeNameBase == "timestamp") &&
             (valueGenerationStrategy == MySQLValueGenerationStrategy.IdentityColumn ||
              valueGenerationStrategy == MySQLValueGenerationStrategy.ComputedColumn) ||
             storeType.StoreTypeNameBase.Contains("text") ||
             storeType.StoreTypeNameBase.Contains("blob") ||
             storeType.StoreTypeNameBase == "geometry" ||
             storeType.StoreTypeNameBase == "json";
      }
      return base.Add(target, diffContext, inline);
    }

#elif NET7_0 || NET8_0
    public MySQLMigrationsModelDiffer(
      [NotNull] IRelationalTypeMappingSource typeMappingSource,
      [NotNull] IMigrationsAnnotationProvider migrationsAnnotations,
      [NotNull] IRowIdentityMapFactory rowIdentityMapFactory,
      [NotNull] CommandBatchPreparerDependencies commandBatchPreparerDependencies)
      : base(
          typeMappingSource,
          migrationsAnnotations,
          rowIdentityMapFactory,
          commandBatchPreparerDependencies)
    {
    }

    protected override IEnumerable<MigrationOperation> Add(IColumn target, DiffContext diffContext, bool inline = false)
    {
      var _property = target.PropertyMappings.ToArray().FirstOrDefault()!.Property;
      if (_property.FindTypeMapping() is RelationalTypeMapping storeType)
      {
        var valueGenerationStrategy = MySQLValueGenerationStrategyCompatibility.GetValueGenerationStrategy(MigrationsAnnotationProvider.ForRemove(target).ToArray());
        // Ensure that null will be set for the columns default value, if CURRENT_TIMESTAMP has been required,
        // or when the store type of the column does not support default values at all.
        inline = inline ||
                 (storeType.StoreTypeNameBase == "datetime" ||
                  storeType.StoreTypeNameBase == "timestamp") &&
                 (valueGenerationStrategy == MySQLValueGenerationStrategy.IdentityColumn ||
                  valueGenerationStrategy == MySQLValueGenerationStrategy.ComputedColumn) ||
                 storeType.StoreTypeNameBase.Contains("text") ||
                 storeType.StoreTypeNameBase.Contains("blob") ||
                 storeType.StoreTypeNameBase == "geometry" ||
                 storeType.StoreTypeNameBase == "json";
      }
      return base.Add(target, diffContext, inline);
    }
#endif


  }
}