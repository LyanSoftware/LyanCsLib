using System;
using System.Drawing;
using System.Runtime.InteropServices;
using static Lytec.Win32.Win32Utils;

namespace Lytec.WinForms;

public static partial class Win32Utils
{
    public static byte GetCharSet(this Font font, IDeviceContext? dc = null)
    {
        var tmpdc = dc ?? Graphics.FromHwnd(IntPtr.Zero);
        var hDC = IntPtr.Zero;
        var hFont = IntPtr.Zero;
        var hFontDefault = IntPtr.Zero;

        try
        {
            hDC = tmpdc.GetHdc();

            hFont = font.ToHfont();
            hFontDefault = Win32.Win32Utils.SelectObject(hDC, hFont);

            Win32.Win32Utils.GetTextMetrics(hDC, out TEXTMETRICW textMetric);

            return textMetric.tmCharSet;
        }
        finally
        {
            if (hFontDefault != IntPtr.Zero)
                Win32.Win32Utils.SelectObject(hDC, hFontDefault);
            if (hFont != IntPtr.Zero)
                Win32.Win32Utils.DeleteObject(hFont);
            tmpdc.ReleaseHdc();
            if (dc == null)
                tmpdc.Dispose();
        }
    }

    /// <summary>
    /// 创建GDI字体对象
    /// </summary>
    /// <param name="family"></param>
    /// <param name="size"></param>
    /// <param name="style"></param>
    /// <returns>GDI字体对象指针</returns>
    public static IntPtr CreateFont(string family, int size, FontStyle style)
    => Win32.Win32Utils.CreateFont(family, size, style.HasFlag(FontStyle.Bold) ? FW_BOLD : FW_NORMAL, style.HasFlag(FontStyle.Italic), style.HasFlag(FontStyle.Underline), style.HasFlag(FontStyle.Strikeout));

    public static void TextOut(this Graphics g, string text, IntPtr gdiFont, Color color, int x = 0, int y = 0, Color? bgColor = null)
    {
        var hdc = g.GetHdc();
        Win32.Win32Utils.SetTextColor(hdc, GDIUtils.ARGB2ABGR(color.ToArgb()));
        if (bgColor != null)
            Win32.Win32Utils.SetBkColor(hdc, bgColor.Value.ToABGR());
        Win32.Win32Utils.SetBkMode(hdc, bgColor == null ? BKMODE_TRANSPARENT : BKMODE_OPAQUE);
        Win32.Win32Utils.SelectObject(hdc, gdiFont);
        Win32.Win32Utils.TextOut(hdc, text, x, y);
        g.ReleaseHdc(hdc);
    }

}
