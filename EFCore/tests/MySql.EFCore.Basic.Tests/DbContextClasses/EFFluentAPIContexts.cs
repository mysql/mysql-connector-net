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
using MySql.Data.EntityFrameworkCore.Extensions;
using System;

namespace MySql.Data.EntityFrameworkCore.Tests.DbContextClasses
{
  public class ComputedColumnContext : MyTestContext
  {
    public DbSet<Employee> Employees { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Employee>()
          .HasKey(b => b.EmployeeId)
          .HasName("PrimaryKey_EmployeeId");

      modelBuilder.Entity<Employee>()
          .Property(p => p.DisplayName)
          .HasComputedColumnSql("CONCAT_WS(' ', LastName , FirstName)");

      modelBuilder.Entity<Employee>()
             .Property(p => p.Timestamp)
             .HasDefaultValue(DateTime.Now)
             //.ForMySQLHasDefaultValueSql("CURRENT_TIMESTAMP")
             .ValueGeneratedOnAddOrUpdate();

    }    
  }

    public class QuickContext : MyTestContext
    {
        public DbSet<QuickEntity> QuickEntity { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QuickEntity>()
            .HasKey(b => b.Id)
            .HasName("PrimaryKey_Id");

            modelBuilder.Entity<QuickEntity>()
            .Property(b => b.Created)
            .HasColumnType("Timestamp");
        }
    }



  public class TableConventionsContext : MyTestContext
  {
    public DbSet<Car> Cars { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      //base.OnConfiguring(optionsBuilder);
      string name = $"db-{this.GetType().Name.ToLowerInvariant()}";
      optionsBuilder.UseMySQL(MySQLTestStore.rootConnectionString + ";database=" + name + ";charset=latin1");

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Car>(e =>
      {
        e.ToTable("somecars");
      
        e.Property(p => p.LicensePlate).HasColumnType("VARCHAR(384)");
      
        e.Property(p => p.State).HasColumnType("VARCHAR(384)");

        e.HasKey(c => new { c.State, c.LicensePlate });
      });


      modelBuilder.Entity<RecordOfSale>()
              .HasOne(s => s.Car)
              .WithMany(c => c.SaleHistory)
              .HasForeignKey(s => new { s.CarState, s.CarLicensePlate })
              .HasPrincipalKey(c => new { c.State, c.LicensePlate });
    }
  }

  public class KeyConventionsContext : MyTestContext
  {
    public DbSet<Car> Cars { get; set; }
    public DbSet<Blog> Blogs { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Car>()
           .HasAlternateKey(c => c.LicensePlate)
           .HasName("AlternateKey_LicensePlate");

      modelBuilder.Entity<Blog>()
              .HasIndex(b => b.Url)
              .HasName("Index_Url");

      modelBuilder.Entity<Blog>()
        .Property(b => b.Url)
        .HasColumnType("varchar(400)");

      modelBuilder.Entity<Blog>()
           .HasOne(p => p.Metadata)
           .WithOne(i => i.Blog)
           .HasForeignKey<BlogMetadata>(b => b.BlogId);

      modelBuilder.Entity<Blog>()
             .HasOne(p => p.RecentPost)
             .WithOne(i => i.Blog)
             .HasForeignKey<Post>(b => b.PostId);

      modelBuilder.Entity<Car>()
               .Property(b => b.Make)
               .HasColumnType("varchar(100)");

      modelBuilder.Entity<Car>()
               .Property(b => b.Model)
               .HasColumnType("varchar(4)")
               .ForMySQLHasDefaultValue("1999");


    }   
  }


  public class ConcurrencyTestsContext : MyTestContext
  {
    public DbSet<Person> People { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Person>()
          .Property(p => p.SocialSecurityNumber)
          .IsConcurrencyToken();
    }
  }
}



