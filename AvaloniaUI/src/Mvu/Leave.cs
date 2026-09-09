namespace Lytec.AvaloniaUI.Mvu;

/// <summary>
/// Describes why a view is about to be removed from its current host.
/// </summary>
public enum LeaveReason
{
    NavigationBack,
    NavigationRootChanged,
    WindowClose,
    ApplicationExit,
}

public enum LeaveDecision
{
    Allow,
    Cancel,
}

public sealed record LeaveContext(
    LeaveReason Reason,
    object Source,
    object? Destination = null,
    bool IsProgrammatic = false,
    bool MayBeTerminatedBySystem = false);

/// <summary>
/// Implemented by a view that owns its unsaved-change decision and cleanup.
/// </summary>
public interface ILeaveAware
{
    ValueTask<LeaveDecision> TryLeaveAsync(LeaveContext context)
        => ValueTask.FromResult(LeaveDecision.Allow);

    ValueTask CleanupAsync(LeaveContext context)
        => ValueTask.CompletedTask;
}

/// <summary>
/// Presents failures raised while a guarded leave operation is running.
/// </summary>
public interface ILeaveErrorHandler
{
    ValueTask ShowErrorAsync(LeaveContext context, Exception exception);
}
