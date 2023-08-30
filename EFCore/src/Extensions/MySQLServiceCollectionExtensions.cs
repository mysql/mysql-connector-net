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
using MySql.EntityFrameworkCore.Infrastructure;
using MySql.EntityFrameworkCore.Infrastructure.Internal;
using MySql.EntityFrameworkCore.Internal;
using MySql.EntityFrameworkCore.Metadata.Conventions;
using MySql.EntityFrameworkCore.Metadata.Internal;
using MySql.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Migrations.Internal;
using MySql.EntityFrameworkCore.Query;
using MySql.EntityFrameworkCore.Query.Internal;
using MySql.EntityFrameworkCore.Storage.Internal;
using MySql.EntityFrameworkCore.Update;
using MySql.EntityFrameworkCore.Utils;
using MySql.EntityFrameworkCore.ValueGeneration.Internal;
using System;

namespace MySql.EntityFrameworkCore.Extensions
{
  /// <summary>
  ///   MySQL extension class for <see cref="IServiceCollection" />.
  /// </summary>
  public static class MySQLServiceCollectionExtensions
  {
    /// <summary>
    ///   <para>
    ///     Registers the given Entity Framework <see cref="DbContext" /> as a service in the <see cref="IServiceCollection" />
    ///     and configures it to connect to a MySQL database.
    ///   </para>
    ///   <para>
    ///     This method is a shortcut for configuring a <see cref="DbContext" /> to use MySQL.
    ///   </para>
    ///   <para>
    ///     Use this method when using dependency injection in your application, such as with ASP.NET Core.
    ///     For applications that don't use dependency injection, consider creating <see cref="DbContext" />
    ///     instances directly with its constructor. The <see cref="DbContext.OnConfiguring" /> method can then be
    ///     overridden to configure the MySQL provider and connection string.
    ///   </para>
    ///   <para>
    ///     To configure the <see cref="DbContextOptions{TContext}" /> for the context, either override the
    ///     <see cref="DbContext.OnConfiguring" /> method in your derived context, or supply
    ///     an optional action to configure the <see cref="DbContextOptions" /> for the context.
    ///   </para>
    ///   <para>
    ///     See <see href="https://aka.ms/efcore-docs-di">Using DbContext with dependency injection</see> for more information.
    ///   </para>
    /// </summary>
    /// <typeparam name="TContext">The type of context to be registered.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="sqlServerOptionsAction">An optional action to enable additional MySQL server-specific configuration.</param>
    /// <param name="optionsAction">An optional action to configure the <see cref="DbContextOptions" /> for the context.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddMySQLServer<TContext>(
      this IServiceCollection serviceCollection,
      string connectionString,
      Action<MySQLDbContextOptionsBuilder>? sqlServerOptionsAction = null,
      Action<DbContextOptionsBuilder>? optionsAction = null)
      where TContext : DbContext
    {
      Check.NotNull(serviceCollection, nameof(serviceCollection));
      Check.NotEmpty(connectionString, nameof(connectionString));

      return serviceCollection.AddDbContext<TContext>(
        (serviceProvider, options) =>
        {
          optionsAction?.Invoke(options);
          options.UseMySQL(connectionString, sqlServerOptionsAction);
        });
    }

    /// <summary>
    ///   <para>
    ///     Adds the services required by the MySQL database provider for Entity Framework
    ///     to an <see cref="IServiceCollection" />.
    ///   </para>
    ///   <para>
    ///     Warning: Do not call this method by mistake. Instead, it is more likely that you need to call <see cref="AddMySQLServer{TContext}" />.
    ///   </para>
    ///   <para>
    ///     Calling this method is no longer necessary when building most applications, including those that
    ///     use dependency injection in ASP.NET or elsewhere.
    ///     It is only needed when building the internal service provider for use with
    ///     the <see cref="DbContextOptionsBuilder.UseInternalServiceProvider" /> method.
    ///     This is not recommend other than for some advanced scenarios.
    ///   </para>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>
    ///     The same service collection so that multiple calls can be chained.
    /// </returns>
    public static IServiceCollection AddEntityFrameworkMySQL(this IServiceCollection services)
    {
      var builder = new EntityFrameworkRelationalServicesBuilder(services)
        .TryAdd<LoggingDefinitions, MySQLLoggingDefinitions>()
        .TryAdd<IDatabaseProvider, DatabaseProvider<MySQLOptionsExtension>>()
        .TryAdd<IRelationalTypeMappingSource, MySQLTypeMappingSource>()
        .TryAdd<ISqlGenerationHelper, MySQLSqlGenerationHelper>()
        .TryAdd<IRelationalAnnotationProvider, MySQLAnnotationProvider>()
        .TryAdd<IModelValidator, MySQLModelValidator>()
        .TryAdd<IProviderConventionSetBuilder, MySQLConventionSetBuilder>()
        .TryAdd<IUpdateSqlGenerator>(p => p.GetRequiredService<IMySQLUpdateSqlGenerator>())
        .TryAdd<IModificationCommandBatchFactory, MySQLModificationCommandBatchFactory>()
        .TryAdd<IValueGeneratorSelector, MySQLValueGeneratorSelector>()
        .TryAdd<IRelationalConnection>(p => p.GetRequiredService<IMySQLRelationalConnection>())
        .TryAdd<IMigrationsSqlGenerator, MySQLMigrationsSqlGenerator>()
        .TryAdd<IRelationalDatabaseCreator, MySQLDatabaseCreator>()
        .TryAdd<IHistoryRepository, MySQLHistoryRepository>()
        .TryAdd<IExecutionStrategyFactory, MySQLExecutionStrategyFactory>()
        .TryAdd<IRelationalQueryStringFactory, MySQLQueryStringFactory>()
        .TryAdd<ICompiledQueryCacheKeyGenerator, MySQLCompiledQueryCacheKeyGenerator>()
        .TryAdd<IMethodCallTranslatorProvider, MySQLMethodCallTranslatorProvider>()
        .TryAdd<IMemberTranslatorProvider, MySQLMemberTranslatorProvider>()
        .TryAdd<IQuerySqlGeneratorFactory, MySQLQueryGeneratorFactory>()
        .TryAdd<IRelationalSqlTranslatingExpressionVisitorFactory, MySQLSqlTranslatingExpressionVisitorFactory>()
        .TryAdd<IRelationalParameterBasedSqlProcessorFactory, MySQLParameterBasedSqlProcessorFactory>()
        .TryAdd<ISqlExpressionFactory, MySQLSqlExpressionFactory>()
        .TryAdd<ISingletonOptions, IMySQLOptions>(p => p.GetRequiredService<IMySQLOptions>())
        .TryAdd<IQueryCompilationContextFactory, MySQLQueryCompilationContextFactory>()
        .TryAdd<IQueryTranslationPostprocessorFactory, MySQLQueryTranslationPostprocessorFactory>()
        .TryAdd<IMigrationsModelDiffer, MySQLMigrationsModelDiffer>()
        .TryAdd<IMigrator, MySQLMigrator>()
          .TryAddProviderSpecificServices(m => m
          .TryAddSingleton<IMySQLOptions, MySQLOptions>()
          .TryAddScoped<IMySQLRelationalConnection, MySQLRelationalConnection>()
          .TryAddScoped<IMySQLUpdateSqlGenerator, MySQLUpdateSqlGenerator>());

      builder.TryAddCoreServices();

      return services;
    }
  }
}