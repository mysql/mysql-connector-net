

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using MySql.EntityFrameworkCore.Migrations.Tests.Utilities;
using MySQL.Data.EntityFrameworkCore;
using MySQL.Data.EntityFrameworkCore.Metadata;
using MySQL.Data.EntityFrameworkCore.Migrations;
using System.Diagnostics;
using Xunit;

namespace MySql.EntityFrameworkCore.Migrations.Tests
{
  public class MySqlMigrationsGeneratorTest : MySqlMigrationsGeneratorTestBase
  {

    protected override IMigrationsSqlGenerator SqlGenerator
    {
      get
      {
        var typeMapper = new MySQLTypeMapper();

        return new MySQLMigrationsSqlGenerator(
            new RelationalCommandBuilderFactory(
                new FakeSensitiveDataLogger<RelationalCommandBuilderFactory>(),
                new DiagnosticListener("FakeListener"),
                typeMapper), new MySQLSqlGenerationHelper(), typeMapper,
            new MySQLAnnotationProvider());
      }
    }

    [Fact]
    public override void CreateTableOperation()
    {
      base.CreateTableOperation();

      Assert.Equal(
          "CREATE TABLE `People` (" + EOL +
          "    `Id` int NOT NULL AUTO_INCREMENT," + EOL +
          "    `EmployerId` int," + EOL +
          "    `SSN` char(11)," + EOL +
          "    PRIMARY KEY (`Id`)," + EOL +
          "    UNIQUE (`SSN`)," + EOL +
          "    FOREIGN KEY (`EmployerId`) REFERENCES `Companies` (`Id`)" + EOL +
          ");" + EOL,
          Sql);
    }

    [Fact]
    public override void AddColumnOperation_with_maxLength()
    {
      base.AddColumnOperation_with_maxLength();
      Assert.Equal("ALTER TABLE `Person` ADD `Name` varchar(30);" + EOL, Sql);
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
      Assert.Equal("CREATE UNIQUE INDEX `IXPersonName` ON Person (`FirstName`, `LastName`);" + EOL, Sql);
    }

    [Fact]
    public override void CreateNonUniqueIndexOperation()
    {
      base.CreateNonUniqueIndexOperation();
      
      Assert.Equal("CREATE INDEX `IXPersonName` ON Person (`Name`);" + EOL, Sql);
    }
    
    [Fact(Skip = "Rename index is only supported in 5.7")]
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
