using System;
using System.Drawing;
using System.Runtime.InteropServices;
#if CS_WINFORM
using System.Windows.Forms;
#endif

namespace Lytec.Win32;

public static partial class Win32Utils
{
    #region 常量

    public const int WM_LBUTTONDOWN = 0x201;
    public const int WM_LBUTTONUP = 0x202;
    public const int HTLEFT = 10;
    public const int HTRIGHT = 11;
    public const int HTTOP = 12;
    public const int HTTOPLEFT = 13;
    public const int HTTOPRIGHT = 14;
    public const int HTBOTTOM = 15;
    public const int HTBOTTOMLEFT = 0x10;
    public const int HTBOTTOMRIGHT = 17;

    /// <summary>
    /// 广播句柄
    /// </summary>
    public const int HWND_BROADCAST = 0xffff;
    /// <summary>
    /// 广播句柄
    /// </summary>
    public static readonly IntPtr Handle_Broadcast = new IntPtr(HWND_BROADCAST);

    /// <summary>
    /// 确认是否可以注销
    /// </summary>
    public const int WM_QUERYENDSESSION = 0x0011;
    /// <summary>
    /// 注销
    /// </summary>
    public const int WM_ENDSESSION = 0x0016;

    #endregion

    #region 结构

    #endregion

    #region API声明

    /// <summary>
    /// 发送Windows消息
    /// </summary>
    /// <param name="hWnd"></param>
    /// <param name="Msg"></param>
    /// <param name="wParam"></param>
    /// <param name="lParam"></param>
    /// <returns></returns>
    [DllImport("user32", SetLastError = true)]
    public static extern bool PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    /// <summary>
    /// 注册Windows消息
    /// </summary>
    /// <param name="message">消息名称</param>
    /// <returns></returns>
    [DllImport("user32")]
    public static extern int RegisterWindowMessage(string message);

    #endregion

    #region 辅助函数

#if CS_WINFORM
    /// <summary>
    /// 是否为注销或关机消息
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static bool IsEndSession(this Message msg) => msg.Msg == WM_QUERYENDSESSION || msg.Msg == WM_ENDSESSION;
#endif

#endregion

}
