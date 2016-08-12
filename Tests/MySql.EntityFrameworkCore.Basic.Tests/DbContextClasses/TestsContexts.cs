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


using EntityFrameworkCore.Basic.Tests.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Microsoft.EntityFrameworkCore.Internal;


using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;


using MySQL.Data.EntityFrameworkCore.Extensions;
using System;
using MySQL.Data.EntityFrameworkCore;

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

    public SimpleContext(DbContextOptions options): base(options)
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

    public TestsContext(DbContextOptions options): base(options)
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

            modelBuilder.Entity<Person>().Property<int>("CreatedAt");
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
}
