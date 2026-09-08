using Avalonia.Controls;
using Avalonia.Markup.Declarative;
using Avalonia.Styling;
using Lytec.AvaloniaUI.Mvu.SimpleTheme;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.AvaloniaUI.Mvu;

public static class MenuItemExtensions
{
    public static T ShowIconColumn<T>(this T control, bool value)
        where T : MenuItem
    {
        MenuItemLayout.SetShowIconColumn(control, value);
        return control;
    }

    public static Style<T> ShowIconColumn<T>(this Style<T> style, bool value)
        where T : MenuItem
    {
        style.Setters.Add(new Setter(MenuItemLayout.ShowIconColumnProperty, value));
        return style;
    }

    public static T EmptySubmenuArrowColumnWidth<T>(this T control, double value)
        where T : MenuItem
    {
        MenuItemLayout.SetEmptySubmenuArrowColumnWidth(control, value);
        return control;
    }

    public static Style<T> EmptySubmenuArrowColumnWidth<T>(this Style<T> style, double value)
        where T : MenuItem
    {
        style.Setters.Add(new Setter(MenuItemLayout.EmptySubmenuArrowColumnWidthProperty, value));
        return style;
    }

    public static T InputGestureTextTheme<T>(this T control, ControlTheme? value)
        where T : MenuItem
    {
        MenuItemLayout.SetInputGestureTextTheme(control, value);
        return control;
    }

    public static Style<T> InputGestureTextTheme<T>(this Style<T> style, ControlTheme? value)
        where T : MenuItem
    {
        style.Setters.Add(new Setter(MenuItemLayout.InputGestureTextThemeProperty, value));
        return style;
    }

}
