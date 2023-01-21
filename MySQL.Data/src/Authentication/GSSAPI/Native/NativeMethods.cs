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

using System;

namespace MySql.Data.Authentication.GSSAPI.Native
{
  internal static class NativeMethods
  {
    private static bool? _isWin;
    private static bool IsWin => _isWin ?? (_isWin = Environment.OSVersion.Platform.ToString().StartsWith("Win")).Value;

    /// <summary>
    /// Converts a contiguous string name to GSS_API internal format
    /// <para>The gss_import_name() function converts a contiguous string name to internal form. In general, 
    /// the internal name returned by means of the output_name parameter will not be a mechanism name; the exception to this is if the input_name_type 
    /// indicates that the contiguous string provided by means of the input_name_buffer parameter is of type GSS_C_NT_EXPORT_NAME, in which case, 
    /// the returned internal name will be a mechanism name for the mechanism that exported the name.</para>
    /// </summary>
    /// <param name="minorStatus">Status code returned by the underlying mechanism.</param>
    /// <param name="inputNameBuffer">The gss_buffer_desc structure containing the name to be imported.</param>
    /// <param name="inputNameType">A gss_OID that specifies the format that the input_name_buffer is in.</param>
    /// <param name="outputName">The gss_name_t structure to receive the returned name in internal form. Storage associated with this name must be freed by the application after use with a call to gss_release_name().</param>
    /// <returns>
    /// <para>The gss_import_name() function may return the following status codes:</para>
    /// <para>GSS_S_COMPLETE: The gss_import_name() function completed successfully.</para>
    /// <para>GSS_S_BAD_NAMETYPE: The input_name_type was unrecognized.</para>
    /// <para>GSS_S_BAD_NAME: The input_name parameter could not be interpreted as a name of the specified type.</para>
    /// <para>GSS_S_BAD_MECH: The input_name_type was GSS_C_NT_EXPORT_NAME, but the mechanism contained within the input_name is not supported.</para>
    /// </returns>
    internal static uint gss_import_name(
      out uint minorStatus,
      ref GssBufferDescStruct inputNameBuffer,
      ref GssOidDescStruct inputNameType,
      out IntPtr outputName)
    {
      return IsWin
        ? NativeMethodsWin64.gss_import_name(out minorStatus, ref inputNameBuffer, ref inputNameType, out outputName)
        : NativeMethodsLinux.gss_import_name(out minorStatus, ref inputNameBuffer, ref inputNameType, out outputName);
    }

