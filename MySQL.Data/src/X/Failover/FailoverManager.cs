// Copyright (c) 2017, 2018, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.Sessions;
using MySqlX.XDevAPI;
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
      MySqlXConnectionStringBuilder Settings = null;
      InternalSession internalSession = null;
      TimeoutException timeoutException = null;

      do
      {
        // Attempt to connect to each host by retrieving the next host based on the failover method being used.
        connectionString = "server=" + currentHost.Host + ";" + originalConnectionString.Substring(originalConnectionString.IndexOf(';') + 1);
        Settings = new MySqlXConnectionStringBuilder(connectionString);
        if (currentHost != null && currentHost.Port != -1)
          Settings.Port = (uint)currentHost.Port;
        if (currentHost.Host == initialHost)
        {
          string exTimeOutMessage = Settings.ConnectTimeout == 0 ? ResourcesX.TimeOutMultipleHost0ms : String.Format(ResourcesX.TimeOutMultipleHost, Settings.ConnectTimeout);
          timeoutException = new TimeoutException(exTimeOutMessage);
        }

        try
        {
          internalSession = InternalSession.GetSession(Settings);
          timeoutException = null;
        }
        catch (Exception ex) { if (!(ex is TimeoutException)) timeoutException = null; }

        if (internalSession != null)
          break;

        currentHost = FailoverGroup.GetNextHost();
      }
      while (currentHost.Host != initialHost);

      // All connection attempts failed.
      if (timeoutException != null)
        throw timeoutException;
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
