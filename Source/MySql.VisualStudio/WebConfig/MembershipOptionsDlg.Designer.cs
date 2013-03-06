namespace MySql.Data.VisualStudio.WebConfig
{
  partial class MembershipOptionsDlg
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
      this.label6 = new System.Windows.Forms.Label();
      this.passwordAttemptWindow = new System.Windows.Forms.NumericUpDown();
      this.passwordRegex = new System.Windows.Forms.TextBox();
      this.label7 = new System.Windows.Forms.Label();
      this.label8 = new System.Windows.Forms.Label();
      this.minPassLength = new System.Windows.Forms.NumericUpDown();
      this.requireUniqueEmail = new System.Windows.Forms.CheckBox();
      this.requireQA = new System.Windows.Forms.CheckBox();
      this.enablePasswordRetrieval = new System.Windows.Forms.CheckBox();
      this.enablePasswordReset = new System.Windows.Forms.CheckBox();
      this.label9 = new System.Windows.Forms.Label();
      this.minRequiredNonAlpha = new System.Windows.Forms.NumericUpDown();
      this.label10 = new System.Windows.Forms.Label();
      this.maxInvalidPassAttempts = new System.Windows.Forms.NumericUpDown();
      this.cancelButton = new System.Windows.Forms.Button();
      this.okButton = new System.Windows.Forms.Button();
      this.groupBox1 = new System.Windows.Forms.GroupBox();
      this.groupBox2 = new System.Windows.Forms.GroupBox();
      this.groupBox3 = new System.Windows.Forms.GroupBox();
      this.label1 = new System.Windows.Forms.Label();
      this.passwordFormat = new System.Windows.Forms.ComboBox();
      ((System.ComponentModel.ISupportInitialize)(this.passwordAttemptWindow)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.minPassLength)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.minRequiredNonAlpha)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.maxInvalidPassAttempts)).BeginInit();
      this.groupBox1.SuspendLayout();
      this.groupBox2.SuspendLayout();
      this.groupBox3.SuspendLayout();
      this.SuspendLayout();
      // 
      // label6
      // 
      this.label6.AutoSize = true;
      this.label6.Location = new System.Drawing.Point(20, 54);
      this.label6.Name = "label6";
      this.label6.Size = new System.Drawing.Size(134, 13);
      this.label6.TabIndex = 28;
      this.label6.Text = "Password Attempt Window";
      // 
      // passwordAttemptWindow
      // 
      this.passwordAttemptWindow.Location = new System.Drawing.Point(229, 48);
      this.passwordAttemptWindow.Name = "passwordAttemptWindow";
      this.passwordAttemptWindow.Size = new System.Drawing.Size(40, 20);
      this.passwordAttemptWindow.TabIndex = 27;
      // 
      // passwordRegex
      // 
      this.passwordRegex.Location = new System.Drawing.Point(20, 102);
      this.passwordRegex.Name = "passwordRegex";
      this.passwordRegex.Size = new System.Drawing.Size(254, 20);
      this.passwordRegex.TabIndex = 26;
      // 
      // label7
      // 
      this.label7.AutoSize = true;
      this.label7.Location = new System.Drawing.Point(20, 86);
      this.label7.Name = "label7";
      this.label7.Size = new System.Drawing.Size(182, 13);
      this.label7.TabIndex = 25;
      this.label7.Text = "Password strength regular expression";
      // 
      // label8
      // 
      this.label8.AutoSize = true;
      this.label8.Location = new System.Drawing.Point(20, 32);
      this.label8.Name = "label8";
      this.label8.Size = new System.Drawing.Size(179, 13);
      this.label8.TabIndex = 24;
      this.label8.Text = "Minimum Required Password Length";
      // 
      // minPassLength
      // 
      this.minPassLength.Location = new System.Drawing.Point(234, 29);
      this.minPassLength.Name = "minPassLength";
      this.minPassLength.Size = new System.Drawing.Size(40, 20);
      this.minPassLength.TabIndex = 23;
      // 
      // requireUniqueEmail
      // 
      this.requireUniqueEmail.AutoSize = true;
      this.requireUniqueEmail.Location = new System.Drawing.Point(15, 53);
      this.requireUniqueEmail.Name = "requireUniqueEmail";
      this.requireUniqueEmail.Size = new System.Drawing.Size(133, 17);
      this.requireUniqueEmail.TabIndex = 22;
      this.requireUniqueEmail.Text = "Requires Unique Email";
      this.requireUniqueEmail.UseVisualStyleBackColor = true;
      // 
      // requireQA
      // 
      this.requireQA.AutoSize = true;
      this.requireQA.Location = new System.Drawing.Point(15, 30);
      this.requireQA.Name = "requireQA";
      this.requireQA.Size = new System.Drawing.Size(148, 17);
      this.requireQA.TabIndex = 21;
      this.requireQA.Text = "Require Question/Answer";
      this.requireQA.UseVisualStyleBackColor = true;
      // 
      // enablePasswordRetrieval
      // 
      this.enablePasswordRetrieval.AutoSize = true;
      this.enablePasswordRetrieval.Location = new System.Drawing.Point(169, 53);
      this.enablePasswordRetrieval.Name = "enablePasswordRetrieval";
      this.enablePasswordRetrieval.Size = new System.Drawing.Size(153, 17);
      this.enablePasswordRetrieval.TabIndex = 20;
      this.enablePasswordRetrieval.Text = "Enable Password Retrieval";
      this.enablePasswordRetrieval.UseVisualStyleBackColor = true;
      // 
      // enablePasswordReset
      // 
      this.enablePasswordReset.AutoSize = true;
      this.enablePasswordReset.Location = new System.Drawing.Point(169, 30);
      this.enablePasswordReset.Name = "enablePasswordReset";
      this.enablePasswordReset.Size = new System.Drawing.Size(139, 17);
      this.enablePasswordReset.TabIndex = 19;
      this.enablePasswordReset.Text = "Enable Password Reset";
      this.enablePasswordReset.UseVisualStyleBackColor = true;
      // 
      // label9
      // 
      this.label9.AutoSize = true;
      this.label9.Location = new System.Drawing.Point(20, 60);
      this.label9.Name = "label9";
      this.label9.Size = new System.Drawing.Size(213, 13);
      this.label9.TabIndex = 18;
      this.label9.Text = "Min Required Non-alphanumeric Characters";
      // 
      // minRequiredNonAlpha
      // 
      this.minRequiredNonAlpha.Location = new System.Drawing.Point(234, 58);
      this.minRequiredNonAlpha.Name = "minRequiredNonAlpha";
      this.minRequiredNonAlpha.Size = new System.Drawing.Size(40, 20);
      this.minRequiredNonAlpha.TabIndex = 17;
      // 
      // label10
      // 
      this.label10.AutoSize = true;
      this.label10.Location = new System.Drawing.Point(20, 26);
      this.label10.Name = "label10";
      this.label10.Size = new System.Drawing.Size(178, 13);
      this.label10.TabIndex = 16;
      this.label10.Text = "Maximum Invalid Password Attempts";
      // 
      // maxInvalidPassAttempts
      // 
      this.maxInvalidPassAttempts.Location = new System.Drawing.Point(231, 19);
      this.maxInvalidPassAttempts.Name = "maxInvalidPassAttempts";
      this.maxInvalidPassAttempts.Size = new System.Drawing.Size(40, 20);
      this.maxInvalidPassAttempts.TabIndex = 15;
      // 
      // cancelButton
      // 
      this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.cancelButton.Location = new System.Drawing.Point(259, 418);
      this.cancelButton.Name = "cancelButton";
      this.cancelButton.Size = new System.Drawing.Size(81, 25);
      this.cancelButton.TabIndex = 31;
      this.cancelButton.Text = "Cancel";
      this.cancelButton.UseVisualStyleBackColor = true;
      // 
      // okButton
      // 
      this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.okButton.Location = new System.Drawing.Point(172, 418);
      this.okButton.Name = "okButton";
      this.okButton.Size = new System.Drawing.Size(81, 25);
      this.okButton.TabIndex = 30;
      this.okButton.Text = "OK";
      this.okButton.UseVisualStyleBackColor = true;
      // 
      // groupBox1
      // 
      this.groupBox1.Controls.Add(this.passwordFormat);
      this.groupBox1.Controls.Add(this.label1);
      this.groupBox1.Controls.Add(this.minPassLength);
      this.groupBox1.Controls.Add(this.minRequiredNonAlpha);
      this.groupBox1.Controls.Add(this.label9);
      this.groupBox1.Controls.Add(this.label8);
      this.groupBox1.Controls.Add(this.label7);
      this.groupBox1.Controls.Add(this.passwordRegex);
      this.groupBox1.Location = new System.Drawing.Point(12, 12);
      this.groupBox1.Name = "groupBox1";
      this.groupBox1.Size = new System.Drawing.Size(328, 194);
      this.groupBox1.TabIndex = 32;
      this.groupBox1.TabStop = false;
      this.groupBox1.Text = "Password Options";
      // 
      // groupBox2
      // 
      this.groupBox2.Controls.Add(this.requireQA);
      this.groupBox2.Controls.Add(this.requireUniqueEmail);
      this.groupBox2.Controls.Add(this.enablePasswordReset);
      this.groupBox2.Controls.Add(this.enablePasswordRetrieval);
      this.groupBox2.Location = new System.Drawing.Point(12, 219);
      this.groupBox2.Name = "groupBox2";
      this.groupBox2.Size = new System.Drawing.Size(328, 86);
      this.groupBox2.TabIndex = 33;
      this.groupBox2.TabStop = false;
      this.groupBox2.Text = "User Options";
      // 
      // groupBox3
      // 
      this.groupBox3.Controls.Add(this.passwordAttemptWindow);
      this.groupBox3.Controls.Add(this.maxInvalidPassAttempts);
      this.groupBox3.Controls.Add(this.label10);
      this.groupBox3.Controls.Add(this.label6);
      this.groupBox3.Location = new System.Drawing.Point(12, 318);
      this.groupBox3.Name = "groupBox3";
      this.groupBox3.Size = new System.Drawing.Size(328, 83);
      this.groupBox3.TabIndex = 34;
      this.groupBox3.TabStop = false;
      this.groupBox3.Text = "Access Options";
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(20, 134);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(88, 13);
      this.label1.TabIndex = 27;
      this.label1.Text = "Password Format";
      // 
      // passwordFormat
      // 
      this.passwordFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.passwordFormat.FormattingEnabled = true;
      this.passwordFormat.Location = new System.Drawing.Point(20, 150);
      this.passwordFormat.Name = "passwordFormat";
      this.passwordFormat.Size = new System.Drawing.Size(254, 21);
      this.passwordFormat.TabIndex = 28;
      // 
      // MembershipOptionsDlg
      // 
      this.AcceptButton = this.okButton;
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = this.cancelButton;
      this.ClientSize = new System.Drawing.Size(359, 458);
      this.Controls.Add(this.groupBox3);
      this.Controls.Add(this.groupBox2);
      this.Controls.Add(this.groupBox1);
      this.Controls.Add(this.cancelButton);
      this.Controls.Add(this.okButton);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "MembershipOptionsDlg";
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Membership Options";
      ((System.ComponentModel.ISupportInitialize)(this.passwordAttemptWindow)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.minPassLength)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.minRequiredNonAlpha)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.maxInvalidPassAttempts)).EndInit();
      this.groupBox1.ResumeLayout(false);
      this.groupBox1.PerformLayout();
      this.groupBox2.ResumeLayout(false);
      this.groupBox2.PerformLayout();
      this.groupBox3.ResumeLayout(false);
      this.groupBox3.PerformLayout();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Label label6;
    private System.Windows.Forms.NumericUpDown passwordAttemptWindow;
    private System.Windows.Forms.TextBox passwordRegex;
    private System.Windows.Forms.Label label7;
    private System.Windows.Forms.Label label8;
    private System.Windows.Forms.NumericUpDown minPassLength;
    private System.Windows.Forms.CheckBox requireUniqueEmail;
    private System.Windows.Forms.CheckBox requireQA;
    private System.Windows.Forms.CheckBox enablePasswordRetrieval;
    private System.Windows.Forms.CheckBox enablePasswordReset;
    private System.Windows.Forms.Label label9;
    private System.Windows.Forms.NumericUpDown minRequiredNonAlpha;
    private System.Windows.Forms.Label label10;
    private System.Windows.Forms.NumericUpDown maxInvalidPassAttempts;
    private System.Windows.Forms.Button cancelButton;
    private System.Windows.Forms.Button okButton;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.GroupBox groupBox2;
    private System.Windows.Forms.GroupBox groupBox3;
    private System.Windows.Forms.ComboBox passwordFormat;
    private System.Windows.Forms.Label label1;
  }
}