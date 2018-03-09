// Copyright © 2018, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MySql.Data.MySqlClient.Authentication
{
  internal class Sha256MemoryAuthenticationPlugin : MySqlAuthenticationPlugin
  {
    public override string PluginName => "SHA256_MEMORY";

    public byte[] GetClientHash(string data, byte[] nonce)
    {
      if (string.IsNullOrEmpty(data))
        return new byte[0];

      SHA256 sha = SHA256.Create();
      byte[] dataBytes = Encoding.UTF8.GetBytes(data);
      byte[] firstHash = sha.ComputeHash(dataBytes);
      byte[] secondHash = sha.ComputeHash(firstHash);
      byte[] thirdHash = new byte[secondHash.Length + nonce.Length];
      secondHash.CopyTo(thirdHash, 0);
      nonce.CopyTo(thirdHash, secondHash.Length);
      thirdHash = sha.ComputeHash(thirdHash);
      byte[] xor = GetXOr(thirdHash, firstHash);

      return Encoding.UTF8.GetBytes(BitConverter.ToString(xor).Replace("-", ""));
    }

    protected byte[] GetXOr(byte[] left, byte[] right)
    {
      if (left.Length != right.Length)
        throw new ArrayTypeMismatchException();

      byte[] result = new byte[left.Length];
      for (int i = 0; i < left.Length; i++)
      {
        result[i] = (byte)(left[i] ^ right[i]);
      }

      return result;
    }
  }
}
