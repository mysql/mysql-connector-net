using System;
using System.Data;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace MobileExplorer
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button serverConnect;
		private System.Windows.Forms.Panel chooseServerPanel;
		private System.Windows.Forms.Panel showServerPanel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label activeServer;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox objectTypeList;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox dbList;
		private System.Windows.Forms.ListBox objectList;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.ListBox serverList;
		private System.Windows.Forms.Button backToChooserBtn;
		private System.Windows.Forms.Panel tableViewPanel;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.DataGrid tableGrid;
		private System.Windows.Forms.Button backToServerBtn;
		private System.Windows.Forms.Button showObject;
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem newServer;

		private MySqlConnection	server;
		private System.Windows.Forms.Label activeTable;
		private ArrayList		servers;
		private System.Windows.Forms.MenuItem deleteServer;
		private System.Windows.Forms.Panel spViewPanel;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox spText;
		private System.Windows.Forms.Button ok;
		private System.Windows.Forms.Button cancel;
		private System.Windows.Forms.Label activeProc;
		private ServerConfig	activeConfig;
	
		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			server = new MySqlConnection();
			servers = new ArrayList();
			LoadServers();
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
			this.chooseServerPanel = new System.Windows.Forms.Panel();
			this.serverConnect = new System.Windows.Forms.Button();
			this.serverList = new System.Windows.Forms.ListBox();
			this.label1 = new System.Windows.Forms.Label();
			this.showServerPanel = new System.Windows.Forms.Panel();
			this.label5 = new System.Windows.Forms.Label();
			this.objectList = new System.Windows.Forms.ListBox();
			this.dbList = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.objectTypeList = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.activeServer = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.backToChooserBtn = new System.Windows.Forms.Button();
			this.tableViewPanel = new System.Windows.Forms.Panel();
			this.label6 = new System.Windows.Forms.Label();
			this.activeTable = new System.Windows.Forms.Label();
			this.tableGrid = new System.Windows.Forms.DataGrid();
			this.backToServerBtn = new System.Windows.Forms.Button();
			this.showObject = new System.Windows.Forms.Button();
			this.mainMenu1 = new System.Windows.Forms.MainMenu();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.newServer = new System.Windows.Forms.MenuItem();
			this.deleteServer = new System.Windows.Forms.MenuItem();
			this.spViewPanel = new System.Windows.Forms.Panel();
			this.label7 = new System.Windows.Forms.Label();
			this.activeProc = new System.Windows.Forms.Label();
			this.spText = new System.Windows.Forms.TextBox();
			this.ok = new System.Windows.Forms.Button();
			this.cancel = new System.Windows.Forms.Button();
			// 
			// chooseServerPanel
			// 
			this.chooseServerPanel.Controls.Add(this.serverConnect);
			this.chooseServerPanel.Controls.Add(this.serverList);
			this.chooseServerPanel.Controls.Add(this.label1);
			this.chooseServerPanel.Location = new System.Drawing.Point(4, 4);
			this.chooseServerPanel.Size = new System.Drawing.Size(220, 296);
			// 
			// serverConnect
			// 
			this.serverConnect.Location = new System.Drawing.Point(132, 212);
			this.serverConnect.Text = "Connect";
			this.serverConnect.Click += new System.EventHandler(this.serverConnect_Click);
			// 
			// serverList
			// 
			this.serverList.Location = new System.Drawing.Point(12, 32);
			this.serverList.Size = new System.Drawing.Size(192, 170);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(12, 12);
			this.label1.Size = new System.Drawing.Size(140, 20);
			this.label1.Text = "Configured Servers";
			// 
			// showServerPanel
			// 
			this.showServerPanel.Controls.Add(this.objectList);
			this.showServerPanel.Controls.Add(this.showObject);
			this.showServerPanel.Controls.Add(this.backToChooserBtn);
			this.showServerPanel.Controls.Add(this.label5);
			this.showServerPanel.Controls.Add(this.dbList);
			this.showServerPanel.Controls.Add(this.label4);
			this.showServerPanel.Controls.Add(this.objectTypeList);
			this.showServerPanel.Controls.Add(this.label3);
			this.showServerPanel.Controls.Add(this.activeServer);
			this.showServerPanel.Controls.Add(this.label2);
			this.showServerPanel.Location = new System.Drawing.Point(4, 4);
			this.showServerPanel.Size = new System.Drawing.Size(228, 264);
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(12, 104);
			this.label5.Text = "Objects";
			// 
			// objectList
			// 
			this.objectList.Location = new System.Drawing.Point(12, 120);
			this.objectList.Size = new System.Drawing.Size(200, 100);
			// 
			// dbList
			// 
			this.dbList.Location = new System.Drawing.Point(80, 40);
			this.dbList.Size = new System.Drawing.Size(132, 22);
			this.dbList.SelectedIndexChanged += new System.EventHandler(this.dbList_SelectedIndexChanged);
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(12, 40);
			this.label4.Size = new System.Drawing.Size(72, 20);
			this.label4.Text = "Database:";
			// 
			// objectTypeList
			// 
			this.objectTypeList.Items.Add("Tables");
			this.objectTypeList.Items.Add("Stored Procedures");
			this.objectTypeList.Items.Add("User Defined Functions");
			this.objectTypeList.Items.Add("Views");
			this.objectTypeList.Location = new System.Drawing.Point(80, 76);
			this.objectTypeList.Size = new System.Drawing.Size(132, 22);
			this.objectTypeList.SelectedIndexChanged += new System.EventHandler(this.objectTypeList_SelectedIndexChanged);
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(12, 76);
			this.label3.Size = new System.Drawing.Size(48, 20);
			this.label3.Text = "Object:";
			// 
			// activeServer
			// 
			this.activeServer.Location = new System.Drawing.Point(80, 12);
			this.activeServer.Size = new System.Drawing.Size(128, 20);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(12, 12);
			this.label2.Size = new System.Drawing.Size(48, 20);
			this.label2.Text = "Server:";
			// 
			// backToChooserBtn
			// 
			this.backToChooserBtn.Location = new System.Drawing.Point(64, 232);
			this.backToChooserBtn.Text = "Back";
			this.backToChooserBtn.Click += new System.EventHandler(this.backToChooserBtn_Click);
			// 
			// tableViewPanel
			// 
			this.tableViewPanel.Controls.Add(this.backToServerBtn);
			this.tableViewPanel.Controls.Add(this.tableGrid);
			this.tableViewPanel.Controls.Add(this.activeTable);
			this.tableViewPanel.Controls.Add(this.label6);
			this.tableViewPanel.Location = new System.Drawing.Point(4, 4);
			this.tableViewPanel.Size = new System.Drawing.Size(224, 264);
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(12, 12);
			this.label6.Size = new System.Drawing.Size(52, 20);
			this.label6.Text = "Table:";
			// 
			// activeTable
			// 
			this.activeTable.Location = new System.Drawing.Point(60, 12);
			this.activeTable.Size = new System.Drawing.Size(152, 20);
			// 
			// tableGrid
			// 
			this.tableGrid.Location = new System.Drawing.Point(4, 32);
			this.tableGrid.Size = new System.Drawing.Size(216, 196);
			this.tableGrid.Text = "dataGrid1";
			// 
			// backToServerBtn
			// 
			this.backToServerBtn.Location = new System.Drawing.Point(144, 236);
			this.backToServerBtn.Text = "Back";
			this.backToServerBtn.Click += new System.EventHandler(this.backToServerBtn_Click);
			// 
			// showObject
			// 
			this.showObject.Location = new System.Drawing.Point(140, 232);
			this.showObject.Text = "Show";
			this.showObject.Click += new System.EventHandler(this.showObject_Click);
			// 
			// mainMenu1
			// 
			this.mainMenu1.MenuItems.Add(this.menuItem1);
			// 
			// menuItem1
			// 
			this.menuItem1.MenuItems.Add(this.newServer);
			this.menuItem1.MenuItems.Add(this.deleteServer);
			this.menuItem1.Text = "Servers";
			// 
			// newServer
			// 
			this.newServer.Text = "&New...";
			this.newServer.Click += new System.EventHandler(this.newServer_Click);
			// 
			// deleteServer
			// 
			this.deleteServer.Text = "&Delete";
			this.deleteServer.Click += new System.EventHandler(this.deleteServer_Click);
			// 
			// spViewPanel
			// 
			this.spViewPanel.Controls.Add(this.cancel);
			this.spViewPanel.Controls.Add(this.ok);
			this.spViewPanel.Controls.Add(this.spText);
			this.spViewPanel.Controls.Add(this.activeProc);
			this.spViewPanel.Controls.Add(this.label7);
			this.spViewPanel.Location = new System.Drawing.Point(4, 4);
			this.spViewPanel.Size = new System.Drawing.Size(220, 296);
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(8, 12);
			this.label7.Size = new System.Drawing.Size(72, 20);
			this.label7.Text = "Procedure:";
			// 
			// activeProc
			// 
			this.activeProc.Location = new System.Drawing.Point(80, 12);
			this.activeProc.Size = new System.Drawing.Size(132, 20);
			// 
			// spText
			// 
			this.spText.Location = new System.Drawing.Point(8, 36);
			this.spText.Multiline = true;
			this.spText.Size = new System.Drawing.Size(204, 192);
			this.spText.Text = "";
			// 
			// ok
			// 
			this.ok.Location = new System.Drawing.Point(64, 236);
			this.ok.Text = "OK";
			this.ok.Click += new System.EventHandler(this.ok_Click);
			// 
			// cancel
			// 
			this.cancel.Location = new System.Drawing.Point(140, 236);
			this.cancel.Text = "Cancel";
			this.cancel.Click += new System.EventHandler(this.cancel_Click);
			// 
			// Form1
			// 
			this.ClientSize = new System.Drawing.Size(230, 372);
			this.Controls.Add(this.chooseServerPanel);
			this.Controls.Add(this.tableViewPanel);
			this.Controls.Add(this.showServerPanel);
			this.Controls.Add(this.spViewPanel);
			this.Menu = this.mainMenu1;
			this.Text = "Form1";

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>

		static void Main() 
		{
			Application.Run(new Form1());
		}

		private void serverConnect_Click(object sender, System.EventArgs e)
		{
			activeConfig = (ServerConfig)servers[serverList.SelectedIndex];
			server.ConnectionString = "server=" + activeConfig.host + ";uid=" + activeConfig.uid + 
				";pwd=" + activeConfig.pwd + ";pooling=false;database=mysql";
			MessageBox.Show(server.ConnectionString, "connstr");
			objectList.Items.Clear();
			activeServer.Text = activeConfig.name;

			try 
			{
				server.Open();

				MySqlCommand cmd = new MySqlCommand("SHOW DATABASES", server);
				using (MySqlDataReader reader = cmd.ExecuteReader()) 
				{
					dbList.Items.Clear();
					while (reader.Read()) 
					{
						dbList.Items.Add(reader.GetString(0));
					}
				}
			}
			catch (MySqlException ex) 
			{
				MessageBox.Show(ex.Message);
			}

			showServerPanel.BringToFront();
		}

		private void dbList_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			try 
			{
				server.Close();
				server.ConnectionString = "server=monster;uid=reggie;pwd=reggie;database=" + dbList.SelectedItem;
				server.Open();
			}
			catch (Exception ex) 
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void objectTypeList_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			string sql = null;

			objectList.Items.Clear();

			switch (objectTypeList.SelectedIndex) 
			{
				case 0: 
					sql = "SHOW TABLES";	
					break;
				case 1:
					if (!server.ServerVersion.StartsWith("5")) return;
					sql = "SELECT name FROM mysql.proc WHERE db='" + server.Database + "'";
					break;
				case 2:
					sql = "select name from mysql.func";
					break;
				case 3:
					return;
			}

			MySqlCommand cmd = new MySqlCommand(sql, server);
			using (MySqlDataReader reader = cmd.ExecuteReader()) 
			{
				while (reader.Read()) 
					objectList.Items.Add(reader.GetString(0));
			}
		}

		private void backToChooserBtn_Click(object sender, System.EventArgs e)
		{
			server.Close();
			this.chooseServerPanel.BringToFront();
		}

		private void backToServerBtn_Click(object sender, System.EventArgs e)
		{
			activeTable.Text = String.Empty;
			tableGrid.DataSource = null;
			showServerPanel.BringToFront();
		}

		private void ShowTable() 
		{
			DataSet ds = new DataSet();
			MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM " + objectList.SelectedItem, server);
			activeTable.Text = objectList.SelectedItem.ToString();
			da.Fill(ds);
			tableGrid.DataSource = ds.Tables[0];
			this.tableViewPanel.BringToFront();
		}

		private void ShowProc() 
		{
			MySqlCommand cmd = new MySqlCommand("SHOW CREATE PROCEDURE " + objectList.SelectedItem, server);
			using (MySqlDataReader reader = cmd.ExecuteReader()) 
			{
				reader.Read();
				string body = reader.GetString(2);
				body = body.Replace("\n", "\r\n");
				spText.Text = body;
			}
			activeProc.Text = objectList.SelectedItem.ToString();
			spViewPanel.BringToFront();
		}

		private void showObject_Click(object sender, System.EventArgs e)
		{
			switch (objectTypeList.SelectedIndex)
			{
				case 0:
					ShowTable(); break;
				case 1:
					ShowProc(); break;
			}
		}

		private void newServer_Click(object sender, System.EventArgs e)
		{
			NewServerDialog d = new NewServerDialog();
			DialogResult result = d.ShowDialog();
			if (result == DialogResult.Cancel) return;

			ServerConfig sc = d.GetServerConfig();
			servers.Add(sc);
			serverList.Items.Add(sc);
			SaveServers();
		}

		private void SaveServers() 
		{
			FileStream fs = new FileStream(@"\My Documents\MobileExplorer.dat", FileMode.Create);
			StreamWriter sw = new StreamWriter(fs);

			foreach (ServerConfig sc in servers) 
			{
				string line = sc.name + "|" + sc.host + "|" + sc.uid + "|" + sc.pwd;
				sw.WriteLine(line);
			}

			sw.Close();
			fs.Close();
		}

		private void LoadServers() 
		{
			servers.Clear();
			serverList.Items.Clear();

			try 
			{
				if (!File.Exists(@"\My Documents\MobileExplorer.dat")) return;

				StreamReader sr = new StreamReader(@"\My Documents\MobileExplorer.dat");
				string line = sr.ReadLine();
				while (line != null) 
				{
					string[] parts = line.Split('|');
					ServerConfig sc = new ServerConfig();
					sc.name = parts[0];
					sc.host = parts[1];
					sc.uid = parts[2];
					sc.pwd = parts[3];
					servers.Add(sc);
					serverList.Items.Add(sc.name);
					line = sr.ReadLine();
				}
				sr.Close();
			}
			catch (Exception ex) 
			{
			}
		}

		private void deleteServer_Click(object sender, System.EventArgs e)
		{
			if (serverList.SelectedIndex == -1)
			{
				MessageBox.Show("You do not have a server selected");
				return;
			}

			ServerConfig sc = (ServerConfig)servers[serverList.SelectedIndex];
			DialogResult result = MessageBox.Show("Are you sure you want to delete server '" + sc.name + "'", 
				"Delete Server", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
			if (result == DialogResult.Cancel) return;
			if (result == DialogResult.No) return;
			servers.RemoveAt(serverList.SelectedIndex);
			SaveServers();
		}

		private void cancel_Click(object sender, System.EventArgs e)
		{
			activeProc.Text = String.Empty;
			spText.Text = String.Empty;
			showServerPanel.BringToFront();
		}

		private void ok_Click(object sender, System.EventArgs e)
		{
			MySqlCommand cmd = new MySqlCommand("DROP PROCEDURE " + objectList.SelectedItem + 
				";" + spText.Text, server);
			cmd.ExecuteNonQuery();
			activeProc.Text = String.Empty;
			spText.Text = String.Empty;
			showServerPanel.BringToFront();
		}
	}

	internal struct ServerConfig 
	{
		public string name;
		public string host;
		public string uid;
		public string pwd;
	}
}
