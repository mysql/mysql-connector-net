// Copyright (c) 2020, 2022, Oracle and/or its affiliates.
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

using MySql.Data.Authentication.GSSAPI;
using MySql.Data.Common;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Authentication
{
  /// <summary>
  /// Allows connections to a user account set with the authentication_ldap_sasl authentication plugin.
  /// </summary>
  internal class MySqlSASLPlugin : MySqlAuthenticationPlugin
  {
    public override string PluginName => "mysql_ldap_sasl";
    string _mechanismName;
    internal static ScramBase scramMechanism;
    internal static GssapiMechanism gssapiMechanism;

    protected override void SetAuthData(byte[] data)
    {
      _mechanismName = Encoding.UTF8.GetString(data);

      switch (_mechanismName)
      {
        case "SCRAM-SHA-1":
          scramMechanism = new ScramSha1Mechanism(GetUsername(), GetMFAPassword(), Settings.Server);
          break;
        case "SCRAM-SHA-256":
          scramMechanism = new ScramSha256Mechanism(GetUsername(), GetMFAPassword(), Settings.Server);
          break;
        case "GSSAPI":
          if (Platform.IsWindows())
            throw new PlatformNotSupportedException(string.Format(Resources.AuthenticationPluginNotSupported, "GSSAPI/Kerberos"));
          gssapiMechanism = new GssapiMechanism(GetUsername(), GetMFAPassword());
          break;
      }
    }

    protected override Task<byte[]> MoreDataAsync(byte[] data, bool execAsync)
    {
      if (_mechanismName == "GSSAPI")
        return Task.FromResult<byte[]>(gssapiMechanism.Challenge(data));
      else
        return Task.FromResult<byte[]>(scramMechanism.Challenge(data));
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
      if (char.IsSurrogate(s, index))
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

    internal static string GetRandomBytes(int n)
    {
      var bytes = new byte[n];

      using (var rng = RandomNumberGenerator.Create())
        rng.GetBytes(bytes);

      return Convert.ToBase64String(bytes);
    }
  }
}