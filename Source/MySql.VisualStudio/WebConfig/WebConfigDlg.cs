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
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using EnvDTE80;
using EnvDTE;
using MySql.Data.VisualStudio.Properties;
using System.Web.Security;
using System.Configuration.Provider;
using System.Web.Profile;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.Data.Common;

namespace MySql.Data.VisualStudio.WebConfig
{
  public partial class WebConfigDlg : Form
  {
    private string webConfigFileName;
    private DTE2 dte;
    private Solution2 solution;
    private Project project;
    private int page;
    private WizardPage[] pages = new WizardPage[4];

    public WebConfigDlg()
    {
      InitializeComponent();

      dte = MySqlDataProviderPackage.GetGlobalService(
          typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
      solution = (Solution2)dte.Solution;
      FindCurrentWebProject();
      EnsureWebConfig();
      LoadInitialState();
      PageChanged();
    }

    private void FindCurrentWebProject()
    {
      project = (Project)solution.Projects.Item(1);
    }

    private void EnsureWebConfig()
    {
      foreach (ProjectItem items in project.ProjectItems)
      {
        if (!String.Equals(items.Name, "web.config", StringComparison.InvariantCultureIgnoreCase)) continue;
        webConfigFileName = items.get_FileNames(1);
        break;
      }
      if (webConfigFileName == null)
      {
        string template = solution.GetProjectItemTemplate("WebConfig.zip", "Web/CSharp");
        ProjectItem item = project.ProjectItems.AddFromTemplate(template, "web.config");
        webConfigFileName = item.get_FileNames(1);
      }
    }

    private void LoadInitialState()
    {
      WebConfig wc = new WebConfig(webConfigFileName);
      LoadInitialMembershipState();
      LoadInitialRoleState();
      LoadInitialProfileState();
      LoadInitialSessionState();

      foreach (WizardPage page in pages)
        page.ProviderConfig.Initialize(wc);
    }

    private void LoadInitialMembershipState()
    {
      pages[0].Title = "Membership";
      pages[0].Description = "Set options for use with the membership provider";
      pages[0].EnabledString = "Use MySQL to manage my membership records";
      pages[0].ProviderConfig = new MembershipConfig();
    }

    private void LoadInitialRoleState()
    {
      pages[1].Title = "Roles";
      pages[1].Description = "Set options for use with the role provider";
      pages[1].EnabledString = "Use MySQL to manage my roles";
      pages[1].ProviderConfig = new RoleConfig();
    }

    private void LoadInitialProfileState()
    {
      pages[2].Title = "Profiles";
      pages[2].Description = "Set options for use with the profile provider";
      pages[2].EnabledString = "Use MySQL to manage my profiles";
      pages[2].ProviderConfig = new ProfileConfig();
    }

    private void LoadInitialSessionState()
    {
      pages[3].Title = "Session State";
      pages[3].Description = "Set options for use with the session state provider";
      pages[3].EnabledString = "Use MySQL to manage my ASP.Net session state";
      pages[3].ProviderConfig = new SessionStateConfig();      
    }

    private void advancedBtn_Click(object sender, EventArgs e)
    {
      MembershipOptionsDlg dlg = new MembershipOptionsDlg();
      MembershipConfig config = pages[0].ProviderConfig as MembershipConfig;
      dlg.Options = config.MemberOptions;
      DialogResult r = dlg.ShowDialog();
      if (DialogResult.Cancel == r) return;
      config.MemberOptions = dlg.Options;
    }

    private void editConnString_Click(object sender, EventArgs e)
    {
      ConnectionStringEditorDlg dlg = new ConnectionStringEditorDlg();
      try
      {
        dlg.ConnectionString = connectionString.Text;
        if (DialogResult.Cancel == dlg.ShowDialog(this)) return;
        connectionString.Text = dlg.ConnectionString;
      }
      catch (ArgumentException)
      {
        MessageBox.Show(this, Resources.ConnectionStringInvalid, Resources.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void nextButton_Click(object sender, EventArgs e)
    {
      if (!SavePageData()) return;
      if (page == pages.Length - 1)
        Finish();
      else
      {
        page++;
        PageChanged();
      }
    }

    private void backButton_Click(object sender, EventArgs e)
    {
      SavePageData();
      page--;
      PageChanged();
    }

    private bool SavePageData()
    {
      if (useProvider.Checked && connectionString.Text.Trim().Length == 0)
      {
        MessageBox.Show(this, Resources.WebConfigConnStrNoEmpty, Resources.ErrorTitle,
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
      }

      GenericConfig config = pages[page].ProviderConfig;
      Options o = config.GenericOptions;
      o.AppName = appName.Text;
      o.AppDescription = appDescription.Text.Trim();
      o.WriteExceptionToLog = writeExToLog.Checked;
      o.AutoGenSchema = autogenSchema.Checked;
      o.EnableExpireCallback = enableExpCallback.Checked;
      o.ConnectionString = connectionString.Text.Trim();
      config.GenericOptions = o;
      return true;
    }

    private void PageChanged()
    {
      pageLabel.Text = pages[page].Title;
      pageDesc.Text = pages[page].Description;
      useProvider.Text = pages[page].EnabledString;

      GenericConfig config = pages[page].ProviderConfig;
      useProvider.Checked = config.Enabled;
      Options o = config.GenericOptions;
      appName.Text = o.AppName;
      appDescription.Text = o.AppDescription;
      writeExToLog.Checked = o.WriteExceptionToLog;
      autogenSchema.Checked = o.AutoGenSchema;
      enableExpCallback.Checked = o.EnableExpireCallback;
      controlPanel.Enabled = config.Enabled;
      connectionString.Text = o.ConnectionString;

      advancedBtn.Visible = page == 0;
      writeExToLog.Visible = page != 2;
      enableExpCallback.Visible = page == 3;
      nextButton.Text = (page == pages.Length - 1) ? "Finish" : "Next";
      backButton.Enabled = page > 0;
    }

    private void Finish()
    {
      WebConfig w = new WebConfig(webConfigFileName);
      pages[0].ProviderConfig.Save(w);
      pages[1].ProviderConfig.Save(w);
      pages[2].ProviderConfig.Save(w);
      pages[3].ProviderConfig.Save(w);
      w.Save();
      Close();
    }

    private void configPanel_Paint(object sender, PaintEventArgs e)
    {
      Pen darkPen = new Pen(SystemColors.ControlDark);
      Pen lightPen = new Pen(SystemColors.ControlLightLight);
      int left = configPanel.ClientRectangle.Left;
      int right = configPanel.ClientRectangle.Right;
      int top = configPanel.ClientRectangle.Top;
      int bottom = configPanel.ClientRectangle.Bottom - 2;
      e.Graphics.DrawLine(darkPen, left, top, right, top);
      e.Graphics.DrawLine(lightPen, left, top + 1, right, top + 1);
      e.Graphics.DrawLine(darkPen, left, bottom, right, bottom);
      e.Graphics.DrawLine(lightPen, left, bottom + 1, right, bottom + 1);
    }

    private void useProvider_CheckStateChanged(object sender, EventArgs e)
    {
      GenericConfig config = pages[page].ProviderConfig;
      config.Enabled = useProvider.Checked;
      controlPanel.Enabled = config.Enabled;
    }

  }

  internal struct WizardPage
  {
    public string Title;
    public string Description;
    public string EnabledString;
    public GenericConfig ProviderConfig;
  }
}
