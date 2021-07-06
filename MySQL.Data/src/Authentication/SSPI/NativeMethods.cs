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

using System;
using System.Runtime.InteropServices;

namespace MySql.Data.Authentication.SSPI
{
  /// <summary>
  /// SSPI Bindings
  /// </summary>
  internal static class NativeMethods
  {
    private const string SECUR32 = "secur32.dll";

    #region Imports
    [DllImport(
      SECUR32,
      CharSet = CharSet.Unicode)]
    internal static extern SecStatus AcquireCredentialsHandle(
      string pszPrincipal,
      string pszPackage,
      int fCredentialUse,
      IntPtr PAuthenticationID,
      IntPtr pAuthData,
      int pGetKeyFn,
      IntPtr pvGetKeyArgument,
      ref SECURITY_HANDLE phCredential,
      IntPtr ptsExpiry);

    [DllImport(
      SECUR32,
      CharSet = CharSet.Unicode)]
    internal static extern SecStatus AcquireCredentialsHandle(
      string pszPrincipal,
      string pszPackage,
      int fCredentialUse,
      IntPtr PAuthenticationID,
      SEC_WINNT_AUTH_IDENTITY pAuthData,
      int pGetKeyFn,
      IntPtr pvGetKeyArgument,
      ref SECURITY_HANDLE phCredential,
      IntPtr ptsExpiry);

    [DllImport(
      SECUR32,
      CharSet = CharSet.Unicode,
      SetLastError = true,
      EntryPoint = "InitializeSecurityContext")]
    internal static extern SecStatus InitializeSecurityContext_0(
      ref SECURITY_HANDLE phCredential,
      IntPtr phContext,
      string pszTargetName,
      int fContextReq,
      int Reserved1,
      int TargetDataRep,
      IntPtr pInput,
      int Reserved2,
      out SECURITY_HANDLE phNewContext,
      out SecBufferDesc pOutput,
      out uint pfContextAttr,
      out SECURITY_INTEGER ptsExpiry);

    [DllImport(
      SECUR32,
      CharSet = CharSet.Unicode,
      SetLastError = true,
      EntryPoint = "InitializeSecurityContext")]
    internal static extern SecStatus InitializeSecurityContext_1(
      ref SECURITY_HANDLE phCredential,
      ref SECURITY_HANDLE phContext,
      string pszTargetName,
      int fContextReq,
      int Reserved1,
      int TargetDataRep,
      ref SecBufferDesc SecBufferDesc,
      int Reserved2,
      out SECURITY_HANDLE phNewContext,
      out SecBufferDesc pOutput,
      out uint pfContextAttr,
      out SECURITY_INTEGER ptsExpiry);

    [DllImport(SECUR32, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int CompleteAuthToken(
        ref SECURITY_HANDLE phContext,
        ref SecBufferDesc pToken);

    [DllImport(
      SECUR32,
      CharSet = CharSet.Unicode,
      SetLastError = false,
      EntryPoint = "QueryContextAttributes")]
    internal static extern int QueryContextAttributes_String(
        ref SECURITY_HANDLE phContext,
        uint ulAttribute,
        ref SecPkgContext_SecString pBuffer);

    [DllImport(SECUR32, CharSet = CharSet.Unicode, SetLastError = false)]
    internal static extern int FreeCredentialsHandle(ref SECURITY_HANDLE pCred);

    [DllImport(SECUR32, CharSet = CharSet.Unicode, SetLastError = false)]
    internal static extern int DeleteSecurityContext(ref SECURITY_HANDLE pContext);
    #endregion
  }
}
