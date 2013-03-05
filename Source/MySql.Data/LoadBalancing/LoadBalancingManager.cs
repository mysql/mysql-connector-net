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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MySql.Data.MySqlClient.LoadBalancing
{
  public static class LoadBalancingManager
  {
    private static Dictionary<string, LoadBalancingServerSelector> selectors = new Dictionary<string, LoadBalancingServerSelector>();

    static LoadBalancingManager()
    {
      // load up our selectors
      if (MySqlConfiguration.Settings == null) return;

      foreach (var group in MySqlConfiguration.Settings.LoadBalancing.ServerGroups)
      {
        LoadBalancingServerSelector selector = null;
        if (string.IsNullOrEmpty(group.SelectorType))
          selector = new LoadBalancingRoundRobinSelector(group);
        else
          selector = CreateSelector(group.SelectorType);
        selectors.Add(group.Name, selector);
      }
    }

    public static LoadBalancingServerConfigurationElement GetServer(string group, bool isMaster)
    {
      if (!IsLoadBalancingGroup(group)) return null;
      LoadBalancingServerSelector selector = selectors[group];
      LoadBalancingServerConfigurationElement server = selector.GetServer(isMaster);
      if (server == null) throw new MySqlException(Properties.Resources.LoadBalancing_NoAvailableServer);

      return server;
    }

    public static LoadBalancingServerGroupConfigurationElement GetGroup(string group)
    {
      if (!IsLoadBalancingGroup(group)) return null;
      foreach (var serverGroup in MySqlConfiguration.Settings.LoadBalancing.ServerGroups)
      {
        if (serverGroup.Name == group) return serverGroup;
      }
      return null;
    }

    public static bool IsLoadBalancingGroup(string group)
    {
      return selectors.ContainsKey(group);
    }

    public static void GetNewConnection(string group, bool master, MySqlConnection connection)
    {
      do
      {
        if (!IsLoadBalancingGroup(group)) return;

        LoadBalancingServerConfigurationElement server = GetServer(group, master);

        Driver driver = new Driver(new MySqlConnectionStringBuilder(server.ConnectionString));
        if (connection.driver == null
          || driver.Settings.ConnectionString != connection.driver.Settings.ConnectionString)
        {
          connection.Close();
          connection.hasBeenOpen = false;
          try
          {
            connection.driver = driver;
            connection.Open();
            return;
          }
          catch (Exception)
          {
            // retry to open a failed connection and update its status
            connection.driver = null;
            server.IsAvailable = false;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate(object sender, DoWorkEventArgs e)
            {
              bool isRunning = false;
              LoadBalancingServerConfigurationElement server1 = e.Argument as LoadBalancingServerConfigurationElement;
              int retryTime = LoadBalancingManager.GetGroup(group).RetryTime;
              System.Timers.Timer timer = new System.Timers.Timer(retryTime * 1000.0);


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
                    string.Format(Properties.Resources.LoadBalancing_ConnectionAttemptFailed, server1.Name));
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
        }
        else
          return;
      } while (true);
    }

    private static LoadBalancingServerSelector CreateSelector(string type)
    {
      Type t = Type.GetType(type);
      return Activator.CreateInstance(t) as LoadBalancingServerSelector;
    }
  }
}
