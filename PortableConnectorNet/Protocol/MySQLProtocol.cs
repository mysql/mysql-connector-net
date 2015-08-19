using System;
using MySql.Communication;
using MySql.Procotol;
using MySql.Data;

namespace MySql.Protocol
{
  internal class MySQLProtocol : ProtocolBase<UniversalStream>
  {
    public MySQLProtocol(MySqlConnectionStringBuilder settings) : base(settings, "")
    {

    }

    public override void OpenConnection()
    {
      throw new NotImplementedException();
    }
  }
}
