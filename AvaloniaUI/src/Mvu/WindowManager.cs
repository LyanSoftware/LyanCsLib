using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Threading;
using Lytec.AvaloniaUI;
using Lytec.Common.Localization;
using Lytec.Common.Localization.Extensions;

namespace Lytec.AvaloniaUI.Mvu;

internal interface IDesktopShutdownCoordinator
{
    Task<bool> TryShutdownAsync(int exitCode, bool mayBeTerminatedBySystem = false);
}

internal sealed class WindowViewManager : IViewManager, IDesktopShutdownCoordinator
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
    private readonly ViewRegistry registry;
    private readonly Dictionary<Window, WindowPathManager> pathManagers = [];
    private Task<bool>? applicationCloseTask;
    private bool applicationCloseInProgress;
    private bool nativeApplicationCloseAllowed;
    private bool disposed;

    public WindowViewManager(
        IClassicDesktopStyleApplicationLifetime desktop,
        ILocalizer localizer,
        AvaloniaMvuOptions options,
        ViewRegistry registry)
    {
        this.desktop = desktop;
        this.localizer = localizer;
        this.options = options;
        this.registry = registry;
        desktop.ShutdownRequested += OnShutdownRequested;
    }

    public ViewManagerState State { get; } = new();

    public event EventHandler? StateChanged;

    public Control InstallRoot(string routeId)
    {
        if (desktop.MainWindow is not null)
            throw new InvalidOperationException()
                .Localize(
                    LocalizeScope,
                    "InstallMainWindowError_MultipleInstallMainWindow",
                    "主窗口已经设置。");

        var lease = registry.Create(routeId);
        Window window;
        try
        {
            window = Create(lease);
        }
        catch
        {
            lease.Dispose();
            throw;
        }

        desktop.MainWindow = window;
        State.CurrentRootId = routeId;
        State.StackDepth = 1;
        RaiseStateChanged();
        return window;
    }

    internal CreatedWindow Create(string routeId)
    {
        var lease = registry.Create(routeId);
        try
        {
            return new CreatedWindow(lease.View, Create(lease));
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    private Window Create(ManagedViewLease lease, string? title = null)
    {
        var view = lease.View;
        var viewOptions = lease.ManagedView.ViewOptions;
        var windowOptions = viewOptions.Window;

        var window = new Window
        {
            Content = view,
            Title = title ?? viewOptions.Title ?? "View",
            Width = windowOptions.Width,
            Height = windowOptions.Height,
            MinWidth = windowOptions.MinWidth,
            MinHeight = windowOptions.MinHeight,
            CanResize = windowOptions.CanResize,
            SizeToContent = windowOptions.SizeToContent,
            ClosingBehavior = WindowClosingBehavior.OwnerWindowOnly,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        if (title is null && viewOptions.TitleBinding is { } titleBinding)
            window.Bind(Window.TitleProperty, titleBinding);

        RegisterWindow(window, lease, windowOptions);
        InitializeRootFocus(window, view);
        return window;
    }

    public Task<bool> OpenAsync(string routeId, Control? ownerView = null)
        => OpenPathAsync([routeId], ownerView);

    public Task<bool> ShowModalAsync(string routeId, Control? ownerView = null)
        => OpenAsync(routeId, ownerView);

    public async Task<bool> OpenPathAsync(
        IReadOnlyList<string> routeIds,
        Control? ownerView = null)
    {
        if (State.IsBusy)
            return false;
        var owner = (ownerView is not null
            ? TopLevel.GetTopLevel(ownerView) as Window
            : desktop.MainWindow)
            ?? throw new InvalidOperationException()
                .Localize(
                    LocalizeScope,
                    "ShowDialogError_MainWindowNotFound",
                    "显示对话框前必须先创建主窗口");
        while (owner.Owner is Window parent)
            owner = parent;

        if (!pathManagers.TryGetValue(owner, out var manager))
        {
            manager = new WindowPathManager(owner, this);
            pathManagers.Add(owner, manager);
            owner.Closed += OnPathRootClosed;
        }
        State.IsBusy = true;
        RaiseStateChanged();
        try
        {
            return await manager.OpenPathAsync(routeIds);
        }
        finally
        {
            State.IsBusy = false;
            RaiseStateChanged();
        }
    }

    public async Task<bool> BackAsync()
    {
        var window = desktop.Windows
            .Where(static window => window.IsVisible)
            .OrderByDescending(GetOwnerDepth)
            .FirstOrDefault();
        return window is not null
            && !ReferenceEquals(window, desktop.MainWindow)
            && await TryCloseAsync(window);
    }

    internal Task<bool> TryCloseAsync(Control view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return TopLevel.GetTopLevel(view) is Window window
            ? TryCloseAsync(window)
            : Task.FromResult(false);
    }

    public ValueTask<bool> TryLeaveAllAsync(bool mayBeTerminatedBySystem = false)
        => new(TryPrepareApplicationCloseAsync(mayBeTerminatedBySystem));

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

    private async Task<bool> TryPrepareApplicationCloseAsync(
        bool mayBeTerminatedBySystem)
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return await Dispatcher.UIThread.InvokeAsync(
                () => TryPrepareApplicationCloseAsync(mayBeTerminatedBySystem));

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
        return await CloseWindows(windows, contexts, desktop.MainWindow);
    }

    private void RegisterWindow(
        Window window,
        ManagedViewLease lease,
        WindowInfo options)
    {
        var managedWindow = new ManagedWindow(window, lease, options);
        managedWindows.Add(window, managedWindow);
        window.Opened += OnWindowOpened;
        window.Closing += OnWindowClosing;
        window.Closed += OnWindowClosed;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
        => UpdateWindowState();

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

        window.Opened -= OnWindowOpened;
        window.Closing -= OnWindowClosing;
        window.Closed -= OnWindowClosed;
        if (managedWindows.Remove(window, out var managedWindow))
            managedWindow.Lease?.Dispose();
        UpdateWindowState();
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
            : new ManagedWindow(window, null, new WindowInfo());
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

        if (managedWindow.Lease is null)
            return true;

        return await managedWindow.Lease.ManagedView.TryLeaveAsync(ToLeaveContext(context))
            is LeaveDecision.Allow;
    }

    private static async ValueTask CleanupAsync(
        ManagedWindow managedWindow,
        WindowCloseContext context)
    {
        var callback = managedWindow.Options.CleanupAsync;
        if (callback is not null)
            await callback(context);

        if (managedWindow.Lease is not null)
            await managedWindow.Lease.ManagedView.CleanupAsync(ToLeaveContext(context));
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
        if (options.LeaveErrorAsync is { } commonCallback)
        {
            await commonCallback(ToLeaveContext(context), exception);
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
        foreach (var manager in pathManagers.Values)
            manager.Dispose();
        pathManagers.Clear();
        foreach (var (window, managedWindow) in managedWindows.ToArray())
        {
            window.Opened -= OnWindowOpened;
            window.Closing -= OnWindowClosing;
            window.Closed -= OnWindowClosed;
            managedWindow.Lease?.Dispose();
        }
        managedWindows.Clear();
    }

    private sealed class ManagedWindow(
        Window window,
        ManagedViewLease? lease,
        WindowInfo options)
    {
        public Window Window { get; } = window;
        public ManagedViewLease? Lease { get; } = lease;
        public WindowInfo Options { get; } = options;
        public Task<bool>? CloseTask { get; set; }
        public bool NativeCloseAllowed { get; set; }
    }

    private void OnPathRootClosed(object? sender, EventArgs e)
    {
        if (sender is not Window rootWindow)
            return;

        rootWindow.Closed -= OnPathRootClosed;
        if (pathManagers.Remove(rootWindow, out var manager))
            manager.Dispose();
    }

    internal void UpdateWindowState()
    {
        State.StackDepth = desktop.Windows.Count(static window => window.IsVisible);
        RaiseStateChanged();
    }

    private void RaiseStateChanged()
        => StateChanged?.Invoke(this, EventArgs.Empty);
}
