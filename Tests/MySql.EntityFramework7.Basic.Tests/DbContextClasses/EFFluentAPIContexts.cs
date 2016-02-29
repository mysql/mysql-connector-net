// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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


using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using MySQL.Data.Entity.Extensions;
using System;

namespace MySql.Data.Entity.Tests.DbContextClasses
{
  public class ComputedColumnContext : DbContext
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
             .HasDefaultValue("0")
             .HasDefaultValueSql("CURRENT_TIMESTAMP")
             .ValueGeneratedOnAddOrUpdate();

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseMySQL(MySqlTestStore.baseConnectionString + ";database=test;");
    }
  }



  public class TableConventionsContext : DbContext
  {
    public DbSet<Car> Cars { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Car>()
          .ToTable("somecars");


      modelBuilder.Entity<RecordOfSale>()
              .HasOne(s => s.Car)
              .WithMany(c => c.SaleHistory)
              .HasForeignKey(s => new { s.CarState, s.CarLicensePlate })
              .HasPrincipalKey(c => new { c.State, c.LicensePlate });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseMySQL(MySqlTestStore.baseConnectionString + ";database=test;");
    }

  }

  public class KeyConventionsContext : DbContext
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

      modelBuilder.Entity<Car>()
               .Property(b => b.Make)
               .ForMySQLHasColumnType("varchar(100)");

      modelBuilder.Entity<Car>()
               .Property(b => b.Model)
               .ForMySQLHasDefaultValue("1999");


    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseMySQL(MySqlTestStore.baseConnectionString + ";database=test;");
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



