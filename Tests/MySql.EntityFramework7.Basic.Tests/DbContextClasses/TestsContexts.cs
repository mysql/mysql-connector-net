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
using Microsoft.Data.Entity.Metadata;
using MySQL.Data.Entity.Extensions;
using System;

namespace MySql.Data.Entity.Tests.DbContextClasses
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

  }


  public class TestsContext : DbContext
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

    public TestsContext(IServiceProvider serviceProvider, DbContextOptions options)
                : base(serviceProvider, options)
    {

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
       optionsBuilder.UseMySQL(MySqlTestStore.baseConnectionString + ";database=test;");
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Blog>()
                .HasOne(p => p.RecentPost)
                .WithOne(i => i.Blog)
                .HasPrincipalKey<Blog>(p => new { p.BlogId, p.Url })
                .HasForeignKey<Post>(b => b.BlogIdFK);

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
}
