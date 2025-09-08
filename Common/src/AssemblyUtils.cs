using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;

namespace Lytec.Common
{
    public static class AssemblyUtils
    {
        public static string GetFileVersion(this Assembly assembly)
        => GetFileVersion(assembly.Location);
        public static string GetFileVersion(string filePath)
        => FileVersionInfo.GetVersionInfo(filePath).FileVersion;
        public static string? GetShortFileVersion(this Assembly assembly)
        => GetShortFileVersion(assembly.Location);
        private static readonly Regex FileVersionShorterRegex = new Regex(@"^(\d+\.\d.*?)\.0+$", RegexOptions.Compiled);
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
        public static void SetLoadResourcesLibraries(this AppDomain appDomain, string Namespace)
        => appDomain.AssemblyResolve += (sender, args) =>
        {
            string dllName = args.Name.Contains(",") ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");
            dllName = dllName.Replace(".", "_");
            if (dllName.EndsWith("_resources")) return null;
            ResourceManager rm = new ResourceManager(Namespace + ".Properties.Resources", Assembly.GetExecutingAssembly());
            byte[] bytes = (byte[])rm.GetObject(dllName);
            return Assembly.Load(bytes);
        };

    }
}
