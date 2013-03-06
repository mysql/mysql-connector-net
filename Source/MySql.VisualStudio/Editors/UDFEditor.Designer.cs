namespace MySql.Data.VisualStudio.Editors
{
  partial class UDFEditor
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
      this.okButton = new System.Windows.Forms.Button();
      this.cancelButton = new System.Windows.Forms.Button();
      this.label1 = new System.Windows.Forms.Label();
      this.functionName = new System.Windows.Forms.TextBox();
      this.label2 = new System.Windows.Forms.Label();
      this.returnType = new System.Windows.Forms.ComboBox();
      this.label3 = new System.Windows.Forms.Label();
      this.libraryName = new System.Windows.Forms.TextBox();
      this.aggregate = new System.Windows.Forms.CheckBox();
      this.SuspendLayout();
      // 
      // okButton
      // 
      this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.okButton.Enabled = false;
      this.okButton.Location = new System.Drawing.Point(178, 116);
      this.okButton.Name = "okButton";
      this.okButton.Size = new System.Drawing.Size(75, 23);
      this.okButton.TabIndex = 4;
      this.okButton.Text = "OK";
      this.okButton.UseVisualStyleBackColor = true;
      // 
      // cancelButton
      // 
      this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.cancelButton.Location = new System.Drawing.Point(259, 116);
      this.cancelButton.Name = "cancelButton";
      this.cancelButton.Size = new System.Drawing.Size(75, 23);
      this.cancelButton.TabIndex = 5;
      this.cancelButton.Text = "Cancel";
      this.cancelButton.UseVisualStyleBackColor = true;
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(17, 15);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(38, 13);
      this.label1.TabIndex = 2;
      this.label1.Text = "Name:";
      this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // functionName
      // 
      this.functionName.Location = new System.Drawing.Point(61, 12);
      this.functionName.Name = "functionName";
      this.functionName.Size = new System.Drawing.Size(273, 20);
      this.functionName.TabIndex = 0;
      this.functionName.TextChanged += new System.EventHandler(this.functionName_TextChanged);
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(9, 43);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(47, 13);
      this.label2.TabIndex = 4;
      this.label2.Text = "Returns:";
      this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // returnType
      // 
      this.returnType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.returnType.FormattingEnabled = true;
      this.returnType.Items.AddRange(new object[] {
            "String",
            "Integer",
            "Real",
            "Decimal"});
      this.returnType.Location = new System.Drawing.Point(62, 38);
      this.returnType.Name = "returnType";
      this.returnType.Size = new System.Drawing.Size(123, 21);
      this.returnType.TabIndex = 1;
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(14, 68);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(41, 13);
      this.label3.TabIndex = 6;
      this.label3.Text = "Library:";
      this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // libraryName
      // 
      this.libraryName.Location = new System.Drawing.Point(62, 65);
      this.libraryName.Name = "libraryName";
      this.libraryName.Size = new System.Drawing.Size(272, 20);
      this.libraryName.TabIndex = 3;
      this.libraryName.TextChanged += new System.EventHandler(this.libraryName_TextChanged);
      // 
      // aggregate
      // 
      this.aggregate.AutoSize = true;
      this.aggregate.Location = new System.Drawing.Point(220, 42);
      this.aggregate.Name = "aggregate";
      this.aggregate.Size = new System.Drawing.Size(75, 17);
      this.aggregate.TabIndex = 2;
      this.aggregate.Text = "Aggregate";
      this.aggregate.UseVisualStyleBackColor = true;
      // 
      // UDFEditor
      // 
      this.AcceptButton = this.okButton;
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = this.cancelButton;
      this.ClientSize = new System.Drawing.Size(342, 147);
      this.ControlBox = false;
      this.Controls.Add(this.aggregate);
      this.Controls.Add(this.libraryName);
      this.Controls.Add(this.label3);
      this.Controls.Add(this.returnType);
      this.Controls.Add(this.label2);
      this.Controls.Add(this.functionName);
      this.Controls.Add(this.label1);
      this.Controls.Add(this.cancelButton);
      this.Controls.Add(this.okButton);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "UDFEditor";
      this.Padding = new System.Windows.Forms.Padding(5);
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Create New UDF";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Button okButton;
    private System.Windows.Forms.Button cancelButton;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox functionName;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.ComboBox returnType;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.TextBox libraryName;
    private System.Windows.Forms.CheckBox aggregate;
  }
}