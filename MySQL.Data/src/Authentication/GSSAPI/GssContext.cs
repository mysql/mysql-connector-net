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

namespace MySql.Data.Authentication.GSSAPI
{
  /// <summary>
  /// Defines a security context
  /// </summary>
  internal class GssContext : IDisposable
  {
    private bool disposed;

    internal bool IsEstablished = false;
    private GssCredentials Credentials;
    private GssContextFlags Flags;

    private IntPtr _context;
    private IntPtr _gssTargetName;

    /// <summary>
    /// Sets the main properties to create and initiate a security context.
    /// </summary>
    /// <param name="spn">Service Principal Name.</param>
    /// <param name="credentials">Credentials.</param>
    /// <param name="flags">Requested flags.</param>
    internal GssContext(string spn, GssCredentials credentials, GssContextFlags flags)
    {
      Credentials = credentials;
      Flags = flags;

      using (var gssTargetNameBuffer = GssType.GetBufferFromString(spn))
      {
        // use the buffer to import the name into a gss_name
        var majorStatus = NativeMethods.gss_import_name(
            out var minorStatus,
            ref gssTargetNameBuffer.Value,
            ref Const.GssNtPrincipalName,
            out _gssTargetName
        );

        if (majorStatus != Const.GSS_S_COMPLETE)
          throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: Unable to import the supplied target name (SPN).",
              majorStatus, minorStatus, Const.GssNtPrincipalName));
      }
    }

    /// <summary>
    /// Initiate the security context
    /// </summary>
    /// <param name="token">Challenge received by the server.</param>
    /// <returns>A byte array containing the response to be sent to the server</returns>
    internal byte[] InitSecContext(byte[] token)
    {
      // If the token is null, supply a NULL pointer as the input
      var inputToken = token == null
          ? Disposable.From(default(GssBufferDescStruct))
          : GssType.GetBufferFromBytes(token);

      var majorStatus = NativeMethods.gss_init_sec_context(
          out var minorStatus,
          Credentials._credentials,
          ref _context,
          _gssTargetName,
          ref Const.GssKrb5MechOidDesc,
          (uint)Flags,
          0,
          IntPtr.Zero,
          ref inputToken.Value,
          IntPtr.Zero,
          out var output,
          IntPtr.Zero,
          IntPtr.Zero
      );

      switch (majorStatus)
      {
        case Const.GSS_S_COMPLETE:
          IsEstablished = true;
          return MarshalOutputToken(output);

        case Const.GSS_S_CONTINUE_NEEDED:
          return MarshalOutputToken(output);

        default:
          throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: Unable to generate the token from the supplied credentials.",
              majorStatus, minorStatus, Const.GssKrb5MechOidDesc));
      }
    }

    /// <summary>
    /// Unwrap a message.
    /// </summary>
    /// <param name="message">Message acquired from the server.</param>
    /// <returns>Unwrapped message.</returns>
    internal byte[] Unwrap(byte[] message)
    {
      var inputMessage = GssType.GetBufferFromBytes(message);

      var majorStatus = NativeMethods.gss_unwrap(
        out var minorStatus,
        _context,
        ref inputMessage.Value,
        out var outputMessage,
        out var confState,
        out var qopState);

      if (majorStatus == Const.GSS_S_COMPLETE)
        return MarshalOutputToken(outputMessage);
      else
        throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: Unable to unwrap the message provided.",
              majorStatus, minorStatus, Const.GssKrb5MechOidDesc));
    }

    /// <summary>
    /// Wrap a message.
    /// </summary>
    /// <param name="message">Message to be wrapped.</param>
    /// <returns>A byte array containing the wrapped message.</returns>
    internal byte[] Wrap(byte[] message)
    {
      var inputMessage = GssType.GetBufferFromBytes(message);

      var majorStatus = NativeMethods.gss_wrap(
        out var minorStatus,
        _context,
        ref inputMessage.Value,
        out var outputMessage);

      if (majorStatus == Const.GSS_S_COMPLETE)
        return MarshalOutputToken(outputMessage);
      else
        throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: Unable to unwrap the message provided.",
              majorStatus, minorStatus, Const.GssKrb5MechOidDesc));
    }

    /// <summary>
    /// Allocate a clr byte array and copy the token data over
    /// </summary>
    /// <param name="gssToken">Buffer.</param>
    /// <returns>A byte array</returns>
    private static byte[] MarshalOutputToken(GssBufferDescStruct gssToken)
    {
      if (gssToken.length > 0)
      {
        var buffer = new byte[gssToken.length];
        Marshal.Copy(gssToken.value, buffer, 0, (int)gssToken.length);

        // Finally, release the underlying gss buffer
        var majorStatus = NativeMethods.gss_release_buffer(out var minorStatus, ref gssToken);
        if (majorStatus != Const.GSS_S_COMPLETE)
          throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: An error occurred releasing the token buffer allocated.",
            majorStatus, minorStatus, Const.GssKrb5MechOidDesc));

        return buffer;
      }
      return new byte[0];
    }

    /// <summary>
    /// Cleanups unmanaged resources
    /// </summary>
    public void Cleanup()
    {
      if (_context != IntPtr.Zero)
      {
        var majStat = NativeMethods.gss_delete_sec_context(out var minStat, ref _context);
        if (majStat != Const.GSS_S_COMPLETE)
          throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: An error occurred when releasing the token buffer allocated by the GSS provider",
            majStat, minStat, Const.GssKrb5MechOidDesc));
      }

      if (_gssTargetName != IntPtr.Zero)
      {
        var majStat = NativeMethods.gss_release_name(out var minStat, ref _gssTargetName);
        if (majStat != Const.GSS_S_COMPLETE)
          throw new MySqlException(ExceptionMessages.FormatGssMessage("GSSAPI: An error occurred when releasing the gss service principal name",
            majStat, minStat, Const.GssNtHostBasedService));
      }
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposed)
      {
        if (disposing)
          Cleanup();

        disposed = true;
      }
    }

    ~GssContext()
    {
      Cleanup();
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }
}