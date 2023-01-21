// Copyright (c) 2014, 2023, Oracle and/or its affiliates.
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
using System.Threading;
using System.Threading.Tasks;


namespace MySql.Data.MySqlClient.Replication
{
  /// <summary>
  /// Manager for Replication and Load Balancing features
  /// </summary>
  internal static class ReplicationManager
  {
    private static List<ReplicationServerGroup> groups = new List<ReplicationServerGroup>();
    private static Object thisLock = new Object();
    //private static Dictionary<string, ReplicationServerSelector> selectors = new Dictionary<string, ReplicationServerSelector>();

    static ReplicationManager()
    {
      Groups = groups;

      // load up our selectors
      if (MySqlConfiguration.Settings == null) return;

      foreach (var group in MySqlConfiguration.Settings.Replication.ServerGroups)
      {
        ReplicationServerGroup g = AddGroup(group.Name, group.GroupType, group.RetryTime);
        foreach (var server in group.Servers)
          g.AddServer(server.Name, server.IsSource, server.ConnectionString);
      }
    }

    /// <summary>
    /// Returns Replication Server Group List
    /// </summary>
    internal static IList<ReplicationServerGroup> Groups { get; private set; }

    /// <summary>
    /// Adds a Default Server Group to the list
    /// </summary>
    /// <param name="name">Group name</param>
    /// <param name="retryTime">Time between reconnections for failed servers</param>
    /// <returns>Replication Server Group added</returns>
    internal static ReplicationServerGroup AddGroup(string name, int retryTime)
    {
      return AddGroup(name, null, retryTime);
    }

    /// <summary>
    /// Adds a Server Group to the list
    /// </summary>
    /// <param name="name">Group name</param>
    /// <param name="groupType">ServerGroup type reference</param>
    /// <param name="retryTime">Time between reconnections for failed servers</param>
    /// <returns>Server Group added</returns>
    internal static ReplicationServerGroup AddGroup(string name, string groupType, int retryTime)
    {
      if (string.IsNullOrEmpty(groupType))
        groupType = "MySql.Data.MySqlClient.Replication.ReplicationRoundRobinServerGroup";
      Type t = Type.GetType(groupType);
      ReplicationServerGroup g = (ReplicationServerGroup)Activator.CreateInstance(t, name, retryTime) as ReplicationServerGroup;
      groups.Add(g);
      return g;
    }

    /// <summary>
    /// Gets the next server from a replication group
    /// </summary>
    /// <param name="groupName">Group name</param>
    /// <param name="isSource">True if the server to return must be a source</param>
    /// <returns>Replication Server defined by the Load Balancing plugin</returns>
    internal static ReplicationServer GetServer(string groupName, bool isSource)
    {
      ReplicationServerGroup group = GetGroup(groupName);
      return group.GetServer(isSource);
    }

    /// <summary>
    /// Gets a Server Group by name
    /// </summary>
    /// <param name="groupName">Group name</param>
    /// <returns>Server Group if found, otherwise throws an MySqlException</returns>
    internal static ReplicationServerGroup GetGroup(string groupName)
    {
      ReplicationServerGroup group = null;
      foreach (ReplicationServerGroup g in groups)
      {
        if (String.Compare(g.Name, groupName, StringComparison.OrdinalIgnoreCase) != 0) continue;
        group = g;
        break;
      }
      if (group == null)
        throw new MySqlException(String.Format(Resources.ReplicationGroupNotFound, groupName));
      return group;
    }

    /// <summary>
    /// Validates if the replication group name exists
    /// </summary>
    /// <param name="groupName">Group name to validate</param>
    /// <returns><c>true</c> if the replication group name is found; otherwise, <c>false</c></returns>
    internal static bool IsReplicationGroup(string groupName)
    {
      foreach (ReplicationServerGroup g in groups)
        if (String.Compare(g.Name, groupName, StringComparison.OrdinalIgnoreCase) == 0) return true;
      return false;
    }

    /// <summary>
    /// Assigns a new server driver to the connection object
    /// </summary>
    /// <param name="groupName">Group name</param>
    /// <param name="source">True if the server connection to assign must be a source</param>
    /// <param name="connection">MySqlConnection object where the new driver will be assigned</param>
    /// <param name="execAsync">Boolean that indicates if the function will be executed asynchronously.</param>
    /// <param name="cancellationToken">the cancellation token.</param>
    internal static async Task GetNewConnectionAsync(string groupName, bool source, MySqlConnection connection, bool execAsync, CancellationToken cancellationToken)
    {
      do
      {
        SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        semaphoreSlim.Wait();

        if (!IsReplicationGroup(groupName)) return;

        ReplicationServerGroup group = GetGroup(groupName);
        ReplicationServer server = group.GetServer(source, connection.Settings);

        if (server == null)
          throw new MySqlException(Resources.Replication_NoAvailableServer);

        try
        {
          bool isNewServer = false;
          if (connection.driver == null || !connection.driver.IsOpen)
          {
            isNewServer = true;
          }
          else
          {
            MySqlConnectionStringBuilder msb = new MySqlConnectionStringBuilder(server.ConnectionString);
            if (!msb.Equals(connection.driver.Settings))
            {
              isNewServer = true;
            }
          }
          if (isNewServer)
          {
            Driver driver = await Driver.CreateAsync(new MySqlConnectionStringBuilder(server.ConnectionString), execAsync, cancellationToken).ConfigureAwait(false);
            connection.driver = driver;
          }
          return;
        }
        catch (MySqlException ex)
        {
          connection.driver = null;
          server.IsAvailable = false;
          MySqlTrace.LogError(ex.Number, ex.ToString());
          if (ex.Number == 1042)
          {
            // retry to open a failed connection and update its status
            group.HandleFailover(server, ex);
          }
          else
            throw;
        }

        semaphoreSlim.Release();

      } while (true);
    }
  }
}
