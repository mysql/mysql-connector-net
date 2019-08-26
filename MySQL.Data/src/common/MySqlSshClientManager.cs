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
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySql.Data.Common
{
  /// <summary>
  /// Static class to manage SSH connections created with SSH .NET.
  /// </summary>
  internal static class MySqlSshClientManager
  {
    #region Properties

    /// <summary>
    /// Gets or sets the SSH client initialized when calling the <seealso cref="SetupSshClient"/> method.
    /// </summary>
    public static SshClient CurrentSshClient { get; private set; }

    #endregion

    #region Fields

    /// <summary>
    /// Stores a list of SSH clients having different connection settings.
    /// </summary>
    private static List<SshClient> _sshClientList;

    #endregion

    static MySqlSshClientManager()
    {
      CurrentSshClient = null;
      _sshClientList = new List<SshClient>();
    }

    /// <summary>
    /// Initializes an <see cref="SshClient"/> instance if no SSH client with similar connection options has already been initialized.
    /// </summary>
    /// <param name="sshHostName">The SSH host name.</param>
    /// <param name="sshUserName">The SSH user name.</param>
    /// <param name="sshPassword">The SSH password.</param>
    /// <param name="sshKeyFile">The SSH key file.</param>
    /// <param name="sshPassphrase">The SSH pass phrase.</param>
    /// <param name="sshPort">The SSH port.</param>
    /// <param name="server">The local host name.</param>
    /// <param name="port">The local port number.</param>
    /// <param name="isXProtocol">Flag to indicate if the connection will be created for the classic or X Protocol.</param>
    /// <returns></returns>
    internal static SshClient SetupSshClient(
      string sshHostName,
      string sshUserName,
      string sshPassword,
      string sshKeyFile,
      string sshPassphrase,
      uint sshPort,
      string server,
      uint port,
      bool isXProtocol)
    {
      if (string.IsNullOrEmpty(sshHostName))
        throw new ArgumentException(string.Format(Resources.ParameterCannotBeNullOrEmpty, nameof(sshHostName)));

      if (string.IsNullOrEmpty(sshUserName))
        throw new ArgumentException(string.Format(Resources.ParameterCannotBeNullOrEmpty, nameof(sshUserName)));

      if (string.IsNullOrEmpty(sshKeyFile) && string.IsNullOrEmpty(sshPassword))
        throw new ArgumentException(Resources.SshAuthenticationModeNotSet);

      SshClient sshClient = null;
      var authenticationMethods = new List<AuthenticationMethod>();
      if (!string.IsNullOrEmpty(sshKeyFile))
      {
        try
        {
          var keyFile = string.IsNullOrEmpty(sshPassphrase)
          ? new PrivateKeyFile(sshKeyFile)
          : new PrivateKeyFile(sshKeyFile, sshPassphrase);
          authenticationMethods.Add(new PrivateKeyAuthenticationMethod(sshUserName, keyFile));
        }
        catch (InvalidOperationException)
        {
          throw new ArgumentException(Resources.SshInvalidPassphrase);
        }
      }

      if (!string.IsNullOrEmpty(sshPassword))
        authenticationMethods.Add(new PasswordAuthenticationMethod(sshUserName, sshPassword));

      ConnectionInfo connectionInfo = new ConnectionInfo(
        sshHostName,
        (int)sshPort,
        sshUserName,
        authenticationMethods.ToArray());
      sshClient = new SshClient(connectionInfo);
      var forwardedPort = new ForwardedPortLocal("127.0.0.1", (uint)(isXProtocol ? port : 3306), server, port);
      foreach (var client in _sshClientList)
      {
        if (sshClient.ConnectionInfo.Username == client.ConnectionInfo.Username
            && sshClient.ConnectionInfo.Host == client.ConnectionInfo.Host
            && sshClient.ConnectionInfo.Port == client.ConnectionInfo.Port
            && sshClient.ConnectionInfo.AuthenticationMethods[0].Username == client.ConnectionInfo.AuthenticationMethods[0].Username
            && sshClient.ConnectionInfo.AuthenticationMethods[0].Name == client.ConnectionInfo.AuthenticationMethods[0].Name)
        {
          var oldForwardedPort = client.ForwardedPorts.Count() > 0 ? (ForwardedPortLocal)client.ForwardedPorts.First() : null;
          if (oldForwardedPort != null
              && forwardedPort.Host == oldForwardedPort.Host
              && forwardedPort.Port == oldForwardedPort.Port
              && forwardedPort.BoundHost == oldForwardedPort.BoundHost
              && forwardedPort.BoundPort == oldForwardedPort.BoundPort)
          {
            if (!client.IsConnected) sshClient.Connect();
            if (!oldForwardedPort.IsStarted) oldForwardedPort.Start();
            return client;
          }
        }
      }

      sshClient.Connect();
      sshClient.AddForwardedPort(forwardedPort);
      forwardedPort.Start();
      _sshClientList.Add(sshClient);
      CurrentSshClient = sshClient;

      return sshClient;
    }
  }
}
