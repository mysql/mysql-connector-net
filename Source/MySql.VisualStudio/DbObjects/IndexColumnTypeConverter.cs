// Copyright © 2008, 2010, Oracle and/or its affiliates. All rights reserved.
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
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Text;

namespace MySql.Data.VisualStudio.DbObjects
{
  class IndexColumnTypeConverter : TypeConverter
  {
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture,
        object value, Type destinationType)
    {
      if (destinationType == typeof(String))
      {
        StringBuilder str = new StringBuilder();
        List<IndexColumn> cols = (value as List<IndexColumn>);
        string separator = String.Empty;
        foreach (IndexColumn ic in cols)
        {
          str.AppendFormat("{2}{0} ({1})", ic.ColumnName,
              ic.SortOrder == IndexSortOrder.Ascending ? "ASC" : "DESC", separator);
          separator = ",";
        }
        return str.ToString();
      }
      return base.ConvertTo(context, culture, value, destinationType);
    }
  }
}
