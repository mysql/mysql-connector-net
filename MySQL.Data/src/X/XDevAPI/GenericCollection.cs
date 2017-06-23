// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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


using MySqlX.XDevAPI.CRUD;

namespace MySqlX.XDevAPI
{

  /// <summary>
  /// Represents a collection of documents with a generic type.
  /// </summary>
  public class Collection<T> : Collection
  {
    /// <summary>
    /// Initializes a new instance of the generic Collection class based on the specified schema 
    /// and name.
    /// </summary>
    /// <param name="s">The <see cref="Schema"/> object associated to this collection.</param>
    /// <param name="name">The name of the collection.</param>
    public Collection(Schema s, string name) : base(s, name)
    {
    }

    /// <summary>
    /// Creates an <see cref="AddStatement"/> containing the provided generic object. The add 
    /// statement can be further modified before execution.
    /// </summary>
    /// <param name="value">The generic object to add.</param>
    /// <returns>An <see cref="AddStatement"/> object containing the object to add.</returns>
    public AddStatement Add(T value)
    {
      return Add(new DbDoc(value));
    }


  }
}
