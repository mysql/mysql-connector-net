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
using MySql.Data.VisualStudio.DbObjects;
using System.Text;
using System.Windows.Forms;
using MySql.Data.VisualStudio.Editors;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;

namespace MySql.Data.VisualStudio
{
  class TableNode : DocumentNode
  {
    private Table table;

    public TableNode(DataViewHierarchyAccessor hierarchyAccessor, int id) :
      base(hierarchyAccessor, id)
    {
      NodeId = "Table";
      //commandGroupGuid = GuidList.DavinciCommandSet;
      DocumentNode.RegisterNode(this);
    }

    #region Properties

    public Table Table
    {
      get { return table; }
    }

    public override bool Dirty
    {
      get
      {
        if (table == null)
          return false;

        return table.HasChanges();
      }
    }

    #endregion

    protected override string GetCurrentName()
    {
      return table.Name;
    }

    /// <summary>
    /// We override Save here because we want to prompt for a new name if this table is new and the user has
    /// not changed the default name
    /// </summary>
    /// <returns></returns>
    protected override bool Save()
    {
      if (table.IsNew && table.Name == Name)
      {
        TableNamePromptDialog dlg = new TableNamePromptDialog();
        dlg.TableName = table.Name;
        if (DialogResult.Cancel == dlg.ShowDialog()) return false;
        table.Name = dlg.TableName;
      }
      try
      {
        return base.Save();
      }
      catch ( MySql.Data.MySqlClient.MySqlException ex)
      {
        // Undo name edited
        table.Name = Name;
        throw;
      }
    }

    public static void CreateNew(DataViewHierarchyAccessor HierarchyAccessor)
    {
      TableNode node = new TableNode(HierarchyAccessor, 0);
      node.Edit();
    }

    protected override void Load()
    {
      if (IsNew)
      {
        table = new Table(this, null, null);
        table.Name = Name;
      }
      else
      {
        DbConnection connection = (DbConnection)HierarchyAccessor.Connection.GetLockedProviderObject();
        string[] restrictions = new string[4] { null, connection.Database, Name, null };
        DataTable columnsTable = connection.GetSchema("Columns", restrictions);

        DataTable dt = connection.GetSchema("Tables", restrictions);
        table = new Table(this, dt.Rows[0], columnsTable);

        HierarchyAccessor.Connection.UnlockProviderObject();
      }
      OnDataLoaded();
    }

    public override string GetSaveSql()
    {
      return Table.GetSql();
    }

    public override object GetEditor()
    {
      return new TableEditorPane(this);
    }

    public override void ExecuteCommand(int command)
    {
      if (command == PkgCmdIDList.cmdCreateTrigger)
      {
        TriggerNode.CreateNew(HierarchyAccessor, this);
      }
      else
        base.ExecuteCommand(command);
    }
  }
}
