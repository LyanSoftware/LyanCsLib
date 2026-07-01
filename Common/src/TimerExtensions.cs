using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common;

public static class TimerExtensions
{
    public static void Restart(this System.Timers.Timer timer, bool triggerEventNow = false)
    {
        timer.Stop();
        if (triggerEventNow)
            timer.TriggerEvent(nameof(timer.Elapsed));
        timer.Start();
    }
}
