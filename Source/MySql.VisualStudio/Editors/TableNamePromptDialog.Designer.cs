namespace MySql.Data.VisualStudio.Editors
{
  partial class TableNamePromptDialog
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
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
      this.tableName = new System.Windows.Forms.TextBox();
      this.Cancel = new System.Windows.Forms.Button();
      this.Ok = new System.Windows.Forms.Button();
      this.SuspendLayout();
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(12, 9);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(132, 13);
      this.label1.TabIndex = 0;
      this.label1.Text = "&Enter a name for the table:";
      // 
      // tableName
      // 
      this.tableName.Location = new System.Drawing.Point(15, 30);
      this.tableName.Name = "tableName";
      this.tableName.Size = new System.Drawing.Size(354, 20);
      this.tableName.TabIndex = 1;
      // 
      // Cancel
      // 
      this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.Cancel.Location = new System.Drawing.Point(294, 59);
      this.Cancel.Name = "Cancel";
      this.Cancel.Size = new System.Drawing.Size(75, 23);
      this.Cancel.TabIndex = 2;
      this.Cancel.Text = "Cancel";
      this.Cancel.UseVisualStyleBackColor = true;
      // 
      // Ok
      // 
      this.Ok.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.Ok.Location = new System.Drawing.Point(213, 59);
      this.Ok.Name = "Ok";
      this.Ok.Size = new System.Drawing.Size(75, 23);
      this.Ok.TabIndex = 3;
      this.Ok.Text = "OK";
      this.Ok.UseVisualStyleBackColor = true;
      // 
      // TableNamePromptDialog
      // 
      this.AcceptButton = this.Ok;
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = this.Cancel;
      this.ClientSize = new System.Drawing.Size(381, 93);
      this.Controls.Add(this.Ok);
      this.Controls.Add(this.Cancel);
      this.Controls.Add(this.tableName);
      this.Controls.Add(this.label1);
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "TableNamePromptDialog";
      this.Padding = new System.Windows.Forms.Padding(9);
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Choose Name";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox tableName;
    private System.Windows.Forms.Button Cancel;
    private System.Windows.Forms.Button Ok;

  }
}
