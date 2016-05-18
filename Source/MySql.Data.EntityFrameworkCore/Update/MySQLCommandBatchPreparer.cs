using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Relational.Metadata;
using Microsoft.Data.Entity.Relational.Update;
using Microsoft.Data.Entity.Metadata;

namespace MySQL.Data.Entity
{
    public class MySQLCommandBatchPreparer : CommandBatchPreparer
    {
        public MySQLCommandBatchPreparer(
                IModificationCommandBatchFactory modificationCommandBatchFactory,
                IParameterNameGeneratorFactory parameterNameGeneratorFactory,
                IComparer<ModificationCommand> modificationCommandComparer,
                MySQLValueBufferFactoryFactory valueBufferFactoryFactory)
                : base(modificationCommandBatchFactory, parameterNameGeneratorFactory, modificationCommandComparer, valueBufferFactoryFactory)
        {
        }

    }
}
