// Copyright © 2008, 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Data.Common;
using System.Data;
using System.Collections.Generic;
using Microsoft.VisualStudio.Data;
using MySql.Data.VisualStudio.Editors;
using System.Windows.Forms;

namespace MySql.Data.VisualStudio
{
  class UDFNode : BaseNode
  {

    public UDFNode(DataViewHierarchyAccessor hierarchyAccessor, int itemId) :
      base(hierarchyAccessor, itemId)
    {
      NodeId = "UDF";
      DocumentNode.RegisterNode(this);
    }

    public override string GetDropSQL()
    {
      return String.Format("DROP FUNCTION `{0}`.`{1}`", Database, Name);
    }

    public static void CreateNew(DataViewHierarchyAccessor HierarchyAccessor)
    {
      UDFNode node = new UDFNode(HierarchyAccessor, 0);
      node.Edit();
    }

    public override void Edit()
    {
      UDFEditor editor = new UDFEditor();
      DialogResult result = editor.ShowDialog();
      if (result == DialogResult.Cancel) return;

      string sql = "CREATE {0} FUNCTION {1} RETURNS {2} SONAME '{3}'";
      sql = String.Format(sql, editor.Aggregate ? "AGGREGATE" : "",
          editor.FunctionName, editor.ReturnTypeByName, editor.LibraryName);

      Name = editor.FunctionName;
      ExecuteSQL(sql);
      SaveNode();
    }
  }
}
