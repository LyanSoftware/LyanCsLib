using Avalonia.Controls;

namespace Lytec.AvaloniaUI.Mvu;

public record WindowInfo
{
    public string Title { get; init; } = "Window";
    public double Width { get; init; } = 800;
    public double Height { get; init; } = 600;
    public double MinWidth { get; init; } = 400;
    public double MinHeight { get; init; } = 300;
    public bool CanResize { get; init; } = true;
    public SizeToContent SizeToContent { get; init; } = SizeToContent.WidthAndHeight;

    /// <summary>
    /// Asynchronously decides whether this window may close. The window remains visible
    /// while this callback runs, so the callback may display an owned confirmation dialog.
    /// </summary>
    public Func<WindowCloseContext, ValueTask<WindowCloseDecision>>? CanCloseAsync { get; init; }

    /// <summary>
    /// Performs the window's asynchronous cleanup after closing has been approved.
    /// The window is hidden before this callback runs.
    /// </summary>
    public Func<WindowCloseContext, ValueTask>? CleanupAsync { get; init; }
}

public enum WindowCloseDecision
{
    Allow,
    Cancel,
}

public sealed record WindowCloseContext(
    Window Window,
    WindowCloseReason Reason,
    bool IsProgrammatic,
    bool IsApplicationExit,
    bool MayBeTerminatedBySystem);

public interface IWindowView
{
    WindowInfo WindowOptions { get; }
}
