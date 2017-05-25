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

using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using Xunit;

namespace MySqlX.Data.Tests 
{
  public class SessionTests : BaseTest
  {
    [Fact]
    public void CanCloseSession()
    {
      XSession s = MySqlX.XDevAPI.MySQLX.GetSession(ConnectionString);
      Assert.True(s.InternalSession.SessionState == SessionState.Open);
      s.Close();
      Assert.Equal(s.InternalSession.SessionState, SessionState.Closed);
    }

    [Fact]
    public void NoPassword()
    {
      XSession session = MySqlX.XDevAPI.MySQLX.GetSession(ConnectionStringNoPassword);
      Assert.True(session.InternalSession.SessionState == SessionState.Open);
      session.Close();
      Assert.Equal(session.InternalSession.SessionState, SessionState.Closed);
    }

    [Fact]
    public void NodeSessionClose()
    {
      NodeSession session = MySQLX.GetNodeSession(ConnectionString);
      Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      session.Close();
      Assert.Equal(SessionState.Closed, session.InternalSession.SessionState);
    }

    [Fact]
    public void CountClosedSession()
    {
      NodeSession nodeSession = MySQLX.GetNodeSession(ConnectionString);
      int sessions = nodeSession.SQL("show processlist").Execute().FetchAll().Count;

      for (int i = 0; i < 20; i++)
      {
        XSession session = MySQLX.GetSession(ConnectionString);
        Assert.True(session.InternalSession.SessionState == SessionState.Open);
        session.Close();
        Assert.Equal(session.InternalSession.SessionState, SessionState.Closed);
      }

      int newSessions = nodeSession.SQL("show processlist").Execute().FetchAll().Count;
      nodeSession.Close();
      Assert.Equal(sessions, newSessions);
    }

    [Fact]
    public void ConnectionStringAsAnonymousType()
    {
      var connstring = new
      {
        server = session.Settings.Server,
        port = session.Settings.Port,
        user = session.Settings.UserID,
        password = session.Settings.Password
      };
      using (var testSession = MySQLX.GetNodeSession(connstring))
      {
        Assert.Equal(SessionState.Open, testSession.InternalSession.SessionState);
      }
    }

