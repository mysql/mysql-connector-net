// Copyright © 2016, 2018, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.EntityFrameworkCore.Storage.Internal;
using MySql.Data.EntityFrameworkCore.Tests;
using MySql.Data.EntityFrameworkCore.Tests.DbContextClasses;
using MySql.Data.MySqlClient;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit;

namespace MySql.Data.EntityFrameworkCore.Tests
{
  public class MySQLTypeMapperTests
  {
    [FactOnVersions("5.6.0", null)]
    static void InsertAllDataTypes()
    {
      DateTime now = new DateTime(DateTime.Today.AddSeconds(1).AddMilliseconds(1).AddTicks(10).Ticks);

      using (var context = new AllDataTypesContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.AllDataTypes.Add(new AllDataTypes()
        {
          AddressNumber1 = 1,
          AddressNumber2 = 2,
          AddressNumber3 = 3,
          AddressNumber4 = 4,
          AddressNumber5 = (long)5,
          AddressNumber6 = 6.36f,
          AddressNumber7 = 7.49f,
          AddressNumber8 = 8.64d,
          AddressNumber9 = 9.81m,
          AddressNumber10 = 10,
          BuildingName1 = "BuildingName1",
          BuildingName2 = "BuildingName2",
          BuildingName3 = "BuildingName3",
          BuildingName4 = "BuildingName4",
          BuildingName5 = "BuildingName5",
          BuildingName6 = UTF8Encoding.UTF8.GetBytes("BuildingName6"),
          BuildingName7 = UTF8Encoding.UTF8.GetBytes("BuildingName7"),
          BuildingName8 = UTF8Encoding.UTF8.GetBytes("BuildingName8"),
          BuildingName9 = UTF8Encoding.UTF8.GetBytes("BuildingName9"),
          BuildingName10 = UTF8Encoding.UTF8.GetBytes("BuildingName10"),
          BuildingName11 = "small",
          BuildingName12 = "small,medium,large,medium",
          BuildingName13 = now,
          BuildingName14 = now,
          BuildingName15 = now.TimeOfDay,
          BuildingName16 = now,
          BuildingName17 = (short)now.Year
        });
        context.SaveChanges();
      }

      using (var context = new AllDataTypesContext())
      {
        var data = context.AllDataTypes.First();
        Assert.Equal(1, data.AddressNumber1);
        Assert.Equal(2, data.AddressNumber2);
        Assert.Equal(3, data.AddressNumber3);
        Assert.Equal(4, data.AddressNumber4);
        Assert.Equal((long)5, data.AddressNumber5);
        Assert.Equal(6.36f, data.AddressNumber6);
        Assert.Equal(7.49f, data.AddressNumber7);
        Assert.Equal(8.64d, data.AddressNumber8);
        Assert.Equal(9.81m, data.AddressNumber9);
        Assert.Equal(10, data.AddressNumber10);
        Assert.Equal("BuildingName1", data.BuildingName1);
        Assert.Equal("BuildingName2", data.BuildingName2);
        Assert.Equal("BuildingName3", data.BuildingName3);
        Assert.Equal("BuildingName4", data.BuildingName4);
        Assert.Equal("BuildingName5", data.BuildingName5);
        Assert.Equal("BuildingName6".PadRight(120,'\0'), UTF8Encoding.UTF8.GetString(data.BuildingName6));
        Assert.Equal("BuildingName7", UTF8Encoding.UTF8.GetString(data.BuildingName7));
        Assert.Equal("BuildingName8", UTF8Encoding.UTF8.GetString(data.BuildingName8));
        Assert.Equal("BuildingName9", UTF8Encoding.UTF8.GetString(data.BuildingName9));
        Assert.Equal("BuildingName10", UTF8Encoding.UTF8.GetString(data.BuildingName10));
        Assert.Equal("small", data.BuildingName11);
        Assert.Equal("small,medium,large", data.BuildingName12);
        Assert.Equal(now.Date, data.BuildingName13);
        Assert.Equal(now, data.BuildingName14);
        Assert.Equal(now.TimeOfDay, data.BuildingName15);
        Assert.Equal(now, data.BuildingName16);
        Assert.Equal(now.Year, data.BuildingName17);
      }
    }

    [Fact]
    public void ValidateStringLength()
    {
      using(var context = new StringTypesContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.Database.OpenConnection();
        MySqlConnection conn = (MySqlConnection)context.Database.GetDbConnection();
        MySqlCommand cmd = new MySqlCommand(
          $"DESC StringType",
          conn);
        using(MySqlDataReader reader = cmd.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.Equal("TinyString", reader.GetString("field"));
          Assert.Equal("varchar(767)", reader.GetString("type"));

          Assert.True(reader.Read());
          Assert.Equal("LongString", reader.GetString("field"));
          Assert.Equal("text", reader.GetString("type"));

          Assert.True(reader.Read());
          Assert.Equal("MediumString", reader.GetString("field"));
          Assert.Equal("mediumtext", reader.GetString("type"));

          Assert.True(reader.Read());
          Assert.Equal("NormalString", reader.GetString("field"));
          Assert.Equal("varchar(3000)", reader.GetString("type"));
        }
      }
    }
  }
}
