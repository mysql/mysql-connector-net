using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace MobileExplorer
{
	/// <summary>
	/// Summary description for NewServerDialog.
	/// </summary>
	public class NewServerDialog : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button okBtn;
		private System.Windows.Forms.TextBox name;
		private System.Windows.Forms.TextBox host;
		private System.Windows.Forms.TextBox uid;
		private System.Windows.Forms.TextBox pwd;
		private System.Windows.Forms.Button cancelBtn;
	
		public NewServerDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.label1 = new System.Windows.Forms.Label();
			this.name = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.host = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.uid = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.pwd = new System.Windows.Forms.TextBox();
			this.okBtn = new System.Windows.Forms.Button();
			this.cancelBtn = new System.Windows.Forms.Button();
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 12);
			this.label1.Size = new System.Drawing.Size(52, 20);
			this.label1.Text = "Name:";
			// 
			// name
			// 
			this.name.Location = new System.Drawing.Point(8, 28);
			this.name.Size = new System.Drawing.Size(168, 22);
			this.name.Text = "";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(8, 72);
			this.label2.Size = new System.Drawing.Size(52, 20);
			this.label2.Text = "Host:";
			// 
			// host
			// 
			this.host.Location = new System.Drawing.Point(8, 88);
			this.host.Size = new System.Drawing.Size(168, 22);
			this.host.Text = "";
			// 
			// label3
			// 
			this.label3.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular);
			this.label3.Location = new System.Drawing.Point(8, 120);
			this.label3.Size = new System.Drawing.Size(60, 20);
			this.label3.Text = "User Id:";
			// 
			// uid
			// 
			this.uid.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular);
			this.uid.Location = new System.Drawing.Point(8, 136);
			this.uid.Size = new System.Drawing.Size(168, 22);
			this.uid.Text = "";
			// 
			// label4
			// 
			this.label4.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular);
			this.label4.Location = new System.Drawing.Point(8, 168);
			this.label4.Size = new System.Drawing.Size(60, 20);
			this.label4.Text = "Password:";
			// 
			// pwd
			// 
			this.pwd.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular);
			this.pwd.Location = new System.Drawing.Point(8, 184);
			this.pwd.Size = new System.Drawing.Size(168, 22);
			this.pwd.Text = "";
			// 
			// okBtn
			// 
			this.okBtn.Location = new System.Drawing.Point(104, 220);
			this.okBtn.Text = "OK";
			this.okBtn.Click += new System.EventHandler(this.okBtn_Click);
			// 
			// cancelBtn
			// 
			this.cancelBtn.Location = new System.Drawing.Point(24, 220);
			this.cancelBtn.Text = "Cancel";
			this.cancelBtn.Click += new System.EventHandler(this.cancelBtn_Click);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			// 
			// NewServerDialog
			// 
			this.ClientSize = new System.Drawing.Size(190, 272);
			this.ControlBox = false;
			this.Controls.Add(this.cancelBtn);
			this.Controls.Add(this.okBtn);
			this.Controls.Add(this.pwd);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.uid);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.host);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.name);
			this.Controls.Add(this.label1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Text = "NewServerDialog";

		}
		#endregion

		private void cancelBtn_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void okBtn_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		internal ServerConfig GetServerConfig() 
		{
			ServerConfig sc = new ServerConfig();
			sc.name = name.Text;
			sc.host = host.Text;
			sc.uid = uid.Text;
			sc.pwd = pwd.Text;
			return sc;
		}
	}
}
