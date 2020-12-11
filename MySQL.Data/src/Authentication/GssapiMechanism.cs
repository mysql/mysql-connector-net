// Copyright (c) 2020, Oracle and/or its affiliates.
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

using MySql.Data.Authentication.GSSAPI;
using MySql.Data.Authentication.GSSAPI.Utility;
using System;

namespace MySql.Data.MySqlClient.Authentication
{
  /// <summary>
  /// The GSSAPI mechanism.
  /// </summary>
  internal class GssapiMechanism
  {
    private bool finalHandshake = false;
    private bool complete = false;
    private GssCredentials gssCredentials = null;
    private GssContext gssContext = null;

    internal string MechanismName
    {
      get { return "GSSAPI"; }
    }

    /// <summary>
    /// Obtain credentials to be used to create a security context
    /// </summary>
    /// <param name="username">username</param>
    /// <param name="password">password</param>
    /// <param name="host">host</param>
    public GssapiMechanism(string username, string password)
    {
      // Gets the Service Principal Name from the Kerberos configuration file.
      string krbServicePrincipal = KerberosConfig.GetServicePrincipalName(username);

      try
      {
        // Attempt to retrieve credentials from default cache file.
        gssCredentials = new GssCredentials(username);
      }
      catch (Exception)
      {
        if (string.IsNullOrWhiteSpace(password))
          throw new MySqlException("Unable to retrieve stored credentials from default cache file.");

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

      // if the authentication is complete, then we can pass null so the OK packet could be read from server
      if (complete)
        return response;
      else if (finalHandshake)
        return DoFinalHandshake(data);
      else
      {
        try
        {
          // Initiate Security Context
          response = gssContext.InitSecContext(data);
        }
        catch (Exception)
        {
          throw new MySqlException("Unable to initiate Security Context.");
        }

        if (gssContext.IsEstablished)
          finalHandshake = true;

        return response;
      }
    }

    /// <summary>
    /// Security context already established.
    /// </summary>
    /// <param name="message">A byte array containing the challenge data from the server</param>
    /// <returns>A non-null byte array containing the response to be sent to the server</returns>
    internal byte[] DoFinalHandshake(byte[] data)
    {
      var unwrapped = gssContext.Unwrap(data);

      byte[] outPutMessage = new byte[4];
      outPutMessage[0] = 1;
      var response = gssContext.Wrap(outPutMessage);
      complete = true;

      return response;
    }
  }
}