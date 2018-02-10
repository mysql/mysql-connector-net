// Copyright © 2016, 2017, Oracle and/or its affiliates. All rights reserved.
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
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using MySql.Data.EntityFrameworkCore.Extensions;
using System;

namespace MySql.Data.EntityFrameworkCore.Tests.DbContextClasses
{

  public class NoConfigurationContext : DbContext
  {
    public DbSet<Blog> Blogs { get; set; }
    //Empty configuration
  }

  public class SimpleContext : DbContext
  {
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }

    public SimpleContext() : base()
    {
    }

    public SimpleContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Blog>()
              .HasOne(p => p.RecentPost)
              .WithOne(i => i.Blog)
              .HasForeignKey<Post>(b => b.PostId);


      modelBuilder.Entity<Blog>()
            .HasOne(p => p.Metadata)
            .WithOne(i => i.Blog)
            .HasForeignKey<BlogMetadata>(b => b.BlogId);

    }

  }


  public class TestsContext : MyTestContext
  {
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Read> Reads { get; set; }
    public DbSet<Tag> Tags { get; set; }

    public DbSet<ReadTag> ReadTags { get; set; }

    public TestsContext()
    { }

    public TestsContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Blog>()
                .HasOne(p => p.RecentPost)
                .WithOne(i => i.Blog)
                .HasPrincipalKey<Blog>(p => new { p.BlogId, p.Url })
                .HasForeignKey<Post>(b => new { b.BlogIdFK, b.Url });

      modelBuilder.Entity<BlogMetadata>()
               .HasOne(p => p.Blog)
               .WithOne(i => i.Metadata)
               .HasForeignKey<Blog>(b => b.BlogId);


      modelBuilder.Entity<ReadTag>()
               .HasKey(t => new { t.ReadId, t.TagId });

      modelBuilder.Entity<ReadTag>()
          .HasOne(pt => pt.Read)
          .WithMany(p => p.ReadTags)
          .HasForeignKey(pt => pt.ReadId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<ReadTag>()
          .HasOne(pt => pt.Tag)
          .WithMany(t => t.ReadTags)
          .HasForeignKey(pt => pt.TagId);

    }
  }

  public class ContextWithShadowProperty : MyTestContext
  {
    public DbSet<Guest> Guests { get; set; }

    public ContextWithShadowProperty(DbContextOptions options) : base(options)
    {
      ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
    }

    public override int SaveChanges()
    {
      var now = DateTime.Now;
      foreach (EntityEntry<Guest> entry in ChangeTracker.Entries<Guest>())
      {
        if (entry.State == EntityState.Added)
        {
          entry.Property("CreatedAt").CurrentValue = now;
        }
        else if (entry.State == EntityState.Modified)
        {
          entry.Property("UpdatedAt").CurrentValue = now;
        }
      }
      return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder
       .Entity<Guest>(e => e.HasOne(a => a.Address).WithOne(p => p.Guest)
       .HasForeignKey<Address>(a => a.IdAddress)
       );

      modelBuilder.Entity<Guest>().Property<DateTime>("CreatedAt");
      modelBuilder.Entity<Guest>().Property<DateTime>("UpdatedAt");
    }
  }


  public class EagerLoadingContext : MyTestContext
  {
    public DbSet<Guest> Guests { get; set; }
    public DbSet<Relative> Persons2 { get; set; }

    public EagerLoadingContext()
    {
    }

    public EagerLoadingContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Guest>()
         .HasOne(p => p.Address)
         .WithOne(i => i.Guest)
         .HasForeignKey<Address>(i => i.IdAddress)
         .HasConstraintName("FKGuestAddress");

      modelBuilder.Entity<Relative>()
       .HasOne(p => p.Address)
       .WithOne(i => i.Relative)
       .HasForeignKey<AddressRelative>(i => i.IdAddressRelative)
       .HasConstraintName("FKRelativeAddress");

      // add shadow property
      modelBuilder.Entity<Guest>().Property<int>("RelativeId");
    }
  }

  public class JsonContext : MyTestContext
  {
    public DbSet<JsonData> JsonEntity { get; set; }
  }

  public partial class AllDataTypesContext : MyTestContext
  {
    public virtual DbSet<AllDataTypes> AllDataTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<AllDataTypes>(entity =>
      {
        entity.HasKey(e => e.AddressNumber1)
            .HasName("PRIMARY");

        entity.ToTable("all_data_types");

        entity.Property(e => e.AddressNumber1)
            .HasColumnName("address_number1")
            .HasColumnType("tinyint(4)");

        entity.Property(e => e.AddressNumber10)
            .HasColumnName("address_number10")
            .HasColumnType("bit(8)");

        entity.Property(e => e.AddressNumber2)
            .HasColumnName("address_number2")
            .HasColumnType("smallint(6)");

        entity.Property(e => e.AddressNumber3)
            .HasColumnName("address_number3")
            .HasColumnType("mediumint(9)");

        entity.Property(e => e.AddressNumber4)
            .HasColumnName("address_number4")
            .HasColumnType("int(11)");

        entity.Property(e => e.AddressNumber5)
            .HasColumnName("address_number5")
            .HasColumnType("bigint(20)");

        entity.Property(e => e.AddressNumber6)
            .HasColumnName("address_number6");

        entity.Property(e => e.AddressNumber7)
            .HasColumnName("address_number7")
            .HasColumnType("float(10,2)");

        entity.Property(e => e.AddressNumber8)
            .HasColumnName("address_number8")
            .HasColumnType("double(10,2)");

        entity.Property(e => e.AddressNumber9)
            .HasColumnName("address_number9")
            .HasColumnType("decimal(6,4)");

        entity.Property(e => e.BuildingName1)
            .IsRequired()
            .HasColumnName("building_name1")
            .HasColumnType("char(100)")
            .HasMaxLength(100);

        entity.Property(e => e.BuildingName10)
            .IsRequired()
            .HasColumnName("building_name10")
            .HasColumnType("longblob")
            .HasMaxLength(-1);

        entity.Property(e => e.BuildingName11)
            .IsRequired()
            .HasColumnName("building_name11")
            .HasColumnType("enum('x-small','small','medium','large','x-large')")
            .HasMaxLength(7);

        entity.Property(e => e.BuildingName12)
            .IsRequired()
            .HasColumnName("building_name12")
            .HasColumnType("set('x-small','small','medium','large','x-large')")
            .HasMaxLength(34);

        entity.Property(e => e.BuildingName13)
            .HasColumnName("building_name13")
            .HasColumnType("date");

        entity.Property(e => e.BuildingName14)
            .HasColumnName("building_name14")
            .HasColumnType("datetime(6)");

        entity.Property(e => e.BuildingName15)
            .HasColumnName("building_name15")
            .HasColumnType("time(6)");

        entity.Property(e => e.BuildingName16)
            .HasColumnName("building_name16")
            .HasColumnType("timestamp(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        entity.Property(e => e.BuildingName17)
            .HasColumnName("building_name17")
            .HasColumnType("year(4)");

        entity.Property(e => e.BuildingName2)
            .IsRequired()
            .HasColumnName("building_name2")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100);

        entity.Property(e => e.BuildingName3)
            .IsRequired()
            .HasColumnName("building_name3")
            .HasColumnType("tinytext")
            .HasMaxLength(255);

        entity.Property(e => e.BuildingName4)
            .IsRequired()
            .HasColumnName("building_name4")
            .HasColumnType("mediumtext")
            .HasMaxLength(16777215);

        entity.Property(e => e.BuildingName5)
            .IsRequired()
            .HasColumnName("building_name5")
            .HasColumnType("longtext")
            .HasMaxLength(-1);

        entity.Property(e => e.BuildingName6)
            .IsRequired()
            .HasColumnName("building_name6")
            .HasColumnType("binary(120)")
            .HasMaxLength(120);

        entity.Property(e => e.BuildingName7)
            .IsRequired()
            .HasColumnName("building_name7")
            .HasColumnType("varbinary(120)")
            .HasMaxLength(120);

        entity.Property(e => e.BuildingName8)
            .IsRequired()
            .HasColumnName("building_name8")
            .HasColumnType("blob")
            .HasMaxLength(65535);

        entity.Property(e => e.BuildingName9)
            .IsRequired()
            .HasColumnName("building_name9")
            .HasColumnType("mediumblob")
            .HasMaxLength(16777215);
      });
    }
  }

  public class WorldContext : MyTestContext
  {
    public virtual DbSet<Country> Countries { get; set; }
    public virtual DbSet<Continent> Continents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      modelBuilder.Entity<Continent>(entity =>
      {
        entity.ToTable("ContinentList", "db-worldcontext");

        entity.HasKey(p => p.Code);

        entity.HasMany(c => c.Countries)
          .WithOne(c => c.Continent);
      });
    }

    public void PopulateData()
    {
      using (WorldContext context = new WorldContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var america = new Continent { Code = "AM", Name = "America" };
        var europe = new Continent { Code = "EU", Name = "Europe" };
        var asia = new Continent { Code = "AS", Name = "Asia" };
        var africa = new Continent { Code = "AF", Name = "Africa" };

        context.AddRange(america, europe, asia, africa);

        var unitedStates = new Country { Code = "US", Name = "United States", Continent = america };
        var canada = new Country { Code = "CA", Name = "Canada", Continent = america };
        var mexico = new Country { Code = "MX", Name = "Mexico", Continent = america };
        var brazil = new Country { Code = "BR", Name = "Brazil", Continent = america };
        var argentina = new Country { Code = "AR", Name = "Argentina", Continent = america };
        context.AddRange(unitedStates, canada, mexico, brazil, argentina);

        var unitedKingdom = new Country { Code = "UK", Name = "United Kingdom", Continent = europe };
        var germany = new Country { Code = "DE", Name = "Germany", Continent = europe };
        var italy = new Country { Code = "IT", Name = "Italy", Continent = europe };
        var spain = new Country { Code = "ES", Name = "Spain", Continent = europe };
        var france = new Country { Code = "FR", Name = "France", Continent = europe };
        context.AddRange(unitedKingdom, germany, italy, spain, france);

        var china = new Country { Code = "CN", Name = "China", Continent = asia };
        var india = new Country { Code = "IN", Name = "India", Continent = asia };
        var japan = new Country { Code = "JP", Name = "Japan", Continent = asia };
        var southKorea = new Country { Code = "KR", Name = "South Korea", Continent = asia };
        context.AddRange(china, india, japan, southKorea);

        var nigeria = new Country { Code = "NG", Name = "Nigeria", Continent = africa };
        var egypt = new Country { Code = "EG", Name = "Egypt", Continent = africa };
        var southAfrica = new Country { Code = "ZA", Name = "South Africa", Continent = africa };
        var algeria = new Country { Code = "DZ", Name = "Algeria", Continent = africa };
        context.AddRange(nigeria, egypt, southAfrica, algeria);

        context.SaveChanges();
      }
    }
  }

  public class FiguresContext : MyTestContext
  {
    public DbSet<Triangle> Triangle { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      modelBuilder.Entity<Triangle>(entity =>
      {
        entity.HasKey(p => p.Id);

        entity.Property(p => p.Area)
          .HasComputedColumnSql("base * height / 2");
      });
    }
  }

  public class StringTypesContext : MyTestContext
  {
    public DbSet<StringTypes> StringType { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      modelBuilder.Entity<StringTypes>(entity =>
      {
        entity.HasKey(p => p.TinyString);

        entity.Property(p => p.NormalString)
          .HasColumnType("varchar(3000)");

        entity.Property(p => p.MediumString)
          .HasColumnType("mediumtext");
      });
    }
  }

  public class BodyShopContext : MyTestContext
  {
    public DbSet<Car> Car { get; set; }

    public DbSet<BodyShop> BodyShop { get; set; }

    public DbSet<Employee> Employee { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<Car>(car =>
      {
        car.ToTable("Cars", "01cars");
        car.HasKey(p => p.CarId);
      });

      modelBuilder.Entity<BodyShop>(bs =>
      {
        bs.ToTable("BodyShops", "02bodyshops");
        bs.HasKey(p => p.BodyShopId);
      });

      modelBuilder.Entity<Employee>(e =>
      {
        e.ToTable("Employees", "03employees");
        e.HasKey(p => p.EmployeeId);
      });
    }
  }

  public class MyContext : MyTestContext
  {
    public DbSet<MyTest> MyTest { get; set; }
  }

  public class CharsetTestContext : MyTestContext
  {
    public DbSet<TestCharsetDA> TestCharsetDA { get; set; }
    public DbSet<TestCharsetFA> TestCharsetFA { get; set; }
    public DbSet<TestCollationDA> TestCollationDA { get; set; }
    public DbSet<TestCollationFA> TestCollationFA { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<TestCharsetFA>(e =>
      {
        e.ForMySQLHasCharset("utf16");
        e.Property(p => p.TestCharsetFAId)
          .ForMySQLHasCharset("latin7")
          .HasMaxLength(255);
      });

      modelBuilder.Entity<TestCollationFA>(e =>
      {
        e.ForMySQLHasCollation("koi8u_bin");
        e.Property(p => p.TestCollationFAId)
          .ForMySQLHasCollation("ucs2_bin")
          .HasMaxLength(255);
      });
    }
  }
}
