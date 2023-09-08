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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using MySql.EntityFrameworkCore.Extensions;
using MySql.EntityFrameworkCore.Metadata.Internal;

namespace MySql.EntityFrameworkCore.Metadata.Conventions
{
  /// <summary>
  ///   A convention that configures store value generation as <see cref="ValueGenerated.OnAdd"/> on properties that are
  ///   part of the primary key and not part of any foreign keys, are configured to have a database default value, or are 
  ///   configured to use a <see cref="MySQLValueGenerationStrategy"/>.
  ///   It also configures properties as <see cref="ValueGenerated.OnAddOrUpdate"/> if they are configured as computed columns.
  /// </summary>
  internal class MySQLValueGenerationConvention : RelationalValueGenerationConvention
  {
    /// <summary>
    /// Creates a new instance of <see cref="MySQLValueGenerationConvention" />.
    /// </summary>
    /// <param name="dependencies"> Parameter object containing dependencies for this convention. </param>
    /// <param name="relationalDependencies">  Parameter object containing relational dependencies for this convention. </param>
    public MySQLValueGenerationConvention(
      [NotNull] ProviderConventionSetBuilderDependencies dependencies,
      [NotNull] RelationalConventionSetBuilderDependencies relationalDependencies)
      : base(dependencies, relationalDependencies)
    {
    }

    /// <summary>
    /// Called after an annotation is changed on a property.
    /// </summary>
    /// <param name="propertyBuilder"> The builder for the property. </param>
    /// <param name="name"> The annotation name. </param>
    /// <param name="annotation"> The new annotation. </param>
    /// <param name="oldAnnotation"> The old annotation.  </param>
    /// <param name="context"> Additional information associated with convention execution. </param>
    public override void ProcessPropertyAnnotationChanged(
      IConventionPropertyBuilder propertyBuilder,
      string name,
      IConventionAnnotation? annotation,
      IConventionAnnotation? oldAnnotation,
      IConventionContext<IConventionAnnotation> context)
    {
      if (name == MySQLAnnotationNames.ValueGenerationStrategy)
      {
        propertyBuilder.ValueGenerated(GetValueGenerated(propertyBuilder.Metadata));
        return;
      }

      base.ProcessPropertyAnnotationChanged(propertyBuilder, name, annotation, oldAnnotation, context);
    }

    /// <summary>
    /// Indicates the store value generation strategy to set for the given property.
    /// </summary>
    /// <param name="property"> The property. </param>
    /// <returns> The store value generation strategy to set for the given property. </returns>
    protected override ValueGenerated? GetValueGenerated(IConventionProperty property)
    {
      var tableName = property.DeclaringEntityType.GetTableName();
      if (tableName == null)
        return null;

      return GetValueGenerated(property, StoreObjectIdentifier.Table(tableName, property.DeclaringEntityType.GetSchema()));
    }

    /// <summary>
    ///   Returns the store value generation strategy to set for the given property.
    /// </summary>
    /// <param name="property"> The property. </param>
    /// <param name="storeObject"> The identifier of the store object. </param>
    /// <returns> The store value generation strategy to set for the given property. </returns>
    public static new ValueGenerated? GetValueGenerated([NotNull] IReadOnlyProperty property, in StoreObjectIdentifier storeObject)
    {
      var valueGenerated = RelationalValueGenerationConvention.GetValueGenerated(property, storeObject);
      if (valueGenerated != null)
        return valueGenerated;

      var valueGenerationStrategy = property.GetValueGenerationStrategy(storeObject);
      if (valueGenerationStrategy.HasValue)
      {
        switch (valueGenerationStrategy.Value)
        {
          case MySQLValueGenerationStrategy.IdentityColumn:
            return ValueGenerated.OnAdd;
          case MySQLValueGenerationStrategy.ComputedColumn:
            return ValueGenerated.OnAddOrUpdate;
        }
      }

      return null;
    }
  }
}