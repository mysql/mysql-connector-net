// Copyright © 2009, 2010, Oracle and/or its affiliates. All rights reserved.
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
using System.Windows.Forms;
using System.Web.Security;

namespace MySql.Data.VisualStudio.WebConfig
{
  public partial class MembershipOptionsDlg : Form
  {
    public MembershipOptionsDlg()
    {
      InitializeComponent();
      passwordFormat.DataSource = Enum.GetValues(typeof(MembershipPasswordFormat));
    }

    internal MembershipOptions Options
    {
      get
      {
        MembershipOptions options = new MembershipOptions();
        options.EnablePasswordReset = enablePasswordReset.Checked;
        options.EnablePasswordRetrieval = enablePasswordRetrieval.Checked;
        options.MaxInvalidPasswordAttempts = (int)maxInvalidPassAttempts.Value;
        options.MinRequiredNonAlphaNumericCharacters = (int)minRequiredNonAlpha.Value;
        options.MinRequiredPasswordLength = (int)minPassLength.Value;
        options.PasswordAttemptWindow = (int)passwordAttemptWindow.Value;
        options.PasswordFormat = (MembershipPasswordFormat)passwordFormat.SelectedItem;
        options.PasswordStrengthRegEx = passwordRegex.Text.Trim();
        options.RequiresQA = requireQA.Checked;
        options.RequiresUniqueEmail = requireUniqueEmail.Checked;
        return options;
      }
      set
      {
        enablePasswordReset.Checked = value.EnablePasswordReset;
        enablePasswordRetrieval.Checked = value.EnablePasswordRetrieval;
        maxInvalidPassAttempts.Value = value.MaxInvalidPasswordAttempts;
        minRequiredNonAlpha.Value = value.MinRequiredNonAlphaNumericCharacters;
        minPassLength.Value = value.MinRequiredPasswordLength;
        passwordAttemptWindow.Value = value.PasswordAttemptWindow;
        passwordFormat.SelectedItem = value.PasswordFormat;
        passwordRegex.Text = value.PasswordStrengthRegEx;
        requireQA.Checked = value.RequiresQA;
        requireUniqueEmail.Checked = value.RequiresUniqueEmail;
      }
    }
  }
}
