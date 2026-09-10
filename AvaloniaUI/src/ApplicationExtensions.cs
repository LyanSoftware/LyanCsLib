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

public sealed class AvaloniaMvuOptions
{
    public MainWindowCloseBehavior MainWindowCloseBehavior { get; set; }
        = MainWindowCloseBehavior.FollowApplicationLifetime;
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

        services.TryAddSingleton<Application>(application);
        services.TryAddSingleton(options);
        services.TryAddSingleton<INavigationStackManagerFactory, NavigationStackManagerFactory>();
        services.TryAddSingleton<IApplicationExitService, ApplicationExitService>();

        if (application.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
        {
            services.TryAddSingleton<IClassicDesktopStyleApplicationLifetime>(desktop);
            services.TryAddSingleton<IWindowManager, WindowManager>();
            services.TryAddSingleton<IWindowPathManagerFactory, WindowPathManagerFactory>();
        }

        return services;
    }
}
