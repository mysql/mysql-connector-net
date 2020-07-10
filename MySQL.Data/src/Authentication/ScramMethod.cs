// Copyright (c) 2020 Oracle and/or its affiliates.
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
  internal class ScramMethod
  {
    private string userName, password, host;
    internal string _cnonce, client;
    byte[] salted, auth;
    internal AuthState _state;

    const string METHOD = "SCRAM-SHA-1";

    public ScramMethod(MySqlConnectionStringBuilder settings)
    {
      this.userName = settings.UserID;
      this.password = settings.Password;
      this.host = settings.Server;
    }

    internal byte[] NextCycle(byte[] input)
    {
      byte[] response = null;

      if (input == null || input.Length == 0)
        _state = AuthState.INITIAL;

      switch (_state)
      {
        case AuthState.INITIAL:
          response = ClientInitial();
          _state = AuthState.FINAL;
          break;
        case AuthState.FINAL:
          response = ProcessServerResponse(input);
          _state = AuthState.VALIDATE;
          break;
        case AuthState.VALIDATE:
          ValidateAuth(input);
          break;
      }

      return response;
    }

    /// <summary>
    /// Builds up the client-first message.
    /// </summary>
    /// <returns>An array of bytes containig the client-first message.</returns>
    internal byte[] ClientInitial()
    {
      _cnonce = _cnonce ?? GetRandomBytes(32);
      Normalize(ref userName);
      client = $"n={userName},r={_cnonce}";
      return Encoding.UTF8.GetBytes($"n,a={userName}," + client);
    }

    /// <summary>
    /// Processes the server response from the client-first message and 
    /// builds up the client-final message.
    /// </summary>
    /// <param name="data">Response from the server.</param>
    /// <returns>An array of bytes containing the client-final message.</returns>
    internal byte[] ProcessServerResponse(byte[] data)
    {
      string response = Encoding.UTF8.GetString(data, 0, data.Length);
      var tokens = new Dictionary<char, string>();

      foreach (string part in response.Split(','))
        if (part[1] == '=') tokens.Add(part[0], part.Substring(2));

      if (!tokens.TryGetValue('s', out string salt)) throw new MySqlException(string.Format(Resources.AuthenticationFailed, host, userName, METHOD, "salt is missing."));
      if (!tokens.TryGetValue('r', out string snonce)) throw new MySqlException(string.Format(Resources.AuthenticationFailed, host, userName, METHOD, "nonce is missing."));
      if (!tokens.TryGetValue('i', out string iterations)) throw new MySqlException(string.Format(Resources.AuthenticationFailed, host, userName, METHOD, "iteration count is missing."));
      if (!tokens['r'].StartsWith(_cnonce, StringComparison.Ordinal)) throw new MySqlException(string.Format(Resources.AuthenticationFailed, host, userName, METHOD, "invalid nonce."));
      if (!int.TryParse(iterations, out int count)) throw new MySqlException(string.Format(Resources.AuthenticationFailed, host, userName, METHOD, "invalid iteration count."));

      var password = Encoding.UTF8.GetBytes(this.password);
      salted = Hi(password, Convert.FromBase64String(salt), count);
      Array.Clear(password, 0, password.Length);

      var withoutProof = "c=" + Convert.ToBase64String(Encoding.ASCII.GetBytes($"n,a={userName},")) + ",r=" + snonce;
      auth = Encoding.UTF8.GetBytes(client + "," + response + "," + withoutProof);

      var ckey = HMAC(salted, Encoding.ASCII.GetBytes("Client Key"));
      Xor(ckey, HMAC(Hash(ckey), auth));

      return Encoding.UTF8.GetBytes(withoutProof + ",p=" + Convert.ToBase64String(ckey));
    }

    /// <summary>
    /// Validates the server response.
    /// </summary>
    /// <param name="data">Server-final message</param>
    internal void ValidateAuth(byte[] data)
    {
      string response = Encoding.UTF8.GetString(data, 0, data.Length);

      if (!response.StartsWith("v=", StringComparison.Ordinal))
        throw new MySqlException(string.Format(Resources.AuthenticationFailed, host, userName, METHOD, "challenge did not start with a signature."));

      var signature = Convert.FromBase64String(response.Substring(2));
      var skey = HMAC(salted, Encoding.ASCII.GetBytes("Server Key"));
      var calculated = HMAC(skey, auth);

      if (signature.Length != calculated.Length)
        throw new MySqlException(string.Format(Resources.AuthenticationFailed, host, userName, METHOD, "challenge contained a signature with an invalid length."));

      for (int i = 0; i < signature.Length; i++)
      {
        if (signature[i] != calculated[i])
          throw new MySqlException(string.Format(Resources.AuthenticationFailed, host, userName, METHOD, "challenge contained an invalid signature."));
      }
    }

    private string Normalize(ref string str)
    {
      var builder = new StringBuilder();
      var prepared = SaslPrep(str);

      for (int i = 0; i < prepared.Length; i++)
      {
        switch (prepared[i])
        {
          case ',': builder.Append("=2C"); break;
          case '=': builder.Append("=3D"); break;
          default:
            builder.Append(prepared[i]);
            break;
        }
      }

      return builder.ToString();
    }

    private static string GetRandomBytes(int n)
    {
      var bytes = new byte[n];

      using (var rng = RandomNumberGenerator.Create())
        rng.GetBytes(bytes);

      return Convert.ToBase64String(bytes);
    }

    /// <summary>
		/// Prepares the user name or password string.
		/// </summary>
		/// <param name="s">The string to prepare.</param>
		/// <returns>The prepared string.</returns>
		internal static string SaslPrep(string s)
    {
      if (s == null)
        throw new ArgumentNullException(nameof(s));

      if (s.Length == 0)
        return s;

      var builder = new StringBuilder(s.Length);
      for (int i = 0; i < s.Length; i++)
      {
        if (IsNonAsciiSpace(s[i]))
        {
          // non-ASII space characters [StringPrep, C.1.2] that can be
          // mapped to SPACE (U+0020).
          builder.Append(' ');
        }
        else if (IsCommonlyMappedToNothing(s[i]))
        {
          // the "commonly mapped to nothing" characters [StringPrep, B.1]
          // that can be mapped to nothing.
        }
        else if (char.IsControl(s[i]))
        {
          throw new ArgumentException("Control characters are prohibited.", nameof(s));
        }
        else if (IsProhibited(s, i))
        {
          throw new ArgumentException("One or more characters in the string are prohibited.", nameof(s));
        }
        else
        {
          builder.Append(s[i]);
        }
      }

      return builder.ToString().Normalize(NormalizationForm.FormKC);
    }

    /// <summary>
    /// Determines if the character is a non-ASCII space.
    /// </summary>
		/// <remarks>
		/// This list was obtained from http://tools.ietf.org/html/rfc3454#appendix-C.1.2
		/// </remarks>
    /// <returns><c>true</c> if the character is a non-ASCII space; otherwise, <c>false</c>.</returns>
    /// <param name="c">The character.</param>
    internal static bool IsNonAsciiSpace(char c)
    {
      switch (c)
      {
        case '\u00A0': // NO-BREAK SPACE
        case '\u1680': // OGHAM SPACE MARK
        case '\u2000': // EN QUAD
        case '\u2001': // EM QUAD
        case '\u2002': // EN SPACE
        case '\u2003': // EM SPACE
        case '\u2004': // THREE-PER-EM SPACE
        case '\u2005': // FOUR-PER-EM SPACE
        case '\u2006': // SIX-PER-EM SPACE
        case '\u2007': // FIGURE SPACE
        case '\u2008': // PUNCTUATION SPACE
        case '\u2009': // THIN SPACE
        case '\u200A': // HAIR SPACE
        case '\u200B': // ZERO WIDTH SPACE
        case '\u202F': // NARROW NO-BREAK SPACE
        case '\u205F': // MEDIUM MATHEMATICAL SPACE
        case '\u3000': // IDEOGRAPHIC SPACE
          return true;
        default:
          return false;
      }
    }

    /// <summary>
    /// Determines if the character is commonly mapped to nothing.
    /// </summary>
    /// <remarks>
    /// This list was obtained from http://tools.ietf.org/html/rfc3454#appendix-B.1
    /// </remarks>
    /// <returns><c>true</c> if the character is commonly mapped to nothing; otherwise, <c>false</c>.</returns>
    /// <param name="c">The character.</param>
    internal static bool IsCommonlyMappedToNothing(char c)
    {
      return c == '\u00AD' || c == '\u034F' || c == '\u1806' || c >= '\u180B' && c <= '\u180D' || c >= '\u200B' && c <= '\u200D'
                || c == '\u2060' || c >= '\uFE00' && c <= '\uFE0F' || c == '\uFEFF';
    }

    /// <summary>
    /// Determines if the character is prohibited.
    /// </summary>
    /// <remarks>
    /// This list was obtained from http://tools.ietf.org/html/rfc3454#appendix-C.3
    /// </remarks>
    /// <returns><c>true</c> if the character is prohibited; otherwise, <c>false</c>.</returns>
    /// <param name="s">The string.</param>
    /// <param name="index">The character index.</param>
    internal static bool IsProhibited(string s, int index)
    {
      int u = char.ConvertToUtf32(s, index);

      // Non-ASCII control characters: https://tools.ietf.org/html/rfc3454#appendix-C.2.2
      if ((u >= 0x0080 && u <= 0x009F) || u == 0x06DD || u == 0x070F || u == 0x180E || u == 0x200C || u == 0x200D ||
        u == 0x2028 || u == 0x2029 || (u >= 0x2060 && u <= 0x2063) || (u >= 0x206A && u <= 0x206F) || u == 0xFEFF ||
        (u >= 0xFFF9 && u <= 0xFFFC) || (u >= 0x1D173 && u <= 0x1D17A))
        return true;

      // Private Use characters: http://tools.ietf.org/html/rfc3454#appendix-C.3
      if ((u >= 0xE000 && u <= 0xF8FF) || (u >= 0xF0000 && u <= 0xFFFFD) || (u >= 0x100000 && u <= 0x10FFFD))
        return true;

      // Non-character code points: http://tools.ietf.org/html/rfc3454#appendix-C.4
      if ((u >= 0xFDD0 && u <= 0xFDEF) || (u >= 0xFFFE && u <= 0xFFFF) || (u >= 0x1FFFE && u <= 0x1FFFF) ||
        (u >= 0x2FFFE && u <= 0x2FFFF) || (u >= 0x3FFFE && u <= 0x3FFFF) || (u >= 0x4FFFE && u <= 0x4FFFF) ||
        (u >= 0x5FFFE && u <= 0x5FFFF) || (u >= 0x6FFFE && u <= 0x6FFFF) || (u >= 0x7FFFE && u <= 0x7FFFF) ||
        (u >= 0x8FFFE && u <= 0x8FFFF) || (u >= 0x9FFFE && u <= 0x9FFFF) || (u >= 0xAFFFE && u <= 0xAFFFF) ||
        (u >= 0xBFFFE && u <= 0xBFFFF) || (u >= 0xCFFFE && u <= 0xCFFFF) || (u >= 0xDFFFE && u <= 0xDFFFF) ||
        (u >= 0xEFFFE && u <= 0xEFFFF) || (u >= 0xFFFFE && u <= 0xFFFFF) || (u >= 0x10FFFE && u <= 0x10FFFF))
        return true;

      // Surrogate code points: http://tools.ietf.org/html/rfc3454#appendix-C.5
      if (char.IsSurrogate(s,index))
        return true;

      // Inappropriate for plain text: http://tools.ietf.org/html/rfc3454#appendix-C.6
      switch (u)
      {
        case 0xFFF9: // INTERLINEAR ANNOTATION ANCHOR
        case 0xFFFA: // INTERLINEAR ANNOTATION SEPARATOR
        case 0xFFFB: // INTERLINEAR ANNOTATION TERMINATOR
        case 0xFFFC: // OBJECT REPLACEMENT CHARACTER
        case 0xFFFD: // REPLACEMENT CHARACTER
          return true;
      }

      // Inappropriate for canonical representation: http://tools.ietf.org/html/rfc3454#appendix-C.7
      if (u >= 0x2FF0 && u <= 0x2FFB)
        return true;

      // Change display properties or are deprecated: http://tools.ietf.org/html/rfc3454#appendix-C.8
      switch (u)
      {
        case 0x0340: // COMBINING GRAVE TONE MARK
        case 0x0341: // COMBINING ACUTE TONE MARK
        case 0x200E: // LEFT-TO-RIGHT MARK
        case 0x200F: // RIGHT-TO-LEFT MARK
        case 0x202A: // LEFT-TO-RIGHT EMBEDDING
        case 0x202B: // RIGHT-TO-LEFT EMBEDDING
        case 0x202C: // POP DIRECTIONAL FORMATTING
        case 0x202D: // LEFT-TO-RIGHT OVERRIDE
        case 0x202E: // RIGHT-TO-LEFT OVERRIDE
        case 0x206A: // INHIBIT SYMMETRIC SWAPPING
        case 0x206B: // ACTIVATE SYMMETRIC SWAPPING
        case 0x206C: // INHIBIT ARABIC FORM SHAPING
        case 0x206D: // ACTIVATE ARABIC FORM SHAPING
        case 0x206E: // NATIONAL DIGIT SHAPES
        case 0x206F: // NOMINAL DIGIT SHAPES
          return true;
      }

      // Tagging characters: http://tools.ietf.org/html/rfc3454#appendix-C.9
      if (u == 0xE0001 || (u >= 0xE0020 && u <= 0xE007F))
        return true;

      return false;
    }

    // Hi(str, salt, i):
    //
    // U1   := HMACSHA1(str, salt + INT(1))
    // U2   := HMACSHA1(str, U1)
    // ...
    // Ui-1 := HMACSHA1(str, Ui-2)
    // Ui   := HMACSHA1(str, Ui-1)
    //
    // Hi := U1 XOR U2 XOR ... XOR Ui
    //
    // where "i" is the iteration count, "+" is the string concatenation
    // operator, and INT(g) is a 4-octet encoding of the integer g, most
    // significant octet first.
    //
    // Hi() is, essentially, PBKDF2 [RFC2898] with HMACSHA1() as the
    // pseudorandom function (PRF) and with dkLen == output length of
    // HMACSHA1() == output length of H().
    private byte[] Hi(byte[] str, byte[] salt, int count)
    {
      using (var hmac = CreateHMACSHA1(str))
      {
        var salt1 = new byte[salt.Length + 4];
        byte[] hi, u1;

        Buffer.BlockCopy(salt, 0, salt1, 0, salt.Length);
        salt1[salt1.Length - 1] = (byte)1;

        hi = u1 = hmac.ComputeHash(salt1);

        for (int i = 1; i < count; i++)
        {
          var u2 = hmac.ComputeHash(u1);
          Xor(hi, u2);
          u1 = u2;
        }

        return hi;
      }
    }

    /// <summary>
    /// Creates the HMAC SHA1 context.
    /// </summary>
    /// <returns>The HMAC context.</returns>
    /// <param name="key">The secret key.</param>
    private KeyedHashAlgorithm CreateHMACSHA1(byte[] key)
    {
      return new HMACSHA1(key);
    }

    /// <summary>
    /// Applies the exclusive-or operation to combine two octet strings.
    /// </summary>
    /// <param name="a">The alpha component.</param>
    /// <param name="b">The blue component.</param>
    private static void Xor(byte[] a, byte[] b)
    {
      for (int i = 0; i < a.Length; i++)
        a[i] = (byte)(a[i] ^ b[i]);
    }

    /// <summary>
    /// Apply the HMAC keyed algorithm.
    /// </summary>
    /// <returns>The results of the HMAC keyed algorithm.</returns>
    /// <param name="key">The key.</param>
    /// <param name="str">The string.</param>
    private byte[] HMAC(byte[] key, byte[] str)
    {
      using (var hmac = CreateHMACSHA1(key))
        return hmac.ComputeHash(str);
    }

    /// <summary>
    /// Applies the cryptographic hash function.
    /// </summary>
    /// <returns>The results of the hash.</returns>
    /// <param name="str">The string.</param>
    private byte[] Hash(byte[] str)
    {
      using (var sha1 = SHA1.Create())
        return sha1.ComputeHash(str);
    }
  }

  /// <summary>
  /// Defines the state of the authentication process.
  /// </summary>
  internal enum AuthState
  {
    INITIAL,
    FINAL,
    VALIDATE
  }
}