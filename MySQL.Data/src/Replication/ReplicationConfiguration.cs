// Copyright © 2013, 2018, Oracle and/or its affiliates. All rights reserved.
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
  /// <summary>
  /// Defines a replication configurarion element in the configuration file.
  /// </summary>
  public sealed class ReplicationConfigurationElement : ConfigurationElement
  {
    /// <summary>
    /// Gets a collection of <see cref="ReplicationServerGroupConfigurationElement"/> objects representing the server groups.
    /// </summary>
    [ConfigurationProperty("ServerGroups", IsRequired = true)]
    [ConfigurationCollection(typeof(ReplicationServerGroupConfigurationElement), AddItemName = "Group")]
    public GenericConfigurationElementCollection<ReplicationServerGroupConfigurationElement> ServerGroups
    {
      get { return (GenericConfigurationElementCollection<ReplicationServerGroupConfigurationElement>)this["ServerGroups"]; }
    }
  }

  /// <summary>
  /// Defines a replication server group in the configuration file.
  /// </summary>
  public sealed class ReplicationServerGroupConfigurationElement : ConfigurationElement
  {
    /// <summary>
    /// Gets or sets the name of the replication server group configuration.
    /// </summary>
    [ConfigurationProperty("name", IsRequired = true)]
    public string Name
    {
      get { return (string)this["name"]; }
      set { this["name"] = value; }
    }

    /// <summary>
    /// Gets or sets the group type of the replication server group configuration.
    /// </summary>
    [ConfigurationProperty("groupType", IsRequired = false)]
    public string GroupType
    {
      get { return (string)this["groupType"]; }
      set { this["groupType"] = value; }
    }

    /// <summary>
    /// Gets or sets the number of seconds to wait for retry.
    /// </summary>
    [ConfigurationProperty("retryTime", IsRequired = false, DefaultValue = 60)]
    public int RetryTime
    {
      get { return (int)this["retryTime"]; }
      set { this["retryTime"] = value; }
    }

    /// <summary>
    /// Gets a collection of <see cref="ReplicationServerConfigurationElement"/> objects representing the
    /// server configurations associated to this group configuration.
    /// </summary>
    [ConfigurationProperty("Servers")]
    [ConfigurationCollection(typeof(ReplicationServerConfigurationElement), AddItemName = "Server")]
    public GenericConfigurationElementCollection<ReplicationServerConfigurationElement> Servers
    {
      get { return (GenericConfigurationElementCollection<ReplicationServerConfigurationElement>)this["Servers"]; }
    }
  }

  /// <summary>
  /// Defines a replication server in configuration file.
  /// </summary>
  public sealed class ReplicationServerConfigurationElement : ConfigurationElement
  {
    /// <summary>
    /// Gets or sets the name of the replication server configuration. 
    /// </summary>
    [ConfigurationProperty("name", IsRequired = true)]
    public string Name
    {
      get { return (string)this["name"]; }
      set { this["name"] = value; }
    }

    /// <summary>
    /// Gets or sets whether the replication server is configured as master.
    /// </summary>
    [ConfigurationProperty("IsMaster", IsRequired = false, DefaultValue = false)]
    public bool IsMaster
    {
      get { return (bool)this["IsMaster"]; }
      set { this["IsMaster"] = value; }
    }

    /// <summary>
    /// Gets or sets the connection string associated to this replication server.
    /// </summary>
    [ConfigurationProperty("connectionstring", IsRequired = true)]
    public string ConnectionString
    {
      get { return (string)this["connectionstring"]; }
      set { this["connectionstring"] = value; }
    }
  }
}
