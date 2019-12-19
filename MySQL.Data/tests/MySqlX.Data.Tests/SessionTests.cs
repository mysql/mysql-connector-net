// Copyright (c) 2015, 2019, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class SessionTests : BaseTest
  {
    [Fact]
    [Trait("Category", "Security")]
    public void CanCloseSession()
    {
      Session s = MySQLX.GetSession(ConnectionString);
      Assert.True(s.InternalSession.SessionState == SessionState.Open);
      s.Close();
      Assert.Equal(s.InternalSession.SessionState, SessionState.Closed);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void NoPassword()
    {
      Session session = MySQLX.GetSession(ConnectionStringNoPassword);
      Assert.True(session.InternalSession.SessionState == SessionState.Open);
      session.Close();
      Assert.Equal(session.InternalSession.SessionState, SessionState.Closed);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SessionClose()
    {
      Session session = MySQLX.GetSession(ConnectionString);
      Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      session.Close();
      Assert.Equal(SessionState.Closed, session.InternalSession.SessionState);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void CountClosedSession()
    {
      Session nodeSession = MySQLX.GetSession(ConnectionString);
      int sessions = ExecuteSQLStatement(nodeSession.SQL("show processlist")).FetchAll().Count;

      for (int i = 0; i < 20; i++)
      {
        Session session = MySQLX.GetSession(ConnectionString);
        Assert.True(session.InternalSession.SessionState == SessionState.Open);
        session.Close();
        Assert.Equal(session.InternalSession.SessionState, SessionState.Closed);
      }

      int newSessions = ExecuteSQLStatement(nodeSession.SQL("show processlist")).FetchAll().Count;
      nodeSession.Close();
      Assert.Equal(sessions, newSessions);
    }

    [Fact]
    [Trait("Category", "Security")]
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
        Assert.Equal(SessionState.Open, testSession.InternalSession.SessionState);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SessionGetSetCurrentSchema()
    {
      using (Session testSession = MySQLX.GetSession(ConnectionString))
      {
        Assert.Equal(SessionState.Open, testSession.InternalSession.SessionState);
        Assert.Null(testSession.GetCurrentSchema());
        Assert.Throws<MySqlException>(() => testSession.SetCurrentSchema(""));
        testSession.SetCurrentSchema(schemaName);
        Assert.Equal(schemaName, testSession.Schema.Name);
        Assert.Equal(schemaName, testSession.GetCurrentSchema().Name);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SessionUsingSchema()
    {
      using (Session mySession = MySQLX.GetSession(ConnectionString + $";database={schemaName};"))
      {
        Assert.Equal(SessionState.Open, mySession.InternalSession.SessionState);
        Assert.Equal(schemaName, mySession.Schema.Name);
        Assert.Equal(schemaName, mySession.GetCurrentSchema().Name);
        Assert.True(SchemaExistsInDatabase(mySession.Schema));
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SessionUsingDefaultSchema()
    {
      using (Session mySession = MySQLX.GetSession(ConnectionString + $";database={schemaName};"))
      {
        Assert.Equal(SessionState.Open, mySession.InternalSession.SessionState);
        Assert.Equal(schemaName, mySession.DefaultSchema.Name);
        Assert.Equal(schemaName, mySession.GetCurrentSchema().Name);
        Assert.True(mySession.Schema.ExistsInDatabase());
        mySession.SetCurrentSchema("mysql");
        Assert.NotEqual(mySession.DefaultSchema.Name, mySession.Schema.Name);
      }

      // DefaultSchema is null because no database was provided in the connection string/URI.
      using (Session mySession = MySQLX.GetSession(ConnectionString))
      {
        Assert.Equal(SessionState.Open, mySession.InternalSession.SessionState);
        Assert.Null(mySession.DefaultSchema);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
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
        Assert.Equal("mysql", internalSession.DefaultSchema.Name);
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
        Assert.StartsWith(string.Format("Unknown database 'test1'"), exception.Message);
      else
        Assert.StartsWith(string.Format("Access denied"), exception.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SessionUsingDefaultSchemaWithConnectionURI()
    {
      using (var session = MySQLX.GetSession(ConnectionStringUri + "?database=mysql"))
      {
        Assert.Equal("mysql", session.DefaultSchema.Name);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void CheckConnectionUri()
    {
      CheckConnectionData("mysqlx://myuser:password@localhost:33060", "myuser", "password", "localhost", 33060);
      CheckConnectionData("mysqlx://my%3Auser:p%40ssword@localhost:33060", "my:user", "p@ssword", "localhost", 33060);
      CheckConnectionData("mysqlx://my%20user:p%40ss%20word@localhost:33060", "my user", "p@ss word", "localhost", 33060);
      CheckConnectionData("mysqlx:// myuser : p%40ssword@localhost:33060", "myuser", "p@ssword", "localhost", 33060);
      CheckConnectionData("mysqlx://myuser@localhost:33060", "myuser", "", "localhost", 33060);
      CheckConnectionData("mysqlx://myuser:p%40ssword@localhost", "myuser", "p@ssword", "localhost", 33060);
      CheckConnectionData("mysqlx://myuser:p%40ssw%40rd@localhost", "myuser", "p@ssw@rd", "localhost", 33060);
      CheckConnectionData("mysqlx://my%40user:p%40ssword@localhost", "my@user", "p@ssword", "localhost", 33060);
      CheckConnectionData("mysqlx://myuser@localhost", "myuser", "", "localhost", 33060);
      CheckConnectionData("mysqlx://myuser@127.0.0.1", "myuser", "", "127.0.0.1", 33060);
      CheckConnectionData("mysqlx://myuser@[::1]", "myuser", "", "[::1]", 33060);
      CheckConnectionData("mysqlx://myuser:password@[2606:b400:440:1040:bd41:e449:45ee:2e1a]", "myuser", "password", "[2606:b400:440:1040:bd41:e449:45ee:2e1a]", 33060);
      CheckConnectionData("mysqlx://myuser:password@[2606:b400:440:1040:bd41:e449:45ee:2e1a]:33060", "myuser", "password", "[2606:b400:440:1040:bd41:e449:45ee:2e1a]", 33060);
      Assert.Throws<UriFormatException>(() => CheckConnectionData("mysqlx://myuser:password@[2606:b400:440:1040:bd41:e449:45ee:2e1a:33060]", "myuser", "password", "[2606:b400:440:1040:bd41:e449:45ee:2e1a]", 33060));
      Assert.Throws<UriFormatException>(() => CheckConnectionData("mysqlx://myuser:password@2606:b400:440:1040:bd41:e449:45ee:2e1a:33060", "myuser", "password", "[2606:b400:440:1040:bd41:e449:45ee:2e1a]", 33060));
      CheckConnectionData("mysqlx://myuser:password@[fe80::bd41:e449:45ee:2e1a%17]", "myuser", "password", "[fe80::bd41:e449:45ee:2e1a]", 33060);
      CheckConnectionData("mysqlx://myuser:password@[(address=[fe80::bd41:e449:45ee:2e1a%17],priority=100)]", "myuser", "password", "[fe80::bd41:e449:45ee:2e1a]", 33060);
      CheckConnectionData("mysqlx://myuser:password@[(address=[fe80::bd41:e449:45ee:2e1a%17]:3305,priority=100)]", "myuser", "password", "[fe80::bd41:e449:45ee:2e1a]", 3305);
      Assert.Throws<UriFormatException>(() => CheckConnectionData("mysqlx://myuser:password@[(address=fe80::bd41:e449:45ee:2e1a%17,priority=100)]", "myuser", "password", "[fe80::bd41:e449:45ee:2e1a]", 33060));
      CheckConnectionData("mysqlx://myuser@localhost/test", "myuser", "", "localhost", 33060, "database", "test");
      CheckConnectionData("mysqlx://myuser@localhost/test?ssl%20mode=none&connecttimeout=10", "myuser", "", "localhost", 33060, "database", "test", "ssl mode", "None", "connecttimeout", "10");
      CheckConnectionData("mysqlx+ssh://myuser:password@localhost:33060", "myuser", "password", "localhost", 33060);
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
      CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version=[ TlSv1.2 ,tLsV11, TLSv1.0 , tls13 ]", "myuser", "password", "localhost", 33060, "tls-version", "Tls, Tls11, Tls12, Tls13");
      CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version=( TlSv1.2 ,tLsV11, TLSv1 , tls13 )", "myuser", "password", "localhost", 33060, "tls-version", "Tls, Tls11, Tls12, Tls13");
      CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version= TlSv1.2 ,tLsV11, TLSv10 , tls13", "myuser", "password", "localhost", 33060, "tls-version", "Tls, Tls11, Tls12, Tls13");
      Assert.Throws<ArgumentException>(() => CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version=TlSv1.2,tLsV2.1", "myuser", "password", "localhost", 33060, "tls-version", ""));
      Assert.Throws<ArgumentException>(() => CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version=SSL3", "myuser", "password", "localhost", 33060, "tls-version", ""));
      Assert.Throws<ArgumentException>(() => CheckConnectionData("mysqlx://myuser:password@localhost:33060?ssl-mode=none&tls-version=TlsV1.2", "myuser", "password", "localhost", 33060, "tls-version", ""));
      Assert.Throws<ArgumentException>(() => CheckConnectionData("mysqlx://myuser:password@localhost:33060?tls-version=TlsV1.2&ssl-mode=none", "myuser", "password", "localhost", 33060, "tls-version", ""));
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectionUsingUri()
    {
      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectionStringNull()
    {
      Assert.Throws<ArgumentNullException>(() => MySQLX.GetSession(null));
    }

    [Fact]
    [Trait("Category", "Security")]
    public void IPv6()
    {
      var csBuilder = new MySqlXConnectionStringBuilder(ConnectionString);
      csBuilder.Server = "::1";
      csBuilder.Port = uint.Parse(XPort);

      using (var session = MySQLX.GetSession(csBuilder.ToString()))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void IPv6AsUrl()
    {
      var csBuilder = new MySqlXConnectionStringBuilder(ConnectionString);
      string connString = $"mysqlx://{csBuilder.UserID}:{csBuilder.Password}@[::1]:{XPort}";
      using (Session session = MySQLX.GetSession(connString))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void IPv6AsAnonymous()
    {
      var csBuilder = new MySqlXConnectionStringBuilder(ConnectionString);
      using (Session session = MySQLX.GetSession(new { server = "::1", user = csBuilder.UserID, password = csBuilder.Password, port = XPort }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void CreateSessionWithUnsupportedOptions()
    {
      var errorMessage = "Option not supported.";
      var connectionUri = string.Format("{0}?", ConnectionStringUri);

      // Use a connection URI.
      var ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "pipe=MYSQL"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "compress=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "allow batch=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "logging=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "sharedmemoryname=MYSQL"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "defaultcommandtimeout=30"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "usedefaultcommandtimeoutforef=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "persistsecurityinfo=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "encrypt=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "integratedsecurity=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "allowpublickeyretrieval=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "autoenlist=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "includesecurityasserts=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "allowzerodatetime=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "convert zero datetime=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "useusageadvisor=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "procedurecachesize=50"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "useperformancemonitor=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "ignoreprepare=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "respectbinaryflags=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "treat tiny as boolean=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "allowuservariables=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "interactive=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "functionsreturnstring=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "useaffectedrows=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "oldguids=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "sqlservermode=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "tablecaching=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "defaulttablecacheage=60"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "checkparameters=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "replication=replication_group"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "exceptioninterceptors=none"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "commandinterceptors=none"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "connectionlifetime=100"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "pooling=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "minpoolsize=0"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "maxpoolsize=20"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "connectionreset=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionUri + "cacheserverproperties=true"));
      Assert.StartsWith(errorMessage, ex.Message);

      // Use a connection string.
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("treatblobsasutf8=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("blobasutf8includepattern=pattern"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession("blobasutf8excludepattern=pattern"));
      Assert.StartsWith(errorMessage, ex.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void CreateBuilderWithUnsupportedOptions()
    {
      var errorMessage = "Option not supported.";
      var ex = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder("pipe=MYSQL"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder("allow batch=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder("respectbinaryflags=true"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder("pooling=false"));
      Assert.StartsWith(errorMessage, ex.Message);
      ex = Assert.Throws<ArgumentException>(() => new MySqlXConnectionStringBuilder("cacheserverproperties=true"));
      Assert.StartsWith(errorMessage, ex.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
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
            Assert.Equal(session.Settings[connectionOption.Key], internalSession.Settings[connectionOption.Key]);
          }
          catch (ArgumentException ex)
          {
            Assert.StartsWith("Option not supported.", ex.Message);
          }
        }
      }
    }

    /// <summary>
    /// WL #12177 Implement connect timeout
    /// </summary>
    [Fact]
    [Trait("Category", "Security")]
    public void ConnectTimeout()
    {
      // Create a session passing the new parameter "connect-timeout" and set it to a valid value.
      // ConnectionString.
      using (Session session = MySQLX.GetSession(ConnectionString + ";connect-timeout=5000;"))
      {
        Assert.True(session.Settings.ConnectTimeout == 5000);
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }

      // ConnectionURI.
      using (Session session = MySQLX.GetSession(ConnectionStringUri + "?connecttimeout=6500"))
      {
        Assert.True(session.Settings.ConnectTimeout == 6500);
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
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
        Assert.Equal(SessionState.Open, testSession.InternalSession.SessionState);
      }

      // Offline (fake)host using default value, 10000ms.
      var conn = "server=143.24.20.36;user=test;password=test;port=33060;";
      TestConnectTimeoutFailureTimeout(conn, 9, 20, "Offline host default value");

      // Offline (fake)host using 15000ms.
      conn = "server=143.24.20.36;user=test;password=test;port=33060;connecttimeout=15000";
      TestConnectTimeoutFailureTimeout(conn, 14, 16, "Offline host 15000ms");

      // Offline (fake)host timeout disabled.
      conn = "server=143.24.20.36;user=test;password=test;port=33060;connecttimeout=0";
      TestConnectTimeoutFailureTimeout(conn, 10, 600, "Offline host timeout disabled");

      // Create a session using the fail over functionality passing two diferrent Server address(one of them is fake host). Must succeed after 2000ms
      conn = $"server=143.24.20.36,localhost;user=test;password=test;port={XPort};connecttimeout=2000;";
      TestConnectTimeoutSuccessTimeout(conn, 2, 4, "Fail over success");

      // Both (fake)servers offline. Connection must time out after 20000ms
      conn = "server=143.24.20.36,143.24.20.35;user=test;password=test;port=33060;";
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
      Assert.Equal(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionString + ";connect-timeout=foo;"));
      Assert.Equal(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionString + ";connect-timeout='';"));
      Assert.Equal(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionString + ";connect-timeout=10.5;"));
      Assert.Equal(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionString + ";connect-timeout=" + Int32.MaxValue + 1));
      Assert.Equal(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionString + ";connect-timeout=10.5;"));
      Assert.Equal(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionString + ";connect-timeout=;"));
      Assert.Equal(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionStringUri + "?connect-timeout= "));
      Assert.Equal(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      ex = Assert.Throws<FormatException>(() => MySQLX.GetSession(ConnectionStringUri + "?connecttimeout="));
      Assert.Equal(ResourcesX.InvalidConnectionTimeoutValue, ex.Message);

      // Valid value for ConnectionTimeout, invalid credentials
      var exception = Assert.Throws<MySqlException>(() => MySQLX.GetSession("server=localhost;user=test;password=noPass;port=33060;connect-timeout=2000;"));
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

    [Fact]
    [Trait("Category", "Security")]
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
        Assert.Equal(ResourcesX.UnableToOpenSession, ex.Message);
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
          Assert.Equal(parameters[i + 1], csbuilder[parameters[i]].ToString());
        }
      }
    }

    /// <summary>
    /// WL12514 - DevAPI: Support session-connect-attributes
    /// </summary>
    [Fact]
    [Trait("Category", "Security")]
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
      Assert.Equal(ResourcesX.InvalidUserDefinedAttribute, ex.Message);

      ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=123"));
      Assert.Equal(ResourcesX.InvalidConnectionAttributes, ex.Message);

      ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connection-attributes=[key=value,key=value2]"));
      Assert.Equal(string.Format(ResourcesX.DuplicateUserDefinedAttribute, "key"), ex.Message);

      ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(new { server = "localhost", port = 33060, user = "root", connectionattributes = "=" }));

      ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";connectionattributes=[=bar]"));
      Assert.Equal(string.Format(ResourcesX.EmptyKeyConnectionAttribute), ex.Message);
    }

    private void TestConnectionAttributes(string connString, Dictionary<string, object> userAttrs = null)
    {
      string sql = "SELECT * FROM performance_schema.session_account_connect_attrs WHERE PROCESSLIST_ID = connection_id()";

      using (Session session = MySQLX.GetSession(connString))
      {
        Assert.Equal(SessionState.Open, session.XSession.SessionState);
        var result = session.SQL(sql).Execute().FetchAll();

        if (session.Settings.ConnectionAttributes == "false")
          Assert.Empty(result);
        else
        {
          Assert.NotEmpty(result);
          MySqlConnectAttrs clientAttrs = new MySqlConnectAttrs();

          if (userAttrs == null)
          {
            Assert.Equal(8, result.Count);

            foreach (Row row in result)
              Assert.StartsWith("_", row[1].ToString());
          }
          else
          {
            Assert.Equal(11, result.Count);

            for (int i = 0; i < userAttrs.Count; i++)
            {
              Assert.True(userAttrs.ContainsKey(result.ElementAt(i)[1].ToString()));
              Assert.True(userAttrs.ContainsValue(result.ElementAt(i)[2]));
            }
          }
        }
      }
    }

    #region Authentication

    [Fact]
    [Trait("Category", "Security")]
    public void MySqlNativePasswordPlugin()
    {
      // TODO: Remove when support for caching_sha2_password plugin is included for X DevAPI.
      if (session.InternalSession.GetServerVersion().isAtLeast(8, 0, 4)) return;

      using (var session = MySQLX.GetSession(ConnectionStringUri))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        var result = ExecuteSQLStatement(session.SQL("SELECT `User`, `plugin` FROM `mysql`.`user` WHERE `User` = 'test';")).FetchAll();
        Assert.Equal("test", session.Settings.UserID);
        Assert.Equal(session.Settings.UserID, result[0][0].ToString());
        Assert.Equal("mysql_native_password", result[0][1].ToString());
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectUsingSha256PasswordPlugin()
    {
      string userName = "testSha256";
      string password = "mysql";
      string pluginName = "sha256_password";
      string connectionStringUri = ConnectionStringUri.Replace("test:test", string.Format("{0}:{1}", userName, password));

      // User with password over TLS connection.
      using (var session = MySQLX.GetSession(connectionStringUri))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        var result = ExecuteSQLStatement(session.SQL(string.Format("SELECT `User`, `plugin` FROM `mysql`.`user` WHERE `User` = '{0}';", userName))).FetchAll();
        Assert.Equal(userName, session.Settings.UserID);
        Assert.Equal(session.Settings.UserID, result[0][0].ToString());
        Assert.Equal(pluginName, result[0][1].ToString());
      }

      // Connect over non-TLS connection.
      using (var session = MySQLX.GetSession(connectionStringUri + "?sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(MySqlAuthenticationMode.SHA256_MEMORY, session.Settings.Auth);
      }

      // User without password over TLS connection.
      ExecuteSQL(String.Format("ALTER USER {0}@'localhost' IDENTIFIED BY ''", userName));
      using (var session = MySQLX.GetSession(ConnectionStringUri.Replace("test:test", string.Format("{0}:{1}", userName, ""))))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        var result = ExecuteSQLStatement(session.SQL(string.Format("SELECT `User`, `plugin` FROM `mysql`.`user` WHERE `User` = '{0}';", userName))).FetchAll();
        Assert.Equal(userName, session.Settings.UserID);
        Assert.Equal(session.Settings.UserID, result[0][0].ToString());
        Assert.Equal(pluginName, result[0][1].ToString());
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectUsingExternalAuth()
    {
      // Should fail since EXTERNAL is currently not supported by X Plugin.
      Exception ex = Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";auth=EXTERNAL"));
      Assert.Equal("Invalid authentication method EXTERNAL", ex.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectUsingPlainAuth()
    {
      using (var session = MySQLX.GetSession(ConnectionStringUri + "?auth=pLaIn"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(MySqlAuthenticationMode.PLAIN, session.Settings.Auth);
      }

      // Should fail since PLAIN requires TLS to be enabled.
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUri + "?auth=PLAIN&sslmode=none"));
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectUsingMySQL41Auth()
    {
      var connectionStringUri = ConnectionStringUri;
      if (session.InternalSession.GetServerVersion().isAtLeast(8, 0, 4))
      {
        // Use connection string uri set with a mysql_native_password user.
        connectionStringUri = ConnectionStringUriNative;
      }

      using (var session = MySQLX.GetSession(connectionStringUri + "?auth=MySQL41"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(MySqlAuthenticationMode.MYSQL41, session.Settings.Auth);
      }

      using (var session = MySQLX.GetSession(connectionStringUri + "?auth=mysql41&sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(MySqlAuthenticationMode.MYSQL41, session.Settings.Auth);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void DefaultAuth()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 5)) return;

      string user = "testsha256";

      ExecuteSQLStatement(session.SQL($"DROP USER IF EXISTS {user}@'localhost'"));
      ExecuteSQLStatement(session.SQL($"CREATE USER {user}@'localhost' IDENTIFIED WITH caching_sha2_password BY '{user}'"));

      string connString = $"mysqlx://{user}:{user}@localhost:{XPort}";
      // Default to PLAIN when TLS is enabled.
      using (var session = MySQLX.GetSession(connString))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(MySqlAuthenticationMode.PLAIN, session.Settings.Auth);
        var result = ExecuteSQLStatement(session.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';")).FetchAll();
        Assert.StartsWith("TLSv1", result[0][1].ToString());
      }

      // Default to SHA256_MEMORY when TLS is not enabled.
      using (var session = MySQLX.GetSession(connString + "?sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(MySqlAuthenticationMode.SHA256_MEMORY, session.Settings.Auth);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ConnectUsingSha256Memory()
    {
      if (!session.InternalSession.GetServerVersion().isAtLeast(8, 0, 5)) return;

      using (var session = MySQLX.GetSession(ConnectionStringUri + "?auth=SHA256_MEMORY"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(MySqlAuthenticationMode.SHA256_MEMORY, session.Settings.Auth);
      }

      using (var session = MySQLX.GetSession(ConnectionStringUri + "?auth=SHA256_MEMORY&sslmode=none"))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
        Assert.Equal(MySqlAuthenticationMode.SHA256_MEMORY, session.Settings.Auth);
      }
    }

    #endregion

    #region SSL

    [Fact]
    [Trait("Category", "Security")]
    public void SSLSession()
    {
      using (var s3 = MySQLX.GetSession(ConnectionStringUri))
      {
        Assert.Equal(SessionState.Open, s3.InternalSession.SessionState);
        var result = ExecuteSQLStatement(s3.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';")).FetchAll();
        Assert.StartsWith("TLSv1", result[0][1].ToString());
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SSLCertificate()
    {
      string path = "../../../../MySql.Data.Tests/";
      string connstring = ConnectionStringUri + $"/?ssl-ca={path}client.pfx&ssl-ca-pwd=pass";
      using (var s3 = MySQLX.GetSession(connstring))
      {
        Assert.Equal(SessionState.Open, s3.InternalSession.SessionState);
        var result = ExecuteSQLStatement(s3.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';")).FetchAll();
        Assert.StartsWith("TLSv1", result[0][1].ToString());
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SSLEmptyCertificate()
    {
      string connstring = ConnectionStringUri + $"/?ssl-ca=";
      // if certificate is empty, it connects without a certificate
      using (var s1 = MySQLX.GetSession(connstring))
      {
        Assert.Equal(SessionState.Open, s1.InternalSession.SessionState);
        var result = ExecuteSQLStatement(s1.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';")).FetchAll();
        Assert.StartsWith("TLSv1", result[0][1].ToString());
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SSLCrl()
    {
      string connstring = ConnectionStringUri + "/?ssl-crl=crlcert.pfx";
      Assert.Throws<NotSupportedException>(() => MySQLX.GetSession(connstring));
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SSLOptions()
    {
      string connectionString = ConnectionStringUri;
      // sslmode is valid.
      using (var connection = MySQLX.GetSession(connectionString + "?sslmode=required"))
      {
        Assert.Equal(SessionState.Open, connection.InternalSession.SessionState);
      }

      using (var connection = MySQLX.GetSession(connectionString + "?ssl-mode=required"))
      {
        Assert.Equal(SessionState.Open, connection.InternalSession.SessionState);
      }

      // sslenable is invalid.
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionString + "?sslenable"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionString + "?ssl-enable"));

      // sslmode=Required is default value.
      using (var connection = MySQLX.GetSession(connectionString))
      {
        Assert.Equal(connection.Settings.SslMode, MySqlSslMode.Required);
      }

      // sslmode case insensitive.
      using (var connection = MySQLX.GetSession(connectionString + "?SsL-mOdE=required"))
      {
        Assert.Equal(SessionState.Open, connection.InternalSession.SessionState);
      }
      using (var connection = MySQLX.GetSession(connectionString + "?SsL-mOdE=VeRiFyca&ssl-ca=../../../../MySql.Data.Tests/client.pfx&ssl-ca-pwd=pass"))
      {
        Assert.Equal(SessionState.Open, connection.InternalSession.SessionState);
        var uri = connection.Uri;
      }

      // Duplicate SSL connection options send error message.
      ArgumentException ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionString + "?sslmode=Required&ssl mode=None"));
      Assert.EndsWith("is duplicated.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionString + "?ssl-ca-pwd=pass&ssl-ca-pwd=pass"));
      Assert.EndsWith("is duplicated.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionString + "?certificatepassword=pass&certificatepassword=pass"));
      Assert.EndsWith("is duplicated.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionString + "?certificatepassword=pass&ssl-ca-pwd=pass"));
      Assert.EndsWith("is duplicated.", ex.Message);

      // send error if sslmode=None and another ssl parameter exists.
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionString + "?sslmode=None&ssl-ca=../../../../MySql.Data.Tests/certificates/client.pfx").InternalSession.SessionState);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SSLRequiredByDefault()
    {
      using (var connection = MySQLX.GetSession(ConnectionStringUri))
      {
        Assert.Equal(MySqlSslMode.Required, connection.Settings.SslMode);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SSLPreferredIsInvalid()
    {
      ArgumentException ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionStringUri + "?ssl-mode=Preferred"));
      Assert.Equal("Value 'Preferred' is not of the correct type.", ex.Message);
      ex = Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionStringUri + "?ssl-mode=Prefered"));
      Assert.Equal("Value 'Prefered' is not of the correct type.", ex.Message);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void SSLCertificatePathKeepsCase()
    {
      var certificatePath = "../../../../MySql.Data.Tests/client.pfx";
      // Connection string in basic format.
      string connString = ConnectionString + ";ssl-ca=" + certificatePath + ";ssl-ca-pwd=pass;";
      var stringBuilder = new MySqlXConnectionStringBuilder(connString);
      Assert.Equal(certificatePath, stringBuilder.CertificateFile);
      Assert.Equal(certificatePath, stringBuilder.SslCa);
      Assert.True(stringBuilder.ConnectionString.Contains(certificatePath));
      connString = stringBuilder.ToString();
      Assert.True(connString.Contains(certificatePath));

      // Connection string in uri format.
      string connStringUri = ConnectionStringUri + "/?ssl-ca=" + certificatePath + "& ssl-ca-pwd=pass;";
      using (var session = MySQLX.GetSession(connStringUri))
      {
        Assert.Equal(certificatePath, session.Settings.CertificateFile);
        Assert.Equal(certificatePath, session.Settings.SslCa);
        Assert.True(session.Settings.ConnectionString.Contains(certificatePath));
        connString = session.Settings.ToString();
        Assert.True(connString.Contains(certificatePath));
      }
    }

    // Fix Bug 24510329 - UNABLE TO CONNECT USING TLS/SSL OPTIONS FOR THE MYSQLX URI SCHEME
    [Trait("Category", "Security")]
    [Theory]
    [InlineData("../../../../MySql.Data.Tests/client.pfx")]
    [InlineData("(../../../../MySql.Data.Tests/client.pfx)")]
    [InlineData(@"(..\..\..\..\MySql.Data.Tests\client.pfx")]
    [InlineData("..\\..\\..\\..\\MySql.Data.Tests\\client.pfx")]
    public void SSLCertificatePathVariations(string certificatePath)
    {
      string connStringUri = ConnectionStringUri + "/?ssl-ca=" + certificatePath + "& ssl-ca-pwd=pass;";

      using (var session = MySQLX.GetSession(connStringUri))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void GetUriWithSSLParameters()
    {
      var session = GetSession();

      var builder = new MySqlXConnectionStringBuilder();
      builder.Server = session.Settings.Server;
      builder.UserID = session.Settings.UserID; ;
      builder.Password = session.Settings.Password;
      builder.Port = session.Settings.Port;
      builder.ConnectionProtocol = MySqlConnectionProtocol.Tcp;
      builder.Database = session.Settings.Database;
      builder.CharacterSet = session.Settings.CharacterSet;
      builder.SslMode = MySqlSslMode.Required;
      builder.SslCa = "../../../../MySql.Data.Tests/client.pfx";
      builder.CertificatePassword = "pass";
      builder.ConnectTimeout = 10000;
      builder.Keepalive = 10;
      builder.Auth = MySqlAuthenticationMode.AUTO;

      var connectionString = builder.ConnectionString;
      string uri = null;

      // Create session with connection string.
      using (var internalSession = MySQLX.GetSession(connectionString))
      {
        uri = internalSession.Uri;
      }

      // Create session with the uri version of the connection string.
      using (var internalSession = MySQLX.GetSession(uri))
      {
        // Compare values of the connection options.
        foreach (string connectionOption in builder.Keys)
        {
          // SslCrl connection option is skipped since it isn't currently supported.
          if (connectionOption == "sslcrl")
            continue;

          // Authentication mode AUTO/DEFAULT is internally assigned, hence it is expected to be different in this scenario. 
          if (connectionOption == "auth")
            Assert.Equal(MySqlAuthenticationMode.PLAIN, internalSession.Settings[connectionOption]);
          else
            Assert.Equal(builder[connectionOption], internalSession.Settings[connectionOption]);
        }
      }
    }

    [Fact]
    [Trait("Category", "Security")]
    public void GetUriKeepsSSLMode()
    {
      var globalSession = GetSession();
      var builder = new MySqlXConnectionStringBuilder();
      builder.Server = globalSession.Settings.Server;
      builder.UserID = globalSession.Settings.UserID;
      builder.Password = globalSession.Settings.Password;
      builder.Port = globalSession.Settings.Port;
      builder.Database = "test";
      builder.CharacterSet = globalSession.Settings.CharacterSet;
      builder.SslMode = MySqlSslMode.VerifyCA;
      // Setting SslCa will also set CertificateFile.
      builder.SslCa = "../../../../MySql.Data.Tests/client.pfx";
      builder.CertificatePassword = "pass";
      builder.ConnectTimeout = 10000;
      builder.Keepalive = 10;
      // Auth will change to the authentication mode internally used PLAIN, MySQL41, SHA256_MEMORY: 
      builder.Auth = MySqlAuthenticationMode.AUTO;
      // Doesn't show in the session.URI because Tcp is the default value. Tcp, Socket and Sockets are treated the same.
      builder.ConnectionProtocol = MySqlConnectionProtocol.Tcp;

      string uri = null;
      using (var internalSession = MySQLX.GetSession(builder.ConnectionString))
      {
        uri = internalSession.Uri;
      }

      using (var internalSession = MySQLX.GetSession(uri))
      {
        Assert.Equal(builder.Server, internalSession.Settings.Server);
        Assert.Equal(builder.UserID, internalSession.Settings.UserID);
        Assert.Equal(builder.Password, internalSession.Settings.Password);
        Assert.Equal(builder.Port, internalSession.Settings.Port);
        Assert.Equal(builder.Database, internalSession.Settings.Database);
        Assert.Equal(builder.CharacterSet, internalSession.Settings.CharacterSet);
        Assert.Equal(builder.SslMode, internalSession.Settings.SslMode);
        Assert.Equal(builder.SslCa, internalSession.Settings.SslCa);
        Assert.Equal(builder.CertificatePassword, internalSession.Settings.CertificatePassword);
        Assert.Equal(builder.ConnectTimeout, internalSession.Settings.ConnectTimeout);
        Assert.Equal(builder.Keepalive, internalSession.Settings.Keepalive);
        Assert.Equal(MySqlAuthenticationMode.PLAIN, internalSession.Settings.Auth);
      }
    }

    [Theory]
    [InlineData("[]", null)]
    [InlineData("Tlsv1.0", "TLSv1")]
    [InlineData("Tlsv1.0, Tlsv1.1", "TLSv1.1")]
    [InlineData("Tlsv1.0, Tlsv1.1, Tlsv1.2", "TLSv1.2")]
//#if NET48 || NETCOREAPP3_0
   // [InlineData("Tlsv1.3", "Tlsv1.3", Skip = "Waiting for full support")]
    //[InlineData("Tlsv1.0, Tlsv1.1, Tlsv1.2, Tlsv1.3", "Tlsv1.3", Skip = "Waiting for full support")]
//#else
    [InlineData("Tlsv1.3", "")]
    [InlineData("Tlsv1.0, Tlsv1.1, Tlsv1.2, Tlsv1.3", "Tlsv1.2")]
//#endif
    public void TlsVersionTest(string tlsVersion, string result)
    {
      var globalSession = GetSession();
      var builder = new MySqlXConnectionStringBuilder();
      builder.Server = globalSession.Settings.Server;
      builder.UserID = globalSession.Settings.UserID;
      builder.Password = globalSession.Settings.Password;
      builder.Port = globalSession.Settings.Port;
      builder.Database = "test";
      void SetTlsVersion() { builder.TlsVersion = tlsVersion; }
      if (result == null)
      {
        Assert.ThrowsAny<Exception>(SetTlsVersion);
        return;
      }

      SetTlsVersion();

      string uri = null;
      if (!String.IsNullOrWhiteSpace(result))
      {
        using (var internalSession = MySQLX.GetSession(builder.ConnectionString))
        {
          uri = internalSession.Uri;
          Assert.Equal(SessionState.Open, internalSession.InternalSession.SessionState);
          Assert.Equal(result, internalSession.SQL("SHOW SESSION STATUS LIKE 'mysqlx_ssl_version'").Execute().FetchAll()[0][1].ToString(), true);
        }
        using (var internalSession = MySQLX.GetSession(uri))
        {
          Assert.Equal(SessionState.Open, internalSession.InternalSession.SessionState);
          Assert.Equal(result, internalSession.SQL("SHOW SESSION STATUS LIKE 'mysqlx_ssl_version'").Execute().FetchAll()[0][1].ToString(), true);
        }
      }
      else
        Assert.Throws<NotSupportedException>(() => MySQLX.GetSession(builder.ConnectionString));
    }
    #endregion
  }
}