    /// <summary>
    /// Allows an application to acquire a handle for a pre-existing credential by name. GSS-API implementations must impose a local access-control
    /// policy on callers of this routine to prevent unauthorized callers from acquiring credentials to which they are not entitled. 
    /// This routine is not intended to provide a "login to the network" function, as such a function would involve the creation of new credentials 
    /// rather than merely acquiring a handle to existing credentials
    /// </summary>
    /// <param name="minorStatus">Mechanism specific status code.</param>
    /// <param name="desiredName">Name of principal whose credential should be acquired.</param>
    /// <param name="timeRequired">Number of seconds that credentials should remain valid. 
    /// Specify GSS_C_INDEFINITE to request that the credentials have the maximum permitted lifetime.</param>
    /// <param name="desiredMechanisms">Set of underlying security mechanisms that may be used. 
    /// GSS_C_NO_OID_SET may be used to obtain an implementation-specific default.</param>
    /// <param name="credentialUsage">GSS_C_BOTH - Credentials may be used either to initiate or accept security contexts. 
    /// GSS_C_INITIATE - Credentials will only be used to initiate security contexts. 
    /// GSS_C_ACCEPT - Credentials will only be used to accept security contexts.</param>
    /// <param name="credentialHandle">The returned credential handle. Resources associated with this credential handle must be released
    /// by the application after use with a call to gss_release_cred().</param>
    /// <param name="actualMech">The set of mechanisms for which the credential is valid. Storage associated with the returned OID-set must 
    /// be released by the application after use with a call to gss_release_oid_set(). Specify NULL if not required.</param>
    /// <param name="expiryTime">Actual number of seconds for which the returned credentials will remain valid. If the implementation does not 
    /// support expiration of credentials, the value GSS_C_INDEFINITE will be returned. Specify NULL if not required.</param>
    /// <returns>
    /// <para>gss_acquire_cred() may return the following status codes:</para>
    /// <para>GSS_S_COMPLETE: Successful completion.</para>
    /// <para>GSS_S_BAD_MECH: Unavailable mechanism requested.</para>
    /// <para> GSS_S_BAD_NAMETYPE: Type contained within desired_name parameter is not supported.</para>
    /// <para>GSS_S_BAD_NAME: Value supplied for desired_name parameter is ill formed.</para>
    /// <para>GSS_S_CREDENTIALS_EXPIRED: The credentials could not be acquired Because they have expired.</para>
    /// <para>GSS_S_NO_CRED: No credentials were found for the specified name.</para>
    /// </returns>
    internal static uint gss_acquire_cred(
      out uint minorStatus,
      IntPtr desiredName,
      uint timeRequired,
      ref GssOidSetStruct desiredMechanisms,
      int credentialUsage,
      ref IntPtr credentialHandle,
      IntPtr actualMech,
      out uint expiryTime)
    {
      return IsWin
        ? NativeMethodsWin64.gss_acquire_cred(out minorStatus, desiredName, timeRequired, ref desiredMechanisms,
            credentialUsage, ref credentialHandle, actualMech, out expiryTime)
        : NativeMethodsLinux.gss_acquire_cred(out minorStatus, desiredName, timeRequired, ref desiredMechanisms,
            credentialUsage, ref credentialHandle, actualMech, out expiryTime);
    }

    /// <summary>
    /// Acquires a credential for use in establishing a security context using a password.
    /// </summary>
    /// <param name="minorStatus">Mechanism specific status code.</param>
    /// <param name="desiredName">Name of principal whose credential should be acquired.</param>
    /// <param name="password">The password.</param>
    /// <param name="timeRequired">Number of seconds that credentials should remain valid. 
    /// Specify GSS_C_INDEFINITE to request that the credentials have the maximum permitted lifetime.</param>
    /// <param name="desiredMechanisms">Set of underlying security mechanisms that may be used. 
    /// GSS_C_NO_OID_SET may be used to obtain an implementation-specific default.</param>
    /// <param name="credentialUsage">GSS_C_BOTH - Credentials may be used either to initiate or accept security contexts. 
    /// GSS_C_INITIATE - Credentials will only be used to initiate security contexts. 
    /// GSS_C_ACCEPT - Credentials will only be used to accept security contexts.</param>
    /// <param name="credentialHandle">The returned credential handle. Resources associated with this credential handle must be released
    /// by the application after use with a call to gss_release_cred().</param>
    /// <param name="actualMechs">The set of mechanisms for which the credential is valid. Storage associated with the returned OID-set must 
    /// be released by the application after use with a call to gss_release_oid_set(). Specify NULL if not required.</param>
    /// <param name="expiryTime">Actual number of seconds for which the returned credentials will remain valid. If the implementation does not 
    /// support expiration of credentials, the value GSS_C_INDEFINITE will be returned. Specify NULL if not required.</param>
    /// <returns>
    /// <para>gss_acquire_cred_with_password() may return the following status codes:</para>
    /// <para>GSS_S_COMPLETE: Successful completion.</para>
    /// <para>GSS_S_BAD_MECH: Unavailable mechanism requested.</para>
    /// <para> GSS_S_BAD_NAMETYPE: Type contained within desired_name parameter is not supported.</para>
    /// <para>GSS_S_BAD_NAME: Value supplied for desired_name parameter is ill formed.</para>
    /// <para>GSS_S_CREDENTIALS_EXPIRED: The credentials could not be acquired Because they have expired.</para>
    /// <para>GSS_S_NO_CRED: No credentials were found for the specified name.</para>
    /// </returns>
    internal static uint gss_acquire_cred_with_password(
      out uint minorStatus,
      IntPtr desiredName,
      ref GssBufferDescStruct password,
      uint timeRequired,
      ref GssOidSetStruct desiredMechanisms,
      int credentialUsage,
      ref IntPtr credentialHandle,
      IntPtr actualMechs,
      out uint expiryTime)
    {
      return IsWin
        ? NativeMethodsWin64.gss_acquire_cred_with_password(out minorStatus, desiredName, ref password, timeRequired,
            ref desiredMechanisms, credentialUsage, ref credentialHandle, actualMechs, out expiryTime)
        : NativeMethodsLinux.gss_acquire_cred_with_password(out minorStatus, desiredName, ref password, timeRequired,
            ref desiredMechanisms, credentialUsage, ref credentialHandle, actualMechs, out expiryTime);
    }

