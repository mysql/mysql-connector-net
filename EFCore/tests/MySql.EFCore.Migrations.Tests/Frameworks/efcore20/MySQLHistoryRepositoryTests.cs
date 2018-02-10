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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.EntityFrameworkCore.Storage.Internal;
using MySql.Data.EntityFrameworkCore.Tests;
using MySql.Data.EntityFrameworkCore;
using MySql.Data.EntityFrameworkCore.Infraestructure;
using MySql.Data.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Migrations.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MySql.Data.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Internal;

namespace MySql.EntityFrameworkCore.Migrations.Tests
{
  public partial class MySQLHistoryRepositoryTests
  {
    private static IHistoryRepository CreateHistoryRepository()
    {
      var optionsBuilder = new DbContextOptionsBuilder();
      optionsBuilder.UseMySQL(MySQLTestStore.rootConnectionString + "database=test;");
      var connection = new MySQLServerConnection(optionsBuilder.Options, new Logger<MySQLServerConnection>(new LoggerFactory()));

      var typeMapper = new MySQLTypeMapper();

      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<MyTestContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      var context = serviceProvider.GetRequiredService<MyTestContext>();

      var creator = context.GetService<IRelationalDatabaseCreator>();
      var cmdBuilder = context.GetService<IRawSqlCommandBuilder>();

      var options = new DbContextOptions<DbContext>(
        new Dictionary<Type, IDbContextOptionsExtension>
        {
          {
            typeof(MySQLOptionsExtension),
            new MySQLOptionsExtension()
          }
        });

      var modelDiffer = new MigrationsModelDiffer(
        new MySQLTypeMapper(),
        new MySQLMigrationsAnnotationProvider(
          new MigrationsAnnotationProviderDependencies()));

      var logger = new DiagnosticsLogger<DbLoggerCategory.Database.Command>(
        new LoggerFactory(),
        new LoggingOptions(),
        new DiagnosticListener("Fake"));

      var commandBuilderFactory = new RelationalCommandBuilderFactory(
        logger,
        typeMapper);

      var sqlGeneratorHelper = new MySQLSqlGenerationHelper(new RelationalSqlGenerationHelperDependencies());

      var migrationsSqlGeneratorDependencies = new MigrationsSqlGeneratorDependencies(
        commandBuilderFactory,
        sqlGeneratorHelper,
        typeMapper);

      var migrationsSqlGeneratior = new MySQLMigrationsSqlGenerator(migrationsSqlGeneratorDependencies);

      HistoryRepositoryDependencies dependencies = new HistoryRepositoryDependencies(
        creator,
        cmdBuilder,
        connection,
        options,
        modelDiffer,
        migrationsSqlGeneratior,
        sqlGeneratorHelper);

      return new MySQLHistoryRepository(dependencies);
    }
  }
}
