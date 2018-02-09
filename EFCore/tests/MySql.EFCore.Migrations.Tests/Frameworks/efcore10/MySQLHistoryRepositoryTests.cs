// Copyright © 2016, 2017 Oracle and/or its affiliates. All rights reserved.
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
using MySql.EntityFrameworkCore.Migrations.Tests.Utilities;
using MySql.Data.EntityFrameworkCore;
using MySql.Data.EntityFrameworkCore.Infraestructure;
using MySql.Data.EntityFrameworkCore.Metadata;
using MySql.Data.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Migrations.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MySql.Data.EntityFrameworkCore.Extensions;

namespace MySql.EntityFrameworkCore.Migrations.Tests
{
  public partial class MySQLHistoryRepositoryTests
  {
    private static IHistoryRepository CreateHistoryRepository()
    {
      var optionsBuilder = new DbContextOptionsBuilder();
      optionsBuilder.UseMySQL(MySQLTestStore.rootConnectionString + "database=test;");
      var connection = new MySQLServerConnection(optionsBuilder.Options, new Logger<MySQLServerConnection>(new LoggerFactory()));

      var annotationsProvider = new MySQLAnnotationProvider();
      var sqlGenerator = new MySQLSqlGenerationHelper();
      var typeMapper = new MySQLTypeMapper();

      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
                      .AddDbContext<MyTestContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      var context = serviceProvider.GetRequiredService<MyTestContext>();


      var creator = context.GetService<IDatabaseCreator>();
      var cmdBuilder = context.GetService<IRawSqlCommandBuilder>();


      return new MySQLHistoryRepository(
          creator,
          cmdBuilder,
          connection,
          new DbContextOptions<DbContext>(
              new Dictionary<Type, IDbContextOptionsExtension>
              {
                        { typeof(MySQLOptionsExtension), new MySQLOptionsExtension() }
              }),
          new MigrationsModelDiffer(
              new MySQLTypeMapper(),
              annotationsProvider,
              new MySQLMigrationsAnnotationProvider()),
          new MySQLMigrationsSqlGenerator(
              new RelationalCommandBuilderFactory(
                  new FakeSensitiveDataLogger<RelationalCommandBuilderFactory>(),
                  new DiagnosticListener("FakeListener"),
                  typeMapper),
              new MySQLSqlGenerationHelper(),
              typeMapper,
              annotationsProvider),
          annotationsProvider,
          sqlGenerator);

    }
  }
}
