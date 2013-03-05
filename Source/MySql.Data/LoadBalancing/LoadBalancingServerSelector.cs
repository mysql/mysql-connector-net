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
using System.Text;

namespace MySql.Data.MySqlClient.LoadBalancing
{
  /// <summary>
  /// 
  /// </summary>
  public abstract class LoadBalancingServerSelector
  {
    protected LoadBalancingServerGroupConfigurationElement Group { get; set; }

    public LoadBalancingServerSelector(LoadBalancingServerGroupConfigurationElement group)
    {
      this.Group = group;
    }

    public abstract LoadBalancingServerConfigurationElement GetServer(bool master);
  }

  /// <summary>
  /// 
  /// </summary>
  public class LoadBalancingRoundRobinSelector : LoadBalancingServerSelector
  {
    int index = -1;
    private int nextServer = 0;

    public LoadBalancingRoundRobinSelector(LoadBalancingServerGroupConfigurationElement group) : base(group)
    {
    }

    public override LoadBalancingServerConfigurationElement GetServer(bool master)
    {
      for (int i = 0; i < Group.Servers.Count; i++)
      {
        LoadBalancingServerConfigurationElement server = Group.GetElementAt(nextServer);
        nextServer = ++nextServer % Group.Servers.Count;
        //if (srv.Status == ServerStatusEnum.Unavailable) continue;
        if (!server.IsAvailable || (master && !server.IsMaster)) continue;
        return server;
      }

      return null;
    }
  }
}
