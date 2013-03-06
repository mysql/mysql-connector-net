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
using System.Windows.Forms;
using MySql.Data.VisualStudio.Editors;
using System.Drawing;
using System.Collections;
using MySql.Data.VisualStudio.Properties;
using System.ComponentModel;
using Microsoft.VisualStudio.OLE.Interop;
using MySql.Data.VisualStudio.DbObjects;
using System.Runtime.InteropServices;
using System.Reflection;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;

namespace MySql.Data.VisualStudio
{
  class TableEditor : UserControl
  {
    private TableNode tableNode;
    private System.ComponentModel.IContainer components;
    private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
    private DataGridViewComboBoxColumn dataGridViewComboBoxColumn1;
    private TabControl tabControl1;
    private TabPage tabPage1;
    private VS2005PropertyGrid columnProperties;
    private Panel panel1;
    private DataGridView columnGrid;
    private TableEditorPane pane;
    private MySplitter splitter1;
    private DataGridViewTextBoxColumn NameColumn;
    private DataGridViewComboBoxColumn TypeColumn;
    private DataGridViewCheckBoxColumn AllowNullColumn;
    private BindingSource columnBindingSource;
    private string[] dataTypes;

    public TableEditor(TableEditorPane pane, TableNode node)
    {
      this.pane = pane;
      tableNode = node;
      node.Saving += delegate { EndGridEditMode(); };
      InitializeComponent();

      dataTypes = Metadata.GetDataTypes(true);
      TypeColumn.Items.AddRange((object[])dataTypes);

      tableNode.DataLoaded += new EventHandler(OnDataLoaded);
      columnGrid.RowTemplate.HeaderCell = new MyDataGridViewRowHeaderCell();
      SetupCommands();
    }

    #region Properties

    List<Index> Indexes
    {
      get { return tableNode.Table.Indexes; }
    }

    List<Column> Columns
    {
      get { return tableNode.Table.Columns; }
    }

    #endregion

    void OnDataLoaded(object sender, EventArgs e)
    {
      columnBindingSource.DataSource = tableNode.Table.Columns;

      // if our type column doesn't already contain our data types then we add 
      // them so our grid won't complain about unsupported values
      foreach (Column c in tableNode.Table.Columns)
        if (!TypeColumn.Items.Contains(c.DataType))
          TypeColumn.Items.Add(c.DataType);

      columnBindingSource.AddingNew -= new AddingNewEventHandler(columnBindingSource_AddingNew);
      tableNode.Table.DataUpdated -= new EventHandler(Table_DataUpdated);
      columnBindingSource.AddingNew += new AddingNewEventHandler(columnBindingSource_AddingNew);
      pane.SelectObject(tableNode.Table);
      tableNode.Table.DataUpdated += new EventHandler(Table_DataUpdated);
    }

    void Table_DataUpdated(object sender, EventArgs e)
    {
      columnGrid.Refresh();
    }

    void columnBindingSource_AddingNew(object sender, AddingNewEventArgs e)
    {
      //Column c = new Column(null);
      ColumnWithTypeDescriptor c = new ColumnWithTypeDescriptor();
      c.OwningTable = tableNode.Table;
      e.NewObject = c;
      c.DataType = "varchar(10)";
    }

