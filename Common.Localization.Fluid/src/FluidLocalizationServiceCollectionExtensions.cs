using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lytec.Common.Localization.Fluid;

public static class FluidLocalizationServiceCollectionExtensions
{
    public static IServiceCollection AddFluidLocalizationTemplateRenderer(
        this IServiceCollection services,
        LocalizationTemplateOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(options ?? new LocalizationTemplateOptions());
        services.TryAddSingleton<
            ILocalizationTemplateRenderer,
            FluidLocalizationTemplateRenderer>();
        return services;
    }
}
