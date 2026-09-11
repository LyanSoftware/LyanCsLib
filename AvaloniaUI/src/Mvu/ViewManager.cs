using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Declarative;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lytec.AvaloniaUI.Mvu;

public enum ViewPresentation
{
    Auto,
    Window,
    Navigation,
}

/// <summary>
/// Describes presentation metadata shared by window and page hosts.
/// </summary>
public record ViewInfo
{
    public string? Title { get; init; }

    /// <summary>
    /// Optional live binding used for both a window title and a page header.
    /// </summary>
    public BindingBase? TitleBinding { get; init; }

    public WindowInfo Window { get; init; } = new();
}

/// <summary>
/// A declarative view that can be hosted by an <see cref="IViewManager"/>.
/// </summary>
public interface IManagedView : ILeaveAware
{
    ViewInfo ViewOptions { get; }
}

/// <summary>
/// Binding-friendly state shared by all presentation implementations.
/// </summary>
public sealed class ViewManagerState : INotifyPropertyChanged
{
    private bool isBusy;
    private string? currentRootId;
    private int stackDepth;
    private int modalDepth;
    private string? openDrawerId;

    public bool IsBusy
    {
        get => isBusy;
        internal set => Set(ref isBusy, value, nameof(IsBusy));
    }

    public string? CurrentRootId
    {
        get => currentRootId;
        internal set => Set(ref currentRootId, value, nameof(CurrentRootId));
    }

    public int StackDepth
    {
        get => stackDepth;
        internal set => Set(ref stackDepth, value, nameof(StackDepth));
    }

    public int ModalDepth
    {
        get => modalDepth;
        internal set => Set(ref modalDepth, value, nameof(ModalDepth));
    }

    public string? OpenDrawerId
    {
        get => openDrawerId;
        internal set => Set(ref openDrawerId, value, nameof(OpenDrawerId));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Platform-neutral view presentation and guarded navigation.
/// </summary>
public interface IViewManager : IDisposable
{
    ViewManagerState State { get; }

    event EventHandler? StateChanged;

    Control InstallRoot(string routeId);

    Task<bool> OpenAsync(string routeId, Control? ownerView = null);

    Task<bool> OpenPathAsync(
        IReadOnlyList<string> routeIds,
        Control? ownerView = null);

    Task<bool> ShowModalAsync(string routeId, Control? ownerView = null);

    Task<bool> BackAsync();

    ValueTask<bool> TryLeaveAllAsync(bool mayBeTerminatedBySystem = false);
}

internal abstract class ViewRegistration(string id)
{
    public string Id { get; } = id;

    public abstract void Register(IServiceCollection services);

    public abstract ManagedViewLease Create(IServiceScopeFactory scopeFactory);
}

internal sealed class ViewRegistration<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView>(string id)
    : ViewRegistration(id)
    where TView : ViewBase, IManagedView
{
    public override void Register(IServiceCollection services)
        => services.TryAddTransient<TView>();

    public override ManagedViewLease Create(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();
        try
        {
            return new ManagedViewLease(
                Id,
                scope.ServiceProvider.GetRequiredService<TView>(),
                scope);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }
}

internal abstract class DrawerRegistration(string id, DrawerOptions options)
{
    public string Id { get; } = id;
    public DrawerOptions Options { get; } = options;

    public abstract void Register(IServiceCollection services);

    public abstract DrawerViewLease Create(IServiceScopeFactory scopeFactory);
}

internal sealed class DrawerRegistration<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView>(string id, DrawerOptions options)
    : DrawerRegistration(id, options)
    where TView : ViewBase
{
    public override void Register(IServiceCollection services)
        => services.TryAddTransient<TView>();

    public override DrawerViewLease Create(IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateScope();
        try
        {
            return new DrawerViewLease(
                Id,
                scope.ServiceProvider.GetRequiredService<TView>(),
                Options,
                scope);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }
}

internal sealed class ManagedViewLease(
    string routeId,
    ViewBase view,
    IServiceScope scope) : IDisposable
{
    private IServiceScope? scope = scope;

    public string RouteId { get; } = routeId;
    public ViewBase View { get; } = view;
    public IManagedView ManagedView { get; } = (IManagedView)view;

    public void Dispose()
        => Interlocked.Exchange(ref scope, null)?.Dispose();
}

internal sealed class DrawerViewLease(
    string id,
    ViewBase view,
    DrawerOptions options,
    IServiceScope scope) : IDisposable
{
    private IServiceScope? scope = scope;

    public string Id { get; } = id;
    public ViewBase View { get; } = view;
    public DrawerOptions Options { get; } = options;

    public void Dispose()
        => Interlocked.Exchange(ref scope, null)?.Dispose();
}

internal sealed class ViewRegistry(
    AvaloniaMvuOptions options,
    IServiceScopeFactory scopeFactory)
{
    public ManagedViewLease Create(string routeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeId);
        return options.Views.TryGetValue(routeId, out var registration)
            ? registration.Create(scopeFactory)
            : throw new KeyNotFoundException($"No view route named '{routeId}' is registered.");
    }

    public IReadOnlyDictionary<string, DrawerRegistration> Drawers => options.Drawers;

    public DrawerViewLease CreateDrawer(string id)
        => options.Drawers.TryGetValue(id, out var registration)
            ? registration.Create(scopeFactory)
            : throw new KeyNotFoundException($"No drawer named '{id}' is registered.");
}
