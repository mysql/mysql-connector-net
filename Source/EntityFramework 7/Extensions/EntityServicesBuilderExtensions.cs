using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Relational;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.ValueGeneration;
using MySQL.Data.Entity;
using MySQL.Data.Entity.Metadata;
using MySQL.Data.Entity.Migrations;
using MySQL.Data.Entity.Update;

namespace Microsoft.Framework.DependencyInjection
{
    public static class EntityServicesBuilderExtensions
    {
        public static EntityFrameworkServicesBuilder AddMySQL(this EntityFrameworkServicesBuilder builder)
        {
            IServiceCollection coll = builder.AddRelational().GetService();
            coll.AddSingleton<IDatabaseProvider, DatabaseProvider<MySQLDatabaseProviderServices, MySQLOptionsExtension>>();
            coll.TryAdd(new ServiceCollection()
                .AddSingleton<MySQLConventionSetBuilder>()
                .AddSingleton<MySQLValueGeneratorCache>()
                .AddSingleton<MySQLUpdateSqlGenerator>()
                .AddSingleton<MySQLTypeMapper>()
                .AddSingleton<MySQLModificationCommandBatchFactory>()
                .AddSingleton<MySQLModelSource>()
                .AddSingleton<MySQLMetadataExtensionProvider>()
                .AddSingleton<MySQLMigrationAnnotationProvider>()
                .AddScoped<MySQLDatabase>()
                .AddScoped<MySQLValueGeneratorSelector>()
                .AddScoped<MySQLDatabaseProviderServices>()
                .AddScoped<MySQLConnection>()
                .AddScoped<MySQLMigrationSqlGenerator>()
                .AddScoped<MySQLHistoryRepository>()
                .AddScoped<MySQLDatabaseCreator>());
            return builder;
        }
    }
}