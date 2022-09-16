// Copyright (c) 2021, 2022, Oracle and/or its affiliates.
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

using MySql.Data.Authentication.GSSAPI.Native;
using MySql.Data.Authentication.GSSAPI.Utility;
using MySql.Data.MySqlClient;
using System;
using System.Runtime.InteropServices;
using static MySql.Data.Authentication.GSSAPI.Native.NativeMethods;

namespace MySql.Data.Authentication.GSSAPI
{
  internal enum CredentialUsage
  {
    Both = 0,
    Initiate = 1,
    Accept = 2
  }

  /// <summary>
  /// Credentials to use to establish the context
  /// </summary>
  internal class GssCredentials : IDisposable
  {
    internal IntPtr _credentials;
    private IntPtr _gssUsername;

    internal string UserName { get; set; }

    /// <summary>
    /// Acquires credentials for the supplied principal using the supplied password
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <param name="usage">GSS_C_BOTH - Credentials may be used either to initiate or accept security contexts. 
    /// GSS_C_INITIATE - Credentials will only be used to initiate security contexts. 
    /// GSS_C_ACCEPT - Credentials will only be used to accept security contexts.</param>
    /// <returns>An object containing the credentials</returns>
    internal GssCredentials(string username, string password, CredentialUsage usage = CredentialUsage.Initiate)
    {
      UserName = username;

      uint minorStatus;
      uint majorStatus;

      // copy the principal name to a gss_buffer
      using (var gssUsernameBuffer = GssType.GetBufferFromString(username))
      using (var gssPasswordBuffer = GssType.GetBufferFromString(password))
      {
        // use the buffer to import the name into a gss_name
        majorStatus = gss_import_name(
            out minorStatus,
            ref gssUsernameBuffer.Value,
            ref Const.GssNtUserName,
            out _gssUsername
        );
        if (majorStatus != Const.GSS_S_COMPLETE)
          throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: Unable to import the supplied user name.",
              majorStatus, minorStatus, Const.GssNtHostBasedService));

        majorStatus = gss_acquire_cred_with_password(
          out minorStatus,
          _gssUsername,
          ref gssPasswordBuffer.Value,
          0,
          ref Const.GssKrb5MechOidSet,
          (int)usage,
          ref _credentials,
          IntPtr.Zero,
          out var actualExpiry);

        if (majorStatus != Const.GSS_S_COMPLETE)
          throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: Unable to acquire credentials for authentication.",
              majorStatus, minorStatus, Const.GssKrb5MechOidDesc));
      }
    }

    /// <summary>
    /// Acquires credentials for the supplied principal using material stored in a valid keytab
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="usage">GSS_C_BOTH - Credentials may be used either to initiate or accept security contexts. 
    /// GSS_C_INITIATE - Credentials will only be used to initiate security contexts. 
    /// GSS_C_ACCEPT - Credentials will only be used to accept security contexts.</param>
    /// <returns>An object containing the credentials</returns>
    internal GssCredentials(string username, CredentialUsage usage = CredentialUsage.Initiate)
    {
      UserName = username;

      // allocate a gss buffer and copy the principal name to it
      using (var gssNameBuffer = GssType.GetBufferFromString(username))
      {
        uint minorStatus;
        uint majorStatus;

        // use the buffer to import the name into a gss_name
        majorStatus = gss_import_name(
            out minorStatus,
            ref gssNameBuffer.Value,
            ref Const.GssNtUserName,
            out var _gssUsername
        );
        if (majorStatus != Const.GSS_S_COMPLETE)
          throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: Unable to import the supplied user name.",
              majorStatus, minorStatus, Const.GssNtHostBasedService));

        majorStatus = gss_acquire_cred(
            out minorStatus,
            _gssUsername,
            Const.GSS_C_INDEFINITE,
            ref Const.GssKrb5MechOidSet,
            (int)usage,
            ref _credentials,
            IntPtr.Zero,
            out var actualExpiry);

        if (majorStatus != Const.GSS_S_COMPLETE)
          throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: Unable acquire credentials for authentication.",
              majorStatus, minorStatus, Const.GssKrb5MechOidDesc));
      }
    }

    /// <summary>
    /// Acquires default credentials stored in the cache
    /// </summary>
    /// <param name="usage">GSS_C_BOTH - Credentials may be used either to initiate or accept security contexts. 
    /// GSS_C_INITIATE - Credentials will only be used to initiate security contexts. 
    /// GSS_C_ACCEPT - Credentials will only be used to accept security contexts.</param>
    /// <returns>An object containing the credentials</returns>
    internal GssCredentials(CredentialUsage usage = CredentialUsage.Initiate)
    {
      uint minorStatus, lifetime;
      int credentialUsage;
      IntPtr name, mechs;

      var majorStatus = gss_acquire_cred(
          out minorStatus,
          Const.GSS_C_NO_NAME,
          Const.GSS_C_INDEFINITE,
          ref Const.GssKrb5MechOidSet,
          (int)usage,
          ref _credentials,
          IntPtr.Zero,
          out var actualExpiry);

      if (majorStatus != Const.GSS_S_COMPLETE)
        throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: Unable acquire credentials for authentication.",
            majorStatus, minorStatus, Const.GssKrb5MechOidDesc));

      majorStatus = gss_inquire_cred(
        out minorStatus,
        _credentials,
        out name,
        out lifetime,
        out credentialUsage,
        out mechs);

      if (majorStatus != Const.GSS_S_COMPLETE)
        throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: Unable to obtain information about the credentials provided.",
            majorStatus, minorStatus, Const.GssKrb5MechOidDesc));

      UserName = TranslateDisplayName(name);
    }

    /// <summary>
    /// Translates a name in internal form to a textual representation.
    /// </summary>
    /// <param name="name">Name in internal form (GSSAPI).</param>
    /// <returns></returns>
    private static string TranslateDisplayName(IntPtr name)
    {
      string userName;

      GssBufferDescStruct buffer;
      gss_display_name(out _, name, out buffer, out _);

      userName = buffer.value == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(buffer.value);

      var majorStatus = gss_release_buffer(out var minorStatus, ref buffer);
      if (majorStatus != Const.GSS_S_COMPLETE)
        throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: An error occurred releasing a buffer.",
                majorStatus, minorStatus, Const.GSS_C_NO_OID));

      return userName;
    }

    public void Dispose()
    {
      uint minorStatus;
      uint majorStatus;

      majorStatus = gss_release_name(out minorStatus, ref _gssUsername);
      if (majorStatus != Const.GSS_S_COMPLETE)
        throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: Unable to release the user name handle.",
          majorStatus, minorStatus, Const.GssNtHostBasedService));

      majorStatus = gss_release_cred(out minorStatus, ref _credentials);
      if (majorStatus != Const.GSS_S_COMPLETE)
        throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: Unable to release the credential handle.",
          majorStatus, minorStatus, Const.GssNtHostBasedService));
    }
  }
}