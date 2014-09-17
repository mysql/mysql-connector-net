// Copyright © 2014, Oracle and/or its affiliates. All rights reserved.
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
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations.Schema;



namespace MySql.Data.Entity.CodeFirst.Tests
{
  /*
   * This data model tests very long names to break FK limit of 64 chars.
   * Also uses Table per Type inheritance (TPT).
   * */
  public class Animalia_Chordata_Dinosauria_Eusaurischia_Theropoda
  {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(40)]
    public string Name { get; set; }
  }

  [Table("Tyrannosauridaes")]
  public class Tyrannosauridae : Animalia_Chordata_Dinosauria_Eusaurischia_Theropoda
  {
    public string SpecieName { get; set; }
    public float Weight { get; set; }
  }

  [Table("Oviraptorosaurias")]
  public class Oviraptorosauria : Animalia_Chordata_Dinosauria_Eusaurischia_Theropoda
  {
    public string SpecieName { get; set; }
    public int EggsPerYear { get; set; }
  }

#if EF6
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
#endif
  public class DinosauriaDBContext : DbContext
  {
    public DbSet<Animalia_Chordata_Dinosauria_Eusaurischia_Theropoda> dinos { get; set; }

    public DinosauriaDBContext()
    {
      Database.SetInitializer<DinosauriaDBContext>(new DinosauriaDBInitializer());
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Tyrannosauridae>().ToTable("Tyrannosauridaes");
      modelBuilder.Entity<Oviraptorosauria>().ToTable("Oviraptorosaurias");
    }
  }

  public class DinosauriaDBInitializer : DropCreateDatabaseReallyAlways<DinosauriaDBContext>
  {
  }
}
