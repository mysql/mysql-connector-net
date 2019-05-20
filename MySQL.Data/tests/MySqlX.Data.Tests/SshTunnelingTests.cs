// Copyright (c) 2019, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using Xunit;

namespace MySqlX.Data.Tests
{
  public class SshTunnelingTests
  {
    #region Fields

    private const string SSH_HOST_NAME = "placeholder";
    private const string SSH_USER_NAME = "placeholder";
    private const string SSH_PASSWORD = "placeholder";
    private const int SSH_PORT = 22;
    private const string KEY_FILE_PATH = "placeholder";
    private const string KEY_FILE_PASSPHRASE = "placeholder";

    private const int MYSQL_SERVER_PORT = 33070;
    private const string MYSQL_HOST_NAME = "localhost";
    private const string MYSQL_ROOT_USER = "root";

    private const string REMOTE_HOST_NAME = "placeholder";

    #endregion

    public SshTunnelingTests()
    {

    }

    /// <summary>
    /// Access MySQL Server as a local user.
    /// </summary>
    /// <remarks>MySQL Server and the SSH server are located in the same machine. The root user
    /// is configured to only allow local connections. Via SSH Tunneling the client can connect with
    /// root user as if it were a local connection.</remarks>
    //[Fact(Skip = "Needs SSH setup on PB2")]
    [Fact]
    public void ConnectAsLocalUser()
    {
      var builder = new MySqlXConnectionStringBuilder();
      builder.UserID = MYSQL_ROOT_USER;
      builder.Server = MYSQL_HOST_NAME;
      builder.Port = MYSQL_SERVER_PORT;
      builder.SshHostName = SSH_HOST_NAME;
      builder.SshUserName = SSH_USER_NAME;
      builder.SshPassword = SSH_PASSWORD;
      builder.SshPort = SSH_PORT;
      ValidateConnection(builder.ConnectionString);

      builder.Server = "127.0.0.1";
      ValidateConnection(builder.ConnectionString);

      builder.Server = "::1";
      ValidateConnection(builder.ConnectionString);
    }

    private void ValidateConnection(string connectionString)
    {
      using (var session = MySQLX.GetSession(connectionString))
      {
        List<Schema> schemas = session.GetSchemas();
        Assert.True(schemas.Count > 0);
        session.Close();
      }
    }
  }
}
