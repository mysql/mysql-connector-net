// Copyright © 2016, 2017, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.EntityFrameworkCore.Extensions;
using MySql.Data.EntityFrameworkCore.Tests.DbContextClasses;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
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

    [Fact]
    public void CharsetTest()
    {
      using (CharsetTestContext context = new CharsetTestContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        using (MySqlConnection conn = (MySqlConnection)context.Database.GetDbConnection())
        {
          conn.Open();
          MySqlCommand cmd = conn.CreateCommand();
          cmd.CommandText = "SHOW CREATE TABLE `TestCharsetDA`";
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            reader.Read();
            string createTable = reader.GetString(1);
            Assert.Equal(@"CREATE TABLE `testcharsetda` (
  `TestCharsetDAId` varbinary(255) NOT NULL,
  PRIMARY KEY (`TestCharsetDAId`)
) ENGINE=InnoDB DEFAULT CHARSET=ascii",
              createTable, true, true, true);
          }

          cmd.CommandText = "SHOW CREATE TABLE `TestCharsetFA`";
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            reader.Read();
            string createTable = reader.GetString(1);

            //Adding "COLLATION" to the string validation at table creation (this happens since MySql 8.0.5 Server)
            if (createTable.Contains("COLLATE latin7_general_ci")) Assert.Equal(@"CREATE TABLE `testcharsetfa` (
  `TestCharsetFAId` varchar(255) CHARACTER SET latin7 COLLATE latin7_general_ci NOT NULL,
  PRIMARY KEY (`TestCharsetFAId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf16",
              createTable, true, true, true);
            else Assert.Equal(@"CREATE TABLE `testcharsetfa` (
  `TestCharsetFAId` varchar(255) CHARACTER SET latin7 NOT NULL,
  PRIMARY KEY (`TestCharsetFAId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf16",
              createTable, true, true, true);
          }

          cmd.CommandText = "SHOW CREATE TABLE `TestCollationDA`";
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            reader.Read();
            string createTable = reader.GetString(1);
            Assert.Equal(@"CREATE TABLE `testcollationda` (
  `TestCollationDAId` varchar(255) CHARACTER SET greek COLLATE greek_bin NOT NULL,
  PRIMARY KEY (`TestCollationDAId`)
) ENGINE=InnoDB DEFAULT CHARSET=cp932 COLLATE=cp932_bin",
              createTable, true, true, true);
          }

          cmd.CommandText = "SHOW CREATE TABLE `TestCollationFA`";
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            reader.Read();
            string createTable = reader.GetString(1);
            Assert.Equal(@"CREATE TABLE `testcollationfa` (
  `TestCollationFAId` varchar(255) CHARACTER SET ucs2 COLLATE ucs2_bin NOT NULL,
  PRIMARY KEY (`TestCollationFAId`)
) ENGINE=InnoDB DEFAULT CHARSET=koi8u COLLATE=koi8u_bin",
              createTable, true, true, true);
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
