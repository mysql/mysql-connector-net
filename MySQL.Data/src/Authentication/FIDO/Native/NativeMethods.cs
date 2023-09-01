// Copyright (c) 2022, 2023, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General internal License, version 2.0, as
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
// See the GNU General internal License, version 2.0, for more details.
//
// You should have received a copy of the GNU General internal License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using MySql.Data.Authentication.FIDO.Utility;
using System;
using System.Runtime.InteropServices;

namespace MySql.Data.Authentication.FIDO.Native
{
  /// <summary>
  /// P/Invoke methods
  /// </summary>
  internal static unsafe class NativeMethods
  {
    private const string DllName = "fido2";

    /// <summary>
    /// The fido_init() function initialises the libfido2 library.
    /// Its invocation must precede that of any other libfido2 function.
    /// If FIDO_DEBUG is set in flags, then debug output will be emitted by libfido2 on stderr.
    /// Alternatively, the FIDO_DEBUG environment variable may be set.
    /// </summary>
    /// <param name="flags">The flags to use during initialization</param>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void fido_init(int flags);

    #region FidoDevice
    /// <summary>
    /// Returns a pointer to a newly allocated, empty fido_dev_t type.
    /// If memory cannot be allocated, <c>null</c> is returned.
    /// </summary>
    /// <returns>A newly allocated, empty fido_dev_t type</returns>
    [DllImport(DllName)]
    internal static extern fido_dev_t* fido_dev_new();

    /// <summary>
    /// Releases the memory backing *dev_p, where *dev_p must have been previously allocated by <see cref="fido_dev_new"/>.
    /// On return, *dev_p is set to <c>null</c>. Either dev_p or *dev_p may be <c>null</c>, in which case fido_dev_free() is a NOP.
    /// </summary>
    /// <param name="dev_p"></param>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void fido_dev_free(fido_dev_t** dev_p);

    /// <summary>
    /// Closes the device represented by dev. If dev is already closed, this is a NOP.
    /// </summary>
    /// <param name="dev">The device to close</param>
    /// <returns><see cref="CtapStatus.Ok"/> on success, anything else on failure</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int fido_dev_close(fido_dev_t* dev);

    /// <summary>
    /// Opens the device pointed to by path, where dev is a freshly allocated or otherwise closed fido_dev_t.
    /// </summary>
    /// <param name="dev">The device handle to store the result</param>
    /// <param name="path">The unique path to the device</param>
    /// <returns><see cref="CtapStatus.Ok"/> on success, anything else on failure</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int fido_dev_open(fido_dev_t* dev, string path);

    /// <summary>
    /// <para>Asks the FIDO device represented by dev for an assertion according to the following parameters defined in assert:
    /// relying party ID;
    /// client data hash;
    /// list of allowed credential IDs;
    /// user presence and user verification attributes.</para>
    /// <para>See fido_assert_set(3) for information on how these values are set.</para>
    /// <para>If a PIN is not needed to authenticate the request against dev, then pin may be NULL.
    /// Otherwise pin must point to a NUL-terminated UTF-8 string.</para>
    /// <para>Please note that fido_dev_get_assert() is synchronous and will block if necessary.</para>
    /// </summary>
    /// <param name="dev">The device to use for generation</param>
    /// <param name="assert">The assert to use for generation</param>
    /// <param name="pin">The pin of the device</param>
    /// <returns><see cref="CtapStatus.Ok"/> on success, anything else on failure</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int fido_dev_get_assert(fido_dev_t* dev, fido_assert_t* assert, string pin);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="dev"/> supports CTAP 2.1 Credential Management.
    /// </summary>
    /// <param name="dev">The device to check.</param>
    /// <returns><see langword="true"/> if <paramref name="dev"/> supports CTAP 2.1 Credential Management; otherwise, <see langword="false"/>.</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    //[return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool fido_dev_supports_credman(fido_dev_t* dev);
    #endregion

    #region FidoDeviceInfo
    /// <summary>
    /// Returns a pointer to a newly allocated, empty fido_dev_info_t type.
    /// If memory cannot be allocated, <c>null</c> is returned.
    /// </summary>
    /// <returns>A newly allocated, empty fido_dev_info_t type</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern fido_dev_info_t* fido_dev_info_new(IntPtr n);

