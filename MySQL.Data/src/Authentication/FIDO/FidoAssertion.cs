// Copyright (c) 2022, 2023, Oracle and/or its affiliates.
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

using MySql.Data.Authentication.FIDO.Native;
using MySql.Data.Authentication.FIDO.Utility;
using System;

namespace MySql.Data.Authentication.FIDO
{
  /// <summary>
  /// A reference struct representing a statement contained within a <see cref="FidoAssertion"/> object
  /// </summary>
  internal unsafe ref struct FidoAssertionStatement
  {
    #region Properties
    /// <summary>
    /// <para>WebAuthn §6.1 https://www.w3.org/TR/webauthn-1/#sec-authenticator-data</para>
    /// Gets the authenticator data for the assertion statement.
    /// </summary>
    public ReadOnlySpan<byte> AuthData { get; }

    /// <summary>
    /// Gets the authenticator data length for the assertion statement.
    /// </summary>
    public int AuthDataLen { get; }

    /// <summary>
    /// Gets the ID for this assertion statement
    /// </summary>
    public ReadOnlySpan<byte> Id { get; }

    /// <summary>
    /// Gets the signature for this assertion statement
    /// </summary>
    public ReadOnlySpan<byte> Signature { get; }

    /// <summary>
    /// Gets the signature length for this assertion statement
    /// </summary>
    public int SignatureLen { get; }
    #endregion

    #region Constructors
    internal FidoAssertionStatement(fido_assert_t* native, int idx)
    {
      IntPtr index = new IntPtr(idx);
      AuthDataLen = NativeMethods.fido_assert_authdata_len(native, index);
      AuthData = new ReadOnlySpan<byte>(NativeMethods.fido_assert_authdata_ptr(native, index), AuthDataLen);
      Id = new ReadOnlySpan<byte>(NativeMethods.fido_assert_id_ptr(native, index), NativeMethods.fido_assert_id_len(native, index));
      SignatureLen = NativeMethods.fido_assert_sig_len(native, index);
      Signature = new ReadOnlySpan<byte>(NativeMethods.fido_assert_sig_ptr(native, index), SignatureLen);
    }
    #endregion
  }

  /// <summary>
  /// Creates an object for holding data about a given assertion. In FIDO2, an assertion
  /// is proof that the authenticator being used has knowledge of the private key associated
  /// with the public key that the other party is in posession of.
  /// </summary>
  internal sealed unsafe class FidoAssertion : IDisposable
  {
    private fido_assert_t* _assert;

    #region Constructors
    static FidoAssertion()
    {
      Init.Call();
    }

    /// <summary>
    /// Default Constructor
    /// </summary>
    /// <exception cref="OutOfMemoryException" />
    public FidoAssertion()
    {
      _assert = NativeMethods.fido_assert_new();
      if (_assert == null)
        throw new OutOfMemoryException();
    }

    /// <summary>
    /// Finalizer
    /// </summary>
    ~FidoAssertion() => ReleaseUnmanagedResources();
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the hash of the client data object that the assertion is based on.
    /// </summary>
    /// <exception cref="CtapException">Thrown if an error occurs while setting the hash</exception>
    public ReadOnlySpan<byte> ClientDataHash
    {
      set
      {
        fixed (byte* value_ = value)
        {
          NativeMethods.fido_assert_set_clientdata_hash(_assert, value_, (IntPtr)value.Length).Check();
        }
      }
    }

    /// <summary>
    /// Gets or sets the relying party that requested this assertion
    /// </summary>
    /// <exception cref="CtapException">Thrown if an error occurs while setting the relying party</exception>
    public string Rp
    {
      set => NativeMethods.fido_assert_set_rp(_assert, value).Check();
    }
    #endregion

    /// <summary>
    /// Adds an allowed credential to this assertion.  If used, only credential objects
    /// with the IDs added via this method will be considered when making an assertion.
    /// </summary>
    /// <param name="credentialId">The ID of the credential to add to the whitelist</param>
    /// <exception cref="CtapException">Thrown if an error occurs while adding the credential</exception>
    public void AllowCredential(ReadOnlySpan<byte> credentialId)
    {
      fixed (byte* cred = credentialId)
      {
        NativeMethods.fido_assert_allow_cred(_assert, cred, (IntPtr)credentialId.Length).Check();
      }
    }

    /// <summary>
    /// Cast operator for using this object as a native handle
    /// </summary>
    /// <param name="assert">The object to use</param>
    public static explicit operator fido_assert_t*(FidoAssertion assert)
    {
      return assert._assert;
    }

    /// <summary>
    /// Gets the assertion statement at the index provided.
    /// </summary>
    /// <param name="idx">The index of the assertion statement to retrieve</param>
    /// <returns>The assertion statement object</returns>
    /// <exception cref="IndexOutOfRangeException">The index is not in the range [0, count)</exception>
    public FidoAssertionStatement GetFidoAssertionStatement(int idx=0)
    {
      return new FidoAssertionStatement(_assert, idx);
    }

    /// <summary>
    /// Gets the number of assertions contained in the authentication device.
    /// </summary>
    /// <returns>The number of assertions contained in the authentication device.</returns>
    public int GetAssertCount()
    {
      return NativeMethods.fido_assert_count(_assert);
    }

    private void ReleaseUnmanagedResources()
    {
      var native = _assert;
      NativeMethods.fido_assert_free(&native);
      _assert = null;
    }

    #region IDisposable
    public void Dispose()
    {
      ReleaseUnmanagedResources();
      GC.SuppressFinalize(this);
    }
    #endregion
  }
}
