using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Lytec.Common;

namespace Lytec.Wpf;

public static class TimerExtensions
{
    public static void Restart(this DispatcherTimer timer, bool triggerEventNow = false)
    {
        timer.Stop();
        if (triggerEventNow)
            timer.TriggerEvent(nameof(timer.Tick));
        timer.Start();
    }
}
