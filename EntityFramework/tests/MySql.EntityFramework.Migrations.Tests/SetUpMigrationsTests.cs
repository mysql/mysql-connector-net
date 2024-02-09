// Copyright © 2013, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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

using MySql.Data.EntityFramework.Properties;
using MySql.Data.EntityFramework.Tests;
using NUnit.Framework;
using System;
using System.Configuration;
using System.Data;
using System.Data.Entity.Migrations;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MySql.Data.EntityFramework.Migrations.Tests
{
  public class SetUpMigrationsTests : DefaultFixture
  {

    private Configuration configuration;
    public DbMigrator Migrator;
    public static string ConnectionStringBlogContext { get; set; }

    [OneTimeSetUp]
    public new void OneTimeSetup()
    {
      ConnectionStringBlogContext = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? MySql.EntityFramework.Migrations.Tests.Properties.Resources.ConnStringMacOS : MySql.EntityFramework.Migrations.Tests.Properties.Resources.ConnString;
      
      configuration = new Configuration();
      DataSet dataSet = ConfigurationManager.GetSection("system.data") as System.Data.DataSet;
      if (dataSet != null)
      {
        DataView vi = dataSet.Tables[0].DefaultView;
        vi.Sort = "Name";
        int idx = -1;
        if (((idx = vi.Find("MySql")) != -1) || ((idx = vi.Find("MySQL Data Provider")) != -1))
        {
          DataRow row = vi[idx].Row;
          dataSet.Tables[0].Rows.Remove(row);
        }
        dataSet.Tables[0].Rows.Add("MySql"
          , "MySql.Data.MySqlClient"
          , "MySql.Data.MySqlClient"
          ,
          typeof(MySql.Data.MySqlClient.MySqlClientFactory).AssemblyQualifiedName);
      }
      Migrator = new DbMigrator(configuration);
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
      using (BlogContext context = new BlogContext())
      {
        if (context.Database.Exists())
        {
          context.Database.Delete();
        }
      }
    }
  }

  internal sealed class Configuration : DbMigrationsConfiguration<BlogContext>
  {
    public Configuration()
    {
      CodeGenerator = new MySqlMigrationCodeGenerator();
      AutomaticMigrationsEnabled = false;
      SetSqlGenerator("MySql.Data.MySqlClient", new MySql.Data.EntityFramework.MySqlMigrationSqlGenerator());
    }

    protected override void Seed(BlogContext context)
    {
    }
  }

  internal sealed class EF6Configuration : DbMigrationsConfiguration<BlogContext>
  {
    public EF6Configuration()
    {
      CodeGenerator = new MySqlMigrationCodeGenerator();
      AutomaticMigrationsEnabled = true;
      SetSqlGenerator("MySql.Data.MySqlClient", new MySql.Data.EntityFramework.MySqlMigrationSqlGenerator());
    }

    protected override void Seed(BlogContext context)
    {
    }
  }
}
