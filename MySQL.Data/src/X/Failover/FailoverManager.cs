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
using MySqlX.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MySqlX.Failover
{
  /// <summary>
  /// Implements common elements that allow to manage the hosts available for client side failover.
  /// </summary>
  internal static class FailoverManager
  {
    /// <summary>
    /// Gets and sets the failover group which consists of a host list.
    /// </summary>
    internal static FailoverGroup FailoverGroup { get; private set; }

    /// <summary>
    /// Resets the manager.
    /// </summary>
    internal static void Reset()
    {
      if (FailoverGroup != null)
        FailoverGroup = null;
    }

    /// <summary>
    /// Sets the host list to be used during failover operations.
    /// </summary>
    /// <param name="hostList">The host list.</param>
    /// <param name="failoverMethod">The failover method.</param>
    internal static void SetHostList(List<XServer> hostList, FailoverMethod failoverMethod)
    {
      switch (failoverMethod)
      {
         case FailoverMethod.Sequential:
           FailoverGroup = new SequentialFailoverGroup(hostList);
           break;
         case FailoverMethod.Priority:
           FailoverGroup = new SequentialFailoverGroup(hostList.OrderByDescending(o => o.Priority).ToList());
           break;
      }
    }

    /// <summary>
    /// Attempts to establish a connection to a host specified from the list.
    /// </summary>
    /// <param name="originalConnectionString">The original connection string set by the user.</param>
    /// <param name="connectionString">An out parameter that stores the updated connection string.</param>
    /// <returns>An <see cref="InternalSession"/> instance if the connection was succesfully established, a <see cref="MySqlException"/> exception is thrown otherwise.</returns>
    internal static InternalSession AttemptConnection(string originalConnectionString, out string connectionString)
    {
      if (FailoverGroup == null || originalConnectionString == null)
      {
        connectionString = null;
        return null;
      }

      XServer currentHost = FailoverGroup.ActiveHost;
      string initialHost = currentHost.Host;
      MySqlConnectionStringBuilder Settings = null;
      InternalSession internalSession = null;

      do
      {
        // Attempt to connect to each host by retrieving the next host based on the failover method being used.
        connectionString = "server=" + currentHost.Host +";" + originalConnectionString.Substring(originalConnectionString.IndexOf(';')+1);
        Settings = new MySqlConnectionStringBuilder(connectionString);
        if (currentHost != null && currentHost.Port!=-1)
          Settings.Port = (uint) currentHost.Port;

        try
        {
          internalSession = InternalSession.GetSession(Settings);
        }
        catch(Exception) {}

        if (internalSession != null)
          break;

        currentHost = FailoverGroup.GetNextHost();
      }
      while (currentHost.Host != initialHost);

      // All connection attempts failed.
      if (internalSession == null)
        throw new MySqlException(Resources.UnableToConnectToHost);

      return internalSession;
    }
  }

  internal enum FailoverMethod
  {
    /// <summary>
    /// Attempts the next host in the list. Moves to the first element if the end of the list is reached.
    /// </summary>
    Sequential,
    /// <summary>
    /// Determines the next host on which to attempt a connection by checking the value of the Priority property in descending order.
    /// </summary>
    Priority
  }
}
