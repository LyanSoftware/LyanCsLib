using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Lytec.AvaloniaUI;
using Lytec.AvaloniaUI.Localization;
using Lytec.Common.Localization;
using Lytec.Common.Localization.Extensions;

namespace Lytec.AvaloniaUI.Mvu;

public sealed class RootBackRequestedEventArgs : EventArgs
{
    public bool Handled { get; set; }
}

/// <summary>
/// Capabilities available when <see cref="IViewManager"/> uses navigation
/// presentation.
/// </summary>
public interface INavigationViewManager : IViewManager
{
    IReadOnlyCollection<string> DrawerIds { get; }

    event EventHandler<RootBackRequestedEventArgs>? RootBackRequested;

    DrawerOptions GetDrawerOptions(string drawerId);

    Task<bool> SwitchRootAsync(string routeId);

    Task<bool> SwitchRootFromDrawerAsync(string routeId);

    Task<bool> OpenDrawerAsync(string drawerId);

    Task<bool> CloseDrawerAsync();
}

internal sealed class NavigationViewManager(
    Application application,
    AvaloniaMvuOptions options,
    ViewRegistry registry,
    ILocalizer localizer,
    IApplicationExitService applicationExitService) :
    INavigationViewManager,
    IDesktopShutdownCoordinator
{
    private const string LocalizeScope = "Lytec.AvaloniaUI.Mvu.NavigationViewManager";
    private readonly Dictionary<Page, ManagedPage> pages = [];
    private readonly Dictionary<string, DrawerViewLease> drawerLeases =
        new(StringComparer.Ordinal);
    private NavigationPage? outerNavigation;
    private NavigationPage? mainNavigation;
    private ContentPage? shellPage;
    private MultiDrawerHost? drawerHost;
    private Window? desktopWindow;
    private LeaveReason operationReason = LeaveReason.NavigationBack;
    private bool operationInProgress;
    private bool nativeDesktopCloseAllowed;
    private Task<bool>? desktopShutdownTask;
    private bool disposed;

    public ViewManagerState State { get; } = new();

    public IReadOnlyCollection<string> DrawerIds
        => [.. registry.Drawers.Keys];

    public event EventHandler? StateChanged;
    public event EventHandler<RootBackRequestedEventArgs>? RootBackRequested;

    public Control InstallRoot(string routeId)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ResetHost();

        var root = CreatePage(routeId);
        try
        {
            mainNavigation = new NavigationPage
            {
                HasShadow = options.Navigation.HasNavigationBarShadow,
                Content = root.Page,
            };
            Subscribe(mainNavigation);

            if (registry.Drawers.Count > 0)
            {
                drawerHost = new MultiDrawerHost();
                drawerHost.SetMainContent(mainNavigation);
                drawerHost.LightDismissRequested += OnLightDismissRequested;
                shellPage = new ContentPage { Content = drawerHost };
                NavigationPage.SetHasNavigationBar(shellPage, false);
                shellPage.PageNavigationSystemBackButtonPressed += OnSystemBack;
                outerNavigation = new NavigationPage
                {
                    HasShadow = false,
                    Content = shellPage,
                };
                Subscribe(outerNavigation);
            }
            else
            {
                outerNavigation = mainNavigation;
            }

            outerNavigation.KeyDown += OnHostKeyDown;
            State.CurrentRootId = routeId;
            UpdateState();
            return InstallPlatformHost(root.Lease.ManagedView.ViewOptions);
        }
        catch
        {
            ReleasePage(root.Page);
            ResetHost();
            throw;
        }
    }

    public Task<bool> OpenAsync(string routeId, Control? ownerView = null)
        => PushAsync(routeId, isModal: false);

    public async Task<bool> OpenPathAsync(
        IReadOnlyList<string> routeIds,
        Control? ownerView = null)
    {
        ArgumentNullException.ThrowIfNull(routeIds);
        EnsureInstalled();
        if (!TryBeginOperation(LeaveReason.NavigationBack))
            return false;

        try
        {
            var navigation = mainNavigation!;
            var current = navigation.NavigationStack
                .Skip(1)
                .Select(page => pages.TryGetValue(page, out var managed)
                    ? managed.Lease.RouteId
                    : null)
                .ToArray();
            var commonLength = 0;
            while (commonLength < current.Length
                && commonLength < routeIds.Count
                && string.Equals(
                    current[commonLength],
                    routeIds[commonLength],
                    StringComparison.Ordinal))
            {
                commonLength++;
            }

            while (navigation.StackDepth - 1 > commonLength)
            {
                if (!await PopOneAsync(navigation))
                    return false;
            }

            for (var i = commonLength; i < routeIds.Count; i++)
            {
                var created = CreatePage(routeIds[i]);
                var oldDepth = navigation.StackDepth;
                await navigation.PushAsync(created.Page);
                if (navigation.StackDepth != oldDepth + 1)
                {
                    ReleasePage(created.Page);
                    return false;
                }
            }

            return true;
        }
        finally
        {
            EndOperation();
        }
    }

    public Task<bool> ShowModalAsync(string routeId, Control? ownerView = null)
        => PushAsync(routeId, isModal: true);

    public async Task<bool> BackAsync()
    {
        EnsureInstalled();
        if (operationInProgress || outerNavigation!.IsNavigating)
            return false;

        if (outerNavigation.ModalStack.Count > 0)
            return await PopAsync(outerNavigation, modal: true);

        if (State.OpenDrawerId is not null)
            return await CloseDrawerAsync();

        if (mainNavigation!.StackDepth > 1)
            return await PopAsync(mainNavigation, modal: false);

        var args = new RootBackRequestedEventArgs();
        RootBackRequested?.Invoke(this, args);
        return args.Handled || await applicationExitService.TryShutdownAsync();
    }

    public async Task<bool> SwitchRootAsync(string routeId)
    {
        EnsureInstalled();
        if (!TryBeginOperation(LeaveReason.NavigationRootChanged))
            return false;

        ManagedPage? created = null;
        try
        {
            var navigation = mainNavigation!;
            if (string.Equals(State.CurrentRootId, routeId, StringComparison.Ordinal))
            {
                while (navigation.StackDepth > 1)
                {
                    if (!await PopOneAsync(navigation))
                        return false;
                }
                return true;
            }

            // Two-phase root switch: create a lazy ViewBase first, but do not
            // attach it until every existing page has agreed to leave.
            created = CreatePage(routeId);
            while (navigation.StackDepth > 1)
            {
                if (!await PopOneAsync(navigation))
                    return false;
            }

            var previousRoot = navigation.CurrentPage;
            await navigation.ReplaceAsync(created.Page);
            if (!ReferenceEquals(navigation.CurrentPage, created.Page))
                return false;

            created = null;
            State.CurrentRootId = routeId;
            if (previousRoot is not null && pages.ContainsKey(previousRoot))
                ReleasePage(previousRoot);
            return true;
        }
        catch (Exception ex)
        {
            await ShowLeaveErrorAsync(
                new LeaveContext(
                    LeaveReason.NavigationRootChanged,
                    mainNavigation!,
                    IsProgrammatic: true),
                ex);
            return false;
        }
        finally
        {
            if (created is not null)
                ReleasePage(created.Page);
            EndOperation();
        }
    }

    public async Task<bool> SwitchRootFromDrawerAsync(string routeId)
    {
        if (!await SwitchRootAsync(routeId))
            return false;
        return State.OpenDrawerId is null || await CloseDrawerAsync();
    }

    public DrawerOptions GetDrawerOptions(string drawerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        return registry.Drawers.TryGetValue(drawerId, out var registration)
            ? registration.Options
            : throw new KeyNotFoundException(
                $"No drawer named '{drawerId}' is registered.");
    }

    public async Task<bool> OpenDrawerAsync(string drawerId)
    {
        EnsureInstalled();
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        if (drawerHost is null)
            return false;
        if (string.Equals(State.OpenDrawerId, drawerId, StringComparison.Ordinal))
            return true;
        if (!TryBeginOperation(LeaveReason.NavigationBack))
            return false;

        try
        {
            if (State.OpenDrawerId is not null)
            {
                await drawerHost.HideAsync(CancellationToken.None);
                State.OpenDrawerId = null;
            }

            if (!drawerLeases.TryGetValue(drawerId, out var lease))
            {
                lease = registry.CreateDrawer(drawerId);
                drawerLeases.Add(drawerId, lease);
            }

            await drawerHost.ShowAsync(
                lease.View,
                lease.Options,
                CancellationToken.None);
            State.OpenDrawerId = drawerId;
            return true;
        }
        finally
        {
            EndOperation();
        }
    }

    public async Task<bool> CloseDrawerAsync()
    {
        EnsureInstalled();
        if (drawerHost is null || State.OpenDrawerId is null)
            return true;
        if (!TryBeginOperation(LeaveReason.NavigationBack))
            return false;

        try
        {
            await drawerHost.HideAsync(CancellationToken.None);
            State.OpenDrawerId = null;
            return true;
        }
        finally
        {
            EndOperation();
        }
    }

    public async ValueTask<bool> TryLeaveAllAsync(
        bool mayBeTerminatedBySystem = false)
    {
        EnsureInstalled();
        if (!TryBeginOperation(LeaveReason.ApplicationExit))
            return false;

        try
        {
            while (outerNavigation!.ModalStack.Count > 0)
            {
                if (!await PopOneModalAsync(outerNavigation))
                    return false;
            }

            if (drawerHost is not null && State.OpenDrawerId is not null)
            {
                await drawerHost.HideAsync(CancellationToken.None);
                State.OpenDrawerId = null;
            }

            while (mainNavigation!.StackDepth > 1)
            {
                if (!await PopOneAsync(mainNavigation))
                    return false;
            }

            return mainNavigation.CurrentPage is not { } root
                || !pages.TryGetValue(root, out var managed)
                || await TryLeaveAndCleanupAsync(
                    managed,
                    new LeaveContext(
                        LeaveReason.ApplicationExit,
                        managed.Lease.View,
                        IsProgrammatic: true,
                        MayBeTerminatedBySystem: mayBeTerminatedBySystem));
        }
        finally
        {
            EndOperation();
        }
    }

    public Task<bool> TryShutdownAsync(
        int exitCode,
        bool mayBeTerminatedBySystem = false)
    {
        if (desktopShutdownTask is not null)
            return desktopShutdownTask;
        desktopShutdownTask = RunDesktopShutdownAsync(
            exitCode,
            mayBeTerminatedBySystem);
        return desktopShutdownTask;
    }

    private async Task<bool> RunDesktopShutdownAsync(
        int exitCode,
        bool mayBeTerminatedBySystem)
    {
        if (!await TryLeaveAllAsync(mayBeTerminatedBySystem)
            || application.ApplicationLifetime
                is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktopShutdownTask = null;
            return false;
        }

        nativeDesktopCloseAllowed = true;
        var shutdown = desktop.TryShutdown(exitCode);
        if (!shutdown)
        {
            nativeDesktopCloseAllowed = false;
            desktopShutdownTask = null;
        }
        return shutdown;
    }

    private async Task<bool> PushAsync(string routeId, bool isModal)
    {
        EnsureInstalled();
        if (!TryBeginOperation(LeaveReason.NavigationBack))
            return false;

        ManagedPage? created = null;
        try
        {
            created = CreatePage(routeId);
            var navigation = isModal ? outerNavigation! : mainNavigation!;
            var oldDepth = isModal
                ? navigation.ModalStack.Count
                : navigation.StackDepth;
            if (isModal)
                await navigation.PushModalAsync(created.Page);
            else
                await navigation.PushAsync(created.Page);
            var pushed = (isModal
                ? navigation.ModalStack.Count
                : navigation.StackDepth) == oldDepth + 1;
            if (pushed)
                created = null;
            return pushed;
        }
        finally
        {
            if (created is not null)
                ReleasePage(created.Page);
            EndOperation();
        }
    }

    private async Task<bool> PopAsync(NavigationPage navigation, bool modal)
    {
        if (!TryBeginOperation(LeaveReason.NavigationBack))
            return false;
        try
        {
            return modal
                ? await PopOneModalAsync(navigation)
                : await PopOneAsync(navigation);
        }
        finally
        {
            EndOperation();
        }
    }

    private static async Task<bool> PopOneAsync(NavigationPage navigation)
    {
        if (navigation.StackDepth <= 1)
            return false;
        var oldDepth = navigation.StackDepth;
        await navigation.PopAsync();
        return navigation.StackDepth == oldDepth - 1;
    }

    private static async Task<bool> PopOneModalAsync(NavigationPage navigation)
    {
        if (navigation.ModalStack.Count == 0)
            return false;
        var oldDepth = navigation.ModalStack.Count;
        await navigation.PopModalAsync();
        return navigation.ModalStack.Count == oldDepth - 1;
    }

    private ManagedPage CreatePage(string routeId)
    {
        var lease = registry.Create(routeId);
        try
        {
            var page = new ContentPage
            {
                Header = lease.ManagedView.ViewOptions.Title,
                Content = lease.View,
            };
            if (lease.ManagedView.ViewOptions.TitleBinding is { } binding)
                page.Bind(Page.HeaderProperty, binding);
            var managed = new ManagedPage(page, lease);
            pages.Add(page, managed);
            page.Navigating += managed.NavigatingHandler =
                args => OnNavigatingAsync(managed, args);
            page.PageNavigationSystemBackButtonPressed += OnSystemBack;
            return managed;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    private async Task OnNavigatingAsync(
        ManagedPage page,
        NavigatingFromEventArgs args)
    {
        if (args.NavigationType is NavigationType.Push or NavigationType.PushModal)
            return;

        var reason = operationInProgress
            ? operationReason
            : LeaveReason.NavigationBack;
        var context = new LeaveContext(
            reason,
            page.Lease.View,
            args.DestinationPage,
            IsProgrammatic: operationInProgress);
        if (!await TryLeaveAndCleanupAsync(page, context))
            args.Cancel = true;
    }

    private async ValueTask<bool> TryLeaveAndCleanupAsync(
        ManagedPage page,
        LeaveContext context)
    {
        try
        {
            if (await page.Lease.ManagedView.TryLeaveAsync(context)
                is LeaveDecision.Cancel)
                return false;
            await page.Lease.ManagedView.CleanupAsync(context);
            return true;
        }
        catch (Exception ex)
        {
            await ShowLeaveErrorAsync(context, ex);
            return false;
        }
    }

    private async ValueTask ShowLeaveErrorAsync(
        LeaveContext context,
        Exception exception)
    {
        if (options.LeaveErrorAsync is { } callback)
        {
            await callback(context, exception);
            return;
        }
        if (outerNavigation is null)
            return;

        var closeButton = new Button
        {
            Content = localizer.Format(
                LocalizeScope,
                "Error.Close",
                DefaultMessage: "关闭"),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        var errorPage = new ContentPage
        {
            Header = localizer.Format(
                LocalizeScope,
                "Error.Header",
                DefaultMessage: "无法离开页面"),
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = (exception.GetLocalizedMessage() is { } message
                            ? localizer.Format(message)
                            : null) ?? exception.Message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    },
                    closeButton,
                },
            },
        };
        closeButton.Click += async (_, _) =>
        {
            if (outerNavigation.ModalStack.Contains(errorPage))
                await outerNavigation.PopModalAsync();
        };
        await outerNavigation.PushModalAsync(errorPage);
    }

    private Control InstallPlatformHost(ViewInfo rootOptions)
    {
        if (application.ApplicationLifetime
            is not IClassicDesktopStyleApplicationLifetime desktop)
            return outerNavigation!;
        if (desktop.MainWindow is not null)
            throw new InvalidOperationException("The desktop main window is already installed.");

        var windowOptions = options.Navigation.DesktopWindow;
        desktopWindow = new Window
        {
            Content = outerNavigation,
            Title = options.Navigation.DesktopTitle
                ?? rootOptions.Title
                ?? "View",
            Width = windowOptions.Width,
            Height = windowOptions.Height,
            MinWidth = windowOptions.MinWidth,
            MinHeight = windowOptions.MinHeight,
            CanResize = windowOptions.CanResize,
            SizeToContent = windowOptions.SizeToContent,
        };
        if (options.Navigation.DesktopTitle is null
            && rootOptions.TitleBinding is { } titleBinding)
            desktopWindow.Bind(Window.TitleProperty, titleBinding);
        desktopWindow.Closing += OnDesktopWindowClosing;
        desktop.ShutdownRequested += OnDesktopShutdownRequested;
        desktop.MainWindow = desktopWindow;
        return desktopWindow;
    }

    private void Subscribe(NavigationPage navigation)
    {
        navigation.Popped += OnPagePopped;
        navigation.PageRemoved += OnPageRemoved;
        navigation.ModalPopped += OnModalPopped;
    }

    private void Unsubscribe(NavigationPage navigation)
    {
        navigation.Popped -= OnPagePopped;
        navigation.PageRemoved -= OnPageRemoved;
        navigation.ModalPopped -= OnModalPopped;
    }

    private void OnPagePopped(object? sender, NavigationEventArgs e)
    {
        ReleasePage(e.Page);
        UpdateState();
    }

    private void OnPageRemoved(object? sender, PageRemovedEventArgs e)
    {
        ReleasePage(e.Page);
        UpdateState();
    }

    private void OnModalPopped(object? sender, ModalPoppedEventArgs e)
    {
        ReleasePage(e.Modal);
        UpdateState();
    }

    private void ReleasePage(Page page)
    {
        if (!pages.Remove(page, out var managed))
            return;
        if (managed.NavigatingHandler is not null)
            page.Navigating -= managed.NavigatingHandler;
        page.PageNavigationSystemBackButtonPressed -= OnSystemBack;
        managed.Lease.Dispose();
    }

    private void OnLightDismissRequested(object? sender, EventArgs e)
        => _ = CloseDrawerAsync();

    private async void OnSystemBack(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        e.Handled = true;
        await BackAsync();
    }

    private async void OnHostKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;
        e.Handled = true;
        await BackAsync();
    }

    private async void OnDesktopWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (nativeDesktopCloseAllowed)
            return;
        e.Cancel = true;
        await TryShutdownAsync(0, e.CloseReason is WindowCloseReason.OSShutdown);
    }

    private async void OnDesktopShutdownRequested(
        object? sender,
        ShutdownRequestedEventArgs e)
    {
        if (nativeDesktopCloseAllowed)
            return;
        e.Cancel = true;
        await TryShutdownAsync(0, mayBeTerminatedBySystem: true);
    }

    private bool TryBeginOperation(LeaveReason reason)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (operationInProgress
            || mainNavigation?.IsNavigating == true
            || outerNavigation?.IsNavigating == true)
            return false;

        operationReason = reason;
        operationInProgress = true;
        State.IsBusy = true;
        RaiseStateChanged();
        return true;
    }

    private void EndOperation()
    {
        operationInProgress = false;
        State.IsBusy = false;
        UpdateState();
    }

    private void UpdateState()
    {
        State.StackDepth = mainNavigation?.StackDepth ?? 0;
        State.ModalDepth = outerNavigation?.ModalStack.Count ?? 0;
        RaiseStateChanged();
    }

    private void RaiseStateChanged()
        => StateChanged?.Invoke(this, EventArgs.Empty);

    private void EnsureInstalled()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (outerNavigation is null || mainNavigation is null)
            throw new InvalidOperationException("InstallRoot must be called before navigation.");
    }

    private void ResetHost()
    {
        if (desktopWindow is not null)
        {
            desktopWindow.Closing -= OnDesktopWindowClosing;
            if (application.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.ShutdownRequested -= OnDesktopShutdownRequested;
            desktopWindow = null;
        }
        outerNavigation?.KeyDown -= OnHostKeyDown;
        if (mainNavigation is not null)
            Unsubscribe(mainNavigation);
        if (outerNavigation is not null
            && !ReferenceEquals(outerNavigation, mainNavigation))
            Unsubscribe(outerNavigation);
        shellPage?.PageNavigationSystemBackButtonPressed -= OnSystemBack;
        drawerHost?.LightDismissRequested -= OnLightDismissRequested;

        foreach (var page in pages.Keys.ToArray())
            ReleasePage(page);
        foreach (var lease in drawerLeases.Values)
            lease.Dispose();
        drawerLeases.Clear();
        outerNavigation = null;
        mainNavigation = null;
        shellPage = null;
        drawerHost = null;
        State.CurrentRootId = null;
        State.OpenDrawerId = null;
        State.StackDepth = 0;
        State.ModalDepth = 0;
        State.IsBusy = false;
    }

    public void Dispose()
    {
        if (disposed)
            return;
        ResetHost();
        disposed = true;
    }

    private sealed class ManagedPage(Page page, ManagedViewLease lease)
    {
        public Page Page { get; } = page;
        public ManagedViewLease Lease { get; } = lease;
        public Func<NavigatingFromEventArgs, Task>? NavigatingHandler { get; set; }
    }
}
