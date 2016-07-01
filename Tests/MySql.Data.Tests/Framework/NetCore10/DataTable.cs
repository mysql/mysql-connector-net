// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
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

using System.Data;
using System.Collections.Generic;
using System;

namespace MySql.Data.MySqlClient.Tests
{
  public class TestDataTable
  {
    public List<DataColumn> Columns = new List<DataColumn>();
    public List<DataRow> Rows = new List<DataRow>();

    public TestDataTable()
    {

    }

    public int GetColumnIndex(string name)
    {
      for (int x = 0; x < Columns.Count; x++)
        if (Columns[x].Name == name) return x;
      throw new ArgumentOutOfRangeException("Column name not found " + name);
    }

    public DataRow NewRow()
    {
      DataRow row = new DataRow();
      row.OwningTable = this;
      return row;
    }

    public void Load(MySqlDataReader reader)
    {
      // add the columns
      for (int x = 0; x < reader.FieldCount; x++)
        Columns.Add(new DataColumn(reader.GetName(x), reader.GetFieldType(x)));

      // now add the rows
      while (reader.Read())
      {
        DataRow row = NewRow();

        object[] rowValues = new object[reader.FieldCount];
        for (int index = 0; index < reader.FieldCount; index++)
          row.Add(reader[index]);
        Rows.Add(row);
      }
    }
  }
}
