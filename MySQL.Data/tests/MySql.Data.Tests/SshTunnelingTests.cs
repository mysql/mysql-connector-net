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

using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.IO;
using Xunit;

namespace MySql.Data.MySqlClient.Tests
{
  public class SshTunnelingTests
  {
    #region Fields

    private const string SSH_HOST_NAME = "placeholder";
    private const string SSH_USER_NAME = "placeholder";
    private const string SSH_PASSWORD = "placeholder";
    private const int SSH_PORT = 22;
    private const string SSH_KEY_FILE_PATH = "placeholder";
    private const string SSH_KEY_FILE_PASSPHRASE = "placeholder";

    private const int MYSQL_SERVER_PORT = 3307;
    private const string MYSQL_HOST_NAME = "localhost";
    private const string MYSQL_ROOT_USER = "root";

    private const string REMOTE_HOST_NAME = "placeholder";
    private const string REMOTE_USER_NAME = "placeholder";
    private const string REMOTE_USER_PASSWORD = "placeholder";

    #endregion

    /// <summary>
    /// Validate that the expected errors are being raised when invalid values are provided.
    /// </summary>
    [Fact(Skip = "Needs SSH setup on PB2")]
    public void ErrorsRaisedByMissingParameters()
    {
      var expectedErrorMessage = "Parameter '{0}' can't be null or empty.";
      var builder = new MySqlConnectionStringBuilder();
      builder.UserID = MYSQL_ROOT_USER;
      builder.Server = MYSQL_HOST_NAME;
      builder.Port = MYSQL_SERVER_PORT;
      builder.SshUserName = SSH_USER_NAME;
      builder.SshPassword = SSH_PASSWORD;

      var exception = Assert.Throws<ArgumentException>(() => ValidateConnection(builder.ConnectionString));
      Assert.Equal(string.Format(expectedErrorMessage, "sshHostName"), exception.Message);

      builder.SshHostName = SSH_HOST_NAME;
      builder.SshPassword = SSH_PASSWORD;
      builder.SshKeyFile = null;
      ValidateConnection(builder.ConnectionString);

      builder.SshPassword = null;
      builder.SshKeyFile = "invalidFile.ppk";
      var fileException = Assert.Throws<FileNotFoundException>(() => ValidateConnection(builder.ConnectionString));
      Assert.StartsWith("Could not find file", fileException.Message);

      builder.SshKeyFile = SSH_KEY_FILE_PATH;
      var sshException2 = Assert.Throws<SshPassPhraseNullOrEmptyException>(() => ValidateConnection(builder.ConnectionString));
      Assert.Equal("Private key is encrypted but passphrase is empty.", sshException2.Message);

      builder.SshPassphrase = SSH_KEY_FILE_PASSPHRASE;
      ValidateConnection(builder.ConnectionString);
    }

    /// <summary>
    /// Access MySQL server as a local user using keys.
    /// </summary>
    /// <remarks>MySQL Server and the SSH server are located in the same machine. The root user
    /// is configured to only allow local connections. Via SSH Tunneling the client can connect with
    /// root user as if it were a local connection.</remarks>
    [Fact(Skip = "Needs SSH setup on PB2")]
    public void ConnectAsLocalUserUsingKeys()
    {
      var builder = new MySqlConnectionStringBuilder();
      builder.UserID = MYSQL_ROOT_USER;
      builder.Server = MYSQL_HOST_NAME;
      builder.Port = MYSQL_SERVER_PORT;
      builder.SshHostName = SSH_HOST_NAME;
      builder.SshUserName = SSH_USER_NAME;
      builder.SshKeyFile = SSH_KEY_FILE_PATH;
      builder.SshPassphrase = SSH_KEY_FILE_PASSPHRASE;
      builder.SshPort = SSH_PORT;

      ValidateConnection(builder.ConnectionString);
    }

