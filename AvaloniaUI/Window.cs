using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;
using Avalonia;
using AvaWindow = Avalonia.Controls.Window;
using Lytec.Common;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using Win32 = Windows.Win32.PInvoke;
using Avalonia.Rendering;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Platform;

namespace Lytec.AvaloniaUI;

public static class Utils
{
    public static async Task ShowDialog(this AvaWindow window, TopLevel top)
    {
        if (Design.IsDesignMode)
            return;

        if (top is AvaWindow w)
            await window.ShowDialog(w);
        else window.Show();
    }

    public static async Task ShowDialog(this AvaWindow window, Visual? v = null)
    {
        if (Design.IsDesignMode)
            return;

        if (TopLevel.GetTopLevel(v) is AvaWindow w)
            await window.ShowDialog(w);
        else window.Show();
    }

    public static void RemoveWindowIcon(this AvaWindow window)
    {
        window.Icon = null;
        if (XPlat.CurrentPlatform == XPlat.Platform.Windows)
        {
            var phandle = window.TryGetPlatformHandle();
            var handle = phandle?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero)
                return;
            var hwnd = new Windows.Win32.Foundation.HWND(handle);
            var style = Win32.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            Win32.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, style | (int)WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME);
            Win32.SendMessage(hwnd, Win32.WM_SETICON, 0, 0);
        }
    }

    public static bool TrySetWindowState([AllowNull] this IRenderRoot root, WindowState state)
    {
        if (root is AvaWindow w)
        {
            w.WindowState = state;
            return true;
        }
        return false;
    }

    public static bool TryGetWindowState([AllowNull] this IRenderRoot root, out WindowState State)
    {
        if (root is AvaWindow w)
        {
            State = w.WindowState;
            return true;
        }
        State = WindowState.Normal;
        return false;
    }

    public static bool CloseWindow([AllowNull] this IRenderRoot root)
    {
        if (root is AvaWindow w)
        {
            w.Close();
            return true;
        }
        return false;
    }

    public static bool UseCustomTitleBar(this AvaWindow w)
    {
        switch (XPlat.CurrentPlatform)
        {
            default:
            case XPlat.Platform.Unknown:
                return false;
            case XPlat.Platform.Windows:
                w.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
                w.ExtendClientAreaTitleBarHeightHint = -1;
                w.ExtendClientAreaToDecorationsHint = true;
                return true;
            case XPlat.Platform.Linux:
                // 需要特殊处理
                //w.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.SystemChrome;
                //w.ExtendClientAreaTitleBarHeightHint = -1;
                //w.ExtendClientAreaToDecorationsHint = false;
                return false;
        }

    }
}
