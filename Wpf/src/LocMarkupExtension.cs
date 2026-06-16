using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;
using Lytec.Common;

namespace Lytec.Wpf;

public class LocExtension : MarkupExtension
{
    public string Key { get; }

    public LocExtension(string key) => Key = Regex.Unescape(key);

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (IsInDesignMode())
            return Key;
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target)
        {
            if (target.TargetObject is DependencyObject obj
                && target.TargetProperty is DependencyProperty dp)
            {
                if (obj.ReadLocalValue(dp) is not BindingExpression)
                {
                    var binding = new Binding($"Localizer[{Key}]")
                    {
                        Mode = BindingMode.OneWay,
                        FallbackValue = Key
                    };
                    obj.Dispatcher.InvokeAsync(() => BindingOperations.SetBinding(obj, dp, binding));
                }
            }
        }
        return Key;
    }

    public static bool IsInDesignMode()
    {
        return DesignerProperties.GetIsInDesignMode(new DependencyObject());
        //return Avalonia.Controls.Design.IsDesignMode;
    }
}
