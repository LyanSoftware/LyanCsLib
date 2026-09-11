using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Lytec.AvaloniaUI.Mvu;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lytec.AvaloniaUI;

public enum MainWindowCloseBehavior
{
    FollowApplicationLifetime,
    ExitApplication,
}

public sealed partial class AvaloniaMvuOptions
{
    public MainWindowCloseBehavior MainWindowCloseBehavior { get; set; }
        = MainWindowCloseBehavior.FollowApplicationLifetime;

    /// <summary>
    /// Overrides the default platform-specific presentation of leave and
    /// cleanup failures.
    /// </summary>
    public Func<LeaveContext, Exception, ValueTask>? LeaveErrorAsync { get; set; }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAvaloniaMvu(
        this IServiceCollection services,
        Application application,
        Action<AvaloniaMvuOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(application);

        var options = new AvaloniaMvuOptions();
        configure?.Invoke(options);

        var desktop = application.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime;
        var presentation = options.Presentation switch
        {
            ViewPresentation.Auto => desktop is null
                ? ViewPresentation.Navigation
                : ViewPresentation.Window,
            ViewPresentation.Window when desktop is null =>
                throw new InvalidOperationException(
                    "Window presentation requires IClassicDesktopStyleApplicationLifetime."),
            _ => options.Presentation,
        };

        services.TryAddSingleton<Application>(application);
        services.TryAddSingleton(options);
        options.RegisterConfiguredViews(
            services,
            includeDrawers: presentation is ViewPresentation.Navigation);
        services.TryAddSingleton<ViewRegistry>();
        services.TryAddSingleton<IApplicationExitService, ApplicationExitService>();

        if (desktop is not null)
            services.TryAddSingleton<IClassicDesktopStyleApplicationLifetime>(desktop);

        if (presentation is ViewPresentation.Window)
        {
            services.TryAddSingleton<WindowViewManager>();
            services.TryAddSingleton<IViewManager>(
                static provider => provider.GetRequiredService<WindowViewManager>());
        }
        else
        {
            services.TryAddSingleton<NavigationViewManager>();
            services.TryAddSingleton<IViewManager>(
                static provider => provider.GetRequiredService<NavigationViewManager>());
            services.TryAddSingleton<INavigationViewManager>(
                static provider => provider.GetRequiredService<NavigationViewManager>());
        }

        return services;
    }
}
