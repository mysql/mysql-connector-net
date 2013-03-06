namespace MySql.Data.VisualStudio.Editors
{
  partial class TableIndexDialog
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
      this.label1 = new System.Windows.Forms.Label();
      this.indexList = new System.Windows.Forms.ListBox();
      this.addButton = new System.Windows.Forms.Button();
      this.deleteButton = new System.Windows.Forms.Button();
      this.closeButton = new System.Windows.Forms.Button();
      this.indexProps = new System.Windows.Forms.PropertyGrid();
      this.SuspendLayout();
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(14, 10);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(208, 15);
      this.label1.TabIndex = 0;
      this.label1.Text = "Selected Primary/Unique Key or Index:";
      // 
      // indexList
      // 
      this.indexList.FormattingEnabled = true;
      this.indexList.ItemHeight = 15;
      this.indexList.Location = new System.Drawing.Point(14, 36);
      this.indexList.Name = "indexList";
      this.indexList.Size = new System.Drawing.Size(205, 304);
      this.indexList.TabIndex = 0;
      this.indexList.SelectedIndexChanged += new System.EventHandler(this.indexList_SelectedIndexChanged);
      // 
      // addButton
      // 
      this.addButton.Location = new System.Drawing.Point(14, 348);
      this.addButton.Name = "addButton";
      this.addButton.Size = new System.Drawing.Size(99, 27);
      this.addButton.TabIndex = 5;
      this.addButton.Text = "Add";
      this.addButton.UseVisualStyleBackColor = true;
      this.addButton.Click += new System.EventHandler(this.addButton_Click);
      // 
      // deleteButton
      // 
      this.deleteButton.Location = new System.Drawing.Point(120, 348);
      this.deleteButton.Name = "deleteButton";
      this.deleteButton.Size = new System.Drawing.Size(99, 27);
      this.deleteButton.TabIndex = 6;
      this.deleteButton.Text = "Delete";
      this.deleteButton.UseVisualStyleBackColor = true;
      this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
      // 
      // closeButton
      // 
      this.closeButton.Location = new System.Drawing.Point(465, 348);
      this.closeButton.Name = "closeButton";
      this.closeButton.Size = new System.Drawing.Size(99, 27);
      this.closeButton.TabIndex = 7;
      this.closeButton.Text = "Close";
      this.closeButton.UseVisualStyleBackColor = true;
      this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
      // 
      // indexProps
      // 
      this.indexProps.Location = new System.Drawing.Point(247, 36);
      this.indexProps.Name = "indexProps";
      this.indexProps.Size = new System.Drawing.Size(317, 304);
      this.indexProps.TabIndex = 10;
      this.indexProps.ToolbarVisible = false;
      this.indexProps.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.indexProps_PropertyValueChanged);
      // 
      // TableIndexDialog
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(577, 387);
      this.Controls.Add(this.indexProps);
      this.Controls.Add(this.closeButton);
      this.Controls.Add(this.deleteButton);
      this.Controls.Add(this.addButton);
      this.Controls.Add(this.indexList);
      this.Controls.Add(this.label1);
      this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "TableIndexDialog";
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Indexes/Keys";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.ListBox indexList;
    private System.Windows.Forms.Button addButton;
    private System.Windows.Forms.Button deleteButton;
    private System.Windows.Forms.Button closeButton;
    private System.Windows.Forms.PropertyGrid indexProps;
  }
}