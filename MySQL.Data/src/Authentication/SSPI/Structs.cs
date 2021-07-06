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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MySql.Data.Authentication.SSPI
{
  [StructLayout(LayoutKind.Sequential)]
  internal struct SecBufferDesc : IDisposable
  {
    private int _ulVersion;
    private int _cBuffers;
    private IntPtr _pBuffers; //Point to SecBuffer

    internal SecBufferDesc(int bufferSize)
    {
      _ulVersion = (int)SecBufferType.SECBUFFER_VERSION;
      _cBuffers = 1;
      SecBuffer secBuffer = new SecBuffer(bufferSize);
      _pBuffers = Marshal.AllocHGlobal(Marshal.SizeOf(secBuffer));
      Marshal.StructureToPtr(secBuffer, _pBuffers, false);
    }

    internal SecBufferDesc(byte[] secBufferBytes)
    {
      _ulVersion = (int)SecBufferType.SECBUFFER_VERSION;
      _cBuffers = 1;
      SecBuffer thisSecBuffer = new SecBuffer(secBufferBytes);
      _pBuffers = Marshal.AllocHGlobal(Marshal.SizeOf(thisSecBuffer));
      Marshal.StructureToPtr(thisSecBuffer, _pBuffers, false);
    }

    public void Dispose()
    {
      if (_pBuffers != IntPtr.Zero)
      {
        Debug.Assert(_cBuffers == 1);
        SecBuffer ThisSecBuffer = Marshal.PtrToStructure<SecBuffer>(_pBuffers);
        ThisSecBuffer.Dispose();
        Marshal.FreeHGlobal(_pBuffers);
        _pBuffers = IntPtr.Zero;
      }
    }

    internal byte[] GetSecBufferByteArray()
    {
      byte[] Buffer = null;

      if (_pBuffers == IntPtr.Zero)
      {
        throw new InvalidOperationException("Object has already been disposed!!!");
      }
      Debug.Assert(_cBuffers == 1);
      SecBuffer secBuffer = Marshal.PtrToStructure<SecBuffer>(_pBuffers);
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
  internal enum SecBufferType
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
  internal struct SecHandle //=PCtxtHandle
  {
    IntPtr dwLower; // ULONG_PTR translates to IntPtr not to uint
    IntPtr dwUpper; // this is crucial for 64-Bit Platforms
  }

  /// <summary>
  /// Describes a buffer allocated by a transport to pass to a security package.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  internal struct SecBuffer : IDisposable
  {
    /// <summary>
    /// Specifies the size, in bytes, of the buffer.
    /// </summary>
    internal int cbBuffer;

    /// <summary>
    /// Bit flags that indicate the type of the buffer.
    /// </summary>
    internal int BufferType;

    /// <summary>
    /// Pointer to a buffer.
    /// </summary>
    internal IntPtr pvBuffer;


    internal SecBuffer(int bufferSize)
    {
      cbBuffer = bufferSize;
      BufferType = (int)SecBufferType.SECBUFFER_TOKEN;
      pvBuffer = Marshal.AllocHGlobal(bufferSize);
    }

    internal SecBuffer(byte[] secBufferBytes)
    {
      cbBuffer = secBufferBytes.Length;
      BufferType = (int)SecBufferType.SECBUFFER_TOKEN;
      pvBuffer = Marshal.AllocHGlobal(cbBuffer);
      Marshal.Copy(secBufferBytes, 0, pvBuffer, cbBuffer);
    }

    internal SecBuffer(byte[] secBufferBytes, SecBufferType bufferType)
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
  internal struct SECURITY_INTEGER
  {
    /// <summary>
    /// Least significant digits.
    /// </summary>
    uint LowPart;

    /// <summary>
    /// Most significant digits.
    /// </summary>
    int HighPart;
    internal SECURITY_INTEGER(int dummy)
    {
      LowPart = 0;
      HighPart = 0;
    }
  }

  /// <summary>
  /// Holds a pointer used to define a security handle.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  internal struct SECURITY_HANDLE
  {
    /// <summary>
    /// Least significant digits.
    /// </summary>
    ulong LowPart;

    /// <summary>
    /// Most significant digits.
    /// </summary>
    ulong HighPart;

    bool IsSet => this.LowPart > 0 || this.HighPart > 0;

    internal SECURITY_HANDLE(int dummy)
    {
      LowPart = HighPart = 0;
    }
  }

  /// <summary>
  /// Indicates the sizes of important structures used in the message support functions.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  internal struct SecPkgContext_Sizes
  {
    /// <summary>
    /// Specifies the maximum size of the security token used in the authentication changes.
    /// </summary>
    uint cbMaxToken;

    /// <summary>
    /// Specifies the maximum size of the signature created by the <b>MakeSignature</b> function.
    /// This member must be zero if integrity services are not requested or available.
    /// </summary>
    uint cbMaxSignature;

    /// <summary>
    /// Specifies the preferred integral size of the messages.
    /// </summary>
    uint cbBlockSize;

    /// <summary>
    /// Size of the security trailer to be appended to messages.
    /// This member should be zero if the relevant services are not requested or available.
    /// </summary>
    uint cbSecurityTrailer;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct SecPkgContext_SecString
  {
    public IntPtr sValue;
  }

  /// <summary>
  /// Implements the 'SEC_WINNT_AUTH_IDENTITY' structure. See:
  /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa380131(v=vs.85).aspx
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  internal struct SEC_WINNT_AUTH_IDENTITY
  {
    [MarshalAs(UnmanagedType.LPWStr)]
    public string User;
    public int UserLength;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Domain;
    public int DomainLength;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Password;
    public int PasswordLength;
    public uint Flags;
  }
}
