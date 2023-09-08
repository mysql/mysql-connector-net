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
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using MySql.EntityFrameworkCore.Extensions;
using NUnit.Framework;

namespace MySql.EntityFrameworkCore.Migrations.Tests
{
  public class MySQLMigrationsGeneratorTest : MySQLMigrationsGeneratorTestBase
  {
    protected override IMigrationsSqlGenerator SqlGenerator
    {
      get
      {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseMySQL(MySQLTestStore.RootConnectionString + "database=test;");

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEntityFrameworkMySQL()
          .AddDbContext<MyTestContext>();

        optionsBuilder.UseInternalServiceProvider(serviceCollection.BuildServiceProvider());

        return new DbContext(optionsBuilder.Options).GetService<IMigrationsSqlGenerator>();
      }
    }

    [Test]
    public override void CreateTableOperation()
    {
      base.CreateTableOperation();

      string result =
          "CREATE TABLE `People` (" + EOL +
          "    `Id` int NOT NULL AUTO_INCREMENT," + EOL +
          "    `EmployerId` int NULL," + EOL +
          "    `SSN` char(11) NULL," + EOL +
          "    PRIMARY KEY (`Id`)," + EOL +
          "    UNIQUE (`SSN`)," + EOL +
          "    FOREIGN KEY (`EmployerId`) REFERENCES `Companies` (`Id`)" + EOL +
          ");" + EOL;
      string fullResult = result.Replace(" NULL,", ",");

      Assert.True(result == Sql || fullResult == Sql);
    }

    [Test]
    public override void AddColumnOperation_with_maxLength()
    {
      base.AddColumnOperation_with_maxLength();
      string result = "ALTER TABLE `Person` ADD `Name` varchar(30);" + EOL;
      string fullResult = "ALTER TABLE `Person` ADD `Name` varchar(30) NULL;" + EOL;
      Assert.True(result == Sql || fullResult == Sql);
    }

    [Test]
    public override void AddColumnOperationWithComputedValueSql()
    {
      base.AddColumnOperationWithComputedValueSql();
      Assert.AreEqual("ALTER TABLE `People` ADD `DisplayName` varchar(50) AS (CONCAT_WS(' ', LastName , FirstName));" + EOL, Sql);
    }

    [Test]
    public override void AddColumnOperationWithDefaultValueSql()
    {
      base.AddColumnOperationWithDefaultValueSql();
      Assert.AreEqual("ALTER TABLE `People` ADD `Timestamp` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP;" + EOL, Sql);
    }

    [Test]
    public override void AlterColumnOperation()
    {
      base.AlterColumnOperation();
      Assert.AreEqual("ALTER TABLE `Person` MODIFY `Age` int NOT NULL DEFAULT 7;" + EOL, Sql);
    }


    [Test]
    public override void AlterColumnOperationWithoutType()
    {
      base.AlterColumnOperationWithoutType();
      Assert.AreEqual("ALTER TABLE `Person` MODIFY `Age` int NOT NULL;" + EOL, Sql);
    }

    [Test]
    public override void RenameTableOperationInSchema()
    {
      base.RenameTableOperationInSchema();
      Assert.AreEqual("ALTER TABLE t1 RENAME t2;" + EOL, Sql);
    }

    [Test]
    public override void CreateUniqueIndexOperation()
    {
      base.CreateUniqueIndexOperation();
      Assert.AreEqual("CREATE UNIQUE INDEX `IXPersonName` ON `Person` (`FirstName`, `LastName`);" + EOL, Sql);
    }

    [Test]
    public override void CreateNonUniqueIndexOperation()
    {
      base.CreateNonUniqueIndexOperation();

      Assert.AreEqual("CREATE INDEX `IXPersonName` ON `Person` (`Name`);" + EOL, Sql);
    }

    [Test]
    [Ignore("Rename index not supported yet")]
    public override void RenameIndexOperation()
    {
      base.RenameIndexOperation();
      Assert.AreEqual("DROP INDEX IXPersonName ON Person; CREATE INDEX IXNombre;" + EOL, Sql);
    }

    [Test]
    public override void DropIndexOperation()
    {
      base.DropIndexOperation();
      Assert.AreEqual("DROP INDEX IXPersonName ON Person;" + EOL, Sql);
    }

    [Test]
    public override void DropPrimaryKeyOperation()
    {
      base.DropPrimaryKeyOperation();
      Assert.AreEqual(string.Empty, Sql);
    }

    [Test]
    public override void AddPrimaryKeyOperation()
    {
      base.AddPrimaryKeyOperation();
      Assert.AreEqual(string.Empty, Sql);
    }
  }
}
