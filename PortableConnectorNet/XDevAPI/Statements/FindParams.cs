using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.XDevAPI.Statements
{
  internal class FindParams : FilterParams
  {
    public string[] GroupBy;
    public string GroupByCritieria;
    public string[] Projection;
  }
}
