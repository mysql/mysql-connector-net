using System;
using System.Collections.Generic;
using System.Text;

namespace MySql.Data.MySqlClient.Replication
{
  public class ReplicationServer
  {
    internal ReplicationServer(string name, bool isMaster, string connectionString)
    {
      Name = name;
      IsMaster = isMaster;
      ConnectionString = connectionString;
    }

    public string Name { get; private set; }
    public bool IsMaster { get; private set; }
    public string ConnectionString { get; private set; }
    internal bool IsAvailable { get; set; }
  }
}
