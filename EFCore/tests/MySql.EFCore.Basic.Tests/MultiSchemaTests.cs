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
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySql.EntityFrameworkCore.Basic.Tests
{
  public class MultiSchemaTests
  {
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
      using (BodyShopContext context = new BodyShopContext())
      {
        using (MySqlConnection conn = (MySqlConnection)context.Database.GetDbConnection())
        {
          MySqlCommand cmd = conn.CreateCommand();
          cmd.CommandText = @"DROP DATABASE IF EXISTS `01cars`;
            DROP DATABASE IF EXISTS `02bodyshops`;
            DROP DATABASE IF EXISTS `03employees`;
            DROP DATABASE IF EXISTS `db-bodyshopinsertcontext`;";
          conn.Open();
          context.Database.EnsureDeleted();
          cmd.Connection = conn;
          cmd.ExecuteNonQuery();
        }
      }
    }

    [Test]
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
              Assert.Contains(reader.GetString(0), databases);
              count++;
            }
          }
          Assert.AreEqual(3, count);
        }
      }
    }

    [Test]
    public void LoadingData()
    {
      using (BodyShopInsertContext context = new BodyShopInsertContext())
      {
        context.Database.EnsureDeleted();
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
          Timestamp = DateTime.Now
        },
        new Employee
        {
          FirstName = "Maurice",
          LastName = "Kent",
          Timestamp = DateTime.Now
        });

        context.SaveChanges();
        Assert.AreEqual(2, context.Car.Count());
        Assert.AreEqual(2, context.BodyShop.Count());
        Assert.AreEqual(2, context.Employee.Count());
      }
    }

    private void PopulateData()
    {
      using (BodyShopContext context = new BodyShopContext())
      {
        // Clean databases
        context.Database.EnsureDeleted();
        MySQLTestStore.DeleteDatabase("01cars");
        MySQLTestStore.DeleteDatabase("02bodyshops");
        MySQLTestStore.DeleteDatabase("03employees");
        context.Database.EnsureCreated();
      }
    }
  }
}