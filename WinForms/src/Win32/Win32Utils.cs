using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Lytec.Win32.Win32Utils;

namespace Lytec.WinForms;

public static partial class Win32Utils
{
    /// <summary>
    /// 添加防火墙例外（按应用程序）
    /// </summary>
    /// <param name="app">目标程序</param>
    /// <param name="name">防火墙例外规则名称</param>
    /// <param name="timeout">超时时间</param>
    /// <returns></returns>
    public static bool AddFirewallException(this IWin32Window _, string? name = null, int timeout = 30000)
    => Win32.Win32Utils.AddFirewallException(Application.ExecutablePath, name, timeout);

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
    /// <summary>
    /// 设置阻止系统关闭时的提示信息
    /// </summary>
    /// <param name="window"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static bool SetShutdownBlockReason(this IWin32Window window, string message) => ShutdownBlockReasonCreate(window.Handle, message);
    /// <summary>
    /// 清除阻止系统关闭时的提示信息
    /// </summary>
    /// <param name="window"></param>
    /// <returns></returns>
    public static bool ClearShutdownBlockReason(this IWin32Window window) => ShutdownBlockReasonDestroy(window.Handle);

    public struct SystemIconId
    {
        public SHSTOCKICONID IconId;
        public SystemIconSize Size;

        public SystemIconId(SHSTOCKICONID id, SystemIconSize size) => (IconId, Size) = (id, size);
    }
    public static IReadOnlyDictionary<SystemIconId, System.Drawing.Icon> SystemIconsCache => _SystemIconsCache;
    static readonly Dictionary<SystemIconId, System.Drawing.Icon> _SystemIconsCache = new Dictionary<SystemIconId, System.Drawing.Icon>();
    public static System.Drawing.Icon GetSystemIcon(SHSTOCKICONID iconid, SystemIconSize size = SystemIconSize.Small)
    {
        var id = new SystemIconId(iconid, size);
        if (!_SystemIconsCache.TryGetValue(id, out var icon))
        {
            SHSTOCKICONINFO sii = new SHSTOCKICONINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(SHSTOCKICONINFO))
            };

            SHGSI sz = SHGSI.SHGSI_SMALLICON;
            switch (size)
            {
                case SystemIconSize.Large: sz = SHGSI.SHGSI_LARGEICON; break;
                case SystemIconSize.Shell: sz = SHGSI.SHGSI_SHELLICONSIZE; break;
            }

            Marshal.ThrowExceptionForHR(SHGetStockIconInfo(iconid,
                 SHGSI.SHGSI_ICON | sz,
                 ref sii));

            _SystemIconsCache[id] = icon = System.Drawing.Icon.FromHandle(sii.hIcon);
        }
        return icon;
    }
    public static System.Drawing.Icon GetSystemUACShieldIcon(SystemIconSize size = SystemIconSize.Small)
    => GetSystemIcon(SHSTOCKICONID.SIID_SHIELD, size);
}
