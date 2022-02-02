// Copyright (c) 2021, 2022, Oracle and/or its affiliates.
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

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Represents a collection of query attributes relevant to a <see cref="MySqlCommand"/>.
  /// </summary>
  public class MySqlAttributeCollection
  {
    readonly List<MySqlAttribute> _items = new List<MySqlAttribute>();

    internal MySqlAttributeCollection(MySqlCommand cmd)
    {
      Clear();
    }

    #region Public Methods
    /// <summary>
    /// Gets the <see cref="MySqlAttribute"/> at the specified index.
    /// </summary>
    public MySqlAttribute this[int index]
    {
      get { return InternalGetParameter(index); }
    }

    /// <summary>
    /// Gets the number of <see cref="MySqlAttribute"/> objects in the collection.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Adds the specified <see cref="MySqlAttribute"/> object to the <see cref="MySqlAttributeCollection"/>.
    /// </summary>
    /// <param name="value"><see cref="MySqlAttribute"/> object to add.</param>
    public MySqlAttribute SetAttribute(MySqlAttribute value)
    {
      return InternalAdd(value);
    }

    /// <summary>
    /// Adds a query attribute and its value.
    /// </summary>
    /// <param name="attributeName">Name of the query attribute.</param>
    /// <param name="value">Value of the query attribute.</param>
    public MySqlAttribute SetAttribute(string attributeName, object value)
    {
      return InternalAdd(new MySqlAttribute(attributeName, value));
    }

    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    public void Clear()
    {
      foreach (MySqlAttribute attr in _items)
        attr.Collection = null;
      _items.Clear();
    }
    #endregion

    #region Private Methods
    private MySqlAttribute InternalAdd(MySqlAttribute value)
    {
      if (value == null)
        throw new ArgumentException("The MySqlAttributeCollection only accepts non-null MySqlAttribute type objects.", "value");

      _items.Add(value);
      value.Collection = this;
      return value;
    }

    private void CheckIndex(int index)
    {
      if (index < 0 || index >= Count)
        throw new IndexOutOfRangeException("Attribute index is out of range.");
    }

    private MySqlAttribute InternalGetParameter(int index)
    {
      CheckIndex(index);
      return _items[index];
    }
    #endregion

    #region MySqlCollection Implementation
    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="MySqlAttributeCollection"/>. 
    /// </summary>
    public IEnumerator<MySqlAttribute> GetEnumerator()
    {
      return _items.GetEnumerator();
    }
    #endregion
  }
}
