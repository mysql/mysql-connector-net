// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.EntityFrameworkCore.Infraestructure;
using MySql.Data.EntityFrameworkCore.Metadata.Conventions;
using MySql.Data.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Migrations.Internal;
using MySql.Data.EntityFrameworkCore.Query;
using MySql.Data.EntityFrameworkCore.Query.Internal;
using MySql.Data.EntityFrameworkCore.Storage.Internal;
using MySql.Data.EntityFrameworkCore.Update;

namespace MySql.Data.EntityFrameworkCore.Extensions
{
  /// <summary>
  /// MySQL extension class for <see cref="IServiceCollection" />.
  /// </summary>
  public static class MySQLServiceCollectionExtensions
  {
    /// <summary>
    /// Adds the services required by the MySQL server database provider for Entity Framework
    /// to an <see cref="IServiceCollection"/>.
    /// </summary>
    /// <example>
    ///     <code>
    ///           public void ConfigureServices(IServiceCollection services)
    ///           {
    ///               var connectionString = "MySQL connection string to database";
    /// 
    ///               services
    ///                   .AddEntityFrameworkMySQL()
    ///                   .AddDbContext&lt;MyContext&gt;((serviceProvider, options) =>
    ///                       options.UseMySQL(connectionString)
    ///                              .UseInternalServiceProvider(serviceProvider));
    ///           }
    ///       </code>
    /// </example>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The same service collection to enable a chain of multiple calls.</returns>
    public static IServiceCollection AddEntityFrameworkMySQL([NotNull] this IServiceCollection services)
    {
      var builder = new EntityFrameworkRelationalServicesBuilder(services)
        .TryAdd<IRelationalCommandBuilderFactory, MySQLCommandBuilderFactory>()
        .TryAdd<IDatabaseProvider, DatabaseProvider<MySQLOptionsExtension>>()
        .TryAdd<IValueGeneratorCache, MySQLValueGeneratorCache>()
        .TryAdd<IRelationalTypeMapper, MySQLTypeMapper>()
        .TryAdd<ISqlGenerationHelper, MySQLSqlGenerationHelper>()
        .TryAdd<IModelSource, MySQLModelSource>()
        .TryAdd<IMigrationsAnnotationProvider, MySQLMigrationsAnnotationProvider>()
        .TryAdd<IUpdateSqlGenerator, MySQLUpdateSqlGenerator>()
        .TryAdd<IConventionSetBuilder, MySQLConventionSetBuilder>()
        .TryAdd<IModificationCommandBatchFactory, MySQLModificationCommandBatchFactory>()
        .TryAdd<IRelationalConnection, MySQLServerConnection>()
        .TryAdd<IMigrationsSqlGenerator, MySQLMigrationsSqlGenerator>()
        .TryAdd<IRelationalDatabaseCreator, MySQLDatabaseCreator>()
        .TryAdd<IHistoryRepository, MySQLHistoryRepository>()
        .TryAdd<IQueryCompilationContextFactory, MySQLQueryCompilationContextFactory>()
        .TryAdd<IMemberTranslator, MySQLCompositeMemberTranslator>()
        .TryAdd<ICompositeMethodCallTranslator, MySQLCompositeMethodCallTranslator>()
        .TryAdd<IQuerySqlGeneratorFactory, MySQLQueryGeneratorFactory>();

      builder.TryAddCoreServices();

      return services;
    }
  }
}