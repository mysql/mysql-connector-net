using MySql.Data.VisualStudio.DbObjects;
namespace MySql.Data.VisualStudio.Editors
{
  partial class IndexColumnEditorDialog
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
      this.label1 = new System.Windows.Forms.Label();
      this.indexGrid = new System.Windows.Forms.DataGridView();
      this.cancelButton = new System.Windows.Forms.Button();
      this.okButton = new System.Windows.Forms.Button();
      this.columnName = new System.Windows.Forms.DataGridViewComboBoxColumn();
      this.sortOrder = new System.Windows.Forms.DataGridViewComboBoxColumn();
      this.indexColumnBindingSource = new System.Windows.Forms.BindingSource(this.components);
      ((System.ComponentModel.ISupportInitialize)(this.indexGrid)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.indexColumnBindingSource)).BeginInit();
      this.SuspendLayout();
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(12, 9);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(265, 15);
      this.label1.TabIndex = 0;
      this.label1.Text = "Specify the columns and sort order for this Index:";
      // 
      // indexGrid
      // 
      this.indexGrid.AllowUserToResizeColumns = false;
      this.indexGrid.AllowUserToResizeRows = false;
      this.indexGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.indexGrid.AutoGenerateColumns = false;
      this.indexGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
      this.indexGrid.BackgroundColor = System.Drawing.SystemColors.Window;
      this.indexGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
      this.indexGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.indexGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.columnName,
            this.sortOrder});
      this.indexGrid.DataSource = this.indexColumnBindingSource;
      this.indexGrid.GridColor = System.Drawing.SystemColors.ControlLight;
      this.indexGrid.Location = new System.Drawing.Point(12, 38);
      this.indexGrid.Name = "indexGrid";
      this.indexGrid.RowHeadersVisible = false;
      this.indexGrid.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.indexGrid.Size = new System.Drawing.Size(515, 260);
      this.indexGrid.TabIndex = 1;
      this.indexGrid.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.indexGrid_CellValidating);
      this.indexGrid.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.indexGrid_EditingControlShowing);
      // 
      // cancelButton
      // 
      this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.cancelButton.Location = new System.Drawing.Point(440, 304);
      this.cancelButton.Name = "cancelButton";
      this.cancelButton.Size = new System.Drawing.Size(87, 28);
      this.cancelButton.TabIndex = 2;
      this.cancelButton.Text = "Cancel";
      this.cancelButton.UseVisualStyleBackColor = true;
      // 
      // okButton
      // 
      this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.okButton.Location = new System.Drawing.Point(347, 304);
      this.okButton.Name = "okButton";
      this.okButton.Size = new System.Drawing.Size(87, 28);
      this.okButton.TabIndex = 3;
      this.okButton.Text = "OK";
      this.okButton.UseVisualStyleBackColor = true;
      this.okButton.Click += new System.EventHandler(this.okButton_Click);
      // 
      // columnName
      // 
      this.columnName.DataPropertyName = "ColumnName";
      this.columnName.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
      this.columnName.DisplayStyleForCurrentCellOnly = true;
      this.columnName.HeaderText = "Column Name";
      this.columnName.Name = "columnName";
      // 
      // sortOrder
      // 
      this.sortOrder.DataPropertyName = "SortOrder";
      this.sortOrder.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
      this.sortOrder.DisplayStyleForCurrentCellOnly = true;
      this.sortOrder.HeaderText = "Sort Order";
      this.sortOrder.Name = "sortOrder";
      // 
      // indexColumnBindingSource
      // 
      this.indexColumnBindingSource.DataSource = typeof(MySql.Data.VisualStudio.Editors.IndexColumnGridRow);
      // 
      // IndexColumnEditorDialog
      // 
      this.AcceptButton = this.okButton;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = this.cancelButton;
      this.ClientSize = new System.Drawing.Size(539, 344);
      this.Controls.Add(this.okButton);
      this.Controls.Add(this.cancelButton);
      this.Controls.Add(this.indexGrid);
      this.Controls.Add(this.label1);
      this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "IndexColumnEditorDialog";
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Index Columns";
      ((System.ComponentModel.ISupportInitialize)(this.indexGrid)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.indexColumnBindingSource)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.DataGridView indexGrid;
    private System.Windows.Forms.Button cancelButton;
    private System.Windows.Forms.Button okButton;
    private System.Windows.Forms.BindingSource indexColumnBindingSource;
    private System.Windows.Forms.DataGridViewComboBoxColumn columnName;
    private System.Windows.Forms.DataGridViewComboBoxColumn sortOrder;
  }
}