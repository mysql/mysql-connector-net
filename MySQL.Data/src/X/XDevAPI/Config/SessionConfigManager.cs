// Copyright © 2016, 2017, Oracle and/or its affiliates. All rights reserved.
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


using MySql.Data;
using MySqlX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MySqlX.XDevAPI.Config
{
  /// <summary>
  /// Represents the user interface used to store/retrieve session configuration data.
  /// </summary>
  public static class SessionConfigManager
  {
    private static IPersistenceHandler persistenceHandler = new DefaultPersistenceHandler();
    private static IPasswordHandler passwordHandler;
    /// <summary>
    /// Saves the session configuration.
    /// </summary>
    /// <param name="name">The session configuration name.</param>
    /// <param name="uri">The session configuration uri.</param>
    /// <param name="jsonAppData">The session configuation data in json format.</param>
    /// <returns>A <see cref="SessionConfig"/> object set with the saved configuration data.</returns>
    public static SessionConfig Save(string name, string uri, string jsonAppData)
    {
      ValidateName(name);
      ValidateUri(uri);

      DbDoc json = new DbDoc();
      json.SetValue("uri", uri);
      if (jsonAppData != null)
        json.SetValue("appdata", new DbDoc(jsonAppData));
      return Save(name, json);
    }
    /// <summary>
    /// Saves the session configuration.
    /// </summary>
    /// <param name="name">The session configuration name.</param>
    /// <param name="uri">The session configuration uri.</param>
    /// <param name="appData">The session configuration data as a dictionary.</param>
    /// <returns>A <see cref="SessionConfig"/> object set with the saved configuration data.</returns>
    public static SessionConfig Save(string name, string uri, Dictionary<string, string> appData)
    {
      ValidateName(name);
      ValidateUri(uri);

      SessionConfig sessionConfig = new SessionConfig(name, uri);
      appData?.ToList().ForEach(i => sessionConfig.SetAppData(i.Key, i.Value));
      return Save(sessionConfig);
    }
    /// <summary>
    /// Saves the session configuration.
    /// </summary>
    /// <param name="name">The session configuration name.</param>
    /// <param name="json">The session configuration data as a <see cref="DbDoc"/> object.</param>
    /// <returns>A <see cref="SessionConfig"/> object set with the saved configuration data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    public static SessionConfig Save(string name, DbDoc json)
    {
      ValidateName(name);
      if (json == null)
        throw new ArgumentNullException("Json");

      if (!json.values.ContainsKey("uri"))
      {
        Dictionary<string, string> dic = new Dictionary<string, string>();
        foreach(var item in json.values)
        {
          Dictionary<string, object> appdata = item.Value as Dictionary<string, object>;
          if (appdata == null && item.Value.GetType().GetTypeInfo().IsGenericType)
            appdata = Tools.GetDictionaryFromAnonymous(item.Value);

          if (appdata == null)
            dic.Add(item.Key, json[item.Key]);
          else
            appdata.ToList().ForEach(i => dic.Add(i.Key, json["appdata." + i.Key]));
        }
        return Save(name, dic);
      }

      Delete(name);
      return persistenceHandler.Save(name, json);
    }
    /// <summary>
    /// Saves the session configuration.
    /// </summary>
    /// <param name="name">The session configuration name.</param>
    /// <param name="json">The session configuration data as a json string.</param>
    /// <returns>A <see cref="SessionConfig"/> object set with the saved configuration data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null or whitespace.</exception>
    public static SessionConfig Save(string name, string json)
    {
      ValidateName(name);
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentNullException("Json");

      return Save(name, new DbDoc(json));
    }
    /// <summary>
    /// Saves the session configuration.
    /// </summary>
    /// <param name="name">The session configuration name.</param>
    /// <param name="json">The session configuration data as a json object.</param>
    /// <returns>A <see cref="SessionConfig"/> object set with the saved configuration data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    public static SessionConfig Save(string name, object json)
    {
      ValidateName(name);
      if (json == null)
        throw new ArgumentNullException("Json");

      return Save(name, new DbDoc(json));
    }
    /// <summary>
    /// Saves the session configuration.
    /// </summary>
    /// <param name="name">The session configuration name.</param>
    /// <param name="data">The session configuration data as a dictionary.</param>
    /// <returns>A <see cref="SessionConfig"/> object set with the saved configuration data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
    /// <exception cref="ArgumentException">Both 'uri' and 'host' were found in <paramref name="data"/> dictionary.</exception>
    /// <exception cref="ArgumentNullException">'host' wasn't found in the <paramref name="data"/> dictionary.</exception>
    public static SessionConfig Save(string name, Dictionary<string, string> data)
    {
      ValidateName(name);
      if (data == null)
        throw new ArgumentNullException("Data");

      DbDoc json = new DbDoc();
      Dictionary<string, object> appdata = new Dictionary<string, object>();
      bool hasUri = false, hasHost = false;
      foreach (string key in data.Keys)
      {
        switch (key)
        {
          case "uri":
            if (hasHost)
              throw new ArgumentException(ResourcesX.JsonUriOrHost);
            if (string.IsNullOrEmpty(data["uri"]))
              throw new ArgumentNullException("uri");
            json.SetValue("uri", data["uri"]);
            hasUri = true;
            break;
          case "host":
          case "user":
          case "password":
          case "port":
          case "schema":
            if (hasUri)
              throw new ArgumentException(ResourcesX.JsonUriOrHost);
            hasHost = true;
            break;
          default:
            appdata.Add(key, data[key]);
            break;
        }
      }
      if (hasHost)
      {
        if (!data.ContainsKey("host") || string.IsNullOrEmpty(data["host"]))
          throw new ArgumentNullException(ResourcesX.NoHost);
        StringBuilder sb = new StringBuilder("mysqlx://");
        if (data.ContainsKey("user") && !string.IsNullOrWhiteSpace(data["user"]))
          sb.Append(data["user"]).Append("@");
        sb.Append(data["host"]);
        if (data.ContainsKey("port") && !string.IsNullOrWhiteSpace(data["port"]))
          sb.Append(":").Append(data["port"]);
        if (data.ContainsKey("schema") && !string.IsNullOrWhiteSpace(data["schema"]))
          sb.Append("/").Append(data["schema"]);

        json.SetValue("uri", sb.ToString());
      }
      if(appdata.Count > 0)
      {
        json.SetValue("appdata", appdata);
      }

      return Save(name, json);
    }
    /// <summary>
    /// Saves the session configuration.
    /// </summary>
    /// <param name="cfg">The <see cref="SessionConfig"/> object to save.</param>
    /// <returns>A <see cref="SessionConfig"/> set with the saved session configuration data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cfg"/> is null.</exception>
    public static SessionConfig Save(SessionConfig cfg)
    {
      if (cfg == null)
        throw new ArgumentNullException("SessionConfig");

      Dictionary<string, object> options = new Dictionary<string, object>();
      options.Add("uri", cfg.Uri);
      if(cfg.appData.Count > 0)
      {
        options.Add("appdata", cfg.appData);
      }
      return Save(cfg.Name, new DbDoc(options));
    }
    /// <summary>
    /// Gets a specific session configuration.
    /// </summary>
    /// <param name="name">The session configuration name.</param>
    /// <returns>The <see cref="SessionConfig"/> that matches the provided session configuration name.</returns>
    public static SessionConfig Get(string name)
    {
      ValidateName(name);

      DbDoc config = persistenceHandler.Load(name);
      SessionConfig cfg = new SessionConfig(name, config["uri"]);
      if (config.values.ContainsKey("appdata"))
      {
        Dictionary<string, object> appdata = config.values["appdata"] as Dictionary<string, object>;
        if (appdata != null)
        {
          foreach (var option in appdata)
            cfg.SetAppData(option.Key, option.Value.ToString());
        }
      }
      return cfg;
    }
    /// <summary>
    /// Deletes a specific session configuration.
    /// </summary>
    /// <param name="name">The session configuration name.</param>
    /// <returns>True if the session configuration was deleted, false otherwise.</returns>
    public static bool Delete(string name)
    {
      ValidateName(name);
      try
      {
        persistenceHandler.Delete(name);
        return true;
      }
      catch
      {
        return false;
      }
    }
    /// <summary>
    /// Retrives the name list of stored session configurations.
    /// </summary>
    /// <returns>A string list with the names of stored session configurations.</returns>
    public static List<string> List()
    {
      return persistenceHandler.List();
    }
    /// <summary>
    /// Sets the persistence handler.
    /// </summary>
    /// <param name="handler">The persistence handler.</param>
    public static void SetPersistenceHandler(IPersistenceHandler handler)
    {
      persistenceHandler = handler;
    }
    /// <summary>
    /// Sets the password handler.
    /// </summary>
    /// <param name="handler">The password handler.</param>
    public static void SetPasswordHandler(IPasswordHandler handler)
    {
      passwordHandler = handler;
    }

    private static void ValidateName(string name)
    {
      if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentNullException("Name");
      if (!Regex.IsMatch(name, @"^\w[\w-.]*$"))
        throw new ArgumentException($"Name is invalid.", "Name");
    }

    private static void ValidateUri(string uri)
    {
      if (string.IsNullOrWhiteSpace(uri))
        throw new ArgumentNullException("Uri");
      if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
        throw new ArgumentException("Uri is invalid.", "Uri");
    }
  }
}
