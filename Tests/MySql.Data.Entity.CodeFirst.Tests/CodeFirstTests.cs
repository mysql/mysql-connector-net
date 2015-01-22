// Copyright © 2014, 2015 Oracle and/or its affiliates. All rights reserved.
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
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using MySql.Data.Entity.CodeFirst.Tests.Properties;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Tests;
using System.Xml.Linq;
using System.Collections.Generic;
using Xunit;
#if EF6
using System.Data.Entity.Migrations.History;
using System.Data.Entity.Spatial;
#else
using System.Data.EntityClient;
using System.Data.Objects;
#if NET_45_OR_GREATER
using System.Data.Spatial;
#endif
#endif

namespace MySql.Data.Entity.CodeFirst.Tests
{
  public class CodeFirstTests : IUseFixture<SetUpCodeFirstTests>, IDisposable
  {
    private SetUpCodeFirstTests st;

    public void SetFixture(SetUpCodeFirstTests data)
    {
      st = data;
    }

    public void Dispose()
    {
    }

    private void ReInitDb()
    {
      st.suExecSQL(string.Format("drop database if exists `{0}`", st.conn.Database));
      st.suExecSQL(string.Format("create database `{0}`", st.conn.Database));
    }

    /// <summary>
    /// Tests for fix of http://bugs.mysql.com/bug.php?id=61230
    /// ("The provider did not return a ProviderManifestToken string.").
    /// </summary>
    [Fact]
    public void SimpleCodeFirstSelect()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      MovieDBContext db = new MovieDBContext();
      db.Database.Initialize(true);
#if EF6
      MovieDBInitialize.DoDataPopulation(db);
#endif
      var l = db.Movies.ToList();
      int j = l.Count;
      foreach (var i in l)
      {
        j--;
      }
      Assert.Equal(0, j);
    }

    /// <summary>
    /// Tests for fix of http://bugs.mysql.com/bug.php?id=62150
    /// ("EF4.1, Code First, CreateDatabaseScript() generates an invalid MySQL script.").
    /// </summary>
    [Fact]
    public void AlterTableTest()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      MovieDBContext db = new MovieDBContext();      
      db.Database.Initialize(true);
#if EF6
      MovieDBInitialize.DoDataPopulation(db);
#endif
      var l = db.MovieFormats.ToList();
      int j = l.Count;
      foreach (var i in l)
      {
        j--;
      }
      Assert.Equal(0, j);
      MovieFormat m = new MovieFormat();
      m.Format = 8.0f;
      db.MovieFormats.Add(m);
      db.SaveChanges();
      MovieFormat m2 = db.MovieFormats.Where(p => p.Format == 8.0f).FirstOrDefault();
      Assert.NotNull(m2);
      Assert.Equal( 8.0f, m2.Format);
    }

    /// <summary>
    /// Fix for "Connector/Net Generates Incorrect SELECT Clause after UPDATE" (MySql bug #62134, Oracle bug #13491689).
    /// </summary>
    [Fact]
    public void ConcurrencyCheck()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (MovieDBContext db = new MovieDBContext())
      {
        db.Database.Delete();
        db.Database.CreateIfNotExists();
#if EF6
        MovieDBInitialize.DoDataPopulation(db);
#endif        
        db.Database.ExecuteSqlCommand(@"DROP TABLE IF EXISTS `MovieReleases`");

        db.Database.ExecuteSqlCommand(
@"CREATE TABLE IF NOT EXISTS `MovieReleases` (
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
            st.CheckSql(m.Groups["item"].Value, SQLSyntax.UpdateWithSelect);
            //Assert.Pass();
          }
        }
        //Assert.Fail();
      }
    }

    /// <summary>
    /// This tests fix for http://bugs.mysql.com/bug.php?id=64216.
    /// </summary>
    [Fact]
    public void CheckByteArray()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      MovieDBContext db = new MovieDBContext();
      db.Database.Initialize(true);
      string dbCreationScript =
        ((IObjectContextAdapter)db).ObjectContext.CreateDatabaseScript();
      Regex rx = new Regex(@"`Data` (?<type>[^\),]*)", RegexOptions.Compiled | RegexOptions.Singleline);
      Match m = rx.Match(dbCreationScript);
      Assert.Equal("longblob", m.Groups["type"].Value);
    }

/// <summary>
    /// Validates a stored procedure call using Code First
    /// Bug #14008699
    [Fact]
    public void CallStoredProcedure()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (MovieDBContext context = new MovieDBContext())
      {
        context.Database.Initialize(true);
        context.Database.ExecuteSqlCommand(@"drop procedure if exists `GetCount`");
        context.Database.ExecuteSqlCommand(@"create procedure `GetCount`() begin select 5; end;");
#if EF6
        long count = context.Database.SqlQuery<long>("GetCount").First();
#else
        int count = context.Database.SqlQuery<int>("GetCount").First();
#endif

        Assert.Equal(5, count);
      }
    }

    /// <summary>
    /// Tests for fix of http://bugs.mysql.com/bug.php?id=63920
    /// Maxlength error when it's used code-first and inheritance (discriminator generated column)
    /// </summary>
    [Fact]
    public void Bug63920_Test1()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
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

        Assert.Equal(context.Vehicles.Count(), records);
      }
    }

    /// <summary>
    /// Tests for fix of http://bugs.mysql.com/bug.php?id=63920
    /// Key reference generation script error when it's used code-first and a single table for the inherited models
    /// </summary>
    [Fact]
    public void Bug63920_Test2()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
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

        Assert.Equal(context.Vehicles.Count(), records);
      }     
    }

    /// <summary>
    /// This test fix for precision customization for columns bug (http://bugs.mysql.com/bug.php?id=65001), 
    /// Trying to customize column precision in Code First does not work).
    /// </summary>
    [Fact]
    public void TestPrecisionNscale()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      MovieDBContext db = new MovieDBContext();
      db.Database.Initialize(true);
      var l = db.Movies.ToList();
      IDataReader r = st.execReader( string.Format( 
@"select numeric_precision, numeric_scale from information_schema.columns 
where table_schema = '{0}' and table_name = 'movies' and column_name = 'Price'", st.conn.Database ));
      r.Read();
      Assert.Equal( 16, r.GetInt32( 0 ) );
      Assert.Equal( 2, r.GetInt32( 1 ) );
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
    [Fact]
    public void TestStringTypeToStoreType()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
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
            Assert.Equal("Description", reader[0].ToString());
            Assert.Equal("NO", reader[1].ToString());
            Assert.Equal("mediumtext", reader[2].ToString());
          }
          reader.Close();

          query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type, character_maximum_length from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Accessories' and column_name ='Name'", conn);
          reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.Equal("Name", reader[0].ToString());
            Assert.Equal("NO", reader[1].ToString());
            Assert.Equal("varchar", reader[2].ToString());
            Assert.Equal("255", reader[3].ToString());
          }
          reader.Close();

          query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type, character_maximum_length from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Accessories' and column_name ='LongDescription'", conn);
          reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.Equal("LongDescription", reader[0].ToString());
            Assert.Equal("NO", reader[1].ToString());
            Assert.Equal("longtext", reader[2].ToString());
            Assert.Equal("4294967295", reader[3].ToString());
          }
        }
      }
    }

    /// <summary>
    /// Test fix for http://bugs.mysql.com/bug.php?id=66066 / http://clustra.no.oracle.com/orabugs/bug.php?id=14479715
    /// (Using EF, crash when generating insert with no values.).
    /// </summary>
    [Fact]
    public void AddingEmptyRow()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (MovieDBContext ctx = new MovieDBContext())
      {
        ctx.Database.Initialize(true);
        ctx.EntitySingleColumns.Add(new EntitySingleColumn());
        ctx.SaveChanges();
      }

      using (MovieDBContext ctx2 = new MovieDBContext())
      {
        var q = from esc in ctx2.EntitySingleColumns where esc.Id == 1 select esc;
        Assert.Equal(1, q.Count());
      }
    }

