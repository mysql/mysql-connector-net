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
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace MySql.Data.MySqlClient
{
  public sealed class LoadBalancingConfigurationElement : ConfigurationElement
  {
    [ConfigurationProperty("ServerGroups", IsRequired = true)]
    [ConfigurationCollection(typeof(LoadBalancingServerGroupConfigurationElement), AddItemName = "Group")]
    public GenericConfigurationElementCollection<LoadBalancingServerGroupConfigurationElement> ServerGroups
    {
      get { return (GenericConfigurationElementCollection<LoadBalancingServerGroupConfigurationElement>)this["ServerGroups"]; }
    }
  }

  public sealed class LoadBalancingServerGroupConfigurationElement : ConfigurationElement
  {
    [ConfigurationProperty("name", IsRequired = true)]
    public string Name
    {
      get { return (string)this["name"]; }
      set { this["name"] = value; }
    }

    [ConfigurationProperty("selectorType", IsRequired = false)]
    public string SelectorType
    {
      get { return (string)this["selectorType"]; }
      set { this["selectorType"] = value; }
    }

    [ConfigurationProperty("retryTime", IsRequired = false, DefaultValue = 60)]
    public int RetryTime
    {
      get { return (int)this["retryTime"]; }
      set { this["retryTime"] = value; }
    }

    [ConfigurationProperty("Servers")]
    [ConfigurationCollection(typeof(LoadBalancingServerConfigurationElement), AddItemName = "Server")]
    public GenericConfigurationElementCollection<LoadBalancingServerConfigurationElement> Servers
    {
      get { return (GenericConfigurationElementCollection<LoadBalancingServerConfigurationElement>)this["Servers"]; }
    }


    public LoadBalancingServerConfigurationElement GetElementAt(int index)
    {
      LoadBalancingServerConfigurationElement[] elements = new LoadBalancingServerConfigurationElement[Servers.Count];
      Servers.CopyTo(elements, 0);
      return elements[index];
    }
  }

  public sealed class LoadBalancingServerConfigurationElement : ConfigurationElement
  {
    public LoadBalancingServerConfigurationElement() : base()
    {
      IsAvailable = true;
    }

    [ConfigurationProperty("name", IsRequired = true)]
    public string Name
    {
      get { return (string)this["name"]; }
      set { this["name"] = value; }
    }

    [ConfigurationProperty("IsMaster", IsRequired = false, DefaultValue = false)]
    public bool IsMaster
    {
      get { return (bool)this["IsMaster"]; }
      set { this["IsMaster"] = value; }
    }

    [ConfigurationProperty("connectionstring", IsRequired = true)]
    public string ConnectionString
    {
      get { return (string)this["connectionstring"]; }
      set { this["connectionstring"] = value; }
    }

    internal bool IsAvailable
    {
      get; 
      set; 
    }
  }
}
