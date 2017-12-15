// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data;
using MySqlX;
using System;
using System.Collections.Generic;

namespace MySqlX.XDevAPI.Relational
{
  /// <summary>
  /// Represents a single row of data in a table.
  /// </summary>
  public class Row
  {
    private object[] _values;
    private Dictionary<string, int> _nameMap;

    internal Row(Dictionary<string,int> nameMap, object[] values)
    {
      _values = values;
      _nameMap = nameMap;
    }

    /// <summary>
    /// Gets the value of the row at the given index.
    /// </summary>
    /// <param name="index">The column index to retrieve the value.</param>
    /// <returns>The value at the index.</returns>
    public object this[int index]
    {
      get { return GetValue(index); }
    }

    /// <summary>
    /// Gets the value of the column as a string.
    /// </summary>
    /// <param name="name">The name of the column.</param>
    /// <returns>The value of the column as a string.</returns>
    public string GetString(string name)
    {
      return GetValue(name).ToString();
    }

    /// <summary>
    /// Gets a string based indexer into the row. Returns the value as a CLR type.
    /// </summary>
    /// <param name="name">The column index to get.</param>
    /// <returns>The CLR value for the column.</returns>
    public object this[string name]
    {
      get
      {
        return GetValue(name);
      }
    }

    private object GetValue(int index)
    {
      if (index < 0 || index >= _values.Length)
        throw new IndexOutOfRangeException(String.Format(ResourcesX.InvalidRowIndex, index));
      return _values[index];
    }

    private object GetValue(string name)
    {
      if (!_nameMap.ContainsKey(name))
        throw new InvalidOperationException(String.Format(ResourcesX.InvalidNameIndex, name));
      return GetValue(_nameMap[name]);
    }
  }
}
