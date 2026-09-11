using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Lytec.AvaloniaUI.Mvu;

namespace Lytec.AvaloniaUI;

/// <summary>
/// Performs the platform-specific final step of exiting an application.
/// </summary>
public interface IApplicationExitHandler
{
    ValueTask<bool> TryExitAsync(int exitCode);
}

/// <summary>
/// Gives a single-view application a chance to leave and clean its active UI
/// before the platform exit handler runs.
/// </summary>
public interface IApplicationExitGuard
{
    ValueTask<bool> TryLeaveApplicationAsync();
}

/// <summary>
/// Coordinates guarded application exit across desktop and single-view platforms.
/// </summary>
public interface IApplicationExitService
{
    IDisposable RegisterHandler(IApplicationExitHandler handler);

    IDisposable RegisterGuard(IApplicationExitGuard guard);

    Task<bool> TryShutdownAsync(int exitCode = 0);
}

internal sealed class ApplicationExitService(
    Application application,
    IServiceProvider services)
    : IApplicationExitService
{
    private readonly RegistrationStack<IApplicationExitHandler> handlers = new();
    private readonly RegistrationStack<IApplicationExitGuard> guards = new();

    public IDisposable RegisterHandler(IApplicationExitHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return handlers.Register(handler);
    }

    public IDisposable RegisterGuard(IApplicationExitGuard guard)
    {
        ArgumentNullException.ThrowIfNull(guard);
        return guards.Register(guard);
    }

    public Task<bool> TryShutdownAsync(int exitCode = 0)
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return Dispatcher.UIThread.InvokeAsync(() => TryShutdownAsync(exitCode));

        return TryShutdownOnUiThreadAsync(exitCode);
    }

    private async Task<bool> TryShutdownOnUiThreadAsync(int exitCode)
    {
        var viewManager = services.GetService(typeof(IViewManager)) as IViewManager;
        if (application.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime
            && viewManager is IDesktopShutdownCoordinator desktopManager)
            return await desktopManager.TryShutdownAsync(exitCode);

        var controlledLifetime = application.ApplicationLifetime
            as IControlledApplicationLifetime;
        var handler = handlers.Current;
        if (controlledLifetime is null && handler is null)
            return false;

        if (guards.Current is { } guard
            && !await guard.TryLeaveApplicationAsync())
            return false;

        if (viewManager is not null
            && !await viewManager.TryLeaveAllAsync())
            return false;

        if (controlledLifetime is not null)
        {
            controlledLifetime.Shutdown(exitCode);
            return true;
        }

        return await handler!.TryExitAsync(exitCode);
    }

    private sealed class RegistrationStack<T>
        where T : class
    {
        private readonly object gate = new();
        private readonly List<Entry> entries = [];

        public T? Current
        {
            get
            {
                lock (gate)
                    return entries.Count == 0 ? null : entries[^1].Value;
            }
        }

        public IDisposable Register(T value)
        {
            var entry = new Entry(value);
            lock (gate)
                entries.Add(entry);

            return new Registration(() =>
            {
                lock (gate)
                    entries.Remove(entry);
            });
        }

        private sealed record Entry(T Value);
    }

    private sealed class Registration(Action unregister) : IDisposable
    {
        private Action? unregister = unregister;

        public void Dispose()
            => Interlocked.Exchange(ref unregister, null)?.Invoke();
    }
}
