using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading
{
  internal class Thread
  {
    public static void Sleep(int milliseconds)
    {
      var delay = Task.Delay(milliseconds);
      delay.Wait();
    }
  }
}
