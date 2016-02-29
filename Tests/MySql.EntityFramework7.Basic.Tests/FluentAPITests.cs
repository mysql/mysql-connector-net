// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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

using Microsoft.Extensions.DependencyInjection;
using MySql.Data.Entity.Tests.DbContextClasses;
using MySql.Data.MySqlClient;
using MySQL.Data.Entity;
using System;
using System.Linq;
using Xunit;

namespace MySql.Data.Entity.Tests
{
  public class FluentAPITests : IDisposable
  {

    [Fact]
    public void EnsureRelationalPatterns()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFramework()
        .AddMySQL()
        .AddDbContext<ComputedColumnContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ComputedColumnContext>())
      {
        context.Database.EnsureCreated();

        var e = new Employee { FirstName = "Jos", LastName = "Stuart" };
        context.Employees.Add(e);
        context.SaveChanges();
        var employeeComputedColumn = context.Employees.SingleOrDefault();
        Assert.True(employeeComputedColumn.DisplayName.Equals("Stuart Jos"), "Wrong computed column");
        context.Database.EnsureDeleted();
      }
    }

    [Fact]
    public void CanNameAlternateKey()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFramework()
        .AddMySQL()
        .AddDbContext<KeyConventionsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<KeyConventionsContext>())
      {
        context.Database.EnsureCreated();
        using (var cnn = new MySqlConnection(MySqlTestStore.baseConnectionString))
        {
          cnn.Open();
          var cmd = new MySqlCommand("SELECT DISTINCT table_name, index_name FROM INFORMATION_SCHEMA.STATISTICS where table_name like 'car' and index_name not like 'PRIMARY' ", cnn);
          var reader = cmd.ExecuteReader();
          while (reader.Read())
          {
            Assert.True(reader.GetString(1).ToString().Equals("AlternateKey_LicensePlate"), "Wrong index creation");            
          }
        }
      }
    }



    [Fact]
    public void CanUseToTable()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFramework()
        .AddMySQL()
        .AddDbContext<TableConventionsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<TableConventionsContext>())
      {
        context.Database.EnsureCreated();
        using (var cnn = new MySqlConnection(MySqlTestStore.baseConnectionString))
        {
          cnn.Open();
          var cmd = new MySqlCommand("SELECT table_name FROM INFORMATION_SCHEMA.STATISTICS where table_schema like 'somecars' ", cnn);
          var reader = cmd.ExecuteReader();
          while (reader.Read())
          {
            Assert.True(reader.GetString(0).ToString().Equals("somecars"), "Wrong table name");
          }
        }
      }
    }


    [Fact]
    public void CanUseConcurrency()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFramework()
        .AddMySQL()
        .AddDbContext<ConcurrencyTestsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ConcurrencyTestsContext>())
      {
        context.Database.EnsureCreated();
        using (var cnn = new MySqlConnection(MySqlTestStore.baseConnectionString))
        {
          cnn.Open();
          var cmd = new MySqlCommand("SELECT table_name FROM INFORMATION_SCHEMA.STATISTICS where table_schema like 'somecars' ", cnn);
          var reader = cmd.ExecuteReader();
          while (reader.Read())
          {
            Assert.True(reader.GetString(0).ToString().Equals("somecars"), "Wrong table name");
          }
        }
      }
    }


    [Fact]
    public void CanUseConcurrencyToken()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFramework()
        .AddMySQL()
        .AddDbContext<ComputedColumnContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ComputedColumnContext>())
      {
        context.Database.EnsureCreated();

        var e = new Employee { FirstName = "Jos", LastName = "Stuart" };
        context.Employees.Add(e);
        context.SaveChanges();
        var employeeComputedColumn = context.Employees.SingleOrDefault();
        Assert.True(employeeComputedColumn.DisplayName.Equals("Stuart Jos"), "Wrong computed column");
        context.Database.EnsureDeleted();
      }
    }


    public void Dispose()
    {
      // ensure database deletion
      using (var cnn = new MySqlConnection(MySqlTestStore.baseConnectionString))
      {
        cnn.Open();
        var cmd = new MySqlCommand("DROP DATABASE IF EXISTS test", cnn);
        cmd.ExecuteNonQuery();        
      }
    }
  }
}
