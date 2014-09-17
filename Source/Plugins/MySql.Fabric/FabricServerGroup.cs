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

using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Replication;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MySql.Fabric
{
  internal class FabricServerGroup : ReplicationServerGroup
  {
    private MySqlConnection fabricConnection;
    private string username;
    private string password;
    private string fabricInstance;
    private int ttl;
    protected List<FabricServer> FabricServers;
    protected DateTime lastUpdate = DateTime.MinValue;

    internal string groupIdProperty;
    internal FabricServerModeEnum? modeProperty;
    internal string tableProperty;
    internal object keyProperty;


    public FabricServerGroup(string name, int retryTime)
      : base(name, retryTime)
    {
      FabricServers = new List<FabricServer>();
    }


    internal protected override ReplicationServer GetServer(bool isMaster)
    {
      if (string.IsNullOrEmpty(groupIdProperty) && string.IsNullOrEmpty(tableProperty))
        throw new MySqlFabricException(Properties.Resources.errorNotGroupNorTable);

      FabricServerModeEnum mode = modeProperty.HasValue ? modeProperty.Value
        : (isMaster ? FabricServerModeEnum.Read_Write : FabricServerModeEnum.Read_only);

      lock (FabricServers)
      {
        var serversInGroup = FabricServers.Where(i => i.GroupId == groupIdProperty
          && (i.Mode & mode) != 0).ToList();

        if (serversInGroup.Count == 0) return null;

        double random_weight = new Random().NextDouble() * serversInGroup.Sum(i => i.Weight);
        double sum_weight = 0.0;

        foreach (FabricServer server in serversInGroup)
        {
          sum_weight += server.Weight;
          if (sum_weight > random_weight) return server.ReplicationServerInstance;
        }

        return serversInGroup.Last().ReplicationServerInstance;
      }
    }

    internal protected override ReplicationServer GetServer(bool isMaster, MySqlConnectionStringBuilder settings)
    {
      if (fabricConnection == null)
      {
        if (servers.Count == 0) throw new MySqlFabricException(Properties.Resources.errorNoFabricSettings);

        username = settings.UserID;
        password = settings.Password;

        fabricConnection = new MySqlConnection(servers[0].ConnectionString);
      }
      groupIdProperty = settings.FabricGroup;
      modeProperty = (FabricServerModeEnum?)settings.FabricServerMode;
      tableProperty = settings.ShardingTable;
      keyProperty = settings.ShardingKey;

      GetServerList();

      return GetServer(isMaster);
    }

    internal protected override void HandleFailover(ReplicationServer server, Exception exception)
    {
      ExecuteCommand("threat report_error", server.Name);
      GetServerList();
      throw exception;
    }

    public void ResetCache()
    {
      lastUpdate = DateTime.MinValue;
      GetServerList();
    }


    protected DataTable ExecuteCommand(string command, params string[] parameters)
    {
      DataTable table = null;
      using (fabricConnection)
      {
        fabricConnection.Open();

        MySqlCommand mysqlCommand = new MySqlCommand(string.Format("CALL {0}({1})",
          command.Trim().Replace(' ', '.'), string.Join(",", parameters)), fabricConnection);
        using (MySqlDataReader reader = mysqlCommand.ExecuteReader())
        {
          reader.Read();
          if (fabricInstance == null) fabricInstance = reader.GetString(0);
          if (fabricInstance != reader.GetString(0)) 
            throw new MySqlFabricException(Properties.Resources.errorFabricUuidChanged);
          ttl = reader.GetInt32(1);
          string message = reader.GetValue(2) as string;
          if (!string.IsNullOrEmpty(message)) throw new MySqlFabricException(message);
          if (reader.NextResult()) table = CreateTable(reader);
        }
      }
      return table;
    }

    protected void GetServerList()
    {
      if (lastUpdate.AddSeconds(ttl) > DateTime.Now) return;
      lock (FabricServers)
      {
        try
        {
          DataTable table = ExecuteCommand("dump servers");
          FabricServers.Clear();
          foreach (DataRow row in table.Rows)
          {
            FabricServers.Add(new FabricServer(new Guid(row["server_uuid"] as string),
              row["group_id"] as string,
              row["host"] as string,
              int.Parse(row["port"] as string),
              (FabricServerModeEnum)Enum.Parse(typeof(FabricServerModeEnum), row["mode"] as string),
              (FabricServerStatusEnum)Enum.Parse(typeof(FabricServerStatusEnum), row["status"] as string),
              float.Parse(row["weight"] as string),
              username,
              password
              ));
          }
          lastUpdate = DateTime.Now;
        }
        catch (Exception ex)
        {
          MySqlTrace.LogError(-1, ex.ToString());
          throw new MySqlFabricException(Properties.Resources.errorConnectFabricServer);
        }
      }
    }

    protected DataTable CreateTable(MySqlDataReader reader)
    {
      DataTable table = new DataTable();
      for (int i = 0; i < reader.FieldCount; i++)
      {
        table.Columns.Add(reader.GetName(i));
      }
      while (reader.Read())
      {
        DataRow row = table.NewRow();
        table.Rows.Add(row);
        for (int i = 0; i < reader.FieldCount; i++)
        {
          row[i] = reader.GetValue(i);
        }
      }

      return table;
    }
  }
}
