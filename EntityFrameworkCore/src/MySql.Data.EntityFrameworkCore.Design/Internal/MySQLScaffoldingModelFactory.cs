// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using Microsoft.EntityFrameworkCore.Scaffolding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using MySql.Data.EntityFrameworkCore.Extensions;
using MySql.Data.EntityFrameworkCore.Design.Metadata.Internal;
using Microsoft.EntityFrameworkCore;

namespace MySql.Data.EntityFrameworkCore.Design.Internal
{
    internal class MySQLScaffoldingModelFactory : RelationalScaffoldingModelFactory
    {
        public MySQLScaffoldingModelFactory(ILoggerFactory loggerFactory, IRelationalTypeMapper typeMapper, IDatabaseModelFactory databaseModelFactory, CandidateNamingService candidateNamingService) 
                : base(loggerFactory, typeMapper, databaseModelFactory, candidateNamingService)
        {
        }

        public override IModel Create(string connectionString, TableSelectionSet tableSelectionSet)
        {
            var model = base.Create(connectionString, tableSelectionSet);
            model.Scaffolding().UseProviderMethodName = nameof(MySQLDbContextOptionsExtensions.UseMySQL);
            return model;
        }

        protected override PropertyBuilder VisitColumn(EntityTypeBuilder builder, ColumnModel column)
        {
            return base.VisitColumn(builder, column);
        }

        protected override RelationalTypeMapping GetTypeMapping(ColumnModel column)
        {
            return base.GetTypeMapping(column);
        }

        protected override KeyBuilder VisitPrimaryKey(EntityTypeBuilder builder, TableModel table)
        {

            var keyBuilder = base.VisitPrimaryKey(builder, table);

            if (keyBuilder == null)
            {
                return null;
            }
            return keyBuilder;
        }

        protected override IndexBuilder VisitIndex(EntityTypeBuilder builder, IndexModel index)
        {
            //var expression = index.MySQL().Expression;
            //if (expression != null)
            //{
            //    Logger.LogWarning($"Ignoring unsupported index {index.Name} with expression ({expression})");
            //    return null;
            //}

            return base.VisitIndex(builder, index);
        }


    }
}
