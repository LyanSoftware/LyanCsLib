using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using SkiaSharp;

namespace Lytec.Image;

[JsonConverter(typeof(ColorJsonConverter))]
[DebuggerDisplay("{" + nameof(DebugView) + ",nq}")]
public readonly partial struct Color : IEquatable<Color>
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public SKColor SKColor => new SKColor(R, G, B, A);

    public Color(byte r, byte g, byte b, byte a = 255) => (R, G, B, A) = (r, g, b, a);
    public Color(int r, int g, int b, int a = 255) => (R, G, B, A) = ((byte)r, (byte)g, (byte)b, (byte)a);
    public Color(SKColor color) : this(color.Red, color.Green, color.Blue, color.Alpha) { }
    public Color(Color color) : this(color.R, color.G, color.B, color.A) { }
    public void Deconstruct(out byte r, out byte g, out byte b, out byte a) => (r, g, b, a) = (R, G, B, A);

    public static implicit operator SKColor(Color color) => color.SKColor;
    public static implicit operator Color(SKColor color) => new(color);

    public Color(string colorName)
    {
        if (!Colors.TryGetValue(colorName, out var c))
            throw new ArgumentException("Unrecognized color name", nameof(colorName));
        R = c.R;
        G = c.G;
        B = c.B;
        A = c.A;
    }
    public static Color FromName(string colorName) => new(colorName);
    public static bool TryFromName(string colorName, out Color color) => Colors.TryGetValue(colorName, out color);

    public override bool Equals(object? obj) => obj is Color color && Equals(color);

    public bool Equals(Color other)
        => R == other.R &&
           G == other.G &&
           B == other.B &&
           A == other.A;

    public override int GetHashCode() => HashCode.Combine(R, G, B, A);

    public static bool operator ==(Color left, Color right) => left.Equals(right);
    public static bool operator !=(Color left, Color right) => !(left == right);

    public override string ToString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string DebugView => ToString();

    public byte GrayScale => (byte)(R * 0.299 + G * 0.587 + B * 0.114);

    /// <summary>
    /// 量化颜色通道
    /// </summary>
    /// <param name="v">通道值</param>
    /// <param name="bits">量化bit数, 1-7, >=8不量化返回原始值</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static byte Quantize(byte v, int bits)
    {
        // 1 <= bits <= 7
        if (bits >= 8)
            return v;
        if (bits < 1)
            throw new ArgumentException(nameof(bits));

        // 离散级别数
        int levels = 1 << bits;
        var maxlv = levels - 1;
        // 先求最近的色阶索引
        var index = (v * maxlv + 127) / 255;
        // 再把索引映射回 0~255
        return (byte)((index * 255 + maxlv / 2) / maxlv);
    }

    /// <summary>
    /// 量化颜色
    /// </summary>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public Color Quantize(int r, int g, int b, int a = 8)
    => new Color(
        Quantize(R, r),
        Quantize(G, g),
        Quantize(B, b),
        Quantize(A, a)
    );
}
