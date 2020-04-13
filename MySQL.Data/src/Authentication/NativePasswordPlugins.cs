// Copyright (c) 2012, 2019, Oracle and/or its affiliates. All rights reserved.
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
using System.Security.Cryptography;
using System.Text;

namespace MySql.Data.MySqlClient.Authentication
{
  /// <summary>
  /// Allows connections to a user account set with the mysql_native_password authentication plugin.
  /// </summary>
  public class MySqlNativePasswordPlugin : MySqlAuthenticationPlugin
  {
    public override string PluginName => "mysql_native_password";

    protected override void SetAuthData(byte[] data)
    {
      // if the data given to us is a null terminated string, we need to trim off the trailing zero
      if (data[data.Length - 1] == 0)
      {
        byte[] b = new byte[data.Length - 1];
        Buffer.BlockCopy(data, 0, b, 0, data.Length - 1);
        base.SetAuthData(b);
      }
      else
        base.SetAuthData(data);
    }

    protected override byte[] MoreData(byte[] data)
    {
      byte[] passBytes = (GetPassword() ?? new byte[1]) as byte[];
      byte[] buffer = new byte[passBytes.Length - 1];
      Array.Copy(passBytes, 1, buffer, 0, passBytes.Length - 1);
      return buffer;
    }

    public override object GetPassword()
    {
      byte[] bytes = Get411Password(Settings.Password, AuthenticationData);
      if (bytes != null && bytes.Length == 1 && bytes[0] == 0) return null;
      return bytes;
    }

    /// <summary>
    /// Returns a byte array containing the proper encryption of the 
    /// given password/seed according to the new 4.1.1 authentication scheme.
    /// </summary>
    /// <param name="password"></param>
    /// <param name="seedBytes"></param>
    /// <returns></returns>
    protected byte[] Get411Password(string password, byte[] seedBytes)
    {
      // if we have no password, then we just return 1 zero byte
      if (password.Length == 0) return new byte[1];
      //SHA1 sha = new SHA1CryptoServiceProvider();
      SHA1 sha = SHA1.Create();
      byte[] firstHash = null;
      try
      {
        firstHash = sha.ComputeHash(this.Encoding.GetBytes(password));
      }
      catch (NullReferenceException)
      {
        firstHash = sha.ComputeHash(Encoding.Default.GetBytes(password));
      }

      byte[] secondHash = sha.ComputeHash(firstHash);

      byte[] input = new byte[seedBytes.Length + secondHash.Length];
      Array.Copy(seedBytes, 0, input, 0, seedBytes.Length);
      Array.Copy(secondHash, 0, input, seedBytes.Length, secondHash.Length);
      byte[] thirdHash = sha.ComputeHash(input);

      byte[] finalHash = new byte[thirdHash.Length + 1];
      finalHash[0] = 0x14;
      Array.Copy(thirdHash, 0, finalHash, 1, thirdHash.Length);

      for (int i = 1; i < finalHash.Length; i++)
        finalHash[i] = (byte)(finalHash[i] ^ firstHash[i - 1]);
      return finalHash;
    }
  }
}
