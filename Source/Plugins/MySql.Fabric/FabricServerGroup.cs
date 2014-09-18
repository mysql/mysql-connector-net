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
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MySql.Fabric
{
  internal class FabricServerGroup : ReplicationServerGroup
  {
    private MySqlConnection fabricConnection;
    private string username;
    private string password;
    private string fabricInstance;
    private int ttl;
    // indexed by group_id
    private object _lockObject = new object();

    internal Dictionary<string, FabricGroup> FabricGroups;
    // indexed by mapping_id
    internal Dictionary<int, FabricShardTable> FabricShardTables;
    // Indexes by "schema.table"
    internal Dictionary<string, FabricShardTable> FabricShardTablesPerTable;
    protected DateTime lastUpdate = DateTime.MinValue;

    internal string groupIdProperty;
    internal FabricServerModeEnum? modeProperty;
    // always in the format "schema.table"
    internal string tableProperty;
    internal object keyProperty;


    public FabricServerGroup(string name, int retryTime)
      : base(name, retryTime)
    {
      FabricGroups = new Dictionary<string, FabricGroup>();
      FabricShardTables = new Dictionary<int, FabricShardTable>();
      FabricShardTablesPerTable = new Dictionary<string, FabricShardTable>();
    }

    internal protected override ReplicationServer GetServer(bool isMaster)
    {
      if (string.IsNullOrEmpty(groupIdProperty) && string.IsNullOrEmpty(tableProperty))
        throw new MySqlFabricException(Properties.Resources.errorNotGroupNorTable);

      FabricServerModeEnum mode = modeProperty.HasValue ? modeProperty.Value
        : (isMaster ? FabricServerModeEnum.Read_Write : FabricServerModeEnum.Read_only);

      lock (_lockObject)
      {
        /*
         * Pick a server using this algorithm.
         * 
         * */
        if (string.IsNullOrEmpty(tableProperty))
        {
          return GetServerByGroup(mode, groupIdProperty);
        }
        else
        {
          ReplicationServer server = GetServerByShard( mode );
          // Ensure the database is the current shard db.
          string[] db = tableProperty.Split('.');
          string conStr = server.ConnectionString;
          if( conStr.IndexOf( "database=" + db[ 0 ] + ";" ) == -1 )
          {
            MySqlConnectionStringBuilder msb = new MySqlConnectionStringBuilder(conStr);
            msb.Database = db[0];
            server.ConnectionString = msb.ToString();
          }
          // return server
          return server;
        }
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

    private ReplicationServer GetServerByShard(FabricServerModeEnum mode)
    {
      FabricShardTable shardTable = FabricShardTablesPerTable[tableProperty];
      object key = keyProperty;
      if (shardTable.TypeShard == FabricShardIndexType.Hash) key = GetMd5Hash(key.ToString());
      foreach( FabricShardIndex idx in shardTable.Indexes )
      {
        if ( ShardKeyCompare( key, idx.LowerBound, idx.Type ) >= 0 )
        {
          return GetServerByGroup(mode, idx.GroupId);
        }
      }
      if (shardTable.TypeShard == FabricShardIndexType.Hash)
      {
        return GetServerByGroup(mode, shardTable.Indexes.First().GroupId);
      }
      return null;
    }

    private ReplicationServer GetServerByGroup(FabricServerModeEnum mode, string groupId)
    {
      var serversInGroup = FabricGroups[groupId].Servers.Where(i => (i.Mode & mode) != 0).ToList();

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

    private int ShardKeyCompare(object o1, object o2, FabricShardIndexType shardType)
    {
      if (shardType == FabricShardIndexType.Range)
      {
        int v;
        DateTime dt;
        if (int.TryParse(o1.ToString(), out v))
        {
          return v - int.Parse(o2.ToString());
        }
        else if (DateTime.TryParse(o1.ToString(), out dt))
        {
          DateTime dt2 = DateTime.Parse(o2.ToString());
          return DateTime.Compare(dt, dt2);
        }
        else
        {
          return string.Compare(o1.ToString(), o2.ToString());
        }
      }
      else if (shardType == FabricShardIndexType.Hash)
      {
        return string.Compare(o1.ToString(), o2.ToString());
        //return int.Parse(GetTruncatedHexString(o1.ToString()), NumberStyles.HexNumber) - 
          //int.Parse(GetTruncatedHexString(o2.ToString()), NumberStyles.HexNumber);
      }
      else throw new NotSupportedException(Enum.GetName(typeof(FabricShardIndexType), 
        FabricShardIndexType.List));
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
      //lock (FabricServers)
      lock (_lockObject)
      {
        if (FabricGroups.Count != 0) return;
        try
        {
          GetGroups();
          GetShards();
          lastUpdate = DateTime.Now;
        }
        catch (Exception ex)
        {
          MySqlTrace.LogError(-1, ex.ToString());
          throw new MySqlFabricException(Properties.Resources.errorConnectFabricServer);
        }
      }
    }

    private void GetShards()
    {
      // Gather shard_tables
      DataTable tblShards = ExecuteCommand("dump shard_tables");
      FabricShardTables.Clear();
      FabricShardTablesPerTable.Clear();
      foreach (DataRow row in tblShards.Rows)
      {
        FabricShardTable shardTable = new FabricShardTable()
        {
          SchemaName = row["schema_name"] as string,
          TableName = row["table_name"] as string,
          ColumnName = row["column_name"] as string,
          MappingId = int.Parse(row["mapping_id"] as string)
        };
        FabricShardTables.Add(shardTable.MappingId, shardTable);
        FabricShardTablesPerTable.Add(string.Format("{0}.{1}", shardTable.SchemaName, shardTable.TableName), shardTable);
      }
      // Gather shard_index'es
      DataTable tblShardIndexes = ExecuteCommand("dump shard_index");
      foreach (DataRow rowIdx in tblShardIndexes.Rows)
      {
        FabricShardIndex Index = new FabricShardIndex()
        {
          GroupId = rowIdx["group_id"] as string,
          LowerBound = rowIdx["lower_bound"] as string,
          ShardId = int.Parse(rowIdx["shard_id"] as string),
          MappingId = int.Parse(rowIdx["mapping_id"] as string)
        };
        FabricShardTable table = FabricShardTables[Index.MappingId];
        table.Indexes.Add(Index);
      }
      // Gather global group
      DataTable tblGlobal = ExecuteCommand("dump shard_maps");
      foreach (DataRow rowGlobal in tblGlobal.Rows)
      {
        int mappingId = int.Parse(rowGlobal["mapping_id"] as string);
        FabricShardTable tbl = FabricShardTables[mappingId];
        tbl.GlobalGroupId = rowGlobal["global_group_id"] as string;
        tbl.TypeShard = (FabricShardIndexType)Enum.Parse(typeof(FabricShardIndexType), rowGlobal["type_name"] as string, true);
        tbl.Indexes.ForEach(i => i.Type = tbl.TypeShard);
      }
      foreach (KeyValuePair<int, FabricShardTable> kvp in FabricShardTables)
      {
        kvp.Value.Indexes.Sort(delegate(FabricShardIndex x, FabricShardIndex y)
        {
          return ShardKeyCompare(x.LowerBound, y.LowerBound, x.Type) * -1;
        });
      }
    }

    private void GetGroups()
    {
      DataTable tblGroups = ExecuteCommand("group lookup_groups");
      FabricGroups.Clear();
      foreach (DataRow row in tblGroups.Rows)
      {
        FabricGroup group = new FabricGroup()
        {
          GroupId = row["group_id"] as string,
          Description = row["description"] as string,
          FailureDetector = row["failure_detector"] as int?,
          MasterUuid = row["master_uuid"] as string
        };
        FabricGroups.Add(group.GroupId, group);
        DataTable tblServers = ExecuteCommand("group lookup_servers", group.GroupId);
        foreach (DataRow rowSrv in tblServers.Rows)
        {
          string[] addressSrv = (rowSrv["address"] as string).Split(':');
          string host = addressSrv[0];
          int port = int.Parse(addressSrv[1]);
          FabricServer server = new FabricServer(
            new Guid(rowSrv["server_uuid"] as string),
            group.GroupId,
            host,
            port,
            (FabricServerModeEnum)Enum.Parse(typeof(FabricServerModeEnum), rowSrv["mode"] as string, true),
            (FabricServerStatusEnum)Enum.Parse(typeof(FabricServerStatusEnum), rowSrv["status"] as string, true),
            float.Parse(rowSrv["weight"] as string),
            username,
            password);
          group.Servers.Add(server);
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

    protected string GetMd5Hash(string input)
    {
      MD5 md5 = MD5.Create();
      byte[] buffer = Encoding.UTF8.GetBytes(input);
      byte[] output = md5.ComputeHash(buffer);

      return BitConverter.ToString(output).Replace("-", "");
    }

    protected string GetTruncatedHexString(string hexString)
    {
      int max_lenght = 7;
      if (hexString.Length > max_lenght) return hexString.Substring(0, max_lenght);
      return hexString;
    }
  }
}
