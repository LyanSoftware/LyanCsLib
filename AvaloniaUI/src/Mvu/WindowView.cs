using Avalonia.Controls;

namespace Lytec.AvaloniaUI.Mvu;

public record WindowInfo
{
    public double Width { get; init; } = 800;
    public double Height { get; init; } = 600;
    public double MinWidth { get; init; } = 400;
    public double MinHeight { get; init; } = 300;
    public bool CanResize { get; init; } = true;
    public SizeToContent SizeToContent { get; init; } = SizeToContent.Manual;

    /// <summary>
    /// Asynchronously decides whether this window may close. The window remains visible
    /// while this callback runs, so the callback may display an owned confirmation dialog.
    /// </summary>
    public Func<WindowCloseContext, ValueTask<LeaveDecision>>? CanCloseAsync { get; init; }

    /// <summary>
    /// Performs the window's asynchronous cleanup after closing has been approved.
    /// The window is disabled while this callback runs and is only hidden after
    /// cleanup succeeds.
    /// </summary>
    public Func<WindowCloseContext, ValueTask>? CleanupAsync { get; init; }

    /// <summary>
    /// Presents an exception raised by the close decision or cleanup. When omitted,
    /// the window presentation manager displays a basic owned error dialog.
    /// </summary>
    public Func<WindowCloseContext, Exception, ValueTask>? CloseErrorAsync { get; init; }
}

public sealed record WindowCloseContext(
    Window Window,
    WindowCloseReason Reason,
    bool IsProgrammatic,
    bool IsApplicationExit,
    bool MayBeTerminatedBySystem);
