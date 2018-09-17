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
  public class EFCore20Tests : IClassFixture<SakilaLiteFixture>
  {
    private SakilaLiteFixture fixture;

    public EFCore20Tests(SakilaLiteFixture fixture)
    {
      this.fixture = fixture;
    }

    // Explicitly compiled query
    private static Func<SakilaLiteContext, int, Customer> _customerById =
      EF.CompileQuery((SakilaLiteContext context, int id) =>
        context.Customer.Single(p => p.CustomerId == id));

    [Fact]
    public void ExplicitlyCompiledQueries()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        var customer = _customerById(context, 9);
        Assert.Equal(9, customer.CustomerId);
        Assert.Equal("MOORE", customer.LastName);
      }
    }

    [Fact]
    public void GraphNewAndExistingEntities()
    {
      using (SakilaLiteUpdateContext context = new SakilaLiteUpdateContext())
      {
        context.InitContext();

        Actor actorNoId, actor;
        context.Attach(actorNoId = new Actor { FirstName = "PENELOPE", LastName = "GUINESS" });
        context.Attach(actor = new Actor { ActorId = 21, FirstName = "KIRSTEN", LastName = "PALTROW" });

        var changes = context.ChangeTracker.Entries();
        Assert.Equal(2, changes.Count());
        Assert.Collection(changes,
          item =>
          {
            Assert.Equal(EntityState.Added, item.State);
          },
          item =>
          {
            Assert.Equal(EntityState.Unchanged, item.State);
          });

        context.SaveChanges();

        var list = context.Actor.ToList();
        Assert.Equal(1, list.Count);
      }
    }

    [Fact]
    public void StringInterpolationInSqlCommands()
    {
      using (SakilaLiteUpdateContext context = new SakilaLiteUpdateContext())
      {
        context.InitContext();

        int id = 1;
        string firstName = "PENELOPE";
        string lastName = "GUINESS";
        DateTime lastUpdate = DateTime.Parse("2006-02-15 04:34:33");
        context.Database.ExecuteSqlCommand($"INSERT INTO actor(actor_id, first_name, last_name, last_update) VALUES ({id}, {firstName}, {lastName}, {lastUpdate:u})");
        Actor actor = context.Set<Actor>().FromSql($"SELECT * FROM actor WHERE actor_id={id} and last_name={lastName}").First();

        Assert.Equal(id, actor.ActorId);
        Assert.Equal(firstName, actor.FirstName);
      }
    }
  }
}