    /// <summary>
    /// Obtains information about a credential.
    /// </summary>
    /// <param name="minorStatus">Mechanism specific status code.</param>
    /// <param name="credentialHandle">A handle that refers to the target credential.</param>
    /// <param name="name">The name whose identity the credential asserts.</param>
    /// <param name="lifetime">The number of seconds for which the credential remain valid. 
    /// If the credential has expired, this parameter is set to zero.</param>
    /// <param name="credentialUsage">How the credential may be used.</param>
    /// <param name="mechs">Set of mechanisms supported by the credential.</param>
    /// <returns>
    /// <para>gss_init_sec_context() may return the following status codes:</para>
    /// <para>GSS_S_COMPLETE: Successful completion.</para>
    /// <para>GSS_S_NO_CRED: The referenced credentials could not be accessed.</para>
    /// <para>GSS_S_DEFECTIVE_CREDENTIAL: The referenced credentials were invalid.</para>
    /// <para>GSS_S_CREDENTIALS_EXPIRED: The referenced credentials have expired. 
    /// If the lifetime parameter is not passed in as NULL, then its value is set to 0.</para>
    /// </returns>
    internal static uint gss_inquire_cred(
      out uint minorStatus,
      IntPtr credentialHandle,
      out IntPtr name,
      out uint lifetime,
      out int credentialUsage,
      out IntPtr mechs)
    {
      return IsWin
        ? NativeMethodsWin64.gss_inquire_cred(out minorStatus, credentialHandle, out name, out lifetime, out credentialUsage, out mechs)
        : NativeMethodsLinux.gss_inquire_cred(out minorStatus, credentialHandle, out name, out lifetime, out credentialUsage, out mechs);
    }

