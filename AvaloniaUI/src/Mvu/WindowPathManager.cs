using Avalonia.Controls;

namespace Lytec.AvaloniaUI.Mvu;

internal readonly record struct CreatedWindow(Control View, Window Window);

/// <summary>
/// Maintains one modal child-window path. Switching paths closes windows back
/// to the common ancestor and then opens missing windows in order.
/// </summary>
internal sealed class WindowPathManager(
    Window rootWindow,
    WindowViewManager windowManager)
    : IDisposable
{
    private readonly List<OpenSegment> openPath = [];
    private bool operationInProgress;
    private bool disposed;

    public bool IsBusy => operationInProgress;

    public async Task<bool> OpenPathAsync(
        IReadOnlyList<string> targetPath)
    {
        ArgumentNullException.ThrowIfNull(targetPath);
        ObjectDisposedException.ThrowIf(disposed, this);

        if (operationInProgress)
            return false;

        operationInProgress = true;
        try
        {
            RemoveClosedSegments();
            var commonLength = GetCommonPrefixLength(targetPath);

            for (var i = openPath.Count - 1; i >= commonLength; i--)
            {
                if (!await windowManager.TryCloseAsync(openPath[i].View))
                    return false;
                RemoveClosedSegments();
            }

            var owner = commonLength == 0
                ? rootWindow
                : openPath[commonLength - 1].Window;
            for (var i = commonLength; i < targetPath.Count; i++)
            {
                var routeId = targetPath[i];
                var created = windowManager.Create(routeId);
                var view = created.View;
                var window = created.Window;
                var opened = new TaskCompletionSource(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                window.Opened += OnOpened;
                window.Closed += OnClosed;

                var dialogTask = window.ShowDialog(owner);
                _ = ObserveDialogAsync(dialogTask);
                await opened.Task;

                if (!window.IsVisible)
                    return false;

                openPath.Add(new OpenSegment(routeId, view, window));
                owner = window;

                void OnOpened(object? sender, EventArgs e)
                {
                    window.Opened -= OnOpened;
                    opened.TrySetResult();
                }

                void OnClosed(object? sender, EventArgs e)
                {
                    window.Opened -= OnOpened;
                    window.Closed -= OnClosed;
                    RemoveWindowAndDescendants(window);
                    opened.TrySetResult();
                }
            }

            (openPath.Count == 0 ? rootWindow : openPath[^1].Window).Activate();
            windowManager.UpdateWindowState();
            return true;
        }
        finally
        {
            operationInProgress = false;
        }
    }

    private int GetCommonPrefixLength(IReadOnlyList<string> targetPath)
    {
        var length = Math.Min(openPath.Count, targetPath.Count);
        var index = 0;
        while (index < length
            && string.Equals(
                openPath[index].Id,
                targetPath[index],
                StringComparison.Ordinal))
        {
            index++;
        }

        return index;
    }

    private void RemoveClosedSegments()
        => openPath.RemoveAll(static segment => !segment.Window.IsVisible);

    private void RemoveWindowAndDescendants(Window window)
    {
        var index = openPath.FindIndex(
            segment => ReferenceEquals(segment.Window, window));
        if (index >= 0)
            openPath.RemoveRange(index, openPath.Count - index);
    }

    private static async Task ObserveDialogAsync(Task dialogTask)
    {
        try
        {
            await dialogTask;
        }
        catch
        {
            // The route operation observes whether the window opened. Keep the
            // dialog task observed so a later failure cannot become unobserved.
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        openPath.Clear();
    }

    private sealed record OpenSegment(string Id, Control View, Window Window);
}
