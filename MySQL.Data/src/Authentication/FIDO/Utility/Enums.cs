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

namespace MySql.Data.Authentication.FIDO.Utility
{
  internal enum FidoStatus
  {
    Ok = 0,
    TxErr = -1,
    RxErr = -2,
    RxNotCbor = -3,
    RxInvalidCbor = -4,
    InvalidParam = -5,
    InvalidSignature = -6,
    InvalidArgument = -7,
    UserPresenceRequired = -8,
    InternalError = -9
  }

  /// <summary>
  /// <para>Status codes as defined in Client to Authenticator Protocol (CTAP) standard</para>
  /// <para>Error response values in the range between <see cref="Ok"/> and <see cref="SpecLast"/> are reserved for spec purposes.</para>
  /// <para>Error response values in the range between <see cref="VendorFirst"/> and <see cref="VendorLast"/>
  /// may be used for vendor-specific implementations. All other response values are reserved for future use and may not be used.
  /// These vendor specific error codes are not interoperable and the platform should treat these errors as any other unknown error codes.</para>
  /// <para>Error response values in the range between <see cref="ExtensionFirst"/> and <see cref="ExtensionLast"/>
  /// may be used for extension-specific implementations.</para>
  /// </summary>
  internal enum CtapStatus : int
  {
    /// <summary>
    /// Indicates successful response.
    /// </summary>
    Ok = 0,

    /// <summary>
    /// The command is not a valid CTAP command.
    /// </summary>
    InvalidCommand = 0x01,

    /// <summary>
    /// The command included an invalid parameter.
    /// </summary>
    InvalidParameter = 0x02,

    /// <summary>
    /// Invalid message or item length.
    /// </summary>
    InvalidLength = 0x03,

    /// <summary>
    /// Invalid message sequencing.
    /// </summary>
    InvalidSeq = 0x04,

    /// <summary>
    /// Message timed out.
    /// </summary>
    Timeout = 0x05,

    /// <summary>
    /// Channel busy.
    /// </summary>
    ChannelBusy = 0x06,

    /// <summary>
    /// Command requires channel lock.
    /// </summary>
    LockRequired = 0x0a,

    /// <summary>
    /// Command not allowed on this cid.
    /// </summary>
    InvalidChannel = 0x0b,

    /// <summary>
    /// Invalid/unexpected CBOR error.
    /// </summary>
    CborUnexpectedType = 0x11,

    /// <summary>
    /// Error when parsing CBOR.
    /// </summary>
    InvalidCbor = 0x12,

    /// <summary>
    /// Missing non-optional parameter.
    /// </summary>
    MissingParameter = 0x14,

    /// <summary>
    /// Limit for number of items exceeded.
    /// </summary>
    LimitExceeded = 0x15,

    /// <summary>
    /// Unsupported extension.
    /// </summary>
    UnsupportedExtension = 0x16,

    /// <summary>
    /// Valid credential found in the exclude list.
    /// </summary>
    CredentialExcluded = 0x19,

    /// <summary>
    /// Processing (Lengthy operation is in progress).
    /// </summary>
    Processing = 0x21,

    /// <summary>
    /// Credential not valid for the authenticator.
    /// </summary>
    InvalidCredential = 0x22,

    /// <summary>
    /// Authentication is waiting for user interaction.
    /// </summary>
    UserActionPending = 0x23,

    /// <summary>
    /// Processing, lengthy operation is in progress.
    /// </summary>
    OperationPending = 0x24,

    /// <summary>
    /// No request is pending.
    /// </summary>
    NoOperations = 0x25,

    /// <summary>
    /// Authenticator does not support requested algorithm.
    /// </summary>
    UnsupportedAlgorithm = 0x26,

    /// <summary>
    /// Not authorized for requested operation.
    /// </summary>
    OperationDenied = 0x27,

    /// <summary>
    /// Internal key storage is full.
    /// </summary>
    KeyStoreFull = 0x28,

    /// <summary>
    /// No outstanding operations.
    /// </summary>
    NoOperationPending = 0x2a,

    /// <summary>
    /// Unsupported option.
    /// </summary>
    UnsupportedOption = 0x2b,

    /// <summary>
    /// Not a valid option for current operation.
    /// </summary>
    InvalidOption = 0x2c,

    /// <summary>
    /// Pending keep alive was cancelled.
    /// </summary>
    KeepAliveCancel = 0x2d,

    /// <summary>
    /// No valid credentials provided.
    /// </summary>
    NoCredentials = 0x2e,

    /// <summary>
    /// Timeout waiting for user interaction.
    /// </summary>
    UserActionTimeout = 0x2f,

    /// <summary>
    /// Continuation command, such as, authenticatorGetNextAssertion not allowed.
    /// </summary>
    NotAllowed = 0x30,

    /// <summary>
    /// PIN Invalid.
    /// </summary>
    PinInvalid = 0x31,

    /// <summary>
    /// PIN Blocked.
    /// </summary>
    PinBlocked = 0x32,

    /// <summary>
    /// PIN authentication,pinAuth, verification failed.
    /// </summary>
    PinAuthInvalid = 0x33,

    /// <summary>
    /// PIN authentication,pinAuth, blocked. Requires power recycle to reset.
    /// </summary>
    PinAuthBlocked = 0x34,

    /// <summary>
    /// No PIN has been set.
    /// </summary>
    PinNotSet = 0x35,

    /// <summary>
    /// PIN is required for the selected operation.
    /// </summary>
    PinRequired = 0x36,

    /// <summary>
    /// PIN policy violation. Currently only enforces minimum length.
    /// </summary>
    PolicyViolation = 0x37,

    /// <summary>
    /// pinToken expired on authenticator.
    /// </summary>
    PinTokenExpired = 0x38,

    /// <summary>
    /// Authenticator cannot handle this request due to memory constraints.
    /// </summary>
    RequestTooLarge = 0x39,

    /// <summary>
    /// The current operation has timed out.
    /// </summary>
    ActionTimeout = 0x3a,

    /// <summary>
    /// User presence is required for the requested operation.
    /// </summary>
    UpRequired = 0x3b,

    /// <summary>
    /// Other unspecified error.
    /// </summary>
    Other = 0x7f,

    /// <summary>
    /// CTAP 2 spec last error.
    /// </summary>
    SpecLast = 0xdf,

    /// <summary>
    /// Extension specific error.
    /// </summary>
    ExtensionFirst = 0xe0,

    /// <summary>
    /// Extension specific error.
    /// </summary>
    ExtensionLast = 0xef,

    /// <summary>
    /// Vendor specific error.
    /// </summary>
    VendorFirst = 0xf0,

    /// <summary>
    /// Vendor specific error.
    /// </summary>
    VendorLast = 0xff
  }
}
