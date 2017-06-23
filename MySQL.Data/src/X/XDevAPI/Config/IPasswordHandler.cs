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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlX.XDevAPI
{
  /// <summary>
  /// Provides support for password persistence.
  /// </summary>
  public interface IPasswordHandler
  {
    /// <summary>
    /// Saves the password for a given key and service.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <param name="service">The service name.</param>
    /// <param name="password">The password to store.</param>
    void Save(string key, string service, string password);

    /// <summary>
    /// Retrieves the password associated to the given key and service.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <param name="service">The service name.</param>
    /// <returns>The password that matches the given key and service.</returns>
    string Load(string key, string service);
  }
}
