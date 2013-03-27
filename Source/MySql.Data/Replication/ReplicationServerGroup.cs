using MySql.Data.MySqlClient.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient.Replication
{
  public abstract class ReplicationServerGroup
  {
    List<ReplicationServer> servers = new List<ReplicationServer>();

    internal ReplicationServerGroup(string name)
    {
      Servers = servers;
    }

    public string Name { get; private set; }
    public int RetryTime { get; private set; }
    public IList<ReplicationServer> Servers { get; private set; }

    public ReplicationServer AddServer(string name, bool isMaster, string connectionString)
    {
      ReplicationServer server = new ReplicationServer(name, isMaster, connectionString);
      servers.Add(server);
      return server;
    }

    public void RemoveServer(string name)
    {
      ReplicationServer serverToRemove = GetServer(name);
      if (serverToRemove == null)
        throw new MySqlException(String.Format(Resources.ReplicationServerNotFound, name));
      servers.Remove(serverToRemove);
    }

    public ReplicationServer GetServer(string name)
    {
      foreach (var server in servers)
        if (String.Compare(name, server.Name, StringComparison.OrdinalIgnoreCase) == 0) return server;
      return null;
    }

    public abstract ReplicationServer GetServer(bool isMaster);
  }
}
