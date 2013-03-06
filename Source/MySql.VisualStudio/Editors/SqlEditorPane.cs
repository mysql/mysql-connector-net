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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio;
using System.Windows.Forms;

namespace MySql.Data.VisualStudio.Editors
{
  class SqlEditorPane : WindowPane//, IOleCommandTarget
  {
    private SqlEditor editor;
    internal SqlEditorFactory Factory { get; private set; }
    internal string DocumentPath { get; private set; }

    public SqlEditorPane(ServiceProvider sp, SqlEditorFactory factory)
      : base(sp)
    {
      Factory = factory;
      DocumentPath = factory.LastDocumentPath;
      editor = new SqlEditor(sp, this);
    }

    public override IWin32Window Window
    {
      get { return editor; }
    }

    //#region IOleCommandTarget Members

    //int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt,
    //    IntPtr pvaIn, IntPtr pvaOut)
    //{
    //    return base.Ex
    //    return VSConstants.S_OK;
    //}

    //int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds,
    //    OLECMD[] prgCmds, IntPtr pCmdText)
    //{
    //    return VSConstants.S_OK;
    //}

    //#endregion
  }
}
