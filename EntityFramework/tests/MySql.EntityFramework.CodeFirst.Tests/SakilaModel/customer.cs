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
    [Table("sakila.customer")]
#else
    [Table("customer")]
#endif
    public partial class customer
    {
        public customer()
        {
            payments = new HashSet<payment>();
            rentals = new HashSet<rental>();
        }

        [Key]
        [Column(TypeName = "usmallint")]
        public int customer_id { get; set; }

        public byte store_id { get; set; }

        [Required]
        [StringLength(45)]
        public string first_name { get; set; }

        [Required]
        [StringLength(45)]
        public string last_name { get; set; }

        [StringLength(50)]
        public string email { get; set; }

        [Column(TypeName = "usmallint")]
        public int address_id { get; set; }

        public bool active { get; set; }

        public DateTime create_date { get; set; }

        [Column(TypeName = "timestamp")]
        public DateTime last_update { get; set; }

        public virtual address address { get; set; }

        public virtual store store { get; set; }

        public virtual ICollection<payment> payments { get; set; }

        public virtual ICollection<rental> rentals { get; set; }
    }
}