/// <summary>
    /// Test for identity columns when type is Integer or Guid (auto-generate
    /// values)
    /// </summary>
    [Fact]
    public void IdentityTest()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (VehicleDbContext context = new VehicleDbContext())
      {
        context.Database.ExecuteSqlCommand("SET GLOBAL sql_mode='STRICT_ALL_TABLES'");
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
          Assert.False(!dr.HasRows, "No records found");

          while (dr.Read())
          {
            string name = dr.GetString(1);
            switch (name)
            {
              case "Nissan":
                Assert.Equal(dr.GetGuid(0), nissan.ManufacturerId);
                Assert.Equal(dr.GetGuid(2), nissan.GroupIdentifier);
                break;
              case "Ford":
                Assert.Equal(dr.GetGuid(0), ford.ManufacturerId);
                Assert.Equal(dr.GetGuid(2), ford.GroupIdentifier);
                break;
              default:
                //Assert.Fail();
                break;
            }
          }
          dr.Close();

          // Validates Integer
          cmd = new MySqlCommand("SELECT * FROM Distributors", conn);
          dr = cmd.ExecuteReader();
          if (!dr.HasRows)
            //Assert.Fail("No records found");
          while (dr.Read())
          {
            string name = dr.GetString(1);
            switch (name)
            {
              case "Distributor1":
                Assert.Equal(dr.GetInt32(0), dis1.DistributorId);
                break;
              case "Distributor2":
                Assert.Equal(dr.GetInt32(0), dis2.DistributorId);
                break;
              default:
                //Assert.Fail();
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
    [Fact]
    public void FirstOrDefaultNested()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (MovieDBContext ctx = new MovieDBContext())
      {
        ctx.Database.Initialize(true);
#if EF6
        MovieDBInitialize.DoDataPopulation(ctx);
#endif
        int DirectorId = 1;
        var q = ctx.Movies.Where(p => p.Director.ID == DirectorId).Select(p => 
          new
          {
            Id = p.ID,
            FirstMovieFormat = p.Formats.Count == 0 ? 0.0 : p.Formats.FirstOrDefault().Format
          });
        string sql = q.ToString();
#if DEBUG
        Debug.WriteLine(sql);
#endif
        int j = q.Count();
        foreach (var r in q)
        {
          j--;
        }
        Assert.Equal(0, j);
      }
    }

    /// <summary>
    /// This tests the fix for bug 73549, Generated Sql does not contain ORDER BY statement whose is requested by LINQ.
    /// </summary>
    [Fact]
    public void FirstOrDefaultNestedWithOrderBy()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      using (SakilaDb db = new SakilaDb())
      {
        var q = from cu in db.customers
                let curAddr = db.addresses.OrderByDescending(p => p.address_id).Where(p => p.address_id == cu.address_id).FirstOrDefault()
                join sto in db.stores on cu.store_id equals sto.store_id
                orderby cu.customer_id descending
                select new
                {
                  curAddr.city.country.country1
                };
        string sql = q.ToString();
        st.CheckSql(sql, SQLSyntax.FirstOrDefaultNestedWithOrderBy);
#if DEBUG
        Debug.WriteLine(sql);
#endif
        int j = q.Count();
        foreach (var r in q)
        {
          //Debug.WriteLine( r.country1 );
        }
        Assert.Equal(599, j);
      }
    }
  
    /// <summary>
    /// SUPPORT FOR DATE TYPES WITH PRECISION
    /// </summary>
    [Fact]
    public void CanDefineDatesWithPrecisionFor56()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      
      if (st.Version < new Version(5, 6)) return;

      ReInitDb();
      using (var db = new ProductsDbContext())
      {
        using (MySqlConnection conn = new MySqlConnection(db.Database.Connection.ConnectionString))
        {
          conn.Open();
          MySqlCommand query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type, DateTime_Precision from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Products' and column_name ='DateTimeWithPrecision'", conn);
          query.Connection = conn;
          MySqlDataReader reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.Equal("DateTimeWithPrecision", reader[0].ToString());
            Assert.Equal("NO", reader[1].ToString());
            Assert.Equal("datetime", reader[2].ToString());
            Assert.Equal("3", reader[3].ToString());
          }
          reader.Close();

          query = new MySqlCommand("Select Column_name, Is_Nullable, Data_Type, DateTime_Precision from information_schema.Columns where table_schema ='" + conn.Database + "' and table_name = 'Products' and column_name ='TimeStampWithPrecision'", conn);
          query.Connection = conn;
          reader = query.ExecuteReader();
          while (reader.Read())
          {
            Assert.Equal("TimeStampWithPrecision", reader[0].ToString());
            Assert.Equal("NO", reader[1].ToString());
            Assert.Equal("timestamp", reader[2].ToString());
            Assert.Equal("3", reader[3].ToString());
          }
          reader.Close();
        }
        db.Database.Delete();
      }
    }

    /// <summary>
    /// Orabug #15935094 SUPPORT FOR CURRENT_TIMESTAMP AS DEFAULT FOR DATETIME WITH EF
    /// </summary>
    [Fact]
    public void CanDefineDateTimeAndTimestampWithIdentity()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      if (st.Version < new Version(5, 6)) return;

      ReInitDb();
      using (var db = new ProductsDbContext())
      {
        MySqlConnection con = (MySqlConnection)db.Database.Connection;
        MySqlCommand cmd = new MySqlCommand("set session sql_mode = '';", con);
        con.Open();
        cmd.ExecuteNonQuery();
        con.Close();
        
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

        Assert.NotNull(db.Products.First().Timestamp);
        Assert.NotNull(db.Products.First().DateCreated);
        Assert.Equal(new DateTime(2012, 3, 18, 23, 9, 7, 6), db.Products.First().DateTimeWithPrecision);
        Assert.Equal(1, db.Products.Count());
        db.Database.Delete();
      }
    }


    /// <summary>
    /// Test of fix for bug Support for EntityFramework 4.3 Code First Generated Identifiers (MySql Bug #67285, Oracle bug #16286397).
    /// FKs are renamed to met http://dev.mysql.com/doc/refman/5.0/en/identifiers.html limitations.
    /// </summary>
    [Fact]
    public void LongIdentifiersInheritanceTPT()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
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


    /// <summary>
    /// Test fix for http://bugs.mysql.com/bug.php?id=67183
    /// (Malformed Query while eager loading with EF 4 due to multiple projections).
    /// </summary>
    [Fact]
    public void ShipTest()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (var context = new ShipContext())
      {
        context.Database.Initialize(true);

        var harbor = new Harbor
        {
          Ships = new HashSet<Ship>
            {
                new Ship
                {
                    CrewMembers = new HashSet<CrewMember>
                    {
                        new CrewMember
                        {
                            Rank = new Rank { Description = "Rank A" },
                            Clearance = new Clearance { Description = "Clearance A" },
                            Description = "CrewMember A"
                        },
                        new CrewMember
                        {
                            Rank = new Rank { Description = "Rank B" },
                            Clearance = new Clearance { Description = "Clearance B" },
                            Description = "CrewMember B"
                        }
                    },
                    Description = "Ship AB"
                },
                new Ship
                {
                    CrewMembers = new HashSet<CrewMember>
                    {
                        new CrewMember
                        {
                            Rank = new Rank { Description = "Rank C" },
                            Clearance = new Clearance { Description = "Clearance C" },
                            Description = "CrewMember C"
                        },
                        new CrewMember
                        {
                            Rank = new Rank { Description = "Rank D" },
                            Clearance = new Clearance { Description = "Clearance D" },
                            Description = "CrewMember D"
                        }
                    },
                    Description = "Ship CD"
                }
            },
          Description = "Harbor ABCD"
        };

        context.Harbors.Add(harbor);
        context.SaveChanges();
      }

      using (var context = new ShipContext())
      {
        DbSet<Harbor> dbSet = context.Set<Harbor>();
        IQueryable<Harbor> query = dbSet;
        query = query.Include(entity => entity.Ships);
        query = query.Include(entity => entity.Ships.Select(s => s.CrewMembers));
        query = query.Include(entity => entity.Ships.Select(s => s.CrewMembers.Select(cm => cm.Rank)));
        query = query.Include(entity => entity.Ships.Select(s => s.CrewMembers.Select(cm => cm.Clearance)));

        string[] data = new string[] { 
          "1,Harbor ABCD,1,1,1,Ship AB,1,1,1,1,1,CrewMember A,1,Rank A,1,Clearance A",
          "1,Harbor ABCD,1,1,1,Ship AB,1,2,1,2,2,CrewMember B,2,Rank B,2,Clearance B",
          "1,Harbor ABCD,1,2,1,Ship CD,1,3,2,3,3,CrewMember C,3,Rank C,3,Clearance C",
          "1,Harbor ABCD,1,2,1,Ship CD,1,4,2,4,4,CrewMember D,4,Rank D,4,Clearance D"
        };
        Dictionary<string, string> outData = new Dictionary<string, string>();

        var sqlString = query.ToString();
#if EF6
        st.CheckSql(sqlString, SQLSyntax.ShipQueryMalformedDueMultipleProjecttionsCorrectedEF6);
#else
        st.CheckSql(sqlString, SQLSyntax.ShipQueryMalformedDueMultipleProjecttionsCorrected);
#endif
        // see below for the generated SQL query

        var harbor = query.Single();
        
        foreach (var ship in harbor.Ships)
        {
          foreach (var crewMember in ship.CrewMembers)
          {
            outData.Add(string.Format( 
              "{0},{1},1,{2},{3},{4},1,{5},{6},{7},{8},{9},{10},{11},{12},{13}",
              harbor.HarborId, harbor.Description, ship.ShipId, harbor.HarborId,
              ship.Description, crewMember.CrewMemberId, crewMember.ShipId, crewMember.RankId,
              crewMember.ClearanceId, crewMember.Description, crewMember.Rank.RankId,
              crewMember.Rank.Description, crewMember.Clearance.ClearanceId,
              crewMember.Clearance.Description), null);
          }
        }
        // check data integrity
        Assert.Equal(outData.Count, data.Length);
        for (int i = 0; i < data.Length; i++)
        {
          Assert.True(outData.ContainsKey(data[i]));
        }
      }
    }

    /// <summary>
    /// Tests fix for bug http://bugs.mysql.com/bug.php?id=68513, Error in LINQ to Entities query when using Distinct().Count().
    /// </summary>
    [Fact]
    public void DistinctCount()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (SiteDbContext ctx = new SiteDbContext())
      {
        ctx.Database.Initialize(true);
        visitante v1 = new visitante() { nCdSite = 1, nCdVisitante = 1, sDsIp = "x1" };
        visitante v2 = new visitante() { nCdSite = 1, nCdVisitante = 2, sDsIp = "x2" };
        site s1 = new site() { nCdSite = 1, sDsTitulo = "MyNewsPage" };
        site s2 = new site() { nCdSite = 2, sDsTitulo = "MySearchPage" };
        ctx.Visitante.Add(v1);
        ctx.Visitante.Add(v2);
        ctx.Site.Add(s1);
        ctx.Site.Add(s2);
        ctx.SaveChanges();

        var q = (from vis in ctx.Visitante.Include("site")
                  group vis by vis.nCdSite into g
                  select new retorno
                  {
                    Key = g.Key,
                    Online = g.Select(e => e.sDsIp).Distinct().Count()
                  });
        string sql = q.ToString();
#if EF6
        st.CheckSql(sql, SQLSyntax.CountGroupBy);
#else
        st.CheckSql(sql, SQLSyntax.CountGroupByEF5);
#endif
        var q2 = q.ToList<retorno>();
        foreach( var row in q2 )
        {
        }
      }
    }

    /// <summary>
    /// Tests fix for bug http://bugs.mysql.com/bug.php?id=68513, Error in LINQ to Entities query when using Distinct().Count().
    /// </summary>
    [Fact]
    public void DistinctCount2()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (SiteDbContext ctx = new SiteDbContext())
      {
        ctx.Database.Initialize(true);
        visitante v1 = new visitante() { nCdSite = 1, nCdVisitante = 1, sDsIp = "x1" };
        visitante v2 = new visitante() { nCdSite = 1, nCdVisitante = 2, sDsIp = "x2" };
        site s1 = new site() { nCdSite = 1, sDsTitulo = "MyNewsPage" };
        site s2 = new site() { nCdSite = 2, sDsTitulo = "MySearchPage" };
        pagina p1 = new pagina() { nCdPagina = 1, nCdVisitante = 1, sDsTitulo = "index.html" };
        ctx.Visitante.Add(v1);
        ctx.Visitante.Add(v2);
        ctx.Site.Add(s1);
        ctx.Site.Add(s2);
        ctx.Pagina.Add(p1);
        ctx.SaveChanges();
        
        var q = (from pag in ctx.Pagina.Include("visitante").Include("site")
                   group pag by pag.visitante.nCdSite into g
                   select new retorno
                   {
                       Key = g.Key,
                       Online = g.Select(e => e.visitante.sDsIp).Distinct().Count()
                   });        
        string sql = q.ToString();
#if EF6
        st.CheckSql(sql, SQLSyntax.CountGroupBy2);
#else
        st.CheckSql(sql, SQLSyntax.CountGroupBy2EF5);
#endif
        var q2 = q.ToList<retorno>();
        foreach (var row in q2)
        {
        }
      }
    }

    /// <summary>
    /// Tests fix for bug http://bugs.mysql.com/bug.php?id=65723, MySql Provider for EntityFramework produces "bad" SQL for OrderBy.
    /// </summary>
    [Fact]
    public void BadOrderBy()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (MovieDBContext db = new MovieDBContext())
      {
        db.Database.Initialize(true);
#if EF6
        MovieDBInitialize.DoDataPopulation(db);
#endif
        DateTime filterDate = new DateTime(1986, 1, 1);
        var q = db.Movies.Where(p => p.ReleaseDate >= filterDate).
          OrderByDescending(p => p.ReleaseDate).Take(2);
        string sql = q.ToString();
        st.CheckSql(SQLSyntax.NestedOrderBy, sql);
        // Data integrity testing
        Movie[] data = new Movie[] {
          new Movie() { ID = 4, Title = "Star Wars, The Sith Revenge", ReleaseDate = new DateTime( 2005, 5, 19 ) },
          new Movie() { ID = 2, Title = "The Matrix", ReleaseDate = new DateTime( 1999, 3, 31 ) }
        };
        int i = 0;
        foreach (Movie m in q)
        {
          Assert.Equal(data[i].ID, m.ID);
          Assert.Equal(data[i].Title, m.Title);
          Assert.Equal(data[i].ReleaseDate, m.ReleaseDate);
          i++;
        }
        Assert.Equal(2, i);
      }
    }

    /// <summary>
    /// Tests fix for bug http://bugs.mysql.com/bug.php?id=69751, Invalid SQL query generated for query with Contains, OrderBy, and Take.
    /// </summary>
    [Fact]
    public void BadContainsOrderByTake()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (MovieDBContext db = new MovieDBContext())
      {
        db.Database.Initialize(true);
#if EF6
        MovieDBInitialize.DoDataPopulation(db);
#endif
        string title = "T";
        var q = from m in db.Movies
                where m.Title.Contains(title)
                orderby m.ID descending
                select m;
        var q1 = q.Take(10);
        string sql = q1.ToString();

        st.CheckSql(SQLSyntax.QueryWithOrderByTakeContains, sql);

        int i = 0;
        foreach (var row in q1)
        {
          Assert.Equal( MovieDBInitialize.data[i].ID, row.ID);
          Assert.Equal( MovieDBInitialize.data[i].Title, row.Title);
          Assert.Equal( MovieDBInitialize.data[i].ReleaseDate, row.ReleaseDate);
          i++;
        }
      }
    }
    
    /// <summary>
    /// Tests fix for bug http://bugs.mysql.com/bug.php?id=69922, Unknown column Extent1...
    /// </summary>
    [Fact]
    public void BadAliasTable()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (PromotionsDB db = new PromotionsDB())
      {
        db.Database.Initialize(true);
        DateTime now = DateTime.Now;
        var q = db
          .HomePromoes
          .Where(x =>
             x.Active
               &&
             (x.ActiveFrom == null || x.ActiveFrom <= now)
               &&
             (x.ActiveTo == null || x.ActiveTo >= now)
          )
          .OrderBy(x => x.DisplayOrder).Select( d => d );
        string sql = q.ToString();
        foreach( var row in q )
        {
        }
      }
    }

    /// <summary>
    /// Tests other variants of bug http://bugs.mysql.com/bug.php?id=69751, Invalid SQL query generated for query with Contains, OrderBy, and Take.
    /// </summary>
    [Fact]
    public void BadContainsOrderByTake2()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (MovieDBContext db = new MovieDBContext())
      {
        db.Database.Initialize(true);
#if EF6
        MovieDBInitialize.DoDataPopulation(db);
#endif
        var q = db.Movies.
                Where(m => !string.IsNullOrEmpty(m.Title) && m.Title.Contains("x")).
                OrderByDescending(m => m.ID).
                Skip(1).
                Take(1);
        string sql = q.ToString();
#if DEBUG
        Debug.WriteLine(sql);
#endif
        List<Movie> l = q.ToList();
        int j = l.Count;
        foreach( Movie m in l )
        {
          j--;
        }
        Assert.Equal(0, j);
      }
    }

    /// <summary>
    /// Tests other variants of bug http://bugs.mysql.com/bug.php?id=69751, Invalid SQL query generated for query with Contains, OrderBy, and Take.
    /// </summary>
    [Fact]
    public void BadContainsOrderByTake3()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (MovieDBContext db = new MovieDBContext())
      {
        db.Database.Initialize(true);
#if EF6
        MovieDBInitialize.DoDataPopulation(db);
#endif
        var q = db.Movies.
                Where(m => !string.IsNullOrEmpty(m.Title) && m.Title.Contains("x")).
                OrderByDescending(m => m.ID).
                Skip(1).
                Take(1).Select(m => new { 
                  Id = m.ID, 
                  CriticsScore = ( 
                    m.Title == "Terminator 1" ? "Good" : 
                    m.Title == "Predator" ? "Sunday best, cheese" : 
                    m.Title == "The Matrix" ? "Really Good" :
                    m.Title == "Star Wars, The Sith Revenge" ? "Really Good" : "Unknown" )
                });
        string sql = q.ToString();
#if DEBUG
        Debug.WriteLine(sql);
#endif
        int j = q.Count();
        foreach (var row in q)
        {
          j--;
        }
        Assert.Equal(0, j);
      }
    }

    /// <summary>
    /// Tests other variants of bug http://bugs.mysql.com/bug.php?id=69751, Invalid SQL query generated for query with Contains, OrderBy, and Take.
    /// </summary>
    [Fact]
    public void BadContainsOrderByTake4()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (MovieDBContext db = new MovieDBContext())
      {
        db.Database.Initialize(true);
#if EF6
        MovieDBInitialize.DoDataPopulation(db);
#endif
        bool q = db.Movies.Any(m => m.ReleaseDate.Year > 1985);
//        string sql = q.ToString();
//#if DEBUG
//        Debug.WriteLine(sql);
//#endif
        //foreach (var row in q)
        //{
        //}
      }
    }

    /// <summary>
    /// Tests other variants of bug http://bugs.mysql.com/bug.php?id=69751, Invalid SQL query generated for query with Contains, OrderBy, and Take.
    /// </summary>
    [Fact]
    public void BadContainsOrderByTake5()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (MovieDBContext db = new MovieDBContext())
      {
        db.Database.Initialize(true);
#if EF6
        MovieDBInitialize.DoDataPopulation(db);
#endif
        // TODO: add subquery like
        // var shifts = Shifts.Where(s => !EmployeeShifts.Where(es => es.ShiftID == s.ShiftID).Any());
        bool q = db.Movies.Where( m => m.ReleaseDate.Month != 10 ).Any(m => m.ReleaseDate.Year > 1985);
//        string sql = q.ToString();
//#if DEBUG
//        Debug.WriteLine(sql);
//#endif
//        foreach (var row in q)
//        {
//        }
      }
    }

    /// <summary>
    /// Tests other variants of bug http://bugs.mysql.com/bug.php?id=69751, Invalid SQL query generated for query with Contains, OrderBy, and Take.
    /// </summary>
    [Fact]
    public void BadContainsOrderByTake6()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (MovieDBContext db = new MovieDBContext())
      {
        db.Database.Initialize(true);
#if EF6
        MovieDBInitialize.DoDataPopulation(db);
#endif
        var q = from m in db.Movies
                where m.Title.Contains("x") && db.Medias.Where( mm => mm.Format == "Digital" ).Any()
                select m;
        string sql = q.ToString();
#if DEBUG
        Debug.WriteLine(sql);
#endif
        int j = q.Count();
        foreach (var row in q)
        {
          j--;
        }
        Assert.Equal(0, j);
      }
    }

  /// <summary>
    /// Test for Mysql Bug 70602: http://bugs.mysql.com/bug.php?id=70602
    /// </summary>
    [Fact]
    public void AutoIncrementBug()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      AutoIncrementBugContext dbContext = new AutoIncrementBugContext();

      dbContext.Database.Initialize(true);
      dbContext.AutoIncrementBug.Add(new AutoIncrementBug() { Description = "Test" });
      dbContext.SaveChanges();
      using (var reader = MySqlHelper.ExecuteReader(dbContext.Database.Connection.ConnectionString, "SHOW COLUMNS FROM AUTOINCREMENTBUGS WHERE EXTRA LIKE '%AUTO_INCREMENT%'"))
      {
        Assert.Equal(true, reader.HasRows);
      }
      dbContext.Database.Delete();
    }

