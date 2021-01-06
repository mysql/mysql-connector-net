// Copyright (c) 2021 Oracle and/or its affiliates.
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
using System;
using System.Runtime.InteropServices;

namespace MySql.Data.Authentication.GSSAPI
{
  /// <summary>
  /// GSS API constants
  /// </summary>
  internal static class Const
  {
    #region GSS Constants
    internal const uint GSS_S_COMPLETE = 0U;
    internal const uint GSS_S_CONTINUE_NEEDED = 1U;

    internal const uint GSS_C_INDEFINITE = 0xFFFFFFFFU;
    internal static IntPtr GSS_C_NO_BUFFER = IntPtr.Zero;
    internal static IntPtr GSS_C_NO_CREDENTIAL = IntPtr.Zero;
    internal static IntPtr GSS_C_NO_NAME = IntPtr.Zero;

    internal static GssOidDescStruct GSS_C_NO_OID = default(GssOidDescStruct);
    internal static GssOidSetStruct GSS_C_NO_OID_SET = default(GssOidSetStruct);

    internal static uint GSS_C_QOP_DEFAULT = 0U;
    #endregion

    #region GSS OIDs
    /// <summary>
    /// GSS_C_NT_HOSTBASED_SERVICE (1.2.840.113554.1.2.1.4)
    /// </summary>
    private static readonly byte[] GssNtHostBasedServiceOid = { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x12, 0x01, 0x02, 0x01, 0x04 };

    internal static GssOidDescStruct GssNtHostBasedService = new GssOidDescStruct
    {
      length = (uint)GssNtHostBasedServiceOid.Length,
      elements = GCHandle.Alloc(GssNtHostBasedServiceOid, GCHandleType.Pinned).AddrOfPinnedObject()
    };

    /// <summary>
    /// GSS_KRB5_NT_PRINCIPAL_NAME (1.2.840.113554.1.2.2.1)
    /// </summary>
    private static readonly byte[] GssNtPrincipalNameOid = { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x12, 0x01, 0x02, 0x02, 0x01 };

    internal static GssOidDescStruct GssNtPrincipalName = new GssOidDescStruct
    {
      length = (uint)GssNtPrincipalNameOid.Length,
      elements = GCHandle.Alloc(GssNtPrincipalNameOid, GCHandleType.Pinned).AddrOfPinnedObject()
    };

    /// <summary>
    /// GSS_C_NT_USER_NAME (1.2.840.113554.1.2.1.1)
    /// </summary>
    private static readonly byte[] GssNtUserNameOid = { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x12, 0x01, 0x02, 0x01, 0x01 };

    internal static GssOidDescStruct GssNtUserName = new GssOidDescStruct
    {
      length = (uint)GssNtUserNameOid.Length,
      elements = GCHandle.Alloc(GssNtUserNameOid, GCHandleType.Pinned).AddrOfPinnedObject()
    };

    /// <summary>
    /// GSS_KRB5_MECH_OID_DESC (1.2.840.113554.1.2.2)
    /// </summary>
    private static readonly byte[] GssKrb5MechOid = { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x12, 0x01, 0x02, 0x02 };

    internal static GssOidDescStruct GssKrb5MechOidDesc = new GssOidDescStruct
    {
      length = (uint)GssKrb5MechOid.Length,
      elements = GCHandle.Alloc(GssKrb5MechOid, GCHandleType.Pinned).AddrOfPinnedObject()
    };

    /// <summary>
    /// GSS_KRB5_MECH_OID_DESC Set
    /// </summary>
    internal static GssOidSetStruct GssKrb5MechOidSet = new GssOidSetStruct
    {
      count = 1,
      elements = GCHandle.Alloc(GssKrb5MechOidDesc, GCHandleType.Pinned).AddrOfPinnedObject()
    };
    #endregion
  }
}