    /// <summary>
    /// Initiates the establishment of a security context between the application and a remote peer. 
    /// Initially, the input_token parameter should be specified either as GSS_C_NO_BUFFER, or as a pointer to a gss_buffer_desc object whose length field 
    /// contains the value zero. The routine may return a output_token which should be transferred to the peer application, where the peer application will 
    /// present it to gss_accept_sec_context. If no token need be sent, gss_init_sec_context will indicate this by setting the length field of the output_token 
    /// argument to zero. To complete the context establishment, one or more reply tokens may be required from the peer application; if so, gss_init_sec_context 
    /// will return a status containing the supplementary information bit GSS_S_CONTINUE_NEEDED. In this case, gss_init_sec_context should be called again when the 
    /// reply token is received from the peer application, passing the reply token to gss_init_sec_context via the input_token parameters.
    /// </summary>
    /// <param name="minorStatus">Mechanism specific status code.</param>
    /// <param name="claimantCredHandle">Handle for credentials claimed. Supply GSS_C_NO_CREDENTIAL to act as a default initiator principal. 
    /// If no default initiator is defined, the function will return GSS_S_NO_CRED.</param>
    /// <param name="contextHandle">Context handle for new context. Supply GSS_C_NO_CONTEXT for first call; use value returned by first call in continuation calls. 
    /// Resources associated with this context-handle must be released by the application after use with a call to gss_delete_sec_context().</param>
    /// <param name="targetName">Name of target.</param>
    /// <param name="mechType">Object ID of desired mechanism. Supply GSS_C_NO_OID to obtain an implementation specific default.</param>
    /// <param name="reqFlags">Contains various independent flags, each of which requests that the context support a specific service option. 
    /// Symbolic names are provided for each flag, and the symbolic names corresponding to the required flags should be logically-ORed together to form the bit-mask value.</param>
    /// <param name="timeReq">Desired number of seconds for which context should remain valid. Supply 0 to request a default validity period.</param>
    /// <param name="inputChanBindings">Application-specified bindings. Allows application to securely bind channel identification information to the security context. 
    /// Specify GSS_C_NO_CHANNEL_BINDINGS if channel bindings are not used.</param>
    /// <param name="inputToken">Token received from peer application. Supply GSS_C_NO_BUFFER, or a pointer to a buffer containing the value GSS_C_EMPTY_BUFFER on initial call.</param>
    /// <param name="actualMechType">Actual mechanism used. The OID returned via this parameter will be a pointer to static storage that should be treated as read-only; 
    /// In particular the application should not attempt to free it. Specify NULL if not required.</param>
    /// <param name="outputToken">Token to be sent to peer application. If the length field of the returned buffer is zero, no token need be sent to the peer application. 
    /// Storage associated with this buffer must be freed by the application after use with a call to gss_release_buffer().</param>
    /// <param name="retFlags">Contains various independent flags, each of which indicates that the context supports a specific service option. 
    /// Specify NULL if not required. Symbolic names are provided for each flag, and the symbolic names corresponding to the required flags should be 
    /// logically-ANDed with the ret_flags value to test whether a given option is supported by the context.</param>
    /// <param name="timeRec">Number of seconds for which the context will remain valid. If the implementation does not support context expiration,
    /// the value GSS_C_INDEFINITE will be returned. Specify NULL if not required.</param>
    /// <returns>
    /// <para>gss_init_sec_context() may return the following status codes:</para>
    /// <para></para>
    /// <para>GSS_S_COMPLETE: Successful completion.</para>
    /// <para>GSS_S_CONTINUE_NEEDED: A token from the peer application is required to complete the context, and gss_init_sec_context() must be called again with that token.</para>
    /// <para>GSS_S_DEFECTIVE_TOKEN: Consistency checks performed on the input_token failed.</para>
    /// <para>GSS_S_DEFECTIVE_CREDENTIAL: Consistency checks performed on the credential failed.</para>
    /// <para>GSS_S_NO_CRED: The supplied credentials are not valid for context acceptance, or the credential handle does not reference any credentials.</para>
    /// <para>GSS_S_CREDENTIALS_EXPIRED: The referenced credentials have expired.</para>
    /// <para>GSS_S_BAD_BINDINGS: The input_token contains different channel bindings than those specified by means of the input_chan_bindings parameter.</para>
    /// <para>GSS_S_BAD_SIG: The input_token contains an invalid MIC or a MIC that cannot be verified.</para>
    /// <para>GSS_S_OLD_TOKEN: The input_token is too old. This is a fatal error while establishing context.</para>
    /// <para>GSS_S_DUPLICATE_TOKEN: The input_token is valid, but it is a duplicate of a token already processed.This is a fatal error while establishing context.</para>
    /// <para>GSS_S_NO_CONTEXT: The supplied context handle does not refer to a valid context.</para>
    /// <para>GSS_S_BAD_NAMETYPE: The provided target_name parameter contains an invalid or unsupported name type.</para>
    /// <para>GSS_S_BAD_NAME: The supplied target_name parameter is ill-formed.</para>
    /// <para>GSS_S_BAD_MECH: The token received specifies a mechanism that is not supported by the implementation or the provided credential.</para>
    /// </returns>
    internal static uint gss_init_sec_context(
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
      IntPtr timeRec)
    {
      return IsWin
        ? NativeMethodsWin64.gss_init_sec_context(out minorStatus, claimantCredHandle, ref contextHandle, targetName,
            ref mechType, reqFlags, timeReq, inputChanBindings, ref inputToken, actualMechType,
            out outputToken, retFlags, timeRec)
        : NativeMethodsLinux.gss_init_sec_context(out minorStatus, claimantCredHandle, ref contextHandle, targetName,
            ref mechType, reqFlags, timeReq, inputChanBindings, ref inputToken, actualMechType,
            out outputToken, retFlags, timeRec);
    }

