using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;

namespace Lytec.Image
{
    public readonly struct Color
    {
        public SKColor SKColor { get; }
        public Color(SKColor color) => SKColor = color;
        public Color(Color color) => SKColor = color.SKColor;
        public static implicit operator SKColor(Color color) => color.SKColor;
        public static implicit operator Color(SKColor color) => new(color);
    }
}
