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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MySql.Data.VisualStudio.Editors
{
  public partial class UDFEditor : Form
  {
    public UDFEditor()
    {
      InitializeComponent();
      returnType.SelectedIndex = 0;
    }

    public bool Aggregate
    {
      get { return aggregate.Checked; }
    }

    public string FunctionName
    {
      get { return functionName.Text.Trim(); }
    }

    public string LibraryName
    {
      get { return libraryName.Text.Trim(); }
    }

    public int ReturnType
    {
      get { return returnType.SelectedIndex; }
    }

    public string ReturnTypeByName
    {
      get { return returnType.SelectedItem as string; }
    }

    private void functionName_TextChanged(object sender, EventArgs e)
    {
      UpdateOkButton();
    }

    private void libraryName_TextChanged(object sender, EventArgs e)
    {
      UpdateOkButton();
    }

    private void UpdateOkButton()
    {
      okButton.Enabled = functionName.Text.Trim().Length > 0 &&
          libraryName.Text.Trim().Length > 0;
    }
  }
}
