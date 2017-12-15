// Copyright (c) 2004, 2016, Oracle and/or its affiliates. All rights reserved.
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
using MySql.Data.MySqlClient;

namespace MySql.Data.Common
{
  /// <summary>
  /// Summary description for Version.
  /// </summary>
  internal struct DBVersion
  {
    private readonly string _srcString;

    public DBVersion(string s, int major, int minor, int build)
    {
      Major = major;
      Minor = minor;
      Build = build;
      _srcString = s;
      IsEnterprise = s.ToLowerInvariant().Contains("-enterprise-");
    }

    public int Major { get; }

    public int Minor { get; }

    public int Build { get; }

    public bool IsEnterprise
    {
      get; private set;
    }

    public static DBVersion Parse(string versionString)
    {
      int start = 0;
      int index = versionString.IndexOf('.', start);
      if (index == -1)
        throw new MySqlException(Resources.BadVersionFormat);
      string val = versionString.Substring(start, index - start).Trim();
      int major = Convert.ToInt32(val, System.Globalization.NumberFormatInfo.InvariantInfo);

      start = index + 1;
      index = versionString.IndexOf('.', start);
      if (index == -1)
        throw new MySqlException(Resources.BadVersionFormat);
      val = versionString.Substring(start, index - start).Trim();
      int minor = Convert.ToInt32(val, System.Globalization.NumberFormatInfo.InvariantInfo);

      start = index + 1;
      int i = start;
      while (i < versionString.Length && Char.IsDigit(versionString, i))
        i++;
      val = versionString.Substring(start, i - start).Trim();
      int build = Convert.ToInt32(val, System.Globalization.NumberFormatInfo.InvariantInfo);

      return new DBVersion(versionString, major, minor, build);
    }

    public bool isAtLeast(int majorNum, int minorNum, int buildNum)
    {
      if (Major > majorNum) return true;
      if (Major == majorNum && Minor > minorNum) return true;
      if (Major == majorNum && Minor == minorNum && Build >= buildNum) return true;
      return false;
    }

    public override string ToString()
    {
      return _srcString;
    }

  }
}
