// Copyright (c) 2014, 2017, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#if EF6
using System.Data.Entity.Spatial;
#endif

namespace MySql.Data.EntityFramework.CodeFirst.Tests
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
