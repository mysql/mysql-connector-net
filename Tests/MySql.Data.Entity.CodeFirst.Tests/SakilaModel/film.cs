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
    [Table("sakila.film")]
#else
    [Table("film")]
#endif
    public partial class film
    {
        public film()
        {
            film_actor = new HashSet<film_actor>();
            film_category = new HashSet<film_category>();
            inventories = new HashSet<inventory>();
        }

        [Key]
        [Column(TypeName = "usmallint")]
        public int film_id { get; set; }

        [Required]
        [StringLength(255)]
        public string title { get; set; }

        [Column(TypeName = "text")]
        [StringLength(65535)]
        public string description { get; set; }

        [Column(TypeName = "year")]
        public short? release_year { get; set; }

        public byte language_id { get; set; }

        public byte? original_language_id { get; set; }

        public byte rental_duration { get; set; }

        public decimal rental_rate { get; set; }

        [Column(TypeName = "usmallint")]
        public int? length { get; set; }

        public decimal replacement_cost { get; set; }

        [Column(TypeName = "enum")]
        [StringLength(65532)]
        public string rating { get; set; }

        [Column(TypeName = "set")]
        [StringLength(65531)]
        public string special_features { get; set; }

        [Column(TypeName = "timestamp")]
        public DateTime last_update { get; set; }

        public virtual ICollection<film_actor> film_actor { get; set; }

        public virtual ICollection<film_category> film_category { get; set; }

        public virtual language language { get; set; }

        public virtual language language1 { get; set; }

        public virtual ICollection<inventory> inventories { get; set; }
    }
}
