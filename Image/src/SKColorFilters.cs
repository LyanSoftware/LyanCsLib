using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;

namespace Lytec.Image;

public static class SKColorFilters
{
    public static readonly SKColorFilter Monochrome = SKColorFilter.CreateColorMatrix(new float[]
    {
        /*         R       G       B    A  Off */
        /* R */ 0.299f, 0.587f, 0.114f, 0, 0,
        /* G */ 0.299f, 0.587f, 0.114f, 0, 0,
        /* B */ 0.299f, 0.587f, 0.114f, 0, 0,
        /* A */    0,      0,      0,   1, 0,
    });

    public static SKColorFilter CreateColoring(SKColor color)
    => SKColorFilter.CreateColorMatrix(new float[]
    {
        color.GetRedF(),   0, 0, 0, 0,
        0, color.GetGreenF(), 0, 0, 0,
        0, 0, color.GetBlueF(),  0, 0,
        0, 0, 0, color.GetAlphaF(), 0
    });
}
