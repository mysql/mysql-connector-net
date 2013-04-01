using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI.Xaml;

namespace MySql.Data
{
    internal class Timer
    {
      TimeSpan period;
      TimerCallback handler;
      private int startDelay;

      public Timer(TimerCallback callback, object state, Int32 dueTime, Int32 periodInSeconds)
      {
        period = new TimeSpan(0, 0, periodInSeconds);
        handler = callback;
        startDelay = dueTime;
        ThreadPoolTimer tpt = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(Elapsed), period);
      }

      private void Elapsed(ThreadPoolTimer timer)
      {
        if (startDelay > 0)
        {
          startDelay -= (int)period.TotalSeconds;
          if (startDelay > 0) return;
        }

        if (handler != null)
          handler(null);
      }
    }

    public delegate void TimerCallback(Object state);
}
