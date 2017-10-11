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

using MySql.Data;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace MySqlX.Failover
{
  /// <summary>
  /// Manages the hosts available for client side failover using the Sequential Failover method.
  /// The Sequential Failover method attempts to connect to the hosts specified in the list one after another until the initial host is reached.
  /// </summary>
  internal class SequentialFailoverGroup : FailoverGroup
  {
    /// <summary>
    /// The index of the current host.
    /// </summary>
    private int _hostIndex;

    public SequentialFailoverGroup(List<XServer> hosts) : base(hosts)
    {
      _hostIndex = 0;
    }

    /// <summary>
    /// Sets the initial active host.
    /// </summary>
    protected internal override void SetInitialActiveServer()
    {
      if (Hosts == null || Hosts.Count == 0)
        throw new MySqlException(Resources.Replication_NoAvailableServer);

      Hosts[0].IsActive = true;
      _activeHost = Hosts[0];
    }

    /// <summary>
    /// Determines the next host.
    /// </summary>
    /// <returns>A <see cref="XServer"/> object that represents the next available host.</returns>
    protected internal override XServer GetNextHost()
    {
      if (Hosts == null)
        throw new MySqlException(Resources.Replication_NoAvailableServer);

      Hosts[_hostIndex].IsActive = false;
      _activeHost = _hostIndex==Hosts.Count-1 ? Hosts[0] : Hosts[_hostIndex+1];
      _activeHost.IsActive = true;
      _hostIndex++;

      return _activeHost;
    }
  }
}