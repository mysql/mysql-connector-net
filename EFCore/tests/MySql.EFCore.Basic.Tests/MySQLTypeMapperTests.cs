// Copyright © 2021, 2024, Oracle and/or its affiliates.
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
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Basic.Tests.DbContextClasses;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  public class MySQLTypeMapperTests
  {
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
      using (var context = new AllDataTypesContext())
        context.Database.EnsureDeleted();
      using (var context = new StringTypesContext())
        context.Database.EnsureDeleted();
      using (var context = new AllBlobTypesContext())
        context.Database.EnsureDeleted();
    }

    [Test]
    public void InsertAllDataTypes()
    {
      if (!TestUtils.IsAtLeast(5, 7, 0))
      {
        Assert.Ignore();
      }

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
        Assert.That(data.AddressNumber1, Is.EqualTo(1));
        Assert.That(data.AddressNumber2, Is.EqualTo(2));
        Assert.That(data.AddressNumber3, Is.EqualTo(3));
        Assert.That(data.AddressNumber4, Is.EqualTo(4));
        Assert.That(data.AddressNumber5, Is.EqualTo((long)5));
        Assert.That(data.AddressNumber6, Is.EqualTo(6.36f));
        Assert.That(data.AddressNumber7, Is.EqualTo(7.49f));
        Assert.That(data.AddressNumber8, Is.EqualTo(8.64d));
        Assert.That(data.AddressNumber9, Is.EqualTo(9.81m));
        Assert.That(data.AddressNumber10, Is.EqualTo(10));
        Assert.That(data.BuildingName1, Is.EqualTo("BuildingName1"));
        Assert.That(data.BuildingName2, Is.EqualTo("BuildingName2"));
        Assert.That(data.BuildingName3, Is.EqualTo("BuildingName3"));
        Assert.That(data.BuildingName4, Is.EqualTo("BuildingName4"));
        Assert.That(data.BuildingName5, Is.EqualTo("BuildingName5"));
        Assert.That(UTF8Encoding.UTF8.GetString(data.BuildingName6!), Is.EqualTo("BuildingName6".PadRight(120, '\0')));
        Assert.That(UTF8Encoding.UTF8.GetString(data.BuildingName7!), Is.EqualTo("BuildingName7"));
        Assert.That(UTF8Encoding.UTF8.GetString(data.BuildingName8!), Is.EqualTo("BuildingName8"));
        Assert.That(UTF8Encoding.UTF8.GetString(data.BuildingName9!), Is.EqualTo("BuildingName9"));
        Assert.That(UTF8Encoding.UTF8.GetString(data.BuildingName10!), Is.EqualTo("BuildingName10"));
        Assert.That(data.BuildingName11, Is.EqualTo("small"));
        Assert.That(data.BuildingName12, Is.EqualTo("small,medium,large"));
        Assert.That(data.BuildingName13, Is.EqualTo(now.Date));
        Assert.That(data.BuildingName14, Is.EqualTo(now));
        Assert.That(data.BuildingName15, Is.EqualTo(now.TimeOfDay));
        Assert.That(data.BuildingName16, Is.EqualTo(now));
        Assert.That(data.BuildingName17, Is.EqualTo(now.Year));
      }
    }

    /// <summary>
    /// Bug#36208913 Byte array type mapping has a fixed limit of 8000 in EFCore
    /// </summary>
    [Test]
    public void LongBlobMapping()
    {
      using (var context = new AllBlobTypesContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();


        var data1 = new byte[7000];
        var data2 = new byte[60000];
        var data3 = new byte[90000];


        context.AllBlobTypes.Add(new AllBlobTypes()
        {
          Id = 1,
          Example1 = data1,
          Example2 = data2,
          Example3 = data3,
        });
        context.SaveChanges();
      }

      using (var context = new AllBlobTypesContext())
      {

        var data1 = new byte[7000];
        var data2 = new byte[60000];
        var data3 = new byte[90000];

        var data = context.AllBlobTypes.First();

        Assert.That(data.Example1.Length, Is.EqualTo(data1.Length));
        Assert.That(data.Example2.Length, Is.EqualTo(data2.Length));
        Assert.That(data.Example3.Length, Is.EqualTo(data3.Length));
      }
    }

    [Test]
    public void ValidateStringLength()
    {
      using (var context = new StringTypesContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        Dictionary<string, string> validation = new Dictionary<string, string>
        {
          { "TinyString", "varchar(255)" },
          { "NormalString", "varchar(3000)" },
          { "MediumString", "mediumtext" },
          { "LongString", "longtext" }
        };

        context.Database.OpenConnection();
        MySqlConnection conn = (MySqlConnection)context.Database.GetDbConnection();
        MySqlCommand cmd = new MySqlCommand(
          $"DESC StringType",
          conn);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          int counter = 0;
          while (reader.Read())
          {
            string field = reader.GetString("field");
            string type = validation[field];
            Assert.That(reader.GetString("type"), Is.EqualTo(type));
            counter++;
          }
          Assert.That(counter, Is.EqualTo(validation.Count));
        }
      }
    }
  }
}
