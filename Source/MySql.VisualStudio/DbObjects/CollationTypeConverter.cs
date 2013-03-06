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
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;

namespace MySql.Data.VisualStudio.DbObjects
{
  internal class CollationTypeConverter : StringConverter
  {
    private DataTable collationData;

    public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
    {
      return true;
    }

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
    {
      return true;
    }

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    {
      Table table = (context.Instance is Table) ?
          (context.Instance as Table) : (context.Instance as Column).OwningTable;

      if (collationData == null)
        PopulateList(table);
      StandardValuesCollection coll =
          new StandardValuesCollection(GetRelevantCollations(context.Instance));
      return coll;
    }

    private List<string> GetRelevantCollations(object instance)
    {
      List<string> collations = new List<string>();
      string charset = String.Empty;
      if (instance is Table)
        charset = (instance as Table).CharacterSet;
      else
        charset = (instance as Column).CharacterSet;
      if (String.IsNullOrEmpty(charset)) return collations;

      foreach (DataRow row in collationData.Rows)
        if (row["charset"].Equals(charset))
          collations.Add(row["collation"].ToString());
      return collations;
    }

    private void PopulateList(Table table)
    {
      collationData = table.OwningNode.GetDataTable("SHOW COLLATION");
    }
  }
}
