// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.EntityFrameworkCore.Tests.DbContextClasses;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace MySql.Data.EntityFrameworkCore.Tests
{
  public class MultiSchemaTests
  {
    [Fact]
    public void MultiSchemaTest()
    {
      PopulateData();
      using (BodyShopContext context = new BodyShopContext())
      {
        using (MySqlConnection conn = (MySqlConnection)context.Database.GetDbConnection())
        {
          MySqlCommand cmd = conn.CreateCommand();
          cmd.CommandText = "SHOW DATABASES WHERE `Database` IN('01cars', '02bodyshops', '03employees')";
          conn.Open();
          List<string> databases = new List<string>(new string[] { "01cars", "02bodyshops", "03employees" });
          int count = 0;
          using (MySqlDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              Assert.True(databases.Contains(reader.GetString(0)));
              count++;
            }
          }
          Assert.Equal(3, count);
        }
      }
    }

    [Fact]
    public void LoadingData()
    {
      PopulateData();
      using(BodyShopContext context = new BodyShopContext())
      {
        Assert.Equal(2, context.Car.Count());
        Assert.Equal(2, context.BodyShop.Count());
        Assert.Equal(2, context.Employee.Count());
      }
    }


    private void PopulateData()
    {
      using(BodyShopContext context = new BodyShopContext())
      {
        // Clean databases
        context.Database.EnsureDeleted();
        MySQLTestStore.DeleteDatabase("01cars");
        MySQLTestStore.DeleteDatabase("02bodyshops");
        MySQLTestStore.DeleteDatabase("03employees");
        context.Database.EnsureCreated();

        context.BodyShop.AddRange(new BodyShop
        {
          Name = "Western Collision Works",
          City = "Hollywood",
          State = "California",
          Brand = "Chevrolet"
        },
        new BodyShop
        {
          Name = "Yosemite Auto Body Shop",
          City = "Pico-Union",
          State = "California",
          Brand = "Ford"
        });

        context.Car.AddRange(new Car
        {
          LicensePlate = "ATD 427",
          Make = "Chevrolet",
          Model = "Cruze",
          State = "New"
        },
        new Car
        {
          LicensePlate = "TJC 265",
          Make = "Fore",
          Model = "Escape",
          State = "Used"
        });

        context.Employee.AddRange(new Employee
        {
          FirstName = "Jonh",
          LastName = "Doe",
          DisplayName = "Jonhy",
          Timestamp = DateTime.Now
        },
        new Employee
        {
          FirstName = "Maurice",
          LastName = "Kent",
          DisplayName = "Mau",
          Timestamp = DateTime.Now
        });

        context.SaveChanges();
      }
    }
  }
}
