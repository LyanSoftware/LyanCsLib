using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Lytec.Common;

public static class EnvInfo
{
    public static string ProcessPath { get; } = GetExecutablePath();

    static string GetExecutablePath()
    {
#if NET6_0_OR_GREATER
        if (Environment.ProcessPath != null)
            return Environment.ProcessPath;
#else
        var p = typeof(Environment).GetProperty("ProcessPath", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (p?.GetValue(null) is string v)
            return v;
#endif
#pragma warning disable CA1839 // 使用 “Environment.ProcessPath”
        return Process.GetCurrentProcess().MainModule!.FileName;
#pragma warning restore CA1839 // 使用 “Environment.ProcessPath”
    }
}
