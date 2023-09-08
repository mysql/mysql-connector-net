// Copyright (c) 2021, 2023, Oracle and/or its affiliates.
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

using MySql.Data.Common;
using MySql.Data.MySqlClient;
using System;

namespace MySql.EntityFrameworkCore.Basic.Tests.Utils
{
  public class TestUtils
  {
    private static Version? _version = null;

    public static Version Version
    {
      get
      {
        if (_version == null)
        {
          MySQLTestStore.SslMode = false;
          using (MySqlConnection conn = new MySqlConnection(MySQLTestStore.BaseConnectionString))
          {
            conn.Open();
            _version = new Version(conn.driver.Version.Major
              , conn.driver.Version.Minor
              , conn.driver.Version.Build);
          }
        }
        return _version;
      }
    }
    public static bool IsAtLeast(int major, int minor, int build)
    {
      if (DBVersion.Parse(Version.ToString()).isAtLeast(major, minor, build))
      {
        return true;
      }
      return false;
    }
  }
}
