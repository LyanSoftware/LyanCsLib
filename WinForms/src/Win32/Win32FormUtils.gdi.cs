using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Lytec.WinForms;

public static partial class Win32FormUtils
{
    #region 常量

    public const int FW_DONTCARE = 0;
    public const int FW_THIN = 100;
    public const int FW_EXTRALIGHT = 200;
    public const int FW_ULTRALIGHT = 200;
    public const int FW_LIGHT = 300;
    public const int FW_NORMAL = 400;
    public const int FW_REGULAR = 400;
    public const int FW_MEDIUM = 500;
    public const int FW_SEMIBOLD = 600;
    public const int FW_DEMIBOLD = 600;
    public const int FW_BOLD = 700;
    public const int FW_EXTRABOLD = 800;
    public const int FW_ULTRABOLD = 800;
    public const int FW_HEAVY = 900;
    public const int FW_BLACK = 900;
    public const int BKMODE_TRANSPARENT = 1;
    public const int BKMODE_OPAQUE = 2;

    public const int DT_TOP = 0x00000000;
    public const int DT_LEFT = 0x00000000;
    public const int DT_CENTER = 0x00000001;
    public const int DT_RIGHT = 0x00000002;
    public const int DT_VCENTER = 0x00000004;
    public const int DT_BOTTOM = 0x00000008;
    public const int DT_WORDBREAK = 0x00000010;
    public const int DT_SINGLELINE = 0x00000020;
    public const int DT_EXPANDTABS = 0x00000040;
    public const int DT_TABSTOP = 0x00000080;
    public const int DT_NOCLIP = 0x00000100;
    public const int DT_EXTERNALLEADING = 0x00000200;
    public const int DT_CALCRECT = 0x00000400;
    public const int DT_NOPREFIX = 0x00000800;
    public const int DT_INTERNAL = 0x00001000;
    public const int DT_EDITCONTROL = 0x00002000;
    public const int DT_PATH_ELLIPSIS = 0x00004000;
    public const int DT_END_ELLIPSIS = 0x00008000;
    public const int DT_MODIFYSTRING = 0x00010000;
    public const int DT_RTLREADING = 0x00020000;
    public const int DT_WORD_ELLIPSIS = 0x00040000;
    public const int DT_NOFULLWIDTHCHARBREAK = 0x00080000;
    public const int DT_HIDEPREFIX = 0x00100000;
    public const int DT_PREFIXONLY = 0x00200000;

    #endregion

    #region 结构

