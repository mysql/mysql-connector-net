// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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

namespace EntityFrameworkCore.Basic.Tests
{
    public class LoadingRelatedDataTests : IDisposable
    {
        private static string Sql => MySqlLoggerFactory.SqlStatements?.Count() > 0 ?
                                     MySqlLoggerFactory.SqlStatements.Last() :
                                     string.Empty;

        DbContext context;

        public LoadingRelatedDataTests()
        {
            //var serviceProvider = new ServiceCollection()
            //             .AddEntityFrameworkMySQL()
            //             .AddSingleton<ILoggerFactory>(new MySqlLoggerFactory())
            //             .AddDbContext<EagerLoadingContext>()
            //             .BuildServiceProvider();

            //context = serviceProvider.GetRequiredService<EagerLoadingContext>();

            var options = new DbContextOptionsBuilder()
            .UseMySQL(MySQLTestStore.baseConnectionString + "bd-eagerloading")
            .UseInternalServiceProvider(new ServiceCollection()
                .AddEntityFrameworkMySQL()                
                .AddSingleton<ILoggerFactory>(new MySqlLoggerFactory())
                .BuildServiceProvider())
            .Options;

            context = new EagerLoadingContext(options);           
            context.Database.EnsureCreated();
                            
        }


        [Fact]
        public void CanIncludeAddressData()
        {
            Assert.False(context.Database.EnsureCreated());

            AddData(context);

                var people
                    = context.Set<Guest>()
                        .Include(p => p.Address)
                        .ToList();

                Assert.Equal(4, people.Count);
                Assert.Equal(3, people.Count(p => p.Address != null));
                Assert.Equal("", Sql);
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

            context.Set<AddressRelative>().AddRange(ad, ad1, ad2);

            context.Set<Relative>().AddRange(
                   new Relative { Name = "L. J.", Address = ad },
                   new Relative { Name = "M. M.", Address = ad1 },
                   new Relative { Name = "Z. Z.", Address = ad2 }
                );

            context.SaveChanges();
        }

        public void Dispose()
        {
           context.Database.EnsureDeleted();          
        }
    }
}
