// Copyright © 2004, 201, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Types;

namespace MySql.Data.MySqlClient
{
#if DNXCORE50
  public sealed partial class MySqlDataReader : DbDataReader
#else
  public sealed partial class MySqlDataReader : DbDataReader, IDataReader, IDataRecord
#endif
  {
    /// <summary>
    /// Gets a value indicating the depth of nesting for the current row.  This method is not 
    /// supported currently and always returns 0.
    /// </summary>
    public override int Depth => 0;

    public MySqlGeometry GetMySqlGeometry(int i)
    {
      try
      {
        IMySqlValue v = GetFieldValue(i, false);
        if (v is MySqlGeometry || v is MySqlBinary)
          return new MySqlGeometry(MySqlDbType.Geometry, (Byte[])v.Value);
      }
      catch
      {
        Throw(new Exception("Can't get MySqlGeometry from value"));
      }
      return new MySqlGeometry(true);
    }

    public MySqlGeometry GetMySqlGeometry(string column)
    {
      return GetMySqlGeometry(GetOrdinal(column));
    }

    /// <summary>
    /// Returns an <see cref="IEnumerator"/> that iterates through the <see cref="MySqlDataReader"/>. 
    /// </summary>
    /// <returns></returns>
    public override IEnumerator GetEnumerator()
    {
      //TODO: REVIEW FOR DOTNETCORE50
      return ResultSet.Values.GetEnumerator();
      //throw new NotImplementedException();
    }
    //TODO: REVIEW HOW TO GET THIS IMPLEMENTATION FOR NETCORE
    //public override IEnumerator GetEnumerator()
    //{
    //  return new DbEnumerator(this, (commandBehavior & CommandBehavior.CloseConnection) != 0);
    //}
  }
}
