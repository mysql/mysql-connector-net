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
using System.IO;

namespace MySql.Data.VisualStudio.Editors
{
  partial class GenerateChangeScriptDialog : Form
  {
    string sql;

    public GenerateChangeScriptDialog(TableNode node)
    {
      sql = node.GetSaveSql();
      InitializeComponent();
      sqlBox.Text = sql;
    }

    private void yesButton_Click(object sender, EventArgs e)
    {
      SaveFileDialog dlg = new SaveFileDialog();
      dlg.DefaultExt = ".sql";
      dlg.CheckPathExists = true;
      dlg.Filter = "SQL Files|*.sql|All Files|*.*";
      dlg.OverwritePrompt = true;
      dlg.Title = "Save Change Script";
      dlg.AutoUpgradeEnabled = false;
      dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
      DialogResult result = dlg.ShowDialog();
      if (DialogResult.OK == result)
        WriteOutChangeScript(dlg.FileName);
      Close();
    }

    private void noButton_Click(object sender, EventArgs e)
    {
      Close();
    }

    private void WriteOutChangeScript(string fileName)
    {
      StreamWriter sw = new StreamWriter(fileName);
      sw.Write(sql);
      sw.Flush();
      sw.Close();
    }
  }
}
