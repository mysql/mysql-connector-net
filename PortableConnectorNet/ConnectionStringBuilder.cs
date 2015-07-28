using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Communication
{
  internal class ConnectionStringBuilder
  {
    private string connectionString;

    public ConnectionStringBuilder(string connectionString)
    {     
      this.connectionString = connectionString;
    }
  }
}
