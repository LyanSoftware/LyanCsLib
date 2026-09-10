using Avalonia.Controls;
using Lytec.Common.Localization.Extensions;

namespace Lytec.AvaloniaUI.Mvu;

/// <summary>
/// Describes one modal window in a route from a desktop main window.
/// </summary>
public sealed record WindowPathSegment(string Id, Func<Control> CreateView);

public interface IWindowPathManager
{
    bool IsBusy { get; }

    Task<bool> OpenPathAsync(IReadOnlyList<WindowPathSegment> targetPath);
}

public interface IWindowPathManagerFactory
{
    IWindowPathManager GetOrCreate(Window rootWindow);
}

/// <summary>
/// Maintains one modal child-window path. Switching paths closes windows back
/// to the common ancestor and then opens missing windows in order.
/// </summary>
internal sealed class WindowPathManager(
    Window rootWindow,
    IWindowManager windowManager)
    : IWindowPathManager, IDisposable
{
    private const string LocalizeScope = "Lytec.AvaloniaUI.Mvu.WindowPathManager";
    private readonly List<OpenSegment> openPath = [];
    private bool operationInProgress;
    private bool disposed;

    public bool IsBusy => operationInProgress;

    public async Task<bool> OpenPathAsync(
        IReadOnlyList<WindowPathSegment> targetPath)
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
                var segment = targetPath[i];
                var view = segment.CreateView()
                    ?? throw new InvalidOperationException()
                        .Localize(
                            LocalizeScope,
                            "OpenPathError_NullView",
                            new { segment.Id },
                            "窗口路径段“{Id}”返回了 null 视图。");
                var window = windowManager.Create(view);
                var opened = new TaskCompletionSource(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                window.Opened += OnOpened;
                window.Closed += OnClosed;

                var dialogTask = window.ShowDialog(owner);
                _ = ObserveDialogAsync(dialogTask);
                await opened.Task;

                if (!window.IsVisible)
                    return false;

                openPath.Add(new OpenSegment(segment.Id, view, window));
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

internal sealed class WindowPathManagerFactory(IWindowManager windowManager)
    : IWindowPathManagerFactory, IDisposable
{
    private readonly Dictionary<Window, WindowPathManager> managers = [];
    private bool disposed;

    public IWindowPathManager GetOrCreate(Window rootWindow)
    {
        ArgumentNullException.ThrowIfNull(rootWindow);
        ObjectDisposedException.ThrowIf(disposed, this);

        while (rootWindow.Owner is Window owner)
            rootWindow = owner;

        if (managers.TryGetValue(rootWindow, out var manager))
            return manager;

        manager = new WindowPathManager(rootWindow, windowManager);
        managers.Add(rootWindow, manager);
        rootWindow.Closed += OnRootWindowClosed;
        return manager;
    }

    private void OnRootWindowClosed(object? sender, EventArgs e)
    {
        if (sender is not Window rootWindow)
            return;

        rootWindow.Closed -= OnRootWindowClosed;
        if (managers.Remove(rootWindow, out var manager))
            manager.Dispose();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        foreach (var (rootWindow, manager) in managers)
        {
            rootWindow.Closed -= OnRootWindowClosed;
            manager.Dispose();
        }
        managers.Clear();
    }
}
