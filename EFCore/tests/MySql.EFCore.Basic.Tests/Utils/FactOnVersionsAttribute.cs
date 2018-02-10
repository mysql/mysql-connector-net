// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.EntityFrameworkCore.Tests;
using MySql.Data.MySqlClient;
using System;
using System.Text.RegularExpressions;
using Xunit;

namespace MySql.Data.EntityFrameworkCore.Tests
{
  public class FactOnVersionsAttribute : FactAttribute
  {
    private static Version _version = null;
    public static Version Version
    {
      get
      {
        if (_version == null)
        {
          using (MySqlConnection conn = new MySqlConnection(MySQLTestStore.baseConnectionString))
          {
            conn.Open();
            _version = new Version(conn.driver.Version.Major
              ,conn.driver.Version.Minor
              ,conn.driver.Version.Build);
          }
        }
        return _version;
      }
    }

    public FactOnVersionsAttribute(string initial, string final) : base()
    {
      try
      {
        Version initialVersion = new Version(initial ?? "0.0.0");
        Version finalVersion = new Version(final ?? "99.99.99");
        if (initialVersion <= Version && Version <= finalVersion)
          base.Skip = null;
        else
        {
          string message = string.Empty;
          if (initial == null)
            message = $"{final} or lower";
          else if (final == null)
            message = $"{initial} or above";
          else
            message = $"between {initial} and {final}";

          base.Skip = "Skipping test because MySql Server is not " + message;
        }
      }
      catch(Exception ex)
      {
        base.Skip = "FactOnVersionsAttribute error: " + ex.Message;
      }
    }
  }
}