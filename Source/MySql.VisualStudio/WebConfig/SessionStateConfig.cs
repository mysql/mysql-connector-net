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
using System.Configuration;
using System.Web.Configuration;
using System.Xml;

namespace MySql.Data.VisualStudio.WebConfig
{
  internal class SessionStateConfig : GenericConfig
  {
    public SessionStateConfig()
      : base()
    {
      typeName = "MySqlSessionStateStoreProvider";
      sectionName = "sessionState";

      Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
      MembershipSection section = (MembershipSection)machineConfig.SectionGroups["system.web"].Sections["membership"];
      foreach (ProviderSettings p in section.Providers)
        if (p.Type.Contains("MySql"))
          ProviderType = p.Type;
      ProviderType = ProviderType.Replace("MySql.Web.Security.MySQLMembershipProvider",
          "MySql.Web.SessionState.MySqlSessionStateStore");
    }

    public override void GetDefaults()
    {
      defaults.ProviderName = "MySqlSessionStateProvider";
      defaults.WriteExceptionToLog = false;
      defaults.ConnectionStringName = "LocalMySqlServer";
      defaults.AutoGenSchema = false;
      defaults.AppName = "/";
    }

    protected override ProviderSettings GetMachineSettings()
    {
      Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
      SessionStateSection section = (SessionStateSection)machineConfig.SectionGroups["system.web"].Sections[sectionName];
      foreach (ProviderSettings p in section.Providers)
        if (p.Type.Contains(typeName)) return p;
      return null;
    }

    public override void Initialize(WebConfig wc)
    {
      GetDefaults();
      values = defaults;

      // get the default provider
      XmlElement e = wc.GetProviderSection(sectionName);
      if (e != null)
      {
        string mode = e.GetAttribute("mode");
        if (String.Compare(mode, "custom", true) == 0)
          DefaultProvider = e.GetAttribute("customProvider");
      }

      e = wc.GetProviderElement(sectionName);
      if (e != null)
      {
        values.ProviderName = e.GetAttribute("name");
        if (e.HasAttribute("connectionStringName"))
          values.ConnectionStringName = e.GetAttribute("connectionStringName");
        if (e.HasAttribute("description"))
          values.AppDescription = e.GetAttribute("description");
        if (e.HasAttribute("applicationName"))
          values.AppName = e.GetAttribute("applicationName");
        if (e.HasAttribute("writeExceptionsToEventLog"))
          values.WriteExceptionToLog = GetBoolValue(e.GetAttribute("writeExceptionsToEventLog"), false);
        if (e.HasAttribute("autogenerateschema"))
          values.AutoGenSchema = GetBoolValue(e.GetAttribute("autogenerateschema"), false);
        if (e.HasAttribute("enableExpireCallback"))
          values.EnableExpireCallback = GetBoolValue(e.GetAttribute("enableExpireCallback"), false);  
      }
      values.ConnectionString = wc.GetConnectionString(values.ConnectionStringName);
      Enabled = OriginallyEnabled = DefaultProvider != null &&
          (DefaultProvider == values.ProviderName ||
          DefaultProvider == defaults.ProviderName);
    }

    public override void Save(WebConfig wc)
    {
      if (OriginallyEnabled)
      {
        XmlElement e = wc.GetProviderSection(sectionName);
        e.ParentNode.RemoveChild(e);
      }

      if (!Enabled) return;

      // we need to save our connection strings even if we are using the default
      // provider definition
      wc.SaveConnectionString(defaults.ConnectionStringName, values.ConnectionStringName,
          values.ConnectionString);
      // we do this so our equality comparison that follows can work
      defaults.ConnectionString = values.ConnectionString;

      XmlElement provider = wc.AddProvider(sectionName, null, values.ProviderName);
      SaveProvider(provider);

      // we are enabled so we want to set our defaultProvider attribute
      XmlElement sessionNode = (XmlElement)provider.ParentNode.ParentNode;
      sessionNode.SetAttribute("mode", "Custom");
      sessionNode.SetAttribute("cookieless", "true");
      sessionNode.SetAttribute("regenerateExpiredSessionId", "true");
      sessionNode.SetAttribute("customProvider", values.ProviderName);
    }
  }
}
