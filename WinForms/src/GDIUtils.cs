using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;

namespace Lytec.WinForms;

public static partial class GDIUtils
{
    public const string GdiPlusImageFilter = "*.jpg; *.png; *.bmp; *.gif; *.tiff";

    public static int ARGB2ABGR(int argb) => (int)(argb & 0xff00ff00) | ((argb >> 16) & 0xff) | ((argb & 0xff) << 16);

    public static int ABGR2ARGB(int abgr) => ARGB2ABGR(abgr);

    public static int ARGB2RGBA(int argb) => (argb >> 24) | ((argb >> 8) & 0xff00) | ((argb & 0xff00) << 8) | ((argb & 0xff) << 24);

    public static int RGBA2ARGB(int rgba) => ARGB2RGBA(rgba);

    public static int ToABGR(this Color color) => ARGB2ABGR(color.ToArgb());

    public static int ToRGBA(this Color color) => ARGB2RGBA(color.ToArgb());

    public static void Deconstruct(this Size size, out int Width, out int Height) => (Width, Height) = (size.Width, size.Height);
    public static void Deconstruct(this Point point, out int X, out int Y) => (X, Y) = (point.X, point.Y);
    public static void Deconstruct(this SizeF size, out float Width, out float Height) => (Width, Height) = (size.Width, size.Height);
    public static void Deconstruct(this PointF point, out float X, out float Y) => (X, Y) = (point.X, point.Y);
    public static void Deconstruct(this Rectangle rect, out int X, out int Y, out int Width, out int Height) => (X, Y, Width, Height) = (rect.X, rect.Y, rect.Width, rect.Height);
    public static void Deconstruct(this Rectangle rect, out Point Location, out Size Size) => (Location, Size) = (rect.Location, rect.Size);
    public static void Deconstruct(this RectangleF rect, out float X, out float Y, out float Width, out float Height) => (X, Y, Width, Height) = (rect.X, rect.Y, rect.Width, rect.Height);
    public static void Deconstruct(this RectangleF rect, out PointF Location, out SizeF Size) => (Location, Size) = (rect.Location, rect.Size);

    public static readonly InstalledFontCollection InstalledFontCollection = new InstalledFontCollection();

    public static readonly FontFamily[] InstalledFontFamilies = InstalledFontCollection.Families;

    public static readonly string[] InstalledFontNames = (from ff in InstalledFontFamilies select ff.Name).ToArray();

    /// <summary>
    /// 将CellHeight转换为EmSize
    /// </summary>
    /// <param name="ff"></param>
    /// <param name="fs"></param>
    /// <param name="cellHeight"></param>
    /// <returns></returns>
    public static float GetEmSize(this FontFamily ff, FontStyle fs, float cellHeight)
    => cellHeight / (ff.GetCellAscent(fs) + ff.GetCellDescent(fs)) * ff.GetEmHeight(fs);

    /// <summary>
    /// 将CellHeight转换为EmSize
    /// </summary>
    /// <param name="font"></param>
    /// <param name="cellHeight"></param>
    /// <returns></returns>
    public static float GetEmSize(this Font font, float cellHeight) => GetEmSize(font.FontFamily, font.Style, cellHeight);

    /// <summary>
    /// 将EmSize转换为CellHeight
    /// </summary>
    /// <param name="ff"></param>
    /// <param name="fs"></param>
    /// <param name="emSize"></param>
    /// <returns></returns>
    public static float GetCellHeight(this FontFamily ff, FontStyle fs, float emSize)
    => emSize * ff.GetEmHeight(fs) / (ff.GetCellAscent(fs) + ff.GetCellDescent(fs));

    /// <summary>
    /// 将EmSize转换为CellHeight
    /// </summary>
    /// <param name="font"></param>
    /// <returns></returns>
    public static float GetCellHeight(this Font font) => GetCellHeight(font.FontFamily, font.Style, font.Size);

    public static float PixelToPointX(this Graphics g, float px) => px * 72 / g.DpiX;
    public static float PixelToPointY(this Graphics g, float px) => px * 72 / g.DpiY;
    public static float PointToPixelX(this Graphics g, float pt) => pt * g.DpiX / 72;
    public static float PointToPixelY(this Graphics g, float pt) => pt * g.DpiY / 72;

    /// <summary>
    /// 设置抗锯齿：将<see cref="Graphics.SmoothingMode"/>设为<see cref="SmoothingMode.HighQuality"/>（开启）或<see cref="SmoothingMode.None"/>（关闭）。
    /// </summary>
    /// <param name="g"></param>
    /// <param name="aa"></param>
    public static void SetAntiAlias(this Graphics g, bool aa) => g.SmoothingMode = aa ? SmoothingMode.HighQuality : SmoothingMode.None;

    /// <summary>
    /// 设置文字抗锯齿：将<see cref="Graphics.TextRenderingHint"/>设为<see cref="TextRenderingHint.AntiAlias"/>（开启）或<see cref="TextRenderingHint.SingleBitPerPixel"/>（关闭）。
    /// </summary>
    /// <param name="g"></param>
    /// <param name="aa"></param>
    public static void SetTextAntiAlias(this Graphics g, bool taa) => g.TextRenderingHint = taa ? TextRenderingHint.AntiAlias : TextRenderingHint.SingleBitPerPixel;

    /// <summary>
    /// 测量用指定的<see cref="Font"/>绘制的指定字符串。
    /// </summary>
    /// <param name="font">字体</param>
    /// <param name="text">字符串</param>
    /// <param name="g">GDI+</param>
    /// <param name="stringFormat">字符串格式</param>
    /// <returns><paramref name="text"/>的实际大小，单位由<see cref="Graphics.PageUnit"/>属性指定。</returns>
    public static SizeF GetStringSize(this Font font, string text, Graphics g, StringFormat? stringFormat = null) => g.MeasureString(text, font, int.MaxValue, stringFormat);

    /// <summary>
    /// 测量用指定的<see cref="Font"/>绘制的指定字符。
    /// </summary>
    /// <param name="g">GDI+</param>
    /// <param name="chr">字符</param>
    /// <returns><paramref name="chr"/>的实际大小，单位由<see cref="Graphics.PageUnit"/>属性指定。</returns>
    public static SizeF GetStringSize(this Font font, char chr, Graphics g, StringFormat? stringFormat = null) => GetStringSize(font, chr.ToString(), g, stringFormat);

}
