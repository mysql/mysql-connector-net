using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using MySql.Data.MySqlClient;

namespace Async
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TextBox nonQueryOutput;
		private System.Windows.Forms.Button nonQueryGo;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox server;
		private System.Windows.Forms.TextBox uid;
		private System.Windows.Forms.TextBox pwd;
		private System.Windows.Forms.TextBox database;
		private System.Windows.Forms.Timer timer1;
		private System.ComponentModel.IContainer components;

		private MySqlConnection	conn;
		private MySqlCommand	cmd;
		private int				nextTime;
		private IAsyncResult	asyncResult;
		private DateTime		start;

		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.database = new System.Windows.Forms.TextBox();
			this.pwd = new System.Windows.Forms.TextBox();
			this.uid = new System.Windows.Forms.TextBox();
			this.server = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.nonQueryGo = new System.Windows.Forms.Button();
			this.nonQueryOutput = new System.Windows.Forms.TextBox();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.tabControl1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(504, 370);
			this.tabControl1.TabIndex = 0;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.database);
			this.tabPage2.Controls.Add(this.pwd);
			this.tabPage2.Controls.Add(this.uid);
			this.tabPage2.Controls.Add(this.server);
			this.tabPage2.Controls.Add(this.label4);
			this.tabPage2.Controls.Add(this.label3);
			this.tabPage2.Controls.Add(this.label2);
			this.tabPage2.Controls.Add(this.label1);
			this.tabPage2.Location = new System.Drawing.Point(4, 22);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Size = new System.Drawing.Size(496, 344);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Configuration";
			// 
			// database
			// 
			this.database.Location = new System.Drawing.Point(116, 128);
			this.database.Name = "database";
			this.database.Size = new System.Drawing.Size(228, 20);
			this.database.TabIndex = 7;
			this.database.Text = "";
			// 
			// pwd
			// 
			this.pwd.Location = new System.Drawing.Point(116, 92);
			this.pwd.Name = "pwd";
			this.pwd.Size = new System.Drawing.Size(228, 20);
			this.pwd.TabIndex = 6;
			this.pwd.Text = "";
			// 
			// uid
			// 
			this.uid.Location = new System.Drawing.Point(116, 56);
			this.uid.Name = "uid";
			this.uid.Size = new System.Drawing.Size(228, 20);
			this.uid.TabIndex = 5;
			this.uid.Text = "";
			// 
			// server
			// 
			this.server.Location = new System.Drawing.Point(116, 20);
			this.server.Name = "server";
			this.server.Size = new System.Drawing.Size(228, 20);
			this.server.TabIndex = 4;
			this.server.Text = "";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(16, 128);
			this.label4.Name = "label4";
			this.label4.TabIndex = 3;
			this.label4.Text = "Database:";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(16, 92);
			this.label3.Name = "label3";
			this.label3.TabIndex = 2;
			this.label3.Text = "Password:";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(16, 56);
			this.label2.Name = "label2";
			this.label2.TabIndex = 1;
			this.label2.Text = "User Id:";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(16, 20);
			this.label1.Name = "label1";
			this.label1.TabIndex = 0;
			this.label1.Text = "Server/Host:";
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.nonQueryGo);
			this.tabPage1.Controls.Add(this.nonQueryOutput);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Size = new System.Drawing.Size(496, 344);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "NonQuery";
			// 
			// nonQueryGo
			// 
			this.nonQueryGo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.nonQueryGo.Location = new System.Drawing.Point(404, 312);
			this.nonQueryGo.Name = "nonQueryGo";
			this.nonQueryGo.TabIndex = 1;
			this.nonQueryGo.Text = "Go";
			this.nonQueryGo.Click += new System.EventHandler(this.nonQueryGo_Click);
			// 
			// nonQueryOutput
			// 
			this.nonQueryOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.nonQueryOutput.Location = new System.Drawing.Point(12, 36);
			this.nonQueryOutput.Multiline = true;
			this.nonQueryOutput.Name = "nonQueryOutput";
			this.nonQueryOutput.Size = new System.Drawing.Size(468, 268);
			this.nonQueryOutput.TabIndex = 0;
			this.nonQueryOutput.Text = "";
			// 
			// timer1
			// 
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// Form1
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(504, 370);
			this.Controls.Add(this.tabControl1);
			this.Name = "Form1";
			this.Text = "Form1";
			this.tabControl1.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new Form1());
		}

		private void nonQueryGo_Click(object sender, System.EventArgs e)
		{
			string connStr = String.Format("server={0};uid={1};pwd={2};database={3}",
				server.Text, uid.Text, pwd.Text, database.Text);
			conn = new MySqlConnection(connStr);
			try 
			{
				conn.Open();

				string sql = "DROP TABLE IF EXISTS AsyncSampleTable; CREATE TABLE AsyncSampleTable (numVal int)";
				cmd = new MySqlCommand(sql, conn);
				cmd.ExecuteNonQuery();

				sql = "DROP PROCEDURE IF EXISTS AsyncSample;" +
					"CREATE PROCEDURE AsyncSample() BEGIN " +
					"set @x=0; repeat set @x=@x+1; until @x > 5000000 end repeat; " +
					"INSERT INTO AsyncSampleTable VALUES (1); end;";
				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();

				cmd.CommandText = "AsyncSample";
				cmd.CommandType = CommandType.StoredProcedure;

				asyncResult = cmd.BeginExecuteNonQuery();
				nextTime = 5;
				timer1.Enabled = true;
				start = DateTime.Now;
			}
			catch (Exception ex) 
			{
				MessageBox.Show("Exception: " + ex.Message);
			}
		}

		private void timer1_Tick(object sender, System.EventArgs e)
		{
			if (! asyncResult.IsCompleted) 
			{
				TimeSpan ts = DateTime.Now.Subtract(start);
				if (ts.TotalSeconds > nextTime) 
				{
					nonQueryOutput.Text += Convert.ToInt32(ts.TotalSeconds) + " seconds" + Environment.NewLine;
					nextTime += 5;
				}
				return;
			}

			int recordsAffected = cmd.EndExecuteNonQuery(asyncResult);
			nonQueryOutput.Text += "Records Affected = " + recordsAffected;
			conn.Close();
			timer1.Enabled = false;
		}
	}
}
