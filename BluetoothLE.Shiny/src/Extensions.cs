using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shiny.Infrastructure;

namespace Lytec.BluetoothLE.Shiny;

public static class Extensions
{
    public static IServiceCollection AddBleService(this IServiceCollection services)
    {
        services.AddShinyCoreServices();
        services.TryAddSingleton<IBleService, BleService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ShinyStartupHostedService>());
        return services;
    }
}
