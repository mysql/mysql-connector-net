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
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Basic.Tests.DbContextClasses;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  public class DataTests
  {
    [TearDown]
    public void TearDown()
    {
      using (var context = new WorldContext())
        context.Database.EnsureDeleted();
      using (var context = new MyContext())
        context.Database.EnsureDeleted();
      using (var context = new SakilaLiteContext())
        context.Database.EnsureDeleted();
    }

    [Test]
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
        Assert.IsNull(result.Exception);
        Assert.AreEqual(4, result.Result);
      }

      using (var context = new WorldContext())
      {
        var continent = await context.FindAsync<Continent>("AS");
        Assert.AreEqual("Asia", continent!.Name);

        var continents = await context.Continents.ToListAsync();
        Assert.AreEqual(4, continents.Count);
      }
    }

    [Test]
    public void ZeroDatetime()
    {
      using (MyContext context = new MyContext())
      {
        MySqlConnectionStringBuilder sb = new MySqlConnectionStringBuilder(context.Database.GetDbConnection().ConnectionString);
        sb.ConvertZeroDateTime = true;

        context.Database.GetDbConnection().ConnectionString = sb.ToString();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        Assert.AreEqual(1, context.Database.ExecuteSqlInterpolated($"INSERT IGNORE INTO MyTest (`Date`) VALUES('0000-00-00')"));

        var item = context.MyTest.First();
        Assert.AreEqual(DateTime.MinValue, item.Date);
      }
    }

    [Test]
    public void SakilaLiteTest()
    {
      using (SakilaLiteContext context = new SakilaLiteContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
      }
    }
  }
}
