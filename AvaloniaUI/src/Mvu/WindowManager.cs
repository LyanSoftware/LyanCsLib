using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Lytec.Common.Localization;
using Lytec.Common.Localization.Extensions;

namespace Lytec.AvaloniaUI.Mvu;

public static class WindowManager
{
    private const string LocalizeScope = "Lytec.AvaloniaUI.Mvu.WindowManager";
    private static LocalizeString Localize(string Key, object? Arguments = null, string? DefaultMessage = null)
    => new(LocalizeScope, Key, Arguments, DefaultMessage);

    private static readonly Dictionary<Window, ManagedWindow> ManagedWindows = [];
    private static IClassicDesktopStyleApplicationLifetime? installedDesktop;
    private static Task<bool>? applicationCloseTask;
    private static bool applicationCloseInProgress;
    private static bool nativeApplicationCloseAllowed;

    public static Window Create<TView>(
        TView view,
        string? title = null)
        where TView : Control, IWindowView
    {
        var options = view.WindowOptions;

        var window = new Window
        {
            Content = view,
            Title = title ?? options.Title,
            Width = options.Width,
            Height = options.Height,
            MinWidth = options.MinWidth,
            MinHeight = options.MinHeight,
            CanResize = options.CanResize,
            ClosingBehavior = WindowClosingBehavior.OwnerWindowOnly,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        RegisterWindow(window, options);
        InitializeRootFocus(window, view);
        return window;
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
        InstallDesktopLifetime(desktop);
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

    public static Task<bool> TryCloseAsync(Control view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return TopLevel.GetTopLevel(view) is Window window
            ? TryCloseAsync(window)
            : Task.FromResult(false);
    }

    public static Task<bool> TryShutdownAsync(
        IClassicDesktopStyleApplicationLifetime desktop,
        int exitCode = 0,
        bool mayBeTerminatedBySystem = false)
    {
        ArgumentNullException.ThrowIfNull(desktop);

        if (!Dispatcher.UIThread.CheckAccess())
            return Dispatcher.UIThread.InvokeAsync(
                () => TryShutdownAsync(desktop, exitCode, mayBeTerminatedBySystem));

        InstallDesktopLifetime(desktop);

        if (applicationCloseTask is not null)
            return applicationCloseTask;

        applicationCloseInProgress = true;
        applicationCloseTask = RunApplicationCloseAndResetAsync(
            desktop,
            exitCode,
            mayBeTerminatedBySystem);
        return applicationCloseTask;
    }

    private static Window? GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
    }

    private static void RegisterWindow(Window window, WindowInfo options)
    {
        var managedWindow = new ManagedWindow(window, options);
        ManagedWindows.Add(window, managedWindow);
        window.Closing += OnWindowClosing;
        window.Closed += OnWindowClosed;
    }

    private static void InstallDesktopLifetime(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (ReferenceEquals(installedDesktop, desktop))
            return;

        if (installedDesktop is not null)
            installedDesktop.ShutdownRequested -= OnShutdownRequested;

        installedDesktop = desktop;
        installedDesktop.ShutdownRequested += OnShutdownRequested;
    }

    private static async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (sender is not Window window
            || !ManagedWindows.TryGetValue(window, out var managedWindow)
            || managedWindow.NativeCloseAllowed
            || nativeApplicationCloseAllowed)
            return;

        e.Cancel = true;

        if (applicationCloseInProgress)
            return;

        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
        {
            if (Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop)
            {
                await TryShutdownAsync(
                    desktop,
                    mayBeTerminatedBySystem: e.CloseReason is WindowCloseReason.OSShutdown);
            }

            return;
        }

        await TryCloseAsync(managedWindow, e.CloseReason, e.IsProgrammatic);
    }

