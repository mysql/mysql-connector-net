using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Storage;

namespace MySQL.Data.Entity
{
    public class MySQLDataStoreSource : DataStoreSource<MySQLDataStoreServices, MySQLOptionsExtension>
    {
        public override void AutoConfigure(DbContextOptionsBuilder optionsBuilder)
        {           
        }

        public override string Name
        {
            get { return "MySQL Data Store";  }
        }
    }
}