    private void InitializeComponent()
    {
      this.components = new System.ComponentModel.Container();
      System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
      System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
      this.tabControl1 = new System.Windows.Forms.TabControl();
      this.tabPage1 = new System.Windows.Forms.TabPage();
      this.columnProperties = new MySql.Data.VisualStudio.Editors.VS2005PropertyGrid();
      this.panel1 = new System.Windows.Forms.Panel();
      this.columnGrid = new System.Windows.Forms.DataGridView();
      this.NameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.TypeColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
      this.AllowNullColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
      this.columnBindingSource = new System.Windows.Forms.BindingSource(this.components);
      this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.dataGridViewComboBoxColumn1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
      this.splitter1 = new MySql.Data.VisualStudio.Editors.MySplitter();
      this.tabControl1.SuspendLayout();
      this.tabPage1.SuspendLayout();
      this.panel1.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.columnGrid)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.columnBindingSource)).BeginInit();
      this.SuspendLayout();
      // 
      // tabControl1
      // 
      this.tabControl1.Controls.Add(this.tabPage1);
      this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tabControl1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.tabControl1.Location = new System.Drawing.Point(6, 10);
      this.tabControl1.Name = "tabControl1";
      this.tabControl1.Padding = new System.Drawing.Point(6, 6);
      this.tabControl1.SelectedIndex = 0;
      this.tabControl1.Size = new System.Drawing.Size(612, 308);
      this.tabControl1.TabIndex = 1;
      // 
      // tabPage1
      // 
      this.tabPage1.BackColor = System.Drawing.SystemColors.Control;
      this.tabPage1.Controls.Add(this.columnProperties);
      this.tabPage1.Location = new System.Drawing.Point(4, 30);
      this.tabPage1.Name = "tabPage1";
      this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
      this.tabPage1.Size = new System.Drawing.Size(604, 274);
      this.tabPage1.TabIndex = 0;
      this.tabPage1.Text = "Column Properties";
      // 
      // columnProperties
      // 
      this.columnProperties.Dock = System.Windows.Forms.DockStyle.Fill;
      this.columnProperties.Location = new System.Drawing.Point(3, 3);
      this.columnProperties.Name = "columnProperties";
      this.columnProperties.Size = new System.Drawing.Size(598, 268);
      this.columnProperties.TabIndex = 0;
      this.columnProperties.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.columnProperties_PropertyValueChanged);
      // 
      // panel1
      // 
      this.panel1.BackColor = System.Drawing.SystemColors.Control;
      this.panel1.Controls.Add(this.tabControl1);
      this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panel1.Location = new System.Drawing.Point(0, 103);
      this.panel1.Name = "panel1";
      this.panel1.Padding = new System.Windows.Forms.Padding(6, 10, 6, 6);
      this.panel1.Size = new System.Drawing.Size(624, 324);
      this.panel1.TabIndex = 9;
      // 
      // columnGrid
      // 
      this.columnGrid.AllowUserToResizeRows = false;
      this.columnGrid.AutoGenerateColumns = false;
      this.columnGrid.BackgroundColor = System.Drawing.SystemColors.Window;
      this.columnGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
      this.columnGrid.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
      dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
      dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
      dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
      dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
      dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
      dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
      this.columnGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
      this.columnGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.columnGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.NameColumn,
            this.TypeColumn,
            this.AllowNullColumn});
      this.columnGrid.DataSource = this.columnBindingSource;
      dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
      dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Window;
      dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.ControlText;
      dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.GradientActiveCaption;
      dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
      dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
      this.columnGrid.DefaultCellStyle = dataGridViewCellStyle4;
      this.columnGrid.Dock = System.Windows.Forms.DockStyle.Top;
      this.columnGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
      this.columnGrid.GridColor = System.Drawing.SystemColors.ControlLight;
      this.columnGrid.Location = new System.Drawing.Point(0, 0);
      this.columnGrid.Name = "columnGrid";
      this.columnGrid.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
      this.columnGrid.RowHeadersWidth = 25;
      this.columnGrid.ShowCellErrors = false;
      this.columnGrid.ShowEditingIcon = false;
      this.columnGrid.ShowRowErrors = false;
      this.columnGrid.Size = new System.Drawing.Size(624, 103);
      this.columnGrid.TabIndex = 2;
      this.columnGrid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.columnGrid_CellClick);
      this.columnGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.columnGrid_CellContentClick);
      this.columnGrid.CellEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.columnGrid_CellEnter);
      this.columnGrid.CellLeave += new System.Windows.Forms.DataGridViewCellEventHandler(this.columnGrid_CellLeave);
      this.columnGrid.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.columnGrid_CellValidating);
      this.columnGrid.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.columnGrid_EditingControlShowing);
      this.columnGrid.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.columnGrid_UserDeletingRow);
      this.columnGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.columnGrid_DataError);
      // 
      // NameColumn
      // 
      this.NameColumn.DataPropertyName = "ColumnName";
      this.NameColumn.HeaderText = "Column Name";
      this.NameColumn.MinimumWidth = 200;
      this.NameColumn.Name = "NameColumn";
      this.NameColumn.Width = 200;
      // 
      // TypeColumn
      // 
      this.TypeColumn.DataPropertyName = "DataType";
      this.TypeColumn.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
      this.TypeColumn.DisplayStyleForCurrentCellOnly = true;
      this.TypeColumn.HeaderText = "Data Type";
      this.TypeColumn.Name = "TypeColumn";
      // 
      // AllowNullColumn
      // 
      this.AllowNullColumn.DataPropertyName = "AllowNull";
      this.AllowNullColumn.HeaderText = "Allow Nulls";
      this.AllowNullColumn.Name = "AllowNullColumn";
      // 
      // columnBindingSource
      // 
      this.columnBindingSource.AllowNew = true;
      this.columnBindingSource.DataSource = typeof(MySql.Data.VisualStudio.DbObjects.Column);
      this.columnBindingSource.CurrentChanged += new System.EventHandler(this.columnBindingSource_CurrentChanged);
      // 
      // dataGridViewTextBoxColumn1
      // 
      this.dataGridViewTextBoxColumn1.DataPropertyName = "ColumnName";
      this.dataGridViewTextBoxColumn1.HeaderText = "Column Name";
      this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
      this.dataGridViewTextBoxColumn1.Width = 150;
      // 
      // dataGridViewComboBoxColumn1
      // 
      this.dataGridViewComboBoxColumn1.DataPropertyName = "DataType";
      this.dataGridViewComboBoxColumn1.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
      this.dataGridViewComboBoxColumn1.DisplayStyleForCurrentCellOnly = true;
      this.dataGridViewComboBoxColumn1.HeaderText = "Data Type";
      this.dataGridViewComboBoxColumn1.Name = "dataGridViewComboBoxColumn1";
      // 
      // splitter1
      // 
      this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
      this.splitter1.Location = new System.Drawing.Point(0, 103);
      this.splitter1.Name = "splitter1";
      this.splitter1.Size = new System.Drawing.Size(624, 6);
      this.splitter1.TabIndex = 10;
      this.splitter1.TabStop = false;
      // 
      // TableEditor
      // 
      this.BackColor = System.Drawing.SystemColors.Window;
      this.Controls.Add(this.splitter1);
      this.Controls.Add(this.panel1);
      this.Controls.Add(this.columnGrid);
      this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.Margin = new System.Windows.Forms.Padding(0);
      this.Name = "TableEditor";
      this.Size = new System.Drawing.Size(624, 427);
      this.tabControl1.ResumeLayout(false);
      this.tabPage1.ResumeLayout(false);
      this.panel1.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.columnGrid)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.columnBindingSource)).EndInit();
      this.ResumeLayout(false);

    }

    #region Command Handling

    private void SetupCommands()
    {
      pane.AddCommand(GuidList.DavinciCommandSet, SharedCommands.cmdidIndexesAndKeys,
          new EventHandler(OnIndexesAndKeys), null);
      pane.AddCommand(GuidList.DavinciCommandSet, SharedCommands.cmdidForeignKeys,
          new EventHandler(OnForeignKeys), new EventHandler(OnQueryForeignKeys));
      pane.AddCommand(VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.PrimaryKey,
          new EventHandler(OnPrimaryKey), new EventHandler(OnQueryPrimaryKey));
      pane.AddCommand(VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.GenerateChangeScript,
          new EventHandler(OnGenerateChangeScript), new EventHandler(OnQueryGenerateChangeScript));
    }

    private void OnQueryPrimaryKey(object sender, EventArgs e)
    {
      bool allKeys = columnGrid.SelectedRows.Count == 0 ? false : true;

      foreach (DataGridViewRow row in columnGrid.SelectedRows)
        if (!tableNode.Table.Columns[row.Index].PrimaryKey)
        {
          allKeys = false;
          break;
        }


      OleMenuCommand primaryKey = sender as OleMenuCommand;
      primaryKey.Checked = allKeys;
    }

    private void OnPrimaryKey(object sender, EventArgs e)
    {
      OleMenuCommand primaryKey = sender as OleMenuCommand;

      foreach (Column c in Columns)
        c.PrimaryKey = false;
      tableNode.Table.DeleteKey(null);

      // if not checked then we are setting the key columns
      if (!primaryKey.Checked)
      {
        Index index = tableNode.Table.CreateIndexWithUniqueName(true);

        List<int> rows = new List<int>();

        foreach (DataGridViewRow row in columnGrid.SelectedRows)
          rows.Add(row.Index);
        if (columnGrid.SelectedRows.Count == 0)
          rows.Add(columnGrid.CurrentCell.RowIndex);
        foreach (int row in rows)
        {
          Columns[row].PrimaryKey = true;
          IndexColumn ic = new IndexColumn();
          ic.OwningIndex = index;
          ic.ColumnName = Columns[row].ColumnName;
          ic.SortOrder = IndexSortOrder.Ascending;
          index.Columns.Add(ic);
        }
        if (index.Columns.Count > 0)
          Indexes.Add(index);
      }
      columnGrid.Refresh();
    }

    private void OnIndexesAndKeys(object sender, EventArgs e)
    {
      TableIndexDialog dlg = new TableIndexDialog(tableNode);
      dlg.ShowDialog();
    }

    private void OnQueryForeignKeys(object sender, EventArgs e)
    {
      OleMenuCommand foreignKey = sender as OleMenuCommand;
      foreignKey.Enabled = tableNode.Table.SupportsFK;
    }

    private void OnForeignKeys(object sender, EventArgs e)
    {
      ForeignKeyDialog dlg = new ForeignKeyDialog(tableNode);
      dlg.ShowDialog();
    }

    private void OnQueryGenerateChangeScript(object sender, EventArgs e)
    {
      bool hasChanges = tableNode.Table.HasChanges();
      OleMenuCommand gcsKey = sender as OleMenuCommand;
      gcsKey.Enabled = hasChanges;
    }

    private void OnGenerateChangeScript(object sender, EventArgs e)
    {
      GenerateChangeScriptDialog dlg = new GenerateChangeScriptDialog(tableNode);
      dlg.ShowDialog();
    }

    #endregion

    private void columnGrid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
    {
      Type t = e.Control.GetType();
      if (t == typeof(DataGridViewComboBoxEditingControl))
      {
        DataGridViewComboBoxEditingControl ec = e.Control as DataGridViewComboBoxEditingControl;
        ec.DropDownStyle = ComboBoxStyle.DropDown;
        columnGrid.NotifyCurrentCellDirty(true);
        ec.Items.Clear();
        foreach (string s in dataTypes)
          ec.Items.Add(s);
        if (tableNode.Table.Columns.Count > columnGrid.CurrentRow.Index)
        {
          Column c = tableNode.Table.Columns[columnGrid.CurrentRow.Index];
          AdjustComboBox(ec, c.DataType);
        }
        ec.TextChanged += new EventHandler(tbDataType_TextChanged);
      }
      else if (t == typeof(DataGridViewTextBoxEditingControl))
      {
        DataGridViewTextBoxEditingControl tec = e.Control as DataGridViewTextBoxEditingControl;
        tec.Multiline = true;
        tec.Dock = DockStyle.Fill;
        tec.BorderStyle = BorderStyle.Fixed3D;        
        tec.TextChanged += new EventHandler(tbColumnName_TextChanged);
      }
    }
    

    private void tbColumnName_TextChanged(object sender, EventArgs e)
    {
      if ((string)columnGrid.CurrentCell.Value != ((DataGridViewTextBoxEditingControl)sender).Text)      
        tableNode.Table.Columns[columnGrid.CurrentRow.Index].ColumnName = ((DataGridViewTextBoxEditingControl)sender).Text;               
    }

    private void tbDataType_TextChanged(object sender, EventArgs e)
    {
      Column c = tableNode.Table.Columns[columnGrid.CurrentRow.Index];
      string oldType = c.DataType;
      string newType = ((DataGridViewComboBoxEditingControl)sender).Text;
      if (!string.IsNullOrEmpty(newType))
      {
        c.DataType = newType;
        if (oldType != c.DataType)
        {
          // Reset properties, since some may not make sense after change of type.
          c.ResetProperties();
        }
      }
    }

    private void columnBindingSource_CurrentChanged(object sender, EventArgs e)
    {
      Column currentObject = columnBindingSource.Current as Column;
      columnProperties.SelectedObject = currentObject;
    }


    private void AdjustComboBox(ComboBox cb, string type)
    {
      if (String.IsNullOrEmpty(type)) return;
      int index = type.IndexOf("(");
      if (index == -1)
        cb.Items.Add(type);
      else
      {
        string baseType = type.Substring(0, index);
        for (int i = 0; i < cb.Items.Count; i++)
        {
          string item = cb.Items[i] as string;
          if (item.StartsWith(baseType, StringComparison.OrdinalIgnoreCase))
          {
            cb.Items[i] = type;
            break;
          }
        }
      }
    }

    private void columnGrid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
    {
      if (e.ColumnIndex != 1) return;
      string type = e.FormattedValue as string;
      if (String.IsNullOrEmpty(type)) return;
      if (!TypeColumn.Items.Contains(type))
      {
        var typeToAdd = type.IndexOf('(') >= 0 ? type.Substring(0, type.IndexOf('(')) : type;
        List<string> types = new List<string>(dataTypes.Length);
        types.AddRange(Metadata.GetDataTypes(false));
        if (types.Contains(typeToAdd.ToLowerInvariant()))
        {
          TypeColumn.Items.Add(type);          
        }
        else
        {
          MessageBox.Show(Resources.InvalidDataType, Resources.ErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
          columnGrid.CurrentCell.Value = dataTypes[0];
          columnGrid.CurrentCell = columnGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
          columnGrid.CurrentCell.Selected = true;
          columnGrid.BeginEdit(true);
          return;
        }
      }      
      columnGrid.CurrentCell.Value = e.FormattedValue;
    }

    private void columnGrid_CellEnter(object sender, DataGridViewCellEventArgs e)
    {
      if (columnGrid.SelectedCells.Count == 1)
        columnGrid.BeginEdit(true);
    }

    private void columnGrid_CellClick(object sender, DataGridViewCellEventArgs e)
    {
      if (e.ColumnIndex == -1)
        columnGrid.EndEdit();
      else
        columnGrid.BeginEdit(true);
    }

    private void columnGrid_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
    {
      Column c = Columns[e.Row.Index];
      if (c.OldColumn != null)
        tableNode.Table.Columns.Delete(c);
    }

    private void columnGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
    {
      if (e.ColumnIndex != 2) return;
      DataGridViewCheckBoxCell cell = columnGrid[e.ColumnIndex, e.RowIndex] as DataGridViewCheckBoxCell;
      Column c = Columns[e.RowIndex];
      c.AllowNull = (bool)cell.EditingCellFormattedValue;
    }

    private void columnProperties_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
    {
      if (e.ChangedItem.PropertyDescriptor.Name == "PrimaryKey")
        columnGrid.Refresh();
    }

    private void columnGrid_CellLeave(object sender, DataGridViewCellEventArgs e)
    {
      int index = columnGrid.CurrentRow.Index;
      if (index >= 0 && index < tableNode.Table.Columns.Count)
        columnProperties.SelectedObject = tableNode.Table.Columns[index];
    }   
    
    public void EndGridEditMode()
    {
      if (columnGrid.IsCurrentCellDirty)
      {
        DataGridViewCell c = columnGrid.CurrentCell;
        DataGridViewCellValidatingEventArgs args = 
          (DataGridViewCellValidatingEventArgs) typeof(DataGridViewCellValidatingEventArgs).GetConstructor(
          BindingFlags.NonPublic | BindingFlags.Instance,
          null, new Type[] { typeof(int), typeof(int), typeof(object) }, null).Invoke(new object[] { c.ColumnIndex, c.RowIndex, c.Value });
        columnGrid_CellValidating(columnGrid, args);
        if (!args.Cancel)
        {
          columnGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
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

  }
}
