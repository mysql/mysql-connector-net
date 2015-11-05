using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Update;
using Microsoft.Framework.Logging;

namespace MySQL.Data.Entity.Update
{
  public class MySQLBatchExecutor : BatchExecutor
  {
    public MySQLBatchExecutor(
            IRelationalTypeMapper typeMapper,
            DbContext context,
            ILoggerFactory loggerFactory)
            : base(typeMapper, context, loggerFactory)
        {
        }
  }
}
