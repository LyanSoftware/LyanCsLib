using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Lytec.Common;

namespace Lytec.Wpf;

public static class TimerExtensions
{
    static void TriggerEvent(object obj, string name, EventArgs? e = null)
    {
        if (obj.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo field
            && field.GetValue(obj) is MulticastDelegate func)
            foreach (Delegate dlg in func.GetInvocationList())
                dlg.Method.Invoke(dlg.Target, [obj, e ?? EventArgs.Empty]);
    }

    public static void Restart(this DispatcherTimer timer, bool triggerEventNow = false)
    {
        timer.Stop();
        if (triggerEventNow)
            TriggerEvent(timer, nameof(timer.Tick));
        timer.Start();
    }
}
