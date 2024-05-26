// Copyright © 2021, 2024, Oracle and/or its affiliates.
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

using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Basic.Tests.DbContextClasses;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Linq;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  public class LoadingRelatedDataTests
  {
    DbContext context;

    public LoadingRelatedDataTests()
    {
      context = new EagerLoadingContext();
      context.Database.EnsureDeleted();
      context.Database.EnsureCreated();
      AddData(context);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
      using (FiguresContext context = new FiguresContext())
        context.Database.EnsureDeleted();
      using (JsonContext context = new JsonContext())
        context.Database.EnsureDeleted();
      using (WorldContext context = new WorldContext())
        context.Database.EnsureDeleted();
      context.Database.EnsureDeleted();
    }

    [Test]
    public void CanUseSkipAndTake()
    {
      Assert.That(!context.Database.EnsureCreated());
      var people
              = context.Set<Guest>()
                  .Skip(2)
                  .Take(1)
                  .ToList();

      Assert.That(people, Has.One.Items);
    }

    [Test]
    public void CanIncludeAddressData()
    {
      Assert.That(!context.Database.EnsureCreated());
      var people
              = context.Set<Guest>()
                  .Include(p => p.Address)
                  .ToList();

      Assert.That(people.Count, Is.EqualTo(4));
      Assert.That(people.Count(p => p.Address != null), Is.EqualTo(3));
    }

    [Test]
    public void CanIncludeGuestData()
    {
      Assert.That(!context.Database.EnsureCreated());
      var ad
              = context.Set<Address>()
                  .Include(p => p.Guest)
                  .ToList();

      Assert.That(ad.Count, Is.EqualTo(3));
      var rows = ad.Select(g => g.Guest).Where(a => a != null).ToList();
      Assert.That(rows.Count(), Is.EqualTo(3));
    }


    [Test]
    public void CanIncludeGuestShadowProperty()
    {
      Assert.That(!context.Database.EnsureCreated());
      var addressRelative
            = context.Set<AddressRelative>()
                .Include(a => a.Relative)
                .ToList();

      Assert.That(addressRelative.Count, Is.EqualTo(3));
      Assert.That(addressRelative.All(p => p.Relative != null));
    }

    [Test]
    public void MixClientServerEvaluation()
    {
      Assert.That(!context.Database.EnsureCreated());
      var list
            = context.Set<Address>()
            .OrderByDescending(a => a.City)
            .Select(a => new { Id = a.IdAddress, City = SetCity(a.City!) })
            .ToList();

      Assert.That(list.Count, Is.EqualTo(3));
      Assert.That(list.First().City, Does.EndWith(" city"));
    }

    private static string SetCity(string name)
    {
      return name + " city";
    }

    [Test]
    public void RawSqlQueries()
    {
      Assert.That(!context.Database.EnsureCreated());
      var guests = context.Set<Guest>().FromSqlRaw("SELECT * FROM Guests")
        .ToList();
      Assert.That(guests.Count, Is.EqualTo(4));
    }

    [Test]
    public void UsingTransactions()
    {
      Assert.That(!context.Database.EnsureCreated());
      using (var transaction = context.Database.BeginTransaction())
      {
        context.Set<Guest>().Add(new Guest()
        {
          Name = "Guest five"
        });
        context.SaveChanges();
      }
      Assert.That(context.Set<Guest>().Count(), Is.EqualTo(4));
    }

    [Test]
    public void DbSetFind()
    {
      var address = context.Set<Address>().Find(1);
      Assert.That(address, Is.Not.Null);
      Assert.That(address!.City, Is.EqualTo("Michigan"));
    }


    [Test]
    public void JsonDataTest()
    {
      Assume.That(TestUtils.IsAtLeast(5, 7, 0));

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
          string smallintWidth = "(6)";
          if (conn.driver.Version.isAtLeast(8, 0, 1))
          {
            charset = "utf8mb4";
            smallintWidth = string.Empty;
          }
          //Adding "COLLATION" to the string validation at table creation (this happens since MySql 8.0.5 Server)
          if (jsonTableDesc.Contains("COLLATE=utf8mb4_0900_ai_ci"))
            Assert.That(jsonTableDesc, Is.EqualTo($"CREATE TABLE `jsonentity` (\n  `Id` smallint{smallintWidth} NOT NULL AUTO_INCREMENT," +
            $"\n  `jsoncol` json DEFAULT NULL,\n  PRIMARY KEY (`Id`)\n) ENGINE=InnoDB DEFAULT CHARSET={charset} COLLATE=utf8mb4_0900_ai_ci").IgnoreCase);
          else Assert.That(jsonTableDesc, Is.EqualTo($"CREATE TABLE `jsonentity` (\n  `Id` smallint{smallintWidth} NOT NULL AUTO_INCREMENT,\n  `jsoncol` json DEFAULT NULL,\n  PRIMARY KEY (`Id`)\n) " +
            $"ENGINE=InnoDB DEFAULT CHARSET={charset}").IgnoreCase);
        }

        context.JsonEntity.Add(new JsonData()
        {
          jsoncol = "{ \"name\": \"Ronald\", \"city\": \"Austin\" }"
        });
        context.SaveChanges();
        JsonData json = context.JsonEntity.First();
        Assert.That(json.jsoncol, Is.EqualTo("{ \"name\": \"Ronald\", \"city\": \"Austin\" }"));
      }
    }


    [Test]
    public void JsonInvalidData()
    {
      Assume.That(TestUtils.IsAtLeast(5, 7, 0));

      using (JsonContext context = new JsonContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.JsonEntity.Add(new JsonData()
        {
          jsoncol = "{ name: Ronald, city: Austin }"
        });

        var ex = Assert.Throws<DbUpdateException>(() => context.SaveChanges())?.GetBaseException();
        Assert.That(((MySqlException)ex!).Number, Is.EqualTo(3140));
      }
    }


    [Test]
    public void ComputedColumns()
    {
      Assume.That(TestUtils.IsAtLeast(5, 7, 0));

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
        Assert.That(data[0].Area, Is.EqualTo(75));
        Assert.That(data[1].Area, Is.EqualTo(50));
      }
    }

    [Test]
    public void ExplicitLoading()
    {
      using (var context = new WorldContext())
      {
        context.PopulateData();
        var america = context.Continents.Single(c => c.Code == "AM");
        Assert.That(america.Countries, Is.Null);
        context.Entry(america)
          .Collection(c => c.Countries!)
          .Load();
        Assert.That(america.Countries!.Count, Is.EqualTo(5));
        Assert.That(america.Countries.Single(c => c.Code == "US").Name, Is.EqualTo("United States"));
      }
    }

    [Test]
    public void ExplicitLoadingQueryingRelatedEntitites()
    {
      using (var context = new WorldContext())
      {
        context.PopulateData();
        var asia = context.Continents.Single(c => c.Code == "AS");
        Assert.That(asia.Countries, Is.Null);
        var list = context.Entry(asia)
          .Collection(c => c.Countries!)
          .Query()
          .Where(c => c.Name!.Contains("i"))
          .ToList();
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
  }
}
