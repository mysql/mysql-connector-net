using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Entity.Metadata.Builders;
using MySQL.Data.Entity.Metadata;

namespace MySQL.Data.Entity
{
    public static class MySQLBuilderExtensions
    {
        public static MySQLPropertyBuilder ForMySQL(this PropertyBuilder propertyBuilder)
        {
            ThrowIf.Argument.IsNull(propertyBuilder, "propertyBuilder");
            return new MySQLPropertyBuilder(propertyBuilder.Metadata);
        }
    }
}
