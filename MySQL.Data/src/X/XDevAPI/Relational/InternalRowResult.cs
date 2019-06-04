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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using MySqlX.XDevAPI.Common;
using MySqlX.Sessions;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Linq;

namespace MySqlX.XDevAPI.Relational
{
  /// <summary>
  /// Represents a resultset that contains rows of data.
  /// </summary>
  public class InternalRowResult : BufferingResult<Row>
  {
    internal InternalRowResult(InternalSession session) : base(session)
    {
    }

    /// <summary>
    /// Gets the columns in this resultset.
    /// </summary>
#if NET_45_OR_GREATER
    public IReadOnlyList<Column> Columns
#else
    public ReadOnlyCollection<Column> Columns
#endif
    {
      get { return _columns.AsReadOnly(); }
    }

    /// <summary>
    /// Gets the number of columns in this resultset.
    /// </summary>
    public Int32 ColumnCount
    {
      get { return _columns.Count; }
    }

    /// <summary>
    /// Gets a list containing the column names in this resultset.
    /// </summary>
    public List<string> ColumnNames
    {
      get { return _columns.Select(o => o.ColumnLabel).ToList(); }
    }

    /// <summary>
    /// Gets the rows of this resultset. This collection will be incomplete unless all the rows have been read
    /// either by using the Next method or the Buffer method.
    /// </summary>
#if NET_45_OR_GREATER
    public IReadOnlyList<Row> Rows
#else
    public ReadOnlyCollection<Row> Rows
#endif
    {
      get { return _items.AsReadOnly(); }
    }

    /// <summary>
    /// Gets the value of the column value at the current index.
    /// </summary>
    /// <param name="index">The column index.</param>
    /// <returns>The CLR value at the column index.</returns>
    public object this[int index]
    {
      get { return GetValue(index); }
    }

    /// <summary>
    /// Allows getting the value of the column value at the current index.
    /// </summary>
    /// <param name="index">The column index.</param>
    /// <returns>The CLR value at the column index.</returns>
    private object GetValue(int index)
    {
      if (_position == _items.Count)
        throw new InvalidOperationException("No data at position");
      return _items[_position][index];
    }

    /// <summary>
    /// Returns the index of the given column name.
    /// </summary>
    /// <param name="name">The name of the column to find.</param>
    /// <returns>The numeric index of column.</returns>
    public int IndexOf(string name)
    {
      if (!NameMap.ContainsKey(name))
        throw new MySqlException("Column not found '" + name + "'");
      return NameMap[name];
    }

    protected override Row ReadItem(bool dumping)
    {
      ///TODO:  fix this
      List<byte[]> values = Protocol.ReadRow(this);
      if (values == null) return null;
      if (dumping) return new Row(NameMap, null);

      Debug.Assert(values.Count == _columns.Count, "Value count does not equal column count");
      object[] clrValues = new object[values.Count];
      for (int i = 0; i < values.Count; i++)
        clrValues[i] = Columns[i]._decoder.ClrValueDecoder(values[i]);

      Row row = new Row(NameMap, clrValues);
      return row;
    }
  }
}
