// Copyright © 2017, 2019, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace MySql.Data.Failover
{
  /// <summary>
  /// Manages the hosts available for client side failover using the Sequential Failover method.
  /// The Sequential Failover method attempts to connect to the hosts specified in the list one after another until the initial host is reached.
  /// </summary>
  internal class SequentialFailoverGroup : FailoverGroup
  {
    /// <summary>
    /// The initial host taken from the list.
    /// </summary>
    private FailoverServer _initialHost;
    /// <summary>
    /// The index of the current host.
    /// </summary>
    private int _hostIndex;
    /// <summary>
    /// The host for the current connection attempt.
    /// </summary>
    private FailoverServer _currentHost;

    public SequentialFailoverGroup(List<FailoverServer> hosts) : base(hosts)
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

      _initialHost = Hosts[0];
      Hosts[0].IsActive = true;
      _activeHost = Hosts[0];
      _currentHost = _activeHost;
    }

    /// <summary>
    /// Determines the next host.
    /// </summary>
    /// <returns>A <see cref="FailoverServer"/> object that represents the next available host.</returns>
    protected internal override FailoverServer GetNextHost()
    {
      if (Hosts == null || Hosts?.Count == 0)
        throw new MySqlException(Resources.Replication_NoAvailableServer);

      var currentServer = Hosts.Find(h => h.Host == _currentHost.Host && h.Port == _currentHost.Port);
      currentServer.IsActive = false;
      _hostIndex = Hosts.IndexOf(currentServer);
      if (Hosts.Count > 1)
      {
        _activeHost = _hostIndex == Hosts.Count - 1 ? Hosts[0] : Hosts[_hostIndex + 1];
        _activeHost.IsActive = true;
        _currentHost = _activeHost;
        _hostIndex++;
      }
      else
        _activeHost = _initialHost;

      return _activeHost;
    }
  }
}