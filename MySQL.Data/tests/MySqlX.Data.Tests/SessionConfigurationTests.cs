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


using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class SessionConfigurationTests
  {
    public SessionConfigurationTests()
    {
      foreach (string name in SessionConfigManager.List())
      {
        SessionConfigManager.Delete(name);
      }
    }

    [Fact]
    public void Save()
    {
      // save using SessionConfig instance
      SessionConfig scSave = new SessionConfig("SessionConfig", "mysqlx://myuser@localhost/SessionConfig");
      scSave.SetAppData("alias", "SessionConfigAlias");
      SessionConfigManager.Save(scSave);
      SessionConfig sc = SessionConfigManager.Get("SessionConfig");
      Assert.Equal("SessionConfig", sc.Name);
      Assert.Equal("mysqlx://myuser@localhost/SessionConfig", sc.Uri);
      Assert.Equal("SessionConfigAlias", sc.GetAppData("alias"));

      // save using Json string and Uri
      SessionConfigManager.Save("JsonString", "{ \"uri\": \"mysqlx://myuser@localhost/JsonString\", \"appdata\": { \"alias\": \"JsonStringAlias\" } }");
      sc = SessionConfigManager.Get("JsonString");
      Assert.Equal("JsonString", sc.Name);
      Assert.Equal("mysqlx://myuser@localhost/JsonString", sc.Uri);
      Assert.Equal("JsonStringAlias", sc.GetAppData("alias"));

      // save using DbDoc
      SessionConfigManager.Save("DbDoc", new DbDoc("{ \"uri\": \"mysqlx://myuser@localhost/DbDoc\", \"appdata\": { \"alias\": \"DbDocAlias\" } }"));
      sc = SessionConfigManager.Get("DbDoc");
      Assert.Equal("DbDoc", sc.Name);
      Assert.Equal("mysqlx://myuser@localhost/DbDoc", sc.Uri);
      Assert.Equal("DbDocAlias", sc.GetAppData("alias"));

      // save overwrites key if key already exists
      SessionConfigManager.Save("DbDoc", new DbDoc("{ \"uri\": \"mysqlx://myuser@localhost/DbDocUpdated\", \"appdata\": { \"alias\": \"DbDocAliasUpdated\", \"other\": 5 } }"));
      sc = SessionConfigManager.Get("DbDoc");
      Assert.Equal("DbDoc", sc.Name);
      Assert.Equal("mysqlx://myuser@localhost/DbDocUpdated", sc.Uri);
      Assert.Equal("DbDocAliasUpdated", sc.GetAppData("alias"));
      Assert.Equal("DbDocAliasUpdated", sc.GetAppData("alias"));
      Assert.Equal("5", sc.GetAppData("other"));

      // save using Dictionary and Uri
      Dictionary<string, string> dic = new Dictionary<string, string>();
      dic.Add("uri", "mysqlx://myuser@localhost/Dictionary");
      dic.Add("alias", "DictionaryAlias");
      SessionConfigManager.Save("Dictionary", dic);
      sc = SessionConfigManager.Get("Dictionary");
      Assert.Equal("Dictionary", sc.Name);
      Assert.Equal("mysqlx://myuser@localhost/Dictionary", sc.Uri);
      Assert.Equal("DictionaryAlias", sc.GetAppData("alias"));

      // save using Dictionary and connection attributes
      Dictionary<string, string> dic2 = new Dictionary<string, string>();
      dic2.Add("user", "myuser");
      dic2.Add("host", "localhost");
      dic2.Add("port", "33060");
      dic2.Add("schema", "Dictionary2");
      dic2.Add("alias", "Dictionary2Alias");
      SessionConfigManager.Save("Dictionary2", dic2);
      sc = SessionConfigManager.Get("Dictionary2");
      Assert.Equal("Dictionary2", sc.Name);
      Assert.Equal("mysqlx://myuser@localhost:33060/Dictionary2", sc.Uri);
      Assert.Equal("Dictionary2Alias", sc.GetAppData("alias"));

      // save using Uri and appdata as json
      SessionConfigManager.Save("UriJson", "mysqlx://myuser@localhost/UriJson", "{ \"alias\": \"UriJsonAlias\" }");
      sc = SessionConfigManager.Get("UriJson");
      Assert.Equal("UriJson", sc.Name);
      Assert.Equal("mysqlx://myuser@localhost/UriJson", sc.Uri);
      Assert.Equal("UriJsonAlias", sc.GetAppData("alias"));

      // save using Uri and appdata as dictionary
      Dictionary<string, string> dicAppData = new Dictionary<string, string>();
      dicAppData.Add("alias", "UriDicAlias");
      SessionConfigManager.Save("UriDic", "mysqlx://myuser@localhost/UriDic", dicAppData);
      sc = SessionConfigManager.Get("UriDic");
      Assert.Equal("UriDic", sc.Name);
      Assert.Equal("mysqlx://myuser@localhost/UriDic", sc.Uri);
      Assert.Equal("UriDicAlias", sc.GetAppData("alias"));

      // save using Json string and connection attributes
      SessionConfigManager.Save("JsonStringAttributes", "{ \"host\": \"localhost\", \"user\": \"\", \"port\": 33060, \"schema\": \"JsonStringAttributes\", \"appdata\": { \"alias\": \"JsonStringAttributesAlias\", \"other\": 5 }, \"a\":1 }");
      sc = SessionConfigManager.Get("JsonStringAttributes");
      Assert.Equal("JsonStringAttributes", sc.Name);
      Assert.Equal("mysqlx://localhost:33060/JsonStringAttributes", sc.Uri);
      Assert.Equal("JsonStringAttributesAlias", sc.GetAppData("alias"));
      Assert.Equal("5", sc.GetAppData("other"));

      // save using json anonymous
      SessionConfigManager.Save("JsonAnonymous", new
      {
        host = "localhost",
        user = "",
        port = 33060,
        schema = "JsonAnonymous",
        appdata = new
        {
          alias = "JsonAnonymousAlias",
          other = 5
        }
      });
      sc = SessionConfigManager.Get("JsonAnonymous");
      Assert.Equal("JsonAnonymous", sc.Name);
      Assert.Equal("mysqlx://localhost:33060/JsonAnonymous", sc.Uri);
      Assert.Equal("JsonAnonymousAlias", sc.GetAppData("alias"));
      Assert.Equal("5", sc.GetAppData("other"));
    }

    [Fact]
    public void SaveDictionaryData()
    {
      Dictionary<string, string> dic = new Dictionary<string, string>();
      List<string[]> combination = new List<string[]>();
      dic.Add("user", "myuser");
      dic.Add("port", "33060");
      dic.Add("schema", "Dictionary");
      dic.Add("alias", "DictionaryAlias");

      for(int i = 0; i < dic.Count; i++)
      {
        Combinations(dic.Select(c => c.Key).ToList(), i + 1).ForEach(k => combination.Add(k));
      }

      int counter = 1;
      foreach (string[] values in combination)
      {
        Dictionary<string, string> dicTest = new Dictionary<string, string>();
        dicTest.Add("host", "localhost");
        values.ToList().ForEach(o => dicTest.Add(o, dic[o]));

        string name = "Dictionary" + string.Join("", values);
        SessionConfigManager.Save(name, dicTest);
        SessionConfig sc = SessionConfigManager.Get(name);
        Assert.Equal(name, sc.Name);

        StringBuilder uri = new StringBuilder("mysqlx://");
        if (dicTest.ContainsKey("user"))
          uri.Append(dicTest["user"]).Append("@");
        uri.Append(dicTest["host"]);
        if (dicTest.ContainsKey("port"))
          uri.Append(":").Append(dicTest["port"]);
        if (dicTest.ContainsKey("schema"))
          uri.Append("/").Append(dicTest["schema"]);

        Assert.Equal(uri.ToString(), sc.Uri);
        if (dicTest.ContainsKey("alias"))
          Assert.Equal("DictionaryAlias", sc.GetAppData("alias"));
        counter++;
      }
    }

    private List<T[]> Combinations<T>(List<T> elements, int members)
    {
      List<T[]> list = new List<T[]>();
      for(int i = 0; i <= (elements.Count - members); i++)
      {
        if (members > 1)
        {
          Combinations(elements.Skip(i + 1).ToList(), members - 1).ForEach(m =>
          {
            List<T> tupla = new List<T>();
            tupla.Add(elements.ElementAt(i));
            tupla.AddRange(m);
            list.Add(tupla.ToArray());
          });
        }
        else
        {
          list.Add(new T[] { elements.ElementAt(i) });
        }
      }
      return list;
    }

    [Fact]
    public void Validations()
    {
      Assert.Throws<ArgumentNullException>(() => SessionConfigManager.Save(null, (string)null));
      Assert.Equal("Name", Assert.Throws<ArgumentException>(() => SessionConfigManager.Save("@#!^^^", "")).ParamName);
      Assert.Equal("Name", Assert.Throws<ArgumentException>(() => SessionConfigManager.Save("1 name", "mysqlx://localhost", (string)null)).ParamName);
      Assert.Equal("Name", Assert.Throws<ArgumentException>(() => SessionConfigManager.Save("1!name", "mysqlx://localhost", (string)null)).ParamName);
      Assert.Equal("Name", Assert.Throws<ArgumentException>(() => SessionConfigManager.Save("1@name", "mysqlx://localhost", (string)null)).ParamName);
      Assert.Equal("Name", Assert.Throws<ArgumentException>(() => SessionConfigManager.Save("1name%", "mysqlx://localhost", (string)null)).ParamName);
    }
  }
}
