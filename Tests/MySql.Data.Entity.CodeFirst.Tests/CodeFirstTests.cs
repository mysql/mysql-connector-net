// Copyright © 2011, 2013, Oracle and/or its affiliates. All rights reserved.
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
#if NET_45_OR_GREATER
using System.Data.Spatial;
#endif
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using MySql.Data.Entity.CodeFirst.Tests.Properties;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Tests;
using NUnit.Framework;
using MySql.Data.Entity;
using MySql.Data.Types;
using System.Xml.Linq;
using System.Collections.Generic;


namespace MySql.Data.Entity.CodeFirst.Tests
{
 
  [TestFixture]
  public class CodeFirstTests : BaseCodeFirstTest
  {
#if NET_45_OR_GREATER
    private DbSpatialServices spatialServices = MySqlSpatialServices.Instance;
#endif

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
      IDataReader r = execReader(string.Format(
@"select numeric_precision, numeric_scale from information_schema.columns 
where table_schema = '{0}' and table_name = 'movies' and column_name = 'Price'", conn.Database));
      r.Read();
      Assert.AreEqual(16, r.GetInt32(0));
      Assert.AreEqual(2, r.GetInt32(1));
    }

    [TearDown]
    public void TearDown()
    {
      ReInitDb();
    }

    private void ReInitDb()
    {
      this.suExecSQL(string.Format("drop database if exists `{0}`", conn.Database));
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

#if NET_45_OR_GREATER    
    #region GeometryFunctionsTests

    //Boundary(g) and IsEmpty are not implemented in MySQL
    //as documented
    //http://dev.mysql.com/doc/refman/5.6/en/functions-that-create-new-geometries-from-existing-ones.html
    [Test]
    public void CanCreateGeogrametryColumn()
    {
      using (SpatialDbContext ctx = new SpatialDbContext())
      {
        ctx.Database.Delete();
        ctx.Database.Create();
        var dis = new SPoint { point = DbGeometry.PointFromText(string.Format("POINT({0} {1})", 47.37, -122.21), 4326) };
        ctx.SPoints.Add(dis);

        var dis_2 = new SPoint { point = DbGeometry.PointFromText(string.Format("POINT({0} {1})", 147.37, -122.21), 4326) };
        ctx.SPoints.Add(dis_2);

        ctx.SaveChanges();

        var points = ctx.SPoints.First();
        Assert.AreEqual("POINT (47.37 -122.21)", points.point.AsText());      
      }
    }

    [Test]
    [ExpectedException(typeof(EntityCommandExecutionException))]
    public void CanThrowExceptionForUnkownFunction()
    {
      using (DistribuitorsContext context = new DistribuitorsContext())
      {
        context.Database.Delete();
        context.Database.Create();

        context.Distributors.Add(new Distributor()
        {
          Name = "Graphic Design Institute",
          point = DbGeometry.FromText("POINT(-122.336106 47.605049)"),
        });

        context.Distributors.Add(new Distributor()
        {
          Name = "School of Fine Art",
          point = DbGeometry.FromText("POINT(-122.335197 47.646711)"),
        });

        context.SaveChanges();

        var myLocation = DbGeometry.FromText("POINT(-122.296623 47.640405)");

        var university = (from u in context.Distributors
                          orderby u.point.Distance(myLocation)
                          select u).FirstOrDefault();

      }
    }


    [Test]  
    public void CanUseDistanceGeometryFunction()
    {
      using (DistribuitorsContext context = new DistribuitorsContext())
      {
        context.Database.Delete();
        context.Database.Create();

        context.Distributors.Add(new Distributor()
        {
          Name = "Graphic Design Institute",
          point = DbGeometry.FromText("POINT(-122.336106 47.605049)"),
        });
        context.SaveChanges();

        var points = (from p in context.Distributors
                      select p.point);

        foreach (var item in points)
          Assert.AreEqual(0.0, DbGeometry.FromText("POINT(-122.336106 47.605049)").Distance(item));
      }
    }


 
    [Test]
    public void CanUseDimensionFunction()
    {
      using (DistribuitorsContext context = new DistribuitorsContext())
      {
        context.Database.Delete();
        context.Database.Create();

        context.Distributors.Add(new Distributor()
        {
          Name = "Graphic Design Institute",
          point = DbGeometry.FromText("POINT(-122.336106 47.605049)"),
        });

        context.Distributors.Add(new Distributor()
        {
          Name = "School of Fine Art",
          point = DbGeometry.FromText("POINT(-122.335197 47.646711)"),
        });

        context.SaveChanges();

        var dimensions = (from u in context.Distributors                          
                          select u.point.Dimension).ToList();

        foreach (var item in dimensions)        
          Assert.AreEqual(0, item);
        
        context.Database.Delete();
      }
    }

    [Test]
    public void CanUseEnvelopeFunction()
    {
      using (DistribuitorsContext context = new DistribuitorsContext())
      {
        context.Database.Delete();
        context.Database.Create();

        context.Distributors.Add(new Distributor()
        {
          Name = "Graphic Design Institute",
          point = DbGeometry.FromText("POINT(-122.336106 47.605049)"),
        });

        context.SaveChanges();

        var envelopes = (from u in context.Distributors
                          select u.point.Envelope.AsText()).ToList();

        foreach (var item in envelopes)
          Assert.AreEqual("POLYGON((-122.336106 47.605049,-122.336106 47.605049,-122.336106 47.605049,-122.336106 47.605049,-122.336106 47.605049))", item);

        context.Database.Delete();
      }
    }

    [Test]
    public void CanUseIsSimpleFunction()
    {
      if (version < new Version(5, 6)) return;
      
      using (DistribuitorsContext context = new DistribuitorsContext())
      {
        context.Database.Delete();
        context.Database.Create();
     
        context.Distributors.Add(new Distributor()
        {
          Name = "Graphic Design Institute",
          point = DbGeometry.FromText("POINT(-122.336106 47.605049)",101),
        });

        context.SaveChanges();
        var result = (from u in context.Distributors
                        select u.point.IsSimple).ToList();

        foreach (var item in result)
          Assert.AreEqual(true, item);

        context.Database.Delete();
      }
    }

    [Test]
    public void CanUseSpatialTypeNameFunction()
    {
      using (DistribuitorsContext context = new DistribuitorsContext())
      {
        context.Database.Delete();
        context.Database.Create();
     
        context.Distributors.Add(new Distributor()
        {
          Name = "Graphic Design Institute",
          point = DbGeometry.FromText("POINT(-122.336106 47.605049)",101),
        });
        
        context.SaveChanges();

        var result = (from u in context.Distributors
                      select u.point.SpatialTypeName).ToList();

        foreach (var item in result)
          Assert.AreEqual("POINT", item);  
        
        context.Database.Delete();
      }
    }

       
    [Test]
    public void CanUseCoordinateSystemIdFunction()
    {
      using (DistribuitorsContext context = new DistribuitorsContext())
      {
        context.Database.Delete();
        context.Database.Create();
     
        context.Distributors.Add(new Distributor()
        {
          Name = "Graphic Design Institute",
          point = DbGeometry.FromText("POINT(-122.336106 47.605049)",101),
        });
        
        context.SaveChanges();

        var result = (from u in context.Distributors
                      select u.point.CoordinateSystemId).ToList();

         foreach (var item in result)
          Assert.AreEqual(101, item);   
        
        context.Database.Delete();
      }
    }

    /// <summary>
    /// This test uses spatial services class
    /// </summary>     
    [Test]
    public void CanGetCoordinatesFromGeometry()
    {
      using (DistribuitorsContext context = new DistribuitorsContext())
      {
        context.Database.Delete();
        context.Database.Create();
     
        context.Distributors.Add(new Distributor()
        {
          Name = "Graphic Design Institute",
          point = DbGeometry.FromText("POINT(-122.336106 47.605049)"),
        });
        
        context.SaveChanges();

        var point = (from u in context.Distributors
                        select u.point).First();        

        Assert.AreEqual(-122.336106, point.XCoordinate);

        Assert.AreEqual(47.605049, point.YCoordinate);  

        var result = (from u in context.Distributors
                        select u.point.XCoordinate).ToList();


        foreach (var item in result)
        {
          Assert.AreEqual(-122.336106, item); 
        }
        context.Database.Delete();
      }
    }


    [Test]
    public void CanUseContainsFunction()
    {
      using (DistribuitorsContext context = new DistribuitorsContext())
      {
        context.Database.Delete();
        context.Database.Create();

        context.Distributors.Add(new Distributor()
        {
          Name = "Graphic Design Institute",
          point = DbGeometry.FromText("POINT(-122.336106 47.605049)"),
        });

        context.SaveChanges();

        var result = (from u in context.Distributors
                      select u.point.Contains(DbGeometry.FromText("POINT (1 1)"))).ToList();


        foreach (var item in result)       
          Assert.AreEqual(false, item);
        
        context.Database.Delete();
      }
    }
     
    [Test]
    public void CanUseGeomatryAsTextFunction()
    {
      using (DistribuitorsContext context = new DistribuitorsContext())
      {
        context.Database.Delete();
        context.Database.Create();

        context.Distributors.Add(new Distributor()
        {
          Name = "Graphic Design Institute",
          point = DbGeometry.FromText("POINT(-122.336106 47.605049)"),
        });
        context.SaveChanges();

        var result = (from u in context.Distributors
                  select u.point.AsText()).ToList();

        foreach (var item in result)       
        Assert.AreEqual("POINT(-122.336106 47.605049)", item);        

         context.Database.Delete();
       }
    }


    [Test]
    public void CanUseBufferFunction()
    {
      if (version < new Version(5, 6)) return;
      
      using (DistribuitorsContext context = new DistribuitorsContext())
      {
        context.Database.Delete();
        context.Database.Create();

        context.Distributors.Add(new Distributor()
        {
          Name = "Graphic Design Institute",
          point = DbGeometry.FromText("POINT(-122.336106 47.605049)"),
        });
        context.SaveChanges();

        var result = (from u in context.Distributors
                        select u.point.Buffer(1212).AsText()).First();        
        
        Assert.AreEqual("POLYGON((-122.336106 -1164.394951,-181.80612728483064 -1162.935043920669,-241.13288007942745 -1158.5588397267024,-300.17344103989853 -1151.2768810773146,-358.78557628354747 -1141.1067108487152,-416.8280840427558 -1128.0728298717872,-474.16113483240827 -1112.2066379074372,-530.6466083113708 -1093.5463580018213,-586.1484260264888 -1072.1369444036795,-640.5328792375019 -1048.0299742656134,-693.668951033109 -1021.2835233902063,-745.4286319621847 -991.9620263203299,-795.6872284197578 -960.1361211106848,-844.3236630448292 -925.8824791545416,-891.2207664063383 -889.2836204756372,-936.2655592745862 -850.4277149302104,-979.3495247980954 -809.4083697980955,-1020.3688699302104 -766.3244042745862,-1059.2247754756372 -721.2796114063383,-1095.8236341545417 -674.3825080448293,-1130.0772761106848 -625.7460734197579,-1161.9031813203298 -575.4874769621847,-1191.2246783902062 -523.727796033109,-1217.9711292656134 -470.5917242375019,-1242.0780994036795 -416.2072710264888,-1263.4875130018213 -360.7054533113708,-1282.1477929074372 -304.2199798324083,-1298.0139848717872 -246.8869290427558,-1311.0478658487152 -188.8444212835475,-1321.2180360773145 -130.2322860398985,-1328.4999947267024 -71.19172507942744,-1332.876198920669 -11.864972284830642,-1334.336106 47.605049,-1328.4999947267024 166.40182307942746,-1321.2180360773145 225.4423840398985,-1311.0478658487152 284.0545192835475,-1298.0139848717872 342.0970270427558,-1282.1477929074372 399.4300778324083,-1263.4875130018213 455.9155513113708,-1242.0780994036795 511.4173690264888,-1217.9711292656134 565.8018222375019,-1191.2246783902062 618.937894033109,-1161.9031813203298 670.6975749621847,-1130.0772761106848 720.9561714197579,-1095.8236341545417 769.5926060448293,-1059.2247754756372 816.4897094063383,-1020.3688699302104 861.5345022745862,-979.3495247980954 904.6184677980955,-936.2655592745862 945.6378129302104,-891.2207664063383 984.4937184756373,-844.3236630448292 1021.0925771545416,-795.6872284197578 1055.3462191106848,-745.4286319621847 1087.1721243203299,-693.668951033109 1116.4936213902063,-640.5328792375019 1143.2400722656134,-586.1484260264888 1167.3470424036796,-530.6466083113708 1188.7564560018213,-474.16113483240827 1207.4167359074372,-416.8280840427558 1223.2829278717873,-358.78557628354747 1236.3168088487153,-300.17344103989853 1246.4869790773146,-241.13288007942745 1253.7689377267025,-181.80612728483064 1258.145141920669,-122.336106 1259.605049,-62.86608471516936 1258.145141920669,-3.5393319205725504 1253.7689377267025,55.501229039898504 1246.4869790773146,114.1133642835475 1236.3168088487153,172.15587204275582 1223.2829278717873,229.4889228324083 1207.4167359074372,285.9743963113708 1188.7564560018213,341.47621402648883 1167.3470424036796,395.86066723750196 1143.2400722656134,448.99673903310907 1116.4936213902063,500.75641996218474 1087.1721243203299,551.0150164197579 1055.3462191106848,599.6514510448293 1021.0925771545416,646.5485544063383 984.4937184756373,691.5933472745862 945.6378129302104,734.6773127980955 904.6184677980955,775.6966579302104 861.5345022745862,814.5525634756373 816.4897094063383,851.1514221545416 769.5926060448293,885.4050641106849 720.9561714197579,917.2309693203299 670.6975749621847,946.5524663902063 618.937894033109,973.2989172656135 565.8018222375019,997.4058874036796 511.4173690264888,1018.8153010018214 455.9155513113708,1037.4755809074372 399.4300778324083,1053.3417728717873 342.0970270427558,1066.3756538487153 284.0545192835475,1076.5458240773146 225.4423840398985,1083.8277827267025 166.40182307942746,1088.203986920669 107.07507028483064,1089.663894 47.605049,1083.8277827267025 -71.19172507942744,1076.5458240773146 -130.2322860398985,1066.3756538487153 -188.8444212835475,1053.3417728717873 -246.8869290427558,1037.4755809074372 -304.2199798324083,1018.8153010018214 -360.7054533113708,997.4058874036796 -416.2072710264888,973.2989172656135 -470.5917242375019,946.5524663902063 -523.727796033109,917.2309693203299 -575.4874769621847,885.4050641106849 -625.7460734197579,851.1514221545416 -674.3825080448293,814.5525634756373 -721.2796114063383,775.6966579302104 -766.3244042745862,734.6773127980955 -809.4083697980955,691.5933472745862 -850.4277149302104,646.5485544063383 -889.2836204756372,599.6514510448293 -925.8824791545416,551.0150164197579 -960.1361211106848,500.75641996218474 -991.9620263203299,448.99673903310907 -1021.2835233902063,395.86066723750196 -1048.0299742656134,341.47621402648883 -1072.1369444036795,285.9743963113708 -1093.5463580018213,229.4889228324083 -1112.2066379074372,172.15587204275582 -1128.0728298717872,114.1133642835475 -1141.1067108487152,55.501229039898504 -1151.2768810773146,-3.5393319205725504 -1158.5588397267024,-62.86608471516936 -1162.935043920669,-122.336106 -1164.394951))", result);

        context.Database.Delete();
      
      }
    }    
    #endregion


    #region SpatialServices
    
    [Test]
    public void CanUseSSContainsFunction()
    {
      using (DistribuitorsContext context = new DistribuitorsContext())
      {
        context.Database.Delete();
        context.Database.Create();

        context.Distributors.Add(new Distributor()
        {
          Name = "Graphic Design Institute",
          point = DbGeometry.FromText("POINT(-122.336106 47.605049)"),
        });
        context.SaveChanges();

        var result = (from u in context.Distributors
                      select u.point).First();

        Assert.AreEqual(result.Contains(DbGeometry.FromText("POINT (1 1)")), false);

        context.Database.Delete();

      }
    }

    [Test]
    public void CanUseCreateProviderValueFunction()
    {
      using (DistribuitorsContext context = new DistribuitorsContext())
      {
        context.Database.Delete();
        context.Database.Create();

        context.Distributors.Add(new Distributor()
        {
          Name = "Graphic Design Institute",
          point = DbGeometry.FromText("POINT(-122.336106 47.605049)"),
        });
        context.SaveChanges();

        var point = (from u in context.Distributors
                      select u.point).First();      
     
        var geometryWellKnownValueWKT = new DbGeometryWellKnownValue()
        {
          CoordinateSystemId = 0,
          WellKnownBinary = null,
          WellKnownText = "POINT(1 2)"
        };

        MySqlGeometry providerValue = (MySqlGeometry)spatialServices.CreateProviderValue(geometryWellKnownValueWKT);
        Assert.AreEqual("POINT(1 2)", providerValue.ToString());


        var geometryWellKnownValueWKB = new DbGeometryWellKnownValue()
        {
          CoordinateSystemId = 0,
          WellKnownBinary = providerValue.Value,
          WellKnownText = null
        };

        MySqlGeometry providerValue_2 = (MySqlGeometry)spatialServices.CreateProviderValue(geometryWellKnownValueWKB);
        Assert.AreEqual("POINT(1 2)", providerValue_2.ToString());

        context.Database.Delete();
      }
    }    
    #endregion
#endif
  
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

