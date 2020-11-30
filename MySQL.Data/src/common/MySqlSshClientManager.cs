// Copyright (c) 2019, 2020, Oracle and/or its affiliates. All rights reserved.
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
using System.Diagnostics;
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
    /// Removes the MySQL unsupported encryptions (SSH Ciphers), MACs and key exchange algorithms.
    /// </summary>
    /// <param name="client">The <see cref="SshClient"/> instance containing the list of supported elements.</param>
    /// <remarks>See https://confluence.oraclecorp.com/confluence/display/GPS/Approved+Security+Technologies%3A+Standards+-+SSH+Ciphers+and+Versions for an updated list.</remarks>
    internal static void RemoveUnsupportedAlgorithms(SshClient client)
    {
      if (client == null
          || client.ConnectionInfo == null
          || client.ConnectionInfo.Encryptions == null)
      {
        return;
      }

      var invalidEncryptions = new string[] {
        "3des-cbc",
        "blowfish-cbc",
        "twofish-cbc",
        "twofish192-cbc",
        "twofish128-cbc",
        "twofish256-cbc",
        "arcfour",
        "arcfour128",
        "arcfour256",
        "cast128-cbc"
      };
      foreach (var cipher in invalidEncryptions)
      {
        if (client.ConnectionInfo.Encryptions.ContainsKey(cipher))
        {
          client.ConnectionInfo.Encryptions.Remove(cipher);
        }
      }

      if (client.ConnectionInfo.KeyExchangeAlgorithms == null)
      {
        return;
      }

      var invalidKeyExchangeAlgorithms = new string[] {
        "diffie-hellman-group-exchange-sha1",
        "diffie-hellman-group1-sha1"
      };
      foreach (var keyExchangeAlgorithm in invalidKeyExchangeAlgorithms)
      {
        if (client.ConnectionInfo.KeyExchangeAlgorithms.ContainsKey(keyExchangeAlgorithm))
        {
          client.ConnectionInfo.KeyExchangeAlgorithms.Remove(keyExchangeAlgorithm);
        }
      }

      if (client.ConnectionInfo.HmacAlgorithms == null)
      {
        return;
      }

      var invalidMACs = new string[] {
        "hmac-md5",
        "hmac-md5-96",
        "hmac-sha1-96",
        "hmac-ripemd160",
        "hmac-ripemd160@openssh.com"
      };
      foreach (var mac in invalidMACs)
      {
        if (client.ConnectionInfo.HmacAlgorithms.ContainsKey(mac))
        {
          client.ConnectionInfo.HmacAlgorithms.Remove(mac);
        }
      }

      if (client.ConnectionInfo.HostKeyAlgorithms == null)
      {
        return;
      }

      var invalidHostKeyAlgorithms = new string[] {
        "ssh-dss"
      };
      foreach (var hostKey in invalidHostKeyAlgorithms)
      {
        if (client.ConnectionInfo.HostKeyAlgorithms.ContainsKey(hostKey))
        {
          client.ConnectionInfo.HostKeyAlgorithms.Remove(hostKey);
        }
      }

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
      var sshClient = new SshClient(connectionInfo);
      RemoveUnsupportedAlgorithms(sshClient);
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
            ValidateDeprecatedAlgorithms(sshClient);

            return client;
          }
        }
      }

      sshClient.Connect();
      sshClient.AddForwardedPort(forwardedPort);
      forwardedPort.Start();
      _sshClientList.Add(sshClient);
      CurrentSshClient = sshClient;
      ValidateDeprecatedAlgorithms(sshClient);

      return sshClient;
    }

    /// <summary>
    /// Raises warning messages if the SSH client is using a deprecated encryption, MAC or key exchanged algorithm.
    /// </summary>
    /// <param name="client">The <see cref="SshClient"/> instance containing the
    /// encryption, MAC algorithm and key exchange algorithm currently being used.</param>
    internal static void ValidateDeprecatedAlgorithms(SshClient client)
    {
      if (client == null
          || !client.IsConnected)
      {
        return;
      }

      var deprecatedEncryptions = new string[] {
        "aes128-cbc",
        "aes192-cbc",
        "aes256-cbc",
      };
      if (deprecatedEncryptions.Any(encryption => client.ConnectionInfo.CurrentServerEncryption == encryption))
      {
        MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.Warning,
            Resources.DeprecatedSshAlgorithm, "encryption", client.ConnectionInfo.CurrentServerEncryption);
      }

      var deprecatedKeyExchangeAlgorithms = new string[] {
        "diffie-hellman-group14-sha1"
      };
      if (deprecatedKeyExchangeAlgorithms.Any(keyExchangeAlgorithm => client.ConnectionInfo.CurrentKeyExchangeAlgorithm == keyExchangeAlgorithm))
      {
        MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.Warning,
            Resources.DeprecatedSshAlgorithm, "key exchange", client.ConnectionInfo.CurrentKeyExchangeAlgorithm);
      }

      var deprecatedMACs = new string[] {
        "hmac-sha1"
      };
      if (deprecatedMACs.Any(mac => client.ConnectionInfo.CurrentServerHmacAlgorithm == mac))
      {
        MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.Warning,
            Resources.DeprecatedSshAlgorithm, "MAC", client.ConnectionInfo.CurrentServerHmacAlgorithm);
      }

      var deprecatedHostKeyAlgorithms = new string[] {
        "ssh-rsa"
      };
      if (deprecatedHostKeyAlgorithms.Any(hostKey => client.ConnectionInfo.CurrentHostKeyAlgorithm == hostKey))
      {
        MySqlTrace.TraceEvent(TraceEventType.Information, MySqlTraceEventType.Warning,
            Resources.DeprecatedSshAlgorithm, "Host Key", client.ConnectionInfo.CurrentHostKeyAlgorithm);
      }

    }
  }
}
