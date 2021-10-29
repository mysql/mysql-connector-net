// Copyright (c) 2013, 2021, Oracle and/or its affiliates.
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
using System.Data;

namespace MySql.Data.MySqlClient.Tests
{
  /// <summary>
  /// Summary description for Utils.
  /// </summary>
  public static class Utils
  {
    public static byte[] CreateBlob(int size)
    {
      byte[] buf = new byte[size];

      Random r = new Random();
      r.NextBytes(buf);
      return buf;
    }

    public static DataTable FillTable(string sql, MySqlConnection conn)
    {
      MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
      DataTable dt = new DataTable();
      da.Fill(dt);
      return dt;
    }

    public static bool TableExists(string tableName, MySqlConnection conn)
    {
      using (conn)
      {
        MySqlCommand cmd = new MySqlCommand($"SELECT * FROM {tableName} LIMIT 0", conn);
        try
        {
          cmd.ExecuteScalar();
          return true;
        }
        catch (Exception)
        {
          return false;
        }
      }
    }
  }
}