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

using System;
using System.Runtime.InteropServices;
using MySql.Data.Authentication.GSSAPI.Native;

namespace MySql.Data.Authentication.GSSAPI.Utility
{
  internal static class ExceptionMessages
  {
    private const int GssCGssCode = 1;
    private const int GssCMechCode = 2;

    internal static string FormatGssMessage(string message, uint majorStatus, uint minorStatus, GssOidDescStruct oid)
    {
      var majorMessage = TranslateMajorStatusCode(majorStatus);
      var minorMessage = TranslateMinorStatusCode(minorStatus, oid);
      return $"{message}{Environment.NewLine}" +
             $"GSS Major: ({majorStatus:x8}) {majorMessage}{Environment.NewLine}" +
             $"GSS Minor: ({minorStatus:x8}) {minorMessage}";
    }

    private static string TranslateMajorStatusCode(uint status)
    {
      var context = IntPtr.Zero;
      var buffer = default(GssBufferDescStruct);
      var oid = default(GssOidDescStruct);

      NativeMethods.gss_display_status(out var _, status, GssCGssCode, ref oid, ref context, ref buffer);
      return buffer.value == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(buffer.value);
    }

    private static string TranslateMinorStatusCode(uint status, GssOidDescStruct oid)
    {
      var context = IntPtr.Zero;
      var buffer = default(GssBufferDescStruct);

      NativeMethods.gss_display_status(out var _, status, GssCMechCode, ref oid, ref context, ref buffer);
      return buffer.value == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(buffer.value);
    }
  }
}