// Copyright © 2015, 2016 Oracle and/or its affiliates. All rights reserved.
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
using System.Reflection;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Update;
using Microsoft.Data.Entity.ValueGeneration;
using MySQL.Data.Entity.Migrations;
using MySQL.Data.Entity.Update;
using MySQL.Data.Entity.Metadata;
using Microsoft.Data.Entity.Query;
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.Query.ExpressionTranslators;
using Microsoft.Data.Entity.Query.Sql;
using MySQL.Data.Entity.Query;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Query.Internal;

namespace MySQL.Data.Entity
{
  public class MySQLDatabaseProviderServices : RelationalDatabaseProviderServices
  {
    public MySQLDatabaseProviderServices(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override string InvariantName
    {
      get { return GetType().GetTypeInfo().Assembly.GetName().Name; }
    }

    public override IDatabaseCreator Creator
    {
      get {
        return GetService<MySQLDatabaseCreator>(); }
    }

    public override IRelationalConnection RelationalConnection
    {
      get { return GetService<MySQLConnection>(); }
    }

    public override ISqlGenerator SqlGenerator
    {
      get { return GetService<MySQLSqlGenerator>(); }
    }

    //public override IValueGeneratorSelector ValueGeneratorSelector
    //{
    //    get { return GetService<MySQLValueGeneratorSelector>();  }
    //}

    public override IRelationalDatabaseCreator RelationalDatabaseCreator
    {
      get { return GetService<RelationalDatabaseCreator>(); }
    }

    public override IRelationalAnnotationProvider AnnotationProvider
    {
      get { return GetService<MySQLAnnotationProvider>();  }
    }

    public override IMigrationsAnnotationProvider MigrationsAnnotationProvider
    {
      get { return GetService<MySQLMigrationsAnnotationProvider>();  }
    }

    public override IHistoryRepository HistoryRepository
    {
      get { return GetService<MySQLHistoryRepository>(); }
    }

    public override IMigrationsSqlGenerator MigrationsSqlGenerator
    {
      get { return GetService<MySQLMigrationsSqlGenerator>(); }
    }

    public override IUpdateSqlGenerator UpdateSqlGenerator
    {
      get { return GetService<MySQLUpdateSqlGenerator>(); }
    }


    public override IValueGeneratorCache ValueGeneratorCache
    {
      get { return GetService<MySQLValueGeneratorCache>(); }
    }

    public override IModelSource ModelSource => GetService<MySQLModelSource>();

    public override IQueryContextFactory QueryContextFactory => GetService<RelationalQueryContextFactory>();

    public override IRelationalTypeMapper TypeMapper
    {
      get { return GetService<MySQLTypeMapper>(); }
    }

    public override IModificationCommandBatchFactory ModificationCommandBatchFactory
    {
      get { return GetService<MySQLModificationCommandBatchFactory>(); }
    }


    public override IMethodCallTranslator CompositeMethodCallTranslator
    {
      get { return GetService<MySQLCompositeMethodCallTranslator>(); }
    }

    public override IMemberTranslator CompositeMemberTranslator
    {
      get { return GetService<MySQLCompositeMemberTranslator>(); }
    }

    public override IRelationalValueBufferFactoryFactory ValueBufferFactoryFactory
    {
      get { return GetService<UntypedRelationalValueBufferFactoryFactory>(); }
    }

    public override ISqlQueryGeneratorFactory SqlQueryGeneratorFactory
    {
      get { return GetService<MySQLQueryGeneratorFactory>(); }
    }
  }
}