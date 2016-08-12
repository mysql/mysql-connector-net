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

using Microsoft.EntityFrameworkCore.Infrastructure;
using MySQL.Data.EntityFrameworkCore.Metadata;
using MySQL.Data.EntityFrameworkCore.Migrations;
using MySQL.Data.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Storage;
using MySQL.Data.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySql.Data.EntityFrameworkCore.Storage.Internal;
using MySQL.Data.EntityFrameworkCore.Migrations.Internal;
using MySQL.Data.EntityFrameworkCore.Infraestructure.Internal;

namespace MySQL.Data.EntityFrameworkCore
{
    public static class MySQLServiceCollectionExtensions
  {

        /// <summary>
        ///     MySQL specific extension methods for IServiceCollection.
        /// </summary>
        public static IServiceCollection AddEntityFrameworkMySQL(this IServiceCollection services)
        {
            ThrowIf.Argument.IsNull(services, "services");

            var service = services.AddRelational();

            service.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseProvider, DatabaseProvider<MySQLDatabaseProviderServices, MySQLOptionsExtension>>());
      
             service.TryAdd(new ServiceCollection()
            .AddSingleton<MySQLValueGeneratorCache>()
            .AddSingleton<MySQLTypeMapper>()
            .AddSingleton<MySQLSqlGenerationHelper>()
            .AddSingleton<MySQLModelSource>()
            .AddSingleton<MySQLAnnotationProvider>()
            .AddSingleton<MySQLMigrationsAnnotationProvider>()

            //.AddSingleton<MySQLConventionSetBuilder>()
            .AddScoped<MySQLUpdateSqlGenerator>()
            //.AddScoped<MySQLSequenceValueGeneratorFactory, SqlServerSequenceValueGeneratorFactory>()
            .AddScoped<MySQLModificationCommandBatchFactory>()
            //.AddScoped<MySQLValueGeneratorSelector>()
            .AddScoped<MySQLDatabaseProviderServices>()
            .AddScoped<MySQLServerConnection>()
            .AddScoped<MySQLMigrationsSqlGenerator>()
            .AddScoped<MySQLDatabaseCreator>()
            .AddScoped<MySQLHistoryRepository>()
            .AddScoped<MySQLCompositeMethodCallTranslator>()
            .AddScoped<MySQLQueryGeneratorFactory>()
            .AddScoped<MySQLCompositeMemberTranslator>());

      return services;
        }
    }
}