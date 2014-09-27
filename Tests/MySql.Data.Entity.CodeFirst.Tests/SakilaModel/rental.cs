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
using System.ComponentModel.DataAnnotations.Schema;
#if EF6
using System.Data.Entity.Spatial;
#endif

namespace MySql.Data.Entity.CodeFirst.Tests
{
#if EF6
    [Table("sakila.rental")]
#else
    [Table("rental")]
#endif
    public partial class rental
    {
        public rental()
        {
            payments = new HashSet<payment>();
        }

        [Key]
        public int rental_id { get; set; }

        public DateTime rental_date { get; set; }

        [Column(TypeName = "umediumint")]
        public int inventory_id { get; set; }

        [Column(TypeName = "usmallint")]
        public int customer_id { get; set; }

        public DateTime? return_date { get; set; }

        public byte staff_id { get; set; }

        [Column(TypeName = "timestamp")]
        public DateTime last_update { get; set; }

        public virtual customer customer { get; set; }

        public virtual inventory inventory { get; set; }

        public virtual ICollection<payment> payments { get; set; }

        public virtual staff staff { get; set; }
    }
}
