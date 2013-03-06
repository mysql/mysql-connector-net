// Copyright © 2008, 2010, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.Common;

namespace MySql.Data.VisualStudio
{
  public partial class ConnectDialog : Form
  {
    private int bigSize = 522;
    private int smallSize = 312;
    private DbProviderFactory factory;
    private DbConnectionStringBuilder connectionStringBuilder;
    private bool populated = false;

    public ConnectDialog()
    {
      InitializeComponent();
      factory = DbProviderFactories.GetFactory("MySql.Data.MySqlClient");
      if (factory == null)
        throw new Exception("MySql Data Provider is not correctly registered");
      connectionStringBuilder = factory.CreateConnectionStringBuilder();
      connectionProperties.SelectedObject = connectionStringBuilder;
    }

    public DbConnection Connection
    {
      get
      {
        DbConnection c = factory.CreateConnection();
        c.ConnectionString = connectionStringBuilder.ConnectionString;
        c.Open();
        return c;
      }
      set
      {
        if (value != null)
        {
          connectionStringBuilder.ConnectionString = value.ConnectionString;
          Rebind();
        }
      }
    }

    private void advancedButton_Click(object sender, EventArgs e)
    {
      this.SuspendLayout();
      if (this.Size.Height > 400)
      {
        advancedButton.Text = "Advanced >>";
        Height = smallSize;
        simplePanel.Visible = true;
        connectionProperties.Visible = false;
        Rebind();
      }
      else
      {
        advancedButton.Text = "Simple <<";
        Height = bigSize;
        simplePanel.Visible = false;
        connectionProperties.Visible = true;
      }
      this.ResumeLayout();
    }

    private void Rebind()
    {
      serverName.Text = connectionStringBuilder["server"] as string;
      userId.Text = connectionStringBuilder["userid"] as string;
      password.Text = connectionStringBuilder["password"] as string;
      database.Text = connectionStringBuilder["database"] as string;
    }

    private void database_DropDown(object sender, EventArgs e)
    {
      if (populated) return;
      populated = true;
      try
      {
        using (DbConnection c = factory.CreateConnection())
        {
          c.ConnectionString = connectionStringBuilder.ConnectionString;
          c.Open();
          DbCommand cmd = c.CreateCommand();
          cmd.CommandText = "SHOW DATABASES";
          using (DbDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
              database.Items.Add(reader.GetString(0));
          }
        }
      }
      catch (Exception)
      {
      }
    }

    private void serverName_Leave(object sender, EventArgs e)
    {
      connectionStringBuilder["server"] = serverName.Text.Trim();
    }

    private void userId_Leave(object sender, EventArgs e)
    {
      connectionStringBuilder["userid"] = userId.Text.Trim();
    }

    private void password_Leave(object sender, EventArgs e)
    {
      connectionStringBuilder["password"] = password.Text.Trim();
    }

    private void database_Leave(object sender, EventArgs e)
    {
      connectionStringBuilder["database"] = database.Text.Trim();
    }

    private void connectButton_Click(object sender, EventArgs e)
    {
      // Ensure all data is populated into the connection string builder
      serverName_Leave(serverName, EventArgs.Empty);
      userId_Leave(serverName, EventArgs.Empty);
      password_Leave(serverName, EventArgs.Empty);
      database_Leave(serverName, EventArgs.Empty);
    }

  }
}
