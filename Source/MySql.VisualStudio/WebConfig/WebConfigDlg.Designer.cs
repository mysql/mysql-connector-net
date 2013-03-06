namespace MySql.Data.VisualStudio.WebConfig
{
  partial class WebConfigDlg
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
      this.connectionString = new System.Windows.Forms.TextBox();
      this.label2 = new System.Windows.Forms.Label();
      this.writeExToLog = new System.Windows.Forms.CheckBox();
      this.appName = new System.Windows.Forms.TextBox();
      this.label1 = new System.Windows.Forms.Label();
      this.autogenSchema = new System.Windows.Forms.CheckBox();
      this.appDescription = new System.Windows.Forms.TextBox();
      this.label3 = new System.Windows.Forms.Label();
      this.editConnString = new System.Windows.Forms.Button();
      this.cancelButton = new System.Windows.Forms.Button();
      this.nextButton = new System.Windows.Forms.Button();
      this.configPanel = new System.Windows.Forms.Panel();
      this.controlPanel = new System.Windows.Forms.Panel();
      this.enableExpCallback = new System.Windows.Forms.CheckBox();
      this.advancedBtn = new System.Windows.Forms.Button();
      this.useProvider = new System.Windows.Forms.CheckBox();
      this.pageLabel = new System.Windows.Forms.Label();
      this.pictureBox1 = new System.Windows.Forms.PictureBox();
      this.pageDesc = new System.Windows.Forms.Label();
      this.backButton = new System.Windows.Forms.Button();
      this.configPanel.SuspendLayout();
      this.controlPanel.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
      this.SuspendLayout();
      // 
      // connectionString
      // 
      this.connectionString.Location = new System.Drawing.Point(114, 63);
      this.connectionString.Multiline = true;
      this.connectionString.Name = "connectionString";
      this.connectionString.Size = new System.Drawing.Size(276, 40);
      this.connectionString.TabIndex = 2;
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(4, 65);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(94, 13);
      this.label2.TabIndex = 3;
      this.label2.Text = "Connection String:";
      // 
      // writeExToLog
      // 
      this.writeExToLog.AutoSize = true;
      this.writeExToLog.Location = new System.Drawing.Point(114, 134);
      this.writeExToLog.Name = "writeExToLog";
      this.writeExToLog.Size = new System.Drawing.Size(164, 17);
      this.writeExToLog.TabIndex = 5;
      this.writeExToLog.Text = "Write exceptions to event log";
      this.writeExToLog.UseVisualStyleBackColor = true;
      // 
      // appName
      // 
      this.appName.Location = new System.Drawing.Point(114, 9);
      this.appName.Name = "appName";
      this.appName.Size = new System.Drawing.Size(363, 20);
      this.appName.TabIndex = 0;
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(60, 12);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(38, 13);
      this.label1.TabIndex = 0;
      this.label1.Text = "Name:";
      // 
      // autogenSchema
      // 
      this.autogenSchema.AutoSize = true;
      this.autogenSchema.Location = new System.Drawing.Point(114, 111);
      this.autogenSchema.Name = "autogenSchema";
      this.autogenSchema.Size = new System.Drawing.Size(132, 17);
      this.autogenSchema.TabIndex = 4;
      this.autogenSchema.Text = "Autogenerate Schema";
      this.autogenSchema.UseVisualStyleBackColor = true;
      // 
      // appDescription
      // 
      this.appDescription.Location = new System.Drawing.Point(114, 36);
      this.appDescription.Name = "appDescription";
      this.appDescription.Size = new System.Drawing.Size(363, 20);
      this.appDescription.TabIndex = 1;
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(35, 38);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(63, 13);
      this.label3.TabIndex = 6;
      this.label3.Text = "Description:";
      // 
      // editConnString
      // 
      this.editConnString.Location = new System.Drawing.Point(396, 63);
      this.editConnString.Name = "editConnString";
      this.editConnString.Size = new System.Drawing.Size(81, 25);
      this.editConnString.TabIndex = 3;
      this.editConnString.Text = "Edit...";
      this.editConnString.UseVisualStyleBackColor = true;
      this.editConnString.Click += new System.EventHandler(this.editConnString_Click);
      // 
      // cancelButton
      // 
      this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.cancelButton.Location = new System.Drawing.Point(400, 293);
      this.cancelButton.Name = "cancelButton";
      this.cancelButton.Size = new System.Drawing.Size(81, 25);
      this.cancelButton.TabIndex = 2;
      this.cancelButton.Text = "Cancel";
      this.cancelButton.UseVisualStyleBackColor = true;
      // 
      // nextButton
      // 
      this.nextButton.Location = new System.Drawing.Point(313, 293);
      this.nextButton.Name = "nextButton";
      this.nextButton.Size = new System.Drawing.Size(81, 25);
      this.nextButton.TabIndex = 1;
      this.nextButton.Text = "Next";
      this.nextButton.UseVisualStyleBackColor = true;
      this.nextButton.Click += new System.EventHandler(this.nextButton_Click);
      // 
      // configPanel
      // 
      this.configPanel.Controls.Add(this.controlPanel);
      this.configPanel.Controls.Add(this.useProvider);
      this.configPanel.Location = new System.Drawing.Point(2, 58);
      this.configPanel.Name = "configPanel";
      this.configPanel.Size = new System.Drawing.Size(492, 225);
      this.configPanel.TabIndex = 8;
      this.configPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.configPanel_Paint);
      // 
      // controlPanel
      // 
      this.controlPanel.Controls.Add(this.enableExpCallback);
      this.controlPanel.Controls.Add(this.label1);
      this.controlPanel.Controls.Add(this.advancedBtn);
      this.controlPanel.Controls.Add(this.connectionString);
      this.controlPanel.Controls.Add(this.autogenSchema);
      this.controlPanel.Controls.Add(this.appDescription);
      this.controlPanel.Controls.Add(this.appName);
      this.controlPanel.Controls.Add(this.label2);
      this.controlPanel.Controls.Add(this.editConnString);
      this.controlPanel.Controls.Add(this.writeExToLog);
      this.controlPanel.Controls.Add(this.label3);
      this.controlPanel.Location = new System.Drawing.Point(3, 31);
      this.controlPanel.Name = "controlPanel";
      this.controlPanel.Size = new System.Drawing.Size(486, 186);
      this.controlPanel.TabIndex = 11;
      // 
      // enableExpCallback
      // 
      this.enableExpCallback.AutoSize = true;
      this.enableExpCallback.Location = new System.Drawing.Point(114, 157);
      this.enableExpCallback.Name = "enableExpCallback";
      this.enableExpCallback.Size = new System.Drawing.Size(171, 17);
      this.enableExpCallback.TabIndex = 7;
      this.enableExpCallback.Text = "Callback for session end event";
      this.enableExpCallback.UseVisualStyleBackColor = true;
      // 
      // advancedBtn
      // 
      this.advancedBtn.Location = new System.Drawing.Point(395, 153);
      this.advancedBtn.Name = "advancedBtn";
      this.advancedBtn.Size = new System.Drawing.Size(81, 25);
      this.advancedBtn.TabIndex = 6;
      this.advancedBtn.Text = "Advanced...";
      this.advancedBtn.UseVisualStyleBackColor = true;
      this.advancedBtn.Click += new System.EventHandler(this.advancedBtn_Click);
      // 
      // useProvider
      // 
      this.useProvider.AutoSize = true;
      this.useProvider.Location = new System.Drawing.Point(118, 14);
      this.useProvider.Name = "useProvider";
      this.useProvider.Size = new System.Drawing.Size(80, 17);
      this.useProvider.TabIndex = 10;
      this.useProvider.Text = "checkBox1";
      this.useProvider.UseVisualStyleBackColor = true;
      this.useProvider.CheckStateChanged += new System.EventHandler(this.useProvider_CheckStateChanged);
      // 
      // pageLabel
      // 
      this.pageLabel.AutoSize = true;
      this.pageLabel.BackColor = System.Drawing.Color.White;
      this.pageLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.pageLabel.Location = new System.Drawing.Point(12, 9);
      this.pageLabel.Name = "pageLabel";
      this.pageLabel.Size = new System.Drawing.Size(124, 13);
      this.pageLabel.TabIndex = 9;
      this.pageLabel.Text = "Page Title Goes Here";
      // 
      // pictureBox1
      // 
      this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Top;
      this.pictureBox1.Image = global::MySql.Data.VisualStudio.Properties.Resources.bannrbmp;
      this.pictureBox1.Location = new System.Drawing.Point(0, 0);
      this.pictureBox1.Name = "pictureBox1";
      this.pictureBox1.Size = new System.Drawing.Size(495, 58);
      this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
      this.pictureBox1.TabIndex = 10;
      this.pictureBox1.TabStop = false;
      // 
      // pageDesc
      // 
      this.pageDesc.AutoSize = true;
      this.pageDesc.BackColor = System.Drawing.Color.White;
      this.pageDesc.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.pageDesc.Location = new System.Drawing.Point(38, 31);
      this.pageDesc.Name = "pageDesc";
      this.pageDesc.Size = new System.Drawing.Size(140, 13);
      this.pageDesc.TabIndex = 11;
      this.pageDesc.Text = "Page Description Goes Here";
      // 
      // backButton
      // 
      this.backButton.Location = new System.Drawing.Point(226, 293);
      this.backButton.Name = "backButton";
      this.backButton.Size = new System.Drawing.Size(81, 25);
      this.backButton.TabIndex = 0;
      this.backButton.Text = "Back";
      this.backButton.UseVisualStyleBackColor = true;
      this.backButton.Click += new System.EventHandler(this.backButton_Click);
      // 
      // WebConfigDlg
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = this.cancelButton;
      this.ClientSize = new System.Drawing.Size(495, 329);
      this.Controls.Add(this.backButton);
      this.Controls.Add(this.pageDesc);
      this.Controls.Add(this.pageLabel);
      this.Controls.Add(this.configPanel);
      this.Controls.Add(this.cancelButton);
      this.Controls.Add(this.nextButton);
      this.Controls.Add(this.pictureBox1);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "WebConfigDlg";
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "MySQL Website Configuration";
      this.configPanel.ResumeLayout(false);
      this.configPanel.PerformLayout();
      this.controlPanel.ResumeLayout(false);
      this.controlPanel.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox connectionString;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.CheckBox writeExToLog;
    private System.Windows.Forms.TextBox appName;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Button editConnString;
    private System.Windows.Forms.TextBox appDescription;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.CheckBox autogenSchema;
    private System.Windows.Forms.Button cancelButton;
    private System.Windows.Forms.Button nextButton;
    private System.Windows.Forms.Panel configPanel;
    private System.Windows.Forms.Label pageLabel;
    private System.Windows.Forms.PictureBox pictureBox1;
    private System.Windows.Forms.Button advancedBtn;
    private System.Windows.Forms.Label pageDesc;
    private System.Windows.Forms.CheckBox useProvider;
    private System.Windows.Forms.Button backButton;
    private System.Windows.Forms.Panel controlPanel;
    private System.Windows.Forms.CheckBox enableExpCallback;
  }
}