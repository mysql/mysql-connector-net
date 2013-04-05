using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections
{
  public class ArrayList : List<object>
  {
    public Array ToArray(Type type)
    {
      Array tempArray = Array.CreateInstance(type);
      Array.Copy(this.ToArray(), tempArray, this.Count);

      return tempArray;
    }
  }
}
