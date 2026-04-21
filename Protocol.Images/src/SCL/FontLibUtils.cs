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

    static CharImg? GetCharImg(this FontInfo font, int chr, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null)
    {
        try
        {
            return font.ModifiedChars.AsQueryable().FirstOrDefault(GetCharImg(chr));
        }
        catch (Exception)
        {
            return font.ModifiedChars.FirstOrDefault(GetCharImg(chr).Compile());
        }
    }
    public static SKBitmap GetCharImage(this FontInfo font, string str, int index = 0, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null)
    => font.GetCharImage(str.GetUtf32Char(index), FontFileTypefaces);
    public static SKBitmap GetCharImage(this FontInfo font, int chr, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null)
    {
        var img = font.GetCharImg(chr, FontFileTypefaces);
        return img == default ? font.GenerateCharImage(font.GetFont(chr, FontFileTypefaces), chr) : SKBitmap.Decode(img.ImageData);
    }

    public static SKBitmap GetCharImageWithString(this FontInfo font, string renderStr, string str, int index = 0, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null)
    => font.GetCharImageWithString(renderStr, str.GetUtf32Char(0), FontFileTypefaces);
    public static SKBitmap GetCharImageWithString(this FontInfo font, string renderStr, int chr, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null)
    {
        var img = font.GetCharImg(chr, FontFileTypefaces);
        return img == default ? font.GenerateStrImage(renderStr, FontFileTypefaces) : SKBitmap.Decode(img.ImageData);
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

    public static SKBitmap GenerateCharImage(this FontInfo info, string str, int index = 0, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null, Color? foreColor = null, Color? backColor = null)
    => info.GenerateCharImage(str.GetUtf32Char(index), FontFileTypefaces, foreColor, backColor);
    public static SKBitmap GenerateCharImage(this FontInfo info, int chr, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null, Color? foreColor = null, Color? backColor = null)
    => info.GenerateCharImage(info.GetFont(chr, FontFileTypefaces), chr, foreColor, backColor);

    private static SKBitmap GenerateCharImage(this FontInfo info, SKTypeface? typeface, int chr, Color? foreColor = null, Color? backColor = null)
    {
        var fgc = foreColor ?? SKColors.White;
        var bgc = backColor ?? SKColors.Black;
        var txt = char.ConvertFromUtf32(chr);
        SKBitmap blank()
        {
            var _blank = new SKBitmap(info.Width, info.Height);
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
        var bmp = new SKBitmap(info.Width, info.Height);
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

    class RenderCharInfoCache : IDisposable
    {
        public bool IsDisposed { get; private set; }


        public int chr;
        public SKFont font { get; }
        public SKPaint paint { get; }
        public float recommendedLineHeight;
        public SKFontMetrics metrics;
        public float baseline;
        public float lineHeight;
        public SKRect bounds;
        public float chrw;

        public RenderCharInfoCache(SKFont font, SKPaint paint)
        {
            this.font = font;
            this.paint = paint;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    font.Dispose();
                    paint.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                IsDisposed = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~RenderCharInfo()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
    public static SKBitmap GenerateStrImage(this FontInfo info, string renderStr, IReadOnlyDictionary<FontTypefaceStyles, SKTypeface>? FontFileTypefaces = null, Color? foreColor = null, Color? backColor = null)
    {
        var fgc = foreColor ?? SKColors.White;
        var bgc = backColor ?? SKColors.Black;
        var txt = renderStr;
        SKBitmap blank()
        {
            var _blank = new SKBitmap(info.Width, info.Height);
            using (var canvas = new SKCanvas(_blank))
                canvas.Clear(bgc);
            return _blank;
        }
        var chrs = txt.ToUtf32CharArray();
        if (chrs.Length < 1)
            return blank();
        var cache = new List<RenderCharInfoCache>();
        foreach (var chr in chrs)
        {
            var typeface = info.GetFont(chr, FontFileTypefaces);
            if (typeface == null)
                continue;
            var font = new SKFont()
            {
                Typeface = typeface,
                Size = info.FontSize,
                Subpixel = info.Antialias,
                Edging = info.Antialias ? SKFontEdging.SubpixelAntialias : SKFontEdging.Alias,
                Hinting = SKFontHinting.Full,
                EmbeddedBitmaps = true,
            };
            var paint = new SKPaint()
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
                continue;
            //var top = info.Overline ? 0 : (bounds.Top + baseline);
            bounds = new(
                0,
                //top,
                0,
                chrw,
                //Math.Abs(top % 1) + (float)Math.Ceiling(baseline + Math.Max(info.Underline ? (fm.UnderlinePosition ?? 0) : 0, bounds.Bottom))
                lineHeight
                );

            cache.Add(new RenderCharInfoCache(font, paint)
            {
                chr = chr,
                recommendedLineHeight = recommendedLineHeight,
                metrics = fm,
                baseline = baseline,
                lineHeight = lineHeight,
                bounds = bounds,
                chrw = chrw,
            });
        }
        if (cache.Count < 1)
            return blank();
        {
            var tw = cache.Sum(v => v.chrw);
            var lineHeight = cache.Max(v => v.lineHeight);
            var baseline = cache.OrderBy(v => v.lineHeight).First().baseline;
            var bounds = new SKRect(
                cache.Min(v => v.bounds.Left),
                cache.Min(v => v.bounds.Top),
                cache.Max(v => v.bounds.Right),
                cache.Max(v => v.bounds.Bottom)
                );
            using var tbmp = new SKBitmap((int)Math.Ceiling(tw), (int)Math.Ceiling(lineHeight));
            using (var canvas = new SKCanvas(tbmp))
            {
                canvas.Clear(bgc);
                var dx = 0f;
                foreach (var v in cache)
                {
                    canvas.DrawText(char.ConvertFromUtf32(v.chr), dx, baseline, v.font, v.paint);
                    dx += v.chrw;
                    if (info.Underline)
                    {
                        var ly = baseline + (v.metrics.UnderlinePosition ?? 0);
                        canvas.DrawLine(v.bounds.Left, ly, v.bounds.Right, ly, v.paint);
                    }
                    if (info.Strikeout)
                    {
                        var ly = baseline + (v.metrics.StrikeoutPosition ?? 0);
                        canvas.DrawLine(v.bounds.Left, ly, v.bounds.Right, ly, v.paint);
                    }
                    if (info.Overline)
                    {
                        var ly = v.bounds.Top >= 1 ? v.bounds.Top - 1 : v.bounds.Top;
                        canvas.DrawLine(v.bounds.Left, ly, v.bounds.Right, ly, v.paint);
                    }
                    if (info.Baseline)
                        canvas.DrawLine(v.bounds.Left, baseline, v.bounds.Right, baseline, v.paint);
                }
            }
            var bmp = new SKBitmap(info.Width, info.Height);
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
                canvas.DrawBitmap(tbmp, bounds, dst);
            }
            return bmp;
        }
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

    public static int GetSCLFormatBytesCountPerChar(this FontInfo font)
    => (font.Height + 7) / 8 * font.Width;

    public static FontLib ExportSBCS(this FontInfo font, Encoding encoding, byte codeEnd = 0xFF)
    {
        var count = codeEnd + 1;
        var chrsize = font.GetSCLFormatBytesCountPerChar();

        var filemaxblock = (int)Math.Floor(MaxFileSize / (float)chrsize);
        if (filemaxblock < count)
            throw new NotSupportedException();
        var buff = new byte[count * chrsize];
        for (int chr = 0; chr <= codeEnd; chr++)
        {
            var str = encoding.GetString(new byte[] { (byte)chr });
            using var bmp = font.GetCharImageWithString(str, chr);
            ExportToSCLFormat_FillCharBitmap(bmp, buff, chr * chrsize);
        }
        return new FontLib(font, encoding, true, font.Width, font.Height, count, new(encoding, 0, codeEnd, 0, 0), buff);
    }
    public const byte Byte2End = 0xFF;
    public static IEnumerable<(int CodePoint, byte[] Data)> GetEncodingTable(this Encoding enc, IEnumerable<byte[]> codes)
    {
        var cps = new HashSet<int>();
        int dec(byte[] buf)
        {
            var str = enc.GetString(buf);
            return str.Length < 1 ? 0 : char.ConvertToUtf32(str, 0);
        }
        foreach (var buf in codes)
        {
            var ch = dec(buf);
            cps.Add(ch);
            yield return (ch, buf);
        }
        if (enc is CustomEncoding enc1)
        {
            foreach (var cp in enc1.ContainsCodePoints)
            {
                if (!cps.Contains(cp))
                {
                    cps.Add(cp);
                    yield return (cp, enc.GetBytes(char.ConvertFromUtf32(cp)));
                }
            }
        }
    }
    public static IEnumerable<(int CodePoint, byte Data)> GetSBCSEncodingTable(this Encoding enc, byte codeEnd = 0xFF)
    => enc.GetEncodingTable(Enumerable.Range(0, codeEnd + 1).Select(b => new byte[] { (byte)b }))
        .Select(d => (d.CodePoint, d.Data[0]));
    public static IEnumerable<(int CodePoint, ushort Data)> GetDBCSEncodingTable(this Encoding enc, byte byte1Start = 0xA0, byte byte1End = 0xFF, byte byte2Start = 0xA0)
    => enc.GetEncodingTable(from b1 in Enumerable.Range(byte1Start, byte1End + 1 - byte1Start)
                            from b2 in Enumerable.Range(byte2Start, Byte2End + 1 - byte2Start)
                            select new byte[] { (byte)b1, (byte)b2 })
        .Select(d => (d.CodePoint, d.Data.Length > 1 ? ((ushort)(d.Data[0] | (d.Data[1] << 8))) : d.Data[0]));
    public static int GetDBCSSplitCount(this FontInfo font, byte byte1Start = 0xA0, byte byte1End = 0xFF, byte byte2Start = 0xA0)
    {
        var byte1Count = byte1End - byte1Start + 1;
        var byte2Count = Byte2End - byte2Start + 1;
        var chrsize = font.GetSCLFormatBytesCountPerChar();

        var filemaxblock = (int)Math.Floor(MaxFileSize / (float)(byte2Count * chrsize));
        return (int)Math.Ceiling(byte1Count / (float)filemaxblock);
    }
    public static IEnumerable<FontLib> ExportDBCS(this FontInfo font, Encoding encoding, byte byte1Start = 0xA0, byte byte1End = 0xFF, byte byte2Start = 0xA0, bool split = false, bool average = true)
    {
        const byte byte2End = Byte2End;
        var byte1Count = byte1End - byte1Start + 1;
        var byte2Count = byte2End - byte2Start + 1;
        var chrsize = font.GetSCLFormatBytesCountPerChar();
        var blocksize = byte2Count * chrsize;
        var filemaxblock = split ? (int)Math.Floor(MaxFileSize / (float)blocksize) : byte1Count;
        var filecount = split ? (int)Math.Ceiling(byte1Count / (float)filemaxblock) : 1;
        if (split && average)
            filemaxblock = (int)Math.Ceiling(byte1Count / (float)filecount);

        for (int block = byte1Start; block <= byte1End; block += filemaxblock)
        {
            var buff = new byte[filemaxblock * blocksize];
            var offset = 0;
            for (var b1 = block; b1 < (block + filemaxblock); b1++)
            {
                for (int b2 = byte2Start; b2 <= byte2End; b2++)
                {
                    var str = encoding.GetString(new byte[] { (byte)b1, (byte)b2 });
                    var chr = str.GetUtf32Char();
                    if (chr != 0 && chr != Rune.ReplacementChar.Value)
                    {
                        using var bmp = font.GetCharImageWithString(str, b1 | (b2 << 8));
                        ExportToSCLFormat_FillCharBitmap(bmp, buff, offset);
                    }
                    offset += chrsize;
                }
            }
            yield return new FontLib(
                font,
                encoding,
                false,
                font.Width,
                font.Height,
                (byte1End + 1 - block) * byte2Count,
                new(encoding, (byte)block, byte1End, byte2Start, byte2End),
                buff);
        }
    }
}
