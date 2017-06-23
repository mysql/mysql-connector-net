// Copyright © 2015, 2017, Oracle and/or its affiliates. All rights reserved.
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

namespace MySqlX.XDevAPI
{

  /// <summary>
  /// Main class for session operations related to Connector/NET implementation of the X DevAPI.
  /// </summary>

  public class MySQLX
  {
    /// <summary>
    /// Initializes a new instance of the MySQLX class.
    /// </summary>
    public MySQLX()
    {
    }

    /// <summary>
    /// Opens a session to the server given or to the first available server if multiple servers were specified.
    /// </summary>
    /// <param name="connectionString">The connection string in basic or URI format.</param>
    /// <returns>A <see cref="Session"/> object representing the established session.</returns>
    /// <remarks>Multiple hosts can be specified as part of the <paramref name="connectionString"/> which
    /// will enable client side failover when trying to establish a connection. For additional details and syntax 
    /// examples refer to the <see cref="BaseSession.BaseSession(string)"/> remarks section.</remarks>
    public static Session GetSession(string connectionString)
    {
      return new Session(connectionString);
    }

    /// <summary>
    /// Opens a session to the server given.
    /// </summary>
    /// <param name="connectionData">The connection data for the server.</param>
    /// <returns>A <see cref="Session"/> object representing the established session.</returns>
    public static Session GetSession(object connectionData)
    {
      return new Session(connectionData);
    }

    //public static Iterator CsvFileRowIterator()
    //{
    //  throw new NotImplementedException();
    //}

    //public static Iterator JsonFileDocIterator()
    //{
    //  throw new NotImplementedException();
    //}
  }
}