    /// <summary>
    /// Allows an application to obtain a textual representation of a GSS-API status code, for display to the user or for logging purposes.
    /// Since some status values may indicate multiple conditions, applications may need to call gss_display_status multiple times, 
    /// each call generating a single text string. The message_context parameter is used by gss_display_status to store state information about which
    /// error messages have already been extracted from a given status_value; message_context must be initialized to 0 by the application prior to the first call,
    /// and gss_display_status will return a non-zero value in this parameter if there are further messages to extract.
    /// </summary>
    /// <param name="minorStatus">Mechanism specific status code.</param>
    /// <param name="status">Status value to be converted.</param>
    /// <param name="statusType">GSS_C_GSS_CODE - status_value is a GSS status code. GSS_C_MECH_CODE - status_value is a mechanism status code.</param>
    /// <param name="mechType">Underlying mechanism (used to interpret a minor status value). Supply GSS_C_NO_OID to obtain the system default.</param>
    /// <param name="messageContext">Should be initialized to zero by the application prior to the first call. 
    /// On return from gss_display_status(), a non-zero status_value parameter indicates that additional messages may be extracted from the status code via 
    /// subsequent calls to gss_display_status(), passing the same status_value, status_type, mech_type, and message_context parameters.</param>
    /// <param name="statusString">Textual interpretation of the status_value. Storage associated with this parameter must be freed by the application
    /// after use with a call to gss_release_buffer().</param>
    /// <returns>
    /// <para>gss_display_status() may return the following status codes:</para>
    /// <para>GSS_S_COMPLETE: Successful completion.</para>
    /// <para>GSS_S_BAD_MECH: Indicates that translation in accordance with an unsupported mechanism type was requested.</para>
    /// <para>GSS_S_BAD_STATUS: The status value was not recognized, or the status type was neither GSS_C_GSS_CODE nor GSS_C_MECH_CODE.</para>
    /// </returns>
    internal static uint gss_display_status(
      out uint minorStatus,
      uint status,
      int statusType,
      ref GssOidDescStruct mechType,
      ref IntPtr messageContext,
      ref GssBufferDescStruct statusString)
    {
      return IsWin
        ? NativeMethodsWin64.gss_display_status(out minorStatus, status, statusType, ref mechType, ref messageContext,
              ref statusString)
        : NativeMethodsLinux.gss_display_status(out minorStatus, status, statusType, ref mechType, ref messageContext,
              ref statusString);
    }

    /// <summary>
    /// Allows an application to obtain a textual representation of an opaque internal-form name for display purposes.
    /// The syntax of a printable name is defined by the GSS-API implementation.
    /// </summary>
    /// <param name="minorStatus">Mechanism specific status code.</param>
    /// <param name="inputName">Name to be displayed.</param>
    /// <param name="nameBuffer">Buffer to receive textual name string.</param>
    /// <param name="nameType">The type of the returned name.</param>
    /// <returns>
    /// <para>gss_display_name() may return the following status codes:</para>
    /// <para>GSS_S_COMPLETE: Successful completion.</para>
    /// <para>GSS_S_BAD_NAME: input_name was ill-formed.</para>
    /// </returns>
    internal static uint gss_display_name(
        out uint minorStatus,
        IntPtr inputName,
        out GssBufferDescStruct nameBuffer,
        out GssOidDescStruct nameType)
    {
      return IsWin
        ? NativeMethodsWin64.gss_display_name(out minorStatus, inputName, out nameBuffer, out nameType)
        : NativeMethodsLinux.gss_display_name(out minorStatus, inputName, out nameBuffer, out nameType);
    }

