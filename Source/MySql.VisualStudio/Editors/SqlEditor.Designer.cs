namespace MySql.Data.VisualStudio.Editors
{
  partial class SqlEditor
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

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.components = new System.ComponentModel.Container();
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlEditor));
      this.toolStrip1 = new System.Windows.Forms.ToolStrip();
      this.connectButton = new System.Windows.Forms.ToolStripButton();
      this.disconnectButton = new System.Windows.Forms.ToolStripButton();
      this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
      this.runSqlButton = new System.Windows.Forms.ToolStripButton();
      this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
      this.serverLabel = new System.Windows.Forms.ToolStripLabel();
      this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
      this.userLabel = new System.Windows.Forms.ToolStripLabel();
      this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
      this.dbLabel = new System.Windows.Forms.ToolStripLabel();
      this.splitter1 = new System.Windows.Forms.Splitter();
      this.tabControl1 = new System.Windows.Forms.TabControl();
      this.resultsPage = new System.Windows.Forms.TabPage();
      this.resultsGrid = new System.Windows.Forms.DataGridView();
      this.messagesPage = new System.Windows.Forms.TabPage();
      this.messages = new System.Windows.Forms.Label();
      this.imageList1 = new System.Windows.Forms.ImageList(this.components);
      this.codeEditor = new MySql.Data.VisualStudio.Editors.VSCodeEditorUserControl();
      this.toolStrip1.SuspendLayout();
      this.tabControl1.SuspendLayout();
      this.resultsPage.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.resultsGrid)).BeginInit();
      this.messagesPage.SuspendLayout();
      this.SuspendLayout();
      // 
      // toolStrip1
      // 
      this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
      this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.connectButton,
            this.disconnectButton,
            this.toolStripSeparator1,
            this.runSqlButton,
            this.toolStripSeparator2,
            this.serverLabel,
            this.toolStripSeparator3,
            this.userLabel,
            this.toolStripSeparator4,
            this.dbLabel});
      this.toolStrip1.Location = new System.Drawing.Point(0, 0);
      this.toolStrip1.Name = "toolStrip1";
      this.toolStrip1.Size = new System.Drawing.Size(604, 25);
      this.toolStrip1.TabIndex = 1;
      this.toolStrip1.Text = "toolStrip1";
      // 
      // connectButton
      // 
      this.connectButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
      this.connectButton.Image = global::MySql.Data.VisualStudio.Properties.Resources.sql_editor_connect;
      this.connectButton.ImageTransparentColor = System.Drawing.Color.Transparent;
      this.connectButton.Name = "connectButton";
      this.connectButton.Size = new System.Drawing.Size(23, 22);
      this.connectButton.Text = "connectButton";
      this.connectButton.ToolTipText = "Connect to MySQL...";
      this.connectButton.Click += new System.EventHandler(this.connectButton_Click);
      // 
      // disconnectButton
      // 
      this.disconnectButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
      this.disconnectButton.Enabled = false;
      this.disconnectButton.Image = global::MySql.Data.VisualStudio.Properties.Resources.sql_editor_disconnect;
      this.disconnectButton.ImageTransparentColor = System.Drawing.Color.Transparent;
      this.disconnectButton.Name = "disconnectButton";
      this.disconnectButton.Size = new System.Drawing.Size(23, 22);
      this.disconnectButton.Text = "Disconnect from MySQL";
      this.disconnectButton.Click += new System.EventHandler(this.disconnectButton_Click);
      // 
      // toolStripSeparator1
      // 
      this.toolStripSeparator1.Name = "toolStripSeparator1";
      this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
      // 
      // runSqlButton
      // 
      this.runSqlButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
      this.runSqlButton.Enabled = false;
      this.runSqlButton.Image = global::MySql.Data.VisualStudio.Properties.Resources.sql_editor_runsql;
      this.runSqlButton.ImageTransparentColor = System.Drawing.Color.Transparent;
      this.runSqlButton.Name = "runSqlButton";
      this.runSqlButton.Size = new System.Drawing.Size(23, 22);
      this.runSqlButton.Text = "runSqlButton";
      this.runSqlButton.ToolTipText = "Run SQL";
      this.runSqlButton.Click += new System.EventHandler(this.runSqlButton_Click);
      // 
      // toolStripSeparator2
      // 
      this.toolStripSeparator2.Name = "toolStripSeparator2";
      this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
      // 
      // serverLabel
      // 
      this.serverLabel.Name = "serverLabel";
      this.serverLabel.Size = new System.Drawing.Size(88, 22);
      this.serverLabel.Text = "Server: <none>";
      // 
      // toolStripSeparator3
      // 
      this.toolStripSeparator3.Name = "toolStripSeparator3";
      this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
      // 
      // userLabel
      // 
      this.userLabel.Name = "userLabel";
      this.userLabel.Size = new System.Drawing.Size(79, 22);
      this.userLabel.Text = "User: <none>";
      // 
      // toolStripSeparator4
      // 
      this.toolStripSeparator4.Name = "toolStripSeparator4";
      this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
      // 
      // dbLabel
      // 
      this.dbLabel.Name = "dbLabel";
      this.dbLabel.Size = new System.Drawing.Size(104, 22);
      this.dbLabel.Text = "Database: <none>";
      // 
      // splitter1
      // 
      this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
      this.splitter1.Location = new System.Drawing.Point(0, 271);
      this.splitter1.Name = "splitter1";
      this.splitter1.Size = new System.Drawing.Size(604, 10);
      this.splitter1.TabIndex = 3;
      this.splitter1.TabStop = false;
      // 
      // tabControl1
      // 
      this.tabControl1.Controls.Add(this.resultsPage);
      this.tabControl1.Controls.Add(this.messagesPage);
      this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tabControl1.ImageList = this.imageList1;
      this.tabControl1.Location = new System.Drawing.Point(0, 281);
      this.tabControl1.Name = "tabControl1";
      this.tabControl1.SelectedIndex = 0;
      this.tabControl1.Size = new System.Drawing.Size(604, 185);
      this.tabControl1.TabIndex = 4;
      // 
      // resultsPage
      // 
      this.resultsPage.Controls.Add(this.resultsGrid);
      this.resultsPage.ImageIndex = 1;
      this.resultsPage.Location = new System.Drawing.Point(4, 23);
      this.resultsPage.Name = "resultsPage";
      this.resultsPage.Padding = new System.Windows.Forms.Padding(3);
      this.resultsPage.Size = new System.Drawing.Size(596, 158);
      this.resultsPage.TabIndex = 0;
      this.resultsPage.Text = "Results";
      this.resultsPage.UseVisualStyleBackColor = true;
      // 
      // resultsGrid
      // 
      this.resultsGrid.AllowUserToAddRows = false;
      this.resultsGrid.AllowUserToDeleteRows = false;
      this.resultsGrid.BackgroundColor = System.Drawing.SystemColors.Window;
      this.resultsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.resultsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
      this.resultsGrid.Location = new System.Drawing.Point(3, 3);
      this.resultsGrid.Name = "resultsGrid";
      this.resultsGrid.Size = new System.Drawing.Size(590, 152);
      this.resultsGrid.TabIndex = 0;
      // 
      // messagesPage
      // 
      this.messagesPage.Controls.Add(this.messages);
      this.messagesPage.ImageIndex = 0;
      this.messagesPage.Location = new System.Drawing.Point(4, 23);
      this.messagesPage.Name = "messagesPage";
      this.messagesPage.Padding = new System.Windows.Forms.Padding(3);
      this.messagesPage.Size = new System.Drawing.Size(596, 158);
      this.messagesPage.TabIndex = 1;
      this.messagesPage.Text = "Messages";
      this.messagesPage.UseVisualStyleBackColor = true;
      // 
      // messages
      // 
      this.messages.Dock = System.Windows.Forms.DockStyle.Fill;
      this.messages.Location = new System.Drawing.Point(3, 3);
      this.messages.Name = "messages";
      this.messages.Size = new System.Drawing.Size(590, 152);
      this.messages.TabIndex = 0;
      // 
      // imageList1
      // 
      this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
      this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
      this.imageList1.Images.SetKeyName(0, "messages_icon.png");
      this.imageList1.Images.SetKeyName(1, "results_icon.png");
      // 
      // codeEditor
      // 
      this.codeEditor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.codeEditor.Dock = System.Windows.Forms.DockStyle.Top;
      this.codeEditor.Location = new System.Drawing.Point(0, 25);
      this.codeEditor.Name = "codeEditor";
      this.codeEditor.Size = new System.Drawing.Size(604, 246);
      this.codeEditor.TabIndex = 2;
      // 
      // SqlEditor
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.tabControl1);
      this.Controls.Add(this.splitter1);
      this.Controls.Add(this.codeEditor);
      this.Controls.Add(this.toolStrip1);
      this.Name = "SqlEditor";
      this.Size = new System.Drawing.Size(604, 466);
      this.toolStrip1.ResumeLayout(false);
      this.toolStrip1.PerformLayout();
      this.tabControl1.ResumeLayout(false);
      this.resultsPage.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.resultsGrid)).EndInit();
      this.messagesPage.ResumeLayout(false);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ToolStrip toolStrip1;
    private VSCodeEditorUserControl codeEditor;
    private System.Windows.Forms.Splitter splitter1;
    private System.Windows.Forms.TabControl tabControl1;
    private System.Windows.Forms.TabPage resultsPage;
    private System.Windows.Forms.TabPage messagesPage;
    private System.Windows.Forms.ToolStripButton connectButton;
    private System.Windows.Forms.ToolStripLabel serverLabel;
    private System.Windows.Forms.ToolStripButton runSqlButton;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
    private System.Windows.Forms.ToolStripButton disconnectButton;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
    private System.Windows.Forms.ToolStripLabel userLabel;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
    private System.Windows.Forms.ToolStripLabel dbLabel;
    private System.Windows.Forms.DataGridView resultsGrid;
    private System.Windows.Forms.Label messages;
    private System.Windows.Forms.ImageList imageList1;

  }
}
