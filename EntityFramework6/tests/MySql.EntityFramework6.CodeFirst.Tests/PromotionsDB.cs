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
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySql.Data.Entity.CodeFirst.Tests
{
#if EF6
  [DbConfigurationType(typeof(MySqlEFConfiguration))]
#endif
  public class PromotionsDB: DbContext
  {
    public virtual DbSet<HomePromo> HomePromoes { get; set; }

    public PromotionsDB()
    {
      Database.SetInitializer<PromotionsDB>(new PromotionsDBInitializer());
    }
  }

  public class PromotionsDBInitializer : DropCreateDatabaseReallyAlways<PromotionsDB>
  {
  }

  public class HomePromo
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int ID { get; set; }

    public string Image { get; set; }

    public string Url { get; set; }

    public int DisplayOrder { get; set; }

    [Column("Active")]
    public bool Active { get; set; }
    [Column("ActiveFrom")]
    public DateTime? ActiveFrom { get; set; }
    [Column("ActiveTo")]
    public DateTime? ActiveTo { get; set; }
  }
}
