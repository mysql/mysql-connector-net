using System;
using MySql.Data.VisualStudio.DbObjects;
using System.Windows.Forms;
namespace MySql.Data.VisualStudio.Editors
{
  partial class ForeignKeyDialog
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.components = new System.ComponentModel.Container();
      this.fkList = new System.Windows.Forms.ListBox();
      this.foreignKeyBindingSource = new System.Windows.Forms.BindingSource(this.components);
      this.label1 = new System.Windows.Forms.Label();
      this.addButton = new System.Windows.Forms.Button();
      this.deleteButton = new System.Windows.Forms.Button();
      this.label2 = new System.Windows.Forms.Label();
      this.label3 = new System.Windows.Forms.Label();
      this.columnGrid = new System.Windows.Forms.DataGridView();
      this.colGridColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
      this.fkGridColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
      this.fkColumnsBindingSource = new System.Windows.Forms.BindingSource(this.components);
      this.closeButton = new System.Windows.Forms.Button();
      this.label5 = new System.Windows.Forms.Label();
      this.fkName = new System.Windows.Forms.TextBox();
      this.label4 = new System.Windows.Forms.Label();
      this.dataGridViewComboBoxColumn1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
      this.dataGridViewComboBoxColumn2 = new System.Windows.Forms.DataGridViewComboBoxColumn();
      this.label6 = new System.Windows.Forms.Label();
      this.label7 = new System.Windows.Forms.Label();
      this.matchType = new MySql.Data.VisualStudio.Editors.MyComboBox();
      this.deleteAction = new MySql.Data.VisualStudio.Editors.MyComboBox();
      this.updateAction = new MySql.Data.VisualStudio.Editors.MyComboBox();
      this.refTable = new MySql.Data.VisualStudio.Editors.MyComboBox();
      ((System.ComponentModel.ISupportInitialize)(this.foreignKeyBindingSource)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.columnGrid)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.fkColumnsBindingSource)).BeginInit();
      this.SuspendLayout();
      // 
      // fkList
      // 
      this.fkList.FormattingEnabled = true;
      this.fkList.ItemHeight = 15;
      this.fkList.Location = new System.Drawing.Point(14, 36);
      this.fkList.Name = "fkList";
      this.fkList.Size = new System.Drawing.Size(181, 304);
      this.fkList.TabIndex = 0;
      // 
      // foreignKeyBindingSource
      // 
      this.foreignKeyBindingSource.AllowNew = false;
      this.foreignKeyBindingSource.CurrentChanged += new System.EventHandler(this.foreignKeyBindingSource_CurrentChanged);
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(10, 9);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(122, 15);
      this.label1.TabIndex = 1;
      this.label1.Text = "Selected Relationship:";
      // 
      // addButton
      // 
      this.addButton.Location = new System.Drawing.Point(14, 347);
      this.addButton.Name = "addButton";
      this.addButton.Size = new System.Drawing.Size(87, 27);
      this.addButton.TabIndex = 2;
      this.addButton.Text = "Add";
      this.addButton.UseVisualStyleBackColor = true;
      this.addButton.Click += new System.EventHandler(this.addButton_Click);
      // 
      // deleteButton
      // 
      this.deleteButton.Location = new System.Drawing.Point(108, 347);
      this.deleteButton.Name = "deleteButton";
      this.deleteButton.Size = new System.Drawing.Size(87, 27);
      this.deleteButton.TabIndex = 3;
      this.deleteButton.Text = "Delete";
      this.deleteButton.UseVisualStyleBackColor = true;
      this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(235, 75);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(101, 15);
      this.label2.TabIndex = 5;
      this.label2.Text = "Referenced Table:";
      this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(288, 104);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(48, 15);
      this.label3.TabIndex = 6;
      this.label3.Text = "Update:";
      this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // columnGrid
      // 
      this.columnGrid.AllowUserToResizeRows = false;
      this.columnGrid.AutoGenerateColumns = false;
      this.columnGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
      this.columnGrid.BackgroundColor = System.Drawing.SystemColors.Window;
      this.columnGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
      this.columnGrid.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
      this.columnGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.columnGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colGridColumn,
            this.fkGridColumn});
      this.columnGrid.DataSource = this.fkColumnsBindingSource;
      this.columnGrid.GridColor = System.Drawing.SystemColors.ControlLight;
      this.columnGrid.Location = new System.Drawing.Point(218, 163);
      this.columnGrid.MultiSelect = false;
      this.columnGrid.Name = "columnGrid";
      this.columnGrid.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
      this.columnGrid.RowHeadersVisible = false;
      this.columnGrid.ShowEditingIcon = false;
      this.columnGrid.Size = new System.Drawing.Size(425, 177);
      this.columnGrid.TabIndex = 10;
      this.columnGrid.VirtualMode = true;
      this.columnGrid.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.columnGrid_CellValidating);
      this.columnGrid.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.columnGrid_CellValueNeeded);
      this.columnGrid.CellValuePushed += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.columnGrid_CellValuePushed);
      this.columnGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.columnGrid_DataError);
      this.columnGrid.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.columnGrid_EditingControlShowing);
      this.columnGrid.RowValidating += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.columnGrid_RowValidating);
      // 
      // colGridColumn
      // 
      this.colGridColumn.DataPropertyName = "Column";
      this.colGridColumn.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
      this.colGridColumn.DisplayStyleForCurrentCellOnly = true;
      this.colGridColumn.HeaderText = "Column";
      this.colGridColumn.Name = "colGridColumn";
      // 
      // fkGridColumn
      // 
      this.fkGridColumn.DataPropertyName = "ReferencedColumn";
      this.fkGridColumn.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
      this.fkGridColumn.DisplayStyleForCurrentCellOnly = true;
      this.fkGridColumn.HeaderText = "Foreign Column";
      this.fkGridColumn.Name = "fkGridColumn";
      // 
      // fkColumnsBindingSource
      // 
      this.fkColumnsBindingSource.AllowNew = true;
      this.fkColumnsBindingSource.DataSource = typeof(MySql.Data.VisualStudio.DbObjects.FKColumnPair);
      // 
      // closeButton
      // 
      this.closeButton.Location = new System.Drawing.Point(556, 347);
      this.closeButton.Name = "closeButton";
      this.closeButton.Size = new System.Drawing.Size(87, 27);
      this.closeButton.TabIndex = 12;
      this.closeButton.Text = "Close";
      this.closeButton.UseVisualStyleBackColor = true;
      this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
      // 
      // label5
      // 
      this.label5.AutoSize = true;
      this.label5.Location = new System.Drawing.Point(488, 104);
      this.label5.Name = "label5";
      this.label5.Size = new System.Drawing.Size(43, 15);
      this.label5.TabIndex = 13;
      this.label5.Text = "Delete:";
      this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // fkName
      // 
      this.fkName.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.foreignKeyBindingSource, "Name", true));
      this.fkName.Location = new System.Drawing.Point(342, 36);
      this.fkName.Name = "fkName";
      this.fkName.Size = new System.Drawing.Size(301, 23);
      this.fkName.TabIndex = 14;
      this.fkName.TextChanged += new System.EventHandler(this.fkName_TextChanged);
      // 
      // label4
      // 
      this.label4.AutoSize = true;
      this.label4.Location = new System.Drawing.Point(294, 44);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(42, 15);
      this.label4.TabIndex = 15;
      this.label4.Text = "Name:";
      // 
      // dataGridViewComboBoxColumn1
      // 
      this.dataGridViewComboBoxColumn1.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.Nothing;
      this.dataGridViewComboBoxColumn1.DisplayStyleForCurrentCellOnly = true;
      this.dataGridViewComboBoxColumn1.HeaderText = "Column";
      this.dataGridViewComboBoxColumn1.Name = "dataGridViewComboBoxColumn1";
      this.dataGridViewComboBoxColumn1.Width = 159;
      // 
      // dataGridViewComboBoxColumn2
      // 
      this.dataGridViewComboBoxColumn2.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
      this.dataGridViewComboBoxColumn2.DisplayStyleForCurrentCellOnly = true;
      this.dataGridViewComboBoxColumn2.HeaderText = "Foreign Column";
      this.dataGridViewComboBoxColumn2.Name = "dataGridViewComboBoxColumn2";
      this.dataGridViewComboBoxColumn2.Width = 159;
      // 
      // label6
      // 
      this.label6.AutoSize = true;
      this.label6.Location = new System.Drawing.Point(488, 134);
      this.label6.Name = "label6";
      this.label6.Size = new System.Drawing.Size(44, 15);
      this.label6.TabIndex = 17;
      this.label6.Text = "Match:";
      this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // label7
      // 
      this.label7.AutoSize = true;
      this.label7.Location = new System.Drawing.Point(215, 145);
      this.label7.Name = "label7";
      this.label7.Size = new System.Drawing.Size(55, 15);
      this.label7.TabIndex = 18;
      this.label7.Text = "Columns";
      // 
      // matchType
      // 
      this.matchType.DataBindings.Add(new System.Windows.Forms.Binding("SelectedItem", this.foreignKeyBindingSource, "Match", true));
      this.matchType.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
      this.matchType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.matchType.Items.AddRange(new object[] {
            MySql.Data.VisualStudio.DbObjects.MatchOption.Full,
            MySql.Data.VisualStudio.DbObjects.MatchOption.Partial,
            MySql.Data.VisualStudio.DbObjects.MatchOption.Simple});
      this.matchType.Location = new System.Drawing.Point(536, 125);
      this.matchType.MinimumSize = new System.Drawing.Size(4, 10);
      this.matchType.Name = "matchType";
      this.matchType.Size = new System.Drawing.Size(107, 24);
      this.matchType.TabIndex = 16;
      // 
      // deleteAction
      // 
      this.deleteAction.DataBindings.Add(new System.Windows.Forms.Binding("SelectedItem", this.foreignKeyBindingSource, "DeleteAction", true));
      this.deleteAction.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
      this.deleteAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.deleteAction.Items.AddRange(new object[] {
            MySql.Data.VisualStudio.DbObjects.ReferenceOption.NoAction,
            MySql.Data.VisualStudio.DbObjects.ReferenceOption.Cascade,
            MySql.Data.VisualStudio.DbObjects.ReferenceOption.Restrict,
            MySql.Data.VisualStudio.DbObjects.ReferenceOption.SetNull});
      this.deleteAction.Location = new System.Drawing.Point(536, 95);
      this.deleteAction.MinimumSize = new System.Drawing.Size(4, 10);
      this.deleteAction.Name = "deleteAction";
      this.deleteAction.Size = new System.Drawing.Size(107, 24);
      this.deleteAction.TabIndex = 9;
      // 
      // updateAction
      // 
      this.updateAction.DataBindings.Add(new System.Windows.Forms.Binding("SelectedItem", this.foreignKeyBindingSource, "UpdateAction", true));
      this.updateAction.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
      this.updateAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.updateAction.FormattingEnabled = true;
      this.updateAction.Items.AddRange(new object[] {
            MySql.Data.VisualStudio.DbObjects.ReferenceOption.NoAction,
            MySql.Data.VisualStudio.DbObjects.ReferenceOption.Cascade,
            MySql.Data.VisualStudio.DbObjects.ReferenceOption.Restrict,
            MySql.Data.VisualStudio.DbObjects.ReferenceOption.SetNull});
      this.updateAction.Location = new System.Drawing.Point(342, 95);
      this.updateAction.MinimumSize = new System.Drawing.Size(4, 10);
      this.updateAction.Name = "updateAction";
      this.updateAction.Size = new System.Drawing.Size(107, 24);
      this.updateAction.TabIndex = 8;
      // 
      // refTable
      // 
      this.refTable.DataBindings.Add(new System.Windows.Forms.Binding("SelectedItem", this.foreignKeyBindingSource, "ReferencedTable", true));
      this.refTable.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
      this.refTable.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.refTable.FormattingEnabled = true;
      this.refTable.Location = new System.Drawing.Point(342, 66);
      this.refTable.MinimumSize = new System.Drawing.Size(4, 10);
      this.refTable.Name = "refTable";
      this.refTable.Size = new System.Drawing.Size(301, 24);
      this.refTable.TabIndex = 4;
      this.refTable.SelectedIndexChanged += new System.EventHandler(this.refTable_SelectedIndexChanged);
      // 
      // ForeignKeyDialog
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(655, 386);
      this.Controls.Add(this.label7);
      this.Controls.Add(this.label6);
      this.Controls.Add(this.matchType);
      this.Controls.Add(this.label4);
      this.Controls.Add(this.fkName);
      this.Controls.Add(this.label5);
      this.Controls.Add(this.closeButton);
      this.Controls.Add(this.columnGrid);
      this.Controls.Add(this.deleteAction);
      this.Controls.Add(this.updateAction);
      this.Controls.Add(this.label3);
      this.Controls.Add(this.label2);
      this.Controls.Add(this.refTable);
      this.Controls.Add(this.deleteButton);
      this.Controls.Add(this.addButton);
      this.Controls.Add(this.label1);
      this.Controls.Add(this.fkList);
      this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "ForeignKeyDialog";
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Foreign Key Relationships";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ForeignKeyDialog_FormClosing);
      ((System.ComponentModel.ISupportInitialize)(this.foreignKeyBindingSource)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.columnGrid)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.fkColumnsBindingSource)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ListBox fkList;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Button addButton;
    private System.Windows.Forms.Button deleteButton;
    private MyComboBox refTable;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private MyComboBox updateAction;
    private MyComboBox deleteAction;
    private System.Windows.Forms.DataGridView columnGrid;
    private System.Windows.Forms.Button closeButton;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.TextBox fkName;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn1;
    private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn2;
    private System.Windows.Forms.Label label6;
    private MyComboBox matchType;
    private System.Windows.Forms.Label label7;
    private System.Windows.Forms.BindingSource foreignKeyBindingSource;
    private BindingSource fkColumnsBindingSource;
    private DataGridViewComboBoxColumn colGridColumn;
    private DataGridViewComboBoxColumn fkGridColumn;
  }
}