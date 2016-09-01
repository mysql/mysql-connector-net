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


using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class SessionConfigurationTests
  {
    [Fact]
    public void ListConfigurations()
    {
      var list = SessionConfigManager.List();
      Assert.Equal(1, list.Count);
    }

    [Fact]
    public void Save()
    {
      foreach (string name in SessionConfigManager.List())
      {
        SessionConfigManager.Delete(name);
      }

      SessionConfigManager.Save(new SessionConfig("SessionConfig", "myuser@localhost/SessionConfig"));
      Assert.Equal("SessionConfig", SessionConfigManager.Get("SessionConfig").Name);

      SessionConfigManager.Save("JsonString", "{ \"uri\": \"myuser@localhost/JsonString\" }");
      Assert.Equal("JsonString", SessionConfigManager.Get("JsonString").Name);

      SessionConfigManager.Save("DbDoc", new DbDoc("{ \"uri\": \"myuser@localhost/DbDoc\" }"));
      Assert.Equal("JsonString", SessionConfigManager.Get("JsonString").Name);

      Dictionary<string, string> dic = new Dictionary<string, string>();
      dic.Add("uri", "myuser@localhost/Dictionary");
      SessionConfigManager.Save("Dictionary", dic);
      Assert.Equal("Dictionary", SessionConfigManager.Get("Dictionary").Name);

      Dictionary<string, string> dic2 = new Dictionary<string, string>();
      dic2.Add("user", "myuser");
      dic2.Add("host", "localhost");
      dic2.Add("port", "33060");
      dic2.Add("schema", "Dictionary2");
      SessionConfigManager.Save("Dictionary2", dic2);
      Assert.Equal("Dictionary2", SessionConfigManager.Get("Dictionary2").Name);
      Assert.Equal("myuser@localhost:33060/Dictionary2", SessionConfigManager.Get("Dictionary2").Uri);

      SessionConfigManager.Save("UriJson", "myuser@localhost/urijson", "{ \"ssl-ca\": \"client.pfx\" }");
      Assert.Equal("UriJson", SessionConfigManager.Get("UriJson").Name);

      Dictionary<string, string> dicAppData = new Dictionary<string, string>();
      dicAppData.Add("ssl-ca", "client.pfx");
      SessionConfigManager.Save("UriDic", "myuser@localhost/uridic", dicAppData);
      Assert.Equal("UriDic", SessionConfigManager.Get("UriDic").Name);
    }

    [Fact]
    public void SaveDictionaryData()
    {

    }
  }
}
