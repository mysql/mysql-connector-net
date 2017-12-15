// Copyright © 2017, Oracle and/or its affiliates. All rights reserved.
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