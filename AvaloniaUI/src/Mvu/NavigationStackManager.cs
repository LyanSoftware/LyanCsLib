using Avalonia.Controls;

namespace Lytec.AvaloniaUI.Mvu;

public interface INavigationStackManager : IDisposable
{
    NavigationPage NavigationPage { get; }

    bool IsBusy { get; }

    event EventHandler? BusyChanged;

    Task<bool> PushAsync(Page page);

    Task<bool> PopAsync();

    Task<bool> PopToRootAsync();

    Task<bool> SwitchRootAsync(Page rootPage);

    ValueTask<bool> TryLeaveAllAsync(bool mayBeTerminatedBySystem = false);
}

public interface INavigationStackManagerFactory
{
    INavigationStackManager Create(
        NavigationPage navigationPage,
        Page rootPage,
        ILeaveErrorHandler? errorHandler = null);
}

/// <summary>
/// Adds asynchronous leave decisions and cleanup to an Avalonia
/// <see cref="NavigationPage"/>.
/// </summary>
internal sealed class NavigationStackManager : INavigationStackManager
{
    private readonly Dictionary<Page, Func<NavigatingFromEventArgs, Task>> handlers = [];
    private readonly ILeaveErrorHandler? errorHandler;
    private LeaveReason operationReason = LeaveReason.NavigationBack;
    private bool operationInProgress;
    private bool disposed;

    public NavigationStackManager(
        NavigationPage navigationPage,
        Page rootPage,
        ILeaveErrorHandler? errorHandler = null)
    {
        NavigationPage = navigationPage ?? throw new ArgumentNullException(nameof(navigationPage));
        this.errorHandler = errorHandler;

        Register(rootPage);
        NavigationPage.Content = rootPage;
        NavigationPage.Popped += OnPagePopped;
        NavigationPage.PageRemoved += OnPageRemoved;
    }

    public NavigationPage NavigationPage { get; }

    public bool IsBusy => operationInProgress || NavigationPage.IsNavigating;

    public event EventHandler? BusyChanged;

    public async Task<bool> PushAsync(Page page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (!TryBeginOperation(LeaveReason.NavigationBack))
            return false;

        try
        {
            Register(page);
            var oldDepth = NavigationPage.StackDepth;
            await NavigationPage.PushAsync(page);
            var pushed = NavigationPage.StackDepth == oldDepth + 1;
            if (!pushed)
                Unregister(page);
            return pushed;
        }
        finally
        {
            EndOperation();
        }
    }

    public async Task<bool> PopAsync()
    {
        if (!TryBeginOperation(LeaveReason.NavigationBack))
            return false;

        try
        {
            return await PopOneAsync();
        }
        finally
        {
            EndOperation();
        }
    }

    /// <summary>
    /// Leaves every page above the root one at a time. Each successful leave is
    /// committed immediately; cancellation does not restore pages already popped.
    /// </summary>
    public async Task<bool> PopToRootAsync()
    {
        if (!TryBeginOperation(LeaveReason.NavigationRootChanged))
            return false;

        try
        {
            while (NavigationPage.StackDepth > 1)
            {
                if (!await PopOneAsync())
                    return false;
            }

            return true;
        }
        finally
        {
            EndOperation();
        }
    }

    /// <summary>
    /// Progressively leaves the current stack and then replaces its root.
    /// </summary>
    public async Task<bool> SwitchRootAsync(Page rootPage)
    {
        ArgumentNullException.ThrowIfNull(rootPage);
        if (!TryBeginOperation(LeaveReason.NavigationRootChanged))
            return false;

        try
        {
            while (NavigationPage.StackDepth > 1)
            {
                if (!await PopOneAsync())
                    return false;
            }

            if (ReferenceEquals(NavigationPage.CurrentPage, rootPage))
                return true;

            var previousRoot = NavigationPage.CurrentPage;
            Register(rootPage);
            await NavigationPage.ReplaceAsync(rootPage);
            var replaced = ReferenceEquals(NavigationPage.CurrentPage, rootPage);
            if (replaced && previousRoot is not null)
                Unregister(previousRoot);
            else if (!replaced)
                Unregister(rootPage);
            return replaced;
        }
        finally
        {
            EndOperation();
        }
    }

