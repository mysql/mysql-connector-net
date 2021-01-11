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

using System.Security.Cryptography;

namespace MySql.Data.MySqlClient.Authentication
{
  /// <summary>
  /// The SCRAM-SHA-256 SASL mechanism.
  /// </summary>
  /// <remarks>
  /// A salted challenge/response SASL mechanism that uses the HMAC SHA-256 algorithm.
  /// </remarks>
  internal class ScramSha256Mechanism : ScramBase
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ScramSha256Mechanism"/> class.
    /// </summary>
    /// <remarks>
    /// Creates a new SCRAM-SHA-256 SASL context.
    /// </remarks>
    /// <param name="username">The user name.</param>
    /// <param name="password">The password.</param>
    /// <param name="host">The host.</param>
    internal ScramSha256Mechanism(string username, string password, string host) : base(username, password, host) { }

    /// <summary>
    /// Gets the name of the method.
    /// </summary>
    internal override string MechanismName
    {
      get { return "SCRAM-SHA-256"; }
    }

    protected override KeyedHashAlgorithm CreateHMAC(byte[] key)
    {
      return new HMACSHA256(key);
    }

    protected override byte[] Hash(byte[] str)
    {
      using (var sha256 = SHA256.Create())
        return sha256.ComputeHash(str);
    }
  }
}