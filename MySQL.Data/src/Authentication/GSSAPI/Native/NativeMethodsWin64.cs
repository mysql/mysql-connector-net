// Copyright (c) 2022, Oracle and/or its affiliates.
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

namespace MySql.Data.Authentication.GSSAPI.Native
{
  /// <summary>
  /// MIT Kerberos 5 GSS Bindings Windows 64bit
  /// </summary>
  internal static class NativeMethodsWin64
  {
    private const string GSS_MODULE_NAME = "gssapi64.dll";

    [DllImport(GSS_MODULE_NAME, EntryPoint = "gss_import_name")]
    internal static extern uint gss_import_name(
      out uint minorStatus,
      ref GssBufferDescStruct inputNameBuffer,
      ref GssOidDescStruct inputNameType,
      out IntPtr outputName);

    [DllImport(GSS_MODULE_NAME, EntryPoint = "gss_acquire_cred")]
    internal static extern uint gss_acquire_cred(
      out uint minorStatus,
      IntPtr desiredName,
      uint timeRequired,
      ref GssOidSetStruct desiredMechanisms,
      int credentialUsage,
      ref IntPtr credentialHandle,
      IntPtr actualMech,
      out uint expiryTime);

    [DllImport(GSS_MODULE_NAME, EntryPoint = "gss_acquire_cred_with_password")]
    internal static extern uint gss_acquire_cred_with_password(
      out uint minorStatus,
      IntPtr desiredName,
      ref GssBufferDescStruct password,
      uint timeRequired,
      ref GssOidSetStruct desiredMechanisms,
      int credentialUsage,
      ref IntPtr credentialHandle,
      IntPtr actualMechs,
      out uint expiryTime);

    [DllImport(GSS_MODULE_NAME, EntryPoint = "gss_init_sec_context")]
    internal static extern uint gss_init_sec_context(
      out uint minorStatus,
      IntPtr claimantCredHandle,
      ref IntPtr contextHandle,
      IntPtr targetName,
      ref GssOidDescStruct mechType,
      uint reqFlags,
      uint timeReq,
      IntPtr inputChanBindings,
      ref GssBufferDescStruct inputToken,
      IntPtr actualMechType,
      out GssBufferDescStruct outputToken,
      IntPtr retFlags,
      IntPtr timeRec);

    [DllImport(GSS_MODULE_NAME, EntryPoint = "gss_display_status")]
    internal static extern uint gss_display_status(
      out uint minorStatus,
      uint status,
      int statusType,
      ref GssOidDescStruct mechType,
      ref IntPtr messageContext,
      ref GssBufferDescStruct statusString);

    [DllImport(GSS_MODULE_NAME, EntryPoint = "gss_release_buffer")]
    internal static extern uint gss_release_buffer(
      out uint minorStatus,
      ref GssBufferDescStruct buffer);

    [DllImport(GSS_MODULE_NAME, EntryPoint = "gss_release_cred")]
    internal static extern uint gss_release_cred(
      out uint minorStatus,
      ref IntPtr credentialHandle);

    [DllImport(GSS_MODULE_NAME, EntryPoint = "gss_release_name")]
    internal static extern uint gss_release_name(
      out uint minorStatus,
      ref IntPtr inputName);

    [DllImport(GSS_MODULE_NAME, EntryPoint = "gss_delete_sec_context")]
    internal static extern uint gss_delete_sec_context(
      out uint minorStatus,
      ref IntPtr contextHandle,
      IntPtr outputToken);

    [DllImport(GSS_MODULE_NAME, EntryPoint = "gss_unwrap")]
    internal static extern uint gss_unwrap(
      out uint minorStatus,
      IntPtr contextHandle,
      ref GssBufferDescStruct inputMessage,
      out GssBufferDescStruct outputMessage,
      out int confState,
      out uint qopState);

    [DllImport(GSS_MODULE_NAME, EntryPoint = "gss_wrap")]
    internal static extern uint gss_wrap(
      out uint minorStatus,
      IntPtr contextHandle,
      int confReqFlag,
      uint qopReq,
      ref GssBufferDescStruct inputMessage,
      int confState,
      out GssBufferDescStruct outputMessage);

    [DllImport(GSS_MODULE_NAME, EntryPoint = "gss_inquire_cred")]
    internal static extern uint gss_inquire_cred(
      out uint minorStatus,
      IntPtr credentialHandle,
      out IntPtr name,
      out uint lifetime,
      out int credentialUsage,
      out IntPtr mechs);

    [DllImport(GSS_MODULE_NAME, EntryPoint = "gss_display_name")]
    internal static extern uint gss_display_name(
        out uint minorStatus,
        IntPtr inputName,
        out GssBufferDescStruct NameBuffer,
        out GssOidDescStruct nameType);
  }
}