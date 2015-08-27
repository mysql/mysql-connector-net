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
using MySql.Protocol;
using System.Diagnostics;

namespace MySql.XDevAPI.Results
{
  public class TableResult : BufferingResult<TableRow>
  {
    Dictionary<string, int> nameMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    List<TableColumn> _columns = new List<TableColumn>();

    internal TableResult(ProtocolBase p) : base(p)
    {
      LoadMetadata();
    }

    public IReadOnlyList<TableColumn> Columns
    {
      get { return _columns.AsReadOnly(); }
    }

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

    public object this[int index]
    {
      get { return GetValue(index); }
    }

    private object GetValue(int index)
    {
      if (Position == Items.Count)
        throw new InvalidOperationException("No data at position");
      return Items[Position][index];
    }

    public int IndexOf(string name)
    {
      if (!nameMap.ContainsKey(name))
        throw new MySqlException("Column not found '" + name + "'");
      return nameMap[name];
    }

    protected override TableRow ReadItem()
    {
      ///TODO:  fix this
      List<byte[]> values = _protocol.ReadRow();
      if (values == null) return null;
      Debug.Assert(values.Count == _columns.Count, "Value count does not equal column count");
      TableRow row = new TableRow(this, _columns.Count);
      row.SetValues(values);
      return row;
    }
  }
}
