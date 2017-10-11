// Copyright © 2016, 2017, Oracle and/or its affiliates. All rights reserved.
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


using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlX.XDevAPI.Config
{
  /// <summary>
  /// Provides support for session configuration data persistence.
  /// </summary>
  public interface IPersistenceHandler
  {
    /// <summary>
    /// Saves the specified session configuration.
    /// </summary>
    /// <param name="name">The name used to identify the session configuration.</param>
    /// <param name="config">The document that holds the session configuration.</param>
    /// <returns>A <see cref="SessionConfig"/> object </returns>
    SessionConfig Save(string name, DbDoc config);
    /// <summary>
    /// Retrieves a document representing the session configuration data for a given name.
    /// </summary>
    /// <param name="name">The name associated to the session configuration.</param>
    /// <returns>The <see cref="DbDoc"/> object matching the provided <paramref name="name"/>.</returns>
    DbDoc Load(string name);
    /// <summary>
    /// Deletes the specified session configuration.
    /// </summary>
    /// <param name="name">The name of the session configuration.</param>
    void Delete(string name);
    /// <summary>
    /// Retrives the name list of stored session configurations.
    /// </summary>
    /// <returns>A string list with the names of stored session configurations.</returns>
    List<string> List();
    /// <summary>
    /// Validates the existence of a specific session configuration.
    /// </summary>
    /// <param name="name">The name of the session configuration.</param>
    /// <returns>True if the session configuration exists, false otherwise.</returns>
    bool Exists(string name);
  }
}
