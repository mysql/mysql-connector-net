// Copyright © 2021, 2024, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is designed to work with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms, as
// designated in a particular file or component or in included license
// documentation. The authors of MySQL hereby grant you an additional
// permission to link the program and your derivative works with the
// separately licensed software that they have either included with
// the program or referenced in the documentation.
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

using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;

namespace MySqlX.Data.Tests
{
  public class XAuth : BaseTest
  {

    [Test, Description("User selects DEFAULT as authentication mechanism-(default user,ssl mode none,fresh connection - ensure password is not cached")]
    [Property("Category", "Security")]
    public void DefaultAuthNullPlugin()
    {
      if (!Platform.IsWindows()) Assert.Ignore("Check for Linux OS");
      if (!session.Version.isAtLeast(8, 4, 0)) Assert.Ignore("Test available only to MySQL Server +8.4.0");

      string pluginName = "caching_sha2_password";//default plugin
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(ConnectionString);
      builder.UserID = "testDefaultPlugin";
      builder.Password = "test";
      CreateUser(builder.UserID, builder.Password, pluginName);
      string connectionString = null, connectionStringUri = null;
      string defaultAuthPlugin = session.SQL("SHOW VARIABLES LIKE 'authentication_policy'").Execute().FetchAll()[0][1].ToString().Split(',')[0] == "*" ? "caching_sha2_password" : "";

      //Connection String
      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password}";
      using (var session1 = MySQLX.GetSession(connectionString))
      {
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));
        var result = session1.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.That(result[0][1].ToString().Contains("TLSv1"));
      }

      connectionString = connectionString + ";sslmode=none;";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.That(session1.Settings.Auth, Is.EqualTo(defaultAuthPlugin == "mysql_native_password" ? MySqlAuthenticationMode.MYSQL41 : MySqlAuthenticationMode.SHA256_MEMORY));

      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};ssl-mode=VerifyCA;ssl-ca={sslCa};ssl-ca-pwd=pass;";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      //invalid values
      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};ssl-mode=required;auth=shaa256memory";
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionString));

      //Uri
      connectionStringUri = "mysqlx://" + builder.UserID + ":" + builder.Password + "@" +
          builder.Server + ":" + XPort;
      using (var session1 = MySQLX.GetSession(connectionStringUri))
      {
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));
        var result = session1.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.That(result[0][1].ToString().Contains("TLSv1"));
      }

      connectionStringUri = connectionStringUri + "?sslmode=none";
      using (var session1 = MySQLX.GetSession(connectionStringUri))
        Assert.That(session1.Settings.Auth, Is.EqualTo(defaultAuthPlugin == "mysql_native_password" ? MySqlAuthenticationMode.MYSQL41 : MySqlAuthenticationMode.SHA256_MEMORY));

      //Anonymous Object
      using (var session1 = MySQLX.GetSession(new { server = builder.Server, port = XPort, user = builder.UserID, password = builder.Password }))
      {
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));
        var result = session1.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.That(result[0][1].ToString().Contains("TLSv1"));
      }

      using (var session1 = MySQLX.GetSession(
      new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        sslmode = MySqlSslMode.Disabled,
        password = builder.Password
      }))
        Assert.That(session1.Settings.Auth, Is.EqualTo(defaultAuthPlugin == "mysql_native_password" ? MySqlAuthenticationMode.MYSQL41 : MySqlAuthenticationMode.SHA256_MEMORY));

      ExecuteSQL("flush privileges");
      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};ssl-mode=none";
      if (defaultAuthPlugin == "mysql_native_password")
        Assert.That(MySQLX.GetSession(connectionString), Is.Not.Null);
      else
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionString));

      //URI
      connectionStringUri = $"mysqlx://{builder.UserID}:{builder.Password}@{builder.Server}:{XPort}?sslmode=none";
      if (defaultAuthPlugin == "mysql_native_password")
        Assert.That(MySQLX.GetSession(connectionStringUri), Is.Not.Null);
      else
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionStringUri));

      //Anonymous Object
      if (defaultAuthPlugin == "mysql_native_password")
        Assert.That(MySQLX.GetSession(new
        {
          server = Host,
          port = XPort,
          user = builder.UserID,
          sslmode = MySqlSslMode.Disabled,
          password = builder.Password
        }), Is.Not.Null);
      else
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(new
        {
          server = Host,
          port = XPort,
          user = builder.UserID,
          sslmode = MySqlSslMode.Disabled,
          password = builder.Password
        }));
    }

    [Test, Description("User selects DEFAULT as authentication mechanism-(default user,ssl mode none with allow public key retrieval=true,fresh connection - ensure password is not cached")]
    [Property("Category", "Security")]
    public void DefaultAuthPublicKeyRetrieval()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("Test available only to MySQL Server +8.0.11");

      string pluginName = "caching_sha2_password";//default plugin
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(ConnectionString);
      builder.UserID = "testDefaultPlugin";
      builder.Password = "test";
      CreateUser(builder.UserID, builder.Password, pluginName);
      string connectionString = null, connectionStringUri = null;
      //Flush Privileges
      ExecuteSQL("flush privileges");
      connectionString = "server=" + Host + ";user=" + builder.UserID + ";" + "port=" + XPort +
                         ";password=" + builder.Password + ";ssl-mode=none;allowpublickeyretrieval=true;pooling=false";
      Assert.Catch(() => MySQLX.GetSession(connectionString));
      connectionString = "server=" + Host + ";user=" + builder.UserID + ";" + "port=" + XPort +
                         ";password=" + builder.Password + ";ssl-mode=none;AllowPublicKeyRetrieval=true;pooling=false";
      Assert.Catch(() => MySQLX.GetSession(connectionString));


      ExecuteSQL("flush privileges");
      connectionStringUri = "mysqlx://" + builder.UserID + ":" + builder.Password + "@" +
           builder.Server + ":" + XPort + "?sslmode=none&allowpublickeyretrieval=true&pooling=false";
      Assert.Catch(() => MySQLX.GetSession(connectionString));
      connectionStringUri = "mysqlx://" + builder.UserID + ":" + builder.Password + "@" +
                 builder.Server + ":" + XPort + "?sslmode=none&AllowPublicKeyRetrieval=false&pooling=false";
      Assert.Catch(() => MySQLX.GetSession(connectionString));

      ExecuteSQL("flush privileges");
      Assert.Catch(() => MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        sslmode = MySqlSslMode.Disabled,
        allowpublickeyretrieval = true,
        pooling = false,
        password = builder.Password
      }));
      Assert.Catch(() => MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        sslmode = MySqlSslMode.Disabled,
        allowpublickeyretrieval = false,
        pooling = false,
        password = builder.Password
      }));
    }

    [Test, Description("User selects DEFAULT as authentication mechanism-mysql_native_password user,ssl mode default,fresh connection")]
    public void MySqlNativePlugin()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("Test available only to MySQL Server +8.0.11");
      if (!Check_Plugin_Enabled("mysql_native_password"))
        Assert.Ignore("mysql_native_password plugin must be enabled on the server to run this test");

      string pluginName = "mysql_native_password";//mysql_native_password  plugin
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(ConnectionString);
      builder.UserID = "testDefaultPlugin";
      builder.Password = "test";
      CreateUser(builder.UserID, builder.Password, pluginName);
      string connectionString = null, connectionStringUri = null;
      //Connection String
      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password}";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      connectionString += ";ssl-mode=VerifyCA;ssl-ca=" + sslCa + ";ssl-ca-pwd=pass;";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};ssl-mode=none";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};ssl-mode=Required;ssl-ca={sslCa};ssl-ca-pwd=pass;";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      //URI
      ExecuteSQL("flush privileges");
      connectionStringUri = $"mysqlx://{builder.UserID}:{builder.Password}@{builder.Server}:{XPort}";
      using (var session1 = MySQLX.GetSession(connectionStringUri))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      connectionStringUri = connectionStringUri + "?ssl-mode=none";
      using (var session1 = MySQLX.GetSession(connectionStringUri))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      //Anonymous Object
      ExecuteSQL("flush privileges");
      using (var session1 = MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        password = builder.Password
      }))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      using (var session1 = MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        sslmode = MySqlSslMode.Disabled,
        password = builder.Password
      }))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingMySQL41Auth()
    {
      if (!Check_Plugin_Enabled("mysql_native_password"))
        Assert.Ignore("mysql_native_password plugin must be enabled on the server to run this test");
      ExecuteSqlAsRoot("CREATE USER IF NOT EXISTS 'testNative'@'%' identified with mysql_native_password by 'test';");

      var connectionStringUri = ConnectionStringUri;
      if (session.InternalSession.GetServerVersion().isAtLeast(8, 0, 4))
      {
        // Use connection string uri set with a mysql_native_password user.
        connectionStringUri = ConnectionStringUriNative;
      }

      using (var session = MySQLX.GetSession(connectionStringUri + "?auth=MySQL41"))
      {
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        Assert.That(session.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));
      }

      using (var session = MySQLX.GetSession(connectionStringUri + "?auth=mysql41&sslmode=none"))
      {
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        Assert.That(session.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void DefaultAuth()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 5)) return;

      string user = "testsha256";

      ExecuteSQLStatement(session.SQL($"DROP USER IF EXISTS {user}@'%'"));
      ExecuteSQLStatement(session.SQL($"CREATE USER {user}@'%' IDENTIFIED WITH caching_sha2_password BY '{user}'"));

      string connString = $"mysqlx://{user}:{user}@{Host}:{XPort}";
      // Default to PLAIN when TLS is enabled.
      using (var session = MySQLX.GetSession(connString))
      {
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        Assert.That(session.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));
        var result = ExecuteSQLStatement(session.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';")).FetchAll();
        Assert.That(result[0][1].ToString(), Does.StartWith("TLSv1"));
      }

      // Default to SHA256_MEMORY when TLS is not enabled.
      using (var session = MySQLX.GetSession(connString + "?sslmode=none"))
      {
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        Assert.That(session.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.SHA256_MEMORY));
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingSha256Memory()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 5)) return;

      using (var session = MySQLX.GetSession(ConnectionStringUri + "?auth=SHA256_MEMORY"))
      {
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        Assert.That(session.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.SHA256_MEMORY));
        var result = session.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.That(result[0][1].ToString().Contains("TLSv1"));
      }

      using (var session = MySQLX.GetSession(ConnectionStringUri + "?auth=SHA256_MEMORY&sslmode=none"))
      {
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        Assert.That(session.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.SHA256_MEMORY));
      }

      using (var session1 = MySQLX.GetSession(
        new
        {
          server = Host,
          port = XPort,
          user = session.Settings.UserID,
          sslmode = MySqlSslMode.Disabled,
          password = session.Settings.Password,
          auth = MySqlAuthenticationMode.SHA256_MEMORY
        }))
      {
        Assert.That(session1.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.SHA256_MEMORY));
      }

      //Exceptions
      var cs = $"server={Host};user={session.Settings.UserID};port={XPort};password=;ssl-mode=none;auth=SHA256_MEMORY";
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(cs));
      cs = $"mysqlx://{session.Settings.UserID}:@{Host}:{XPort}?sslmode=none&auth=SHA256_MEMORY";
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(cs));
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = session.Settings.UserID,
        sslmode = MySqlSslMode.Disabled,
        password = "",
        auth = MySqlAuthenticationMode.SHA256_MEMORY
      }));

    }

    [TestCase("mysql_native_password")]
    [TestCase("sha256_password")]
    [Property("Category", "Security")]
    public void Sha256MemoryAuthWithDifferentPlugin(string pluginName)
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("Test available only to MySQL Server +8.0.11");

      if (pluginName == "mysql_native_password")
      {
        if (!Check_Plugin_Enabled("mysql_native_password"))
          Assert.Ignore("mysql_native_password plugin must be enabled on the server to run this test");
      }
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(ConnectionString);
      builder.UserID = "testDefaultPlugin";
      builder.Password = "test";
      CreateUser(builder.UserID, builder.Password, pluginName);
      string connectionString = null, connectionStringUri = null;
      //Connection string
      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};auth = SHA256_MEMORY";
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionString));

      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};sslmode=none;auth=SHA256_MEMORY";
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionString));
      ExecuteSQL("flush privileges");
      //Uri
      connectionStringUri = $"mysqlx://{builder.UserID}:{builder.Password}@{builder.Server}:{XPort}?auth=SHA256_MEMORY";
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionStringUri));

      connectionStringUri = $"mysqlx://{builder.UserID}:{builder.Password}@{builder.Server}:{XPort}?sslmode=none;auth=SHA256_MEMORY";
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionStringUri));
      ExecuteSQL("flush privileges");
      //Anonymous Object
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        password = builder.Password,
        auth = MySqlAuthenticationMode.SHA256_MEMORY
      }));

      Assert.Throws<MySqlException>(() => MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        password = builder.Password,
        sslmode = MySqlSslMode.Disabled,
        auth = MySqlAuthenticationMode.SHA256_MEMORY
      }));
    }

    [Test, Description("Test MySQLX plugin Extern Support")]
    [Property("Category", "Security")]
    public void NativeAuthValidAndInvalidConnection()
    {
      if (!Platform.IsWindows()) Assert.Ignore("Check for Linux OS");
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("Test available only to MySQL Server +8.0.11");
      if (!Check_Plugin_Enabled("mysql_native_password"))
        Assert.Ignore("mysql_native_password plugin must be enabled on the server to run this test");
      ExecuteSqlAsRoot("CREATE USER IF NOT EXISTS 'testNative'@'%' identified with mysql_native_password by 'test';");

      var user = "testNative";
      var pwd = "test";
      var cs = $"server={Host};user={user};port={XPort};password={pwd}";
      //Connection String
      using (var session1 = MySQLX.GetSession(ConnectionString))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      using (var session1 = MySQLX.GetSession(cs + ";auth=mysql41"))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      using (var session1 = MySQLX.GetSession(cs + ";auth=mysql41;ssl-mode=none"))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      using (var session1 = MySQLX.GetSession(cs + ";auth=mysql41;ssl-mode=Required"))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      using (var session1 = MySQLX.GetSession(ConnectionString + ";auth=PLAIN"))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      using (var session1 = MySQLX.GetSession(ConnectionString + ";auth=plain;ssl-mode=Required"))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      using (var session1 = MySQLX.GetSession(cs + ";ssl-mode=none"))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      using (var session1 = MySQLX.GetSession(ConnectionString + ";ssl-mode=Required"))
      {
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));
        var result = session.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.That(result[0][1].ToString().Contains("TLSv1"));
      }

      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";auth=PLAIN;ssl-mode=none"));
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";auth=EXTERNAL"));
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";auth=EXTERNAL;ssl-mode=none"));
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";auth=EXTERNAL;ssl-mode=Required"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionString + ";auth=INVALID"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionString + ";auth=INVALID;ssl-mode=none"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionString + ";auth=INVALID;ssl-mode=Required"));
      //Uri
      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?auth=MySQL41"))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?auth=MySQL41&ssl-mode=none"))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?auth=MySQL41&ssl-mode=Required"))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?auth=PLAIN"))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=PLAIN&ssl-mode=none"));

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?auth=PLAIN&ssl-mode=Required"))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?ssl-mode=none"))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?ssl-mode=Required"))
      {
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));
        var result = session.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.That(result[0][1].ToString().Contains("TLSv1"));
      }

      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=EXTERNAL"));
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=EXTERNAL&ssl-mode=none"));
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=EXTERNAL&ssl-mode=Required"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=INVALID"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=INVALID&ssl-mode=none"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=INVALID&ssl-mode=Required"));
      //Anonymous Object
      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd
      }))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.MYSQL41
      }))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.MYSQL41,
        sslmode = MySqlSslMode.Disabled
      }))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.MYSQL41,
        sslmode = MySqlSslMode.Required
      }))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.PLAIN
      }))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      Assert.Throws<MySqlException>(() => MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.PLAIN,
        sslmode = MySqlSslMode.Disabled
      }));

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.PLAIN,
        sslmode = MySqlSslMode.Required
      }))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        sslmode = MySqlSslMode.Disabled
      }))
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.MYSQL41));

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        sslmode = MySqlSslMode.Required
      }))
      {
        Assert.That(session1.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));
        var result = session1.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.That(result[0][1].ToString().Contains("TLSv1"));
      }

      Assert.Throws<MySqlException>(() => MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.EXTERNAL
      }));

    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingExternalAuth()
    {
      // Should fail since EXTERNAL is currently not supported by X Plugin.
      Exception ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";auth=EXTERNAL"));
      Assert.That(ex.Message, Is.EqualTo("Invalid authentication method EXTERNAL"));

      ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUri + "?auth=EXTERNAL"));
      Assert.That(ex.Message, Is.EqualTo("Invalid authentication method EXTERNAL"));
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingPlainAuth()
    {
      using (var session = MySQLX.GetSession(ConnectionString + ";auth=pLaIn"))
      {
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        Assert.That(session.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));
      }

      using (var session = MySQLX.GetSession(ConnectionStringUri + "?auth=pLaIn"))
      {
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        Assert.That(session.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.PLAIN));
      }

      // Should fail since PLAIN requires TLS to be enabled.
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUri + "?auth=PLAIN&sslmode=none"));
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingSha256PasswordPlugin()
    {
      ExecuteSqlAsRoot($"DROP USER IF EXISTS 'testSha256'@'%';");
      ExecuteSqlAsRoot($"CREATE USER 'testSha256'@'%' identified with sha256_password by 'mysql';");
      ExecuteSqlAsRoot($"GRANT ALL PRIVILEGES  ON *.*  TO 'testSha256'@'%';");

      string userName = "testSha256";
      string password = "mysql";
      string pluginName = "sha256_password";
      string connectionStringUri = ConnectionStringUri.Replace("test:test", string.Format("{0}:{1}", userName, password));

      // User with password over TLS connection.
      using (var session = MySQLX.GetSession(connectionStringUri))
      {
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        var result = ExecuteSQLStatement(session.SQL(string.Format("SELECT `User`, `plugin` FROM `mysql`.`user` WHERE `User` = '{0}';", userName))).FetchAll();
        Assert.That(session.Settings.UserID, Is.EqualTo(userName));
        Assert.That(result[0][0].ToString(), Is.EqualTo(session.Settings.UserID));
        Assert.That(result[0][1].ToString(), Is.EqualTo(pluginName));
      }

      // Connect over non-TLS connection.
      using (var session = MySQLX.GetSession(connectionStringUri + "?sslmode=none"))
      {
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        Assert.That(session.Settings.Auth, Is.EqualTo(MySqlAuthenticationMode.SHA256_MEMORY));
      }

      // User without password over TLS connection.
      ExecuteSQL($"ALTER USER {userName}@'%' IDENTIFIED BY ''");
      using (var session = MySQLX.GetSession(ConnectionStringUri.Replace("test:test", string.Format("{0}:{1}", userName, ""))))
      {
        Assert.That(session.InternalSession.SessionState, Is.EqualTo(SessionState.Open));
        var result = ExecuteSQLStatement(session.SQL(string.Format("SELECT `User`, `plugin` FROM `mysql`.`user` WHERE `User` = '{0}';", userName))).FetchAll();
        Assert.That(session.Settings.UserID, Is.EqualTo(userName));
        Assert.That(result[0][0].ToString(), Is.EqualTo(session.Settings.UserID));
        Assert.That(result[0][1].ToString(), Is.EqualTo(pluginName));
      }
    }

    public string CreateUser(string userName, string password, string plugin)
    {
      string host = Host == "localhost" ? Host : "%";

      ExecuteSqlAsRoot(String.Format("DROP USER IF EXISTS '{0}'@'{1}';", userName, host));
      ExecuteSqlAsRoot(
        String.Format(
          "CREATE USER '{0}'@'{1}' IDENTIFIED {2} BY '{3}'", userName, host,
          (plugin == null ? string.Empty : String.Format("WITH '{0}' ", plugin)), password));

      ExecuteSqlAsRoot(String.Format("GRANT ALL ON *.* TO '{0}'@'{1}'", userName, host));
      ExecuteSqlAsRoot("FLUSH PRIVILEGES");
      return userName;
    }
  }
}
