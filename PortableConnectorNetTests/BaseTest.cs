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

using System;
using MySql.XDevAPI;
using Xunit;
using MySql.XDevAPI.Relational;

namespace PortableConnectorNetTests
{
  public class BaseTest : IDisposable
  {
    protected XSession session;
    protected Schema testSchema;
    private static NodeSession nodeSession;
    protected static string schemaName;

    public static string ConnectionString { get; private set; }
    public static string ConnectionStringNoPassword { get; private set; }

    static BaseTest()
    {
      schemaName = "test";
      ConnectionString = "server=localhost;port=33060;uid=test;password=test";
      ConnectionStringNoPassword = "server=localhost;port=33060;uid=testNoPass;";
    }

    public BaseTest()
    {
      session = GetSession();
      testSchema = session.GetSchema(schemaName);
      if (testSchema.ExistsInDatabase())
        session.DropSchema(schemaName);
      session.CreateSchema(schemaName);
    }

    protected Table GetTable(string schema, string table)
    {
      return GetSession().GetSchema(schema).GetTable(table);
    }

    protected SqlResult ExecuteSQL(string sql)
    {
      NodeSession nodeSession = GetNodeSession();
      nodeSession.SQL("USE test").Execute();
      SqlResult r = nodeSession.SQL(sql).Execute();
      var rows = r.HasData ? r.FetchAll() : null;
      Assert.True(r.Succeeded);
      return r;
    }

    protected Collection CreateCollection(string name)
    {
      XSession s = GetSession();
      Schema test = s.GetSchema("test");
      test.DropCollection(name);
      return test.CreateCollection(name);
    }

    public XSession GetSession()
    {
      if (session == null)
        session = MySqlX.GetSession(ConnectionString);
      return session;
    }

    public NodeSession GetNodeSession()
    {
      if (nodeSession == null)
        nodeSession = MySqlX.GetNodeSession(ConnectionString);
      return nodeSession;
    }

    public void Dispose()
    {
      XSession s = GetSession();
      s.DropSchema(schemaName);
      Schema schema = s.GetSchema(schemaName);
      Assert.False(schema.ExistsInDatabase());
    }
  }
}