    /// <summary>
    /// Free storage associated with a buffer. The storage must have been allocated by a GSS-API routine. 
    /// In addition to freeing the associated storage, the routine will zero the length field in the descriptor to which the buffer parameter refers, 
    /// and implementations are encouraged to additionally set the pointer field in the descriptor to NULL. Any buffer object returned by a GSS-API routine 
    /// may be passed to gss_release_buffer (even if there is no storage associated with the buffer).
    /// </summary>
    /// <param name="minorStatus">Mechanism-specific status code.</param>
    /// <param name="buffer">The storage associated with the buffer will be deleted. The gss_buffer_desc object will not be freed, 
    /// but its length field will be zeroed.</param>
    /// <returns>
    /// <para>The gss_release_buffer() function may return the following status codes:</para>
    /// <para>GSS_S_COMPLETE: Successful completion</para>
    /// </returns>
    internal static uint gss_release_buffer(
      out uint minorStatus,
      ref GssBufferDescStruct buffer)
    {
      return IsWin
        ? NativeMethodsWin64.gss_release_buffer(out minorStatus, ref buffer)
        : NativeMethodsLinux.gss_release_buffer(out minorStatus, ref buffer);
    }

    /// <summary>
    /// Delete a security context. gss_delete_sec_context will delete the local data structures associated with the specified security context, 
    /// and may generate an output_token, which when passed to the peer gss_process_context_token will instruct it to do likewise. 
    /// If no token is required by the mechanism, the GSS-API should set the length field of the output_token (if provided) to zero.
    /// No further security services may be obtained using the context specified by context_handle.
    /// </summary>
    /// <param name="minorStatus">Mechanism specific status code.</param>
    /// <param name="contextHandle">Context handle identifying context to delete. After deleting the context, 
    /// the GSS-API will set this context handle to GSS_C_NO_CONTEXT.</param>
    /// <returns>
    /// <para>The gss_delete_sec_context() function may return the following status codes:</para>
    /// <para>GSS_S_COMPLETE: Successful completion.</para>
    /// <para>GSS_S_NO_CONTEXT: No valid context was supplied.</para>
    /// </returns>
    internal static uint gss_delete_sec_context(
      out uint minorStatus,
      ref IntPtr contextHandle)
    {
      return IsWin
        ? NativeMethodsWin64.gss_delete_sec_context(out minorStatus, ref contextHandle, Const.GSS_C_NO_BUFFER)
        : NativeMethodsLinux.gss_delete_sec_context(out minorStatus, ref contextHandle, Const.GSS_C_NO_BUFFER);
    }

    /// <summary>
    /// Free GSSAPI-allocated storage associated with an internal-form name. The name is set to GSS_C_NO_NAME on successful completion of this call.
    /// </summary>
    /// <param name="minorStatus">Mechanism specific status code.</param>
    /// <param name="inputName">The name to be deleted.</param>
    /// <returns>
    /// <para>The gss_release_name() function may return the following status codes:</para>
    /// <para>GSS_S_COMPLETE: Successful completion.</para>
    /// <para>GSS_S_BAD_NAME: The name parameter did not contain a valid name.</para>
    /// </returns>
    internal static uint gss_release_name(
      out uint minorStatus,
      ref IntPtr inputName)
    {
      return IsWin
        ? NativeMethodsWin64.gss_release_name(out minorStatus, ref inputName)
        : NativeMethodsLinux.gss_release_name(out minorStatus, ref inputName);
    }

    /// <summary>
    /// Informs GSS-API that the specified credential handle is no longer required by the application, and frees associated resources.
    /// The cred_handle is set to GSS_C_NO_CREDENTIAL on successful completion of this call.
    /// </summary>
    /// <param name="minorStatus">Mechanism specific status code.</param>
    /// <param name="credentialHandle">Opaque handle identifying credential to be released. If GSS_C_NO_CREDENTIAL is supplied, 
    /// the routine will complete successfully, but will do nothing.</param>
    /// <returns>
    /// <para>The gss_release_cred() function may return the following status codes:</para>
    /// <para>GSS_S_COMPLETE: Successful completion.</para>
    /// <para>GSS_S_NO_CRED: Credentials could not be accessed.</para>
    /// </returns>
    internal static uint gss_release_cred(
      out uint minorStatus,
      ref IntPtr credentialHandle)
    {
      return IsWin
        ? NativeMethodsWin64.gss_release_cred(out minorStatus, ref credentialHandle)
        : NativeMethodsLinux.gss_release_cred(out minorStatus, ref credentialHandle);
    }

