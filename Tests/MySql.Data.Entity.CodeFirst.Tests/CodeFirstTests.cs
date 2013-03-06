// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.EntityClient;
using System.Data.Objects;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using MySql.Data.Entity.CodeFirst.Tests.Properties;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Tests;
using NUnit.Framework;
using System.Xml.Linq;
using System.Collections.Generic;

namespace MySql.Data.Entity.CodeFirst.Tests
{
  [TestFixture]
  public class CodeFirstTests : BaseCodeFirstTest
  {
    /// <summary>
    /// Tests for fix of http://bugs.mysql.com/bug.php?id=61230
    /// ("The provider did not return a ProviderManifestToken string.").
    /// </summary>
    [Test]
    public void SimpleCodeFirstSelect()
    {
      MovieDBContext db = new MovieDBContext();
      db.Database.Initialize(true);
      var l = db.Movies.ToList();
      foreach (var i in l)
      {
      }
    }

    /// <summary>
    /// Tests for fix of http://bugs.mysql.com/bug.php?id=62150
    /// ("EF4.1, Code First, CreateDatabaseScript() generates an invalid MySQL script.").
    /// </summary>
    [Test]
    public void AlterTableTest()
    {
      ReInitDb();
      MovieDBContext db = new MovieDBContext();      
      db.Database.Initialize(true);
      var l = db.MovieFormats.ToList();
      foreach (var i in l)
      {
      }
      MovieFormat m = new MovieFormat();
      m.Format = 8.0f;
      db.MovieFormats.Add(m);
      db.SaveChanges();
    }

