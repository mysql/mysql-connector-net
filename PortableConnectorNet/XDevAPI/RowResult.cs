using MySql.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.XDevAPI
{
  public class RowResult : Result
  {
    internal RowResult(ProtocolBase p) : base(p)
    {
    }

    public List<ResultRow> Rows
    {
      get { return _activeResults.Rows;  }
    }
  }
}
