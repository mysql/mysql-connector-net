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
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Extensions;

namespace MySql.EntityFrameworkCore.Metadata.Conventions
{
  /// <summary>
  ///   <para>
  ///     A convention set builder for MySQL.
  ///   </para>
  ///   <para>
  ///     The service lifetime is <see cref="ServiceLifetime.Scoped" /> and multiple registrations
  ///     are allowed. This means that each <see cref="DbContext" /> instance uses its own
  ///     set of instances of this service.
  ///     The implementations may depend on other services registered with any lifetime.
  ///     The implementations do not need to be thread-safe.
  ///   </para>
  /// </summary>
  internal class MySQLConventionSetBuilder : RelationalConventionSetBuilder
  {
    public MySQLConventionSetBuilder(
      [NotNull] ProviderConventionSetBuilderDependencies dependencies,
      [NotNull] RelationalConventionSetBuilderDependencies relationalDependencies)
      : base(dependencies, relationalDependencies)
    {
    }

    /// <summary>
    ///   Builds and returns the convention set for MySQL.
    /// </summary>
    /// <returns> The convention set for MySQL. </returns>
    public override ConventionSet CreateConventionSet()
    {
      var conventionSet = base.CreateConventionSet();

      conventionSet.ModelInitializedConventions.Add(new RelationalMaxIdentifierLengthConvention(64, Dependencies, RelationalDependencies));

      conventionSet.PropertyAddedConventions.Add(new MySqlCharsetAttributeConvention(Dependencies));
      conventionSet.PropertyAddedConventions.Add(new MySqlCollationAttributeConvention(Dependencies));
      #if !NET8_0
      conventionSet.EntityTypeAddedConventions.Add(new MySqlEntityCharsetAttributeConvention(Dependencies));
      conventionSet.EntityTypeAddedConventions.Add(new MySqlEntityCollationAttributeConvention(Dependencies));
      #endif
      ValueGenerationConvention valueGeneratorConvention = new MySQLValueGenerationConvention(Dependencies, RelationalDependencies);
      ReplaceConvention(conventionSet.EntityTypeBaseTypeChangedConventions, valueGeneratorConvention);
      ReplaceConvention(conventionSet.EntityTypePrimaryKeyChangedConventions, valueGeneratorConvention);
      ReplaceConvention(conventionSet.ForeignKeyAddedConventions, valueGeneratorConvention);
      ReplaceConvention(conventionSet.ForeignKeyRemovedConventions, valueGeneratorConvention);

      conventionSet.PropertyAnnotationChangedConventions.Add((MySQLValueGenerationConvention)valueGeneratorConvention);

      return conventionSet;
    }

    /// <summary>
    /// <para>
    ///   Call this method to build a <see cref="ConventionSet" /> for MySQL when using
    ///   the <see cref="ModelBuilder" /> outside of <see cref="DbContext.OnModelCreating" />.
    /// </para>
    /// <para>
    ///   Note that it is unusual to use this method.
    ///   Consider using <see cref="DbContext" /> in the normal way instead.
    /// </para>
    /// </summary>
    /// <returns> The convention set. </returns>
    public static ConventionSet Build()
    {
      var serviceProvider = new ServiceCollection()
        .AddEntityFrameworkMySQL()
        .AddDbContext<DbContext>(o => o.UseMySQL("Server=."))
        .BuildServiceProvider();

      using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
      {
        using (var context = serviceScope.ServiceProvider.GetService<DbContext>())
        {
          return ConventionSet.CreateConventionSet(context!);
        }
      }
    }
  }
}
