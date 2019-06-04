// Copyright (c) 2015, 2018, Oracle and/or its affiliates. All rights reserved.
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
