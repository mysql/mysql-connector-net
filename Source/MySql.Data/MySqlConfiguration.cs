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
