// Copyright (c) 2004, 2019, Oracle and/or its affiliates. All rights reserved.
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

namespace MySql.Data.MySqlClient
{
  /// <summary>
  /// Summary description for Crypt.
  /// </summary>
  internal class Crypt
  {
    /// <summary>
    /// Simple XOR scramble
    /// </summary>
    /// <param name="from">Source array</param>
    /// <param name="fromIndex">Index inside source array</param>
    /// <param name="to">Destination array</param>
    /// <param name="toIndex">Index inside destination array</param>
    /// <param name="password">Password used to xor the bits</param>
    /// <param name="length">Number of bytes to scramble</param>
    private static void XorScramble(byte[] from, int fromIndex, byte[] to, int toIndex,
                    byte[] password, int length)
    {
      // make sure we were called properly
      if (fromIndex < 0 || fromIndex >= from.Length)
        throw new ArgumentException(Resources.IndexMustBeValid, nameof(fromIndex));
      if ((fromIndex + length) > from.Length)
        throw new ArgumentException(Resources.FromAndLengthTooBig, nameof(fromIndex));
      if (from == null)
        throw new ArgumentException(Resources.BufferCannotBeNull, nameof(@from));
      if (to == null)
        throw new ArgumentException(Resources.BufferCannotBeNull, nameof(to));
      if (toIndex < 0 || toIndex >= to.Length)
        throw new ArgumentException(Resources.IndexMustBeValid, nameof(toIndex));
      if ((toIndex + length) > to.Length)
        throw new ArgumentException(Resources.IndexAndLengthTooBig, nameof(toIndex));
      if (password == null || password.Length < length)
        throw new ArgumentException(Resources.PasswordMustHaveLegalChars, nameof(password));
      if (length < 0)
        throw new ArgumentException(Resources.ParameterCannotBeNegative, nameof(length));

      // now perform the work
      for (int i = 0; i < length; i++)
        to[toIndex++] = (byte)(from[fromIndex++] ^ password[i]);
    }

    /// <summary>
    /// Returns a byte array containing the proper encryption of the 
    /// given password/seed according to the new 4.1.1 authentication scheme.
    /// </summary>
    /// <param name="password"></param>
    /// <param name="seed"></param>
    /// <returns></returns>
    public static byte[] Get411Password(string password, string seed)
    {
      // if we have no password, then we just return 2 zero bytes
      if (password.Length == 0) return new byte[1];

      SHA1 sha = SHA1.Create();

      byte[] firstHash = sha.ComputeHash(Encoding.Default.GetBytes(password));
      byte[] secondHash = sha.ComputeHash(firstHash);
      byte[] seedBytes = Encoding.Default.GetBytes(seed);

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
      //byte[] buffer = new byte[finalHash.Length - 1];
      //Array.Copy(finalHash, 1, buffer, 0, finalHash.Length - 1);
      //return buffer;
    }

    private static double rand(ref long seed1, ref long seed2, long max)
    {
      seed1 = (seed1 * 3) + seed2;
      seed1 %= max;
      seed2 = (seed1 + seed2 + 33) % max;
      return (seed1 / (double)max);
    }

    /// <summary>
    /// Encrypts a password using the MySql encryption scheme
    /// </summary>
    /// <param name="password">The password to encrypt</param>
    /// <param name="seed">The encryption seed the server gave us</param>
    /// <param name="new_ver">Indicates if we should use the old or new encryption scheme</param>
    /// <returns></returns>
    public static String EncryptPassword(String password, String seed, bool new_ver)
    {
      long max = 0x3fffffff;
      if (!new_ver)
        max = 0x01FFFFFF;
      if (string.IsNullOrEmpty(password))
        return password;

      long[] hash_seed = Hash(seed);
      long[] hash_pass = Hash(password);

      long seed1 = (hash_seed[0] ^ hash_pass[0]) % max;
      long seed2 = (hash_seed[1] ^ hash_pass[1]) % max;
      if (!new_ver)
        seed2 = seed1 / 2;

      char[] scrambled = new char[seed.Length];
      for (int x = 0; x < seed.Length; x++)
      {
        double r = rand(ref seed1, ref seed2, max);
        scrambled[x] = (char)(Math.Floor(r * 31) + 64);
      }

      if (new_ver)
      {
        /* Make it harder to break */
        char extra = (char)Math.Floor(rand(ref seed1, ref seed2, max) * 31);
        for (int x = 0; x < scrambled.Length; x++)
          scrambled[x] ^= extra;
      }

      return new string(scrambled);
    }

    /// <summary>
    /// Hashes a password using the algorithm from Monty's code.
    /// The first element in the return is the result of the "old" hash.
    /// The second element is the rest of the "new" hash.
    /// </summary>
    /// <param name="P">Password to be hashed</param>
    /// <returns>Two element array containing the hashed values</returns>
    private static long[] Hash(String P)
    {
      long val1 = 1345345333;
      long val2 = 0x12345671;
      long inc = 7;

      for (int i = 0; i < P.Length; i++)
      {
        if (P[i] == ' ' || P[i] == '\t') continue;
        long temp = (0xff & P[i]);
        val1 ^= (((val1 & 63) + inc) * temp) + (val1 << 8);
        val2 += (val2 << 8) ^ val1;
        inc += temp;
      }

      long[] hash = new long[2];
      hash[0] = val1 & 0x7fffffff;
      hash[1] = val2 & 0x7fffffff;
      return hash;
    }
  }
}