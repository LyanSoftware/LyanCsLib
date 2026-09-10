using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Lytec.AvaloniaUI;
using Lytec.Common.Localization;
using Lytec.Common.Localization.Extensions;

namespace Lytec.AvaloniaUI.Mvu;

public interface IWindowManager
{
    Window Create(Control view, string? title = null);

    Window InstallMainWindow(Control view);

    Task ShowDialogAsync(Control view, Control? ownerView = null, string? title = null);

    void Close(Control view);

    Task<bool> TryCloseAsync(Control view);

    Task<bool> TryShutdownAsync(int exitCode = 0, bool mayBeTerminatedBySystem = false);
}

internal sealed class WindowManager : IWindowManager
{
    private const string LocalizeScope = "Lytec.AvaloniaUI.Mvu.WindowManager";
    private static LocalizeString Localize(string Key, object? Arguments = null, string? DefaultMessage = null)
    => new(LocalizeScope, Key, Arguments, DefaultMessage);
    private string i18n(string Key, object? Arguments = null, string? DefaultMessage = null)
    => localizer.Format(LocalizeScope, Key, Arguments, DefaultMessage);

    private readonly Dictionary<Window, ManagedWindow> managedWindows = [];
    private readonly IClassicDesktopStyleApplicationLifetime desktop;
    private readonly ILocalizer localizer;
    private readonly AvaloniaMvuOptions options;
    private Task<bool>? applicationCloseTask;
    private bool applicationCloseInProgress;
    private bool nativeApplicationCloseAllowed;
    private bool disposed;

    public WindowManager(
        IClassicDesktopStyleApplicationLifetime desktop,
        ILocalizer localizer,
        AvaloniaMvuOptions options)
    {
        this.desktop = desktop;
        this.localizer = localizer;
        this.options = options;
        desktop.ShutdownRequested += OnShutdownRequested;
    }

    public Window Create(Control view, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (view is not IWindowView windowView)
            throw new ArgumentException(
                i18n(
                    "CreateError_WindowViewRequired",
                    new
                    {
                        ViewType = view.GetType().FullName,
                        InterfaceType = nameof(IWindowView),
                    },
                    "类型“{ViewType}”必须实现 {InterfaceType}。"),
                nameof(view));

        var options = windowView.WindowOptions;

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

    public Window InstallMainWindow(Control view)
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

    public async Task ShowDialogAsync(
        Control view,
        Control? ownerView = null,
        string? title = null)
    {
        var owner = (ownerView is not null
            ? TopLevel.GetTopLevel(ownerView) as Window
            : desktop.MainWindow)
            ?? throw new InvalidOperationException()
                .Localize(
                    LocalizeScope,
                    "ShowDialogError_MainWindowNotFound",
                    "显示对话框前必须先创建主窗口"
                    );
        var window = Create(view, title);
        await window.ShowDialog(owner);
    }

    public void Close(Control view)
    {
        (TopLevel.GetTopLevel(view) as Window)?.Close();
    }

    public Task<bool> TryCloseAsync(Control view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return TopLevel.GetTopLevel(view) is Window window
            ? TryCloseAsync(window)
            : Task.FromResult(false);
    }

    public Task<bool> TryShutdownAsync(
        int exitCode = 0,
        bool mayBeTerminatedBySystem = false)
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return Dispatcher.UIThread.InvokeAsync(
                () => TryShutdownAsync(exitCode, mayBeTerminatedBySystem));

        if (applicationCloseTask is not null)
            return applicationCloseTask;

        applicationCloseInProgress = true;
        applicationCloseTask = RunApplicationCloseAndResetAsync(
            exitCode,
            mayBeTerminatedBySystem);
        return applicationCloseTask;
    }

    private void RegisterWindow(Window window, WindowInfo options)
    {
        var managedWindow = new ManagedWindow(window, view: window.Content as ILeaveAware, options);
        managedWindows.Add(window, managedWindow);
        window.Closing += OnWindowClosing;
        window.Closed += OnWindowClosed;
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (sender is not Window window
            || !managedWindows.TryGetValue(window, out var managedWindow)
            || managedWindow.NativeCloseAllowed
            || nativeApplicationCloseAllowed)
            return;

        e.Cancel = true;

        if (applicationCloseInProgress)
            return;

        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
        {
            await TryShutdownAsync(
                mayBeTerminatedBySystem: e.CloseReason is WindowCloseReason.OSShutdown);

            return;
        }

        if (ReferenceEquals(window, desktop.MainWindow)
            && options.MainWindowCloseBehavior is MainWindowCloseBehavior.ExitApplication)
        {
            await TryShutdownAsync();
            return;
        }

        await TryCloseAsync(managedWindow, e.CloseReason, e.IsProgrammatic);
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is not Window window)
            return;

