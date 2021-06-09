// Copyright (c) 2021, Oracle and/or its affiliates.
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
using System;
using static MySql.Data.Authentication.SSPI.NativeMethods;

namespace MySql.Data.Authentication.SSPI
{
  internal class SspiCredentials
  {
    /// <summary>
    /// A safe handle to the credential's handle.
    /// </summary>
    internal SECURITY_HANDLE credentialsHandle = default;

    /// <summary>
    /// Acquires a handle to preexisting credentials of a security principal.
    /// </summary>
    internal SspiCredentials(string package)
    {
      var result = AcquireCredentialsHandle(
        null,
        package,
        Const.SECPKG_CRED_BOTH,
        IntPtr.Zero,
        IntPtr.Zero,
        0,
        IntPtr.Zero,
        ref credentialsHandle,
        IntPtr.Zero);

      if (result != SecStatus.SEC_E_OK)
        throw new MySqlException($"AcquireCredentialsHandle failed with error code: {result}");
    }

    internal SspiCredentials(string principal, string username, string password, string domain, string package)
    {
      var authenticationData = new SEC_WINNT_AUTH_IDENTITY
      {
        User = username,
        UserLength = username.Length,
        Domain = domain,
        DomainLength = domain.Length,
        Password = password,
        PasswordLength = password.Length,
        Flags = Const.SEC_WINNT_AUTH_IDENTITY_UNICODE
      };

      var result = AcquireCredentialsHandle(
        principal,
        package,
        Const.SECPKG_CRED_BOTH,
        IntPtr.Zero,
        authenticationData,
        0,
        IntPtr.Zero,
        ref credentialsHandle,
        IntPtr.Zero);

      if (result != SecStatus.SEC_E_OK)
        throw new Exception($"Unable to aquire credentials for {principal} with error code: {result}");
    }
  }
}
