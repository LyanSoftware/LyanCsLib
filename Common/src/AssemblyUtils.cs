using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;

namespace Lytec.Common
{
    public static partial class AssemblyUtils
    {
#if NET7_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.SuppressMessage("SingleFile", "IL3000: 单文件发布模式下Assembly.Location总是返回空字符串", Justification = "<挂起>")]
#endif
        public static string? GetFileVersion(this Assembly assembly)
        => !assembly.Location.IsNullOrEmpty() ? GetFileVersion(assembly.Location) : null;
        public static string? GetFileVersion(string filePath)
        => FileVersionInfo.GetVersionInfo(filePath).FileVersion;
#if NET7_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.SuppressMessage("SingleFile", "IL3000: 单文件发布模式下Assembly.Location总是返回空字符串", Justification = "<挂起>")]
#endif
        public static string? GetShortFileVersion(this Assembly assembly)
        => !assembly.Location.IsNullOrEmpty() ? GetShortFileVersion(assembly.Location) : null;
#if NET7_0_OR_GREATER
        [GeneratedRegex(@"^(\d+\.\d.*?)\.0+$", RegexOptions.Compiled)]
        private static partial Regex GetFileVersionShorterRegex();
        private static Regex FileVersionShorterRegex { get; } = GetFileVersionShorterRegex();
#else
        private static Regex FileVersionShorterRegex { get; } = new Regex(@"^(\d+\.\d.*?)\.0+$", RegexOptions.Compiled);
#endif
        public static string? GetShortFileVersion(string filePath)
        {
            var v = GetFileVersion(filePath);
            if (v.IsNullOrEmpty())
                return null;
            for (var m = FileVersionShorterRegex.Match(v); m.Success; m = FileVersionShorterRegex.Match(v))
                v = m.Groups[1].Value;
            return v;
        }

        /// <summary>
        /// 设置尝试加载Resources内dll
        /// </summary>
        /// <param name="appDomain"></param>
        /// <param name="Namespace">Resources所属命名空间</param>
        [RequiresUnreferencedCode("依赖Assembly.Load加载dll")]
        public static void SetLoadResourcesLibraries(this AppDomain appDomain, string Namespace)
        => appDomain.AssemblyResolve += (sender, args) =>
        {
            string dllName =
#if NET6_0_OR_GREATER
                args.Name.Contains(',')
#else
                args.Name.Contains(",")
#endif
                ? args.Name[..args.Name.IndexOf(',')] : args.Name.Replace(".dll", "");
            dllName = dllName.Replace(".", "_");
            if (dllName.EndsWith("_resources")) return null;
            ResourceManager rm = new ResourceManager(Namespace + ".Properties.Resources", Assembly.GetExecutingAssembly());
            if (rm.GetObject(dllName) is byte[] bytes)
                return Assembly.Load(bytes);
            throw new MissingManifestResourceException();
        };

    }
}
