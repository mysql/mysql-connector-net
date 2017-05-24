// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.XDevAPI;
using Xunit;
using MySqlX.XDevAPI.Relational;
using System.Reflection;
using System.IO;
using MySql.Data.MySqlClient;

namespace MySqlX.Data.Tests
{
  public class BaseTest : IDisposable
  {
    protected Session session;
    protected Schema testSchema;
    protected static readonly string schemaName;

    public static string ConnectionString { get; private set; }
    public static string ConnectionStringUri { get; private set; }
    public static string ConnectionStringNoPassword { get; private set; }
    public static string ConnectionStringRoot { get; private set; }

#if NETCORE10
    private static ConfigUtils config = new ConfigUtils(Path.GetFullPath(@"../../../../../..") + @"/appsettings.json");
#endif

        static BaseTest()
    {
#if NETCORE10
      Port = config.GetValue("MySql:Data:Port") ?? "3306";
      XPort = config.GetValue("MySqlX:Data:Port") ?? "33060";
#else
      Port = "3305";
      XPort = "33050";
#endif
      schemaName = "test";
      ConnectionStringRoot = $"server=localhost;port={Port};uid=root;password=";
      ConnectionString = $"server=localhost;port={XPort};uid=test;password=test";
      ConnectionStringNoPassword = $"server=localhost;port={XPort};uid=testNoPass;";
      ConnectionStringUri = $"mysqlx://test:test@localhost:{XPort}";
    }

    protected static string Port
    {
      get; private set;
    }

    protected static string XPort
    {
      get; private set;
    }

    public BaseTest()
    {
      Assembly executingAssembly = typeof(BaseTest).GetTypeInfo().Assembly;
      Stream stream = executingAssembly.GetManifestResourceStream("MySqlX.Data.Tests.Properties.CreateUsers.sql");
      StreamReader sr = new StreamReader(stream);
      string sql = sr.ReadToEnd();
      sr.Dispose();
      ExecuteSqlAsRoot(sql);
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
      Session session = GetSession();
      session.SetCurrentSchema(schemaName);
      SqlResult r = session.SQL(sql).Execute();
      var rows = r.HasData ? r.FetchAll() : null;
      return r;
    }

    protected Collection CreateCollection(string name)
    {
      Session session = GetSession();
      Schema test = session.GetSchema(schemaName);
      test.DropCollection(name);
      return test.CreateCollection(name);
    }

    public Session GetSession(bool setCurrentSchema = false)
    {
      if (session == null)
      {
        session = MySQLX.GetSession(ConnectionString);
        if (setCurrentSchema)
          session.SetCurrentSchema(schemaName);
      }
      return session;
    }

    protected void ExecuteSqlAsRoot(string sql)
    {
      var rootConn = new MySqlConnection(ConnectionStringRoot + ";ssl mode=none");
      rootConn.Open();
      MySqlScript s = new MySqlScript(rootConn, sql);
      s.Execute();
      rootConn.Close();
    }

    public virtual void Dispose()
    {
      using (Session s = GetSession())
      {
        Schema schema = s.GetSchema(schemaName);
        if(schema.ExistsInDatabase())
            s.DropSchema(schemaName);
        Assert.False(schema.ExistsInDatabase());
      }
    }
  }
}
