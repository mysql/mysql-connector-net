// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.ComponentModel.DataAnnotations.Schema;



namespace MySql.Data.Entity.CodeFirst.Tests
{
  public class Movie
  {
    public int ID { get; set; }
    public string Title { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string Genre { get; set; }
    public decimal Price { get; set; }    
    public Director Director { get; set; }
    public List<MovieFormat> Formats { get; set; }
    public byte[] Data { get; set; }
  }

  public class Director
  {
    public int ID { get; set; }
    public string Name { get; set; }
    public int YearBorn { get; set; }
  }

  public class MovieFormat
  {
    [Key]
    public float Format { get; set; }
  }

  public class MovieDBContext : DbContext
  {
    public DbSet<Movie> Movies { get; set; }
    public DbSet<MovieFormat> MovieFormats { get; set; }
    public DbSet<MovieRelease> MovieReleases { get; set; }
    public DbSet<EntitySingleColumn> EntitySingleColumns { get; set; }

    public MovieDBContext()
    {
      Database.SetInitializer<MovieDBContext>(new MovieDBInitialize());
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      modelBuilder.Entity<Movie>().Property(x => x.Price).HasPrecision(16, 2);
    }
  }

  public class EntitySingleColumn
  {
    public int Id { get; set; }
  }

  public class MovieRelease
  {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public virtual int Id { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public virtual DateTime Timestamp { get; set; }

    [ConcurrencyCheck, Required, MaxLength(45)]
    public virtual string Name { get; set; }
  }

  public class MovieDBInitialize : DropCreateDatabaseReallyAlways<MovieDBContext>
  {
    protected override void Seed(MovieDBContext context)
    {
      base.Seed(context);

      context.Database.ExecuteSqlCommand("CREATE PROCEDURE GetCount() BEGIN SELECT 5; END");
      context.SaveChanges();
    }
  }
}