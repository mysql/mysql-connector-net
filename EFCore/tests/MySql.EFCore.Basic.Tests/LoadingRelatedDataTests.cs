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
using Microsoft.Extensions.Logging;
using MySql.Data.EntityFrameworkCore.Extensions;
using MySql.Data.EntityFrameworkCore.Tests.DbContextClasses;
using MySql.Data.MySqlClient;
using System;
using System.Linq;
using Xunit;

namespace MySql.Data.EntityFrameworkCore.Tests
{
  public class LoadingRelatedDataTests : IDisposable
  {

    DbContext context;
    DbContextOptions options;
    IServiceCollection collection = new ServiceCollection()
                                    .AddEntityFrameworkMySQL();

    public LoadingRelatedDataTests()
    {

      options = new DbContextOptionsBuilder()
                   .UseInternalServiceProvider(collection.BuildServiceProvider())
                   .UseMySQL(MySQLTestStore.baseConnectionString + "bd-eagerloading")
                   .Options;

      context = new EagerLoadingContext(options);
      context.Database.EnsureDeleted();
      context.Database.EnsureCreated();
      AddData(context);

    }

    [Fact]
    public void CanUseSkipAndTake()
    {
      Assert.False(context.Database.EnsureCreated());
      var people
              = context.Set<Guest>()
                  .Skip(2)
                  .Take(1)
                  .ToList();

      Assert.Equal(1, people.Count);
    }

    [Fact]
    public void CanIncludeAddressData()
    {
      Assert.False(context.Database.EnsureCreated());
      var people
              = context.Set<Guest>()
                  .Include(p => p.Address)
                  .ToList();

      Assert.Equal(4, people.Count);
      Assert.Equal(3, people.Count(p => p.Address != null));
      //                Assert.Equal(@"SELECT `p`.`IdGuest`, `p`.`Name`, `p`.`RelativeId`, `a`.`IdAddress`, `a`.`City`, `a`.`Street`
      //FROM `Guests` AS `p`
      //LEFT JOIN `Address` AS `a` ON `a`.`IdAddress` = `p`.`IdGuest`", Sql);
    }

    [Fact]
    public void CanIncludeGuestData()
    {
      Assert.False(context.Database.EnsureCreated());
      var ad
              = context.Set<Address>()
                  .Include(p => p.Guest)
                  .ToList();

      Assert.Equal(3, ad.Count);
      var rows = ad.Select(g => g.Guest).Where(a => a != null).ToList();
      Assert.Equal(3, rows.Count());

      // TODO check the logger implementation
      //            Assert.Equal(@"SELECT `p`.`IdAddress`, `p`.`City`, `p`.`Street`, `g`.`IdGuest`, `g`.`Name`, `g`.`RelativeId`
      //FROM `Address` AS `p`
      //INNER JOIN `Guests` AS `g` ON `p`.`IdAddress` = `g`.`IdGuest`", Sql);
    }


    [Fact]
    public void CanIncludeGuestShadowProperty()
    {
      Assert.False(context.Database.EnsureCreated());
      var addressRelative
            = context.Set<AddressRelative>()
                .Include(a => a.Relative)
                .ToList();

      Assert.Equal(3, addressRelative.Count);
      Assert.True(addressRelative.All(p => p.Relative != null));
      // TODO: review what should be the result here (acc. EF tests should be 6)
      //            Assert.Equal(13, context.ChangeTracker.Entries().Count());
      //            Assert.Equal(@"SELECT `a`.`IdAddressRelative`, `a`.`City`, `a`.`Street`, `p`.`IdRelative`, `p`.`Name`
      //FROM `AddressRelative` AS `a`
      //INNER JOIN `Persons2` AS `p` ON `a`.`IdAddressRelative` = `p`.`IdRelative`", Sql);
    }

    [Fact]
    public void MixClientServerEvaluation()
    {
      Assert.False(context.Database.EnsureCreated());
      var list
            = context.Set<Address>()
            .OrderByDescending(a => a.City)
            .Select(a => new { Id = a.IdAddress, City = SetCity(a.City) })
            .ToList();

      Assert.Equal(3, list.Count);
      Assert.True(list.First().City.EndsWith(" city"));
    }

    private string SetCity(string name)
    {
      return name + " city";
    }

    [Fact]
    public void RawSqlQueries()
    {
      Assert.False(context.Database.EnsureCreated());
      var guests = context.Set<Guest>().FromSql("SELECT * FROM Guests")
        .ToList();
      Assert.Equal(4, guests.Count);
    }

    [Fact]
    public void UsingTransactions()
    {
      Assert.False(context.Database.EnsureCreated());
      using (var transaction = context.Database.BeginTransaction())
      {
        context.Set<Guest>().Add(new Guest()
        {
          Name = "Guest five"
        });
        context.SaveChanges();
      }
      Assert.Equal(4, context.Set<Guest>().Count());
    }

    [Fact]
    public void DbSetFind()
    {
      var address = context.Set<Address>().Find(1);
      Assert.NotNull(address);
      Assert.Equal("Michigan", address.City);
    }

