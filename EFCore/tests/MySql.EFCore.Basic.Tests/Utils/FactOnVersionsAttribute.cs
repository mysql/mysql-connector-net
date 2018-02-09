// Copyright © 2017 Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
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