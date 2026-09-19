using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Lytec.BluetoothLE.Shiny;

internal sealed class ShinyStartupHostedService(
    IEnumerable<IShinyStartupTask> startupTasks,
    ILogger<ShinyStartupHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var startupTask in startupTasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            startupTask.Start();
            logger.LogDebug(
                "Shiny startup task {StartupTask} ran successfully",
                startupTask.GetType().FullName);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
