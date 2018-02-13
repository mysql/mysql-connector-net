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
using MySql.Data.EntityFrameworkCore.Tests.DbContextClasses;
using MySql.Data.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MySql.Data.EntityFrameworkCore.Extensions;

namespace MySql.Data.EntityFrameworkCore.Tests
{
    public class ConcurrencyTests
    {

        [Fact]
        public void CanHandleConcurrencyConflicts()
        {           
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddEntityFrameworkMySQL()
              .AddDbContext<ConcurrencyTestsContext>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            using (var context = serviceProvider.GetRequiredService<ConcurrencyTestsContext>())
            { 
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                context.People.Add(new Person { Name = "Mike Redman", SocialSecurityNumber = "SSS1229932", PhoneNumber = "565665656" });
                context.SaveChanges();
                            
                var person = context.People.Single(p => p.PersonId == 1);
                person.SocialSecurityNumber = "SS15555";
            
                context.Database.ExecuteSqlCommand("UPDATE People SET Name = 'Jane' WHERE PersonId = 1");

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
                                    Assert.Equal(databaseValue, "Jane");
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
    }
}
