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
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Basic.Tests.DbContextClasses;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using MySql.EntityFrameworkCore.Extensions;
using NUnit.Framework;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  public class FluentAPITests
  {
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
      using (ConnStringOnConfiguringContext context = new ConnStringOnConfiguringContext())
        context.Database.EnsureDeleted();
      using (KeyConventionsContext context = new KeyConventionsContext())
        context.Database.EnsureDeleted();
      using (WorldContext context = new WorldContext())
        context.Database.EnsureDeleted();
    }

    [Test]
    public void EnsureRelationalPatterns()
    {
      if (!TestUtils.IsAtLeast(5, 7, 0))
      {
        Assert.Ignore();
      }

      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<ComputedColumnContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ComputedColumnContext>())
      {
        context.Database.EnsureCreated();

        var e = new Employee { FirstName = "Jos", LastName = "Stuart", Timestamp = DateTime.Now };
        context.Employees.Add(e);
        context.SaveChanges();
        var employeeComputedColumn = context.Employees.FirstOrDefault();
        Assert.True(employeeComputedColumn!.DisplayName!.Equals("Stuart Jos"), "Wrong computed column");
        context.Database.EnsureDeleted();
      }
    }


    [Test]
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
          DateTimeOffset dt = DateTime.Now;
          var e = new QuickEntity { Name = "Jos", Created = dt };
          context.QuickEntity.Add(e);
          context.SaveChanges();
          var row = context.QuickEntity.FirstOrDefault();
          Assert.AreEqual(dt, row!.Created);
        }
        finally
        {
          context.Database.EnsureDeleted();
        }
      }
    }


    [Test]
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
          DateTimeOffset dt = DateTime.Now;
          var e = new QuickEntity { Name = "Jos", Created = dt };
          context.QuickEntity.Add(e);
          context.SaveChanges();
          var result = await context.QuickEntity.FirstOrDefaultAsync();
          Assert.AreEqual(dt, result!.Created);
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


    [Test]
    public void CanNameAlternateKey()
    {
      var serviceCollection = new ServiceCollection();

      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<KeyConventionsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<KeyConventionsContext>())
      {
        context.Database.EnsureCreated();
        using (var cnn = new MySqlConnection(MySQLTestStore.BaseConnectionString))
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

    [Test]
    public void CanUseToTable()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<TableConventionsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<TableConventionsContext>())
      {
        context.Database.EnsureCreated();
        using (var cnn = new MySqlConnection(MySQLTestStore.BaseConnectionString))
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


    [Test]
    public void CanUseConcurrency()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL()
        .AddDbContext<ConcurrencyTestsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ConcurrencyTestsContext>())
      {
        context.Database.EnsureCreated();
        using (var cnn = new MySqlConnection(MySQLTestStore.BaseConnectionString))
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


    [Test]
    public void CanUseConcurrencyToken()
    {
      if (!TestUtils.IsAtLeast(5, 7, 0))
      {
        Assert.Ignore();
      }

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
        var employeeComputedColumn = context.Employees.SingleOrDefault();
        Assert.True(employeeComputedColumn!.DisplayName!.Equals("Stuart Jos"), "Wrong computed column");
        context.Database.EnsureDeleted();
      }
    }


    [Test]
    public void CanUseContainsInQuery()
    {
      if (!TestUtils.IsAtLeast(5, 7, 0))
      {
        Assert.Ignore();
      }

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
        var result = context.Employees.Where(t => t.FirstName!.Contains("jo")).ToList();
        Assert.That(result, Has.One.Items);
        context.Database.EnsureDeleted();
      }
    }


    [Test]
    public void CanUseContainsVarInQuery()
    {
      if (!TestUtils.IsAtLeast(5, 7, 0))
      {
        Assert.Ignore();
      }

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
        var result = context.Employees.Where(t => t.FirstName!.Contains(test)).ToList();
        Assert.That(result, Has.One.Items);
        context.Database.EnsureDeleted();
      }
    }


    [Test]
    public void CanUseContainsWithInvalidValue()
    {
      if (!TestUtils.IsAtLeast(5, 7, 0))
      {
        Assert.Ignore();
      }

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
        var result = context.Employees.Where(t => t.FirstName!.Contains("XXXXXXXX$%^&*()!")).ToList();
        Assert.IsEmpty(result);
        result = context.Employees.Where(t => t.FirstName!.Contains("null")).ToList();
        Assert.IsEmpty(result);
        context.Database.EnsureDeleted();
      }
    }


    [Test]
    public void CanUseContainsWithVariableInQuery()
    {
      if (!TestUtils.IsAtLeast(5, 7, 0))
      {
        Assert.Ignore();
      }

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
        var result = context.Employees.Where(t => t.FirstName!.Contains(avalue)).ToList();
        Assert.That(result, Has.One.Items);
        context.Database.EnsureDeleted();
      }
    }

    [Test]
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
            StringAssert.AreEqualIgnoringCase("continentlist", reader.GetString(0));
            Assert.True(reader.Read());
            StringAssert.AreEqualIgnoringCase("countrylist", reader.GetString(0));
          }

        }
      }
    }

    [Test]
    public void CharsetTest()
    {
      using (ConnStringOnConfiguringContext context = new ConnStringOnConfiguringContext())
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
            createTable = Regex.Replace(createTable, @"\t|\n|\r", string.Empty);
            string txt = "CREATE TABLE `testcharsetda` (  `TestCharsetDAId` varbinary(255) NOT NULL,  PRIMARY KEY (`TestCharsetDAId`)) ENGINE=InnoDB DEFAULT CHARSET=ascii";
            StringAssert.AreEqualIgnoringCase(txt, createTable);
          }

          cmd.CommandText = "SHOW CREATE TABLE `TestCharsetFA`";
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            reader.Read();
            string createTable = reader.GetString(1);
            string txt = string.Empty;
            createTable = Regex.Replace(createTable, @"\t|\n|\r", string.Empty);

            //Adding "COLLATION" to the string validation at table creation (this happens since MySql 8.0.5 Server)
            if (createTable.Contains("COLLATE latin7_general_ci"))
            {
              txt = "CREATE TABLE `testcharsetfa` (  `TestCharsetFAId` varchar(255) CHARACTER SET latin7 COLLATE latin7_general_ci NOT NULL,  PRIMARY KEY (`TestCharsetFAId`)) ENGINE=InnoDB DEFAULT CHARSET=utf16";
              StringAssert.AreEqualIgnoringCase(txt, createTable);
            }
            else
            {
              txt = "CREATE TABLE `testcharsetfa` (  `TestCharsetFAId` varchar(255) CHARACTER SET latin7 NOT NULL,  PRIMARY KEY (`TestCharsetFAId`)) ENGINE=InnoDB DEFAULT CHARSET=utf16";
              StringAssert.AreEqualIgnoringCase(txt, createTable);
            }
          }

          cmd.CommandText = "SHOW CREATE TABLE `TestCollationDA`";
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            reader.Read();
            string createTable = reader.GetString(1);
            createTable = Regex.Replace(createTable, @"\t|\n|\r", string.Empty);
            string txt = "CREATE TABLE `testcollationda` (  `TestCollationDAId` varchar(255) CHARACTER SET greek COLLATE greek_bin NOT NULL,  PRIMARY KEY (`TestCollationDAId`)) ENGINE=InnoDB DEFAULT CHARSET=cp932 COLLATE=cp932_bin";
            StringAssert.AreEqualIgnoringCase(txt, createTable);
          }

          cmd.CommandText = "SHOW CREATE TABLE `TestCollationFA`";
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            reader.Read();
            string createTable = reader.GetString(1);
            createTable = Regex.Replace(createTable, @"\t|\n|\r", string.Empty);
            string txt = "CREATE TABLE `testcollationfa` (  `TestCollationFAId` varchar(255) CHARACTER SET ucs2 COLLATE ucs2_bin NOT NULL,  PRIMARY KEY (`TestCollationFAId`)) ENGINE=InnoDB DEFAULT CHARSET=koi8u COLLATE=koi8u_bin";
            StringAssert.AreEqualIgnoringCase(txt, createTable);
          }
        }
      }
    }
  }
}
