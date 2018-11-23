// Copyright (c) 2018, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.EntityFrameworkCore.Tests.DbContextClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace MySql.Data.EntityFrameworkCore.Tests
{
  public class ModelingTests : IClassFixture<SakilaLiteFixture>
  {
    private SakilaLiteFixture fixture;

    public ModelingTests(SakilaLiteFixture fixture)
    {
      this.fixture = fixture;
    }

    [Fact]
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

        Action<FilmLite, int, string, short> validate = (film, id, title, releaseYear) =>
        {
          Assert.Equal(id, film.FilmId);
          Assert.NotNull(film.Details);
          Assert.NotNull(film.Details.Film);
          Assert.Equal(id, film.Details.FilmId);
          Assert.Equal(title, film.Title);
          Assert.Equal(releaseYear, film.Details.ReleaseYear);
        };

        Assert.Collection(films,
          film => validate(film, 1, "ACADEMY DINOSAUR", 2006),
          film => validate(film, 2, "ACE GOLDFINGER", 2006)
        );
      }
    }

    [Fact]
    public void ScalarFunctionMapping()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        context.Database.ExecuteSqlCommand("CREATE FUNCTION FilmsByActorCount(id SMALLINT) RETURNS INT RETURN (SELECT COUNT(*) FROM film_actor WHERE actor_id = id);");
        var query = context.Actor.Where(c => SakilaLiteContext.FilmsByActorCount(c.ActorId) == 18).ToList();
        Assert.Collection<Actor>(query,
          e => { Assert.Equal(31, e.ActorId); },
          e => { Assert.Equal(71, e.ActorId); }
        );
      }
    }

    [Fact]
    public void LikeFunction()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        var query = context.Actor.Where(c => EF.Functions.Like(c.LastName, "A%")).ToList();
        Assert.NotEmpty(query);
        foreach(Actor actor in query)
        {
          Assert.StartsWith("A", actor.LastName);
        }
      }
    }

    [Fact]
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

          context.Customer.Find((short)1).Address = address;
          context.SaveChanges();

          var customer = context.Customer.Where(p => p.Address.AddressId == 1).First();
          Assert.Equal(1, customer.CustomerId);
          Assert.Equal("47 MySakila Drive", customer.Address.Address);
        }
        finally
        {
          context.Database.EnsureDeleted();
        }
      }
    }

    [Fact]
    public void ModelLevelQueryFilter()
    {
      using(SakilaLiteContext context = new SakilaLiteContext())
      {
        Assert.Equal(584, context.Customer.Count());
        Assert.Equal(599, context.Customer.IgnoreQueryFilters().Count());
      }
    }
  }
}
