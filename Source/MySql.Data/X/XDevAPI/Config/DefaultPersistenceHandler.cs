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
    private Dictionary<string, string> list;
    private readonly string fileName = "MySQLsessions.json";
    private readonly string programdataFile;
    private readonly string appdataFile;


    public DefaultPersistenceHandler()
    {
      string programdataPath = Environment.GetEnvironmentVariable("programdata");
      if (!string.IsNullOrEmpty(programdataPath))
      {
        programdataFile = Path.Combine(programdataPath, fileName);
      }

      string appdataPath = Environment.GetEnvironmentVariable("appdata");
      if (!string.IsNullOrEmpty(appdataPath))
      {
        appdataFile = Path.Combine(appdataPath, fileName);
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
      string value = list[name];
      return new DbDoc($"{{ \"{name}\": {value} }}");
    }

    public SessionConfig Save(string name, DbDoc config)
    {
      var data = ReadConfigData(appdataFile);
      SessionConfig sessionConfig = new SessionConfig(name, config["uri"]);
      data.Add(name, config.ToString());
      DbDoc json = new DbDoc(data);
      File.WriteAllText(appdataFile, json.ToString());
      return sessionConfig;
    }


    private void LoadConfigData()
    {
      list = new Dictionary<string, string>();

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


    private Dictionary<string, string> ReadConfigData(string path)
    {
      Dictionary<string, string> configList = new Dictionary<string, string>();

      try
      {
        if (!File.Exists(path)) return configList;
        string fileData = File.ReadAllText(path);
        var data = Serialization.JsonParser.Parse(fileData);
        foreach (var item in data)
        {
          configList[item.Key] = item.Value.ToString();
        }
        return configList;
      }
      catch(Exception ex)
      {
        throw new FormatException(string.Format(ResourcesX.ErrorParsingConfigFile, path), ex);
      }
    }
  }
}