#if EF6
    [Fact]
    public void SimpleCodeFirstSelectCbc()
    {
      MovieCodedBasedConfigDBContext db = new MovieCodedBasedConfigDBContext();
      db.Database.Initialize(true);
      var l = db.Movies.ToList();
      foreach (var i in l)
      {
        Console.WriteLine(i);
      }
    }

    [Fact]
    public void TestStoredProcedureMapping()
    {
      using (var db = new MovieCodedBasedConfigDBContext())
      {
        db.Database.Initialize(true);
        var movie = new MovieCBC()
        {
          Title = "Sharknado",
          Genre = "Documental",
          Price = 1.50M,
          ReleaseDate = DateTime.Parse("01/07/2013")
        };

        db.Movies.Add(movie);
        db.SaveChanges();
        movie.Genre = "Fiction";
        db.SaveChanges();
        db.Movies.Remove(movie);
        db.SaveChanges();
      }
    }
    
    [Fact]
    public void MigrationHistoryConfigurationTest()
    {
      ReInitDb();
      MovieCodedBasedConfigDBContext db = new MovieCodedBasedConfigDBContext();
      db.Database.Initialize(true);
      var l = db.Movies.ToList();
      foreach (var i in l)
      {
      }
      var result = MySqlHelper.ExecuteScalar("server=localhost;User Id=root;database=test;logging=true; port=3305;", "SELECT COUNT(_MigrationId) FROM __MySqlMigrations;");
      Assert.Equal(1, int.Parse(result.ToString()));
    }

    [Fact]
    public void DbSetRangeTest()
    {
      ReInitDb();
      using (MovieDBContext db = new MovieDBContext())
      {
        db.Database.Initialize(true);
        Movie m1 = new Movie() { Title = "Terminator 1", ReleaseDate = new DateTime(1984, 10, 26) };
        Movie m2 = new Movie() { Title = "The Matrix", ReleaseDate = new DateTime(1999, 3, 31) };
        Movie m3 = new Movie() { Title = "Predator", ReleaseDate = new DateTime(1987, 6, 12) };
        Movie m4 = new Movie() { Title = "Star Wars, The Sith Revenge", ReleaseDate = new DateTime(2005, 5, 19) };
        db.Movies.AddRange( new Movie[] { m1, m2, m3, m4 });
        db.SaveChanges();
        var q = from m in db.Movies select m;
        Assert.Equal(4, q.Count());
        foreach (var row in q)
        {
        }
        db.Movies.RemoveRange(q.ToList());
        db.SaveChanges();
        var q2 = from m in db.Movies select m;
        Assert.Equal(0, q2.Count());
      }
    }

    [Fact]
    public void EnumSupportTest()
    {
      using (var dbCtx = new EnumTestSupportContext())
      {
        dbCtx.Database.Initialize(true);
        dbCtx.SchoolSchedules.Add(new SchoolSchedule() { TeacherName = "Pako", Subject = SchoolSubject.History });
        dbCtx.SaveChanges();

        var schedule = (from s in dbCtx.SchoolSchedules
                        where s.Subject == SchoolSubject.History
                        select s).FirstOrDefault();

        Assert.NotEqual(null, schedule);
        Assert.Equal(SchoolSubject.History, schedule.Subject);
      }
    }

    [Fact]
    public void SpatialSupportTest()
    {
      using (var dbCtx = new JourneyContext())
      {
        dbCtx.Database.Initialize(true);
        dbCtx.MyPlaces.Add(new MyPlace()
        {
          name = "JFK INTERNATIONAL AIRPORT OF NEW YORK",
          location = DbGeometry.FromText("POINT(40.644047 -73.782291)"),
        });
        dbCtx.SaveChanges();

        var place = (from p in dbCtx.MyPlaces
                     where p.name == "JFK INTERNATIONAL AIRPORT OF NEW YORK"
                     select p).FirstOrDefault();

        var point = DbGeometry.FromText("POINT(40.717957 -73.736501)");

        var distance = (point.Distance(place.location) * 100);

        Assert.NotEqual(null, place);
        Assert.Equal(8.6944880240295852D, distance.Value);

        var distanceDB = from p in dbCtx.MyPlaces
                         select p.location.Distance(point);

        Assert.Equal(0.086944880240295852D, distanceDB.FirstOrDefault());
      }
    }

    [Fact]
    public void BeginTransactionSupportTest()
    {
      using (var dbcontext = new MovieCodedBasedConfigDBContext())
      {
        dbcontext.Database.Initialize(true);
        using (var transaction = dbcontext.Database.BeginTransaction())
        {
          try
          {
            dbcontext.Movies.Add(new MovieCBC()
            {
              Title = "Sharknado",
              Genre = "Documental",
              Price = 1.50M,
              ReleaseDate = DateTime.Parse("01/07/2013")
            });

            dbcontext.SaveChanges();
            var result = MySqlHelper.ExecuteScalar("server=localhost;User Id=root;database=test;logging=true; port=3305;", "select COUNT(*) from moviecbcs;");
            Assert.Equal(0, int.Parse(result.ToString()));

            transaction.Commit();

            result = MySqlHelper.ExecuteScalar("server=localhost;User Id=root;database=test;logging=true; port=3305;", "select COUNT(*) from moviecbcs;");
            Assert.Equal(1, int.Parse(result.ToString()));
          }
          catch (Exception)
          {
            transaction.Rollback();
          }
        }
      }
    }

    /// <summary>
    /// This test covers two new features on EF6: 
    /// 1- "DbContext.Database.UseTransaction, that use a transaction created from an open connection"
    /// 2- "DbContext can now be created with a DbConnection that is already opened"
    /// </summary>
    [Fact]
    public void UseTransactionSupportTest()
    {
      using (var connection = new MySqlConnection("server=localhost;User Id=root;database=test;logging=true; port=3305;"))
      {
        connection.Open();
        using (var transaction = connection.BeginTransaction())
        {
          try
          {
            using (var dbcontext = new MovieCodedBasedConfigDBContext(connection, contextOwnsConnection: false))
            {
              dbcontext.Database.Initialize(true);
              dbcontext.Database.UseTransaction(transaction);
              dbcontext.Movies.Add(new MovieCBC()
              {
                Title = "Sharknado",
                Genre = "Documental",
                Price = 1.50M,
                ReleaseDate = DateTime.Parse("01/07/2013")
              });

              dbcontext.SaveChanges();
            }
            var result = MySqlHelper.ExecuteScalar("server=localhost;User Id=root;database=test;logging=true; port=3305;", "select COUNT(*) from moviecbcs;");
            Assert.Equal(0, int.Parse(result.ToString()));

            transaction.Commit();

            result = MySqlHelper.ExecuteScalar("server=localhost;User Id=root;database=test;logging=true; port=3305;", "select COUNT(*) from moviecbcs;");
            Assert.Equal(1, int.Parse(result.ToString()));
          }
          catch (Exception)
          {
            transaction.Rollback();
          }
        }
      }
    }

    [Fact]
    public void HasChangesSupportTest()
    {
      using (var dbcontext = new MovieCodedBasedConfigDBContext())
      {
        dbcontext.Database.Initialize(true);

        dbcontext.Movies.Add(new MovieCBC()
        {
          Title = "Sharknado",
          Genre = "Documental",
          Price = 1.50M,
          ReleaseDate = DateTime.Parse("01/07/2013")
        });

        Assert.Equal(true, dbcontext.ChangeTracker.HasChanges());
        dbcontext.SaveChanges();
        Assert.Equal(false, dbcontext.ChangeTracker.HasChanges());
      }
    }

    [Fact]
    public void MySqlLoggingToFileSupportTest()
    {
      string logName = "mysql.log";
      //if (System.IO.File.Exists(logName))
      //  System.IO.File.Delete(logName);

      using (var dbcontext = new MovieCodedBasedConfigDBContext())
      {
        dbcontext.Database.Log = MySqlLogger.Logger(logName, true).Write;

        dbcontext.Database.Initialize(true);
        dbcontext.Movies.Add(new MovieCBC()
        {
          Title = "Sharknado",
          Genre = "Documental",
          Price = 1.50M,
          ReleaseDate = DateTime.Parse("01/07/2013")
        });
        dbcontext.SaveChanges();
      }

      Assert.Equal(true, System.IO.File.Exists(logName));
    }

    [Fact]
    public void MySqlLoggingToConsoleSupportTest()
    {
      string logName = "mysql_2.log";
      if (System.IO.File.Exists(logName))
        System.IO.File.Delete(logName);

      System.IO.FileStream file;
      System.IO.StreamWriter writer;
      System.IO.TextWriter txtOut = Console.Out;
      try
      {
        file = new System.IO.FileStream(logName, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
        writer = new System.IO.StreamWriter(file);
      }
      catch (Exception e)
      {
        throw e;
      }
      Console.SetOut(writer);

      using (var dbcontext = new MovieCodedBasedConfigDBContext())
      {
        dbcontext.Database.Log = new MySqlLogger(s => Console.Write(s)).Write;

        dbcontext.Database.Initialize(true);
        dbcontext.Movies.Add(new MovieCBC()
        {
          Title = "Sharknado",
          Genre = "Documental",
          Price = 1.50M,
          ReleaseDate = DateTime.Parse("01/07/2013")
        });
        dbcontext.SaveChanges();
      }
      Console.SetOut(txtOut);
      writer.Close();
      file.Close();

      Assert.Equal(true, System.IO.File.Exists(logName));
    }

    [Fact]
    public void EntityAndComplexTypeSupportTest()
    {
      using (var dbContext = new EntityAndComplexTypeContext())
      {
        dbContext.Database.Initialize(true);
        dbContext.Students.Add(
              new Student()
              {
                Name = "Pakorasu Pakolas",
                Address = new Address() { City = "Mazatlan", Street = "Tierra de Venados 440" },
                Schedule = new List<SchoolSchedule>() { new SchoolSchedule() { TeacherName = "Pako", Subject = SchoolSubject.History } }
              });
        dbContext.SaveChanges();

        var student = (from s in dbContext.Students
                        select s).FirstOrDefault();

        Assert.NotEqual(null, student);
        Assert.NotEqual(null, student.Schedule);
        Assert.NotEqual(true, string.IsNullOrEmpty(student.Address.Street));
        Assert.NotEqual(0, student.Schedule.Count());
      }
    }

    /// <summary>
    /// TO RUN THIS TEST ITS NECESSARY TO ENABLE THE EXECUTION STRATEGY IN THE CLASS MySqlEFConfiguration (Source\MySql.Data.Entity\MySqlConfiguration.cs) AS WELL AS START A MYSQL SERVER INSTACE WITH THE OPTION "--max_connections=3"
    /// WHY 3?: 1)For main process (User: root, DB: mysql). 2)For Setup Class. 3)For the connections in this test.
    /// The expected result is that opening a third connection and trying to open a fourth(with an asynchronous task) the execution strategy implementation handle the reconnection process until the third one is closed.
    /// </summary>
    //[Fact] //<---DON'T FORGET ME TO RUN! =D
    public void ExecutionStrategyTest()
    {
      var connection = new MySqlConnection("server=localhost;User Id=root;logging=true; port=3305;");
      using (var dbcontext = new MovieCodedBasedConfigDBContext())
      {
        dbcontext.Database.Initialize(true);
        dbcontext.Movies.Add(new MovieCBC()
        {
          Title = "Sharknado",
          Genre = "Documental",
          Price = 1.50M,
          ReleaseDate = DateTime.Parse("01/07/2013")
        });
        connection.Open();
        System.Threading.Tasks.Task.Factory.StartNew(() => { dbcontext.SaveChanges(); });
        Thread.Sleep(1000);
        connection.Close();
        connection.Dispose();
      }
      var result = MySqlHelper.ExecuteScalar("server=localhost;User Id=root;database=test;logging=true; port=3305;", "select COUNT(*) from moviecbcs;");
      Assert.Equal(1, int.Parse(result.ToString()));
    }
#endif

    [Fact]
    public void UnknownProjectC1()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (MovieDBContext db = new MovieDBContext())
      {
        db.Database.Initialize(true);
#if EF6
        MovieDBInitialize.DoDataPopulation(db);
#endif
        long myKey = 20;
        var q = (from r in db.Movies where (r.ID == myKey) select (long)r.ID).OrderBy(p => p);
        string sql = q.ToString();
#if EF6
        st.CheckSql(sql, SQLSyntax.UnknownProjectC1EF6 );
#else
        st.CheckSql(sql, SQLSyntax.UnknownProjectC1);
#endif

#if DEBUG
        Debug.WriteLine(sql);
#endif
        long[] array = (from r in db.Movies where (r.ID == myKey) select (long)r.ID).OrderBy(p => p).ToArray();
      }
    }

    [Fact]
    public void StartsWithTest()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      MovieDBContext db = new MovieDBContext();
      db.Database.Initialize(true);
