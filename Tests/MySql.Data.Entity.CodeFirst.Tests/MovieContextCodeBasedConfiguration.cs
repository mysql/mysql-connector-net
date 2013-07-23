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
using System.Data.Entity.Config;


namespace MySql.Data.Entity.CodeFirst.Tests
{
  [DbConfigurationType(typeof(MyConfiguration))]
  public class MovieCodedBasedConfigDBContext : MovieDBContext
  {
    public DbSet<Director> Directors { get; set; }

    public MovieCodedBasedConfigDBContext()
      : base()
    {
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
#if EF6
      modelBuilder.Entity<Director>().MapToStoredProcedures();
#endif
    }
  }

  public class MovieDBInitializeCBC : DropCreateDatabaseReallyAlways<MovieCodedBasedConfigDBContext>
  {
    protected override void Seed(MovieCodedBasedConfigDBContext context)
    {
      base.Seed(context);
#if EF6
      context.Database.ExecuteSqlCommand("CREATE PROCEDURE Director_Insert(in Name varchar(100), in YearBorn int) BEGIN INSERT INTO Director VALUES (Name, YearBorn); SELECT LAST_INSERT_ID() AS Id; END");
#endif
      context.SaveChanges();
    }
  }
}