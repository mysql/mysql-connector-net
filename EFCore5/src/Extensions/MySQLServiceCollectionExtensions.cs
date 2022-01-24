// Copyright (c) 2020, 2022, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Diagnostics.Internal;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using MySql.EntityFrameworkCore.Internal;
using MySql.EntityFrameworkCore.Metadata.Conventions;
using MySql.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Migrations.Internal;
using MySql.EntityFrameworkCore.Query;
using MySql.EntityFrameworkCore.Query.Internal;
using MySql.EntityFrameworkCore.Storage.Internal;
using MySql.EntityFrameworkCore.Update;
using MySql.EntityFrameworkCore.ValueGeneration.Internal;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  /// MySQL extension class for <see cref="IServiceCollection" />.
  /// </summary>
  public static class MySQLServiceCollectionExtensions
  {
    /// <summary>
    ///     <para>
    ///         Adds the services required by the Microsoft SQL Server database provider for Entity Framework
    ///         to an <see cref="IServiceCollection" />.
    ///     </para>
    ///     <para>
    ///         Calling this method is no longer necessary when building most applications, including those that
    ///         use dependency injection in ASP.NET or elsewhere.
    ///         It is only needed when building the internal service provider for use with
    ///         the <see cref="DbContextOptionsBuilder.UseInternalServiceProvider" /> method.
    ///         This is not recommend other than for some advanced scenarios.
    ///     </para>
    /// </summary>
    /// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
    /// <returns>
    ///     The same service collection so that multiple calls can be chained.
    /// </returns>
    public static IServiceCollection AddEntityFrameworkMySQL([NotNull] this IServiceCollection services)
    {
      var builder = new EntityFrameworkRelationalServicesBuilder(services)
        .TryAdd<LoggingDefinitions, MySQLLoggingDefinitions>()
        .TryAdd<IDatabaseProvider, DatabaseProvider<MySQLOptionsExtension>>()
        .TryAdd<IRelationalTypeMappingSource, MySQLTypeMapper>()
        .TryAdd<ISqlGenerationHelper, MySQLSqlGenerationHelper>()
        .TryAdd<IRelationalAnnotationProvider, MySQLMigrationsAnnotationProvider>()
        .TryAdd<IModelValidator, MySQLModelValidator>()
        .TryAdd<IProviderConventionSetBuilder, MySQLConventionSetBuilder>()
        .TryAdd<IUpdateSqlGenerator>(p => p.GetService<IMySQLUpdateSqlGenerator>())
        .TryAdd<IModificationCommandBatchFactory, MySQLModificationCommandBatchFactory>()
        .TryAdd<IValueGeneratorSelector, MySQLValueGeneratorSelector>()
        .TryAdd<IRelationalConnection>(p => p.GetService<IMySQLServerConnection>())
        .TryAdd<IMigrationsSqlGenerator, MySQLMigrationsSqlGenerator>()
        .TryAdd<IRelationalDatabaseCreator, MySQLDatabaseCreator>()
        .TryAdd<IHistoryRepository, MySQLHistoryRepository>()
        .TryAdd<ICompiledQueryCacheKeyGenerator, MySQLCompiledQueryCacheKeyGenerator>()
        .TryAdd<IExecutionStrategyFactory, MySQLExecutionStrategyFactory>()

        .TryAdd<ISingletonOptions, IMySQLOptions>(p => p.GetService<IMySQLOptions>())

        // New Query Pipeline
        .TryAdd<IMethodCallTranslatorProvider, MySQLMethodCallTranslatorProvider>()
        .TryAdd<IMemberTranslatorProvider, MySQLMemberTranslatorProvider>()
        .TryAdd<IQuerySqlGeneratorFactory, MySQLQueryGeneratorFactory>()
        .TryAdd<ISqlExpressionFactory, MySQLSqlExpressionFactory>()
        .TryAdd<IRelationalSqlTranslatingExpressionVisitorFactory, MySQLSqlTranslatingExpressionVisitorFactory>()
        .TryAdd<IRelationalParameterBasedSqlProcessorFactory, MySQLParameterBasedSqlProcessorFactory>()
        .TryAdd<IQueryTranslationPostprocessorFactory, MySQLQueryTranslationPostprocessorFactory>()
        .TryAdd<IMigrationsModelDiffer, MySQLMigrationsModelDiffer>()
        .TryAddProviderSpecificServices(m => m
            .TryAddSingleton<IMySQLOptions, MySQLOptions>()
            .TryAddScoped<IMySQLServerConnection, MySQLServerConnection>()
            .TryAddSingleton<IMySQLUpdateSqlGenerator, MySQLUpdateSqlGenerator>());

      builder.TryAddCoreServices();

      return services;
    }
  }
}