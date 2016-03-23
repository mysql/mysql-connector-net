// Copyright © 2015, 2016 Oracle and/or its affiliates. All rights reserved.
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
      using(NodeSession testSession = MySQLX.GetNodeSession(ConnectionString))
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

    protected void CheckConnectionStringAsUri(string connectionstring, string user, string password, string server, uint port)
    {
      string result = this.session.ParseConnectionStringFromUri(connectionstring);
      MySql.Data.MySqlClient.MySqlConnectionStringBuilder csbuilder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(result);
      Assert.True(user == csbuilder.UserID, string.Format("Expected:{0} Current:{1} in {2}", user, csbuilder.UserID, connectionstring));
      Assert.True(password == csbuilder.Password, string.Format("Expected:{0} Current:{1} in {2}", password, csbuilder.Password, connectionstring));
      Assert.True(server == csbuilder.Server, string.Format("Expected:{0} Current:{1} in {2}", server, csbuilder.Server, connectionstring));
      Assert.True(port == csbuilder.Port, string.Format("Expected:{0} Current:{1} in {2}", port, csbuilder.Port, connectionstring));
    }

    [Fact]
    public void ConnectionStringAsUri()
    {
      CheckConnectionStringAsUri("mysqlx://myuser:p@ssword@localhost:33060", "myuser", "p@ssword", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://myuser:p@ss word@localhost:33060", "myuser", "p@ss word", "localhost", 33060);
      CheckConnectionStringAsUri("//myuser:p@ssword@localhost:33060", "myuser", "p@ssword", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx:// myuser : p@ssword@ localhost : 33060 ", "myuser", "p@ssword", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://myuser@localhost:33060", "myuser", "", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://myuser:p@ssword@localhost", "myuser", "p@ssword", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://myuser:p@ssw@rd@localhost", "myuser", "p@ssw@rd", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://my@user:p@ssword@localhost", "my@user", "p@ssword", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://myuser@localhost", "myuser", "", "localhost", 33060);
      CheckConnectionStringAsUri("mysqlx://myuser@127.0.0.1", "myuser", "", "127.0.0.1", 33060);
      CheckConnectionStringAsUri("mysqlx://_!\"#$s&/=-%r@localhost", "_!\"#$s&/=-%r", "", "localhost", 33060);
      Assert.Throws<ArgumentException>(() => CheckConnectionStringAsUri("mysql://myuser@localhost", "myuser", "", "localhost", 33060));
      Assert.Throws<ArgumentException>(() => CheckConnectionStringAsUri("myuser@localhost", "myuser", "", "localhost", 33060));
      Assert.Throws<ArgumentException>(() => CheckConnectionStringAsUri("mysqlx://uid=myuser;server=localhost", "myuser", "", "localhost", 33060));
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
  }
}
