using System;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Lytec.Common;

namespace Lytec.Wpf;

public static class LabelClickBehavior
{
    public static readonly DependencyProperty TriggerTargetClickProperty =
        DependencyProperty.RegisterAttached(
            "TriggerTargetClick",
            typeof(bool),
            typeof(LabelClickBehavior),
            new PropertyMetadata(false, OnTriggerTargetClickChanged));

    public static bool GetTriggerTargetClick(DependencyObject obj) => (bool)obj.GetValue(TriggerTargetClickProperty);
    public static void SetTriggerTargetClick(DependencyObject obj, bool value) => obj.SetValue(TriggerTargetClickProperty, value);

    static readonly MouseButtonEventHandler MouseDownEventHandler = (sender, e) =>
    {
        if (sender is Label label)
        {
            label.Target?.Focus();
            if (label.Target is ButtonBase btn)
                btn.PerformClick();
        }
    };

    private static void OnTriggerTargetClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Label label)
        {
            label.MouseDown -= MouseDownEventHandler;
            if ((bool)e.NewValue)
                label.MouseDown += MouseDownEventHandler;
        }
        else throw new InvalidOperationException();
    }
}
