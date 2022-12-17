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

namespace MySql.Data.Common.DnsClient
{
  /// <summary>
  /// DNS record type.
  /// </summary>
  internal enum RecordType
  {
    SRV = 33
  }

  // RFC 1035 (https://tools.ietf.org/html/rfc1035#section-3.2.4)
  // 3.2.4. CLASS values

  /// <summary>
  /// CLASS fields appear in resource records.
  /// </summary>
  internal enum QueryClass
  {
    /// <summary>
    /// The Internet.
    /// </summary>
    IN = 1
  }

  // RFC 1035 (https://tools.ietf.org/html/rfc1035#section-3.2.3)

  /// <summary>
  /// DNS question type.
  /// QueryType are a superset of RecordType.
  /// </summary>
  internal enum QueryType
  {
    /// <summary>
    /// A resource record which specifies the location of the server(s) for a specific protocol and domain.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc2782">RFC 2782</seealso>
    /// <seealso cref="DnsSrvRecord"/>
    SRV = RecordType.SRV
  }

  /// <summary>
  /// DNS Record OpCode.
  /// A four bit field that specifies kind of query in this message.
  /// This value is set by the originator of a query and copied into the response.
  /// </summary>
  internal enum OPCode
  {
    /// <summary>
    /// A standard query (QUERY).
    /// </summary>
    Query = 0,
    /// <summary>
    /// Retired IQUERY code.
    /// </summary>
    IQUERY = 1,
    /// <summary>
    /// A server status request (STATUS).
    /// </summary>
    Status = 2,
    /// <summary>
    /// Notify OpCode.
    /// </summary>
    Notify = 4,
    /// <summary>
    /// Update OpCode.
    /// </summary>
    Update = 5
  }
}
