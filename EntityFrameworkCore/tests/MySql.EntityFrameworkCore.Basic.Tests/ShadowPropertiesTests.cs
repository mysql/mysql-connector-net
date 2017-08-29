// Copyright © 2016, 2017 Oracle and/or its affiliates. All rights reserved.
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
