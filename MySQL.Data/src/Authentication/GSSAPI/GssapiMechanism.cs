// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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

using MySql.Data.Authentication.GSSAPI.Utility;
using MySql.Data.MySqlClient;
using System;

namespace MySql.Data.Authentication.GSSAPI
{
  /// <summary>
  /// The GSSAPI mechanism.
  /// </summary>
  internal class GssapiMechanism
  {
    private bool finalHandshake = false;
    private GssCredentials gssCredentials = null;
    internal GssContext gssContext = null;

    internal string MechanismName
    {
      get { return "GSSAPI"; }
    }

    /// <summary>
    /// Obtain credentials to be used to create a security context
    /// </summary>
    /// <param name="username">username</param>
    /// <param name="password">password</param>
    /// <param name="krbServicePrincipal">host</param>
    public GssapiMechanism(string username, string password, string krbServicePrincipal = null)
    {
      // Gets the Service Principal Name from the Kerberos configuration file.
      krbServicePrincipal = krbServicePrincipal ?? KerberosConfig.GetServicePrincipalName(username);

      try
      {
        // Attempt to retrieve credentials from default cache file.
        gssCredentials = new GssCredentials(username);
      }
      catch (Exception ex)
      {
        if (string.IsNullOrWhiteSpace(password))
          throw new MySqlException("Unable to retrieve stored credentials from default cache file.", ex);

        // Attempt to retrieve credentials using username and password.
        gssCredentials = new GssCredentials(username, password);
      }

      gssContext = new GssContext(krbServicePrincipal, gssCredentials, GssContextFlags.Deleg | GssContextFlags.Mutual);
    }

    /// <summary>
    /// Processes the challenge data.
    /// </summary>
    /// <param name="data">A byte array containing the challenge data from the server</param>
    /// <returns>A byte array containing the response to be sent to the server</returns>
    internal byte[] Challenge(byte[] data)
    {
      byte[] response = null;

      if (finalHandshake)
        return DoFinalHandshake(data);
      else
      {
        try
        {
          // Initiate Security Context
          response = gssContext.InitSecContext(data);
        }
        catch (Exception ex)
        {
          throw new MySqlException("Unable to initiate security context.", ex);
        }

        if (gssContext.IsEstablished)
          finalHandshake = true;

        return response;
      }
    }

    /// <summary>
    /// Security context already established.
    /// </summary>
    /// <param name="data">A byte array containing the challenge data from the server</param>
    /// <returns>A non-null byte array containing the response to be sent to the server</returns>
    internal byte[] DoFinalHandshake(byte[] data)
    {
      // if the authentication is complete, then we can pass null so the OK packet could be read from server
      if (data.Length == 0)
        return null;

      var unwrapped = gssContext.Unwrap(data);

      byte[] outPutMessage = new byte[4];
      outPutMessage[0] = 1;
      var response = gssContext.Wrap(outPutMessage);

      return response;
    }
  }
}