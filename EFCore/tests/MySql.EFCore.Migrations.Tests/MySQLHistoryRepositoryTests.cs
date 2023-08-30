// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using MySql.EntityFrameworkCore.Extensions;
using NUnit.Framework;

namespace MySql.EntityFrameworkCore.Migrations.Tests
{
  public partial class MySQLHistoryRepositoryTests
  {
    private static IHistoryRepository CreateHistoryRepository()
    {
      var optionsBuilder = new DbContextOptionsBuilder();
      optionsBuilder.UseMySQL(MySQLTestStore.RootConnectionString + "database=test;");

      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<MyTestContext>();

      optionsBuilder.UseInternalServiceProvider(serviceCollection.BuildServiceProvider());

      return new DbContext(optionsBuilder.Options).GetService<IHistoryRepository>();
    }

    [Test]
    public void GetBeginIfNotExistsScript_works()
    {
      var repository = CreateHistoryRepository();
      var script = repository.GetBeginIfNotExistsScript("Migration1");
    }

    [Test]
    public void GetBeginIfExistsScript_works()
    {
      var repository = CreateHistoryRepository();
      var script = repository.GetBeginIfExistsScript("Migration1");
    }

    [Test]
    public void CanCreateDatabase()
    {
      var repository = CreateHistoryRepository();
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
                      .AddDbContext<MyTestContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();
      var context = serviceProvider.GetRequiredService<MyTestContext>();
      var creator = context.GetService<IRelationalDatabaseCreator>();

      var cmdBuilder = context.GetService<IRawSqlCommandBuilder>();
      Assert.False(creator.Exists());
      Assert.AreEqual("IF EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = 'MigrationId')\nBEGIN",
          cmdBuilder.Build(repository.GetBeginIfExistsScript("MigrationId")).CommandText.Replace("\r", string.Empty));
    }
  }
}
