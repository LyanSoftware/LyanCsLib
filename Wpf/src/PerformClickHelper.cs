using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;


namespace Lytec.Wpf;

public static class PerformClickHelper
{
    private static readonly MethodInfo? OnClickMethod =
        typeof(ButtonBase).GetMethod(
            "OnClick",
            BindingFlags.Instance | BindingFlags.NonPublic);

    public static void PerformClick(this ButtonBase button)
    {
        if (button?.IsEnabled != true)
            return;
        if (!button.IsVisible || !button.IsLoaded)
            return;

        OnClickMethod?.Invoke(button, null);
    }
}
