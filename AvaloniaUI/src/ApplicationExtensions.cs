using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Lytec.AvaloniaUI.Mvu;

namespace Lytec.AvaloniaUI;

public static class ApplicationExtensions
{
    public static async Task<bool> TryShutdownAsync(this Application app, int exitCode = 0)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return await WindowManager.TryShutdownAsync(desktop, exitCode);

        var controlled = app.ApplicationLifetime as IControlledApplicationLifetime;
        var exitHandler = ApplicationExit.GetExitHandler(app);
        if (controlled is null && exitHandler is null)
            return false;

        if (ApplicationExit.GetExitGuard(app) is { } guard
            && !await guard.TryLeaveApplicationAsync())
            return false;

        if (controlled is not null)
        {
            controlled.Shutdown(exitCode);
            return true;
        }

        return await exitHandler!.TryExitAsync(exitCode);
    }
}
