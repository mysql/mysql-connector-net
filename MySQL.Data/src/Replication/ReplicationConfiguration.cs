// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Used to define a Replication configurarion element in configuration file
  /// </summary>
  public sealed class ReplicationConfigurationElement : ConfigurationElement
  {
    [ConfigurationProperty("ServerGroups", IsRequired = true)]
    [ConfigurationCollection(typeof(ReplicationServerGroupConfigurationElement), AddItemName = "Group")]
    public GenericConfigurationElementCollection<ReplicationServerGroupConfigurationElement> ServerGroups
    {
      get { return (GenericConfigurationElementCollection<ReplicationServerGroupConfigurationElement>)this["ServerGroups"]; }
    }
  }

  /// <summary>
  /// Used to define a Replication server group in configuration file
  /// </summary>
  public sealed class ReplicationServerGroupConfigurationElement : ConfigurationElement
  {
    [ConfigurationProperty("name", IsRequired = true)]
    public string Name
    {
      get { return (string)this["name"]; }
      set { this["name"] = value; }
    }

    [ConfigurationProperty("groupType", IsRequired = false)]
    public string GroupType
    {
      get { return (string)this["groupType"]; }
      set { this["groupType"] = value; }
    }

    [ConfigurationProperty("retryTime", IsRequired = false, DefaultValue = 60)]
    public int RetryTime
    {
      get { return (int)this["retryTime"]; }
      set { this["retryTime"] = value; }
    }

    [ConfigurationProperty("Servers")]
    [ConfigurationCollection(typeof(ReplicationServerConfigurationElement), AddItemName = "Server")]
    public GenericConfigurationElementCollection<ReplicationServerConfigurationElement> Servers
    {
      get { return (GenericConfigurationElementCollection<ReplicationServerConfigurationElement>)this["Servers"]; }
    }
  }

  /// <summary>
  /// Defines a Replication server in configuration file
  /// </summary>
  public sealed class ReplicationServerConfigurationElement : ConfigurationElement
  {
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
  }
}
