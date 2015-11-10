// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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


namespace MySql.Data.Entity.CodeFirst.Tests
{
  public class Harbor
  {
    public int HarborId { get; set; }
    public virtual ICollection<Ship> Ships { get; set; }

    public string Description { get; set; }
  }

  public class Ship
  {
    public int ShipId { get; set; }
    public int HarborId { get; set; }
    public virtual Harbor Harbor { get; set; }
    public virtual ICollection<CrewMember> CrewMembers { get; set; }

    public string Description { get; set; }
  }

  public class CrewMember
  {
    public int CrewMemberId { get; set; }
    public int ShipId { get; set; }
    public virtual Ship Ship { get; set; }
    public int RankId { get; set; }
    public virtual Rank Rank { get; set; }
    public int ClearanceId { get; set; }
    public virtual Clearance Clearance { get; set; }

    public string Description { get; set; }
  }

  public class Rank
  {
    public int RankId { get; set; }
    public virtual ICollection<CrewMember> CrewMembers { get; set; }

    public string Description { get; set; }
  }

  public class Clearance
  {
    public int ClearanceId { get; set; }
    public virtual ICollection<CrewMember> CrewMembers { get; set; }

    public string Description { get; set; }
  }

#if EF6
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
#endif
  public class ShipContext : DbContext
  {
    public DbSet<Harbor> Harbors { get; set; }
    public DbSet<Ship> Ships { get; set; }
    public DbSet<CrewMember> CrewMembers { get; set; }
    public DbSet<Rank> Ranks { get; set; }
    public DbSet<Clearance> Clearances { get; set; }

    public ShipContext()
    {
      Database.SetInitializer(new DropCreateDatabaseAlways<ShipContext>());
    }
  }
}
