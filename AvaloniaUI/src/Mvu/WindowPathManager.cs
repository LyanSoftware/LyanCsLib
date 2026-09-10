using Avalonia.Controls;
using Lytec.Common.Localization.Extensions;

namespace Lytec.AvaloniaUI.Mvu;

/// <summary>
/// Describes one modal window in a route from a desktop main window.
/// </summary>
public abstract record WindowPathSegment
{
    public string Id { get; }

    private protected WindowPathSegment(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    internal abstract CreatedWindow Create(IWindowManager windowManager);
    
    public static WindowPathSegment Create<TView>(string id, Func<TView> createView)
        where TView : Control, IWindowView
    => new WindowPathManager.WindowPathSegment<TView>(id, createView);
}

internal readonly record struct CreatedWindow(Control View, Window Window);

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

    internal sealed record WindowPathSegment<TView> : WindowPathSegment
        where TView : Control, IWindowView
    {
        public Func<TView> CreateView { get; }

        public WindowPathSegment(string id, Func<TView> createView)
            : base(id)
        {
            ArgumentNullException.ThrowIfNull(createView);
            CreateView = createView;
        }

        internal override CreatedWindow Create(IWindowManager windowManager)
        {
            var view = CreateView()
                ?? throw new InvalidOperationException()
                    .Localize(
                        LocalizeScope,
                        "OpenPathError_NullView",
                        new { Id },
                        "窗口路径段“{Id}”返回了 null 视图。");
            return new CreatedWindow(view, windowManager.Create(view));
        }
    }

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
                var created = segment.Create(windowManager);
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
