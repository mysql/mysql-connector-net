﻿// Copyright © 2017 Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MySql.Data.EntityFrameworkCore.Tests
{
    public class DataTests
    {

    [Fact]
    public async Task AsyncData()
    {
      using (var context = new WorldContext())
      {
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var america = new Continent { Code = "AM", Name = "America" };
        var europe = new Continent { Code = "EU", Name = "Europe" };
        var asia = new Continent { Code = "AS", Name = "Asia" };
        var africa = new Continent { Code = "AF", Name = "Africa" };

        await context.AddAsync(america);
        await context.AddRangeAsync(europe, asia, africa);

        var result = context.SaveChangesAsync();
        result.Wait(30_000);
        Assert.Null(result.Exception);
        Assert.Equal(4, result.Result);
      }

      using (var context = new WorldContext())
      {
        var continent = await context.FindAsync<Continent>("AS");
        Assert.Equal("Asia", continent.Name);

        var continents = await context .Continents.ToListAsync();
        Assert.Equal(4, continents.Count);
      }
    }

    [Fact]
    public void ZeroDatetime()
    {
      using (MyContext context = new MyContext())
      {
        MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(context.Database.GetDbConnection().ConnectionString);
        sb.ConvertZeroDateTime = true;

        context.Database.GetDbConnection().ConnectionString = sb.ToString();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        Assert.Equal(1, context.Database.ExecuteSqlCommand("INSERT IGNORE INTO MyTest (`Date`) VALUES('0000-00-00')"));

        var item = context.MyTest.First();
        Assert.Equal(DateTime.MinValue, item.Date);
      }
    }
  }
}
