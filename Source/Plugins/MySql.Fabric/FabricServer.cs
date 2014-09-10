// Copyright © 2014, Oracle and/or its affiliates. All rights reserved.
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

using MySql.Data.MySqlClient.Replication;
using System;

namespace MySql.Fabric
{
  internal class FabricServer
  {
    ReplicationServer replicationServerInstance = null;

    internal FabricGroup Group { get; private set; }

    public Guid ServerUuid { get; private set; }

    public string GroupId { get; private set; }

    public string Host { get; private set; }

    public int Port { get; private set; }

    public FabricServerModeEnum Mode { get; private set; }

    public FabricServerStatusEnum Status { get; private set; }

    public float Weight { get; private set; }

    public ReplicationServer ReplicationServerInstance { get; private set; }


    public FabricServer(Guid serverUuid, string groupId, string host, int port, FabricServerModeEnum mode, FabricServerStatusEnum status, float weight, string user, string passowrd)
    {
      ServerUuid = serverUuid;
      GroupId = groupId;
      Host = host;
      Port = port;
      Mode = mode;
      Status = status;
      Weight = weight;

      ReplicationServerInstance = new ReplicationServer(
        serverUuid.ToString(),
        mode == FabricServerModeEnum.Read_Write || mode == FabricServerModeEnum.Write_only,
        string.Format("server={0};port={1};uid={2};password={3};", host, port, user, passowrd)
        );
    }
  }
}
