using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;
using GDIColor = System.Drawing.Color;

namespace Lytec.Image.GDIPlus;

public static class ColorUtils
{
    public static GDIColor ToGDIColor(this SKColor c) => GDIColor.FromArgb(c.Alpha, c.Red, c.Green, c.Blue);
    public static SKColor ToSKColor(this GDIColor c) => new SKColor(c.R, c.G, c.B, c.A);
    public static Color ToColor(this GDIColor c) => new SKColor(c.R, c.G, c.B, c.A);
}