#if EF6
      MovieDBInitialize.DoDataPopulation(db);
#endif
      string term = "The";
      var l = db.Movies.Where(p => p.Title.StartsWith(term));

      string sql = l.ToString();

      st.CheckSql(sql, SQLSyntax.QueryWithStartsWith);

#if DEBUG
      Debug.WriteLine(sql);
#endif
      int j = l.Count();
      foreach (var i in l)
      {
        j--;
      }
      Assert.Equal(0, j);
    }

    [Fact]
    public void EndsWithTest()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      MovieDBContext db = new MovieDBContext();
      db.Database.Initialize(true);
#if EF6
      MovieDBInitialize.DoDataPopulation(db);
#endif
      string term = "The";
      var l = db.Movies.Where(p => p.Title.EndsWith(term));

      string sql = l.ToString();

      st.CheckSql(sql, SQLSyntax.QueryWithEndsWith);

#if DEBUG
      Debug.WriteLine(sql);
#endif
      int j = l.Count();
      foreach (var i in l)
      {
        j--;
      }
      Assert.Equal(0, j);
    }

    [Fact]
    public void ContainsTest()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      MovieDBContext db = new MovieDBContext();
      db.Database.Initialize(true);
#if EF6
      MovieDBInitialize.DoDataPopulation(db);
