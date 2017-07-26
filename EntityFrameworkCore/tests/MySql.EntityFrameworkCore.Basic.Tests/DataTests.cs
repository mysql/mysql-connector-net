// Copyright © 2017 Oracle and/or its affiliates. All rights reserved.
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
using System.Threading.Tasks;
using Xunit;

namespace MySql.Data.EntityFrameworkCore.Tests
{
    public class DataTests
    {

    [Fact]
    public async Task AsyncData()
    {
      using (var context = new FiguresContext())
      {
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        Triangle t1 = new Triangle
        {
          Base = 5,
          Height = 5
        };
        Triangle t2 = new Triangle
        {
          Base = 8,
          Height = 14
        };
        Triangle t3 = new Triangle
        {
          Base = 12,
          Height = 3
        };

        await context.Triangle.AddAsync(t1);
        await context.Triangle.AddRangeAsync(new Triangle[] { t2, t3 });

        var result = context.SaveChangesAsync();
        result.Wait(10_000);
        Assert.Null(result.Exception);
        Assert.Equal(3, result.Result);
      }

      using (var context = new FiguresContext())
      {
        var triangle = await context.FindAsync<Triangle>(1);
        Assert.Equal(5, triangle.Height);

        var triangles = await context.Triangle.ToListAsync();
        Assert.Equal(3, triangles.Count);
        Assert.Equal(8, triangles.First(c => c.Id == 2).Base);
      }
    }
  }
}
