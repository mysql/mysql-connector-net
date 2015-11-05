using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Storage;

namespace MySQL.Data.Entity
{
    public class MySQLDatabaseProvider : DatabaseProvider<MySQLDatabaseProviderServices, MySQLOptionsExtension>
    {
        public override void AutoConfigure(DbContextOptionsBuilder optionsBuilder)
        {
        }

        public override string Name
        {
            get
            {
                return "MySQL Database";
            }
        }
    }
}
