// Copyright (c) 2012, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace MySql.Data.MySqlClient.Authentication
{
  /// <summary>
  /// 
  /// </summary>
  [SuppressUnmanagedCodeSecurity()]
  internal class MySqlWindowsAuthenticationPlugin : MySqlAuthenticationPlugin
  {
    SECURITY_HANDLE outboundCredentials = new SECURITY_HANDLE(0);
    SECURITY_HANDLE clientContext = new SECURITY_HANDLE(0);
    SECURITY_INTEGER lifetime = new SECURITY_INTEGER(0);
    bool continueProcessing;
    string targetName = null;

    protected override void CheckConstraints()
    {
      string platform = String.Empty;
      
      int p = (int)Environment.OSVersion.Platform;
      if ((p == 4) || (p == 128))
        platform = "Unix";
      else if (Environment.OSVersion.Platform == PlatformID.MacOSX) 
        platform = "Mac OS/X";

      if (!String.IsNullOrEmpty(platform))
        throw new MySqlException(String.Format(Resources.WinAuthNotSupportOnPlatform, platform));

      base.CheckConstraints();
    }

    public override string GetUsername()
    {
      string username = base.GetUsername();
      if (String.IsNullOrEmpty(username)) 
        return "auth_windows";
      return username;
    }

    public override string PluginName
    {
      get { return "authentication_windows_client"; }
    }

    protected override byte[] MoreData(byte[] moreData)
    {
      if (moreData == null)
        AcquireCredentials();

      byte[] clientBlob = null;

      if (continueProcessing)
        InitializeClient(out clientBlob, moreData, out continueProcessing);

      if (!continueProcessing || clientBlob == null || clientBlob.Length == 0)
      {
        FreeCredentialsHandle(ref outboundCredentials);
        DeleteSecurityContext(ref clientContext);
        return null;
      }
      return clientBlob;
    }

    void InitializeClient(out byte[] clientBlob, byte[] serverBlob, out bool continueProcessing)
    {
      clientBlob = null;
      continueProcessing = true;
      SecBufferDesc clientBufferDesc = new SecBufferDesc(MAX_TOKEN_SIZE);
      SECURITY_INTEGER initLifetime = new SECURITY_INTEGER(0);
      int ss = -1;
      try
      {
        uint ContextAttributes = 0;

        if (serverBlob == null)
        {
          ss = InitializeSecurityContext(
              ref outboundCredentials,
              IntPtr.Zero,
              targetName,
              STANDARD_CONTEXT_ATTRIBUTES,
              0,
              SECURITY_NETWORK_DREP,
              IntPtr.Zero, /* always zero first time around */
              0,
              out clientContext,
              out clientBufferDesc,
              out ContextAttributes,
              out initLifetime);

        }
        else
        {
          SecBufferDesc serverBufferDesc = new SecBufferDesc(serverBlob);

          try
          {
            ss = InitializeSecurityContext(ref outboundCredentials,
                ref clientContext,
                targetName,
                STANDARD_CONTEXT_ATTRIBUTES,
                0,
                SECURITY_NETWORK_DREP,
                ref serverBufferDesc,
                0,
                out clientContext,
                out clientBufferDesc,
                out ContextAttributes,
                out initLifetime);
          }
          finally
          {
            serverBufferDesc.Dispose();
          }
        }


        if ((SEC_I_COMPLETE_NEEDED == ss)
            || (SEC_I_COMPLETE_AND_CONTINUE == ss))
        {
          CompleteAuthToken(ref clientContext, ref clientBufferDesc);
        }

        if (ss != SEC_E_OK &&
            ss != SEC_I_CONTINUE_NEEDED &&
            ss != SEC_I_COMPLETE_NEEDED &&
            ss != SEC_I_COMPLETE_AND_CONTINUE)
        {
          throw new MySqlException(
              "InitializeSecurityContext() failed  with errorcode " + ss);
        }

        clientBlob = clientBufferDesc.GetSecBufferByteArray();
      }
      finally
      {
        clientBufferDesc.Dispose();
      }
      continueProcessing = (ss != SEC_E_OK && ss != SEC_I_COMPLETE_NEEDED);
    }

    private void AcquireCredentials()
    {

      continueProcessing = true;

      int ss = AcquireCredentialsHandle(null, "Negotiate", SECPKG_CRED_OUTBOUND,
              IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, ref outboundCredentials,
              ref lifetime);
      if (ss != SEC_E_OK)
        throw new MySqlException("AcquireCredentialsHandle failed with errorcode" + ss);
    }

    #region SSPI Constants and Imports

    const int SEC_E_OK = 0;
    const int SEC_I_CONTINUE_NEEDED = 0x90312;
    const int SEC_I_COMPLETE_NEEDED = 0x1013;
    const int SEC_I_COMPLETE_AND_CONTINUE = 0x1014;

    const int SECPKG_CRED_OUTBOUND = 2;
    const int SECURITY_NETWORK_DREP = 0;
    const int SECURITY_NATIVE_DREP = 0x10;
    const int SECPKG_CRED_INBOUND = 1;
    const int MAX_TOKEN_SIZE = 12288;
    const int SECPKG_ATTR_SIZES = 0;
    const int STANDARD_CONTEXT_ATTRIBUTES = 0;

    [DllImport("secur32", CharSet = CharSet.Unicode)]
    static extern int AcquireCredentialsHandle(
        string pszPrincipal,
        string pszPackage,
        int fCredentialUse,
        IntPtr PAuthenticationID,
        IntPtr pAuthData,
        int pGetKeyFn,
        IntPtr pvGetKeyArgument,
        ref SECURITY_HANDLE phCredential,
        ref SECURITY_INTEGER ptsExpiry);

    [DllImport("secur32", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int InitializeSecurityContext(
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

    [DllImport("secur32", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int InitializeSecurityContext(
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

    [DllImport("secur32", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int CompleteAuthToken(
        ref SECURITY_HANDLE phContext,
        ref SecBufferDesc pToken);

    [DllImport("secur32.Dll", CharSet = CharSet.Unicode, SetLastError = false)]
    public static extern int QueryContextAttributes(
        ref SECURITY_HANDLE phContext,
        uint ulAttribute,
        out SecPkgContext_Sizes pContextAttributes);

    [DllImport("secur32.Dll", CharSet = CharSet.Unicode, SetLastError = false)]
    public static extern int FreeCredentialsHandle(ref SECURITY_HANDLE pCred);

    [DllImport("secur32.Dll", CharSet = CharSet.Unicode, SetLastError = false)]
    public static extern int DeleteSecurityContext(ref SECURITY_HANDLE pCred);

    #endregion
  }

  [StructLayout(LayoutKind.Sequential)]
  struct SecBufferDesc : IDisposable
  {

    public int ulVersion;
    public int cBuffers;
    public IntPtr pBuffers; //Point to SecBuffer

    public SecBufferDesc(int bufferSize)
    {
      ulVersion = (int)SecBufferType.SECBUFFER_VERSION;
      cBuffers = 1;
      SecBuffer secBuffer = new SecBuffer(bufferSize);
      pBuffers = Marshal.AllocHGlobal(Marshal.SizeOf(secBuffer));
      Marshal.StructureToPtr(secBuffer, pBuffers, false);
    }

    public SecBufferDesc(byte[] secBufferBytes)
    {
      ulVersion = (int)SecBufferType.SECBUFFER_VERSION;
      cBuffers = 1;
      SecBuffer thisSecBuffer = new SecBuffer(secBufferBytes);
      pBuffers = Marshal.AllocHGlobal(Marshal.SizeOf(thisSecBuffer));
      Marshal.StructureToPtr(thisSecBuffer, pBuffers, false);
    }

    public void Dispose()
    {
      if (pBuffers != IntPtr.Zero)
      {
        Debug.Assert(cBuffers == 1);
        SecBuffer ThisSecBuffer = Marshal.PtrToStructure< SecBuffer>(pBuffers);
        ThisSecBuffer.Dispose();
        Marshal.FreeHGlobal(pBuffers);
        pBuffers = IntPtr.Zero;
      }
    }

    public byte[] GetSecBufferByteArray()
    {
      byte[] Buffer = null;

      if (pBuffers == IntPtr.Zero)
      {
        throw new InvalidOperationException("Object has already been disposed!!!");
      }
      Debug.Assert(cBuffers == 1);
      SecBuffer secBuffer = Marshal.PtrToStructure< SecBuffer>(pBuffers);
      if (secBuffer.cbBuffer > 0)
      {
        Buffer = new byte[secBuffer.cbBuffer];
        Marshal.Copy(secBuffer.pvBuffer, Buffer, 0, secBuffer.cbBuffer);
      }
      return (Buffer);
    }

  }

  /// <summary>
  /// Defines the type of the security buffer.
  /// </summary>
  public enum SecBufferType
  {
    SECBUFFER_VERSION = 0,
    SECBUFFER_EMPTY = 0,
    SECBUFFER_DATA = 1,
    SECBUFFER_TOKEN = 2
  }

  /// <summary>
  /// Defines a security handle.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct SecHandle //=PCtxtHandle
  {
    IntPtr dwLower; // ULONG_PTR translates to IntPtr not to uint
    IntPtr dwUpper; // this is crucial for 64-Bit Platforms
  }

  /// <summary>
  /// Describes a buffer allocated by a transport to pass to a security package.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct SecBuffer : IDisposable
  {
    /// <summary>
    /// Specifies the size, in bytes, of the buffer.
    /// </summary>
    public int cbBuffer;

    /// <summary>
    /// Bit flags that indicate the type of the buffer.
    /// </summary>
    public int BufferType;

    /// <summary>
    /// Pointer to a buffer.
    /// </summary>
    public IntPtr pvBuffer;


    public SecBuffer(int bufferSize)
    {
      cbBuffer = bufferSize;
      BufferType = (int)SecBufferType.SECBUFFER_TOKEN;
      pvBuffer = Marshal.AllocHGlobal(bufferSize);
    }

    public SecBuffer(byte[] secBufferBytes)
    {
      cbBuffer = secBufferBytes.Length;
      BufferType = (int)SecBufferType.SECBUFFER_TOKEN;
      pvBuffer = Marshal.AllocHGlobal(cbBuffer);
      Marshal.Copy(secBufferBytes, 0, pvBuffer, cbBuffer);
    }

    public SecBuffer(byte[] secBufferBytes, SecBufferType bufferType)
    {
      cbBuffer = secBufferBytes.Length;
      BufferType = (int)bufferType;
      pvBuffer = Marshal.AllocHGlobal(cbBuffer);
      Marshal.Copy(secBufferBytes, 0, pvBuffer, cbBuffer);
    }

    public void Dispose()
    {
      if (pvBuffer != IntPtr.Zero)
      {
        Marshal.FreeHGlobal(pvBuffer);
        pvBuffer = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// Hold a numeric value used in defining other data types.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct SECURITY_INTEGER
  {
    /// <summary>
    /// Least significant digits.
    /// </summary>
    public uint LowPart;

    /// <summary>
    /// Most significant digits.
    /// </summary>
    public int HighPart;
    public SECURITY_INTEGER(int dummy)
    {
      LowPart = 0;
      HighPart = 0;
    }
  };

  /// <summary>
  /// Holds a pointer used to define a security handle.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct SECURITY_HANDLE
  {
    /// <summary>
    /// Least significant digits.
    /// </summary>
    public IntPtr LowPart;

    /// <summary>
    /// Most significant digits.
    /// </summary>
    public IntPtr HighPart;

    public SECURITY_HANDLE(int dummy)
    {
      LowPart = HighPart = new IntPtr(0);
    }
  };

  /// <summary>
  /// Indicates the sizes of important structures used in the message support functions.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct SecPkgContext_Sizes
  {
    /// <summary>
    /// Specifies the maximum size of the security token used in the authentication changes.
    /// </summary>
    public uint cbMaxToken;

    /// <summary>
    /// Specifies the maximum size of the signature created by the <b>MakeSignature</b> function.
    /// This member must be zero if integrity services are not requested or available.
    /// </summary>
    public uint cbMaxSignature;

    /// <summary>
    /// Specifies the preferred integral size of the messages.
    /// </summary>
    public uint cbBlockSize;

    /// <summary>
    /// Size of the security trailer to be appended to messages.
    /// This member should be zero if the relevant services are not requested or available.
    /// </summary>
    public uint cbSecurityTrailer;
  };

}
