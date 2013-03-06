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
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;

namespace MySql.Data.VisualStudio
{
  [Guid("7363513B-298D-49eb-B9D9-264CE6C47540")]
  class TableEditorPane : WindowPane
  {
    TableEditor tableEditor;
    List<object> objectsForInspection = new List<object>();

    public TableEditorPane(TableNode table)
      : base(null)
    {
      tableEditor = new TableEditor(this, table);
    }

    public override IWin32Window Window
    {
      get { return tableEditor; }
    }

    protected override void OnCreate()
    {
      base.OnCreate();

      // set up our property window tracking
      SelectionContainer selContainer = new SelectionContainer();
      selContainer.SelectableObjects = objectsForInspection;
      selContainer.SelectedObjects = objectsForInspection;
      ITrackSelection trackSelectionRef = GetService(typeof(STrackSelection)) as ITrackSelection;
      trackSelectionRef.OnSelectChange(selContainer);
    }

    internal void SelectObject(object objectToInspect)
    {
      objectsForInspection.Clear();
      objectsForInspection.Add(objectToInspect);
    }

    internal void AddCommand(Guid group, int commandId, EventHandler doCmd, EventHandler queryCmd)
    {
      IMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as IMenuCommandService;
      if (mcs == null) return;


      CommandID cmd = new CommandID(group, commandId);
      OleMenuCommand mc = new OleMenuCommand(doCmd, cmd);
      if (queryCmd != null)
        mc.BeforeQueryStatus += queryCmd;
      mcs.AddCommand(mc);
    }
  }
}
