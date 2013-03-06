// Copyright © 2011, Oracle and/or its affiliates. All rights reserved.
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
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using MySql.Data.Entity;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using MySql.Data.MySqlClient.Tests;


namespace MySql.Data.Entity.Migrations.Tests
{
  public class BaseMigrationsTests : BaseTest
  {

    private Configuration configuration;        
    
    public DbMigrator Migrator;
        
    [SetUp]
    public override void Setup()
    {
      base.Setup();
      configuration = new Configuration();
      DataSet dataSet = ConfigurationManager.GetSection("system.data") as System.Data.DataSet;
      DataView vi = dataSet.Tables[0].DefaultView;
      vi.Sort = "Name";
      if (vi.Find("MySql") == -1)
      {
        dataSet.Tables[0].Rows.Add("MySql"
          , "MySql.Data.MySqlClient"
          , "MySql.Data.MySqlClient"
          ,
          typeof(MySql.Data.MySqlClient.MySqlClientFactory).AssemblyQualifiedName);
      }
      Migrator = new DbMigrator(configuration);
    }

    [TearDown]
    public override void Teardown()
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
      AutomaticMigrationsEnabled = false;
      SetSqlGenerator("MySql.Data.MySqlClient", new MySql.Data.Entity.MySqlMigrationSqlGenerator());
    }

    protected override void Seed(BlogContext context)
    {
    }
  }
}
