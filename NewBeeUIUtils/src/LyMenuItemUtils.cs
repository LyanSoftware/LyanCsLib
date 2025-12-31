using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Markup.Declarative;

public static class LyMenuItemUtils
{
    public static T HotKey<T>(this T control, string gesture) where T : MenuItem
    => control.HotKey(KeyGesture.Parse(gesture));

    public static T HotKey<T>(this T control, Func<string> func, Action<KeyGesture>? onChanged = null, [CallerArgumentExpression(nameof(func))] string? expression = null) where T : MenuItem
    => control.HotKey(() => KeyGesture.Parse(func()), onChanged, expression);
    
    public static T InputGesture<T>(this T control, string gesture) where T : MenuItem
    => control.InputGesture(KeyGesture.Parse(gesture));

    public static T InputGesture<T>(this T control, Func<string> func, Action<KeyGesture>? onChanged = null, [CallerArgumentExpression(nameof(func))] string? expression = null) where T : MenuItem
    => control.InputGesture(() => KeyGesture.Parse(func()), onChanged, expression);
    
    public static T HotKeyAndGesture<T>(this T control, KeyGesture value) where T : MenuItem
    => control.HotKey(value).InputGesture(value);

    public static T HotKeyAndGesture<T>(this T control, Func<KeyGesture> func, Action<KeyGesture>? onChanged = null, [CallerArgumentExpression(nameof(func))] string? expression = null) where T : MenuItem
    => control.HotKey(func, onChanged, expression).InputGesture(func, onChanged, expression);

    public static T HotKeyAndGesture<T>(this T control, string gesture) where T : MenuItem
    {
        var value = KeyGesture.Parse(gesture);
        return control.HotKey(value).InputGesture(value);
    }

    public static T HotKeyAndGesture<T>(this T control, Func<string> func, Action<KeyGesture>? onChanged = null, [CallerArgumentExpression(nameof(func))] string? expression = null) where T : MenuItem
    {
        var f = () => KeyGesture.Parse(func());
        return control.HotKey(f, onChanged, expression).InputGesture(f, onChanged, expression);
    }

    public static T HotKeyAndGesture<T>(this T control, IBinding binding) where T : MenuItem
    => control.HotKey(binding).InputGesture(binding);

    public static T HotKeyAndGesture<T>(this T control, AvaloniaProperty avaloniaProperty, BindingMode? bindingMode = null, IValueConverter? converter = null, ViewBase? overrideView = null) where T : MenuItem
    => control.HotKey(avaloniaProperty, bindingMode, converter, overrideView).InputGesture(avaloniaProperty, bindingMode, converter, overrideView);

}
