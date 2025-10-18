using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;

namespace Lytec.Image
{
    public record FontTypefaceStyles(int Weight, int Width, SKFontStyleSlant Slant)
    {
        public FontTypefaceStyles(SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant) : this((int)weight, (int)width, slant) { }
    }
}
