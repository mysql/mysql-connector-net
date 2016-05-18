using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Entity.Relational.Migrations.Infrastructure;
using Microsoft.Data.Entity.Relational;
using Microsoft.Data.Entity.Relational.Metadata;

namespace MySQL.Data.Entity.Migrations
{
    public class MySQLModelDiffer : ModelDiffer
    {
        public MySQLModelDiffer(IRelationalTypeMapper typeMapper, IRelationalMetadataExtensionProvider metadataExtensions)
            : base(typeMapper, metadataExtensions)
        {
        }
    }
}