    [Flags]
    public enum DrawTextFormats : int
    {
        Top = DT_TOP,
        Left = DT_LEFT,
        Center = DT_CENTER,
        Right = DT_RIGHT,
        VerticalCenter = DT_VCENTER,
        Bottom = DT_BOTTOM,
        WordBreak = DT_WORDBREAK,
        SingleLine = DT_SINGLELINE,
        ExpandTabs = DT_EXPANDTABS,
        TabStop = DT_TABSTOP,
        NoClip = DT_NOCLIP,
        ExternalLeading = DT_EXTERNALLEADING,
        CalcRect = DT_CALCRECT,
        NoPrefix = DT_NOPREFIX,
        Internal = DT_INTERNAL,
        EditControl = DT_EDITCONTROL,
        PathEllipsis = DT_PATH_ELLIPSIS,
        EndEllipsis = DT_END_ELLIPSIS,
        ModifyString = DT_MODIFYSTRING,
        RTLReading = DT_RTLREADING,
        WordEllipsis = DT_WORD_ELLIPSIS,
        NoFullWidthCharBreak = DT_NOFULLWIDTHCHARBREAK,
        HidePrefix = DT_HIDEPREFIX,
        PrefixOnly = DT_PREFIXONLY,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Rect
    {
        public int Left, Top, Right, Bottom;
        public Rect(Rectangle rect)
        {
            Left = rect.Left;
            Top = rect.Top;
            Right = rect.Right;
            Bottom = rect.Bottom;
        }
        public static implicit operator Rect(Rectangle rect) => new Rect(rect);
        public static implicit operator Rectangle(Rect rect) => new Rectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TEXTMETRICW
    {
        public int tmHeight;
        public int tmAscent;
        public int tmDescent;
        public int tmInternalLeading;
        public int tmExternalLeading;
        public int tmAveCharWidth;
        public int tmMaxCharWidth;
        public int tmWeight;
        public int tmOverhang;
        public int tmDigitizedAspectX;
        public int tmDigitizedAspectY;
        public ushort tmFirstChar;
        public ushort tmLastChar;
        public ushort tmDefaultChar;
        public ushort tmBreakChar;
        public byte tmItalic;
        public byte tmUnderlined;
        public byte tmStruckOut;
        public byte tmPitchAndFamily;
        public byte tmCharSet;
    }

    public enum BkMode
    {
        Error = 0,
        Transparent = 1,
        Opaque = 2
    }

    #endregion

    #region API声明

    [DllImport("gdi32.dll")]
    public extern static IntPtr CreateFont(int nHeight, int nWidth, int nEscapement,
        int nOrientation, int fnWeight, bool fdwItalic, bool fdwUnderline, bool fdwStrikeOut, int fdwCharSet,
        int fdwOutputPrecision, int fdwClipPrecision, int fdwQuality, int fdwPitchAndFamily, string lpszFace);

    [DllImport("gdi32.dll")]
    public static extern bool GetTextMetrics(this IntPtr hdc, out TEXTMETRICW lptm);

    [DllImport("gdi32.dll")]
    public extern static int TextOut(this IntPtr hdc, int nXStart, int nYStart, string lpString, int cbString);

    /// <summary>
    /// 选择对象到设备上下文
    /// </summary>
    /// <param name="hdc"></param>
    /// <param name="hgdiobj"></param>
    /// <returns>被替换的旧对象指针</returns>
    [DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
    public static extern IntPtr SelectObject(this IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(this IntPtr hObject);

    [DllImport("gdi32.dll")]
    public static extern int GetBkMode(this IntPtr hdc);

    [DllImport("gdi32.dll")]
    public extern static int SetBkMode(this IntPtr hdc, int flag);

    [DllImport("gdi32.dll")]
    public extern static int GetBkColor(this IntPtr hdc);

    [DllImport("gdi32.dll")]
    public extern static int SetBkColor(this IntPtr hdc, int abgr);

    [DllImport("gdi32.dll")]
    public extern static int GetTextColor(this IntPtr hdc);

    [DllImport("gdi32.dll")]
    public extern static int SetTextColor(this IntPtr hdc, int abgr);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int DrawText(HandleRef hDC, string lpchText, int nCount, ref Rect lpRect, DrawTextFormats uFormat);

    #endregion

    #region 辅助函数

    /// <summary>
    /// 选择GDI对象到GDI设备上下文
    /// </summary>
    /// <param name="hdc">GDI设备</param>
    /// <param name="hgdiobj">GDI对象</param>
    /// <param name="oldhgdiobj">被替换的旧GDI对象指针</param>
    public static void SelectObject(this IntPtr hdc, IntPtr hgdiobj, out IntPtr oldhgdiobj)
    => oldhgdiobj = SelectObject(hdc, hgdiobj);

    /// <summary>
    /// 创建GDI字体对象
    /// </summary>
    /// <param name="family"></param>
    /// <param name="size"></param>
    /// <param name="weight"></param>
    /// <param name="italic"></param>
    /// <param name="underline"></param>
    /// <param name="strike"></param>
    /// <returns>GDI字体对象指针</returns>
    public static IntPtr CreateFont(string family, int size, int weight = 400, bool italic = false, bool underline = false, bool strike = false)
    => CreateFont(-size, 0, 0, 0, weight, italic, underline, strike, 1, 0, 0, 0, 0, family);

    public static void TextOut(this IntPtr hdc, string text, int x = 0, int y = 0)
    => TextOut(hdc, x, y, text, text.Length);

    public static int DrawText(HandleRef hDC, string text, Rectangle rect, DrawTextFormats formats)
    {
        var r = new Rect(rect);
        return DrawText(hDC, text, text.Length, ref r, formats);
    }

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
            hFontDefault = SelectObject(hDC, hFont);

            GetTextMetrics(hDC, out TEXTMETRICW textMetric);

            return textMetric.tmCharSet;
        }
        finally
        {
            if (hFontDefault != IntPtr.Zero)
                SelectObject(hDC, hFontDefault);
            if (hFont != IntPtr.Zero)
                DeleteObject(hFont);
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
    => CreateFont(family, size, style.HasFlag(FontStyle.Bold) ? FW_BOLD : FW_NORMAL, style.HasFlag(FontStyle.Italic), style.HasFlag(FontStyle.Underline), style.HasFlag(FontStyle.Strikeout));

    public static void TextOut(this Graphics g, string text, IntPtr gdiFont, Color color, int x = 0, int y = 0, Color? bgColor = null)
    {
        var hdc = g.GetHdc();
        SetTextColor(hdc, GDIUtils.ARGB2ABGR(color.ToArgb()));
        if (bgColor != null)
            SetBkColor(hdc, bgColor.Value.ToABGR());
        SetBkMode(hdc, bgColor == null ? BKMODE_TRANSPARENT : BKMODE_OPAQUE);
        SelectObject(hdc, gdiFont);
        TextOut(hdc, text, x, y);
        g.ReleaseHdc(hdc);
    }

    #endregion

}