    [FactOnVersions("5.7.0", null)]
    public void JsonDataTest()
    {
      using (JsonContext context = new JsonContext())
      {
        var model = context.Model;
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        using (MySqlConnection conn = (MySqlConnection)context.Database.GetDbConnection())
        {
          conn.Open();
          MySqlCommand cmd = new MySqlCommand("SHOW CREATE TABLE JsonEntity", conn);
          string jsonTableDesc;
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            reader.Read();
            jsonTableDesc = reader.GetString(1);
          }
          string charset = "latin1";
          if (conn.driver.Version.isAtLeast(8, 0, 1))
            charset = "utf8mb4";
          //Adding "COLLATION" to the string validation at table creation (this happens since MySql 8.0.5 Server)
          if (jsonTableDesc.Contains("COLLATE=utf8mb4_0900_ai_ci")) Assert.Equal($"CREATE TABLE `jsonentity` (\n  `Id` smallint(6) NOT NULL AUTO_INCREMENT," +
            $"\n  `jsoncol` json DEFAULT NULL,\n  PRIMARY KEY (`Id`)\n) ENGINE=InnoDB DEFAULT CHARSET={charset} COLLATE=utf8mb4_0900_ai_ci", jsonTableDesc, true, true, true);
          else Assert.Equal($"CREATE TABLE `jsonentity` (\n  `Id` smallint(6) NOT NULL AUTO_INCREMENT,\n  `jsoncol` json DEFAULT NULL,\n  PRIMARY KEY (`Id`)\n) " +
            $"ENGINE=InnoDB DEFAULT CHARSET={charset}", jsonTableDesc, true, true, true);
        }

        context.JsonEntity.Add(new JsonData()
        {
          jsoncol = "{ \"name\": \"Ronald\", \"city\": \"Austin\" }"
        });
        context.SaveChanges();
        JsonData json = context.JsonEntity.First();
        Assert.Equal("{ \"name\": \"Ronald\", \"city\": \"Austin\" }", json.jsoncol);
      }
    }

    [FactOnVersions("5.7.0", null)]
    public void JsonInvalidData()
    {
      using (JsonContext context = new JsonContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.JsonEntity.Add(new JsonData()
        {
          jsoncol = "{ name: Ronald, city: Austin }"
        });
        MySqlException ex = (MySqlException)Assert.ThrowsAny<DbUpdateException>(() => context.SaveChanges()).GetBaseException();
        // Error Code: 3140. Invalid JSON text
        Assert.Equal(3140, ex.Number);
      }
    }

    [FactOnVersions("5.7.0", null)]
    public void ComputedColumns()
    {
      using (FiguresContext context = new FiguresContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        Triangle[] data = new Triangle[2];
        data[0] = new Triangle()
        {
          Id = 33,
          Base = 15,
          Height = 10
        };
        data[1] = new Triangle()
        {
          Base = 20,
          Height = 5
        };
        context.Triangle.AddRange(data);
        context.Triangle.Add(data[1]);
        context.SaveChanges();
        Assert.Equal(75, data[0].Area);
        Assert.Equal(50, data[1].Area);
      }
    }

    [Fact]
    public void ExplicitLoading()
    {
      using (var context = new WorldContext())
      {
        context.PopulateData();
        var america = context.Continents.Single(c => c.Code == "AM");
        Assert.Null(america.Countries);
        context.Entry(america)
          .Collection(c => c.Countries)
          .Load();
        Assert.Equal(5, america.Countries.Count);
        Assert.Equal("United States", america.Countries.Single(c => c.Code == "US").Name);
      }
    }

    [Fact]
    public void ExplicitLoadingQueryingRelatedEntitites()
    {
      using (var context = new WorldContext())
      {
        context.PopulateData();
        var asia = context.Continents.Single(c => c.Code == "AS");
        Assert.Null(asia.Countries);
        var list = context.Entry(asia)
          .Collection(c => c.Countries)
          .Query()
          .Where(c => c.Name.Contains("i"))
          .ToList();
        Assert.Equal(2, asia.Countries.Count);
        Assert.Equal(2, list.Count);
        Assert.Equal("China", list.Single(c => c.Code == "CN").Name);
        Assert.Equal("India", list.Single(c => c.Code == "IN").Name);
      }
    }



    private void AddData(DbContext context)
    {
      var d = new Address { Street = "Street one", City = "Michigan" };
      var d1 = new Address { Street = "Street two", City = "San Francisco" };
      var d2 = new Address { Street = "Street three", City = "Denver" };

      context.Set<Guest>().AddRange(
               new Guest { Name = "Guest one", Address = d },
               new Guest { Name = "Guest two", Address = d1 },
               new Guest { Name = "Guest three", Address = d2 },
               new Guest { Name = "Guest four" }
               );

      context.Set<Address>().AddRange(d, d1, d2);

      var ad = new AddressRelative { Street = "Street one", City = "Michigan" };
      var ad1 = new AddressRelative { Street = "Street two", City = "San Francisco" };
      var ad2 = new AddressRelative { Street = "Street three", City = "Denver" };

      context.Set<Relative>().AddRange(
             new Relative { Name = "L. J.", Address = ad },
             new Relative { Name = "M. M.", Address = ad1 },
             new Relative { Name = "Z. Z.", Address = ad2 }
          );

      context.Set<AddressRelative>().AddRange(ad, ad1, ad2);

      context.SaveChanges();
    }

    public void Dispose()
    {
      context.Database.EnsureDeleted();
    }
  }
}
