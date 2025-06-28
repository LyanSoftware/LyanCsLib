using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Lytec.Common
{
    public static class VersionUtils
    {
        public static (string? Version, string? FileVersion) GetVersion(this Assembly assembly)
        {
            var ver = assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version;
            var fver = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            return (ver, fver);
        }
        public static (string? Version, string? FileVersion) GetAssemblyVersion(this Type t)
        => t.Assembly.GetVersion();
    }
}
