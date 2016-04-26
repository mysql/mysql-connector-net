

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using MySql.EF7.Migrations.Tests.Utilities;
using MySQL.Data.Entity;
using MySQL.Data.Entity.Metadata;
using MySQL.Data.Entity.Migrations;
using System.Diagnostics;
using Xunit;

namespace MySql.EF7.Migrations.Tests
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


    public override void AddColumnOperation_with_maxLength()
    {
      base.AddColumnOperation_with_maxLength();
      Assert.Equal("", Sql);
    }



  }
}
