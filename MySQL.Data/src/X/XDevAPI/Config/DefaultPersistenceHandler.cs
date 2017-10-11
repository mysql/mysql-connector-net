// Copyright © 2016, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlX.XDevAPI.Config
{
  internal class DefaultPersistenceHandler : IPersistenceHandler
  {
    private Dictionary<string, object> list;
    private readonly string fileName = "MySQLsessions.json";
    private readonly string programdataFile;
    private readonly string appdataFile;
    private const string linux_system_path = "/etc/mysql/sessions.json";
    private const string linux_user_path = "/.mysql/sessions.json";


    public DefaultPersistenceHandler()
    {
      switch (Tools.GetOS())
      {
        case DataAccess.OS.Windows:
          string programdataPath = Environment.GetEnvironmentVariable("programdata");
          if (string.IsNullOrEmpty(programdataPath))
            throw new ArgumentNullException(ResourcesX.ProgramDataNotDefined);
          else
            programdataFile = Path.Combine(programdataPath, fileName);

          string appdataPath = Environment.GetEnvironmentVariable("appdata");
          if (string.IsNullOrEmpty(appdataPath))
            throw new ArgumentNullException(ResourcesX.AppdataNotDefined);
          else
            appdataFile = Path.Combine(appdataPath, fileName);

          break;

        case DataAccess.OS.Linux:
        case DataAccess.OS.MacOS:
          programdataFile = linux_system_path;
          appdataFile = Environment.GetEnvironmentVariable("HOME") + linux_user_path;
          break;
      }
    }

    public void Delete(string name)
    {
      var data = ReadConfigData(appdataFile);
      if (!data.Remove(name)) throw new KeyNotFoundException(name);
      DbDoc json = new DbDoc(data);
      File.WriteAllText(appdataFile, json.ToString());
    }

    public bool Exists(string name)
    {
      LoadConfigData();
      return list.ContainsKey(name);
    }

    public List<string> List()
    {
      LoadConfigData();
      return list.Keys.ToList<string>();
    }

    public DbDoc Load(string name)
    {
      LoadConfigData();
      return new DbDoc(list[name]);
    }

    public SessionConfig Save(string name, DbDoc config)
    {
      var data = ReadConfigData(appdataFile);
      SessionConfig sessionConfig = new SessionConfig(name, config["uri"]);
      if (config.values.ContainsKey("appdata"))
      {
        var appdata = config.values["appdata"] as Dictionary<string, object>;
        if (appdata == null)
          throw new FormatException("appdata");
        appdata.ToList().ForEach(i => sessionConfig.SetAppData(i.Key, i.Value.ToString()));
      }
      data.Add(name, config);
      DbDoc json = new DbDoc(data);
      string dir = Directory.GetParent(appdataFile).FullName;
      if(!Directory.Exists(dir))
        Directory.CreateDirectory(dir);
      File.WriteAllText(appdataFile, json.ToString());
      return sessionConfig;
    }


    private void LoadConfigData()
    {
      list = new Dictionary<string, object>();

      //Load system configuration data (read only)
      if (!string.IsNullOrEmpty(programdataFile))
      {
        foreach (var item in ReadConfigData(programdataFile))
          list.Add(item.Key, item.Value);
      }

      //Load user configuration data (read/write)
      if (!string.IsNullOrEmpty(appdataFile))
      {
        foreach (var item in ReadConfigData(appdataFile))
          list[item.Key] = item.Value;
      }
    }


    private Dictionary<string, object> ReadConfigData(string path)
    {
      try
      {
        if (!File.Exists(path)) return new Dictionary<string, object>();
        string fileData = File.ReadAllText(path);
        return JsonParser.Parse(fileData);
      }
      catch(Exception ex)
      {
        throw new FormatException(string.Format(ResourcesX.ErrorParsingConfigFile, path), ex);
      }
    }
  }
}
