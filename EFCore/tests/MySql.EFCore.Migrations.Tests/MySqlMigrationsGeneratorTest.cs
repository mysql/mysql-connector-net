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

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using MySql.Data.EntityFrameworkCore.Storage.Internal;
using MySql.EntityFrameworkCore.Migrations.Tests.Utilities;
using MySql.Data.EntityFrameworkCore;
using MySql.Data.EntityFrameworkCore.Metadata;
using MySql.Data.EntityFrameworkCore.Migrations;
using System.Diagnostics;
using Xunit;

namespace MySql.EntityFrameworkCore.Migrations.Tests
{
  public partial class MySQLMigrationsGeneratorTest : MySQLMigrationsGeneratorTestBase
  {
    [Fact]
    public override void CreateTableOperation()
    {
      base.CreateTableOperation();

      string result =
          "CREATE TABLE `People` (" + EOL +
          "    `Id` int NOT NULL AUTO_INCREMENT," + EOL +
         $"    `EmployerId` int NULL," + EOL +
          "    `SSN` char(11) NULL," + EOL +
          "    PRIMARY KEY (`Id`)," + EOL +
          "    UNIQUE (`SSN`)," + EOL +
          "    FOREIGN KEY (`EmployerId`) REFERENCES `Companies` (`Id`)" + EOL +
          ");" + EOL;
      string fullResult = result.Replace(" NULL,", ",");

      Assert.True(result == Sql || fullResult == Sql);
    }

    [Fact]
    public override void AddColumnOperation_with_maxLength()
    {
      base.AddColumnOperation_with_maxLength();
      string result = "ALTER TABLE `Person` ADD `Name` varchar(30);" + EOL;
      string fullResult = "ALTER TABLE `Person` ADD `Name` varchar(30) NULL;" + EOL;
      Assert.True(result == Sql || fullResult == Sql);
    }

    [Fact]
    public override void AddColumnOperationWithComputedValueSql()
    {
      base.AddColumnOperationWithComputedValueSql();
      Assert.Equal("ALTER TABLE `People` ADD `DisplayName` varchar(50) AS  (CONCAT_WS(' ', LastName , FirstName));" + EOL, Sql);
    }

    [Fact]
    public override void AddColumnOperationWithDefaultValueSql()
    {
      base.AddColumnOperationWithDefaultValueSql();
      Assert.Equal("ALTER TABLE `People` ADD `Timestamp` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP;"  + EOL, Sql);
    }

    [Fact]
    public override void AlterColumnOperation()
    {
      base.AlterColumnOperation();
      Assert.Equal("ALTER TABLE Person MODIFY `Age` int NOT NULL DEFAULT 7;" + EOL, Sql);
    }


    [Fact]
    public override void AlterColumnOperationWithoutType()
    {
      base.AlterColumnOperationWithoutType();
      Assert.Equal("ALTER TABLE Person MODIFY `Age` int NOT NULL;" + EOL, Sql);
    }

    [Fact]
    public override void RenameTableOperationInSchema()
    {
      base.RenameTableOperationInSchema();
      Assert.Equal("ALTER TABLE t1 RENAME t2;" + EOL, Sql);            
    }

    [Fact]
    public override void CreateUniqueIndexOperation()
    {
      base.CreateUniqueIndexOperation();
      Assert.Equal("CREATE UNIQUE INDEX `IXPersonName` ON `Person` (`FirstName`, `LastName`);" + EOL, Sql);
    }

    [Fact]
    public override void CreateNonUniqueIndexOperation()
    {
      base.CreateNonUniqueIndexOperation();
      
      Assert.Equal("CREATE INDEX `IXPersonName` ON `Person` (`Name`);" + EOL, Sql);
    }
    
    [Fact(Skip = "Rename index not supported yet")]
    public override void RenameIndexOperation()
    {
      base.RenameIndexOperation();
      Assert.Equal("DROP INDEX IXPersonName ON Person; CREATE INDEX IXNombre;" + EOL, Sql);
    }
    
    public override void DropIndexOperation()
    {
      base.DropIndexOperation();
      Assert.Equal("DROP INDEX IXPersonName ON Person;" + EOL, Sql);
    }
  }
}
