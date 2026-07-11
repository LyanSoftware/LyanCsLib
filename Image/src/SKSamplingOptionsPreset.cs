using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;

namespace Lytec.Image;

public static partial class SKSamplingOptionsPreset
{
    public static SKSamplingOptions Default => SKSamplingOptions.Default;
    public static SKSamplingOptions Nearest { get; } = new SKSamplingOptions(SKFilterMode.Nearest);
    public static SKSamplingOptions Linear { get; } = new SKSamplingOptions(SKFilterMode.Linear);
}
