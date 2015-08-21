using MySql.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.XDevAPI
{
  public class DocumentResult : Result
  {
    internal DocumentResult(ProtocolBase p) : base(p)
    {
    }
  }
}
