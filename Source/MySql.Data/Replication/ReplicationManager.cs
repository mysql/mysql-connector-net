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
using System.ComponentModel;
using System.Text;

namespace MySql.Data.MySqlClient.Replication
{
  public static class ReplicationManager
  {
    private static List<ReplicationServerGroup> groups = new List<ReplicationServerGroup>();
    //private static Dictionary<string, ReplicationServerSelector> selectors = new Dictionary<string, ReplicationServerSelector>();

    static ReplicationManager()
    {
      Groups = groups;

#if !CF && !RT
      // load up our selectors
      if (MySqlConfiguration.Settings == null) return;

      foreach (var group in MySqlConfiguration.Settings.Replication.ServerGroups)
      {
        ReplicationServerGroup g = AddGroup(group.Name, group.GroupType, group.RetryTime);
        foreach (var server in group.Servers)
          g.AddServer(server.Name, server.IsMaster, server.ConnectionString);
      }
#endif
    }

    public static IList<ReplicationServerGroup> Groups { get; private set; }

    public static ReplicationServerGroup AddGroup(string name, int retryTime)
    {
      return AddGroup( name, null, retryTime);
    }

    public static ReplicationServerGroup AddGroup(string name, string groupType, int retryTime)
    {
      if (string.IsNullOrEmpty(groupType))
        groupType = "MySql.Data.MySqlClient.Replication.ReplicationRoundRobinServerGroup";
      Type t = Type.GetType(groupType);
      ReplicationServerGroup g = (ReplicationServerGroup)Activator.CreateInstance(t, name, retryTime) as ReplicationServerGroup;
      groups.Add(g);
      return g;
    }

    public static ReplicationServer GetServer(string groupName, bool isMaster)
    {
      ReplicationServerGroup group = GetGroup(groupName);
      return group.GetServer(isMaster);
    }

    public static ReplicationServerGroup GetGroup(string groupName)
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

    public static bool IsReplicationGroup(string groupName)
    {
      foreach (ReplicationServerGroup g in groups)
        if (String.Compare(g.Name, groupName, StringComparison.OrdinalIgnoreCase) == 0) return true;
      return false;
    }

    public static void GetNewConnection(string groupName, bool master, MySqlConnection connection)
    {
      do
      {
        if (!IsReplicationGroup(groupName)) return;

        ReplicationServerGroup group = GetGroup(groupName);
        ReplicationServer server = group.GetServer(master);

        if (server == null)
          throw new MySqlException(Properties.Resources.Replication_NoAvailableServer);

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
              ReplicationServer server1 = e.Argument as ReplicationServer;
              int retryTime = ReplicationManager.GetGroup(groupName).RetryTime;
#if !RT
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
                    string.Format(Properties.Resources.Replication_ConnectionAttemptFailed, server1.Name));
                }
                finally
                {
                  isRunning = false;
                }
              };
              timer.Elapsed += elapsedEvent;
              timer.Start();
              elapsedEvent(sender, null);
#else
              Windows.UI.Xaml.DispatcherTimer timer = new Windows.UI.Xaml.DispatcherTimer();
              TimeSpan ts = new TimeSpan(retryTime * 1000);
              System.EventHandler<object> elapsedEvent = (TickSender, TickEventArgs) =>
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
                        string.Format(Properties.Resources.Replication_ConnectionAttemptFailed, server1.Name));
                  }
                  finally
                  {
                      isRunning = false;
                  }
              };
              timer.Tick += elapsedEvent;
              elapsedEvent(sender, null);
              timer.Start();
#endif
            };

            worker.RunWorkerAsync(server);
          }
        }
        else
          return;
      } while (true);
    }
  }
}
