// Copyright © 2017 Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.EntityFrameworkCore.DataAnnotations
{
  /// <summary>
  /// Sets the collation in an entity property.
  /// </summary>
  public class MySqlCollationAttribute : Attribute
  {
    /// <summary>
    /// Collation set in the attribute.
    /// </summary>
    public string Collation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlCollationAttribute"/> class.
    /// </summary>
    /// <param name="collation">Collation to use.</param>
    public MySqlCollationAttribute([NotNull] string collation)
    {
      Collation = collation;
    }
  }
}
