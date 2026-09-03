using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Lytec.AvaloniaUI.Mvu;

namespace Lytec.AvaloniaUI;

public static class ApplicationExtensions
{
    public static async Task<bool> TryShutdownAsync(this Application app, int exitCode = 0)
    {
        ArgumentNullException.ThrowIfNull(app);

        switch (app.ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                return await WindowManager.TryShutdownAsync(desktop, exitCode);
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = null;
                return true;
            case IControlledApplicationLifetime controlled:
                controlled.Shutdown(exitCode);
                return true;
            default:
                return false;
        }
    }
}
