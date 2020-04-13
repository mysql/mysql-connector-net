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

using System.Linq;

namespace MySql.Data.Common
{
  /// <summary>
  /// Wrapper class used for handling SSH connections.
  /// </summary>
  internal class Ssh
  {
    #region Fields

    private bool _isXProtocol;
    private uint _port;
    private string _server;
    private string _sshHostName;
    private string _sshKeyFile;
    private string _sshPassphrase;
    private string _sshPassword;
    private uint _sshPort;
    private string _sshUserName;

    #endregion

    internal Ssh(
      string sshHostName,
      string sshUserName,
      string sshPassword,
      string sshKeyFile,
      string sshPassphrase,
      uint sshPort,
      string server,
      uint port,
      bool isXProtocol
    )
    {
      _sshHostName = sshHostName;
      _sshUserName = sshUserName;
      _sshPassword = sshPassword;
      _sshKeyFile = sshKeyFile;
      _sshPassphrase = sshPassphrase;
      _sshPort = sshPort;
      _server = server;
      _port = port;
      _isXProtocol = isXProtocol;
    }

    /// <summary>
    /// Starts the SSH client.
    /// </summary>
    internal void StartClient()
    {
      MySqlSshClientManager.SetupSshClient(
        _sshHostName,
        _sshUserName,
        _sshPassword,
        _sshKeyFile,
        _sshPassphrase,
        _sshPort,
        _server,
        _port,
        _isXProtocol
        );
    }

    /// <summary>
    /// Stops the SSH client.
    /// </summary>
    internal void StopClient()
    {
      var sshClient = MySqlSshClientManager.CurrentSshClient;
      if (sshClient != null && sshClient.IsConnected)
      {
        sshClient.ForwardedPorts.First().Stop();
        sshClient.Disconnect();
      }
    }
  }
}
