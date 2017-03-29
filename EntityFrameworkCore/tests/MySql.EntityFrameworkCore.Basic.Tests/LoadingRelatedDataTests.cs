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

using EntityFrameworkCore.Basic.Tests.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.EntityFrameworkCore.Tests;
using MySql.Data.EntityFrameworkCore.Tests.DbContextClasses;
using MySQL.Data.EntityFrameworkCore;
using MySQL.Data.EntityFrameworkCore.Extensions;
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
                                        .AddEntityFrameworkMySQL()
                                        .AddSingleton<ILoggerFactory>(new MySqlLoggerFactory());

        string Sql => MySqlLoggerFactory.SqlStatements.LastOrDefault();

        public LoadingRelatedDataTests()
        {         

            options = new DbContextOptionsBuilder()
                         .UseInternalServiceProvider(collection.BuildServiceProvider())
                         .UseMySQL(MySQLTestStore.baseConnectionString + "bd-eagerloading")
                         .Options;

            context = new EagerLoadingContext(options);
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
            Assert.True(addressRelative.All(p => p.Relative!= null));
            // TODO: review what should be the result here (acc. EF tests should be 6)
//            Assert.Equal(13, context.ChangeTracker.Entries().Count());
//            Assert.Equal(@"SELECT `a`.`IdAddressRelative`, `a`.`City`, `a`.`Street`, `p`.`IdRelative`, `p`.`Name`
//FROM `AddressRelative` AS `a`
//INNER JOIN `Persons2` AS `p` ON `a`.`IdAddressRelative` = `p`.`IdRelative`", Sql);
        }



        private void AddData(DbContext context)
        {
            var d = new Address { Street = "Street one", City = "Michigan" };
            var d1 = new Address { Street = "Street two", City = "San Francisco" };
            var d2 = new Address { Street = "Street three", City = "Denver" };

            context.Set<Guest>().AddRange(
                     new Guest { Name = "Guest one", Address = d },
                     new Guest { Name = "Guest two", Address = d1 },
                     new Guest { Name = "Guest three", Address = d2},
                     new Guest { Name = "Guest four"}
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
