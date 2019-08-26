// Copyright (c) 2014, 2019, Oracle and/or its affiliates. All rights reserved.
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



using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MySql.Data.MySqlClient.Replication
{
  /// <summary>
  /// Base class used to implement load balancing features.
  /// </summary>
  public abstract class ReplicationServerGroup
  {
    /// <summary>
    /// List of servers available for replication.
    /// </summary>
    protected List<ReplicationServer> servers = new List<ReplicationServer>();

    /// <param name="name">The group name.</param>
    /// <param name="retryTime">The number of seconds to perform a retry.</param>
    public ReplicationServerGroup(string name, int retryTime)
    {
      Servers = servers;
      Name = name;
      RetryTime = retryTime;
    }

    /// <summary>
    /// Gets the group name.
    /// </summary>
    public string Name { get; protected set; }
    /// <summary>
    /// Gets the retry time between connections to failed servers.
    /// </summary>
    public int RetryTime { get; protected set; }
    /// <summary>
    /// Gets the server list in the group.
    /// </summary>
    protected IList<ReplicationServer> Servers { get; private set; }

    /// <summary>
    /// Adds a server into the group.
    /// </summary>
    /// <param name="name">The server name.</param>
    /// <param name="isMaster">A flag indicating if the server to add is master or slave.</param>
    /// <param name="connectionString">The connection string used by this server.</param>
    /// <returns>A <see cref="ReplicationServer"/> object representing the recently added object.</returns>
    internal protected ReplicationServer AddServer(string name, bool isMaster, string connectionString)
    {
      ReplicationServer server = new ReplicationServer(name, isMaster, connectionString);
      servers.Add(server);
      return server;
    }

    /// <summary>
    /// Removes a server from the group.
    /// </summary>
    /// <param name="name">The server name.</param>
    internal protected void RemoveServer(string name)
    {
      ReplicationServer serverToRemove = GetServer(name);
      if (serverToRemove == null)
        throw new MySqlException(String.Format(Resources.ReplicationServerNotFound, name));
      servers.Remove(serverToRemove);
    }

    /// <summary>
    /// Gets a server by name.
    /// </summary>
    /// <param name="name">The server name.</param>
    /// <returns>The replication server.</returns>
    internal protected ReplicationServer GetServer(string name)
    {
      foreach (var server in servers)
        if (String.Compare(name, server.Name, StringComparison.OrdinalIgnoreCase) == 0) return server;
      return null;
    }

    /// <summary>
    /// Must be implemented. Defines the next server for a custom load balancing implementation.
    /// </summary>
    /// <param name="isMaster">Defines if the server to return is a master or any.</param>
    /// <returns>The next server based on the load balancing implementation.
    /// Null if no available server is found.
    /// </returns>
    internal protected abstract ReplicationServer GetServer(bool isMaster);

    /// <summary>
    /// Defines the next server for a custom load balancing implementation.
    /// </summary>
    /// <param name="isMaster">Defines if the server to return is a master or any.</param>
    /// <param name="settings">Currently not being used.</param>
    /// <returns>The next server based on the load balancing implementation.
    /// Null if no available server is found.
    /// </returns>
    internal protected virtual ReplicationServer GetServer(bool isMaster, MySqlConnectionStringBuilder settings)
    {
      return GetServer(isMaster);
    }

    /// <summary>
    /// Handles a failed connection to a server.
    /// </summary>
    /// <param name="server">The failed server.</param>
    /// <remarks>This method can be overrided to implement a custom failover handling.</remarks>
    internal protected virtual void HandleFailover(ReplicationServer server)
    {
      BackgroundWorker worker = new BackgroundWorker();
      worker.DoWork += delegate(object sender, DoWorkEventArgs e)
      {
        bool isRunning = false;
        ReplicationServer server1 = e.Argument as ReplicationServer;
        System.Timers.Timer timer = new System.Timers.Timer(RetryTime * 1000.0);

        System.Timers.ElapsedEventHandler elapsedEvent = delegate(object sender1, System.Timers.ElapsedEventArgs e1)
        {
          if (isRunning) return;
          try
          {
            isRunning = true;
            using (MySqlConnection connectionFailed = new MySqlConnection(server.ConnectionString))
            {
              connectionFailed.Open();
              server1.IsAvailable = true;
              timer.Stop();
            }
          }
          catch
          {
            MySqlTrace.LogWarning(0,
              string.Format(Resources.Replication_ConnectionAttemptFailed, server1.Name));
          }
          finally
          {
            isRunning = false;
          }
        };
        timer.Elapsed += elapsedEvent;
        timer.Start();
        elapsedEvent(sender, null);
      };

      worker.RunWorkerAsync(server);
    }

    /// <summary>
    /// Handles a failed connection to a server.
    /// </summary>
    /// <param name="server">The failed server.</param>
    /// <param name="exception">The exception that caused the failover.</param>
    internal protected virtual void HandleFailover(ReplicationServer server, Exception exception)
    {
      HandleFailover(server);
    }
  }
}
