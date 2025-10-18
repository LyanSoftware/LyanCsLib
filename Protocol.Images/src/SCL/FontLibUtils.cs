using System.Linq.Expressions;
using System.Text;
using SkiaSharp;
using Lytec.Common;
using Newtonsoft.Json.Linq;
using Lytec.Image;
using static Lytec.Protocol.SCL.Constants;
using Lytec.Common.Text.Encoding;
using HarfBuzzSharp;

namespace Lytec.Protocol.Images.SCL;

public static class FontLibUtils
{
    public static readonly Encoding Latin1Encoding = Encoding.GetEncoding("ISO-8859-1");
    public static readonly Encoding SystemEncoding = Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage) ?? Encoding.Default;

    public static Expression<Func<CharImg, bool>> GetCharImg(int chr)
    => cg => cg.Char == chr;

    public static SKBitmap GetCharImage(this FontInfo font, string str, int index = 0, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null, Encoding? encoding = default)
    => font.GetCharImage(str.GetUtf32Char(index), FontFileTypefaces, encoding);
    public static SKBitmap GetCharImage(this FontInfo font, int chr, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null, Encoding? encoding = default)
    {
        CharImg? img;
        try
        {
            img = font.ModifiedChars.AsQueryable().FirstOrDefault(GetCharImg(chr));
        }
        catch (Exception)
        {
            img = font.ModifiedChars.FirstOrDefault(GetCharImg(chr).Compile());
        }
        return img == default ? font.GenerateCharImage(font.GetFont(chr, FontFileTypefaces), chr, encoding: encoding) : SKBitmap.Decode(img.ImageData);
    }

    public static IReadOnlyDictionary<string, string> FontFamilyLocalNames { get; private set; }
    public static IReadOnlyDictionary<string, string> FontFamilyNames { get; private set; }
    public static SKFontManager FontManager
    {
        get => _FontManager;
        set
        {
            _FontManager = value;
            FontFamilyLocalNames = value.FontFamilies.ToDictionary(f => value.MatchFamily(f).FamilyName);
            FontFamilyNames = FontFamilyLocalNames.ToDictionary(f => f.Value, f => f.Key);
        }
    }
    private static SKFontManager _FontManager;

    public static class Fonts
    {
        public static SKTypeface SimSun { get; } = SKTypeface.FromFamilyName(nameof(SimSun));
        public static SKTypeface Arial { get; } = SKTypeface.FromFamilyName(nameof(Arial));
    }

    static FontLibUtils()
    {
        var fm = SKFontManager.Default;
        FontFamilyLocalNames = fm.FontFamilies.ToDictionary(f => fm.MatchFamily(f).FamilyName);
        FontFamilyNames = FontFamilyLocalNames.ToDictionary(f => f.Value, f => f.Key);
        _FontManager = fm;
    }

    public static SKTypeface? GetFont(this FontInfo lib, string str, int index = 0, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null)
    => lib.GetFont(str.GetUtf32Char(index), FontFileTypefaces);
    public static SKTypeface? GetFont(this FontInfo lib, int chr, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null)
    {
        SKTypeface font;
        if (lib.UseFontFile)
        {
            if (FontFileTypefaces == null)
                return null;
            font = FontFileTypefaces.FirstOrDefault(kv => kv.Key.Weight == (int)lib.Weight && kv.Key.Slant == lib.Slant).Value;
            if (font == null)
                font = FontFileTypefaces.FirstOrDefault(kv => kv.Key.Weight == (int)lib.Weight).Value;
            if (font == null)
                font = FontFileTypefaces.FirstOrDefault(kv => kv.Key.Slant == lib.Slant).Value;
            if (font == null)
                font = FontFileTypefaces.FirstOrDefault().Value;
            if (font == null)
                return null;
        }
        else font = SKTypeface.FromFamilyName(lib.Font, lib.Weight, SKFontStyleWidth.Normal, lib.Slant);
        if (font?.GetGlyph(chr) == 0)
        {
            // 字体回退
            if (!lib.UseFontFile)
                font.Dispose();
            font = FontManager.MatchCharacter(chr);
        }
        return font;
    }

    public static SKBitmap GenerateCharImage(this FontInfo info, string str, int index = 0, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null, Color? foreColor = null, Color? backColor = null, Encoding? encoding = default)
    => info.GenerateCharImage(str.GetUtf32Char(index), FontFileTypefaces, foreColor, backColor, encoding);
    public static SKBitmap GenerateCharImage(this FontInfo info, int chr, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null, Color? foreColor = null, Color? backColor = null, Encoding? encoding = default)
    => info.GenerateCharImage(info.GetFont(chr, FontFileTypefaces), chr, foreColor, backColor, encoding);

    private static SKBitmap GenerateCharImage(this FontInfo info, SKTypeface? typeface, int chr, Color? foreColor = null, Color? backColor = null, Encoding? encoding = default)
    {
        var fgc = foreColor ?? SKColors.White;
        var bgc = backColor ?? SKColors.Black;
        var txt = char.ConvertFromUtf32(chr);
        var w = info.Size;
        if (encoding == default)
        {
            static bool isHalfwidth(byte[] buf1) => buf1.Length == 1 && buf1[0] != 0x3F;
            if (chr < 0x80 || isHalfwidth(Latin1Encoding.GetBytes(txt)) || isHalfwidth(SystemEncoding.GetBytes(txt)))
                w /= 2;
        }
        else if (encoding.GetByteCount(txt) == 1)
            w /= 2;
        SKBitmap blank()
        {
            var _blank = new SKBitmap(w, info.Size);
            using (var canvas = new SKCanvas(_blank))
                canvas.Clear(bgc);
            return _blank;
        }
        if (chr == 0 || typeface == null)
            return blank();
        using var font = new SKFont()
        {
            Typeface = typeface,
            Size = info.FontSize,
            Subpixel = info.Antialias,
            Edging = info.Antialias ? SKFontEdging.SubpixelAntialias : SKFontEdging.Alias,
            Hinting = info.Antialias ? SKFontHinting.Full : SKFontHinting.None,
        };
        using var paint = new SKPaint()
        {
            IsAntialias = info.Antialias,
            Typeface = font.Typeface,
            TextSize = font.Size,
            Color = fgc,
            FilterQuality = info.Antialias ? SKFilterQuality.High : SKFilterQuality.None,
        };
        var recommendedLineHeight = paint.GetFontMetrics(out var fm);
        var baseline = -fm.Ascent;
        var lineHeight = -fm.Ascent + fm.Descent;
        var bounds = new SKRect();
        var chrw = paint.MeasureText(txt, ref bounds);
        if (chrw <= 0)
            return blank();
        //var top = info.Overline ? 0 : (bounds.Top + baseline);
        bounds = new(
            0,
            //top,
            0,
            chrw,
            //Math.Abs(top % 1) + (float)Math.Ceiling(baseline + Math.Max(info.Underline ? (fm.UnderlinePosition ?? 0) : 0, bounds.Bottom))
            lineHeight
            );
        using var tbmp = new SKBitmap((int)Math.Ceiling(chrw), (int)Math.Ceiling(lineHeight));
        using (var canvas = new SKCanvas(tbmp))
        {
            canvas.Clear(bgc);
            canvas.DrawText(txt, 0, baseline, font, paint);
            if (info.Underline)
            {
                var ly = baseline + (fm.UnderlinePosition ?? 0);
                canvas.DrawLine(bounds.Left, ly, bounds.Right, ly, paint);
            }
            if (info.Strikeout)
            {
                var ly = baseline + (fm.StrikeoutPosition ?? 0);
                canvas.DrawLine(bounds.Left, ly, bounds.Right, ly, paint);
            }
            if (info.Overline)
            {
                var ly = bounds.Top >= 1 ? bounds.Top - 1 : bounds.Top;
                canvas.DrawLine(bounds.Left, ly, bounds.Right, ly, paint);
            }
            if (info.Baseline)
                canvas.DrawLine(bounds.Left, baseline, bounds.Right, baseline, paint);
        }
        var bmp = new SKBitmap(w, info.Size);
        using (var canvas = new SKCanvas(bmp))
        {
            canvas.Clear(bgc);
            //var src = new SKRect(
            //    bounds.Left + (bounds.Width > bmp.Width ? (bounds.Width - bmp.Width) / 2 : 0),
            //    bounds.Top + (bounds.Height > bmp.Height ? (bounds.Height - bmp.Height) / 2 : 0),
            //    bounds.Left + (bounds.Width > bmp.Width ? (bounds.Width - bmp.Width) / 2 + bmp.Width : bounds.Width),
            //    bounds.Top + (bounds.Height > bmp.Height ? (bounds.Height - bmp.Height) / 2 + bmp.Height : bounds.Height)
            //    );
            //var dst = new SKRect(
            //    bounds.Width > bmp.Width ? 0 : (bmp.Width - bounds.Width) / 2,
            //    bounds.Height > bmp.Height ? 0 : (bmp.Height - bounds.Height) / 2,
            //    bounds.Width > bmp.Width ? bmp.Width : (bmp.Width - bounds.Width) / 2 + bounds.Width,
            //    bounds.Height > bmp.Height ? bmp.Height : (bmp.Height - bounds.Height) / 2 + bounds.Height
            //    );
            var dst = new SKRect(
                bounds.Width > bmp.Width ? 0 : (bmp.Width - bounds.Width) / 2,
                bounds.Height > bmp.Height ? 0 : (bmp.Height - bounds.Height) / 2,
                bounds.Width > bmp.Width ? bmp.Width : (bmp.Width - bounds.Width) / 2 + bounds.Width,
                bounds.Height > bmp.Height ? bmp.Height : (bmp.Height - bounds.Height) / 2 + bounds.Height
                );
            canvas.DrawBitmap(tbmp, bounds, dst, paint);
        }
        return bmp;
    }

    private static int ExportToSCLFormat_FillCharBitmap(SKBitmap bmp, byte[] buff, int offset)
    {
        var i = offset;
        for (var x = 0; x < bmp.Width; x++)
        {
            for (var y = 0; y < bmp.Height;)
            {
                var t = 0;
                for (var b = 0; b < 8; b++, y++)
                    t |= (bmp.GetPixel(x, y).GetGrayScale() > 127 ? 1 : 0) << (7 - b);
                buff[i++] = (byte)t;
            }
        }
        return i - offset;
    }

    public static int GetSCLFormatBytesCountPerChar(this FontInfo font, bool is8BitAnsiOrASCII = true)
    => (font.Size + 7) / 8 * (is8BitAnsiOrASCII ? font.Size / 2 : font.Size);

    public static byte[] ExportToSCLFormat_8BitAnsi(this FontInfo font, byte codeEnd, Encoding encoding)
    {
        var count = codeEnd + 1;
        var bytesPerChar = font.GetSCLFormatBytesCountPerChar(true);
        var buff = new byte[count * bytesPerChar];
        for (int chr = 0; chr <= codeEnd; chr++)
        {
            using var bmp = font.GetCharImage(chr, encoding: encoding);
            ExportToSCLFormat_FillCharBitmap(bmp, buff, chr * bytesPerChar);
        }
        return buff;
    }

    public static IList<FontLib> ExportSBCS(this FontInfo font, Encoding encoding, byte codeEnd = 0xFF, bool average = false)
    {
        var count = codeEnd + 1;
        var chrsize = GetSCLFormatBytesCountPerChar(font, true);
        var filemaxblock = (int)Math.Floor(MaxFileSize / (float)chrsize);
        if (filemaxblock >= count)
        {
            var buff = new byte[count * chrsize];
            for (int chr = 0; chr <= codeEnd; chr++)
            {
                var ucode = char.ConvertToUtf32(encoding.GetString(new byte[] { (byte)chr }), 0);
                using var bmp = font.GetCharImage(ucode, encoding: encoding);
                ExportToSCLFormat_FillCharBitmap(bmp, buff, chr * chrsize);
            }
            return new List<FontLib>()
            {
                new FontLib(font, encoding, true, font.Size / 2, font.Size, count, new(encoding, 0, codeEnd, 0, 0), buff)
            };
        }
        else throw new NotImplementedException();
    }
    public static IList<FontLib> ExportDBCS(this FontInfo font, Encoding encoding, byte byte1Start = 0xA0, byte byte1End = 0xFF, byte byte2Start = 0xA0, byte byte2End = 0xFF, bool average = false)
    {
        var byte1Count = byte1End - byte1Start + 1;
        var byte2Count = byte2End - byte2Start + 1;
        var chrsize = GetSCLFormatBytesCountPerChar(font, false);
        var filemaxblock = (int)Math.Floor(MaxFileSize / (float)(byte2Count * chrsize));
        var filecount = (int)Math.Ceiling(byte1Count / (float)filemaxblock);
        if (average)
            filemaxblock = (int)Math.Ceiling(byte1Count / (float)filecount);

        if (filecount > 1)
            throw new NotImplementedException();

        var count = (byte1End - byte1Start + 1) * (byte2End - byte2Start + 1);
        var buff = new byte[count * chrsize];
        var offset = 0;
        for (int b1 = byte1Start; b1 <= byte1End; b1++)
        {
            for (int b2 = byte2Start; b2 <= byte2End; b2++)
            {
                var chr = encoding.GetString(new byte[] { (byte)b1, (byte)b2 }).GetUtf32Char();
#if DEBUG
                var str = char.ConvertFromUtf32(chr);
#endif
                if (chr != 0 && chr != Rune.ReplacementChar.Value)
                {
                    using var bmp = font.GetCharImage(chr);
                    ExportToSCLFormat_FillCharBitmap(bmp, buff, offset);
                }
                offset += chrsize;
            }
        }
        return new List<FontLib>()
        {
            new FontLib(font, encoding, false, font.Size, font.Size, count, new(encoding, byte1Start, byte1End, byte2Start, byte2End), buff)
        };
    }
}
