using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Lytec.Common.Localization.Extensions;

namespace Lytec.AvaloniaUI.Mvu;

public static class WindowManager
{
    private const string LocalizeScope = "Lytec.AvaloniaUI.Mvu.WindowManager";

    public static Window Create<TView>(
        TView view,
        string? title = null)
        where TView : Control, IWindowView
    {
        var options = view.WindowOptions;

        return new Window
        {
            Content = view,
            Title = title ?? options.Title,
            Width = options.Width,
            Height = options.Height,
            MinWidth = options.MinWidth,
            MinHeight = options.MinHeight,
            CanResize = options.CanResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
    }

    public static Window InstallMainWindow<TView>(
        this IClassicDesktopStyleApplicationLifetime desktop,
        TView view)
        where TView : Control, IWindowView
    {
        if (desktop.MainWindow is not null)
            throw new InvalidOperationException()
                .Localize(
                    LocalizeScope,
                    "InstallMainWindowError_MultipleInstallMainWindow",
                    "主窗口已经设置。"
                    );

        var window = Create(view);
        desktop.MainWindow = window;
        return window;
    }

    public static async Task ShowDialogAsync<TView>(
        this TView view,
        Control? ownerView = null,
        string? title = null)
        where TView : Control, IWindowView
    {
        var owner = (ownerView is not null
            ? TopLevel.GetTopLevel(ownerView) as Window
            : GetMainWindow())
            ?? throw new InvalidOperationException()
                .Localize(
                    LocalizeScope,
                    "ShowDialogError_MainWindowNotFound",
                    "显示对话框前必须先创建主窗口"
                    );
        var window = Create(view, title);
        await window.ShowDialog(owner);
    }

    public static void Close(Control view)
    {
        (TopLevel.GetTopLevel(view) as Window)?.Close();
    }

    private static Window? GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
    }
}