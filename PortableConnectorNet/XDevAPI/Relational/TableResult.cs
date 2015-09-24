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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using MySql.Protocol;
using MySql.XDevAPI.Common;

namespace MySql.XDevAPI.Relational
{
  /// <summary>
  /// Repreents a resultset that contains rows of data.  
  /// </summary>
  public class TableResult : BufferingResult<TableRow>
  {
    Dictionary<string, int> nameMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    List<TableColumn> _columns = new List<TableColumn>();

    internal TableResult(ProtocolBase p, bool autoClose = true) : base(p, autoClose)
    {
      LoadMetadata();
    }

    /// <summary>
    /// The columns of this resulset
    /// </summary>
    public IReadOnlyList<TableColumn> Columns
    {
      get { return _columns.AsReadOnly(); }
    }

    /// <summary>
    /// The rows of this resultset.  This collection will be incomplete unless all the rows have been read
    /// either by using the Next method or the Buffer method.
    /// </summary>
    public IReadOnlyCollection<TableRow> Rows
    {
      get { return Items; }
    }

    private void LoadMetadata()
    {
      _columns = _protocol.LoadColumnMetadata();
      for (int i = 0; i < _columns.Count; i++)
        nameMap.Add(_columns[i].Name, i);
    }

    /// <summary>
    /// Allows getting the value of the column value at the current index.
    /// </summary>
    /// <param name="index">Column index</param>
    /// <returns>CLR  value at the column index</returns>
    public object this[int index]
    {
      get { return GetValue(index); }
    }

    /// <summary>
    /// Allows getting the value of the column value at the current index.
    /// </summary>
    /// <param name="index">Column index</param>
    /// <returns>CLR  value at the column index</returns>
    private object GetValue(int index)
    {
      if (Position == Items.Count)
        throw new InvalidOperationException("No data at position");
      return Items[Position][index];
    }

    /// <summary>
    /// Returns the index of the given column name
    /// </summary>
    /// <param name="name">Name of the column to find</param>
    /// <returns>Numeric index of column</returns>
    public int IndexOf(string name)
    {
      if (!nameMap.ContainsKey(name))
        throw new MySqlException("Column not found '" + name + "'");
      return nameMap[name];
    }

    protected override TableRow ReadItem(bool dumping)
    {
      ///TODO:  fix this
      List<byte[]> values = _protocol.ReadRow(_autoClose ? this : null);
      if (values == null) return null;
      if (dumping) return new TableRow(this, 0);

      Debug.Assert(values.Count == _columns.Count, "Value count does not equal column count");
      TableRow row = new TableRow(this, _columns.Count);
      row.SetValues(values);
      return row;
    }
  }
}
