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

/*
 * This file contains implementation of custom connection dialog. 
 */
using System;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.VisualStudio.Data.AdoDotNet;
using MySql.Data.VisualStudio.Properties;
using System.Data.Common;
using Microsoft.VisualStudio.Data;

namespace MySql.Data.VisualStudio
{
  /// <summary>
  /// Represents a custom data connection UI control for entering
  /// connection information.
  /// </summary>
  internal partial class MySqlDataConnectionUI : DataConnectionUIControl //Stub
  {
    private bool dbListPopulated;

    public MySqlDataConnectionUI()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Initializes GUI elements with properties values.
    /// </summary>
    public override void LoadProperties()
    {
      if (ConnectionProperties == null)
        throw new Exception(Resources.ConnectionPropertiesNull);

      Button okButton = this.ParentForm.AcceptButton as Button;
      okButton.Click += new EventHandler(okButton_Click);

      AdoDotNetConnectionProperties prop =
                (ConnectionProperties as AdoDotNetConnectionProperties);
      DbConnectionStringBuilder cb = prop.ConnectionStringBuilder;

      loadingInProcess = true;
      try
      {
        serverNameTextBox.Text = (string)cb["Server"];
        userNameTextBox.Text = (string)cb["User Id"];
        passwordTextBox.Text = (string)cb["Password"];
        dbList.Text = (string)cb["Database"];
        savePasswordCheckBox.Checked = (bool)cb["Persist Security Info"];
      }
      finally
      {
        loadingInProcess = false;
      }
    }

    /// <summary>
    /// We hook the ok button of our parent form so we can implement our 
    /// 'create database' functionality
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void okButton_Click(object sender, EventArgs e)
    {
      bool exists = DatabaseExists();
      if (exists) return;

      String prompt = String.Format(Resources.UnknownDbPromptCreate, dbList.Text);
      prompt = prompt.Replace(@"\n", @"\n");
      DialogResult result = MessageBox.Show(prompt, null, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
      this.ParentForm.DialogResult = DialogResult.None;
      if (result == DialogResult.Yes)
      {
        if (!AttemptToCreateDatabase())
          MessageBox.Show(String.Format(Resources.ErrorAttemptingToCreateDB, dbList.Text));
        else
          this.ParentForm.DialogResult = DialogResult.OK;
      }
    }

    /// <summary>
    /// Handles TextChanged event from all textboxes.
    /// </summary>
    /// <param name="sender">Reference to sender object.</param>
    /// <param name="e">Additional event data. Not used.</param>
    private void SetProperty(object sender, EventArgs e)
    {
      // Only set properties if we are not currently loading them
      if (!loadingInProcess)
      {
        Control source = sender as Control;

        // if the user changes the host or user id then
        // we need to repopulate our db list
        if (source.Tag.Equals("Server") ||
            source.Tag.Equals("User id"))
          dbListPopulated = false;

        // Tag is used to determine property name
        if (source != null && source.Tag is string)
          ConnectionProperties[source.Tag as string] = source.Text;
      }
      dbList.Enabled = serverNameTextBox.Text.Trim().Length > 0 &&
          userNameTextBox.Text.Trim().Length > 0;
    }

    /// <summary>
    /// Handles Leave event and trims text.
    /// </summary>
    /// <param name="sender">Reference to sender object.</param>
    /// <param name="e">Additional event data. Not used.</param>
    private void TrimControlText(object sender, EventArgs e)
    {
      Control c = sender as Control;
      c.Text = c.Text.Trim();
    }

    /// <summary>
    /// Used to prevent OnChange event handling during initialization.
    /// </summary>
    private bool loadingInProcess = false;

    private void SavePasswordChanged(object sender, EventArgs e)
    {
      // Only set properties if we are not currently loading them
      if (!loadingInProcess)
        ConnectionProperties[savePasswordCheckBox.Tag as string]
            = savePasswordCheckBox.Checked;
    }

    private void dbList_DropDown(object sender, EventArgs e)
    {
      if (dbListPopulated) return;

      AdoDotNetConnectionProperties prop =
          (ConnectionProperties as AdoDotNetConnectionProperties);
      DbConnectionStringBuilder cb = prop.ConnectionStringBuilder;

      try
      {
        using (MySqlConnectionSupport conn = new MySqlConnectionSupport())
        {
          conn.Initialize(null);
          conn.ConnectionString = cb.ConnectionString;
          conn.Open(false);
          dbList.Items.Clear();
          using (DataReader reader = conn.Execute("SHOW DATABASES", 1, null, 0))
          {
            while (reader.Read())
            {
              string dbName = reader.GetItem(0).ToString().ToLowerInvariant();
              if (dbName == "information_schema") continue;
              if (dbName == "mysql") continue;
              dbList.Items.Add(reader.GetItem(0));
            }
            dbListPopulated = true;
          }
        }
      }
      catch (Exception)
      {
        MessageBox.Show(Resources.UnableToRetrieveDatabaseList);
      }
    }

    private bool DatabaseExists()
    {
      AdoDotNetConnectionProperties prop =
          (ConnectionProperties as AdoDotNetConnectionProperties);
      DbConnectionStringBuilder cb = prop.ConnectionStringBuilder;

      try
      {
        using (MySqlConnectionSupport conn = new MySqlConnectionSupport())
        {
          conn.Initialize(null);
          conn.ConnectionString = cb.ConnectionString;
          conn.Open(false);
        }
        return true;
      }
      catch (DbException ex)
      {
        string msg = ex.Message.ToLowerInvariant();
        if (msg.StartsWith("unknown database", StringComparison.OrdinalIgnoreCase)) return false;
        throw;
      }
    }

    private bool AttemptToCreateDatabase()
    {
      AdoDotNetConnectionProperties prop =
          (ConnectionProperties as AdoDotNetConnectionProperties);
      DbConnectionStringBuilder cb = prop.ConnectionStringBuilder;

      string olddb = (string)cb["Database"];
      cb["Database"] = "";
      try
      {
        using (MySqlConnectionSupport conn = new MySqlConnectionSupport())
        {
          conn.Initialize(null);
          conn.ConnectionString = cb.ConnectionString;
          conn.Open(false);
          conn.ExecuteWithoutResults("CREATE DATABASE `" + dbList.Text + "`", 1, null, 0);
        }
        return true;
      }
      catch (Exception)
      {
        MessageBox.Show(String.Format(Resources.ErrorAttemptingToCreateDB, dbList.Text));
        return false;
      }
      finally
      {
        cb["Database"] = olddb;
      }
    }
  }
}