#endif
      string term = "The";
      var l = db.Movies.Where(p => p.Title.Contains(term));

      string sql = l.ToString();
      st.CheckSql(sql, SQLSyntax.QueryWithContains);

#if DEBUG
      Debug.WriteLine(sql);
#endif
      int j = l.Count();
      foreach (var i in l)
      {
        j--;
      }
      Assert.Equal(0, j);
    }


    /// <summary>
    /// Test to reproduce bug http://bugs.mysql.com/bug.php?id=73643, Exception when using IEnumera.Contains(model.property) in Where predicate
    /// </summary>
    [Fact]
    public void TestContainsListWithCast()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using (MovieDBContext db = new MovieDBContext())
      {
        db.Database.Initialize(true);

        long[] longs = new long[] { 1, 2, 3 };
        var q = db.Movies.Where(p => longs.Contains((long)p.ID));
        string sql = q.ToString();
#if EF6
        st.CheckSql(sql, SQLSyntax.TestContainsListWithCast);
#else
        st.CheckSql(sql, SQLSyntax.TestContainsListWithCastEF5);
#endif
#if DEBUG
        Debug.WriteLine(sql);
#endif
        var l = q.ToList();
      }
    }

    /// <summary>
    /// Test to reproduce bug http://bugs.mysql.com/bug.php?id=73643, Exception when using IEnumera.Contains(model.property) in Where predicate
    /// </summary>
    [Fact]
    public void TestContainsListWitConstant()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using( MovieDBContext db = new MovieDBContext() )
      {
        db.Database.Initialize(true);

        List<string> strIds = new List<string>(new string[] { "two" });
        var q = db.Movies.Where(p => strIds.Contains("two"));
        string sql = q.ToString();
#if EF6
        st.CheckSql(sql, SQLSyntax.TestContainsListWitConstant );
#else
        st.CheckSql(sql, SQLSyntax.TestContainsListWitConstantEF5);
#endif
#if DEBUG
        Debug.WriteLine(sql);
#endif
        var l = q.ToList();
      }
    }

    /// <summary>
    /// Test to reproduce bug http://bugs.mysql.com/bug.php?id=73643, Exception when using IEnumera.Contains(model.property) in Where predicate
    /// </summary>
    [Fact]
    public void TestContainsListWithParameterReference()
    {
#if DEBUG
      Debug.WriteLine(new StackTrace().GetFrame(0).GetMethod().Name);
#endif
      ReInitDb();
      using( MovieDBContext db = new MovieDBContext() )
      {
        db.Database.Initialize(true);

        long[] longs = new long[] { 1, 2, 3 };
        int myNum = 1;
        var q = db.Movies.Where(p => longs.Contains(myNum));
        string sql = q.ToString();
#if EF6
        st.CheckSql(sql, SQLSyntax.TestContainsListWithParameterReference );
#else
        st.CheckSql(sql, SQLSyntax.TestContainsListWithParameterReferenceEF5);
#endif
#if DEBUG
        Debug.WriteLine(sql);
#endif
        var l = q.ToList();
      }
    }

    [Fact]
    public void ReplaceTableNameVisitor()
    {
      using (SakilaDb context = new SakilaDb())
      {
        var date = new DateTime(2005, 6, 1);
        var rentals = context.customers.Where(t => t.rentals.Any(r => r.rental_date < date)).OrderBy(o => o.customer_id);
        string sql = rentals.ToString();
        st.CheckSql(sql, SQLSyntax.ReplaceNameVisitorQuery);
#if DEBUG
        Debug.WriteLine(sql);
#endif
        var result = rentals.ToList();
        Assert.Equal(520, rentals.Count());
      }
    }


    /// <summary>
    /// Bug #70941 - Invalid SQL query when eager loading two nested collections
    /// </summary>
    [Fact]
    public void InvalidQuery()
    {
      using (UsingUnionContext context = new UsingUnionContext())
      {
        if (context.Database.Exists())
        context.Database.Delete();
        
        context.Database.Create();
                
        for (int i = 1; i <= 3; i++)
        {
          var order = new Order();
          var items = new List<Item>();

          items.Add(new Item { Id = 1 });
          items.Add(new Item { Id = 2 });
          items.Add(new Item { Id = 3 });

          order.Items = items;
          var client = new Client { Id = i };
          client.Orders = new List<Order>();
          client.Orders.Add(order);

          context.Clients.Add(client);
        }       
        context.SaveChanges();
                
        var clients = context.Clients
                    .Include(c => c.Orders.Select(o => o.Items))
                    .Include(c => c.Orders.Select(o => o.Discounts)).ToList();        

        Assert.Equal(clients.Count(), 3);
        Assert.Equal(clients.Where(t => t.Id == 1).Single().Orders.Count(), 1);
        Assert.Equal(clients.Where(t => t.Id == 1).Single().Orders.Where(j => j.Id == 1).Single().Items.Count(), 3);
      }    
    }
  }
}

