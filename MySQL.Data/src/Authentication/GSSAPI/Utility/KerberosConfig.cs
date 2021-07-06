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

using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MySql.Data.Authentication.GSSAPI.Utility
{
  /// <summary>
  /// Gets the Kerberos configuration from the "krb5.conf/krb5.ini" file
  /// </summary>
  internal class KerberosConfig
  {
    private const string KRBCONFIG_LINUX = @"/etc/krb5.conf";

    private const char COMMENT_HASH = '#';
    private const char COMMENT_SEMI = ';';
    private const char SECTION_OPEN = '[';
    private const char SECTION_CLOSE = ']';
    private const char GROUP_OPEN = '{';
    private const char GROUP_CLOSE = '}';
    private static readonly char[] Equal = new char[] { '=' };

    static List<KeyValuePair<string, string>> LibDefaults = new List<KeyValuePair<string, string>>();
    static List<KeyValuePair<string, string>> Realms = new List<KeyValuePair<string, string>>();
    static List<KeyValuePair<string, string>> AppDefaults = new List<KeyValuePair<string, string>>();

    private const string SERVICE_NAME = "ldap/";
    private static string Domain;

    private static void ReadConfig()
    {
      StringReader reader;

      // Tries to get Kerberos configuration file path from environment variables, otherwise
      // set the default path.
      string krbConfigPath = Environment.GetEnvironmentVariable("KRB5_CONFIG");

      if (string.IsNullOrEmpty(krbConfigPath))
        krbConfigPath = KRBCONFIG_LINUX;

      if (File.Exists(krbConfigPath))
      {
        try { reader = new StringReader(File.ReadAllText(krbConfigPath)); }
        catch { throw new MySqlException("Unable to read Kerberos configuration file."); }
      }
      else
        throw new MySqlException("Kerberos configuration file is missing.");

      while (TryReadLine(reader, out string currentLine))
      {
        if (CanSkip(currentLine))
          continue;

        while (IsSectionLine(currentLine))
          currentLine = ReadSection(currentLine, reader);
      }
    }

    internal static string GetServicePrincipalName(string username)
    {
      Domain = SplitUserNameDomain(username);
      ReadConfig();

      string kdc;

      // Try to obtain the LDAP server hostname from AppDefaults section first then on the Realms section
      try { kdc = AppDefaults.First(e => e.Key == "ldap_server_host").Value.Trim().Replace("\"", string.Empty).ToLowerInvariant(); }
      catch { kdc = Realms.First(e => e.Key == "kdc").Value.Trim().ToLowerInvariant(); }

      if (kdc.IndexOf('.') > 0)
        kdc = kdc.Substring(0, kdc.IndexOf('.')).ToLowerInvariant();

      return SERVICE_NAME + kdc;
    }

    private static string SplitUserNameDomain(string original)
    {
      if (string.IsNullOrWhiteSpace(original))
        throw new InvalidOperationException("Username cannot be an empty string.");

      var index = original.IndexOf('@');
      string domain;

      if (index == 0)
        throw new InvalidOperationException("Domain cannot be an empty string.");
      else
        domain = original.Substring(index + 1, original.Length - index - 1);

      return domain;
    }

    #region ReadConfigFile
    private static bool TryReadLine(StringReader reader, out string currentLine)
    {
      currentLine = reader.ReadLine();

      if (currentLine == null)
        return false;

      for (var i = 0; i < currentLine.Length; i++)
      {
        if (IsComment(currentLine[i]))
        {
          currentLine = currentLine.Substring(0, i);
          break;
        }
      }

      currentLine = currentLine.Trim().Trim('\uFEFF', '\u200B');

      return true;
    }

    private static bool IsComment(char ch)
    {
      return ch.Equals(COMMENT_HASH) || ch.Equals(COMMENT_SEMI);
    }

    private static bool CanSkip(string trimmed)
    {
      if (trimmed.Length == 0)
        return true;

      return IsComment(trimmed[0]);
    }

    private static bool IsSectionLine(string currentLine)
    {
      if (string.IsNullOrWhiteSpace(currentLine))
        return false;

      return currentLine[0] == SECTION_OPEN && currentLine[currentLine.Length - 1] == SECTION_CLOSE;
    }

    private static string ReadSection(string currentLine, StringReader reader)
    {
      string name = currentLine.Substring(1, currentLine.Length - 2).ToLower();

      while (TryReadLine(reader, out currentLine))
      {
        if (CanSkip(currentLine))
          continue;

        if (currentLine[0] == SECTION_OPEN)
          break;

        switch (name)
        {
          case "libdefaults":
            ReadValue(currentLine, reader, LibDefaults);
            break;
          case "realms":
            ReadValue(currentLine, reader, Realms);
            break;
          case "appdefaults":
            ReadValue(currentLine, reader, AppDefaults);
            break;
        }
      }

      return currentLine;
    }

    private static void ReadValue(string currentLine, StringReader reader, List<KeyValuePair<string, string>> section)
    {
      if (currentLine.IndexOf(GROUP_OPEN) >= 0)
        ReadValues(currentLine, reader, section);
      else
      {
        var split = currentLine.Split(Equal, 2);

        if (split.Length == 2)
          section.Add(new KeyValuePair<string, string>(split[0].Trim(), split[1].Trim()));
      }
    }

    private static void ReadValues(string currentLine, StringReader reader, List<KeyValuePair<string, string>> section)
    {
      var split = currentLine.Split(Equal, 2);

      if (split.Length != 2)
        return;

      while (TryReadLine(reader, out currentLine))
      {
        if (CanSkip(currentLine))
          continue;

        if (currentLine[0] == GROUP_CLOSE)
          break;

        if (split[0].Trim().Equals(Domain, StringComparison.OrdinalIgnoreCase)
          || split[0].Trim().Equals("mysql", StringComparison.OrdinalIgnoreCase))
          ReadValue(currentLine, reader, section);
      }
    }
    #endregion
  }
}