        window.Closing -= OnWindowClosing;
        window.Closed -= OnWindowClosed;
        managedWindows.Remove(window);
    }

    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (nativeApplicationCloseAllowed)
            return;

        e.Cancel = true;
        await TryShutdownAsync(mayBeTerminatedBySystem: true);
    }

    private Task<bool> TryCloseAsync(Window window)
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return Dispatcher.UIThread.InvokeAsync(() => TryCloseAsync(window));

        if (applicationCloseInProgress
            || !managedWindows.TryGetValue(window, out var managedWindow))
            return Task.FromResult(false);

        return TryCloseAsync(
            managedWindow,
            WindowCloseReason.WindowClosing,
            isProgrammatic: true);
    }

    private Task<bool> TryCloseAsync(
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

    private async Task<bool> CloseWindows(
        IReadOnlyList<ManagedWindow> windows,
        WindowCloseContext[] contexts,
        Window? retainedWindow)
    {
        var enabled = windows.Select(w => w.Window.IsEnabled).ToList();
        foreach (var window in windows)
            window.Window.IsEnabled = false;

        var closedCount = 0;
        try
        {
            for (var i = 0; i < windows.Count; i++)
            {
                var w = windows[i];
                try
                {
                    w.Window.IsEnabled = true;
                    if (!await CanCloseAsync(w, contexts[i]))
                        return false;
                    w.Window.IsEnabled = false;
                    await CleanupAsync(w, contexts[i]);
                    if (!ReferenceEquals(w.Window, retainedWindow))
                    {
                        bool ok = false;
                        void handler(object? sender, EventArgs? args) => ok = true;
                        try
                        {
                            w.NativeCloseAllowed = true;
                            w.Window.Closed += handler;
                            w.Window.Close();
                        }
                        finally
                        {
                            w.NativeCloseAllowed = ok;
                            w.Window.Closed -= handler;
                        }
                        if (!ok)
                        {
                            w.Window.IsEnabled = true;
                            return false;
                        }
                        closedCount++;
                    }
                }
                catch (Exception ex)
                {
                    w.Window.IsEnabled = true;
                    await ShowCloseErrorAsync(w, contexts[i], ex);
                    return false;
                }
            }
            return true;
        }
        finally
        {
            for (var i = closedCount; i < windows.Count; i++)
            {
                // i==closedCount为失败的那个窗口，或是退出时保留的 MainWindow。
                // 后续窗口的IsEnabled都没有被临时设为true过
                if (enabled[i] || i == closedCount)
                    windows[i].Window.IsEnabled = true;
                windows[i].CloseTask = null;
            }
        }
    }

    private async Task<bool> RunWindowCloseAndResetAsync(
        IReadOnlyList<ManagedWindow> windows,
        ManagedWindow rootWindow,
        WindowCloseReason reason,
        bool isProgrammatic)
    {
        await Task.Yield();

        var contexts = windows.Select(window => new WindowCloseContext(
            window.Window,
            ReferenceEquals(window, rootWindow)
                ? reason
                : WindowCloseReason.OwnerWindowClosing,
            isProgrammatic,
            IsApplicationExit: false,
            MayBeTerminatedBySystem: false)).ToArray();

        if (!await CloseWindows(windows, contexts, retainedWindow: null))
            return false;

        return true;
    }

    private async Task<bool> RunApplicationCloseAndResetAsync(
        int exitCode,
        bool mayBeTerminatedBySystem)
    {
        await Task.Yield();

        try
        {
            var pendingWindowCloses = managedWindows.Values
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

            if (!await CloseWindows(windows, contexts, desktop.MainWindow))
                return false;

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

    private IEnumerable<ManagedWindow> OrderForApplicationClose(
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

    private ManagedWindow GetManagedWindow(Window window)
    {
        return managedWindows.TryGetValue(window, out var managedWindow)
            ? managedWindow
            : new ManagedWindow(window, window.Content as ILeaveAware, new WindowInfo());
    }

    private IEnumerable<ManagedWindow> OrderOwnedWindowGroup(Window rootWindow)
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
        if (callback is not null
            && await callback(context) is LeaveDecision.Cancel)
            return false;

        if (managedWindow.View is null)
            return true;

        return await managedWindow.View.TryLeaveAsync(ToLeaveContext(context))
            is LeaveDecision.Allow;
    }

    private static async ValueTask CleanupAsync(
        ManagedWindow managedWindow,
        WindowCloseContext context)
    {
        var callback = managedWindow.Options.CleanupAsync;
        if (callback is not null)
            await callback(context);

        if (managedWindow.View is not null)
            await managedWindow.View.CleanupAsync(ToLeaveContext(context));
    }

    private async ValueTask ShowCloseErrorAsync(
        ManagedWindow managedWindow,
        WindowCloseContext context,
        Exception exception)
    {
        if (managedWindow.Options.CloseErrorAsync is { } callback)
        {
            await callback(context, exception);
            return;
        }

        var dialog = new Window
        {
            Title = i18n("CloseErrorWindowTitle", DefaultMessage: "无法关闭窗口"),
            Width = 480,
            Height = 240,
            MinWidth = 320,
            MinHeight = 180,
            CanResize = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var closeButton = new Button
        {
            Content = i18n("CloseButton", DefaultMessage: "关闭"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
        };
        closeButton.Click += (_, _) => dialog.Close();
        dialog.Content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBox
                {
                    Text = (exception.GetLocalizedMessage() is { } lstr ? localizer.Format(lstr) : null)
                        ?? exception.Message,
                    IsReadOnly = true,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    AcceptsReturn = true,
                },
                closeButton,
            },
        };
        Grid.SetRow(closeButton, 1);
        await dialog.ShowDialog(managedWindow.Window);
    }

    private static LeaveContext ToLeaveContext(WindowCloseContext context)
        => new(
            context.IsApplicationExit
                ? LeaveReason.ApplicationExit
                : LeaveReason.WindowClose,
            context.Window.Content ?? context.Window,
            IsProgrammatic: context.IsProgrammatic,
            MayBeTerminatedBySystem: context.MayBeTerminatedBySystem);

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

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        desktop.ShutdownRequested -= OnShutdownRequested;
        foreach (var window in managedWindows.Keys.ToArray())
        {
            window.Closing -= OnWindowClosing;
            window.Closed -= OnWindowClosed;
        }
        managedWindows.Clear();
    }

    private sealed class ManagedWindow(
        Window window,
        ILeaveAware? view,
        WindowInfo options)
    {
        public Window Window { get; } = window;
        public ILeaveAware? View { get; } = view;
        public WindowInfo Options { get; } = options;
        public Task<bool>? CloseTask { get; set; }
        public bool NativeCloseAllowed { get; set; }
    }
}