    /// <summary>
    /// Progressively removes child pages and prepares the root page for the
    /// application to exit. The root remains hosted until the platform exits.
    /// </summary>
    public async ValueTask<bool> TryLeaveAllAsync(
        bool mayBeTerminatedBySystem = false)
    {
        if (!TryBeginOperation(LeaveReason.ApplicationExit))
            return false;

        try
        {
            while (NavigationPage.StackDepth > 1)
            {
                if (!await PopOneAsync())
                    return false;
            }

            return NavigationPage.CurrentPage is not { } root
                || await TryLeaveAndCleanupAsync(
                    root,
                    new LeaveContext(
                        LeaveReason.ApplicationExit,
                        root,
                        IsProgrammatic: true,
                        MayBeTerminatedBySystem: mayBeTerminatedBySystem));
        }
        finally
        {
            EndOperation();
        }
    }

    private async Task<bool> PopOneAsync()
    {
        if (NavigationPage.StackDepth <= 1)
            return false;

        var oldDepth = NavigationPage.StackDepth;
        await NavigationPage.PopAsync();
        return NavigationPage.StackDepth == oldDepth - 1;
    }

    private void Register(Page page)
    {
        if (handlers.ContainsKey(page))
            return;

        Func<NavigatingFromEventArgs, Task> handler = args => OnNavigatingAsync(page, args);
        handlers.Add(page, handler);
        page.Navigating += handler;
    }

    private void Unregister(Page page)
    {
        if (!handlers.Remove(page, out var handler))
            return;

        page.Navigating -= handler;
    }

    private void OnPagePopped(object? sender, NavigationEventArgs e)
        => Unregister(e.Page);

    private void OnPageRemoved(object? sender, PageRemovedEventArgs e)
        => Unregister(e.Page);

    private async Task OnNavigatingAsync(Page page, NavigatingFromEventArgs args)
    {
        if (args.NavigationType is NavigationType.Push or NavigationType.PushModal)
            return;

        var reason = operationInProgress
            ? operationReason
            : LeaveReason.NavigationBack;
        var context = new LeaveContext(
            reason,
            page,
            args.DestinationPage,
            IsProgrammatic: operationInProgress);

        if (!await TryLeaveAndCleanupAsync(page, context))
            args.Cancel = true;
    }

    private async ValueTask<bool> TryLeaveAndCleanupAsync(
        object target,
        LeaveContext context)
    {
        if (target is not ILeaveAware leaveAware)
            return true;

        try
        {
            if (await leaveAware.TryLeaveAsync(context) is LeaveDecision.Cancel)
                return false;

            await leaveAware.CleanupAsync(context);
            return true;
        }
        catch (Exception ex)
        {
            if (errorHandler is not null)
                await errorHandler.ShowErrorAsync(context, ex);
            return false;
        }
    }

    private bool TryBeginOperation(LeaveReason reason)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (operationInProgress || NavigationPage.IsNavigating)
            return false;

        operationReason = reason;
        operationInProgress = true;
        BusyChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void EndOperation()
    {
        operationInProgress = false;
        BusyChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        NavigationPage.Popped -= OnPagePopped;
        NavigationPage.PageRemoved -= OnPageRemoved;
        foreach (var (page, handler) in handlers)
            page.Navigating -= handler;
        handlers.Clear();
    }
}

internal sealed class NavigationStackManagerFactory
    : INavigationStackManagerFactory
{
    public INavigationStackManager Create(
        NavigationPage navigationPage,
        Page rootPage,
        ILeaveErrorHandler? errorHandler = null)
        => new NavigationStackManager(navigationPage, rootPage, errorHandler);
}
