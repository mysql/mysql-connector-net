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

namespace MySql.Data.Authentication.SSPI
{
  /// <summary>
  /// SSPI constants
  /// </summary>
  internal static class Const
  {
    internal const int SECPKG_CRED_BOTH = 0x00000003;
    internal const int SECURITY_NETWORK_DREP = 0;
    internal const int SECURITY_NATIVE_DREP = 0x10;
    internal const int SECPKG_CRED_INBOUND = 1;
    internal const int MAX_TOKEN_SIZE = 12288;
    internal const int SECPKG_ATTR_SIZES = 0;
    internal const int STANDARD_CONTEXT_ATTRIBUTES = 0;
    internal const uint SEC_WINNT_AUTH_IDENTITY_UNICODE = 2;
  }

  internal enum SecStatus : uint
  {
    SEC_E_OK = 0,
    SEC_I_CONTINUE_NEEDED = 0x90312,
    SEC_I_COMPLETE_NEEDED = 0x1013,
    SEC_I_COMPLETE_AND_CONTINUE = 0x1014,
  }
}
