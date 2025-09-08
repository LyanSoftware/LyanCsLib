using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Lytec.Win32;

public static partial class Win32Utils
{
    #region 常量

    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_TOPMOST = 0x00000008;
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;
    public const int LWA_ALPHA = 0;
    public const int AC_SRC_OVER = 0x00;
    public const int AC_SRC_ALPHA = 0x01;
    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    #endregion

    #region 结构

    public enum BlendFlags : uint
    {
        None = 0x00,
        ULW_COLORKEY = 0x01,
        ULW_ALPHA = 0x02,
        ULW_OPAQUE = 0x04
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;

        public BlendFunction(byte op, byte flags, byte alpha, byte format)
        {
            BlendOp = op;
            BlendFlags = flags;
            SourceConstantAlpha = alpha;
            AlphaFormat = format;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ComboBoxInfo
    {
        public static readonly int SizeConst = Marshal.SizeOf<ComboBoxInfo>();
        public static readonly ComboBoxInfo Default = new ComboBoxInfo() { cbSize = SizeConst };
        public static ComboBoxInfo NewInstance() => Default;

        public int cbSize;
        public Rect rcItem;
        public Rect rcButton;
        public int buttonState;
        public IntPtr hwndCombo;
        public IntPtr hwndEdit;
        public IntPtr hwndList;
    }

    #endregion

    #region API声明

    [DllImport("user32")]
    public static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

    [DllImport("user32")]
    public static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("user32")]
    public static extern int SetLayeredWindowAttributes(IntPtr hwnd, int crKey, int bAlpha, int dwFlags);

    [DllImport("user32")]
    public static extern bool GetCaretPos(out Point point);

    [DllImport("user32", SetLastError = true)]
    public static extern bool SetCaretPos(int x, int y);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("user32.dll")]
    public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst, ref Size psize, IntPtr hdcSrc, ref Point pptSrc, uint crKey, [In] ref BlendFunction pblend, uint dwFlags);

    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out Rect pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    public static extern bool GetComboBoxInfo(IntPtr hWnd, ref ComboBoxInfo pcbi);

    #endregion

    #region 辅助函数

    /// <summary>
    /// 设置窗体透明（鼠标穿透）效果
    /// </summary>
    /// <param name="transparent"></param>
    public static void SetWindowExTransparent(this IntPtr hwnd, bool transparent = true)
    {
        uint style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, transparent ? (style | WS_EX_TRANSPARENT) : (style & ~(uint)WS_EX_TRANSPARENT));
    }

    /// <summary>
    /// 设置分层窗口<br/>
    /// 可能需要覆盖CreateParams设置ExStyle|=WS_EX_LAYERED
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="layered"></param>
    public static void SetWindowExLayered(this IntPtr hwnd, bool layered = true)
    {
        uint style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, layered ? (style | WS_EX_LAYERED) : (style & ~(uint)WS_EX_LAYERED));
    }

    /// <summary>
    /// 设置分层窗体Alpha
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="alpha"></param>
    public static void SetLayeredWindowAlpha(this IntPtr hwnd, int alpha) => SetLayeredWindowAttributes(hwnd, 0, alpha, LWA_ALPHA);

#if USE_GDI_PLUS

    /// <summary>
    /// 使用<paramref name="bitmap"/>更新分层窗体
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="bitmap"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static bool UpdateLayeredWindow(this IntPtr hwnd, Bitmap bitmap, int x, int y) => UpdateLayeredWindow(hwnd, bitmap, new Point(x, y));

    /// <summary>
    /// 使用<paramref name="bitmap"/>更新分层窗体
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="bitmap"></param>
    /// <param name="location"></param>
    /// <returns></returns>
    public static bool UpdateLayeredWindow(this IntPtr hwnd, Bitmap bitmap, Point location)
    {
        // 1. Create a compatible DC with screen;
        // 2. Select the bitmap with 32bpp with alpha-channel in the compatible DC;
        // 3. Call the UpdateLayeredWindow.

        if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            throw new ApplicationException("The bitmap must be 32ppp with alpha-channel.");

        IntPtr screenDc = GetDC(IntPtr.Zero);
        IntPtr memDc = CreateCompatibleDC(screenDc);
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr oldBitmap = IntPtr.Zero;
        bool ok = false;

        try
        {
            hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));  // grab a GDI handle from this GDI+ bitmap
            oldBitmap = SelectObject(memDc, hBitmap);

            Size size = bitmap.Size;
            Point pointSource = new Point();
            Point topPos = location;
            BlendFunction blend = new BlendFunction
            {
                BlendOp = AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = AC_SRC_ALPHA
            };

            ok = UpdateLayeredWindow(hwnd, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, (uint)BlendFlags.ULW_ALPHA);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, screenDc);
            if (hBitmap != IntPtr.Zero)
            {
                SelectObject(memDc, oldBitmap);
                DeleteObject(hBitmap);
            }
            DeleteDC(memDc);
        }
        return ok;
    }

#endif

    #endregion

}
