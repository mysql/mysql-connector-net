// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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

using MySqlX.Properties;
using System;
using System.Collections.Generic;

namespace MySqlX.XDevAPI.Relational
{
  /// <summary>
  /// Represents a single row of data in a table
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
    /// Allows getting the value of the row at the given index.
    /// </summary>
    /// <param name="index">The column index to retrieve the value</param>
    /// <returns>The value at the index</returns>
    public object this[int index]
    {
      get { return GetValue(index); }
    }

    /// <summary>
    /// Retrieves the column value as a string.
    /// </summary>
    /// <param name="name">Name of the column</param>
    /// <returns>The value of the column as a string</returns>
    public string GetString(string name)
    {
      return GetValue(name).ToString();
    }

    /// <summary>
    /// String based indexer into the row.  Returns the value as a CLR type
    /// </summary>
    /// <param name="name">The column index to get</param>
    /// <returns>CLR value for the column</returns>
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
