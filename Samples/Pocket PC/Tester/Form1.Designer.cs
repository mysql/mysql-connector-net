namespace DeviceApplication1
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MainMenu mainMenu1;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.mainMenu1 = new System.Windows.Forms.MainMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.runTestsMenu = new System.Windows.Forms.MenuItem();
            this.showDetailsMenu = new System.Windows.Forms.MenuItem();
            this.label1 = new System.Windows.Forms.Label();
            this.testTree = new System.Windows.Forms.TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList();
            this.runSelected = new System.Windows.Forms.MenuItem();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.Add(this.menuItem1);
            // 
            // menuItem1
            // 
            this.menuItem1.MenuItems.Add(this.runTestsMenu);
            this.menuItem1.MenuItems.Add(this.showDetailsMenu);
            this.menuItem1.MenuItems.Add(this.runSelected);
            this.menuItem1.Text = "&Options";
            // 
            // runTestsMenu
            // 
            this.runTestsMenu.Text = "&Run Tests";
            this.runTestsMenu.Click += new System.EventHandler(this.runTestsMenu_Click);
            // 
            // showDetailsMenu
            // 
            this.showDetailsMenu.Text = "&Show Details...";
            this.showDetailsMenu.Click += new System.EventHandler(this.showDetailsMenu_Click);
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(11, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(108, 20);
            this.label1.Text = "Tests:";
            // 
            // testTree
            // 
            this.testTree.ImageIndex = 2;
            this.testTree.ImageList = this.imageList1;
            this.testTree.Location = new System.Drawing.Point(3, 34);
            this.testTree.Name = "testTree";
            this.testTree.SelectedImageIndex = 2;
            this.testTree.Size = new System.Drawing.Size(234, 231);
            this.testTree.TabIndex = 17;
            this.imageList1.Images.Clear();
            this.imageList1.Images.Add(((System.Drawing.Image)(resources.GetObject("resource"))));
            this.imageList1.Images.Add(((System.Drawing.Image)(resources.GetObject("resource1"))));
            this.imageList1.Images.Add(((System.Drawing.Image)(resources.GetObject("resource2"))));
            // 
            // runSelected
            // 
            this.runSelected.Text = "Run Selected &Tests";
            this.runSelected.Click += new System.EventHandler(this.runSelected_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(240, 268);
            this.Controls.Add(this.testTree);
            this.Controls.Add(this.label1);
            this.Menu = this.mainMenu1;
            this.Name = "MainForm";
            this.Text = "MySQL Tester";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem runTestsMenu;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TreeView testTree;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.MenuItem showDetailsMenu;
        private System.Windows.Forms.MenuItem runSelected;
    }
}

