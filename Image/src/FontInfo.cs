using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using SkiaSharp;

namespace Lytec.Image
{

    public class FontInfo
    {
        public ISet<CharImg> ModifiedChars { get; set; } = new HashSet<CharImg>();

        public string Font { get; set; } = "";

        public bool UseFontFile { get; set; }

        /// <summary> 字库字符尺寸（高度，像素） </summary>
        public int Size { get; set; }

        /// <summary> 基于字体字号（像素） </summary>
        public int FontSize { get; set; }

        public SKFontStyleWeight Weight { get; set; } = SKFontStyleWeight.Normal;

        public SKFontStyleSlant Slant { get; set; } = SKFontStyleSlant.Upright;

        public FontOptions Options { get; set; } = FontOptions.None;

        public bool Underline => Options.HasFlag(FontOptions.Underline);

        public bool Strikeout => Options.HasFlag(FontOptions.Strikeout);

        public bool Overline => Options.HasFlag(FontOptions.Overline);

        public bool Baseline => Options.HasFlag(FontOptions.Baseline);

        public bool Antialias => Options.HasFlag(FontOptions.Antialias);
    }
}