    /// <summary>
    /// Converts a message previously protected by gss_wrap back to a usable form, verifying the embedded MIC. 
    /// The conf_state parameter indicates whether the message was encrypted; the qop_state parameter indicates the strength of
    /// protection that was used to provide the confidentiality and integrity services.
    /// </summary>
    /// <param name="minorStatus">Mechanism specific status code.</param>
    /// <param name="contextHandle">Identifies the context on which the message arrived.</param>
    /// <param name="inputMessage">Protected message.</param>
    /// <param name="outputMessage">Buffer to receive unwrapped message.</param>
    /// <param name="confState">State of the configuration.</param>
    /// <param name="qopState">State of the QoP.</param>
    /// <returns>
    /// <para>The gss_unwrap() function may return the following status codes:</para>
    /// <para>GSS_S_COMPLETE: Successful completion.</para>
    /// <para>GSS_S_DEFECTIVE_TOKEN: The token failed consistency checks.</para>
    /// <para>GSS_S_BAD_SIG: The MIC was incorrect.</para>
    /// <para>GSS_S_DUPLICATE_TOKEN: The token was valid, and contained a correct MIC for the message, but it had already been processed.</para>
    /// <para>GSS_S_OLD_TOKEN: The token was valid, and contained a correct MIC for the message, but it is too old to check for duplication.</para>
    /// <para>GSS_S_UNSEQ_TOKEN: The token was valid, and contained a correct MIC for the message, but has been verified out of sequence; 
    /// a later token has already been received.</para>
    /// <para>GSS_S_GAP_TOKEN: The token was valid, and contained a correct MIC for the message, but has been verified out of sequence; 
    /// an earlier expected token has not yet been received.</para>
    /// <para>GSS_S_CONTEXT_EXPIRED: The context has already expired.</para>
    /// <para>GSS_S_NO_CONTEXT: The context_handle parameter did not identify a valid context.</para>
    /// </returns>
    internal static uint gss_unwrap(
      out uint minorStatus,
      IntPtr contextHandle,
      ref GssBufferDescStruct inputMessage,
      out GssBufferDescStruct outputMessage,
      out int confState,
      out uint qopState)
    {
      return IsWin
        ? NativeMethodsWin64.gss_unwrap(out minorStatus, contextHandle, ref inputMessage, out outputMessage, out confState, out qopState)
        : NativeMethodsLinux.gss_unwrap(out minorStatus, contextHandle, ref inputMessage, out outputMessage, out confState, out qopState);
    }

    /// <summary>
    /// Attaches a cryptographic MIC and optionally encrypts the specified input_message. The output_message contains both the MIC and the message.
    /// The qop_req parameter allows a choice between several cryptographic algorithms, if supported by the chosen mechanism.
    /// </summary>
    /// <param name="minorStatus">Mechanism specific status code.</param>
    /// <param name="contextHandle">Identifies the context on which the message arrived.</param>
    /// <param name="inputMessage">Message to be protected.</param>
    /// <param name="outputMessage"> Buffer to receive protected message.</param>
    /// <returns>
    /// <para>The gss_unwrap() function may return the following status codes:</para>
    /// <para>GSS_S_COMPLETE: Successful completion.</para>   
    /// <para>GSS_S_CONTEXT_EXPIRED: The context has already expired.</para>
    /// <para>GSS_S_NO_CONTEXT: The context_handle parameter did not identify a valid context.</para>
    /// <para>GSS_S_BAD_QOP: The specified QOP is not supported by the mechanism.</para>
    /// </returns>
    internal static uint gss_wrap(
      out uint minorStatus,
      IntPtr contextHandle,
      ref GssBufferDescStruct inputMessage,
      out GssBufferDescStruct outputMessage)
    {
      return IsWin
        ? NativeMethodsWin64.gss_wrap(out minorStatus, contextHandle, 0, Const.GSS_C_QOP_DEFAULT, ref inputMessage, 0, out outputMessage)
        : NativeMethodsLinux.gss_wrap(out minorStatus, contextHandle, 0, Const.GSS_C_QOP_DEFAULT, ref inputMessage, 0, out outputMessage);
    }
  }
}