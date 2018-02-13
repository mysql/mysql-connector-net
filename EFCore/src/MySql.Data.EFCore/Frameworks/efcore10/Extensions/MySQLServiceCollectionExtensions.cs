// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.EntityFrameworkCore.Infrastructure;
using MySql.Data.EntityFrameworkCore.Metadata;
using MySql.Data.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.Data.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySql.Data.EntityFrameworkCore.Storage.Internal;
using MySql.Data.EntityFrameworkCore.Migrations.Internal;
using MySql.Data.EntityFrameworkCore.Infraestructure;
using MySql.Data.EntityFrameworkCore.Query.Internal;
using MySql.Data.EntityFrameworkCore.Metadata.Conventions;

namespace MySql.Data.EntityFrameworkCore.Extensions
{
  /// <summary>
  /// MySQL extension class for <see cref="IServiceCollection" />.
  /// </summary>
  public static class MySQLServiceCollectionExtensions
  {
    /// <summary>
    /// Extension method used to configure all MySQL services.
    /// </summary>
    /// <param name="services">Collection of service descriptors.</param>
    /// <returns>Collection of MySQL services descriptors.</returns>
    public static IServiceCollection AddEntityFrameworkMySQL(this IServiceCollection services)
    {
      ThrowIf.Argument.IsNull(services, "services");

      var service = services.AddRelational()
      .AddScoped<IRelationalCommandBuilderFactory, MySQLCommandBuilderFactory>();

      service.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseProvider, DatabaseProvider<MySQLDatabaseProviderServices, MySQLOptionsExtension>>());

      service.TryAdd(new ServiceCollection()
     .AddSingleton<MySQLValueGeneratorCache>()
     .AddSingleton<MySQLTypeMapper>()
     .AddSingleton<MySQLSqlGenerationHelper>()
     .AddSingleton<MySQLModelSource>()
     .AddSingleton<MySQLAnnotationProvider>()
     .AddSingleton<MySQLMigrationsAnnotationProvider>()
     .AddScoped<MySQLConventionSetBuilder>()
     .AddScoped<MySQLUpdateSqlGenerator>()
     .AddScoped<MySQLModificationCommandBatchFactory>()
     .AddScoped<MySQLDatabaseProviderServices>()
     .AddScoped<MySQLServerConnection>()
     .AddScoped<MySQLMigrationsSqlGenerator>()
     .AddScoped<MySQLDatabaseCreator>()
     .AddScoped<MySQLHistoryRepository>()
     .AddQuery());

      return services;
    }

    private static IServiceCollection AddQuery(this IServiceCollection serviceCollection)
    {
      return serviceCollection
              .AddScoped<MySQLQueryCompilationContextFactory>()
              .AddScoped<MySQLCompositeMemberTranslator>()
              .AddScoped<MySQLCompositeMethodCallTranslator>()
              .AddScoped<MySQLQueryGeneratorFactory>();
    }
  }
}