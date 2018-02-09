// Copyright © 2015, 2017 Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using MySql.Data.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Update;
using MySql.Data.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using MySql.Data.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.Sql;
using MySql.Data.EntityFrameworkCore.Storage.Internal;
using MySql.Data.EntityFrameworkCore.Migrations.Internal;
using MySql.Data.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using MySql.Data.EntityFrameworkCore.Metadata.Conventions;

namespace MySql.Data.EntityFrameworkCore
{
  /// <summary>
  /// RelationalDatabaseProviderServices implementation for MySQL 
  /// </summary>
  internal class MySQLDatabaseProviderServices : RelationalDatabaseProviderServices
  {
    public MySQLDatabaseProviderServices(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override string InvariantName => GetType().GetTypeInfo().Assembly.GetName().Name;    
    public override IDatabaseCreator Creator => GetService<MySQLDatabaseCreator>(); 
    
    public override IRelationalConnection RelationalConnection => GetService<MySQLServerConnection>(); 
    
    public override ISqlGenerationHelper SqlGenerationHelper => GetService<MySQLSqlGenerationHelper>();
       
    public override IRelationalDatabaseCreator RelationalDatabaseCreator => GetService<MySQLDatabaseCreator>(); 
    
    public override IMigrationsAnnotationProvider MigrationsAnnotationProvider => GetService<MySQLMigrationsAnnotationProvider>();     
    public override IHistoryRepository HistoryRepository => GetService<MySQLHistoryRepository>();     

    public override IMigrationsSqlGenerator MigrationsSqlGenerator => GetService<MySQLMigrationsSqlGenerator>();

    public override IModelSource ModelSource => GetService<MySQLModelSource>();
    public override IUpdateSqlGenerator UpdateSqlGenerator => GetService<MySQLUpdateSqlGenerator>();
    
    public override IValueGeneratorCache ValueGeneratorCache => GetService<MySQLValueGeneratorCache>();

    public override IRelationalTypeMapper TypeMapper => GetService<MySQLTypeMapper>();
    public override IModificationCommandBatchFactory ModificationCommandBatchFactory => GetService<MySQLModificationCommandBatchFactory>();
    public override IRelationalAnnotationProvider AnnotationProvider => GetService<MySQLAnnotationProvider>();
    public override IMethodCallTranslator CompositeMethodCallTranslator => GetService<MySQLCompositeMethodCallTranslator>();
    public override IMemberTranslator CompositeMemberTranslator => GetService<MySQLCompositeMemberTranslator>();
    public override IQueryCompilationContextFactory QueryCompilationContextFactory  => GetService<MySQLQueryCompilationContextFactory>();
    public override IQuerySqlGeneratorFactory QuerySqlGeneratorFactory => GetService<MySQLQueryGeneratorFactory>();

    public override IConventionSetBuilder ConventionSetBuilder => GetService<MySQLConventionSetBuilder>();
  }
}