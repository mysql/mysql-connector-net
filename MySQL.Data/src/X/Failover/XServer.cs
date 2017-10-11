// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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

namespace MySqlX.Failover
{
  /// <summary>
  /// Depicts a host which can be failed over to.
  /// </summary>
  internal class XServer
  {
    #region Properties

    /// <summary>
    /// Gets and sets the name or address of the host.
    /// </summary>
    internal string Host { get; private set; }
    /// <summary>
    /// Gets and sets the port number.
    /// </summary>
    internal int Port { get; private set; }
    /// <summary>
    /// Gets a value between 0 and 100 which represents the priority of the host.
    /// </summary>
    internal int Priority { get; private set; }
    /// <summary>
    /// Flag to indicate if this host is currently being used.
    /// </summary>
    internal bool IsActive { get; set; }

    #endregion

    internal XServer(string host, int port, int priority)
    {
      this.Host = host;
      this.Port = port;
      this.Priority = priority;
    }
  }
}
