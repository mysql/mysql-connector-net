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

using MySql.Communication;
using MySql.Procotol;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MySql.XDevAPI
{
  public class ResultSet
  {
    public List<ResultRow> Rows = new List<ResultRow>();
    public List<Column> Columns = new List<Column>();
    private Dictionary<string, int> nameMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    internal ProtocolBase<UniversalStream> Protocol;

    public int Position { get; private set; }
    public int PageSize { get; private set; }

    internal ResultSet()
    {
      PageSize = 20;
      Position = 0;
    }

    public void FinishLoading()
    {
      while (Next()) ;
    }

    public bool Next()
    {
      if (Position == Rows.Count)
        if (!PageInRows()) return false;
      ///TODO:  support multiple resultsets

      Position++;
      return true;
    }

    private bool PageInRows()
    {
      for (int i = 0; i < PageSize; i++)
        if (!ReadRow()) break;
      return Position < Rows.Count;
    }

    private bool ReadRow()
    {
      List<byte[]> values = Protocol.ReadRow();
      if (values == null) return false;
      Debug.Assert(values.Count == Columns.Count, "Value count does not equal column count");
      ResultRow row = new ResultRow(this, Columns.Count);
      row.SetValues(values);
      Rows.Add(row);
      return true;
    }

    internal void AddColumn(Column c)
    {
      ///TODO:  add asserts or checks here
      Columns.Add(c);
      nameMap.Add(c.Name, Columns.Count - 1);
    }

    public int IndexOf(string name)
    {
      if (!nameMap.ContainsKey(name))
        throw new MySqlException("Column not found '" + name + "'");
      return nameMap[name];
    }
  }
}
