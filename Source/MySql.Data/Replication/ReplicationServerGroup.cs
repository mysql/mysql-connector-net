// Copyright © 2013, Oracle and/or its affiliates. All rights reserved.
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


using MySql.Data.MySqlClient.Properties;
using System;
using System.Collections.Generic;
using System.Text;

namespace MySql.Data.MySqlClient.Replication
{
  /// <summary>
  /// Base class used to implement load balancing features
  /// </summary>
  public abstract class ReplicationServerGroup
  {
    protected List<ReplicationServer> servers = new List<ReplicationServer>();

    /// <param name="name">Group name</param>
    /// <param name="retryTime"></param>
    public ReplicationServerGroup(string name, int retryTime)
    {
      Servers = servers;
      Name = name;
      RetryTime = retryTime;
    }

    /// <summary>
    /// Group name
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// Retry time between connections to failed servers
    /// </summary>
    public int RetryTime { get; private set; }
    /// <summary>
    /// Servers list in the group
    /// </summary>
    public IList<ReplicationServer> Servers { get; private set; }

    /// <summary>
    /// Adds a server into the group
    /// </summary>
    /// <param name="name">Server name</param>
    /// <param name="isMaster">True if the server to add is master, False for slave server</param>
    /// <param name="connectionString">Connection string used by this server</param>
    /// <returns></returns>
    public ReplicationServer AddServer(string name, bool isMaster, string connectionString)
    {
      ReplicationServer server = new ReplicationServer(name, isMaster, connectionString);
      servers.Add(server);
      return server;
    }

    /// <summary>
    /// Removes a server from group
    /// </summary>
    /// <param name="name">Server name</param>
    public void RemoveServer(string name)
    {
      ReplicationServer serverToRemove = GetServer(name);
      if (serverToRemove == null)
        throw new MySqlException(String.Format(Resources.ReplicationServerNotFound, name));
      servers.Remove(serverToRemove);
    }

    /// <summary>
    /// Gets a server by name
    /// </summary>
    /// <param name="name">Server name</param>
    /// <returns>Replication server</returns>
    public ReplicationServer GetServer(string name)
    {
      foreach (var server in servers)
        if (String.Compare(name, server.Name, StringComparison.OrdinalIgnoreCase) == 0) return server;
      return null;
    }

    /// <summary>
    /// Must be implemented. Defines the next server for a custom load balancing implementation.
    /// </summary>
    /// <param name="isMaster">Defines if the server to return is a master or any</param>
    /// <returns>Next server based on the load balancing implementation</returns>
    public abstract ReplicationServer GetServer(bool isMaster);
  }
}
