using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Relational.Migrations;
using Microsoft.Data.Entity.Relational;
using Microsoft.Framework.Logging;

namespace MySQL.Data.Entity
{
    public class MySQLDatabaseFactory : IDatabaseFactory
    {
        private DbContext context;
        private MySQLDataStoreCreator dataStoreCreator;
        private MySQLConnection connection;
        private IMigrator migrator;
        private ILoggerFactory loggerFactory;

        public MySQLDatabaseFactory()
        {

        }

        public MySQLDatabaseFactory(
            DbContext context,
            MySQLDataStoreCreator dataStoreCreator,
            MySQLConnection connection,
            IMigrator migrator,
            ILoggerFactory loggerFactory)
        {
            //Check.NotNull(context, nameof(context));
            //Check.NotNull(dataStoreCreator, nameof(dataStoreCreator));
            //Check.NotNull(connection, nameof(connection));
            //Check.NotNull(migrator, nameof(migrator));
            //Check.NotNull(loggerFactory, nameof(loggerFactory));

            this.context = context;
            this.dataStoreCreator = dataStoreCreator;
            this.connection = connection;
            this.migrator = migrator;
            this.loggerFactory = loggerFactory;
        }

        public Database CreateDatabase()
        {
            MySQLDatabase d = new MySQLDatabase(context, dataStoreCreator, migrator, connection, loggerFactory);
            return d;
        }
    }
}