    [Fact]
    public void NodeSession_Get_Set_CurrentSchema()
    {
      using (NodeSession testSession = MySQLX.GetNodeSession(ConnectionString))
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
    public void NodeSessionUsingSchema()
    {
      using (NodeSession mySession = MySQLX.GetNodeSession(ConnectionString + $";database={schemaName};"))
      {
        Assert.Equal(SessionState.Open, mySession.InternalSession.SessionState);
        Assert.Equal(schemaName, mySession.Schema.Name);
        Assert.Equal(schemaName, mySession.GetCurrentSchema().Name);
        Assert.True(mySession.Schema.ExistsInDatabase());
      }
    }

    [Fact]
    public void XSessionUsingSchema()
    {
      using (XSession mySession = MySQLX.GetSession(ConnectionString + $";database={schemaName};"))
      {
        Assert.Equal(SessionState.Open, mySession.InternalSession.SessionState);
        Assert.Equal(schemaName, mySession.Schema.Name);
        Assert.True(mySession.Schema.ExistsInDatabase());
      }
    }

    protected void CheckConnectionStringAsUri(string connectionstring, string user, string password, string server, uint port, params string[] parameters)
    {
      string result = this.session.ParseConnectionStringFromUri(connectionstring);
      MySql.Data.MySqlClient.MySqlConnectionStringBuilder csbuilder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(result);
      Assert.True(user == csbuilder.UserID, string.Format("Expected:{0} Current:{1} in {2}", user, csbuilder.UserID, connectionstring));
      Assert.True(password == csbuilder.Password, string.Format("Expected:{0} Current:{1} in {2}", password, csbuilder.Password, connectionstring));
      Assert.True(server == csbuilder.Server, string.Format("Expected:{0} Current:{1} in {2}", server, csbuilder.Server, connectionstring));
      Assert.True(port == csbuilder.Port, string.Format("Expected:{0} Current:{1} in {2}", port, csbuilder.Port, connectionstring));
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

    [Fact]
    public void ConnectionStringAsUri()
    {
      CheckConnectionStringAsUri("mysqlx://myuser:password@localhost:33060", "myuser", "password", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://my%3Auser:p%40ssword@localhost:33060", "my:user", "p@ssword", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://my%20user:p%40ss%20word@localhost:33060", "my user", "p@ss word", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx:// myuser : p%40ssword@localhost:33060", "myuser", "p@ssword", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://myuser@localhost:33060", "myuser", "", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://myuser:p%40ssword@localhost", "myuser", "p@ssword", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://myuser:p%40ssw%40rd@localhost", "myuser", "p@ssw@rd", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://my%40user:p%40ssword@localhost", "my@user", "p@ssword", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://myuser@localhost", "myuser", "", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://myuser@127.0.0.1", "myuser", "", "127.0.0.1", 33060);
      CheckConnectionStringAsUri("mysqlx://myuser@[::1]", "myuser", "", "[::1]", 33060);
      CheckConnectionStringAsUri("mysqlx://myuser@localhost/test", "myuser", "", "localhost", 33060, "database", "test");
      CheckConnectionStringAsUri("mysqlx://myuser@localhost/test?ssl%20mode=none&pooling=false", "myuser", "", "localhost", 33060, "database", "test", "ssl mode", "None", "pooling", "False");
      CheckConnectionStringAsUri("mysqlx+ssh://myuser:password@localhost:33060", "myuser", "password", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://_%21%22%23%24s%26%2F%3D-%25r@localhost", "_!\"#$s&/=-%r", "", "localhost", 33060);
      Assert.Throws<ArgumentException>(() => CheckConnectionStringAsUri("mysql://myuser@localhost", "myuser", "", "localhost", 33060));
      Assert.Throws<ArgumentException>(() => CheckConnectionStringAsUri("myuser@localhost", "myuser", "", "localhost", 33060));
      Assert.Throws<UriFormatException>(() => CheckConnectionStringAsUri("mysqlx://uid=myuser;server=localhost", "myuser", "", "localhost", 33060));
      CheckConnectionStringAsUri("mysqlx://user:password@server.example.com/", "user", "password", "server.example.com", 33060, "ssl mode", "Required");
      CheckConnectionStringAsUri("mysqlx://user:password@server.example.com/?ssl-ca=(c:\\client.pfx)", "user", "password", "server.example.com", 33060, "ssl mode", "Required", "ssl-ca", "(c:%5Cclient.pfx)");
      Assert.Throws<NotSupportedException>(() => CheckConnectionStringAsUri("mysqlx://user:password@server.example.com/?ssl-crl=(c:\\crl.pfx)", "user", "password", "server.example.com", 33060, "ssl mode", "Required", "ssl-crl", "(c:%5Ccrl.pfx)"));
    }

    [Fact]
    public void ConnectionUsingUri()
    {
      using (var session = MySQLX.GetNodeSession(ConnectionStringUri))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Fact]
    public void ConnectionStringNull()
    {
      Assert.Throws<ArgumentNullException>(() => MySQLX.GetSession(null));
      Assert.Throws<ArgumentNullException>(() => MySQLX.GetNodeSession(null));
    }

    [Fact]
    public void SslSession()
    {
      using (var s3 = MySQLX.GetNodeSession(ConnectionStringUri))
      {
        Assert.Equal(SessionState.Open, s3.InternalSession.SessionState);
        var result = s3.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.StartsWith("TLSv1", result[0][1].ToString());
      }
    }

    [Fact]
    public void SslCertificate()
    {
      string path = "../../../../MySql.Data.Tests/";
      string connstring = ConnectionStringUri + $"/?ssl-ca={path}client.pfx&ssl-ca-pwd=pass";
      using (var s3 = MySQLX.GetNodeSession(connstring))
      {
        Assert.Equal(SessionState.Open, s3.InternalSession.SessionState);
        var result = s3.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.StartsWith("TLSv1", result[0][1].ToString());
      }
    }

    [Fact]
    public void SslEmptyCertificate()
    {
      string connstring = ConnectionStringUri + $"/?ssl-ca=";
      // if certificate is empty, it connects without a certificate
      using (var s1 = MySQLX.GetNodeSession(connstring))
      {
        Assert.Equal(SessionState.Open, s1.InternalSession.SessionState);
        var result = s1.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.StartsWith("TLSv1", result[0][1].ToString());
      }
    }

    [Fact]
    public void SslCrl()
    {
      string connstring = ConnectionStringUri + "/?ssl-crl=crlcert.pfx";
      Assert.Throws<NotSupportedException>(() => MySQLX.GetNodeSession(connstring));
    }

    [Fact]
    public void SSlOptions()
    {
      string connectionString = ConnectionStringUri;
      // sslmode is valid.
      using(var connection = MySQLX.GetNodeSession(connectionString + "?sslmode=none"))
      {
        Assert.Equal(SessionState.Open, connection.InternalSession.SessionState);
      }
      using(var connection = MySQLX.GetNodeSession(connectionString + "?ssl-mode=none"))
      {
        Assert.Equal(SessionState.Open, connection.InternalSession.SessionState);
      }

      // sslenable is invalid.
      Assert.Throws<ArgumentException>(() => MySQLX.GetNodeSession(connectionString + "?sslenable"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetNodeSession(connectionString + "?ssl-enable"));

      // sslmode=Required is default value.
      using(var connection = MySQLX.GetNodeSession(connectionString))
      {
        Assert.Equal(connection.Settings.SslMode, MySqlSslMode.Required);
      }

      // sslmode=Preferred is invalid.
      Assert.Throws<ArgumentException>(() => MySQLX.GetNodeSession(connectionString + "?ssl-mode=Preferred"));

      // sslmode=Disabled is defaulted to None.
      using(var connection = MySQLX.GetNodeSession(connectionString + "?ssl-mode=Disabled"))
      {
        Assert.Equal(MySqlSslMode.None, connection.Settings.SslMode);
      }

      // sslmode=Required is default value.
      using(var connection = MySQLX.GetNodeSession(connectionString))
      {
        Assert.Equal(MySqlSslMode.Required, connection.Settings.SslMode);
      }

      // sslmode case insensitive.
      using(var connection = MySQLX.GetNodeSession(connectionString + "?SsL-mOdE=none"))
      {
        Assert.Equal(SessionState.Open, connection.InternalSession.SessionState);
      }
      using(var connection = MySQLX.GetNodeSession(connectionString + "?SsL-mOdE=VeRiFyca&ssl-ca=../../../../MySql.Data.Tests/client.pfx&ssl-ca-pwd=pass"))
      {
        Assert.Equal(SessionState.Open, connection.InternalSession.SessionState);
      }

      // Duplicate SSL connection options send error message.
      ArgumentException ex = Assert.Throws<ArgumentException>(() => MySQLX.GetNodeSession(connectionString + "?sslmode=Required&ssl mode=None"));
      Assert.EndsWith("is duplicated.", ex.Message);

      // send error if sslmode=None and another ssl parameter exists.
      Assert.Throws<ArgumentException>(() => MySQLX.GetNodeSession(connectionString + "?sslmode=None&ssl-ca=../../../../MySql.Data.Tests/certificates/client.pfx").InternalSession.SessionState);
    }

        [Fact]
    public void IPv6()
    {
      MySqlConnectionStringBuilder csBuilder = new MySqlConnectionStringBuilder(ConnectionString);
      csBuilder.Server = "::1";
      csBuilder.Port = uint.Parse(XPort);

      using (var session = MySQLX.GetSession(csBuilder.ToString()))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Fact]
    public void IPv6AsUrl()
    {
      MySqlConnectionStringBuilder csBuilder = new MySqlConnectionStringBuilder(ConnectionString);
      string connString = $"mysqlx://{csBuilder.UserID}:{csBuilder.Password}@[::1]:{XPort}";
      using (XSession session = MySQLX.GetSession(connString))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }
    }

    [Fact]
    public void IPv6AsAnonymous()
    {
      MySqlConnectionStringBuilder csBuilder = new MySqlConnectionStringBuilder(ConnectionString);
      using (XSession session = MySQLX.GetSession(new { server = "::1", user = csBuilder.UserID, password = csBuilder.Password, port = XPort }))
      {
        Assert.Equal(SessionState.Open, session.InternalSession.SessionState);
      }
    }
  }
}
