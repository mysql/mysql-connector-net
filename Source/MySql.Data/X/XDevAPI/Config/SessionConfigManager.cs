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
  public static class SessionConfigManager
  {
    private static IPersistenceHandler persistenceHandler = new DefaultPersistenceHandler();
    private static IPasswordHandler passwordHandler;

    public static SessionConfig Save(string name, string uri, string jsonAppData = null)
    {
      SessionConfig cfg = new SessionConfig(name, uri);
      // TODO add appdata from json string
      return persistenceHandler.Save(name, new DbDoc());
    }

    public static SessionConfig Save(string name, string uri, Dictionary<string, string> appData = null)
    {
      SessionConfig sessionConfig = new SessionConfig(name, uri);
      appData?.ToList().ForEach(i => sessionConfig.SetAppData(i.Key, i.Value));
      return Save(sessionConfig);
    }

    public static SessionConfig Save(string name, DbDoc json)
    {
      return persistenceHandler.Save(name, json);
    }

    public static SessionConfig Save(string name, Dictionary<string, string> appData)
    {
      return persistenceHandler.Save(name, new DbDoc());
    }

    public static SessionConfig Save(SessionConfig cfg)
    {
      return Save(cfg.Name, new DbDoc(cfg.ToJsonString()));
    }

    public static SessionConfig Update(SessionConfig cfg)
    {
      throw new NotImplementedException();
    }

    public static SessionConfig Get(string name)
    {
      DbDoc config = persistenceHandler.Load(name);
      SessionConfig cfg = new SessionConfig(config["name"], config["uri"]);
      //TODO add appdata from json string
      return cfg;
    }

    public static bool Delete(string name)
    {
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

    public static List<string> List()
    {
      return persistenceHandler.List();
    }

    public static void SetPersistenceHandler(IPersistenceHandler handler)
    {
      persistenceHandler = handler;
    }

    public static void SetPasswordHandler(IPasswordHandler handler)
    {
      passwordHandler = handler;
    }
  }
}
