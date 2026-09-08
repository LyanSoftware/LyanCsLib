using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.AvaloniaUI;

public static class ApplicationExtensions
{
    public static void Shutdown(this Application app, int exitCode)
    {
        switch (app.ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.Shutdown(exitCode);
                break;
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = null;
                break;
            case IControlledApplicationLifetime controlled:
                controlled.Shutdown(exitCode);
                break;
        }
    }
    public static void Shutdown(this Application app) => Shutdown(app, 0);
}
