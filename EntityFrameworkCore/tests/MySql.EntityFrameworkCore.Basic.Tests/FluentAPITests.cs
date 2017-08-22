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
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.EntityFrameworkCore.Tests.DbContextClasses;
using MySql.Data.MySqlClient;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MySql.Data.EntityFrameworkCore.Tests
{
  public class FluentAPITests : IDisposable
  {

    [FactOnVersions("5.7.0", null)]
    public void EnsureRelationalPatterns()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<ComputedColumnContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ComputedColumnContext>())
      {
        context.Database.EnsureCreated();

        var e = new Employee { FirstName = "Jos", LastName = "Stuart" };
        context.Employees.Add(e);
        context.SaveChanges();
        var employeeComputedColumn = context.Employees.FirstOrDefault();
        Assert.True(employeeComputedColumn.DisplayName.Equals("Stuart Jos"), "Wrong computed column");
        context.Database.EnsureDeleted();
      }
    }



    [Fact]
    public void CanUseModelWithDateTimeOffset()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
      .AddDbContext<QuickContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<QuickContext>())
      {
        try
        {
          context.Database.EnsureCreated();
          var dt = DateTime.Now;
          var e = new QuickEntity { Name = "Jos", Created = dt };
          context.QuickEntity.Add(e);
          context.SaveChanges();
          var row = context.QuickEntity.FirstOrDefault();
          Assert.Equal(dt, row.Created);
        }
        catch (Exception)
        {
          throw;
        }
        finally
        {
          context.Database.EnsureDeleted();
        }
      }
    }


    [Fact]
    public async Task CanUseModelWithDateTimeOffsetAsync()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
      .AddDbContext<QuickContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<QuickContext>())
      {
        try
        {
          context.Database.EnsureCreated();
          var dt = DateTime.Now;
          var e = new QuickEntity { Name = "Jos", Created = dt };
          context.QuickEntity.Add(e);
          context.SaveChanges();
          var result = await context.QuickEntity.FirstOrDefaultAsync();
          Assert.Equal(dt, result.Created);
        }
        catch (Exception)
        {
          throw;
        }
        finally
        {
          context.Database.EnsureDeleted();
        }
      }
    }


    [Fact]
    public void CanNameAlternateKey()
    {
      var serviceCollection = new ServiceCollection();

      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<KeyConventionsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<KeyConventionsContext>())
      {
        context.Database.EnsureCreated();
        using (var cnn = new MySqlConnection(MySQLTestStore.baseConnectionString))
        {
          cnn.Open();
          var cmd = new MySqlCommand("SELECT DISTINCT table_name, index_name FROM INFORMATION_SCHEMA.STATISTICS where table_name like 'cars' and index_name not like 'PRIMARY' ", cnn);
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
      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<TableConventionsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<TableConventionsContext>())
      {
        context.Database.EnsureCreated();
        using (var cnn = new MySqlConnection(MySQLTestStore.baseConnectionString))
        {
          cnn.Open();
          var cmd = new MySqlCommand("SELECT table_name FROM INFORMATION_SCHEMA.STATISTICS where table_name like 'somecars' ", cnn);
          var reader = cmd.ExecuteReader();
          while (reader.Read())
          {
            Assert.True(reader.GetString(0).ToString().Equals("somecars"), "Wrong table name");
          }
        }
        context.Database.EnsureDeleted();
      }
    }


    [Fact]
    public void CanUseConcurrency()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<ConcurrencyTestsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ConcurrencyTestsContext>())
      {
        context.Database.EnsureCreated();
        using (var cnn = new MySqlConnection(MySQLTestStore.baseConnectionString))
        {
          cnn.Open();
          var cmd = new MySqlCommand("SELECT table_name FROM INFORMATION_SCHEMA.STATISTICS where table_schema like 'somecars' ", cnn);
          var reader = cmd.ExecuteReader();
          while (reader.Read())
          {
            Assert.True(reader.GetString(0).ToString().Equals("somecars"), "Wrong table name");
          }
        }
        context.Database.EnsureDeleted();
      }
    }


    [FactOnVersions("5.7.0", null)]
    public void CanUseConcurrencyToken()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
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

    [FactOnVersions("5.7.0", null)]
    public void CanUseContainsInQuery()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<ComputedColumnContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ComputedColumnContext>())
      {
        context.Database.EnsureCreated();
        var e = new Employee { FirstName = "Jos", LastName = "Stuart" };
        context.Employees.Add(e);
        context.SaveChanges();
        var result = context.Employees.Where(t => t.FirstName.Contains("jo")).ToList();
        Assert.Equal(1, result.Count);
        context.Database.EnsureDeleted();
      }
    }

    [FactOnVersions("5.7.0", null)]
    public void CanUseContainsVarInQuery()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<ComputedColumnContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ComputedColumnContext>())
      {
        context.Database.EnsureCreated();
        var e = new Employee { FirstName = "Jos", LastName = "Stuart" };
        context.Employees.Add(e);
        context.SaveChanges();
        var test = "jo";
        var result = context.Employees.Where(t => t.FirstName.Contains(test)).ToList();
        Assert.Equal(1, result.Count);
        context.Database.EnsureDeleted();
      }
    }


    [FactOnVersions("5.7.0", null)]
    public void CanUseContainsWithInvalidValue()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<ComputedColumnContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ComputedColumnContext>())
      {
        context.Database.EnsureCreated();
        var e = new Employee { FirstName = "Jos", LastName = "Stuart" };
        context.Employees.Add(e);
        context.SaveChanges();
        var result = context.Employees.Where(t => t.FirstName.Contains("XXXXXXXX$%^&*()!")).ToList();
        Assert.Equal(0, result.Count);
        result = context.Employees.Where(t => t.FirstName.Contains(null)).ToList();
        Assert.Equal(0, result.Count);
        context.Database.EnsureDeleted();
      }
    }

    [FactOnVersions("5.7.0", null)]
    public void CanUseContainsWithVariableInQuery()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<ComputedColumnContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ComputedColumnContext>())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        var e = new Employee { FirstName = "Jos", LastName = "Stuart" };
        context.Employees.Add(e);
        context.SaveChanges();
        var avalue = "jo";
        var result = context.Employees.Where(t => t.FirstName.Contains(avalue)).ToList();
        Assert.Equal(1, result.Count);
        context.Database.EnsureDeleted();
      }
    }

    [Fact]
    public void TableAttributeTest()
    {
      using (WorldContext context = new WorldContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        using (MySqlConnection conn = new MySqlConnection(context.Database.GetDbConnection().ConnectionString))
        {
          conn.Open();
          MySqlCommand cmd = new MySqlCommand("SHOW TABLES", conn);
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            Assert.True(reader.Read());
            Assert.Equal("continentlist", reader.GetString(0)
              , ignoreCase: true, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
            Assert.True(reader.Read());
            Assert.Equal("countrylist", reader.GetString(0)
              , ignoreCase: true, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
          }

        }
      }
    }

    public void Dispose()
    {
      // ensure database deletion
      using (var cnn = new MySqlConnection(MySQLTestStore.baseConnectionString))
      {
        cnn.Open();
        var cmd = new MySqlCommand("DROP DATABASE IF EXISTS test", cnn);
        cmd.ExecuteNonQuery();
      }
    }
  }
}
