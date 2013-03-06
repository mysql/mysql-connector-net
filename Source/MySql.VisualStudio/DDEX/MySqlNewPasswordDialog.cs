// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace MySql.Data.VisualStudio
{
  public partial class MySqlNewPasswordDialog : Form
  {
    MySqlConnection _connection;

    public MySqlNewPasswordDialog(MySqlConnection connection)
    {
      _connection = connection;
      InitializeComponent();
    }

    private void btnOk_Click(object sender, EventArgs e)
    {
      try
      {
        errorProvider1.Clear();
        if (txtPassword.Text != txtConfirm.Text)
        {
          errorProvider1.SetError(txtConfirm, Properties.Resources.NewPassword_PasswordNotMatch);
          return;
        }
        if (string.IsNullOrEmpty(txtPassword.Text))
        {
          errorProvider1.SetError(txtPassword, Properties.Resources.NewPassword_ProvideNewPassword);
          return;
        }

        MySqlCommand cmd = new MySqlCommand(string.Format("SET PASSWORD = PASSWORD('{0}')", txtPassword.Text), _connection);
        cmd.ExecuteNonQuery();
        _connection.Close();
        _connection.Open();
        this.Close();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Error");
      }
    }
    

    private void btnCancel_Click(object sender, EventArgs e)
    {

    }
  }
}
