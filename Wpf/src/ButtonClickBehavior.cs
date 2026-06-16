using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Lytec.Wpf;

public static class ButtonClickBehavior
{
    public static readonly DependencyProperty ClickTriggersProperty =
        DependencyProperty.RegisterAttached(
            "ClickTriggers",
            typeof(ClickTrigger),
            typeof(ButtonClickBehavior),
            new PropertyMetadata(ClickTrigger.None, OnClickTriggerChanged));

    public static void SetClickTriggers(DependencyObject obj, ClickTrigger value)
    {
        obj.SetValue(ClickTriggersProperty, value);
    }

    public static ClickTrigger GetClickTriggers(DependencyObject obj)
    {
        return (ClickTrigger)obj.GetValue(ClickTriggersProperty);
    }

    public static readonly DependencyProperty IsButtonDownProperty =
        DependencyProperty.RegisterAttached(
            "IsButtonDown",
            typeof(bool),
            typeof(ButtonClickBehavior),
            new PropertyMetadata(false));

    public static void SetIsButtonDown(DependencyObject obj, bool value)
    {
        obj.SetValue(IsButtonDownProperty, value);
    }

    public static bool GetIsButtonDown(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsButtonDownProperty);
    }

    private static void OnClickTriggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ButtonBase button)
            return;

        button.PreviewMouseDown -= Button_PreviewMouseDown;
        button.PreviewMouseUp -= Button_PreviewMouseUp;
        button.PreviewTouchDown -= Button_TouchDown;
        button.PreviewTouchUp -= Button_TouchUp;
        button.PreviewStylusDown -= Button_StylusDown;
        button.PreviewStylusUp -= Button_StylusUp;

        var mode = (ClickTrigger)e.NewValue;
        if (mode == ClickTrigger.None)
            return;

        if ((mode & ClickTrigger.Mouse) != 0)
        {
            button.PreviewMouseDown += Button_PreviewMouseDown;
            button.PreviewMouseUp += Button_PreviewMouseUp;
        }

        if ((mode & ClickTrigger.Touch) != 0)
        {
            button.PreviewTouchDown += Button_TouchDown;
            button.PreviewTouchUp += Button_TouchUp;
        }

        if ((mode & ClickTrigger.Stylus) != 0)
        {
            button.PreviewStylusDown += Button_StylusDown;
            button.PreviewStylusUp += Button_StylusUp;
        }
    }

    private static bool ButtonDown(ButtonBase btn, ClickTrigger current)
    {
        if ((GetClickTriggers(btn) & current) == 0)
            return false;

        SetIsButtonDown(btn, true);
        if (btn is ToggleButton tb)
        {
            tb.IsChecked = true;
            return true;
        }
        return false;
    }

    private static bool ButtonUp(ButtonBase btn, ClickTrigger current)
    {
        if ((GetClickTriggers(btn) & current) == 0)
            return false;
        var open = GetIsButtonDown(btn);
        SetIsButtonDown(btn, false);
        if (btn is ToggleButton tb)
            tb.IsChecked = false;
        if (open)
        {
            if (btn.Command?.CanExecute(btn.CommandParameter) == true)
                btn.Command.Execute(btn.CommandParameter);
            btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btn));
        }
        return open;
    }

    private static ClickTrigger GetClickTrigger(MouseButton mouseBtn) => mouseBtn switch
    {
        MouseButton.Left => ClickTrigger.Left,
        MouseButton.Right => ClickTrigger.Right,
        MouseButton.Middle => ClickTrigger.Middle,
        MouseButton.XButton1 => ClickTrigger.XButton1,
        MouseButton.XButton2 => ClickTrigger.XButton2,
        _ => ClickTrigger.None
    };

    private static void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ButtonBase btn) return;
        e.Handled = ButtonDown(btn, GetClickTrigger(e.ChangedButton));
    }

    private static void Button_TouchDown(object sender, TouchEventArgs e)
    {
        if (sender is not ButtonBase btn) return;
        e.Handled = ButtonDown(btn, ClickTrigger.Touch);
    }

    private static void Button_StylusDown(object sender, StylusDownEventArgs e)
    {
        if (sender is not ButtonBase btn) return;
        e.Handled = ButtonDown(btn, ClickTrigger.Stylus);
    }

    private static void Button_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ButtonBase btn) return;
        e.Handled = ButtonUp(btn, GetClickTrigger(e.ChangedButton));
    }

    private static void Button_TouchUp(object sender, TouchEventArgs e)
    {
        if (sender is not ButtonBase btn) return;
        e.Handled = ButtonUp(btn, ClickTrigger.Touch);
    }

    private static void Button_StylusUp(object sender, StylusEventArgs e)
    {
        if (sender is not ButtonBase btn) return;
        e.Handled = ButtonUp(btn, ClickTrigger.Stylus);
    }
}