    /// <summary>
    /// Returns a pointer to the path of di
    /// </summary>
    /// <param name="di">The object to act on</param>
    /// <returns>A pointer to the path of di</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ConstStringMarshaler))]
    internal static extern string fido_dev_info_path(fido_dev_info_t* di);

    /// <summary>
    /// Returns a pointer to the idx entry of di
    /// </summary>
    /// <param name="di">The object to act on</param>
    /// <param name="idx">The index of the object to retrieve</param>
    /// <returns>A pointer to the idx entry of di</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern fido_dev_info_t* fido_dev_info_ptr(fido_dev_info_t* di, IntPtr idx);

    /// <summary>
    /// Fills devlist with up to ilen FIDO devices found by the underlying operating system.
    /// Currently only USB HID devices are supported.
    /// The number of discovered devices is returned in olen, where olen is an addressable pointer.
    /// </summary>
    /// <param name="devlist">The devlist pointer to store the result in</param>
    /// <param name="ilen">The number of entries that the list can hold</param>
    /// <param name="olen">A pointer to where the number of entries that were written will be stored</param>
    /// <returns><see cref="CtapStatus.Ok"/> on success, anything else on failure</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fido_dev_info_manifest(fido_dev_info_t* devlist, IntPtr ilen, IntPtr* olen);

    /// <summary>
    /// Releases the memory backing *devlist_p, where *devlist_p must have been previously allocated by <see cref="fido_dev_info_new(IntPtr)"/>.
    /// On return, *devlist_p is set to <c>null</c>. Either devlist_p or *devlist_p may be <c>null</c>, in which case fido_dev_info_free() is a NOP.
    /// </summary>
    /// <param name="devlist_p"></param>
    /// <param name="n">The number of entries this object was allocated to hold</param>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void fido_dev_info_free(fido_dev_info_t** devlist_p, IntPtr n);

    /// <summary>
    /// Returns the vendor of the device
    /// </summary>
    /// <param name="di">The object to act on</param>
    /// <returns>The vendor of the device</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern short fido_dev_info_vendor(fido_dev_info_t* di);

    /// <summary>
    /// Returns the product of the device
    /// </summary>
    /// <param name="di">The object to act on</param>
    /// <returns>The product of the device</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern short fido_dev_info_product(fido_dev_info_t* di);

    /// <summary>
    /// Returns a pointer to the product string of di
    /// </summary>
    /// <param name="di">The object to act on</param>
    /// <returns>A pointer to the product string of di</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ConstStringMarshaler))]
    public static extern string fido_dev_info_product_string(fido_dev_info_t* di);

    /// <summary>
    /// Returns a pointer to the manufacturer string of di
    /// </summary>
    /// <param name="di">The object to act on</param>
    /// <returns>A pointer to the manufacturer string of di</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ConstStringMarshaler))]
    public static extern string fido_dev_info_manufacturer_string(fido_dev_info_t* di);
    #endregion

    #region FidoAssert
    /// <summary>
    /// Returns a pointer to a newly allocated, empty fido_assert_t type.
    /// If memory cannot be allocated, <c>null</c> is returned
    /// </summary>
    /// <returns>A newly allocated, empty fido_assert_t type</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern fido_assert_t* fido_assert_new();

    /// <summary>
    /// Releases the memory backing *assert_p, where *assert_p must have been previously allocated by <see cref="fido_assert_new"/>.
    /// On return, *assert_p is set to <c>null</c>. Either assert_p or *assert_p may be <c>null</c>, in which case fido_assert_free() is a NOP.
    /// </summary>
    /// <param name="assert_p">The object to free</param>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void fido_assert_free(fido_assert_t** assert_p);

