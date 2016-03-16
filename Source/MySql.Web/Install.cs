// Copyright © 2004, 2016, Oracle and/or its affiliates. All rights reserved.
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

#if !MONO && !PocketPC
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Collections;
using System.IO;
using Microsoft.Win32;
using System.Xml;
using System.Reflection;
using System.Security.Permissions;

namespace MySql.Web.Security
{
  [RunInstaller(true)]
  [PermissionSetAttribute(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  [PermissionSetAttribute(SecurityAction.LinkDemand, Name = "FullTrust")]
  public class CustomInstaller : Installer
  {
    /// <summary>
    /// When overridden in a derived class, performs the installation.
    /// </summary>
    /// <param name="stateSaver">An <see cref="T:System.Collections.IDictionary"/> used to save information needed to perform a commit, rollback, or uninstall operation.</param>
    /// <exception cref="T:System.ArgumentException">
    /// The <paramref name="stateSaver"/> parameter is null.
    /// </exception>
    /// <exception cref="T:System.Exception">
    /// An exception occurred in the <see cref="E:System.Configuration.Install.Installer.BeforeInstall"/> event handler of one of the installers in the collection.
    /// -or-
    /// An exception occurred in the <see cref="E:System.Configuration.Install.Installer.AfterInstall"/> event handler of one of the installers in the collection.
    /// </exception>
    public override void Install(IDictionary stateSaver)
    {
      base.Install(stateSaver);
      AddProviderToMachineConfig();
    }

    /// <summary>
    /// When overridden in a derived class, removes an installation.
    /// </summary>
    /// <param name="savedState">An <see cref="T:System.Collections.IDictionary"/> that contains the state of the computer after the installation was complete.</param>
    /// <exception cref="T:System.ArgumentException">
    /// The saved-state <see cref="T:System.Collections.IDictionary"/> might have been corrupted.
    /// </exception>
    /// <exception cref="T:System.Configuration.Install.InstallException">
    /// An exception occurred while uninstalling. This exception is ignored and the uninstall continues. However, the application might not be fully uninstalled after the uninstallation completes.
    /// </exception>
    public override void Uninstall(IDictionary savedState)
    {
      base.Uninstall(savedState);
      RemoveProviderFromMachineConfig();
    }

    private void AddProviderToMachineConfig()
    {
      object installRoot = Registry.GetValue(
          @"HKEY_LOCAL_MACHINE\Software\Microsoft\.NETFramework\",
          "InstallRoot", null);
      if (installRoot == null)
        throw new Exception("Unable to retrieve install root for .NET framework");
      UpdateMachineConfigs(installRoot.ToString(), true);

      string installRoot64 = installRoot.ToString();
      installRoot64 = installRoot64.Substring(0, installRoot64.Length - 1);
      installRoot64 = string.Format("{0}64{1}", installRoot64,
          Path.DirectorySeparatorChar);
      if (Directory.Exists(installRoot64))
        UpdateMachineConfigs(installRoot64, true);
    }

    private void UpdateMachineConfigs(string rootPath, bool add)
    {
      string[] dirs = new string[2] { "v2.0.50727", "v4.0.30319" };
      foreach (string frameworkDir in dirs)
      {
        string path = rootPath + frameworkDir;

        string configPath = String.Format(@"{0}\CONFIG", path);
        if (Directory.Exists(configPath))
        {
          if (add)
            AddProviderToMachineConfigInDir(configPath);
          else
            RemoveProviderFromMachineConfigInDir(configPath);
        }
      }
    }

    private void AddProviderToMachineConfigInDir(string path)
    {
      string configFile = String.Format(@"{0}\machine.config", path);
      if (!File.Exists(configFile)) return;

      // now read the config file into memory
      StreamReader sr = new StreamReader(configFile);
      string configXML = sr.ReadToEnd();
      sr.Close();

      // load the XML into the XmlDocument
      XmlDocument doc = new XmlDocument();
      doc.LoadXml(configXML);

      AddDefaultConnectionString(doc);
      AddMembershipProvider(doc);
      AddRoleProvider(doc);
      AddProfileProvider(doc);
      AddSiteMapProvider(doc);
	  AddPersonalizationProvider(doc);

      // Save the document to a file and auto-indent the output.
      XmlTextWriter writer = new XmlTextWriter(configFile, null);
      writer.Formatting = Formatting.Indented;
      doc.Save(writer);
      writer.Flush();
      writer.Close();
    }

    private void AddDefaultConnectionString(XmlDocument doc)
    {
      // create our new node
      XmlElement newNode = (XmlElement)doc.CreateNode(XmlNodeType.Element, "add", "");

      // add the proper attributes
      newNode.SetAttribute("name", "LocalMySqlServer");
      newNode.SetAttribute("connectionString", "");

      XmlNodeList nodes = doc.GetElementsByTagName("connectionStrings");
      XmlNode connectionStringList = nodes[0];

      bool alreadyThere = false;
      foreach (XmlNode node in connectionStringList.ChildNodes)
      {
        string nameValue = node.Attributes["name"].Value;
        if (nameValue == "LocalMySqlServer")
        {
          alreadyThere = true;
          break;
        }
      }

      if (!alreadyThere)
        connectionStringList.AppendChild(newNode);
    }

    private void AddMembershipProvider(XmlDocument doc)
    {
      // create our new node
      XmlElement newNode = (XmlElement)doc.CreateNode(XmlNodeType.Element, "add", "");

      // add the proper attributes
      newNode.SetAttribute("name", "MySQLMembershipProvider");

      // add the type attribute by reflecting on the executing assembly
      Assembly a = Assembly.GetExecutingAssembly();
      string type = String.Format("MySql.Web.Security.MySQLMembershipProvider, {0}",
          a.FullName.Replace("Installers", "Web").Replace(".NET40", string.Empty));
      newNode.SetAttribute("type", type);

      newNode.SetAttribute("connectionStringName", "LocalMySqlServer");
      newNode.SetAttribute("enablePasswordRetrieval", "false");
      newNode.SetAttribute("enablePasswordReset", "true");
      newNode.SetAttribute("requiresQuestionAndAnswer", "true");
      newNode.SetAttribute("applicationName", "/");
      newNode.SetAttribute("requiresUniqueEmail", "false");
      newNode.SetAttribute("passwordFormat", "Clear");
      newNode.SetAttribute("maxInvalidPasswordAttempts", "5");
      newNode.SetAttribute("minRequiredPasswordLength", "7");
      newNode.SetAttribute("minRequiredNonalphanumericCharacters", "1");
      newNode.SetAttribute("passwordAttemptWindow", "10");
      newNode.SetAttribute("passwordStrengthRegularExpression", "");

      XmlNodeList nodes = doc.GetElementsByTagName("membership");
      XmlNode providerList = nodes[0].FirstChild;

      foreach (XmlNode node in providerList.ChildNodes)
      {
        string typeValue = node.Attributes["type"].Value;
        if (typeValue.StartsWith("MySql.Web.Security.MySQLMembershipProvider", StringComparison.OrdinalIgnoreCase))
        {
          providerList.RemoveChild(node);
          break;
        }
      }

      providerList.AppendChild(newNode);
    }

    private void AddRoleProvider(XmlDocument doc)
    {
      // create our new node
      XmlElement newNode = (XmlElement)doc.CreateNode(XmlNodeType.Element, "add", "");

      // add the proper attributes
      newNode.SetAttribute("name", "MySQLRoleProvider");

      // add the type attribute by reflecting on the executing assembly
      Assembly a = Assembly.GetExecutingAssembly();
      string type = String.Format("MySql.Web.Security.MySQLRoleProvider, {0}",
          a.FullName.Replace("Installers", "Web").Replace(".NET40", string.Empty));
      newNode.SetAttribute("type", type);

      newNode.SetAttribute("connectionStringName", "LocalMySqlServer");
      newNode.SetAttribute("applicationName", "/");

      XmlNodeList nodes = doc.GetElementsByTagName("roleManager");
      XmlNode providerList = nodes[0].FirstChild;

      foreach (XmlNode node in providerList.ChildNodes)
      {
        string typeValue = node.Attributes["type"].Value;
        if (typeValue.StartsWith("MySql.Web.Security.MySQLRoleProvider", StringComparison.OrdinalIgnoreCase))
        {
          providerList.RemoveChild(node);
          break;
        }
      }

      providerList.AppendChild(newNode);
    }

    private void AddProfileProvider(XmlDocument doc)
    {
      // create our new node
      XmlElement newNode = (XmlElement)doc.CreateNode(XmlNodeType.Element, "add", "");

      // add the proper attributes
      newNode.SetAttribute("name", "MySQLProfileProvider");

      // add the type attribute by reflecting on the executing assembly
      Assembly a = Assembly.GetExecutingAssembly();
      string type = String.Format("MySql.Web.Profile.MySQLProfileProvider, {0}",
          a.FullName.Replace("Installers", "Web").Replace(".NET40", string.Empty));
      newNode.SetAttribute("type", type);

      newNode.SetAttribute("connectionStringName", "LocalMySqlServer");
      newNode.SetAttribute("applicationName", "/");

      XmlNodeList nodes = doc.GetElementsByTagName("profile");
      XmlNode providerList = nodes[0].FirstChild;

      foreach (XmlNode node in providerList.ChildNodes)
      {
        string typeValue = node.Attributes["type"].Value;
        if (typeValue.StartsWith("MySql.Web.Profile.MySQLProfileProvider", StringComparison.OrdinalIgnoreCase))
        {
          providerList.RemoveChild(node);
          break;
        }
      }

      providerList.AppendChild(newNode);
    }

    private void AddSiteMapProvider(XmlDocument doc)
    {
      // create our new node
      XmlElement newNode = (XmlElement)doc.CreateNode(XmlNodeType.Element, "add", "");

      // add the proper attributes
      newNode.SetAttribute("name", "MySqlSiteMapProvider");

      // add the type attribute by reflecting on the executing assembly
      Assembly a = Assembly.GetExecutingAssembly();
      string type = String.Format("MySql.Web.SiteMap.MySqlSiteMapProvider, {0}",
          a.FullName.Replace("Installers", "Web").Replace(".NET40", string.Empty));
      newNode.SetAttribute("type", type);

      newNode.SetAttribute("connectionStringName", "LocalMySqlServer");
      newNode.SetAttribute("applicationName", "/");

      XmlNodeList nodes = doc.GetElementsByTagName("siteMap");

      // It may not exists initially
      if (nodes == null || nodes.Count == 0)
      {
        XmlNodeList nodesRoot = doc.GetElementsByTagName("system.web");
        XmlElement node = (XmlElement)doc.CreateNode(XmlNodeType.Element, "siteMap", "");
        nodesRoot[0].AppendChild(node);
      }
      if(nodes[0].ChildNodes.Count == 0)
      {
        XmlElement node2 = (XmlElement)doc.CreateNode(XmlNodeType.Element, "providers", "");
        nodes[0].AppendChild(node2);
      }
      XmlNode providerList = nodes[0].FirstChild;

      foreach (XmlNode node in providerList.ChildNodes)
      {
        string typeValue = node.Attributes["type"].Value;
        if (typeValue.StartsWith("MySql.Web.SiteMap.MySqlSiteMapProvider", StringComparison.OrdinalIgnoreCase))
        {
          providerList.RemoveChild(node);
          break;
        }
      }
      providerList.AppendChild(newNode);
    }

	
    private void AddPersonalizationProvider(XmlDocument doc)
    {
      XmlNode webpartNode = doc.GetElementsByTagName("webParts")[0];

      //check if webpart node exists
      if (webpartNode == null)
      {
        webpartNode = doc.CreateNode(XmlNodeType.Element, "webParts", "");
        doc.GetElementsByTagName("system.web")[0].AppendChild(webpartNode);
      }
      if (webpartNode.ChildNodes.Count == 0)
      {
        var personalizationNode = doc.CreateNode(XmlNodeType.Element, "personalization", "");
        webpartNode.AppendChild(personalizationNode);
      }
      if (webpartNode.FirstChild.ChildNodes.Count == 0)
      {
        webpartNode.FirstChild.AppendChild(doc.CreateNode(XmlNodeType.Element, "providers", ""));
      }
      
      
      // create our new node
      XmlElement newNode = (XmlElement)doc.CreateNode(XmlNodeType.Element, "add", "");

      // add the proper attributes
      newNode.SetAttribute("name", "MySQLPersonalizationProvider");

      // add the type attribute by reflecting on the executing assembly
      Assembly a = Assembly.GetExecutingAssembly();
      string type = String.Format("MySql.Web.Personalization.MySqlPersonalizationProvider, {0}",
          a.FullName.Replace("Installers", "Web").Replace(".NET40", string.Empty));
      newNode.SetAttribute("type", type);

      newNode.SetAttribute("connectionStringName", "LocalMySqlServer");
      newNode.SetAttribute("applicationName", "/");

      XmlNode providerPersonalizationList = webpartNode.FirstChild.FirstChild;

      if (providerPersonalizationList!= null && providerPersonalizationList.ChildNodes!= null)
      {
        foreach (XmlNode node in providerPersonalizationList.ChildNodes)
        {
          string typeValue = node.Attributes["type"].Value;
          if (typeValue.StartsWith("MySql.Web.Personalization.MySqlPersonalizationProvider", StringComparison.OrdinalIgnoreCase))
          {
            providerPersonalizationList.RemoveChild(node);
            break;
          }
        }
      }

      providerPersonalizationList.AppendChild(newNode);
    }

		
    private void RemoveProviderFromMachineConfig()
    {
      object installRoot = Registry.GetValue(
          @"HKEY_LOCAL_MACHINE\Software\Microsoft\.NETFramework\",
          "InstallRoot", null);
      if (installRoot == null)
        throw new Exception("Unable to retrieve install root for .NET framework");
      UpdateMachineConfigs(installRoot.ToString(), false);

      string installRoot64 = installRoot.ToString();
      installRoot64 = installRoot64.Substring(0, installRoot64.Length - 1);
      installRoot64 = string.Format("{0}64{1}", installRoot64,
          Path.DirectorySeparatorChar);
      if (Directory.Exists(installRoot64))
        UpdateMachineConfigs(installRoot64, false);
    }

    private void RemoveProviderFromMachineConfigInDir(string path)
    {
      string configFile = String.Format(@"{0}\machine.config", path);
      if (!File.Exists(configFile)) return;

      // now read the config file into memory
      StreamReader sr = new StreamReader(configFile);
      string configXML = sr.ReadToEnd();
      sr.Close();

      // load the XML into the XmlDocument
      XmlDocument doc = new XmlDocument();
      doc.LoadXml(configXML);

      RemoveDefaultConnectionString(doc);
      RemoveMembershipProvider(doc);
      RemoveRoleProvider(doc);
      RemoveProfileProvider(doc);
      RemoveSiteMapProvider(doc);
	    RemovePersonalizationProvider(doc);

      // Save the document to a file and auto-indent the output.
      XmlTextWriter writer = new XmlTextWriter(configFile, null);
      writer.Formatting = Formatting.Indented;
      doc.Save(writer);
      writer.Flush();
      writer.Close();
    }

    private void RemoveDefaultConnectionString(XmlDocument doc)
    {
      XmlNodeList nodes = doc.GetElementsByTagName("connectionStrings");
      if (nodes.Count == 0) return;
      XmlNode connectionStringList = nodes[0];
      foreach (XmlNode node in connectionStringList.ChildNodes)
      {
        string name = node.Attributes["name"].Value;
        if (name == "LocalMySqlServer")
        {
          connectionStringList.RemoveChild(node);
          break;
        }
      }
    }

    private void RemoveMembershipProvider(XmlDocument doc)
    {
      XmlNodeList nodes = doc.GetElementsByTagName("membership");
      if (nodes.Count == 0) return;
      XmlNode providersNode = nodes[0].FirstChild;
      foreach (XmlNode node in providersNode.ChildNodes)
      {
        string name = node.Attributes["name"].Value;
        if (name == "MySQLMembershipProvider")
        {
          providersNode.RemoveChild(node);
          break;
        }
      }
    }

    private void RemoveRoleProvider(XmlDocument doc)
    {
      XmlNodeList nodes = doc.GetElementsByTagName("roleManager");
      if (nodes.Count == 0) return;
      XmlNode providersNode = nodes[0].FirstChild;
      foreach (XmlNode node in providersNode.ChildNodes)
      {
        string name = node.Attributes["name"].Value;
        if (name == "MySQLRoleProvider")
        {
          providersNode.RemoveChild(node);
          break;
        }
      }
    }

    private void RemoveProfileProvider(XmlDocument doc)
    {
      XmlNodeList nodes = doc.GetElementsByTagName("profile");
      if (nodes.Count == 0) return;
      XmlNode providersNode = nodes[0].FirstChild;
      foreach (XmlNode node in providersNode.ChildNodes)
      {
        string name = node.Attributes["name"].Value;
        if (name == "MySQLProfileProvider")
        {
          providersNode.RemoveChild(node);
          break;
        }
      }
    }

    private void RemoveSiteMapProvider(XmlDocument doc)
    {
      XmlNodeList nodes = doc.GetElementsByTagName("siteMap");
      if (nodes.Count == 0 || nodes[0].FirstChild == null) return;
      XmlNode providersNode = nodes[0].FirstChild;
      foreach (XmlNode node in providersNode.ChildNodes)
      {
        string name = node.Attributes["name"].Value;
        if ( string.Compare( name, "MySqlSiteMapProvider", StringComparison.OrdinalIgnoreCase ) == 0 )
        {
          providersNode.RemoveChild(node);
          break;
        }
      }
      if (providersNode.ChildNodes.Count == 0 
        && nodes[0].ChildNodes.Count == 1)
      {
        nodes[0].ParentNode.RemoveChild(nodes[0]);
      }
    }
	
	private void RemovePersonalizationProvider(XmlDocument doc)
    {
      XmlNodeList nodes = doc.GetElementsByTagName("personalization");
      if (nodes.Count == 0 || nodes[0].FirstChild == null) return;
      XmlNode providersNode = nodes[0].FirstChild;
      foreach (XmlNode node in providersNode.ChildNodes)
      {
        string name = node.Attributes["name"].Value;
        if (name == "MySQLPersonalizationProvider")
        {
          providersNode.RemoveChild(node);
          break;
        }
      }
      if (providersNode.ChildNodes.Count == 0
        && nodes[0].ChildNodes.Count == 1
        && nodes[0].ParentNode.ChildNodes.Count == 1)
      {
        nodes[0].ParentNode.ParentNode.RemoveChild(nodes[0].ParentNode);
      }
    }
  }
}

#endif