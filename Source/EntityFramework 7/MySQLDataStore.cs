using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Entity.ChangeTracking.Internal;
using Microsoft.Data.Entity.Query;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Framework.Logging;
using Microsoft.Data.Entity.Relational;
using Microsoft.Data.Entity.Metadata.Internal;
using Microsoft.Data.Entity.Relational.Update;
using MySQL.Data.Entity.Query;
using MySQL.Data.Entity.Update;
using Microsoft.Data.Entity.Relational.Query.Methods;

namespace MySQL.Data.Entity
{
    public class MySQLDataStore : RelationalDataStore
    {
        // pass-through construction (allows dependency injection)
        public MySQLDataStore(
            IModel model,
            IEntityKeyFactorySource entityKeyFactorySource,
            IEntityMaterializerSource entityMaterializerSource,
            IClrAccessorSource<IClrPropertyGetter> clrPropertyGetterSource,
            MySQLConnection connection,
            ICommandBatchPreparer batchPreparer,
            IBatchExecutor batchExecutor,
            IDbContextOptions contextOptions,
            ILoggerFactory loggerFactory,
            IRelationalValueBufferFactoryFactory valueBufferFactoryFactory,
            IMethodCallTranslator compositeMethodCallTranslator,
            IMemberTranslator compositeMemberTranslator,
            IRelationalTypeMapper typeMapper)
            : base(
                model,
                entityKeyFactorySource,
                entityMaterializerSource,
                clrPropertyGetterSource,
                connection,
                batchPreparer,
                batchExecutor,
                contextOptions,
                loggerFactory,
                valueBufferFactoryFactory,
                compositeMethodCallTranslator,
                compositeMemberTranslator,
                typeMapper)
        {

        }



        //Task<int> SaveChangesAsync(IReadOnlyList<StateEntry> stateEntries,
        //                   CancellationToken cancellationToken);

        //IEnumerable<TResult> Query<TResult>(QueryModel queryModel,
        //                                    StateManager stateManager);

        //IAsyncEnumerable<TResult> AsyncQuery<TResult>(QueryModel queryModel,
        //                                              StateManager stateManager);

    }
}