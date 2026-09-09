using System.Runtime.CompilerServices;
using Avalonia;

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

public static class ApplicationExit
{
    private static readonly ConditionalWeakTable<Application, Registrations> Table = new();

    public static IDisposable RegisterExitHandler(
        this Application application,
        IApplicationExitHandler handler)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(handler);
        return Table.GetOrCreateValue(application).SetExitHandler(handler);
    }

    public static IDisposable RegisterExitGuard(
        this Application application,
        IApplicationExitGuard guard)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(guard);
        return Table.GetOrCreateValue(application).SetExitGuard(guard);
    }

    internal static IApplicationExitHandler? GetExitHandler(Application application)
        => Table.TryGetValue(application, out var registrations)
            ? registrations.ExitHandler
            : null;

    internal static IApplicationExitGuard? GetExitGuard(Application application)
        => Table.TryGetValue(application, out var registrations)
            ? registrations.ExitGuard
            : null;

    private sealed class Registrations
    {
        private readonly object gate = new();
        private IApplicationExitHandler? exitHandler;
        private IApplicationExitGuard? exitGuard;

        public IApplicationExitHandler? ExitHandler
        {
            get { lock (gate) return exitHandler; }
        }

        public IApplicationExitGuard? ExitGuard
        {
            get { lock (gate) return exitGuard; }
        }

        public IDisposable SetExitHandler(IApplicationExitHandler value)
        {
            lock (gate)
                exitHandler = value;
            return new Registration(() =>
            {
                lock (gate)
                {
                    if (ReferenceEquals(exitHandler, value))
                        exitHandler = null;
                }
            });
        }

        public IDisposable SetExitGuard(IApplicationExitGuard value)
        {
            lock (gate)
                exitGuard = value;
            return new Registration(() =>
            {
                lock (gate)
                {
                    if (ReferenceEquals(exitGuard, value))
                        exitGuard = null;
                }
            });
        }
    }

    private sealed class Registration(Action unregister) : IDisposable
    {
        private Action? unregister = unregister;

        public void Dispose() => Interlocked.Exchange(ref unregister, null)?.Invoke();
    }
}
