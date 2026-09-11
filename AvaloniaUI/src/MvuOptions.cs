using Avalonia.Markup.Declarative;
using System.Diagnostics.CodeAnalysis;
using Lytec.AvaloniaUI.Mvu;
using Microsoft.Extensions.DependencyInjection;

namespace Lytec.AvaloniaUI;

public sealed partial class AvaloniaMvuOptions
{
    private readonly Dictionary<string, ViewRegistration> views =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, DrawerRegistration> drawers =
        new(StringComparer.Ordinal);

    public ViewPresentation Presentation { get; set; } = ViewPresentation.Auto;

    public NavigationHostOptions Navigation { get; } = new();

    public AvaloniaMvuOptions AddView<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView>(string id)
        where TView : ViewBase, IManagedView
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (!views.TryAdd(id, new ViewRegistration<TView>(id)))
            throw new ArgumentException(
                $"A view route named '{id}' is already registered.",
                nameof(id));
        return this;
    }

    public AvaloniaMvuOptions AddDrawer<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView>(
        string id,
        DrawerEdge edge,
        Action<DrawerOptions>? configure = null)
        where TView : ViewBase
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var drawerOptions = new DrawerOptions { Edge = edge };
        configure?.Invoke(drawerOptions);
        if (!drawers.TryAdd(
            id,
            new DrawerRegistration<TView>(id, drawerOptions)))
        {
            throw new ArgumentException(
                $"A drawer named '{id}' is already registered.",
                nameof(id));
        }
        return this;
    }

    internal IReadOnlyDictionary<string, ViewRegistration> Views => views;
    internal IReadOnlyDictionary<string, DrawerRegistration> Drawers => drawers;

    internal void RegisterConfiguredViews(
        IServiceCollection services,
        bool includeDrawers)
    {
        foreach (var view in views.Values)
            view.Register(services);
        if (includeDrawers)
        {
            foreach (var drawer in drawers.Values)
                drawer.Register(services);
        }
    }
}