    private static void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is not Window window)
            return;

        window.Closing -= OnWindowClosing;
        window.Closed -= OnWindowClosed;
        ManagedWindows.Remove(window);
    }

    private static async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (nativeApplicationCloseAllowed)
            return;

        e.Cancel = true;
        if (installedDesktop is not null)
            await TryShutdownAsync(installedDesktop, mayBeTerminatedBySystem: true);
    }

    private static Task<bool> TryCloseAsync(Window window)
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return Dispatcher.UIThread.InvokeAsync(() => TryCloseAsync(window));

        if (applicationCloseInProgress
            || !ManagedWindows.TryGetValue(window, out var managedWindow))
            return Task.FromResult(false);

        return TryCloseAsync(
            managedWindow,
            WindowCloseReason.WindowClosing,
            isProgrammatic: true);
    }

    private static Task<bool> TryCloseAsync(
        ManagedWindow managedWindow,
        WindowCloseReason reason,
        bool isProgrammatic)
    {
        if (managedWindow.CloseTask is not null)
            return managedWindow.CloseTask;

        var windows = OrderOwnedWindowGroup(managedWindow.Window).ToArray();
        var pendingClose = windows
            .Select(static window => window.CloseTask)
            .FirstOrDefault(static task => task is not null);
        if (pendingClose is not null)
            return pendingClose;

        var closeTask = RunWindowCloseAndResetAsync(
            windows,
            managedWindow,
            reason,
            isProgrammatic);
        foreach (var window in windows)
            window.CloseTask = closeTask;
        return closeTask;
    }

    private static async Task<bool> RunWindowCloseAndResetAsync(
        IReadOnlyList<ManagedWindow> windows,
        ManagedWindow rootWindow,
        WindowCloseReason reason,
        bool isProgrammatic)
    {
        await Task.Yield();

        try
        {
            var contexts = windows.Select(window => new WindowCloseContext(
                window.Window,
                ReferenceEquals(window, rootWindow)
                    ? reason
                    : WindowCloseReason.OwnerWindowClosing,
                isProgrammatic,
                IsApplicationExit: false,
                MayBeTerminatedBySystem: false)).ToArray();

            for (var i = 0; i < windows.Count; i++)
            {
                if (!await CanCloseAsync(windows[i], contexts[i]))
                    return false;
            }

            foreach (var window in windows)
                window.Window.Hide();

            for (var i = 0; i < windows.Count; i++)
                await CleanupAsync(windows[i], contexts[i]);

            foreach (var window in windows)
                window.NativeCloseAllowed = true;
            try
            {
                rootWindow.Window.Close();
            }
            finally
            {
                foreach (var window in windows)
                    window.NativeCloseAllowed = false;
            }

            return true;
        }
        finally
        {
            foreach (var window in windows)
                window.CloseTask = null;
        }
    }

    private static async Task<bool> RunApplicationCloseAndResetAsync(
        IClassicDesktopStyleApplicationLifetime desktop,
        int exitCode,
        bool mayBeTerminatedBySystem)
    {
        await Task.Yield();

        try
        {
            var pendingWindowCloses = ManagedWindows.Values
                .Select(static window => window.CloseTask)
                .Where(static task => task is not null)
                .Cast<Task<bool>>()
                .Distinct()
                .ToArray();
            if (pendingWindowCloses.Length > 0)
                await Task.WhenAll(pendingWindowCloses);

            var windows = OrderForApplicationClose(desktop).ToArray();
            var reason = mayBeTerminatedBySystem
                ? WindowCloseReason.OSShutdown
                : WindowCloseReason.ApplicationShutdown;
            var contexts = windows.Select(window => new WindowCloseContext(
                window.Window,
                reason,
                IsProgrammatic: !mayBeTerminatedBySystem,
                IsApplicationExit: true,
                MayBeTerminatedBySystem: mayBeTerminatedBySystem)).ToArray();

            for (var i = 0; i < windows.Length; i++)
            {
                if (!await CanCloseAsync(windows[i], contexts[i]))
                    return false;
            }

            foreach (var window in windows)
                window.Window.Hide();

            for (var i = 0; i < windows.Length; i++)
                await CleanupAsync(windows[i], contexts[i]);

            nativeApplicationCloseAllowed = true;
            try
            {
                return desktop.TryShutdown(exitCode);
            }
            finally
            {
                nativeApplicationCloseAllowed = false;
            }
        }
        finally
        {
            applicationCloseInProgress = false;
            applicationCloseTask = null;
        }
    }

    private static IEnumerable<ManagedWindow> OrderForApplicationClose(
        IClassicDesktopStyleApplicationLifetime desktop)
    {
        return desktop.Windows
            .Select((window, index) => new
            {
                Managed = GetManagedWindow(window),
                OwnerDepth = GetOwnerDepth(window),
                IsMainWindow = ReferenceEquals(window, desktop.MainWindow),
                Index = index,
            })
            .OrderByDescending(static item => item.OwnerDepth)
            .ThenBy(static item => item.IsMainWindow)
            .ThenBy(static item => item.Index)
            .Select(static item => item.Managed);
    }

    private static ManagedWindow GetManagedWindow(Window window)
    {
        return ManagedWindows.TryGetValue(window, out var managedWindow)
            ? managedWindow
            : new ManagedWindow(window, new WindowInfo());
    }

    private static IEnumerable<ManagedWindow> OrderOwnedWindowGroup(Window rootWindow)
    {
        return EnumerateOwnedWindows(rootWindow)
            .Select(GetManagedWindow)
            .OrderByDescending(static window => GetOwnerDepth(window.Window));
    }

    private static IEnumerable<Window> EnumerateOwnedWindows(Window window)
    {
        foreach (var child in window.OwnedWindows)
        {
            foreach (var descendant in EnumerateOwnedWindows(child))
                yield return descendant;
        }

        yield return window;
    }

    private static int GetOwnerDepth(Window window)
    {
        var depth = 0;
        for (var owner = window.Owner; owner is not null; owner = owner.Owner)
            depth++;
        return depth;
    }

    private static async ValueTask<bool> CanCloseAsync(
        ManagedWindow managedWindow,
        WindowCloseContext context)
    {
        var callback = managedWindow.Options.CanCloseAsync;
        return callback is null
            || await callback(context) is WindowCloseDecision.Allow;
    }

    private static async ValueTask CleanupAsync(
        ManagedWindow managedWindow,
        WindowCloseContext context)
    {
        var callback = managedWindow.Options.CleanupAsync;
        if (callback is not null)
            await callback(context);
    }

    private static void InitializeRootFocus(Window window, Control view)
    {
        // 根视图只作为窗口初始焦点的兜底，不加入 Tab 导航顺序。
        view.Focusable = true;
        view.IsTabStop = false;

        window.Opened += OnOpened;

        void OnOpened(object? sender, EventArgs e)
        {
            window.Opened -= OnOpened;

            // 延迟到 Loaded 优先级，让子控件的自动聚焦逻辑先执行。
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (window.FocusManager?.GetFocusedElement() is null)
                        view.Focus();
                },
                DispatcherPriority.Loaded);
        }
    }

    private sealed class ManagedWindow(Window window, WindowInfo options)
    {
        public Window Window { get; } = window;
        public WindowInfo Options { get; } = options;
        public Task<bool>? CloseTask { get; set; }
        public bool NativeCloseAllowed { get; set; }
    }
}
