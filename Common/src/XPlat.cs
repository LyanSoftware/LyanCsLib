using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Lytec.Common;

public class XPlat
{
    public enum Platform
    {
        Unknown = 0,
        Windows,
        Linux,
        MacOS
    }

    public static Platform GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Platform.Windows;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Platform.Linux;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return Platform.MacOS;
        else return Platform.Unknown;
    }
    public static readonly Platform CurrentPlatform = GetCurrentPlatform();

    public static void OpenUrl(string url)
    {
        switch (CurrentPlatform)
        {
            default:
            case Platform.Unknown:
                throw new Exception("invalid url: " + url);
            case Platform.Windows:
                //https://stackoverflow.com/a/2796367/241446
                Process.Start(new ProcessStartInfo() { UseShellExecute = true, FileName = url });
                break;
            case Platform.Linux:
                Process.Start("x-www-browser", url);
                break;
            case Platform.MacOS:
                Process.Start("open", url);
                break;
        }
    }
}