    /// <summary>
    /// Access MySQL server as a local user.
    /// </summary>
    /// <remarks>MySQL Server and the SSH server are located in the same machine. The root user
    /// is configured to only allow local connections. Via SSH Tunneling the client can connect with
    /// root user as if it were a local connection.</remarks>
    [Fact(Skip = "Needs SSH setup on PB2")]
    public void ConnectAsLocalUser()
    {
      var builder = new MySqlConnectionStringBuilder();
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

    /// <summary>
    /// Access MySQL Server as a remote user.
    /// </summary>
    /// <remarks>MySQL Server and the SSH server are located on different machines. Requests made to
    /// the SSH server are forwarded to the machine where the MySQL server instance is running.</remarks>
    [Fact(Skip = "Needs SSH setup on PB2")]
    public void ConnectAsRemoteUser()
    {
      CreateRemoteUser();

      var builder = new MySqlConnectionStringBuilder();
      builder.UserID = REMOTE_USER_NAME;
      builder.Password = REMOTE_USER_PASSWORD;
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

    /// <summary>
    /// Performs a fallback to authenticate with password whenever the Ssh connection fails with an error other than invalid passphrase.
    /// </summary>
    [Fact(Skip = "Needs SSH setup on PB2")]
    public void ConnectionFallback()
    {
      var builder = new MySqlConnectionStringBuilder();
      builder.UserID = MYSQL_ROOT_USER;
      builder.Server = MYSQL_HOST_NAME;
      builder.Port = MYSQL_SERVER_PORT;
      builder.SshHostName = SSH_HOST_NAME;
      builder.SshUserName = SSH_USER_NAME;
      //builder.SshKeyFile = SSH_KEY_FILE_PATH;
      builder.SshPassphrase = SSH_KEY_FILE_PASSPHRASE;
      builder.SshPassword = SSH_PASSWORD;
      builder.SshPort = SSH_PORT;
      ValidateConnection(builder.ConnectionString);
    }

    /// <summary>
    /// Validates that Connector/NET without SSH tunneling is not able to connect to a MySQL Server
    /// instance running on a different machine using a local user.
    /// </summary>
    [Fact(Skip = "Needs SSH setup on PB2")]
    public void ConnectionFailsWithLocalUserOnRemoteMachineWithoutSSHTunneling()
    {
      CreateRemoteUser();

      // Fails because the root user is only accesible in the localhost.
      var builder = new MySqlConnectionStringBuilder();
      builder.UserID = MYSQL_ROOT_USER;
      builder.Server = SSH_HOST_NAME;
      builder.Port = MYSQL_SERVER_PORT;
      var connection = new MySqlConnection(builder.ConnectionString);
      Assert.Throws<MySqlException>(() => connection.Open());

      // Works because the remote user does allow outside connections.
      builder.UserID = REMOTE_USER_NAME;
      builder.Password = REMOTE_USER_PASSWORD;
      builder.Server = SSH_HOST_NAME;
      builder.Port = MYSQL_SERVER_PORT;
      ValidateConnection(builder.ConnectionString);
    }

    /// <summary>
    /// Validates that the SSH Server can be accessed via a user and password.
    /// </summary>
    [Fact(Skip = "Needs SSH setup on PB2")]
    public void ConnectToSSHServerUsingPassword()
    {
      using (SftpClient sftp = new SftpClient(SSH_HOST_NAME, SSH_PORT, SSH_USER_NAME, SSH_PASSWORD))
      {
        sftp.Connect();
        var files = sftp.ListDirectory(string.Empty);
        foreach (var file in files) {}
        sftp.Disconnect();
      }
    }

    /// <summary>
    /// Validates that the SSH Server can be accessed via a user and passphrase.
    /// </summary>
    [Fact(Skip = "Needs SSH setup on PB2")]
    public void ConnectToSSHServerUsingKeys()
    {
      var keyFile = new PrivateKeyFile(SSH_KEY_FILE_PATH, SSH_KEY_FILE_PASSPHRASE);
      var authenticationMethod = new PrivateKeyAuthenticationMethod(SSH_USER_NAME, keyFile);
      ConnectionInfo con = new ConnectionInfo(SSH_HOST_NAME, SSH_PORT, SSH_USER_NAME, authenticationMethod);
      using (SftpClient sftp = new SftpClient(con))
      {
        sftp.Connect();
        var files = sftp.ListDirectory(string.Empty);
        foreach (var file in files) {}
        sftp.Disconnect();
      }
    }

    private void ValidateConnection(string connectionString)
    {
      using (var connection = new MySqlConnection(connectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SELECT user, host, plugin FROM mysql.user", connection);
        var reader = command.ExecuteReader();
        Assert.True(reader.HasRows);
        connection.Close();
      }
    }

    private void CreateRemoteUser()
    {
      var builder = new MySqlConnectionStringBuilder();
      builder.UserID = MYSQL_ROOT_USER;
      builder.Server = MYSQL_HOST_NAME;
      builder.Port = MYSQL_SERVER_PORT;
      builder.SshHostName = SSH_HOST_NAME;
      builder.SshUserName = SSH_USER_NAME;
      builder.SshPassword = SSH_PASSWORD;
      builder.SshPort = SSH_PORT;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand($"CREATE USER IF NOT EXISTS '{REMOTE_USER_NAME}'@'%' IDENTIFIED BY '{REMOTE_USER_PASSWORD}'", connection);
        command.ExecuteNonQuery();
        command.CommandText = $"GRANT ALL ON *.* TO '{REMOTE_USER_NAME}'@'%'";
        command.ExecuteNonQuery();
        connection.Close();
      }
    }
  }
}
