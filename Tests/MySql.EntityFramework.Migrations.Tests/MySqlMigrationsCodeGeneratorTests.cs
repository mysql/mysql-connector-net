// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using System.Data.Metadata.Edm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MySql.Data.Entity.Migrations.Tests
{
  public class MySqlMigrationsCodeGeneratorTests
  {
    [Test]
    public void CanGenerateAddColumnMigration()
    { 
      var migration = new MySqlMigrationCodeGenerator().Generate("MigrationCodeGenerationTest",
                        new[]
                            {
                                new AddColumnOperation(
                                    "TestTable",
                                    new ColumnModel(PrimitiveTypeKind.DateTime){ Name = "MyDateTime", Precision = 3 })
                            },
                        "SourceModel", "TargetModel", "MyTestNamespace", "MyClass");
      Assert.AreEqual(@"namespace MyTestNamespace
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MyClass : DbMigration
    {
        public override void Up()
        {
            AddColumn(""TestTable"", ""MyDateTime"", c => c.DateTime(precision: 3));
        }
        
        public override void Down()
        {
            DropColumn(""TestTable"", ""MyDateTime"");
        }
    }
}
", migration.UserCode);    
    }

    [Test]
    public void CanGenerateAddColumnForGeometryType()
    { 
      var migration = new MySqlMigrationCodeGenerator().Generate("MigrationCodeGenerationTest",
        new []
        {
          new AddColumnOperation("TestTable", new ColumnModel(PrimitiveTypeKind.Geometry){ Name = "MyGeometryColumn" })        
        }, "SourceModel", "TargetModel","MyTestNamespace", "MyClass");

      Assert.AreEqual(@"namespace MyTestNamespace
{
    using System;
    using System.Data.Entity.Migrations;
    using System.Data.Spatial;
    
    public partial class MyClass : DbMigration
    {
        public override void Up()
        {
            AddColumn(""TestTable"", ""MyGeometryColumn"", c => c.Geometry());
        }
        
        public override void Down()
        {
            DropColumn(""TestTable"", ""MyGeometryColumn"");
        }
    }
}
", migration.UserCode);
    }

    [Test]
    public void CanGenerateCreateTableCode()
    {
      var createTable = new CreateTableOperation("TestTable");
      var columns = new ColumnModel[3];

      columns[0] = new ColumnModel(PrimitiveTypeKind.Int32) { Name = "IdColumn", IsIdentity = true };
      columns[1] = new ColumnModel(PrimitiveTypeKind.String) { Name = "StringColumn", IsNullable = false};
      columns[2] = new ColumnModel(PrimitiveTypeKind.Decimal) { Name = "DecimalColumn", Precision = 5, Scale = 2 };

      createTable.Columns.Add(columns[0]);
      createTable.Columns.Add(columns[1]);
      createTable.Columns.Add(columns[2]);
      
      createTable.PrimaryKey = new AddPrimaryKeyOperation { Name = "PKTestTable" };
      createTable.PrimaryKey.Columns.Add("IdColumn");
      
      var migration = new MySqlMigrationCodeGenerator().Generate("MigrationCodeGenerationTest", new [] {createTable}, "SourceModel", "TargetModel", "MyTestNamespace", "MyClass");

      Assert.AreEqual(@"namespace MyTestNamespace
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MyClass : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                ""TestTable"",
                c => new
                    {
                        IdColumn = c.Int(identity: true),
                        StringColumn = c.String(nullable: false),
                        DecimalColumn = c.Decimal(precision: 5, scale: 2),
                    })
                .PrimaryKey(t => t.IdColumn, name: ""PKTestTable"");
            
        }
        
        public override void Down()
        {
            DropTable(""TestTable"");
        }
    }
}
", migration.UserCode);
    }

    [Test]
    public void CanGenerateForeignKeyCode()
    {
      var fkOperation = new AddForeignKeyOperation();

      fkOperation.Name = "MyForeignKeyTest";
      fkOperation.PrincipalTable = "PrincipalTableTest";
      fkOperation.DependentTable = "DependentTableTest";
      fkOperation.PrincipalColumns.Add("IdColumn");
      fkOperation.DependentColumns.Add("IdDependentColumn");
      fkOperation.CascadeDelete = true;
      
       var migration = new MySqlMigrationCodeGenerator().Generate("MigrationCodeGenerationTest", new [] {fkOperation}, "SourceModel", "TargetModel", "MyTestNamespace", "MyClass");

       Assert.AreEqual(@"namespace MyTestNamespace
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MyClass : DbMigration
    {
        public override void Up()
        {
            AddForeignKey(""DependentTableTest"", ""IdDependentColumn"", ""PrincipalTableTest"", ""IdColumn"", cascadeDelete: true, name: ""MyForeignKeyTest"");
        }
        
        public override void Down()
        {
            DropForeignKey(""DependentTableTest"", ""MyForeignKeyTest"");
        }
    }
}
", migration.UserCode);
    }
  }
}
