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
using System.Text;
using System.Configuration;

namespace MySql.Data.MySqlClient
{
  public sealed class MySqlConfiguration : ConfigurationSection
  {
    private static MySqlConfiguration settings
      = ConfigurationManager.GetSection("MySQL") as MySqlConfiguration;

    public static MySqlConfiguration Settings
    {
      get { return settings; }
    }

    [ConfigurationProperty("ExceptionInterceptors", IsRequired = false)]
    [ConfigurationCollection(typeof(InterceptorConfigurationElement), AddItemName = "add", ClearItemsName = "clear", RemoveItemName = "remove")]
    public GenericConfigurationElementCollection<InterceptorConfigurationElement> ExceptionInterceptors
    {
      get { return (GenericConfigurationElementCollection<InterceptorConfigurationElement>)this["ExceptionInterceptors"]; }
    }

    [ConfigurationProperty("CommandInterceptors", IsRequired = false)]
    [ConfigurationCollection(typeof(InterceptorConfigurationElement), AddItemName = "add", ClearItemsName = "clear", RemoveItemName = "remove")]
    public GenericConfigurationElementCollection<InterceptorConfigurationElement> CommandInterceptors
    {
      get { return (GenericConfigurationElementCollection<InterceptorConfigurationElement>)this["CommandInterceptors"]; }
    }

    [ConfigurationProperty("AuthenticationPlugins", IsRequired = false)]
    [ConfigurationCollection(typeof(AuthenticationPluginConfigurationElement), AddItemName = "add", ClearItemsName = "clear", RemoveItemName = "remove")]
    public GenericConfigurationElementCollection<AuthenticationPluginConfigurationElement> AuthenticationPlugins
    {
      get { return (GenericConfigurationElementCollection<AuthenticationPluginConfigurationElement>)this["AuthenticationPlugins"]; }
    }

    [ConfigurationProperty("Replication", IsRequired = true)]
    public ReplicationConfigurationElement Replication
    {
      get
      {
        return (ReplicationConfigurationElement)this["Replication"];
      }
      set
      {
        this["Replication"] = value;
      }
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class AuthenticationPluginConfigurationElement : ConfigurationElement
  {
    [ConfigurationProperty("name", IsRequired = true)]
    public string Name
    {
      get
      {
        return (string)this["name"];
      }
      set
      {
        this["name"] = value;
      }
    }

    [ConfigurationProperty("type", IsRequired = true)]
    public string Type
    {
      get
      {
        return (string)this["type"];
      }
      set
      {
        this["type"] = value;
      }
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class InterceptorConfigurationElement : ConfigurationElement
  {
    [ConfigurationProperty("name", IsRequired = true)]
    public string Name
    {
      get
      {
        return (string)this["name"];
      }
      set
      {
        this["name"] = value;
      }
    }

    [ConfigurationProperty("type", IsRequired = true)]
    public string Type
    {
      get
      {
        return (string)this["type"];
      }
      set
      {
        this["type"] = value;
      }
    }
  }

  /// <summary>
  /// 
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public sealed class GenericConfigurationElementCollection<T> : ConfigurationElementCollection, IEnumerable<T> where T : ConfigurationElement, new()
  {
    List<T> _elements = new List<T>();

    protected override ConfigurationElement CreateNewElement()
    {
      T newElement = new T();
      _elements.Add(newElement);
      return newElement;
    }

    protected override object GetElementKey(ConfigurationElement element)
    {
      return _elements.Find(e => e.Equals(element));
    }

    public new IEnumerator<T> GetEnumerator()
    {
      return _elements.GetEnumerator();
    }
  }
}
