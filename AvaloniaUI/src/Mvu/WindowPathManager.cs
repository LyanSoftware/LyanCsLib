using Avalonia.Controls;

namespace Lytec.AvaloniaUI.Mvu;

/// <summary>
/// Describes one modal window in a route from a desktop main window.
/// </summary>
public sealed record WindowPathSegment(string Id, Func<Control> CreateView);

/// <summary>
/// Maintains one modal child-window path. Switching paths closes windows back
/// to the common ancestor and then opens missing windows in order.
/// </summary>
public sealed class WindowPathManager
{
    private readonly List<OpenSegment> openPath = [];
    private bool operationInProgress;

    public bool IsBusy => operationInProgress;

    public async Task<bool> OpenPathAsync(
        Control rootView,
        IReadOnlyList<WindowPathSegment> targetPath)
    {
        ArgumentNullException.ThrowIfNull(rootView);
        ArgumentNullException.ThrowIfNull(targetPath);

        if (operationInProgress)
            return false;

        var rootWindow = TopLevel.GetTopLevel(rootView) as Window
            ?? throw new InvalidOperationException(
                "The root view must be attached to a desktop Window.");

        operationInProgress = true;
        try
        {
            RemoveClosedSegments();
            var commonLength = GetCommonPrefixLength(targetPath);

            for (var i = openPath.Count - 1; i >= commonLength; i--)
            {
                if (!await WindowManager.TryCloseAsync(openPath[i].View))
                    return false;
                RemoveClosedSegments();
            }

            var owner = commonLength == 0
                ? rootWindow
                : openPath[commonLength - 1].Window;
            for (var i = commonLength; i < targetPath.Count; i++)
            {
                var segment = targetPath[i];
                var view = segment.CreateView()
                    ?? throw new InvalidOperationException(
                        $"Window path segment '{segment.Id}' returned a null view.");
                var window = WindowManager.Create(view);
                var opened = new TaskCompletionSource(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                window.Opened += OnOpened;
                window.Closed += OnClosed;

                var dialogTask = window.ShowDialog(owner);
                _ = ObserveDialogAsync(dialogTask);
                await opened.Task;

                openPath.Add(new OpenSegment(segment.Id, view, window));
                owner = window;

                void OnOpened(object? sender, EventArgs e)
                {
                    window.Opened -= OnOpened;
                    opened.TrySetResult();
                }

                void OnClosed(object? sender, EventArgs e)
                {
                    window.Closed -= OnClosed;
                    RemoveWindowAndDescendants(window);
                    opened.TrySetResult();
                }
            }

            (openPath.Count == 0 ? rootWindow : openPath[^1].Window).Activate();
            return true;
        }
        finally
        {
            operationInProgress = false;
        }
    }

    private int GetCommonPrefixLength(IReadOnlyList<WindowPathSegment> targetPath)
    {
        var length = Math.Min(openPath.Count, targetPath.Count);
        var index = 0;
        while (index < length
            && string.Equals(
                openPath[index].Id,
                targetPath[index].Id,
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
            // ShowDialog failures are surfaced by the route operation where possible;
            // observing the task prevents a later unobserved-task exception.
        }
    }

    private sealed record OpenSegment(string Id, Control View, Window Window);
}