    /// <summary>
    /// Fix for "Connector/Net Generates Incorrect SELECT Clause after UPDATE" (MySql bug #62134, Oracle bug #13491689).
    /// </summary>
    [Test]
    public void ConcurrencyCheck()
    {
      using (MovieDBContext db = new MovieDBContext())
      {
        db.Database.ExecuteSqlCommand(
@"DROP TABLE IF EXISTS `test3`.`MovieReleases`");

        db.Database.ExecuteSqlCommand(
@"CREATE TABLE `MovieReleases` (
  `Id` int(11) NOT NULL,
  `Name` varbinary(45) NOT NULL,
  `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=binary");
        MySqlTrace.Listeners.Clear();
        MySqlTrace.Switch.Level = SourceLevels.All;
        GenericListener listener = new GenericListener();
        MySqlTrace.Listeners.Add(listener);
        try
        {
          MovieRelease mr = db.MovieReleases.Create();
          mr.Id = 1;
          mr.Name = "Commercial";
          db.MovieReleases.Add(mr);
          db.SaveChanges();
          mr.Name = "Director's Cut";
          db.SaveChanges();
        }
        finally
        {
          db.Database.ExecuteSqlCommand(@"DROP TABLE IF EXISTS `MovieReleases`");
        }
        // Check sql        
        Regex rx = new Regex(@"Query Opened: (?<item>UPDATE .*)", RegexOptions.Compiled | RegexOptions.Singleline);
        foreach (string s in listener.Strings)
        {
          Match m = rx.Match(s);
          if (m.Success)
          {
            CheckSql(m.Groups["item"].Value, SQLSyntax.UpdateWithSelect);
            Assert.Pass();
          }
        }
        Assert.Fail();
      }
    }

    /// <summary>
    /// This tests fix for http://bugs.mysql.com/bug.php?id=64216.
    /// </summary>
    [Test]
    public void CheckByteArray()
    {
      MovieDBContext db = new MovieDBContext();
      db.Database.Initialize(true);
      string dbCreationScript =
        ((IObjectContextAdapter)db).ObjectContext.CreateDatabaseScript();
      Regex rx = new Regex(@"`Data` (?<type>[^\),]*)", RegexOptions.Compiled | RegexOptions.Singleline);
      Match m = rx.Match(dbCreationScript);
      Assert.AreEqual("longblob", m.Groups["type"].Value);
    }

/// <summary>
    /// Validates a stored procedure call using Code First
    /// Bug #14008699
    [Test]
    public void CallStoredProcedure()
    {
      using (MovieDBContext context = new MovieDBContext())
      {
        context.Database.Initialize(true);
        int count = context.Database.SqlQuery<int>("GetCount").First();

        Assert.AreEqual(5, count);
      }
    }

    /// <summary>
    /// Tests for fix of http://bugs.mysql.com/bug.php?id=63920
    /// Maxlength error when it's used code-first and inheritance (discriminator generated column)
    /// </summary>
    [Test]
    public void Bug63920_Test1()
    {
      ReInitDb();
      using (VehicleDbContext context = new VehicleDbContext())
      {
        context.Database.Delete();
        context.Database.Initialize(true);
        
        context.Vehicles.Add(new Car { Id = 1, Name = "Mustang", Year = 2012, CarProperty = "Car" });
        context.Vehicles.Add(new Bike { Id = 101, Name = "Mountain", Year = 2011, BikeProperty = "Bike" });
        context.SaveChanges();

        var list = context.Vehicles.ToList();

        int records = -1;
        using (MySqlConnection conn = new MySqlConnection(context.Database.Connection.ConnectionString))
        {
          conn.Open();
          MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Vehicles", conn);
          records = Convert.ToInt32(cmd.ExecuteScalar());
        }

        Assert.AreEqual(context.Vehicles.Count(), records);
      }
    }

    /// <summary>
    /// Tests for fix of http://bugs.mysql.com/bug.php?id=63920
    /// Key reference generation script error when it's used code-first and a single table for the inherited models
    /// </summary>
    [Test]
    public void Bug63920_Test2()
    {
      ReInitDb();
      using (VehicleDbContext2 context = new VehicleDbContext2())
      {
        context.Database.Delete();
        context.Database.Initialize(true);

        context.Vehicles.Add(new Car2 { Id = 1, Name = "Mustang", Year = 2012, CarProperty = "Car" });
        context.Vehicles.Add(new Bike2 { Id = 101, Name = "Mountain", Year = 2011, BikeProperty = "Bike" });
        context.SaveChanges();

        var list = context.Vehicles.ToList();

        int records = -1;
        using (MySqlConnection conn = new MySqlConnection(context.Database.Connection.ConnectionString))
        {
          conn.Open();
          MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM Vehicle2", conn);
          records = Convert.ToInt32(cmd.ExecuteScalar());
        }

        Assert.AreEqual(context.Vehicles.Count(), records);
      }     
    }

    /// <summary>
    /// This test fix for precision customization for columns bug (http://bugs.mysql.com/bug.php?id=65001), 
    /// Trying to customize column precision in Code First does not work).
    /// </summary>
    [Test]
    public void TestPrecisionNscale()
    {
      MovieDBContext db = new MovieDBContext();
      db.Database.Initialize(true);
      var l = db.Movies.ToList();
      IDataReader r = execReader( string.Format( 
@"select numeric_precision, numeric_scale from information_schema.columns 
where table_schema = '{0}' and table_name = 'movies' and column_name = 'Price'", conn.Database ));
      r.Read();
      Assert.AreEqual( 16, r.GetInt32( 0 ) );
      Assert.AreEqual( 2, r.GetInt32( 1 ) );
    }

    [TearDown]
    public void TearDown()
    {
      ReInitDb();
    }

    private void ReInitDb()
    {
      this.suExecSQL( string.Format( "drop database if exists `{0}`", conn.Database ));
      this.suExecSQL(string.Format("create database `{0}`", conn.Database));
    }

    /// <summary>
    /// Test String types to StoreType for String
    /// A string with FixedLength=true will become a char 
    /// Max Length left empty will be char(max)
    /// Max Length(100) will be char(100) 
    /// while FixedLength=false will result in nvarchar. 
    /// Max Length left empty will be nvarchar(max)
    /// Max Length(100) will be nvarchar(100)                
    /// </summary>
    [Test]
    public void TestStringTypeToStoreType()
    {
      using (VehicleDbContext3 context = new VehicleDbContext3())
      {
        if (context.Database.Exists()) context.Database.Delete();
        context.Database.CreateIfNotExists();
        context.Accessories.Add(new Accessory { Name = "Accesory One", Description = "Accesories descriptions", LongDescription = "Some long description" });
        context.SaveChanges();

        using (MySqlConnection conn = new MySqlConnection(context.Database.Connection.ConnectionString))
        {
          conn.Open();
          MySqlCommand query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Accessories' and column_name ='Description'", conn);
          query.Connection = conn;
          MySqlDataReader reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.AreEqual("Description", reader[0].ToString());
            Assert.AreEqual("NO", reader[1].ToString());
            Assert.AreEqual("mediumtext", reader[2].ToString());
          }
          reader.Close();

          query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type, character_maximum_length from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Accessories' and column_name ='Name'", conn);
          reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.AreEqual("Name", reader[0].ToString());
            Assert.AreEqual("NO", reader[1].ToString());
            Assert.AreEqual("varchar", reader[2].ToString());
            Assert.AreEqual("255", reader[3].ToString());
          }
          reader.Close();

          query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type, character_maximum_length from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Accessories' and column_name ='LongDescription'", conn);
          reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.AreEqual("LongDescription", reader[0].ToString());
            Assert.AreEqual("NO", reader[1].ToString());
            Assert.AreEqual("longtext", reader[2].ToString());
            Assert.AreEqual("4294967295", reader[3].ToString());
          }
        }
      }
    }

    /// <summary>
    /// Test fix for http://bugs.mysql.com/bug.php?id=66066 / http://clustra.no.oracle.com/orabugs/bug.php?id=14479715
    /// (Using EF, crash when generating insert with no values.).
    /// </summary>
    [Test]
    public void AddingEmptyRow()
    {
      using (MovieDBContext ctx = new MovieDBContext())
      {
        ctx.Database.Initialize(true);
        ctx.EntitySingleColumns.Add(new EntitySingleColumn());
        ctx.SaveChanges();
      }

      using (MovieDBContext ctx2 = new MovieDBContext())
      {
        var q = from esc in ctx2.EntitySingleColumns where esc.Id == 1 select esc;
        Assert.AreEqual(1, q.Count());
      }
    }

/// <summary>
    /// Test for identity columns when type is Integer or Guid (auto-generate
    /// values)
    /// </summary>
    [Test]
    public void IdentityTest()
    {
      using (VehicleDbContext context = new VehicleDbContext())
      {
        if (context.Database.Exists()) context.Database.Delete();
        context.Database.CreateIfNotExists();

        // Identity as Guid
        Manufacturer nissan = new Manufacturer
        {
          Name = "Nissan"
        };
        Manufacturer ford = new Manufacturer
        {
          Name = "Ford"
        };
        context.Manufacturers.Add(nissan);
        context.Manufacturers.Add(ford);

        // Identity as Integer
        Distributor dis1 = new Distributor
        {
          Name = "Distributor1"
        };
        Distributor dis2 = new Distributor
        {
          Name = "Distributor2"
        };
        context.Distributors.Add(dis1);
        context.Distributors.Add(dis2);

        context.SaveChanges();

        using (MySqlConnection conn = new MySqlConnection(context.Database.Connection.ConnectionString))
        {
          conn.Open();

          // Validates Guid
          MySqlCommand cmd = new MySqlCommand("SELECT * FROM Manufacturers", conn);
          MySqlDataReader dr = cmd.ExecuteReader();
          if (!dr.HasRows)
            Assert.Fail("No records found");
          while (dr.Read())
          {
            string name = dr.GetString(1);
            switch (name)
            {
              case "Nissan":
                Assert.AreEqual(dr.GetGuid(0), nissan.ManufacturerId);
                Assert.AreEqual(dr.GetGuid(2), nissan.GroupIdentifier);
                break;
              case "Ford":
                Assert.AreEqual(dr.GetGuid(0), ford.ManufacturerId);
                Assert.AreEqual(dr.GetGuid(2), ford.GroupIdentifier);
                break;
              default:
                Assert.Fail();
                break;
            }
          }
          dr.Close();

          // Validates Integer
          cmd = new MySqlCommand("SELECT * FROM Distributors", conn);
          dr = cmd.ExecuteReader();
          if (!dr.HasRows)
            Assert.Fail("No records found");
          while (dr.Read())
          {
            string name = dr.GetString(1);
            switch (name)
            {
              case "Distributor1":
                Assert.AreEqual(dr.GetInt32(0), dis1.DistributorId);
                break;
              case "Distributor2":
                Assert.AreEqual(dr.GetInt32(0), dis2.DistributorId);
                break;
              default:
                Assert.Fail();
                break;
            }
          }
          dr.Close();
        }
      }
    }

    /// <summary>
    /// This test the fix for bug 67377.
    /// </summary>
    [Test]
    public void FirstOrDefaultNested()
    {
      ReInitDb();
      using (MovieDBContext ctx = new MovieDBContext())
      {
        ctx.Database.Initialize(true);
        int DirectorId = 1;
        var q = ctx.Movies.Where(p => p.Director.ID == DirectorId).Select(p => 
          new
          {
            Id = p.ID,
            FirstMovieFormat = p.Formats.Count == 0 ? 0.0 : p.Formats.FirstOrDefault().Format
          });
        string sql = q.ToString();
        foreach (var r in q)
        {
        }
      }
    }
  
     /// <summary>
    /// SUPPORT FOR DATE TYPES WITH PRECISION
    /// </summary>
    [Test]
    public void CanDefineDatesWithPrecisionFor56()
    {
      if (Version < new Version(5, 6)) return;

      using (var db = new ProductsDbContext())
      {
        db.Database.CreateIfNotExists();
        using (MySqlConnection conn = new MySqlConnection(db.Database.Connection.ConnectionString))
        {
          conn.Open();
          MySqlCommand query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type, DateTime_Precision from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Products' and column_name ='DateTimeWithPrecision'", conn);
          query.Connection = conn;
          MySqlDataReader reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.AreEqual("DateTimeWithPrecision", reader[0].ToString());
            Assert.AreEqual("NO", reader[1].ToString());
            Assert.AreEqual("datetime", reader[2].ToString());
            Assert.AreEqual("3", reader[3].ToString());
          }
          reader.Close();

          query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type, DateTime_Precision from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Products' and column_name ='TimeStampWithPrecision'", conn);
          query.Connection = conn;
          reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.AreEqual("TimeStampWithPrecision", reader[0].ToString());
            Assert.AreEqual("NO", reader[1].ToString());
            Assert.AreEqual("timestamp", reader[2].ToString());
            Assert.AreEqual("3", reader[3].ToString());
          }
          reader.Close();
        }
        db.Database.Delete();
      }
    }

    /// <summary>
    /// Orabug #15935094 SUPPORT FOR CURRENT_TIMESTAMP AS DEFAULT FOR DATETIME WITH EF
    /// </summary>
    [Test]
    public void CanDefineDateTimeAndTimestampWithIdentity()
    {
      ReInitDb();
      if (Version < new Version(5, 6)) return;

      using (var db = new ProductsDbContext())
      {
        //db.Database.CreateIfNotExists();
        db.Database.Initialize(true);
        Product product = new Product
        {
          //Omitting Identity Columns
          DateTimeWithPrecision = DateTime.Now,
          TimeStampWithPrecision = DateTime.Now
        };

        db.Products.Add(product);
        db.SaveChanges();

        var updateProduct = db.Products.First();
        updateProduct.DateTimeWithPrecision = new DateTime(2012, 3, 18, 23, 9, 7, 6);
        db.SaveChanges();

        Assert.AreNotEqual(null, db.Products.First().Timestamp);
        Assert.AreNotEqual(null, db.Products.First().DateCreated);
        Assert.AreEqual(new DateTime(2012, 3, 18, 23, 9, 7, 6), db.Products.First().DateTimeWithPrecision);
        Assert.AreEqual(1, db.Products.Count());

        db.Database.Delete();
      }
    }

    /// <summary>
    /// Test of fix for bug Support for EntityFramework 4.3 Code First Generated Identifiers (MySql Bug #67285, Oracle bug #16286397).
    /// FKs are renamed to met http://dev.mysql.com/doc/refman/5.0/en/identifiers.html limitations.
    /// </summary>
    [Test]
    public void LongIdentifiersInheritanceTPT()
    {
      ReInitDb();
      using (DinosauriaDBContext db = new DinosauriaDBContext())
      {
        db.Database.Initialize(true);
        Tyrannosauridae ty = new Tyrannosauridae() { Id = 1, Name = "Genghis Rex", SpecieName = "TRex", Weight = 1000 };
        db.dinos.Add(ty);
        Oviraptorosauria ovi = new Oviraptorosauria() { Id = 2, EggsPerYear = 100, Name = "John the Velociraptor", SpecieName = "Oviraptor" };
        db.dinos.Add(ovi);
        db.SaveChanges();
      }
    }
  }
}

