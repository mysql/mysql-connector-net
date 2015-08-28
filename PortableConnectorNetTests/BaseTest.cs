// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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

using MySql.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableConnectorNetTests
{
  public class BaseTest
  {
    private static Session session;
    private static NodeSession nodeSession;

    protected Schema SetupSchema(string name)
    {
      Session s = GetSession();
      Schema schema = s.GetSchema(name);
      if (schema.ExistsInDatabase())
        s.DropSchema(name);
      schema = s.CreateSchema(name);
      return schema;
    }

    protected Table GetTable(string schema, string table)
    {
      return GetSession().GetSchema(schema).GetTable(table);
    }

    protected void ExecuteSQL(string sql)
    {
      NodeSession nodeSession = GetNodeSession();
      nodeSession.ExecuteSql(sql, true, null);
    }

    protected Collection CreateCollection(string name)
    {
      Session s = GetSession();
      Schema test = s.GetSchema("test");
      Collection c = test.GetCollection(name);
      if (c.ExistsInDatabase())
        c.Drop();
      return test.CreateCollection(name);
    }

    protected Session GetSession()
    {
      if (session == null)
        session = MySqlX.GetSession("server=localhost;port=33060;uid=userx;password=userx1");
      return session;
    }

    protected NodeSession GetNodeSession()
    {
      if (nodeSession == null)
        nodeSession = MySqlX.GetNodeSession("server=localhost;port=33060;uid=userx;password=userx1");
      return nodeSession;
    }
  }
}
