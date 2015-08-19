using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.XDevAPI
{
  public class Column
  {
    internal Column(string name)
    {
      Name = name;
    }

    public string Name { get; }
  }
}