    /// <summary>
    /// Adds ptr to the list of credentials allowed in assert, where ptr points to a credential ID of len bytes.
    /// A copy of ptr is made, and no references to the passed pointer are kept.
    /// If this call fails, the existing list of allowed credentials is preserved.
    /// </summary>
    /// <param name="assert">The object to act on</param>
    /// <param name="ptr">A pointer to the ID of the credential to allow</param>
    /// <param name="len">The length of the data inside of <paramref name="ptr"/></param>
    /// <returns></returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int fido_assert_allow_cred(fido_assert_t* assert, byte* ptr, IntPtr len);

    /// <summary>
    /// Set the client data hash of assert
    /// </summary>
    /// <param name="assert">The assertion object to act on</param>
    /// <param name="ptr">The client data hash to set</param>
    /// <param name="len">The length of the data in <paramref name="ptr"/></param>
    /// <returns><see cref="CtapStatus.Ok"/> on success, anything else on failure</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int fido_assert_set_clientdata_hash(fido_assert_t* assert, byte* ptr, IntPtr len);

    /// <summary>
    /// Sets the relying party of assert
    /// </summary>
    /// <param name="assert">The assertion object to act on</param>
    /// <param name="id">The ID of the the relying party</param>
    /// <returns><see cref="CtapStatus.Ok"/> on success, anything else on failure</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int fido_assert_set_rp(fido_assert_t* assert, string id);

    /// <summary>
    /// Returns the length of the authenticator data of statement idx in assert
    /// </summary>
    /// <param name="assert">The assertion object to act on</param>
    /// <param name="idx">The index to retrieve</param>
    /// <returns>The length of the authenticator data of statement idx in assert</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fido_assert_authdata_len(fido_assert_t* assert, IntPtr idx);

    /// <summary>
    /// Returns a pointer to the authenticator data of statement idx in assert
    /// </summary>
    /// <param name="assert">The assertion object to act on</param>
    /// <param name="idx">The index to retrieve</param>
    /// <returns>A pointer to the authenticator data of statement idx in assert</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* fido_assert_authdata_ptr(fido_assert_t* assert, IntPtr idx);

    /// <summary>
    /// Returns the length of the signature of statement idx in assert
    /// </summary>
    /// <param name="assert">The assertion object to act on</param>
    /// <param name="idx">The index to retrieve</param>
    /// <returns>The length of the signature of statement idx in assert</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fido_assert_sig_len(fido_assert_t* assert, IntPtr idx);

    /// <summary>
    /// Returns a pointer to the signature of statement idx in assert
    /// </summary>
    /// <param name="assert">The assertion object to act on</param>
    /// <param name="idx">The index to retrieve</param>
    /// <returns>A pointer to the signatureof statement idx in assert</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* fido_assert_sig_ptr(fido_assert_t* assert, IntPtr idx);

    /// <summary>
    /// Returns the length of the ID of statement idx in assert
    /// </summary>
    /// <param name="assert">The assertion object to act on</param>
    /// <param name="idx">The index to retrieve</param>
    /// <returns>The length of the ID of statement idx in assert</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fido_assert_id_len(fido_assert_t* assert, IntPtr idx);

    /// <summary>
    /// Returns a pointer to the ID of statement idx in assert.
    /// </summary>
    /// <param name="assert">The assertion object to act on.</param>
    /// <param name="idx">The index to retrieve.</param>
    /// <returns>A pointer to the ID of statement idx in assert.</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* fido_assert_id_ptr(fido_assert_t* assert, IntPtr idx);

    /// <summary>
    /// Returns the length of the client data hash of an assertion.
    /// </summary>
    /// <param name="assert">The assertion object to act on.</param>
    /// <returns>The length of the client data hash of statement idx of the assertion.</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fido_assert_clientdata_hash_len(fido_assert_t* assert);

    /// <summary>
    /// Returns a pointer to the client data hash of an assertion.
    /// </summary>
    /// <param name="assert">The assertion object to act on.</param>
    /// <returns>A pointer to the client data hash of the assertion.</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* fido_assert_clientdata_hash_ptr(fido_assert_t* assert);

    /// <summary>
    /// Returns the number of statements in assertion.
    /// </summary>
    /// <param name="assert">The assertion object to act on.</param>
    /// <returns>The number of statements in assertion.</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fido_assert_count(fido_assert_t* assert);
    #endregion
  }
}
