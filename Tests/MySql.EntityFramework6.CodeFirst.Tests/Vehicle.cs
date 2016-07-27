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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#if !EF6
#if NET_45_OR_GREATER
using System.Data.Spatial;
#endif
#endif



namespace MySql.Data.Entity.CodeFirst.Tests
{
#if EF6
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
#endif
  public class VehicleDbContext : DbContext
  {
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Manufacturer> Manufacturers { get; set; }
    public DbSet<Distributor> Distributors { get; set; }

    public VehicleDbContext()
    {
      Database.SetInitializer<VehicleDbContext>( new VehicleDBInitializer() );
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      //modelBuilder.Entity<Vehicle>()
      //    .Map<Car>(o => o.ToTable("Cars"))
      //    .Map<Bike>(o => o.ToTable("Bikes"));
      modelBuilder.Entity<Car>().ToTable("Cars");
      modelBuilder.Entity<Bike>().ToTable("Bikes");
    }
  }

  public class VehicleDBInitializer : DropCreateDatabaseReallyAlways<VehicleDbContext>
  { 
  }

#if EF6
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
#endif
  public class VehicleDbContext2 : DbContext
  {
    public DbSet<Vehicle2> Vehicles { get; set; }

    public VehicleDbContext2()
    {
      Database.SetInitializer<VehicleDbContext2>( new VehicleDBInitializer2() );
    }
  }

  public class VehicleDBInitializer2 : DropCreateDatabaseReallyAlways<VehicleDbContext2>
  { 
  }

  /// <summary>
  /// This initializer really drops the database, not just once per AppDomain (like the DropCreateDatabaseAlways).
  /// </summary>
  /// <typeparam name="TContext"></typeparam>
  public class DropCreateDatabaseReallyAlways<TContext> : IDatabaseInitializer<TContext> where TContext : DbContext
  { 
    public void InitializeDatabase(TContext context)
    {
      context.Database.Delete();
      context.Database.CreateIfNotExists();
      this.Seed(context);
      context.SaveChanges();
    }

    protected virtual void Seed(TContext context)
    {      
    }
  }

  public class Vehicle
  {
    public int Id { get; set; }
    public int Year { get; set; }
    
    [MaxLength(1024)]
    public string Name { get; set; }
  }

#if EF6
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
#endif
  public class VehicleDbContext3 : DbContext
  {
    public DbSet<Accessory> Accessories { get; set; }

  }

  public class Accessory
  {
    [Key]
    [MaxLength(255)]
    public string Name { get; set; }

    [Required]
    [MaxLength(80000)]
    public string Description { get; set; }

    [Required]
    [MaxLength(16777216)]
    public string LongDescription { get; set; }       
    
  }

  public class Car : Vehicle
  {
    public string CarProperty { get; set; }
  }

  public class Bike : Vehicle
  {
    public string BikeProperty { get; set; }
  }
  public class Vehicle2
  {
    public int Id { get; set; }
    public int Year { get; set; }
    [MaxLength(1024)]
    public string Name { get; set; }
  }

  public class Car2 : Vehicle2
  {
    public string CarProperty { get; set; }
  }

  public class Bike2 : Vehicle2
  {
    public string BikeProperty { get; set; }
  }

  public class Manufacturer
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid ManufacturerId { get; set; }
    public string Name { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid GroupIdentifier { get; set; }
  }

  public class Distributor
  {
    public int DistributorId { get; set; }
    public string Name { get; set; }
  }


  public class Product
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime DateCreated { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(TypeName = "timestamp")]
    public DateTime Timestamp { get; set; }

    public DateTime DateTimeWithPrecision { get; set; }

    [Column(TypeName = "TimeStamp")]
    public DateTime TimeStampWithPrecision { get; set; }

  }

#if EF6
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
#endif
  public class ProductsDbContext : DbContext
  {
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      Database.SetInitializer<ProductsDbContext>(new ProductDBInitializer());

      modelBuilder.Entity<Product>()
    .Property(f => f.DateTimeWithPrecision)
    .HasColumnType("DateTime")
    .HasPrecision(3);

      modelBuilder.Entity<Product>()
    .Property(f => f.TimeStampWithPrecision)
    .HasColumnType("Timestamp")
    .HasPrecision(3);

#if EF6
      Database.SetInitializer<ProductsDbContext>(new MigrateDatabaseToLatestVersion<ProductsDbContext, Configuration<ProductsDbContext>>());
#endif
    }
  }

  public class ProductDBInitializer : DropCreateDatabaseReallyAlways<ProductsDbContext>
  {
  }

  public class Names
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public DateTime DateCreated { get; set; }
  }

#if EF6
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
#endif
  public class ShortDbContext : DbContext
  {
    public DbSet<Names> Names { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Names>()
    .Property(f => f.DateCreated)
    .HasColumnType("DateTime")
    .HasPrecision(9);
    }
  }

#if EF6
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
#endif
  public class AutoIncrementBugContext : DbContext
  {
    public DbSet<AutoIncrementBug> AutoIncrementBug { get; set; }

    public AutoIncrementBugContext()
    {
      Database.SetInitializer<AutoIncrementBugContext>(new AutoIncrementBugInitialize<AutoIncrementBugContext>());
      Database.SetInitializer<AutoIncrementBugContext>(new MigrateDatabaseToLatestVersion<AutoIncrementBugContext, AutoIncrementConfiguration<AutoIncrementBugContext>>());
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
    }
  }

  public class AutoIncrementBugInitialize<TContext> : IDatabaseInitializer<TContext> where TContext : DbContext
  {
    public void InitializeDatabase(TContext context)
    {
      context.Database.Delete();
      context.Database.CreateIfNotExists();
      this.Seed(context);
      context.SaveChanges();
    }

    protected virtual void Seed(TContext context)
    {
    }
  }

  public class AutoIncrementBug
  {
    [Key]
    public short MyKey { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AutoIncrementBugId { get; set; }
    public string Description { get; set; }
  }

  public class AutoIncrementConfiguration<TContext> : System.Data.Entity.Migrations.DbMigrationsConfiguration<TContext> where TContext : DbContext
  {
    public AutoIncrementConfiguration()
    {
      AutomaticMigrationsEnabled = true;
      //CodeGenerator = new MySqlMigrationCodeGenerator();
      SetSqlGenerator("MySql.Data.MySqlClient", new MySql.Data.Entity.MySqlMigrationSqlGenerator());
    }
  }


  [Table("client")]
  public class Client
  {
    [Key]
    public int Id { get; set; }
    public ICollection<Order> Orders { get; set; }
  }

  [Table("order")]
  public class Order
  {
    [Key]
    public int Id { get; set; }    
    public ICollection<Item> Items { get; set; }
    public ICollection<Discount> Discounts { get; set; }
  }

  [Table("item")]
  public class Item
  {
    [Key]
    public int Id { get; set; }    
  }

  [Table("discount")]
  public class Discount
  {
    [Key]
    public int Id { get; set; }
  }


#if EF6
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
#endif
  public class UsingUnionContext : DbContext
  {
    public DbSet<Client> Clients { get; set; }
    public DbSet<Item> Items { get; set; }

    public DbSet<Order> Orders { get; set; }

    public DbSet<Discount> Discounts { get; set; } 
  }



}
