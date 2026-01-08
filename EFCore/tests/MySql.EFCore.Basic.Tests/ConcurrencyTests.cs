// Copyright © 2021, 2026, Oracle and/or its affiliates.
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
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Basic.Tests.DbContextClasses;
using MySql.EntityFrameworkCore.Extensions;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Linq;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  public class ConcurrencyTests
  {
    [Test]
    public void CanHandleConcurrencyConflicts()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL().AddDbContext<ConcurrencyTestsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ConcurrencyTestsContext>())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.People.Add(new Person { Name = "John Doe", PersonNumber = "3333333333", PhoneNumber = "1111111111" });
        context.SaveChanges();

        var person = context.People.Single(p => p.PersonId == 1);
        person.PersonNumber = "4444444444";

        context.Database.ExecuteSqlInterpolated($"UPDATE People SET Name = 'Jane' WHERE PersonId = 1");

        try
        {
          context.SaveChanges();
        }
        catch (DbUpdateConcurrencyException ex)
        {
          foreach (var entry in ex.Entries)
          {
            if (entry.Entity is Person)
            {
              // Using a NoTracking query means we get the entity but it is not tracked by the context
              // and will not be merged with existing entities in the context.
              var databaseEntity = context.People.AsNoTracking().Single(p => p.PersonId == ((Person)entry.Entity).PersonId);
              var databaseEntry = context.Entry(databaseEntity);

              foreach (var property in entry.Metadata.GetProperties())
              {
                if (property.Name.Equals("Name"))
                {
                  var proposedValue = entry.Property(property.Name).CurrentValue;
                  var originalValue = entry.Property(property.Name).OriginalValue;
                  var databaseValue = databaseEntry.Property(property.Name).CurrentValue;
                  entry.Property(property.Name).OriginalValue = databaseEntry.Property(property.Name).CurrentValue;
                  Assert.That(databaseValue, Is.EqualTo("Jane"));
                }
              }
            }
            else
            {
              throw new NotSupportedException("Don't know how to handle concurrency conflicts for " + entry.Metadata.Name);
            }
          }

          // Retry the save operation
          context.SaveChanges();
        }
        finally
        {
          context.Database.EnsureDeleted();
        }
      }
    }

    [Test]
    public void CanHandleUpdateConcurrencyConflicts()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL().AddDbContext<ConcurrencyTestsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ConcurrencyTestsContext>())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.People.Add(new Person
        { Name = "John Doe", PersonNumber = "3333333333", PhoneNumber = "1111111111" });
        context.SaveChanges();

        var updatePerson1 = context.People.Single(p => p.PersonId == 1);
        var updatePerson2 = context.People.Single(p => p.PersonId == 1);

        updatePerson1.PersonNumber = "2222222222";
        updatePerson1.PhoneNumber = "555555555";

        context.SaveChanges();

        var item = context.ChangeTracker.Entries().First(x => Object.ReferenceEquals(x.Entity, updatePerson2));
        item.OriginalValues["PersonNumber"] = "3333333333";
        updatePerson2.PersonNumber = "44444444";
        updatePerson2.PhoneNumber = "666666666";

        TestDelegate action = () => context.SaveChanges();
        Assert.Throws<DbUpdateConcurrencyException>(action);
      }
    }

    [Test]
    public void CanHandleDeleteConcurrencyConflicts()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL().AddDbContext<ConcurrencyTestsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ConcurrencyTestsContext>())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.Remove(new Person { PersonId = 2 , PersonNumber = "88888888" });

        TestDelegate action = () => context.SaveChanges();
        Assert.Throws<DbUpdateConcurrencyException>(action);
      }
    }

    [Test]
    public void CanHandleInsertConcurrencyConflicts()
    {
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddEntityFrameworkMySQL().AddDbContext<ConcurrencyTestsContext>();

      var serviceProvider = serviceCollection.BuildServiceProvider();

      using (var context = serviceProvider.GetRequiredService<ConcurrencyTestsContext>())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.People.Add(new Person
        { Name = "John Doe", PersonNumber = "3333333333", PhoneNumber = "1111111111" });
        context.SaveChanges();

        var insertedPerson1 = context.People.Single(p => p.PersonId == 1);
        insertedPerson1.PersonNumber = "1111111111";

        context.SaveChanges();

        var item = context.ChangeTracker.Entries().First(x => Object.ReferenceEquals(x.Entity, insertedPerson1));
        item.OriginalValues["PersonNumber"] = "3333333333";

        context.People.Add(new Person
        { Name = "John Doe", PersonNumber = "3333333333", PhoneNumber = "1111111111" });

        TestDelegate action = () => context.SaveChanges();
        Assert.Throws<DbUpdateConcurrencyException>(action);
      }
    }
  }
}
