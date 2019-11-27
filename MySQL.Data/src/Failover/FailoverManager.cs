// Copyright (c) 2017, 2019, Oracle and/or its affiliates. All rights reserved.
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
using MySqlX.Sessions;
using MySqlX.XDevAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace MySql.Data.Failover
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
    internal static void SetHostList(List<FailoverServer> hostList, FailoverMethod failoverMethod)
    {
      if (FailoverGroup != null)
        return;

      switch (failoverMethod)
      {
        case FailoverMethod.Sequential:
          FailoverGroup = new SequentialFailoverGroup(hostList);
          break;
        case FailoverMethod.Priority:
          FailoverGroup = new SequentialFailoverGroup(hostList.OrderByDescending(o => o.Priority).ToList());
          break;
        case FailoverMethod.Random:
          FailoverGroup = new RandomFailoverGroup(hostList);
          break;
      }
    }

    /// <summary>
    /// Attempts to establish a connection to a host specified from the list.
    /// </summary>
    /// <param name="originalConnectionString">The original connection string set by the user.</param>
    /// <param name="connectionString">An out parameter that stores the updated connection string.</param>
    /// <param name="client">A <see cref="Client"/> object in case this is a pooling scenario.</param>
    /// <returns>An <see cref="InternalSession"/> instance if the connection was succesfully established, a <see cref="MySqlException"/> exception is thrown otherwise.</returns>
    internal static InternalSession AttemptConnectionXProtocol(string originalConnectionString, out string connectionString, bool isDefaulPort, Client client = null)
    {
      if (FailoverGroup == null || originalConnectionString == null)
      {
        connectionString = null;
        return null;
      }

      if (client != null)
      {
        if (client.Hosts == null)
        {
          client.Hosts = FailoverGroup.Hosts;
          client.DemotedHosts = new ConcurrentQueue<FailoverServer>();
        }
        else
        {
          FailoverGroup.Hosts = client.Hosts;
        }
      }

      FailoverServer currentHost = FailoverGroup.ActiveHost;
      FailoverServer initialHost = currentHost;
      MySqlXConnectionStringBuilder Settings = null;
      InternalSession internalSession = null;

      do
      {
        // Attempt to connect to each host by retrieving the next host based on the failover method being used.
        connectionString = originalConnectionString.Contains("server") ?
          originalConnectionString.Replace(originalConnectionString.Split(';').First(p => p.Contains("server")).Split('=')[1], currentHost.Host) :
          "server=" + currentHost.Host + ";" + originalConnectionString;
        if (currentHost != null && currentHost.Port != -1)
          connectionString = connectionString.Replace(connectionString.Split(';').First(p => p.Contains("port")).Split('=')[1], currentHost.Port.ToString());
        Settings = new MySqlXConnectionStringBuilder(connectionString, isDefaulPort);

        try { internalSession = InternalSession.GetSession(Settings); }
        catch (Exception) { }

        if (internalSession != null)
          break;

        var tmpHost = currentHost;
        currentHost = FailoverGroup.GetNextHost();

        if (client != null)
        {
          tmpHost.DemotedTime = DateTime.Now;
          client.Hosts.Remove(tmpHost);
          client.DemotedHosts.Enqueue(tmpHost);

          if (client.DemotedServersTimer == null)
            client.DemotedServersTimer = new Timer(new TimerCallback(client.ReleaseDemotedHosts),
              null, Client.DEMOTED_TIMEOUT, Timeout.Infinite);
        }
      }
      while (!currentHost.Equals(initialHost));

      // All connection attempts failed.
      if (internalSession == null)
        throw new MySqlException(Resources.UnableToConnectToHost);

      return internalSession;
    }

    /// <summary>
    /// Attempts to establish a connection to a host specified from the list.
    /// </summary>
    /// <param name="connection">MySqlConnection object where the new driver will be assigned</param>
    /// <param name="originalConnectionString">The original connection string set by the user.</param>
    /// <param name="connectionString">An out parameter that stores the updated connection string.</param>
    /// <param name="mySqlPoolManager">A <see cref="MySqlPoolManager"> in case this is a pooling scenario."/></param>
    internal static void AttemptConnection(MySqlConnection connection, string originalConnectionString, out string connectionString, bool mySqlPoolManager = false)
    {
      if (mySqlPoolManager)
        if (MySqlPoolManager.Hosts == null)
        {
          MySqlPoolManager.Hosts = FailoverGroup.Hosts;
          MySqlPoolManager.DemotedHosts = new ConcurrentQueue<FailoverServer>();
        }
        else
          FailoverGroup.Hosts = MySqlPoolManager.Hosts;

      FailoverServer currentHost = FailoverGroup.ActiveHost;
      FailoverServer initialHost = currentHost;
      Driver driver = null;

      do
      {
        // Attempt to connect to each host by retrieving the next host based on the failover method being used
        MySqlConnectionStringBuilder msb;
        connectionString = "server=" + currentHost.Host + ";" + originalConnectionString.Substring(originalConnectionString.IndexOf(';') + 1);
        if (currentHost != null && currentHost.Port != -1)
          connectionString += ";port=" + currentHost.Port;
        msb = new MySqlConnectionStringBuilder(connectionString);

        if ((FailoverGroup.Hosts.Count == 1 && !mySqlPoolManager) ||
          (mySqlPoolManager && MySqlPoolManager.Hosts.Count == 1 && MySqlPoolManager.DemotedHosts.IsEmpty))
          return;

        try
        {
          driver = Driver.Create(msb);
          if (!mySqlPoolManager)
            connection.driver = driver;
          break;
        }
        catch (Exception) { }

        var tmpHost = currentHost;
        currentHost = FailoverGroup.GetNextHost();

        if (mySqlPoolManager)
        {
          tmpHost.DemotedTime = DateTime.Now;
          MySqlPoolManager.Hosts.Remove(tmpHost);
          MySqlPoolManager.DemotedHosts.Enqueue(tmpHost);

          if (MySqlPoolManager.DemotedServersTimer == null)
            MySqlPoolManager.DemotedServersTimer = new Timer(new TimerCallback(MySqlPoolManager.ReleaseDemotedHosts),
              null, MySqlPoolManager.DEMOTED_TIMEOUT, Timeout.Infinite);
        }
      } while (!currentHost.Equals(initialHost));

      if (driver == null)
        throw new MySqlException(Resources.UnableToConnectToHost);
    }

    /// <summary>
    /// Creates a <see cref="FailoverGroup"/> if more than one host is found.
    /// </summary>
    /// <param name="hierPart">A string containing an unparsed list of hosts.</param>
    /// <param name="isXProtocol"><c>true</c> if the connection is X Protocol; otherwise <c>false</c>.</param>
    /// <param name="connectionDataIsUri"><c>true</c> if the connection data is a URI; otherwise <c>false</c>.</param>
    /// <returns>The number of hosts found, -1 if an error was raised during parsing.</returns>
    internal static int ParseHostList(string hierPart, bool isXProtocol, bool connectionDataIsUri = true)
    {
      if (string.IsNullOrWhiteSpace(hierPart)) return -1;

      int hostCount = -1;
      FailoverMethod failoverMethod = FailoverMethod.Random;
      string[] hostArray = null;
      List<FailoverServer> hostList = new List<FailoverServer>();
      hierPart = hierPart.Replace(" ", "");

      if (!hierPart.StartsWith("(") && !hierPart.EndsWith(")"))
      {
        hostArray = hierPart.Split(',');
        if (hostArray.Length == 1)
          return 1;

        foreach (var host in hostArray)
          hostList.Add(ConvertToFailoverServer(host, connectionDataIsUri: connectionDataIsUri));

        hostCount = hostArray.Length;
      }
      else
      {
        string[] groups = hierPart.Split(new string[] { "),(" }, StringSplitOptions.RemoveEmptyEntries);
        bool? allHavePriority = null;
        int defaultPriority = -1;
        foreach (var group in groups)
        {
          // Remove leading parenthesis.
          var normalizedGroup = group;
          if (normalizedGroup.StartsWith("("))
            normalizedGroup = group.Substring(1);

          if (normalizedGroup.EndsWith(")"))
            normalizedGroup = normalizedGroup.Substring(0, normalizedGroup.Length - 1);

          string[] items = normalizedGroup.Split(',');
          string[] keyValuePairs = items[0].Split('=');
          if (keyValuePairs[0].ToLowerInvariant() != "address")
            throw new KeyNotFoundException(string.Format(ResourcesX.KeywordNotFound, "address"));

          string host = keyValuePairs[1];
          if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentNullException("server");

          if (items.Length == 2)
          {
            if (allHavePriority != null && allHavePriority == false)
              throw new ArgumentException(ResourcesX.PriorityForAllOrNoHosts);

            allHavePriority = allHavePriority ?? true;
            keyValuePairs = items[1].Split('=');
            if (keyValuePairs[0].ToLowerInvariant() != "priority")
              throw new KeyNotFoundException(string.Format(ResourcesX.KeywordNotFound, "priority"));

            if (string.IsNullOrWhiteSpace(keyValuePairs[1]))
              throw new ArgumentNullException("priority");

            int priority = -1;
            Int32.TryParse(keyValuePairs[1], out priority);
            if (priority < 0 || priority > 100)
              throw new ArgumentException(ResourcesX.PriorityOutOfLimits);

            if (isXProtocol)
              hostList.Add(ConvertToFailoverServer(BaseSession.IsUnixSocket(host) ? BaseSession.NormalizeUnixSocket(host) : host, priority, connectionDataIsUri: connectionDataIsUri));
            else
              hostList.Add(ConvertToFailoverServer(host, priority));
          }
          else
          {
            if (allHavePriority != null && allHavePriority == true)
              throw new ArgumentException(ResourcesX.PriorityForAllOrNoHosts);

            allHavePriority = allHavePriority ?? false;

            hostList.Add(ConvertToFailoverServer(host, defaultPriority, connectionDataIsUri: connectionDataIsUri));
          }
        }

        hostCount = groups.Length;
        if (hostList.GroupBy(h => h.Priority).ToList().Count > 1)
          failoverMethod = FailoverMethod.Priority;
        else
          failoverMethod = FailoverMethod.Random;
      }

      SetHostList(hostList, failoverMethod);
      return hostCount;
    }

    /// <summary>
    /// Creates a <see cref="FailoverServer"/> object based on the provided parameters.
    /// </summary>
    /// <param name="host">The host string that can be a simple host name or a host name and port.</param>
    /// <param name="priority">The priority of the host.</param>
    /// <param name="port">The port number of the host.</param>
    /// <param name="connectionDataIsUri"><c>true</c> if the connection data is a URI; otherwise <c>false</c>.</param>
    /// <returns></returns>
    private static FailoverServer ConvertToFailoverServer(string host, int priority = -1, int port = -1, bool connectionDataIsUri = true)
    {
      host = host.Trim();
      int colonIndex = -1;
      if (IPAddress.TryParse(host, out IPAddress address))
      {
        switch (address.AddressFamily)
        {
          case System.Net.Sockets.AddressFamily.InterNetworkV6:
            if (host.StartsWith("[") && host.Contains("]") && !host.EndsWith("]"))
              colonIndex = host.LastIndexOf(":");

            break;
          default:
            colonIndex = host.IndexOf(":");
            break;
        }
      }
      else
        colonIndex = host.IndexOf(":");

      if (colonIndex != -1)
      {
        if (!connectionDataIsUri)
          throw new ArgumentException(ResourcesX.PortNotSupported);

        int.TryParse(host.Substring(colonIndex + 1), out port);
        host = host.Substring(0, colonIndex);
      }

      return new FailoverServer(host, port, priority);
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
    Priority,
    /// <summary>
    /// Determines the next host on which to attempt a connection randomly.
    /// </summary>
    Random
  }
}
