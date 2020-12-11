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

namespace MySql.Data.Authentication.GSSAPI
{
  [Flags]
  internal enum GssContextFlags
  {
    /// <summary>
    /// No flags provided
    /// </summary>
    None = 0,
    /// <summary>
    /// Delegates credentials to a remote peer. Do not delegate the credentials if the value is false.
    /// </summary>
    Deleg = 1,
    /// <summary>
    /// Requests that the peer authenticate itself. If false, authenticate to the remote peer only.
    /// </summary>
    Mutual = 2,
    /// <summary>
    /// Enables replay detection for messages protected with gss_wrap(3GSS) or gss_get_mic(3GSS). Do not attempt to detect replayed messages if false.
    /// </summary>
    Replay = 4,
    /// <summary>
    /// Enables detection of out-of-sequence protected messages. Do not attempt to detect out-of-sequence messages if false.
    /// </summary>
    Sequence = 8,
    /// <summary>
    /// Requests that confidential service be made available by means of gss_wrap(3GSS). If false, no per-message confidential service is required.
    /// </summary>
    Conf = 16,
    /// <summary>
    /// Requests that integrity service be made available by means of gss_wrap(3GSS) or gss_get_mic(3GSS). If false, no per-message integrity service is required.
    /// </summary>
    Integ = 32,
    /// <summary>
    /// Does not reveal the initiator's identify to the acceptor. Otherwise, authenticate normally.
    /// </summary>
    Anon = 64,
    /// <summary>
    /// (Returned only) If true, the protection services specified by the states of GSS_C_CONF_FLAG and GSS_C_INTEG_FLAG are available 
    /// if the accompanying major status return value is either GSS_S_COMPLETE or GSS_S_CONTINUE_NEEDED. If false, the protection services are available 
    /// only if the accompanying major status return value is GSS_S_COMPLETE.
    /// </summary>
    ProtReady = 128,
    /// <summary>
    /// (Returned only) If true, the resultant security context may be transferred to other processes by means of a call to gss_export_sec_context(3GSS). If false, the security context cannot be transferred.
    /// </summary>
    Trans = 256,
    DelegPolicy = 32768
  }
}