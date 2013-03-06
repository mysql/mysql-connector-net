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
using System.Xml;
using System.Configuration;
using System.Web.Configuration;
using System.Web.Security;
using System.Diagnostics;
using System.Web.Profile;
using System.Collections.Generic;

namespace MySql.Data.VisualStudio.WebConfig
{
  internal class WebConfig
  {
    private string webConfigFile;
    private XmlDocument webDoc;

    public WebConfig(string filename)
    {
      webConfigFile = filename;
      Initialize();
    }

    private void Initialize()
    {
      if (webDoc == null && System.IO.File.Exists(webConfigFile))
      {
        webDoc = new XmlDocument();
        webDoc.Load(webConfigFile);
      }
    }

    public void Save()
    {
      webDoc.Save(webConfigFile);
    }

    public XmlElement GetProviderSection(string type)
    {
      if (webDoc == null) return null;
      XmlNodeList nodes = webDoc.GetElementsByTagName(type);
      if (nodes.Count == 0) return null;
      return nodes[0] as XmlElement;
    }

    public XmlElement GetProviderElement(string type)
    {
      XmlNode el = GetSystemWebNode(type, false, false);
      if (el == null || el.FirstChild == null) return null;
      el = el.FirstChild;  // move to the <providers> element
      if (el.ChildNodes.Count == 0) return null;

      foreach (XmlNode node in el.ChildNodes)
      {
        if (String.Compare(node.Name, "remove", true) == 0 ||
            String.Compare(node.Name, "clear", true) == 0) continue;
        string typeName = node.Attributes["type"].Value;
        if (typeName.StartsWith("MySql.Web.", StringComparison.OrdinalIgnoreCase)) return node as XmlElement;
      }

      return null;
    }

    public XmlElement GetListItem(string topNode, string nodeName, string itemName)
    {
      Debug.Assert(webDoc != null);
      XmlNodeList nodes = webDoc.GetElementsByTagName(topNode);
      if (nodes.Count == 0) return null;

      // nodeName == null just means return the top node
      XmlNode node = nodes[0];
      if (nodeName == null)
        return node as XmlElement;

      // we are looking for something lower but there is nothing here
      if (node.ChildNodes.Count == 0) return null;

      // if we are looking in a provider list, then step over the providers element
      if (node.FirstChild.Name == "providers")
        node = node.FirstChild;

      foreach (XmlNode child in node.ChildNodes)
      {
        if (child.Name != nodeName) continue;
        if (String.Compare(child.Attributes["name"].Value, itemName, true) == 0)
          return child as XmlElement;
      }
      return null;
    }

    public string GetConnectionString(string name)
    {
      XmlElement el = GetListItem("connectionStrings", "add", name);
      if (el == null) return null;
      return el.Attributes["connectionString"].Value;
    }

    public void SaveConnectionString(string defaultName, string name, string connectionString)
    {
      Debug.Assert(webDoc != null);
      XmlNode connStrNode = null;

      XmlNodeList nodes = webDoc.GetElementsByTagName("connectionStrings");
      if (nodes.Count == 0)
      {
        XmlNode topNode = webDoc.GetElementsByTagName("configuration")[0];
        connStrNode = webDoc.CreateElement("connectionStrings");
        XmlNode syswebElement = webDoc.GetElementsByTagName("system.web")[0];
        topNode.InsertBefore(connStrNode, syswebElement);
      }
      else
        connStrNode = nodes[0];

      // remove all traces of the old connection strings
      RemoveConnectionString(connStrNode, name);

      if (defaultName == name)
      {
        XmlElement remove = webDoc.CreateElement("remove");
        remove.SetAttribute("name", defaultName);
        connStrNode.AppendChild(remove);
      }

      XmlElement add = webDoc.CreateElement("add");
      add.SetAttribute("name", name);
      add.SetAttribute("connectionString", connectionString);
      add.SetAttribute("providerName", "MySql.Data.MySqlClient");
      connStrNode.AppendChild(add);
    }

    private void RemoveConnectionString(XmlNode parentNode, string name)
    {
      List<XmlNode> toBeDeleted = new List<XmlNode>();

      foreach (XmlNode node in parentNode.ChildNodes)
      {
        if (String.Compare(node.Attributes["name"].Value, name, true) == 0)
          toBeDeleted.Add(node);
      }
      foreach (XmlNode node in toBeDeleted)
        parentNode.RemoveChild(node);
    }

    public void SetDefaultProvider(string sectionName, string providerName)
    {
      XmlElement e = GetSystemWebNode(sectionName, true, false) as XmlElement;
      e.SetAttribute("defaultProvider", providerName);
    }

    public void RemoveProvider(string sectionName, string defaultName, string name)
    {
      XmlElement section = GetProviderSection(sectionName);
      if (section == null) return;

      section.RemoveAttribute("defaultProvider");

      if (section.FirstChild == null) return;
      XmlElement providers = section.FirstChild as XmlElement;

      List<XmlNode> toBeDeleted = new List<XmlNode>();
      foreach (XmlNode node in providers.ChildNodes)
      {
        if (String.Compare("clear", node.Name, true) == 0) continue;
        string nodeName = node.Attributes["name"].Value;
        if ((node.Name == "remove" && String.Compare(nodeName, defaultName, true) == 0) ||
            String.Compare(nodeName, name, true) == 0)
          toBeDeleted.Add(node);
      }
      foreach (XmlNode node in toBeDeleted)
        providers.RemoveChild(node);
      if (providers.ChildNodes.Count == 0)
        section.ParentNode.RemoveChild(section);
    }

    public XmlNode GetSystemWebNode(string name, bool createTopNode, bool createProvidersNode)
    {
      XmlNode webNode = null;
      XmlNode systemWebNode = webDoc.GetElementsByTagName("system.web")[0];
      foreach (XmlNode node in systemWebNode.ChildNodes)
        if (node.Name == name)
        {
          webNode = node;
          break;
        }
      if (webNode == null && createTopNode)
      {
        webNode = (XmlNode)webDoc.CreateElement(name);
        systemWebNode.InsertBefore(webNode, systemWebNode.FirstChild);
      }
      if (createProvidersNode)
      {
        if (webNode.ChildNodes.Count == 0)
          webNode.AppendChild(webDoc.CreateElement("providers"));
      }
      return webNode;
    }

    private string GetDefaultRoleProvider()
    {
      XmlElement el = (XmlElement)GetSystemWebNode("roleManager", false, false);
      if (el == null) return null;
      if (!el.HasAttribute("defaultProvider")) return null;
      return el.Attributes["defaultProvider"].Value;
    }

    public XmlElement AddProvider(string sectionName, string defaultName, string name)
    {
      XmlElement e = (XmlElement)GetSystemWebNode(sectionName, true, true);
      e = e.FirstChild as XmlElement;

      // if we are adding a provider def with the same name as default then we
      // need to remove the default
      if (String.Compare(defaultName, name, true) == 0)
      {
        XmlElement remove = webDoc.CreateElement("remove");
        remove.SetAttribute("name", defaultName);
        e.AppendChild(remove);
      }

      XmlElement add = webDoc.CreateElement("add");
      add.SetAttribute("name", name);
      e.AppendChild(add);
      return add;
    }

  }
}
