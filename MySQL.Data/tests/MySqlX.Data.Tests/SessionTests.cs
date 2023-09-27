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

using MySql.Data;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Relational;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlX.Data.Tests
{
  public class SessionTests : BaseTest
  {
    [Test]
    [Property("Category", "Security")]
    public void CanCloseSession()
    {
      Session s = MySQLX.GetSession(ConnectionString);
      Assert.True(s.InternalSession.SessionState == SessionState.Open);
      s.Close();
      Assert.AreEqual(s.InternalSession.SessionState, SessionState.Closed);
    }

    [Test]
    [Property("Category", "Security")]
    public void NoPassword()
    {
      Session session = MySQLX.GetSession(ConnectionStringNoPassword);
      Assert.True(session.InternalSession.SessionState == SessionState.Open);
      session.Close();
      Assert.AreEqual(session.InternalSession.SessionState, SessionState.Closed);
    }

    [Test]
    [Property("Category", "Security")]
    public void SessionClose()
    {
      Session session = MySQLX.GetSession(ConnectionString);
      Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
      session.Close();
      Assert.AreEqual(SessionState.Closed, session.InternalSession.SessionState);
    }

    [Test]
    [Property("Category", "Security")]
    [Ignore("Check this. Result is not always the same")]
    public void CountClosedSession()
    {
      int sessions, newSessions;

      using (Session nodeSession = MySQLX.GetSession(ConnectionString))
      {
        sessions = ExecuteSQLStatement(nodeSession.SQL("show processlist")).FetchAll().Count;

        for (int i = 0; i < 20; i++)
        {
          Session session = MySQLX.GetSession(ConnectionString);
          Assert.True(session.InternalSession.SessionState == SessionState.Open);
          session.Close();
          Assert.AreEqual(session.InternalSession.SessionState, SessionState.Closed);
        }

        newSessions = ExecuteSQLStatement(nodeSession.SQL("show processlist")).FetchAll().Count;
      }

      Assert.AreEqual(sessions, newSessions);
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectionStringAsAnonymousType()
    {
      var connstring = new
      {
        server = session.Settings.Server,
        port = session.Settings.Port,
        user = session.Settings.UserID,
        password = session.Settings.Password
      };

      using (var testSession = MySQLX.GetSession(connstring))
      {
        Assert.AreEqual(SessionState.Open, testSession.InternalSession.SessionState);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void SessionGetSetCurrentSchema()
    {
      using (Session testSession = MySQLX.GetSession(ConnectionString))
      {
        Assert.AreEqual(SessionState.Open, testSession.InternalSession.SessionState);
        Assert.Null(testSession.GetCurrentSchema());
        Assert.Throws<MySqlException>(() => testSession.SetCurrentSchema(""));
        testSession.SetCurrentSchema(schemaName);
        Assert.AreEqual(schemaName, testSession.Schema.Name);
        Assert.AreEqual(schemaName, testSession.GetCurrentSchema().Name);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void SessionUsingSchema()
    {
      using (Session mySession = MySQLX.GetSession(ConnectionString + $";database={schemaName};"))
      {
        Assert.AreEqual(SessionState.Open, mySession.InternalSession.SessionState);
        Assert.AreEqual(schemaName, mySession.Schema.Name);
        Assert.AreEqual(schemaName, mySession.GetCurrentSchema().Name);
        Assert.True(SchemaExistsInDatabase(mySession.Schema));
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void SessionUsingDefaultSchema()
    {
      using (Session mySession = MySQLX.GetSession(ConnectionString + $";database={schemaName};"))
      {
        Assert.AreEqual(SessionState.Open, mySession.InternalSession.SessionState);
        Assert.AreEqual(schemaName, mySession.DefaultSchema.Name);
        Assert.AreEqual(schemaName, mySession.GetCurrentSchema().Name);
        Assert.True(mySession.Schema.ExistsInDatabase());
        mySession.SetCurrentSchema("mysql");
        Assert.AreNotEqual(mySession.DefaultSchema.Name, mySession.Schema.Name);
      }

      // DefaultSchema is null because no database was provided in the connection string/URI.
      using (Session mySession = MySQLX.GetSession(ConnectionString))
      {
        Assert.AreEqual(SessionState.Open, mySession.InternalSession.SessionState);
        Assert.Null(mySession.DefaultSchema);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void SessionUsingDefaultSchemaWithAnonymousObject()
    {
      var globalSession = GetSession();

      using (var internalSession = MySQLX.GetSession(new
      {
        server = globalSession.Settings.Server,
        port = globalSession.Settings.Port,
        user = globalSession.Settings.UserID,
        password = globalSession.Settings.Password,
        sslmode = MySqlSslMode.Required,
        database = "mysql"
      }))
      {
        Assert.AreEqual("mysql", internalSession.DefaultSchema.Name);
      }

      // DefaultSchema is null when no database is provided.
      using (var internalSession = MySQLX.GetSession(new
      {
        server = globalSession.Settings.Server,
        port = globalSession.Settings.Port,
        user = globalSession.Settings.UserID,
        password = globalSession.Settings.Password,
        sslmode = MySqlSslMode.Required,
      }))
      {
        Assert.Null(internalSession.DefaultSchema);
      }

      // Access denied error is raised when database does not exist for servers 8.0.12 and below.
      // This behavior was fixed since MySql Server 8.0.13 version. Now the error 
      // shows the proper message, "Unknown database..."
      if (session.InternalSession.GetServerVersion().isAtLeast(8, 0, 13)) return;
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession(new
      {
        server = globalSession.Settings.Server,
        port = globalSession.Settings.Port,
        user = globalSession.Settings.UserID,
        password = globalSession.Settings.Password,
        sslmode = MySqlSslMode.Required,
        database = "test1"
      }
      ));

      if (session.InternalSession.GetServerVersion().isAtLeast(8, 0, 13))
        StringAssert.StartsWith(string.Format("Unknown database 'test1'"), exception.Message);
      else
        StringAssert.StartsWith(string.Format("Access denied"), exception.Message);
    }

    [Test]
    [Property("Category", "Security")]
    public void SessionUsingDefaultSchemaWithConnectionURI()
    {
      using (var session = MySQLX.GetSession(ConnectionStringUri + "?database=mysql"))
      {
        Assert.AreEqual("mysql", session.DefaultSchema.Name);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void CheckConnectionUri()
    {
      CheckConnectionData($"mysqlx://myuser:password@{Host}:{XPort}", "myuser", "password", Host, uint.Parse(XPort));
      CheckConnectionData($"mysqlx://my%3Auser:p%40ssword@{Host}:{XPort}", "my:user", "p@ssword", Host, uint.Parse(XPort));
      CheckConnectionData($"mysqlx://my%20user:p%40ss%20word@{Host}:{XPort}", "my user", "p@ss word", Host, uint.Parse(XPort));
      CheckConnectionData($"mysqlx:// myuser : p%40ssword@{Host}:{XPort}", "myuser", "p@ssword", Host, uint.Parse(XPort));
      CheckConnectionData($"mysqlx://myuser@{Host}:{XPort}", "myuser", "", Host, uint.Parse(XPort));
      CheckConnectionData($"mysqlx://myuser:p%40ssword@{Host}", "myuser", "p@ssword", Host, uint.Parse(XPort));
      CheckConnectionData($"mysqlx://myuser:p%40ssw%40rd@{Host}", "myuser", "p@ssw@rd", Host, uint.Parse(XPort));
      CheckConnectionData($"mysqlx://my%40user:p%40ssword@{Host}", "my@user", "p@ssword", Host, uint.Parse(XPort));
      CheckConnectionData($"mysqlx://myuser@{Host}", "myuser", "", Host, uint.Parse(XPort));
      CheckConnectionData($"mysqlx://myuser@{Host}", "myuser", "", Host, uint.Parse(XPort));
      CheckConnectionData("mysqlx://myuser@[::1]", "myuser", "", "[::1]", uint.Parse(XPort));
      CheckConnectionData("mysqlx://myuser:password@[2606:b400:440:1040:bd41:e449:45ee:2e1a]", "myuser", "password", "[2606:b400:440:1040:bd41:e449:45ee:2e1a]", uint.Parse(XPort));
      CheckConnectionData($"mysqlx://myuser:password@[2606:b400:440:1040:bd41:e449:45ee:2e1a]:{XPort}", "myuser", "password", "[2606:b400:440:1040:bd41:e449:45ee:2e1a]", uint.Parse(XPort));
      Assert.Throws<UriFormatException>(() => CheckConnectionData("mysqlx://myuser:password@[2606:b400:440:1040:bd41:e449:45ee:2e1a:33060]", "myuser", "password", "[2606:b400:440:1040:bd41:e449:45ee:2e1a]", uint.Parse(XPort)));
      Assert.Throws<UriFormatException>(() => CheckConnectionData($"mysqlx://myuser:password@2606:b400:440:1040:bd41:e449:45ee:2e1a:{XPort}", "myuser", "password", "[2606:b400:440:1040:bd41:e449:45ee:2e1a]", uint.Parse(XPort)));
      CheckConnectionData("mysqlx://myuser:password@[fe80::bd41:e449:45ee:2e1a%17]", "myuser", "password", "[fe80::bd41:e449:45ee:2e1a]", uint.Parse(XPort));
      CheckConnectionData("mysqlx://myuser:password@[(address=[fe80::bd41:e449:45ee:2e1a%17],priority=100)]", "myuser", "password", "[fe80::bd41:e449:45ee:2e1a]", uint.Parse(XPort));
      CheckConnectionData("mysqlx://myuser:password@[(address=[fe80::bd41:e449:45ee:2e1a%17]:3305,priority=100)]", "myuser", "password", "[fe80::bd41:e449:45ee:2e1a]", 3305);
      Assert.Throws<UriFormatException>(() => CheckConnectionData("mysqlx://myuser:password@[(address=fe80::bd41:e449:45ee:2e1a%17,priority=100)]", "myuser", "password", "[fe80::bd41:e449:45ee:2e1a]", 33060));
      CheckConnectionData("mysqlx://myuser@localhost/test", "myuser", "", "localhost", 33060, "database", schemaName);
#if NET7_0
      CheckConnectionData("mysqlx://myuser@localhost/test?ssl%20mode=disabled&connecttimeout=10", "myuser", "", "localhost", 33060, "database", schemaName, "ssl mode", "None", "connecttimeout", "10");
#else
      CheckConnectionData("mysqlx://myuser@localhost/test?ssl%20mode=disabled&connecttimeout=10", "myuser", "", "localhost", 33060, "database", schemaName, "ssl mode", "Disabled", "connecttimeout", "10");
#endif
      CheckConnectionData("mysqlx://_%21%22%23%24s%26%2F%3D-%25r@localhost", "_!\"#$s&/=-%r", "", "localhost", 33060);
      CheckConnectionData("mysql://myuser@localhost", "", "", "", 33060);
      CheckConnectionData("myuser@localhost", "", "", "", 33060);
      Assert.Throws<UriFormatException>(() => CheckConnectionData("mysqlx://uid=myuser;server=localhost", "", "", "", 33060));
      CheckConnectionData("mysqlx://user:password@server.example.com/", "user", "password", "server.example.com", 33060, "ssl mode", "Required");
      CheckConnectionData("mysqlx://user:password@server.example.com/?ssl-ca=(c:%5Cclient.pfx)", "user", "password", "server.example.com", 33060, "ssl mode", "Required", "ssl-ca", "c:\\client.pfx");
      Assert.Throws<NotSupportedException>(() => CheckConnectionData("mysqlx://user:password@server.example.com/?ssl-crl=(c:%5Ccrl.pfx)", "user", "password", "server.example.com", 33060, "ssl mode", "Required", "ssl-crl", "(c:\\crl.pfx)"));
      // tls-version
      CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version=TlSv1.2", "myuser", "password", "localhost", 33060, "tls-version", "Tls12");
      CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version=TlS1.2", "myuser", "password", "localhost", 33060, "tls-version", "Tls12");
      CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version=TlSv12", "myuser", "password", "localhost", 33060, "tls-version", "Tls12");
      CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version=TlS12", "myuser", "password", "localhost", 33060, "tls-version", "Tls12");
      CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version=[ TlSv1.2 ,tLsV11, TLSv1.0 , tls13 ]", "myuser", "password", "localhost", 33060, "tls-version", "Tls12, Tls13");
      CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version=( TlSv1.2 ,tLsV11, TLSv1 , tls13 )", "myuser", "password", "localhost", 33060, "tls-version", "Tls12, Tls13");
      CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version= TlSv1.2 ,tLsV11, TLSv10 , tls13", "myuser", "password", "localhost", 33060, "tls-version", "Tls12, Tls13");
      Assert.Throws<ArgumentException>(() => CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version=SSL3", "myuser", "password", "localhost", 33060, "tls-version", ""));
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectionUsingUri()
    {
      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectionStringNull()
    {
      Assert.Throws<ArgumentNullException>(() => MySQLX.GetSession(null));
    }

    [Test]
    [Property("Category", "Security")]
    public void IPv6()
    {
      var csBuilder = new MySqlXConnectionStringBuilder(ConnectionString);
      csBuilder.Server = GetMySqlServerIp(true);
      csBuilder.Port = uint.Parse(XPort);

      if (string.IsNullOrEmpty(csBuilder.Server)) Assert.Ignore("No IPv6 available.");

      using (var session = MySQLX.GetSession(csBuilder.ToString()))
      {
        Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void IPv6AsUrl()
    {
      var csBuilder = new MySqlXConnectionStringBuilder(ConnectionString);
      string ipv6 = GetMySqlServerIp(true);
      if (string.IsNullOrEmpty(ipv6)) Assert.Ignore("No IPv6 available.");

      string connString = $"mysqlx://{csBuilder.UserID}:{csBuilder.Password}@[{ipv6}]:{XPort}";
      using (Session session = MySQLX.GetSession(connString))
      {
        Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void IPv6AsAnonymous()
    {
      var csBuilder = new MySqlXConnectionStringBuilder(ConnectionString);
      string ipv6 = GetMySqlServerIp(true);
      if (string.IsNullOrEmpty(ipv6)) Assert.Ignore("No IPv6 available.");

      using (Session session = MySQLX.GetSession(new { server = ipv6, user = csBuilder.UserID, password = csBuilder.Password, port = XPort }))
      {
        Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void CreateSessionWithUnsupportedOptions()
    {
      var errorMessage = "Option not supported.";
      var connectionUri = string.Format("{0}?", ConnectionStringUri);

      // Use a connection URI.
      var ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "pipe=MYSQL"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "compress=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "allow batch=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "logging=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "sharedmemoryname=MYSQL"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "defaultcommandtimeout=30"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "usedefaultcommandtimeoutforef=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "persistsecurityinfo=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "encrypt=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "integratedsecurity=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "allowpublickeyretrieval=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "autoenlist=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "includesecurityasserts=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "allowzerodatetime=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "convert zero datetime=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "useusageadvisor=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "procedurecachesize=50"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "useperformancemonitor=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "respectbinaryflags=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "treat tiny as boolean=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "allowuservariables=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "interactive=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "functionsreturnstring=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "useaffectedrows=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "oldguids=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "sqlservermode=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "tablecaching=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "defaulttablecacheage=60"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "checkparameters=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "replication=replication_group"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "exceptioninterceptors=none"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "commandinterceptors=none"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "connectionlifetime=100"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "pooling=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "minpoolsize=0"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "maxpoolsize=20"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "connectionreset=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "cacheserverproperties=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);

      // Use a connection string.
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("treatblobsasutf8=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("blobasutf8includepattern=pattern"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("blobasutf8excludepattern=pattern"));
      StringAssert.StartsWith(errorMessage, ex.Message);
    }

    [Test]
    [Property("Category", "Security")]
    public void CreateBuilderWithUnsupportedOptions()
    {
      var errorMessage = "Option not supported.";
      var ex = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder("pipe=MYSQL"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder("allow batch=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder("respectbinaryflags=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder("pooling=false"));
      StringAssert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder("cacheserverproperties=true"));
      StringAssert.StartsWith(errorMessage, ex.Message);
    }

    [Test]
    [Property("Category", "Security")]
    public void GetUri()
    {
      using (var internalSession = MySQLX.GetSession(session.Uri))
      {
        // Validate that all properties keep their original value.
        foreach (var connectionOption in session.Settings.values)
        {
          // SslCrl connection option is skipped since it isn't currently supported.
          if (connectionOption.Key == "sslcrl")
            continue;

          try
          {
            Assert.AreEqual(session.Settings[connectionOption.Key], internalSession.Settings[connectionOption.Key]);
          }
          catch (ArgumentException ex)
          {
            StringAssert.StartsWith("Option not supported.", ex.Message);
          }
        }
      }
    }

    /// <summary>
    /// WL #12177 Implement connect timeout
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void ConnectTimeout()
    {
      if (Platform.IsMacOSX()) Assert.Ignore("Check failure on MacOS: <System.Net.Internals.SocketExceptionFactory+ExtendedSocketException (51): Network is unreachable 143.24.20.36:33060");

      // Create a session passing the new parameter "connect-timeout" and set it to a valid value.
      // ConnectionString.
      using (Session session = MySQLX.GetSession(ConnectionString + ";connect-timeout=5000;"))
      {
        Assert.True(session.Settings.ConnectTimeout == 5000);
        Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
      }

      // ConnectionURI.
      using (Session session = MySQLX.GetSession(ConnectionStringUri + "?connecttimeout=6500"))
      {
        Assert.True(session.Settings.ConnectTimeout == 6500);
        Assert.AreEqual(SessionState.Open, session.InternalSession.SessionState);
      }

      // Anonymous Object using default value, 10000ms.
      var connstring = new
      {
        server = session.Settings.Server,
        port = session.Settings.Port,
        user = session.Settings.UserID,
        password = session.Settings.Password,
        connecttimeout = session.Settings.ConnectTimeout
      };

      using (var testSession = MySQLX.GetSession(connstring))
      {
        Assert.True(session.Settings.ConnectTimeout == 10000);
        Assert.AreEqual(SessionState.Open, testSession.InternalSession.SessionState);
      }

      // Create a session using the fail over functionality passing two diferrent Server address(one of them is fake host). Must succeed after 2000ms
      var conn = $"server=143.24.20.36,{Host};user=test;password=test;port={XPort};connecttimeout=2000;";
      TestConnectTimeoutSuccessTimeout(conn, 0, 5, "Fail over success");

      // Offline (fake)host using default value, 10000ms.
      conn = $"server=143.24.20.36;user=test;password=test;port={XPort};";
      TestConnectTimeoutFailureTimeout(conn, 9, 20, "Offline host default value");

      // Offline (fake)host using 15000ms.
      conn = $"server=143.24.20.36;user=test;password=test;port={XPort};connecttimeout=15000";
      TestConnectTimeoutFailureTimeout(conn, 14, 17, "Offline host 15000ms");

      // Offline (fake)host timeout disabled. Commented due to unexpected behavior
      //conn = $"server=143.24.20.36;user=test;password=test;port={XPort};connecttimeout=0";
      //TestConnectTimeoutFailureTimeout(conn, 10, 600, "Offline host timeout disabled");

      // Both (fake)servers offline. Connection must time out after 20000ms
      conn = $"server=143.24.20.36,143.24.20.35;user=test;password=test;port={XPort};";
      DateTime start = DateTime.Now;
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(conn));
      TimeSpan diff = DateTime.Now.Subtract(start);
      Assert.True(diff.TotalSeconds > 19 && diff.TotalSeconds < 21, String.Format("Timeout exceeded ({0}). Actual time: {1}", "Fail over failure", diff));

      // Valid session no time out
      start = DateTime.Now;
      using (Session session = MySQLX.GetSession(ConnectionStringUri + "?connecttimeout=2000"))
        session.SQL("SELECT SLEEP(10)").Execute();
      diff = DateTime.Now.Subtract(start);
      Assert.True(diff.TotalSeconds > 10);

      //Invalid Values for Connection Timeout parameter
      var ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionString + ";connect-timeout=-1;"));
      Assert.AreEqual(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionString + ";connect-timeout=foo;"));
      Assert.AreEqual(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionString + ";connect-timeout='';"));
      Assert.AreEqual(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionString + ";connect-timeout=10.5;"));
      Assert.AreEqual(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionString + ";connect-timeout=" + Int32.MaxValue + 1));
      Assert.AreEqual(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionString + ";connect-timeout=10.5;"));
      Assert.AreEqual(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionString + ";connect-timeout=;"));
      Assert.AreEqual(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionStringUri + "?connect-timeout= "));
      Assert.AreEqual(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionStringUri + "?connecttimeout="));
      Assert.AreEqual(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      // Valid value for ConnectionTimeout, invalid credentials
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession($"server={Host};user=test;password=noPass;port={XPort};connect-timeout=2000;"));
      Assert.NotNull(exception);
    }

    private void TestConnectTimeoutFailureTimeout(String connString, int minTime, int maxTime, string test)
    {
      DateTime start = DateTime.Now;
      Assert.Throws<TimeoutException>(() => MySQLX.GetSession(connString));
      TimeSpan diff = DateTime.Now.Subtract(start);
      Assert.True(diff.TotalSeconds > minTime && diff.TotalSeconds < maxTime, String.Format("Timeout exceeded ({0}). Actual time: {1}", test, diff));
    }

    private void TestConnectTimeoutSuccessTimeout(String connString, int minTime, int maxTime, string test)
    {
      DateTime start = DateTime.Now;
      MySQLX.GetSession(connString);
      TimeSpan diff = DateTime.Now.Subtract(start);
      Assert.True(diff.TotalSeconds > minTime && diff.TotalSeconds < maxTime, String.Format("Timeout exceeded ({0}). Actual time: {1}", test, diff));
    }

    [Test]
    [Property("Category", "Security")]
    public void MaxConnections()
    {
      try
      {
        List<Session> sessions = new List<Session>();
        ExecuteSqlAsRoot("SET @@global.mysqlx_max_connections = 2");
        for (int i = 0; i <= 2; i++)
        {
          Session newSession = MySQLX.GetSession(ConnectionString);
          sessions.Add(newSession);
        }
        Assert.False(true, "MySqlException should be thrown");
      }
      catch (MySqlException ex)
      {
        Assert.AreEqual(ResourcesX.UnableToOpenSession, ex.Message);
      }
      finally
      {
        ExecuteSqlAsRoot("SET @@global.mysqlx_max_connections = 100");
      }
    }

    protected void CheckConnectionData(string connectionData, string user, string password, string server, uint port, params string[] parameters)
    {
      string result = this.session.ParseConnectionData(connectionData);
      var csbuilder = new MySqlXConnectionStringBuilder(result);
      Assert.True(user == csbuilder.UserID, string.Format("Expected:{0} Current:{1} in {2}", user, csbuilder.UserID, connectionData));
      Assert.True(password == csbuilder.Password, string.Format("Expected:{0} Current:{1} in {2}", password, csbuilder.Password, connectionData));
      Assert.True(server == csbuilder.Server, string.Format("Expected:{0} Current:{1} in {2}", server, csbuilder.Server, connectionData));
      Assert.True(port == csbuilder.Port, string.Format("Expected:{0} Current:{1} in {2}", port, csbuilder.Port, connectionData));
      if (parameters != null)
      {
        if (parameters.Length % 2 != 0)
          throw new ArgumentOutOfRangeException();
        for (int i = 0; i < parameters.Length; i += 2)
        {
          Assert.True(csbuilder.ContainsKey(parameters[i]));
          Assert.AreEqual(parameters[i + 1], csbuilder[parameters[i]].ToString());
        }
      }
    }

    /// <summary>
    /// WL12514 - DevAPI: Support session-connect-attributes
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void ConnectionAttributes()
    {
      if (!(session.Version.isAtLeast(8, 0, 16))) return;

      // Validate that MySQLX.GetSession() supports a new 'connection-attributes' query parameter
      // with default values and all the client attributes starts with a '_'.
      TestConnectionAttributes(ConnectionString + ";connection-attributes=true;");
      TestConnectionAttributes(ConnectionStringUri + "?connectionattributes");

      // Validate that no attributes, client or user defined, are sent to server when the value is "false".
      TestConnectionAttributes(ConnectionString + ";connection-attributes=false;");
      TestConnectionAttributes(ConnectionStringUri + "?connectionattributes=false");

      // Validate default behavior with different scenarios.
      TestConnectionAttributes(ConnectionString + ";connection-attributes;");
      TestConnectionAttributes(ConnectionStringUri + "?connectionattributes=true");
      TestConnectionAttributes(ConnectionString + ";connection-attributes=;");
      TestConnectionAttributes(ConnectionStringUri + "?connectionattributes=[]");

      // Validate user-defined attributes to be sent to server.
      Dictionary<string, object> userAttrs = new Dictionary<string, object>
      {
        { "foo", "bar" },
        { "quua", "qux" },
        { "key", null }
      };
      TestConnectionAttributes(ConnectionString + ";connection-attributes=[foo=bar,quua=qux,key]", userAttrs);
      TestConnectionAttributes(ConnectionStringUri + "?connectionattributes=[foo=bar,quua=qux,key=]", userAttrs);

      // Errors
      var ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=[_key=value]"));
      Assert.AreEqual(ResourcesX.InvalidUserDefinedAttribute, ex.Message);

      ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=123"));
      Assert.AreEqual(ResourcesX.InvalidConnectionAttributes, ex.Message);

      ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=[key=value,key=value2]"));
      Assert.AreEqual(string.Format(ResourcesX.DuplicateUserDefinedAttribute, "key"), ex.Message);

      ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(new { server = Host, port = XPort, user = RootUser, connectionattributes = "=" }));

      ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connectionattributes=[=bar]"));
      Assert.AreEqual(string.Format(ResourcesX.EmptyKeyConnectionAttribute), ex.Message);
    }

    private void TestConnectionAttributes(string connString, Dictionary<string, object> userAttrs = null)
    {
      string sql = "SELECT * FROM performance_schema.session_account_connect_attrs WHERE PROCESSLIST_ID = connection_id()";

      using (Session session = MySQLX.GetSession(connString))
      {
        Assert.AreEqual(SessionState.Open, session.XSession.SessionState);
        var result = session.SQL(sql).Execute().FetchAll();

        if (session.Settings.ConnectionAttributes == "false")
          CollectionAssert.IsEmpty(result);
        else
        {
          CollectionAssert.IsNotEmpty(result);
          MySqlConnectAttrs clientAttrs = new MySqlConnectAttrs();

          if (userAttrs == null)
          {
            Assert.AreEqual(8, result.Count);

            foreach (Row row in result)
              StringAssert.StartsWith("_", row[1].ToString());
          }
          else
          {
            Assert.AreEqual(11, result.Count);

            for (int i = 0; i < userAttrs.Count; i++)
            {
              Assert.True(userAttrs.ContainsKey(result.ElementAt(i)[1].ToString()));
              Assert.True(userAttrs.ContainsValue(result.ElementAt(i)[2]));
            }
          }
        }
      }
    }

    [TestCase("localhost")]
    [TestCase("127.0.0.1")]
    [TestCase("[::1]")]
    [Description("IPv6 connection Scenario [localhost],[127.0.0.1]")]
    public void ConnectionTest(string serverName)
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test only applies foe Windows OS.");

      serverName = serverName.Replace("localhost", Host);

      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      string connStr = "server=" + Host + ";user=" + sb.UserID + ";port=" + XPort + ";password=" + sb.Password + ";" + "sslmode=" + MySqlSslMode.Required;

      using (var sessionTest = MySQLX.GetSession(connStr))
      {
        Assert.AreEqual(SessionState.Open, sessionTest.InternalSession.SessionState);
      }

      using (var sessionTest = MySQLX.GetSession("mysqlx://" + sb.UserID + ":" + sb.Password + "@" + serverName + ":" + XPort))
      {
        Assert.AreEqual(SessionState.Open, sessionTest.InternalSession.SessionState);
      }

      using (var sessionTest = MySQLX.GetSession(new { server = serverName, port = XPort, user = sb.UserID, password = sb.Password }))
      {
        Assert.AreEqual(SessionState.Open, sessionTest.InternalSession.SessionState);
      }
      //wrong port
      connStr = "server=" + sb.Server + ";user=" + sb.UserID + ";port=" + 33090 + ";password=" + sb.Password + ";" + "sslmode=" + MySqlSslMode.Required;
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connStr));

    }

    [TestCase("[::$]")]
    [TestCase("[::11]")]
    [Description("IPv6 connection server * and ::$,invalid hostname")]
    public void IPv6ConnectionExceptions(string serverName)
    {
      if (!Platform.IsWindows()) return;

      Session sessionTest = null;
      string connStr = "server=" + serverName + ";user=test;port=" + XPort + ";password=test;sslmode=" + MySqlSslMode.Required;
      Assert.Catch(() => sessionTest = MySQLX.GetSession(connStr));
      Assert.Catch(() => sessionTest = MySQLX.GetSession("mysqlx://test:test@" + serverName + ":" + XPort));
      Assert.Catch(() => sessionTest = MySQLX.GetSession(new { server = serverName, port = XPort, user = schemaName, password = schemaName }));
    }

    [Test, Description("Unified connection string refinement-Negative Scenarios")]
    public void ConnectionNegativeScenarios()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      var ipv6HostName2 = GetIPV6Address();
      string ipAddress = GetMySqlServerIp();

      Session session1 = null;
      Assert.Catch(() => session1 = MySQLX.GetSession("mysql:x//test:test@" + ipAddress + ":" + XPort));
      Assert.Catch(() => session1 = MySQLX.GetSession("my:sqlx//test:test@" + ipAddress + ":" + XPort));
      Assert.Catch(() => session1 = MySQLX.GetSession("mysqlx:://test:test@" + ipAddress + ":" + XPort));
      string ipv6address = "f345::" + GetIPV6Address() + ":1xde";
      Assert.Catch(() => session1 = MySQLX.GetSession("mysqlx://test:test@[" + ipv6address + "]:" + XPort));
      Assert.Catch(() => session1 = MySQLX.GetSession("mysqlx://test:test@" + ipAddress + ":" + XPort + "/" + "unknowndatabase"));
      string connStr = "mysqlx://test:test@" + session.Settings.Server + ":" + XPort + "/?" + "ssl-mode=VerifyFull&ssl-ca=" + sslCa + "&ssl-ca-pwd=wrongpass";
      Assert.Catch(() => session1 = MySQLX.GetSession(connStr));
    }

    [Test, Description("Session.Uri")]
    public void SessionUriAndDefaultSchemaTest()
    {
      if (!Platform.IsWindows()) return;

      using (var session1 = MySQLX.GetSession(ConnectionString))
      {
        Assert.IsNotNull(session1.Uri);
      }

      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      var connectionString = ConnectionStringUserWithSSLPEM + ";protocol=TCP;database="
          + sb.Database + ";characterset=utf8mb4;sslmode=Required;connect-timeout=10;keepalive=10;auth=PLAIN";
      using (var session1 = MySQLX.GetSession(connectionString))
      {
        Assert.IsNotNull(session1.Uri);
      }

      using (var session1 = MySQLX.GetSession(new
      {
        server = sb.Server,
        port = XPort,
        user = sb.UserID,
        password = sb.Password,
        sslmode = MySqlSslMode.Required
      }))
      {
        Assert.IsNotNull(session1.Uri);
      }

      var conn = new MySqlConnectionStringBuilder();
      conn.Server = sb.Server;
      conn.UserID = sb.UserID;
      conn.Password = sb.Password;
      conn.Port = Convert.ToUInt32(XPort);
      conn.Database = schemaName;
      conn.CharacterSet = "utf8mb4";
      conn.SslMode = MySqlSslMode.VerifyCA;
      conn.SslCa = sslCa;
      conn.CertificatePassword = sslCertificatePassword;
      conn.Keepalive = 10;
      conn.ConnectionProtocol = MySqlConnectionProtocol.Tcp;

      using (var session1 = MySQLX.GetSession(conn.ConnectionString))
      {
        Assert.IsNotNull(session1.Uri);
      }

      using (var session1 = MySQLX.GetSession(ConnectionStringUri + "/?ssl-mode=Required;"))
      {
        Assert.IsNotNull(session1.Uri);
      }

      conn = new MySqlConnectionStringBuilder();
      conn.Server = sb.Server;
      conn.UserID = sb.UserID;
      conn.Password = sb.Password;
      conn.Port = Convert.ToUInt32(XPort);
      conn.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      conn.Database = schemaName;
      conn.CharacterSet = "utf8mb4";
      conn.SslMode = MySqlSslMode.Required;
      conn.SslCa = sslCa;
      conn.CertificatePassword = sslCertificatePassword;
      conn.Keepalive = 10;
      connectionString = conn.ConnectionString;
      using (var session1 = MySQLX.GetSession(connectionString))
      {
        Assert.AreEqual(schemaName, session1.DefaultSchema.Name);
        Assert.IsNotNull(session1.Uri);
      }

      conn = new MySqlConnectionStringBuilder();
      conn.Server = sb.Server;
      conn.UserID = sb.UserID;
      conn.Password = sb.Password;
      conn.Port = Convert.ToUInt32(XPort);
      conn.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      conn.Database = schemaName;
      conn.CharacterSet = "utf8mb4";
      conn.SslMode = MySqlSslMode.VerifyCA;
      conn.SslCa = sslCa;
      conn.CertificatePassword = sslCertificatePassword;
      conn.Keepalive = 10;
      connectionString = conn.ConnectionString;
      using (var session1 = MySQLX.GetSession(connectionString))
      {
        Assert.AreEqual(schemaName, session1.DefaultSchema.Name);
        Assert.IsNotNull(session1.Uri);
      }

      conn = new MySqlConnectionStringBuilder();
      conn.Server = sb.Server;
      conn.UserID = sb.UserID;
      conn.Password = sb.Password;
      conn.Port = Convert.ToUInt32(XPort);
      conn.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      conn.Database = schemaName;
      conn.CharacterSet = "utf8mb4";
      conn.Keepalive = 10;
      connectionString = conn.ConnectionString;
      using (var session1 = MySQLX.GetSession(connectionString))
      {
        Assert.AreEqual(schemaName, session1.DefaultSchema.Name);
        Assert.IsNotNull(session1.Uri);
        session1.DropSchema("㭋玤䂜蚌");
        session1.CreateSchema("㭋玤䂜蚌");
        session1.SQL("USE 㭋玤䂜蚌").Execute();
        Assert.AreEqual(schemaName, session1.DefaultSchema.Name);
      }

      conn.Database = "㭋玤䂜蚌";
      connectionString = conn.ConnectionString;
      using (var session1 = MySQLX.GetSession(connectionString))
      {
        Assert.AreEqual("㭋玤䂜蚌", session1.DefaultSchema.Name);
        Assert.IsNotNull(session1.Uri);
      }

      conn.Server = sb.Server;
      conn.UserID = sb.UserID;
      conn.Password = sb.Password;
      conn.Port = Convert.ToUInt32(XPort);
      conn.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      conn.Database = "㭋玤䂜蚌";
      conn.CharacterSet = "utf8mb4";
      conn.SslMode = MySqlSslMode.VerifyCA;
      conn.SslCa = sslCa;
      conn.CertificatePassword = sslCertificatePassword;
      conn.Keepalive = 10;
      connectionString = conn.ConnectionString;
      using (var session1 = MySQLX.GetSession(connectionString))
      {
        Assert.AreEqual("㭋玤䂜蚌", session1.DefaultSchema.Name);
        Assert.IsNotNull(session1.Uri);
        session1.DropSchema("㭋玤䂜蚌");
      }

    }

    [Test, Description("Test MySqlX plugin Connection for user with wrong password")]
    public void GetSessionWithWrongPassword()
    {
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      sb.Password = "wrongPassword";
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(sb.ConnectionString));
    }

    [Test, Description("Test MySqlX plugin Connection for user with correct password but non MysqlX Server")]
    public void GetSessionWithWrongPort()
    {
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      sb.Port = Convert.ToUInt32(Port);
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(sb.ConnectionString));
    }

    [Test, Description("Test MySqlX plugin Issue a drop command after session already closed")]
    public void GetSessionDropAlreadyClosedConnection()
    {
      Session testSession = MySQLX.GetSession(ConnectionString);
      testSession.Close();
      testSession.Close();//works and behaviour expected but any input command should fail
      Assert.Throws<MySqlException>(() => testSession.DropSchema(schemaName));

      testSession = MySQLX.GetSession(ConnectionStringNoPassword);
      testSession.Close();
      testSession.Close();//works and behaviour expected but any input command should fail
      Assert.Throws<MySqlException>(() => testSession.DropSchema(schemaName));
    }

    [Test, Description("Session.DefaultSchema")]
    public void SessionDefaultSchema()
    {
      if (!Platform.IsWindows()) return;

      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      sb.Database = null;
      var session1 = MySQLX.GetSession(sb.ConnectionString);
      Assert.AreEqual(null, session1.DefaultSchema);
      string connectionString = ConnectionString + ";protocol=Socket;database=" + schemaName + ";characterset=utf8mb4;sslmode=VerifyCA;ssl-ca=" +
      sslCa + ";certificatepassword=" + sslCertificatePassword + ";connect-timeout=10;keepalive=10;auth=PLAIN";
      session1 = MySQLX.GetSession(connectionString);
      Assert.AreEqual(schemaName, session1.DefaultSchema.Name);

      session1 = MySQLX.GetSession(ConnectionStringUri + "/" + schemaName + "?" + "auth=PLAIN&characterset=utf8mb4");
      Assert.AreEqual(schemaName, session1.DefaultSchema.Name);

      session1 = MySQLX.GetSession(new
      {
        server = sb.Server,
        port = XPort,
        user = sb.UserID,
        password = sb.Password,
        sslmode = MySqlSslMode.Required,
        database = schemaName
      });
      Assert.AreEqual(schemaName, session1.DefaultSchema.Name);
      session1.DefaultSchema.CreateCollection("tester");
      session1.DefaultSchema.DropCollection("tester");
      Assert.AreEqual(schemaName, session1.DefaultSchema.Session.DefaultSchema.Name);

      var conn = new MySqlConnectionStringBuilder();
      session1.DropSchema("㭋玤䂜蚌");
      session1.CreateSchema("㭋玤䂜蚌");
      session1.SQL("USE 㭋玤䂜蚌").Execute();
      Assert.AreEqual(schemaName, session1.DefaultSchema.Name);
      session1.Dispose();
      conn.Server = sb.Server;
      conn.UserID = sb.UserID;
      conn.Password = sb.Password;
      conn.Port = Convert.ToUInt32(XPort);
      conn.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      conn.Database = "㭋玤䂜蚌";
      conn.CharacterSet = "utf8mb4";
      conn.SslMode = MySqlSslMode.Required;
      conn.SslCa = sslCa;
      conn.CertificatePassword = sslCertificatePassword;
      conn.Keepalive = 10;
      connectionString = conn.ConnectionString;
      session1 = MySQLX.GetSession(connectionString);
      Assert.AreEqual("㭋玤䂜蚌", session1.DefaultSchema.Name);
      StringAssert.Contains("㭋玤䂜蚌", session1.Uri);

      conn.Server = sb.Server;
      conn.UserID = sb.UserID;
      conn.Password = sb.Password;
      conn.Port = Convert.ToUInt32(XPort);
      conn.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      conn.Database = "㭋玤䂜蚌";
      conn.CharacterSet = "utf8mb4";
      conn.SslMode = MySqlSslMode.Required;
      conn.SslCa = sslCa;
      conn.CertificatePassword = sslCertificatePassword;
      conn.Keepalive = 10;
      connectionString = conn.ConnectionString;
      session1 = MySQLX.GetSession(connectionString);
      Assert.AreEqual("㭋玤䂜蚌", session1.DefaultSchema.Name);
      StringAssert.Contains("㭋玤䂜蚌", session1.Uri);
      session1.DefaultSchema.CreateCollection("tester");
      session1.DefaultSchema.DropCollection("tester");
      Assert.AreEqual("㭋玤䂜蚌", session1.DefaultSchema.Session.DefaultSchema.Name);
    }

    [Test, Description("Session BaseString/MySQLXConnectionString Builder")]
    public void ConnectionStringBuilderXpluginTests()
    {
      if (!Platform.IsWindows()) return;

      MySqlXConnectionStringBuilder mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
      mysqlx0.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.ConnectTimeout = 10;
      mysqlx0.Keepalive = 10;
      mysqlx0.CertificateFile = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.CertificateThumbprint = "";


      using (var xpluginconn = MySQLX.GetSession(mysqlx0.ConnectionString))
      {
        Assert.AreEqual(SessionState.Open, xpluginconn.InternalSession.SessionState);
      }

      mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
      mysqlx0.Server = "::1";
      mysqlx0.Database = schemaName;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.ConnectTimeout = 10;
      mysqlx0.Keepalive = 10;
      mysqlx0.CertificateFile = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.CertificateThumbprint = sslCertificatePassword;

      using (var xpluginconn = MySQLX.GetSession(mysqlx0.ConnectionString))
      {
        Assert.AreEqual(SessionState.Open, xpluginconn.InternalSession.SessionState);
      }

      mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
      mysqlx0.Database = schemaName;
      mysqlx0.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.VerifyCA;
      mysqlx0.ConnectTimeout = 10;
      mysqlx0.Keepalive = 10;
      mysqlx0.CertificateFile = sslCa;
      mysqlx0.CertificatePassword = "pass";
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.CertificateThumbprint = "";

      using (var xpluginconn = MySQLX.GetSession(mysqlx0.ConnectionString))
      {
        Assert.AreEqual(SessionState.Open, xpluginconn.InternalSession.SessionState);
      }

      mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
      mysqlx0.Database = schemaName;
      mysqlx0.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.ConnectTimeout = 10;
      mysqlx0.Keepalive = 10;

      using (var xpluginconn = MySQLX.GetSession(mysqlx0.ConnectionString))
      {
        Assert.AreEqual(SessionState.Open, xpluginconn.InternalSession.SessionState);
      }

      //Scenario-2
      string valid = "server=" + mysqlx0.Server + ";user id=" + mysqlx0.UserID + ";password=" + mysqlx0.Password + ";port=" + XPort + ";protocol=Socket;database=" + schemaName + ";characterset=utf8mb4;sslmode=Required;certificatefile=" + sslCa + ";certificatepassword=" + sslCertificatePassword + ";connect-timeout=10;keepalive=10;certificatestorelocation=LocalMachine;certificatethumbprint=;";
      using (var xpluginconn = MySQLX.GetSession(valid))
      {
        Assert.AreEqual(SessionState.Open, xpluginconn.InternalSession.SessionState);
      }

      //Scenario-3
      mysqlx0 = new MySqlXConnectionStringBuilder(ConnectionString);
      mysqlx0.Database = schemaName;
      mysqlx0.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.ConnectTimeout = 10;
      mysqlx0.Keepalive = 10;
      mysqlx0.CertificateFile = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.CertificateThumbprint = "";
      mysqlx0.Auth = MySqlAuthenticationMode.AUTO;
      mysqlx0.SslCa = sslCa;
      using (var xpluginconn = MySQLX.GetSession(mysqlx0.ConnectionString))
      {
        Assert.AreEqual(SessionState.Open, xpluginconn.InternalSession.SessionState);
      }

      //Basic Scenarios
      var connectionstr = "server=" + mysqlx0.Server + ";database=" + mysqlx0.Database + ";port="
          + XPort + ";userid=" + mysqlx0.UserID + ";password="
          + mysqlx0.Password
          + ";characterset=utf8mb4;sslmode=Required;connect-timeout=20000;keepalive=20000;certificatefile="
          + sslCa + ";certificatepassword=" + sslCertificatePassword
          + ";certificatestorelocation=LocalMachine;certificatethumbprint=";
      using (var xpluginconn = MySQLX.GetSession(connectionstr))
      {
        Assert.AreEqual(SessionState.Open, xpluginconn.InternalSession.SessionState);
      }

      connectionstr = "mysqlx://" + mysqlx0.Server + ":" + XPort + "/" +
          schemaName + "?connect-timeout=10&userid=" + mysqlx0.UserID + "&password="
          + mysqlx0.Password + "&sslca=" + sslCa + "&certificatepassword="
          + sslCertificatePassword + "&keepalive=10&characterset=utf8mb4";

      using (var xpluginconn = MySQLX.GetSession(connectionstr))
      {
        Assert.AreEqual(SessionState.Open, xpluginconn.InternalSession.SessionState);
      }

      using (var xpluginconn = MySQLX.GetSession(new
      {
        server = mysqlx0.Server,
        port = XPort,
        user = mysqlx0.UserID,
        password = mysqlx0.Password
      }))
      {
        Assert.AreEqual(SessionState.Open, xpluginconn.InternalSession.SessionState);
      }

    }

    [Test, Description("Connection Measurement Test")]
    public void ConnectionTimeTest()
    {
      int secondsExpected = 15;
      var connObject = new { server = Host, port = XPort, user = session.Settings.UserID, password = session.Settings.Password };
      MeasureConnectionString(ConnectionString, secondsExpected, "Connection String", 5);
      MeasureConnectionString(ConnectionStringUri, secondsExpected, "Connection String URI", 5);
      MeasureConnectionObject(connObject, secondsExpected, "Connection Object", 5);
    }

    [Test, Description("Connection time with Database set")]
    public void ConnectionTimeWithDatabaseTest()
    {
      int secondsExpected = 15;
      var connString = ConnectionString + ";database=test";
      var connStringURI = ConnectionStringUri + "/?database=test";
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      var connectionObject = new
      {
        server = sb.Server,
        port = XPort,
        user = sb.UserID,
        password = sb.Password,
        database = schemaName
      };
      MeasureConnectionString(connString, secondsExpected, "Connection String", 5);
      MeasureConnectionString(connStringURI, secondsExpected, "Connection String URI", 5);
      MeasureConnectionObject(connectionObject, secondsExpected, "Connection Object", 5);
    }

    [Test, Description("REFACTOR PARSING OF CONNECTION STRING IN X DEVAPI")]
    [Ignore("Uncomment to execute")]
    public void ParseConnectionStringBenchmark_S1()
    {
      long transactions = 0;
      long startTime = 0;
      long endTime = 0;
      long queryRunTime = 0;
      int iterations = 10;
      long NANO_TO_MILLI = 1000000;
      long elapsedTime = 0;
      long conTime = 0;
      int i = 0;

      string connStr = ConnectionString;

      var session = MySQLX.GetSession(connStr);
      session.Close();

      for (int j = 0; j < 1; j++)
      {
        transactions = 0;
        startTime = 0;
        endTime = 0;
        queryRunTime = 0;
        iterations = 20;
        elapsedTime = 0;
        conTime = 0;
        connStr = ConnectionStringUri;
        startTime = NanoTime();
        for (i = 0; i < iterations; i++)
        {
          queryRunTime = queryRunTime + DoConnectString(connStr);
        }
        endTime = NanoTime();
        transactions = i;
        elapsedTime = (endTime - startTime); //in nano
        conTime = (elapsedTime / NANO_TO_MILLI) - (queryRunTime / NANO_TO_MILLI);
        var t = CalculateTPS(conTime, transactions);
        var log = ("Connected to MySQL using URI with iterations " + iterations + " with TPS:" + t);
        Assert.IsNotNull(t);

        transactions = 0;
        startTime = 0;
        endTime = 0;
        queryRunTime = 0;
        iterations = 20;
        elapsedTime = 0;
        conTime = 0;
        connStr = ConnectionString;
        startTime = NanoTime();
        for (i = 0; i < iterations; i++)
        {
          queryRunTime = queryRunTime + DoConnectString(connStr);
        }
        endTime = NanoTime();
        transactions = i;
        elapsedTime = (endTime - startTime); //in nano
        conTime = (elapsedTime / NANO_TO_MILLI) - (queryRunTime / NANO_TO_MILLI);
        t = CalculateTPS(conTime, transactions);
        log = ("Connected to MySQL using connection string with iterations " + iterations + " with TPS:" + t);
        Console.WriteLine(log);
        Assert.IsNotNull(t);

        transactions = 0;
        startTime = 0;
        endTime = 0;
        queryRunTime = 0;
        iterations = 20;
        elapsedTime = 0;
        conTime = 0;
        MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
        var conn = new
        {
          server = sb.Server,
          port = XPort,
          user = sb.UserID,
          password = sb.Password
        };
        startTime = NanoTime();
        for (i = 0; i < iterations; i++)
        {
          queryRunTime = queryRunTime + DoConnectObject(conn);
        }
        endTime = NanoTime();
        transactions = i;
        elapsedTime = (endTime - startTime); //in nano
        conTime = (elapsedTime / NANO_TO_MILLI) - (queryRunTime / NANO_TO_MILLI);
        t = CalculateTPS(conTime, transactions);
        log = ("Connected to MySQL using Anonymous with iterations "
            + iterations +
            " with TPS:" + t);
      }
    }

    [Test, Description("REFACTOR PARSING OF CONNECTION STRING IN X DEVAPI")]
    [Ignore("Uncomment to execute")]
    public void ParseConnectionStringBenchmark_S2()
    {
      var connStr = ConnectionStringUri;
      long queryStartTime = 0, queryRunTime = 0;
      queryStartTime = NanoTime();
      var session1 = MySQLX.GetSession(connStr);
      queryRunTime = NanoTime() - queryStartTime;
      session1.Close();
      for (int j = 0; j < 20; j++)
      {
        connStr = ConnectionStringUri;
        queryStartTime = 0; queryRunTime = 0;
        queryStartTime = NanoTime();
        session1 = MySQLX.GetSession(connStr);
        queryRunTime = NanoTime() - queryStartTime;
        session1.Close();
        session1.Dispose();
        var log = ("Connected to MySQL using URI:" + queryRunTime / 1000000);

        connStr = ConnectionString;
        queryStartTime = 0; queryRunTime = 0;
        queryStartTime = NanoTime();
        session1 = MySQLX.GetSession(connStr);
        queryRunTime = NanoTime() - queryStartTime;
        session1.Close();
        session1.Dispose();
        log = ("Connected to MySQL using connection string:" + queryRunTime / 1000000);

        MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
        var conn = new
        {
          server = sb.Server,
          port = XPort,
          user = sb.UserID,
          password = sb.Password
        };
        queryStartTime = 0; queryRunTime = 0;
        queryStartTime = NanoTime();
        session1 = MySQLX.GetSession(conn);
        queryRunTime = NanoTime() - queryStartTime;
        session1.Close();
        session1.Dispose();
        log = ("Connected to MySQL using Connection Object:" + queryRunTime / 1000000);
      }
    }

    [Test, Description("Getsession/Session-URI")]
    public void GetSessionUriPositiveTests()
    {
      string[] positiveStringList = new string[6];
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      positiveStringList[0] = "mysqlx://" + sb.UserID + ":" + sb.Password + "@" + sb.Server + ":" + XPort + "/?ssl-mode=Required";
      positiveStringList[1] = "mysqlx://" + sb.UserID + ":" + sb.Password + "@" + Host + ":" + XPort + "/?ssl-mode=Required";
      positiveStringList[2] = "mysqlx://" + sb.UserID + ":" + sb.Password + "@" + Host + ":" + XPort + "/" + schemaName + "?ssl-mode=Required";
      positiveStringList[3] = "mysqlx://" + sb.UserID + ":" + sb.Password + "@" + sb.Server + ":" + XPort + "/" + schemaName + "?ssl-mode=Required&auth=SHA256_MEMORY";
      positiveStringList[4] = "mysqlx://" + sb.UserID + ":" + sb.Password + "@" + sb.Server + ":" + XPort + "/" + schemaName + "?ssl-mode=Required&characterset=utf8mb4";
      positiveStringList[5] = "mysqlx://" + sb.UserID + ":" + sb.Password + "@" + sb.Server + ":" + XPort + "/" + schemaName + "?" + "ssl-mode=Required";

      foreach (var connStr in positiveStringList)
      {
        using (Session c = MySQLX.GetSession(connStr))
        {
          Assert.AreEqual(SessionState.Open, c.InternalSession.SessionState);
        }
      }
    }

    [Test, Description("Getsession/Session-URI Negative Scenarios")]
    public void GetSessionUriNegativeTests()
    {
      string[] NegativeStringList = new string[8];
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      NegativeStringList[0] = "mysqlx://" + sb.UserID + ":" + sb.Password + "@" + sb.Server + ":" + 9999 + ";ssl-mode=required";
      NegativeStringList[1] = "mysqlx://" + sb.UserID + ":" + sb.Password + "@" + "129.0.0.1" + ":" + XPort + ";ssl-mode=required";
      NegativeStringList[2] = "mysqlx://" + sb.UserID + ":" + "wrongpassword" + "@" + "localhost" + ":" + XPort + "/" + schemaName + "?ssl-mode=required";
      NegativeStringList[3] = "mysqlxyzzzz://" + sb.UserID + ":" + sb.Password + "@" + sb.Server + ":" + XPort + "/" + schemaName + "?ssl-mode=required";
      NegativeStringList[4] = "mysqlx://" + "wrongsb.UserID" + ":" + sb.Password + "@" + sb.Server + ":" + XPort + "/" + schemaName + "?ssl-mode=required";
      NegativeStringList[5] = "mysqlx://" + sb.UserID + ":" + sb.Password + "@" + "wronglocalhost" + "/" + schemaName + "?sslmode=required";
      NegativeStringList[6] = "mysqlx://" + sb.UserID + ":" + sb.Password + "@" + sb.Server + ":" + XPort + "/" + schemaName + "?" + "ssl-mode*&^%$#@!invalidvalues123*()";
      NegativeStringList[7] = "mysqlx://" + sb.UserID + ":" + sb.Password + "@" + sb.Server + ":" + XPort + "/" + schemaName + "?" + "invalidvalues123invalidvalues123invalidvalues123invalidvalues123invalidvalues123invalidvalues123invalidvalues123invalidvalues123" + ";ssl-mode=required";

      foreach (var connStr in NegativeStringList)
      {
        Assert.Catch(() => MySQLX.GetSession(connStr));
      }
    }

    [Test, Description("Getsession using Anonymous Type Negative-Wrong Password")]
    public void GetSessionAnonymousTypeNegative()
    {
      var connectionStringObject = new { connection = $"server={Host};user={session.Settings.UserID};port={XPort};password=wrong_password;sslmode={MySqlSslMode.Required}" };
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionStringObject.connection));
    }

    [Test, Description("Support Session Anonymous as uri string Positive")]
    public void GetSessionWithAnonymousObjectURI()
    {
      var connectionStringObject = new { connection = ConnectionStringUri };
      using (Session sessionPlain = MySQLX.GetSession(connectionStringObject.connection))
      {
        var db = sessionPlain.GetSchema(schemaName);
        var col = db.GetCollection("my_collection_123456789");
        if (col.ExistsInDatabase())
        {
          db.DropCollection("my_collection_123456789");
          db.CreateCollection("my_collection_123456789");
        }
        else { db.CreateCollection("my_collection_123456789"); }
        db.DropCollection("my_collection_123456789");
      }
      if (Convert.ToInt32(XPort) == 33060)//Connect to server on localhost with user userx using URI string default port
      {
        MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
        connectionStringObject = new { connection = "mysqlx://" + sb.UserID + ":" + sb.Password + "@" + sb.Server };
        using (Session sessionPlain = MySQLX.GetSession(connectionStringObject.connection))
        {
          var db = sessionPlain.GetSchema(schemaName);
          var col = db.GetCollection("my_collection_123456789");
          if (col.ExistsInDatabase())
          {
            db.DropCollection("my_collection_123456789");
            db.CreateCollection("my_collection_123456789");
          }
          else { db.CreateCollection("my_collection_123456789"); }
          db.DropCollection("my_collection_123456789");
        }
      }
    }

    [Test, Description("Support Session connection string as uri string Negative-Invalid Password")]
    public void GetSessionURIWrongPassword()
    {
      string invalidPassword = "invalid";
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      string connectionString = "mysqlx://" + sb.UserID + ":" + invalidPassword + "@" + sb.Server + ":" + XPort;
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionString));
      connectionString = "mysqlx://" + sb.UserID + ":" + invalidPassword + "@" + sb.Server;
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionString));
    }

    // Connection Timeout Tests
    [Test, Description("Remote offline host without connect-timeout parameter.Mysql getclient with pooling and maxsize 2 and queue timeout 2000 milliseconds")]
    public void TimeoutUsingClientAndPooling_S1()
    {
      MySqlXConnectionStringBuilder sb = new MySqlXConnectionStringBuilder(ConnectionString);
      string serverName = "10.10.10.10";
      string connStr = "server=" + serverName + ";user=" + sb.UserID + ";port=" + XPort + ";password="
           + sb.Password + ";";
      var connectionpooling = "{ \"pooling\": { \"maxSize\": 1, \"queueTimeout\": 2000 , \"maxIdleTime\":1000, \"enabled\": true} }";
      var connectionpoolingObject = new { pooling = new { enabled = true, maxSize = 1, queueTimeout = 2000, maxIdleTime = 1000 } };
      Client client = MySQLX.GetClient(connStr, connectionpoolingObject);
      TestFailureTimeout(client, 9, 11, "Timeout value between 9 and 11 seconds");
      var connStrUri = "mysqlx://" + sb.UserID + ":" + sb.Password + "@" + serverName + ":" + XPort;
      client = MySQLX.GetClient(connStrUri, connectionpoolingObject);
      TestFailureTimeout(client, 9, 11, "Timeout value between 9 and 11 seconds");
      var connObj = new { server = serverName, port = XPort, user = sb.UserID, password = sb.Password };
      client = MySQLX.GetClient(connObj, connectionpoolingObject);
      TestFailureTimeout(client, 9, 11, "Timeout value between 9 and 11 seconds");
      client = MySQLX.GetClient(connStr, connectionpooling);
      TestFailureTimeout(client, 9, 11, "Timeout value between 9 and 11 seconds");
      client = MySQLX.GetClient(connStrUri, connectionpooling);
      TestFailureTimeout(client, 9, 11, "Timeout value between 9 and 11 seconds");
      client = MySQLX.GetClient(connObj, connectionpooling);
      TestFailureTimeout(client, 9, 11, "Timeout value between 9 and 11 seconds");
    }

    [Test, Description("failover connection string with one offline host and one online host and disable connect - timeout parameter(set to 0) " +
           ".Mysql getclient with pooling and maxsize 2 and queue timeout 2000 milliseconds.Both the session should be successful after" +
           "waiting for the respective lower layer socket timeout")]
    public void TimeoutUsingClientAndPooling_S2()
    {
      int connectTimeout = 1;
      string hostList = string.Empty;
      string localIP = session.Settings.Server;
      int minTime = 0;
      int maxTime = 30;
      session.Settings.UserID = "testAnyhost";
      string connStr = "server=10.10.10.10," + localIP + ";port=" + XPort + ";uid=" + session.Settings.UserID + ";" + "password=" + session.Settings.Password +
          ";connect-timeout=" + connectTimeout;
      var connectionpoolingObject = new { pooling = new { enabled = true, maxSize = 1, queueTimeout = 20000, maxIdleTime = 1000 } };

      TestClientSuccessTimeout(minTime, maxTime, $"Timeout value between {minTime} and {maxTime} seconds", connStr, connectionpoolingObject);

      var connStrUri = "mysqlx://" + session.Settings.UserID + ":" + session.Settings.Password + "@[192.1.10.10," + localIP + ":" + XPort + "]" + "/?connect-timeout=" + connectTimeout;
      TestClientSuccessTimeout(minTime, maxTime, $"Timeout value between {minTime} and {maxTime} seconds", connStr, connectionpoolingObject);

    }

    [Test, Description("connect - timeout parameter set as 1000 milliseconds.Create a pool of two sessions." +
               "pooling(enabled:true,maxSize:2,queueTimeout: 2000 milliseconds).Try to create a third connection and verify the behaviour(Queue timeout expected)")]
    public void TimeoutReachingMaxSizePool()
    {
      int connectTimeout = 1000;
      string hostList = string.Empty;
      string localIP = session.Settings.Server;
      string connStr = "server=" + localIP + ";port=" + XPort + ";uid=" + session.Settings.UserID + ";" + "password=" + session.Settings.Password +
          ";connect-timeout=" + connectTimeout;
      var connectionpooling = "{ \"pooling\": { \"maxSize\": 2, \"queueTimeout\": 2000 , \"maxIdleTime\":1000, \"enabled\": true} }";
      var connectionpoolingObject = new { pooling = new { enabled = true, maxSize = 2, queueTimeout = 2000, maxIdleTime = 1000 } };
      using (Client client = MySQLX.GetClient(connStr, connectionpoolingObject))
      {
        var session1 = client.GetSession();
        var session2 = client.GetSession();
        TestClientQueueTimeout(client, 1, 3, "Test queue timeout");
      }
      var connStrUri = "mysqlx://" + session.Settings.UserID + ":" + session.Settings.Password + "@[" + localIP + ":" + XPort + "]" + "/?connect-timeout=" + connectTimeout;
      using (var client = MySQLX.GetClient(connStrUri, connectionpoolingObject))
      {

        var session1 = client.GetSession();
        var session2 = client.GetSession();
        TestClientQueueTimeout(client, 1, 3, "Test queue timeout");
      }
      var connObj = new { server = "" + localIP, port = XPort, uid = session.Settings.UserID, password = session.Settings.Password, connecttimeout = connectTimeout };
      using (var client = MySQLX.GetClient(connObj, connectionpoolingObject))
      {
        var session1 = client.GetSession();
        var session2 = client.GetSession();
        TestClientQueueTimeout(client, 1, 3, "Test queue timeout");
      }

      using (Client client = MySQLX.GetClient(connStr, connectionpooling))
      {
        var session1 = client.GetSession();
        var session2 = client.GetSession();
        TestClientQueueTimeout(client, 1, 3, "Test queue timeout");
      }
      using (var client = MySQLX.GetClient(connStrUri, connectionpooling))
      {
        var session1 = client.GetSession();
        var session2 = client.GetSession();
        TestClientQueueTimeout(client, 1, 3, "Test queue timeout");
      }
      using (var client = MySQLX.GetClient(connObj, connectionpooling))
      {
        var session1 = client.GetSession();
        var session2 = client.GetSession();
        TestClientQueueTimeout(client, 1, 3, "Test queue timeout");
      }
      for (var i = 1; i <= 2; i++)
      {
        hostList = "(address=143.24.20.36,priority=1),(address=" + localIP + ",priority=0)";
      }
      connStr = "server=" + hostList + ";user=" + session.Settings.UserID + ";port=" + XPort + ";password="
           + session.Settings.Password + ";connect-timeout=" + connectTimeout;

      using (var client = MySQLX.GetClient(connStr, connectionpoolingObject))
      {
        var session1 = client.GetSession();
        var session2 = client.GetSession();
        TestClientQueueTimeout(client, 1, 3, "Test queue timeout X");
      }

      var connObj1 = new { server = hostList, port = XPort, user = session.Settings.UserID, password = session.Settings.Password, connecttimeout = connectTimeout };
      using (var client = MySQLX.GetClient(connObj1, connectionpoolingObject))
      {
        var session1 = client.GetSession();
        var session2 = client.GetSession();
        TestClientQueueTimeout(client, 1, 3, "Test queue timeout Y");
      }
      using (var client = MySQLX.GetClient(connStr, connectionpooling))
      {
        var session1 = client.GetSession();
        var session2 = client.GetSession();
        TestClientQueueTimeout(client, 1, 3, "Test queue timeout X");
      }
      connObj1 = new { server = hostList, port = XPort, user = session.Settings.UserID, password = session.Settings.Password, connecttimeout = connectTimeout };
      using (var client = MySQLX.GetClient(connObj1, connectionpooling))
      {
        var session1 = client.GetSession();
        var session2 = client.GetSession();
        TestClientQueueTimeout(client, 1, 3, "Test queue timeout A");
      }

    }

    [Test, Description("scenario 0(connectionString,connectionUri,Anonymous Object)-Without connect timeout and max timeout should be 10s")]
    public void TimeoutWithWrongHost()
    {
      string serverName = "vigdis07.no.oracle.com";
      string connStr = "server=" + serverName + ";user=" + session.Settings.UserID + ";port=" + XPort + ";password="
           + session.Settings.Password + ";";
      TestConnectStringTimeoutFailureTimeout(connStr, 0, 11, "Timeout value between 9 and 11 seconds");
      connStr = "mysqlx://" + session.Settings.UserID + ":" + session.Settings.Password + "@" + serverName + ":" + XPort;
      TestConnectStringTimeoutFailureTimeout(connStr, 0, 11, "Timeout value between 9 and 11 seconds");
      var connObj = new { server = serverName, port = XPort, user = session.Settings.UserID, password = session.Settings.Password };
      TestConnectObjTimeoutFailureTimeout(connObj, 0, 11, "Timeout value between 9 and 11 seconds");

    }

    [Test, Description("scenario 1(connectionString,connectionUri,Anonymous Object)")]
    public void MeasureNoTimeoutResponse()
    {
      string connStr = "server=" + session.Settings.Server + ";user=" + session.Settings.UserID + ";port=" + XPort + ";password="
           + session.Settings.Password + ";" + "connect-timeout=90;";
      TestConnectStringTimeoutSuccessTimeout(connStr, 0, 3, "Timeout value between 0 and 1 second");
      connStr = "mysqlx://" + session.Settings.UserID + ":" + session.Settings.Password + "@" + session.Settings.Server + ":" + XPort + "?connect-timeout=900;";
      TestConnectStringTimeoutSuccessTimeout(connStr, 0, 3, "Timeout value between 0 and 1 second");
      var connObj = new { server = session.Settings.Server, port = XPort, user = session.Settings.UserID, password = session.Settings.Password, connecttimeout = 9000 };
      TestConnectObjectTimeoutSuccessTimeout(connObj, 0, 3, "Timeout value between 0 and 1 second");
    }

    [Test, Description("scenario 2(connectionString,connectionUri,Anonymous Object with all options)")]
    public void NoTimeoutWithManyOptions()
    {
      if (!Platform.IsWindows()) return;
      string connStr = "server=" + session.Settings.Server + ";user id=" + session.Settings.UserID + ";password=" +
          session.Settings.Password + ";port=" + XPort + ";protocol=Socket;" +
          "database=" + schemaName + ";characterset=utf8mb4;sslmode=VerifyCA;ssl-ca="
          + sslCa + ";certificatepassword=pass;keepalive=10;auth=PLAIN;"
          + "connect-timeout=900;";
      TestConnectStringTimeoutSuccessTimeout(connStr, 0, 1, "Timeout value between 0 and 1 second");
      connStr = "mysqlx://" + session.Settings.Server + ":" + XPort + "/" + schemaName
          + "?" + "user id=" + session.Settings.UserID + "&password=" + session.Settings.Password + "&sslca="
          + sslCa + "&certificatepassword=pass&keepalive=10&characterset=utf8mb4&auth=PLAIN&connect-timeout=900";
      TestConnectStringTimeoutSuccessTimeout(connStr, 0, 1, "Timeout value between 0 and 1 second");
      var connObj =
          new
          {
            server = session.Settings.Server,
            port = XPort,
            user = session.Settings.UserID,
            password = session.Settings.Password,
            sslmode = MySqlSslMode.VerifyCA,
            CertificateFile = sslCa,
            CertificatePassword = sslCertificatePassword,
            database = schemaName,
            keepalive = 10,
            characterset = "utf8mb4",
            auth = MySqlAuthenticationMode.PLAIN,
            connecttimeout = 9000
          };
      TestConnectStringTimeoutSuccessTimeout(connStr, 0, 1, "Timeout value between 0 and 1 second");
    }

    [Test, Description("scenario 1(MysqlxStringBuilder)")]
    public void TimeoutSuccessWithStringBuilder()
    {
      var connStrBuilder = new MySqlXConnectionStringBuilder();
      connStrBuilder.ConnectTimeout = 9000;
      connStrBuilder.UserID = session.Settings.UserID;
      connStrBuilder.Password = session.Settings.Password;
      connStrBuilder.Port = Convert.ToUInt32(XPort);
      connStrBuilder.Server = session.Settings.Server;
      TestConnectStringTimeoutSuccessTimeout(connStrBuilder.ConnectionString, 0, 3, "Timeout value between 0 and 3 second");
      string connStr = "server=" + session.Settings.Server + ";user=" + session.Settings.UserID + ";port=" + XPort + ";password="
          + session.Settings.Password + ";" + "connect-timeout=9000;";
      connStrBuilder = new MySqlXConnectionStringBuilder(connStr);
      TestConnectStringTimeoutSuccessTimeout(connStrBuilder.ConnectionString, 0, 3, "Timeout value between 0 and 3 second");
    }

    [Test, Description("scenario 2(MysqlxStringBuilder with all options)")]
    public void TimeoutSuccessWithStringBuilderAllOptions()
    {
      string connStr = null;
      MySqlXConnectionStringBuilder mysqlx0 = null;
      if (!Platform.IsWindows()) return;
      connStr = "server=" + session.Settings.Server + ";user id=" + session.Settings.UserID + ";password=" +
                session.Settings.Password + ";port=" + XPort + ";protocol=Socket;" +
                "database=" + schemaName + ";characterset=utf8mb4;sslmode=Required;ssl-ca="
                + sslCa + $";certificatepassword={sslCertificatePassword};certificatestorelocation=LocalMachine;"
                + ";keepalive =10;auth=PLAIN;certificatethumbprint=;"
                + "connect-timeout=" + 9000;
      mysqlx0 = new MySqlXConnectionStringBuilder(connStr);
      TestConnectStringTimeoutSuccessTimeout(mysqlx0.ConnectionString, 0, 1, "Timeout value between 0 and 1 second");
      mysqlx0 = new MySqlXConnectionStringBuilder();
      mysqlx0.Server = session.Settings.Server;
      mysqlx0.UserID = session.Settings.UserID;
      mysqlx0.Password = session.Settings.Password;
      mysqlx0.Port = Convert.ToUInt32(XPort);
      mysqlx0.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      mysqlx0.Database = schemaName;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.SslCa = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.Keepalive = 10;
      mysqlx0.Auth = MySqlAuthenticationMode.PLAIN;
      mysqlx0.CertificateThumbprint = "";
      mysqlx0.ConnectTimeout = (uint)90000;
      TestConnectStringTimeoutSuccessTimeout(mysqlx0.ConnectionString, 0, 1, "Timeout value between 0 and 1 second");
    }

    [Test, Description("scenario 3(MysqlxStringBuilder with all options-set minimum timeout to 1 and keep on increasing till gets connected)")]
    public void TimeoutIncreasingUntilConnect()
    {
      string connStr = null;
      MySqlXConnectionStringBuilder mysqlx0 = null;

      for (int i = 1; i < 20; i++)
      {
        connStr = ConnectionString + ";protocol=Socket;" +
              "database=" + schemaName + ";characterset=utf8mb4;sslmode=VerifyCA;ssl-ca="
              + sslCa + $";certificatepassword={sslCertificatePassword};certificatestorelocation=LocalMachine;"
              + ";auth=PLAIN;certificatethumbprint=;"
              + "connect-timeout=" + i;
        mysqlx0 = new MySqlXConnectionStringBuilder(connStr);
        using (var conn = MySQLX.GetSession(mysqlx0.ConnectionString))
        {
          Assert.IsNotNull(conn.Uri);
        }
        break;
      }

      for (int i = 1; i < 20; i++)
      {
        mysqlx0 = new MySqlXConnectionStringBuilder();
        mysqlx0.Server = session.Settings.Server;
        mysqlx0.UserID = session.Settings.UserID;
        mysqlx0.Password = session.Settings.Password;
        mysqlx0.Port = Convert.ToUInt32(XPort);
        mysqlx0.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
        mysqlx0.Database = schemaName;
        mysqlx0.CharacterSet = "utf8mb4";
        mysqlx0.SslMode = MySqlSslMode.VerifyCA;
        mysqlx0.SslCa = sslCa;
        mysqlx0.CertificatePassword = sslCertificatePassword;
        mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
        mysqlx0.Auth = MySqlAuthenticationMode.PLAIN;
        mysqlx0.CertificateThumbprint = "";
        mysqlx0.ConnectTimeout = (uint)i;

        using (var conn = MySQLX.GetSession(mysqlx0.ConnectionString))
        {
          Assert.IsNotNull(conn.Uri);
        }
        break;
      }
    }

    [Test, Description("scenario 1(connectionString,connectionUri,Anonymous Object with default timeout)")]
    public void ValidateDefaultTimeoutParameter()
    {
      uint defaultTimeout = 1;
      string connStr = ConnectionString + ";" + "connect-timeout=" + defaultTimeout;
      for (int i = 0; i < 10; i++)
      {
        using (var conn = MySQLX.GetSession(connStr))
        {
          Assert.AreEqual(conn.Settings.ConnectTimeout, defaultTimeout);
        }
      }
      connStr = "mysqlx://" + session.Settings.UserID + ":" + session.Settings.Password + "@" + session.Settings.Server + ":" + XPort + "?connect-timeout=" + defaultTimeout;
      for (int i = 0; i < 10; i++)
      {
        using (var conn = MySQLX.GetSession(connStr))
        {
          Assert.AreEqual(conn.Settings.ConnectTimeout, defaultTimeout);
        }
      }
      var connObj1 = new { server = session.Settings.Server, port = XPort, user = session.Settings.UserID, password = session.Settings.Password, connecttimeout = defaultTimeout };
      for (int i = 0; i < 10; i++)
      {
        using (var conn = MySQLX.GetSession(connObj1))
        {
          Assert.AreEqual(conn.Settings.ConnectTimeout, defaultTimeout);
        }
      }
    }

    [Test, Description("scenario 2(MysqlxStringBuilder with default timeout)")]
    public void ValidateDefaultTimeoutParameterWithStringBuilder()
    {
      uint defaultTimeout = 1;
      var connStrBuilder = new MySqlXConnectionStringBuilder();
      connStrBuilder.ConnectTimeout = defaultTimeout;
      connStrBuilder.UserID = session.Settings.UserID;
      connStrBuilder.Password = session.Settings.Password;
      connStrBuilder.Port = Convert.ToUInt32(XPort);
      connStrBuilder.Server = session.Settings.Server;
      for (int i = 0; i < 10; i++)
      {
        using (var conn = MySQLX.GetSession(connStrBuilder.ConnectionString))
        {
          Assert.AreEqual(conn.Settings.ConnectTimeout, defaultTimeout);
        }
      }
      string connStr = ConnectionString + ";" + "connect-timeout=" + defaultTimeout;
      connStrBuilder = new MySqlXConnectionStringBuilder(connStr);
      for (int i = 0; i < 10; i++)
      {
        using (var conn = MySQLX.GetSession(connStrBuilder.ConnectionString))
        {
          Assert.AreEqual(conn.Settings.ConnectTimeout, defaultTimeout);
        }
      }
    }

    [Test, Description("scenario 1(MysqlxStringBuilder with connect timeout option for offline server)")]
    public void TimeoutOfflineServerWithStringBuilder()
    {
      int connectionTimeout = 2000;
      string serverName = "vigdis07.no.oracle.com";
      var connStrBuilder = new MySqlXConnectionStringBuilder();
      connStrBuilder.ConnectTimeout = (uint)connectionTimeout;
      connStrBuilder.UserID = session.Settings.UserID;
      connStrBuilder.Password = session.Settings.Password;
      connStrBuilder.Port = Convert.ToUInt32(XPort);
      connStrBuilder.Server = serverName;
      TestConnectStringTimeoutFailureTimeout(connStrBuilder.ConnectionString, 0, 21, "Offline host timeout value in between 1 and 21 seconds");

      string connStr = "server=" + serverName + ";user=" + session.Settings.UserID + ";port=" + XPort + ";password="
          + session.Settings.Password + ";" + "connect-timeout=" + connectionTimeout;
      connStrBuilder = new MySqlXConnectionStringBuilder(connStr);
      TestConnectStringTimeoutFailureTimeout(connStrBuilder.ConnectionString, 0, 21, "Offline host timeout value in between 1 and 21 seconds");
    }

    [Test, Description("scenario 1(connectionString,connectionUri,Anonymous Object,MysqlxStringBuilder with connect timeout option=1 for online server)")]
    public void TimeoutSuccessConnectOptionOne()
    {
      int connectionTimeout = 1;
      string connStr = ConnectionString + ";" + "connect-timeout=" + connectionTimeout;
      TestConnectStringTimeoutSuccessTimeout(connStr, 0, 5, "Checking the timeout between 0 to 5 seconds");
      connStr = ConnectionStringUri + "?connect-timeout=" + connectionTimeout;
      TestConnectStringTimeoutSuccessTimeout(connStr, 0, 5, "Checking the timeout between 0 to 5 seconds");
      var connectionObj = new { server = session.Settings.Server, port = XPort, user = session.Settings.UserID, password = session.Settings.Password, connecttimeout = connectionTimeout };
      TestConnectObjectTimeoutSuccessTimeout(connectionObj, 0, 5, "Checking the timeout between 0 to 5 seconds");

      var connStrBuilder = new MySqlXConnectionStringBuilder();
      connStrBuilder.ConnectTimeout = (uint)connectionTimeout;
      connStrBuilder.UserID = session.Settings.UserID;
      connStrBuilder.Password = session.Settings.Password;
      connStrBuilder.Port = Convert.ToUInt32(XPort);
      connStrBuilder.Server = session.Settings.Server;
      TestConnectStringTimeoutSuccessTimeout(connStrBuilder.ConnectionString, 0, 5, "Checking the timeout between 0 to 5 seconds");

      connStr = ConnectionString + ";" + "connect-timeout=" + connectionTimeout;
      connStrBuilder = new MySqlXConnectionStringBuilder(connStr);
      TestConnectStringTimeoutSuccessTimeout(connStrBuilder.ConnectionString, 0, 5, "Checking the timeout between 0 to 5 seconds");

    }

    [Test, Description("scenario 1(connectionString,connectionUri,Anonymous Object,MysqlxStringBuilder with connect timeout option=0 for offline server)")]
    public void TimeoutOfflineServerConnectOptionZero()
    {
      int connectionTimeout = 0;
      string serverName = "vigdis07.no.oracle.com";

      string connStr = "server=" + serverName + ";user=" + session.Settings.UserID + ";port=" + XPort + ";password="
            + session.Settings.Password + ";" + "connect-timeout=" + connectionTimeout;
      TestConnectStringTimeoutFailureTimeout(connStr, 0, 50000, "Checking the timeout between 0 to 50000 milliseconds");

      connStr = "mysqlx://" + session.Settings.UserID + ":" + session.Settings.Password + "@" + serverName + ":" + XPort + "?connect-timeout=" + connectionTimeout;
      TestConnectStringTimeoutFailureTimeout(connStr, 0, 50000, "Checking the timeout between 0 to 50000 milliseconds");

      var connObj = new { server = serverName, port = XPort, user = session.Settings.UserID, password = session.Settings.Password, connecttimeout = connectionTimeout };
      TestConnectObjTimeoutFailureTimeout(connObj, 0, 50000, "Checking the timeout between 0 to 50000 milliseconds");

      var connStrBuilder = new MySqlXConnectionStringBuilder();
      connStrBuilder.ConnectTimeout = (uint)connectionTimeout;
      connStrBuilder.UserID = session.Settings.UserID;
      connStrBuilder.Password = session.Settings.Password;
      connStrBuilder.Port = Convert.ToUInt32(XPort);
      connStrBuilder.Server = serverName;
      TestConnectStringTimeoutFailureTimeout(connStr, 0, 50000, "Checking the timeout between 0 to 50000 milliseconds");

      connStr = "server=" + serverName + ";user=" + session.Settings.UserID + ";port=" + XPort + ";password="
          + session.Settings.Password + ";" + "connect-timeout=" + connectionTimeout;
      connStrBuilder = new MySqlXConnectionStringBuilder(connStr);
      TestConnectStringTimeoutFailureTimeout(connStr, 0, 50000, "Checking the timeout between 0 to 50000 milliseconds");

    }

    [Test, Description("(connectionString,connectionUri,Anonymous Object.Test that the timeout will be reset for each connection attempt in a failover scenario")]
    public void ConnectTimeoutSeveralAddreses()
    {
      StringBuilder hostList = new StringBuilder();
      int connectionTimeout = 1000;
      var priority = 100;
      for (var i = 1; i <= 101; i++)
      {
        hostList.Append("(address=server" + i + ".example,priority=" + (priority != 0 ? priority-- : 0) + "),");
        if (i == 101) hostList.Append($"(address={Host},priority=0)");
      }

      using (var session1 = MySQLX.GetSession("server=" + hostList + ";port=" + XPort + ";uid=" +
                                             session.Settings.UserID + ";password=" + session.Settings.Password + ";connect-timeout=" +
                                             connectionTimeout + ";ssl-mode=required"))
      {
        Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
        var schema = session1.GetSchema("test");
        Assert.IsNotNull(schema);
      }

      var connStr = "mysqlx://" + session.Settings.UserID + ":" + session.Settings.Password + "@[" + hostList + "]/?connect-timeout=" + connectionTimeout;
      using (var session1 = MySQLX.GetSession(connStr))
      {
        Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
        var schema = session1.GetSchema("test");
        Assert.IsNotNull(schema);
      }

      using (var session1 = MySQLX.GetSession(new
      {
        server = hostList.ToString(),
        port = XPort,
        user = session.Settings.UserID,
        password = session.Settings.Password,
        sslmode = MySqlSslMode.Required
      }))
      {
        Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
        var schema = session1.GetSchema("test");
        Assert.IsNotNull(schema);
      }

      var strList = "(address=143.24.20.36,priority=0),(address=10.172.165.157,priority=1)";
      connectionTimeout = 2000;
      var connString = "server=" + strList + ";port=" + XPort + ";uid=" +
                        session.Settings.UserID + ";password=" + session.Settings.Password + ";connect-timeout=" +
                        connectionTimeout + ";ssl-mode=required";
      Stopwatch sw = new Stopwatch();
      sw.Start();
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connString));
      sw.Stop();
      Assert.True(sw.Elapsed.Seconds > 0 && sw.Elapsed.Seconds < 21);

      connStr = "mysqlx://" + session.Settings.UserID + ":" + session.Settings.Password + "@[" + strList + "]" + "/?connect-timeout=" + connectionTimeout;
      sw = new Stopwatch();
      sw.Start();
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connString));
      sw.Stop();
      Assert.True(sw.Elapsed.Seconds > 0 && sw.Elapsed.Seconds < 21);

      var connObj1 = new
      {
        server = strList,
        port = XPort,
        user = session.Settings.UserID,
        password = session.Settings.Password,
        connecttimeout = connectionTimeout
      };
      sw = new Stopwatch();
      sw.Start();
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connString));
      sw.Stop();
      Assert.True(sw.Elapsed.Seconds > 0 && sw.Elapsed.Seconds < 21);
    }

    [Test, Description("(connectionString,connectionUri,Anonymous Object.Test that the timeout will be reset for each connection attempt in a failover scenario")]
    public void ConnectTimeoutWithFailoverAndNotValidHost()
    {
      string hostList = "(address=143.24.20.36,priority=0),(address=143.24.70.98,priority=1)";
      int connectionTimeout = 2000;
      // URL
      var connString = "server=" + hostList + ";port=" + XPort + ";uid=" +
                        session.Settings.UserID + ";password=" + session.Settings.Password + ";connect-timeout=" +
                        connectionTimeout;
      Stopwatch sw = new Stopwatch();
      sw.Start();
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connString));
      sw.Stop();
      Assert.True(sw.Elapsed.Seconds > 0 && sw.Elapsed.Seconds < 10);
      // URI
      var connStr = "mysqlx://" + session.Settings.UserID + ":" + session.Settings.Password + "@[" + hostList + "]" + "/?connect-timeout=" + connectionTimeout;
      sw = new Stopwatch();
      sw.Start();
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connString));
      sw.Stop();
      Assert.True(sw.Elapsed.Seconds > 0 && sw.Elapsed.Seconds < 10);

      // Object
      var connObj = new
      {
        server = hostList,
        port = XPort,
        user = session.Settings.UserID,
        password = session.Settings.Password,
        connecttimeout = connectionTimeout
      };
      sw = new Stopwatch();
      sw.Start();
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connString));
      sw.Stop();
      Assert.True(sw.Elapsed.Seconds > 0 && sw.Elapsed.Seconds < 10);
    }

    [Test, Description("Confirm that the timeout is only applied to the connection process, not to any subsequent operation after the connection is established")]
    public void ValidateConnectTimeoutScope()
    {
      if (!Platform.IsWindows()) return;
      string connStr = null;
      connStr = "server=" + session.Settings.Server + ";user=" + session.Settings.UserID + ";port=" + XPort + ";password="
            + session.Settings.Password + ";" + "connect-timeout=10000;";
      using (var conn = MySQLX.GetSession(connStr))
      {
        conn.SQL("SELECT SLEEP(10)").Execute();
        var res = conn.SQL("select @@port").Execute().FirstOrDefault();
        Assert.IsNotNull(res);
      }

      connStr = "mysqlx://" + session.Settings.UserID + ":" + session.Settings.Password + "@" + session.Settings.Server + ":" + XPort + "?connect-timeout=10000;";
      using (var conn = MySQLX.GetSession(connStr))
      {
        conn.SQL("SELECT SLEEP(10)").Execute();
        var res = conn.SQL("select @@port").Execute().FirstOrDefault();
        Assert.IsNotNull(res);
      }

      var connObj = new
      {
        server = session.Settings.Server,
        port = XPort,
        user = session.Settings.UserID,
        password = session.Settings.Password,
        connecttimeout = 10000
      };
      using (var conn = MySQLX.GetSession(connObj))
      {
        conn.SQL("SELECT SLEEP(10)").Execute();
        var res = conn.SQL("select @@port").Execute().FirstOrDefault();
        Assert.IsNotNull(res);
      }

      var connStrBuilder = new MySqlXConnectionStringBuilder();
      connStrBuilder.ConnectTimeout = 10000;
      connStrBuilder.UserID = session.Settings.UserID;
      connStrBuilder.Password = session.Settings.Password;
      connStrBuilder.Port = Convert.ToUInt32(XPort);
      connStrBuilder.Server = session.Settings.Server;
      using (var conn = MySQLX.GetSession(connStrBuilder.ConnectionString))
      {
        conn.SQL("SELECT SLEEP(10)").Execute();
        var res = conn.SQL("select @@port").Execute().FirstOrDefault();
        Assert.IsNotNull(res);
      }

      connStr = "server=" + session.Settings.Server + ";user=" + session.Settings.UserID + ";port=" + XPort + ";password="
                  + session.Settings.Password + ";" + "connect-timeout=10000;";
      connStrBuilder = new MySqlXConnectionStringBuilder(connStr);
      using (var conn = MySQLX.GetSession(connStrBuilder.ConnectionString))
      {
        conn.SQL("SELECT SLEEP(10)").Execute();
        var res = conn.SQL("select @@port").Execute().FirstOrDefault();
        Assert.IsNotNull(res);
      }

      connStr = ConnectionString + ";protocol=Socket;" +
                    "database=" + schemaName + ";characterset=utf8mb4;sslmode=Required;ssl-ca="
                    + sslCa + $";certificatepassword={sslCertificatePassword};certificatestorelocation=LocalMachine;"
                    + ";keepalive =10;auth=PLAIN;certificatethumbprint=;"
                    + "connect-timeout=" + 10000;
      var mysqlx0 = new MySqlXConnectionStringBuilder(connStr);
      using (var conn = MySQLX.GetSession(mysqlx0.ConnectionString))
      {
        conn.SQL("SELECT SLEEP(10)").Execute();
        var res = conn.SQL("select @@port").Execute().FirstOrDefault();
        Assert.IsNotNull(res);
      }

      mysqlx0 = new MySqlXConnectionStringBuilder();
      mysqlx0.UserID = session.Settings.UserID;
      mysqlx0.Password = session.Settings.Password;
      mysqlx0.Port = Convert.ToUInt32(XPort);
      mysqlx0.Server = session.Settings.Server;
      mysqlx0.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      mysqlx0.Database = schemaName;
      mysqlx0.CharacterSet = "utf8mb4";
      mysqlx0.SslMode = MySqlSslMode.Required;
      mysqlx0.SslCa = sslCa;
      mysqlx0.CertificatePassword = sslCertificatePassword;
      mysqlx0.CertificateStoreLocation = MySqlCertificateStoreLocation.LocalMachine;
      mysqlx0.Keepalive = 10;
      mysqlx0.Auth = MySqlAuthenticationMode.PLAIN;
      mysqlx0.CertificateThumbprint = "";
      mysqlx0.ConnectTimeout = (uint)10000;

      using (var conn = MySQLX.GetSession(mysqlx0.ConnectionString))
      {
        conn.SQL("SELECT SLEEP(10)").Execute();
        var res = conn.SQL("select @@port").Execute().FirstOrDefault();
        Assert.IsNotNull(res);
      }
    }

    [Test, Description("Test that if an unexpected error occurs during the specified time frame, the execution should stop and the error must be reported to the user")]
    public void WrongPasswordException()
    {
      string connStr = null;
      string password = "wrongpassword";
      Object[] ConnectTimeout = new Object[] { 10000 };
      for (int i = 0; i < ConnectTimeout.Length; i++)
      {
        // Connection String
        connStr = "server=" + session.Settings.Server + ";user=" + session.Settings.UserID + ";port=" + XPort + ";password="
              + password + ";" + "connect-timeout=" + ConnectTimeout[i];
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connStr));
        //String Builder
        var connStrBuilder = new MySqlXConnectionStringBuilder(connStr);
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connStrBuilder.ConnectionString));
        // Uri
        connStr = "mysqlx://" + session.Settings.UserID + ":" + password + "@" + session.Settings.Server + ":" + XPort + "?connect-timeout=" + ConnectTimeout[i];
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connStr));
        // Anonymous Object
        var connObj = new { server = session.Settings.Server, port = XPort, user = session.Settings.UserID, password = password, connecttimeout = ConnectTimeout[i] };
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connStr));
      }
    }

    [Test, Description("Test the default connect timeout with offline server with concurrent connections")]
    [Ignore("Test its not well implemented")] // TO DO
    public async Task ConnectTimeoutConcurrentConnections()
    {
      await Task.Run(() => SubThread1());
      await Task.Run(() => SubThread2());
    }

    private void SubThread1()
    {
      string serverName = "vigdis07.no.oracle.com";
      for (int i = 0; i < 5; i++)
      {
        string connStr = "server=" + serverName + ";user=" + session.Settings.UserID + ";port=" + XPort + ";password="
                  + session.Settings.Password + ";" + "connect-timeout=2000;";
        TestConnectStringTimeoutFailureTimeout(connStr, 0, 5, "Timeout value between 1 and 3 second");
      }
    }

    private void SubThread2()
    {
      string serverName = "vigdis07.no.oracle.com";
      for (int i = 0; i < 5; i++)
      {
        string connStr = "server=" + serverName + ";user=" + session.Settings.UserID + ";port=" + XPort + ";password="
                  + session.Settings.Password + ";" + "connect-timeout=2000;";
        TestConnectStringTimeoutFailureTimeout(connStr, 0, 5, "Timeout value between 1 and 3 second");
      }
    }


    [Test, Description("CONNECT-TIMEOUT WORKS WITH BLANK VALUES FOR CONNECTION STRING AND URI")]
    public void ConnectTimeoutBlankValues()
    {
      var connObj = new { server = session.Settings.Server, port = XPort, user = session.Settings.UserID, password = session.Settings.Password, connecttimeout = "" };
      Assert.Throws<FormatException>(() => MySQLX.GetSession(connObj));

      connObj = new { server = session.Settings.Server, port = XPort, user = session.Settings.UserID, password = session.Settings.Password, connecttimeout = " " };
      Assert.Throws<FormatException>(() => MySQLX.GetSession(connObj));

      var connStr = $"server={session.Settings.Server};user={session.Settings.UserID};port={XPort};password={session.Settings.Password};connect-timeout=''";
      Assert.Throws<FormatException>(() => MySQLX.GetSession(connStr));

      connStr = $"server={session.Settings.Server};user={session.Settings.UserID};port={XPort};password={session.Settings.Password};connect-timeout=' '";
      Assert.Throws<FormatException>(() => MySQLX.GetSession(connStr));

      connStr = $"server={session.Settings.Server};user={session.Settings.UserID};port={XPort};password={session.Settings.Password};connect-timeout=";
      Assert.Throws<FormatException>(() => MySQLX.GetSession(connStr));

      connStr = $"server={session.Settings.Server};user={session.Settings.UserID};port={XPort};password={session.Settings.Password};connect-timeout= ";
      Assert.Throws<FormatException>(() => MySQLX.GetSession(connStr));

      connStr = $"mysqlx://{session.Settings.UserID}:{session.Settings.Password}@{session.Settings.Server}:{XPort}?connect-timeout=";
      Assert.Throws<FormatException>(() => MySQLX.GetSession(connStr));

      connStr = $"mysqlx://{session.Settings.UserID}:{session.Settings.Password}@{session.Settings.Server}:{XPort}?connect-timeout= ";
      Assert.Throws<FormatException>(() => MySQLX.GetSession(connStr));
    }

    /// <summary>
    /// Bug28624010 
    /// </summary>
    [Test, Description("CONNECTIONTIMEOUT OPT WORKS WITH XPLUGN IF A CLASSIC CONN IS ESTABLISD")]
    public void TimeoutWithClassicConnection()
    {
      int connectionTimeout = 10;
      var connStr1 = $"server={session.Settings.Server};user={session.Settings.UserID};port={Port};password={session.Settings.Password};sslmode={MySqlSslMode.Required}";
      var conn = new MySqlConnection(connStr1);
      conn.Open();
      conn.Close();
      var connStr = $"server={session.Settings.Server};user={session.Settings.UserID};port={XPort};password={session.Settings.Password};connectiontimeout={connectionTimeout}";
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connStr));

      connStr = $"mysqlx://{session.Settings.UserID}:{session.Settings.Password}@{session.Settings.Server}:{XPort}?connectiontimeout={connectionTimeout} ";
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connStr));

      var connObj = new { server = session.Settings.Server, port = XPort, user = session.Settings.UserID, password = session.Settings.Password, connectiontimeout = connectionTimeout };
      Assert.Throws<KeyNotFoundException>(() => MySQLX.GetSession(connObj));
    }

    [Test, Description("MySQLX Session Stress test")]
    public void SessionStressTest()
    {
      for (int i = 0; i < 1000; i++)
      {
        using (Session session1 = MySQLX.GetSession(ConnectionString))
        {
          Assert.AreEqual(SessionState.Open, session1.InternalSession.SessionState);
          session1.Close();
        }
      }
    }

    [Test, Description("Getsession using Anonymous Type-Positive")]
    public void GetSessionAnonymousTypePositiveStress()
    {
      for (int i = 0; i < 200; i++)
      {
        var connectionStringObject = new { connection = $"server={Host};user={session.Settings.UserID};port={XPort};password={session.Settings.Password};sslmode={MySqlSslMode.Required};" };
        using (Session sessionPlain = MySQLX.GetSession(connectionStringObject.connection))
        {
          var db = sessionPlain.GetSchema(schemaName);
          var col = CreateCollection("my_collection_123456789");
          sessionPlain.Close();
        }
      }
    }

    [Test, Description("Test Audit Plugin")]
    public void SessionAuditPluginTest()
    {
      using (var mysqlx = MySQLX.GetSession(ConnectionString))
      {
        Assert.AreEqual(SessionState.Open, mysqlx.InternalSession.SessionState);
        mysqlx.Close();
      }
      using (var mysql = new MySqlConnection($"server={Host};user={session.Settings.UserID};port={Port};password={session.Settings.Password}"))
      {
        mysql.Open();
        Assert.AreEqual(ConnectionState.Open, mysql.connectionState);
        mysql.Close();
      }
      Assert.Throws<MySqlException>(() => MySQLX.GetSession($"server={Host};user={session.Settings.UserID};port={XPort};password=wrong"));
    }

    [Test, Description("Classic Client with xprotocol server")]
    public void ClassicClientXProtocol()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is for Windows OS only");
      if (!session.Version.isAtLeast(5, 7, 0)) Assert.Ignore("This test is for MySql 5.7 or higher");

      string connectionString = $"server={Host};user={session.Settings.UserID};port={XPort};password={session.Settings.Password}";
      using (var session1 = new MySqlConnection(connectionString))
      {
        Exception ex = Assert.Throws<MySqlException>(() => session1.Open());
        Assert.AreEqual("Unsupported protocol version.", ex.Message);
      }
    }

    #region Methods

    public void TestConnectObjectTimeoutSuccessTimeout(object connString, int minTime, int maxTime, string test)
    {
      Stopwatch sw = new Stopwatch();
      sw.Start();
      var conn = MySQLX.GetSession(connString);
      sw.Stop();
      Assert.True(sw.Elapsed.Seconds >= minTime && sw.Elapsed.Seconds <= maxTime, String.Format("Timeout exceeded ({0}). Actual time: {1}", test, sw.Elapsed));
    }

    public void TestConnectStringTimeoutSuccessTimeout(String connString, int minTime, int maxTime, string test)
    {
      Stopwatch sw = new Stopwatch();
      sw.Start();
      var conn = MySQLX.GetSession(connString);
      sw.Stop();
      Assert.True(sw.Elapsed.Seconds >= minTime && sw.Elapsed.Seconds <= maxTime, String.Format("Timeout exceeded ({0}). Actual time: {1}", test, sw.Elapsed));
    }

    public void TestConnectObjTimeoutFailureTimeout(object connString, int minTime, int maxTime, string test)
    {
      Stopwatch sw = new Stopwatch();
      sw.Start();
      Assert.Catch(() => MySQLX.GetSession(connString));
      sw.Stop();
      Assert.True(sw.Elapsed.Seconds >= minTime && sw.Elapsed.Seconds < maxTime, String.Format("Timeout exceeded ({0}). Actual time: {1}", test, sw.Elapsed));
    }

    public void TestConnectStringTimeoutFailureTimeout(String connString, int minTime, int maxTime, string test)
    {
      Stopwatch sw = new Stopwatch();
      sw.Start();
      Assert.Catch(() => MySQLX.GetSession(connString));
      sw.Stop();
      Assert.True(sw.Elapsed.Seconds >= minTime && sw.Elapsed.Seconds < maxTime, String.Format("Timeout exceeded ({0}). Actual time: {1}", test, sw.Elapsed));
    }

    public void TestClientQueueTimeout(Client client, int minTime, int maxTime, string test)
    {
      Stopwatch sw = new Stopwatch();
      sw.Start();
      Assert.Throws<TimeoutException>(() => client.GetSession());
      sw.Stop();
      Assert.True(sw.Elapsed.Seconds >= minTime && sw.Elapsed.Seconds < maxTime,
          String.Format("Timeout exceeded ({0}). Actual time: {1}", test, sw.Elapsed));
    }

    public void TestClientSuccessTimeout(int minTime, int maxTime, string test, string connectionString, object poolingObject)
    {
      using (var client = MySQLX.GetClient(connectionString, poolingObject))
      {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        client.GetSession();
        sw.Stop();
        Assert.True(sw.Elapsed.Seconds >= minTime && sw.Elapsed.Seconds < maxTime, String.Format("Timeout exceeded ({0}). Actual time: {1}", test, sw.Elapsed));
      }
    }

    public void TestClientSuccessTimeout(int minTime, int maxTime, string test, object connectionObject, object poolingObject)
    {
      using (var client = MySQLX.GetClient(connectionObject, poolingObject))
      {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        client.GetSession();
        sw.Stop();
        Assert.True(sw.Elapsed.Seconds >= minTime && sw.Elapsed.Seconds < maxTime, String.Format("Timeout exceeded ({0}). Actual time: {1}", test, sw.Elapsed));
      }
    }
    public void TestFailureTimeout(Client client, int minTime, int maxTime, string test)
    {
      DateTime start = DateTime.Now;
      Assert.Catch(() => client.GetSession());
      TimeSpan diff = DateTime.Now.Subtract(start);
      Assert.True(diff.TotalSeconds >= minTime && diff.TotalSeconds < maxTime, String.Format("Timeout exceeded ({0}). Actual time: {1}", test, diff));
    }

    private void MeasureConnectionString(string connStr, int maxTime, string test, int iteration)
    {
      Stopwatch sw = new Stopwatch();
      sw.Start();

      for (int i = 0; i < iteration; i++)
      {
        using Session conn = MySQLX.GetSession(connStr);
      }

      sw.Stop();
      Assert.True(sw.Elapsed.Seconds < maxTime, String.Format("Timeout exceeded ({0}). Actual time: {1}", test, sw.Elapsed));
    }

    private void MeasureConnectionObject(object connStr, int maxTime, string test, int iteration)
    {
      Stopwatch sw = new Stopwatch();
      sw.Start();

      for (int i = 0; i < iteration; i++)
      {
        using Session conn = MySQLX.GetSession(connStr);
      }

      sw.Stop();
      Assert.True(sw.Elapsed.Seconds < maxTime, String.Format("Timeout exceeded ({0}). Actual time: {1}", test, sw.Elapsed));
    }

    public long NanoTime()
    {
      long nano = 10000L * Stopwatch.GetTimestamp();
      nano /= TimeSpan.TicksPerMillisecond;
      nano *= 100L;
      return nano;
    }

    public long DoConnectString(string connectionString)
    {
      long queryStartTime, queryRunTime = 0;
      queryStartTime = NanoTime();
      var session = MySQLX.GetSession(connectionString);
      queryRunTime = NanoTime() - queryStartTime;
      session.Close();
      session.Dispose();
      return queryRunTime;
    }

    public long DoConnectObject(object connectionString)
    {
      long queryStartTime, queryRunTime = 0;
      queryStartTime = NanoTime();
      var session = MySQLX.GetSession(connectionString);
      queryRunTime = NanoTime() - queryStartTime;
      session.Close();
      session.Dispose();
      return queryRunTime;
    }

    /// <summary>
    /// Calculate the Connection per second </summary>
    /// <param name="elapsedTime">Total execution time.(Connection+query execution time)</param>
    /// <param name="transactions">Query execution time </param>
    public float CalculateTPS(long elapsedTime, long transactions)
    {
      float tps = 0;
      if (elapsedTime > 0.0)
      {
        tps = (float)transactions / elapsedTime;
        tps = tps * 1000;
      }
      return tps;
    }

    #endregion Methods

  }
}
