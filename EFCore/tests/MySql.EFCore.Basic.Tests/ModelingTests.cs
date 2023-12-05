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
using MySql.EntityFrameworkCore.Basic.Tests.DbContextClasses;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  [TestFixture]
  public class ModelingTests : SakilaLiteFixture
  {
    [Test]
    public void TableSplitting()
    {
      // Inserting data
      using (SakilaLiteTableSplittingContext context = new SakilaLiteTableSplittingContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.Films.Add(new FilmLite
        {
          FilmId = 1,
          Title = "ACADEMY DINOSAUR",
          Description = "A Epic Drama of a Feminist And a Mad Scientist who must Battle a Teacher in The Canadian Rockies",
          Details = new FilmDetails
          {
            FilmId = 1,
            ReleaseYear = 2006,
            LanguageId = 1,
            OriginalLanguageId = null,
            RentalDuration = 6,
            RentalRate = 0.99m,
            Length = 86,
            ReplacementCost = 20.99m,
            Rating = "PG",
            SpecialFeatures = "Deleted Scenes,Behind the Scenes",
            LastUpdate = DateTime.Parse("2006-02-15 05:03:42")
          }
        });
        context.Films.Add(new FilmLite
        {
          FilmId = 2,
          Title = "ACE GOLDFINGER",
          Description = "A Astounding Epistle of a Database Administrator And a Explorer who must Find a Car in Ancient China"
        });
        context.FilmDetails.Add(new FilmDetails
        {
          FilmId = 2,
          ReleaseYear = 2006,
          LanguageId = 1,
          OriginalLanguageId = null,
          RentalDuration = 3,
          RentalRate = 4.99m,
          Length = 48,
          ReplacementCost = 12.99m,
          Rating = "G",
          SpecialFeatures = "Trailers,Deleted Scenes",
          LastUpdate = DateTime.Parse("2006-02-15 05:03:42")
        });

        context.SaveChanges();
      }

      // Validating data
      using (SakilaLiteTableSplittingContext context = new SakilaLiteTableSplittingContext())
      {
        var films = context.Films.Include(e => e.Details).ToList();

        Assert.That(films.Where(a => a.FilmId == 1), Has.One.Items);
        Assert.That(films.Where(a => a.FilmId == 2), Has.One.Items);
        Assert.That(films.Select(c => c.Title), Has.Exactly(1).Matches<string>(Title => Title.Contains("ACADEMY DINOSAUR")));
        Assert.That(films.Select(c => c.Title), Has.Exactly(1).Matches<string>(Title => Title.Contains("ACE GOLDFINGER")));
        Assert.That(films.Where(a => a.Details!.Film!.Details!.ReleaseYear == 2006), Has.Exactly(2).Items);
      }
    }

    [Test]
    public void ScalarFunctionMapping()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        context.Database.ExecuteSqlInterpolated($"CREATE FUNCTION FilmsByActorCount(id SMALLINT) RETURNS INT RETURN (SELECT COUNT(*) FROM film_actor WHERE actor_id = id);");
        var query = context.Actor.Where(c => SakilaLiteContext.FilmsByActorCount(c.ActorId) == 18).ToList();
        Assert.That(query.Select(a => a.ActorId), Has.Exactly(1).Matches<short>(actorid => actorid.Equals(31)));
        Assert.That(query.Select(a => a.ActorId), Has.Exactly(1).Matches<short>(actorid => actorid.Equals(71)));
      }
    }

    [Test]
    public void LikeFunction()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        var query = context.Actor.Where(c => EF.Functions.Like(c.LastName!, "A%")).ToList();
        Assert.IsNotEmpty(query);
        foreach (Actor actor in query)
        {
          StringAssert.StartsWith("A", actor.LastName);
        }
      }
    }

    [Test]
    [Ignore("Fix this")]
    public void OwnedEntityTypes()
    {
      using (SakilaLiteOwnedTypesContext context = new SakilaLiteOwnedTypesContext())
      {
        try
        {
          context.InitContext();

          SakilaAddress address = new SakilaAddress
          {
            Address = "47 MySakila Drive",
            Address2 = null,
            AddressId = 1,
            CityId = 300,
            District = "Alberta",
            Phone = "",
            PostalCode = "",
            LastUpdate = DateTime.Parse("2014-09-25 22:30:27")
          };

          context.Customer.Find((short)1)!.Address = address;
          context.SaveChanges();

          var customer = context.Customer.Where(p => p.Address!.AddressId == 1).First();
          Assert.AreEqual(1, customer.CustomerId);
          Assert.AreEqual("47 MySakila Drive", customer.Address!.Address);
        }
        finally
        {
          context.Database.EnsureDeleted();
        }
      }
    }

    [Test]
    public void ModelLevelQueryFilter()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        Assert.AreEqual(584, context.Customer.Count());
        Assert.AreEqual(599, context.Customer.IgnoreQueryFilters().Count());
      }
    }

    /// <summary>
    /// Bug 31337609 - MySql.EntityFrameworkCore NUGET PACKAGE 8.0.20
    /// </summary>
    [Test]
    public void ConvertToType()
    {
      using (var context = new SakilaLiteContext())
      {
        var ls = new List<ActorTest>();
        ls = context.Actor.OrderBy(a => a.ActorId).Select(b => new ActorTest
        {
          Number = Convert.ToInt32(b.ActorId),
          Value = b.LastUpdate.ToString(),
          Text = b.FirstName,
        }).ToList();

        Assert.IsNotEmpty(ls);
      }
    }
  }
}