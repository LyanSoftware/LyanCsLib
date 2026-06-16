using System;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

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
            // label.Target.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            switch (label.Target)
            {
                case RadioButton rb:
                    rb.IsChecked = true;
                    break;
                case ToggleButton tb:
                    tb.IsChecked = !tb.IsChecked;
                    break;
                case Button btn:
                    (new ButtonAutomationPeer(btn).GetPattern(PatternInterface.Invoke) as IInvokeProvider)?.Invoke();
                    break;
            }
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
