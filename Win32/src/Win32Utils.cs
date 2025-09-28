using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Lytec.Win32;

public static partial class Win32Utils
{
#region 常量

    public const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;
    public const int ERROR_HOTKEY_NOT_REGISTERED = 1419;

#endregion

#region 结构

    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Ctrl = 2,
        Shift = 4,
        WindowsKey = 8
    }

    /// <summary>
    /// Win32API异常
    /// </summary>
    public class Win32APIException : Exception
    {
        public int ErrorCode { get; }

        public Win32APIException(int errorCode, string message = "") : base(message) => ErrorCode = errorCode;
    }

    #endregion

#region API声明

    public enum SCColorType : uint
    {
        SuperComm_RG = 0,
        SuperComm_RGB = 1,
        SCL2008_RG = 2,
        SCL2008_RGB = 3,
        SC3000_RGB = 4
    }
    /// <summary>
    /// 将BMP或JPEG文件转换成包含一个图片的XMP文件，不支持其它格式的图片文件。
    /// </summary>
    /// <param name="ColorType">颜色类型</param>
    /// <param name="Width">目标XMP图像宽度</param>
    /// <param name="Height">目标XMP图像高度</param>
    /// <param name="bStretched">是否拉伸原图像至目标宽高</param>
    /// <param name="PictFileName">要转换的BMP或JPEG文件路径</param>
    /// <param name="XMPFileName">输出XMP文件路径</param>
    /// <returns></returns>
    [DllImport("SCL_API_cdecl.dll", CallingConvention = CallingConvention.Cdecl)]
    extern static bool SCL_PicToXMPFile(SCColorType ColorType, int Width, int Height, bool bStretched, string PictFileName, string XMPFileName);
    /// <summary>
    /// 获取上一个WINAPI函数的错误信息
    /// </summary>
    /// <returns></returns>
    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();

    /// <summary>
    /// 注册全局热键
    /// </summary>
    /// <param name="hwnd">绑定的窗体</param>
    /// <param name="hotkeyID">热键唯一ID</param>
    /// <param name="keyModifiers">修饰键</param>
    /// <param name="key">主键</param>
    /// <returns></returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hwnd, int hotkeyID, KeyModifiers keyModifiers, uint key);

    /// <summary>
    /// 注销全局热键
    /// </summary>
    /// <param name="hWnd">绑定的窗体</param>
    /// <param name="id">热键唯一ID</param>
    /// <returns></returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>
    /// 在路径及标准扩展路径中查找文件
    /// </summary>
    /// <param name="pszFile">要查找的文件名</param>
    /// <param name="ppszOtherDirs">查找路径</param>
    /// <returns></returns>
    [DllImport("shlwapi.dll")]
    static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In] string[] ppszOtherDirs);

    /// <summary>
    /// 设置阻止系统关闭时的提示信息
    /// </summary>
    /// <param name="hWnd"></param>
    /// <param name="pwszReason"></param>
    /// <returns></returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] string reason);

    /// <summary>
    /// 清除阻止系统关闭时的提示信息
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns></returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);

    #endregion

    #region 辅助函数

    /// <summary>
    /// 判断是否正以管理员权限运行
    /// </summary>
    /// <returns></returns>
    public static bool IsRunningAsAdministrator()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    /// <summary>
    /// 添加防火墙例外（按应用程序）
    /// </summary>
    /// <param name="path">目标程序</param>
    /// <param name="name">防火墙例外规则名称</param>
    /// <param name="timeout">超时时间</param>
    /// <returns></returns>
    public static bool AddFirewallException(string path, string? name = null, int timeout = 30000, Encoding? encoding = default)
    {
        if (name == null)
            name = Path.GetFileNameWithoutExtension(path);
        var bat = Path.GetTempPath() + Guid.NewGuid() + ".bat";
        try
        {
            File.WriteAllText(bat, $"netsh advfirewall firewall delete rule name=all program=\"{path}\""
                + "\r\n"
                + $"netsh advfirewall firewall add rule name=\"{name}\" dir=in action=allow profile=any program=\"{path}\" enable=yes"
                , encoding ?? Encoding.Default);
            var info = new System.Diagnostics.ProcessStartInfo()
            {
                FileName = "cmd.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                Arguments = $"/c {bat}"
            };
            if (!IsRunningAsAdministrator())
            {
                info.Verb = "runas";
                info.UseShellExecute = true;
            }
            var process = System.Diagnostics.Process.Start(info);
            if (!process.WaitForExit(timeout))
            {
                process.Kill();
                return false;
            }
            return process.ExitCode == 0;
        }
        finally
        {
            File.Delete(bat);
        }
    }

    /// <summary>
    /// 设置阻止系统关闭时的提示信息
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static bool SetShutdownBlockReason(IntPtr hwnd, string message) => Win32.Win32Utils.ShutdownBlockReasonCreate(hwnd, message);
    /// <summary>
    /// 清除阻止系统关闭时的提示信息
    /// </summary>
    /// <param name="hwnd"></param>
    /// <returns></returns>
    public static bool ClearShutdownBlockReason(IntPtr hwnd) => ShutdownBlockReasonDestroy(hwnd);
    #region Serach in System Extend Path


    private const int MAX_PATH = 260;
    private static readonly Lazy<Dictionary<string, string>> appPaths = new Lazy<Dictionary<string, string>>(LoadAppPaths);
    private static readonly Lazy<string[]> executableExtensions = new Lazy<string[]>(LoadExecutableExtensions);

    public static string? TryGetFullPathForCommand(string command)
    {
        if (Path.HasExtension(command))
            return TryGetFullPathForFileName(command);

        return TryGetFullPathByProbingExtensions(command);
    }

    private static string[] LoadExecutableExtensions() => Environment.GetEnvironmentVariable("PATHEXT").Split(';');

    private static Dictionary<string, string> LoadAppPaths()
    {
        var appPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths");
        if (key == null)
            return appPaths;
        foreach (var subkeyName in key.GetSubKeyNames())
        {
            using var subkey = key.OpenSubKey(subkeyName);
            var v = subkey?.GetValue(string.Empty)?.ToString();
            if (v == null)
                continue;
            appPaths.Add(subkeyName, v);
        }

        return appPaths;
    }

    private static string? TryGetFullPathByProbingExtensions(string command)
    {
        foreach (var extension in executableExtensions.Value)
        {
            var result = TryGetFullPathForFileName(command + extension);
            if (result != null)
                return result;
        }

        return null;
    }

    private static string? TryGetFullPathForFileName(string fileName) =>
        TryGetFullPathFromPathEnvironmentVariable(fileName) ?? TryGetFullPathFromAppPaths(fileName);

    private static string? TryGetFullPathFromAppPaths(string fileName) =>
        appPaths.Value.TryGetValue(fileName, out var path) ? path : null;

    private static string? TryGetFullPathFromPathEnvironmentVariable(string fileName)
    {
        if (fileName.Length >= MAX_PATH)
            throw new ArgumentException($"The executable name '{fileName}' must have less than {MAX_PATH} characters.", nameof(fileName));

        var sb = new StringBuilder(fileName, MAX_PATH);
        return PathFindOnPath(sb, new string[] { Directory.GetCurrentDirectory() }) ? sb.ToString() : null;
    }

    #endregion

    #region Get System Icon

    [Flags]
    public enum SHGSI : uint
    {
        SHGSI_ICONLOCATION = 0,
        SHGSI_ICON = 0x000000100,
        SHGSI_SYSICONINDEX = 0x000004000,
        SHGSI_LINKOVERLAY = 0x000008000,
        SHGSI_SELECTED = 0x000010000,
        SHGSI_LARGEICON = 0x000000000,
        SHGSI_SMALLICON = 0x000000001,
        SHGSI_SHELLICONSIZE = 0x000000004
    }
    public enum SHSTOCKICONID : uint
    {
        SIID_DOCNOASSOC = 0,
        SIID_DOCASSOC = 1,
        SIID_APPLICATION = 2,
        SIID_FOLDER = 3,
        SIID_FOLDEROPEN = 4,
        SIID_DRIVE525 = 5,
        SIID_DRIVE35 = 6,
        SIID_DRIVEREMOVE = 7,
        SIID_DRIVEFIXED = 8,
        SIID_DRIVENET = 9,
        SIID_DRIVENETDISABLED = 10,
        SIID_DRIVECD = 11,
        SIID_DRIVERAM = 12,
        SIID_WORLD = 13,
        SIID_SERVER = 15,
        SIID_PRINTER = 16,
        SIID_MYNETWORK = 17,
        SIID_FIND = 22,
        SIID_HELP = 23,
        SIID_SHARE = 28,
        SIID_LINK = 29,
        SIID_SLOWFILE = 30,
        SIID_RECYCLER = 31,
        SIID_RECYCLERFULL = 32,
        SIID_MEDIACDAUDIO = 40,
        SIID_LOCK = 47,
        SIID_AUTOLIST = 49,
        SIID_PRINTERNET = 50,
        SIID_SERVERSHARE = 51,
        SIID_PRINTERFAX = 52,
        SIID_PRINTERFAXNET = 53,
        SIID_PRINTERFILE = 54,
        SIID_STACK = 55,
        SIID_MEDIASVCD = 56,
        SIID_STUFFEDFOLDER = 57,
        SIID_DRIVEUNKNOWN = 58,
        SIID_DRIVEDVD = 59,
        SIID_MEDIADVD = 60,
        SIID_MEDIADVDRAM = 61,
        SIID_MEDIADVDRW = 62,
        SIID_MEDIADVDR = 63,
        SIID_MEDIADVDROM = 64,
        SIID_MEDIACDAUDIOPLUS = 65,
        SIID_MEDIACDRW = 66,
        SIID_MEDIACDR = 67,
        SIID_MEDIACDBURN = 68,
        SIID_MEDIABLANKCD = 69,
        SIID_MEDIACDROM = 70,
        SIID_AUDIOFILES = 71,
        SIID_IMAGEFILES = 72,
        SIID_VIDEOFILES = 73,
        SIID_MIXEDFILES = 74,
        SIID_FOLDERBACK = 75,
        SIID_FOLDERFRONT = 76,
        SIID_SHIELD = 77,
        SIID_WARNING = 78,
        SIID_INFO = 79,
        SIID_ERROR = 80,
        SIID_KEY = 81,
        SIID_SOFTWARE = 82,
        SIID_RENAME = 83,
        SIID_DELETE = 84,
        SIID_MEDIAAUDIODVD = 85,
        SIID_MEDIAMOVIEDVD = 86,
        SIID_MEDIAENHANCEDCD = 87,
        SIID_MEDIAENHANCEDDVD = 88,
        SIID_MEDIAHDDVD = 89,
        SIID_MEDIABLURAY = 90,
        SIID_MEDIAVCD = 91,
        SIID_MEDIADVDPLUSR = 92,
        SIID_MEDIADVDPLUSRW = 93,
        SIID_DESKTOPPC = 94,
        SIID_MOBILEPC = 95,
        SIID_USERS = 96,
        SIID_MEDIASMARTMEDIA = 97,
        SIID_MEDIACOMPACTFLASH = 98,
        SIID_DEVICECELLPHONE = 99,
        SIID_DEVICECAMERA = 100,
        SIID_DEVICEVIDEOCAMERA = 101,
        SIID_DEVICEAUDIOPLAYER = 102,
        SIID_NETWORKCONNECT = 103,
        SIID_INTERNET = 104,
        SIID_ZIPFILE = 105,
        SIID_SETTINGS = 106,
        SIID_DRIVEHDDVD = 132,
        SIID_DRIVEBD = 133,
        SIID_MEDIAHDDVDROM = 134,
        SIID_MEDIAHDDVDR = 135,
        SIID_MEDIAHDDVDRAM = 136,
        SIID_MEDIABDROM = 137,
        SIID_MEDIABDR = 138,
        SIID_MEDIABDRE = 139,
        SIID_CLUSTEREDDRIVE = 140,
        SIID_MAX_ICONS = 175
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SHSTOCKICONINFO
    {
        public uint cbSize;
        public IntPtr hIcon;
        public int iSysIconIndex;
        public int iIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        public string szPath;
    }
    [DllImport("Shell32.dll", SetLastError = false)]
    public static extern int SHGetStockIconInfo(SHSTOCKICONID siid, SHGSI uFlags, ref SHSTOCKICONINFO psii);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern int DestroyIcon(IntPtr hIcon);
    public enum SystemIconSize
    {
        Small,
        Large,
        Shell
    }
#endregion

#endregion

}
