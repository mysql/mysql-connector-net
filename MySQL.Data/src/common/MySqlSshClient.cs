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
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Text;

namespace MySql.Data.common
{
  internal class MySqlSshClient
  {
    internal SshClient SshClient { get; private set; }

    public MySqlSshClient(MySqlConnectionStringBuilder settings)
    {
      SetupSshClient(
        settings.SshAuthenticationMode,
        settings.SshHostName,
        settings.SshUserName,
        settings.SshPassword,
        settings.SshKeyFile,
        settings.SshPassphrase,
        settings.SshPort,
        settings.Server,
        settings.Port,
        false
        );
    }

    public MySqlSshClient(MySqlXConnectionStringBuilder settings)
    {
      SetupSshClient(
        settings.SshAuthenticationMode,
        settings.SshHostName,
        settings.SshUserName,
        settings.SshPassword,
        settings.SshKeyFile,
        settings.SshPassphrase,
        settings.SshPort,
        settings.Server,
        settings.Port,
        true
        );
    }

    private void SetupSshClient(
      SshAuthenticationMode sshAuthenticationMode,
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

      if (sshAuthenticationMode == SshAuthenticationMode.Password)
        SshClient = new SshClient(sshHostName, (int)sshPort, sshUserName, sshPassword);
      else
      {
        if (string.IsNullOrEmpty(sshKeyFile))
          throw new ArgumentException(string.Format(Resources.ParameterCannotBeNullOrEmpty, nameof(sshKeyFile)));

        var keyFile = string.IsNullOrEmpty(sshPassphrase)
          ? new PrivateKeyFile(sshKeyFile)
          : new PrivateKeyFile(sshKeyFile, sshPassphrase);
        var authenticationMethod = new PrivateKeyAuthenticationMethod(sshUserName, keyFile);
        ConnectionInfo connectionInfo = new ConnectionInfo(sshHostName, (int)sshPort, sshUserName, authenticationMethod);
        SshClient = new SshClient(connectionInfo);
      }

      SshClient.Connect();
      var forwardedPort = new ForwardedPortLocal("127.0.0.1", (uint)(isXProtocol ? port : 3306), server, port);
      SshClient.AddForwardedPort(forwardedPort);
      forwardedPort.Start();
    }
  }
}
