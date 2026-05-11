using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;

namespace Lytec.Image
{
    public readonly struct Color
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }

        public SKColor SKColor => new SKColor(R, G, B, A);

        public Color(byte r, byte g, byte b, byte a) => (R, G, B, A) = (r, g, b, a);
        public Color(SKColor color) : this(color.Red, color.Green, color.Blue, color.Alpha) { }
        public Color(Color color) : this(color.R, color.G, color.B, color.A) { }
        public void Deconstruct(out byte r, out byte g, out byte b, out byte a) => (r, g, b, a) = (R, G, B, A);

        public static implicit operator SKColor(Color color) => color.SKColor;
        public static implicit operator Color(SKColor color) => new(color);
    }
}
