// Copyright © 2015, Oracle and/or its affiliates. All rights reserved.
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
  /// Main class for session operations for MySQL Hybrid.
  /// </summary>

  public class MySQLX
  {
    /// <summary>
    /// Opens a session to the server given
    /// </summary>
    /// <param name="connectionSting">Connection data for the server</param>
    /// <returns>Session</returns>
    public static XSession GetSession(string connectionSting)
    {
      return new XSession(connectionSting);
    }

    /// <summary>
    /// Opens a session to the server given
    /// </summary>
    /// <param name="connectionData">Connection data for the server</param>
    /// <returns>Session</returns>
    public static XSession GetSession(object connectionData)
    {
      return new XSession(connectionData);
    }

    /// <summary>
    /// Opens a node session to the server given
    /// </summary>
    /// <param name="connectionSting">Connection string for the server</param>
    /// <returns>Session</returns>
    public static NodeSession GetNodeSession(string connectionSting)
    {
      return new NodeSession(connectionSting);
    }

    /// <summary>
    /// Opens a node session to the server given
    /// </summary>
    /// <param name="connectionData">Connection data for the server</param>
    /// <returns>Session</returns>
    public static NodeSession GetNodeSession(object connectionData)
    {
      return new NodeSession(connectionData);
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
