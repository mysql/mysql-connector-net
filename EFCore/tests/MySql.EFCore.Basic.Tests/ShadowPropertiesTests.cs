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
using System.Data;
using System.Linq;
using Xunit;

namespace MySql.Data.EntityFrameworkCore.Tests
{
    public class ShadowPropertiesTests : IDisposable
    {

        ServiceCollection serviceCollection;
        IServiceProvider serviceProvider;
        DbContext context;

        public ShadowPropertiesTests()
        {
            serviceCollection = new ServiceCollection();
            serviceCollection.AddEntityFrameworkMySQL()
              .AddDbContext<ContextWithShadowProperty>();

            serviceProvider = serviceCollection.BuildServiceProvider();
            context = serviceProvider.GetRequiredService<ContextWithShadowProperty>();         
            context.Database.EnsureCreated();                        
        }


        [Fact]
        public void CanUseShadowPropertyWhenUpdatingEntry()
        {
            Assert.False(context.Database.EnsureCreated());
            var ad = new Address { Street = "New street two", City = "Chicago" };
            var g = new Guest { Name = "Guest number two", Address = ad };
            context.Set<Guest>().Add(g);
            context.SaveChanges();

            // update entry
            var entry = context.Set<Guest>().Where(t => t.Name.Equals("Guest number two")).First();
            Assert.False(entry == null);

            entry.Name = "Guest number two updated";
            context.SaveChanges();

            // check data using MySqlCommand
            using (var cnn = new MySqlConnection(context.Database.GetDbConnection().ConnectionString))
            {
                if (cnn.State != ConnectionState.Open)
                    cnn.Open();

                var cmd = new MySqlCommand("Select UpdatedAt from Guests where IdGuest=" + entry.IdGuest, cnn);
                var updatedAt = cmd.ExecuteScalar();
                Assert.False(updatedAt == null);
                Assert.True(((DateTime)updatedAt).Date.CompareTo(DateTime.Now.Date) == 0);
            }
        }

        [Fact]
        public void CanUseShadowPropertyWhenAddingEntry()
        {
                    
                Assert.False(context.Database.EnsureCreated());
                var ad = new Address { Street = "New street", City = "Oregon" };
                var g = new Guest { Name = "Guest number one", Address = ad };
                context.Set<Guest>().Add(g);   

                context.SaveChanges();            

                // check data using MySqlCommand
                using (var cnn = new MySqlConnection(context.Database.GetDbConnection().ConnectionString))
                {
                    if (cnn.State != ConnectionState.Open)
                        cnn.Open();

                    var cmd = new MySqlCommand("Select CreatedAt from Guests Limit 1", cnn);
                    var createdAt = cmd.ExecuteScalar();
                    Assert.False(createdAt == null);
                    Assert.True(((DateTime)createdAt).Date.CompareTo(DateTime.Now.Date) == 0);
                }                   
        }

        public void Dispose()
        {
            context.Database.EnsureDeleted();
        }
    }   
}
