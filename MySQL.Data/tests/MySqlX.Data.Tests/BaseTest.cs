// Copyright (c) 2015, 2022, Oracle and/or its affiliates.
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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using MySqlX.XDevAPI.CRUD;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using System;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace MySqlX.Data.Tests
{
  public class BaseTest
  {
    protected Session session;
    protected Schema testSchema;
    protected static readonly string schemaName = "test";
    public static readonly string sslCa = "ca.pem";
    public static readonly string sslCert = "client-cert.pem";
    public static readonly string sslKey = "client-key.pem";
    public static readonly string clientPfxIncorrect = "client-incorrect.pfx";
    public static readonly string clientPfx = "client.pfx";
    public static readonly string sslCertificatePassword = "pass";

    #region Properties
    internal static string Port { get; private set; } = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
    internal static string XPort { get; private set; } = Environment.GetEnvironmentVariable("MYSQLX_PORT") ?? "33060";
    internal static string Host { get; private set; } = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost";
    internal static string RootUser { get; private set; } = Environment.GetEnvironmentVariable("MYSQL_ROOT_USER") ?? "root";
    internal static string RootPassword { get; private set; } = Environment.GetEnvironmentVariable("MYSQL_ROOT_PASSWORD") ?? string.Empty;
    public static string ConnectionString { get; private set; }
    public static string ConnectionStringUri { get; private set; }
    public static string ConnectionStringNoPassword { get; private set; }
    public static string ConnectionStringRoot { get; private set; }
    public static string ConnectionStringUriNative { get; private set; }
    public static string ConnectionStringUserWithSSLPEM { get; private set; }
    public static string connSSLURI { get; private set; }
    public static bool ServerIsDown { get; set; }
    #endregion

    #region Ctor
    static BaseTest()
    {
      ConnectionStringRoot = $"server={Host};port={Port};uid={RootUser};password={RootPassword}";
      ConnectionString = $"server={Host};port={XPort};uid=test;password=test";
      ConnectionStringNoPassword = $"server={Host};port={XPort};uid=testNoPass;";
      ConnectionStringUri = $"mysqlx://test:test@{Host}:{XPort}";
      ConnectionStringUriNative = $"mysqlx://testNative:test@{Host}:{XPort}";
      ConnectionStringUserWithSSLPEM = $"server={Host};user=test;port={XPort};password=test;SslCa={sslCa};SslCert={sslCert};SslKey={sslKey}";
      connSSLURI = ConnectionStringUri + $"?Ssl-ca={sslCa}&SslCert={sslCert}&SslKey={sslKey}&Auth=AUTO";
    }
    #endregion

    #region SetUp
    [OneTimeSetUp]
    public virtual void BaseSetUp()
    {
      Assembly executingAssembly = typeof(BaseTest).GetTypeInfo().Assembly;
      Stream stream = executingAssembly.GetManifestResourceStream("MySqlX.Data.Tests.Properties.CreateUsers.sql");
      StreamReader sr = new StreamReader(stream);
      string sql = sr.ReadToEnd();
      sr.Dispose();
      ExecuteSqlAsRoot(sql);
      session = GetSession();
      testSchema = session.GetSchema(schemaName);
      if (SchemaExistsInDatabase(testSchema))
        session.DropSchema(schemaName);
      session.CreateSchema(schemaName);
    }
    #endregion

    #region TearDown
    [OneTimeTearDown]
    public virtual void BaseTearDown()
    {
      using (Session s = GetSession())
      {
        Schema schema = s.GetSchema(schemaName);
        if (SchemaExistsInDatabase(schema))
          s.DropSchema(schemaName);
        schema.Session.DropSchema(schemaName);
        Assert.False(SchemaExistsInDatabase(schema));
      }

      DropUsers();
    }
    #endregion

    #region Methods
    private void DropUsers()
    {
      var users = FillTable("SELECT user, host FROM mysql.user WHERE user NOT LIKE 'mysql%' AND user NOT LIKE 'root'");
      foreach (DataRow row in users.Rows)
        ExecuteSqlAsRoot(string.Format("DROP USER '{0}'@'{1}'", row[0].ToString(), row[1].ToString()));
      ExecuteSqlAsRoot("FLUSH PRIVILEGES");
    }

    public DataTable FillTable(string sql)
    {
      DataTable dt = new DataTable();
      using (var conn = new MySqlConnection(ConnectionStringRoot))
      {
        conn.Open();
        MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
        da.Fill(dt);
      }
      return dt;
    }

    protected Table GetTable(string schema, string table)
    {
      return GetSession().GetSchema(schema).GetTable(table);
    }

    protected SqlResult ExecuteSQL(string sql)
    {
      Session session = GetSession();
      session.SetCurrentSchema(schemaName);
      SqlResult r = ExecuteSQLStatement(session.SQL(sql));
      var rows = r.HasData ? r.FetchAll() : null;
      return r;
    }

    protected Collection CreateCollection(string name, string schema = null)
    {
      if (schema == null)
        schema = schemaName;
      Schema test = session.GetSchema(schema);
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
      using (var conn = new MySqlConnection(ConnectionStringRoot + ";ssl mode=none"))
      {
        conn.Open();
        MySqlScript s = new(conn, sql);
        s.Execute();
      }
    }

    protected Result ExecuteAddStatement(AddStatement stmt)
    {
      return stmt.Execute();
    }

    protected Result ExecuteModifyStatement(ModifyStatement stmt)
    {
      return stmt.Execute();
    }

    protected DocResult ExecuteFindStatement(FindStatement stmt)
    {
      return stmt.Execute();
    }

    protected SqlResult ExecuteSQLStatement(SqlStatement stmt)
    {
      return stmt.Execute();
    }

    protected Result ExecuteRemoveStatement(RemoveStatement stmt)
    {
      return stmt.Execute();
    }

    protected RowResult ExecuteSelectStatement(TableSelectStatement stmt)
    {
      return stmt.Execute();
    }

    protected Result ExecuteInsertStatement(TableInsertStatement stmt)
    {
      return stmt.Execute();
    }

    protected Result ExecuteUpdateStatement(TableUpdateStatement stmt)
    {
      return stmt.Execute();
    }

    protected Result ExecuteDeleteStatement(TableDeleteStatement stmt)
    {
      return stmt.Execute();
    }

    protected bool SchemaExistsInDatabase(Schema schema)
    {
      return schema.ExistsInDatabase();
    }

    protected bool CollectionExistsInDatabase(Collection collection)
    {
      return collection.ExistsInDatabase();
    }

    protected bool TableExistsInDatabase(Table table)
    {
      return table.ExistsInDatabase();
    }

    public string GetIPV6Address()
    {
      string strHostName = System.Net.Dns.GetHostName();
      IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(strHostName);
      IPAddress[] addr = ipEntry.AddressList;
      return addr[0].ToString();
    }

    /// <summary>
    /// Method to get the local ip address of the active MySql Server
    /// </summary>
    /// <param name="isIpV6">when is true return IPv6(::1), otherwise return IPv4(127.0.0.1) which is the default</param>
    /// <returns>Return the ip address as string</returns>
    public string GetMySqlServerIp(bool isIpV6 = false)
    {
      var hostname = session.SQL("Select SUBSTRING_INDEX(host, ':', 1) as 'ip' From information_schema.processlist WHERE ID = connection_id()").Execute().FetchOne()[0].ToString();
      string ipv4 = string.Empty;
      string ipv6 = string.Empty;

      foreach (var item in Dns.GetHostEntry(hostname).AddressList)
      {
        switch (item.AddressFamily)
        {
          case AddressFamily.InterNetwork:
            ipv4 = item.ToString();
            break;
          case AddressFamily.InterNetworkV6:
            ipv6 = item.ToString();
            break;
        }
      }

      return isIpV6 ? ipv6 : ipv4;
    }

    #endregion
  }
}
