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

using MySql.Data.MySqlClient;
using System;
using static MySql.Data.Authentication.SSPI.NativeMethods;

namespace MySql.Data.Authentication.SSPI
{
  internal class SspiSecurityContext : IDisposable
  {
    private SECURITY_HANDLE securityContext = default;
    private SspiCredentials credentials;

    /// <summary>
    /// Creates an instance of SspiSecurityContext with credentials provided.
    /// </summary>
    /// <param name="credentials">Credentials to be used with the Security Context</param>
    internal SspiSecurityContext(SspiCredentials credentials)
    {
      this.credentials = credentials;
    }

    /// <summary>
    /// Initiates the client side, outbound security context from a credential handle.
    /// </summary>
    /// <param name="clientBlob">Byte array to be sent to the server.</param>
    /// <param name="serverBlob">Byte array received by the server.</param>
    /// <param name="targetName">The target.</param>
    internal ContextStatus InitializeSecurityContext(out byte[] clientBlob, byte[] serverBlob, string targetName)
    {
      clientBlob = null;
      SecBufferDesc clientBufferDesc = new SecBufferDesc(Const.MAX_TOKEN_SIZE);
      SECURITY_INTEGER initLifetime = new SECURITY_INTEGER(0);
      SecStatus result = 0;

      try
      {
        uint ContextAttributes = 0;

        if (serverBlob == null)
        {
          result = InitializeSecurityContext_0(
            ref credentials.credentialsHandle,
            IntPtr.Zero,
            targetName,
            Const.STANDARD_CONTEXT_ATTRIBUTES,
            0,
            Const.SECURITY_NETWORK_DREP,
            IntPtr.Zero, /* always zero first time around */
            0,
            out securityContext,
            out clientBufferDesc,
            out ContextAttributes,
            out initLifetime);
        }
        else
        {
          SecBufferDesc serverBufferDesc = new SecBufferDesc(serverBlob);

          try
          {
            result = InitializeSecurityContext_1(
              ref credentials.credentialsHandle,
              ref securityContext,
              targetName,
              Const.STANDARD_CONTEXT_ATTRIBUTES,
              0,
              Const.SECURITY_NETWORK_DREP,
              ref serverBufferDesc,
              0,
              out securityContext,
              out clientBufferDesc,
              out ContextAttributes,
              out initLifetime);
          }
          finally
          {
            serverBufferDesc.Dispose();
          }
        }

        if ((SecStatus.SEC_I_COMPLETE_NEEDED == result)
            || (SecStatus.SEC_I_COMPLETE_AND_CONTINUE == result))
        {
          CompleteAuthToken(ref securityContext, ref clientBufferDesc);
        }

        if (result != SecStatus.SEC_E_OK &&
            result != SecStatus.SEC_I_CONTINUE_NEEDED &&
            result != SecStatus.SEC_I_COMPLETE_NEEDED &&
            result != SecStatus.SEC_I_COMPLETE_AND_CONTINUE)
        {
          throw new MySqlException("InitializeSecurityContext() failed  with errorcode " + result);
        }

        clientBlob = clientBufferDesc.GetSecBufferByteArray();
      }
      finally
      {
        clientBufferDesc.Dispose();
      }

      if (result == SecStatus.SEC_I_CONTINUE_NEEDED)
        return ContextStatus.RequiresContinuation;

      return ContextStatus.Accepted;
    }

    public void Dispose()
    {
      FreeCredentialsHandle(ref credentials.credentialsHandle);
      DeleteSecurityContext(ref securityContext);
    }
  }

  internal enum ContextStatus
  {
    RequiresContinuation,
    Accepted,
    Error
  }
}
