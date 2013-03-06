namespace Profiling
{
    partial class Form1
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
            this.label2 = new System.Windows.Forms.Label();
            this.server = new System.Windows.Forms.TextBox();
            this.userid = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.password = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.database = new System.Windows.Forms.TextBox();
            this.sockets = new System.Windows.Forms.CheckBox();
            this.portNum = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.pipeName = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.useNamedPipes = new System.Windows.Forms.CheckBox();
            this.memName = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.useSharedMem = new System.Windows.Forms.CheckBox();
            this.output = new System.Windows.Forms.TextBox();
            this.testBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Server:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "User Id:";
            // 
            // server
            // 
            this.server.Location = new System.Drawing.Point(59, 8);
            this.server.Name = "server";
            this.server.Size = new System.Drawing.Size(149, 20);
            this.server.TabIndex = 2;
            // 
            // userid
            // 
            this.userid.Location = new System.Drawing.Point(59, 34);
            this.userid.Name = "userid";
            this.userid.Size = new System.Drawing.Size(149, 20);
            this.userid.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(214, 37);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Password:";
            // 
            // password
            // 
            this.password.Location = new System.Drawing.Point(276, 37);
            this.password.Name = "password";
            this.password.Size = new System.Drawing.Size(149, 20);
            this.password.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(214, 15);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(56, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Database:";
            // 
            // database
            // 
            this.database.Location = new System.Drawing.Point(276, 8);
            this.database.Name = "database";
            this.database.Size = new System.Drawing.Size(149, 20);
            this.database.TabIndex = 7;
            this.database.Text = "test";
            // 
            // sockets
            // 
            this.sockets.AutoSize = true;
            this.sockets.Checked = true;
            this.sockets.CheckState = System.Windows.Forms.CheckState.Checked;
            this.sockets.Location = new System.Drawing.Point(15, 65);
            this.sockets.Name = "sockets";
            this.sockets.Size = new System.Drawing.Size(84, 17);
            this.sockets.TabIndex = 8;
            this.sockets.Text = "Use TCP/IP";
            this.sockets.UseVisualStyleBackColor = true;
            // 
            // portNum
            // 
            this.portNum.Location = new System.Drawing.Point(220, 62);
            this.portNum.Name = "portNum";
            this.portNum.Size = new System.Drawing.Size(53, 20);
            this.portNum.TabIndex = 10;
            this.portNum.Text = "3306";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(139, 65);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(29, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Port:";
            // 
            // pipeName
            // 
            this.pipeName.Location = new System.Drawing.Point(220, 86);
            this.pipeName.Name = "pipeName";
            this.pipeName.Size = new System.Drawing.Size(53, 20);
            this.pipeName.TabIndex = 13;
            this.pipeName.Text = "MYSQL";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(139, 89);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(62, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "Pipe Name:";
            // 
            // useNamedPipes
            // 
            this.useNamedPipes.AutoSize = true;
            this.useNamedPipes.Checked = true;
            this.useNamedPipes.CheckState = System.Windows.Forms.CheckState.Checked;
            this.useNamedPipes.Location = new System.Drawing.Point(15, 88);
            this.useNamedPipes.Name = "useNamedPipes";
            this.useNamedPipes.Size = new System.Drawing.Size(111, 17);
            this.useNamedPipes.TabIndex = 11;
            this.useNamedPipes.Text = "Use Named Pipes";
            this.useNamedPipes.UseVisualStyleBackColor = true;
            // 
            // memName
            // 
            this.memName.Location = new System.Drawing.Point(220, 109);
            this.memName.Name = "memName";
            this.memName.Size = new System.Drawing.Size(53, 20);
            this.memName.TabIndex = 16;
            this.memName.Text = "MYSQL";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(139, 112);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(78, 13);
            this.label7.TabIndex = 15;
            this.label7.Text = "Memory Name:";
            // 
            // useSharedMem
            // 
            this.useSharedMem.AutoSize = true;
            this.useSharedMem.Checked = true;
            this.useSharedMem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.useSharedMem.Location = new System.Drawing.Point(15, 111);
            this.useSharedMem.Name = "useSharedMem";
            this.useSharedMem.Size = new System.Drawing.Size(122, 17);
            this.useSharedMem.TabIndex = 14;
            this.useSharedMem.Text = "Use Shared Memory";
            this.useSharedMem.UseVisualStyleBackColor = true;
            // 
            // output
            // 
            this.output.Location = new System.Drawing.Point(12, 160);
            this.output.Multiline = true;
            this.output.Name = "output";
            this.output.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.output.Size = new System.Drawing.Size(413, 332);
            this.output.TabIndex = 17;
            // 
            // testBtn
            // 
            this.testBtn.Location = new System.Drawing.Point(350, 102);
            this.testBtn.Name = "testBtn";
            this.testBtn.Size = new System.Drawing.Size(75, 23);
            this.testBtn.TabIndex = 18;
            this.testBtn.Text = "Test";
            this.testBtn.UseVisualStyleBackColor = true;
            this.testBtn.Click += new System.EventHandler(this.testBtn_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(436, 504);
            this.Controls.Add(this.testBtn);
            this.Controls.Add(this.output);
            this.Controls.Add(this.memName);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.useSharedMem);
            this.Controls.Add(this.pipeName);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.useNamedPipes);
            this.Controls.Add(this.portNum);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.sockets);
            this.Controls.Add(this.database);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.password);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.userid);
            this.Controls.Add(this.server);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox server;
        private System.Windows.Forms.TextBox userid;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox password;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox database;
        private System.Windows.Forms.CheckBox sockets;
        private System.Windows.Forms.TextBox portNum;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox pipeName;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox useNamedPipes;
        private System.Windows.Forms.TextBox memName;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox useSharedMem;
        private System.Windows.Forms.TextBox output;
        private System.Windows.Forms.Button testBtn;
    }
}

