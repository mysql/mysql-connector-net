// Copyright © 2015, 2018, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
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

    static BaseTest()
    {
      Port = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
      XPort = Environment.GetEnvironmentVariable("MYSQLX_PORT") ?? "33060";
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
