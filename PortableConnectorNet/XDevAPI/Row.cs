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
using System.Linq;

namespace MySql.XDevAPI
{
  public class ResultRow
  {
    private object[] values;
    private byte[][] valuesAsBytes;
    private ResultSet resultSet;

    internal ResultRow(ResultSet rs, int count)
    {
      resultSet = rs;
      values = new object[count];
      valuesAsBytes = new byte[count][];
    }

    public object this[int index]
    {
      get { return values[index]; }
      set { values[index] = value; }
    }

    public string GetString(string name)
    {
      int index = resultSet.IndexOf(name);
      return values[index].ToString();
    }
    public object this[string name]
    {
      get
      {
        return this[resultSet.IndexOf(name)];
      }
    }

    public object[] ItemArray
    {
      get { return values; }
    }

    internal void SetValues(List<byte[]> valueBuffers)
    {
      for (int i = 0; i < valueBuffers.Count; i++)
      {
        valuesAsBytes[i] = valueBuffers[i];
        values[i] = resultSet.Columns[i]._decoder.ClrValueDecoder(valueBuffers[i]);
      }
    }
  }
}
