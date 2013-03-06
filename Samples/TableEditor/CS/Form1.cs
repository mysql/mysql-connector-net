// Copyright (C) 2004-2005 MySQL AB
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using MySql.Data.MySqlClient;

namespace TableEditor
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox tables;
		private System.Windows.Forms.TextBox server;
		private System.Windows.Forms.TextBox userid;
		private System.Windows.Forms.TextBox password;
		private System.Windows.Forms.Button connectBtn;
		private System.Windows.Forms.Button updateBtn;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.ComboBox databaseList;
		private System.Windows.Forms.Label label5;

		private MySqlConnection		conn;
		private DataTable			data;
		private MySqlDataAdapter	da;
		private System.Windows.Forms.DataGrid dataGrid;
		private MySqlCommandBuilder	cb;

		public Form1()
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
			this.label1 = new System.Windows.Forms.Label();
			this.server = new System.Windows.Forms.TextBox();
			this.userid = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.password = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.connectBtn = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.tables = new System.Windows.Forms.ComboBox();
			this.dataGrid = new System.Windows.Forms.DataGrid();
			this.updateBtn = new System.Windows.Forms.Button();
			this.databaseList = new System.Windows.Forms.ComboBox();
			this.label5 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.dataGrid)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 11);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "Server:";
			// 
			// server
			// 
			this.server.Location = new System.Drawing.Point(56, 8);
			this.server.Name = "server";
			this.server.Size = new System.Drawing.Size(320, 20);
			this.server.TabIndex = 1;
			this.server.Text = "";
			// 
			// userid
			// 
			this.userid.Location = new System.Drawing.Point(56, 32);
			this.userid.Name = "userid";
			this.userid.Size = new System.Drawing.Size(120, 20);
			this.userid.TabIndex = 3;
			this.userid.Text = "";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(8, 37);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(48, 16);
			this.label2.TabIndex = 2;
			this.label2.Text = "User Id:";
			// 
			// password
			// 
			this.password.Location = new System.Drawing.Point(260, 32);
			this.password.Name = "password";
			this.password.PasswordChar = '*';
			this.password.Size = new System.Drawing.Size(116, 20);
			this.password.TabIndex = 5;
			this.password.Text = "";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(192, 37);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(56, 16);
			this.label3.TabIndex = 4;
			this.label3.Text = "Password:";
			// 
			// connectBtn
			// 
			this.connectBtn.Location = new System.Drawing.Point(400, 8);
			this.connectBtn.Name = "connectBtn";
			this.connectBtn.TabIndex = 6;
			this.connectBtn.Text = "Connect";
			this.connectBtn.Click += new System.EventHandler(this.connectBtn_Click);
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(8, 109);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(64, 16);
			this.label4.TabIndex = 0;
			this.label4.Text = "Tables";
			// 
			// tables
			// 
			this.tables.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.tables.Location = new System.Drawing.Point(80, 104);
			this.tables.Name = "tables";
			this.tables.Size = new System.Drawing.Size(296, 21);
			this.tables.TabIndex = 7;
			this.tables.SelectedIndexChanged += new System.EventHandler(this.tables_SelectedIndexChanged);
			// 
			// dataGrid
			// 
			this.dataGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.dataGrid.DataMember = "";
			this.dataGrid.HeaderForeColor = System.Drawing.SystemColors.ControlText;
			this.dataGrid.Location = new System.Drawing.Point(8, 136);
			this.dataGrid.Name = "dataGrid";
			this.dataGrid.Size = new System.Drawing.Size(544, 312);
			this.dataGrid.TabIndex = 8;
			// 
			// updateBtn
			// 
			this.updateBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.updateBtn.Location = new System.Drawing.Point(477, 104);
			this.updateBtn.Name = "updateBtn";
			this.updateBtn.TabIndex = 9;
			this.updateBtn.Text = "Update";
			this.updateBtn.Click += new System.EventHandler(this.updateBtn_Click);
			// 
			// databaseList
			// 
			this.databaseList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.databaseList.Location = new System.Drawing.Point(80, 80);
			this.databaseList.Name = "databaseList";
			this.databaseList.Size = new System.Drawing.Size(296, 21);
			this.databaseList.TabIndex = 11;
			this.databaseList.SelectedIndexChanged += new System.EventHandler(this.databaseList_SelectedIndexChanged);
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(8, 85);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(64, 16);
			this.label5.TabIndex = 10;
			this.label5.Text = "Databases";
			// 
			// Form1
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(560, 453);
			this.Controls.Add(this.databaseList);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.updateBtn);
			this.Controls.Add(this.dataGrid);
			this.Controls.Add(this.tables);
			this.Controls.Add(this.connectBtn);
			this.Controls.Add(this.password);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.userid);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.server);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.label4);
			this.Name = "Form1";
			this.Text = "Form1";
			((System.ComponentModel.ISupportInitialize)(this.dataGrid)).EndInit();
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

		private void connectBtn_Click(object sender, System.EventArgs e)
		{
			if (conn != null)
				conn.Close();
	
			string connStr = String.Format("server={0};user id={1}; password={2}; database=mysql; pooling=false",
				server.Text, userid.Text, password.Text );

			try 
			{
				conn = new MySqlConnection( connStr );
				conn.Open();

				GetDatabases();
			}
			catch (MySqlException ex) 
			{
				MessageBox.Show( "Error connecting to the server: " + ex.Message );
			}
		}

		private void GetDatabases() 
		{
			MySqlDataReader reader = null;

			MySqlCommand cmd = new MySqlCommand("SHOW DATABASES", conn);
			try 
			{
				reader = cmd.ExecuteReader();
				databaseList.Items.Clear();
				while (reader.Read()) 
				{
					databaseList.Items.Add( reader.GetString(0) );
				}
			}
			catch (MySqlException ex) 
			{
				MessageBox.Show("Failed to populate database list: " + ex.Message );
			}
			finally 
			{
				if (reader != null) reader.Close();
			}
		}

		private void databaseList_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			MySqlDataReader reader = null;

			conn.ChangeDatabase( databaseList.SelectedItem.ToString() );

			MySqlCommand cmd = new MySqlCommand("SHOW TABLES", conn);
			try 
			{
				reader = cmd.ExecuteReader();
				tables.Items.Clear();
				while (reader.Read()) 
				{
					tables.Items.Add( reader.GetString(0) );
				}
			}
			catch (MySqlException ex) 
			{
				MessageBox.Show("Failed to populate table list: " + ex.Message );
			}
			finally 
			{
				if (reader != null) reader.Close();
			}
		}

		private void tables_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			data = new DataTable();
			
			da = new MySqlDataAdapter("SELECT * FROM " + tables.SelectedItem.ToString(), conn );
			cb = new MySqlCommandBuilder( da );

			da.Fill( data );

			dataGrid.DataSource = data;
		}

		private void updateBtn_Click(object sender, System.EventArgs e)
		{
			DataTable changes = data.GetChanges();
			da.Update( changes );
			data.AcceptChanges();
		}
	}
}
