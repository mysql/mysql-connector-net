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

using System;
using Microsoft.Data.Entity.ChangeTracking.Internal;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Internal;
using Microsoft.Data.Entity.Query;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Update;
using Microsoft.Framework.Logging;

namespace MySQL.Data.Entity
{
  public class MySQLDatabase : RelationalDatabase
  {
    public MySQLDatabase(
        [NotNull] IModel model,
        [NotNull] IEntityKeyFactorySource entityKeyFactorySource,
        [NotNull] IEntityMaterializerSource entityMaterializerSource,
        [NotNull] IClrAccessorSource<IClrPropertyGetter> clrPropertyGetterSource,
        [NotNull] MySQLConnection connection,
        [NotNull] ICommandBatchPreparer batchPreparer,
        [NotNull] IBatchExecutor batchExecutor,
        [NotNull] IDbContextOptions options,
        [NotNull] ILoggerFactory loggerFactory,
        [NotNull] IRelationalValueBufferFactoryFactory valueBufferFactoryFactory,
        [NotNull] IMethodCallTranslator compositeMethodCallTranslator,
        [NotNull] IMemberTranslator compositeMemberTranslator,
        [NotNull] IRelationalTypeMapper typeMapper,
        [NotNull] IRelationalMetadataExtensionProvider relationalExtensions)
        : base(
            model,
            entityKeyFactorySource,
            entityMaterializerSource,
            clrPropertyGetterSource,
            connection,
            batchPreparer,
            batchExecutor,
            options,
            loggerFactory,
            valueBufferFactoryFactory,
            compositeMethodCallTranslator,
            compositeMemberTranslator,
            typeMapper,
            relationalExtensions)
    {
    }

    protected override RelationalQueryCompilationContext CreateQueryCompilationContext(
      ILinqOperatorProvider linqOperatorProvider, 
      IResultOperatorHandler resultOperatorHandler, 
      IQueryMethodProvider queryMethodProvider, 
      IMethodCallTranslator compositeMethodCallTranslator, 
      IMemberTranslator compositeMemberTranslator)
    {
      throw new NotImplementedException();
    }
  }
}