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
using MySql.Data.VisualStudio.DbObjects;
using System.Collections;
using MySql.Data.VisualStudio.Properties;

namespace MySql.Data.VisualStudio.Editors
{
  partial class ForeignKeyDialog : Form
  {
    TableNode tableNode;
    List<string> columnNames = new List<string>();
    List<string> fkColumnNames = new List<string>();
    const string None = "<None>";

    public ForeignKeyDialog(TableNode node)
    {
      tableNode = node;
      Application.EnableVisualStyles();
      InitializeComponent();

      // create a list of all tables in this database
      DataTable dt = tableNode.GetDataTable(
        String.Format(@"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
          WHERE TABLE_SCHEMA = '{0}' AND ENGINE = 'InnoDB'", tableNode.Database));
      List<string> tables = new List<string>();
      foreach (DataRow row in dt.Rows)
        tables.Add(row[0].ToString());
      refTable.DataSource = tables;

      colGridColumn.HeaderText = tableNode.Table.Name;
      colGridColumn.Items.Add(None);
      foreach (Column c in tableNode.Table.Columns)
      {
        if (c.ColumnName == null) continue;
        columnNames.Add(c.ColumnName);
        colGridColumn.Items.Add(c.ColumnName);
      }
     
      foreignKeyBindingSource.DataSource = tableNode.Table.ForeignKeys;
      fkList.DataSource = foreignKeyBindingSource;
      if (!InEditMode) ShowEditControls(false);
    }

    private bool ValidGridData()
    {
      if (!InEditMode) return true;

      // Removes empty rows
      columnGrid.CurrentCell = null;
      for (int i = 0; i < fkColumnsBindingSource.Count; i++)
      {
        FKColumnPair pair = fkColumnsBindingSource[i] as FKColumnPair;
        if ((string.IsNullOrEmpty(pair.Column) || pair.Column.Equals(None))
          && (string.IsNullOrEmpty(pair.ReferencedColumn) || pair.ReferencedColumn.Equals(None)))
        {
          fkColumnsBindingSource.RemoveAt(i);
          columnGrid.CurrentCell = null;
          i--;
        }
      }

      for( int i = 0; i < columnGrid.Rows.Count; i++ )
      {
        string str1 = ( string )((DataGridViewComboBoxCell)columnGrid.Rows[i].Cells[0]).FormattedValue;
        string str2 = (string)((DataGridViewComboBoxCell)columnGrid.Rows[i].Cells[1]).FormattedValue;
        if ((string.IsNullOrEmpty(str1) && string.IsNullOrEmpty(str2) && i == 0 ) ||
          (str1.Equals("<None>", StringComparison.InvariantCultureIgnoreCase)) ||
          (str2.Equals("<None>", StringComparison.InvariantCultureIgnoreCase)))
        {
          MessageBox.Show( Resources.FkDlgBeforeClose );
          return false;
        }
      }
      foreach( object o in foreignKeyBindingSource )
      {
        ForeignKey fk = ( ForeignKey )o;
        if( fk.Columns.Count == 0 )
        {
          MessageBox.Show( string.Format( Resources.FkNoColumnsForForeignKey, fk.Name ), Resources.ErrorCaption );
          return false;
        }
      }
      return true;
    }

    private void closeButton_Click(object sender, EventArgs e)
    {
      if (ValidGridData())
      {
        Close();
      }
    }

    private void addButton_Click(object sender, EventArgs e)
    {      
      ForeignKey key = new ForeignKey(tableNode.Table, null);
      if (refTable.SelectedValue != null)
      {
        key.SetName(String.Format("FK_{0}_{1}", tableNode.Table.Name,
            refTable.SelectedValue), true);
        key.NameSet = false;
        key.ReferencedTable = refTable.SelectedValue.ToString();
      }
      foreignKeyBindingSource.Add(key);
      fkList.SelectedIndex = fkList.Items.Count - 1;
    }

    private void deleteButton_Click(object sender, EventArgs e)
    {
      int index = fkList.SelectedIndex;      
      tableNode.Table.ForeignKeys.Delete(index);
      (fkList.DataSource as BindingSource).ResetBindings(false);
    }

    private void refTable_SelectedIndexChanged(object sender, EventArgs e)
    {      
      string refTableName = refTable.Items[refTable.SelectedIndex].ToString();
      fkGridColumn.HeaderText = refTableName;

      //reset the items list for the fk column
      string sql = @"SELECT column_name FROM INFORMATION_SCHEMA.COLUMNS WHERE 
                TABLE_SCHEMA='{0}' AND TABLE_NAME='{1}'";
      DataTable dt = tableNode.GetDataTable(String.Format(sql, tableNode.Database, refTableName));
      fkColumnNames.Clear();
      foreach (DataRow row in dt.Rows)
        fkColumnNames.Add(row[0].ToString());

      fkGridColumn.Items.Clear();
      fkGridColumn.Items.Add(None);
      foreach (string col in fkColumnNames)
        fkGridColumn.Items.Add(col);

      if (foreignKeyBindingSource.Current == null) return;

      // update the key name if it is not already finalized
      ForeignKey key = foreignKeyBindingSource.Current as ForeignKey;
      if (key.IsNew && !key.NameSet)
      {
        string name = String.Format("FK_{0}_{1}", tableNode.Table.Name, refTableName);
        key.SetName(name, true);
      }
    }

    private void columnGrid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
    {
      Type t = e.Control.GetType();
      if (t != typeof(DataGridViewComboBoxEditingControl)) return;

      DataGridViewComboBoxEditingControl ec = e.Control as DataGridViewComboBoxEditingControl;
      ec.DrawMode = DrawMode.OwnerDrawFixed;
      ec.DrawItem += new DrawItemEventHandler(dropdown_DrawItem);

      // now update the items that should be seen in this control
      ec.Items.Clear();
      ec.Items.Add(None);
      int index = columnGrid.CurrentCell.ColumnIndex;
      List<string> cols = index == 0 ? columnNames : fkColumnNames;
      ForeignKey key = foreignKeyBindingSource.Current as ForeignKey;

      foreach (string s in cols)
      {
        bool alreadyUsed = false;
        if (s != (string)columnGrid.CurrentCell.Value)
          foreach (FKColumnPair pair in key.Columns)
            if ((index == 0 && pair.Column == s) ||
                (index == 1 && pair.ReferencedColumn == s))
            {
              alreadyUsed = true;
              break;
            }
        if (!alreadyUsed)
          ec.Items.Add(s);
      }
      int selIndex = ec.FindStringExact(columnGrid.CurrentCell.Value as string);
      if (selIndex > 0)
        ec.SelectedIndex = selIndex;
    }

    void dropdown_DrawItem(object sender, DrawItemEventArgs e)
    {
      MyComboBox.DrawComboBox(sender as ComboBox, e);
    }

    private bool InEditMode { 
      get {
        return foreignKeyBindingSource.Current != null && fkColumnsBindingSource.DataSource == ((ForeignKey)foreignKeyBindingSource.Current).Columns;
      }
    }

    private void foreignKeyBindingSource_CurrentChanged(object sender, EventArgs e)
    {
      ShowEditControls(foreignKeyBindingSource.Current != null);
      if (foreignKeyBindingSource.Current == null)
      {
        columnGrid.Rows.Clear();
        return;
      }
      ForeignKey key = foreignKeyBindingSource.Current as ForeignKey;
      fkColumnsBindingSource.DataSource = key.Columns;
    }

    private void ShowEditControls(bool Show)
    {
      columnGrid.Visible = Show;
      matchType.Visible = Show;
      label6.Visible = Show;
      deleteAction.Visible = Show;
      label5.Visible = Show;
      updateAction.Visible = Show;
      label3.Visible = Show;
      refTable.Visible = Show;
      label2.Visible = Show;
      fkName.Visible = Show;
      label4.Visible = Show;
      label7.Visible = Show;
    }

    private void columnGrid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
    {
      int index = e.ColumnIndex;
      DataGridViewComboBoxCell cell =
          (DataGridViewComboBoxCell)columnGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];

      FKColumnPair pair = fkColumnsBindingSource.Current as FKColumnPair;
      string value = e.FormattedValue as string;

      if (value == None)
      {
        cell.Value = null;
        if (index == 0)
          pair.Column = null;
        else
          pair.ReferencedColumn = null;
      }
      else
        cell.Value = e.FormattedValue as string;
    }

    private void columnGrid_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
    {
      if (columnGrid.Rows[e.RowIndex].IsNewRow)
        return;

      int index = e.RowIndex;

      DataGridViewCell parentCell = columnGrid.Rows[e.RowIndex].Cells[0];
      DataGridViewCell childCell = columnGrid.Rows[e.RowIndex].Cells[1];
      string parent = parentCell.Value as string;
      string child = childCell.Value as string;

      bool bad = false;
      parentCell.ErrorText = childCell.ErrorText = null;

      if ((String.IsNullOrEmpty(parent) || parent == None) &&
          (!String.IsNullOrEmpty(child) && child != None))
      {
        parentCell.ErrorText = Resources.FKNeedColumn;
        bad = true;
      }
      else if ((String.IsNullOrEmpty(child) || child == None) &&
          (!String.IsNullOrEmpty(parent) && parent != None))
      {
        childCell.ErrorText = Resources.FKNeedColumn;
        bad = true;
      }
      if (bad)
      {
        MessageBox.Show(Resources.FKColumnsNotMatched, null, MessageBoxButtons.OK, MessageBoxIcon.Information);
        e.Cancel = true;
        return;
      }
      else if( 
        ( refTable.SelectedValue.ToString() == tableNode.Table.Name ) &&
        ( parent == child ) )
      {
        MessageBox.Show(Resources.FKSameColumn, null, MessageBoxButtons.OK, MessageBoxIcon.Information);
        e.Cancel = true;
        return;
      }
      FKColumnPair pair = fkColumnsBindingSource.Current as FKColumnPair;
      pair.Column = parent;
      pair.ReferencedColumn = child;
      fkColumnsBindingSource.EndEdit();
    }

    private void columnGrid_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
    {
      if (columnGrid.Rows[e.RowIndex].IsNewRow)
        return;

      FKColumnPair pair = fkColumnsBindingSource[e.RowIndex] as FKColumnPair;
      switch (e.ColumnIndex)
      {
        case 0:
          e.Value = pair.Column;
          break;
        case 1:
          e.Value = pair.ReferencedColumn;
          break;
      }
    }

    private void columnGrid_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
    {
      if (columnGrid.Rows[e.RowIndex].IsNewRow)
        return;

      FKColumnPair fk;
      if ((e.RowIndex == columnGrid.Rows.Count - 1) && (columnGrid.Rows.Count > fkColumnsBindingSource.Count))
      {
        fk = new FKColumnPair() { Column = "", ReferencedColumn = "" };
        fkColumnsBindingSource.Add(fk);
      }
      else
      {
        fk = (fkColumnsBindingSource[e.RowIndex] as FKColumnPair);
      }
      switch (e.ColumnIndex)
      {
        case 0:
          fk.Column = (string)e.Value == None ? null : (string)e.Value;
          break;
        case 1:
          fk.ReferencedColumn = (string)e.Value == None ? null : (string)e.Value;
          break;
      }
    }

    private void columnGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
    {
      e.ThrowException = false;
      if (e.Context != DataGridViewDataErrorContexts.Display &&
        e.Context != DataGridViewDataErrorContexts.Formatting)
      {
        return;
      }
    }

    private void ForeignKeyDialog_FormClosing(object sender, FormClosingEventArgs e)
    {
      if ((e.CloseReason != CloseReason.TaskManagerClosing) &&
        (e.CloseReason != CloseReason.WindowsShutDown ) &&
        (e.CloseReason != CloseReason.ApplicationExitCall ))
      {
        if (!ValidGridData()) e.Cancel = true;
      }
    }

    private void fkName_TextChanged(object sender, EventArgs e)
    {
      if (fkName.Modified)
      {
        ForeignKey key = foreignKeyBindingSource.Current as ForeignKey;
        key.NameSet = true;
      }
    }

  }
}
