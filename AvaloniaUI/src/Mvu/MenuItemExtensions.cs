using Avalonia.Controls;
using Lytec.AvaloniaUI.Mvu.SimpleTheme;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.AvaloniaUI.Mvu;

public static class MenuItemExtensions
{
    public static T ShowIconColumn<T>(
        this T control,
        bool value)
        where T : MenuItem
    {
        MenuItemLayout.SetShowIconColumn(control, value);
        return control;
    }
}
