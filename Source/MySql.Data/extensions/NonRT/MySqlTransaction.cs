using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace MySql.Data.MySqlClient
{
  public sealed partial class MySqlTransaction : DbTransaction
  {
    protected override DbConnection DbConnection
    {
      get { return conn; }
    }
  }

}
