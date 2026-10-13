using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common;

public static class TimerExtensions
{
    public static void Restart(this System.Timers.Timer timer)
    {
        timer.Stop();
        timer.Start();
    }
}
