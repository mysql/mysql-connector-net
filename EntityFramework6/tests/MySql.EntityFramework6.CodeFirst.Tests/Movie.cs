// Copyright © 2014 Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Entity.Migrations;
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
    public virtual ICollection<MovieFormat> Formats { get; set; }
    public virtual ICollection<MovieMedia> Medias { get; set; }
    public byte[] Data { get; set; }
  }

  public class MovieMedia
  {
    public int ID { get; set; }
    public int MovieID { get; set; }
    public string Format { get; set; }
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

#if EF6
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
#endif
  public class MovieDBContext : DbContext
  {
    public DbSet<Movie> Movies { get; set; }
    public DbSet<MovieFormat> MovieFormats { get; set; }
    public DbSet<MovieRelease> MovieReleases { get; set; }
    public DbSet<EntitySingleColumn> EntitySingleColumns { get; set; }
    public DbSet<MovieMedia> Medias { get; set; }

    public MovieDBContext()
    {
#if !EF6
      Database.SetInitializer<MovieDBContext>(new MovieDBInitialize());
#else
      Database.SetInitializer<MovieDBContext>(new MigrateDatabaseToLatestVersion<MovieDBContext, Configuration<MovieDBContext>>());
#endif
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
#if EF6
      modelBuilder.Configurations.AddFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
#endif
      modelBuilder.Entity<Movie>().Property(x => x.Price).HasPrecision(16, 2);
      modelBuilder.Entity<Movie>().HasMany(p => p.Formats);
      modelBuilder.Entity<Movie>().HasMany( p => p.Medias );
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
    public static Movie[] data = new Movie[] {
          new Movie() { ID = 4, Title = "Star Wars, The Sith Revenge", ReleaseDate = new DateTime( 2005, 5, 19 ) },
          new Movie() { ID = 3, Title = "Predator", ReleaseDate = new DateTime(1987, 6, 12) },
          new Movie() { ID = 2, Title = "The Matrix", ReleaseDate = new DateTime( 1999, 3, 31 ) },
          new Movie() { ID = 1, Title = "Terminator 1", ReleaseDate = new DateTime(1984, 10, 26) }
        };

#if !EF6
    protected override void Seed(MovieDBContext ctx)
    {
      base.Seed(ctx);
      DoDataPopulation( ctx );
    }
#endif

    internal static void DoDataPopulation( MovieDBContext ctx )
    {
      ctx.Database.ExecuteSqlCommand("CREATE PROCEDURE GetCount() BEGIN SELECT 5; END");
      Movie m1 = new Movie() { Title = "Terminator 1", ReleaseDate = new DateTime(1984, 10, 26) };
      Movie m2 = new Movie() { Title = "The Matrix", ReleaseDate = new DateTime(1999, 3, 31) };
      Movie m3 = new Movie() { Title = "Predator", ReleaseDate = new DateTime(1987, 6, 12) };
      Movie m4 = new Movie() { Title = "Star Wars, The Sith Revenge", ReleaseDate = new DateTime(2005, 5, 19) };
      ctx.Movies.Add(m1);
      ctx.Movies.Add(m2);
      ctx.Movies.Add(m3);
      ctx.Movies.Add(m4);
      ctx.SaveChanges();
      ctx.Entry(m1).Collection(p => p.Medias).Load();
      m1.Medias.Add( new MovieMedia() { Format = "DVD" } );
      m1.Medias.Add( new MovieMedia() { Format = "BlueRay" } );
      ctx.Entry(m2).Collection(p => p.Medias).Load();
      m2.Medias.Add(new MovieMedia() { Format = "DVD" });
      m2.Medias.Add(new MovieMedia() { Format = "Digital" });
      ctx.Entry(m3).Collection(p => p.Medias).Load();
      m3.Medias.Add(new MovieMedia() { Format = "DVD" });
      m3.Medias.Add(new MovieMedia() { Format = "VHS" });
      ctx.Entry(m4).Collection(p => p.Medias).Load();
      m4.Medias.Add(new MovieMedia() { Format = "Digital" });
      m4.Medias.Add(new MovieMedia() { Format = "VHS" });
      ctx.SaveChanges();
    }
  }
}