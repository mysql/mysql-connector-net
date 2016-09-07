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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlX.XDevAPI.Config
{
  public class SessionConfig
  {
    internal Dictionary<string, object> appData = new Dictionary<string, object>();

    internal SessionConfig(string name, string uri)
    {
      if (string.IsNullOrEmpty(name))
        throw new ArgumentNullException("name");
      if (string.IsNullOrEmpty(uri))
        throw new ArgumentNullException("uri");
      Name = name;
      Uri = uri;
    }

    public string Name { get; protected set; }

    public string Uri { get; set; }

    public void SetAppData(string key, string value)
    {
      appData.Add(key, value);
    }

    public void DeleteAppData(string key)
    {
      appData.Remove(key);
    }

    public string GetAppData(string key)
    {
      return appData[key].ToString();
    }

    public void Save()
    {
      SessionConfigManager.Save(this);
    }

    public string ToJsonString()
    {
      List<string> options = new List<string>();
      options.Add($"\"uri\": \"{Uri}\"");
      if(appData.Count > 0)
      {
        string appdataOptions = string.Join(", ", appData.Select(i => $"\"{i.Key}\": \"{i.Value}\""));
        options.Add($"\"appdata\": {{ {appdataOptions} }}");
      }
      return $"{{ \"{Name}\": {{ {string.Join(", ", options)} }} }}";
    }
  }
}
